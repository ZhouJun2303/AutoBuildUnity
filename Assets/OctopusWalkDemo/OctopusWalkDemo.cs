using UnityEngine;

namespace OctopusWalk
{
    /// <summary>
    /// Playable cube terrain for the ported octopus gait.
    /// The body is the ZhangYu1 model and textures in this folder.
    /// WASD moves, Q/E turns in place, Shift sprints, right mouse orbits, scroll zooms.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(20000)]
    public sealed class OctopusWalkDemo : MonoBehaviour
    {
        [Header("Movement")]
        public float WalkSpeed = 0.8f;
        public float SprintSpeed = 1.6f;
        public float TurnSpeed = 120f;

        [Header("Camera")]
        public float CameraPitch = 32f;
        public float CameraDistance = 5f;

        [Header("Model")]
        public GameObject OctopusModel;
        public Material OctopusMaterial;

        // Matches OctopusLocomotionPreview: SkinRoot scale, root clearance, and camera.
        private const float ReferenceVisualScale = 1.12f;
        private const float ReferenceBodyClearance = 0.8f;
        private const float Cell = 2f;
        private const int HalfCells = 9;
        private const string ModelPath = "Assets/OctopusWalkDemo/ZhangYu1_C.fbx";
        private const string MaterialPath = "Assets/OctopusWalkDemo/ZhangYu1_C.mat";

        private Transform m_Movement;
        private Transform m_Visual;
        private OctopusLocomotion m_Locomotion;
        private Camera m_Camera;
        private float m_Yaw;
        private float m_CamYaw;
        private float m_CamPitch;
        private float m_CamDistance;
        private float m_LookHeight = 0.4f;
        private float m_MapLimit;
        private GUIStyle m_HudStyle;
        private static Mesh s_CubeMesh;

        private void Start()
        {
            m_CamYaw = 30f;
            m_CamPitch = CameraPitch;
            m_CamDistance = CameraDistance;
            m_MapLimit = (HalfCells - 1) * Cell;
            SetupLighting();
            BuildGround();
            Physics.SyncTransforms();
            BuildOctopus(PickSpawn());
            m_Camera = Camera.main;
            if (m_Camera == null)
                m_Camera = CreateCamera();
            m_Camera.clearFlags = CameraClearFlags.Skybox;
            m_Camera.nearClipPlane = 0.03f;
            m_Camera.farClipPlane = 80f;
            m_Camera.fieldOfView = 38f;
        }

        private void Update()
        {
            if (m_Movement == null) return;
            MoveBody();
            FollowGround();
        }

        private void LateUpdate()
        {
            if (m_Movement == null || m_Camera == null) return;
            UpdateCamera();
        }

        private void OnGUI()
        {
            if (m_HudStyle == null)
            {
                var font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "SimHei", "Arial" }, 16);
                m_HudStyle = new GUIStyle(GUI.skin.box)
                {
                    font = font,
                    fontSize = 16,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                    padding = new RectOffset(12, 12, 8, 8)
                };
                m_HudStyle.normal.textColor = Color.white;
            }

            int planted = m_Locomotion != null ? m_Locomotion.PlantedCount : 0;
            int legs = m_Locomotion != null ? m_Locomotion.LegCount : 0;
            string text =
                "WASD / 方向键   移动\n" +
                "Q / E   原地转向\n" +
                "Shift   加速\n" +
                "鼠标右键   旋转镜头    滚轮   缩放\n\n" +
                "着地触手  " + planted + " / " + legs;
            GUI.Box(new Rect(12f, 12f, 390f, 148f), text, m_HudStyle);
        }

        private void MoveBody()
        {
            float horizontal = KeyAxis(KeyCode.A, KeyCode.LeftArrow, KeyCode.D, KeyCode.RightArrow);
            float vertical = KeyAxis(KeyCode.S, KeyCode.DownArrow, KeyCode.W, KeyCode.UpArrow);
            float turn = KeyAxis(KeyCode.Q, KeyCode.None, KeyCode.E, KeyCode.None);
            bool moving = horizontal * horizontal + vertical * vertical > 0.01f;

            // The preview translates the root, then turns it by at most angularSpeed.
            // Snapping yaw onto the camera direction queues a full turn and lifts the planted arms.
            Vector3 wish = Vector3.zero;
            if (moving)
            {
                Vector3 forward = Quaternion.Euler(0f, m_CamYaw, 0f) * Vector3.forward;
                Vector3 right = Quaternion.Euler(0f, m_CamYaw, 0f) * Vector3.right;
                wish = forward * vertical + right * horizontal;
                wish.y = 0f;
                if (wish.sqrMagnitude > 0.0001f)
                {
                    wish.Normalize();
                    float wishYaw = Mathf.Atan2(wish.x, wish.z) * Mathf.Rad2Deg;
                    m_Yaw = Mathf.MoveTowardsAngle(m_Yaw, wishYaw, TurnSpeed * Time.deltaTime);
                }
            }
            else if (Mathf.Abs(turn) > 0.01f)
            {
                m_Yaw += turn * TurnSpeed * Time.deltaTime;
            }

            m_Yaw = Mathf.Repeat(m_Yaw + 180f, 360f) - 180f;
            m_Movement.rotation = Quaternion.Euler(0f, m_Yaw, 0f);
            if (!moving) return;

            float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? SprintSpeed : WalkSpeed;
            Vector3 position = m_Movement.position + wish * (speed * Time.deltaTime);
            position.x = Mathf.Clamp(position.x, -m_MapLimit, m_MapLimit);
            position.z = Mathf.Clamp(position.z, -m_MapLimit, m_MapLimit);
            m_Movement.position = position;
        }

        private void FollowGround()
        {
            Vector3 origin = m_Movement.position + Vector3.up * 8f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 20f, 1, QueryTriggerInteraction.Ignore))
                return;
            if (Vector3.Dot(hit.normal, Vector3.up) < 0.45f)
                return;
            // Same clearance the preview uses. Smoothing this height keeps the planted feet
            // off their landings, so idle never settles.
            Vector3 position = m_Movement.position;
            position.y = hit.point.y + ReferenceBodyClearance;
            m_Movement.position = position;
        }

        private void UpdateCamera()
        {
            if (Input.GetMouseButton(1))
            {
                m_CamYaw += Input.GetAxis("Mouse X") * 5f;
                m_CamPitch = Mathf.Clamp(m_CamPitch - Input.GetAxis("Mouse Y") * 3f, 14f, 72f);
            }
            else
            {
                m_CamYaw = Mathf.LerpAngle(m_CamYaw, m_Yaw, 1f - Mathf.Exp(-4f * Time.deltaTime));
            }

            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) > 0.01f)
                m_CamDistance = Mathf.Clamp(m_CamDistance * (1f - scroll * 0.05f), 1.2f, 20f);

            Vector3 look = m_Movement.position + Vector3.up * m_LookHeight;
            Quaternion orbit = Quaternion.Euler(m_CamPitch, m_CamYaw, 0f);
            m_Camera.transform.SetPositionAndRotation(look - orbit * Vector3.forward * m_CamDistance, orbit);
        }

        private void SetupLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.46f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.20f, 0.16f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.62f, 0.74f, 0.84f);
            RenderSettings.fogStartDistance = 26f;
            RenderSettings.fogEndDistance = 58f;

            Light sun = FindFirstObjectByType<Light>();
            if (sun == null)
            {
                var lightGo = new GameObject("Directional Light");
                sun = lightGo.AddComponent<Light>();
                sun.type = LightType.Directional;
                lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
        }

        private void BuildGround()
        {
            var root = new GameObject("Ground").transform;
            var bands = new Material[8];
            for (int i = 0; i < bands.Length; i++)
            {
                float t = i / (float)(bands.Length - 1);
                Color color = Color.Lerp(new Color(0.42f, 0.36f, 0.27f), new Color(0.78f, 0.80f, 0.74f), t);
                bands[i] = MakeMaterial(color, 0.04f);
            }
            Material boulderMaterial = MakeMaterial(new Color(0.34f, 0.32f, 0.30f), 0.08f);
            Material rampMaterial = MakeMaterial(new Color(0.70f, 0.46f, 0.30f), 0.08f);

            for (int iz = -HalfCells; iz <= HalfCells; iz++)
            {
                for (int ix = -HalfCells; ix <= HalfCells; ix++)
                {
                    float x = ix * Cell;
                    float z = iz * Cell;
                    float height = TerraceHeight(x, z);
                    int band = Mathf.Clamp(Mathf.RoundToInt(height / 0.35f), 0, bands.Length - 1);
                    AddWorldCube(root, "Terrace", new Vector3(x, height * 0.5f, z), Quaternion.identity,
                        new Vector3(Cell, height, Cell), bands[band], true);

                    if (!IsBoulder(ix, iz)) continue;
                    float boulderHeight = 0.35f + (Mathf.Abs(ix * 13 + iz * 7) % 2) * 0.2f;
                    AddWorldCube(root, "Boulder", new Vector3(x, height + boulderHeight * 0.5f, z), Quaternion.identity,
                        new Vector3(1.35f, boulderHeight, 1.35f), boulderMaterial, true);
                }
            }

            AddRamp(root, rampMaterial, new Vector3(8f, 0f, 6f), 16f, 25f);
            AddRamp(root, rampMaterial, new Vector3(-10f, 0f, 4f), -14f, 70f);
            AddRamp(root, rampMaterial, new Vector3(6f, 0f, -8f), 18f, 140f);
            AddRamp(root, rampMaterial, new Vector3(-7f, 0f, -10f), -12f, -40f);
            AddRamp(root, rampMaterial, new Vector3(12f, 0f, -2f), 15f, 10f);
        }

        private void AddRamp(Transform root, Material material, Vector3 xz, float pitch, float yaw)
        {
            float height = TerraceHeight(xz.x, xz.z);
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            AddWorldCube(root, "Ramp", new Vector3(xz.x, height + 0.22f, xz.z), rotation,
                new Vector3(3.2f, 0.34f, 2.1f), material, true);
        }

        private Vector3 PickSpawn()
        {
            Vector3 spawn = new Vector3(0f, TerraceHeight(0f, 0f), 0f);
            float best = float.MaxValue;
            for (int iz = -2; iz <= 2; iz++)
            {
                for (int ix = -2; ix <= 2; ix++)
                {
                    if (IsBoulder(ix, iz)) continue;
                    float x = ix * Cell;
                    float z = iz * Cell;
                    float height = TerraceHeight(x, z);
                    float slope = 0f;
                    slope = Mathf.Max(slope, Mathf.Abs(height - TerraceHeight(x + Cell, z)));
                    slope = Mathf.Max(slope, Mathf.Abs(height - TerraceHeight(x - Cell, z)));
                    slope = Mathf.Max(slope, Mathf.Abs(height - TerraceHeight(x, z + Cell)));
                    slope = Mathf.Max(slope, Mathf.Abs(height - TerraceHeight(x, z - Cell)));
                    float score = slope * 8f + Mathf.Abs(ix) + Mathf.Abs(iz);
                    if (score >= best) continue;
                    best = score;
                    spawn = new Vector3(x, height, z);
                }
            }
            return spawn;
        }

        private void BuildOctopus(Vector3 spawn)
        {
            GameObject model = ResolveModel();
            if (model == null)
            {
                Debug.LogError("OctopusWalkDemo: 未找到 ZhangYu1_C.fbx。");
                return;
            }

            var movementGo = new GameObject("Octopus");
            m_Movement = movementGo.transform;
            m_Movement.SetPositionAndRotation(spawn + Vector3.up * ReferenceBodyClearance, Quaternion.identity);

            var skinRoot = new GameObject("SkinRoot");
            m_Visual = skinRoot.transform;
            m_Visual.SetParent(m_Movement, false);
            m_Visual.localPosition = Vector3.zero;
            m_Visual.localRotation = Quaternion.identity;
            m_Visual.localScale = Vector3.one * ReferenceVisualScale;

            GameObject visual = Instantiate(model, m_Visual);
            visual.name = "ZhangYu1_C";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator != null) animator.enabled = false;
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;
            ApplyOctopusMaterial(visual);

            float modelHeight = 1f;
            foreach (SkinnedMeshRenderer renderer in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.updateWhenOffscreen = true;
                modelHeight = Mathf.Max(modelHeight, renderer.bounds.size.y);
            }
            m_LookHeight = modelHeight * 0.4f;

            SetLayer(movementGo, 2);
            m_Locomotion = movementGo.AddComponent<OctopusLocomotion>();
            m_Locomotion.GroundMask = 1;
            m_Locomotion.WallMask2D = 0;
            m_Locomotion.AllowFlatGroundFallback = false;
            if (!m_Locomotion.Initialize(m_Movement, m_Visual, animator))
                Debug.LogError("OctopusWalkDemo: 章鱼骨架绑定失败，模型里需要 connectBone05 到 connectBone12。");
        }

        private GameObject ResolveModel()
        {
            if (OctopusModel != null) return OctopusModel;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
#else
            return null;
#endif
        }

        private void ApplyOctopusMaterial(GameObject visual)
        {
            Material material = OctopusMaterial;
#if UNITY_EDITOR
            if (material == null)
                material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
#endif
            if (material == null) return;
            foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                var slots = renderer.sharedMaterials;
                if (slots == null || slots.Length == 0)
                {
                    renderer.sharedMaterial = material;
                    continue;
                }
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = material;
                renderer.sharedMaterials = slots;
            }
        }

        private static float TerraceHeight(float x, float z)
        {
            float broad = Mathf.PerlinNoise(x * 0.085f + 1.7f, z * 0.085f + 4.2f);
            float detail = Mathf.PerlinNoise(x * 0.22f + 8.5f, z * 0.22f + 2.1f);
            float raw = 0.45f + broad * 1.05f + (detail - 0.5f) * 0.35f;
            return Mathf.Max(0.4f, Mathf.Round(raw / 0.35f) * 0.35f);
        }

        private static bool IsBoulder(int ix, int iz)
        {
            if (Mathf.Abs(ix) <= 2 && Mathf.Abs(iz) <= 2) return false;
            int hash = ix * 73856093 ^ iz * 19349663;
            return (hash & 255) < 18;
        }

        private static float KeyAxis(KeyCode negative, KeyCode negativeAlt, KeyCode positive, KeyCode positiveAlt)
        {
            float value = 0f;
            if (negative != KeyCode.None && Input.GetKey(negative)) value -= 1f;
            if (negativeAlt != KeyCode.None && Input.GetKey(negativeAlt)) value -= 1f;
            if (positive != KeyCode.None && Input.GetKey(positive)) value += 1f;
            if (positiveAlt != KeyCode.None && Input.GetKey(positiveAlt)) value += 1f;
            return Mathf.Clamp(value, -1f, 1f);
        }

        private static void AddWorldCube(Transform parent, string name, Vector3 worldPosition, Quaternion worldRotation,
            Vector3 localScale, Material material, bool collider)
        {
            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = CubeMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (collider) go.AddComponent<BoxCollider>();
            go.transform.SetParent(parent, false);
            go.transform.localScale = localScale;
            go.transform.SetPositionAndRotation(worldPosition, worldRotation);
        }

        private static Material MakeMaterial(Color color, float smoothness)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            material.color = color;
            return material;
        }

        private static void SetLayer(GameObject root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
            return go.AddComponent<Camera>();
        }

        private static Mesh CubeMesh
        {
            get
            {
                if (s_CubeMesh == null)
                {
                    var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    s_CubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
                    Object.DestroyImmediate(temp);
                }
                return s_CubeMesh;
            }
        }
    }
}

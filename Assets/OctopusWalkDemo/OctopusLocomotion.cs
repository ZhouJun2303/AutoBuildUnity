using System;
using UnityEngine;

namespace OctopusWalk
{
    /// <summary>
    /// World-space planted contacts and rotation-aware stepping for the octopus's eight arms.
    /// Gameplay owns the movement root; this component owns only the visible skin and its bones.
    /// </summary>
    [DisallowMultipleComponent, DefaultExecutionOrder(10000)]
    [AddComponentMenu("Octopus Walk/Ground Locomotion")]
    public sealed partial class OctopusLocomotion : MonoBehaviour
    {
        [Header("Foot placement (relative to arm length)")]
        [Range(0.45f, 0.85f)] public float StanceReach = 0.72f;
        [Range(0.08f, 0.4f)] public float StepDistance = 0.22f;
        [Range(0.03f, 0.3f)] public float StepHeight = 0.14f;
        [Range(0.08f, 0.4f)] public float StepDuration = 0.18f;
        [Range(1, 4)] public int MaxSimultaneousSteps = 2;
        [Header("Turning and shape")]
        [Range(30f, 360f)] public float MaxTurnSpeed = 180f;
        [Range(5f, 25f)] public float MaxFootAngle = 20f;
        [Range(0.05f, 0.5f)] public float ArchHeight = 0.24f;
        [Range(0.3f, 0.7f)] public float BodyHeight = 0.5f;
        [Range(0f, 0.08f)] public float BodyMotion = 0.025f;
        [Range(4, 32)] public int SolverIterations = 16;
        [Header("Rendering")]
        [Tooltip("Extra local-space padding as a multiple of mean tentacle length while procedural pose is active.")]
        [Range(0f, 1.5f)] public float SkinBoundsPadding = 0.45f;
        [Tooltip("Keep procedural tentacles visible when they move outside the authored animation bounds.")]
        public bool KeepSkinVisible = true;
        [Header("Ground")]
        public LayerMask GroundMask = (1 << 3) | (1 << 13); // Floor and PartialFloor in this project.
        public LayerMask WallMask2D = 1 << 7; // Gameplay walls use world XZ mapped to Physics2D XY.
        [Tooltip("The existing flat game maps use y=0 even where no floor collider is loaded.")]
        public bool AllowFlatGroundFallback = true;
        public float FlatGroundHeight;
        public bool DrawContacts;

        public int LegCount => m_Legs == null ? 0 : m_Legs.Length;
        public int PlantedCount { get; private set; }
        public float MaxContactError { get; private set; }
        public float MaxSectorError { get; private set; }
        public bool IsInitialized => m_Initialized;
        public bool IsLocomotionActive => m_Active;

        private Transform m_MovementRoot;
        private Transform m_VisualRoot;
        private Animator m_Animator;
        private bool m_AnimatorWasEnabled;
        private bool m_Initialized;
        private bool m_Active;
        private Vector3 m_VisualRestPosition;
        private Quaternion m_VisualRestRotation;
        private Quaternion m_HeadingOffset;
        private Vector3 m_PreviousPosition;
        private Vector3 m_PreviousScale;
        private Vector3 m_Velocity;
        private float m_VisualYaw;
        private float m_AngularVelocity;
        private float m_PreviousHeading;
        private float m_TurnDebt;
        private float m_LastTurnDirection;
        private float m_TurnIdleTime;
        private float m_MeanLength;
        private float m_BodyHeight;
        private float m_StanceHeightOffset;
        private float m_StepClock;
        private int m_LastStepped = -1;
        private Leg[] m_Legs;
        private BonePose[] m_RestPose;
        private SkinBoundsPose[] m_SkinBounds;

        private struct SkinBoundsPose
        {
            public SkinnedMeshRenderer Renderer;
            public Bounds Bounds;
            public bool UpdateWhenOffscreen;
        }

        private struct Contact
        {
            public Vector3 Point;
            public Vector3 Normal;
            public Vector3 Direction;
            public Transform Surface;
            public Vector3 LocalPoint;
            public Vector3 LocalNormal;
            public Vector3 LocalDirection;
            public bool HasWallConstraint;
            public Vector3 WallPoint;
            public Vector3 WallNormal;
            public float WallClearance;

            public void FollowSurface()
            {
                if (Surface == null) return;
                Point = Surface.TransformPoint(LocalPoint);
                Normal = Surface.TransformDirection(LocalNormal).normalized;
                Direction = Surface.TransformDirection(LocalDirection).normalized;
            }
        }

        private sealed class Leg
        {
            public Transform[] Bones;
            public Quaternion[] FrameOffsets;
            public Vector3[] RestOffsets;
            public Vector3[] Positions;
            public float[] Lengths;
            public Vector3 RestRoot;
            public Vector3 Radial;
            public float Length;
            public float SectorAngle;
            public Contact Contact;
            public Contact Landing;
            public Vector3 Foot;
            public Vector3 Normal = Vector3.up;
            public Vector3 Direction;
            public Vector3 LiftFrom;
            public Vector3 LiftNormal;
            public Vector3 LiftDirection;
            public float Progress;
            public float Duration;
            public float ContactWeight;
            public float Urgency;
            public float LastStep;
            public bool Stepping;
            public bool HasContact;
        }

        public bool Initialize(Transform movementRoot, Transform visualRoot, Animator animator)
        {
            if (movementRoot == null || visualRoot == null || movementRoot == visualRoot) return false;
            if (m_Initialized && m_MovementRoot == movementRoot && m_VisualRoot == visualRoot)
            {
                SetLocomotionActive(true);
                ResetPose();
                return true;
            }
            Shutdown();
            m_MovementRoot = movementRoot;
            m_VisualRoot = visualRoot;
            m_Animator = animator;
            m_VisualRestPosition = visualRoot.localPosition;
            m_VisualRestRotation = visualRoot.localRotation;
            m_HeadingOffset = Quaternion.Inverse(YawRotation(movementRoot)) * visualRoot.rotation;
            if (!BindRig()) return false;
            RefreshLengths();
            CaptureSkinBounds();
            m_Initialized = true;
            SetLocomotionActive(true);
            return true;
        }

        public void SetLocomotionActive(bool active)
        {
            active &= m_Initialized;
            if (active == m_Active) return;
            m_Active = active;
            if (active)
            {
                m_AnimatorWasEnabled = m_Animator != null && m_Animator.enabled;
                if (m_Animator != null) m_Animator.enabled = false;
                ResetPose();
                ApplySkinBounds(true);
            }
            else
            {
                RestoreRig();
                ApplySkinBounds(false);
                if (m_Animator != null) m_Animator.enabled = m_AnimatorWasEnabled;
            }
        }

        public void Shutdown()
        {
            SetLocomotionActive(false);
            m_Initialized = false;
            m_Legs = null;
            m_RestPose = null;
            m_SkinBounds = null;
            PlantedCount = 0;
        }

        private void OnDisable() => SetLocomotionActive(false);
        private void OnDestroy() => Shutdown();

        private void LateUpdate()
        {
            if (Application.isPlaying) Simulate(Time.deltaTime);
        }

        public void ResetPose()
        {
            if (!m_Initialized) return;
            RestoreRig();
            m_VisualYaw = Heading(m_MovementRoot);
            m_PreviousHeading = m_VisualYaw;
            m_TurnDebt = 0f;
            m_LastTurnDirection = 0f;
            m_TurnIdleTime = 0f;
            m_PreviousPosition = m_MovementRoot.position;
            m_PreviousScale = m_VisualRoot.lossyScale;
            m_Velocity = Vector3.zero;
            m_AngularVelocity = 0f;
            m_BodyHeight = 0f;
            m_StepClock = 0f;
            m_LastStepped = -1;
            RefreshLengths();
            float rootHeight = 0f;
            for (int i = 0; i < m_Legs.Length; i++) rootHeight += m_Legs[i].Bones[0].position.y;
            rootHeight /= m_Legs.Length;
            ProbeGround(m_MovementRoot.position, m_MeanLength, out Contact bodyGround);
            m_StanceHeightOffset = bodyGround.Point.y + m_MeanLength * BodyHeight - rootHeight;
            ApplyBodyPose(0f);
            for (int i = 0; i < m_Legs.Length; i++)
            {
                Leg leg = m_Legs[i];
                leg.Stepping = false;
                leg.LastStep = -1f;
                leg.Progress = 0f;
                leg.HasContact = FindLanding(leg, 0f, out leg.Contact);
                if (!leg.HasContact)
                {
                    leg.Contact = new Contact { Point = leg.Bones[leg.Bones.Length - 1].position,
                        Normal = Vector3.up, Direction = WorldRadial(leg) };
                }
                leg.Landing = leg.Contact;
                SetFootFromContact(leg);
            }
            if (m_Active) SolveArms();
            if (m_Active) ApplySkinBounds(true);
        }

        /// <summary>Also callable by the editor preview, without initializing any game services.</summary>
        public void Simulate(float deltaTime)
        {
            if (!m_Active || !m_Initialized || !enabled || deltaTime <= 0f || float.IsNaN(deltaTime)) return;
            if (m_MovementRoot == null || m_VisualRoot == null) return;
            Vector3 position = m_MovementRoot.position;
            Vector3 displacement = position - m_PreviousPosition;
            if (displacement.sqrMagnitude > m_MeanLength * m_MeanLength * 4f ||
                (m_VisualRoot.lossyScale - m_PreviousScale).sqrMagnitude > 0.000001f || deltaTime > 0.25f)
            {
                ResetPose();
                return;
            }
            float dt = Mathf.Min(deltaTime, 0.05f);
            m_StepClock += dt;
            Vector3 velocity = Vector3.ProjectOnPlane(displacement / deltaTime, Vector3.up);
            m_Velocity = Vector3.Lerp(m_Velocity, velocity, 1f - Mathf.Exp(-12f * dt));
            m_PreviousPosition = position;

            // A root can turn instantly for targeting. The visible skin turns only as far as its
            // planted arms can support, and steppers relieve the angular pressure on later frames.
            float remainingYaw = UpdateTurnRequest(dt);
            float yawStep = Mathf.Clamp(remainingYaw, -MaxTurnSpeed * dt, MaxTurnSpeed * dt);
            yawStep = LimitTurnToContacts(yawStep);
            m_VisualYaw = Mathf.Repeat(m_VisualYaw + yawStep + 180f, 360f) - 180f;
            m_TurnDebt -= yawStep;
            m_AngularVelocity = yawStep / dt;
            ApplyBodyPose(dt);

            for (int i = 0; i < m_Legs.Length; i++)
            {
                Leg leg = m_Legs[i];
                if (leg.Stepping) AdvanceStep(leg, dt);
                else
                {
                    leg.Contact.FollowSurface();
                    SetFootFromContact(leg);
                }
            }
            ScheduleSteps(remainingYaw);
            SolveArms();
        }

        private float UpdateTurnRequest(float dt)
        {
            float heading = Heading(m_MovementRoot);
            float delta = Mathf.DeltaAngle(m_PreviousHeading, heading);
            m_PreviousHeading = heading;
            m_TurnDebt += delta;
            if (Mathf.Abs(delta) > 0.05f)
            {
                float direction = Mathf.Sign(delta);
                if (m_LastTurnDirection != 0f && direction != m_LastTurnDirection)
                {
                    // A reversal cancels the old queued turn. Rejoin the nearest heading;
                    // forcing the new sign here could turn a 1-degree correction into 359 degrees.
                    m_TurnDebt = Mathf.DeltaAngle(m_VisualYaw, heading);
                }
                m_LastTurnDirection = direction;
                m_TurnIdleTime = 0f;
            }
            else
            {
                m_TurnIdleTime += dt;
                if (m_TurnIdleTime > 0.15f)
                {
                    m_TurnDebt = Mathf.DeltaAngle(m_VisualYaw, heading);
                    m_LastTurnDirection = 0f;
                }
            }
            // Missing complete revolutions at extreme input rates must not leave seconds of
            // queued spins. Unlike DeltaAngle, signed modulo never reverses an ongoing turn.
            m_TurnDebt %= 360f;
            return m_TurnDebt;
        }

        private float LimitTurnToContacts(float requested)
        {
            float lower = -MaxTurnSpeed;
            float upper = MaxTurnSpeed;
            bool constrained = false;
            for (int i = 0; i < m_Legs.Length; i++)
            {
                Leg leg = m_Legs[i];
                if (leg.Stepping || !leg.HasContact) continue;
                leg.Contact.FollowSurface();
                // Use the current body centre, not its already-rotated parent's coordinate frame.
                Vector3 radial = Quaternion.Euler(0f, m_VisualYaw, 0f) * m_HeadingOffset * leg.Radial;
                Vector3 toContact = leg.Contact.Point - m_MovementRoot.position;
                float angle = SignedGroundAngle(radial, toContact);
                float limit = leg.SectorAngle;
                lower = Mathf.Max(lower, angle - limit);
                upper = Mathf.Min(upper, angle + limit);
                constrained = true;
            }
            if (!constrained) return requested;
            if (lower > upper) return 0f; // Translation made support incompatible: replant, never wind up.
            // Do not force a turn in the opposite direction just to repair an old contact.
            if (requested >= 0f) return Mathf.Clamp(requested, 0f, Mathf.Max(0f, upper));
            return Mathf.Clamp(requested, Mathf.Min(0f, lower), 0f);
        }

        private void ApplyBodyPose(float dt)
        {
            float targetHeight = 0f;
            int moving = 0;
            for (int i = 0; i < m_Legs.Length; i++)
            {
                if (!m_Legs[i].Stepping) continue;
                targetHeight -= Mathf.Sin(m_Legs[i].Progress * Mathf.PI) * BodyMotion * m_MeanLength;
                moving++;
            }
            if (moving > 0) targetHeight /= moving;
            m_BodyHeight = Mathf.Lerp(m_BodyHeight, targetHeight, 1f - Mathf.Exp(-14f * dt));
            m_VisualRoot.localPosition = m_VisualRestPosition;
            m_VisualRoot.position += Vector3.up * (m_StanceHeightOffset + m_BodyHeight);
            m_VisualRoot.rotation = Quaternion.Euler(0f, m_VisualYaw, 0f) * m_HeadingOffset;
        }

        private void ScheduleSteps(float remainingYaw)
        {
            int moving = 0;
            bool fastTurn = Mathf.Abs(remainingYaw) > 35f;
            int budget = Mathf.Min(m_Legs.Length / 2, fastTurn ? Mathf.Max(3, MaxSimultaneousSteps) : MaxSimultaneousSteps);
            for (int i = 0; i < m_Legs.Length; i++)
            {
                Leg leg = m_Legs[i];
                leg.Urgency = 0f;
                if (leg.Stepping) { moving++; continue; }
                float angularLead = Mathf.Clamp(remainingYaw, -leg.SectorAngle * 0.6f, leg.SectorAngle * 0.6f);
                if (!FindLanding(leg, angularLead, out leg.Landing)) continue;
                Vector3 radial = WorldRadial(leg);
                float angle = SignedGroundAngle(radial, leg.Contact.Point - m_MovementRoot.position);
                float distance = Vector3.Distance(leg.Contact.Point, leg.Landing.Point);
                float rootDistance = Vector3.Distance(leg.Bones[0].position, leg.Contact.Point);
                float angularDemand = Mathf.Abs(angle - angularLead);
                leg.Urgency = Mathf.Max(distance / Mathf.Max(leg.Length * StepDistance, 0.001f),
                    angularDemand / Mathf.Max(leg.SectorAngle * 0.62f, 1f));
                if (!leg.HasContact) leg.Urgency = 10f;
                if (Mathf.Abs(angle) > leg.SectorAngle * 0.9f || rootDistance > leg.Length * 0.9f)
                    leg.Urgency = Mathf.Max(leg.Urgency, 3f);
                // A locked point beyond its angular sector would wind the arm around a neighbor.
                // Translation can outrun the normal gait, so safety releases may exceed the budget.
                if (Mathf.Abs(angle) > leg.SectorAngle + 3f || rootDistance > leg.Length * 0.98f)
                {
                    BeginStep(leg, true);
                    moving++;
                }
            }
            while (moving < budget)
            {
                int best = -1;
                float priority = 1f;
                for (int offset = 1; offset <= m_Legs.Length; offset++)
                {
                    int i = (m_LastStepped + offset + m_Legs.Length) % m_Legs.Length;
                    Leg leg = m_Legs[i];
                    if (leg.Stepping || leg.Urgency <= 1f || NeighborIsStepping(i)) continue;
                    float score = leg.Urgency + Mathf.Min(0.2f, (m_StepClock - leg.LastStep) * 0.02f);
                    if (score > priority) { priority = score; best = i; }
                }
                if (best < 0) break;
                BeginStep(m_Legs[best], fastTurn);
                m_LastStepped = best;
                moving++;
            }
        }

        private bool NeighborIsStepping(int index)
        {
            int count = m_Legs.Length;
            return m_Legs[(index + count - 1) % count].Stepping || m_Legs[(index + 1) % count].Stepping;
        }

        private void BeginStep(Leg leg, bool urgent)
        {
            leg.LiftFrom = leg.Foot;
            leg.LiftNormal = leg.Normal;
            leg.LiftDirection = leg.Direction;
            leg.Progress = 0f;
            float speed = m_Velocity.magnitude + Mathf.Abs(m_AngularVelocity) * Mathf.Deg2Rad * leg.Length;
            leg.Duration = Mathf.Clamp(StepDuration / (1f + speed / Mathf.Max(leg.Length, 0.01f) * 0.12f), 0.075f, StepDuration);
            if (urgent) leg.Duration = Mathf.Min(leg.Duration, 0.09f);
            leg.LastStep = m_StepClock;
            leg.Stepping = true;
            leg.HasContact = false;
        }

        private void AdvanceStep(Leg leg, float dt)
        {
            leg.Progress = Mathf.Clamp01(leg.Progress + dt / Mathf.Max(0.04f, leg.Duration));
            // Replan a swing on reversal or lateral movement before landing. A planted foot is
            // never retargeted; airborne targets remain inside this arm's current radial sector.
            float yawLead = Mathf.Clamp(m_TurnDebt,
                -leg.SectorAngle * 0.6f, leg.SectorAngle * 0.6f);
            if (FindLanding(leg, yawLead, out Contact updated)) leg.Landing = updated;
            leg.Landing.FollowSurface();
            float t = leg.Progress;
            float travel = Smooth01(Mathf.InverseLerp(0.12f, 0.92f, t));
            Vector3 point = Vector3.Lerp(leg.LiftFrom, leg.Landing.Point, travel);
            float lift = Mathf.Sin(Mathf.PI * t) * leg.Length * StepHeight;
            point += Vector3.up * lift;
            // Keep the swing on its own side of the body even after an abrupt reversal.
            point = ClampToSector(leg, point, leg.SectorAngle);
            leg.Foot = point;
            leg.Normal = Vector3.Slerp(leg.LiftNormal, leg.Landing.Normal, travel).normalized;
            leg.Direction = Vector3.Slerp(leg.LiftDirection, leg.Landing.Direction, travel).normalized;
            leg.ContactWeight = t < 0.2f ? 1f - Smooth01(t / 0.2f) : Smooth01((t - 0.75f) / 0.25f);
            if (t < 1f) return;
            leg.Contact = leg.Landing;
            leg.HasContact = true;
            leg.Stepping = false;
            SetFootFromContact(leg);
        }

        private bool FindLanding(Leg leg, float angularLead, out Contact contact)
        {
            Vector3 root = m_VisualRoot.TransformPoint(leg.RestRoot);
            Vector3 radial = WorldRadial(leg);
            float height = Mathf.Max(0f, root.y - (leg.HasContact ? leg.Contact.Point.y : FlatGroundHeight));
            float reach = leg.Length * StanceReach;
            float horizontal = Mathf.Sqrt(Mathf.Max(reach * reach - height * height, leg.Length * leg.Length * 0.04f));
            Vector3 candidate = root + radial * horizontal;
            Vector3 lead = Vector3.ClampMagnitude(m_Velocity * StepDuration * 0.55f, leg.Length * 0.18f);
            candidate += lead;
            Vector3 offset = candidate - m_MovementRoot.position;
            offset = Quaternion.AngleAxis(angularLead, Vector3.up) * offset;
            candidate = ClampToSector(leg, m_MovementRoot.position + offset, leg.SectorAngle * 0.8f);
            Vector2 wallOrigin = new Vector2(m_MovementRoot.position.x, m_MovementRoot.position.z);
            Vector2 wallDelta = new Vector2(candidate.x, candidate.z) - wallOrigin;
            float wallDistance = wallDelta.magnitude;
            bool hasWallConstraint = false;
            Vector3 wallPoint = Vector3.zero;
            Vector3 wallNormal = Vector3.zero;
            if (wallDistance > 0.001f && WallMask2D.value != 0)
            {
                RaycastHit2D wall = Physics2D.Raycast(wallOrigin, wallDelta / wallDistance, wallDistance, WallMask2D);
                if (wall.collider != null)
                {
                    // A wall can be closer than the nominal stance radius. Keep a short
                    // emergency landing inside the safe side instead of abandoning this leg;
                    // otherwise a planted point can remain behind the body and wind the arm
                    // indefinitely while the root is pressed against the wall.
                    float safeDistance = Mathf.Max(0f, wall.distance - leg.Length * 0.04f);
                    Vector2 safe = wallOrigin + wallDelta / wallDistance * safeDistance;
                    candidate.x = safe.x;
                    candidate.z = safe.y;
                    // Physics2D uses world XY while gameplay uses world XZ. The ray hit
                    // normal points back toward the ray origin, which is the safe half-space.
                    wallPoint = new Vector3(wall.point.x, 0f, wall.point.y);
                    wallNormal = new Vector3(wall.normal.x, 0f, wall.normal.y);
                    hasWallConstraint = wallNormal.sqrMagnitude > 0.000001f;
                }
            }
            if (!ProbeGround(candidate, leg.Length, out contact)) return false;
            contact.Direction = Vector3.ProjectOnPlane(contact.Point - root, contact.Normal).normalized;
            if (contact.Direction.sqrMagnitude < 0.001f) contact.Direction = radial;
            // Keep the render surface just above the collider; the distal bones run through the mesh centre.
            contact.Point += contact.Normal * leg.Length * 0.012f;
            if (hasWallConstraint)
            {
                float wallError = leg.Length * 0.02f -
                    Vector3.Dot(contact.Point - wallPoint, wallNormal.normalized);
                if (wallError > 0f) contact.Point += wallNormal.normalized * wallError;
            }
            if (Vector3.Distance(root, contact.Point) > leg.Length * 0.94f) return false;
            contact.HasWallConstraint = hasWallConstraint;
            contact.WallPoint = wallPoint;
            contact.WallNormal = wallNormal.normalized;
            contact.WallClearance = leg.Length * 0.02f;
            if (contact.Surface != null)
            {
                contact.LocalPoint = contact.Surface.InverseTransformPoint(contact.Point);
                contact.LocalNormal = contact.Surface.InverseTransformDirection(contact.Normal);
                contact.LocalDirection = contact.Surface.InverseTransformDirection(contact.Direction);
            }
            return true;
        }

        private bool ProbeGround(Vector3 candidate, float length, out Contact contact)
        {
            float height = Mathf.Max(length, 0.2f);
            Vector3 origin = candidate;
            origin.y = Mathf.Max(candidate.y, m_MovementRoot.position.y) + height;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, height * 3f, GroundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Dot(hit.normal, Vector3.up) >= 0.45f)
            {
                contact = new Contact { Point = hit.point, Normal = hit.normal, Surface = hit.collider.transform };
                return true;
            }
            contact = new Contact { Point = new Vector3(candidate.x, FlatGroundHeight, candidate.z), Normal = Vector3.up };
            return AllowFlatGroundFallback;
        }

        private Vector3 ClampToSector(Leg leg, Vector3 point, float maxAngle)
        {
            Vector3 offset = point - m_MovementRoot.position;
            float radius = new Vector2(offset.x, offset.z).magnitude;
            Vector3 radial = WorldRadial(leg);
            float angle = Mathf.Clamp(SignedGroundAngle(radial, offset), -maxAngle, maxAngle);
            Vector3 planar = Quaternion.AngleAxis(angle, Vector3.up) * radial * radius;
            return new Vector3(m_MovementRoot.position.x + planar.x, point.y, m_MovementRoot.position.z + planar.z);
        }

        private Vector3 WorldRadial(Leg leg)
        {
            Vector3 direction = m_VisualRoot.TransformDirection(leg.Radial);
            direction.y = 0f;
            return direction.sqrMagnitude > 0.00001f ? direction.normalized : Vector3.forward;
        }

        private static void SetFootFromContact(Leg leg)
        {
            leg.Foot = leg.Contact.Point;
            leg.Normal = leg.Contact.Normal;
            leg.Direction = leg.Contact.Direction;
            leg.ContactWeight = leg.HasContact ? 1f : 0f;
        }

        private void SolveArms()
        {
            PlantedCount = 0;
            MaxContactError = 0f;
            MaxSectorError = 0f;
            for (int i = 0; i < m_Legs.Length; i++)
            {
                Leg leg = m_Legs[i];
                Vector3 root = m_VisualRoot.TransformPoint(leg.RestRoot);
                Vector3 groundPoint = leg.Stepping ? leg.Landing.Point : leg.Contact.Point;
                Contact constraintContact = leg.Stepping ? leg.Landing : leg.Contact;
                Vector3 wallNormal = constraintContact.HasWallConstraint ? constraintContact.WallNormal : Vector3.zero;
                float error = OctopusTentacleSolver.Solve(leg.Positions, leg.Lengths, root, leg.Foot,
                    groundPoint, leg.Normal, leg.Direction, leg.ContactWeight, leg.Length * ArchHeight,
                    SolverIterations, constraintContact.WallPoint, wallNormal,
                    constraintContact.WallClearance);
                if (leg.HasContact)
                {
                    PlantedCount++;
                    MaxContactError = Mathf.Max(MaxContactError, error);
                }
                float sectorError = Mathf.Abs(SignedGroundAngle(WorldRadial(leg), leg.Foot - m_MovementRoot.position));
                MaxSectorError = Mathf.Max(MaxSectorError, sectorError);
                ApplyArmPose(leg);
            }
        }

        private void ApplyArmPose(Leg leg)
        {
            for (int i = 0; i < leg.Bones.Length; i++)
            {
                Vector3 tangent = i + 1 < leg.Positions.Length ? leg.Positions[i + 1] - leg.Positions[i] :
                    leg.Positions[i] - leg.Positions[i - 1];
                // Rebuild a twist-free world frame from a stable arm plane every frame. Never
                // accumulate rotations from the previous pose (which winds skin during full turns).
                Quaternion frame = ArmFrame(tangent, WorldRadial(leg), leg.Normal);
                leg.Bones[i].SetPositionAndRotation(leg.Positions[i], frame * leg.FrameOffsets[i]);
            }
        }

        internal static Quaternion ArmFrame(Vector3 tangent, Vector3 radial, Vector3 normal)
        {
            if (tangent.sqrMagnitude < 0.0000001f) tangent = radial;
            tangent.Normalize();
            Vector3 side = Vector3.Cross(normal, radial).normalized;
            Vector3 up = Vector3.Cross(tangent, side);
            if (up.sqrMagnitude < 0.00001f) up = Vector3.ProjectOnPlane(normal, tangent);
            if (up.sqrMagnitude < 0.00001f) up = Vector3.ProjectOnPlane(Vector3.forward, tangent);
            return Quaternion.LookRotation(tangent, up.normalized);
        }

        private static float SignedGroundAngle(Vector3 from, Vector3 to)
        {
            from.y = to.y = 0f;
            if (from.sqrMagnitude < 0.000001f || to.sqrMagnitude < 0.000001f) return 0f;
            return Vector3.SignedAngle(from, to, Vector3.up);
        }

        private static float Heading(Transform t) => Mathf.Atan2(t.forward.x, t.forward.z) * Mathf.Rad2Deg;
        private static Quaternion YawRotation(Transform t) => Quaternion.Euler(0f, Heading(t), 0f);
        private static float Smooth01(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

        private void OnDrawGizmosSelected()
        {
            if (!DrawContacts || m_Legs == null) return;
            foreach (Leg leg in m_Legs)
            {
                Gizmos.color = leg.Stepping ? Color.yellow : Color.cyan;
                Gizmos.DrawWireSphere(leg.Foot, leg.Length * 0.035f);
                Gizmos.DrawLine(leg.Foot, leg.Foot + leg.Normal * leg.Length * 0.1f);
                for (int i = 1; i < leg.Positions.Length; i++) Gizmos.DrawLine(leg.Positions[i - 1], leg.Positions[i]);
            }
        }
    }
}

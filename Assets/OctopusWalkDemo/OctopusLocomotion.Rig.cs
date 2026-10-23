using System;
using System.Collections.Generic;
using UnityEngine;

namespace OctopusWalk
{
    public sealed partial class OctopusLocomotion
    {
        private struct BonePose
        {
            public Transform Transform;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
        }

        private bool BindRig()
        {
            var transforms = m_VisualRoot.GetComponentsInChildren<Transform>(true);
            var legs = new List<Leg>(8);
            var poses = new List<BonePose>(100);
            foreach (Transform bone in transforms)
            {
                if (IsRigBone(bone.name))
                    poses.Add(new BonePose { Transform = bone, Position = bone.localPosition,
                        Rotation = bone.localRotation, Scale = bone.localScale });
                if (!IsWalkingRoot(bone.name)) continue;
                var chain = new List<Transform>(10) { bone };
                Transform current = bone;
                for (int depth = 0; depth < 32; depth++)
                {
                    Transform next = null;
                    for (int c = 0; c < current.childCount; c++)
                    {
                        Transform child = current.GetChild(c);
                        // Pirate accessories are siblings of Bone15_044, not links in its arm.
                        if (!child.name.StartsWith("Bone", StringComparison.OrdinalIgnoreCase)) continue;
                        if (next != null) return false; // An ambiguous skeleton must keep its authored animation.
                        next = child;
                    }
                    if (next == null) break;
                    chain.Add(next);
                    current = next;
                }
                if (chain.Count < 5) return false;
                var leg = new Leg
                {
                    Bones = chain.ToArray(),
                    Positions = new Vector3[chain.Count],
                    Lengths = new float[chain.Count - 1],
                    RestOffsets = new Vector3[chain.Count - 1],
                    FrameOffsets = new Quaternion[chain.Count],
                    RestRoot = m_VisualRoot.InverseTransformPoint(chain[0].position)
                };
                // All eight connect roots coincide on these models. The endpoint gives the
                // anatomical radial direction, and also handles the importer's handedness.
                Vector3 worldRadial = chain[chain.Count - 1].position - chain[0].position;
                worldRadial.y = 0f;
                if (worldRadial.sqrMagnitude < 0.000001f) return false;
                worldRadial.Normalize();
                leg.Radial = m_VisualRoot.InverseTransformDirection(worldRadial).normalized;
                for (int i = 0; i < chain.Count; i++)
                {
                    Vector3 tangent = i < chain.Count - 1 ? chain[i + 1].position - chain[i].position :
                        chain[i].position - chain[i - 1].position;
                    leg.FrameOffsets[i] = Quaternion.Inverse(ArmFrame(tangent, worldRadial, Vector3.up)) * chain[i].rotation;
                    if (i < chain.Count - 1)
                        leg.RestOffsets[i] = m_VisualRoot.InverseTransformVector(tangent);
                }
                legs.Add(leg);
            }
            // Avoid partially taking over a skin: the production rig has exactly eight walking arms.
            if (legs.Count != 8) return false;
            legs.Sort((a, b) => Mathf.Atan2(a.Radial.x, a.Radial.z).CompareTo(Mathf.Atan2(b.Radial.x, b.Radial.z)));
            m_Legs = legs.ToArray();
            m_RestPose = poses.ToArray();
            for (int i = 0; i < m_Legs.Length; i++)
            {
                float left = Vector3.Angle(m_Legs[i].Radial, m_Legs[(i + 7) % 8].Radial);
                float right = Vector3.Angle(m_Legs[i].Radial, m_Legs[(i + 1) % 8].Radial);
                // Leave a gap between neighboring arms even when both approach their sector edge.
                m_Legs[i].SectorAngle = Mathf.Min(MaxFootAngle, Mathf.Min(left, right) * 0.42f);
            }
            return true;
        }

        private void RefreshLengths()
        {
            m_MeanLength = 0f;
            for (int i = 0; i < m_Legs.Length; i++)
            {
                Leg leg = m_Legs[i];
                leg.Length = 0f;
                for (int j = 0; j < leg.Lengths.Length; j++)
                {
                    leg.Lengths[j] = m_VisualRoot.TransformVector(leg.RestOffsets[j]).magnitude;
                    leg.Length += leg.Lengths[j];
                }
                m_MeanLength += leg.Length;
            }
            m_MeanLength = Mathf.Max(0.01f, m_MeanLength / m_Legs.Length);
        }

        private void CaptureSkinBounds()
        {
            if (m_VisualRoot == null)
            {
                m_SkinBounds = null;
                return;
            }

            SkinnedMeshRenderer[] renderers = m_VisualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            m_SkinBounds = new SkinBoundsPose[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                m_SkinBounds[i] = new SkinBoundsPose
                {
                    Renderer = renderers[i],
                    Bounds = renderers[i].localBounds,
                    UpdateWhenOffscreen = renderers[i].updateWhenOffscreen
                };
            }
        }

        private void ApplySkinBounds(bool expanded)
        {
            if (m_SkinBounds == null) return;
            float padding = Mathf.Max(0f, SkinBoundsPadding) * Mathf.Max(0.01f, m_MeanLength);
            for (int i = 0; i < m_SkinBounds.Length; i++)
            {
                SkinBoundsPose pose = m_SkinBounds[i];
                if (pose.Renderer == null) continue;
                if (!expanded || !KeepSkinVisible)
                {
                    pose.Renderer.localBounds = pose.Bounds;
                    pose.Renderer.updateWhenOffscreen = pose.UpdateWhenOffscreen;
                    continue;
                }

                Bounds bounds = pose.Bounds;
                // Expand in all local axes because a rotated arm can leave the authored
                // animation envelope in any direction. Keeping updateWhenOffscreen false
                // retains normal renderer culling cost after this one-time conservative edit.
                Vector3 scale = pose.Renderer.transform.lossyScale;
                Vector3 localPadding = new Vector3(
                    Mathf.Max(0.02f, padding / Mathf.Max(0.01f, Mathf.Abs(scale.x))),
                    Mathf.Max(0.02f, padding / Mathf.Max(0.01f, Mathf.Abs(scale.y))),
                    Mathf.Max(0.02f, padding / Mathf.Max(0.01f, Mathf.Abs(scale.z))));
                bounds.Expand(localPadding * 2f);
                pose.Renderer.localBounds = bounds;
                pose.Renderer.updateWhenOffscreen = false;
            }
        }

        private void RestoreRig()
        {
            if (m_RestPose != null)
            {
                for (int i = 0; i < m_RestPose.Length; i++)
                {
                    BonePose pose = m_RestPose[i];
                    if (pose.Transform == null) continue;
                    pose.Transform.localPosition = pose.Position;
                    pose.Transform.localRotation = pose.Rotation;
                    pose.Transform.localScale = pose.Scale;
                }
            }
            if (m_VisualRoot == null) return;
            m_VisualRoot.localPosition = m_VisualRestPosition;
            m_VisualRoot.localRotation = m_VisualRestRotation;
        }

        private static bool IsRigBone(string name)
        {
            return name == "Skeleton" || name.StartsWith("Bone", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("connectBone", StringComparison.OrdinalIgnoreCase)
                || name.StartsWith("root_Bone", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsWalkingRoot(string name)
        {
            const string prefix = "connectBone";
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
            int number = 0;
            int i = prefix.Length;
            for (; i < name.Length && name[i] >= '0' && name[i] <= '9'; i++)
                number = number * 10 + name[i] - '0';
            return number >= 5 && number <= 12 && i > prefix.Length;
        }
    }
}

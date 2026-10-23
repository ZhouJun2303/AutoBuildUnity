using UnityEngine;

namespace OctopusWalk
{
    /// <summary>
    /// Allocation-free, deterministic planar chain IK. All points are world-space positions;
    /// rotations must be rebuilt from the bind pose by the caller, never accumulated here.
    /// Original implementation of geometric arc seeding and FABRIK distance projections.
    /// </summary>
    public static class OctopusTentacleSolver
    {
        private const float Epsilon = 0.000001f;
        private const float TwoPi = Mathf.PI * 2f;

        public static float Solve(Vector3[] positions, float[] lengths, Vector3 root, Vector3 tip,
            Vector3 groundNormal, Vector3 contactDirection, float contactWeight, float archHeight,
            int iterations = 12)
        {
            return Solve(positions, lengths, root, tip, tip, groundNormal, contactDirection,
                contactWeight, archHeight, iterations, Vector3.zero, Vector3.zero, 0f);
        }

        /// <summary>
        /// Fixes the root and preserves every segment length. A reachable target is approached
        /// within the iteration budget; the return value is the remaining tip error. The ground
        /// point is independent of the tip so a lifted foot does not raise the collision plane.
        /// contactDirection runs towards the tip along the ground. At full contact the final
        /// short section is flat; if that orientation cannot be reached it is released smoothly.
        /// archHeight is a seed preference; chain length and reach determine the actual arch.
        /// </summary>
        public static float Solve(Vector3[] positions, float[] lengths, Vector3 root, Vector3 tip,
            Vector3 groundPoint, Vector3 groundNormal, Vector3 contactDirection,
            float contactWeight, float archHeight, int iterations = 12)
        {
            return Solve(positions, lengths, root, tip, groundPoint, groundNormal, contactDirection,
                contactWeight, archHeight, iterations, Vector3.zero, Vector3.zero, 0f);
        }

        /// <summary>
        /// Same as <see cref="Solve(Vector3[], float[], Vector3, Vector3, Vector3, Vector3, Vector3, float, float, int)"/>,
        /// with an optional vertical wall half-space. wallNormal points toward the safe side
        /// of the wall; every joint is projected to dot(point, wallNormal) >= wallPlane.
        /// The constraint is intentionally a half-space rather than a single contact point so
        /// a complete bone segment cannot cross a wall between two valid endpoints.
        /// </summary>
        public static float Solve(Vector3[] positions, float[] lengths, Vector3 root, Vector3 tip,
            Vector3 groundPoint, Vector3 groundNormal, Vector3 contactDirection,
            float contactWeight, float archHeight, int iterations,
            Vector3 wallPoint, Vector3 wallNormal, float wallClearance)
        {
            if (positions == null || lengths == null || positions.Length != lengths.Length + 1 ||
                !IsFinite(root) || !IsFinite(tip) || !IsFinite(groundPoint))
                return float.PositiveInfinity;

            int count = lengths.Length;
            float total = 0f;
            for (int i = 0; i < count; i++)
            {
                if (!IsFinite(lengths[i]) || lengths[i] < 0f)
                    return float.PositiveInfinity;
                total += lengths[i];
            }

            if (!IsFinite(total))
                return float.PositiveInfinity;

            Vector3 normal = SafeDirection(groundNormal, Vector3.up);
            Vector3 tangent = Vector3.ProjectOnPlane(contactDirection, normal);
            if (tangent.sqrMagnitude < Epsilon * Epsilon)
                tangent = Vector3.ProjectOnPlane(tip - root, normal);
            tangent = SafeDirection(tangent, Perpendicular(normal));

            // An embedded root cannot satisfy the original plane. Keep the root authoritative
            // and lower only this chain's mathematical plane instead of stretching its bones.
            float floor = Mathf.Min(Vector3.Dot(groundPoint, normal), Vector3.Dot(root, normal));
            float targetHeight = Vector3.Dot(tip, normal) - floor;
            Vector3 target = targetHeight < 0f ? tip - normal * targetHeight : tip;
            var constraints = new SurfaceConstraints
            {
                GroundNormal = normal,
                GroundPlane = floor,
                HasWall = IsFinite(wallNormal) && wallNormal.sqrMagnitude > Epsilon * Epsilon,
                WallPoint = new Vector3(wallPoint.x, 0f, wallPoint.z),
                WallNormal = Vector3.zero,
                WallPlane = 0f
            };
            if (constraints.HasWall)
            {
                constraints.WallNormal = SafeDirection(new Vector3(wallNormal.x, 0f, wallNormal.z), Vector3.right);
                constraints.WallPlane = Vector3.Dot(constraints.WallPoint, constraints.WallNormal) +
                    (IsFinite(wallClearance) ? Mathf.Max(0f, wallClearance) : 0f);
                target = ConstrainPoint(target, constraints);
            }

            if (count == 0 || total <= Epsilon)
            {
                for (int i = 0; i < positions.Length; i++)
                    positions[i] = root;
                return Vector3.Distance(root, tip);
            }

            Vector3 delta = target - root;
            float distance = delta.magnitude;
            if (distance >= total - Epsilon)
            {
                Vector3 direction = SafeDirection(delta, tangent);
                positions[0] = root;
                for (int i = 0; i < count; i++)
                    positions[i + 1] = ProjectSegment(positions[i], positions[i] + direction * lengths[i],
                        lengths[i], direction, constraints);
                return Vector3.Distance(positions[count], tip);
            }

            SeedArc(positions, lengths, count, total, root, target, normal, tangent, archHeight);

            int freeCount = count;
            float freeLength = total;
            Vector3 freeTarget = target;
            float weight = IsFinite(contactWeight) ? Mathf.Clamp01(contactWeight) : 0f;

            if (weight > Epsilon && count >= 4)
            {
                // Reserve one tip bone, then extend the section up to four while remaining
                // within 18% of the chain. This also works with strongly tapered rigs.
                int contactCount = 1;
                float contactLength = lengths[count - 1];
                while (contactCount < 4 && contactCount < count - 2 &&
                       contactLength + lengths[count - contactCount - 1] <= total * 0.18f)
                {
                    contactLength += lengths[count - contactCount - 1];
                    contactCount++;
                }

                int candidateCount = count - contactCount;
                float candidateLength = total - contactLength;
                float longest = 0f;
                for (int i = 0; i < candidateCount; i++)
                    longest = Mathf.Max(longest, lengths[i]);
                float minimumReach = Mathf.Max(0f, longest * 2f - candidateLength);
                Vector3 candidate = ContactRoot(positions, lengths, candidateCount, target,
                    tangent, weight, constraints, false);

                float candidateReach = Vector3.Distance(root, candidate);
                if (candidateReach > candidateLength || candidateReach < minimumReach)
                {
                    float low = 0f;
                    float high = weight;
                    for (int pass = 0; pass < 12; pass++)
                    {
                        float mid = (low + high) * 0.5f;
                        Vector3 point = ContactRoot(positions, lengths, candidateCount, target,
                            tangent, mid, constraints, false);
                        float reach = Vector3.Distance(root, point);
                        if (reach <= candidateLength && reach >= minimumReach)
                            low = mid;
                        else
                            high = mid;
                    }
                    weight = low;
                }

                // At almost zero weight the unconstrained whole-chain solution is more stable
                // than reserving an unreachable or fully straight contact section.
                if (weight > 0.001f && candidateLength > Epsilon)
                {
                    freeCount = candidateCount;
                    freeLength = candidateLength;
                    freeTarget = ContactRoot(positions, lengths, freeCount, target, tangent,
                        weight, constraints, true);
                    SeedArc(positions, lengths, freeCount, freeLength, root, freeTarget,
                        normal, tangent, archHeight);
                }
            }

            float tolerance = Mathf.Max(total * 0.0001f, Epsilon);
            int passes = Mathf.Clamp(iterations, 1, 64);
            for (int pass = 0; pass < passes; pass++)
            {
                positions[freeCount] = freeTarget;
                for (int i = freeCount - 1; i >= 0; i--)
                    positions[i] = ProjectSegment(positions[i + 1], positions[i], lengths[i],
                        -tangent, constraints);

                positions[0] = root;
                for (int i = 0; i < freeCount; i++)
                    positions[i + 1] = ProjectSegment(positions[i], positions[i + 1], lengths[i],
                        tangent, constraints);

                if ((positions[freeCount] - freeTarget).sqrMagnitude <= tolerance * tolerance)
                    break;
            }

            // A finite iteration budget may leave a small prefix residual. Translate the
            // contact section with that residual so the boundary bone is never stretched.
            // Use exact sphere/plane projections to retain both length and ground clearance.
            if (freeCount < count)
            {
                Vector3 offset = positions[freeCount] - freeTarget;
                for (int i = freeCount; i < count; i++)
                    positions[i + 1] = ProjectSegment(positions[i], positions[i + 1] + offset,
                        lengths[i], tangent, constraints);
            }

            return Vector3.Distance(positions[count], tip);
        }

        private struct SurfaceConstraints
        {
            public Vector3 GroundNormal;
            public float GroundPlane;
            public bool HasWall;
            public Vector3 WallPoint;
            public Vector3 WallNormal;
            public float WallPlane;
        }

        private static Vector3 ContactRoot(Vector3[] positions, float[] lengths, int first,
            Vector3 tip, Vector3 tangent, float weight, SurfaceConstraints constraints, bool write)
        {
            Vector3 point = tip;
            for (int i = lengths.Length - 1; i >= first; i--)
            {
                Vector3 original = SafeDirection(positions[i + 1] - positions[i], tangent);
                Vector3 direction = SafeDirection(Vector3.Lerp(original, tangent, weight), tangent);
                Vector3 parent = ProjectSegment(point, point - direction * lengths[i], lengths[i],
                    -tangent, constraints);
                // Traverse backwards: the original current segment is read before its endpoint
                // is replaced, so no temporary array is needed.
                if (write)
                    positions[i + 1] = point;
                point = parent;
            }
            if (write)
                positions[first] = point;
            return point;
        }

        private static void SeedArc(Vector3[] positions, float[] lengths, int count, float total,
            Vector3 root, Vector3 target, Vector3 normal, Vector3 tangent, float archHeight)
        {
            Vector3 delta = target - root;
            float distance = delta.magnitude;
            Vector3 along = SafeDirection(delta, tangent);
            Vector3 bend = SafeDirection(Vector3.ProjectOnPlane(normal, along),
                SafeDirection(Vector3.ProjectOnPlane(tangent, along), Perpendicular(along)));

            // A monotonic turn along a circular arc gives every bone its exact length and
            // produces a convex upper arch. Solve the resultant chord length, then rotate its
            // tiny asymmetry back onto the target chord (important for tapered skeletons).
            float low = 0f;
            float high = TwoPi;
            Vector2 fullTurn = ArcChord(lengths, count, total, high);
            if (distance > Epsilon && fullTurn.magnitude <= distance)
            {
                for (int pass = 0; pass < 18; pass++)
                {
                    float mid = (low + high) * 0.5f;
                    if (ArcChord(lengths, count, total, mid).magnitude > distance)
                        low = mid;
                    else
                        high = mid;
                }

                float turn = (low + high) * 0.5f;
                Vector2 chord = ArcChord(lengths, count, total, turn);
                float offset = -Mathf.Atan2(chord.y, chord.x);
                float travelled = 0f;
                positions[0] = root;
                for (int i = 0; i < count; i++)
                {
                    float angle = turn * (0.5f - (travelled + lengths[i] * 0.5f) / total) + offset;
                    positions[i + 1] = positions[i] + lengths[i] *
                        (along * Mathf.Cos(angle) + bend * Mathf.Sin(angle));
                    travelled += lengths[i];
                }
                return;
            }

            // Coincident endpoints and a dominant single bone may have no circular solution.
            // A deterministic bowed seed still allows the distance solver to find the closest
            // feasible pose and avoids the collinear singularity of a straight-line seed.
            float preferred = IsFinite(archHeight) ? Mathf.Max(0f, archHeight) : 0f;
            float height = Mathf.Max(preferred,
                Mathf.Sqrt(Mathf.Max(0f, total * total - distance * distance)) * 0.4f);
            float cumulative = 0f;
            positions[0] = root;
            for (int i = 0; i < count; i++)
            {
                cumulative += lengths[i];
                float t = cumulative / total;
                positions[i + 1] = Vector3.Lerp(root, target, t) + bend * (height * Mathf.Sin(Mathf.PI * t));
            }
        }

        private static Vector2 ArcChord(float[] lengths, int count, float total, float turn)
        {
            float x = 0f;
            float y = 0f;
            float travelled = 0f;
            for (int i = 0; i < count; i++)
            {
                float angle = turn * (0.5f - (travelled + lengths[i] * 0.5f) / total);
                x += lengths[i] * Mathf.Cos(angle);
                y += lengths[i] * Mathf.Sin(angle);
                travelled += lengths[i];
            }
            return new Vector2(x, y);
        }

        private static Vector3 ProjectSegment(Vector3 anchor, Vector3 candidate, float length,
            Vector3 fallback, SurfaceConstraints constraints)
        {
            Vector3 direction = SafeDirection(candidate - anchor, fallback);
            Vector3 result = anchor + direction * length;
            // Ground and wall are both convex half-spaces. Re-projecting the exact sphere
            // intersection a few times preserves the bone length while converging at their
            // shared edge (a wall/floor corner) without allocations.
            for (int pass = 0; pass < 4; pass++)
            {
                bool changed = false;
                if (Vector3.Dot(result, constraints.GroundNormal) < constraints.GroundPlane)
                {
                    result = ProjectSphereAbovePlane(anchor, result, length, fallback,
                        constraints.GroundNormal, constraints.GroundPlane);
                    changed = true;
                }
                if (constraints.HasWall && Vector3.Dot(result, constraints.WallNormal) < constraints.WallPlane)
                {
                    result = ProjectSphereAbovePlane(anchor, result, length, fallback,
                        constraints.WallNormal, constraints.WallPlane);
                    changed = true;
                }
                if (!changed) break;
            }
            return result;
        }

        private static Vector3 ConstrainPoint(Vector3 point, SurfaceConstraints constraints)
        {
            for (int pass = 0; pass < 3; pass++)
            {
                bool changed = false;
                float groundError = constraints.GroundPlane - Vector3.Dot(point, constraints.GroundNormal);
                if (groundError > 0f)
                {
                    point += constraints.GroundNormal * groundError;
                    changed = true;
                }
                if (constraints.HasWall)
                {
                    float wallError = constraints.WallPlane - Vector3.Dot(point, constraints.WallNormal);
                    if (wallError > 0f)
                    {
                        point += constraints.WallNormal * wallError;
                        changed = true;
                    }
                }
                if (!changed) break;
            }
            return point;
        }

        private static Vector3 ProjectSphereAbovePlane(Vector3 anchor, Vector3 candidate, float length,
            Vector3 fallback, Vector3 planeNormal, float plane)
        {
            float anchorHeight = Vector3.Dot(anchor, planeNormal) - plane;
            if (anchorHeight >= length) return anchor - planeNormal * length;
            if (anchorHeight <= -length) return anchor + planeNormal * length;

            float required = -anchorHeight;
            Vector3 direction = SafeDirection(candidate - anchor, fallback);
            Vector3 planar = SafeDirection(Vector3.ProjectOnPlane(direction, planeNormal),
                SafeDirection(Vector3.ProjectOnPlane(fallback, planeNormal), Perpendicular(planeNormal)));
            float radius = Mathf.Sqrt(Mathf.Max(0f, length * length - required * required));
            return anchor + planeNormal * required + planar * radius;
        }

        private static Vector3 Perpendicular(Vector3 direction)
        {
            Vector3 axis = Mathf.Abs(direction.y) < 0.9f ? Vector3.up : Vector3.right;
            return Vector3.Cross(direction, axis).normalized;
        }

        private static Vector3 SafeDirection(Vector3 value, Vector3 fallback)
        {
            float square = value.sqrMagnitude;
            return IsFinite(square) && square > Epsilon * Epsilon
                ? value / Mathf.Sqrt(square) : fallback;
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    public enum BestTarget { Closest, Facing }
    public enum UseMode { Press, Hold }

    [Serializable]
    public struct InteractSettings
    {
        [Tooltip("How far (tiles, centre to centre) a target can be and still be used.")]
        [Min(0f)] public float reach;
        [Tooltip("Closest: the nearest target in reach. Facing: the one most in front of you (closest breaks ties; nothing in front -> closest).")]
        public BestTarget best;
        [Tooltip("Press: use on the key going down. Hold: keep it down for holdTime.")]
        public UseMode use;
        [Tooltip("Seconds to hold when use is Hold.")]
        [Min(0f)] public float holdTime;

        public InteractSettings(float reach, BestTarget best, UseMode use, float holdTime)
        {
            this.reach = reach;
            this.best = best;
            this.use = use;
            this.holdTime = holdTime;
        }
    }

    // The one "which target" rule (CLAUDE.md, one copy of each core rule). Takes plain
    // points so the checks, the player and any later AI call the same thing. Facing is a
    // direction (Facing.ToVector of the player's facing).
    public static class Interaction
    {
        // Angles closer than this count as equal, and the closer target wins.
        const float SameAngle = 1f;

        // Index of the best point in reach, or -1 for none.
        public static int Pick(Vector2 from, Vector2 facing, IReadOnlyList<Vector2> points, InteractSettings s)
        {
            float reach2 = s.reach * s.reach;
            int closest = -1, front = -1;
            float closestD = float.MaxValue, frontA = float.MaxValue, frontD = float.MaxValue;
            bool useFacing = s.best == BestTarget.Facing && facing.sqrMagnitude > 1e-8f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 to = points[i] - from;
                float d = to.sqrMagnitude;
                if (d > reach2) continue;
                if (d < closestD) { closestD = d; closest = i; }
                if (!useFacing) continue;
                // Standing on it counts as straight ahead.
                float a = d < 1e-8f ? 0f : Vector2.Angle(facing, to);
                if (a >= 90f) continue;
                bool better = front < 0 || a < frontA - SameAngle || (Mathf.Abs(a - frontA) <= SameAngle && d < frontD);
                if (better) { front = i; frontA = a; frontD = d; }
            }
            return useFacing && front >= 0 ? front : closest;
        }
    }
}

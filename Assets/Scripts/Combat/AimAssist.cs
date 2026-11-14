using UnityEngine;

namespace TopDownKit
{
    // Aim assist settings: a feel setting on the shooter (Gun), not on the weapon.
    [System.Serializable]
    public struct AimAssistSettings
    {
        [Tooltip("0 = off. 1 = aim straight at the target. Between: that share of the angle is closed.")]
        [Range(0f, 1f)] public float strength;
        [Tooltip("Degrees each side of the aim a target may be in.")]
        [Range(0f, 90f)] public float cone;
        [Tooltip("Tiles: targets further away are ignored.")]
        [Min(0f)] public float range;
    }

    // The one aim-assist rule: nudges an aim direction toward the nearest living target of
    // another team inside a cone. Aim still arrives as data; this only bends it before a
    // shot or swing. Sees through walls (nothing in the kit checks sight yet).
    public static class AimAssist
    {
        public static Vector2 Nudge(Vector2 origin, Vector2 aim, int team, AimAssistSettings s)
        {
            if (s.strength <= 0f || s.cone <= 0f || s.range <= 0f || aim.sqrMagnitude < 1e-6f) return aim;
            Vector2 best = Vector2.zero;
            float bestDist = float.MaxValue;
            var all = Health.All;
            for (int i = 0; i < all.Count; i++)
            {
                Health h = all[i];
                if (h == null || h.team == team || h.Dead) continue;
                Vector2 d = (Vector2)h.transform.position - origin;
                float dist = d.magnitude;
                if (dist > s.range || dist < 1e-4f || dist >= bestDist) continue;
                if (Mathf.Abs(Vector2.SignedAngle(aim, d)) > s.cone) continue;
                best = d;
                bestDist = dist;
            }
            if (bestDist == float.MaxValue) return aim;
            float turn = Vector2.SignedAngle(aim, best) * Mathf.Clamp01(s.strength) * Mathf.Deg2Rad;
            float c = Mathf.Cos(turn), sn = Mathf.Sin(turn);
            Vector2 a = aim.normalized;
            return new Vector2(a.x * c - a.y * sn, a.x * sn + a.y * c);
        }
    }
}

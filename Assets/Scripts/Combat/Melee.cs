using UnityEngine;

namespace TopDownKit
{
    // The one melee rule: is a point inside the part of a swing swept so far? Plain
    // numbers in, a yes out; MeleeSwing calls it every fixed step, a check can call it
    // directly. Angles are degrees relative to the aim, counter-clockwise positive.
    public static class Melee
    {
        // Where the blade points (degrees from the aim) after `elapsed` seconds of a swing.
        // A swing sweeps from -arc/2 to +arc/2; swingTime 0 = the whole arc at once.
        public static float BladeAngle(float arc, float swingTime, float elapsed)
        {
            float t = swingTime <= 0f ? 1f : Mathf.Clamp01(elapsed / swingTime);
            return -arc * 0.5f + arc * t;
        }

        // True if `point` is within `reach` tiles of `origin` (counting `targetRadius`, the
        // target's own size) and its direction lies between the angles `from` and `to`
        // (degrees from `aim`, from <= to).
        public static bool InSweep(Vector2 origin, Vector2 aim, float from, float to, float reach, Vector2 point, float targetRadius)
        {
            Vector2 d = point - origin;
            float dist = d.magnitude;
            if (dist - targetRadius > reach) return false;
            if (dist < 1e-4f) return true;   // standing on the wielder: always hit
            float a = Vector2.SignedAngle(aim, d);
            // A target this close covers some angle on its own; widen the sweep by it.
            float pad = targetRadius >= dist ? 180f : Mathf.Asin(targetRadius / dist) * Mathf.Rad2Deg;
            return a >= from - pad && a <= to + pad;
        }
    }
}

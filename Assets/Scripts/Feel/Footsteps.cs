using UnityEngine;

namespace TopDownKit
{
    // Add-on: plays the Step slot every `stride` tiles this object walks. Counted on the
    // fixed step from how far it really moved, so walls and grid steps count right.
    // Dashes and jumps longer than a tile in one step (warps) do not count.
    public class Footsteps : MonoBehaviour
    {
        [Tooltip("Tiles walked between two step sounds (1 = once per tile, right for grid step).")]
        [Min(0.1f)] public float stride = 1f;

        Vector2 last;
        float walked;
        bool started;
        Dash dash;
        Rigidbody2D body;

        void FixedUpdate()
        {
            if (!started) { body = GetComponent<Rigidbody2D>(); dash = GetComponent<Dash>(); }
            // The body's position: an interpolated transform lags behind on the fixed step.
            Vector2 p = body != null ? body.position : (Vector2)transform.position;
            if (!started) { started = true; last = p; return; }
            float d = Vector2.Distance(p, last);
            last = p;
            if (d > 1f || (dash != null && dash.IsDashing)) return;
            walked += d;
            if (walked < stride) return;
            walked -= stride;
            FeelPlayer.Play(FeelEvent.Step, p);
        }
    }
}

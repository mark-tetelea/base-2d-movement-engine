using UnityEngine;

namespace TopDownKit
{
    // The one knockback / recoil rule (CLAUDE.md, Decision 2). A push is its own velocity,
    // kept apart from the walking velocity and added to it after Movement.Step - pushed
    // into Step, an Instant feel would cancel it in one step (LOG 2026-10-18). The player
    // uses it now; enemies, bullets and recoil call the same two functions later.
    public static class Knockback
    {
        // A push of `speed` tiles per second pointing from `source` towards `target`
        // (away from the source). Source and target in the same place push nowhere.
        public static Vector2 Away(Vector2 source, Vector2 target, float speed)
        {
            Vector2 d = target - source;
            return d == Vector2.zero ? Vector2.zero : d.normalized * speed;
        }

        // The push after one step of dt seconds: it loses stopRate tiles/s every second,
        // in a straight line, until it is zero. A stopRate of 0 or less stops it at once.
        // A push of speed v lasts v / stopRate seconds and carries the mover
        // v * v / (2 * stopRate) tiles.
        public static Vector2 Step(Vector2 push, float stopRate, float dt)
        {
            if (stopRate <= 0f) return Vector2.zero;
            return Vector2.MoveTowards(push, Vector2.zero, stopRate * dt);
        }
    }
}

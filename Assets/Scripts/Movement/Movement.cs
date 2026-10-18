using UnityEngine;

namespace TopDownKit
{
    // The one movement rule (CLAUDE.md, Decision 2): the player calls it now; enemies,
    // editor previews and tests call the same function later. It never reads Time, input
    // or any scene object, so the same call gives the same answer anywhere.
    public static class Movement
    {
        // The velocity (tiles per second) after one step of dt seconds.
        //
        // moveDirection is where the mover wants to go, and its length is how hard: 1 is
        // full speed, 0 is letting go. Longer than 1 is cut to 1, so a keyboard diagonal
        // (1, 1) is no faster than a straight line and a touch stick can ask for less.
        //
        // With a direction, the velocity heads for moveDirection * speed at the speed-up
        // rate (speed / timeToFullSpeed); with none, it heads for zero at the stopping
        // rate (speed / timeToStop). Both are straight lines in velocity space, so a full
        // reversal takes twice timeToFullSpeed. A time of 0 jumps straight there.
        public static Vector2 Step(Vector2 velocity, Vector2 moveDirection, MoveSettings settings, float dt)
        {
            if (moveDirection.sqrMagnitude > 1f) moveDirection.Normalize();
            Vector2 target = moveDirection * settings.speed;

            float time = moveDirection == Vector2.zero ? settings.timeToStop : settings.timeToFullSpeed;
            if (time <= 0f || settings.speed <= 0f) return target;

            return Vector2.MoveTowards(velocity, target, settings.speed / time * dt);
        }
    }
}

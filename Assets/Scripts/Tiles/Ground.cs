using UnityEngine;

namespace TopDownKit
{
    // The one ground rule (CLAUDE.md, one copy of each core rule): how a crumbling tile
    // changes state, and what the ground does to whoever stands on it. GroundMap calls it
    // every fixed step; tests call the same functions. Times are counted in whole fixed
    // steps (trap 79), so a 1 s break is exactly 60 steps at any frame rate.
    public static class Ground
    {
        public static int Steps(float seconds, float dt) => Mathf.Max(1, Mathf.RoundToInt(seconds / dt));

        // One step of a crumbling tile. Normal -> Warning when stepped on (breakAfter);
        // Warning -> Broken when that runs out (backAfter); Broken -> Normal when that runs
        // out, unless backAfter is 0 (stays broken). Still stood on once it is back, it
        // starts again on the next step. A tile that does not crumble stays Normal.
        public static GroundState Crumble(GroundState state, ref int stepsLeft, bool steppedOn, GroundTile tile, float dt)
        {
            switch (state)
            {
                case GroundState.Warning:
                    if (--stepsLeft > 0) return GroundState.Warning;
                    stepsLeft = tile.backAfter > 0f ? Steps(tile.backAfter, dt) : 0;
                    return GroundState.Broken;
                case GroundState.Broken:
                    if (tile.backAfter <= 0f || --stepsLeft > 0) return GroundState.Broken;
                    return GroundState.Normal;
                case GroundState.Normal:
                    if (!steppedOn || tile == null || !tile.Crumbles) return GroundState.Normal;
                    stepsLeft = Steps(tile.breakAfter, dt);
                    return GroundState.Warning;
                default:
                    return state;
            }
        }

        // What the ground does this step to `who`, standing on `rules`, through the one
        // damage rule (Health.Hit: invulnerable time and a protecting dash apply). A kill
        // that does not land (invulnerable, dashing) is tried again every step.
        // `stepsToHit` counts down to the next damage-per-second hit; 0 = hit now.
        public static void Hurt(Health who, GroundRules rules, ref int stepsToHit, float dt)
        {
            if (who == null || who.Dead) return;
            if (rules.kills) { who.Hit(who.maxHealth, Vector2.zero, HitSource.Ground); return; }
            if (rules.damagePerSecond <= 0) { stepsToHit = 0; return; }
            if (stepsToHit > 0) { stepsToHit--; return; }
            who.Hit(rules.damagePerSecond, Vector2.zero, HitSource.Ground);
            stepsToHit = Steps(1f, dt) - 1;
        }

        public static bool Hurts(GroundRules r) => r.kills || r.damagePerSecond > 0;
    }
}

using UnityEngine;

namespace TopDownKit
{
    // The one damage rule (CLAUDE.md, one copy of each core rule). The player, enemies and
    // tests all go through it via Health.Hit. Health is whole points; invulnerable time is
    // in seconds and counts down on the fixed step.
    public static class Damage
    {
        // Takes `amount` off `health` unless the target is already dead, still
        // invulnerable from the last hit, or shielded (a dodge roll). A hit that lands
        // starts `invulnerableTime` seconds of invulnerability. Returns whether it landed.
        // Below this, invulnerable time counts as over: float steps of 1/60 never sum to
        // exactly 1, and without it a 1 s invulnerability lasted 61 steps (LOG 2026-11-14).
        const float Done = 1e-4f;

        public static bool Apply(ref int health, ref float invulnerableLeft, int amount, float invulnerableTime, bool shielded)
        {
            if (health <= 0 || amount <= 0 || shielded || invulnerableLeft > Done) return false;
            health = Mathf.Max(0, health - amount);
            invulnerableLeft = invulnerableTime;
            return true;
        }

        // Invulnerable time after one step of dt seconds.
        public static float Tick(float invulnerableLeft, float dt)
        {
            float left = invulnerableLeft - dt;
            return left > Done ? left : 0f;
        }
    }
}

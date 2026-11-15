using System;
using UnityEngine;

namespace TopDownKit
{
    // One phase of a boss: what its EnemyBrain and Gun do from a health level down.
    [Serializable]
    public struct BossPhase
    {
        [Tooltip("Shown on the HUD under the boss's name (empty = nothing).")]
        public string name;
        [Tooltip("The phase starts when health falls to this fraction of the maximum or below (the first phase: 1).")]
        [Range(0f, 1f)] public float fromHealth;
        public EnemyBehaviour behaviour;
        [Tooltip("The weapon in this phase. Empty = keep the one it has.")]
        public Weapon weapon;
        [Tooltip("The movement feel in this phase. Empty = keep the one it has.")]
        public MoveFeel feel;
        [Tooltip("KeepDistanceAndShoot: the distance (tiles) it holds.")]
        [Min(0f)] public float keepDistance;
        [Tooltip("Fire the gun in any behaviour (see EnemyBrain.alwaysFire).")]
        public bool alwaysFire;
    }

    // Add-on that makes an enemy a boss: its phases switch at health levels, each one
    // swapping the EnemyBrain behaviour, the Gun's weapon and the movement feel - the same
    // pieces every enemy uses. At each switch it stands still for `switchPause` seconds,
    // so the player sees the change coming. With a Hud set it shows a boss health bar
    // while alive and awake (its object switched on).
    [RequireComponent(typeof(EnemyBrain), typeof(Health))]
    public class BossPhases : MonoBehaviour
    {
        public string bossName = "Boss";
        [Tooltip("In order, highest health first.")]
        public BossPhase[] phases;
        [Tooltip("Seconds it stands still when a new phase starts (0 = none).")]
        [Min(0f)] public float switchPause = 0.6f;
        [Tooltip("Shows the boss bar here. Empty = no bar.")]
        public Hud hud;

        EnemyBrain brain;
        Health health;
        Gun gun;
        PlayerMover mover;
        int pauseLeft;

        public int Phase { get; private set; } = -1;

        void FixedUpdate()
        {
            if (brain == null)
            {
                brain = GetComponent<EnemyBrain>();
                health = GetComponent<Health>();
                gun = GetComponent<Gun>();
                mover = GetComponent<PlayerMover>();
            }
            if (phases == null || phases.Length == 0) return;
            float f = health.maxHealth > 0 ? (float)health.Current / health.maxHealth : 0f;
            int want = 0;
            for (int i = 0; i < phases.Length; i++) if (f <= phases[i].fromHealth + 1e-5f) want = i;
            if (want != Phase) Enter(want, Phase >= 0);

            if (pauseLeft > 0 && --pauseLeft == 0) brain.enabled = true;

            if (hud == null) hud = Hud.Current;   // a generated room: the level's HUD
            if (hud != null)
            {
                hud.Boss = health.Dead ? null : health;
                string p = phases[Phase].name;
                hud.BossName = string.IsNullOrEmpty(p) ? bossName : bossName + " - " + p;
            }
        }

        void Enter(int index, bool pause)
        {
            Phase = index;
            BossPhase p = phases[index];
            brain.behaviour = p.behaviour;
            brain.keepDistance = p.keepDistance;
            brain.alwaysFire = p.alwaysFire;
            if (p.feel != null && mover != null) mover.feel = p.feel;
            if (p.weapon != null && gun != null)
            {
                gun.weapons = new[] { p.weapon };
                gun.Select(0);
                gun.Refill();
            }
            if (pause && switchPause > 0f)
            {
                pauseLeft = Mathf.Max(1, Mathf.RoundToInt(switchPause / Time.fixedDeltaTime));
                brain.enabled = false;
                if (mover != null) mover.SetMoveDirection(Vector2.zero);
                if (gun != null) gun.SetTrigger(false);
            }
        }

        void OnDisable()
        {
            if (hud != null && ReferenceEquals(hud.Boss, health)) hud.Boss = null;
        }
    }
}

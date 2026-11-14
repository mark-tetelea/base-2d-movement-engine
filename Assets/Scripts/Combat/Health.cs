using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Health points for anything that can be hit: the player and enemies alike. Damage
    // goes through the one rule (Damage.Apply); a hit that lands pushes the target with
    // the one knockback rule (if it has a PlayerMover), flashes its sprites and can shake
    // the camera. A Dash with its invulnerable flag on protects while it runs.
    // Where a hit came from (Health.LastSource): add-ons such as sounds and hit-stop read it.
    public enum HitSource { Other, Shot, Melee, Touch, Ground }

    public class Health : MonoBehaviour
    {
        [Min(1)] public int maxHealth = 5;
        [Tooltip("Hits from the same team are ignored (no friendly fire). Player 0, enemies 1.")]
        public int team;
        [Tooltip("Seconds after a hit during which further hits are ignored.")]
        [Min(0f)] public float invulnerableTime = 0.6f;
        [Tooltip("Seconds the sprites show the flash colour after a hit.")]
        [Min(0f)] public float flashTime = 0.1f;
        public Color flashColor = new Color(1f, 0.25f, 0.25f, 1f);
        [Tooltip("Blink the sprites while invulnerable (after the flash).")]
        public bool blinkWhileInvulnerable = true;
        [Tooltip("Screen shake in tiles when this is hit (0 = none). Needs a CameraRig on the main camera.")]
        [Min(0f)] public float shakeOnHit;
        [Tooltip("Destroy the object when health reaches 0. Off for the player (something else restarts).")]
        public bool destroyOnDeath = true;

        public int Current { get; private set; }
        public bool Dead => started && Current <= 0;
        public bool Invulnerable => invulnerableLeft > 0f;
        public event Action<Health> Damaged;
        public event Action<Health> Died;
        // The same, for every Health at once (add-ons such as sounds and particles listen here).
        public static event Action<Health> AnyDamaged;
        public static event Action<Health> AnyDied;
        // Where the last hit that landed came from.
        public HitSource LastSource { get; private set; }

        static readonly List<Health> all = new List<Health>();
        public static IReadOnlyList<Health> All => all;

        bool started;
        float invulnerableLeft;
        float flashLeft;
        PlayerMover mover;
        Dash dash;
        SpriteRenderer[] sprites;
        Color[] colors;

        void OnEnable() { Init(); if (!all.Contains(this)) all.Add(this); }
        void OnDisable() { all.Remove(this); }

        void Init()
        {
            if (started) return;
            started = true;
            Current = maxHealth;
            mover = GetComponent<PlayerMover>();
            dash = GetComponent<Dash>();
        }

        // Back to full health (a restart without reloading the scene).
        public void Restore() { Init(); Current = maxHealth; invulnerableLeft = 0f; }

        // Gives health back, up to maxHealth (a health pickup). Not while dead.
        public void Heal(int amount)
        {
            Init();
            if (amount <= 0 || Current <= 0) return;
            Current = Mathf.Min(maxHealth, Current + amount);
        }

        // Damage with a knockback push (tiles/s, already pointing the way to push; zero for
        // none). Returns whether the hit landed.
        public bool Hit(int amount, Vector2 push, HitSource source = HitSource.Other)
        {
            Init();
            // Looked up here too, so a Dash or mover added after this woke up still counts (trap 14).
            if (dash == null) dash = GetComponent<Dash>();
            if (mover == null) mover = GetComponent<PlayerMover>();
            bool shielded = dash != null && dash.IsInvulnerable;
            int hp = Current;
            if (!Damage.Apply(ref hp, ref invulnerableLeft, amount, invulnerableTime, shielded)) return false;
            Current = hp;
            LastSource = source;
            if (mover != null && push != Vector2.zero) mover.AddPush(push);
            flashLeft = flashTime;
            if (shakeOnHit > 0f)
            {
                Camera cam = Camera.main;
                CameraRig rig = cam != null ? cam.GetComponent<CameraRig>() : null;
                if (rig != null) rig.Shake(shakeOnHit);
            }
            Damaged?.Invoke(this);
            AnyDamaged?.Invoke(this);
            if (Current <= 0)
            {
                Died?.Invoke(this);
                AnyDied?.Invoke(this);
                if (destroyOnDeath) Destroy(gameObject);
            }
            return true;
        }

        void FixedUpdate() { invulnerableLeft = Damage.Tick(invulnerableLeft, Time.fixedDeltaTime); }

        // Looks only: the flash and blink run on frame time.
        void Update()
        {
            if (sprites == null)
            {
                sprites = GetComponentsInChildren<SpriteRenderer>(true);
                colors = new Color[sprites.Length];
                for (int i = 0; i < sprites.Length; i++) colors[i] = sprites[i].color;
            }
            flashLeft = Mathf.Max(0f, flashLeft - Time.deltaTime);
            bool flash = flashLeft > 0f;
            bool hidden = !flash && blinkWhileInvulnerable && invulnerableLeft > 0f && Mathf.Repeat(Time.time * 12f, 1f) < 0.5f;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color c = flash ? flashColor : colors[i];
                if (hidden) c.a *= 0.25f;
                sprites[i].color = c;
            }
        }
    }
}

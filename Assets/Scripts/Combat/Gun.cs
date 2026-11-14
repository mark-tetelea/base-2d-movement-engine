using UnityEngine;

namespace TopDownKit
{
    // Fires the current Weapon along an aim direction, on the fixed step. Input arrives as
    // data: whoever drives it (PlayerShooter for the keyboard and mouse, EnemyBrain for an
    // enemy, a touch button later) calls SetAim / SetTrigger / Reload / Select.
    // Recoil pushes the shooter's PlayerMover with the one knockback rule. Settings on the
    // weapon pick the extras: charge (hold, release), melee (a MeleeSwing, added on first
    // use), bounces (the Bullet's job). Aim assist bends the aim at the shot, here.
    public class Gun : MonoBehaviour
    {
        [Tooltip("Weapons carried; Select switches between them.")]
        public Weapon[] weapons;
        [Tooltip("Bullets ignore this team (set it the same as the shooter's Health).")]
        public int team;
        [Tooltip("Tiles from the shooter's centre where bullets appear.")]
        [Min(0f)] public float muzzleDistance = 0.5f;
        [Tooltip("Seed for the random angle of single-bullet spread, so a run repeats.")]
        public int seed = 1;
        [Tooltip("Bends the aim toward the nearest enemy in a cone at each shot or swing. Strength 0 = off.")]
        public AimAssistSettings aimAssist = new AimAssistSettings { strength = 0f, cone = 15f, range = 10f };

        int current;
        int[] ammo;
        float cooldown, reloadLeft, charged;
        bool trigger, pressed, charging;
        MeleeSwing swing;
        Vector2 aim = Vector2.right;
        System.Random random;
        PlayerMover mover;

        public Weapon Current => weapons != null && weapons.Length > 0 ? weapons[current] : null;
        public int CurrentIndex => current;
        public int Ammo { get { Init(); return ammo.Length > 0 ? ammo[current] : 0; } }
        public bool Reloading => reloadLeft > 0f;
        public int ShotsFired { get; private set; }
        // 0..1 while a charge weapon is held, else 0 (for a HUD or a glow).
        public float Charge01 { get { Weapon w = Current; return charging && w != null && w.chargeTime > 0f ? Mathf.Clamp01(charged / w.chargeTime) : 0f; } }

        void Init()
        {
            if (ammo != null && ammo.Length == (weapons == null ? 0 : weapons.Length)) return;
            ammo = new int[weapons == null ? 0 : weapons.Length];
            for (int i = 0; i < ammo.Length; i++) ammo[i] = weapons[i] != null ? weapons[i].magazine : 0;
            random = new System.Random(seed);
            mover = GetComponent<PlayerMover>();
        }

        public void SetAim(Vector2 direction) { if (direction.sqrMagnitude > 1e-6f) aim = direction.normalized; }

        // Held: the trigger is down. A press between two steps is kept until a step uses it.
        public void SetTrigger(bool held)
        {
            if (held && !trigger) pressed = true;
            trigger = held;
        }

        public void Reload()
        {
            Init();
            Weapon w = Current;
            if (w == null || w.magazine <= 0 || Reloading || ammo[current] >= w.magazine) return;
            reloadLeft = Mathf.Max(w.reloadTime, 1e-4f);
        }

        // Full magazines in every weapon, no reload, ready to fire (a restart, an ammo crate).
        public void Refill()
        {
            Init();
            for (int i = 0; i < ammo.Length; i++) ammo[i] = weapons[i] != null ? weapons[i].magazine : 0;
            reloadLeft = 0f;
            cooldown = 0f;
        }

        // Switching cancels a reload; the cooldown carries over.
        public void Select(int index)
        {
            Init();
            if (weapons == null || index < 0 || index >= weapons.Length || weapons[index] == null) return;
            current = index;
            reloadLeft = 0f;
            charging = false;
            charged = 0f;
        }

        void FixedUpdate()
        {
            Init();
            Weapon w = Current;
            float dt = Time.fixedDeltaTime;
            bool press = pressed;
            pressed = false;
            if (cooldown > 0f) cooldown -= dt;
            if (w == null) return;

            if (Reloading)
            {
                charging = false;
                reloadLeft -= dt;
                if (reloadLeft <= 1e-5f) { reloadLeft = 0f; ammo[current] = w.magazine; }
                return;
            }
            if (w.magazine > 0 && ammo[current] <= 0) { charging = false; Reload(); return; }
            bool busy = cooldown > 1e-5f || (swing != null && swing.Swinging);

            if (w.chargeTime > 0f)
            {
                // Hold to charge, release to fire. A press and release between two steps
                // still fires, uncharged. Charging waits for the cooldown.
                if (!charging && (trigger || press) && !busy) { charging = true; charged = 0f; }
                if (!charging) return;
                if (trigger) { charged += dt; return; }
                charging = false;
                Shoot(w, Mathf.Clamp01(charged / w.chargeTime));
                return;
            }

            bool wantShot = trigger && w.automatic || press;
            if (!wantShot || busy) return;
            Shoot(w, 0f);
        }

        // One shot or swing at charge t (0..1), then ammo and cooldown.
        void Shoot(Weapon w, float t)
        {
            float damageScale = Mathf.Lerp(1f, w.chargeDamage, t);
            float sizeScale = Mathf.Lerp(1f, w.chargeSize, t);
            float speedScale = Mathf.Lerp(1f, w.chargeSpeed, t);
            Vector2 dir = AimAssist.Nudge(transform.position, aim, team, aimAssist);
            ShotsFired++;
            if (w.kind == WeaponKind.Melee)
            {
                if (swing == null) swing = GetComponent<MeleeSwing>();
                if (swing == null) swing = gameObject.AddComponent<MeleeSwing>();
                swing.Begin(w, dir, team, Mathf.RoundToInt(w.damage * damageScale), w.melee.reach * sizeScale);
                cooldown = Mathf.Max(cooldown, 0f) + w.melee.swingTime + w.melee.cooldown;
            }
            else
            {
                Fire(w, dir, damageScale, sizeScale, speedScale);
                // Keeps the rate exact when 1/fireRate is not a whole number of steps.
                cooldown = Mathf.Max(cooldown, 0f) + 1f / w.fireRate;
            }
            if (w.magazine > 0) ammo[current]--;
            if (mover != null && w.recoil > 0f) mover.AddPush(-dir * w.recoil);
            if (w.shakeOnShot > 0f)
            {
                Camera cam = Camera.main;
                CameraRig rig = cam != null ? cam.GetComponent<CameraRig>() : null;
                if (rig != null) rig.Shake(w.shakeOnShot);
            }
        }

        void Fire(Weapon w, Vector2 dir, float damageScale, float sizeScale, float speedScale)
        {
            Vector2 origin = (Vector2)transform.position + dir * muzzleDistance;
            float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            for (int i = 0; i < w.bulletsPerShot; i++)
            {
                float offset;
                if (w.bulletsPerShot == 1) offset = ((float)random.NextDouble() - 0.5f) * w.spread;
                else offset = -w.spread * 0.5f + w.spread * i / (w.bulletsPerShot - 1);
                float a = (baseAngle + offset) * Mathf.Deg2Rad;
                Bullet.Spawn(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * w.bulletSpeed * speedScale, w, team, damageScale, sizeScale);
            }
        }
    }
}

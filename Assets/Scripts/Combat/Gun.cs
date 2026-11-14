using UnityEngine;

namespace TopDownKit
{
    // Fires the current Weapon along an aim direction, on the fixed step. Input arrives as
    // data: whoever drives it (PlayerShooter for the keyboard and mouse, EnemyBrain for an
    // enemy, a touch button later) calls SetAim / SetTrigger / Reload / Select.
    // Recoil pushes the shooter's PlayerMover with the one knockback rule.
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

        int current;
        int[] ammo;
        float cooldown, reloadLeft;
        bool trigger, pressed;
        Vector2 aim = Vector2.right;
        System.Random random;
        PlayerMover mover;

        public Weapon Current => weapons != null && weapons.Length > 0 ? weapons[current] : null;
        public int CurrentIndex => current;
        public int Ammo { get { Init(); return ammo.Length > 0 ? ammo[current] : 0; } }
        public bool Reloading => reloadLeft > 0f;
        public int ShotsFired { get; private set; }

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
        }

        void FixedUpdate()
        {
            Init();
            Weapon w = Current;
            float dt = Time.fixedDeltaTime;
            bool wantShot = trigger && (w != null && w.automatic) || pressed;
            pressed = false;
            if (cooldown > 0f) cooldown -= dt;
            if (w == null) return;

            if (Reloading)
            {
                reloadLeft -= dt;
                if (reloadLeft <= 1e-5f) { reloadLeft = 0f; ammo[current] = w.magazine; }
                return;
            }
            if (w.magazine > 0 && ammo[current] <= 0) { Reload(); return; }
            if (!wantShot || cooldown > 1e-5f) return;

            Fire(w);
            if (w.magazine > 0) ammo[current]--;
            // Keeps the rate exact when 1/fireRate is not a whole number of steps.
            cooldown = Mathf.Max(cooldown, 0f) + 1f / w.fireRate;
        }

        void Fire(Weapon w)
        {
            ShotsFired++;
            Vector2 origin = (Vector2)transform.position + aim * muzzleDistance;
            float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            for (int i = 0; i < w.bulletsPerShot; i++)
            {
                float offset;
                if (w.bulletsPerShot == 1) offset = ((float)random.NextDouble() - 0.5f) * w.spread;
                else offset = -w.spread * 0.5f + w.spread * i / (w.bulletsPerShot - 1);
                float a = (baseAngle + offset) * Mathf.Deg2Rad;
                Bullet.Spawn(origin, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * w.bulletSpeed, w, team);
            }
            if (mover != null && w.recoil > 0f) mover.AddPush(-aim * w.recoil);
            if (w.shakeOnShot > 0f)
            {
                Camera cam = Camera.main;
                CameraRig rig = cam != null ? cam.GetComponent<CameraRig>() : null;
                if (rig != null) rig.Shake(w.shakeOnShot);
            }
        }
    }
}

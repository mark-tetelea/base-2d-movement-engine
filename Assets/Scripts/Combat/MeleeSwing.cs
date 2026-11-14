using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Add-on piece: one melee swing at a time, on the fixed step. Gun starts it for a
    // weapon of kind Melee (it adds this component on first use). The swing follows the
    // wielder, keeps the aim it started with, and hits each target of another team once
    // per swing through the same Health.Hit as bullets. Walls stop it (low barriers do
    // not, like bullets). The blade art is a child stretched to the reach (looks only).
    public class MeleeSwing : MonoBehaviour
    {
        public bool Swinging { get; private set; }
        public int Swings { get; private set; }

        Weapon weapon;
        Vector2 aim;
        int team, damage;
        float reach, elapsed, lastAngle;
        bool hideNextStep;
        readonly HashSet<Health> hitThisSwing = new HashSet<Health>();
        Transform bladeRoot;
        SpriteRenderer blade;
        static Sprite plain;
        static readonly RaycastHit2D[] wallHits = new RaycastHit2D[16];

        // Starts a swing of `w` along `direction`. Damage and reach are already scaled
        // (charge). Ignored while a swing runs.
        public void Begin(Weapon w, Vector2 direction, int wielderTeam, int swingDamage, float swingReach)
        {
            if (Swinging || w == null) return;
            weapon = w;
            aim = direction.sqrMagnitude > 1e-6f ? direction.normalized : Vector2.right;
            team = wielderTeam;
            damage = swingDamage;
            reach = swingReach;
            elapsed = 0f;
            lastAngle = -w.melee.arc * 0.5f;
            hitThisSwing.Clear();
            Swinging = true;
            Swings++;
            ShowBlade(true);
            Sweep(0f);
        }

        void FixedUpdate()
        {
            if (!Swinging) return;
            Sweep(Time.fixedDeltaTime);
        }

        // Moves the blade on by dt and hits whatever the newly swept part covers.
        void Sweep(float dt)
        {
            MeleeSettings m = weapon.melee;
            elapsed += dt;
            float angle = Melee.BladeAngle(m.arc, m.swingTime, elapsed);
            Vector2 origin = transform.position;
            var all = Health.All;
            for (int i = all.Count - 1; i >= 0; i--)
            {
                Health h = all[i];
                if (h == null || h.team == team || h.Dead || hitThisSwing.Contains(h)) continue;
                Vector2 p = h.transform.position;
                float r = 0f;
                Collider2D c = h.GetComponent<Collider2D>();
                if (c != null) { p = c.bounds.center; r = Mathf.Min(c.bounds.extents.x, c.bounds.extents.y); }
                if (!Melee.InSweep(origin, aim, lastAngle, angle, reach, p, r)) continue;
                if (WallBetween(origin, p)) continue;
                hitThisSwing.Add(h);
                h.Hit(damage, (p - origin).normalized * weapon.knockback);
            }
            lastAngle = angle;
            PlaceBlade(angle);
            if (m.swingTime <= 0f || elapsed >= m.swingTime - 1e-4f)
            {
                Swinging = false;
                // A swing with no swing time still shows its blade for one step.
                if (m.swingTime > 0f) ShowBlade(false);
                else hideNextStep = true;
            }
        }

        // Looks only: hides a zero-time swing's blade on the next frame.
        void Update()
        {
            if (hideNextStep && !Swinging) { hideNextStep = false; ShowBlade(false); }
        }

        // Solid, non-low colliders that belong to nothing with Health count as walls.
        static bool WallBetween(Vector2 from, Vector2 to)
        {
            ContactFilter2D filter = default;
            filter.useTriggers = false;
            int n = Physics2D.Linecast(from, to, filter, wallHits);
            for (int i = 0; i < n; i++)
            {
                Collider2D c = wallHits[i].collider;
                if (c.GetComponent<ShootOver>() != null) continue;
                if (c.GetComponentInParent<Health>() != null) continue;
                return true;
            }
            return false;
        }

        void ShowBlade(bool on)
        {
            if (bladeRoot == null)
            {
                if (!on) return;
                bladeRoot = new GameObject("Blade").transform;
                bladeRoot.SetParent(transform, false);
                var art = new GameObject("Art");
                art.transform.SetParent(bladeRoot, false);
                blade = art.AddComponent<SpriteRenderer>();
                blade.sortingOrder = 49;
            }
            bladeRoot.gameObject.SetActive(on);
            if (!on) return;
            MeleeSettings m = weapon.melee;
            if (m.sprite != null) blade.sprite = m.sprite;
            else
            {
                if (plain == null)
                {
                    var tex = Texture2D.whiteTexture;
                    plain = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
                }
                blade.sprite = plain;
            }
            blade.color = m.color.a > 0f ? m.color : Color.white;
            // Art is scaled to match the data (trap 8): one sprite unit long -> reach tiles.
            Vector2 size = blade.sprite.bounds.size;
            blade.transform.localPosition = new Vector3(reach * 0.5f, 0f, 0f);
            blade.transform.localScale = new Vector3(reach / Mathf.Max(size.x, 1e-4f), m.width / Mathf.Max(size.y, 1e-4f), 1f);
        }

        void PlaceBlade(float angle)
        {
            if (bladeRoot == null) return;
            float a = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + angle;
            bladeRoot.rotation = Quaternion.Euler(0f, 0f, a);
        }

        void OnDisable() { Swinging = false; if (bladeRoot != null) bladeRoot.gameObject.SetActive(false); }
    }
}

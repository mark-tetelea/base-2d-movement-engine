using UnityEngine;
using UnityEngine.Pool;

namespace TopDownKit
{
    // A bullet flies in a straight line on the fixed step. Each step it sweeps a circle
    // along the distance it is about to cover, so a fast bullet cannot skip through a thin
    // wall or a target. The first solid thing in the way stops it: a wall, or anything with
    // a Health of another team (it takes the damage and a push along the bullet's path).
    // Same-team colliders, triggers and low barriers (ShootOver) are flown through. With
    // bounces left (Weapon.bounces) a wall reflects it instead; the bounce happens inside
    // the step, so no distance is lost, and range counts the whole path.
    // Bullets are reused through Unity's own ObjectPool (Bullet.Spawn); see LOG 2026-11-14
    // for the measurement that asked for it.
    [RequireComponent(typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        // Off: Instantiate and Destroy every bullet (kept only to repeat the measurement).
        public static bool reuse = true;
        public static int Alive { get; private set; }

        Rigidbody2D body;
        SpriteRenderer art;
        Vector2 velocity;
        float rangeLeft;
        int damage, team, bouncesLeft;
        float knockback, radius;
        bool flying;

        static ObjectPool<Bullet> pool;
        static Transform root;
        static readonly RaycastHit2D[] hits = new RaycastHit2D[16];

        // Fires one bullet from `position` at `velocity` (tiles/s) for a shooter of `team`.
        // damageScale and sizeScale come from a charged shot (1 = the weapon as it is).
        public static Bullet Spawn(Vector2 position, Vector2 velocity, Weapon w, int team, float damageScale = 1f, float sizeScale = 1f)
        {
            if (root == null)
            {
                // First bullet in this scene (a scene load destroyed the old ones).
                root = new GameObject("Bullets").transform;
                Alive = 0;
                pool = new ObjectPool<Bullet>(Create, b => b.gameObject.SetActive(true),
                    b => b.gameObject.SetActive(false), b => { if (b != null) Destroy(b.gameObject); },
                    false, 64, 1000);
            }
            Bullet bullet = reuse ? pool.Get() : Create();
            bullet.Launch(position, velocity, w, team, damageScale, sizeScale);
            return bullet;
        }

        static Bullet Create()
        {
            var go = new GameObject("Bullet");
            go.transform.SetParent(root, false);
            var b = go.AddComponent<Bullet>();
            b.body = go.GetComponent<Rigidbody2D>();
            b.body.bodyType = RigidbodyType2D.Kinematic;
            b.body.interpolation = RigidbodyInterpolation2D.Interpolate;
            b.art = go.AddComponent<SpriteRenderer>();
            b.art.sortingOrder = 50;
            return b;
        }

        void Launch(Vector2 position, Vector2 v, Weapon w, int shooterTeam, float damageScale, float sizeScale)
        {
            transform.position = position;
            body.position = position;
            velocity = v;
            rangeLeft = w.range;
            damage = Mathf.RoundToInt(w.damage * damageScale);
            knockback = w.knockback;
            radius = w.bulletRadius * sizeScale;
            bouncesLeft = w.bounces;
            // Art scaled to match the data (trap 8); reset every launch, bullets are reused.
            transform.localScale = new Vector3(sizeScale, sizeScale, 1f);
            team = shooterTeam;
            art.sprite = w.bulletSprite;
            art.color = w.bulletColor;
            transform.right = v;
            flying = true;
            Alive++;
        }

        void FixedUpdate()
        {
            if (!flying) return;
            float dt = Time.fixedDeltaTime;
            Vector2 pos = body.position;
            float speed = velocity.magnitude;
            Vector2 dir = velocity / speed;
            float travel = Mathf.Min(speed * dt, rangeLeft);

            ContactFilter2D filter = default;
            filter.useTriggers = false;
            // A few bounces per step at most (a corner); the rest of the step is dropped.
            for (int pass = 0; pass < 4 && travel > 1e-5f; pass++)
            {
                int n = Physics2D.CircleCast(pos, radius, dir, filter, hits, travel);
                float nearest = float.MaxValue;
                int hit = -1;
                Health hitHealth = null;
                for (int i = 0; i < n; i++)
                {
                    if (hits[i].collider.GetComponent<ShootOver>() != null) continue;   // a low barrier
                    Health h = hits[i].collider.GetComponentInParent<Health>();
                    if (h != null && (h.team == team || h.Dead)) continue;
                    // Touching a wall it is already leaving (just bounced off it).
                    if (h == null && hits[i].distance <= 1e-4f && Vector2.Dot(hits[i].normal, dir) >= 0f) continue;
                    if (hits[i].distance < nearest) { nearest = hits[i].distance; hit = i; hitHealth = h; }
                }
                if (hit < 0)
                {
                    pos += dir * travel;
                    rangeLeft -= travel;
                    travel = 0f;
                    break;
                }
                if (hitHealth != null || bouncesLeft <= 0)
                {
                    if (hitHealth != null) hitHealth.Hit(damage, dir * knockback, HitSource.Shot);
                    Release();
                    return;
                }
                // Bounce: to the contact, a hair off the wall, reflected.
                Vector2 normal = hits[hit].normal;
                bouncesLeft--;
                rangeLeft -= nearest;
                travel -= nearest;
                pos = hits[hit].centroid + normal * 0.02f;
                dir = Vector2.Reflect(dir, normal).normalized;
                velocity = dir * speed;
                transform.right = dir;
            }
            body.MovePosition(pos);
            if (rangeLeft <= 1e-5f) Release();
        }

        void Release()
        {
            flying = false;
            Alive--;
            if (reuse && pool != null) pool.Release(this);
            else Destroy(gameObject);
        }
    }
}

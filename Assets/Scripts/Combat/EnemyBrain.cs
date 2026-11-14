using UnityEngine;

namespace TopDownKit
{
    public enum EnemyBehaviour { Chase, KeepDistanceAndShoot, Wander }

    // A tiny enemy. It walks with the same PlayerMover and movement rule as the player
    // (keyboard off; this sets the direction as data), takes damage through the same
    // Health, and shoots with the same Gun. What it does is a setting. Everything runs on
    // the fixed step and counts steps, so it behaves the same at any frame rate.
    [RequireComponent(typeof(PlayerMover))]
    public class EnemyBrain : MonoBehaviour
    {
        public EnemyBehaviour behaviour = EnemyBehaviour.Chase;
        [Tooltip("Who it goes for.")]
        public Transform target;
        [Tooltip("Tiles: it ignores the target beyond this.")]
        [Min(0f)] public float sightRange = 14f;
        [Tooltip("KeepDistanceAndShoot: the distance (tiles) it tries to hold.")]
        [Min(0f)] public float keepDistance = 6f;
        [Tooltip("Wander and strafing: seconds between changes of direction.")]
        [Min(0.05f)] public float turnEvery = 1.2f;
        [Tooltip("Touch damage: hits the target when the centres are closer than this (tiles). 0 = none.")]
        [Min(0f)] public float touchRange = 0.9f;
        [Min(0)] public int touchDamage = 1;
        [Tooltip("Push on the target from a touch, tiles/s.")]
        [Min(0f)] public float touchKnockback = 10f;
        public int seed = 1;

        PlayerMover mover;
        Gun gun;
        Health targetHealth;
        System.Random random;
        int step;
        Vector2 wanderDir;
        float strafeSide = 1f;

        void FixedUpdate()
        {
            if (mover == null)
            {
                mover = GetComponent<PlayerMover>();
                mover.readKeyboard = false;
                gun = GetComponent<Gun>();
                random = new System.Random(seed);
            }
            if (target != null && targetHealth == null) targetHealth = target.GetComponent<Health>();
            bool targetAlive = target != null && (targetHealth == null || !targetHealth.Dead);

            Vector2 me = transform.position;
            Vector2 to = targetAlive ? (Vector2)target.position - me : Vector2.zero;
            float dist = to.magnitude;
            bool sees = targetAlive && dist <= sightRange;
            int turnSteps = Mathf.Max(1, Mathf.RoundToInt(turnEvery / Time.fixedDeltaTime));
            bool turn = step % turnSteps == 0;
            step++;

            Vector2 move = Vector2.zero;
            switch (behaviour)
            {
                case EnemyBehaviour.Chase:
                    if (sees) move = to / Mathf.Max(dist, 1e-4f);
                    break;
                case EnemyBehaviour.KeepDistanceAndShoot:
                    if (sees)
                    {
                        Vector2 toward = to / Mathf.Max(dist, 1e-4f);
                        if (turn) strafeSide = random.NextDouble() < 0.5 ? -1f : 1f;
                        if (dist > keepDistance + 1f) move = toward;
                        else if (dist < keepDistance - 1f) move = -toward;
                        else move = new Vector2(-toward.y, toward.x) * strafeSide * 0.6f;
                    }
                    break;
                case EnemyBehaviour.Wander:
                    if (turn)
                    {
                        double r = random.NextDouble();
                        float a = (float)(random.NextDouble() * Mathf.PI * 2.0);
                        wanderDir = r < 0.25 ? Vector2.zero : new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.6f;
                    }
                    move = wanderDir;
                    break;
            }
            mover.SetMoveDirection(move);

            if (gun != null)
            {
                if (sees) gun.SetAim(to);
                gun.SetTrigger(sees && behaviour == EnemyBehaviour.KeepDistanceAndShoot);
            }

            if (sees && touchRange > 0f && touchDamage > 0 && dist < touchRange && targetHealth != null)
                targetHealth.Hit(touchDamage, Knockback.Away(me, target.position, touchKnockback));
        }
    }
}

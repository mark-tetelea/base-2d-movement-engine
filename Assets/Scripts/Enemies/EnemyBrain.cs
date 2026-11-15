using UnityEngine;

namespace TopDownKit
{
    public enum EnemyBehaviour { Chase, KeepDistanceAndShoot, Wander }

    // A tiny enemy. It walks with the same PlayerMover and movement rule as the player
    // (keyboard off; this sets the direction as data), takes damage through the same
    // Health, and shoots with the same Gun. What it does is a setting. Everything runs on
    // the fixed step and counts steps, so it behaves the same at any frame rate.
    //
    // Add-ons it uses when present: PathWalker (walks around walls to reach the target),
    // EnemyGroup (on a leader: followers ask it for their place). It runs before
    // PlayerMover, so the direction it sets is used in the same step.
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(PlayerMover))]
    public class EnemyBrain : MonoBehaviour, ILevelPart
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
        [Tooltip("Fire a gun whenever the target is in sight, whatever the behaviour (a chaser with a gun). KeepDistanceAndShoot always fires; a melee weapon swings when the target is in reach.")]
        public bool alwaysFire;
        [Tooltip("Touch damage: hits the target when the centres are closer than this (tiles). 0 = none.")]
        [Min(0f)] public float touchRange = 0.9f;
        [Min(0)] public int touchDamage = 1;
        [Tooltip("Push on the target from a touch, tiles/s.")]
        [Min(0f)] public float touchKnockback = 10f;
        public int seed = 1;

        PlayerMover mover;
        Gun gun;
        PathWalker walker;
        EnemyGroup ownGroup;
        Health targetHealth;
        System.Random random;
        int step;
        Vector2 wanderDir;
        float strafeSide = 1f;

        // Set by a leader's EnemyGroup: follow it, in this slot.
        EnemyGroup group;
        int groupSlot;
        public void JoinGroup(EnemyGroup leader, int slot) { group = leader; groupSlot = slot; }

        void FixedUpdate()
        {
            if (mover == null)
            {
                mover = GetComponent<PlayerMover>();
                mover.readKeyboard = false;
                random = new System.Random(seed);
            }
            // Looked up every step, so add-ons added or swapped at runtime count (a boss phase).
            gun = GetComponent<Gun>();
            walker = GetComponent<PathWalker>();
            ownGroup = GetComponent<EnemyGroup>();
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
            bool inGroup = group != null && group.isActiveAndEnabled;
            bool breakOff = sees && group != null && group.breakRange > 0f && dist <= group.breakRange;
            if (inGroup && !breakOff)
            {
                // Follow the leader: head for the place, slowing inside the last tile.
                Vector2 place = group.Place(groupSlot);
                Vector2 off = place - (Vector2)GridStep.StandPoint(GetComponent<Rigidbody2D>());
                float d = off.magnitude;
                if (d > 1f) move = Toward(place);
                else if (d > 0.1f) move = off;
            }
            else switch (behaviour)
            {
                case EnemyBehaviour.Chase:
                    if (sees) move = Toward(target.position);
                    break;
                case EnemyBehaviour.KeepDistanceAndShoot:
                    if (sees)
                    {
                        Vector2 toward = to / Mathf.Max(dist, 1e-4f);
                        if (turn) strafeSide = random.NextDouble() < 0.5 ? -1f : 1f;
                        // With paths, a wall between it and the target means "go round", not "shoot the wall".
                        bool blocked = walker != null && !PathWalker.ShotClear(me, target.position);
                        if (dist > keepDistance + 1f || blocked) move = Toward(target.position);
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
            if (ownGroup != null && ownGroup.HasFollowers) move *= ownGroup.leaderPace;
            mover.SetMoveDirection(move);

            if (gun != null)
            {
                if (sees) gun.SetAim(to);
                gun.SetTrigger(WantsTrigger(gun.Current, sees, dist));
            }

            if (sees && touchRange > 0f && touchDamage > 0 && dist < touchRange && targetHealth != null)
                targetHealth.Hit(touchDamage, Knockback.Away(me, target.position, touchKnockback), HitSource.Touch);
        }

        // Straight at a point, or round the walls with a PathWalker.
        Vector2 Toward(Vector2 point)
        {
            if (walker != null) return walker.Steer(point);
            Vector2 d = point - (Vector2)transform.position;
            return d.sqrMagnitude < 1e-6f ? Vector2.zero : d.normalized;
        }

        // Whether the trigger is held this step. Guns fire while shooting is on; melee swings
        // when the target is in reach; a charge weapon is held to full charge, then let go.
        bool WantsTrigger(Weapon w, bool sees, float dist)
        {
            if (w == null || !sees) return false;
            bool fire = w.kind == WeaponKind.Melee
                ? dist <= w.melee.reach + 0.5f
                : behaviour == EnemyBehaviour.KeepDistanceAndShoot || alwaysFire;
            if (!fire) return false;
            if (w.chargeTime > 0f && gun.Charge01 >= 1f) return false;   // release: the shot goes now
            return true;
        }

        // In a generated room: chase the level's player, own random seed.
        public void PlacedInLevel(Transform player, int levelSeed)
        {
            if (target == null) target = player;
            seed = levelSeed;
        }
    }
}

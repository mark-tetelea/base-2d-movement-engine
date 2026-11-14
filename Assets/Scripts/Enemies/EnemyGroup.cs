using UnityEngine;

namespace TopDownKit
{
    public enum GroupShape { Line, Column, Wedge, Ring }

    // Add-on on a leader enemy: its followers move with it, each holding a place in a
    // shape around it, `spacing` tiles apart. The leader does whatever its own EnemyBrain
    // says, a little slower (leaderPace) so the followers keep up. A follower breaks off
    // and fights by its own behaviour while the target is within `breakRange` of it, and
    // goes back to its place when the target is farther. When the leader dies (this
    // component goes with it) every follower is on its own.
    [RequireComponent(typeof(EnemyBrain))]
    public class EnemyGroup : MonoBehaviour
    {
        [Tooltip("The enemies that follow this one. Each needs an EnemyBrain.")]
        public EnemyBrain[] followers;
        public GroupShape shape = GroupShape.Wedge;
        [Tooltip("Tiles between neighbours in the shape.")]
        [Min(0.5f)] public float spacing = 1.5f;
        [Tooltip("On: the shape turns to face the way the leader walks. Off: it keeps its world layout (followers behind = to the left).")]
        public bool turnWithLeader = true;
        [Tooltip("Tiles: a follower this close to the target fights on its own. 0 = never breaks off.")]
        [Min(0f)] public float breakRange = 4f;
        [Tooltip("The leader's speed x this while followers live, so they can keep up (1 = full speed).")]
        [Range(0.1f, 1f)] public float leaderPace = 0.8f;

        PlayerMover mover;
        Vector2 heading = Vector2.right;

        void Start()
        {
            if (followers == null) return;
            for (int i = 0; i < followers.Length; i++)
                if (followers[i] != null) followers[i].JoinGroup(this, i);
        }

        // Any follower still alive.
        public bool HasFollowers
        {
            get
            {
                if (followers == null) return false;
                foreach (EnemyBrain f in followers) if (f != null && f.isActiveAndEnabled) return true;
                return false;
            }
        }

        void FixedUpdate()
        {
            if (mover == null) mover = GetComponent<PlayerMover>();
            Vector2 v = mover.MoveVelocity;
            if (v.sqrMagnitude > 0.04f) heading = v.normalized;
        }

        // Where follower `slot` should stand now (world, tiles).
        public Vector2 Place(int slot)
        {
            Vector2 local = Offset(slot);
            Vector2 forward = turnWithLeader ? heading : Vector2.right;
            Vector2 side = new Vector2(-forward.y, forward.x);
            Vector2 at = mover != null ? GridStep.StandPoint(mover.GetComponent<Rigidbody2D>()) : (Vector2)transform.position;
            return at + forward * local.x + side * local.y;
        }

        // The slot's place in the leader's frame: x forward, y to the left.
        Vector2 Offset(int slot)
        {
            int n = followers == null ? 1 : Mathf.Max(1, followers.Length);
            int rank = slot / 2 + 1;
            float sideSign = slot % 2 == 0 ? 1f : -1f;
            switch (shape)
            {
                case GroupShape.Line:   // side by side with the leader
                    return new Vector2(0f, sideSign * rank * spacing);
                case GroupShape.Column: // one behind the other
                    return new Vector2(-(slot + 1) * spacing, 0f);
                case GroupShape.Ring:
                    float a = (slot + 0.5f) / n * Mathf.PI * 2f;
                    return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * spacing;
                default:                // Wedge: a V behind the leader
                    return new Vector2(-rank * spacing, sideSign * rank * spacing);
            }
        }
    }
}

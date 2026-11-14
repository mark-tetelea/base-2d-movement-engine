using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Add-on: the mover walks around walls, blue ground and closed doors instead of
    // pushing into them. Whoever steers it (EnemyBrain, a group slot) asks Steer(goal)
    // for a move direction each fixed step. When the way to the goal is clear it walks
    // straight; otherwise it follows a GridPath, planned again every `replanEvery`
    // seconds. Free movement looks ahead along the path and cuts across where the way is
    // clear; a grid-step walker (its feel has gridStep) gets the direction to the next
    // tile, four or eight ways as its feel says. No path within `giveUpDistance`: it walks
    // straight at the goal, as an enemy without this add-on does.
    [RequireComponent(typeof(PlayerMover))]
    public class PathWalker : MonoBehaviour
    {
        [Tooltip("Seconds between two path searches (the target keeps moving).")]
        [Min(0.05f)] public float replanEvery = 0.5f;
        [Tooltip("Tiles: the longest path it looks for. Farther, or no path: it walks straight at the goal.")]
        [Min(1f)] public float giveUpDistance = 30f;
        [Tooltip("Ground that hurts or kills (red, a trail) counts as a wall for paths. The goal itself is always allowed.")]
        public bool avoidHurtingGround = true;

        PlayerMover mover;
        Rigidbody2D body;
        float radius = 0.4f;
        int replanLeft;
        int next;
        bool gaveUp;
        Vector2Int plannedGoal;
        readonly List<Vector2Int> path = new List<Vector2Int>();
        static readonly RaycastHit2D[] hits = new RaycastHit2D[16];

        // The planned tiles (for a gizmo or a check) and whether the last search failed.
        public IReadOnlyList<Vector2Int> Path => path;
        public bool GaveUp => gaveUp;

        void Init()
        {
            if (mover != null) return;
            mover = GetComponent<PlayerMover>();
            body = GetComponent<Rigidbody2D>();
            Collider2D c = GetComponent<Collider2D>();
            if (c is CircleCollider2D circle) radius = circle.radius * Mathf.Abs(transform.lossyScale.x);
            else if (c is BoxCollider2D box) radius = 0.5f * Mathf.Max(box.size.x * Mathf.Abs(transform.lossyScale.x), box.size.y * Mathf.Abs(transform.lossyScale.y));
        }

        // Body size for path tiles: a little wider than the body, never under 0.8 tiles.
        float Size => Mathf.Max(0.8f, radius * 2f + 0.05f);

        // A move direction (length 1, or zero when there) toward `goal`. Call once per fixed step.
        public Vector2 Steer(Vector2 goal)
        {
            Init();
            Vector2 at = GridStep.StandPoint(body);
            Vector2 straight = goal - at;
            if (straight.sqrMagnitude < 1e-6f) return Vector2.zero;
            MoveSettings s = mover.CurrentSettings;
            bool grid = s.gridStep;
            if (replanLeft > 0) replanLeft--;

            if (!grid && WayClear(at, goal)) { path.Clear(); next = 0; gaveUp = false; return straight.normalized; }

            Vector2Int goalCell = GridPath.Cell(goal);
            bool finished = next >= path.Count;
            if (replanLeft <= 0 || (finished && !gaveUp) || (finished && goalCell != plannedGoal))
            {
                replanLeft = Mathf.Max(1, Mathf.RoundToInt(replanEvery / Time.fixedDeltaTime));
                plannedGoal = goalCell;
                gaveUp = !GridPath.Find(GridPath.Cell(at), goalCell, Size, avoidHurtingGround, !grid || s.eightDirections, giveUpDistance, path);
                next = 0;
            }
            if (gaveUp) return straight.normalized;

            // Drop the tiles already reached.
            while (next < path.Count && Reached(at, path[next], grid)) next++;
            if (next >= path.Count) return grid ? GridDirection(at, goalCell) : straight.normalized;

            if (grid) return GridDirection(at, path[next]);
            // Free movement: head for the farthest of the next few tiles it can walk to straight.
            Vector2 aim = GridPath.Centre(path[next]);
            for (int k = Mathf.Min(next + 3, path.Count - 1); k > next; k--)
            {
                Vector2 c = GridPath.Centre(path[k]);
                if (WayClear(at, c)) { aim = c; break; }
            }
            Vector2 d = aim - at;
            return d.sqrMagnitude < 1e-6f ? Vector2.zero : d.normalized;
        }

        static bool Reached(Vector2 at, Vector2Int cell, bool grid)
        {
            float r = grid ? 0.05f : 0.2f;
            return (at - GridPath.Centre(cell)).sqrMagnitude < r * r;
        }

        // A grid walker's step toward a tile next to it (each part -1, 0 or 1).
        static Vector2 GridDirection(Vector2 at, Vector2Int cell)
        {
            Vector2Int from = GridPath.Cell(at);
            return new Vector2(Mathf.Clamp(cell.x - from.x, -1, 1), Mathf.Clamp(cell.y - from.y, -1, 1));
        }

        // The body can walk from a to b in a straight line: nothing standing still in the
        // way, and (if avoided) no hurting ground crossed after its own tile.
        public bool WayClear(Vector2 a, Vector2 b)
        {
            Init();
            Vector2 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return true;
            ContactFilter2D filter = default;
            filter.useTriggers = false;
            int n = Physics2D.CircleCast(a, radius + 0.02f, d / len, filter, hits, len);
            for (int i = 0; i < n; i++) if (GridPath.Obstacle(hits[i].collider)) return false;
            if (avoidHurtingGround)
            {
                Vector2Int start = GridPath.Cell(a), end = GridPath.Cell(b);
                int samples = Mathf.CeilToInt(len / 0.25f);
                for (int i = 1; i <= samples; i++)
                {
                    Vector2Int c = GridPath.Cell(a + d * (i / (float)samples));
                    if (c != start && c != end && GridPath.Hurts(c)) return false;
                }
            }
            return true;
        }

        // A bullet from here would reach the point: no wall in between (low barriers that
        // bullets fly over, ShootOver, do not count). For shooters that want a clear shot.
        public static bool ShotClear(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            float len = d.magnitude;
            if (len < 1e-4f) return true;
            ContactFilter2D filter = default;
            filter.useTriggers = false;
            int n = Physics2D.Raycast(from, d / len, filter, hits, len);
            for (int i = 0; i < n; i++)
                if (GridPath.Obstacle(hits[i].collider) && hits[i].collider.GetComponent<ShootOver>() == null) return false;
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.9f);
            for (int i = Mathf.Max(0, next); i < path.Count; i++)
            {
                Vector2 c = GridPath.Centre(path[i]);
                Gizmos.DrawWireCube(c, new Vector3(0.3f, 0.3f, 0f));
                if (i > 0) Gizmos.DrawLine(GridPath.Centre(path[i - 1]), c);
            }
        }
    }
}

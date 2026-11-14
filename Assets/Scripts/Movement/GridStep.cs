using UnityEngine;

namespace TopDownKit
{
    // The one grid-step rule: one whole tile per step, landing on tile centres. PlayerMover
    // calls it when its feel has gridStep on; tests call the same functions. Tile centres
    // sit at whole numbers + 0.5 (the kit's tilemaps: a Grid at the origin, 1-tile cells).
    // What blocks a step is whatever blocks free walking - a solid collider in the target
    // tile - so free movement and grid steps obey the same walls and ground tiles.
    public static class GridStep
    {
        // The point of a mover that stands on tiles and lands on tile centres: the centre
        // of the collider on its body object (the pivot is at the feet; the collider sits
        // above it). Without a collider, the body's position.
        public static Vector2 StandPoint(Rigidbody2D body)
        {
            if (body == null) return Vector2.zero;
            Collider2D c = body.GetComponent<Collider2D>();
            return body.position + (c != null ? c.offset : Vector2.zero);
        }

        public static Vector2 Centre(Vector2 p) => new Vector2(Mathf.Floor(p.x) + 0.5f, Mathf.Floor(p.y) + 0.5f);

        // Within a thousandth of a tile of a centre.
        public static bool OnCentre(Vector2 p) => (p - Centre(p)).sqrMagnitude < 1e-6f;

        // The step (each part -1, 0 or 1) a move direction asks for. Eight: the nearest of
        // 8 directions. Four: the longer axis; a keyboard diagonal (a tie) goes sideways
        // first, and Candidates offers the other axis if that is blocked.
        public static Vector2Int Direction(Vector2 input, bool eight)
        {
            if (input.sqrMagnitude < 1e-6f) return Vector2Int.zero;
            if (eight)
            {
                int k = Mathf.RoundToInt(Mathf.Atan2(input.y, input.x) / (Mathf.PI / 4f));
                float a = k * Mathf.PI / 4f;
                return new Vector2Int(Mathf.RoundToInt(Mathf.Cos(a)), Mathf.RoundToInt(Mathf.Sin(a)));
            }
            float ax = Mathf.Abs(input.x), ay = Mathf.Abs(input.y);
            if (ay > ax + 1e-4f) return new Vector2Int(0, (int)Mathf.Sign(input.y));
            return new Vector2Int((int)Mathf.Sign(input.x), 0);
        }

        // The steps to try, in order, for a move direction: the step it asks for, then
        // (when that is a diagonal, or a 4-way tie) each single axis the input also holds.
        // Fills `into` (size 3) and returns how many.
        public static int Candidates(Vector2 input, bool eight, Vector2Int[] into)
        {
            Vector2Int d = Direction(input, eight);
            if (d == Vector2Int.zero) return 0;
            int n = 0;
            into[n++] = d;
            bool tie = Mathf.Abs(Mathf.Abs(input.x) - Mathf.Abs(input.y)) <= 1e-4f;
            if (d.x != 0 && d.y != 0)
            {
                into[n++] = new Vector2Int(d.x, 0);
                into[n++] = new Vector2Int(0, d.y);
            }
            else if (!eight && tie) into[n++] = d.x != 0 ? new Vector2Int(0, (int)Mathf.Sign(input.y)) : new Vector2Int((int)Mathf.Sign(input.x), 0);
            return n;
        }

        // Fixed steps one tile takes at `speed` tiles per second (at least 1): the pace
        // while walking. 0 speed: never moves.
        public static int StepsPerTile(float speed, float dt) => speed <= 0f ? 0 : Mathf.Max(1, Mathf.RoundToInt(1f / (speed * dt)));

        // Fixed steps the slide between tiles takes: stepTime in whole steps, never longer
        // than the pace. 0 = a jump.
        public static int SlideSteps(float stepTime, int stepsPerTile, float dt) =>
            Mathf.Clamp(Mathf.RoundToInt(stepTime / dt), 0, stepsPerTile);

        // The velocity that covers from -> to in exactly `slideSteps` fixed steps.
        public static Vector2 SlideVelocity(Vector2 from, Vector2 to, int slideSteps, float dt) =>
            slideSteps <= 0 ? Vector2.zero : (to - from) / (slideSteps * dt);

        static readonly Collider2D[] found = new Collider2D[8];

        // Whether a tile centre is blocked for `self`: any solid (non-trigger) collider in
        // the tile, other than self's own - a wall, a blocking ground tile, another mover.
        public static bool Blocked(Vector2 centre, Rigidbody2D self)
        {
            ContactFilter2D filter = default;
            filter.useTriggers = false;
            int n = Physics2D.OverlapBox(centre, new Vector2(0.8f, 0.8f), 0f, filter, found);
            for (int i = 0; i < n; i++)
                if (found[i].attachedRigidbody != self || self == null) return true;
            return false;
        }

        // Whether a step from `from` along `step` is allowed: the target tile is free and,
        // for a diagonal, both tiles beside the corner are too (no cutting corners).
        public static bool CanStep(Vector2 from, Vector2Int step, Rigidbody2D self)
        {
            if (Blocked(from + (Vector2)step, self)) return false;
            if (step.x != 0 && step.y != 0)
                return !Blocked(from + new Vector2(step.x, 0f), self) && !Blocked(from + new Vector2(0f, step.y), self);
            return true;
        }

        // The free tile centre nearest to p (p's own tile first, then its 8 neighbours by
        // distance). Returns false if all nine are blocked.
        public static bool NearestFree(Vector2 p, Rigidbody2D self, out Vector2 centre)
        {
            Vector2 c0 = Centre(p);
            centre = c0;
            if (!Blocked(c0, self)) return true;
            float best = float.MaxValue;
            bool any = false;
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                {
                    if (x == 0 && y == 0) continue;
                    Vector2 c = c0 + new Vector2(x, y);
                    float d = (c - p).sqrMagnitude;
                    if (d < best && !Blocked(c, self)) { best = d; centre = c; any = true; }
                }
            return any;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // The one path rule (CLAUDE.md, one copy of each core rule): an A* search over the
    // tile grid. Tile (x, y) covers x..x+1, y..y+1, its centre at x + 0.5 (the kit's
    // tilemaps: a Grid at the origin, 1-tile cells).
    //
    // A tile is open when nothing that stands still blocks a body of the walker's size at
    // its centre: a wall, a blocking ground tile (GroundMap's runtime colliders, so blue
    // and closed door ground count), a door object, a chest. Movers (the player, other
    // enemies) never block a path; they are pushed past. Optionally, ground that hurts
    // (red, a trail) counts as closed too. No navmesh: the kit's levels are tile grids,
    // and the same check serves free movement and grid steps.
    public static class GridPath
    {
        public static Vector2Int Cell(Vector2 p) => new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));
        public static Vector2 Centre(Vector2Int c) => new Vector2(c.x + 0.5f, c.y + 0.5f);

        // Something that stands still and stops walkers: a solid collider with no body or
        // a static one (walls, ground blocks, doors, chests). Movers have dynamic bodies.
        public static bool Obstacle(Collider2D c) =>
            c != null && !c.isTrigger && (c.attachedRigidbody == null || c.attachedRigidbody.bodyType == RigidbodyType2D.Static);

        // ---- Open tiles, looked up once per fixed step ----------------------------------

        static readonly Dictionary<(Vector2Int, int), bool> open = new Dictionary<(Vector2Int, int), bool>();
        static float openTime = -1f;
        static readonly Collider2D[] found = new Collider2D[16];

        // Whether a body `size` tiles across can stand on the tile's centre. The answer is
        // kept for the rest of this fixed step (doors and crumbling change between steps).
        public static bool Open(Vector2Int cell, float size, bool avoidHurting)
        {
            if (Time.fixedTime != openTime) { open.Clear(); openTime = Time.fixedTime; }
            int key = Mathf.RoundToInt(size * 8f) * 2 + (avoidHurting ? 1 : 0);
            if (open.TryGetValue((cell, key), out bool ok)) return ok;
            ok = !Blocked(Centre(cell), size) && !(avoidHurting && Hurts(cell));
            open[(cell, key)] = ok;
            return ok;
        }

        static bool Blocked(Vector2 centre, float size)
        {
            ContactFilter2D filter = default;
            filter.useTriggers = false;
            int n = Physics2D.OverlapBox(centre, new Vector2(size, size), 0f, filter, found);
            for (int i = 0; i < n; i++) if (Obstacle(found[i])) return true;
            return false;
        }

        // Ground on this tile hurts or kills (asked through GroundCheck, which the Tiles
        // folder's GroundMap answers - so paths need no Tiles folder).
        public static bool Hurts(Vector2Int cell) => GroundCheck.HurtsAt(Centre(cell));

        // ---- The search -----------------------------------------------------------------

        // Costs in tenths of a tile: 10 straight, 14 diagonal.
        const int Straight = 10, Diagonal = 14;
        const int MaxNodes = 6000;
        static readonly Vector2Int[] steps8 =
        {
            new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(-1, -1), new Vector2Int(1, -1),
        };
        static readonly Dictionary<Vector2Int, int> index = new Dictionary<Vector2Int, int>();
        static readonly List<Vector2Int> cells = new List<Vector2Int>();
        static readonly List<int> cost = new List<int>();
        static readonly List<int> parent = new List<int>();
        static readonly List<bool> done = new List<bool>();
        static readonly List<int> heap = new List<int>();
        static readonly List<int> heapF = new List<int>();

        static int Guess(Vector2Int a, Vector2Int b, bool eight)
        {
            int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
            if (!eight) return Straight * (dx + dy);
            return Straight * Mathf.Max(dx, dy) + (Diagonal - Straight) * Mathf.Min(dx, dy);
        }

        // The shortest path of open tiles from `start` to `goal`, written into `path`
        // (the tiles after start, goal last). Eight: diagonal steps allowed, never cutting
        // a corner (both tiles beside it must be open too); four: straight steps only (a
        // 4-way grid-step walker). The start and the goal always count as open (the walker
        // is already there; the target may stand by a wall or on red). Gives up - returns
        // false - when no path of at most `maxTiles` tiles exists.
        public static bool Find(Vector2Int start, Vector2Int goal, float size, bool avoidHurting, bool eight, float maxTiles, List<Vector2Int> path)
        {
            path.Clear();
            if (start == goal) return true;
            int maxCost = Mathf.RoundToInt(maxTiles * Straight);
            if (Guess(start, goal, eight) > maxCost) return false;
            index.Clear(); cells.Clear(); cost.Clear(); parent.Clear(); done.Clear(); heap.Clear(); heapF.Clear();
            Add(start, 0, -1);
            Push(0, Guess(start, goal, eight));
            int dirs = eight ? 8 : 4;
            while (heap.Count > 0)
            {
                int n = Pop();
                if (done[n]) continue;
                done[n] = true;
                Vector2Int at = cells[n];
                if (at == goal)
                {
                    for (int i = n; i > 0; i = parent[i]) path.Add(cells[i]);
                    path.Reverse();
                    return true;
                }
                if (cells.Count > MaxNodes) return false;
                for (int d = 0; d < dirs; d++)
                {
                    Vector2Int s = steps8[d];
                    Vector2Int next = at + s;
                    bool diagonal = d >= 4;
                    int g = cost[n] + (diagonal ? Diagonal : Straight);
                    if (g > maxCost) continue;
                    bool known = index.TryGetValue(next, out int k);
                    if (known && (done[k] || cost[k] <= g)) continue;
                    if (next != goal && !Open(next, size, avoidHurting)) continue;
                    if (diagonal && !(Passable(new Vector2Int(at.x + s.x, at.y), goal, size, avoidHurting)
                                      && Passable(new Vector2Int(at.x, at.y + s.y), goal, size, avoidHurting))) continue;
                    if (known) { cost[k] = g; parent[k] = n; }
                    else { k = cells.Count; Add(next, g, n); }
                    Push(k, g + Guess(next, goal, eight));
                }
            }
            return false;
        }

        static bool Passable(Vector2Int c, Vector2Int goal, float size, bool avoidHurting) => c == goal || Open(c, size, avoidHurting);

        static void Add(Vector2Int c, int g, int from)
        {
            index[c] = cells.Count;
            cells.Add(c); cost.Add(g); parent.Add(from); done.Add(false);
        }

        // A binary min-heap of node indices by f = cost + guess.
        static void Push(int node, int f)
        {
            heap.Add(node); heapF.Add(f);
            int i = heap.Count - 1;
            while (i > 0)
            {
                int p = (i - 1) / 2;
                if (heapF[p] <= heapF[i]) break;
                Swap(i, p);
                i = p;
            }
        }

        static int Pop()
        {
            int top = heap[0];
            int last = heap.Count - 1;
            Swap(0, last);
            heap.RemoveAt(last); heapF.RemoveAt(last);
            int i = 0;
            while (true)
            {
                int l = 2 * i + 1, r = l + 1, m = i;
                if (l < heap.Count && heapF[l] < heapF[m]) m = l;
                if (r < heap.Count && heapF[r] < heapF[m]) m = r;
                if (m == i) break;
                Swap(i, m);
                i = m;
            }
            return top;
        }

        static void Swap(int a, int b)
        {
            (heap[a], heap[b]) = (heap[b], heap[a]);
            (heapF[a], heapF[b]) = (heapF[b], heapF[a]);
        }
    }
}

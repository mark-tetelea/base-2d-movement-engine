using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // The one layout rule for generated levels: plain data in, plain data out, so the
    // generator, editor checks and tests all use the same code.
    //
    // Pieces sit on a grid of cells. It places the start piece at (0,0), then grows a tree:
    // pick a random free door spot of a placed room, pick a random allowed piece with a
    // spot on the facing side, place it if all its cells are free, and join the two spots.
    // The end piece goes on last, on a free spot of the room farthest from the start (by
    // doors), so it is a dead end and the whole level lies between start and end. A tree
    // means every room is reachable from the start. If the room count cannot be reached
    // with this seed it tries again (derived seeds) before giving up.
    public static class Layout
    {
        public struct Shape
        {
            public Vector2Int cells;
            public DoorSpot[] doors;
            public Shape(Vector2Int cells, DoorSpot[] doors) { this.cells = cells; this.doors = doors; }
        }

        public struct Placed
        {
            public int shape;          // index into the shapes array
            public Vector2Int origin;  // bottom-left cell
            public int parent;         // the room it was attached to (-1 for the start)
            public int depth;          // doors from the start
        }

        // A door between rooms a and b: a's spot index and b's spot index.
        public struct Link { public int a, spotA, b, spotB; }

        public class Result
        {
            public readonly List<Placed> rooms = new List<Placed>();
            public readonly List<Link> links = new List<Link>();
            public int endRoom = -1;
            public int attempts;
        }

        public static Vector2Int Step(DoorSide s)
        {
            switch (s)
            {
                case DoorSide.North: return Vector2Int.up;
                case DoorSide.East: return Vector2Int.right;
                case DoorSide.South: return Vector2Int.down;
                default: return Vector2Int.left;
            }
        }

        public static DoorSide Opposite(DoorSide s) => (DoorSide)(((int)s + 2) % 4);

        // `roomCount` counts the start and end rooms (at least 2). `allowed` are the shapes
        // the middle rooms are picked from. Returns false only if no attempt worked.
        public static bool Generate(Shape[] shapes, int start, int end, int[] allowed, int roomCount, int seed, Result result, int maxAttempts = 50)
        {
            roomCount = Mathf.Max(2, roomCount);
            var random = new System.Random(seed);
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                result.attempts = attempt;
                if (TryOnce(shapes, start, end, allowed, roomCount, random, result)) return true;
            }
            result.rooms.Clear();
            result.links.Clear();
            result.endRoom = -1;
            return false;
        }

        static bool TryOnce(Shape[] shapes, int start, int end, int[] allowed, int roomCount, System.Random random, Result r)
        {
            r.rooms.Clear();
            r.links.Clear();
            r.endRoom = -1;
            var taken = new Dictionary<Vector2Int, int>();
            var open = new List<(int room, int spot)>();

            Place(shapes, start, Vector2Int.zero, -1, 0, r, taken, open);

            var order = new List<int>(allowed);
            int guard = 0;
            while (r.rooms.Count < roomCount - 1 && open.Count > 0 && guard++ < 10000)
            {
                int pick = random.Next(open.Count);
                (int room, int spot) slot = open[pick];
                Shuffle(order, random);
                if (!Attach(shapes, slot, order, random, r, taken, open)) open.RemoveAt(pick);
            }
            if (r.rooms.Count < roomCount - 1) return false;

            // The end room: the deepest rooms first, random among equals.
            var slots = new List<(int room, int spot)>(open);
            Shuffle(slots, random);
            slots.Sort((x, y) => r.rooms[y.room].depth.CompareTo(r.rooms[x.room].depth));
            var only = new List<int> { end };
            foreach (var slot in slots)
                if (Attach(shapes, slot, only, random, r, taken, open))
                {
                    r.endRoom = r.rooms.Count - 1;
                    return true;
                }
            return false;
        }

        // Tries the shapes in order on one free spot; places the first that fits.
        static bool Attach(Shape[] shapes, (int room, int spot) slot, List<int> candidates, System.Random random,
            Result r, Dictionary<Vector2Int, int> taken, List<(int room, int spot)> open)
        {
            Placed from = r.rooms[slot.room];
            DoorSpot d = shapes[from.shape].doors[slot.spot];
            Vector2Int outside = from.origin + d.cell + Step(d.side);
            if (taken.ContainsKey(outside)) return false;
            DoorSide need = Opposite(d.side);
            var spots = new List<int>();
            foreach (int s in candidates)
            {
                Shape shape = shapes[s];
                spots.Clear();
                for (int i = 0; i < shape.doors.Length; i++) if (shape.doors[i].side == need) spots.Add(i);
                Shuffle(spots, random);
                foreach (int i in spots)
                {
                    Vector2Int origin = outside - shape.doors[i].cell;
                    if (!Free(shape, origin, taken)) continue;
                    int index = Place(shapes, s, origin, slot.room, from.depth + 1, r, taken, open);
                    r.links.Add(new Link { a = slot.room, spotA = slot.spot, b = index, spotB = i });
                    open.RemoveAll(o => (o.room == slot.room && o.spot == slot.spot) || (o.room == index && o.spot == i));
                    return true;
                }
            }
            return false;
        }

        static bool Free(Shape shape, Vector2Int origin, Dictionary<Vector2Int, int> taken)
        {
            for (int x = 0; x < shape.cells.x; x++)
                for (int y = 0; y < shape.cells.y; y++)
                    if (taken.ContainsKey(origin + new Vector2Int(x, y))) return false;
            return true;
        }

        static int Place(Shape[] shapes, int s, Vector2Int origin, int parent, int depth, Result r,
            Dictionary<Vector2Int, int> taken, List<(int room, int spot)> open)
        {
            int index = r.rooms.Count;
            r.rooms.Add(new Placed { shape = s, origin = origin, parent = parent, depth = depth });
            Shape shape = shapes[s];
            for (int x = 0; x < shape.cells.x; x++)
                for (int y = 0; y < shape.cells.y; y++)
                    taken[origin + new Vector2Int(x, y)] = index;
            for (int i = 0; i < shape.doors.Length; i++) open.Add((index, i));
            return index;
        }

        static void Shuffle<T>(List<T> list, System.Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T t = list[i]; list[i] = list[j]; list[j] = t;
            }
        }

        // ---- Checks ---------------------------------------------------------------------

        // Rooms reachable from the start through the links (the finishable-level check).
        public static int Reachable(Result r)
        {
            if (r.rooms.Count == 0) return 0;
            var seen = new bool[r.rooms.Count];
            var queue = new Queue<int>();
            seen[0] = true;
            queue.Enqueue(0);
            int n = 1;
            while (queue.Count > 0)
            {
                int at = queue.Dequeue();
                foreach (Link l in r.links)
                {
                    int other = l.a == at ? l.b : l.b == at ? l.a : -1;
                    if (other < 0 || seen[other]) continue;
                    seen[other] = true;
                    n++;
                    queue.Enqueue(other);
                }
            }
            return n;
        }

        // Every link joins two facing spots on neighbouring cells, and no rooms overlap.
        // Returns an empty string when the layout is sound, else what is wrong.
        public static string Problems(Shape[] shapes, Result r)
        {
            var taken = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < r.rooms.Count; i++)
            {
                Placed p = r.rooms[i];
                Vector2Int c = shapes[p.shape].cells;
                for (int x = 0; x < c.x; x++)
                    for (int y = 0; y < c.y; y++)
                    {
                        Vector2Int cell = p.origin + new Vector2Int(x, y);
                        if (taken.TryGetValue(cell, out int other)) return "rooms " + other + " and " + i + " overlap at " + cell;
                        taken[cell] = i;
                    }
            }
            foreach (Link l in r.links)
            {
                DoorSpot a = shapes[r.rooms[l.a].shape].doors[l.spotA];
                DoorSpot b = shapes[r.rooms[l.b].shape].doors[l.spotB];
                Vector2Int ca = r.rooms[l.a].origin + a.cell, cb = r.rooms[l.b].origin + b.cell;
                if (b.side != Opposite(a.side) || ca + Step(a.side) != cb) return "link " + l.a + "-" + l.b + " does not line up";
            }
            return "";
        }
    }
}

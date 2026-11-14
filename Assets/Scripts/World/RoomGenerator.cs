using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TopDownKit
{
    // Builds a new level when the scene starts, from hand-made RoomPieces (prefabs). The
    // layout comes from the one rule (Layout.Generate); this only makes it real:
    //  - each piece is placed, its Floor and Walls tiles copied into the level's tilemaps
    //    (one wall collider, one GroundMap, so movement, bullets, paths and ground tiles
    //    work as in any hand-made level), and its own tilemaps removed;
    //  - each joined pair of door spots is cut through both walls. In a room with a
    //    RoomLock the opening is door ground (doorTile) with a TileDoor added to the lock,
    //    so it shuts while the room is locked; elsewhere it is open ground;
    //  - enemies, locks and boss bars inside the pieces are pointed at the player and HUD;
    //  - the player is moved to the start room.
    // Rooms are ordinary Rooms, so every camera layout (open, slide, fade) works.
    //
    // Runs first (Awake, early order) so GroundMap, RoomLock and the rest start on the
    // finished level.
    [DefaultExecutionOrder(-100)]
    public class RoomGenerator : MonoBehaviour
    {
        [Header("Pieces")]
        [Tooltip("The first room; the player starts here.")]
        public RoomPiece startPiece;
        [Tooltip("The last room (a boss room, an exit): always a dead end, as far from the start as the layout allows.")]
        public RoomPiece endPiece;
        [Tooltip("The pieces the rooms in between are picked from.")]
        public RoomPiece[] pieces = new RoomPiece[0];

        [Header("Layout")]
        [Tooltip("Rooms in all, start and end included.")]
        [Min(2)] public int roomCount = 7;
        [Tooltip("0 = a new layout every run; any other number = the same layout every time.")]
        public int seed;
        [Tooltip("Tiles a door is wide.")]
        [Min(1)] public int doorWidth = 3;

        [Header("Level")]
        [Tooltip("The level's floor tilemap (with a GroundMap, if ground tiles are used).")]
        public Tilemap floor;
        [Tooltip("The level's wall tilemap (with its collider).")]
        public Tilemap walls;
        [Tooltip("Door ground for doors of locking rooms (a GroundTile with isDoor). Empty = doors are plain floor and never shut.")]
        public GroundTile doorTile;
        [Tooltip("Moved to the start room; enemies and locks are pointed at it.")]
        public Transform player;
        [Tooltip("Given to boss bars in the pieces (optional).")]
        public Hud hud;
        [Tooltip("Build when the scene starts. Off: call Build() yourself.")]
        public bool buildOnAwake = true;

        // What was built: the seed used (to report or replay a layout), the rooms in order
        // of placement (start first), the end room.
        public int UsedSeed { get; private set; }
        public bool Built { get; private set; }
        public readonly List<RoomPiece> Rooms = new List<RoomPiece>();
        public RoomPiece StartRoom => Rooms.Count > 0 ? Rooms[0] : null;
        public RoomPiece EndRoom { get; private set; }

        void Awake() { if (buildOnAwake) Build(); }

        // The pieces as plain shapes for the layout rule: start, end, then the others.
        public RoomPiece[] AllPieces()
        {
            var list = new List<RoomPiece> { startPiece, endPiece };
            foreach (RoomPiece p in pieces) if (p != null && !list.Contains(p)) list.Add(p);
            return list.ToArray();
        }

        public static Layout.Shape[] Shapes(RoomPiece[] all)
        {
            var shapes = new Layout.Shape[all.Length];
            for (int i = 0; i < all.Length; i++) shapes[i] = new Layout.Shape(all[i].cells, all[i].doors);
            return shapes;
        }

        // The middle pieces' indexes into AllPieces().
        public int[] Allowed(RoomPiece[] all)
        {
            var list = new List<int>();
            foreach (RoomPiece p in pieces)
            {
                int i = System.Array.IndexOf(all, p);
                if (p != null && i >= 2 && !list.Contains(i)) list.Add(i);
            }
            return list.ToArray();
        }

        public static int PickSeed(int seed) => seed != 0 ? seed : new System.Random().Next(1, int.MaxValue);

        public bool Build()
        {
            if (Built) return true;
            if (startPiece == null || endPiece == null || floor == null || walls == null)
            {
                Debug.LogError("RoomGenerator: start piece, end piece, floor and walls are needed.", this);
                return false;
            }
            RoomPiece[] all = AllPieces();
            Vector2Int cellTiles = startPiece.cellTiles;
            foreach (RoomPiece p in all)
                if (p.cellTiles != cellTiles) { Debug.LogError("RoomGenerator: " + p.name + " has cell size " + p.cellTiles + ", the start piece " + cellTiles + ".", this); return false; }

            UsedSeed = PickSeed(seed);
            var layout = new Layout.Result();
            if (!Layout.Generate(Shapes(all), 0, 1, Allowed(all), roomCount, UsedSeed, layout))
            {
                Debug.LogError("RoomGenerator: no layout of " + roomCount + " rooms with these pieces (seed " + UsedSeed + ").", this);
                return false;
            }

            var random = new System.Random(UsedSeed);
            Rooms.Clear();
            foreach (Layout.Placed p in layout.rooms)
                Rooms.Add(Make(all[p.shape], p.origin, cellTiles, Rooms.Count, random));
            EndRoom = Rooms[layout.endRoom];

            foreach (Layout.Link l in layout.links)
            {
                Cut(Rooms[l.a], Rooms[l.a].doors[l.spotA]);
                Cut(Rooms[l.b], Rooms[l.b].doors[l.spotB]);
            }

            if (player != null)
            {
                RoomPiece s = Rooms[0];
                Vector2 at = s.playerStart != null ? (Vector2)s.playerStart.position : (Vector2)s.transform.position;
                player.position = new Vector3(at.x, at.y, player.position.z);
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                if (body != null) body.position = at;
            }
            Built = true;
            return true;
        }

        RoomPiece Make(RoomPiece prefab, Vector2Int origin, Vector2Int cellTiles, int number, System.Random random)
        {
            Vector2 corner = new Vector2(origin.x * cellTiles.x, origin.y * cellTiles.y);
            Vector2 centre = corner + (Vector2)prefab.SizeTiles * 0.5f;
            RoomPiece room = Instantiate(prefab, new Vector3(centre.x, centre.y, 0f), Quaternion.identity, transform);
            room.name = "Room " + number + " (" + prefab.name + ")";
            room.GetComponent<Room>().size = room.SizeTiles;

            Copy(room.floor, floor);
            Copy(room.walls, walls);
            foreach (Tilemap t in new[] { room.floor, room.walls })
                if (t != null) { t.gameObject.SetActive(false); Destroy(t.gameObject); }

            // Point the piece's contents at this level (waves are switched off, so search those too).
            foreach (RoomLock l in room.GetComponentsInChildren<RoomLock>(true)) if (l.player == null) l.player = player;
            foreach (EnemyBrain b in room.GetComponentsInChildren<EnemyBrain>(true))
            {
                if (b.target == null) b.target = player;
                b.seed = random.Next(1, int.MaxValue);
            }
            foreach (Gun g in room.GetComponentsInChildren<Gun>(true)) g.seed = random.Next(1, int.MaxValue);
            foreach (Drops d in room.GetComponentsInChildren<Drops>(true)) d.seed = random.Next(1, int.MaxValue);
            if (hud != null) foreach (BossPhases b in room.GetComponentsInChildren<BossPhases>(true)) if (b.hud == null) b.hud = hud;
            return room;
        }

        // Copies a piece's tiles into the level at the same world place.
        void Copy(Tilemap from, Tilemap to)
        {
            if (from == null) return;
            from.CompressBounds();
            foreach (Vector3Int c in from.cellBounds.allPositionsWithin)
            {
                TileBase t = from.GetTile(c);
                if (t == null) continue;
                Vector3 world = from.CellToWorld(c) + from.layoutGrid.cellSize * 0.5f;
                to.SetTile(to.WorldToCell(world), t);
            }
        }

        // Cuts one room's half of a door: its own wall tiles at the spot.
        void Cut(RoomPiece room, DoorSpot spot)
        {
            Vector2Int size = room.SizeTiles;
            Vector2 corner = (Vector2)room.transform.position - (Vector2)size * 0.5f;
            Vector2Int[] cells = RoomPiece.DoorCells(spot, room.cellTiles, size, doorWidth);
            RoomLock roomLock = room.GetComponent<RoomLock>();
            bool shuts = roomLock != null && doorTile != null;

            // Plain ground for rooms that never lock: the floor just inside the door.
            Vector2Int inward = -Layout.Step(spot.side);
            TileBase plain = floor.GetTile(floor.WorldToCell(corner + cells[0] + inward + Vector2.one * 0.5f));
            foreach (Vector2Int c in cells)
            {
                Vector3Int cell = floor.WorldToCell(corner + c + Vector2.one * 0.5f);
                walls.SetTile(walls.WorldToCell(corner + c + Vector2.one * 0.5f), null);
                floor.SetTile(cell, shuts ? doorTile : plain);
            }
            if (!shuts) return;

            Vector2Int first = cells[0];
            var go = new GameObject("Door " + spot.side + " " + spot.cell);
            go.transform.SetParent(room.transform, false);
            go.transform.position = corner + first + Vector2.one * 0.5f;
            TileDoor door = go.AddComponent<TileDoor>();
            bool across = spot.side == DoorSide.North || spot.side == DoorSide.South;
            door.size = across ? new Vector2Int(doorWidth, 1) : new Vector2Int(1, doorWidth);
            var list = new List<GameObject>();
            if (roomLock.doors != null) list.AddRange(roomLock.doors);
            list.Add(go);
            roomLock.doors = list.ToArray();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TopDownKit
{
    public enum DoorSide { North, East, South, West }

    // A spot on a piece's outer wall where a door may be cut: which side, and which cell
    // of the piece (pieces bigger than one cell have one possible door per cell edge).
    // The door is cut in the middle of that cell's edge, so two pieces always line up.
    [Serializable]
    public struct DoorSpot
    {
        public DoorSide side;
        [Tooltip("Which cell of the piece, counted right and up from its bottom-left cell.")]
        public Vector2Int cell;

        public DoorSpot(DoorSide side, Vector2Int cell) { this.side = side; this.cell = cell; }
    }

    // A hand-made room piece for the RoomGenerator: a prefab with a Room, its own small
    // Floor and Walls tilemaps (painted closed, cell (0,0) at the piece's bottom-left
    // corner) and a list of door spots. Anything else in it - enemies in waves, crates, a
    // RoomLock, a boss - comes along as it is. A new piece is a new prefab; no code.
    //
    // The piece covers `cells` grid cells of `cellTiles` tiles each. Every piece a
    // generator uses must have the same cellTiles.
    [RequireComponent(typeof(Room))]
    public class RoomPiece : MonoBehaviour
    {
        [Tooltip("Size in grid cells (1x1 = one screen-sized room, 2x1 = twice as wide).")]
        public Vector2Int cells = Vector2Int.one;
        [Tooltip("Tiles in one grid cell. The same for every piece of a generator.")]
        public Vector2Int cellTiles = new Vector2Int(40, 22);
        [Tooltip("The piece's ground; copied into the level's floor tilemap.")]
        public Tilemap floor;
        [Tooltip("The piece's walls, closed all round; copied into the level's wall tilemap. Door spots are cut out of it.")]
        public Tilemap walls;
        [Tooltip("Where a door may be cut. Unused spots stay wall.")]
        public DoorSpot[] doors = new DoorSpot[0];
        [Tooltip("Where the player starts, if this is the start piece (empty = the middle).")]
        public Transform playerStart;

        public Vector2Int SizeTiles => new Vector2Int(cells.x * cellTiles.x, cells.y * cellTiles.y);

        // The tiles (relative to the piece's bottom-left) a door at this spot cuts out of
        // the piece's own wall, `width` tiles wide. The neighbour cuts its own wall the same
        // way, so the opening goes through both.
        public static Vector2Int[] DoorCells(DoorSpot d, Vector2Int cellTiles, Vector2Int sizeTiles, int width)
        {
            var result = new Vector2Int[width];
            int x0 = d.cell.x * cellTiles.x, y0 = d.cell.y * cellTiles.y;
            int alongX = x0 + cellTiles.x / 2 - width / 2;
            int alongY = y0 + cellTiles.y / 2 - width / 2;
            for (int i = 0; i < width; i++)
            {
                switch (d.side)
                {
                    case DoorSide.North: result[i] = new Vector2Int(alongX + i, sizeTiles.y - 1); break;
                    case DoorSide.South: result[i] = new Vector2Int(alongX + i, 0); break;
                    case DoorSide.East: result[i] = new Vector2Int(sizeTiles.x - 1, alongY + i); break;
                    default: result[i] = new Vector2Int(0, alongY + i); break;
                }
            }
            return result;
        }

        void OnValidate()
        {
            Room room = GetComponent<Room>();
            if (room != null) room.size = SizeTiles;
        }

        void OnDrawGizmos()
        {
            if (doors == null) return;
            Vector2 corner = (Vector2)transform.position - (Vector2)SizeTiles * 0.5f;
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            foreach (DoorSpot d in doors)
                foreach (Vector2Int c in DoorCells(d, cellTiles, SizeTiles, 3))
                    Gizmos.DrawWireCube(corner + c + Vector2.one * 0.5f, Vector3.one * 0.9f);
        }
    }
}

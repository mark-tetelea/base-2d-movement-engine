using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // A room is a marked rectangle in the scene (centre = this object, size in tiles).
    // The same level plays open or in rooms by the camera's layout setting; rooms only
    // matter to the room layouts and to add-ons like RoomLock.
    public class Room : MonoBehaviour
    {
        [Tooltip("Width and height in tiles, centred on this object.")]
        public Vector2 size = new Vector2(40f, 22.5f);

        static readonly List<Room> all = new List<Room>();
        public static IReadOnlyList<Room> All => all;

        // The player has been inside (set by whoever tracks it - the minimap; kept by saves).
        public bool Visited { get; set; }

        public Rect Bounds => new Rect((Vector2)transform.position - size * 0.5f, size);

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() { all.Remove(this); }

        // The room holding the point, or null. Rooms should not overlap; if they do,
        // the first one found wins.
        public static Room At(Vector2 point)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i].Bounds.Contains(point)) return all[i];
            return null;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}

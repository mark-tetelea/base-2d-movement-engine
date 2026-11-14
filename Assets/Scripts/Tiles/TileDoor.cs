using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Door ground: while this object is active, the door tiles (GroundTile.isDoor) under
    // it are closed; switched off, they open. It is a plain object switched on and off,
    // so the kit's existing switches drive it with no second door system: put it in a
    // RoomLock's doors (closed until the room is cleared), a Lever's offWhenOn / onWhenOn,
    // or a LockedDoor's blockers (opens with a key).
    public class TileDoor : MonoBehaviour
    {
        [Tooltip("Tiles covered, counted right and up from the tile this object stands in.")]
        public Vector2Int size = Vector2Int.one;

        static readonly List<TileDoor> all = new List<TileDoor>();
        public static IReadOnlyList<TileDoor> All => all;

        void OnEnable() { if (!all.Contains(this)) all.Add(this); GroundMap.DoorsChanged(); }
        void OnDisable() { all.Remove(this); GroundMap.DoorsChanged(); }

        void OnDrawGizmos()
        {
            Vector3 o = new Vector3(Mathf.Floor(transform.position.x), Mathf.Floor(transform.position.y), 0f);
            Gizmos.color = new Color(0.9f, 0.4f, 1f, 0.8f);
            Gizmos.DrawWireCube(o + new Vector3(size.x, size.y, 0f) * 0.5f, new Vector3(size.x, size.y, 0f));
        }
    }
}

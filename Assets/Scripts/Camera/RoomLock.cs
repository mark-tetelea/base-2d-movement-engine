using UnityEngine;

namespace TopDownKit
{
    // Add-on: the room's doors shut once the player is inside and stay shut until
    // Clear() is called (Isaac style). There are no enemies yet; whatever decides the
    // room is cleared just calls Clear(). Doors are plain objects (a solid collider and
    // its art) switched on and off.
    [RequireComponent(typeof(Room))]
    public class RoomLock : MonoBehaviour
    {
        [Tooltip("Who shuts the doors by walking in.")]
        public Transform player;
        [Tooltip("Door blockers, switched on while locked.")]
        public GameObject[] doors;
        [Tooltip("How far (tiles) inside the room edge the player must be before the doors shut, so they never shut on the player.")]
        [Min(0f)] public float shutInset = 2f;

        public bool Cleared { get; private set; }
        public bool Locked { get; private set; }

        // A room's doors shutting (true) or opening (false), not counting the first setup.
        public static event System.Action<RoomLock, bool> AnyDoors;

        Room room;
        bool setUp;

        void Start() { SetDoors(false); }

        public void Clear()
        {
            Cleared = true;
            SetDoors(false);
        }

        void FixedUpdate()
        {
            if (Cleared || Locked || player == null) return;
            if (room == null) room = GetComponent<Room>();
            Rect r = room.Bounds;
            Rect inner = new Rect(r.xMin + shutInset, r.yMin + shutInset, r.width - 2f * shutInset, r.height - 2f * shutInset);
            if (inner.Contains((Vector2)player.position)) SetDoors(true);
        }

        void SetDoors(bool shut)
        {
            bool changed = setUp && shut != Locked;
            setUp = true;
            Locked = shut;
            if (changed)
            {
                AnyDoors?.Invoke(this, shut);
                Cues.Raise(shut ? FeelEvent.DoorShut : FeelEvent.DoorOpen, transform.position, gameObject);
            }
            if (doors == null) return;
            foreach (GameObject d in doors) if (d != null) d.SetActive(shut);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit.TestArena
{
    // Test arena only: L cycles the camera layout (Open / Rooms slide / Rooms fade) on the
    // same level, C clears the room the player stands in (opens a RoomLock's doors).
    // The third label line names the layout.
    public class CameraKeys : MonoBehaviour
    {
        public CameraRig rig;

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || rig == null) return;
            if (k.lKey.wasPressedThisFrame) rig.SetLayout((CameraLayout)(((int)rig.layout + 1) % 3));
            if (k.cKey.wasPressedThisFrame && rig.target != null)
            {
                Room r = Room.At(rig.target.position);
                RoomLock rl = r != null ? r.GetComponent<RoomLock>() : null;
                if (rl != null) rl.Clear();
            }
        }

        void OnGUI()
        {
            if (rig == null) return;
            Room r = rig.target != null ? Room.At(rig.target.position) : null;
            RoomLock rl = r != null ? r.GetComponent<RoomLock>() : null;
            string lockText = rl == null ? "" : rl.Cleared ? "   (cleared)" : rl.Locked ? "   LOCKED - C clears" : "   (locks when you walk in)";
            GUI.Label(new Rect(8, 44, 1200, 24),
                "Camera: " + rig.layout + "   L layout   Room: " + (r != null ? r.name : "-") + lockText);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit.TestArena
{
    // Test arena only: F switches 4 / 8 facings, G faces walk / aim, H aims from mouse /
    // walk. The second label line names what is in use. Also places the aim marker.
    public class FacingKeys : MonoBehaviour
    {
        public PlayerFacing facing;
        public PlayerAim aim;
        [Tooltip("A small sprite placed along the aim, so aim is visible.")]
        public Transform aimMarker;
        [Tooltip("How far from the player the marker sits, tiles.")]
        public float markerDistance = 1.5f;

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && facing != null)
            {
                if (k.fKey.wasPressedThisFrame)
                    facing.settings.directions = facing.settings.directions == FacingCount.Eight ? FacingCount.Four : FacingCount.Eight;
                if (k.gKey.wasPressedThisFrame)
                    facing.settings.faceTowards = facing.settings.faceTowards == FaceTowards.Walk ? FaceTowards.Aim : FaceTowards.Walk;
            }
            if (k != null && aim != null && k.hKey.wasPressedThisFrame)
                aim.aimFrom = aim.aimFrom == AimFrom.Mouse ? AimFrom.Walk : AimFrom.Mouse;
        }

        void LateUpdate()
        {
            if (aimMarker == null || aim == null) return;
            Vector3 origin = aim.transform.position;
            aimMarker.position = origin + (Vector3)(aim.Direction * markerDistance);
        }

        void OnGUI()
        {
            if (facing == null) return;
            string a = aim != null ? aim.aimFrom.ToString() : "-";
            GUI.Label(new Rect(8, 26, 1200, 24),
                "F facings: " + (int)facing.settings.directions + "   G face towards: " + facing.settings.faceTowards
                + "   H aim from: " + a + "   (facing " + facing.Current + ")");
        }
    }
}

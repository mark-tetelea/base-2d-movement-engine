using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    public enum AimFrom { Mouse, Walk }

    // Turns the player's input into an aim direction (data), the way PlayerMover turns
    // keys into a move direction. Nothing downstream reads the mouse: touch sticks later
    // only need to set Direction another way. The last aim is kept when there is none.
    public class PlayerAim : MonoBehaviour
    {
        [Tooltip("Mouse: towards the world point under the cursor. Walk: the last walk direction.")]
        public AimFrom aimFrom = AimFrom.Mouse;
        [Tooltip("The camera the cursor is read through. Empty = Camera.main.")]
        public Camera view;

        PlayerMover mover;
        bool holding;

        public Vector2 Direction { get; private set; } = Vector2.down;

        // Input as data from elsewhere (a touch aim stick): aim this way until Release;
        // the mouse / walk is not read meanwhile.
        public void Hold(Vector2 direction)
        {
            holding = true;
            if (direction.sqrMagnitude > 1e-6f) Direction = direction.normalized;
        }

        public void Release() { holding = false; }

        void Update()
        {
            if (holding) return;
            if (mover == null) mover = GetComponent<PlayerMover>();
            Vector2 d = Vector2.zero;
            if (aimFrom == AimFrom.Walk)
            {
                if (mover != null) d = mover.LastMoveDirection;
            }
            else
            {
                Camera cam = view != null ? view : Camera.main;
                Mouse m = Mouse.current;
                if (cam != null && m != null)
                {
                    Vector2 screen = m.position.ReadValue();
                    Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
                    d = (Vector2)world - (Vector2)transform.position;
                }
            }
            if (d.sqrMagnitude > 1e-6f) Direction = d.normalized;
        }
    }
}

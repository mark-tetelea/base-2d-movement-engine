using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // Keyboard and mouse -> Gun, as data: left mouse is the trigger, R reloads, 1-9 or the
    // mouse wheel switch weapons, aim comes from PlayerAim. A touch layout later replaces
    // this file, not the Gun.
    [RequireComponent(typeof(Gun))]
    public class PlayerShooter : MonoBehaviour
    {
        [Tooltip("Number keys 1-9 pick weapons. Off where the number keys do something else (the test arena's feel keys).")]
        public bool numberKeysSelect = true;

        Gun gun;
        PlayerAim aim;
        PlayerMover mover;
        bool pressStartedOnUI;
        bool otherTrigger;

        public bool PressStartedOnUI => pressStartedOnUI;

        // Input as data from elsewhere (a touch fire button or aim stick), added to the mouse.
        public void SetOtherTrigger(bool down) { otherTrigger = down; }

        void Update()
        {
            if (gun == null) { gun = GetComponent<Gun>(); aim = GetComponent<PlayerAim>(); mover = GetComponent<PlayerMover>(); }
            if (aim != null) gun.SetAim(aim.Direction);
            if (PauseMenu.IsPaused) { gun.SetTrigger(false); return; }
            bool paused = mover != null && mover.inputPaused;
            Mouse m = Mouse.current;
            // A press that starts on a screen (button, slot, picker) never shoots, even
            // while held after the screen closes (trap 21).
            if (m != null && m.leftButton.wasPressedThisFrame) pressStartedOnUI = KitUI.PointerOverUI;
            bool mouse = !pressStartedOnUI && m != null && m.leftButton.isPressed;
            gun.SetTrigger(!paused && (mouse || otherTrigger));

            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.rKey.wasPressedThisFrame) gun.Reload();
                if (numberKeysSelect)
                    for (int i = 0; i < 9; i++)
                        if (k[Key.Digit1 + i].wasPressedThisFrame) gun.Select(i);
            }
            if (m != null && gun.weapons != null && gun.weapons.Length > 1)
            {
                float wheel = m.scroll.ReadValue().y;
                int n = gun.weapons.Length;
                if (wheel > 0f) gun.Select((gun.CurrentIndex + n - 1) % n);
                else if (wheel < 0f) gun.Select((gun.CurrentIndex + 1) % n);
            }
        }
    }
}

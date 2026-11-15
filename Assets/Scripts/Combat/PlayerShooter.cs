using UnityEngine;

namespace TopDownKit
{
    // Controls -> Gun, as data: Fire is the trigger (left mouse unless rebound), Reload,
    // Slot 1-9 and Next/Last weapon (the wheel) switch weapons, aim comes from PlayerAim.
    // Touch buttons press the same controls; the Gun never knows where input came from.
    [RequireComponent(typeof(Gun))]
    public class PlayerShooter : MonoBehaviour
    {
        [Tooltip("The Slot 1-9 controls pick weapons. Off where the number keys do something else (the test arena's feel keys).")]
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
            if (GamePause.IsPaused) { gun.SetTrigger(false); return; }
            bool paused = mover != null && mover.inputPaused;
            // A press that starts on a screen (button, slot, picker) never shoots, even
            // while held after the screen closes (trap 21).
            if (Controls.KeysPressed(Controls.Fire)) pressStartedOnUI = KitUI.PointerOverUI;
            // Touch buttons are screens themselves, so they skip that guard.
            bool held = (!pressStartedOnUI && Controls.KeysHeld(Controls.Fire)) || Controls.HeldElsewhere(Controls.Fire);
            gun.SetTrigger(!paused && (held || otherTrigger));

            if (Controls.Pressed(Controls.Reload)) gun.Reload();
            if (numberKeysSelect)
                for (int i = 0; i < 9; i++)
                    if (Controls.Pressed(Controls.Slot(i))) gun.Select(i);
            if (gun.weapons != null && gun.weapons.Length > 1)
            {
                int n = gun.weapons.Length;
                if (Controls.Pressed(Controls.PrevWeapon)) gun.Select((gun.CurrentIndex + n - 1) % n);
                else if (Controls.Pressed(Controls.NextWeapon)) gun.Select((gun.CurrentIndex + 1) % n);
            }
        }
    }
}

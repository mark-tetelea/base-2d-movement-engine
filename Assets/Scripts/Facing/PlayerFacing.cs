using UnityEngine;

namespace TopDownKit
{
    // Which way the player faces, decided on the fixed step from the walk direction
    // (PlayerMover) and the aim direction (PlayerAim) by the one facing rule.
    public class PlayerFacing : MonoBehaviour
    {
        public FacingSettings settings = new FacingSettings(FacingCount.Eight, FaceTowards.Walk);

        PlayerMover mover;
        PlayerAim aim;

        // 0-7, counter-clockwise from right (see Facing). Starts facing the camera.
        public int Current { get; private set; } = Facing.Down;

        void FixedUpdate()
        {
            if (mover == null) mover = GetComponent<PlayerMover>();
            if (aim == null) aim = GetComponent<PlayerAim>();
            Vector2 walk = mover != null ? mover.InputDirection : Vector2.zero;
            Vector2 aimDir = aim != null ? aim.Direction : walk;
            Current = Facing.Step(Current, walk, aimDir, settings);
        }
    }
}

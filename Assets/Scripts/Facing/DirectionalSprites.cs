using System;
using UnityEngine;

namespace TopDownKit
{
    // Idle and walk by sprite swap, from the facing. Plain on purpose: no Animator.
    // Each direction's frames: [0] idle, [1] and [2] the two steps. The walk plays
    // step, idle, step, idle. Visual only, so it runs in Update.
    [RequireComponent(typeof(SpriteRenderer))]
    public class DirectionalSprites : MonoBehaviour
    {
        [Serializable]
        public class Frames { public Sprite[] frames = new Sprite[3]; }

        [Tooltip("Indexed by facing: 0 right, 1 up-right, 2 up, 3 up-left, 4 left, 5 down-left, 6 down, 7 down-right.")]
        public Frames[] directions = new Frames[8];
        [Tooltip("On: left facings (3, 4, 5) show the right-side drawing flipped, so 5 drawings cover 8. Off: they use their own drawings.")]
        public bool mirrorLeft = true;
        [Tooltip("Frames per second of the walk cycle.")]
        public float walkFps = 8f;
        [Tooltip("Walking speed (tiles/s) under which the idle frame shows.")]
        public float walkThreshold = 0.1f;

        SpriteRenderer render;
        PlayerFacing facing;
        PlayerMover mover;
        float walkClock;

        void Update()
        {
            if (render == null) render = GetComponent<SpriteRenderer>();
            if (facing == null) facing = GetComponent<PlayerFacing>();
            if (mover == null) mover = GetComponent<PlayerMover>();
            if (facing == null) return;

            int f = facing.Current;
            bool flip = false;
            if (mirrorLeft && Facing.PointsLeft(f)) { f = Facing.Mirror(f); flip = true; }

            bool walking = mover != null && mover.MoveVelocity.magnitude > walkThreshold;
            walkClock = walking ? walkClock + Time.deltaTime : 0f;
            int frame = 0;
            if (walking)
            {
                int i = (int)(walkClock * walkFps) % 4;   // step, idle, step, idle
                frame = i == 0 ? 1 : i == 2 ? 2 : 0;
            }

            Sprite s = Pick(f, frame);
            if (s != null) render.sprite = s;
            render.flipX = flip;
        }

        Sprite Pick(int f, int frame)
        {
            if (directions == null || f >= directions.Length || directions[f] == null) return null;
            Sprite[] fr = directions[f].frames;
            if (fr == null || fr.Length == 0) return null;
            Sprite s = frame < fr.Length ? fr[frame] : null;
            return s != null ? s : fr[0];
        }
    }
}

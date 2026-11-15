using UnityEngine;

namespace TopDownKit
{
    // Plays a SpriteAnimSet by sprite swap: no Animator, visual only, so it runs in Update.
    // Which animation: one started with Play (Attack, Hurt - plays once) or Hold (Death -
    // stays), else Dash while a Dash runs, else Walk while moving, else Idle. Any it does
    // not find in the set falls back to the next (no Dash anim: Walk; no Walk: Idle).
    // Which facing: the PlayerFacing if there is one, else the way it last moved (enemies).
    // Combat plays Attack on a shot or swing, Hurt on a hit, Death on dying.
    [RequireComponent(typeof(SpriteRenderer))]
    public class DirectionalSprites : MonoBehaviour
    {
        [Tooltip("The animations (Create > TopDownKit > Sprite Animations). Swap the asset to swap the character's art.")]
        public SpriteAnimSet art;
        [Tooltip("Walking speed (tiles/s) under which Idle plays.")]
        public float walkThreshold = 0.1f;
        [Tooltip("With no PlayerFacing: 4 or 8 facings from the way it moves.")]
        public FacingCount facingsWhenNoAim = FacingCount.Eight;

        SpriteRenderer render;
        PlayerFacing facing;
        PlayerMover mover;
        Dash dash;
        int lastFacing = 6;   // down
        string playing;       // what is showing now
        float clock;
        string oneShot;       // Play: once, then back
        string held;          // Hold: until Release

        // The animation showing now, and how far into it (seconds).
        public string Current => playing;
        public float Time01 { get; private set; }

        // Plays an animation once (Attack, Hurt, a game's own), then goes back to the usual one.
        public void Play(string anim)
        {
            if (held != null || art == null || art.Find(anim) == null) return;
            oneShot = anim;
            Switch(anim);
        }

        // Shows an animation until Release (Death stays on its last frame).
        public void Hold(string anim)
        {
            if (art == null || art.Find(anim) == null) return;
            held = anim;
            oneShot = null;
            Switch(anim);
        }

        public void Release() { held = null; }

        void Switch(string anim) { if (playing != anim) { playing = anim; clock = 0f; } }

        void Update()
        {
            if (render == null)
            {
                render = GetComponent<SpriteRenderer>();
                facing = GetComponent<PlayerFacing>();
                mover = GetComponent<PlayerMover>();
                dash = GetComponent<Dash>();
            }
            if (art == null) return;

            // Which facing (0-7).
            int f = lastFacing;
            if (facing != null) f = facing.Current;
            else if (mover != null && mover.MoveVelocity.magnitude > walkThreshold)
                f = Facing.Snap(mover.MoveVelocity, facingsWhenNoAim, lastFacing);
            lastFacing = f;

            // Which animation.
            string want = held ?? oneShot ?? Usual();
            Switch(want);
            SpriteAnim a = art.Find(playing);
            if (a == null) return;

            bool flip = false;
            if (art.mirrorLeft && a.facings != null && a.facings.Length > 1 && Facing.PointsLeft(f)) { f = Facing.Mirror(f); flip = true; }
            SpriteFrames fr = a.Get(f);
            if (fr == null) return;

            clock += UnityEngine.Time.deltaTime;
            int n = fr.frames.Length;
            int i = Mathf.FloorToInt(clock * a.fps);
            if (a.loop) i %= n;
            else if (i >= n)
            {
                i = n - 1;
                if (oneShot == playing) { oneShot = null; }   // done: back to the usual next frame
            }
            Time01 = n > 0 ? Mathf.Clamp01((float)i / n) : 0f;
            Sprite s = fr.frames[i];
            if (s != null) render.sprite = s;
            render.flipX = flip;
        }

        // Dash while dashing, Walk while moving, else Idle - whichever the set has.
        string Usual()
        {
            if (dash != null && dash.IsDashing && art.Find(Anims.Dash) != null) return Anims.Dash;
            bool walking = mover != null && mover.MoveVelocity.magnitude > walkThreshold;
            if (walking && art.Find(Anims.Walk) != null) return Anims.Walk;
            return Anims.Idle;
        }
    }
}

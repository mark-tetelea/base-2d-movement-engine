using System;
using UnityEngine;

namespace TopDownKit
{
    // The one camera script. Put it on the camera (next to the Pixel Perfect Camera).
    // The layout is a setting: Open follows the target anywhere; the room layouts keep the
    // view inside the target's current Room and slide or fade to the next one. Rooms bigger
    // than the view are followed inside, clamped to their edges.
    //
    // Runs in LateUpdate after the interpolated player has moved: the camera is
    // presentation, not gameplay, so it reads the frame time; the maths is CameraFollow's.
    // The final position is always on whole screen pixels.
    [DefaultExecutionOrder(100)]
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Lean toward this aim (optional).")]
        public PlayerAim aim;
        [Tooltip("Keys are paused on this mover during slides and fades (optional).")]
        public PlayerMover mover;

        public CameraLayout layout = CameraLayout.Open;
        public FollowSettings follow = new FollowSettings { smoothTime = 0.12f, deadZone = new Vector2(1f, 0.75f), lookAhead = 1.5f };

        [Tooltip("Visible area in tiles (640x360 at 16 px per tile = 40 x 22.5).")]
        public Vector2 viewTiles = new Vector2(40f, 22.5f);
        [Tooltip("Screen pixels per tile; the camera position is rounded to these.")]
        public float pixelsPerTile = 16f;

        [Header("Rooms")]
        [Tooltip("Seconds the slide to the next room takes.")]
        [Min(0.01f)] public float slideTime = 0.5f;
        public bool pauseInputDuringSlide = true;
        [Tooltip("Seconds to fade out, and again to fade in.")]
        [Min(0.01f)] public float fadeTime = 0.25f;
        public Color fadeColor = Color.black;

        [Header("Screen shake")]
        [Tooltip("Multiplies every shake (0 = shake off).")]
        [Min(0f)] public float shakeScale = 1f;
        [Tooltip("How fast a shake dies away, tiles per second.")]
        [Min(0.01f)] public float shakeDecay = 2f;

        enum Phase { Follow, Slide, FadeOut, FadeIn }

        Vector2 anchor, pos;
        Room room;
        Phase phase = Phase.Follow;
        float t;
        Vector2 slideFrom, slideTo;
        Action atDark;
        float shake;
        Vector2 shakeOffset;
        SpriteRenderer fade;
        bool started;

        public Room CurrentRoom => room;
        public bool InTransition => phase != Phase.Follow;
        // The camera centre before shake (on whole pixels): what the room check measures.
        public Vector2 BasePosition { get; private set; }

        Vector2 HalfView => viewTiles * 0.5f;
        Vector2 TargetPos => target != null ? (Vector2)target.position : pos;
        bool Rooms => layout != CameraLayout.Open;

        // Shake by up to this many tiles; it dies away by shakeDecay.
        public void Shake(float tiles) { shake = Mathf.Max(shake, tiles * shakeScale); }

        // Switches layout and jumps there; cancels any slide or fade in progress.
        public void SetLayout(CameraLayout l)
        {
            layout = l;
            if (phase != Phase.Follow) { phase = Phase.Follow; atDark = null; Pause(false); SetFade(0f); }
            Snap();
        }

        // Fades out, calls whatAtDark (move the player to another area), cuts the camera
        // there and fades back in. Keys are paused the whole time.
        public void FadeThen(Action whatAtDark)
        {
            atDark = whatAtDark;
            phase = Phase.FadeOut;
            t = 0f;
            Pause(true);
        }

        // Jumps straight to where the camera should be, no smoothing.
        public void Snap()
        {
            anchor = TargetPos;
            room = Room.At(TargetPos);
            pos = Goal();
        }

        Vector2 Goal()
        {
            Vector2 g = anchor + (aim != null ? aim.Direction * follow.lookAhead : Vector2.zero);
            if (Rooms && room != null) g = CameraFollow.Clamp(g, room.Bounds, HalfView);
            return g;
        }

        void LateUpdate()
        {
            if (!started) { started = true; Snap(); }
            float dt = Time.deltaTime;

            switch (phase)
            {
                case Phase.Follow:
                    anchor = CameraFollow.DeadZone(anchor, TargetPos, follow.deadZone);
                    Room now = Room.At(TargetPos);
                    if (now != null && now != room)
                    {
                        if (room == null || layout == CameraLayout.Open) room = now;
                        else if (layout == CameraLayout.RoomsSlide) StartSlide(now);
                        else FadeThen(null);
                    }
                    if (phase == Phase.Follow)
                    {
                        pos = CameraFollow.Smooth(pos, Goal(), follow.smoothTime, dt);
                        if (Rooms && room != null) pos = CameraFollow.Clamp(pos, room.Bounds, HalfView);
                    }
                    break;

                case Phase.Slide:
                    t += dt / slideTime;
                    pos = Vector2.Lerp(slideFrom, slideTo, Mathf.SmoothStep(0f, 1f, Mathf.Min(t, 1f)));
                    if (t >= 1f) { phase = Phase.Follow; anchor = CameraFollow.DeadZone(anchor, TargetPos, follow.deadZone); Pause(false); }
                    break;

                case Phase.FadeOut:
                    t += dt / fadeTime;
                    if (t >= 1f)
                    {
                        Action a = atDark;
                        atDark = null;
                        a?.Invoke();
                        Snap();
                        phase = Phase.FadeIn;
                        t = 0f;
                    }
                    break;

                case Phase.FadeIn:
                    t += dt / fadeTime;
                    if (t >= 1f) { phase = Phase.Follow; Pause(false); }
                    break;
            }

            SetFade(phase == Phase.FadeOut ? Mathf.Clamp01(t) : phase == Phase.FadeIn ? 1f - Mathf.Clamp01(t) : 0f);

            shake = Mathf.Max(0f, shake - shakeDecay * dt);
            shakeOffset = shake > 0f ? UnityEngine.Random.insideUnitCircle * shake : Vector2.zero;

            BasePosition = CameraFollow.SnapToPixel(pos, pixelsPerTile);
            Vector2 p = CameraFollow.SnapToPixel(pos + shakeOffset, pixelsPerTile);
            transform.position = new Vector3(p.x, p.y, transform.position.z);
        }

        void StartSlide(Room next)
        {
            room = next;
            anchor = TargetPos;
            slideFrom = pos;
            slideTo = Goal();
            phase = Phase.Slide;
            t = 0f;
            if (pauseInputDuringSlide) Pause(true);
        }

        void Pause(bool on) { if (mover != null) mover.inputPaused = on; }

        // A full-view quad in front of the camera, made on first use.
        void SetFade(float alpha)
        {
            if (fade == null)
            {
                if (alpha <= 0f) return;
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                var go = new GameObject("Fade");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, 1f);
                go.transform.localScale = new Vector3(viewTiles.x + 2f, viewTiles.y + 2f, 1f);
                fade = go.AddComponent<SpriteRenderer>();
                fade.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                fade.sortingOrder = short.MaxValue;
            }
            Color c = fadeColor;
            c.a = alpha;
            fade.color = c;
            fade.enabled = alpha > 0f;
        }
    }
}

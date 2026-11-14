using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // The keyboard player. Reads WASD / arrow keys into a move direction every frame and
    // moves on the fixed 60 Hz step with the one movement rule (CLAUDE.md, Decision 1).
    // The Rigidbody2D only stops walls being walked through; it must have gravity 0 and
    // interpolation on, which the test arena sets up.
    //
    // The body's velocity is two parts added together: the walking velocity (Movement.Step)
    // and a knockback push (Knockback.Step), kept apart so an Instant feel cannot cancel a
    // push in one step. Add-ons (SpeedZone, Dash) talk to it through the public methods.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMover : MonoBehaviour
    {
        [Tooltip("How moving feels. Read every step, so swapping or tuning it shows at once.")]
        public MoveFeel feel;
        [Tooltip("While on, keys are ignored (walking eases to a stop by the feel). Set by camera fades and slides.")]
        public bool inputPaused;
        [Tooltip("On: WASD / arrows steer it (the player). Off: something else calls SetMoveDirection (an enemy brain, a touch stick).")]
        public bool readKeyboard = true;

        Rigidbody2D body;
        Vector2 held;              // keys held at the last frame
        Vector2 pressedSinceStep;  // a new direction no step has used yet
        Vector2 move;              // walking velocity, tiles/s
        Vector2 push;              // knockback velocity, tiles/s
        Vector2 sentVelocity;      // what the last step gave the body (move + push)
        bool driven;               // an add-on set this step's walking velocity
        Vector2 drivenVelocity;
        readonly List<SpeedZone> zones = new List<SpeedZone>();

        // Grid step (MoveSettings.gridStep): one slide to a tile centre at a time.
        bool sliding;
        int slideLeft;             // fixed steps until the slide arrives
        int paceLeft;              // fixed steps until the next step may start
        Vector2 slideTo;           // stand point it arrives at
        Vector2 slideVelocity;
        Vector2 pendingStep;       // a press no grid step has used yet (buffered)
        readonly Vector2Int[] candidates = new Vector2Int[3];

        // Grid step: whether a slide is under way, and the tile centre it ends on.
        public bool Sliding => sliding;
        public Vector2 SlideTarget => slideTo;

        public Vector2 MoveVelocity => move;
        public Vector2 Push => push;
        // The last direction asked for (right before any), for a dash with no keys held.
        public Vector2 LastMoveDirection { get; private set; } = Vector2.right;
        // The direction the last step walked in (zero when no keys), for facing.
        public Vector2 InputDirection { get; private set; }

        // The settings in force now: the feel, changed by any zones the mover stands in.
        public MoveSettings CurrentSettings => feel == null ? default : SpeedZone.Apply(feel.settings, zones);

        public void EnterZone(SpeedZone z) { if (!zones.Contains(z)) zones.Add(z); }
        public void ExitZone(SpeedZone z) { zones.Remove(z); }

        // Knockback / recoil: adds a push (tiles/s), which then dies away by the feel's
        // knockbackStopRate. Make one with Knockback.Away.
        public void AddPush(Vector2 p) { push += p; }

        // Next step, use this walking velocity instead of Movement.Step (a dash).
        public void DriveThisStep(Vector2 velocity) { driven = true; drivenVelocity = velocity; }

        // Sets the walking velocity now (end of a dash). Steps go on from it.
        public void SetMoveVelocity(Vector2 velocity)
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            move = velocity;
            sentVelocity = move + push;
            body.linearVelocity = sentVelocity;
        }

        // Input as data: the direction to walk from the next step on (length 1 or less).
        // Kept until changed. Used when readKeyboard is off.
        public void SetMoveDirection(Vector2 direction)
        {
            if (direction != held && direction != Vector2.zero) pressedSinceStep = direction;
            held = direction;
            if (direction != Vector2.zero) LastMoveDirection = direction.normalized;
        }

        void Update()
        {
            if (!readKeyboard) return;
            Vector2 keys = ReadKeys();
            // Latch a new direction until a step uses it, so a tap that starts and ends
            // between two steps still moves (Decision 1, input timing).
            if (keys != held && keys != Vector2.zero) pressedSinceStep = keys;
            held = keys;
            // Updated here too, so a dash pressed together with a new direction uses it.
            if (keys != Vector2.zero) LastMoveDirection = keys.normalized;
        }

        void FixedUpdate()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            Vector2 direction = held != Vector2.zero ? held : pressedSinceStep;
            bool newPress = pressedSinceStep != Vector2.zero;
            if (inputPaused) { direction = Vector2.zero; newPress = false; pendingStep = Vector2.zero; }
            pressedSinceStep = Vector2.zero;
            InputDirection = direction;
            if (direction != Vector2.zero) LastMoveDirection = direction.normalized;
            if (feel == null) return;
            float dt = Time.fixedDeltaTime;
            MoveSettings s = CurrentSettings;

            TakeWallLoss();

            if (s.gridStep && !driven) { GridFixedStep(direction, newPress, s, dt); return; }
            sliding = false; paceLeft = 0; pendingStep = Vector2.zero;

            move = driven ? drivenVelocity : Movement.Step(move, direction, s, dt);
            driven = false;
            sentVelocity = move + push;
            body.linearVelocity = sentVelocity;
            // The push decays after it was used, so a fresh push moves one full step.
            push = Knockback.Step(push, s.knockbackStopRate, dt);
        }

        // Grid step, one fixed step. Arrives (snapping the last thousandth of a tile), then
        // starts the next step when the pace allows: back onto a tile centre first if
        // something left it between two (a dash, a push, a shove), else in the direction
        // asked for - held (holdToWalk) or a buffered press. Nothing starts while a push
        // is moving it. Blocked steps are not taken (GridStep.CanStep).
        void GridFixedStep(Vector2 direction, bool newPress, MoveSettings s, float dt)
        {
            if (newPress) pendingStep = direction;
            Vector2 offset = GridStep.StandPoint(body) - body.position;

            if (sliding && --slideLeft <= 0)
            {
                sliding = false;
                // Arrived unless something stopped it on the way; then it lines up below.
                if ((GridStep.StandPoint(body) - slideTo).sqrMagnitude < 0.01f) body.position = slideTo - offset;
            }
            if (paceLeft > 0) paceLeft--;

            if (!sliding && paceLeft <= 0 && push == Vector2.zero)
            {
                Vector2 at = GridStep.StandPoint(body);
                int perTile = GridStep.StepsPerTile(s.speed, dt);
                if (perTile > 0)
                {
                    if (!GridStep.OnCentre(at))
                    {
                        if (GridStep.NearestFree(at, body, out Vector2 c)) StartSlide(at, c, perTile, s, dt, offset);
                    }
                    else
                    {
                        Vector2 want = s.holdToWalk && direction != Vector2.zero ? direction : pendingStep;
                        pendingStep = Vector2.zero;
                        int n = GridStep.Candidates(want, s.eightDirections, candidates);
                        for (int i = 0; i < n; i++)
                            if (GridStep.CanStep(at, candidates[i], body)) { StartSlide(at, at + (Vector2)candidates[i], perTile, s, dt, offset); break; }
                    }
                }
            }

            move = sliding ? slideVelocity : Vector2.zero;
            sentVelocity = move + push;
            body.linearVelocity = sentVelocity;
            push = Knockback.Step(push, s.knockbackStopRate, dt);
        }

        void StartSlide(Vector2 from, Vector2 to, int perTile, MoveSettings s, float dt, Vector2 offset)
        {
            int slide = GridStep.SlideSteps(s.stepTime, perTile, dt);
            paceLeft = perTile;
            slideTo = to;
            if (slide <= 0) { body.position = to - offset; sliding = false; slideVelocity = Vector2.zero; return; }
            sliding = true;
            slideLeft = slide;
            slideVelocity = GridStep.SlideVelocity(from, to, slide, dt);
        }

        // After the physics step the body has lost whatever a wall (or anything else) took
        // from the velocity it was given, so pushing into a wall builds up nothing. With no
        // push, the walking velocity is simply the body's. With a push, the part of each
        // that heads into what was lost is removed from both.
        void TakeWallLoss()
        {
            Vector2 actual = body.linearVelocity;
            if (push == Vector2.zero) { move = actual; return; }
            Vector2 lost = sentVelocity - actual;
            if (lost.sqrMagnitude < 1e-8f) return;
            Vector2 n = lost.normalized;
            float m = Vector2.Dot(move, n), p = Vector2.Dot(push, n);
            if (m > 0f) move -= n * m;
            if (p > 0f) push -= n * p;
        }

        // Opposite keys cancel. Diagonals come out as (1, 1); the rule cuts them to length 1.
        static Vector2 ReadKeys()
        {
            Keyboard k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = 0f, y = 0f;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) x += 1f;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) x -= 1f;
            if (k.wKey.isPressed || k.upArrowKey.isPressed) y += 1f;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) y -= 1f;
            return new Vector2(x, y);
        }
    }
}

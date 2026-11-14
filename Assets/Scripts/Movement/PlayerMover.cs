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

        Rigidbody2D body;
        Vector2 held;              // keys held at the last frame
        Vector2 pressedSinceStep;  // a new direction no step has used yet
        Vector2 move;              // walking velocity, tiles/s
        Vector2 push;              // knockback velocity, tiles/s
        Vector2 sentVelocity;      // what the last step gave the body (move + push)
        bool driven;               // an add-on set this step's walking velocity
        Vector2 drivenVelocity;
        readonly List<SpeedZone> zones = new List<SpeedZone>();

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

        void Update()
        {
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
            if (inputPaused) direction = Vector2.zero;
            pressedSinceStep = Vector2.zero;
            InputDirection = direction;
            if (direction != Vector2.zero) LastMoveDirection = direction.normalized;
            if (feel == null) return;
            float dt = Time.fixedDeltaTime;
            MoveSettings s = CurrentSettings;

            TakeWallLoss();

            move = driven ? drivenVelocity : Movement.Step(move, direction, s, dt);
            driven = false;
            sentVelocity = move + push;
            body.linearVelocity = sentVelocity;
            // The push decays after it was used, so a fresh push moves one full step.
            push = Knockback.Step(push, s.knockbackStopRate, dt);
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

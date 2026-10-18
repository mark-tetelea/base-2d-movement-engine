using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // The keyboard player. Reads WASD / arrow keys into a move direction every frame and
    // moves on the fixed 60 Hz step with the one movement rule (CLAUDE.md, Decision 1).
    // The Rigidbody2D only stops walls being walked through; it must have gravity 0 and
    // interpolation on, which the test arena sets up.
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMover : MonoBehaviour
    {
        [Tooltip("How moving feels. Read every step, so swapping or tuning it shows at once.")]
        public MoveFeel feel;

        Rigidbody2D body;
        Vector2 held;              // keys held at the last frame
        Vector2 pressedSinceStep;  // a new direction no step has used yet

        void Update()
        {
            Vector2 keys = ReadKeys();
            // Latch a new direction until a step uses it, so a tap that starts and ends
            // between two steps still moves (Decision 1, input timing).
            if (keys != held && keys != Vector2.zero) pressedSinceStep = keys;
            held = keys;
        }

        void FixedUpdate()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            Vector2 direction = held != Vector2.zero ? held : pressedSinceStep;
            pressedSinceStep = Vector2.zero;
            if (feel == null) return;

            // Start from the body's own velocity: after the physics step it has already
            // lost whatever a wall took, so pushing into a wall builds up nothing.
            body.linearVelocity = Movement.Step(body.linearVelocity, direction, feel.settings, Time.fixedDeltaTime);
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

using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // How a dash / dodge roll goes. Numbers in tiles and seconds (Decision 3).
    [Serializable]
    public struct DashSettings
    {
        [Tooltip("Tiles covered by one dash, in open floor.")]
        [Min(0f)] public float distance;

        [Tooltip("Seconds one dash lasts. Rounded to whole 60 Hz steps; the speed is set so the distance stays exact.")]
        [Min(0f)] public float duration;

        [Tooltip("Seconds from the end of one dash until the next can start.")]
        [Min(0f)] public float cooldown;

        [Tooltip("Whether damage is ignored during the dash (a dodge roll). Stored now; damage reads it later.")]
        public bool invulnerable;
    }

    // Optional add-on: a dash / dodge roll for a PlayerMover. While it runs it drives the
    // mover's walking velocity in a straight line; knockback still adds on top. Afterwards
    // the mover carries on from at most its top speed in the dash direction, so a gliding
    // feel does not keep the dash's speed. Runs before PlayerMover in each fixed step.
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(PlayerMover))]
    public class Dash : MonoBehaviour
    {
        public DashSettings settings = new DashSettings { distance = 3f, duration = 0.2f, cooldown = 0.4f, invulnerable = true };

        PlayerMover mover;
        bool requested;
        Vector2 direction;
        int stepsLeft;
        float cooldownLeft;
        Vector2 velocity;

        public bool IsDashing => stepsLeft > 0;
        // Every dash that starts (sounds, particles).
        public static event Action<Dash> AnyStarted;
        // Read by damage later (phase 4).
        public bool IsInvulnerable => IsDashing && settings.invulnerable;

        // Input arrives as data: Space for the keyboard player; a touch button calls Request.
        void Update()
        {
            if (PauseMenu.IsPaused) return;
            Keyboard k = Keyboard.current;
            if (k != null && k.spaceKey.wasPressedThisFrame) requested = true;
        }

        // Asks for a dash at the next step (ignored while dashing or cooling down).
        public void Request() { requested = true; }

        void FixedUpdate()
        {
            if (mover == null) mover = GetComponent<PlayerMover>();
            float dt = Time.fixedDeltaTime;
            bool start = requested && !mover.inputPaused;
            requested = false;

            if (stepsLeft > 0)
            {
                stepsLeft--;
                if (stepsLeft > 0) { mover.DriveThisStep(velocity); return; }
                // The last dash step has moved: hand back at most top speed, then walk.
                float top = mover.CurrentSettings.speed;
                mover.SetMoveVelocity(direction * Mathf.Min(top, velocity.magnitude));
                cooldownLeft = settings.cooldown;
                return;
            }
            if (cooldownLeft > 0f) { cooldownLeft -= dt; return; }
            if (!start) return;

            direction = mover.LastMoveDirection;
            velocity = DashVelocity(direction, settings, dt);
            stepsLeft = Steps(settings, dt);
            mover.DriveThisStep(velocity);
            AnyStarted?.Invoke(this);
        }

        // Whole fixed steps one dash lasts (at least 1).
        public static int Steps(DashSettings s, float dt) => Mathf.Max(1, Mathf.RoundToInt(s.duration / dt));

        // The dash rule: a straight line, fast enough to cover exactly `distance` tiles in
        // the whole number of steps it lasts.
        public static Vector2 DashVelocity(Vector2 direction, DashSettings s, float dt)
        {
            if (direction == Vector2.zero) return Vector2.zero;
            return direction.normalized * (s.distance / (Steps(s, dt) * dt));
        }
    }
}

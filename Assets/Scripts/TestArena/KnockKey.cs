using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit.TestArena
{
    // Test arena only: K knocks the player away from the mouse pointer, so knockback can
    // be tried with every feel. Stands in for a hit until enemies exist (phase 4).
    public class KnockKey : MonoBehaviour
    {
        public PlayerMover player;

        [Tooltip("Push given by one press, in tiles per second.")]
        [Min(0f)] public float strength = 14f;

        bool pressed;

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && k.kKey.wasPressedThisFrame) pressed = true;
        }

        void FixedUpdate()
        {
            if (!pressed || player == null) return;
            pressed = false;
            Camera cam = Camera.main;
            Mouse m = Mouse.current;
            if (cam == null || m == null) return;
            Vector2 mouse = cam.ScreenToWorldPoint(m.position.ReadValue());
            player.AddPush(Knockback.Away(mouse, player.GetComponent<Rigidbody2D>().position, strength));
        }
    }
}

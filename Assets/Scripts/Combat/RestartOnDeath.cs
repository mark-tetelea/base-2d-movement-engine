using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownKit
{
    // Add-on for the player: when its Health hits 0, stop its input and reload the scene
    // after a short wait. A game with lives or a game-over screen replaces this.
    [RequireComponent(typeof(Health))]
    public class RestartOnDeath : MonoBehaviour
    {
        [Tooltip("Seconds between death and the restart.")]
        [Min(0f)] public float delay = 1.5f;

        Health health;
        float left = -1f;

        public bool Restarting => left >= 0f;

        void OnEnable()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        void OnDisable() { if (health != null) health.Died -= OnDied; }

        void OnDied(Health h)
        {
            left = delay;
            PlayerMover mover = GetComponent<PlayerMover>();
            if (mover != null) mover.PauseInput(this, true);
            Gun gun = GetComponent<Gun>();
            if (gun != null) gun.enabled = false;
        }

        void FixedUpdate()
        {
            if (left < 0f) return;
            left -= Time.fixedDeltaTime;
            if (left <= 0f) SceneManager.LoadScene(gameObject.scene.buildIndex);
        }
    }
}

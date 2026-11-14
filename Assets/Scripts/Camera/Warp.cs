using UnityEngine;

namespace TopDownKit
{
    // Add-on: a door to another area (Undertale style). Walking into the trigger fades
    // out, moves the player to the destination, cuts the camera there and fades in.
    // Put the destination outside any warp, or the player bounces back.
    [RequireComponent(typeof(Collider2D))]
    public class Warp : MonoBehaviour
    {
        public Transform destination;
        [Tooltip("Empty = the CameraRig on the main camera.")]
        public CameraRig rig;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (destination == null) return;
            var body = other.attachedRigidbody;
            if (body == null || body.GetComponent<PlayerMover>() == null) return;
            if (rig == null && Camera.main != null) rig = Camera.main.GetComponent<CameraRig>();
            if (rig == null) { Move(body); return; }
            if (rig.InTransition) return;
            rig.FadeThen(() => Move(body));
        }

        void Move(Rigidbody2D body)
        {
            Vector2 to = destination.position;
            body.position = to;
            body.transform.position = to;
        }
    }
}

using UnityEngine;

namespace TopDownKit
{
    // Add-on target: a lever. Each use flips it and switches its targets on or off
    // (a gate, a bridge, a light). The art is flipped to show which way it points.
    public class Lever : Interactable
    {
        [Tooltip("Objects active while the lever is off; switched off when it is on.")]
        public GameObject[] offWhenOn;
        [Tooltip("Objects switched on while the lever is on.")]
        public GameObject[] onWhenOn;
        public SpriteRenderer art;
        public bool on;

        void Reset() { verb = "Pull"; }

        void Start() { Apply(); }

        // Every pull of a lever (sounds).
        public static event System.Action<Lever> AnyPulled;

        public override void Use(Interactor who) { on = !on; Apply(); AnyPulled?.Invoke(this); }

        void Apply()
        {
            if (offWhenOn != null) foreach (GameObject g in offWhenOn) if (g != null) g.SetActive(!on);
            if (onWhenOn != null) foreach (GameObject g in onWhenOn) if (g != null) g.SetActive(on);
            if (art != null) art.flipX = on;
        }
    }
}

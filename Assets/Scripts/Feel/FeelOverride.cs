using UnityEngine;

namespace TopDownKit
{
    // Add-on: this object's hits and death use other slots of the FeelSet (a crate breaks
    // instead of dying; a game's player can have its own hurt sound).
    public class FeelOverride : MonoBehaviour
    {
        public FeelEvent onHit = FeelEvent.Hit;
        public FeelEvent onDeath = FeelEvent.Death;
    }
}

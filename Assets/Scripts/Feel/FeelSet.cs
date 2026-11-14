using System;
using UnityEngine;

namespace TopDownKit
{
    // Everything that can make a sound, an effect or a hit-stop. A game that needs more
    // adds names at the END (slots are saved by number) and calls FeelPlayer.Play itself.
    public enum FeelEvent
    {
        Step, Shot, Swing, Dash,
        Hit, MeleeHit, PlayerHurt, Death, Break,
        Pickup, DoorOpen, DoorShut, Crumble, UiClick,
        Buy, Sell, Save
    }

    // One event's slot: a sound, an effect and a freeze. Each part may be left empty.
    [Serializable]
    public struct FeelSlot
    {
        public FeelEvent id;
        [Tooltip("Clips for this event; one is picked at random. Empty = silent.")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume;
        [Tooltip("Random pitch change, +/- this much (0 = always the same).")]
        [Range(0f, 0.5f)] public float pitchJitter;
        [Tooltip("Effect prefab placed where it happens (a Burst, a ParticleSystem, anything). Empty = none.")]
        public GameObject effect;
        [Tooltip("Seconds before the effect is put away (0 = it switches itself off, like a Burst).")]
        [Min(0f)] public float effectLife;
        [Tooltip("Hit-stop: seconds the game freezes when this happens (0 = off). Meant for hits and deaths.")]
        [Min(0f)] public float hitStop;
    }

    // A game's set of sounds, effects and hit-stop lengths, one slot per event. Swap the
    // whole set by pointing the scene's FeelPlayer at another asset; swap one sound by
    // dropping a clip in its slot. No code.
    [CreateAssetMenu(menuName = "TopDownKit/Feel Set", fileName = "FeelSet")]
    public class FeelSet : ScriptableObject
    {
        public FeelSlot[] slots;
        [Tooltip("Played on a loop while the scene runs. Empty = no music.")]
        public AudioClip music;

        // The slot for an event, or false if the set has none. Read every call (trap 15).
        public bool Find(FeelEvent id, out FeelSlot slot)
        {
            if (slots != null)
                for (int i = 0; i < slots.Length; i++)
                    if (slots[i].id == id) { slot = slots[i]; return true; }
            slot = default;
            return false;
        }
    }
}

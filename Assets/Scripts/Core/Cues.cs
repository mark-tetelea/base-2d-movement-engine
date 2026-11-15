using System;
using UnityEngine;

namespace TopDownKit
{
    // Everything that can make a sound, an effect or a hit-stop. A game that needs more
    // adds names at the END (slots are saved by number) and raises them with Cues.Raise.
    public enum FeelEvent
    {
        Step, Shot, Swing, Dash,
        Hit, MeleeHit, PlayerHurt, Death, Break,
        Pickup, DoorOpen, DoorShut, Crumble, UiClick,
        Buy, Sell, Save
    }

    // One moment worth a sound or an effect: what, where, from which object.
    public struct Cue
    {
        public FeelEvent id;
        public Vector2 at;
        // The object it happened to (may be null). Its own slots can override the set's.
        public GameObject from;
        // For hits and deaths: the Health's team (-1 = not a hit).
        public int team;
        // For hits and deaths: the ground did it (red tiles, a trail).
        public bool fromGround;
    }

    // The kit's systems say "this happened here" through one static event, and the Feel
    // add-on (or a game's own listener) turns that into sounds, effects and hit-stop. So no
    // system needs the Feel folder, and deleting Feel - or any system - breaks nothing.
    // Callers (CLAUDE.md, three real callers): Health, Gun, Pickup, Lever, LockedDoor,
    // GroundMap, Shop, SaveGame raise; FeelPlayer listens.
    public static class Cues
    {
        public static event Action<Cue> Raised;

        public static void Raise(FeelEvent id, Vector2 at, GameObject from = null)
        {
            Raised?.Invoke(new Cue { id = id, at = at, from = from, team = -1 });
        }

        public static void Raise(Cue cue) { Raised?.Invoke(cue); }
    }
}

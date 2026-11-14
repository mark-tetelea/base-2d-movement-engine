using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Something the interact key can use: a chest, an NPC, an item on the floor, a door or
    // a lever. Each is an add-on piece deriving from this; the Interactor picks the best
    // one in reach with the one rule (Interaction.Pick) and calls Use.
    public abstract class Interactable : MonoBehaviour
    {
        [Tooltip("Word shown with the prompt (Open, Talk, Take...).")]
        public string verb = "Use";
        [Tooltip("Where the target is for reach and facing, from this object's pivot (tiles). Things with feet: about half their height up.")]
        public Vector2 centre = new Vector2(0f, 0.5f);
        [Tooltip("Where the prompt shows, from the pivot (tiles).")]
        public Vector2 promptOffset = new Vector2(0f, 1.4f);

        static readonly List<Interactable> all = new List<Interactable>();
        public static IReadOnlyList<Interactable> All => all;

        public Vector2 Point => (Vector2)transform.position + centre;

        protected virtual void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        protected virtual void OnDisable() { all.Remove(this); }

        // Whether it shows up as a target at all right now (an open door, an auto pickup: no).
        public virtual bool CanUse(Interactor who) => true;
        public abstract void Use(Interactor who);
    }
}

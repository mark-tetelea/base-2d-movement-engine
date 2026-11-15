using UnityEngine;

namespace TopDownKit
{
    // How an item on the floor is picked up.
    // Walk: taken by walking onto it (a Collector's collect reach).
    // Interact: an interaction target, taken with the interact key.
    // Magnet: flies to a Collector from magnet reach, then taken.
    public enum PickupMode { Walk, Interact, Magnet }

    // What an item is: a name, an icon and how many fit in one slot, plus how it is picked
    // up and whether it can be dropped. Settings only; what an item does when used belongs
    // to the game (or to an add-on piece that reads it).
    [CreateAssetMenu(menuName = "TopDownKit/Item", fileName = "Item")]
    public class Item : ScriptableObject
    {
        public string displayName = "Item";
        public Sprite icon;
        [Tooltip("How many fit in one slot. 1 = never stacks (weapons, key items).")]
        [Min(1)] public int maxStack = 1;

        [Header("On the floor")]
        [Tooltip("How it is picked up from the floor. A Pickup in the scene may override it.")]
        public PickupMode pickup = PickupMode.Interact;
        [Tooltip("Magnet: pulled in from this far (tiles). 0 = the Collector's magnet reach.")]
        [Min(0f)] public float magnetReach;
        [Tooltip("Magnet: flies this fast (tiles/s). 0 = the Collector's magnet speed.")]
        [Min(0f)] public float magnetSpeed;
        [Tooltip("Off: it can never be dropped from the bag or hotbar (quest items).")]
        public bool droppable = true;
        [Tooltip("What it looks like lying on the floor when dropped or spilled: a prefab (animated, glowing, any art). Empty = its icon on a plain sprite.")]
        public GameObject floorPrefab;

        public string Name => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}

using UnityEngine;

namespace TopDownKit
{
    // What an item is: a name, an icon and how many fit in one slot. Settings only; what an
    // item does when used belongs to the game (or to an add-on piece that reads it).
    [CreateAssetMenu(menuName = "TopDownKit/Item", fileName = "Item")]
    public class Item : ScriptableObject
    {
        public string displayName = "Item";
        public Sprite icon;
        [Tooltip("How many fit in one slot. 1 = never stacks (weapons, key items).")]
        [Min(1)] public int maxStack = 1;

        public string Name => string.IsNullOrEmpty(displayName) ? name : displayName;
    }
}

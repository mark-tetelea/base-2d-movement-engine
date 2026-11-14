using UnityEngine;

namespace TopDownKit
{
    // Puts an Inventory on an object: the player's bag, a chest, a shop shelf. The slots
    // are serialised, so a chest's starting contents are set in the inspector.
    public class Bag : MonoBehaviour
    {
        public Inventory inventory = new Inventory(new InventorySettings(20, 0, 0));

        public Inventory Inventory { get { inventory.Fit(); return inventory; } }

        void OnValidate() { if (inventory != null) inventory.Fit(); }
    }
}

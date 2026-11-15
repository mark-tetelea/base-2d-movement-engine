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

    public static class BagLookup
    {
        // The bag of whoever is interacting (null: none). The Interaction folder does not
        // know bags; this is how inventory targets (chests, shops, key doors) find one.
        public static Bag GetBag(this Interactor who) => who != null ? who.GetComponent<Bag>() : null;
    }
}

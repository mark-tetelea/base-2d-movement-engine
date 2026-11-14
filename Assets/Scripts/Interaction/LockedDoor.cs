using UnityEngine;

namespace TopDownKit
{
    // Add-on target: a door that opens with a key item from the user's Bag. Opening
    // switches the blockers off for good. Without the key it says so in the TextBox.
    public class LockedDoor : Interactable
    {
        [Tooltip("The item that opens it. None: it opens with no key.")]
        public Item key;
        [Tooltip("The key is used up.")]
        public bool consumeKey = true;
        [Tooltip("Solid door objects switched off when it opens (can include this object's own art).")]
        public GameObject[] blockers;
        public string lockedText = "Locked.";

        public bool IsOpen { get; private set; }

        void Reset() { verb = "Open"; }

        public override bool CanUse(Interactor who) => !IsOpen;

        public override void Use(Interactor who)
        {
            Bag bag = who != null ? who.Bag : null;
            if (key != null && (bag == null || !bag.Inventory.Has(key)))
            {
                if (TextBox.Current != null) TextBox.Current.Show(who, null, lockedText);
                return;
            }
            if (key != null && consumeKey) bag.Inventory.Remove(key, 1);
            Open();
        }

        public void Open()
        {
            IsOpen = true;
            if (blockers == null) return;
            foreach (GameObject b in blockers) if (b != null) b.SetActive(false);
        }
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // The first slots of the Bag are the hotbar (InventorySettings.hotbarSlots). Number
    // keys pick the selected slot; Q drops one of what it holds on the floor in front of
    // the player. Input arrives as data: Select and Drop (keys only when readKeyboard).
    [RequireComponent(typeof(Bag))]
    public class Hotbar : MonoBehaviour
    {
        [Tooltip("On: number keys and Q drive it. Off: something else calls Select / Drop.")]
        public bool readKeyboard = true;
        [Tooltip("How far in front (tiles) a dropped item lands.")]
        [Min(0f)] public float dropDistance = 1f;
        [Tooltip("Seconds before a dropped item can be taken again.")]
        [Min(0f)] public float dropWait = 0.5f;
        [Tooltip("Sorting order of dropped items (match the floor items).")]
        public int dropSortingOrder;

        Bag bag;
        Interactor interactor;

        public int Selected { get; private set; }
        public Inventory Inventory { get { if (bag == null) bag = GetComponent<Bag>(); return bag.Inventory; } }
        public ItemStack SelectedStack => Selected < Inventory.Size ? Inventory.slots[Selected] : default;

        public void Select(int slot)
        {
            int n = Mathf.Min(Inventory.settings.hotbarSlots, Inventory.Size);
            if (slot >= 0 && slot < n) Selected = slot;
        }

        // Drops count of the selected slot in front (or at the feet). Returns the floor item.
        public Pickup Drop(int count = 1)
        {
            ItemStack taken = Inventory.TakeFrom(Selected, count);
            if (taken.Empty) return null;
            if (interactor == null) interactor = GetComponent<Interactor>();
            Vector2 ahead = interactor != null ? interactor.FacingDirection : Vector2.down;
            Vector2 at = (Vector2)transform.position + ahead.normalized * dropDistance;
            return Pickup.Drop(taken.item, taken.count, at, dropSortingOrder, dropWait);
        }

        void Update()
        {
            if (!readKeyboard || PauseMenu.IsPaused) return;
            if (interactor == null) interactor = GetComponent<Interactor>();
            if (interactor != null && interactor.PanelOpen) return;
            Keyboard k = Keyboard.current;
            if (k == null) return;
            int n = Mathf.Min(Inventory.settings.hotbarSlots, 9);
            for (int i = 0; i < n; i++)
                if (k[Key.Digit1 + i].wasPressedThisFrame) Select(i);
            if (k.qKey.wasPressedThisFrame) Drop(1);
        }
    }
}

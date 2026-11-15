using UnityEngine;

namespace TopDownKit
{
    // The first slots of the Bag are the hotbar (InventorySettings.hotbarSlots). Number
    // keys pick the selected slot; Q drops one of what it holds on the floor in front of
    // the player (unless the item is not droppable). Input arrives as data: Select and Drop (keys only when readKeyboard).
    [RequireComponent(typeof(Bag))]
    public class Hotbar : MonoBehaviour, IHudSlots
    {
        [Tooltip("On: the Slot 1-9 and Drop controls (number keys, Q) drive it. Off: something else calls Select / Drop.")]
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
        public Pickup Drop(int count = 1) => DropSlot(Selected, count);

        // Drops count of any bag slot (the bag screen uses this too). Items with droppable
        // off stay. The floor item uses its item's pickup mode; this player cannot take it
        // back by walking or magnet until it has stepped out of reach once.
        public Pickup DropSlot(int slot, int count)
        {
            if (slot < 0 || slot >= Inventory.Size) return null;
            ItemStack s = Inventory.slots[slot];
            if (s.Empty || !s.item.droppable) return null;
            ItemStack taken = Inventory.TakeFrom(slot, count);
            if (taken.Empty) return null;
            if (interactor == null) interactor = GetComponent<Interactor>();
            Vector2 ahead = interactor != null ? interactor.FacingDirection : Vector2.down;
            Vector2 at = (Vector2)transform.position + ahead.normalized * dropDistance;
            return Pickup.Spawn(taken.item, taken.count, at, dropSortingOrder, dropWait, gameObject);
        }

        void Update()
        {
            if (!readKeyboard || GamePause.IsPaused) return;
            if (interactor == null) interactor = GetComponent<Interactor>();
            if (interactor != null && interactor.PanelOpen) return;
            int n = Mathf.Min(Inventory.settings.hotbarSlots, 9);
            for (int i = 0; i < n; i++)
                if (Controls.Pressed(Controls.Slot(i))) Select(i);
            if (Controls.Pressed(Controls.Drop)) Drop(1);
        }

        // ---- what the HUD shows (IHudSlots) ----
        public int SlotCount => Mathf.Min(Inventory.settings.hotbarSlots, Inventory.Size);
        public Sprite SlotIcon(int i) => Inventory.slots[i].Empty ? null : Inventory.slots[i].item.icon;
        public int SlotAmount(int i) => Inventory.slots[i].Empty ? 0 : Inventory.slots[i].count;
        public int SelectedSlot => Selected;
        public void ClickSlot(int i) { Select(i); }
    }
}

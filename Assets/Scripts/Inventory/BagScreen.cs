using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // Add-on on the player: the bag screen. I opens and closes it (so does the interact
    // key, like the chest screen); clicking a slot drops that whole stack on the floor in
    // front of the player through the hotbar's drop (Hotbar.DropSlot), so it uses the
    // item's pickup mode and the same "not straight back" rule. Items with droppable off
    // stay. Drawn on the kit's UI panel (KitUI). Input arrives as data: Toggle and Drop.
    [RequireComponent(typeof(Hotbar))]
    public class BagScreen : MonoBehaviour, IUsePanel
    {
        [Tooltip("On: the I key opens and closes it. Off: something else calls Toggle.")]
        public bool readKeyboard = true;
        [Tooltip("Slots per row on screen.")]
        [Min(1)] public int columns = 5;

        Hotbar hotbar;
        Interactor interactor;

        public bool IsOpen { get; private set; }
        Hotbar Hotbar { get { if (hotbar == null) hotbar = GetComponent<Hotbar>(); return hotbar; } }
        Interactor Interactor { get { if (interactor == null) interactor = GetComponent<Interactor>(); return interactor; } }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            if (IsOpen) return;
            if (Interactor != null && Interactor.PanelOpen) return;   // a chest or shop is open
            IsOpen = true;
            if (Interactor != null) Interactor.OpenPanel(this);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (Interactor != null) Interactor.ClosePanel(this);
        }

        public void UsePressed(Interactor who) { Close(); }

        // Drops the whole stack of a bag slot. Returns the floor item (null: empty or not droppable).
        public Pickup Drop(int slot) => IsOpen ? Hotbar.DropSlot(slot, Hotbar.Inventory.slots[slot].count) : null;

        void Update()
        {
            if (!readKeyboard || PauseMenu.IsPaused) return;
            Keyboard k = Keyboard.current;
            if (k != null && k.iKey.wasPressedThisFrame) Toggle();
        }

        VisualElement root, grid;
        VisualElement[] slots;

        void Build()
        {
            root = KitUI.Box(6f);
            root.name = "BagScreen";
            var title = KitUI.Label("Bag", 9, KitUI.Gold);
            title.style.marginBottom = 2;
            root.Add(title);
            grid = KitUI.Group(FlexDirection.Row);
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.width = columns * (KitUI.SlotSize + 2);
            root.Add(grid);
            var hint = KitUI.Label("click a slot to drop it   [I] close", 8, KitUI.Dim);
            hint.style.marginTop = 4;
            root.Add(hint);
            var wrap = KitUI.Group();
            KitUI.Fill(wrap);
            wrap.style.alignItems = Align.Center;
            wrap.style.justifyContent = Justify.Center;
            root.style.position = Position.Relative;
            wrap.Add(root);
            KitUI.Root.Add(wrap);
            slots = null;
        }

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18
            root.parent.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (!IsOpen) return;
            Inventory inv = Hotbar.Inventory;
            if (slots == null || slots.Length != inv.Size)
            {
                grid.Clear();
                slots = new VisualElement[inv.Size];
                for (int i = 0; i < inv.Size; i++)
                {
                    int k = i;
                    slots[i] = KitUI.Slot(() => Drop(k));
                    grid.Add(slots[i]);
                }
            }
            for (int i = 0; i < inv.Size; i++)
                KitUI.SetSlot(slots[i], inv.slots[i], false, i < inv.settings.hotbarSlots ? (i + 1).ToString() : "");
        }

        void OnDisable() { Close(); }

        void OnDestroy() { if (root != null && root.parent != null) root.parent.RemoveFromHierarchy(); }
    }
}

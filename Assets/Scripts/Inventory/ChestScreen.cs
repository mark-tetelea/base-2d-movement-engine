using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // The transfer screen between the player's Bag and a chest's Bag: click a slot to move
    // its stack to the other side, T takes everything, the interact key closes it. Every
    // move goes through the one rule (Inventory.Move). Drawn on the kit's UI panel (KitUI).
    public class ChestScreen : MonoBehaviour, IUsePanel
    {
        static ChestScreen current;
        public static ChestScreen Current => current;

        [Tooltip("Slots per row on screen.")]
        [Min(1)] public int columns = 5;

        Interactor who;
        Chest chest;

        public bool IsOpen => chest != null;
        public Chest Chest => chest;

        void OnEnable() { current = this; }
        void OnDisable() { if (current == this) current = null; }

        public void Show(Interactor interactor, Chest c)
        {
            who = interactor;
            chest = c;
            if (who != null) who.OpenPanel(this);
        }

        public void UsePressed(Interactor interactor) { Close(); }

        public void Close()
        {
            chest = null;
            if (who != null) who.ClosePanel(this);
            who = null;
        }

        // Data entry points (the buttons and keys call these; checks call them directly).
        public int Take(int slot) => IsOpen ? Inventory.Move(chest.Bag.Inventory, slot, who.Bag.Inventory) : 0;
        public int Put(int slot) => IsOpen ? Inventory.Move(who.Bag.Inventory, slot, chest.Bag.Inventory) : 0;
        public int TakeAll() => IsOpen ? Inventory.MoveAll(chest.Bag.Inventory, who.Bag.Inventory) : 0;

        void Update()
        {
            if (!IsOpen) return;
            Keyboard k = Keyboard.current;
            if (k != null && !PauseMenu.IsPaused && k.tKey.wasPressedThisFrame) TakeAll();
        }

        VisualElement root, chestGrid, bagGrid;
        Label chestName;
        VisualElement[] chestSlots, bagSlots;

        VisualElement Side(string name, out Label title, out VisualElement grid)
        {
            var side = KitUI.Group();
            side.style.marginLeft = side.style.marginRight = 6;
            title = KitUI.Label(name, 9, KitUI.Gold);
            title.style.marginBottom = 2;
            grid = KitUI.Group(FlexDirection.Row);
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.width = columns * (KitUI.SlotSize + 2);
            side.Add(title); side.Add(grid);
            return side;
        }

        void Build()
        {
            root = KitUI.Box(6f);
            root.name = "ChestScreen";
            root.style.position = Position.Absolute;
            root.style.left = 0; root.style.right = 0; root.style.top = 60;
            root.style.marginLeft = root.style.marginRight = StyleKeyword.Auto;
            root.style.alignSelf = Align.Center;
            var row = KitUI.Group(FlexDirection.Row);
            row.Add(Side("", out chestName, out chestGrid));
            row.Add(Side("You", out _, out bagGrid));
            root.Add(row);
            var hint = KitUI.Label("click a slot to move it   [T] take all   [E] close", 8, KitUI.Dim);
            hint.style.marginTop = 4;
            root.Add(hint);
            var wrap = KitUI.Group();
            KitUI.Fill(wrap);
            wrap.style.alignItems = Align.Center;
            root.style.position = Position.Relative;
            wrap.Add(root);
            KitUI.Root.Add(wrap);
            chestSlots = bagSlots = null;
        }

        VisualElement[] Fill(VisualElement grid, VisualElement[] slots, Inventory inv, bool isChest)
        {
            if (slots == null || slots.Length != inv.Size)
            {
                grid.Clear();
                slots = new VisualElement[inv.Size];
                for (int i = 0; i < inv.Size; i++)
                {
                    int k = i;
                    slots[i] = KitUI.Slot(() => { if (isChest) Take(k); else Put(k); });
                    grid.Add(slots[i]);
                }
            }
            for (int i = 0; i < inv.Size; i++)
                KitUI.SetSlot(slots[i], inv.slots[i], false, !isChest && i < inv.settings.hotbarSlots ? (i + 1).ToString() : "");
            return slots;
        }

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18
            bool show = IsOpen && who != null && who.Bag != null;
            root.parent.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            chestName.text = chest.name;
            chestSlots = Fill(chestGrid, chestSlots, chest.Bag.Inventory, true);
            bagSlots = Fill(bagGrid, bagSlots, who.Bag.Inventory, false);
        }

        void OnDestroy() { if (root != null && root.parent != null) root.parent.RemoveFromHierarchy(); }
    }
}

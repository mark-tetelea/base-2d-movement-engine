using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // The transfer screen between the player's Bag and a chest's Bag: click a slot to move
    // its stack to the other side, T takes everything, the interact key closes it. Every
    // move goes through the one rule (Inventory.Move). OnGUI placeholder (phase 6 screens).
    public class ChestScreen : MonoBehaviour, IUsePanel
    {
        static ChestScreen current;
        public static ChestScreen Current => current;

        [Tooltip("Slots per row on screen.")]
        [Min(1)] public int columns = 5;

        Interactor who;
        Chest chest;
        GUIStyle title;

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
            if (k != null && k.tKey.wasPressedThisFrame) TakeAll();
        }

        void OnGUI()
        {
            if (!IsOpen || who == null || who.Bag == null) return;
            int fs = Mathf.Max(14, Screen.height / 36);
            if (title == null) title = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            title.fontSize = fs;
            float cell = Mathf.Floor(Screen.height / 12f);
            float gap = Mathf.Floor(cell * 0.1f);
            float x0 = Screen.width * 0.5f - columns * (cell + gap) - cell * 0.5f;
            float x1 = Screen.width * 0.5f + cell * 0.5f;
            float y = Screen.height * 0.18f;
            Grid(chest.name, chest.Bag.Inventory, x0, y, cell, gap, true);
            Grid("You", who.Bag.Inventory, x1, y, cell, gap, false);
            GUI.Label(new Rect(x0, Screen.height * 0.75f, Screen.width, fs * 1.6f), "click a slot to move it   [T] take all   [E] close", title);
        }

        void Grid(string label, Inventory inv, float x, float y, float cell, float gap, bool isChest)
        {
            GUI.Label(new Rect(x, y - cell * 0.7f, cell * columns, cell * 0.7f), label, title);
            for (int i = 0; i < inv.Size; i++)
            {
                Rect r = new Rect(x + (i % columns) * (cell + gap), y + (i / columns) * (cell + gap), cell, cell);
                bool hot = !isChest && i < inv.settings.hotbarSlots;
                InventoryGui.Slot(r, inv.slots[i], false);
                if (hot) GUI.Label(new Rect(r.x + 3f, r.y, cell, cell * 0.4f), (i + 1).ToString());
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    if (isChest) Take(i); else Put(i);
                }
            }
        }
    }
}

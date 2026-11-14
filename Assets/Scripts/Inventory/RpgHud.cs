using UnityEngine;

namespace TopDownKit
{
    // The Little RPG room demo's readout: the hotbar, the coin count, the keys, and a win
    // line once the goal item is in the bag. A developer OnGUI label like ShooterHud, not
    // kit UI - the real screens are phase 6.
    public class RpgHud : MonoBehaviour
    {
        public Hotbar hotbar;
        public Interactor interactor;
        public Item coin;
        [Tooltip("Having this item wins the demo.")]
        public Item goal;

        GUIStyle style;

        public bool Won => goal != null && hotbar != null && hotbar.Inventory.Has(goal);

        void OnGUI()
        {
            if (hotbar == null) return;
            int fs = Mathf.Max(14, Screen.height / 40);
            if (style == null) style = new GUIStyle(GUI.skin.label);
            style.fontSize = fs;
            Inventory inv = hotbar.Inventory;
            float h = fs * 1.6f;
            string coins = coin != null ? "Coins " + inv.Count(coin) : "";
            GUI.Label(new Rect(10, 6, Screen.width, h), coins, style);
            GUI.Label(new Rect(10, 6 + h, Screen.width, h), "WASD move   E use   1-5 hotbar   Q drop   (chest: click / T take all)", style);

            int n = Mathf.Min(inv.settings.hotbarSlots, inv.Size);
            float cell = Mathf.Floor(Screen.height / 14f), gap = Mathf.Floor(cell * 0.1f);
            float x = (Screen.width - n * (cell + gap)) * 0.5f, y = Screen.height - cell - gap * 2f;
            bool panel = interactor != null && interactor.PanelOpen;
            if (!panel)
                for (int i = 0; i < n; i++)
                {
                    Rect r = new Rect(x + i * (cell + gap), y, cell, cell);
                    InventoryGui.Slot(r, inv.slots[i], i == hotbar.Selected);
                    GUI.Label(new Rect(r.x + 3f, r.y, cell, cell * 0.4f), (i + 1).ToString());
                }
            if (Won && !panel) GUI.Label(new Rect(Screen.width * 0.5f - 160, Screen.height * 0.25f, 600, h), "You found the crown - you win", style);
        }
    }
}

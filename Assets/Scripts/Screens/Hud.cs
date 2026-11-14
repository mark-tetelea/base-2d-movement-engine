using UnityEngine;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // The kit's one HUD: a health bar (when a Health is set), the weapon and ammo (when a
    // Gun is set), the hotbar (when a Hotbar is set; click a slot to select it), a status
    // line and a centre banner that game rules fill in (Status, Banner), and a help line.
    // Every part is optional, so each demo sets only what it has.
    public class Hud : MonoBehaviour
    {
        public Health health;
        public Gun gun;
        public Hotbar hotbar;
        [Tooltip("Hides the hotbar while this one has a panel (text box, chest) open.")]
        public Interactor interactor;
        [Tooltip("The controls, shown small along the top.")]
        [TextArea] public string help;

        // Filled in by game rules (ArenaGoal, RpgGoal, ...). Empty hides them.
        public string Status { get; set; } = "";
        public string Banner { get; set; } = "";

        VisualElement root, hpFill, hotbarRow;
        VisualElement[] slots;
        Label hpText, ammo, status, banner, helpLabel;

        void Build()
        {
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "Hud";

            // Top-left: health, then status.
            var top = KitUI.Group();
            top.style.position = Position.Absolute; top.style.left = 4; top.style.top = 4;
            var hpRow = KitUI.Group(FlexDirection.Row);
            hpRow.style.alignItems = Align.Center;
            var hpBack = new VisualElement { pickingMode = PickingMode.Ignore };
            hpBack.style.width = 64; hpBack.style.height = 6;
            hpBack.style.backgroundColor = KitUI.Ink;
            KitUI.Border(hpBack, KitUI.Edge, 1f);
            hpFill = new VisualElement { pickingMode = PickingMode.Ignore };
            hpFill.style.height = Length.Percent(100);
            hpFill.style.backgroundColor = KitUI.Red;
            hpBack.Add(hpFill);
            hpRow.Add(hpBack);
            hpText = KitUI.Label("", 8);
            hpText.style.marginLeft = 3;
            hpRow.Add(hpText);
            hpRow.style.display = health != null ? DisplayStyle.Flex : DisplayStyle.None;
            top.Add(hpRow);
            status = KitUI.Label("", 8, KitUI.Gold);
            top.Add(status);
            root.Add(top);

            helpLabel = KitUI.Label(help ?? "", 7, KitUI.Dim);
            helpLabel.style.position = Position.Absolute; helpLabel.style.right = 4; helpLabel.style.top = 4;
            helpLabel.style.unityTextAlign = TextAnchor.UpperRight;
            root.Add(helpLabel);

            // Bottom-right: weapon and ammo.
            ammo = KitUI.Label("", 9);
            ammo.style.position = Position.Absolute; ammo.style.right = 4; ammo.style.bottom = 4;
            root.Add(ammo);

            // Bottom-centre: the hotbar.
            hotbarRow = KitUI.Group(FlexDirection.Row);
            hotbarRow.style.position = Position.Absolute; hotbarRow.style.bottom = 2;
            hotbarRow.style.left = 0; hotbarRow.style.right = 0;
            hotbarRow.style.justifyContent = Justify.Center;
            root.Add(hotbarRow);

            banner = KitUI.Label("", 14, KitUI.Gold);
            banner.style.position = Position.Absolute; banner.style.left = 0; banner.style.right = 0; banner.style.top = 90;
            banner.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(banner);

            KitUI.Root.Add(root);
            slots = null;
        }

        void BuildSlots(int n)
        {
            hotbarRow.Clear();
            slots = new VisualElement[n];
            for (int i = 0; i < n; i++)
            {
                int k = i;
                slots[i] = KitUI.Slot(() => { if (hotbar != null) hotbar.Select(k); });
                hotbarRow.Add(slots[i]);
            }
        }

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18

            if (health != null)
            {
                float f = health.maxHealth > 0 ? Mathf.Clamp01((float)health.Current / health.maxHealth) : 0f;
                hpFill.style.width = Length.Percent(f * 100f);
                hpText.text = health.Current + "/" + health.maxHealth;
            }

            string a = "";
            if (gun != null && gun.Current != null)
            {
                Weapon w = gun.Current;
                a = w.name + "  " + (w.magazine <= 0 ? "inf" : gun.Reloading ? "reloading" : gun.Ammo + "/" + w.magazine);
                if (gun.Charge01 > 0f) a += "  charge " + Mathf.RoundToInt(gun.Charge01 * 100f) + "%";
            }
            ammo.text = a;

            bool panelOpen = interactor != null && interactor.PanelOpen;
            if (hotbar != null && !panelOpen)
            {
                Inventory inv = hotbar.Inventory;
                int n = Mathf.Min(inv.settings.hotbarSlots, inv.Size);
                if (slots == null || slots.Length != n) BuildSlots(n);
                for (int i = 0; i < n; i++) KitUI.SetSlot(slots[i], inv.slots[i], i == hotbar.Selected, (i + 1).ToString());
                hotbarRow.style.display = DisplayStyle.Flex;
            }
            else hotbarRow.style.display = DisplayStyle.None;

            status.text = Status ?? "";
            banner.text = panelOpen ? "" : Banner ?? "";
            helpLabel.text = help ?? "";
        }

        void OnDisable() { if (root != null) root.RemoveFromHierarchy(); root = null; }
    }
}

using UnityEngine;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // What the HUD can show, as promises other folders keep - so the Screens folder needs
    // no Combat, Inventory or Interaction folder.
    // A bar: Combat's Health.
    public interface IHudMeter { int Current { get; } int Max { get; } bool Dead { get; } }
    // A line of text: Combat's Gun (weapon and ammo).
    public interface IHudText { string HudText { get; } }
    // A row of slots: Inventory's Hotbar.
    public interface IHudSlots
    {
        int SlotCount { get; }
        Sprite SlotIcon(int i);
        int SlotAmount(int i);
        int SelectedSlot { get; }
        void ClickSlot(int i);
    }
    // Something that opens screens over the game (Interaction's Interactor): the hotbar and
    // banner hide while one is open.
    public interface IHudPanel { bool PanelOpen { get; } }

    // The kit's one HUD: a health bar (when Health is set), a text line such as the weapon
    // and ammo (when Ammo is set), the hotbar (when Hotbar is set; click a slot to select
    // it), a status line and a centre banner that game rules fill in (Status, Banner), a boss
    // bar (Boss), and a help line. Every part is optional, so each demo sets only what it has.
    // Where each part sits, and its colours and art, come from the theme (UITheme).
    public class Hud : MonoBehaviour
    {
        [Tooltip("Anything with health (Combat's Health).")]
        public MonoBehaviour health;
        [Tooltip("Anything with a HUD text line (Combat's Gun: weapon and ammo).")]
        public MonoBehaviour gun;
        [Tooltip("Anything with slots (Inventory's Hotbar).")]
        public MonoBehaviour hotbar;
        [Tooltip("Hides the hotbar while this one has a panel (text box, chest) open (Interaction's Interactor).")]
        public MonoBehaviour interactor;
        [Tooltip("The controls, shown small along the top. {Use}, {Dash}... become the keys bound to those controls; {Move} the four walk keys.")]
        [TextArea] public string help;

        static Hud current;
        // The scene's HUD (a boss in a generated room finds it here).
        public static Hud Current => current;

        // Filled in by game rules (ArenaGoal, RpgGoal, ...). Empty hides them.
        public string Status { get; set; } = "";
        public string Banner { get; set; } = "";
        // A boss bar along the top while set and alive (BossPhases fills it in).
        public IHudMeter Boss { get; set; }
        public string BossName { get; set; } = "";

        VisualElement root, hpRow, hpFill, hotbarRow, bossRow, bossFill;
        VisualElement[] slots;
        Label hpText, ammo, status, banner, helpLabel, bossLabel;
        int helpVersion = -1;
        string helpShown;

        IHudMeter Meter => health as IHudMeter;
        IHudText Text => gun as IHudText;
        IHudSlots Slots => hotbar as IHudSlots;
        IHudPanel PanelOwner => interactor as IHudPanel;

        void OnEnable() { current = this; }

        // A dragged-in object may land on the wrong component: pick the one that fits.
        void OnValidate()
        {
            health = Fit<IHudMeter>(health);
            gun = Fit<IHudText>(gun);
            hotbar = Fit<IHudSlots>(hotbar);
            interactor = Fit<IHudPanel>(interactor);
        }

        static MonoBehaviour Fit<T>(MonoBehaviour m) where T : class
        {
            if (m == null || m is T) return m;
            foreach (MonoBehaviour c in m.GetComponents<MonoBehaviour>()) if (c is T) return c;
            return null;
        }

        void Build()
        {
            UITheme t = KitUI.Theme;
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "Hud";

            // Health, then status.
            var top = KitUI.Group();
            top.AddToClassList("hud-health");
            hpRow = KitUI.Group(FlexDirection.Row);
            hpRow.style.alignItems = Align.Center;
            hpRow.Add(KitUI.Bar(t.healthBarWidth, out hpFill));
            hpText = KitUI.Label("", 8);
            hpText.style.marginLeft = 3;
            hpRow.Add(hpText);
            top.Add(hpRow);
            status = KitUI.Label("", 8, KitUI.Gold);
            top.Add(status);
            root.Add(KitUI.Placed(top, t.hudHealth));

            helpLabel = KitUI.Label(help ?? "", 7, KitUI.Dim);
            helpLabel.AddToClassList("hud-help");
            root.Add(KitUI.Placed(helpLabel, t.hudHelp));

            ammo = KitUI.Label("", 9);
            ammo.AddToClassList("hud-ammo");
            root.Add(KitUI.Placed(ammo, t.hudAmmo));

            hotbarRow = KitUI.Group(FlexDirection.Row);
            hotbarRow.AddToClassList("hud-hotbar");
            root.Add(KitUI.Placed(hotbarRow, t.hudHotbar));

            bossRow = KitUI.Group();
            bossRow.AddToClassList("hud-boss");
            bossRow.style.alignItems = Align.Center;
            bossLabel = KitUI.Label("", 8, KitUI.Gold);
            bossRow.Add(bossLabel);
            bossRow.Add(KitUI.Bar(t.bossBarWidth, out bossFill));
            root.Add(KitUI.Placed(bossRow, t.hudBoss));

            banner = KitUI.Label("", 14, KitUI.Gold);
            banner.AddToClassList("hud-banner");
            root.Add(KitUI.Placed(banner, t.hudBanner));

            KitUI.Root.Add(root);
            slots = null;
            helpVersion = -1;
        }

        void BuildSlots(int n)
        {
            hotbarRow.Clear();
            slots = new VisualElement[n];
            for (int i = 0; i < n; i++)
            {
                int k = i;
                slots[i] = KitUI.Slot(() => { if (Slots != null) Slots.ClickSlot(k); });
                hotbarRow.Add(slots[i]);
            }
        }

        // The help text with {Control} names replaced by their keys (they follow rebinds).
        public static string WithKeys(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0) return text ?? "";
            var b = new System.Text.StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf('{', i);
                int close = open >= 0 ? text.IndexOf('}', open) : -1;
                if (open < 0 || close < 0) { b.Append(text, i, text.Length - i); break; }
                b.Append(text, i, open - i);
                string id = text.Substring(open + 1, close - open - 1);
                b.Append(id == "Move" ? MoveKeys() : Controls.Describe(id));
                i = close + 1;
            }
            return b.ToString();
        }

        // {Move}: the four walk keys, "WASD" when each is one letter.
        static string MoveKeys()
        {
            string[] k = { Controls.Describe(Controls.Up), Controls.Describe(Controls.Left), Controls.Describe(Controls.Down), Controls.Describe(Controls.Right) };
            for (int i = 0; i < 4; i++) { int cut = k[i].IndexOf(" / "); if (cut > 0) k[i] = k[i].Substring(0, cut); }
            bool letters = k[0].Length == 1 && k[1].Length == 1 && k[2].Length == 1 && k[3].Length == 1;
            return string.Join(letters ? "" : "/", k);
        }

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18

            IHudMeter m = Meter;
            hpRow.style.display = m != null ? DisplayStyle.Flex : DisplayStyle.None;
            if (m != null)
            {
                float f = m.Max > 0 ? Mathf.Clamp01((float)m.Current / m.Max) : 0f;
                hpFill.style.width = Length.Percent(f * 100f);
                hpText.text = m.Current + "/" + m.Max;
            }

            IHudMeter boss = Boss;
            if (boss is Object bo && bo == null) boss = Boss = null;   // destroyed
            bool showBoss = boss != null && !boss.Dead;
            bossRow.style.display = showBoss ? DisplayStyle.Flex : DisplayStyle.None;
            if (showBoss)
            {
                bossFill.style.width = Length.Percent(Mathf.Clamp01((float)boss.Current / Mathf.Max(1, boss.Max)) * 100f);
                bossLabel.text = BossName ?? "";
            }

            ammo.text = Text != null ? Text.HudText : "";

            bool panelOpen = PanelOwner != null && PanelOwner.PanelOpen;
            IHudSlots s = Slots;
            if (s != null && !panelOpen)
            {
                int n = s.SlotCount;
                if (slots == null || slots.Length != n) BuildSlots(n);
                for (int i = 0; i < n; i++) KitUI.SetSlot(slots[i], s.SlotIcon(i), s.SlotAmount(i), i == s.SelectedSlot, (i + 1).ToString());
                hotbarRow.style.display = DisplayStyle.Flex;
            }
            else hotbarRow.style.display = DisplayStyle.None;

            status.text = Status ?? "";
            banner.text = panelOpen ? "" : Banner ?? "";
            if (helpVersion != Controls.Version || !ReferenceEquals(helpShown, help))
            {
                helpLabel.text = WithKeys(help);
                helpVersion = Controls.Version;
                helpShown = help;
            }
        }

        void OnDisable()
        {
            if (current == this) current = null;
            if (root != null) root.RemoveFromHierarchy();
            root = null;
        }
    }
}

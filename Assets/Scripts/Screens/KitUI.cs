using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // The one place the kit's screens hang from: a UI Toolkit panel built in code, sized
    // in the game's own pixels (640x360, scaled by the Pixel Perfect Camera's whole-number
    // ratio - trap 32), plus the shared bits every screen uses (a box, a button, a slot)
    // and "is the pointer over a screen?" (trap 21). Callers: Hud, PauseMenu, TextBox,
    // ChestScreen, StartMenu, the Playground picker.
    public static class KitUI
    {
        // The panel's settings and theme: Resources/KitPanel (a copy is used, so play mode
        // never writes the scale into the asset).
        const string PanelAsset = "KitPanel";
        public const int BaseWidth = 640, BaseHeight = 360;

        static UIDocument doc;
        static PanelSettings panel;

        // The root every screen adds its element to. Built on first use, rebuilt when the
        // scene (and so the panel) is gone - trap 18.
        public static VisualElement Root
        {
            get
            {
                if (doc == null || doc.rootVisualElement == null) Build();
                return doc.rootVisualElement;
            }
        }

        static void Build()
        {
            var go = new GameObject("Kit UI");
            doc = go.AddComponent<UIDocument>();
            if (panel == null)
            {
                PanelSettings src = Resources.Load<PanelSettings>(PanelAsset);
                panel = src != null ? Object.Instantiate(src) : ScriptableObject.CreateInstance<PanelSettings>();
                panel.name = "Kit panel (runtime)";
                panel.scaleMode = PanelScaleMode.ConstantPixelSize;
            }
            doc.panelSettings = panel;
            go.AddComponent<KitUIScaler>();
            VisualElement r = doc.rootVisualElement;
            r.pickingMode = PickingMode.Ignore;   // trap 17
            r.style.flexGrow = 1f;
            ApplyFont(r);
            EnsureEventSystem();
        }

        // Trap 16: no EventSystem with the Input System module = every button dead.
        static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        // Whole-number scale: the camera's pixel ratio when there is a Pixel Perfect Camera,
        // else the same rule it uses (largest whole multiple of 640x360 that fits).
        public static int Scale()
        {
            int s = Mathf.Min(Screen.width / BaseWidth, Screen.height / BaseHeight);
            Camera cam = Camera.main;
            if (cam != null)
            {
                var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
                if (ppc != null && ppc.isActiveAndEnabled && ppc.pixelRatio > 0) s = ppc.pixelRatio;
            }
            return Mathf.Max(1, s);
        }

        internal static void ApplyScale()
        {
            if (panel != null) panel.scale = Scale();
        }

        // True when the mouse is over a screen element that takes clicks. Gameplay mouse
        // input checks this so a click on a screen does not also shoot (trap 21).
        public static bool PointerOverUI
        {
            get
            {
                Mouse m = Mouse.current;
                if (m == null || doc == null || doc.rootVisualElement == null) return false;
                IPanel p = doc.rootVisualElement.panel;
                if (p == null) return false;
                Vector2 s = m.position.ReadValue();
                s.y = Screen.height - s.y;   // trap 20: ScreenToPanel does not flip Y
                Vector2 at = RuntimePanelUtils.ScreenToPanel(p, s);
                VisualElement hit = p.Pick(at);
                return hit != null && hit != doc.rootVisualElement;
            }
        }

        // --- Shared looks: the theme (Resources/KitTheme, a UITheme asset) ----------------

        const string ThemeAsset = "KitTheme";
        static UITheme theme;

        // The look in use. A game may set another theme before its screens are built.
        public static UITheme Theme
        {
            get
            {
                if (theme == null)
                {
                    theme = Resources.Load<UITheme>(ThemeAsset);
                    if (theme == null) theme = ScriptableObject.CreateInstance<UITheme>();
                }
                return theme;
            }
            set { theme = value; if (doc != null && doc.rootVisualElement != null) ApplyFont(doc.rootVisualElement); }
        }

        public static Color Ink => Theme.ink;
        public static Color Panel => Theme.panel;
        public static Color Edge => Theme.edge;
        public static Color Text => Theme.text;
        public static Color Dim => Theme.dim;
        public static Color Gold => Theme.gold;
        public static Color Red => Theme.red;
        public static Color Green => Theme.green;

        // The theme's font on the root; every label inherits it.
        static void ApplyFont(VisualElement r)
        {
            UITheme t = Theme;
            if (t.fontAsset != null) r.style.unityFontDefinition = FontDefinition.FromSDFFont(t.fontAsset);
            else if (t.font != null) r.style.unityFontDefinition = FontDefinition.FromFont(t.font);
            else r.style.unityFontDefinition = StyleKeyword.Null;
        }

        // A text size through the theme: scaled, and for a pixel font a whole multiple of its size.
        public static int FontSize(int asked)
        {
            UITheme t = Theme;
            float s = asked * t.textScale;
            if (t.pixelFontSize > 0) return Mathf.Max(1, Mathf.RoundToInt(s / t.pixelFontSize)) * t.pixelFontSize;
            return Mathf.Max(1, Mathf.RoundToInt(s));
        }

        public static Label Label(string text, int size = 9, Color? color = null)
        {
            var l = new Label(text);
            l.AddToClassList("kit-label");
            l.pickingMode = PickingMode.Ignore;
            l.style.fontSize = FontSize(size);
            l.style.color = color ?? Text;
            // A 1-pixel dark shadow so text reads over any floor.
            if (Theme.textShadow) l.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 0f, color = Ink };
            l.style.marginLeft = l.style.marginRight = l.style.marginTop = l.style.marginBottom = 0;
            l.style.paddingLeft = l.style.paddingRight = l.style.paddingTop = l.style.paddingBottom = 0;
            return l;
        }

        // A plain container that lets clicks through.
        public static VisualElement Group(FlexDirection dir = FlexDirection.Column)
        {
            var v = new VisualElement();
            v.pickingMode = PickingMode.Ignore;
            v.style.flexDirection = dir;
            return v;
        }

        // Theme art on an element: the sprite (9-sliced by its borders) if there is one,
        // else a flat fill with an edge.
        public static void Paint(VisualElement v, Sprite art, Color fill, Color edge)
        {
            if (art != null)
            {
                v.style.backgroundImage = new StyleBackground(art);
                v.style.backgroundColor = Color.clear;
                Border(v, Color.clear, 0f);
            }
            else
            {
                v.style.backgroundImage = new StyleBackground(StyleKeyword.None);
                v.style.backgroundColor = fill;
                Border(v, edge, Theme.edgeWidth);
            }
        }

        // A filled box with a 1-pixel edge (or the theme's box art). Pickable (it blocks clicks behind it).
        public static VisualElement Box(float pad = 4f)
        {
            var v = new VisualElement();
            v.AddToClassList("kit-box");
            Paint(v, Theme.box, Panel, Edge);
            v.style.paddingLeft = v.style.paddingRight = v.style.paddingTop = v.style.paddingBottom = pad;
            return v;
        }

        public static void Border(VisualElement v, Color c, float w)
        {
            v.style.borderLeftWidth = v.style.borderRightWidth = v.style.borderTopWidth = v.style.borderBottomWidth = w;
            v.style.borderLeftColor = v.style.borderRightColor = v.style.borderTopColor = v.style.borderBottomColor = c;
        }

        // A bar (health, boss): the back and its fill. Set the fill's width in percent.
        public static VisualElement Bar(int width, out VisualElement fill)
        {
            var back = new VisualElement { pickingMode = PickingMode.Ignore };
            back.AddToClassList("kit-bar");
            back.style.width = width; back.style.height = Theme.barHeight;
            Paint(back, Theme.barBack, Ink, Edge);
            fill = new VisualElement { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("kit-bar-fill");
            fill.style.height = Length.Percent(100);
            if (Theme.barFill != null) fill.style.backgroundImage = new StyleBackground(Theme.barFill);
            else fill.style.backgroundColor = Red;
            back.Add(fill);
            return back;
        }

        // Every kit button press (a click sound).
        public static event System.Action Clicked;

        public static Button Button(string text, System.Action click, int size = 9)
        {
            var b = new Button(() =>
            {
                Clicked?.Invoke();
                Cues.Raise(FeelEvent.UiClick, Vector2.zero);
                click?.Invoke();
            }) { text = text };
            b.AddToClassList("kit-button");
            b.style.fontSize = FontSize(size);
            b.style.color = Text;
            Paint(b, Theme.button, Ink, Edge);
            b.style.borderTopLeftRadius = b.style.borderTopRightRadius = b.style.borderBottomLeftRadius = b.style.borderBottomRightRadius = 0;
            b.style.marginLeft = b.style.marginRight = 0;
            b.style.marginTop = b.style.marginBottom = 1;
            b.style.paddingLeft = b.style.paddingRight = 4;
            b.style.paddingTop = b.style.paddingBottom = 1;
            return b;
        }

        // Fill the whole screen (for overlays) or pin to a corner.
        public static void Fill(VisualElement v)
        {
            v.style.position = Position.Absolute;
            v.style.left = v.style.top = v.style.right = v.style.bottom = 0;
        }

        // A full-screen layer that holds `part` at a theme place (a HUD part).
        public static VisualElement Placed(VisualElement part, UIPlace place)
        {
            var layer = Group();
            Fill(layer);
            int col = (int)place.anchor % 3, row = (int)place.anchor / 3;
            layer.style.alignItems = col == 0 ? Align.FlexStart : col == 1 ? Align.Center : Align.FlexEnd;
            layer.style.justifyContent = row == 0 ? Justify.FlexStart : row == 1 ? Justify.Center : Justify.FlexEnd;
            layer.style.paddingLeft = col == 0 ? place.offset.x : 0f;
            layer.style.paddingRight = col == 2 ? place.offset.x : 0f;
            layer.style.paddingTop = row == 0 ? place.offset.y : 0f;
            layer.style.paddingBottom = row == 2 ? place.offset.y : 0f;
            if (part is Label l)
                l.style.unityTextAlign = row == 0 ? (col == 0 ? TextAnchor.UpperLeft : col == 1 ? TextAnchor.UpperCenter : TextAnchor.UpperRight)
                                       : row == 1 ? (col == 0 ? TextAnchor.MiddleLeft : col == 1 ? TextAnchor.MiddleCenter : TextAnchor.MiddleRight)
                                       : (col == 0 ? TextAnchor.LowerLeft : col == 1 ? TextAnchor.LowerCenter : TextAnchor.LowerRight);
            layer.Add(part);
            return layer;
        }

        // --- Inventory slot: a box, the item icon at a whole-number size, the count --------

        public static int SlotSize => Theme.slotSize;

        public static VisualElement Slot(System.Action click)
        {
            var s = new VisualElement();
            s.AddToClassList("kit-slot");
            s.style.width = s.style.height = SlotSize;
            Paint(s, Theme.slot, Ink, Edge);
            s.style.marginRight = 2;
            s.style.marginBottom = 2;
            s.style.alignItems = Align.Center;
            s.style.justifyContent = Justify.Center;
            if (click == null) s.pickingMode = PickingMode.Ignore;
            else s.RegisterCallback<ClickEvent>(e => click());
            var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
            icon.style.width = icon.style.height = Theme.iconSize;
            s.Add(icon);
            var num = Label("", 7, Dim); num.name = "num";
            num.style.position = Position.Absolute; num.style.left = 1; num.style.top = 0;
            s.Add(num);
            var count = Label("", 7); count.name = "count";
            count.style.position = Position.Absolute; count.style.right = 1; count.style.bottom = 0;
            s.Add(count);
            return s;
        }

        // Shows an item (icon, amount; null = empty) in a slot, gold-edged when selected.
        public static void SetSlot(VisualElement s, Sprite icon, int amount, bool selected, string number = "")
        {
            UITheme t = Theme;
            if (selected && t.slotSelected != null) Paint(s, t.slotSelected, Ink, Gold);
            else if (!selected && t.slot != null) Paint(s, t.slot, Ink, Edge);
            else Paint(s, null, Ink, selected ? Gold : Edge);
            var iconEl = s.Q("icon");
            iconEl.style.backgroundImage = icon != null ? new StyleBackground(icon) : new StyleBackground(StyleKeyword.None);
            s.Q<Label>("num").text = number;
            s.Q<Label>("count").text = icon != null && amount > 1 ? amount.ToString() : "";
        }
    }

    // Keeps the panel's whole-number scale in step with the window (and the camera).
    // Runs while paused too: it only reads the screen.
    [AddComponentMenu("")]
    public class KitUIScaler : MonoBehaviour
    {
        void LateUpdate() { KitUI.ApplyScale(); }
    }
}

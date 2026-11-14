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

        // --- Shared looks (Endesga 32 colours) -------------------------------------------

        public static readonly Color Ink = new Color32(0x18, 0x14, 0x25, 255);
        public static readonly Color Panel = new Color32(0x26, 0x2b, 0x44, 255);   // opaque (trap 36)
        public static readonly Color Edge = new Color32(0x5a, 0x69, 0x88, 255);
        public static readonly Color Text = new Color32(0xff, 0xff, 0xff, 255);
        public static readonly Color Dim = new Color32(0x8b, 0x9b, 0xb4, 255);
        public static readonly Color Gold = new Color32(0xfe, 0xae, 0x34, 255);
        public static readonly Color Red = new Color32(0xe4, 0x3b, 0x44, 255);
        public static readonly Color Green = new Color32(0x63, 0xc7, 0x4d, 255);

        public static Label Label(string text, int size = 9, Color? color = null)
        {
            var l = new Label(text);
            l.pickingMode = PickingMode.Ignore;
            l.style.fontSize = size;
            l.style.color = color ?? Text;
            // A 1-pixel dark shadow so text reads over any floor.
            l.style.textShadow = new TextShadow { offset = new Vector2(1f, 1f), blurRadius = 0f, color = Ink };
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

        // A filled box with a 1-pixel edge. Pickable (it blocks clicks behind it).
        public static VisualElement Box(float pad = 4f)
        {
            var v = new VisualElement();
            v.style.backgroundColor = Panel;
            Border(v, Edge, 1f);
            v.style.paddingLeft = v.style.paddingRight = v.style.paddingTop = v.style.paddingBottom = pad;
            return v;
        }

        public static void Border(VisualElement v, Color c, float w)
        {
            v.style.borderLeftWidth = v.style.borderRightWidth = v.style.borderTopWidth = v.style.borderBottomWidth = w;
            v.style.borderLeftColor = v.style.borderRightColor = v.style.borderTopColor = v.style.borderBottomColor = c;
        }

        public static Button Button(string text, System.Action click, int size = 9)
        {
            var b = new Button(click) { text = text };
            b.style.fontSize = size;
            b.style.color = Text;
            b.style.backgroundColor = Ink;
            Border(b, Edge, 1f);
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

        // --- Inventory slot: a box, the item icon at a whole-number size, the count --------

        public const int SlotSize = 20;

        public static VisualElement Slot(System.Action click)
        {
            var s = new VisualElement();
            s.style.width = s.style.height = SlotSize;
            s.style.backgroundColor = Ink;
            Border(s, Edge, 1f);
            s.style.marginRight = 2;
            s.style.marginBottom = 2;
            s.style.alignItems = Align.Center;
            s.style.justifyContent = Justify.Center;
            if (click == null) s.pickingMode = PickingMode.Ignore;
            else s.RegisterCallback<ClickEvent>(e => click());
            var icon = new VisualElement { name = "icon", pickingMode = PickingMode.Ignore };
            icon.style.width = icon.style.height = 16;
            s.Add(icon);
            var num = Label("", 7, Dim); num.name = "num";
            num.style.position = Position.Absolute; num.style.left = 1; num.style.top = 0;
            s.Add(num);
            var count = Label("", 7); count.name = "count";
            count.style.position = Position.Absolute; count.style.right = 1; count.style.bottom = 0;
            s.Add(count);
            return s;
        }

        public static void SetSlot(VisualElement s, ItemStack stack, bool selected, string number = "")
        {
            Border(s, selected ? Gold : Edge, 1f);
            var icon = s.Q("icon");
            Sprite sp = stack.Empty ? null : stack.item.icon;
            icon.style.backgroundImage = sp != null ? new StyleBackground(sp) : new StyleBackground(StyleKeyword.None);
            s.Q<Label>("num").text = number;
            s.Q<Label>("count").text = !stack.Empty && stack.count > 1 ? stack.count.ToString() : "";
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

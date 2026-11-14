using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TopDownKit
{
    public enum TouchMode { Auto, On, Off }

    // Add-on: on-screen controls for phones, on the kit's panel - a move stick (left), an
    // aim stick (right; dragging it also fires, a setting), and buttons: shoot, dash,
    // interact, weapon, pause. They feed the same input-as-data entry points the keyboard
    // does (PlayerMover.SetMoveDirection, PlayerAim.Hold, PlayerShooter.SetOtherTrigger,
    // Dash.Request, Interactor), so no rule knows about touch. Auto shows them on a phone
    // or once a finger touches the screen; On forces them on (testing on PC with the
    // mouse); Off hides them. Each finger is followed by its own pointer id.
    // Runs before the player's scripts, so what it sets is used the same frame.
    [DefaultExecutionOrder(-50)]
    public class TouchControls : MonoBehaviour
    {
        public TouchMode mode = TouchMode.Auto;

        [Header("Player (any may be empty)")]
        public PlayerMover mover;
        public PlayerAim aim;
        public PlayerShooter shooter;
        public Dash dash;
        public Interactor interactor;
        public Gun gun;

        [Header("Settings")]
        [Tooltip("Dragging the aim stick past the dead zone fires.")]
        public bool aimStickFires = true;
        [Tooltip("Part of the stick's reach that does nothing (0-1).")]
        [Range(0f, 0.9f)] public float deadZone = 0.2f;
        [Tooltip("Past this part of the reach the move stick walks at full speed.")]
        [Range(0.1f, 1f)] public float fullAt = 0.8f;
        [Tooltip("Stick reach in UI pixels (640x360 screen).")]
        [Min(4f)] public float stickRadius = 26f;
        public bool showWeaponButton = true;
        public bool showPauseButton = true;

        bool touchSeen;
        bool restoreKeyboard;
        Stick move, look;
        Pad fire, dashPad, use, weapon, pause;
        VisualElement root, playArea;

        public bool Shown => mode == TouchMode.On || (mode == TouchMode.Auto && (Application.isMobilePlatform || touchSeen));
        public Vector2 MoveValue => move != null ? move.Value : Vector2.zero;
        public Vector2 AimValue => look != null ? look.Value : Vector2.zero;

        // ---- a stick: a ring and a knob; one pointer at a time -------------------------

        class Stick
        {
            public VisualElement ring, knob;
            public int pointer = -1;
            public Vector2 Value;   // -1..1, y up
            float radius;

            public Stick(VisualElement parent, float x, float y, float r)
            {
                radius = r;
                ring = new VisualElement();
                ring.style.position = Position.Absolute;
                ring.style.left = x - r; ring.style.top = y - r;
                ring.style.width = ring.style.height = r * 2f;
                Round(ring, r);
                ring.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.45f);
                KitUI.Border(ring, KitUI.Edge, 1f);
                knob = new VisualElement { pickingMode = PickingMode.Ignore };
                knob.style.position = Position.Absolute;
                float k = r * 0.45f;
                knob.style.width = knob.style.height = k * 2f;
                Round(knob, k);
                knob.style.backgroundColor = new Color(0.55f, 0.6f, 0.7f, 0.8f);
                ring.Add(knob);
                parent.Add(ring);
                Show(Vector2.zero);
                ring.RegisterCallback<PointerDownEvent>(e => { if (pointer < 0) { pointer = e.pointerId; ring.CapturePointer(e.pointerId); Set(e.localPosition); } });
                ring.RegisterCallback<PointerMoveEvent>(e => { if (e.pointerId == pointer) Set(e.localPosition); });
                ring.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == pointer) Let(); });
                ring.RegisterCallback<PointerCancelEvent>(e => { if (e.pointerId == pointer) Let(); });
                ring.RegisterCallback<PointerCaptureOutEvent>(e => Let());
            }

            void Set(Vector3 local)
            {
                Vector2 d = new Vector2(local.x - radius, radius - local.y) / radius;   // y up
                Value = Vector2.ClampMagnitude(d, 1f);
                Show(Value);
            }

            public void Let()
            {
                if (pointer >= 0 && ring.HasPointerCapture(pointer)) ring.ReleasePointer(pointer);
                pointer = -1;
                Value = Vector2.zero;
                Show(Value);
            }

            void Show(Vector2 v)
            {
                float k = radius * 0.45f;
                knob.style.left = radius - k + v.x * radius;
                knob.style.top = radius - k - v.y * radius;
            }
        }

        // ---- a round button: down while a pointer holds it -----------------------------

        class Pad
        {
            public VisualElement circle;
            public int pointer = -1;
            public bool Down => pointer >= 0;
            public bool Pressed;   // went down since the last frame

            public Pad(VisualElement parent, string text, float x, float y, float r)
            {
                circle = new VisualElement();
                circle.style.position = Position.Absolute;
                circle.style.left = x - r; circle.style.top = y - r;
                circle.style.width = circle.style.height = r * 2f;
                Round(circle, r);
                circle.style.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 0.55f);
                KitUI.Border(circle, KitUI.Edge, 1f);
                circle.style.alignItems = Align.Center;
                circle.style.justifyContent = Justify.Center;
                circle.Add(KitUI.Label(text, 8));
                parent.Add(circle);
                circle.RegisterCallback<PointerDownEvent>(e => { if (pointer < 0) { pointer = e.pointerId; Pressed = true; circle.CapturePointer(e.pointerId); Look(); } });
                circle.RegisterCallback<PointerUpEvent>(e => { if (e.pointerId == pointer) Let(); });
                circle.RegisterCallback<PointerCancelEvent>(e => { if (e.pointerId == pointer) Let(); });
                circle.RegisterCallback<PointerCaptureOutEvent>(e => Let());
            }

            public void Let()
            {
                if (pointer >= 0 && circle.HasPointerCapture(pointer)) circle.ReleasePointer(pointer);
                pointer = -1;
                Look();
            }

            void Look() { circle.style.backgroundColor = Down ? new Color(0.35f, 0.4f, 0.55f, 0.75f) : new Color(0.1f, 0.1f, 0.15f, 0.55f); }
        }

        static void Round(VisualElement v, float r)
        {
            v.style.borderTopLeftRadius = v.style.borderTopRightRadius = v.style.borderBottomLeftRadius = v.style.borderBottomRightRadius = r;
        }

        void Build()
        {
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "Touch";
            playArea = KitUI.Group();
            KitUI.Fill(playArea);
            root.Add(playArea);
            float W = KitUI.BaseWidth, H = KitUI.BaseHeight, r = stickRadius;
            move = new Stick(playArea, 14f + r, H - 14f - r, r);
            look = new Stick(playArea, W - 14f - r, H - 14f - r, r);
            fire = new Pad(playArea, "Fire", W - 14f - 2f * r - 26f, H - 14f - 2f * r - 6f, 13f);
            dashPad = new Pad(playArea, "Dash", W - 14f - 2f * r - 34f, H - 30f, 13f);
            use = new Pad(root, "Use", W - 14f - r, H - 14f - 2f * r - 24f, 13f);   // also closes panels
            weapon = showWeaponButton ? new Pad(playArea, "Gun", W - 14f - r - 34f, H - 14f - 2f * r - 34f, 11f) : null;
            pause = showPauseButton ? new Pad(root, "II", W - 14f, 12f, 9f) : null;
            KitUI.Root.Add(root);
        }

        void Update()
        {
            Touchscreen ts = Touchscreen.current;
            if (!touchSeen && ts != null && ts.primaryTouch.press.isPressed) touchSeen = true;

            if (root == null || root.panel == null) { ReleaseAll(); Build(); }   // trap 18
            bool shown = Shown && !PauseMenu.IsPaused;
            bool panel = interactor != null && interactor.PanelOpen;
            root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            playArea.style.display = shown && !panel ? DisplayStyle.Flex : DisplayStyle.None;
            if (!shown || panel)
            {
                // Nothing stays held while hidden (a finger lifted on a hidden control is never seen).
                move.Let(); look.Let(); fire.Let(); dashPad.Let(); if (weapon != null) weapon.Let();
                if (!shown) { use.Let(); if (pause != null) pause.Let(); }
            }
            Feed(shown);
        }

        void Feed(bool shown)
        {
            if (move == null) return;
            // Move: keys are switched off only while the stick is held.
            Vector2 m = Shape(move.Value, true);
            if (mover != null)
            {
                // Grid step reads a change of direction as a new press: send whole tile
                // directions so it only changes when the stick turns to another one.
                MoveSettings s = mover.CurrentSettings;
                if (s.gridStep && m != Vector2.zero) m = (Vector2)GridStep.Direction(m, s.eightDirections);
                if (move.pointer >= 0)
                {
                    if (mover.readKeyboard) { restoreKeyboard = true; mover.readKeyboard = false; }
                    mover.SetMoveDirection(m);
                }
                else if (restoreKeyboard)
                {
                    mover.SetMoveDirection(Vector2.zero);
                    mover.readKeyboard = true;
                    restoreKeyboard = false;
                }
            }

            Vector2 a = Shape(look.Value, false);
            if (aim != null) { if (a != Vector2.zero) aim.Hold(a); else aim.Release(); }
            if (shooter != null) shooter.SetOtherTrigger(shown && (fire.Down || (aimStickFires && a != Vector2.zero)));
            if (dash != null && dashPad.Pressed) dash.Request();
            if (interactor != null)
            {
                if (interactor.readKeyboard) interactor.SetOtherHeld(use.Down);
                else interactor.SetUseHeld(use.Down);
            }
            if (weapon != null && weapon.Pressed && gun != null && gun.weapons != null && gun.weapons.Length > 1)
                gun.Select((gun.CurrentIndex + 1) % gun.weapons.Length);
            if (pause != null && pause.Pressed && PauseMenu.Current != null) PauseMenu.Current.Pause();
            dashPad.Pressed = use.Pressed = fire.Pressed = false;
            if (weapon != null) weapon.Pressed = false;
            if (pause != null) pause.Pressed = false;
        }

        // Dead zone; the move stick reaches full speed at fullAt, the aim stick is a direction.
        Vector2 Shape(Vector2 v, bool isMove)
        {
            float len = v.magnitude;
            if (len <= deadZone) return Vector2.zero;
            if (!isMove) return v / len;
            float t = Mathf.Clamp01((len - deadZone) / Mathf.Max(1e-4f, fullAt - deadZone));
            return v / len * t;
        }

        void ReleaseAll()
        {
            if (move == null) return;
            move.Let(); look.Let(); fire.Let(); dashPad.Let(); use.Let();
            if (weapon != null) weapon.Let();
            if (pause != null) pause.Let();
        }

        void OnDisable()
        {
            ReleaseAll();
            Feed(false);
            if (aim != null) aim.Release();
            if (root != null) root.RemoveFromHierarchy();
            root = null;
        }
    }
}

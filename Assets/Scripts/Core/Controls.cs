using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // The one place keys and mouse buttons are read. Code asks for a control by name
    // (Controls.Pressed(Controls.Dash)); which keys that means comes from the ControlSet
    // asset (Resources/KitControls), with the player's own changes on top (Rebind, kept by
    // the Save add-on). Rules still never read keys: the player's components turn controls
    // into data (a move direction, a trigger), and touch buttons press the same controls
    // with SetHeld / Tap, so a rebind or a touch screen changes nothing in the rules.
    // While the game is paused (GamePause) only controls marked whilePaused answer; while
    // the rebind screen waits for a key (Listening) none do.
    public static class Controls
    {
        // The kit's control names (rows of the asset).
        public const string Up = "Up", Down = "Down", Left = "Left", Right = "Right";
        public const string Fire = "Fire", Reload = "Reload", NextWeapon = "NextWeapon", PrevWeapon = "PrevWeapon";
        public const string Dash = "Dash", Use = "Use", Drop = "Drop", Bag = "Bag", TakeAll = "TakeAll";
        public const string Map = "Map", Pause = "Pause";
        public static string Slot(int i) => "Slot" + (i + 1);

        const string AssetName = "KitControls";
        static ControlSet set;
        static bool looked;
        // The player's changes, by control name.
        static readonly Dictionary<string, KeyBinding> mine = new Dictionary<string, KeyBinding>();
        static readonly HashSet<string> warned = new HashSet<string>();
        // Touch buttons and other sources: held controls, and one-frame presses.
        static readonly HashSet<string> held = new HashSet<string>();
        static readonly Dictionary<string, int> tapped = new Dictionary<string, int>();
        static readonly Dictionary<string, int> heldSince = new Dictionary<string, int>();

        // Domain reload is off in this project: start every play session clean (the Save
        // add-on reads the player's keys back from its settings file).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            mine.Clear(); held.Clear(); tapped.Clear(); heldSince.Clear(); warned.Clear();
            Listening = false;
            GamePause.IsPaused = false;
            set = null; looked = false;
        }

        // True while the rebind screen waits for a key: every control reads as up.
        public static bool Listening { get; set; }

        // Bumped on every rebind (the Save add-on notices and keeps them).
        public static int Version { get; private set; }

        // The asset in use; a game may point this at another set.
        public static ControlSet Set
        {
            get
            {
                if (set == null && !looked)
                {
                    looked = true;
                    set = Resources.Load<ControlSet>(AssetName);
                    if (set == null)
                    {
                        set = ScriptableObject.CreateInstance<ControlSet>();
                        set.rows = ControlSet.KitDefaults();
                    }
                }
                return set;
            }
            set { Controls.set = value; looked = true; Version++; }
        }

        public static IReadOnlyList<ControlRow> Rows => Set.rows;

        // The keys of a control now: the player's change, else the asset's.
        public static KeyBinding Keys(string id)
        {
            if (mine.TryGetValue(id, out KeyBinding b)) return b;
            ControlRow r = Set.Find(id);
            if (r != null) return r.keys;
            if (warned.Add(id)) Debug.LogWarning("Controls: no row \"" + id + "\" in the control set; it has no keys.");
            return default;
        }

        static bool Answers(string id)
        {
            if (Listening) return false;
            if (!GamePause.IsPaused) return true;
            ControlRow r = Set.Find(id);
            return r != null && r.whilePaused;
        }

        // Down now (keys, mouse button, or a touch source holding it).
        public static bool Held(string id)
        {
            if (!Answers(id)) return false;
            if (held.Contains(id)) return true;
            KeyBinding b = Keys(id);
            return KeyHeld(b.key) || KeyHeld(b.alt) || PointerHeld(b.mouse);
        }

        // Went down this frame.
        public static bool Pressed(string id)
        {
            if (!Answers(id)) return false;
            if (tapped.TryGetValue(id, out int f) && f == Time.frameCount) return true;
            if (heldSince.TryGetValue(id, out int h) && h == Time.frameCount) return true;
            KeyBinding b = Keys(id);
            return KeyPressed(b.key) || KeyPressed(b.alt) || PointerPressed(b.mouse);
        }

        // Only the keys and mouse (not touch buttons): for "did this press start on a screen?".
        public static bool KeysHeld(string id)
        {
            if (!Answers(id)) return false;
            KeyBinding b = Keys(id);
            return KeyHeld(b.key) || KeyHeld(b.alt) || PointerHeld(b.mouse);
        }

        public static bool KeysPressed(string id)
        {
            if (!Answers(id)) return false;
            KeyBinding b = Keys(id);
            return KeyPressed(b.key) || KeyPressed(b.alt) || PointerPressed(b.mouse);
        }

        // Held by a touch button or another source (SetHeld).
        public static bool HeldElsewhere(string id) => Answers(id) && held.Contains(id);

        // The four walk controls as a direction; opposite keys cancel. Diagonals come out
        // as (1, 1); the movement rule cuts them to length 1.
        public static Vector2 Move()
        {
            float x = 0f, y = 0f;
            if (Held(Right)) x += 1f;
            if (Held(Left)) x -= 1f;
            if (Held(Up)) y += 1f;
            if (Held(Down)) y -= 1f;
            return new Vector2(x, y);
        }

        // Input as data from elsewhere (a touch button): holds a control down until released.
        public static void SetHeld(string id, bool down)
        {
            if (down)
            {
                if (held.Add(id)) heldSince[id] = Time.frameCount;
            }
            else { held.Remove(id); heldSince.Remove(id); }
        }

        // A one-frame press from elsewhere (a touch button). Read by Pressed this frame.
        public static void Tap(string id) { tapped[id] = Time.frameCount; }

        // ---- rebinding -------------------------------------------------------------------

        public static void Rebind(string id, KeyBinding keys) { mine[id] = keys; Version++; }

        public static void ResetAll() { mine.Clear(); Version++; }

        public static bool Changed(string id) => mine.ContainsKey(id);

        // The player's changes as text ("Dash=Space,None,None;..."), for a settings file.
        public static string Export()
        {
            var b = new StringBuilder();
            foreach (var kv in mine)
                b.Append(kv.Key).Append('=').Append(kv.Value.key).Append(',').Append(kv.Value.alt).Append(',').Append(kv.Value.mouse).Append(';');
            return b.ToString();
        }

        // Takes the player's changes back from Export's text. Unknown names are skipped.
        public static void Import(string text)
        {
            mine.Clear();
            if (!string.IsNullOrEmpty(text))
                foreach (string part in text.Split(';'))
                {
                    int eq = part.IndexOf('=');
                    if (eq <= 0) continue;
                    string[] v = part.Substring(eq + 1).Split(',');
                    if (v.Length != 3) continue;
                    if (Enum.TryParse(v[0], out Key k) && Enum.TryParse(v[1], out Key a) && Enum.TryParse(v[2], out PointerButton m))
                        mine[part.Substring(0, eq)] = new KeyBinding(k, a, m);
                }
            Version++;
        }

        // ---- what a binding reads --------------------------------------------------------

        public static bool KeyHeld(Key k)
        {
            Keyboard kb = Keyboard.current;
            return k != Key.None && kb != null && kb[k].isPressed;
        }

        public static bool KeyPressed(Key k)
        {
            Keyboard kb = Keyboard.current;
            return k != Key.None && kb != null && kb[k].wasPressedThisFrame;
        }

        public static bool PointerHeld(PointerButton p)
        {
            Mouse m = Mouse.current;
            if (m == null) return false;
            switch (p)
            {
                case PointerButton.Left: return m.leftButton.isPressed;
                case PointerButton.Right: return m.rightButton.isPressed;
                case PointerButton.Middle: return m.middleButton.isPressed;
                case PointerButton.WheelUp: return m.scroll.ReadValue().y > 0f;
                case PointerButton.WheelDown: return m.scroll.ReadValue().y < 0f;
                default: return false;
            }
        }

        public static bool PointerPressed(PointerButton p)
        {
            Mouse m = Mouse.current;
            if (m == null) return false;
            switch (p)
            {
                case PointerButton.Left: return m.leftButton.wasPressedThisFrame;
                case PointerButton.Right: return m.rightButton.wasPressedThisFrame;
                case PointerButton.Middle: return m.middleButton.wasPressedThisFrame;
                // The wheel has no "down": each frame it turns counts as a press.
                case PointerButton.WheelUp: return m.scroll.ReadValue().y > 0f;
                case PointerButton.WheelDown: return m.scroll.ReadValue().y < 0f;
                default: return false;
            }
        }

        // The words for a binding ("E / Up", "Left mouse"), for help lines and the rebind screen.
        public static string Describe(KeyBinding b)
        {
            var parts = new List<string>();
            if (b.key != Key.None) parts.Add(KeyName(b.key));
            if (b.alt != Key.None) parts.Add(KeyName(b.alt));
            if (b.mouse != PointerButton.None) parts.Add(PointerName(b.mouse));
            return parts.Count == 0 ? "-" : string.Join(" / ", parts);
        }

        public static string Describe(string id) => Describe(Keys(id));

        public static string KeyName(Key k)
        {
            if (k >= Key.Digit1 && k <= Key.Digit9) return ((int)(k - Key.Digit1) + 1).ToString();
            if (k == Key.Digit0) return "0";
            switch (k)
            {
                case Key.UpArrow: return "Up";
                case Key.DownArrow: return "Down";
                case Key.LeftArrow: return "Left";
                case Key.RightArrow: return "Right";
                case Key.Escape: return "Esc";
                default: return k.ToString();
            }
        }

        public static string PointerName(PointerButton p)
        {
            switch (p)
            {
                case PointerButton.Left: return "Left mouse";
                case PointerButton.Right: return "Right mouse";
                case PointerButton.Middle: return "Middle mouse";
                case PointerButton.WheelUp: return "Wheel up";
                case PointerButton.WheelDown: return "Wheel down";
                default: return "-";
            }
        }
    }

    // Whether the game is paused. Set by whatever pauses (the PauseMenu); read by every
    // control and anything that must stop with the game.
    public static class GamePause
    {
        public static bool IsPaused { get; set; }
    }
}

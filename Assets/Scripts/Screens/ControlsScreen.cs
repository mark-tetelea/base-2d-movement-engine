using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // The rebind screen, opened from the pause menu: every control of the ControlSet with
    // two buttons - its main key (or mouse button) and a second key. Click one, press the
    // new key or mouse button; Backspace clears it, Esc cancels. "Reset" goes back to the
    // asset's keys. Changes go through Controls.Rebind; the Save add-on keeps them in its
    // settings file. Runs while paused (unscaled), driven by the PauseMenu.
    public class ControlsScreen
    {
        VisualElement root, list;
        Label note;
        string waitingFor;   // control id
        bool waitingAlt;
        int waitFrom;
        public bool IsOpen { get; private set; }

        public void Open()
        {
            IsOpen = true;
            waitingFor = null;
            Build();
        }

        public void Close()
        {
            IsOpen = false;
            waitingFor = null;
            Controls.Listening = false;
            if (root != null) root.RemoveFromHierarchy();
            root = null;
        }

        public void BringToFront() { if (root != null) root.BringToFront(); }

        void Build()
        {
            if (root != null) root.RemoveFromHierarchy();
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "Controls";
            root.pickingMode = PickingMode.Position;
            root.style.backgroundColor = KitUI.Theme.overlay;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;

            var box = KitUI.Box(6f);
            var title = KitUI.Label("Controls", 12, KitUI.Gold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 3;
            box.Add(title);
            list = KitUI.Group(FlexDirection.Row);
            list.style.flexWrap = Wrap.Wrap;
            list.style.width = 2 * 226;
            box.Add(list);
            note = KitUI.Label("", 8, KitUI.Dim);
            note.style.marginTop = 3;
            box.Add(note);
            var buttons = KitUI.Group(FlexDirection.Row);
            buttons.style.justifyContent = Justify.Center;
            var reset = KitUI.Button("Reset all", () => { Controls.ResetAll(); Fill(); });
            reset.style.marginRight = 4;
            buttons.Add(reset);
            buttons.Add(KitUI.Button("Back", Close));
            box.Add(buttons);
            root.Add(box);
            KitUI.Root.Add(root);
            Fill();
        }

        void Fill()
        {
            list.Clear();
            foreach (ControlRow r in Controls.Rows)
            {
                if (r == null || string.IsNullOrEmpty(r.id)) continue;
                string id = r.id;
                KeyBinding k = Controls.Keys(id);
                var line = KitUI.Group(FlexDirection.Row);
                line.style.alignItems = Align.Center;
                line.style.width = 222;
                line.style.marginRight = 4;
                var name = KitUI.Label(string.IsNullOrEmpty(r.label) ? id : r.label, 8, Controls.Changed(id) ? KitUI.Gold : KitUI.Text);
                name.style.width = 108;
                line.Add(name);
                string main = k.key != Key.None ? Controls.KeyName(k.key) : k.mouse != PointerButton.None ? Controls.PointerName(k.mouse) : "-";
                string alt = k.alt != Key.None ? Controls.KeyName(k.alt) : "-";
                bool waitMain = waitingFor == id && !waitingAlt, waitAlt = waitingFor == id && waitingAlt;
                var a = KitUI.Button(waitMain ? "..." : main, () => Wait(id, false), 8);
                a.style.width = 62; a.style.marginRight = 2;
                var b = KitUI.Button(waitAlt ? "..." : alt, () => Wait(id, true), 8);
                b.style.width = 46;
                line.Add(a);
                line.Add(b);
                list.Add(line);
            }
            note.text = waitingFor != null ? "Press a key or mouse button.  Backspace: none.  " + Controls.KeyName(Key.Escape) + ": cancel"
                                          : "Click a key to change it.";
        }

        void Wait(string id, bool alt)
        {
            waitingFor = id;
            waitingAlt = alt;
            waitFrom = Time.frameCount + 1;   // the click that started it is not the answer
            Controls.Listening = true;
            Fill();
        }

        // Called every frame while open (by the PauseMenu).
        public void Update()
        {
            if (!IsOpen) return;
            if (root == null || root.panel == null) Build();   // trap 18
            Keyboard kb = Keyboard.current;
            if (waitingFor == null)
            {
                // The Pause key (or Esc) goes back to the menu.
                if (Controls.Pressed(Controls.Pause) || (kb != null && kb.escapeKey.wasPressedThisFrame)) Close();
                return;
            }
            if (Time.frameCount < waitFrom) return;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Stop(); return; }
            KeyBinding k = Controls.Keys(waitingFor);
            if (kb != null && kb.backspaceKey.wasPressedThisFrame)
            {
                if (waitingAlt) k.alt = Key.None; else { k.key = Key.None; k.mouse = PointerButton.None; }
                Controls.Rebind(waitingFor, k);
                Stop();
                return;
            }
            if (kb != null)
                foreach (UnityEngine.InputSystem.Controls.KeyControl c in kb.allKeys)
                {
                    if (c == null || !c.wasPressedThisFrame || c.keyCode == Key.None) continue;
                    if (waitingAlt) k.alt = c.keyCode; else { k.key = c.keyCode; k.mouse = PointerButton.None; }
                    Controls.Rebind(waitingFor, k);
                    Stop();
                    return;
                }
            if (!waitingAlt)
                foreach (PointerButton p in new[] { PointerButton.Left, PointerButton.Right, PointerButton.Middle, PointerButton.WheelUp, PointerButton.WheelDown })
                    if (Controls.PointerPressed(p))
                    {
                        k.mouse = p;
                        k.key = Key.None;
                        Controls.Rebind(waitingFor, k);
                        Stop();
                        return;
                    }
        }

        void Stop()
        {
            waitingFor = null;
            Controls.Listening = false;
            Fill();
        }
    }
}

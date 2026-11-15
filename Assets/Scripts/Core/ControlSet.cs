using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // A mouse button or the wheel, as a second way to trigger a control.
    public enum PointerButton { None, Left, Right, Middle, WheelUp, WheelDown }

    // The keys of one control: a key, a second key, and a mouse button. Any may be None.
    [Serializable]
    public struct KeyBinding
    {
        public Key key;
        public Key alt;
        public PointerButton mouse;

        public KeyBinding(Key key, Key alt = Key.None, PointerButton mouse = PointerButton.None)
        {
            this.key = key; this.alt = alt; this.mouse = mouse;
        }
    }

    // One control: the name code asks for, the words the rebind screen shows, its keys.
    [Serializable]
    public class ControlRow
    {
        [Tooltip("The name code reads it by (Controls.Pressed(\"Dash\")). Do not rename one the kit uses.")]
        public string id;
        [Tooltip("What the rebind screen calls it.")]
        public string label;
        public KeyBinding keys;
        [Tooltip("Still read while the game is paused (the pause key itself).")]
        public bool whilePaused;
    }

    // Every key the game reads, as a setting: the asset Resources/KitControls. Change a key
    // here (no code), add a row for a game's own control and read it with Controls, delete
    // the rows of a system you removed. The player's own changes (the rebind screen in the
    // pause menu) go on top and are kept by the Save add-on's settings file.
    // Callers (CLAUDE.md, three real callers): every demo - Arena shooter, Little RPG room,
    // Dungeon, Playground - reads all its keys through it.
    [CreateAssetMenu(menuName = "TopDownKit/Control Set", fileName = "KitControls")]
    public class ControlSet : ScriptableObject
    {
        public List<ControlRow> rows = new List<ControlRow>();

        public ControlRow Find(string id)
        {
            if (rows != null)
                foreach (ControlRow r in rows)
                    if (r != null && r.id == id) return r;
            return null;
        }

        // The kit's own controls with their usual keys (used to fill a new asset).
        public static List<ControlRow> KitDefaults()
        {
            ControlRow R(string id, string label, Key k, Key alt = Key.None, PointerButton m = PointerButton.None, bool paused = false)
                => new ControlRow { id = id, label = label, keys = new KeyBinding(k, alt, m), whilePaused = paused };
            var list = new List<ControlRow>
            {
                R(Controls.Up, "Walk up", Key.W, Key.UpArrow),
                R(Controls.Down, "Walk down", Key.S, Key.DownArrow),
                R(Controls.Left, "Walk left", Key.A, Key.LeftArrow),
                R(Controls.Right, "Walk right", Key.D, Key.RightArrow),
                R(Controls.Fire, "Shoot", Key.None, Key.None, PointerButton.Left),
                R(Controls.Reload, "Reload", Key.R),
                R(Controls.NextWeapon, "Next weapon", Key.None, Key.None, PointerButton.WheelDown),
                R(Controls.PrevWeapon, "Last weapon", Key.None, Key.None, PointerButton.WheelUp),
                R(Controls.Dash, "Dash", Key.Space),
                R(Controls.Use, "Use / talk", Key.E),
                R(Controls.Drop, "Drop item", Key.Q),
                R(Controls.Bag, "Bag", Key.I),
                R(Controls.TakeAll, "Take all (chest)", Key.T),
                R(Controls.Map, "Map", Key.M),
                R(Controls.Pause, "Pause", Key.Escape, Key.None, PointerButton.None, true),
            };
            for (int i = 0; i < 9; i++) list.Add(R(Controls.Slot(i), "Slot " + (i + 1), Key.Digit1 + i));
            list.Add(R("Picker", "Settings (Playground)", Key.Tab));
            list.Add(R("Knock", "Knock (Playground)", Key.K));
            return list;
        }
    }
}

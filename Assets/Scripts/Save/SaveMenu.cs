using UnityEngine;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // Save and Load buttons in the pause menu, and the slot list they open (on the kit's
    // panel, over the pause menu). A slot shows its scene and time, "empty", "broken" or
    // "from a newer version"; clicking it saves or loads. Add-on: it registers its buttons
    // with PauseMenu.AddButton, so the pause menu never knows about saving.
    [RequireComponent(typeof(SaveGame))]
    public class SaveMenu : MonoBehaviour
    {
        public bool showSave = true;
        public bool showLoad = true;

        enum Mode { Closed, Save, Load }
        Mode mode;
        SaveGame save;
        VisualElement root, list;
        Label title, note;
        string message = "";

        public bool IsOpen => mode != Mode.Closed;

        SaveGame Game { get { if (save == null) save = GetComponent<SaveGame>(); return save; } }

        void OnEnable()
        {
            if (showSave) PauseMenu.AddButton("Save", OpenSave);
            if (showLoad) PauseMenu.AddButton("Load", OpenLoad);
        }

        void OnDisable()
        {
            PauseMenu.RemoveButton("Save", OpenSave);
            PauseMenu.RemoveButton("Load", OpenLoad);
            if (root != null) root.RemoveFromHierarchy();
            root = null;
        }

        // Entry points (the buttons call these; checks call them directly).
        public void OpenSave() { mode = Mode.Save; message = Game.CannotSave(); Refresh(); }
        public void OpenLoad() { mode = Mode.Load; message = ""; Refresh(); }
        public void Close() { mode = Mode.Closed; }

        public void Pick(int slot)
        {
            if (mode == Mode.Save)
            {
                string err = Game.Save(slot);
                message = err == "" ? Game.LastNote : err;
                Refresh();
            }
            else if (mode == Mode.Load)
            {
                string err = Game.Load(slot);
                message = err;   // on success the scene reloads
                if (err == "") mode = Mode.Closed;
                else Refresh();
            }
        }

        void Build()
        {
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "SaveMenu";
            root.pickingMode = PickingMode.Position;
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            var box = KitUI.Box(8f);
            box.style.width = 200;
            title = KitUI.Label("", 12, KitUI.Gold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 4;
            box.Add(title);
            list = KitUI.Group();
            box.Add(list);
            note = KitUI.Label("", 8, KitUI.Dim);
            note.style.marginTop = 3;
            note.style.whiteSpace = WhiteSpace.Normal;
            box.Add(note);
            box.Add(KitUI.Button("Back", Close));
            root.Add(box);
            KitUI.Root.Add(root);
        }

        void Refresh()
        {
            if (root == null || root.panel == null) Build();
            title.text = mode == Mode.Save ? "Save" : "Load";
            list.Clear();
            for (int i = 0; i < Game.slots; i++)
            {
                int k = i;
                list.Add(KitUI.Button((i + 1) + "   " + Describe(i), () => Pick(k), 8));
            }
            note.text = message;
        }

        string Describe(int slot)
        {
            SaveFiles.State st = Game.Peek(slot, out SaveData d, out _);
            switch (st)
            {
                case SaveFiles.State.Empty: return "empty";
                case SaveFiles.State.Newer: return "from a newer version";
                case SaveFiles.State.Broken: return "broken";
            }
            string s = (string.IsNullOrEmpty(d.scene) ? "" : d.scene + "  ") + d.savedAt;
            return st == SaveFiles.State.FromBackup ? s + " (backup)" : s;
        }

        void LateUpdate()
        {
            if (mode != Mode.Closed && !PauseMenu.IsPaused) mode = Mode.Closed;
            if (root == null || root.panel == null) { if (mode == Mode.Closed) return; Build(); Refresh(); }
            root.style.display = mode != Mode.Closed ? DisplayStyle.Flex : DisplayStyle.None;
            if (mode != Mode.Closed) root.BringToFront();
        }
    }
}

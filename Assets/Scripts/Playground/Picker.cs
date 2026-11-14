using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace TopDownKit.Playground
{
    // Playground only: a mouse-driven panel (Tab shows / hides it) that switches every
    // choice the kit has, live. Each row is a name and a button showing the value in use;
    // a click moves to the next value. It only writes the kit's own settings - nothing
    // here is a rule. Also places the aim marker.
    public class Picker : MonoBehaviour
    {
        public PlayerMover mover;
        public MoveFeel[] feels;
        [Tooltip("Grid-step feels the Grid step row cycles through (after off).")]
        public MoveFeel[] gridFeels;
        public Dash dash;
        public PlayerFacing facing;
        public PlayerAim aim;
        public CameraRig rig;
        public Gun gun;
        public Interactor interactor;
        public Collector collector;
        [Tooltip("Reach values (tiles) the reach row cycles through.")]
        public float[] reaches = { 1f, 1.5f, 2.5f };
        [Tooltip("Magnet reach values (tiles) the magnet row cycles through; 0 = off.")]
        public float[] magnets = { 0f, 2.5f, 5f };
        [Tooltip("Aim assist strengths the aim assist row cycles through; 0 = off.")]
        public float[] assists = { 0f, 0.35f, 0.7f };
        public FeelPlayer feel;
        public DayNight dayNight;
        [Tooltip("Master volumes the volume row cycles through.")]
        public float[] volumes = { 1f, 0.5f, 0.25f };
        public bool startOpen = true;

        [Tooltip("A small sprite placed along the aim, so aim is visible.")]
        public Transform aimMarker;
        [Tooltip("How far from the player the marker sits, tiles.")]
        public float markerDistance = 1.5f;

        struct Row { public string name; public Func<string> value; public Action next; public Button button; }
        readonly List<Row> rows = new List<Row>();
        VisualElement root, box;
        bool open;

        public bool IsOpen => open;
        public int RowCount => rows.Count;

        void Awake() { open = startOpen; }

        static string Num(float v) => v.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        static int Next(int i, int n) => n <= 0 ? 0 : (i + 1) % n;
        static int IndexOf(float[] a, float v)
        {
            for (int i = 0; i < a.Length; i++) if (Mathf.Abs(a[i] - v) < 1e-3f) return i;
            return -1;
        }

        void Add(string name, Func<bool> has, Func<string> value, Action next)
        {
            if (!has()) return;
            rows.Add(new Row { name = name, value = value, next = next });
        }

        void Rows()
        {
            rows.Clear();
            Add("Feel", () => mover != null && feels != null && feels.Length > 0,
                () => mover.feel != null ? mover.feel.name : "-",
                () => mover.feel = feels[Next(Array.IndexOf(feels, mover.feel), feels.Length)]);
            Add("Grid step", () => mover != null && gridFeels != null && gridFeels.Length > 0,
                () => mover.feel != null && mover.feel.settings.gridStep ? mover.feel.name : "off", NextGrid);
            Add("Dash (Space)", () => dash != null, () => dash.enabled ? "on" : "off", () => dash.enabled = !dash.enabled);
            Add("Facings", () => facing != null, () => ((int)facing.settings.directions).ToString(),
                () => facing.settings.directions = facing.settings.directions == FacingCount.Eight ? FacingCount.Four : FacingCount.Eight);
            Add("Face towards", () => facing != null, () => facing.settings.faceTowards.ToString(),
                () => facing.settings.faceTowards = facing.settings.faceTowards == FaceTowards.Walk ? FaceTowards.Aim : FaceTowards.Walk);
            Add("Aim from", () => aim != null, () => aim.aimFrom.ToString(),
                () => aim.aimFrom = aim.aimFrom == AimFrom.Mouse ? AimFrom.Walk : AimFrom.Mouse);
            Add("Camera", () => rig != null, () => rig.layout.ToString(),
                () => rig.SetLayout((CameraLayout)(((int)rig.layout + 1) % 3)));
            Add("Weapon", () => gun != null && gun.weapons != null && gun.weapons.Length > 0,
                () => gun.Current != null ? gun.Current.name : "-",
                () => gun.Select(Next(gun.CurrentIndex, gun.weapons.Length)));
            Add("Aim assist", () => gun != null && assists.Length > 0,
                () => gun.aimAssist.strength <= 0f ? "off" : Num(gun.aimAssist.strength),
                () => gun.aimAssist.strength = assists[Next(IndexOf(assists, gun.aimAssist.strength), assists.Length)]);
            Add("Best target", () => interactor != null, () => interactor.settings.best.ToString(),
                () => interactor.settings.best = interactor.settings.best == BestTarget.Facing ? BestTarget.Closest : BestTarget.Facing);
            Add("Use", () => interactor != null, () => interactor.settings.use.ToString(),
                () => interactor.settings.use = interactor.settings.use == UseMode.Press ? UseMode.Hold : UseMode.Press);
            Add("Reach", () => interactor != null && reaches.Length > 0, () => Num(interactor.settings.reach) + " tiles",
                () => interactor.settings.reach = reaches[Next(IndexOf(reaches, interactor.settings.reach), reaches.Length)]);
            Add("Magnet", () => collector != null && magnets.Length > 0,
                () => collector.settings.magnetReach <= 0f ? "off" : Num(collector.settings.magnetReach) + " tiles",
                () => collector.settings.magnetReach = magnets[Next(IndexOf(magnets, collector.settings.magnetReach), magnets.Length)]);
            Add("Hit-stop", () => feel != null, () => feel.hitStop ? "on" : "off", () => feel.hitStop = !feel.hitStop);
            Add("Particles", () => feel != null, () => feel.effects ? "on" : "off", () => feel.effects = !feel.effects);
            Add("Sounds", () => feel != null, () => feel.sounds ? "on" : "off", () => feel.sounds = !feel.sounds);
            Add("Volume", () => feel != null && volumes.Length > 0, () => Mathf.RoundToInt(feel.master * 100f) + "%",
                () => feel.master = volumes[Next(IndexOf(volumes, feel.master), volumes.Length)]);
            Add("Day/night", () => dayNight != null, () => dayNight.mode.ToString(),
                () => dayNight.mode = (DayNightMode)(((int)dayNight.mode + 1) % 3));
            Add("Room", () => rig != null && rig.target != null, RoomText, ClearRoom);
        }

        // off -> each grid feel -> off (back to the last free feel).
        MoveFeel lastFree;
        void NextGrid()
        {
            int i = Array.IndexOf(gridFeels, mover.feel);
            if (i < 0 && mover.feel != null && !mover.feel.settings.gridStep) lastFree = mover.feel;
            if (i + 1 < gridFeels.Length) mover.feel = gridFeels[i + 1];
            else mover.feel = lastFree != null ? lastFree : feels != null && feels.Length > 0 ? feels[0] : mover.feel;
        }

        string RoomText()
        {
            Room r = Room.At(rig.target.position);
            if (r == null) return "-";
            RoomLock l = r.GetComponent<RoomLock>();
            string s = l == null ? "" : l.Cleared ? " (cleared)" : l.Locked ? " LOCKED - click" : "";
            return r.name + s;
        }

        void ClearRoom()
        {
            Room r = Room.At(rig.target.position);
            RoomLock l = r != null ? r.GetComponent<RoomLock>() : null;
            if (l != null) l.Clear();
        }

        // Entry points: Click(row) is what a button does; checks call it directly (trap 39).
        public void Click(int row) { if (row >= 0 && row < rows.Count) rows[row].next(); }
        public string RowName(int row) => rows[row].name;
        public string RowValue(int row) => rows[row].value();
        public void Toggle() { open = !open; }

        void Build()
        {
            Rows();
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "Picker";
            box = KitUI.Box(4f);
            box.style.position = Position.Absolute;
            box.style.left = 4; box.style.top = 22;
            box.style.width = 150;
            var title = KitUI.Label("Settings  [Tab] hide", 8, KitUI.Dim);
            title.style.marginBottom = 2;
            box.Add(title);
            for (int i = 0; i < rows.Count; i++)
            {
                int k = i;
                var line = KitUI.Group(FlexDirection.Row);
                line.style.alignItems = Align.Center;
                var name = KitUI.Label(rows[i].name, 8, KitUI.Dim);
                name.style.width = 58;
                line.Add(name);
                Row r = rows[i];
                r.button = KitUI.Button("", () => Click(k), 8);
                r.button.style.flexGrow = 1f;
                r.button.style.unityTextAlign = TextAnchor.MiddleLeft;
                rows[i] = r;
                line.Add(r.button);
                box.Add(line);
            }
            root.Add(box);
            KitUI.Root.Add(root);
        }

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && !PauseMenu.IsPaused && k.tabKey.wasPressedThisFrame) Toggle();
        }

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18
            box.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            if (open)
                foreach (Row r in rows) r.button.text = r.value();
            if (aimMarker != null && aim != null)
                aimMarker.position = aim.transform.position + (Vector3)(aim.Direction * markerDistance);
        }

        void OnDisable() { if (root != null) root.RemoveFromHierarchy(); root = null; }
    }
}

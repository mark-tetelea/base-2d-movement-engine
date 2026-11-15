using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace TopDownKit
{
    public enum MapMode { Off, Corner, FullOnKey }

    // Add-on: a map of the level's Rooms (hand-placed or generated) on the kit's panel.
    // Off; Corner = a small map in the corner that follows the player (the key or a click
    // on it opens the full map); FullOnKey = nothing until the key opens the full map.
    // Rooms appear once visited (a setting) - Room.Visited, which saves keep. Shows the
    // player, and optionally enemies and items inside rooms that are shown. Looks only:
    // it reads positions every frame and changes nothing in the game.
    public class Minimap : MonoBehaviour
    {
        public MapMode mode = MapMode.Corner;
        [Tooltip("Who the map follows and marks rooms visited for. Empty = the main camera's CameraRig target.")]
        public Transform player;
        [Tooltip("On: a room shows only once the player has been in it. Off: every room shows.")]
        public bool revealOnVisit = true;
        public bool showEnemies = true;
        public bool showItems = true;
        [Tooltip("Health team drawn as enemies.")]
        public int enemyTeam = 1;

        [Header("Look (UI pixels)")]
        [Tooltip("Corner map: UI pixels from the top (below the HUD's help line).")]
        public float cornerTop = 24f;
        [Tooltip("Corner map size.")]
        public Vector2 cornerSize = new Vector2(96f, 56f);
        [Tooltip("Corner map: UI pixels per tile.")]
        [Min(0.1f)] public float cornerScale = 1f;
        [Tooltip("Largest full map size; it shrinks to fit the rooms.")]
        public Vector2 fullSize = new Vector2(560f, 300f);

        static Color RoomSeen => KitUI.Theme.mapRoomSeen;
        static Color RoomHere => KitUI.Theme.mapRoomHere;
        static Color RoomEdge => KitUI.Theme.mapRoomEdge;

        bool full;
        VisualElement root, corner, cornerContent, fullBox, fullContent;
        readonly List<VisualElement> roomPool = new List<VisualElement>();
        readonly List<VisualElement> dotPool = new List<VisualElement>();
        int roomsUsed, dotsUsed;

        public bool FullOpen => full && mode != MapMode.Off;
        public bool CornerShown => mode == MapMode.Corner && !FullOpen;

        // Entry points (the key, a click on the corner map, touch).
        public void ToggleFull() { if (mode != MapMode.Off) full = !full; }

        Transform Player
        {
            get
            {
                if (player != null) return player;
                Camera cam = Camera.main;
                CameraRig rig = cam != null ? cam.GetComponent<CameraRig>() : null;
                return rig != null ? rig.target : null;
            }
        }

        void Update()
        {
            Transform p = Player;
            if (p != null)
            {
                Room r = Room.At(p.position);
                if (r != null) r.Visited = true;
            }
            if (GamePause.IsPaused || mode == MapMode.Off) { if (mode == MapMode.Off) full = false; return; }
            if (Controls.Pressed(Controls.Map)) ToggleFull();
        }

        void Build()
        {
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "Minimap";

            corner = KitUI.Box(0f);
            corner.style.position = Position.Absolute;
            corner.style.right = 4; corner.style.top = cornerTop;
            corner.style.width = cornerSize.x; corner.style.height = cornerSize.y;
            corner.style.overflow = Overflow.Hidden;
            corner.style.backgroundColor = KitUI.Ink;
            corner.RegisterCallback<ClickEvent>(e => ToggleFull());
            cornerContent = KitUI.Group();
            cornerContent.style.position = Position.Absolute;
            corner.Add(cornerContent);
            root.Add(corner);

            var wrap = KitUI.Group();
            KitUI.Fill(wrap);
            wrap.style.alignItems = Align.Center;
            wrap.style.justifyContent = Justify.Center;
            fullBox = KitUI.Box(0f);
            fullBox.style.backgroundColor = KitUI.Ink;
            fullBox.style.overflow = Overflow.Hidden;
            fullBox.RegisterCallback<ClickEvent>(e => ToggleFull());
            fullContent = KitUI.Group();
            fullContent.style.position = Position.Absolute;
            fullBox.Add(fullContent);
            wrap.Add(fullBox);
            root.Add(wrap);
            KitUI.Root.Add(root);
            roomPool.Clear(); dotPool.Clear();
        }

        bool Shown(Room r) => !revealOnVisit || r.Visited;

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18
            bool cornerOn = CornerShown && !GamePause.IsPaused;
            bool fullOn = FullOpen && !GamePause.IsPaused;
            corner.style.display = cornerOn ? DisplayStyle.Flex : DisplayStyle.None;
            fullBox.parent.style.display = fullOn ? DisplayStyle.Flex : DisplayStyle.None;
            if (!cornerOn && !fullOn) return;

            Transform p = Player;
            Vector2 at = p != null ? (Vector2)p.position : Vector2.zero;
            Room here = Room.At(at);

            // What is drawn, in world tiles.
            bool any = false;
            Rect world = default;
            foreach (Room r in Room.All)
            {
                if (r == null || !Shown(r)) continue;
                world = any ? Union(world, r.Bounds) : r.Bounds;
                any = true;
            }
            if (!any) world = new Rect(at - Vector2.one * 10f, Vector2.one * 20f);

            VisualElement content;
            float scale;
            Vector2 topLeft;   // world point at the content's top-left
            if (fullOn)
            {
                content = fullContent;
                scale = Mathf.Min(fullSize.x / Mathf.Max(1f, world.width), fullSize.y / Mathf.Max(1f, world.height));
                scale = scale >= 1f ? Mathf.Floor(scale) : scale;   // whole pixels when it can
                fullBox.style.width = Mathf.Ceil(world.width * scale) + 8;
                fullBox.style.height = Mathf.Ceil(world.height * scale) + 8;
                topLeft = new Vector2(world.xMin - 4f / scale, world.yMax + 4f / scale);
            }
            else
            {
                content = cornerContent;
                scale = cornerScale;
                topLeft = at + new Vector2(-cornerSize.x * 0.5f, cornerSize.y * 0.5f) / scale;
            }

            roomsUsed = dotsUsed = 0;
            foreach (Room r in Room.All)
            {
                if (r == null || !Shown(r)) continue;
                Rect b = r.Bounds;
                VisualElement e = Take(roomPool, ref roomsUsed, content, false);
                Place(e, new Vector2(b.xMin, b.yMax), b.size, topLeft, scale);
                e.style.backgroundColor = r == here ? RoomHere : RoomSeen;
            }
            foreach (VisualElement e in roomPool.GetRange(roomsUsed, roomPool.Count - roomsUsed)) e.style.display = DisplayStyle.None;

            // Items and enemies come from MapDots (Pickup and Health add themselves), so the
            // map needs neither the Inventory nor the Combat folder.
            foreach (IMapDot d in MapDots.All)
            {
                if (d == null || (d is Object o && o == null) || !d.DotShown) continue;
                bool item = d.DotKind == MapDots.Item;
                if (item ? !showItems : (!showEnemies || d.DotKind != enemyTeam)) continue;
                Vector2 at2 = d.DotPosition;
                if (Visible(at2)) Dot(content, at2, 2f, item ? KitUI.Gold : KitUI.Red, topLeft, scale);
            }
            if (p != null) Dot(content, at, 3f, KitUI.Text, topLeft, scale);
            for (int i = dotsUsed; i < dotPool.Count; i++) dotPool[i].style.display = DisplayStyle.None;
        }

        bool Visible(Vector2 point)
        {
            Room r = Room.At(point);
            return r != null && Shown(r);
        }

        static Rect Union(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));

        // World (tiles, y up) -> content pixels (y down), whole pixels.
        static void Place(VisualElement e, Vector2 worldTopLeft, Vector2 sizeTiles, Vector2 topLeft, float scale)
        {
            e.style.left = Mathf.Round((worldTopLeft.x - topLeft.x) * scale);
            e.style.top = Mathf.Round((topLeft.y - worldTopLeft.y) * scale);
            e.style.width = Mathf.Max(1f, Mathf.Round(sizeTiles.x * scale));
            e.style.height = Mathf.Max(1f, Mathf.Round(sizeTiles.y * scale));
        }

        void Dot(VisualElement content, Vector2 world, float px, Color c, Vector2 topLeft, float scale)
        {
            VisualElement e = Take(dotPool, ref dotsUsed, content, true);
            e.style.left = Mathf.Round((world.x - topLeft.x) * scale - px * 0.5f);
            e.style.top = Mathf.Round((topLeft.y - world.y) * scale - px * 0.5f);
            e.style.width = e.style.height = px;
            e.style.backgroundColor = c;
        }

        // A pooled element in content (moved there if it was in the other map).
        static VisualElement Take(List<VisualElement> pool, ref int used, VisualElement content, bool dot)
        {
            if (used == pool.Count)
            {
                var e = new VisualElement { pickingMode = PickingMode.Ignore };
                e.style.position = Position.Absolute;
                if (!dot) KitUI.Border(e, RoomEdge, 1f);
                pool.Add(e);
            }
            VisualElement v = pool[used++];
            if (v.parent != content) content.Add(v);
            v.style.display = DisplayStyle.Flex;
            if (dot) v.BringToFront();
            return v;
        }

        void OnDisable() { if (root != null) root.RemoveFromHierarchy(); root = null; }
    }
}

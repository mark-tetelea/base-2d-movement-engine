using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TopDownKit
{
    // Goes on a floor tilemap painted with GroundTiles (plain tiles there count as normal
    // ground). Each fixed step it finds who stands on which tile (anything with a Health,
    // at its GridStep.StandPoint), runs crumbling tiles through the one ground rule, and
    // hurts whoever stands on deadly ground. It keeps the state of each tile (warning,
    // broken, closed) and shows it through the tile's art.
    //
    // Blocking is done with ordinary colliders, so free movement, grid steps and bullets
    // need no ground code of their own: two invisible tilemaps next to this one hold a
    // collider block on every tile that blocks walking - "Solid" for tiles that stop
    // bullets too, "Low" (marked ShootOver) for tiles bullets fly over.
    //
    // Temporary ground (PaintFor): another GroundTile laid over a plain tile for a while -
    // a trail of red behind an enemy, a short-lived wall. While it lasts the tile's rules
    // are the temporary tile's; its art is drawn on a third runtime tilemap above this
    // one, fading out, and the ground underneath comes back unchanged.
    [RequireComponent(typeof(Tilemap))]
    public class GroundMap : MonoBehaviour
    {
        struct Cell { public GroundState state; public int stepsLeft; }
        struct Temp { public GroundTile tile; public int stepsLeft, steps; public Health owner; }

        static readonly List<GroundMap> all = new List<GroundMap>();
        public static IReadOnlyList<GroundMap> All => all;

        Tilemap map, solid, low, overlay;
        TilemapCollider2D solidCollider, lowCollider;
        CompositeCollider2D solidShape, lowShape;
        Tile block;
        bool ready, collidersDirty;

        readonly Dictionary<Vector3Int, Cell> cells = new Dictionary<Vector3Int, Cell>();   // crumbling, not Normal
        readonly HashSet<Vector3Int> closed = new HashSet<Vector3Int>();
        readonly HashSet<Vector3Int> nowClosed = new HashSet<Vector3Int>();
        readonly Dictionary<Health, int> hurtSteps = new Dictionary<Health, int>();
        readonly List<Vector3Int> stepped = new List<Vector3Int>();
        readonly List<Vector3Int> work = new List<Vector3Int>();
        readonly List<Vector3Int> doorWork = new List<Vector3Int>();
        readonly Dictionary<Vector3Int, Temp> temps = new Dictionary<Vector3Int, Temp>();
        readonly List<Vector3Int> tempWork = new List<Vector3Int>();
        readonly Dictionary<GroundTile, Tile> overlayTiles = new Dictionary<GroundTile, Tile>();

        public Tilemap Tilemap { get { if (map == null) map = GetComponent<Tilemap>(); return map; } }

        void OnEnable() { if (!all.Contains(this)) all.Add(this); }
        void OnDisable() { all.Remove(this); ready = false; }

        void Start()
        {
            solid = Mirror("Ground blocks (runtime)", false, out solidCollider, out solidShape);
            low = Mirror("Ground low blocks (runtime)", true, out lowCollider, out lowShape);
            overlay = Overlay();
            foreach (Vector3Int c in Tilemap.cellBounds.allPositionsWithin)
                if (Tilemap.GetTile(c) is GroundTile) ApplyBlock(c);
            ready = true;
            RefreshDoors();
            FlushColliders();
        }

        // ---- Queries -------------------------------------------------------------------

        public Vector3Int CellAt(Vector2 world) => Tilemap.WorldToCell(world);
        // The tile in force: a temporary one while it lasts, else the painted one.
        public GroundTile TileAt(Vector3Int cell) =>
            temps.TryGetValue(cell, out Temp t) ? t.tile : Tilemap.GetTile(cell) as GroundTile;

        // The running map with ground at this point, or null.
        public static GroundMap Under(Vector2 world)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].ready && all[i].Tilemap.HasTile(all[i].CellAt(world))) return all[i];
            return null;
        }

        // Lays `tile` over the cell for `seconds` (painting again restarts the time). Only
        // on plain ground: a tile with no rules of its own that does not crumble and is no
        // door, so a trail never fills a hole or opens a door. `owner` (may be null) is never
        // hurt by it - an enemy does not die in its own trail. Returns whether it was laid.
        public bool PaintFor(Vector3Int cell, GroundTile tile, float seconds, Health owner = null)
        {
            if (!ready || tile == null || seconds <= 0f) return false;
            if (!temps.ContainsKey(cell))
            {
                TileBase under = Tilemap.GetTile(cell);
                if (under == null) return false;
                if (under is GroundTile g && (g.Crumbles || g.isDoor || !Plain(g.rules))) return false;
                if (State(cell) != GroundState.Normal) return false;
            }
            int steps = Ground.Steps(seconds, Time.fixedDeltaTime);
            bool changed = !temps.TryGetValue(cell, out Temp old) || old.tile != tile;
            temps[cell] = new Temp { tile = tile, stepsLeft = steps, steps = steps, owner = owner };
            if (changed)
            {
                overlay.SetTile(cell, OverlayTile(tile));
                ApplyBlock(cell);
            }
            overlay.SetColor(cell, tile.color);
            FlushColliders();
            return true;
        }

        static bool Plain(GroundRules r) => !r.blocksWalking && !r.kills && r.damagePerSecond <= 0;

        public GroundState State(Vector3Int cell)
        {
            if (closed.Contains(cell)) return GroundState.Closed;
            return cells.TryGetValue(cell, out Cell x) ? x.state : GroundState.Normal;
        }

        // The rules in force on a tile now (plain or empty tiles: none).
        public GroundRules RulesAt(Vector3Int cell)
        {
            GroundTile t = TileAt(cell);
            return t == null ? default : t.RulesIn(State(cell));
        }

        // The state of a tile for its art; Normal where no GroundMap runs (edit mode).
        public static GroundState StateAt(Tilemap tilemap, Vector3Int cell)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i].Tilemap == tilemap) return all[i].State(cell);
            return GroundState.Normal;
        }

        // Where anything with a Health stands.
        public static Vector2 StandPoint(Component who)
        {
            Rigidbody2D b = who.GetComponent<Rigidbody2D>();
            return b != null ? GridStep.StandPoint(b) : (Vector2)who.transform.position;
        }

        // ---- The fixed step ------------------------------------------------------------

        void FixedUpdate()
        {
            if (!ready) return;
            float dt = Time.fixedDeltaTime;
            IReadOnlyList<Health> healths = Health.All;

            // Who stepped on a crumbling tile that is still whole.
            stepped.Clear();
            for (int i = healths.Count - 1; i >= 0; i--)
            {
                Health h = healths[i];
                if (h == null || h.Dead) continue;
                Vector3Int c = CellAt(StandPoint(h));
                GroundTile t = TileAt(c);
                if (t != null && t.Crumbles && State(c) == GroundState.Normal && !stepped.Contains(c)) stepped.Add(c);
            }

            // Crumbling tiles: every tile already counting down, plus the new ones.
            work.Clear();
            work.AddRange(cells.Keys);
            foreach (Vector3Int c in stepped) if (!cells.ContainsKey(c)) work.Add(c);
            foreach (Vector3Int c in work)
            {
                if (closed.Contains(c)) continue;
                cells.TryGetValue(c, out Cell cell);
                GroundState was = cell.state;
                cell.state = Ground.Crumble(was, ref cell.stepsLeft, stepped.Contains(c), TileAt(c), dt);
                if (cell.state == GroundState.Normal) cells.Remove(c); else cells[c] = cell;
                if (cell.state != was) Changed(c);
            }

            // Deadly ground, after this step's breaks.
            for (int i = healths.Count - 1; i >= 0; i--)
            {
                Health h = healths[i];
                if (h == null) continue;
                Vector3Int at = CellAt(StandPoint(h));
                GroundRules r = RulesAt(at);
                bool own = temps.TryGetValue(at, out Temp tt) && tt.owner == h;
                if (h.Dead || own || !Ground.Hurts(r)) { hurtSteps.Remove(h); continue; }
                hurtSteps.TryGetValue(h, out int left);
                Ground.Hurt(h, r, ref left, dt);
                hurtSteps[h] = left;
            }

            // Temporary ground: count down, fade, give the cell back.
            tempWork.Clear();
            tempWork.AddRange(temps.Keys);
            foreach (Vector3Int c in tempWork)
            {
                Temp t = temps[c];
                if (--t.stepsLeft <= 0)
                {
                    temps.Remove(c);
                    overlay.SetTile(c, null);
                    ApplyBlock(c);
                    continue;
                }
                temps[c] = t;
                Color col = t.tile.color;
                col.a *= 0.25f + 0.75f * t.stepsLeft / t.steps;
                overlay.SetColor(c, col);
            }

            FlushColliders();
        }

        // ---- Doors ---------------------------------------------------------------------

        // A TileDoor was switched on or off: every running map works out its closed tiles.
        public static void DoorsChanged()
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i] != null && all[i].ready) { all[i].RefreshDoors(); all[i].FlushColliders(); }
        }

        // A door tile is closed while an active TileDoor covers it.
        void RefreshDoors()
        {
            nowClosed.Clear();
            IReadOnlyList<TileDoor> doors = TileDoor.All;
            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i] == null) continue;
                Vector3Int o = CellAt(doors[i].transform.position);
                for (int x = 0; x < doors[i].size.x; x++)
                    for (int y = 0; y < doors[i].size.y; y++)
                    {
                        Vector3Int c = new Vector3Int(o.x + x, o.y + y, 0);
                        GroundTile t = TileAt(c);
                        if (t != null && t.isDoor) nowClosed.Add(c);
                    }
            }
            doorWork.Clear();
            foreach (Vector3Int c in closed) if (!nowClosed.Contains(c)) doorWork.Add(c);
            foreach (Vector3Int c in nowClosed) if (!closed.Contains(c)) doorWork.Add(c);
            closed.Clear();
            closed.UnionWith(nowClosed);
            foreach (Vector3Int c in doorWork) Changed(c);
        }

        // ---- Colliders and art ---------------------------------------------------------

        void Changed(Vector3Int c)
        {
            ApplyBlock(c);
            Tilemap.RefreshTile(c);
        }

        void ApplyBlock(Vector3Int c)
        {
            if (solid == null || low == null) return;   // being unloaded
            GroundRules r = RulesAt(c);
            solid.SetTile(c, r.blocksWalking && r.blocksBullets ? block : null);
            low.SetTile(c, r.blocksWalking && !r.blocksBullets ? block : null);
            collidersDirty = true;
        }

        // Colliders follow at once, not at the end of the frame, so the next physics step
        // and any grid-step check already see them.
        void FlushColliders()
        {
            if (!collidersDirty || solidCollider == null || lowCollider == null) return;
            collidersDirty = false;
            solidCollider.ProcessTilemapChanges();
            lowCollider.ProcessTilemapChanges();
            solidShape.GenerateGeometry();
            lowShape.GenerateGeometry();
        }

        // The art of temporary ground: a tilemap drawn just above this one.
        Tilemap Overlay()
        {
            var go = new GameObject("Ground overlay (runtime)");
            go.layer = gameObject.layer;
            go.transform.SetParent(transform.parent, false);
            go.transform.localPosition = transform.localPosition;
            go.transform.localRotation = transform.localRotation;
            go.transform.localScale = transform.localScale;
            Tilemap tm = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            var mine = GetComponent<TilemapRenderer>();
            if (mine != null)
            {
                r.sortingLayerID = mine.sortingLayerID;
                r.sortingOrder = mine.sortingOrder + 1;
                r.sharedMaterial = mine.sharedMaterial;
            }
            return tm;
        }

        // A plain runtime Tile with the ground tile's art, its colour free to fade.
        Tile OverlayTile(GroundTile g)
        {
            if (overlayTiles.TryGetValue(g, out Tile t) && t != null) return t;
            t = ScriptableObject.CreateInstance<Tile>();
            t.name = g.name + " (overlay)";
            t.sprite = g.sprite;
            t.color = g.color;
            t.flags = TileFlags.None;
            t.colliderType = Tile.ColliderType.None;
            t.hideFlags = HideFlags.DontSave;
            overlayTiles[g] = t;
            return t;
        }

        Tilemap Mirror(string name, bool shootOver, out TilemapCollider2D col, out CompositeCollider2D shape)
        {
            if (block == null)
            {
                block = ScriptableObject.CreateInstance<Tile>();
                block.name = "Ground block (runtime)";
                block.colliderType = Tile.ColliderType.Grid;
                block.hideFlags = HideFlags.DontSave;
            }
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(transform.parent, false);
            go.transform.localPosition = transform.localPosition;
            go.transform.localRotation = transform.localRotation;
            go.transform.localScale = transform.localScale;
            Tilemap tm = go.AddComponent<Tilemap>();
            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            col = go.AddComponent<TilemapCollider2D>();
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
            shape = go.AddComponent<CompositeCollider2D>();
            shape.geometryType = CompositeCollider2D.GeometryType.Polygons;
            if (shootOver) go.AddComponent<ShootOver>();
            return tm;
        }
    }
}

// One-off setup for phase 11 (EDIT mode, after crate.png is exported and imported):
//     unity command eval_file --file Tools/measure/setup-phase11.cs
// Prefabs: Coin and Heart pickups, a destructible Crate (Health + Drops), seven room pieces
// for the generator (Start, Pillars, Storeroom, Hall 2x1, Pool, Tower 1x2, Boss). Scene:
// Assets/Scenes/Dungeon.unity, copied once from ArenaShooter, its rooms and tiles removed,
// a RoomGenerator and a DungeonGoal added. Adds Dungeon to the build list and the start menu.
// Safe to re-run: prefabs are rebuilt, scene objects updated.
if (EditorApplication.isPlaying) return "exit play mode first";
if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty) return "the open scene has unsaved changes; save or close it first";
string log = "";
const string scenePath = "Assets/Scenes/Dungeon.unity";
if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
{
    AssetDatabase.CopyAsset("Assets/Scenes/ArenaShooter.unity", scenePath);
    log += "copied ArenaShooter; ";
}
// Trap 73: open the scene first, then load assets.
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }
void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
Folder("Assets/Prefabs", "Pickups"); Folder("Assets/Prefabs", "World"); Folder("Assets/Prefabs", "Rooms");

var coinItem = Load<TopDownKit.Item>("Assets/Data/Items/Coin.asset");
var heartSprite = Load<Sprite>("Assets/Art/Generated/heart.png");
var crateSprite = Load<Sprite>("Assets/Art/Generated/crate.png");
var floorTile = Load<UnityEngine.Tilemaps.TileBase>("Assets/Art/Generated/Tiles/Floor.asset");
var wallTile = Load<UnityEngine.Tilemaps.TileBase>("Assets/Art/Generated/Tiles/Wall.asset");
var blue = Load<TopDownKit.GroundTile>("Assets/Data/Tiles/Blue.asset");
var red = Load<TopDownKit.GroundTile>("Assets/Data/Tiles/Red.asset");
var door = Load<TopDownKit.GroundTile>("Assets/Data/Tiles/Door.asset");
var chaser = Load<GameObject>("Assets/Prefabs/Enemies/Chaser.prefab");
var shooter = Load<GameObject>("Assets/Prefabs/Enemies/Shooter.prefab");
var wanderer = Load<GameObject>("Assets/Prefabs/Enemies/Wanderer.prefab");
var boss = Load<GameObject>("Assets/Prefabs/Enemies/Boss.prefab");

var player = GameObject.Find("Player");
int order = player.GetComponentInChildren<SpriteRenderer>().sortingOrder;
var floorMap = GameObject.Find("Floor").GetComponent<UnityEngine.Tilemaps.Tilemap>();
var wallMap = GameObject.Find("Walls").GetComponent<UnityEngine.Tilemaps.Tilemap>();
int floorOrder = floorMap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder;
int wallOrder = wallMap.GetComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder;

GameObject Save(GameObject go, string path) { var p = PrefabUtility.SaveAsPrefabAsset(go, path); UnityEngine.Object.DestroyImmediate(go); return p; }

// ---- Pickups and the crate ----------------------------------------------------------
GameObject PickupPrefab(string name, Sprite sprite, int coins, int heal)
{
    var go = new GameObject(name);
    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order - 1;
    var p = go.AddComponent<TopDownKit.Pickup>();
    p.mode = TopDownKit.PickupChoice.FromItem; p.centre = Vector2.zero; p.verb = "Take"; p.promptOffset = new Vector2(0f, 1f);
    p.stack = coins > 0 ? new TopDownKit.ItemStack(coinItem, coins) : new TopDownKit.ItemStack(null, 0);
    p.heal = heal; p.onlyWhenHurt = true; p.waitBeforeCollect = 0.3f;
    return Save(go, "Assets/Prefabs/Pickups/" + name + ".prefab");
}
var coinPrefab = PickupPrefab("Coin", coinItem.icon, 1, 0);
var heartPrefab = PickupPrefab("Heart", heartSprite, 0, 1);

GameObject crate;
{
    var go = new GameObject("Crate");
    var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = crateSprite; sr.sortingOrder = order;
    go.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);   // no body: a static obstacle
    var h = go.AddComponent<TopDownKit.Health>();
    h.maxHealth = 3; h.team = 2; h.invulnerableTime = 0.05f; h.flashTime = 0.08f; h.blinkWhileInvulnerable = false;
    h.shakeOnHit = 0f; h.destroyOnDeath = true;
    var d = go.AddComponent<TopDownKit.Drops>();
    d.table = new[] {
        new TopDownKit.Drops.Line { prefab = coinPrefab, weight = 5f, count = 1 },
        new TopDownKit.Drops.Line { prefab = coinPrefab, weight = 2f, count = 3 },
        new TopDownKit.Drops.Line { prefab = heartPrefab, weight = 1.5f, count = 1 } };
    d.nothingWeight = 3f; d.rolls = 1; d.scatter = 0.35f;
    crate = Save(go, "Assets/Prefabs/World/Crate.prefab");
}

// ---- Room pieces --------------------------------------------------------------------
// Coordinates are tiles from the piece's bottom-left corner; one cell is 40 x 22 tiles.
const int CW = 40, CH = 22;
TopDownKit.DoorSpot D(TopDownKit.DoorSide s, int x, int y) => new TopDownKit.DoorSpot(s, new Vector2Int(x, y));
var N = TopDownKit.DoorSide.North; var E = TopDownKit.DoorSide.East; var S = TopDownKit.DoorSide.South; var W = TopDownKit.DoorSide.West;
var all4 = new[] { D(N, 0, 0), D(E, 0, 0), D(S, 0, 0), D(W, 0, 0) };

GameObject root = null; int pw = 0, ph = 0;
UnityEngine.Tilemaps.Tilemap pf = null, pwall = null;
var waves = new System.Collections.Generic.Dictionary<string, GameObject>();

UnityEngine.Tilemaps.Tilemap Map(string name, int sortOrder)
{
    var go = new GameObject(name);
    go.transform.SetParent(root.transform, false);
    go.transform.localPosition = new Vector3(-pw / 2f, -ph / 2f, 0f);
    var m = go.AddComponent<UnityEngine.Tilemaps.Tilemap>();
    go.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>().sortingOrder = sortOrder;
    return m;
}
void Begin(string name, int cx, int cy, TopDownKit.DoorSpot[] doors, bool locks)
{
    root = new GameObject(name);
    pw = cx * CW; ph = cy * CH;
    root.AddComponent<Grid>();
    root.AddComponent<TopDownKit.Room>().size = new Vector2(pw, ph);
    var rp = root.AddComponent<TopDownKit.RoomPiece>();
    rp.cells = new Vector2Int(cx, cy); rp.cellTiles = new Vector2Int(CW, CH); rp.doors = doors;
    pf = Map("Floor", floorOrder); pwall = Map("Walls", wallOrder);
    rp.floor = pf; rp.walls = pwall;
    for (int x = 0; x < pw; x++)
        for (int y = 0; y < ph; y++)
        {
            bool edge = x == 0 || y == 0 || x == pw - 1 || y == ph - 1;
            if (edge) pwall.SetTile(new Vector3Int(x, y, 0), wallTile); else pf.SetTile(new Vector3Int(x, y, 0), floorTile);
        }
    waves.Clear();
    if (locks) { var l = root.AddComponent<TopDownKit.RoomLock>(); l.doors = new GameObject[0]; root.AddComponent<TopDownKit.RoomWaves>(); }
}
void Block(int x0, int y0, int w, int h)
{
    for (int x = x0; x < x0 + w; x++)
        for (int y = y0; y < y0 + h; y++) { pwall.SetTile(new Vector3Int(x, y, 0), wallTile); pf.SetTile(new Vector3Int(x, y, 0), null); }
}
void Ground(UnityEngine.Tilemaps.TileBase t, int x0, int y0, int w, int h)
{
    for (int x = x0; x < x0 + w; x++)
        for (int y = y0; y < y0 + h; y++) pf.SetTile(new Vector3Int(x, y, 0), t);
}
Vector3 Local(float x, float y) => new Vector3(x - pw / 2f, y - ph / 2f, 0f);
void Crate(int x, int y)
{
    var c = (GameObject)PrefabUtility.InstantiatePrefab(crate, root.transform);
    c.transform.localPosition = Local(x + 0.5f, y + 0.5f);
}
void Enemy(string wave, GameObject prefab, float x, float y)
{
    if (!waves.TryGetValue(wave, out var w)) { w = new GameObject(wave); w.transform.SetParent(root.transform, false); waves[wave] = w; }
    var e = (GameObject)PrefabUtility.InstantiatePrefab(prefab, w.transform);
    e.transform.localPosition = Local(x, y);
}
GameObject End()
{
    var rw = root.GetComponent<TopDownKit.RoomWaves>();
    if (rw != null)
    {
        var list = new System.Collections.Generic.List<GameObject>(waves.Values);
        list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        foreach (var w in list) w.SetActive(false);
        rw.waves = list.ToArray();
    }
    string name = root.name;
    var p = Save(root, "Assets/Prefabs/Rooms/" + name + ".prefab");
    log += name + "; ";
    return p;
}

Begin("Start", 1, 1, all4, false);
Crate(5, 4); Crate(6, 4); Crate(5, 5); Crate(33, 17); Crate(34, 17); Crate(34, 16);
var start = End();

Begin("Pillars", 1, 1, all4, true);
Block(10, 5, 2, 2); Block(28, 5, 2, 2); Block(10, 15, 2, 2); Block(28, 15, 2, 2);
Crate(15, 10); Crate(24, 11);
Enemy("Wave 1", chaser, 8, 11); Enemy("Wave 1", chaser, 31, 11); Enemy("Wave 1", chaser, 20, 17);
Enemy("Wave 2", shooter, 12, 17); Enemy("Wave 2", shooter, 27, 4); Enemy("Wave 2", chaser, 20, 5);
var pillars = End();

Begin("Storeroom", 1, 1, all4, true);
for (int x = 6; x <= 9; x++) Crate(x, 5);
for (int x = 30; x <= 33; x++) Crate(x, 16);
Crate(14, 14); Crate(15, 14); Crate(14, 15); Crate(25, 6); Crate(26, 6); Crate(26, 7);
Enemy("Wave 1", wanderer, 10, 11); Enemy("Wave 1", wanderer, 30, 11); Enemy("Wave 1", wanderer, 20, 16);
Enemy("Wave 2", chaser, 8, 16); Enemy("Wave 2", chaser, 32, 5);
var storeroom = End();

Begin("Hall", 2, 1, new[] { D(W, 0, 0), D(E, 1, 0), D(N, 0, 0), D(N, 1, 0), D(S, 0, 0), D(S, 1, 0) }, true);
Block(38, 6, 4, 10);
Crate(55, 4); Crate(56, 4); Crate(24, 17); Crate(25, 17);
Enemy("Wave 1", chaser, 15, 6); Enemy("Wave 1", chaser, 15, 16); Enemy("Wave 1", chaser, 65, 6); Enemy("Wave 1", chaser, 65, 16);
Enemy("Wave 2", shooter, 30, 11); Enemy("Wave 2", shooter, 50, 11); Enemy("Wave 2", chaser, 70, 11);
var hall = End();

Begin("Pool", 1, 1, all4, true);
Ground(blue, 15, 8, 10, 6);
Ground(red, 3, 3, 3, 3); Ground(red, 34, 16, 3, 3);
Crate(8, 17); Crate(31, 4);
Enemy("Wave 1", shooter, 20, 17); Enemy("Wave 1", shooter, 20, 4); Enemy("Wave 1", wanderer, 8, 11);
Enemy("Wave 2", chaser, 32, 6); Enemy("Wave 2", chaser, 6, 16); Enemy("Wave 2", chaser, 32, 11);
var pool = End();

Begin("Tower", 1, 2, new[] { D(S, 0, 0), D(N, 0, 1), D(W, 0, 0), D(W, 0, 1), D(E, 0, 0), D(E, 0, 1) }, true);
Block(12, 12, 2, 2); Block(26, 12, 2, 2); Block(12, 30, 2, 2); Block(26, 30, 2, 2);
Block(8, 21, 9, 2); Block(24, 21, 9, 2);
Crate(5, 38); Crate(34, 5);
Enemy("Wave 1", chaser, 10, 6); Enemy("Wave 1", chaser, 30, 6); Enemy("Wave 1", chaser, 20, 38);
Enemy("Wave 2", shooter, 8, 35); Enemy("Wave 2", shooter, 32, 35); Enemy("Wave 2", chaser, 20, 8);
var tower = End();

Begin("Boss", 1, 1, all4, true);
Block(8, 5, 2, 2); Block(30, 5, 2, 2); Block(8, 15, 2, 2); Block(30, 15, 2, 2);
Enemy("Wave 1", boss, 20, 15);
var bossRoom = End();

// ---- The scene ----------------------------------------------------------------------
foreach (var name in new[] { "Rooms", "Warps", "Dungeon" })
{
    var g = GameObject.Find(name);
    if (g != null) { UnityEngine.Object.DestroyImmediate(g); log += "removed " + name + "; "; }
}
floorMap.ClearAllTiles(); wallMap.ClearAllTiles();
Get<TopDownKit.GroundMap>(floorMap.gameObject);

var bag = Get<TopDownKit.Bag>(player);
Get<TopDownKit.Collector>(player);
EditorUtility.SetDirty(bag);

var hudGo = GameObject.Find("HUD");
foreach (var ag in hudGo.GetComponents<TopDownKit.ArenaGoal>()) UnityEngine.Object.DestroyImmediate(ag);
var hud = hudGo.GetComponent<TopDownKit.Hud>();

var genGo = new GameObject("Dungeon");
var gen = genGo.AddComponent<TopDownKit.RoomGenerator>();
gen.startPiece = start.GetComponent<TopDownKit.RoomPiece>();
gen.endPiece = bossRoom.GetComponent<TopDownKit.RoomPiece>();
var middle = new[] { pillars, storeroom, hall, pool, tower };
gen.pieces = new TopDownKit.RoomPiece[middle.Length];
for (int i = 0; i < middle.Length; i++) gen.pieces[i] = middle[i].GetComponent<TopDownKit.RoomPiece>();
gen.roomCount = 7; gen.seed = 0; gen.doorWidth = 3;
gen.floor = floorMap; gen.walls = wallMap; gen.doorTile = door;
gen.player = player.transform; gen.hud = hud;

var goal = Get<TopDownKit.DungeonGoal>(hudGo);
goal.hud = hud; goal.player = player.GetComponent<TopDownKit.Health>(); goal.generator = gen; goal.bag = bag; goal.coin = coinItem;
EditorUtility.SetDirty(goal); EditorUtility.SetDirty(hud);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

// Build list: Dungeon after the others; the start menu gets a button.
var listB = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
if (!listB.Exists(s => s.path == scenePath)) { listB.Add(new EditorBuildSettingsScene(scenePath, true)); EditorBuildSettings.scenes = listB.ToArray(); log += "added to build list; "; }

var menuScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/StartMenu.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var menu = UnityEngine.Object.FindFirstObjectByType<TopDownKit.StartMenu>();
if (System.Array.IndexOf(menu.scenes, "Dungeon") < 0)
{
    var ls = new System.Collections.Generic.List<string>(menu.labels) { "Dungeon" }; menu.labels = ls.ToArray();
    var ss = new System.Collections.Generic.List<string>(menu.scenes) { "Dungeon" }; menu.scenes = ss.ToArray();
    EditorUtility.SetDirty(menu);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(menuScene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(menuScene);
    log += "start menu button; ";
}
string build = ""; foreach (var s in EditorBuildSettings.scenes) build += System.IO.Path.GetFileNameWithoutExtension(s.path) + ",";
return log + "build: " + build;

// One-off setup for phase 13b, the item pickup pass (EDIT mode):
//     unity command eval_file --file Tools/measure/setup-phase13b.cs
// Items: each item's pickup mode (Coin magnet, Gem magnet with its own longer reach,
// Potion walk, Key and Crown interact; the Crown cannot be dropped - the RPG's goal).
// Room pieces (Dungeon): Chasers, Shooters and Wanderers drop coins and hearts (Drops).
// Playground: a row of one item per mode in room A (Potion walk, Gem magnet, Crown
// interact, a Coin overridden to interact), wanderers in D drop coins and hearts, the bag
// screen on the player, help text. LittleRpg: the hermit's chest spills its coins, the
// bag screen on the player, help text.
// Safe to re-run: objects are found by name and updated.
if (EditorApplication.isPlaying) return "exit play mode first";
if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty) return "the open scene has unsaved changes; save or close it first";
string log = "";

T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }

// ---- items ------------------------------------------------------------------------------
TopDownKit.Item Item(string n, TopDownKit.PickupMode mode, float reach, float speed, bool droppable)
{
    var it = Load<TopDownKit.Item>("Assets/Data/Items/" + n + ".asset");
    it.pickup = mode; it.magnetReach = reach; it.magnetSpeed = speed; it.droppable = droppable;
    EditorUtility.SetDirty(it);
    return it;
}
var coin = Item("Coin", TopDownKit.PickupMode.Magnet, 0f, 0f, true);
var gem = Item("Gem", TopDownKit.PickupMode.Magnet, 4.5f, 6f, true);
var potion = Item("Potion", TopDownKit.PickupMode.Walk, 0f, 0f, true);
var key = Item("Key", TopDownKit.PickupMode.Interact, 0f, 0f, true);
var crown = Item("Crown", TopDownKit.PickupMode.Interact, 0f, 0f, false);
AssetDatabase.SaveAssets();
var heart = Load<GameObject>("Assets/Prefabs/Pickups/Heart.prefab");

TopDownKit.Drops.Line[] EnemyTable() => new[] {
    new TopDownKit.Drops.Line { item = coin, weight = 4f, count = 1 },
    new TopDownKit.Drops.Line { item = coin, weight = 1.5f, count = 3 },
    new TopDownKit.Drops.Line { prefab = heart, weight = 1.5f, count = 1 } };
void EnemyDrops(GameObject e)
{
    var d = Get<TopDownKit.Drops>(e);
    d.table = EnemyTable(); d.nothingWeight = 3f; d.rolls = 1; d.scatter = 0.35f; d.seed = 0; d.itemSortingOrder = 1;
    EditorUtility.SetDirty(d);
}

// ---- room pieces (Dungeon) --------------------------------------------------------------
foreach (string g in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Rooms" }))
{
    string path = AssetDatabase.GUIDToAssetPath(g);
    var root = PrefabUtility.LoadPrefabContents(path);
    int n = 0;
    foreach (var b in root.GetComponentsInChildren<TopDownKit.EnemyBrain>(true))
    {
        if (b.GetComponent<TopDownKit.BossPhases>() != null) continue;   // the boss drops nothing
        EnemyDrops(b.gameObject); n++;
    }
    if (n > 0) { PrefabUtility.SaveAsPrefabAsset(root, path); log += System.IO.Path.GetFileNameWithoutExtension(path) + " " + n + " enemies; "; }
    PrefabUtility.UnloadPrefabContents(root);
}

// ---- scenes -----------------------------------------------------------------------------
UnityEngine.SceneManagement.Scene Open(string n) => UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/" + n + ".unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
void Save(UnityEngine.SceneManagement.Scene s) { UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(s); UnityEditor.SceneManagement.EditorSceneManager.SaveScene(s); }
void Help(string have, string add)
{
    foreach (var h in UnityEngine.Object.FindObjectsByType<TopDownKit.Hud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        if (h.help != null && !h.help.Contains(add.Trim())) { h.help = h.help.Replace(have, have + add); EditorUtility.SetDirty(h); }
}

// Playground
{
    var s = Open("Playground");
    var player = GameObject.Find("Player");
    Get<TopDownKit.BagScreen>(player);
    var parent = GameObject.Find("Playground").transform;
    var modes = GameObject.Find("Pickup modes");
    if (modes == null) { modes = new GameObject("Pickup modes"); modes.transform.SetParent(parent, false); }
    void Place(string name, TopDownKit.Item item, Vector2 at, TopDownKit.PickupChoice choice)
    {
        Transform t = modes.transform.Find(name);
        GameObject go = t != null ? t.gameObject : new GameObject(name);
        go.transform.SetParent(modes.transform, false);
        go.transform.position = at;
        var sr = Get<SpriteRenderer>(go); sr.sprite = item.icon; sr.sortingOrder = 1;
        var p = Get<TopDownKit.Pickup>(go);
        p.stack = new TopDownKit.ItemStack(item, 1); p.mode = choice;
        p.verb = "Take"; p.centre = Vector2.zero; p.promptOffset = new Vector2(0f, 1f);
        EditorUtility.SetDirty(go);
    }
    Place("Potion (walk)", potion, new Vector2(3f, -8f), TopDownKit.PickupChoice.FromItem);
    Place("Gem (magnet, own reach)", gem, new Vector2(6f, -8f), TopDownKit.PickupChoice.FromItem);
    Place("Crown (interact, no drop)", crown, new Vector2(9f, -8f), TopDownKit.PickupChoice.FromItem);
    Place("Coin (override: interact)", coin, new Vector2(12f, -8f), TopDownKit.PickupChoice.Interact);
    foreach (var b in UnityEngine.Object.FindObjectsByType<TopDownKit.EnemyBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        if (b.name.StartsWith("Wanderer")) { EnemyDrops(b.gameObject); log += "Playground " + b.name + " drops; "; }
    Help("Tab settings", "  I bag  Q drop");
    Save(s);
    log += "Playground saved; ";
}

// LittleRpg
{
    var s = Open("LittleRpg");
    Get<TopDownKit.BagScreen>(GameObject.Find("Player"));
    foreach (var c in UnityEngine.Object.FindObjectsByType<TopDownKit.Chest>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        if (c.name == "Hermit chest") { c.spillOnOpen = true; c.spillRadius = 1.2f; c.spillSortingOrder = 1; EditorUtility.SetDirty(c); log += "hermit chest spills; "; }
    Help("Q drop", "  I bag");
    Save(s);
    log += "LittleRpg saved; ";
}
return log;

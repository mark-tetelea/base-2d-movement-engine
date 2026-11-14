// One-off setup for phase 13 (EDIT mode, after potion.png and gem.png are exported):
//     unity command eval_file --file Tools/measure/setup-phase13.cs
// Assets: Potion and Gem items, the RPG shop's stock, Buy / Sell / Save slots in KitFeel.
// Scenes (all four demos): a "Save" object (SaveGame + SaveMenu: Save / Load in the pause
// menu), a "Minimap" and "Touch controls" wired to the player. LittleRpg: a Shopkeeper in
// room A and a ShopScreen. Playground: picker rows for the map and touch.
// Safe to re-run: objects are found by name and updated.
if (EditorApplication.isPlaying) return "exit play mode first";
if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty) return "the open scene has unsaved changes; save or close it first";
string log = "";

T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }
void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var col); return col; }

// ---- assets ------------------------------------------------------------------------------
AssetDatabase.ImportAsset("Assets/Art/Generated/potion.png");
AssetDatabase.ImportAsset("Assets/Art/Generated/gem.png");
TopDownKit.Item MakeItem(string name, string shown, string icon, int stack)
{
    string path = "Assets/Data/Items/" + name + ".asset";
    var it = AssetDatabase.LoadAssetAtPath<TopDownKit.Item>(path);
    if (it == null) { it = ScriptableObject.CreateInstance<TopDownKit.Item>(); AssetDatabase.CreateAsset(it, path); }
    it.displayName = shown; it.icon = Load<Sprite>(icon); it.maxStack = stack;
    EditorUtility.SetDirty(it);
    return it;
}
var potion = MakeItem("Potion", "Potion", "Assets/Art/Generated/potion.png", 10);
var gem = MakeItem("Gem", "Gem", "Assets/Art/Generated/gem.png", 5);

Folder("Assets/Data", "Shops");
var stock = AssetDatabase.LoadAssetAtPath<TopDownKit.ShopStock>("Assets/Data/Shops/RpgShop.asset");
if (stock == null) { stock = ScriptableObject.CreateInstance<TopDownKit.ShopStock>(); AssetDatabase.CreateAsset(stock, "Assets/Data/Shops/RpgShop.asset"); }
stock.lines = new[] {
    new TopDownKit.ShopStock.Line { item = potion, price = 4, amount = 5 },
    new TopDownKit.ShopStock.Line { item = gem, price = 15, amount = 2 } };
EditorUtility.SetDirty(stock);

var feelSet = Load<TopDownKit.FeelSet>("Assets/Data/Feel/KitFeel.asset");
{
    var slots = new System.Collections.Generic.List<TopDownKit.FeelSlot>(feelSet.slots);
    feelSet.Find(TopDownKit.FeelEvent.Pickup, out var pick);
    feelSet.Find(TopDownKit.FeelEvent.UiClick, out var click);
    void Slot(TopDownKit.FeelEvent id, TopDownKit.FeelSlot from, float volume, float jitter)
    {
        if (slots.Exists(s => s.id == id)) return;
        slots.Add(new TopDownKit.FeelSlot { id = id, clips = from.clips, volume = volume, pitchJitter = jitter, effect = null, effectLife = 0f, hitStop = 0f });
        log += "feel slot " + id + "; ";
    }
    Slot(TopDownKit.FeelEvent.Buy, pick, 0.6f, 0.05f);
    Slot(TopDownKit.FeelEvent.Sell, pick, 0.5f, 0.2f);
    Slot(TopDownKit.FeelEvent.Save, click, 0.7f, 0f);
    feelSet.slots = slots.ToArray();
    EditorUtility.SetDirty(feelSet);
}
AssetDatabase.SaveAssets();

var allItems = new System.Collections.Generic.List<TopDownKit.Item>();
foreach (string g in AssetDatabase.FindAssets("t:TopDownKit.Item", new[] { "Assets/Data/Items" }))
    allItems.Add(AssetDatabase.LoadAssetAtPath<TopDownKit.Item>(AssetDatabase.GUIDToAssetPath(g)));
allItems.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
log += allItems.Count + " items; ";

// ---- scenes ------------------------------------------------------------------------------
void SetUp(string path, TopDownKit.MapMode map, bool mapHelp)
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
    var coin = Load<TopDownKit.Item>("Assets/Data/Items/Coin.asset");   // trap 73: after opening
    var items = new System.Collections.Generic.List<TopDownKit.Item>();
    foreach (var i in allItems) items.Add(AssetDatabase.LoadAssetAtPath<TopDownKit.Item>(AssetDatabase.GetAssetPath(i)));
    var player = GameObject.Find("Player");
    if (player == null) throw new System.Exception("no Player in " + path);

    var mapGo = GameObject.Find("Minimap") ?? new GameObject("Minimap");
    var mm = Get<TopDownKit.Minimap>(mapGo);
    mm.mode = map; mm.player = player.transform;
    EditorUtility.SetDirty(mm);

    var touchGo = GameObject.Find("Touch controls") ?? new GameObject("Touch controls");
    var tc = Get<TopDownKit.TouchControls>(touchGo);
    tc.mode = TopDownKit.TouchMode.Auto;
    tc.mover = player.GetComponent<TopDownKit.PlayerMover>();
    tc.aim = player.GetComponent<TopDownKit.PlayerAim>();
    tc.shooter = player.GetComponent<TopDownKit.PlayerShooter>();
    tc.dash = player.GetComponent<TopDownKit.Dash>();
    tc.interactor = player.GetComponent<TopDownKit.Interactor>();
    tc.gun = player.GetComponent<TopDownKit.Gun>();
    tc.showWeaponButton = tc.gun != null && tc.gun.weapons != null && tc.gun.weapons.Length > 1;
    EditorUtility.SetDirty(tc);

    var saveGo = GameObject.Find("Save") ?? new GameObject("Save");
    var sg = Get<TopDownKit.SaveGame>(saveGo);
    Get<TopDownKit.SaveMenu>(saveGo);
    sg.player = player.transform; sg.coin = coin; sg.items = items.ToArray();
    sg.parts = TopDownKit.SaveParts.All; sg.slots = 3; sg.folder = "Saves"; sg.saveSettings = true;
    var settings = new System.Collections.Generic.List<TopDownKit.SavedSetting>();
    var feel = UnityEngine.Object.FindFirstObjectByType<TopDownKit.FeelPlayer>();
    if (feel != null)
        foreach (string f in new[] { "master", "effectsVolume", "music", "sounds", "effects", "hitStop" })
            settings.Add(new TopDownKit.SavedSetting { target = feel, field = f });
    settings.Add(new TopDownKit.SavedSetting { target = mm, field = "mode" });
    settings.Add(new TopDownKit.SavedSetting { target = tc, field = "mode" });
    sg.settings = settings.ToArray();
    EditorUtility.SetDirty(sg);

    var hud = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Hud>();
    if (hud != null && mapHelp && hud.help != null && !hud.help.Contains("M map")) { hud.help += "  M map"; EditorUtility.SetDirty(hud); }

    if (path.EndsWith("LittleRpg.unity"))
    {
        var root = GameObject.Find("RPG");
        var walls = GameObject.Find("Walls").GetComponent<UnityEngine.Tilemaps.Tilemap>();
        var keeper = root.transform.Find("Keeper").gameObject;
        Vector3 at = new Vector3(11f, 1f, 0f);
        if (walls.HasTile(walls.WorldToCell(at))) throw new System.Exception("wall at the shop spot");
        var shopGo = root.transform.Find("Shopkeeper") != null ? root.transform.Find("Shopkeeper").gameObject : new GameObject("Shopkeeper");
        shopGo.transform.SetParent(root.transform, false);
        shopGo.transform.position = at;
        var ksr = keeper.GetComponent<SpriteRenderer>();
        var sr = Get<SpriteRenderer>(shopGo);
        sr.sprite = ksr.sprite; sr.sortingOrder = ksr.sortingOrder; sr.color = Hex("#b55088");
        var col = Get<CircleCollider2D>(shopGo);
        col.radius = 0.375f; col.offset = new Vector2(0f, 0.375f);
        var shop = Get<TopDownKit.Shop>(shopGo);
        shop.verb = "Shop"; shop.stock = AssetDatabase.LoadAssetAtPath<TopDownKit.ShopStock>("Assets/Data/Shops/RpgShop.asset");
        shop.currency = coin; shop.sellRate = 0.5f; shop.title = "Shop";
        shop.centre = new Vector2(0f, 0.4f); shop.promptOffset = new Vector2(0f, 1.6f);
        EditorUtility.SetDirty(shop);
        var chestScreen = UnityEngine.Object.FindFirstObjectByType<TopDownKit.ChestScreen>();
        Get<TopDownKit.ShopScreen>(chestScreen.gameObject);
        log += "shop; ";
    }
    if (path.EndsWith("Playground.unity"))
    {
        var picker = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Playground.Picker>();
        picker.minimap = mm; picker.touch = tc;
        EditorUtility.SetDirty(picker);
    }
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    log += System.IO.Path.GetFileNameWithoutExtension(path) + " saved; ";
}

SetUp("Assets/Scenes/ArenaShooter.unity", TopDownKit.MapMode.Corner, true);
SetUp("Assets/Scenes/LittleRpg.unity", TopDownKit.MapMode.Corner, true);
SetUp("Assets/Scenes/Dungeon.unity", TopDownKit.MapMode.Corner, true);
SetUp("Assets/Scenes/Playground.unity", TopDownKit.MapMode.Off, false);
return log;

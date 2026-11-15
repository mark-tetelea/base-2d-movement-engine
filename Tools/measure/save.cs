// Save / load check (phase 13, the one vital check). Run IN PLAY MODE on LittleRpg:
//     unity command eval_file --file Tools/measure/save.cs
// poll:  unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("save_state") as string;'
// Uses a scratch folder (SaveFiles.FolderOverride), never the real saves (trap 50).
// A. Files: write/read, backup on overwrite, broken / truncated / missing-field / nested
//    missing-field / wrong-type / newer / garbage files never throw and never lose the
//    last good save; a broken or newer file is set aside, not overwritten.
// B. LittleRpg: change every saved system, save, change more, load (scene reloads) ->
//    the state read back is exactly the saved one.
// C. Dungeon: clear a room, save, load -> same layout seed, same state.
// D. From the Dungeon, load the LittleRpg slot (another scene) -> the LittleRpg state.
// E. SaveGame.Load on a broken and a newer slot: says so, no scene change.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on LittleRpg first";
if (System.AppDomain.CurrentDomain.GetData("save_state") as string == "running") return "already running";
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LittleRpg") return "play LittleRpg first";

var results = new System.Collections.Generic.List<string>();
int failed = 0; string error = null;
void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + (detail != "" ? " (" + detail + ")" : "")); }
Application.runInBackground = true;
// Trap 84: the editor's mouse can read as held in play mode, and the Dungeon player would
// fire (and recoil) on spawn. A virtual mouse with nothing pressed, removed at the end.
var virtualMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("SaveCheckMouse");
virtualMouse.MakeCurrent();

string dir = System.IO.Path.Combine(Application.temporaryCachePath, "save-check-" + System.DateTime.Now.Ticks);
System.IO.Directory.CreateDirectory(dir);
TopDownKit.SaveFiles.FolderOverride = dir;
const int V = TopDownKit.SaveGame.Version;
TopDownKit.SaveFiles.Upgrade up = TopDownKit.SaveGame.UpgradeSteps;
bool Good(string p) => TopDownKit.SaveFiles.ReadOne(p, V, up, out TopDownKit.SaveData _, out _) == TopDownKit.SaveFiles.State.Ok;
string J(TopDownKit.SaveData d) { var c = JsonUtility.FromJson<TopDownKit.SaveData>(JsonUtility.ToJson(d)); c.savedAt = ""; return JsonUtility.ToJson(c, true); }
// Game round trips: the position is restored exactly, but a player resting against a wall
// or crate is nudged by the physics contact skin (~0.0001 tiles) right after; compared
// within 0.01 tiles (under 1/6 pixel). Everything else must match exactly.
string Same(string saved, string loaded)
{
    var a = JsonUtility.FromJson<TopDownKit.SaveData>(saved); var b = JsonUtility.FromJson<TopDownKit.SaveData>(loaded);
    float off = Vector2.Distance(a.playerPosition, b.playerPosition);
    if (off > 0.01f) return "player moved " + off + " tiles";
    b.playerPosition = a.playerPosition;
    string d = Diff(saved, JsonUtility.ToJson(b, true));
    return d == "" ? "" : d;
}

// ---- A. files ---------------------------------------------------------------------------
TopDownKit.SaveData Sample(int coins)
{
    return new TopDownKit.SaveData
    {
        version = V, savedAt = "x", parts = new[] { "Player", "Bag" }, scene = "S", seed = 7,
        playerPosition = new Vector2(1.25f, -3.5f), playerHealth = 4,
        bag = new[] { new TopDownKit.SlotSave { item = "Coin", count = coins }, new TopDownKit.SlotSave { item = "", count = 0 } },
        coins = coins, chests = new TopDownKit.ChestSave[0], openDoors = new string[0], leversOn = new[] { "Lever:a/b" },
        clearedRooms = new string[0], visitedRooms = new string[0], pickups = new TopDownKit.PickupSave[0],
        floorItems = new[] { new TopDownKit.FloorItemSave { item = "Gem", count = 1, position = new Vector2(0.1f, 0.2f), mode = "FromItem" } },
        shops = new TopDownKit.ShopSave[0]
    };
}
try
{
    string f = System.IO.Path.Combine(dir, "a.json");
    string bak = TopDownKit.SaveFiles.BackupOf(f);
    TopDownKit.SaveFiles.State Rd(out TopDownKit.SaveData d, out string note) => TopDownKit.SaveFiles.Read(f, V, up, out d, out note);

    Check("write 1", TopDownKit.SaveFiles.Write(f, Sample(1), Good) == "", "");
    var st = Rd(out var d1, out string n1);
    Check("read back exactly", st == TopDownKit.SaveFiles.State.Ok && J(d1) == J(Sample(1)), st + " " + n1);
    Check("no backup after the first write", !System.IO.File.Exists(bak), "");
    TopDownKit.SaveFiles.Write(f, Sample(2), Good);
    Rd(out var d2, out _);
    TopDownKit.SaveFiles.ReadOne(bak, V, up, out TopDownKit.SaveData b2, out _);
    Check("second write: new main, old one is the backup", d2 != null && d2.coins == 2 && b2 != null && b2.coins == 1, "");

    string good = System.IO.File.ReadAllText(f);
    void Bad(string name, string text, TopDownKit.SaveFiles.State want)
    {
        System.IO.File.WriteAllText(f, text);
        TopDownKit.SaveFiles.State s = TopDownKit.SaveFiles.State.Broken;
        TopDownKit.SaveData d = null; string note = "";
        try { s = Rd(out d, out note); }
        catch (System.Exception e) { Check(name + ": no exception", false, e.Message); return; }
        bool ok = want == TopDownKit.SaveFiles.State.FromBackup ? s == want && d != null && d.coins == 1 : s == want && d == null;
        Check(name + " -> " + want, ok, s + (note != "" ? ": " + note : ""));
        System.IO.File.WriteAllText(f, good);
    }
    Bad("truncated", good.Substring(0, good.Length / 2), TopDownKit.SaveFiles.State.FromBackup);
    Bad("empty file", "", TopDownKit.SaveFiles.State.FromBackup);
    Bad("garbage", "\u0000\u0001 not json", TopDownKit.SaveFiles.State.FromBackup);
    Bad("null", "null", TopDownKit.SaveFiles.State.FromBackup);
    Bad("a list", "[1,2]", TopDownKit.SaveFiles.State.FromBackup);
    Bad("a part cut out (coins)", good.Replace("\"coins\":", "\"coinz\":"), TopDownKit.SaveFiles.State.FromBackup);
    Bad("a whole section cut out (shops)", System.Text.RegularExpressions.Regex.Replace(good, ",\\s*\"shops\":\\s*\\[\\s*\\]", ""), TopDownKit.SaveFiles.State.FromBackup);
    Bad("a nested field cut out (slot count)", good.Replace("\"count\": 2", "\"cnt\": 2"), TopDownKit.SaveFiles.State.FromBackup);
    Bad("a nested field cut out (position.y)", System.Text.RegularExpressions.Regex.Replace(good, "\"y\":\\s*-3\\.5", "\"z\": -3.5"), TopDownKit.SaveFiles.State.FromBackup);
    Bad("wrong kind (health as text)", System.Text.RegularExpressions.Regex.Replace(good, "\"playerHealth\":\\s*4", "\"playerHealth\": \"4\""), TopDownKit.SaveFiles.State.FromBackup);
    string vPat = "\"version\":\\s*" + V + ",";
    Bad("no version", System.Text.RegularExpressions.Regex.Replace(good, vPat, ""), TopDownKit.SaveFiles.State.FromBackup);
    Bad("version 0", System.Text.RegularExpressions.Regex.Replace(good, vPat, "\"version\": 0,"), TopDownKit.SaveFiles.State.FromBackup);
    Bad("newer version", System.Text.RegularExpressions.Regex.Replace(good, vPat, "\"version\": 99,"), TopDownKit.SaveFiles.State.Newer);

    // Version 1 -> 2 (phase 13b): floor items had `auto`, now `mode` by meaning.
    {
        string v1 = System.Text.RegularExpressions.Regex.Replace(good, vPat, "\"version\": 1,");
        v1 = v1.Replace("\"mode\": \"FromItem\"", "\"auto\": true");
        v1 = v1.Replace("\"floorItems\": [", "\"floorItems\": [ { \"item\": \"Key\", \"count\": 1, \"position\": { \"x\": 0.0, \"y\": 0.0 }, \"auto\": false },");
        System.IO.File.WriteAllText(f, v1);
        var s1 = Rd(out var u, out string un);
        bool ok = s1 == TopDownKit.SaveFiles.State.Ok && u != null && u.version == V && u.floorItems.Length == 2
            && u.floorItems[0].mode == "Interact" && u.floorItems[1].mode == "Magnet" && u.floorItems[1].item == "Gem";
        Check("version 1 file upgrades (auto -> mode by meaning)", ok && !v1.Contains("\"mode\""), s1 + " " + un + (u != null ? " " + JsonUtility.ToJson(u) : ""));
        System.IO.File.WriteAllText(f, good);
    }

    // A broken main must not become the backup when saving over it, and is kept aside.
    System.IO.File.WriteAllText(f, "{ broken");
    Check("save over a broken file", TopDownKit.SaveFiles.Write(f, Sample(3), Good) == "", "");
    TopDownKit.SaveFiles.ReadOne(bak, V, up, out TopDownKit.SaveData b3, out _);
    Rd(out var d3, out _);
    var kept = System.IO.Directory.GetFiles(dir, "a.kept-*.json");
    Check("  the last good backup survives", b3 != null && b3.coins == 1, b3 == null ? "none" : "coins " + b3.coins);
    Check("  the broken file is kept aside", kept.Length == 1 && System.IO.File.ReadAllText(kept[0]) == "{ broken", kept.Length + " kept");
    Check("  the new save is in", d3 != null && d3.coins == 3, "");
    // A newer file is kept aside too, never overwritten.
    string newer = System.Text.RegularExpressions.Regex.Replace(System.IO.File.ReadAllText(f), "\"version\":\\s*" + V + ",", "\"version\": 99,");
    System.IO.File.WriteAllText(f, newer);
    TopDownKit.SaveFiles.Write(f, Sample(4), Good);
    kept = System.IO.Directory.GetFiles(dir, "a.kept-*.json");
    bool newerKept = false; foreach (string k in kept) if (System.IO.File.ReadAllText(k) == newer) newerKept = true;
    Check("save over a newer file keeps it aside", newerKept, kept.Length + " kept");
    Check("no temp file left", System.IO.Directory.GetFiles(dir, "*.tmp").Length == 0, "");
    // Both copies broken: refused, nothing thrown.
    System.IO.File.WriteAllText(f, "x"); System.IO.File.WriteAllText(bak, "y");
    Check("main and backup both broken -> Broken", Rd(out var d5, out _) == TopDownKit.SaveFiles.State.Broken && d5 == null, "");
    // Missing main, good backup.
    System.IO.File.Delete(f); TopDownKit.SaveFiles.Write(bak, Sample(6), Good);
    Check("main missing, backup good -> backup", Rd(out var d6, out _) == TopDownKit.SaveFiles.State.FromBackup && d6.coins == 6, "");
    // Settings file round trip.
    string sf = System.IO.Path.Combine(dir, "s.json");
    var sd = new TopDownKit.SettingsData { version = V, values = new[] { new TopDownKit.SettingSave { key = "FeelPlayer.master", value = "0.5" } } };
    TopDownKit.SaveFiles.Write(sf, sd, p => true);
    Check("settings file reads back", TopDownKit.SaveFiles.Read(sf, V, null, out TopDownKit.SettingsData sr, out _) == TopDownKit.SaveFiles.State.Ok
        && sr.values.Length == 1 && sr.values[0].value == "0.5", "");
}
catch (System.Exception e) { error = "files: " + e; }

// ---- B-E. the game ----------------------------------------------------------------------
TopDownKit.Item ItemAt(string n) => AssetDatabase.LoadAssetAtPath<TopDownKit.Item>("Assets/Data/Items/" + n + ".asset");
TopDownKit.SaveGame Game() => TopDownKit.SaveGame.Current;
GameObject Player() => GameObject.Find("Player");
void Place(Vector2 p) { var rb = Player().GetComponent<Rigidbody2D>(); rb.position = p; rb.linearVelocity = Vector2.zero; Player().transform.position = p; }
string rpgSaved = null, dunSaved = null; int dunSeed = 0;

var steps = new System.Collections.Generic.List<(string name, System.Func<bool> run)>();
float waitUntil = 0f;
void Do(string name, System.Action a) => steps.Add((name, () => { a(); return true; }));
// Game time, not real time: a slow scene load passes real seconds with few physics steps (trap 78).
void Wait(float s) => steps.Add(("wait", () => { if (waitUntil <= 0f) waitUntil = Time.time + s; if (Time.time < waitUntil) return false; waitUntil = 0f; return true; }));
void WaitScene(string scene) => steps.Add(("wait for " + scene, () =>
    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == scene && Game() != null && Game().gameObject.scene.name == scene
    && (Game().Applied || Time.frameCount > 0) && Player() != null));
void WaitApplied(string scene) => steps.Add(("wait for load into " + scene, () =>
    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == scene && Game() != null && Game().Applied));

Do("change every saved system (LittleRpg)", () =>
{
    var p = Player();
    var bag = p.GetComponent<TopDownKit.Bag>().Inventory;
    Place(new Vector2(3f, -3f));
    var hp = p.GetComponent<TopDownKit.Health>();   // the RPG player has none; the Dungeon one does
    if (hp != null) hp.Hit(2, Vector2.zero);
    bag.Add(ItemAt("Coin"), 9);
    bag.Add(ItemAt("Potion"), 2);
    var keyChest = GameObject.Find("Key chest").GetComponent<TopDownKit.Chest>();
    keyChest.SetOpened(true);
    TopDownKit.Inventory.Move(keyChest.Bag.Inventory, 1, bag);   // coins out, key left in the chest
    GameObject.Find("Locked door").GetComponent<TopDownKit.LockedDoor>().Open(true);
    GameObject.Find("Lever").GetComponent<TopDownKit.Lever>().Set(true);
    foreach (var r in TopDownKit.Room.All) if (r.name == "Room C") r.Visited = true;
    var coins = new System.Collections.Generic.List<TopDownKit.Pickup>();
    foreach (var pk in TopDownKit.Pickup.All) if (pk.name == "Coin") coins.Add(pk);
    coins[0].Collect(p);                 // taken
    coins[1].stack.count = 3;            // partly changed
    p.GetComponent<TopDownKit.Hotbar>().Select(0);
    var shop = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Shop>();
    Check("shop sells a potion", shop.BuyLine(bag, 0) == TopDownKit.TradeResult.Done, "");
    int coinsBefore = bag.Count(ItemAt("Coin"));
    var gemResult = shop.BuyLine(bag, 1);
    Check("shop: a gem (15) with " + coinsBefore + " coins", gemResult == (coinsBefore >= 15 ? TopDownKit.TradeResult.Done : TopDownKit.TradeResult.NoMoney), gemResult.ToString());
    int potionSlot = -1; for (int i = 0; i < bag.Size; i++) if (bag.slots[i].item == ItemAt("Potion")) potionSlot = i;
    Check("shop buys back a potion for half", shop.SellSlot(bag, potionSlot) == TopDownKit.TradeResult.Done, "");
    TopDownKit.Pickup.Spawn(ItemAt("Gem"), 1, new Vector2(2f, -6f + 3f), 0, 0f);   // an item lying loose
});
Wait(0.3f);
Do("save slot 1", () =>
{
    rpgSaved = J(Game().Capture());
    string err = Game().Save(0);
    Check("save slot 1", err == "", err);
    Check("  the file names every saved part", rpgSaved.Contains("Room C") && rpgSaved.Contains("Locked door") && rpgSaved.Contains("Lever")
        && rpgSaved.Contains("Key chest") && rpgSaved.Contains("Shopkeeper") && rpgSaved.Contains("\"Gem\""), "");
    // Change things again; the load must undo all of it.
    Place(new Vector2(-10f, 5f));
    Player().GetComponent<TopDownKit.Bag>().Inventory.Add(ItemAt("Crown"), 1);
});
Do("load slot 1", () => { string err = Game().Load(0); Check("load slot 1 starts", err == "", err); });
WaitApplied("LittleRpg");
Wait(0.2f);
Do("compare", () =>
{
    string back = J(Game().Capture());
    string why = Same(rpgSaved, back);
    Check("LittleRpg round trip is exact", why == "", why);
});

// C. Dungeon
Do("to the Dungeon", () => UnityEngine.SceneManagement.SceneManager.LoadScene("Dungeon"));
WaitScene("Dungeon");
Wait(0.5f);
// Some layouts spawn the player overlapping something; physics pushes it out over the
// first few steps (NEXT.md). Save only once it is at rest, as a player would.
Vector2 lastPos = Vector2.zero; float restSince = -1f;
steps.Add(("player at rest", () =>
{
    Vector2 now = Player().GetComponent<Rigidbody2D>().position;
    if ((now - lastPos).sqrMagnitude > 1e-8f || restSince < 0f) { lastPos = now; restSince = Time.fixedTime; return false; }
    return Time.fixedTime - restSince >= 0.25f;
}));
Do("change the Dungeon", () =>
{
    var gen = UnityEngine.Object.FindFirstObjectByType<TopDownKit.RoomGenerator>();
    dunSeed = gen.UsedSeed;
    var l = gen.Rooms[gen.Rooms.Count - 1].GetComponent<TopDownKit.RoomLock>();
    if (l == null) foreach (var r in gen.Rooms) if (r.GetComponent<TopDownKit.RoomLock>() != null) l = r.GetComponent<TopDownKit.RoomLock>();
    l.Clear();
    gen.Rooms[1].GetComponent<TopDownKit.Room>().Visited = true;
    Player().GetComponent<TopDownKit.Bag>().Inventory.Add(ItemAt("Coin"), 4);
    var dh = Player().GetComponent<TopDownKit.Health>();
    Check("Dungeon player has health to save", dh != null, "");
    if (dh != null) dh.Hit(2, Vector2.zero);
    dunSaved = J(Game().Capture());
    string err = Game().Save(1);
    Check("save slot 2 (Dungeon)", err == "", err);
});
Do("load slot 2", () => { string err = Game().Load(1); Check("load slot 2 starts", err == "", err); });
WaitApplied("Dungeon");
Wait(0.2f);
Do("compare dungeon", () =>
{
    var gen = UnityEngine.Object.FindFirstObjectByType<TopDownKit.RoomGenerator>();
    Check("Dungeon: same layout seed", gen.UsedSeed == dunSeed, dunSeed + " -> " + gen.UsedSeed);
    string back = J(Game().Capture());
    string why = Same(dunSaved, back);
    Check("Dungeon round trip is exact", why == "", why);
});

// D. Another scene's slot
Do("load slot 1 from the Dungeon", () => { string err = Game().Load(0); Check("load LittleRpg slot from the Dungeon starts", err == "", err); });
WaitApplied("LittleRpg");
Wait(0.2f);
Do("compare again", () => { string why = Same(rpgSaved, J(Game().Capture())); Check("cross-scene load is exact", why == "", why); });

// E. Broken and newer slots through SaveGame
Do("broken slot", () =>
{
    System.IO.File.WriteAllText(Game().SlotPath(2), "{\"version\": 1, \"scene\": ");
    int sceneHandle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle;
    string err = Game().Load(2);
    Check("broken slot: refused, says so, same scene", err.Contains("broken") && UnityEngine.SceneManagement.SceneManager.GetActiveScene().handle == sceneHandle, err);
    System.IO.File.WriteAllText(Game().SlotPath(2), System.IO.File.ReadAllText(Game().SlotPath(0)).Replace("\"version\": " + V, "\"version\": " + (V + 1)));
    err = Game().Load(2);
    Check("newer slot: refused, says so", err.Contains("newer"), err);
    // Slot 1 broken but its backup good (slot 1 was written once, so make a backup by saving twice).
    Game().Save(0); Game().Save(0);
    System.IO.File.WriteAllText(Game().SlotPath(0), "{");
    Check("broken slot with a good backup: shown as backup", Game().Peek(0, out var _, out string note) == TopDownKit.SaveFiles.State.FromBackup, note);
});

// F. Settings survive a scene change (they are written on exit from the last values seen,
// not read from components that are already being destroyed).
Do("change settings", () =>
{
    UnityEngine.Object.FindFirstObjectByType<TopDownKit.FeelPlayer>().master = 0.25f;
    UnityEngine.Object.FindFirstObjectByType<TopDownKit.Minimap>().mode = TopDownKit.MapMode.FullOnKey;
});
Wait(0.7f);
Do("scene change", () => UnityEngine.SceneManagement.SceneManager.LoadScene("Dungeon"));
WaitScene("Dungeon");
Do("settings kept", () =>
{
    var fp = UnityEngine.Object.FindFirstObjectByType<TopDownKit.FeelPlayer>();
    var mm = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Minimap>();
    var tc = UnityEngine.Object.FindFirstObjectByType<TopDownKit.TouchControls>();
    Check("settings survive a scene change", Mathf.Approximately(fp.master, 0.25f) && mm.mode == TopDownKit.MapMode.FullOnKey && tc.mode == TopDownKit.TouchMode.Auto,
        "master " + fp.master + ", map " + mm.mode + ", touch " + tc.mode);
    string txt = System.IO.File.ReadAllText(System.IO.Path.Combine(dir, "settings.json"));
    Check("  the file holds all eight", System.Text.RegularExpressions.Regex.Matches(txt, "\"key\"").Count == 8, txt.Replace(System.Environment.NewLine, " "));
    fp.master = 1f; mm.mode = TopDownKit.MapMode.Corner;
});
Wait(0.7f);

string Diff(string a, string b)
{
    if (a == b) return "";
    if (a == null || b == null) return "missing";
    var la = a.Split('\n'); var lb = b.Split('\n');
    for (int i = 0; i < Mathf.Min(la.Length, lb.Length); i++) if (la[i] != lb[i]) return "line " + i + ": saved [" + la[i].Trim() + "] loaded [" + lb[i].Trim() + "]";
    return "lengths " + la.Length + " / " + lb.Length;
}

int stepIndex = 0; float stepStart = 0f; bool started = false;
var original = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var systems = loop.subSystemList;
void Finish()
{
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(original);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(virtualMouse);
    Application.runInBackground = false;
    // FolderOverride stays set: leaving play mode writes the settings once more, and that
    // must go to the scratch folder too (the next domain reload clears it).
    System.AppDomain.CurrentDomain.SetData("save_state", (error != null ? "error: " + error + "\n" : "done: ")
        + (failed == 0 && error == null ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks  (folder " + dir + ")\n" + string.Join("\n", results));
}
void OnUpdate()
{
    try
    {
        if (stepIndex >= steps.Count) return;
        if (!started) { started = true; stepStart = Time.realtimeSinceStartup; }
        if (steps[stepIndex].run()) { stepIndex++; stepStart = Time.realtimeSinceStartup; }
        else if (Time.realtimeSinceStartup - stepStart > 20f) { Check("step '" + steps[stepIndex].name + "'", false, "timed out"); stepIndex = steps.Count; }
        if (stepIndex >= steps.Count) Finish();
    }
    catch (System.Exception e) { error = steps[Mathf.Min(stepIndex, steps.Count - 1)].name + ": " + e; stepIndex = steps.Count; Finish(); }
}
if (error != null) { Finish(); return "file checks threw"; }
for (int i = 0; i < systems.Length; i++)
{
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.Update))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.SaveGame), updateDelegate = OnUpdate });
        systems[i].subSystemList = list.ToArray();
    }
}
loop.subSystemList = systems;
System.AppDomain.CurrentDomain.SetData("save_state", "running");
UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
return "files: " + (failed == 0 ? "all pass" : failed + " failed") + "; game steps started (" + steps.Count + ")";

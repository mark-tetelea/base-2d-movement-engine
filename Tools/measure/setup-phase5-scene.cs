// One-off setup for phase 5, part 2 (EDIT mode, after setup-phase5-assets.cs):
//     unity command eval_file --file Tools/measure/setup-phase5-scene.cs
// Copies TestArena into Assets/Scenes/LittleRpg.unity (once) and turns it into the
// Little RPG room demo: Instant feel, 4 facings, rooms fade; a Guide NPC and a storage
// chest in A; coins; a locked door A -> B (its key is in a chest in C); the crown chest
// and the Keeper in B; a lever in C opens a gate over the warp to D (Hermit, a chest).
// B's north door stays shut for good, so the locked door is the only way into B.
// Safe to re-run: rebuilds the "RPG" objects and updates the rest.
if (EditorApplication.isPlaying) return "exit play mode first";
const string path = "Assets/Scenes/LittleRpg.unity";
string log = "";
if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
{
    AssetDatabase.CopyAsset("Assets/Scenes/TestArena.unity", path);
    log += "copied; ";
}
// Trap 73: open the scene first, then load assets.
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);

T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
var instant = Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Instant.asset");
var coin = Load<TopDownKit.Item>("Assets/Data/Items/Coin.asset");
var key = Load<TopDownKit.Item>("Assets/Data/Items/Key.asset");
var crown = Load<TopDownKit.Item>("Assets/Data/Items/Crown.asset");
var guideLines = Load<TopDownKit.DialogueLines>("Assets/Data/Dialogue/Guide.asset");
var keeperLines = Load<TopDownKit.DialogueLines>("Assets/Data/Dialogue/Keeper.asset");
var hermitLines = Load<TopDownKit.DialogueLines>("Assets/Data/Dialogue/Hermit.asset");
var square = Load<Sprite>("Assets/Art/Placeholders/white-square.png");
var chestSprite = Load<Sprite>("Assets/Art/Generated/chest.png");
var chestOpen = Load<Sprite>("Assets/Art/Generated/chest-open.png");
var leverSprite = Load<Sprite>("Assets/Art/Generated/lever.png");
var heartSprite = Load<Sprite>("Assets/Art/Generated/heart.png");
var promptSprite = Load<Sprite>("Assets/Art/Generated/prompt-e.png");
Sprite npcSprite = null;
foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/player-down.png"))
    if (o is Sprite sp && sp.name == "player-down_0") npcSprite = sp;
if (npcSprite == null) throw new System.Exception("no player-down_0");

void Kill(string name) { var g = GameObject.Find(name); if (g != null) { UnityEngine.Object.DestroyImmediate(g); log += "removed " + name + "; "; } }
Kill("Feel Keys (test arena)");
Kill("Zones");
Kill("Aim Marker");
Kill("RPG");
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }
Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var col); return col; }
var walls = GameObject.Find("Walls").GetComponent<UnityEngine.Tilemaps.Tilemap>();
void Free(float x, float y) { if (walls.HasTile(walls.WorldToCell(new Vector3(x, y, 0f)))) throw new System.Exception("wall at " + x + "," + y); }

// Player: Instant, 4 facings by walking, aim from walking (no mouse in this demo).
var player = GameObject.Find("Player");
var mover = Get<TopDownKit.PlayerMover>(player);
mover.feel = instant; mover.readKeyboard = true;
var facing = Get<TopDownKit.PlayerFacing>(player);
facing.settings = new TopDownKit.FacingSettings(TopDownKit.FacingCount.Four, TopDownKit.FaceTowards.Walk);
var aim = Get<TopDownKit.PlayerAim>(player);
aim.aimFrom = TopDownKit.AimFrom.Walk;
var dash = player.GetComponent<TopDownKit.Dash>();
if (dash != null) UnityEngine.Object.DestroyImmediate(dash);
var bag = Get<TopDownKit.Bag>(player);
bag.inventory = new TopDownKit.Inventory(new TopDownKit.InventorySettings(10, 5, 0));
var hotbar = Get<TopDownKit.Hotbar>(player);
int order = player.GetComponent<SpriteRenderer>().sortingOrder;
hotbar.dropSortingOrder = order - 1;
var interactor = Get<TopDownKit.Interactor>(player);
interactor.settings = new TopDownKit.InteractSettings(1.5f, TopDownKit.BestTarget.Facing, TopDownKit.UseMode.Press, 0.5f);
interactor.centre = new Vector2(0f, 0.375f);   // the collider's centre; the pivot is at the feet
var collector = Get<TopDownKit.Collector>(player);
collector.settings = new TopDownKit.CollectSettings(2.5f, 10f, 0.6f);
collector.centre = new Vector2(0f, 0.375f);
foreach (var c in new Component[] { mover, facing, aim, bag, hotbar, interactor, collector }) EditorUtility.SetDirty(c);
player.transform.position = new Vector3(-12f, 0f, 0f);

var rig = GameObject.Find("Main Camera").GetComponent<TopDownKit.CameraRig>();
rig.layout = TopDownKit.CameraLayout.RoomsFade;
EditorUtility.SetDirty(rig);

// B: no lock in this demo. Its west door goes (the locked door takes the gap); the north
// door stays as a permanent blocker so the only way in is the locked door.
var roomB = GameObject.Find("Room B");
var lockB = roomB.GetComponent<TopDownKit.RoomLock>();
if (lockB != null) UnityEngine.Object.DestroyImmediate(lockB);
var west = roomB.transform.Find("Door west"); if (west != null) UnityEngine.Object.DestroyImmediate(west.gameObject);
var north = roomB.transform.Find("Door north") ?? roomB.transform.Find("Sealed door");
if (north != null) { north.name = "Sealed door"; north.gameObject.SetActive(true); north.GetComponentInChildren<SpriteRenderer>().color = Hex("#5a6988"); }

var root = new GameObject("RPG");
GameObject Thing(string name, Transform parent, float x, float y)
{
    Free(x, y);
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.position = new Vector3(x, y, 0f);
    return go;
}
SpriteRenderer Art(GameObject go, Sprite s, Color c, int ord)
{
    var sr = go.AddComponent<SpriteRenderer>();
    sr.sprite = s; sr.color = c; sr.sortingOrder = ord;
    return sr;
}
GameObject Block(string name, Transform parent, float cx, float cy, float w, float h, string color)
{
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.position = new Vector3(cx, cy, 0f);
    go.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
    var a = new GameObject("Art");
    a.transform.SetParent(go.transform, false);
    a.transform.localScale = new Vector3(w, h, 1f);
    Art(a, square, Hex(color), order + 1);
    return go;
}
TopDownKit.Chest Chest(string name, float x, float y, params (TopDownKit.Item item, int count)[] contents)
{
    var go = Thing(name, root.transform, x, y);
    var sr = Art(go, chestSprite, Color.white, order);
    var box = go.AddComponent<BoxCollider2D>();
    box.size = new Vector2(0.875f, 0.5f); box.offset = new Vector2(0f, 0.3125f);
    var b = go.AddComponent<TopDownKit.Bag>();
    b.inventory = new TopDownKit.Inventory(new TopDownKit.InventorySettings(10, 0, 0));
    foreach (var c in contents) b.inventory.Add(c.item, c.count);
    var ch = go.AddComponent<TopDownKit.Chest>();
    ch.verb = "Open"; ch.art = sr; ch.closedSprite = chestSprite; ch.openSprite = chestOpen;
    ch.centre = new Vector2(0f, 0.4f); ch.promptOffset = new Vector2(0f, 1.4f);
    return ch;
}
void Npc(string name, float x, float y, TopDownKit.DialogueLines lines, string tint)
{
    var go = Thing(name, root.transform, x, y);
    Art(go, npcSprite, Hex(tint), order);
    var col = go.AddComponent<CircleCollider2D>();
    col.radius = 0.375f; col.offset = new Vector2(0f, 0.375f);
    var t = go.AddComponent<TopDownKit.Talker>();
    t.verb = "Talk"; t.lines = lines; t.centre = new Vector2(0f, 0.4f); t.promptOffset = new Vector2(0f, 1.6f);
}
void Coin(float x, float y)
{
    var go = Thing("Coin", root.transform, x, y);
    Art(go, coin.icon, Color.white, order - 1);
    var p = go.AddComponent<TopDownKit.Pickup>();
    p.mode = TopDownKit.PickupChoice.FromItem; p.stack = new TopDownKit.ItemStack(coin, 1); p.centre = Vector2.zero;
}

// Room A.
Npc("Guide", -4f, 3f, guideLines, "#63c74d");
Chest("Storage chest", -16f, -8f);
for (int i = 0; i < 5; i++) Coin(-8f + 2f * i, -2f);
Coin(-17f, 8f); Coin(-15f, 8f);
// The locked door fills the A <-> B gap (cells x 19-20, y 6-8). Used from the A side.
var door = Block("Locked door", root.transform, 20f, 7.5f, 2f, 3f, "#b55088");
var ld = door.AddComponent<TopDownKit.LockedDoor>();
ld.verb = "Unlock"; ld.key = key; ld.consumeKey = true; ld.blockers = new[] { door };
ld.lockedText = "Locked. It needs a key.";
ld.centre = new Vector2(-1f, 0f); ld.promptOffset = new Vector2(-1.5f, 2f);

// Room C: the key chest, coins, the lever and the gate over the warp to D.
Chest("Key chest", 8f, 24f, (key, 1), (coin, 10));
for (int i = 0; i < 6; i++) Coin(20f + 2f * i, 16f);
var gate = Block("Gate", root.transform, 50f, 35f, 3f, 3f, "#5a6988");
var leverGo = Thing("Lever", root.transform, 45f, 34f);
var lsr = Art(leverGo, leverSprite, Color.white, order);
leverGo.AddComponent<BoxCollider2D>().size = new Vector2(0.75f, 0.4f);
leverGo.GetComponent<BoxCollider2D>().offset = new Vector2(0f, 0.25f);
var lever = leverGo.AddComponent<TopDownKit.Lever>();
lever.verb = "Pull"; lever.art = lsr; lever.offWhenOn = new[] { gate }; lever.centre = new Vector2(0f, 0.4f);

// Room B: the Keeper, the crown chest, coins.
Npc("Keeper", 46f, 4f, keeperLines, "#feae34");
Chest("Crown chest", 52f, 0f, (crown, 1), (coin, 5));
for (int i = 0; i < 4; i++) Coin(26f + 2f * i, 7.5f);

// Area D: the Hermit, a coin chest and a heart (heals when hurt; nothing hurts here).
Npc("Hermit", 106f, 2f, hermitLines, "#c0cbdc");
Chest("Hermit chest", 112f, -2f, (coin, 25));
var heart = Thing("Heart", root.transform, 108f, -3f);
Art(heart, heartSprite, Color.white, order - 1);
var hp = heart.AddComponent<TopDownKit.Pickup>(); hp.mode = TopDownKit.PickupChoice.FromItem; hp.heal = 1; hp.centre = Vector2.zero;

// Screens and the prompt.
var ui = new GameObject("UI");
ui.transform.SetParent(root.transform, false);
ui.AddComponent<TopDownKit.TextBox>();
ui.AddComponent<TopDownKit.ChestScreen>();
var hud = ui.AddComponent<TopDownKit.RpgHud>();
hud.hotbar = hotbar; hud.interactor = interactor; hud.coin = coin; hud.goal = crown;
var prompt = new GameObject("Interact Prompt");
prompt.transform.SetParent(root.transform, false);
var pr = prompt.AddComponent<TopDownKit.InteractPrompt>();
pr.interactor = interactor;
var icon = new GameObject("Icon"); icon.transform.SetParent(prompt.transform, false);
pr.icon = Art(icon, promptSprite, Color.white, order + 5);
var bar = new GameObject("Hold bar"); bar.transform.SetParent(prompt.transform, false);
pr.holdBar = Art(bar, square, Hex("#feae34"), order + 5);
pr.holdBar.enabled = false; pr.icon.enabled = false;

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
foreach (var s in EditorBuildSettings.scenes) if (s.path != path) list.Add(s);
list.Insert(1, new EditorBuildSettingsScene(path, true));
EditorBuildSettings.scenes = list.ToArray();
string names = ""; foreach (var s in EditorBuildSettings.scenes) names += System.IO.Path.GetFileNameWithoutExtension(s.path) + " ";
return log + "saved; build list: " + names;

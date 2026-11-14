// One-off setup for phase 6 (EDIT mode):
//     unity command eval_file --file Tools/measure/setup-phase6.cs
// 1. The UI panel: Assets/UI/KitTheme.tss + Assets/Resources/KitPanel.asset.
// 2. TestArena.unity becomes Playground.unity (moved, same GUID); its test-key object is
//    replaced by "Playground" (Picker + KnockKey); the player gets a gun, health, a bag,
//    hotbar, interactor and magnet; a chest, an NPC and coins in room A, two wanderers in
//    area D; the shared screens.
// 3. ArenaShooter and LittleRpg get the shared Hud + PauseMenu (goal scripts feed the Hud).
// 4. Assets/Scenes/StartMenu.unity; build list StartMenu, ArenaShooter, LittleRpg, Playground.
// Safe to re-run.
if (EditorApplication.isPlaying) return "exit play mode first";
string log = "";
T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }
Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var col); return col; }
void RemoveMissing(GameObject g) { GameObjectUtility.RemoveMonoBehavioursWithMissingScript(g); }

// --- 1. Panel ---------------------------------------------------------------------------
if (!AssetDatabase.IsValidFolder("Assets/UI")) AssetDatabase.CreateFolder("Assets", "UI");
if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
const string tssPath = "Assets/UI/KitTheme.tss";
if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(tssPath) == null)
{
    System.IO.File.WriteAllText(tssPath, "@import url(\"unity-theme://default\");\n");
    AssetDatabase.ImportAsset(tssPath);
    log += "theme made; ";
}
var theme = Load<UnityEngine.UIElements.ThemeStyleSheet>(tssPath);
const string panelPath = "Assets/Resources/KitPanel.asset";
var panel = AssetDatabase.LoadAssetAtPath<UnityEngine.UIElements.PanelSettings>(panelPath);
if (panel == null)
{
    panel = ScriptableObject.CreateInstance<UnityEngine.UIElements.PanelSettings>();
    AssetDatabase.CreateAsset(panel, panelPath);
    log += "panel made; ";
}
panel.themeStyleSheet = theme;
panel.scaleMode = UnityEngine.UIElements.PanelScaleMode.ConstantPixelSize;
panel.scale = 1f;
panel.referenceSpritePixelsPerUnit = 16f;   // trap 35
panel.sortingOrder = 10;
EditorUtility.SetDirty(panel);
AssetDatabase.SaveAssets();

// --- 2. Playground ----------------------------------------------------------------------
const string pg = "Assets/Scenes/Playground.unity";
if (AssetDatabase.LoadAssetAtPath<SceneAsset>(pg) == null)
{
    string err = AssetDatabase.MoveAsset("Assets/Scenes/TestArena.unity", pg);
    if (!string.IsNullOrEmpty(err)) return log + "move failed: " + err;
    log += "TestArena -> Playground; ";
}
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(pg, UnityEditor.SceneManagement.OpenSceneMode.Single);   // trap 73

var feels = new[] { Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Instant.asset"), Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Glide.asset"),
    Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Ice.asset"), Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Heavy.asset") };
var weapons = new[] { Load<TopDownKit.Weapon>("Assets/Data/Weapons/Pistol.asset"), Load<TopDownKit.Weapon>("Assets/Data/Weapons/Shotgun.asset"),
    Load<TopDownKit.Weapon>("Assets/Data/Weapons/Rapid.asset") };
var coin = Load<TopDownKit.Item>("Assets/Data/Items/Coin.asset");
var key = Load<TopDownKit.Item>("Assets/Data/Items/Key.asset");
var guideLines = Load<TopDownKit.DialogueLines>("Assets/Data/Dialogue/Guide.asset");
var wanderer = Load<GameObject>("Assets/Prefabs/Enemies/Wanderer.prefab");
var square = Load<Sprite>("Assets/Art/Placeholders/white-square.png");
var chestSprite = Load<Sprite>("Assets/Art/Generated/chest.png");
var chestOpen = Load<Sprite>("Assets/Art/Generated/chest-open.png");
var promptSprite = Load<Sprite>("Assets/Art/Generated/prompt-e.png");
Sprite npcSprite = null;
foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Generated/player-down.png"))
    if (o is Sprite sp && sp.name == "player-down_0") npcSprite = sp;

void Kill(string name) { var g = GameObject.Find(name); if (g != null) { UnityEngine.Object.DestroyImmediate(g); log += "removed " + name + "; "; } }
Kill("Feel Keys (test arena)");
Kill("Playground");

var player = GameObject.Find("Player");
RemoveMissing(player);
var mover = Get<TopDownKit.PlayerMover>(player);
var facing = Get<TopDownKit.PlayerFacing>(player);
var aim = Get<TopDownKit.PlayerAim>(player);
var dash = Get<TopDownKit.Dash>(player);
var hp = Get<TopDownKit.Health>(player);
hp.maxHealth = 6; hp.team = 0; hp.invulnerableTime = 1f; hp.destroyOnDeath = false; hp.shakeOnHit = 0.3f;
Get<TopDownKit.RestartOnDeath>(player).delay = 1.5f;
var gun = Get<TopDownKit.Gun>(player);
gun.weapons = weapons; gun.team = 0; gun.muzzleDistance = 0.5f;
Get<TopDownKit.PlayerShooter>(player).numberKeysSelect = true;
var bag = Get<TopDownKit.Bag>(player);
bag.inventory = new TopDownKit.Inventory(new TopDownKit.InventorySettings(10, 5, 0));
var hotbar = Get<TopDownKit.Hotbar>(player);
hotbar.readKeyboard = false;   // number keys pick weapons here; the hotbar is clicked
int order = player.GetComponent<SpriteRenderer>().sortingOrder;
hotbar.dropSortingOrder = order - 1;
var interactor = Get<TopDownKit.Interactor>(player);
interactor.settings = new TopDownKit.InteractSettings(1.5f, TopDownKit.BestTarget.Facing, TopDownKit.UseMode.Press, 0.5f);
interactor.centre = new Vector2(0f, 0.375f);
var collector = Get<TopDownKit.Collector>(player);
collector.settings = new TopDownKit.CollectSettings(2.5f, 10f, 0.6f);
collector.centre = new Vector2(0f, 0.375f);
foreach (var c in player.GetComponents<Component>()) EditorUtility.SetDirty(c);

var rig = GameObject.Find("Main Camera").GetComponent<TopDownKit.CameraRig>();
var walls = GameObject.Find("Walls").GetComponent<UnityEngine.Tilemaps.Tilemap>();
void Free(float x, float y) { if (walls.HasTile(walls.WorldToCell(new Vector3(x, y, 0f)))) throw new System.Exception("wall at " + x + "," + y); }

var root = new GameObject("Playground");
GameObject Thing(string name, Transform parent, float x, float y)
{
    Free(x, y);
    var go = new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.position = new Vector3(x, y, 0f);
    return go;
}
SpriteRenderer Art(GameObject go, Sprite s, Color c, int ord) { var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = s; sr.color = c; sr.sortingOrder = ord; return sr; }

// Things to use in room A (the lower-left part, away from the zones along the top).
var chestGo = Thing("Chest", root.transform, -14f, -6f);
var csr = Art(chestGo, chestSprite, Color.white, order);
var cbox = chestGo.AddComponent<BoxCollider2D>(); cbox.size = new Vector2(0.875f, 0.5f); cbox.offset = new Vector2(0f, 0.3125f);
var cbag = chestGo.AddComponent<TopDownKit.Bag>();
cbag.inventory = new TopDownKit.Inventory(new TopDownKit.InventorySettings(10, 0, 0));
cbag.inventory.Add(key, 1); cbag.inventory.Add(coin, 10);
var ch = chestGo.AddComponent<TopDownKit.Chest>();
ch.verb = "Open"; ch.art = csr; ch.closedSprite = chestSprite; ch.openSprite = chestOpen;
ch.centre = new Vector2(0f, 0.4f); ch.promptOffset = new Vector2(0f, 1.4f);
var npc = Thing("Guide", root.transform, -11f, -6f);
Art(npc, npcSprite, Hex("#63c74d"), order);
var ncol = npc.AddComponent<CircleCollider2D>(); ncol.radius = 0.375f; ncol.offset = new Vector2(0f, 0.375f);
var talker = npc.AddComponent<TopDownKit.Talker>();
talker.verb = "Talk"; talker.lines = guideLines; talker.centre = new Vector2(0f, 0.4f); talker.promptOffset = new Vector2(0f, 1.6f);
for (int i = 0; i < 6; i++)
{
    var c = Thing("Coin", root.transform, -8f + 1.5f * i, -8f);
    Art(c, coin.icon, Color.white, order - 1);
    var p = c.AddComponent<TopDownKit.Pickup>(); p.auto = true; p.stack = new TopDownKit.ItemStack(coin, 1); p.centre = Vector2.zero;
}
// Two wanderers in area D (reached by the warp), away from the measuring lanes in A.
foreach (var pos in new[] { new Vector2(108f, 3f), new Vector2(112f, -3f) })
{
    Free(pos.x, pos.y);
    var e = (GameObject)PrefabUtility.InstantiatePrefab(wanderer, root.transform);
    e.transform.position = pos;
    var brain = e.GetComponent<TopDownKit.EnemyBrain>(); brain.target = player.transform; brain.seed = (int)pos.x;
    e.GetComponentInChildren<SpriteRenderer>().sortingOrder = order;
    PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
}

// The prompt over the current target.
var prompt = new GameObject("Interact Prompt");
prompt.transform.SetParent(root.transform, false);
var pr = prompt.AddComponent<TopDownKit.InteractPrompt>();
pr.interactor = interactor;
var icon = new GameObject("Icon"); icon.transform.SetParent(prompt.transform, false);
pr.icon = Art(icon, promptSprite, Color.white, order + 5);
var barGo = new GameObject("Hold bar"); barGo.transform.SetParent(prompt.transform, false);
pr.holdBar = Art(barGo, square, Hex("#feae34"), order + 5);
pr.holdBar.enabled = false; pr.icon.enabled = false;

// The picker, K knockback, and the shared screens.
var picker = root.AddComponent<TopDownKit.Playground.Picker>();
picker.mover = mover; picker.feels = feels; picker.dash = dash; picker.facing = facing; picker.aim = aim;
picker.rig = rig; picker.gun = gun; picker.interactor = interactor; picker.collector = collector;
var marker = GameObject.Find("Aim Marker");
picker.aimMarker = marker != null ? marker.transform : null;
root.AddComponent<TopDownKit.Playground.KnockKey>().player = mover;
TopDownKit.Hud Screens(GameObject host, TopDownKit.Health h, TopDownKit.Gun g, TopDownKit.Hotbar hb, TopDownKit.Interactor it, string help)
{
    var hud = Get<TopDownKit.Hud>(host);
    hud.health = h; hud.gun = g; hud.hotbar = hb; hud.interactor = it; hud.help = help;
    Get<TopDownKit.PauseMenu>(host).menuScene = 0;
    EditorUtility.SetDirty(hud);
    return hud;
}
Screens(root, hp, gun, hotbar, interactor,
    "WASD move  mouse aim  click shoot  R reload  1-3 weapon\nSpace dash  E use  K knockback  Tab settings  Esc pause");
root.AddComponent<TopDownKit.TextBox>();
root.AddComponent<TopDownKit.ChestScreen>();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
log += "Playground saved; ";

// --- 3. The two demos -------------------------------------------------------------------
var arena = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ArenaShooter.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var ap = GameObject.Find("Player");
var hudGo = GameObject.Find("HUD");
var goal = hudGo.GetComponent<TopDownKit.ArenaGoal>();
goal.hud = Screens(hudGo, ap.GetComponent<TopDownKit.Health>(), ap.GetComponent<TopDownKit.Gun>(), null, null,
    "WASD move  mouse aim  click shoot  R reload\n1-3 / wheel weapon  Space dash  Esc pause");
EditorUtility.SetDirty(goal);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(arena);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(arena);
log += "ArenaShooter saved; ";

var rpg = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/LittleRpg.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var rp = GameObject.Find("Player");
var ui = GameObject.Find("RPG").transform.Find("UI").gameObject;
var rgoal = ui.GetComponent<TopDownKit.RpgGoal>();
rgoal.hud = Screens(ui, rp.GetComponent<TopDownKit.Health>(), null, rp.GetComponent<TopDownKit.Hotbar>(), rp.GetComponent<TopDownKit.Interactor>(),
    "WASD move  E use  1-5 hotbar  Q drop\nchest: click / T take all  Esc pause");
EditorUtility.SetDirty(rgoal);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(rpg);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(rpg);
log += "LittleRpg saved; ";

// --- 4. Start menu ----------------------------------------------------------------------
const string sm = "Assets/Scenes/StartMenu.unity";
var menu = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
var camGo = new GameObject("Main Camera");
camGo.tag = "MainCamera";
var cam = camGo.AddComponent<Camera>();
cam.orthographic = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Hex("#181425");
camGo.transform.position = new Vector3(0f, 0f, -10f);
camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
new GameObject("Start Menu").AddComponent<TopDownKit.StartMenu>();
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(menu, sm);
log += "StartMenu saved; ";

EditorBuildSettings.scenes = new[] {
    new EditorBuildSettingsScene(sm, true), new EditorBuildSettingsScene("Assets/Scenes/ArenaShooter.unity", true),
    new EditorBuildSettingsScene("Assets/Scenes/LittleRpg.unity", true), new EditorBuildSettingsScene(pg, true) };
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(pg, UnityEditor.SceneManagement.OpenSceneMode.Single);
return log + "build list: StartMenu, ArenaShooter, LittleRpg, Playground";

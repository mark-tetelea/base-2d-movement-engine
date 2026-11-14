// One-off setup for phase 4, part 2 (EDIT mode, after setup-phase4-assets.cs):
//     unity command eval_file --file Tools/measure/setup-phase4-scene.cs
// Copies TestArena into Assets/Scenes/ArenaShooter.unity (once) and turns it into the
// Arena shooter demo: test keys, zones and the aim marker out; Glide, face the aim,
// health, guns, restart on death; rooms slide; B, C and D lock with enemy waves; a HUD.
// Adds both scenes to the build list (the restart reloads by build index).
// Safe to re-run: it rebuilds the waves and updates the rest.
if (EditorApplication.isPlaying) return "exit play mode first";
const string path = "Assets/Scenes/ArenaShooter.unity";
string log = "";
if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
{
    AssetDatabase.CopyAsset("Assets/Scenes/TestArena.unity", path);
    log += "copied; ";
}
// Trap 73: open the scene first, then load assets.
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);

T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
var glide = Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Glide.asset");
var pistol = Load<TopDownKit.Weapon>("Assets/Data/Weapons/Pistol.asset");
var shotgun = Load<TopDownKit.Weapon>("Assets/Data/Weapons/Shotgun.asset");
var rapid = Load<TopDownKit.Weapon>("Assets/Data/Weapons/Rapid.asset");
var chaser = Load<GameObject>("Assets/Prefabs/Enemies/Chaser.prefab");
var shooter = Load<GameObject>("Assets/Prefabs/Enemies/Shooter.prefab");
var wanderer = Load<GameObject>("Assets/Prefabs/Enemies/Wanderer.prefab");
var square = Load<Sprite>("Assets/Art/Placeholders/white-square.png");

void Kill(string name) { var g = GameObject.Find(name); if (g != null) { UnityEngine.Object.DestroyImmediate(g); log += "removed " + name + "; "; } }
Kill("Feel Keys (test arena)");
Kill("Zones");
Kill("Aim Marker");

var player = GameObject.Find("Player");
foreach (var k in player.GetComponents<TopDownKit.TestArena.KnockKey>()) UnityEngine.Object.DestroyImmediate(k);
foreach (var k in UnityEngine.Object.FindObjectsByType<TopDownKit.TestArena.KnockKey>(FindObjectsSortMode.None)) UnityEngine.Object.DestroyImmediate(k);
Transform[] Children(GameObject g) { var a = new Transform[g.transform.childCount]; for (int i = 0; i < a.Length; i++) a[i] = g.transform.GetChild(i); return a; }
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }

var mover = Get<TopDownKit.PlayerMover>(player);
mover.feel = glide; mover.readKeyboard = true;
var facing = Get<TopDownKit.PlayerFacing>(player);
facing.settings = new TopDownKit.FacingSettings(TopDownKit.FacingCount.Eight, TopDownKit.FaceTowards.Aim);
var aim = Get<TopDownKit.PlayerAim>(player);
aim.aimFrom = TopDownKit.AimFrom.Mouse;
var hp = Get<TopDownKit.Health>(player);
hp.maxHealth = 6; hp.team = 0; hp.invulnerableTime = 1f; hp.flashTime = 0.1f; hp.blinkWhileInvulnerable = true;
hp.shakeOnHit = 0.3f; hp.destroyOnDeath = false;
var gun = Get<TopDownKit.Gun>(player);
gun.weapons = new[] { pistol, shotgun, rapid }; gun.team = 0; gun.muzzleDistance = 0.5f;
Get<TopDownKit.PlayerShooter>(player).numberKeysSelect = true;
Get<TopDownKit.RestartOnDeath>(player).delay = 1.5f;
Get<TopDownKit.Dash>(player);
foreach (var c in new Component[] { mover, facing, aim, hp, gun }) EditorUtility.SetDirty(c);
player.transform.position = new Vector3(-12f, 0f, 0f);
int order = player.GetComponentInChildren<SpriteRenderer>().sortingOrder;

var cam = GameObject.Find("Main Camera");
var rig = cam.GetComponent<TopDownKit.CameraRig>();
rig.layout = TopDownKit.CameraLayout.RoomsSlide;
EditorUtility.SetDirty(rig);

var hudGo = GameObject.Find("HUD") ?? new GameObject("HUD");
var hud = Get<TopDownKit.ShooterHud>(hudGo);
hud.player = hp; hud.gun = gun; hud.rig = rig;
EditorUtility.SetDirty(hud);

Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var col); return col; }
GameObject Door(Transform parent, string name, float cx, float cy, float w, float h)
{
    var t = parent.Find(name);
    var go = t != null ? t.gameObject : new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.position = new Vector3(cx, cy, 0f);
    var box = Get<BoxCollider2D>(go);
    box.size = new Vector2(w, h);
    var art = go.transform.Find("Art");
    var artGo = art != null ? art.gameObject : new GameObject("Art");
    artGo.transform.SetParent(go.transform, false);
    artGo.transform.localScale = new Vector3(w, h, 1f);
    var sr = Get<SpriteRenderer>(artGo);
    sr.sprite = square; sr.color = Hex("#b55088"); sr.sortingOrder = order + 1;
    return go;
}

// Waves: switched-off children of the room; enemies placed in world tiles.
TopDownKit.RoomWaves Waves(GameObject room, params (string wave, GameObject prefab, float x, float y)[] spawns)
{
    var lk = Get<TopDownKit.RoomLock>(room);
    lk.player = player.transform;
    var rw = Get<TopDownKit.RoomWaves>(room);
    foreach (Transform t in Children(room))
        if (t.name.StartsWith("Wave ")) UnityEngine.Object.DestroyImmediate(t.gameObject);
    var waves = new System.Collections.Generic.List<GameObject>();
    int seed = 1;
    foreach (var s in spawns)
    {
        var w = waves.Find(g => g.name == s.wave);
        if (w == null) { w = new GameObject(s.wave); w.transform.SetParent(room.transform, false); w.transform.position = Vector3.zero; waves.Add(w); }
        var e = (GameObject)PrefabUtility.InstantiatePrefab(s.prefab, w.transform);
        e.transform.position = new Vector3(s.x, s.y, 0f);
        var brain = e.GetComponent<TopDownKit.EnemyBrain>();
        brain.target = player.transform; brain.seed = seed++;
        var eg = e.GetComponent<TopDownKit.Gun>(); if (eg != null) eg.seed = seed++;
        e.GetComponentInChildren<SpriteRenderer>().sortingOrder = order;
        PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
        if (eg != null) PrefabUtility.RecordPrefabInstancePropertyModifications(eg);
    }
    waves.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    foreach (var w in waves) w.SetActive(false);
    rw.waves = waves.ToArray();
    EditorUtility.SetDirty(lk); EditorUtility.SetDirty(rw);
    log += room.name + " " + waves.Count + " waves, " + spawns.Length + " enemies; ";
    return rw;
}

var roomA = GameObject.Find("Room A");
var roomB = GameObject.Find("Room B");
var roomC = GameObject.Find("Room C");
var areaD = GameObject.Find("Area D");

// Room A: two wanderers to warm up on, loose (no lock).
foreach (Transform t in Children(roomA)) if (t.name.StartsWith("Wanderer")) UnityEngine.Object.DestroyImmediate(t.gameObject);
foreach (var p in new[] { new Vector2(6f, 4f), new Vector2(8f, -5f) })
{
    var e = (GameObject)PrefabUtility.InstantiatePrefab(wanderer, roomA.transform);
    e.transform.position = p;
    var brain = e.GetComponent<TopDownKit.EnemyBrain>(); brain.target = player.transform; brain.seed = (int)p.y + 10;
    PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
}

Waves(roomB,
    ("Wave 1", chaser, 48f, 5f), ("Wave 1", chaser, 48f, -5f), ("Wave 1", chaser, 54f, 0f),
    ("Wave 2", shooter, 52f, 6f), ("Wave 2", shooter, 52f, -6f), ("Wave 2", chaser, 56f, 0f));

// Room C gets its own doors in its own wall row, beside B's north door.
var lockC = Get<TopDownKit.RoomLock>(roomC);
lockC.doors = new[] { Door(roomC.transform, "Door south-west", 13.5f, 11.5f, 3f, 1f), Door(roomC.transform, "Door south-east", 39.5f, 11.5f, 3f, 1f) };
Waves(roomC,
    ("Wave 1", wanderer, 0f, 30f), ("Wave 1", wanderer, 20f, 32f), ("Wave 1", chaser, 30f, 26f), ("Wave 1", chaser, 5f, 22f),
    ("Wave 2", shooter, -8f, 30f), ("Wave 2", shooter, 25f, 36f), ("Wave 2", shooter, 45f, 28f),
    ("Wave 3", chaser, -14f, 28f), ("Wave 3", chaser, 52f, 36f), ("Wave 3", shooter, 20f, 18f), ("Wave 3", chaser, 20f, 38f));

// Area D (reached by the warp): no doors, the final waves.
Waves(areaD,
    ("Wave 1", shooter, 114f, 3f), ("Wave 1", shooter, 114f, -3f), ("Wave 1", chaser, 116f, 0f),
    ("Wave 2", chaser, 112f, 4f), ("Wave 2", chaser, 112f, -4f), ("Wave 2", chaser, 117f, 3f), ("Wave 2", chaser, 117f, -3f), ("Wave 2", shooter, 118f, 0f));
EditorUtility.SetDirty(lockC);

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();
list.Add(new EditorBuildSettingsScene(path, true));
list.Add(new EditorBuildSettingsScene("Assets/Scenes/TestArena.unity", true));
EditorBuildSettings.scenes = list.ToArray();
return log + "saved; build list: ArenaShooter, TestArena";

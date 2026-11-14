// One-off setup for phase 1 (run in EDIT mode with TestArena open):
//     unity command eval_file --file Tools/measure/setup-phase1.cs
// Makes the Ice and Heavy feels, gives every feel a knockback stop rate, adds the Dash and
// knock key to the arena, and lays out three floor zones in the free strip at the top.
// Safe to re-run: it updates what is already there.
if (EditorApplication.isPlaying) return "exit play mode first";
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (!scene.path.EndsWith("TestArena.unity")) return "open TestArena first, not " + scene.path;
string dir = "Assets/Data/Movement/";
string log = "";

TopDownKit.MoveFeel Feel(string name, float speed, float full, float stop, float kb)
{
    var f = AssetDatabase.LoadAssetAtPath<TopDownKit.MoveFeel>(dir + name + ".asset");
    bool isNew = f == null;
    if (isNew) f = ScriptableObject.CreateInstance<TopDownKit.MoveFeel>();
    var s = f.settings;
    if (isNew) { s.speed = speed; s.timeToFullSpeed = full; s.timeToStop = stop; }
    if (s.knockbackStopRate <= 0f) s.knockbackStopRate = kb;
    f.settings = s;
    if (isNew) AssetDatabase.CreateAsset(f, dir + name + ".asset"); else EditorUtility.SetDirty(f);
    log += name + (isNew ? " made; " : " updated; ");
    return f;
}
var instant = Feel("Instant", 6f, 0f, 0f, 40f);
var glide = Feel("Glide", 6f, 0.4f, 0.8f, 40f);
var ice = Feel("Ice", 6f, 1.0f, 2.0f, 12f);
var heavy = Feel("Heavy", 4f, 0.5f, 0.15f, 80f);
AssetDatabase.SaveAssets();

// A plain white 16x16 square for the zone placeholders (tinted per zone).
string sqPath = "Assets/Art/Placeholders/white-square.png";
if (AssetDatabase.LoadAssetAtPath<Sprite>(sqPath) == null)
{
    System.IO.Directory.CreateDirectory("Assets/Art/Placeholders");
    var tex = new Texture2D(16, 16);
    var px = new Color[256];
    for (int i = 0; i < 256; i++) px[i] = Color.white;
    tex.SetPixels(px);
    System.IO.File.WriteAllBytes(sqPath, tex.EncodeToPNG());
    AssetDatabase.ImportAsset(sqPath);
    log += "square made; ";
}
var square = AssetDatabase.LoadAssetAtPath<Sprite>(sqPath);
if (square == null) return log + "square sprite did not import";

var player = GameObject.Find("Player");
var mover = player.GetComponent<TopDownKit.PlayerMover>();
if (player.GetComponent<TopDownKit.Dash>() == null) player.AddComponent<TopDownKit.Dash>();
var keysGo = GameObject.Find("Feel Keys (test arena)");
var feelKeys = keysGo.GetComponent<TopDownKit.TestArena.FeelKeys>();
feelKeys.feels = new[] { instant, glide, ice, heavy };
var knock = keysGo.GetComponent<TopDownKit.TestArena.KnockKey>();
if (knock == null) knock = keysGo.AddComponent<TopDownKit.TestArena.KnockKey>();
knock.player = mover;
EditorUtility.SetDirty(feelKeys);
EditorUtility.SetDirty(knock);

int playerOrder = player.GetComponentInChildren<SpriteRenderer>().sortingOrder;
var zonesRoot = GameObject.Find("Zones");
if (zonesRoot == null) zonesRoot = new GameObject("Zones");

void Zone(string name, float cx, float cy, float w, float h, float scale, TopDownKit.MoveFeel feel, Color color)
{
    var t = zonesRoot.transform.Find(name);
    var go = t != null ? t.gameObject : new GameObject(name);
    go.transform.SetParent(zonesRoot.transform, false);
    go.transform.position = new Vector3(cx, cy, 0f);
    var box = go.GetComponent<BoxCollider2D>();
    if (box == null) box = go.AddComponent<BoxCollider2D>();
    box.isTrigger = true;
    box.size = new Vector2(w, h);
    var z = go.GetComponent<TopDownKit.SpeedZone>();
    if (z == null) z = go.AddComponent<TopDownKit.SpeedZone>();
    z.speedScale = scale;
    z.feel = feel;
    // Art on a child at the zone's size; the root stays at scale 1 (traps 8 and 9).
    var art = go.transform.Find("Art");
    var artGo = art != null ? art.gameObject : new GameObject("Art");
    artGo.transform.SetParent(go.transform, false);
    artGo.transform.localScale = new Vector3(w, h, 1f);
    var sr = artGo.GetComponent<SpriteRenderer>();
    if (sr == null) sr = artGo.AddComponent<SpriteRenderer>();
    sr.sprite = square;
    sr.color = color;
    sr.sortingOrder = playerOrder - 1;
}
Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); c.a = 0.55f; return c; }
// Free floor: cells x -18..5, y 6..8 (world y 6..9). Harness runs stay at y <= 1.
Zone("Speed pad", -14.5f, 7.5f, 5f, 3f, 1.6f, null, Hex("#feae34"));
Zone("Mud", -7.5f, 7.5f, 5f, 3f, 0.5f, null, Hex("#733e39"));
Zone("Ice patch", 0.5f, 7.5f, 7f, 3f, 1f, ice, Hex("#2ce8f5"));

UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return log + "player order " + playerOrder + "; scene saved";

// One-off setup for phase 8, part 1 (EDIT mode, after the tile PNGs from pxkit were exported):
//     unity command eval_file --file Tools/measure/setup-phase8-assets.cs
// Makes the ground tile assets (Assets/Data/Tiles/) and the grid-step feels
// (Assets/Data/Movement/). Safe to re-run: existing assets are updated, not duplicated.
// Part 2 (setup-phase8-scene.cs) puts them in the Playground.
if (EditorApplication.isPlaying) return "exit play mode first";
string log = "";
T Asset<T>(string path) where T : ScriptableObject
{
    var a = AssetDatabase.LoadAssetAtPath<T>(path);
    if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); log += "made " + path + "; "; }
    return a;
}
if (!AssetDatabase.IsValidFolder("Assets/Data/Tiles")) AssetDatabase.CreateFolder("Assets/Data", "Tiles");

Sprite S(string name)
{
    string p = "Assets/Art/Generated/" + name + ".png";
    var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
    if (s == null) { log += "MISSING " + p + "; "; return null; }
    if (Mathf.Abs(s.pixelsPerUnit - 16f) > 0.01f || s.rect.width != 16f) log += "BAD IMPORT " + p + " ppu " + s.pixelsPerUnit + " w " + s.rect.width + "; ";
    return s;
}

TopDownKit.GroundTile G(string name, Sprite sprite, TopDownKit.GroundRules rules)
{
    var g = Asset<TopDownKit.GroundTile>("Assets/Data/Tiles/" + name + ".asset");
    g.sprite = sprite; g.color = Color.white; g.rules = rules;
    g.breakAfter = 0f; g.backAfter = 0f; g.warningSprite = null; g.brokenSprite = null;
    g.whenBroken = new TopDownKit.GroundRules { kills = true };
    g.isDoor = false; g.closedSprite = null;
    g.whenClosed = new TopDownKit.GroundRules { blocksWalking = true, blocksBullets = true };
    return g;
}

var normal = G("Normal", S("floor"), default);
var red = G("Red", S("ground-red"), new TopDownKit.GroundRules { kills = true });
var blue = G("Blue", S("ground-blue"), new TopDownKit.GroundRules { blocksWalking = true, blocksBullets = false });
var crumble = G("Crumbling", S("crumble"), default);
crumble.breakAfter = 0.75f; crumble.backAfter = 3f;
crumble.warningSprite = S("crumble-warning"); crumble.brokenSprite = S("crumble-broken");
crumble.whenBroken = new TopDownKit.GroundRules { kills = true };
var door = G("Door", S("door-open"), default);
door.isDoor = true; door.closedSprite = S("door-closed");
door.whenClosed = new TopDownKit.GroundRules { blocksWalking = true, blocksBullets = true };
foreach (var g in new[] { normal, red, blue, crumble, door }) EditorUtility.SetDirty(g);

TopDownKit.MoveFeel F(string name, float speed, float stepTime, bool hold, bool eight)
{
    var f = Asset<TopDownKit.MoveFeel>("Assets/Data/Movement/" + name + ".asset");
    f.settings = new TopDownKit.MoveSettings { speed = speed, timeToFullSpeed = 0f, timeToStop = 0f, knockbackStopRate = 40f,
        gridStep = true, stepTime = stepTime, holdToWalk = hold, eightDirections = eight };
    EditorUtility.SetDirty(f);
    return f;
}
F("Grid", 5f, 0.2f, true, false);
F("Grid 8", 5f, 0.2f, true, true);
F("Grid tap", 8f, 0.1f, false, false);
AssetDatabase.SaveAssets();
return log + "ok";

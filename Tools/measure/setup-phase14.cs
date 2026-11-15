// One-off setup for phase 14, make everything swappable (EDIT mode):
//     unity command eval_file --file Tools/measure/setup-phase14.cs
// - Resources/KitControls (every key as a setting) and Resources/KitTheme (the UI look).
// - The player's art moves from DirectionalSprites' old 3-frame sheets into a
//   SpriteAnimSet asset (Assets/Data/Art/PlayerAnims): Idle = frame 0, Walk = step, idle,
//   step, idle at the old walk rate - the same look as before.
// - HUD help lines name controls ({Move}, {Use}...) so they follow rebinds.
// Safe to re-run: assets are reused, scenes already moved over are left alone.
// NOTE: the player part needed DirectionalSprites' old fields (directions, mirrorLeft,
// walkFps); they were removed after this ran (2026-11-15), so that part no longer compiles
// as is - kept as the record of how PlayerAnims was made.
if (EditorApplication.isPlaying) return "exit play mode first";
if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty) return "the open scene has unsaved changes; save or close it first";
string log = "";

void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }

// ---- controls and theme ----------------------------------------------------------------
Folder("Assets", "Resources");
if (AssetDatabase.LoadAssetAtPath<TopDownKit.ControlSet>("Assets/Resources/KitControls.asset") == null)
{
    var set = ScriptableObject.CreateInstance<TopDownKit.ControlSet>();
    set.rows = TopDownKit.ControlSet.KitDefaults();
    AssetDatabase.CreateAsset(set, "Assets/Resources/KitControls.asset");
    log += "made KitControls (" + set.rows.Count + " rows)\n";
}
if (AssetDatabase.LoadAssetAtPath<TopDownKit.UITheme>("Assets/Resources/KitTheme.asset") == null)
{
    AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TopDownKit.UITheme>(), "Assets/Resources/KitTheme.asset");
    log += "made KitTheme\n";
}

// ---- player animations -----------------------------------------------------------------
Folder("Assets/Data", "Art");
const string animPath = "Assets/Data/Art/PlayerAnims.asset";

TopDownKit.SpriteAnimSet FromLegacy(TopDownKit.DirectionalSprites d)
{
    var set = AssetDatabase.LoadAssetAtPath<TopDownKit.SpriteAnimSet>(animPath);
    if (set != null) return set;
    set = ScriptableObject.CreateInstance<TopDownKit.SpriteAnimSet>();
    set.mirrorLeft = d.mirrorLeft;
    var idle = new TopDownKit.SpriteAnim { name = "Idle", fps = 1f, loop = true, facings = new TopDownKit.SpriteFrames[8] };
    var walk = new TopDownKit.SpriteAnim { name = "Walk", fps = d.walkFps, loop = true, facings = new TopDownKit.SpriteFrames[8] };
    for (int f = 0; f < 8; f++)
    {
        Sprite[] fr = d.directions != null && f < d.directions.Length && d.directions[f] != null ? d.directions[f].frames : null;
        bool has = fr != null && fr.Length > 0 && fr[0] != null;
        idle.facings[f] = new TopDownKit.SpriteFrames { frames = has ? new[] { fr[0] } : new Sprite[0] };
        Sprite a = has && fr.Length > 1 && fr[1] != null ? fr[1] : has ? fr[0] : null;
        Sprite b = has && fr.Length > 2 && fr[2] != null ? fr[2] : a;
        walk.facings[f] = new TopDownKit.SpriteFrames { frames = has ? new[] { a, fr[0], b, fr[0] } : new Sprite[0] };
    }
    set.anims = new[] { idle, walk };
    AssetDatabase.CreateAsset(set, animPath);
    log += "made PlayerAnims from " + d.gameObject.name + "\n";
    return set;
}

string[] scenes = { "Assets/Scenes/Playground.unity", "Assets/Scenes/ArenaShooter.unity", "Assets/Scenes/LittleRpg.unity", "Assets/Scenes/Dungeon.unity" };
var helps = new System.Collections.Generic.Dictionary<string, string>
{
    { "Playground", "{Move} move  mouse aim  {Fire} shoot  {Reload} reload  1-6 weapon\n{Dash} dash\n{Use} use  {Knock} knockback  {Picker} settings  {Bag} bag  {Drop} drop  {Pause} pause" },
    { "ArenaShooter", "{Move} move  mouse aim  {Fire} shoot  {Reload} reload\n1-3 / wheel weapon  {Dash}\ndash  {Pause} pause  {Map} map" },
    { "Dungeon", "{Move} move  mouse aim  {Fire} shoot  {Reload} reload\n1-3 / wheel weapon  {Dash}\ndash  {Pause} pause  {Map} map" },
    { "LittleRpg", "{Move} move  {Use} use  1-5 hotbar  {Drop} drop  {Bag} bag\nchest: click / {TakeAll} take all\n{Pause} pause  {Map} map" },
};
foreach (string path in scenes)
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
    bool dirty = false;
    foreach (var d in UnityEngine.Object.FindObjectsByType<TopDownKit.DirectionalSprites>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    {
        if (d.art != null) continue;
        d.art = FromLegacy(d);
        EditorUtility.SetDirty(d);
        dirty = true;
        log += scene.name + ": " + d.gameObject.name + " uses PlayerAnims\n";
    }
    foreach (var h in UnityEngine.Object.FindObjectsByType<TopDownKit.Hud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
    {
        if (!helps.TryGetValue(scene.name, out string text) || h.help == text) continue;
        h.help = text;
        EditorUtility.SetDirty(h);
        dirty = true;
        log += scene.name + ": help line uses control names\n";
    }
    if (dirty) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
AssetDatabase.SaveAssets();
return log == "" ? "nothing to do" : log;

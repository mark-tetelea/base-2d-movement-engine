// One-off setup for phase 5, part 1 (EDIT mode):
//     unity command eval_file --file Tools/measure/setup-phase5-assets.cs
// Pivots of the new floor-standing sprites (bottom-centre on the feet row), the items
// (Coin, Key, Crown) and the demo's dialogue lines. Safe to re-run: updates in place.
if (EditorApplication.isPlaying) return "exit play mode first";
string log = "";
// Chest and lever stand on the floor; their art's bottom row is 1 px up (ART.md rule 7).
foreach (var n in new[] { "chest", "chest-open", "lever" })
{
    var path = "Assets/Art/Generated/" + n + ".png";
    var ti = (TextureImporter)AssetImporter.GetAtPath(path);
    var st = new TextureImporterSettings();
    ti.ReadTextureSettings(st);
    st.spriteAlignment = (int)SpriteAlignment.Custom;
    st.spritePivot = new Vector2(0.5f, 1f / 16f);
    ti.SetTextureSettings(st);
    ti.SaveAndReimport();
    log += n + " pivot " + AssetDatabase.LoadAssetAtPath<Sprite>(path).pivot + "; ";
}

if (!AssetDatabase.IsValidFolder("Assets/Data/Items")) AssetDatabase.CreateFolder("Assets/Data", "Items");
if (!AssetDatabase.IsValidFolder("Assets/Data/Dialogue")) AssetDatabase.CreateFolder("Assets/Data", "Dialogue");

T Asset<T>(string path) where T : ScriptableObject
{
    var a = AssetDatabase.LoadAssetAtPath<T>(path);
    if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); log += "made " + path + "; "; }
    return a;
}
void Item(string n, string shown, int stack, string sprite)
{
    var i = Asset<TopDownKit.Item>("Assets/Data/Items/" + n + ".asset");
    i.displayName = shown; i.maxStack = stack;
    i.icon = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/" + sprite + ".png");
    if (i.icon == null) throw new System.Exception("no sprite " + sprite);
    EditorUtility.SetDirty(i);
}
Item("Coin", "Coin", 99, "coin");
Item("Key", "Key", 1, "key");
Item("Crown", "Crown", 1, "crown");

void Lines(string n, string speaker, params string[] lines)
{
    var d = Asset<TopDownKit.DialogueLines>("Assets/Data/Dialogue/" + n + ".asset");
    d.speaker = speaker; d.lines = lines;
    EditorUtility.SetDirty(d);
}
Lines("Guide", "Guide",
    "Hello! The crown is in the room to the east, but that door is locked.",
    "I hid the key in a chest in the big room up north.",
    "Pick up coins on the way. The chest beside me is yours to keep things in.");
Lines("Keeper", "Keeper",
    "You got through! The crown is in the chest over there.",
    "Take it and the room is yours.");
Lines("Hermit", "Hermit",
    "You found the lever and the warp. Nothing to do here but count your coins.");
AssetDatabase.SaveAssets();
return log + "saved";

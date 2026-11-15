// Opens every scene in the build list and every prefab, and counts components whose
// script is missing (EDIT mode). After moving or deleting script folders this should
// say 0 everywhere.
//     unity command eval_file --file Tools/measure/missing-scripts.cs
if (EditorApplication.isPlaying) return "exit play mode first";
if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty) return "the open scene has unsaved changes; save or close it first";
string log = "";
int total = 0;
int Count(GameObject g)
{
    int n = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g);
    foreach (Transform c in g.transform) n += Count(c.gameObject);
    return n;
}
foreach (var s in EditorBuildSettings.scenes)
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(s.path, UnityEditor.SceneManagement.OpenSceneMode.Single);
    int n = 0;
    foreach (GameObject g in scene.GetRootGameObjects()) n += Count(g);
    log += scene.name + ": " + n + "\n";
    total += n;
}
int prefabs = 0;
foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
{
    string p = AssetDatabase.GUIDToAssetPath(guid);
    var g = AssetDatabase.LoadAssetAtPath<GameObject>(p);
    int n = g != null ? Count(g) : 0;
    prefabs++;
    if (n > 0) { log += p + ": " + n + "\n"; total += n; }
}
return log + prefabs + " prefabs looked at\nmissing scripts in all: " + total;

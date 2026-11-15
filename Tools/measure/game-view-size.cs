// Sets the Game view to one of Unity's built-in sizes, by the start of its name (EDIT or
// PLAY mode). Phase 15, for running screens.cs at both sizes:
//     unity command eval --code 'System.AppDomain.CurrentDomain.SetData("gv_want", "WXGA"); return "ok";'
//     unity command eval_file --file Tools/measure/game-view-size.cs
// "Full HD" = 1920x1080 (the project's size), "WXGA" = 1366x768 (trap 32's test size).
// Uses editor internals by reflection (GameView, GameViewSizes); says what it found.
string want = System.AppDomain.CurrentDomain.GetData("gv_want") as string ?? "Full HD";
var asm = typeof(UnityEditor.EditorWindow).Assembly;
var gvType = asm.GetType("UnityEditor.GameView");
var sizesType = asm.GetType("UnityEditor.GameViewSizes");
var single = typeof(UnityEditor.ScriptableSingleton<>).MakeGenericType(sizesType);
var sizes = single.GetProperty("instance").GetValue(null);
var groupType = sizesType.GetProperty("currentGroupType").GetValue(sizes);
var group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { groupType });
var texts = (string[])group.GetType().GetMethod("GetDisplayTexts").Invoke(group, null);
int index = -1;
for (int i = 0; i < texts.Length; i++) if (texts[i].StartsWith(want)) { index = i; break; }
if (index < 0) return "no size starting with '" + want + "' in: " + string.Join(" | ", texts);
var gv = UnityEditor.EditorWindow.GetWindow(gvType, false, null, false);
var select = gvType.GetMethod("SizeSelectionCallback", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
if (select == null) return "GameView.SizeSelectionCallback not found";
select.Invoke(gv, new object[] { index, null });
return "game view size: " + texts[index] + " (index " + index + ")";

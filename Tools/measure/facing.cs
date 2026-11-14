// Facing check. Not compiled by Unity (it lives outside Assets/). Run IN PLAY MODE on
// Assets/Scenes/TestArena.unity, from the project root:
//
//     unity command eval_file --file Tools/measure/facing.cs
//
// then poll until it says done (about 10 s):
//
//     unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("fc_state") as string;'
//
// It drives the REAL input path - a virtual keyboard and a virtual mouse, read by
// PlayerMover and PlayerAim like real ones - and checks, case by case, the facing, the
// sprite shown and the flip. Cases: 8 walk directions (left ones flip), standing still
// keeps the facing, 4 directions (diagonal keeps the current facing), face-the-aim while
// walking the other way, aim from walk. It puts the settings back when done.
// eval runs this as a method body: no using lines, so names are written out in full.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on TestArena first";
if (System.AppDomain.CurrentDomain.GetData("fc_state") as string == "running") return "already running";

var player = GameObject.Find("Player");
var facing = player.GetComponent<TopDownKit.PlayerFacing>();
var aim = player.GetComponent<TopDownKit.PlayerAim>();
var sr = player.GetComponent<SpriteRenderer>();
var cam = Camera.main;
var oldSettings = facing.settings;
var oldAimFrom = aim.aimFrom;
var oldView = aim.view;

var settings = UnityEngine.InputSystem.InputSystem.settings;
var oldEditorBehaviour = settings.editorInputBehaviorInPlayMode;
var oldBackground = settings.backgroundBehavior;
settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
Application.runInBackground = true;

var kb = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("FacingKeyboard");
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("FacingMouse");
mouse.MakeCurrent(); kb.MakeCurrent();
var W = UnityEngine.InputSystem.Key.W; var A = UnityEngine.InputSystem.Key.A;
var S = UnityEngine.InputSystem.Key.S; var D = UnityEngine.InputSystem.Key.D;

// Each case: what to set, which keys, where the mouse points (world offset from the
// player, or none), how many frames to hold, and what to expect.
var cases = new System.Collections.Generic.List<object[]>();
// name, count, towards, aimFrom, keys, mouseOffset(or null), expected facing(s), expect flip
System.Func<string, TopDownKit.FacingCount, TopDownKit.FaceTowards, TopDownKit.AimFrom, UnityEngine.InputSystem.Key[], object, int[], bool, object[]> C =
    (n, c, t, a, k, m, e, f) => new object[] { n, c, t, a, k, m, e, f };
var E8 = TopDownKit.FacingCount.Eight; var F4 = TopDownKit.FacingCount.Four;
var Walk = TopDownKit.FaceTowards.Walk; var Aim = TopDownKit.FaceTowards.Aim;
var Mouse = TopDownKit.AimFrom.Mouse; var FromWalk = TopDownKit.AimFrom.Walk;
var none = new UnityEngine.InputSystem.Key[0];
cases.Add(C("walk right", E8, Walk, Mouse, new[] { D }, null, new[] { 0 }, false));
cases.Add(C("walk up-right", E8, Walk, Mouse, new[] { W, D }, null, new[] { 1 }, false));
cases.Add(C("walk up", E8, Walk, Mouse, new[] { W }, null, new[] { 2 }, false));
cases.Add(C("walk up-left", E8, Walk, Mouse, new[] { W, A }, null, new[] { 3 }, true));
cases.Add(C("walk left", E8, Walk, Mouse, new[] { A }, null, new[] { 4 }, true));
cases.Add(C("walk down-left", E8, Walk, Mouse, new[] { S, A }, null, new[] { 5 }, true));
cases.Add(C("walk down", E8, Walk, Mouse, new[] { S }, null, new[] { 6 }, false));
cases.Add(C("walk down-right", E8, Walk, Mouse, new[] { S, D }, null, new[] { 7 }, false));
cases.Add(C("stand still keeps down-right", E8, Walk, Mouse, none, null, new[] { 7 }, false));
cases.Add(C("4 dirs: right", F4, Walk, Mouse, new[] { D }, null, new[] { 0 }, false));
cases.Add(C("4 dirs: up-right keeps right", F4, Walk, Mouse, new[] { W, D }, null, new[] { 0 }, false));
cases.Add(C("4 dirs: up", F4, Walk, Mouse, new[] { W }, null, new[] { 2 }, false));
cases.Add(C("4 dirs: up-left keeps up", F4, Walk, Mouse, new[] { W, A }, null, new[] { 2 }, false));
cases.Add(C("4 dirs: down-left from up goes left", F4, Walk, Mouse, new[] { S, A }, null, new[] { 4 }, true));
cases.Add(C("face aim (mouse left) while walking right", E8, Aim, Mouse, new[] { D }, new Vector2(-4f, 0f), new[] { 4 }, true));
cases.Add(C("face aim (mouse up-right) while walking down", E8, Aim, Mouse, new[] { S }, new Vector2(3f, 3f), new[] { 1 }, false));
cases.Add(C("face walk while mouse is left", E8, Walk, Mouse, new[] { D }, new Vector2(-4f, 0f), new[] { 0 }, false));
cases.Add(C("aim from walk: face aim, walk up", E8, Aim, FromWalk, new[] { W }, new Vector2(-4f, 0f), new[] { 2 }, false));

int index = -1, startFrame = 0;
var results = new System.Collections.Generic.List<string>();
int failed = 0;
const int HOLD = 12;   // game frames to hold each case before reading it

void Finish(string error)
{
    EditorApplication.update -= Tick;
    UnityEngine.InputSystem.InputSystem.RemoveDevice(kb);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    facing.settings = oldSettings; aim.aimFrom = oldAimFrom; aim.view = oldView;
    settings.editorInputBehaviorInPlayMode = oldEditorBehaviour;
    settings.backgroundBehavior = oldBackground;
    string body = string.Join("\n", results);
    System.AppDomain.CurrentDomain.SetData("fc_state", error != null ? "error: " + error + "\n" + body
        : "done: " + (cases.Count - failed) + "/" + cases.Count + " passed\n" + body);
}

void Start(int i)
{
    var c = cases[i];
    facing.settings = new TopDownKit.FacingSettings((TopDownKit.FacingCount)c[1], (TopDownKit.FaceTowards)c[2]);
    aim.aimFrom = (TopDownKit.AimFrom)c[3];
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState((UnityEngine.InputSystem.Key[])c[4]));
}

void PointMouse(object offset)
{
    if (offset == null) return;
    Vector3 world = player.transform.position + (Vector3)(Vector2)offset;
    Vector3 screen = cam.WorldToScreenPoint(world);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = new Vector2(screen.x, screen.y) });
}

void Tick()
{
    try
    {
        if (!EditorApplication.isPlaying) { Finish("play mode ended"); return; }
        if (index < 0) { index = 0; startFrame = Time.frameCount; Start(0); return; }

        PointMouse(cases[index][5]);   // the player moves, so re-point every frame
        if (Time.frameCount - startFrame < HOLD) return;
        var c = cases[index];
        int[] want = (int[])c[6];
        bool wantFlip = (bool)c[7];
        int got = facing.Current;
        int drawn = TopDownKit.Facing.PointsLeft(got) ? TopDownKit.Facing.Mirror(got) : got;
        string[] names = { "right", "up-right", "up", "up-left", "left", "down-left", "down", "down-right" };
        string sprite = sr.sprite != null ? sr.sprite.name : "none";
        bool spriteOk = sprite.StartsWith("player-" + names[drawn] + "_");
        bool ok = System.Array.IndexOf(want, got) >= 0 && sr.flipX == wantFlip && spriteOk;
        if (!ok) failed++;
        results.Add((ok ? "PASS " : "FAIL ") + c[0] + ": facing " + got + " (want " + string.Join("/", want) + "), flip "
            + sr.flipX + " (want " + wantFlip + "), sprite " + sprite + ", aim " + aim.Direction.ToString("F2"));
        index++; startFrame = Time.frameCount;
        if (index >= cases.Count) { UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState()); Finish(null); return; }
        Start(index);
    }
    catch (System.Exception e) { Finish(e.ToString()); }
}

System.AppDomain.CurrentDomain.SetData("fc_state", "running");
EditorApplication.update += Tick;
return "started " + cases.Count + " cases";

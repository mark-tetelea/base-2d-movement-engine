// Movement measuring harness. Not compiled by Unity (it lives outside Assets/).
// Run IN PLAY MODE on Assets/Scenes/TestArena.unity, from the project root:
//
//     unity command eval_file --file Tools/measure/movement.cs
//
// then poll until it says done (about 60 s of play):
//
//     unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("mv_state") as string;'
//
// and read the result with: node Tools/measure/analyse-movement.js
//
// It drives the REAL key path: a virtual keyboard fed with queued key events, read by
// PlayerMover like any keyboard. A recorder appended to the FixedUpdate player loop logs
// the player's body after every fixed step into Temp/measure-movement.csv. It changes the
// editor's input routing, vsync and frame cap for the run and puts all of them back.
// eval runs this as a method body: no using lines, so names are written out in full.
// Numbers are written with the invariant culture (this editor's locale uses decimal commas).

if (!EditorApplication.isPlaying) return "not playing - enter play mode on TestArena first";
if (System.AppDomain.CurrentDomain.GetData("mv_state") as string == "running") return "already running";
string outPath = System.IO.Path.GetFullPath(Application.dataPath + "/../Temp/measure-movement.csv");
var inv = System.Globalization.CultureInfo.InvariantCulture;

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var startFeel = mover.feel;
var instant = AssetDatabase.LoadAssetAtPath<TopDownKit.MoveFeel>("Assets/Data/Movement/Instant.asset");
var glide = AssetDatabase.LoadAssetAtPath<TopDownKit.MoveFeel>("Assets/Data/Movement/Glide.asset");
var W = UnityEngine.InputSystem.Key.W; var A = UnityEngine.InputSystem.Key.A;
var S = UnityEngine.InputSystem.Key.S; var D = UnityEngine.InputSystem.Key.D;

var settings = UnityEngine.InputSystem.InputSystem.settings;
var oldEditorBehaviour = settings.editorInputBehaviorInPlayMode;
var oldBackground = settings.backgroundBehavior;
int oldVsync = QualitySettings.vSyncCount;
int oldTarget = Application.targetFrameRate;
settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
QualitySettings.vSyncCount = 0;
Application.runInBackground = true;
var kb = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("MeasureKeyboard");
kb.MakeCurrent();

// Segments, run one after another by the per-frame hook:
//   lane  - from (-15, 0): settle 0.6 s, hold D 2.0 s, release, wait 1.5 s
//   dirs  - from (0, 0): settle 0.6 s, each of 8 key combos for 0.3 s, release, wait 0.5 s
//   taps  - from (0, -2): settle 0.5 s, 10 taps of D each visible to exactly one frame
//   wall  - from (-15, 0): hold A 2.0 s into the left wall
//   slide - from (-17, -5): hold W+A 2.0 s, diagonally into the left wall and along it
var plan = new System.Collections.Generic.List<(string kind, int fps, TopDownKit.MoveFeel feel)>
{
    ("lane", 30, instant), ("lane", 144, instant), ("lane", 30, glide), ("lane", 144, glide),
    ("lane", 30, instant), ("lane", 144, instant), ("lane", 30, glide), ("lane", 144, glide),
    ("dirs", 60, instant), ("taps", 144, instant),
    ("wall", 60, instant), ("slide", 60, instant), ("wall", 60, glide), ("slide", 60, glide),
};
var combos = new UnityEngine.InputSystem.Key[][] {
    new[] { W }, new[] { W, D }, new[] { D }, new[] { S, D }, new[] { S }, new[] { S, A }, new[] { A }, new[] { W, A } };

var lines = new System.Collections.Generic.List<string> { "run,kind,fps,feel,phase,fixedTime,realtime,frame,x,y,vx,vy" };
int run = -1; string phase = "next"; float phaseStart = 0f; int sub = 0; int tapFrame = -1;
string error = null;

void Keys(params UnityEngine.InputSystem.Key[] keys) { UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys)); }
void Phase(string p) { phase = p; phaseStart = Time.time; }
string N(double v) => v.ToString("R", inv);

var original = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var systems = loop.subSystemList;

void Finish()
{
    try { System.IO.File.WriteAllLines(outPath, lines); } catch (System.Exception e) { error = (error ?? "") + " write: " + e.Message; }
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(original);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(kb);
    settings.editorInputBehaviorInPlayMode = oldEditorBehaviour;
    settings.backgroundBehavior = oldBackground;
    QualitySettings.vSyncCount = oldVsync;
    Application.targetFrameRate = oldTarget;
    mover.feel = startFeel;
    System.AppDomain.CurrentDomain.SetData("mv_state", error == null ? "done rows=" + (lines.Count - 1) + " -> " + outPath : "error: " + error);
}

void OnFixed()
{
    if (run < 0 || run >= plan.Count) return;
    var p = plan[run];
    Vector2 pos = rb.position, v = rb.linearVelocity;
    lines.Add(run + "," + p.kind + "," + p.fps + "," + p.feel.name + "," + phase + sub + "," + N(Time.fixedTime) + ","
        + N(Time.realtimeSinceStartupAsDouble) + "," + Time.frameCount + "," + N(pos.x) + "," + N(pos.y) + "," + N(v.x) + "," + N(v.y));
}

void OnFrame()
{
    try
    {
        float t = Time.time - phaseStart;
        if (phase == "next")
        {
            run++;
            if (run >= plan.Count) { Finish(); return; }
            var p = plan[run];
            Application.targetFrameRate = p.fps;
            mover.feel = p.feel;
            rb.position = p.kind == "dirs" ? new Vector2(0f, 0f) : p.kind == "taps" ? new Vector2(0f, -2f)
                : p.kind == "slide" ? new Vector2(-17f, -5f) : new Vector2(-15f, 0f);
            rb.linearVelocity = Vector2.zero;
            Keys();
            sub = 0;
            Phase("settle");
            return;
        }
        var k = plan[run].kind;
        if (phase == "settle" && t >= (k == "taps" ? 0.5f : 0.6f))
        {
            if (k == "lane") { Keys(D); Phase("hold"); }
            else if (k == "wall") { Keys(A); Phase("hold"); }
            else if (k == "slide") { Keys(W, A); Phase("hold"); }
            else if (k == "dirs") { Keys(combos[0]); Phase("dir"); }
            else { Keys(D); tapFrame = Time.frameCount; Phase("tap"); }
        }
        else if (phase == "hold" && t >= 2.0f) { Keys(); Phase("after"); }
        else if (phase == "after" && t >= 1.5f) Phase("next");
        else if (phase == "dir" && t >= 0.3f)
        {
            sub++;
            if (sub < combos.Length) { Keys(combos[sub]); Phase("dir"); }
            else { Keys(); sub = 0; Phase("after"); }
        }
        else if (phase == "tap" && Time.frameCount == tapFrame + 1) { Keys(); Phase("tapwait"); }
        else if (phase == "tapwait" && t >= 0.3f)
        {
            sub++;
            if (sub < 10) { Keys(D); tapFrame = Time.frameCount; Phase("tap"); }
            else Phase("next");
        }
    }
    catch (System.Exception e) { error = e.ToString(); Finish(); }
}

for (int i = 0; i < systems.Length; i++)
{
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.FixedUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.Movement), updateDelegate = OnFixed });
        systems[i].subSystemList = list.ToArray();
    }
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.PreLateUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Insert(0, new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.PlayerMover), updateDelegate = OnFrame });
        systems[i].subSystemList = list.ToArray();
    }
}
loop.subSystemList = systems;
System.AppDomain.CurrentDomain.SetData("mv_state", "running");
UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
return "started: " + plan.Count + " segments, about 60 s";

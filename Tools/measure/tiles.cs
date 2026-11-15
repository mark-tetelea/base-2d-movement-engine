// Phase 8 check: ground tiles and grid-step. Not compiled by Unity (outside Assets/).
// Run IN PLAY MODE on Assets/Scenes/Playground.unity, from the project root:
//
//     unity command eval_file --file Tools/measure/tiles.cs
//
// then poll until it says done (about 40 s):
//
//     unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("tl_state") as string;'
//
// Part 1 - grid step through the REAL key path (a virtual keyboard, as movement.cs), each
// feel at 30 and 144 fps: every step that is not mid-slide stands exactly on a tile centre,
// and both frame rates arrive on the same tiles in the same order. Grid, Grid 8, Grid tap.
// Part 2 - each tile rule, on the fixed step, with a dummy (a Chaser with its brain removed):
// red kills; a dash over red protects until it ends on red; damage per second lands once a
// second; blue stops free walking and grid steps but not a bullet; a closed door stops a
// bullet; crumbling breaks and comes back at the set times (at 30 and 144 fps); door ground
// opens and closes on a lever, a key and a cleared room.
// Writes Temp/measure-tiles.csv (the grid trace). Numbers use the invariant culture.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on Playground first";
if (System.AppDomain.CurrentDomain.GetData("tl_state") as string == "running") return "already running";
string outPath = System.IO.Path.GetFullPath(Application.dataPath + "/../Temp/measure-tiles.csv");
var inv = System.Globalization.CultureInfo.InvariantCulture;
string N(double v) => v.ToString("0.#####", inv);

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var interactor = player.GetComponent<TopDownKit.Interactor>();
var startFeel = mover.feel;
var startPos = rb.position;
TopDownKit.MoveFeel Feel(string n) => AssetDatabase.LoadAssetAtPath<TopDownKit.MoveFeel>("Assets/Data/Movement/" + n + ".asset");
var grid4 = Feel("Grid"); var grid8 = Feel("Grid 8"); var gridTap = Feel("Grid tap");
var chaser = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Chaser.prefab");
var pistol = AssetDatabase.LoadAssetAtPath<TopDownKit.Weapon>("Assets/Data/Weapons/Pistol.asset");
var keyItem = AssetDatabase.LoadAssetAtPath<TopDownKit.Item>("Assets/Data/Items/Key.asset");
var crumbleTile = AssetDatabase.LoadAssetAtPath<TopDownKit.GroundTile>("Assets/Data/Tiles/Crumbling.asset");
var map = GameObject.Find("Floor").GetComponent<TopDownKit.GroundMap>();
var lever = GameObject.Find("Tile garden").GetComponentInChildren<TopDownKit.Lever>();
var keyLock = GameObject.Find("Tile garden").GetComponentInChildren<TopDownKit.LockedDoor>();
var lockB = GameObject.Find("Room B").GetComponent<TopDownKit.RoomLock>();
var W = UnityEngine.InputSystem.Key.W; var A = UnityEngine.InputSystem.Key.A; var D = UnityEngine.InputSystem.Key.D;

var settings = UnityEngine.InputSystem.InputSystem.settings;
var oldEditorBehaviour = settings.editorInputBehaviorInPlayMode;
var oldBackground = settings.backgroundBehavior;
int oldVsync = QualitySettings.vSyncCount, oldTarget = Application.targetFrameRate;
settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
QualitySettings.vSyncCount = 0;
Application.runInBackground = true;
var kb = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("TilesKeyboard");
kb.MakeCurrent();
// Phase 15: the real keyboard / mouse are switched off while this runs and back on at the end.
// A real key press makes that device "current", and the virtual hold stops counting - the
// phase 8 camera flake (the player stopped mid-walk). sh Tools/measure/run-check.sh also
// switches every device back on after play mode, in case a run dies half way.
void RealDevices(bool on, params UnityEngine.InputSystem.InputDevice[] mine)
{
    foreach (var d in UnityEngine.InputSystem.InputSystem.devices)
    {
        if (System.Array.IndexOf(mine, d) >= 0) continue;
        bool same = false;
        foreach (var m in mine) if ((m is UnityEngine.InputSystem.Keyboard && d is UnityEngine.InputSystem.Keyboard) || (m is UnityEngine.InputSystem.Mouse && d is UnityEngine.InputSystem.Mouse)) same = true;
        if (!same) continue;
        if (on && !d.enabled) UnityEngine.InputSystem.InputSystem.EnableDevice(d);
        if (!on && d.enabled) UnityEngine.InputSystem.InputSystem.DisableDevice(d);
    }
}
RealDevices(false, kb);
void Keys(params UnityEngine.InputSystem.Key[] keys) { UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys)); }

var results = new System.Collections.Generic.List<string>();
int pass = 0, fail = 0;
void Check(string name, bool ok, string detail) { if (ok) pass++; else fail++; results.Add((ok ? "PASS " : "FAIL ") + name + " - " + detail); }
string error = null;
var lines = new System.Collections.Generic.List<string> { "run,feel,fps,step,x,y,sliding" };

// ---------------- Part 1: grid step, driven by frames ----------------
var plan = new System.Collections.Generic.List<(TopDownKit.MoveFeel feel, int fps)>
    { (grid4, 30), (grid4, 144), (grid8, 30), (grid8, 144), (gridTap, 30), (gridTap, 144) };
var gridStart = new Vector2(-10.5f, -2.5f);   // stand point, room A, open floor
var arrivals = new System.Collections.Generic.List<string>[plan.Count];
float maxOff = 0f; int offCount = 0, settled = 0;
int run = -1, runStep = 0; bool wasSliding = false; Vector2 lastTarget = Vector2.zero;
string phase = "next"; float phaseStart = 0f; int sub = 0, tapFrame = -1;
void Phase(string p) { phase = p; phaseStart = Time.time; }
Vector2 Offset(Rigidbody2D b) => TopDownKit.GridStep.StandPoint(b) - b.position;
void PlaceAt(Rigidbody2D b, Vector2 stand) { var p = stand - Offset(b); b.position = p; b.transform.position = p; b.linearVelocity = Vector2.zero; }

void GridFixed()
{
    if (run < 0 || run >= plan.Count || phase == "next") return;
    Vector2 s = TopDownKit.GridStep.StandPoint(rb);
    lines.Add(run + "," + plan[run].feel.name + "," + plan[run].fps + "," + runStep + "," + N(s.x) + "," + N(s.y) + "," + (mover.Sliding ? 1 : 0));
    runStep++;
    if (!mover.Sliding)
    {
        settled++;
        float off = (s - TopDownKit.GridStep.Centre(s)).magnitude;
        if (off > maxOff) maxOff = off;
        if (off > 1e-4f) offCount++;
    }
    // Every tile walked to, in order: a new slide target (each is checked to be a centre).
    if (mover.Sliding && (!wasSliding || mover.SlideTarget != lastTarget))
    {
        Vector2 t = mover.SlideTarget;
        arrivals[run].Add(N(t.x) + ":" + N(t.y));
        if ((t - TopDownKit.GridStep.Centre(t)).magnitude > 1e-4f) offCount++;
        lastTarget = t;
    }
    wasSliding = mover.Sliding;
}

// ---------------- Part 2: tile rules, on the fixed step ----------------
var tasks = new System.Collections.Generic.List<(string name, int fps, System.Func<int, bool> step)>();
int task = -1, ts = 0;
GameObject dummy = null; TopDownKit.Health dh = null; Rigidbody2D drb = null; TopDownKit.PlayerMover dm = null;
int mark1 = -1, mark2 = -1, mark3 = -1; bool flag = false; float fx = 0f; TopDownKit.Bullet bullet = null;

void Dummy(Vector2 stand)
{
    if (dummy != null) UnityEngine.Object.Destroy(dummy);
    dummy = UnityEngine.Object.Instantiate(chaser);
    UnityEngine.Object.DestroyImmediate(dummy.GetComponent<TopDownKit.EnemyBrain>());
    dm = dummy.GetComponent<TopDownKit.PlayerMover>(); dm.readKeyboard = false;
    dh = dummy.GetComponent<TopDownKit.Health>();
    drb = dummy.GetComponent<Rigidbody2D>();
    PlaceAt(drb, stand);
}
bool Gone() => dummy == null || dh == null || dh.Dead;
bool SolidAt(Vector2 p, string mirror)
{
    foreach (var c in Physics2D.OverlapPointAll(p)) if (!c.isTrigger && c.name.StartsWith(mirror)) return true;
    return false;
}
Vector3Int Cell(float x, float y) => map.CellAt(new Vector2(x, y));

tasks.Add(("red kills", 60, s =>
{
    if (s == 0) { Dummy(new Vector2(-16.5f, 14.5f)); return false; }
    if (Gone() && mark1 < 0) mark1 = s;
    if (s < 10) return false;
    Check("red kills whoever stands on it", mark1 >= 0 && mark1 <= 2, "dead at step " + mark1 + " after being put on red");
    return true;
}));
tasks.Add(("dash over red", 60, s =>
{
    if (s == 0) { mark1 = -1; mark2 = -1; mark3 = -1; flag = false; Dummy(new Vector2(-14.5f, 14.5f)); dummy.AddComponent<TopDownKit.Dash>(); dm.SetMoveDirection(Vector2.left); return false; }
    var dash = dummy != null ? dummy.GetComponent<TopDownKit.Dash>() : null;
    if (s == 1) { dm.SetMoveDirection(Vector2.zero); dash.Request(); return false; }
    if (dash != null && dash.IsDashing) { mark2 = s; if (mark3 < 0) mark3 = s; if (Gone()) flag = true; }
    if (Gone() && mark1 < 0) mark1 = s;
    if (s < 40) return false;
    Check("a dash over red protects; ending on red kills", !flag && mark2 > 0 && mark1 > mark2,
        "dash steps " + mark3 + ".." + mark2 + ", dead at step " + mark1 + (flag ? ", DIED WHILE DASHING" : ""));
    return true;
}));
tasks.Add(("damage per second", 60, s =>
{
    if (s == 0) { Dummy(new Vector2(-10.5f, 25.5f)); dh.maxHealth = 10; dh.invulnerableTime = 0f; dh.Restore(); mark1 = 0; return false; }
    var r = new TopDownKit.GroundRules { damagePerSecond = 2 };
    TopDownKit.Ground.Hurt(dh, r, ref mark1, Time.fixedDeltaTime);   // steps 1..121
    if (s < 121) return false;
    Check("damage per second: one hit a second, the first at once", dh.Current == 4, "hp 10 -> " + dh.Current + " after 121 steps of 2/s (hits at 0, 60, 120)");
    return true;
}));
tasks.Add(("blue blocks free walking", 60, s =>
{
    if (s == 0) { Dummy(new Vector2(-15.5f, 16.5f)); fx = -99f; return false; }
    dm.SetMoveDirection(Vector2.right);
    fx = Mathf.Max(fx, TopDownKit.GridStep.StandPoint(drb).x);
    if (s < 90) return false;
    Check("blue stops free walking", fx < -13f + 1e-3f, "furthest x " + N(fx) + " (blue starts at -13, body radius 0.4)");
    return true;
}));
tasks.Add(("blue blocks grid steps", 60, s =>
{
    if (s == 0) { Dummy(new Vector2(-14.5f, 16.5f)); dm.feel = grid4; fx = -99f; return false; }
    dm.SetMoveDirection(Vector2.right);
    fx = Mathf.Max(fx, TopDownKit.GridStep.StandPoint(drb).x);
    if (s < 90) return false;
    Check("blue stops grid steps", Mathf.Abs(fx + 13.5f) < 1e-4f, "furthest x " + N(fx) + " (start -14.5; -13.5 is free, blue starts at -12.5)");
    return true;
}));
tasks.Add(("bullet over blue", 60, s =>
{
    if (s == 0) { if (dummy != null) UnityEngine.Object.Destroy(dummy); fx = -99f; bullet = TopDownKit.Bullet.Spawn(new Vector2(-15.5f, 16.5f), Vector2.right * pistol.bulletSpeed, pistol, 0); return false; }
    if (bullet.gameObject.activeSelf) fx = Mathf.Max(fx, bullet.transform.position.x);
    if (s < 30) return false;
    Check("bullets fly over blue", fx > -9f && SolidAt(new Vector2(-12.5f, 16.5f), "Ground low"),
        "bullet reached x " + N(fx) + " from -15.5; low-block collider on blue " + SolidAt(new Vector2(-12.5f, 16.5f), "Ground low"));
    return true;
}));
tasks.Add(("closed door stops a bullet", 60, s =>
{
    if (s == 0) { fx = -99f; bullet = TopDownKit.Bullet.Spawn(new Vector2(-2.5f, 14.5f), Vector2.up * pistol.bulletSpeed, pistol, 0); return false; }
    if (bullet.gameObject.activeSelf) fx = Mathf.Max(fx, bullet.transform.position.y);
    if (s < 30) return false;
    Check("a closed door stops bullets", fx < 17f && !bullet.gameObject.activeSelf, "bullet got to y " + N(fx) + " (door at y 17)");
    return true;
}));
System.Func<int, bool> Crumble(string label) => s =>
{
    var c = Cell(-7.5f, 15.5f);
    if (s == 0) { mark1 = mark2 = mark3 = -1; Dummy(new Vector2(-7.5f, 15.5f)); return false; }
    var st = map.State(c);
    if (st == TopDownKit.GroundState.Warning && mark1 < 0) mark1 = s;
    if (st == TopDownKit.GroundState.Broken && mark2 < 0) { mark2 = s; flag = SolidAt(new Vector2(-7.5f, 15.5f), "Ground"); }
    if (st == TopDownKit.GroundState.Normal && mark2 >= 0 && mark3 < 0) mark3 = s;
    if (s < 260) return false;
    int wantBreak = Mathf.RoundToInt(crumbleTile.breakAfter * 60f), wantBack = Mathf.RoundToInt(crumbleTile.backAfter * 60f);
    Check("crumbling at " + label + ": breaks and comes back at the set times",
        mark1 >= 0 && mark2 - mark1 == wantBreak && mark3 - mark2 == wantBack && Gone() && !flag,
        "warning at step " + mark1 + ", broken " + (mark2 - mark1) + " steps later (want " + wantBreak + "), back " + (mark3 - mark2)
        + " steps after that (want " + wantBack + "); dummy on it died " + Gone() + "; broken = kills, no collider " + !flag);
    return true;
};
tasks.Add(("crumble 30", 30, Crumble("30 fps")));
tasks.Add(("crumble 144", 144, Crumble("144 fps")));
tasks.Add(("lever door", 60, s =>
{
    var c = Cell(-2.5f, 17.5f); var p = new Vector2(-2.5f, 17.5f);
    if (s == 0) { flag = map.State(c) == TopDownKit.GroundState.Closed && SolidAt(p, "Ground blocks"); lever.Use(null); return false; }
    if (s == 1) { mark1 = map.State(c) == TopDownKit.GroundState.Normal && !SolidAt(p, "Ground") ? 1 : 0; lever.Use(null); return false; }
    bool closedAgain = map.State(c) == TopDownKit.GroundState.Closed && SolidAt(p, "Ground blocks");
    Check("door ground: a lever opens and shuts it", flag && mark1 == 1 && closedAgain, "closed at start " + flag + ", open after a pull " + (mark1 == 1) + ", closed after another " + closedAgain);
    return true;
}));
tasks.Add(("key door", 60, s =>
{
    var c = Cell(4.5f, 17.5f); var p = new Vector2(4.5f, 17.5f);
    var bag = TopDownKit.BagLookup.GetBag(interactor).Inventory;   // phase 14: Interactor.Bag became the Inventory folder's GetBag
    if (s == 0) { bag.Remove(keyItem, 99); keyLock.Use(interactor); return false; }
    if (s == 1)
    {
        flag = map.State(c) == TopDownKit.GroundState.Closed && SolidAt(p, "Ground blocks");
        if (TopDownKit.TextBox.Current != null && TopDownKit.TextBox.Current.IsOpen) TopDownKit.TextBox.Current.UsePressed(interactor);
        bag.Add(keyItem, 1); keyLock.Use(interactor); return false;
    }
    bool open = map.State(c) == TopDownKit.GroundState.Normal && !SolidAt(p, "Ground");
    Check("door ground: a key opens it", flag && open && !bag.Has(keyItem), "stays shut without the key " + flag + ", open with it " + open + ", key used up " + !bag.Has(keyItem));
    return true;
}));
tasks.Add(("room door", 60, s =>
{
    var c = Cell(39.5f, 10.5f); var p = new Vector2(39.5f, 10.5f);
    if (s == 0) { mover.feel = startFeel; PlaceAt(rb, new Vector2(40.5f, 0.5f)); mark1 = -1; return false; }
    if (lockB.Locked && mark1 < 0) { mark1 = s; flag = map.State(c) == TopDownKit.GroundState.Closed && SolidAt(p, "Ground blocks"); lockB.Clear(); return false; }
    if (mark1 < 0) return s > 30 ? Fail("room B never locked") : false;
    bool open = map.State(c) == TopDownKit.GroundState.Normal && !SolidAt(p, "Ground");
    Check("door ground: a cleared room opens it", flag && open, "shut when B locked " + flag + ", open after Clear() " + open);
    PlaceAt(rb, startPos);
    return true;
}));
bool Fail(string why) { Check("task", false, why); return true; }

void TaskFixed()
{
    if (task < 0 || task >= tasks.Count) return;
    if (tasks[task].step(ts)) { task++; ts = 0; if (task < tasks.Count) Application.targetFrameRate = tasks[task].fps; }
    else ts++;
}

// ---------------- Driving ----------------
var original = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
void Finish()
{
    try
    {
        for (int i = 0; i + 1 < plan.Count; i += 2)
        {
            string a = string.Join(" ", arrivals[i]), b = string.Join(" ", arrivals[i + 1]);
            Check("grid " + plan[i].feel.name + ": same tiles in the same order at 30 and 144 fps", a == b && arrivals[i].Count > 0,
                arrivals[i].Count + " tiles: " + a + (a == b ? "" : " | 144: " + b));
        }
        Check("grid steps stand exactly on tile centres", offCount == 0 && settled > 100,
            settled + " settled steps, " + offCount + " off centre, max " + N(maxOff) + " tiles");
        System.IO.File.WriteAllLines(outPath, lines);
    }
    catch (System.Exception e) { error = (error ?? "") + " " + e.Message; }
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(original);
    RealDevices(true, kb);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(kb);
    settings.editorInputBehaviorInPlayMode = oldEditorBehaviour;
    settings.backgroundBehavior = oldBackground;
    QualitySettings.vSyncCount = oldVsync;
    Application.targetFrameRate = oldTarget;
    mover.feel = startFeel;
    if (dummy != null) UnityEngine.Object.Destroy(dummy);
    string head = error != null ? "error: " + error : "done " + pass + "/" + (pass + fail);
    System.AppDomain.CurrentDomain.SetData("tl_state", head + "\n" + string.Join("\n", results));
}

void OnFixed()
{
    try { GridFixed(); TaskFixed(); }
    catch (System.Exception e) { error = e.ToString(); task = 999; phase = "dead"; Finish(); }
}

void OnFrame()
{
    try
    {
        if (phase == "dead" || phase == "tasks") { if (phase == "tasks" && task >= tasks.Count) { phase = "dead"; Finish(); } return; }
        float t = Time.time - phaseStart;
        if (phase == "next")
        {
            run++;
            if (run >= plan.Count) { Keys(); Application.targetFrameRate = 60; mover.feel = startFeel; PlaceAt(rb, startPos); phase = "tasks"; task = 0; ts = 0; return; }
            Application.targetFrameRate = plan[run].fps;
            mover.feel = plan[run].feel;
            PlaceAt(rb, gridStart);
            arrivals[run] = new System.Collections.Generic.List<string>();
            Keys(); runStep = 0; wasSliding = false; sub = 0;
            Phase("settle");
            return;
        }
        bool tap = plan[run].feel == gridTap;
        if (phase == "settle" && t >= 0.3f)
        {
            if (tap) { Keys(D); tapFrame = Time.frameCount; Phase("tap"); }
            else { Keys(D); Phase("d"); }
        }
        else if (phase == "d" && t >= 0.7f) { Keys(); Phase("w1"); }
        else if (phase == "w1" && t >= 0.4f) { Keys(W, D); Phase("wd"); }
        else if (phase == "wd" && t >= 0.65f) { Keys(); Phase("w2"); }
        else if (phase == "w2" && t >= 0.4f) { Keys(A); Phase("a"); }
        else if (phase == "a" && t >= 0.5f) { Keys(); Phase("end"); }
        else if (phase == "tap" && Time.frameCount == tapFrame + 1) { Keys(); Phase("tapwait"); }
        else if (phase == "tapwait" && t >= 0.25f)
        {
            sub++;
            if (sub < 5) { Keys(sub == 4 ? W : D); tapFrame = Time.frameCount; Phase("tap"); }
            else Phase("end");
        }
        else if (phase == "end" && t >= 0.6f) Phase("next");
    }
    catch (System.Exception e) { error = e.ToString(); phase = "dead"; Finish(); }
}

var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var systems = loop.subSystemList;
for (int i = 0; i < systems.Length; i++)
{
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.FixedUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.GroundMap), updateDelegate = OnFixed });
        systems[i].subSystemList = list.ToArray();
    }
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.PreLateUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Insert(0, new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.GridStep), updateDelegate = OnFrame });
        systems[i].subSystemList = list.ToArray();
    }
}
loop.subSystemList = systems;
System.AppDomain.CurrentDomain.SetData("tl_state", "running");
UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
return "started: " + plan.Count + " grid runs, then " + tasks.Count + " tile checks";

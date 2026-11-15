// Camera check. Not compiled by Unity (it lives outside Assets/). Run IN PLAY MODE on
// Assets/Scenes/Playground.unity, from the project root:
//
//     unity command eval_file --file Tools/measure/camera.cs
//
// then poll until it says done (about 30 s):
//
//     unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("cam_state") as string;'
//
// Walks the player through the rooms with a virtual keyboard (the real key path) in each
// layout: A -> B (a slide or a fade), B's doors lock and hold, C clears them, B -> C (the
// big room, followed inside), C -> warp -> area D. On every editor tick it checks:
//   - the camera position is on whole pixels (1/16 tile),
//   - in the room layouts, outside slides and fades, the view stays inside the current
//     room (a room smaller than the view on one axis must be centred on that axis), and
//     the current room is the one the player stands in.
// Shake is not triggered here; it is deliberately not clamped to rooms.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on Playground first";
if (System.AppDomain.CurrentDomain.GetData("cam_state") as string == "running") return "already running";

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var cam = Camera.main;
var rig = cam.GetComponent<TopDownKit.CameraRig>();
var lockB = GameObject.Find("Room B").GetComponent<TopDownKit.RoomLock>();
var oldLayout = rig.layout;

var settings = UnityEngine.InputSystem.InputSystem.settings;
var oldEditorBehaviour = settings.editorInputBehaviorInPlayMode;
var oldBackground = settings.backgroundBehavior;
settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
Application.runInBackground = true;
var kb = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("CameraKeyboard");
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
var W = UnityEngine.InputSystem.Key.W; var A = UnityEngine.InputSystem.Key.A; var D = UnityEngine.InputSystem.Key.D;

var results = new System.Collections.Generic.List<string>();
int failed = 0, checks = 0, pixelBad = 0, roomBad = 0, ticks = 0;
string firstBad = "";
bool wasTransition = false;
int transitions = 0;

void Keys(params UnityEngine.InputSystem.Key[] k)
{
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(k));
}
void Teleport(float x, float y)
{
    rb.position = new Vector2(x, y);
    rb.linearVelocity = Vector2.zero;
    player.transform.position = new Vector3(x, y, 0f);
}
void Check(string name, bool ok, string detail)
{
    if (!ok) failed++;
    results.Add((ok ? "PASS " : "FAIL ") + rig.layout + ": " + name + " (" + detail + ")");
}
string Room() { return rig.CurrentRoom != null ? rig.CurrentRoom.name : "none"; }
// Phase 15: on a failed walk, say why the player stopped (the phase 8 flake left no clue).
var mover = player.GetComponent<TopDownKit.PlayerMover>();
string Why()
{
    string s = " | feel " + (mover.feel != null ? mover.feel.name : "none") + ", v " + rb.linearVelocity.ToString("F2") + ", keys W " + kb.wKey.isPressed + " A " + kb.aKey.isPressed + " D " + kb.dKey.isPressed
        + ", current kb " + (UnityEngine.InputSystem.Keyboard.current == kb) + ", timeScale " + Time.timeScale + ", solid near:";
    foreach (var c in Physics2D.OverlapCircleAll(rb.position, 1.2f)) if (!c.isTrigger && c.attachedRigidbody != rb) s += " " + c.name;
    return s;
}

int stepIndex = 0, stepFrame = 0, lastFrame = -1;
float stepStart = 0f;
float T() { return Time.time - stepStart; }
// Steps: each returns true when done. A frame counter per step gives timeouts.
var steps = new System.Collections.Generic.List<System.Func<int, bool>>();
void Leg(TopDownKit.CameraLayout layout, bool lockTest)
{
    steps.Add(f => { if (f == 0) { Keys(); rig.SetLayout(TopDownKit.CameraLayout.Open); Teleport(16f, 7.5f); transitions = 0; } if (f < 4) return false; rig.SetLayout(layout); return true; });
    steps.Add(f => { if (f == 0) Keys(D);
        bool done = rb.position.x > 25f && !rig.InTransition;
        if (done || T() > 10f) { Keys(); Check("walk A -> B", done && Room() == "Room B" && (layout == TopDownKit.CameraLayout.Open || transitions >= 1), "x " + rb.position.x.ToString("F2") + ", room " + Room() + ", transitions " + transitions + (done ? "" : Why())); return true; }
        return false; });
    if (lockTest)
    {
        steps.Add(f => { if (f == 0) Keys(A); if (T() < 1.5f) return false; Keys();
            Check("doors lock and hold", lockB.Locked && rb.position.x > 21f, "locked " + lockB.Locked + ", x " + rb.position.x.ToString("F2"));
            lockB.Clear();
            Check("clear opens", !lockB.Locked && !lockB.doors[0].activeSelf, "locked " + lockB.Locked);
            return true; });
    }
    steps.Add(f => { if (f == 0) { Keys(); rig.SetLayout(TopDownKit.CameraLayout.Open); Teleport(39.5f, 5f); } if (f < 4) return false; rig.SetLayout(layout); return true; });
    steps.Add(f => { if (f == 0) { Keys(W); transitions = 0; }
        bool done = rb.position.y > 32f && !rig.InTransition;
        if (done || T() > 15f) { Keys(); Check("walk B -> C and up the big room", done && Room() == "Room C", "y " + rb.position.y.ToString("F2") + ", room " + Room() + ", cam y " + cam.transform.position.y.ToString("F3") + (done ? "" : Why())); return true; }
        return false; });
    steps.Add(f => { if (f == 0) { Keys(); rig.SetLayout(TopDownKit.CameraLayout.Open); Teleport(46f, 35f); } if (f < 4) return false; rig.SetLayout(layout); return true; });
    steps.Add(f => { if (f == 0) Keys(D);
        bool done = Room() == "Area D" && !rig.InTransition && T() > 0.5f;
        if (done || T() > 10f) { Keys(); Check("warp C -> D fades and cuts", done && rb.position.x > 100f, "x " + rb.position.x.ToString("F2") + ", room " + Room() + (done ? "" : Why())); return true; }
        return false; });
}
Leg(TopDownKit.CameraLayout.RoomsSlide, true);
Leg(TopDownKit.CameraLayout.RoomsFade, false);
Leg(TopDownKit.CameraLayout.Open, false);



void Finish(string error)
{
    EditorApplication.update -= Tick;
    RealDevices(true, kb);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(kb);
    settings.editorInputBehaviorInPlayMode = oldEditorBehaviour;
    settings.backgroundBehavior = oldBackground;
    rig.layout = oldLayout;
    Teleport(0f, -2f);
    rig.SetLayout(oldLayout);
    string summary = "per-tick checks: " + checks + " (pixel bad " + pixelBad + ", room bad " + roomBad + ")" + (firstBad != "" ? " first bad: " + firstBad : "");
    if (pixelBad + roomBad > 0) failed++;
    System.AppDomain.CurrentDomain.SetData("cam_state", (error != null ? "error: " + error + "\n" : "done: " + (failed == 0 ? "ALL PASS" : failed + " FAILED") + "\n")
        + summary + "\n" + string.Join("\n", results));
}

void Tick()
{
    try
    {
        if (!EditorApplication.isPlaying) { Finish("play mode ended"); return; }
        if (Time.frameCount == lastFrame) return;   // one look per game frame
        lastFrame = Time.frameCount;

        bool tr = rig.InTransition;
        if (tr && !wasTransition) transitions++;
        wasTransition = tr;

        // Per-frame checks.
        checks++;
        Vector2 p = cam.transform.position;
        Vector2 b = rig.BasePosition;
        bool onPixel = Mathf.Abs(p.x * 16f - Mathf.Round(p.x * 16f)) < 1e-3f && Mathf.Abs(p.y * 16f - Mathf.Round(p.y * 16f)) < 1e-3f;
        if (!onPixel) { pixelBad++; if (firstBad == "") firstBad = "off pixel " + p.ToString("F4"); }
        if (rig.layout != TopDownKit.CameraLayout.Open && !tr && rig.CurrentRoom != null && stepFrame > 4)
        {
            Rect r = rig.CurrentRoom.Bounds;
            Vector2 h = rig.viewTiles * 0.5f;
            bool okX = r.width <= h.x * 2f ? Mathf.Abs(b.x - r.center.x) < 0.07f : (b.x - h.x >= r.xMin - 1e-3f && b.x + h.x <= r.xMax + 1e-3f);
            bool okY = r.height <= h.y * 2f ? Mathf.Abs(b.y - r.center.y) < 0.07f : (b.y - h.y >= r.yMin - 1e-3f && b.y + h.y <= r.yMax + 1e-3f);
            var at = TopDownKit.Room.At(player.transform.position);
            bool sameRoom = at == null || at == rig.CurrentRoom;
            if (!(okX && okY && sameRoom)) { roomBad++; if (firstBad == "") firstBad = "cam " + b.ToString("F3") + " room " + rig.CurrentRoom.name + " player in " + (at != null ? at.name : "none"); }
        }

        if (steps[stepIndex](stepFrame)) { stepIndex++; stepFrame = 0; stepStart = Time.time; if (stepIndex >= steps.Count) { Finish(null); return; } }
        else stepFrame++;
    }
    catch (System.Exception e) { Finish(e.ToString()); }
}

System.AppDomain.CurrentDomain.SetData("cam_state", "running");
EditorApplication.update += Tick;
return "started " + steps.Count + " steps";

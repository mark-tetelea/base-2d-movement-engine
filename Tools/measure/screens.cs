// Screens check (phase 6). Run IN PLAY MODE on Assets/Scenes/Playground.unity:
//     unity command eval_file --file Tools/measure/screens.cs
// Checks: the UI scale is the camera's pixel ratio (trap 32); the pointer over a picker
// button counts as "over UI" (read at the press) and floor does not (trap 20/21); a press that
// starts on the picker does not shoot, one on the floor does (trap 21, both halves); a
// picker click changes its setting; Esc-style pause stops the fixed step and unpauses.
// Polls itself: call again until it says done.
if (!EditorApplication.isPlaying) return "not playing - enter play mode on Playground first";
var D = System.AppDomain.CurrentDomain;
Application.runInBackground = true;   // phase 15: an unfocused editor stops the game clock (trap 6)
var results = new System.Collections.Generic.List<string>();
void Check(string n, bool ok, string d) { results.Add((ok ? "PASS " : "FAIL ") + n + " (" + d + ")"); }
var doc = UnityEngine.Object.FindFirstObjectByType<UnityEngine.UIElements.UIDocument>();
var cam = Camera.main;
var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
Check("UI scale = camera pixel ratio", Mathf.Approximately(doc.panelSettings.scale, ppc.pixelRatio), "screen " + Screen.width + "x" + Screen.height + ", ui " + doc.panelSettings.scale + ", ppc " + ppc.pixelRatio);

var picker = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Playground.Picker>();
var player = GameObject.Find("Player");
var gun = player.GetComponent<TopDownKit.Gun>();
var shooter = player.GetComponent<TopDownKit.PlayerShooter>();
// Its own mouse: the real mouse's state events overwrite queued presses (trap 84).
var mouse = UnityEngine.InputSystem.InputSystem.GetDevice("ScreensMouse") as UnityEngine.InputSystem.Mouse;
if (mouse == null) mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("ScreensMouse");
mouse.MakeCurrent();
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
RealDevices(false, mouse);
if (!picker.IsOpen) picker.Toggle();

// Where is the first picker button on screen? Panel coords * scale, Y flipped.
var btn = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.Button>(UnityEngine.UIElements.UQueryExtensions.Q(doc.rootVisualElement, "Picker"));
var wb = btn.worldBound; float k = doc.panelSettings.scale;
Vector2 onButton = new Vector2(wb.center.x * k, Screen.height - wb.center.y * k);
Vector2 onFloor = new Vector2(Screen.width * 0.75f, Screen.height * 0.5f);

string state = D.GetData("sc_state") as string;
var ins = UnityEngine.InputSystem.InputSystem.settings;
if (state == null)
{
    D.SetData("sc_eb", ins.editorInputBehaviorInPlayMode); D.SetData("sc_bb", ins.backgroundBehavior);
    ins.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
    ins.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = onButton });
    D.SetData("sc_state", "over");
    return "step 1 set; call again";
}
if (state == "over")
{
    // No hover-only check here: the real mouse sends its own position between calls.
    // The press check below reads PointerOverUI in the frame the press arrives.
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = onFloor });
    D.SetData("sc_state", "floor");
    return "step 2; call again";
}
if (state == "floor")
{
    D.SetData("sc_floor", (!TopDownKit.KitUI.PointerOverUI ? "PASS" : "FAIL") + " pointer over floor = not over UI (at " + onFloor + ")");
    // Press on the button: must not shoot.
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = onButton });
    D.SetData("sc_shots", gun.ShotsFired);
    var ms = new UnityEngine.InputSystem.LowLevel.MouseState { position = onButton }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, ms);
    D.SetData("sc_t", Time.time);
    D.SetData("sc_state", "pressUI");
    return "step 3; call again";
}
if (state == "pressUI")
{
    if (Time.time - (float)D.GetData("sc_t") < 0.3f) return "waiting";
    int before = (int)D.GetData("sc_shots");
    D.SetData("sc_ui", (gun.ShotsFired == before && shooter.PressStartedOnUI ? "PASS" : "FAIL") + " press on picker does not shoot (shots " + before + " -> " + gun.ShotsFired + ", startedOnUI " + shooter.PressStartedOnUI + ")");
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = onFloor });
    D.SetData("sc_state", "release");
    return "step 4; call again";
}
if (state == "release")
{
    D.SetData("sc_shots", gun.ShotsFired);
    var ms = new UnityEngine.InputSystem.LowLevel.MouseState { position = onFloor }.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left, true);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, ms);
    D.SetData("sc_t", Time.time);
    D.SetData("sc_state", "pressFloor");
    return "step 5; call again";
}
if (state == "pressFloor")
{
    if (Time.time - (float)D.GetData("sc_t") < 0.3f) return "waiting";
    int before = (int)D.GetData("sc_shots");
    string floorShot = (gun.ShotsFired > before ? "PASS" : "FAIL") + " press on floor shoots (shots " + before + " -> " + gun.ShotsFired + ")";
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, new UnityEngine.InputSystem.LowLevel.MouseState { position = onFloor });
    // Picker click changes the setting; pause stops the fixed step.
    var mover = player.GetComponent<TopDownKit.PlayerMover>();
    var f0 = mover.feel; picker.Click(0);
    string pk = (mover.feel != f0 ? "PASS" : "FAIL") + " picker row 0 changes the feel (" + f0.name + " -> " + mover.feel.name + ")";
    picker.Click(0); picker.Click(0); picker.Click(0);
    string back = (mover.feel == f0 ? "PASS" : "FAIL") + " four clicks cycle back (" + mover.feel.name + ")";
    var pm = UnityEngine.Object.FindFirstObjectByType<TopDownKit.PauseMenu>();
    pm.Pause();
    string pz = (Time.timeScale == 0f && TopDownKit.PauseMenu.IsPaused ? "PASS" : "FAIL") + " pause sets timeScale 0";
    pm.Resume();
    string rz = (Time.timeScale == 1f && !TopDownKit.PauseMenu.IsPaused ? "PASS" : "FAIL") + " resume sets timeScale 1";
    D.SetData("sc_state", null);
    RealDevices(true, mouse);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    ins.editorInputBehaviorInPlayMode = (UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode)D.GetData("sc_eb");
    ins.backgroundBehavior = (UnityEngine.InputSystem.InputSettings.BackgroundBehavior)D.GetData("sc_bb");
    return "done\n" + results[0] + "\n" + D.GetData("sc_floor") + "\n" + D.GetData("sc_ui") + "\n" + floorShot + "\n" + pk + "\n" + back + "\n" + pz + "\n" + rz;
}
return "unknown state " + state;

// Phase 15 check: rebinding (phase 14) and the touch path (phase 13), on Assets/Scenes/Playground.unity
// (sh Tools/measure/run-check.sh Playground Tools/measure/controls.cs ct_state 90).
// Rebind: the real rebind screen (pause menu > Controls) is told which row was clicked (its
// button's click - trap 39: a scripted click never presses), then a VIRTUAL keyboard / mouse
// presses the new key, read by the screen like real ones:
//   - Dash -> Left Shift by a key press; then Left Shift dashes and Space does not.
//   - Reload -> right mouse button by a mouse press; then the right button reloads.
//   - Esc while waiting cancels; Reset all brings the asset's keys back.
// Touch (input as data - no finger): TouchControls set On; its sticks and pads are given values
// the way a finger would set them; checks the player walks, keys come back, the fire pad and
// the aim stick shoot, Dash / Use / Gun pads act, and nothing stays held while paused.
// Saves go to a scratch folder (SaveFiles.FolderOverride; the runner clears it afterwards);
// the player's own rebinds are put back at the end.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on Playground first";
var D = System.AppDomain.CurrentDomain;
if (D.GetData("ct_state") as string == "running") return "already running";
var IS = typeof(UnityEngine.InputSystem.InputSystem);
var Key = typeof(UnityEngine.InputSystem.Key);

string scratch = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kit-controls-check");
System.IO.Directory.CreateDirectory(scratch);
TopDownKit.SaveFiles.FolderOverride = scratch;
string oldControls = TopDownKit.Controls.Export();

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var gun = player.GetComponent<TopDownKit.Gun>();
var dash = player.GetComponent<TopDownKit.Dash>();
var aim = player.GetComponent<TopDownKit.PlayerAim>();
var it = player.GetComponent<TopDownKit.Interactor>();
var pm = UnityEngine.Object.FindFirstObjectByType<TopDownKit.PauseMenu>();
var touch = UnityEngine.Object.FindFirstObjectByType<TopDownKit.TouchControls>();
var oldTouchMode = touch.mode;
Application.runInBackground = true;

var settings = UnityEngine.InputSystem.InputSystem.settings;
var oldEditorBehaviour = settings.editorInputBehaviorInPlayMode;
var oldBackground = settings.backgroundBehavior;
settings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
var kb = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("ControlsKeyboard");
var mouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>("ControlsMouse");
var realOff = new System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice>();
foreach (var d in UnityEngine.InputSystem.InputSystem.devices) if ((d is UnityEngine.InputSystem.Keyboard || d is UnityEngine.InputSystem.Mouse) && d != kb && d != mouse && d.enabled) realOff.Add(d);
foreach (var d in realOff) UnityEngine.InputSystem.InputSystem.DisableDevice(d);
kb.MakeCurrent(); mouse.MakeCurrent();
Vector2 floorPoint = new Vector2(Screen.width * 0.75f, Screen.height * 0.5f);   // right of the player, off every screen
void Keys(params UnityEngine.InputSystem.Key[] k) { UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(k)); }
void Mouse(bool left, bool right)
{
    var s = new UnityEngine.InputSystem.LowLevel.MouseState { position = floorPoint };
    if (left) s = s.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left);
    if (right) s = s.WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Right);
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse, s);
}

var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
object Screen0() => typeof(TopDownKit.PauseMenu).GetField("rebind", flags).GetValue(pm);
void ClickRow(string id, bool alt) { var s = Screen0(); s.GetType().GetMethod("Wait", flags).Invoke(s, new object[] { id, alt }); }
object Part(string field) => typeof(TopDownKit.TouchControls).GetField(field, flags).GetValue(touch);
void Finger(string part, int pointer, Vector2 value)
{
    var o = Part(part); var t = o.GetType();
    t.GetField("pointer").SetValue(o, pointer);
    if (t.GetField("Value") != null) t.GetField("Value").SetValue(o, value);
    if (t.GetField("Pressed") != null && pointer >= 0) t.GetField("Pressed").SetValue(o, true);
}
void Lift(string part) { var o = Part(part); o.GetType().GetMethod("Let").Invoke(o, null); }

var results = new System.Collections.Generic.List<string>();
int failed = 0; string error = null;
void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
void Place(Vector2 p) { rb.position = p; rb.linearVelocity = Vector2.zero; player.transform.position = p; }

var steps = new System.Collections.Generic.List<System.Func<int, bool>>();
int stepIndex = 0, stepFrame = 0, lastFrame = -1; float stepStart = 0f;
float T() => Time.unscaledTime - stepStart;
void Wait(float s) => steps.Add(f => T() >= s);
void Frames(int n) => steps.Add(f => f >= n);
void Do(System.Action a) => steps.Add(f => { a(); return true; });
int dashes = 0;
System.Action<TopDownKit.Dash> onDash = d => { if (d == dash) dashes++; };
TopDownKit.Dash.AnyStarted += onDash;

// ---- rebind by a key press ----
Do(() => { Keys(); Mouse(false, false); TopDownKit.Controls.ResetAll(); Place(new Vector2(0f, -2f)); pm.Pause(); pm.OpenControls(); });
Frames(2);
Do(() => { Check("the pause menu opens the rebind screen", pm.ControlsOpen && TopDownKit.PauseMenu.IsPaused, "open " + pm.ControlsOpen); ClickRow("Dash", false); });
Frames(3);
Do(() => Keys(UnityEngine.InputSystem.Key.LeftShift));
Frames(3);
Do(() => { Keys(); Check("rebind screen: pressing Left Shift makes it Dash's key", TopDownKit.Controls.Keys("Dash").key == UnityEngine.InputSystem.Key.LeftShift && !TopDownKit.Controls.Listening,
    "Dash = " + TopDownKit.Controls.Describe("Dash")); });
// Esc cancels
Do(() => ClickRow("Use", false));
Frames(3);
Do(() => Keys(UnityEngine.InputSystem.Key.Escape));
Frames(3);
Do(() => { Keys(); Check("rebind screen: Esc while waiting cancels (Use unchanged, screen still open)", TopDownKit.Controls.Keys("Use").key == UnityEngine.InputSystem.Key.E && pm.ControlsOpen && !TopDownKit.Controls.Listening,
    "Use = " + TopDownKit.Controls.Describe("Use") + ", open " + pm.ControlsOpen); });
// a mouse button
Do(() => ClickRow("Reload", false));
Frames(3);
Do(() => Mouse(false, true));
Frames(3);
Do(() => { Mouse(false, false); Check("rebind screen: the right mouse button becomes Reload's", TopDownKit.Controls.Keys("Reload").mouse == TopDownKit.PointerButton.Right && TopDownKit.Controls.Keys("Reload").key == UnityEngine.InputSystem.Key.None,
    "Reload = " + TopDownKit.Controls.Describe("Reload")); });
Frames(2);
Do(() => { var s = Screen0(); s.GetType().GetMethod("Close").Invoke(s, null); pm.Resume(); });
Frames(3);
// the new keys work in play
int before = 0;
Do(() => { gun.Select(0); gun.Refill(); before = dashes; Keys(UnityEngine.InputSystem.Key.Space); });
Frames(3);
Do(() => Keys());
Wait(0.6f);
Do(() => { Check("after the rebind Space no longer dashes", dashes == before, "dashes " + (dashes - before)); before = dashes; Keys(UnityEngine.InputSystem.Key.LeftShift); });
Frames(3);
Do(() => Keys());
Wait(0.2f);
Do(() => { Check("Left Shift dashes", dashes == before + 1, "dashes " + (dashes - before)); Mouse(true, false); });
Frames(3);
Do(() => Mouse(false, false));
Wait(0.3f);
Do(() => Mouse(false, true));
Frames(3);
Do(() => { Mouse(false, false); Check("a left click on the floor shot; the right button now reloads", gun.Reloading && gun.Ammo < gun.Current.magazine, "ammo " + gun.Ammo + ", reloading " + gun.Reloading); });
Do(() => { TopDownKit.Controls.ResetAll(); Check("Reset all gives the asset's keys back", TopDownKit.Controls.Keys("Dash").key == UnityEngine.InputSystem.Key.Space && TopDownKit.Controls.Keys("Reload").key == UnityEngine.InputSystem.Key.R, "Dash " + TopDownKit.Controls.Describe("Dash") + ", Reload " + TopDownKit.Controls.Describe("Reload")); });
Wait(0.5f);

// ---- touch, as data ----
Vector2 at0 = Vector2.zero; int shots0 = 0;
Do(() => { touch.mode = TopDownKit.TouchMode.On; Place(new Vector2(0f, -2f)); });
Frames(2);
Do(() => { at0 = rb.position; Finger("move", 11, new Vector2(1f, 0f)); });
Wait(0.4f);
Do(() => { Check("move stick: the player walks right; the keyboard is off while the stick is held", rb.position.x > at0.x + 1f && !mover.readKeyboard, "moved " + (rb.position.x - at0.x).ToString("F2") + ", keyboard " + mover.readKeyboard); Lift("move"); });
Frames(3);
Do(() => Check("move stick let go: the player stops; keyboard back", mover.readKeyboard && rb.linearVelocity.magnitude < 0.01f, "keyboard " + mover.readKeyboard + ", speed " + rb.linearVelocity.magnitude.ToString("F2")));
Do(() => { gun.Refill(); shots0 = gun.ShotsFired; Finger("fire", 12, Vector2.zero); });
Wait(0.3f);
Do(() => { Check("Fire pad: shoots", gun.ShotsFired > shots0, "shots " + (gun.ShotsFired - shots0)); Lift("fire"); });
Wait(0.3f);
Do(() => { shots0 = gun.ShotsFired; Finger("look", 13, new Vector2(0f, 1f)); });
Wait(0.3f);
Do(() => { Check("aim stick: aims where it points and fires", gun.ShotsFired > shots0 && Vector2.Dot(aim.Direction, Vector2.up) > 0.99f, "shots " + (gun.ShotsFired - shots0) + ", aim " + aim.Direction.ToString("F2")); Lift("look"); });
Wait(0.6f);
Do(() => { before = dashes; Finger("dashPad", 14, Vector2.zero); });
Frames(3);
Do(() => { Lift("dashPad"); Check("Dash pad: dashes", dashes == before + 1, "dashes " + (dashes - before)); });
int w0 = 0;
Do(() => { w0 = gun.CurrentIndex; Finger("weapon", 15, Vector2.zero); });
Frames(3);
Do(() => { Lift("weapon"); Check("Gun pad: next weapon", gun.CurrentIndex == (w0 + 1) % gun.weapons.Length, w0 + " -> " + gun.CurrentIndex); gun.Select(0); });
TopDownKit.Interactable crown = null;
Do(() => { foreach (var p in TopDownKit.Pickup.All) if (p != null && p.name.StartsWith("Crown")) crown = p; Place(new Vector2(9f, -7f)); });
Wait(0.4f);
Do(() => Finger("use", 16, Vector2.zero));
Frames(3);
Do(() => { Lift("use"); Check("Use pad: uses the best target (takes the crown)", crown == null, "crown still there " + (crown != null)); });
Do(() => { Finger("fire", 17, Vector2.zero); pm.Pause(); });
Frames(3);
Do(() => Check("while paused the touch controls hide and let go (Fire not held)", !TopDownKit.Controls.Held(TopDownKit.Controls.Fire), "Fire held " + TopDownKit.Controls.Held(TopDownKit.Controls.Fire)));
Do(() => pm.Resume());

void Finish()
{
    EditorApplication.update -= Tick;
    TopDownKit.Dash.AnyStarted -= onDash;
    try { foreach (var p in new[] { "move", "look", "fire", "dashPad", "use", "weapon" }) Lift(p); } catch { }
    touch.mode = oldTouchMode;
    if (TopDownKit.PauseMenu.IsPaused) pm.Resume();
    TopDownKit.Controls.Import(oldControls);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(kb);
    UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
    foreach (var d in realOff) UnityEngine.InputSystem.InputSystem.EnableDevice(d);
    settings.editorInputBehaviorInPlayMode = oldEditorBehaviour;
    settings.backgroundBehavior = oldBackground;
    D.SetData("ct_state", (error != null ? "error: " + error + "\n" : "done: " + (failed == 0 ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks\n") + string.Join("\n", results));
}
void Tick()
{
    try
    {
        if (!EditorApplication.isPlaying) { error = "play mode ended"; Finish(); return; }
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (steps[stepIndex](stepFrame)) { stepIndex++; stepFrame = 0; stepStart = Time.unscaledTime; if (stepIndex >= steps.Count) { Finish(); return; } }
        else stepFrame++;
    }
    catch (System.Exception e) { error = e.ToString(); Finish(); }
}
stepStart = Time.unscaledTime;
D.SetData("ct_state", "running");
EditorApplication.update += Tick;
return "started " + steps.Count + " steps";

// Phase 15 check: the phase 9 combat additions and phase 12 hit-stop. Run IN PLAY MODE on
// Assets/Scenes/Playground.unity (sh Tools/measure/run-check.sh Playground Tools/measure/combat2.cs c2_state)
// poll:  unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("c2_state") as string;'
// Driven from the fixed step through the Gun's entry points (aim and trigger as data), against
// dummies (the Chaser prefab with its brain taken out, 10 health, no invulnerable time):
//   - Sword: one swing hits a target 30 deg off the aim once for its damage; one past the reach is not hit.
//   - Charger: held past chargeTime then let go = damage x chargeDamage; a tap = plain damage.
//   - Bouncer: fired at a wall, the bullet comes back (still alive, flying the other way).
//   - Aim assist: strength 0.7, target 10 deg off -> the shot leaves 7 deg off; strength 0 -> 0;
//     a target outside the cone -> 0; a nearer crate (aimAssistTarget off) is skipped.
//   - Hit-stop: the same sword hit with hit-stop on and off gives the same dummy path step by
//     step (the freeze only waits); a freeze stops the fixed step and gives timeScale back;
//     a pause during a freeze stays paused.
// Other enemies are switched off; the player's own trigger input is off. All put back at the end.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on Playground first";
var D = System.AppDomain.CurrentDomain;
if (D.GetData("c2_state") as string == "running") return "already running";

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var gun = player.GetComponent<TopDownKit.Gun>();
var shooter = player.GetComponent<TopDownKit.PlayerShooter>();
var feel = TopDownKit.FeelPlayer.Current;
var chaser = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Chaser.prefab");
var pistol = gun.weapons[0];
float oldSpread = pistol.spread;
var oldAssist = gun.aimAssist;
bool oldHitStop = feel != null && feel.hitStop;
var velField = typeof(TopDownKit.Bullet).GetField("velocity", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
Application.runInBackground = true;
shooter.enabled = false;
gun.SetTrigger(false);
var off = new System.Collections.Generic.List<GameObject>();
foreach (var b in UnityEngine.Object.FindObjectsByType<TopDownKit.EnemyBrain>(FindObjectsSortMode.None)) { off.Add(b.gameObject); b.gameObject.SetActive(false); }
// Real keyboards and mice off (a real click would pull the trigger), back on at the end.
var realOff = new System.Collections.Generic.List<UnityEngine.InputSystem.InputDevice>();
foreach (var d in UnityEngine.InputSystem.InputSystem.devices) if ((d is UnityEngine.InputSystem.Keyboard || d is UnityEngine.InputSystem.Mouse) && d.enabled) realOff.Add(d);
foreach (var d in realOff) UnityEngine.InputSystem.InputSystem.DisableDevice(d);

var results = new System.Collections.Generic.List<string>();
int failed = 0; string error = null; string phase = "fixed";
void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
void Place(Vector2 p) { rb.position = p; rb.linearVelocity = Vector2.zero; player.transform.position = p; }
var dummies = new System.Collections.Generic.List<GameObject>();
TopDownKit.Health Dummy(Vector2 at)
{
    var d = UnityEngine.Object.Instantiate(chaser, at, Quaternion.identity);
    UnityEngine.Object.DestroyImmediate(d.GetComponent<TopDownKit.EnemyBrain>());
    var pw = d.GetComponent<TopDownKit.PathWalker>(); if (pw != null) UnityEngine.Object.DestroyImmediate(pw);
    var h = d.GetComponent<TopDownKit.Health>(); h.maxHealth = 10; h.invulnerableTime = 0f; h.Restore();
    dummies.Add(d);
    return h;
}
void ClearDummies() { foreach (var d in dummies) if (d != null) UnityEngine.Object.Destroy(d); dummies.Clear(); }
// The bullet nearest the player: the one just fired (an older one, like the Bouncer's, may still fly).
TopDownKit.Bullet OneBullet() { TopDownKit.Bullet best = null; float bd = 1e9f; foreach (var b in UnityEngine.Object.FindObjectsByType<TopDownKit.Bullet>(FindObjectsSortMode.None)) { float d = Vector2.Distance(b.transform.position, rb.position); if (d < bd) { bd = d; best = b; } } return best; }
Vector2 Vel(TopDownKit.Bullet b) => (Vector2)velField.GetValue(b);
float Deg(Vector2 v) => Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

// ---- fixed-step tasks: each gets its own step counter s (0, 1, 2...) until it returns true ----
var tasks = new System.Collections.Generic.List<(string name, System.Func<int, bool> run)>();
TopDownKit.Health d1 = null, d2 = null;
int swings0 = 0;

tasks.Add(("sword", s =>
{
    if (s == 0)
    {
        Place(new Vector2(0f, 0f)); gun.Select(3); gun.Refill(); gun.SetAim(Vector2.right);
        d1 = Dummy(new Vector2(1.2f * Mathf.Cos(30f * Mathf.Deg2Rad), 1.2f * Mathf.Sin(30f * Mathf.Deg2Rad)));
        d2 = Dummy(new Vector2(3.2f, 0f));   // reach 1.5 + its radius is well short of 3.2
        var sw = player.GetComponent<TopDownKit.MeleeSwing>(); swings0 = sw != null ? sw.Swings : 0;
    }
    if (s == 5) gun.SetTrigger(true);
    if (s == 6) gun.SetTrigger(false);
    if (s < 40) return false;
    var sw2 = player.GetComponent<TopDownKit.MeleeSwing>();
    int swings = sw2 != null ? sw2.Swings - swings0 : 0;
    Check("sword: one swing hits a target 30 deg off once, for its damage (2)", d1.Current == 8 && swings == 1, "hp " + d1.Current + ", swings " + swings);
    Check("sword: a target past the reach is not hit", d2.Current == 10, "hp " + d2.Current);
    ClearDummies();
    return true;
}));

tasks.Add(("charge", s =>
{
    if (s == 0) { Place(new Vector2(0f, 0f)); gun.Select(4); gun.Refill(); gun.SetAim(Vector2.right); d1 = Dummy(new Vector2(4f, 0f)); }
    if (s == 2) gun.SetTrigger(true);
    if (s == 2 + 70) gun.SetTrigger(false);           // held 70 steps = 1.17 s > chargeTime 1 s
    if (s == 130) Check("charger: held to full charge, one hit of damage x4", d1.Current == 6, "hp " + d1.Current);
    if (s == 131) { Place(new Vector2(0f, 0f)); d1.Restore(); d1.transform.position = new Vector3(4f, 0f, 0f); d1.GetComponent<Rigidbody2D>().position = new Vector2(4f, 0f); }
    if (s == 135) gun.SetTrigger(true);
    if (s == 136) gun.SetTrigger(false);              // a tap: fires uncharged
    if (s < 200) return false;
    Check("charger: a tap fires uncharged (damage 1)", d1.Current == 9, "hp " + d1.Current);
    ClearDummies();
    return true;
}));

float minX = 0f; bool sawBullet = false;
tasks.Add(("bounce", s =>
{
    if (s == 0) { Place(new Vector2(-10f, 0f)); gun.Select(5); gun.Refill(); gun.SetAim(Vector2.left); minX = 0f; sawBullet = false; }
    if (s == 2) gun.SetTrigger(true);
    if (s == 3) gun.SetTrigger(false);
    var b = OneBullet();
    if (b != null) { sawBullet = true; minX = Mathf.Min(minX, b.transform.position.x); }
    if (s < 60) return false;
    Vector2 v = b != null ? Vel(b) : Vector2.zero;
    Check("bouncer: a shot at the west wall comes back", sawBullet && b != null && v.x > 0f && minX < -18f && minX > -19.01f,
        "alive " + (b != null) + ", velocity " + v.ToString("F1") + ", nearest the wall x " + minX.ToString("F2") + " (face at -19)");
    return true;
}));

float a7 = 99f, a0 = 99f, aOut = 99f, aCrate = 99f; GameObject crate = null;
tasks.Add(("aim assist", s =>
{
    float ang = 10f * Mathf.Deg2Rad;
    if (s == 0)
    {
        Place(new Vector2(0f, 0f)); gun.Select(0); gun.Refill(); pistol.spread = 0f; gun.SetAim(Vector2.right);
        d1 = Dummy(new Vector2(6f * Mathf.Cos(ang), 6f * Mathf.Sin(ang)));
        gun.aimAssist = new TopDownKit.AimAssistSettings { strength = 0.7f, cone = 15f, range = 10f };
    }
    if (s == 2) gun.SetTrigger(true);
    if (s == 3) gun.SetTrigger(false);
    if (s == 4) { var b = OneBullet(); a7 = b != null ? Deg(Vel(b)) : 99f; }
    if (s == 40) { gun.aimAssist = new TopDownKit.AimAssistSettings { strength = 0f, cone = 15f, range = 10f }; gun.SetTrigger(true); }
    if (s == 41) gun.SetTrigger(false);
    if (s == 42) { var b = OneBullet(); a0 = b != null ? Deg(Vel(b)) : 99f; }
    if (s == 80)
    {
        gun.aimAssist = new TopDownKit.AimAssistSettings { strength = 0.7f, cone = 15f, range = 10f };
        float o = 25f * Mathf.Deg2Rad; d1.transform.position = new Vector3(6f * Mathf.Cos(o), 6f * Mathf.Sin(o), 0f); d1.GetComponent<Rigidbody2D>().position = d1.transform.position;
    }
    if (s == 82) gun.SetTrigger(true);
    if (s == 83) gun.SetTrigger(false);
    if (s == 84) { var b = OneBullet(); aOut = b != null ? Deg(Vel(b)) : 99f; }
    if (s == 100)
    {
        float c = 10f * Mathf.Deg2Rad, e = -10f * Mathf.Deg2Rad;
        crate = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/World/Crate.prefab"), new Vector3(4f * Mathf.Cos(c), 4f * Mathf.Sin(c), 0f), Quaternion.identity);
        d1.transform.position = new Vector3(6f * Mathf.Cos(e), 6f * Mathf.Sin(e), 0f); d1.GetComponent<Rigidbody2D>().position = d1.transform.position;
    }
    if (s == 102) gun.SetTrigger(true);
    if (s == 103) gun.SetTrigger(false);
    if (s == 104) { var b = OneBullet(); aCrate = b != null ? Deg(Vel(b)) : 99f; UnityEngine.Object.Destroy(crate); }
    if (s < 110) return false;
    Check("aim assist skips crates (aimAssistTarget off): a nearer crate 10 deg up, an enemy 10 deg down -> bends down 7 deg", Mathf.Abs(aCrate + 7f) < 0.2f, aCrate.ToString("F2") + " deg");
    Check("aim assist 0.7: a target 10 deg off bends the shot 7 deg", Mathf.Abs(a7 - 7f) < 0.2f, a7.ToString("F2") + " deg");
    Check("aim assist 0: the shot goes straight", Mathf.Abs(a0) < 0.01f, a0.ToString("F2") + " deg");
    Check("aim assist: a target outside the cone (25 deg, cone 15) is ignored", Mathf.Abs(aOut) < 0.01f, aOut.ToString("F2") + " deg");
    pistol.spread = oldSpread; gun.aimAssist = oldAssist;
    ClearDummies();
    return true;
}));

// Hit-stop on and off: the dummy's path after a sword hit, step by step.
var paths = new System.Collections.Generic.List<string>[] { new System.Collections.Generic.List<string>(), new System.Collections.Generic.List<string>() };
bool freezeSeen = false;
for (int run = 0; run < 2; run++)
{
    int r = run;
    tasks.Add(("hit-stop " + (r == 0 ? "on" : "off"), s =>
    {
        if (s == 0)
        {
            if (feel != null) feel.hitStop = r == 0;
            Place(new Vector2(0f, 0f)); gun.Select(3); gun.Refill(); gun.SetAim(Vector2.right);
            d1 = Dummy(new Vector2(1.2f, 0f));
        }
        if (s == 5) gun.SetTrigger(true);
        if (s == 6) gun.SetTrigger(false);
        if (s >= 5 && d1 != null) paths[r].Add(d1.Current + "@" + d1.transform.position.x.ToString("F5"));
        if (s < 50) return false;
        ClearDummies();
        return true;
    }));
}

int taskStep = 0, frameStep = 0;
float f0 = 0f; bool steppedInFreeze = false; float realStart = 0f;
void OnFixed()
{
    if (phase != "fixed") return;
    try
    {
        if (tasks[0].run(taskStep++)) { tasks.RemoveAt(0); taskStep = 0; if (tasks.Count == 0) { phase = "freeze"; frameStep = 0; } }
    }
    catch (System.Exception e) { error = e.ToString(); phase = "end"; }
}

// ---- frame side: the freeze itself, then the end ----
void Tick()
{
    try
    {
        if (!EditorApplication.isPlaying) { error = "play mode ended"; Finish(); return; }
        if (feel != null && feel.Freezing) freezeSeen |= phase == "fixed";
        if (phase == "fixed") return;
        if (phase == "end") { Finish(); return; }
        int f = frameStep++;
        if (phase == "freeze")
        {
            if (f == 0)
            {
                bool same = paths[0].Count > 10 && string.Join(" ", paths[0]) == string.Join(" ", paths[1]) && paths[0][paths[0].Count - 1].StartsWith("8@");
                Check("hit-stop: the sword hit happened and froze the game (hit-stop on)", freezeSeen, "freeze seen " + freezeSeen);
                Check("hit-stop on vs off: the dummy's health and path are the same step by step", same,
                    paths[0].Count + " / " + paths[1].Count + " steps; on " + (paths[0].Count > 3 ? paths[0][3] : "-") + " off " + (paths[1].Count > 3 ? paths[1][3] : "-"));
                if (feel == null) { Check("hit-stop: a FeelPlayer in the scene", false, "none"); phase = "end"; return; }
                feel.hitStop = true; feel.Freeze(0.2f); f0 = Time.fixedTime; realStart = Time.realtimeSinceStartup; steppedInFreeze = false;
                return;
            }
            if (feel.Freezing) { if (Time.fixedTime != f0) steppedInFreeze = true; return; }
            float took = Time.realtimeSinceStartup - realStart;
            Check("hit-stop: no fixed step runs during a 0.2 s freeze, then time runs again", !steppedInFreeze && Time.timeScale == 1f && took > 0.15f && took < 0.5f,
                "stepped " + steppedInFreeze + ", timeScale " + Time.timeScale + ", lasted " + took.ToString("F2") + " s");
            phase = "pause"; frameStep = 0;
            return;
        }
        if (phase == "pause")
        {
            var pm = UnityEngine.Object.FindFirstObjectByType<TopDownKit.PauseMenu>();
            if (f == 0) { feel.Freeze(0.2f); pm.Pause(); return; }
            if (feel.Freezing) return;
            bool stayed = Time.timeScale == 0f && TopDownKit.PauseMenu.IsPaused;
            pm.Resume();
            Check("hit-stop: a pause during a freeze stays paused; Resume gives time back", stayed && Time.timeScale == 1f, "after freeze timeScale 0 = " + stayed + ", after Resume " + Time.timeScale);
            phase = "end";
        }
    }
    catch (System.Exception e) { error = e.ToString(); Finish(); }
}

var original = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
void Finish()
{
    EditorApplication.update -= Tick;
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(original);
    ClearDummies();
    pistol.spread = oldSpread; gun.aimAssist = oldAssist; gun.SetTrigger(false); gun.Select(0);
    if (feel != null) feel.hitStop = oldHitStop;
    shooter.enabled = true;
    foreach (var g in off) if (g != null) g.SetActive(true);
    foreach (var d in realOff) UnityEngine.InputSystem.InputSystem.EnableDevice(d);
    if (TopDownKit.PauseMenu.IsPaused) UnityEngine.Object.FindFirstObjectByType<TopDownKit.PauseMenu>().Resume();
    D.SetData("c2_state", (error != null ? "error: " + error + "\n" : "done: " + (failed == 0 ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks\n") + string.Join("\n", results));
}

var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var systems = loop.subSystemList;
for (int i = 0; i < systems.Length; i++)
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.FixedUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.Melee), updateDelegate = OnFixed });
        systems[i].subSystemList = list.ToArray();
    }
loop.subSystemList = systems;
D.SetData("c2_state", "running");
UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
EditorApplication.update += Tick;
return "started " + tasks.Count + " tasks";

// Combat check. Not compiled by Unity (it lives outside Assets/). Run IN PLAY MODE on
// Assets/Scenes/ArenaShooter.unity, from the project root:
//
//     unity command eval_file --file Tools/measure/combat.cs
//
// then poll until it says done (about 45 s):
//
//     unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("cb_state") as string;'
//
// Part 1, the same script at 30 and at 144 fps, everything counted in fixed steps:
//   - the player shoots a dummy (an enemy prefab with its brain removed) 4 times with the
//     pistol and once with the shotgun: hits, health, the dummy's knockback, ammo;
//   - a pistol shot at the west wall: the bullet must stop at the wall, not fly its range;
//   - the player is hit every step for 150 steps: hits must land 60 steps apart (1 s
//     invulnerable time), and a hit during a dash (invulnerable flag) must not land.
//   Writes one CSV row per fixed step; the two frame rates must match row for row.
// Part 2, allocation: 3 s idle, 3 s of heavy fire with Instantiate/Destroy, 3 s with
//   reused bullets (ObjectPool). Sums "GC Allocated In Frame" over each.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on ArenaShooter first";
if (System.AppDomain.CurrentDomain.GetData("cb_state") as string == "running") return "already running";
var inv = System.Globalization.CultureInfo.InvariantCulture;
string outPath = System.IO.Path.GetFullPath("Tools/measure/out/combat.csv");
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outPath));

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var hp = player.GetComponent<TopDownKit.Health>();
var gun = player.GetComponent<TopDownKit.Gun>();
var dash = player.GetComponent<TopDownKit.Dash>();
var shooterInput = player.GetComponent<TopDownKit.PlayerShooter>();
var chaser = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Chaser.prefab");
var pistol = gun.weapons[0]; var rapid = gun.weapons[2];
float oldSpread = pistol.spread, oldRate = rapid.fireRate; int oldCount = rapid.bulletsPerShot, oldMag = rapid.magazine;
pistol.spread = 0f; // single-bullet spread is random; the comparison wants the same path
shooterInput.enabled = false; // the mouse must not pull the trigger
foreach (var b in UnityEngine.Object.FindObjectsByType<TopDownKit.EnemyBrain>(FindObjectsSortMode.None)) b.gameObject.SetActive(false);
int oldTarget = Application.targetFrameRate, oldVsync = QualitySettings.vSyncCount;
QualitySettings.vSyncCount = 0;
Application.runInBackground = true;

var plan = new[] { 30, 144 };
var lines = new System.Collections.Generic.List<string> { "fps,step,playerHp,landed,dummyHp,dummyX,bullets,ammo" };
var results = new System.Collections.Generic.List<string>();
int run = -1, step = 0; string part = "start"; string error = null;
GameObject dummy = null; TopDownKit.Health dh = null; Rigidbody2D drb = null;
int landedSteps = 0;
var landings = new System.Collections.Generic.List<int>();
var wallStops = new System.Collections.Generic.List<int>();
float phaseStart = 0f; long allocSum = 0; int frames = 0; float worst = 0f; int spawned0 = 0;
var rec = Unity.Profiling.ProfilerRecorder.StartNew(Unity.Profiling.ProfilerCategory.Memory, "GC Allocated In Frame");
string N(double v) => v.ToString("0.#####", inv);

var original = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var systems = loop.subSystemList;

void Finish()
{
    try { System.IO.File.WriteAllLines(outPath, lines); } catch (System.Exception e) { error = (error ?? "") + " write: " + e.Message; }
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(original);
    rec.Dispose();
    pistol.spread = oldSpread; rapid.fireRate = oldRate; rapid.bulletsPerShot = oldCount; rapid.magazine = oldMag;
    TopDownKit.Bullet.reuse = true;
    shooterInput.enabled = true;
    QualitySettings.vSyncCount = oldVsync;
    Application.targetFrameRate = oldTarget;
    if (dummy != null) UnityEngine.Object.Destroy(dummy);
    // Phase 15: the check compares the two frame rates itself, row by row (was by hand).
    var byRate = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>();
    for (int i = 1; i < lines.Count; i++) { int c = lines[i].IndexOf(','); string k = lines[i].Substring(0, c); if (!byRate.ContainsKey(k)) byRate[k] = new System.Collections.Generic.List<string>(); byRate[k].Add(lines[i].Substring(c + 1)); }
    string verdict = "FAIL 30 vs 144 fps: a run is missing";
    if (byRate.ContainsKey("30") && byRate.ContainsKey("144"))
    {
        var r30 = byRate["30"]; var r144 = byRate["144"]; int differ = 0, first = -1;
        for (int i = 0; i < Mathf.Min(r30.Count, r144.Count); i++) if (r30[i] != r144[i]) { differ++; if (first < 0) first = i; }
        bool same = differ == 0 && r30.Count == r144.Count && r30.Count > 500;
        verdict = (same ? "PASS" : "FAIL") + " 30 vs 144 fps: " + r30.Count + " / " + r144.Count + " steps, " + differ + " differing" + (first >= 0 ? " (first at step " + first + ")" : "");
    }
    results.Insert(0, verdict);
    System.AppDomain.CurrentDomain.SetData("cb_state", error == null ? "done\n" + string.Join("\n", results) + "\n-> " + outPath : "error: " + error);
}

void Place(float x, float y) { rb.position = new Vector2(x, y); rb.linearVelocity = Vector2.zero; player.transform.position = new Vector3(x, y, 0f); }

// Part 1: driven from the fixed step, so frame rate cannot change what is asked.
void OnFixed()
{
    try
    {
        if (part != "steps") return;
        int s = step++;
        if (s == 0)
        {
            // Phase 15: the editor's mouse can read as held at Play (trap 84), leaving the trigger
            // down before PlayerShooter was switched off - then step 10's press had no edge.
            gun.SetTrigger(false);
            hp.Restore(); gun.Select(0); gun.Refill(); Place(-15f, 0f);
            dummy = UnityEngine.Object.Instantiate(chaser, new Vector3(-8f, 0f, 0f), Quaternion.identity);
            UnityEngine.Object.DestroyImmediate(dummy.GetComponent<TopDownKit.EnemyBrain>());
            dh = dummy.GetComponent<TopDownKit.Health>(); dh.maxHealth = 10; dh.Restore();
            drb = dummy.GetComponent<Rigidbody2D>();
            gun.SetAim(Vector2.right);
            landings.Clear(); wallStops.Clear();
        }
        bool landed = false;
        // Pistol x4 at steps 10, 40, 70, 100; shotgun once at 140 from 3 tiles.
        if (s == 10 || s == 40 || s == 70 || s == 100) gun.SetTrigger(true);
        if (s == 11 || s == 41 || s == 71 || s == 101 || s == 141 || s == 221) gun.SetTrigger(false);
        if (s == 130) { gun.Select(1); Place(drb.position.x - 3f, 0f); }
        if (s == 140) gun.SetTrigger(true);
        // Wall shot: pistol to the west wall from 4.5 tiles.
        if (s == 200) { gun.Select(0); UnityEngine.Object.Destroy(dummy); Place(-15f, 0f); gun.SetAim(Vector2.left); }
        if (s == 220) gun.SetTrigger(true);
        if (s > 221 && s < 280 && TopDownKit.Bullet.Alive == 0 && wallStops.Count == 0) wallStops.Add(s - 221);
        // Damage every step from 300 to 449; then a dash at 520 with hits during it.
        if (s >= 300 && s < 450) landed = hp.Hit(1, Vector2.zero);
        if (s == 450) hp.Restore();
        if (s == 520) dash.Request();
        if (s >= 523 && s < 528) landed = hp.Hit(1, Vector2.zero);
        if (landed) landings.Add(s);
        lines.Add(plan[run] + "," + s + "," + hp.Current + "," + (landed ? 1 : 0) + "," + (dh != null ? dh.Current : -1) + ","
            + (drb != null ? N(drb.position.x) : "") + "," + TopDownKit.Bullet.Alive + "," + gun.Ammo);
        if (s == 560)
        {
            results.Add(plan[run] + " fps: dummy hp at step 199: " + (lines[lines.Count - 362].Split(',')[4]) + ", hits landed at steps " + string.Join(" ", landings)
                + ", wall bullet gone " + (wallStops.Count > 0 ? wallStops[0] + " steps after the shot" : "never"));
            part = "next";
        }
    }
    catch (System.Exception e) { error = e.ToString(); part = "error"; }
}

// Frame side: switches runs and does part 2.
void OnFrame()
{
    try
    {
        if (part == "error") { Finish(); return; }
        if (part == "start" || part == "next")
        {
            run++;
            if (run < plan.Length) { Application.targetFrameRate = plan[run]; step = 0; part = "settle"; phaseStart = Time.time; return; }
            Application.targetFrameRate = 60;
            gun.Select(2); rapid.fireRate = 60f; rapid.bulletsPerShot = 5; rapid.magazine = 0;
            hp.Restore(); Place(-15f, 0f); gun.SetAim(Vector2.right);
            part = "idle"; phaseStart = Time.time; allocSum = 0; frames = 0; worst = 0f;
            return;
        }
        if (part == "settle") { if (Time.time - phaseStart > 0.3f) part = "steps"; return; }
        if (part == "steps") return;
        float t = Time.time - phaseStart;
        if (t > 0.2f) { allocSum += rec.LastValue; frames++; worst = Mathf.Max(worst, Time.unscaledDeltaTime); }
        if (t < 3.2f) return;
        string line = part + ": " + frames + " frames, " + (allocSum / 1024) + " KB allocated (" + (frames > 0 ? allocSum / frames : 0) + " B/frame), worst frame " + N(worst * 1000) + " ms, shots " + (gun.ShotsFired - spawned0);
        results.Add(line);
        spawned0 = gun.ShotsFired;
        if (part == "idle") { TopDownKit.Bullet.reuse = false; gun.SetTrigger(true); part = "instantiate"; }
        else if (part == "instantiate") { TopDownKit.Bullet.reuse = true; part = "pool"; }
        else { gun.SetTrigger(false); Finish(); return; }
        phaseStart = Time.time; allocSum = 0; frames = 0; worst = 0f;
    }
    catch (System.Exception e) { error = e.ToString(); Finish(); }
}

for (int i = 0; i < systems.Length; i++)
{
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.FixedUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Add(new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.Damage), updateDelegate = OnFixed });
        systems[i].subSystemList = list.ToArray();
    }
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.PreLateUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Insert(0, new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.Gun), updateDelegate = OnFrame });
        systems[i].subSystemList = list.ToArray();
    }
}
loop.subSystemList = systems;
System.AppDomain.CurrentDomain.SetData("cb_state", "running");
UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
return "started, about 20 s";

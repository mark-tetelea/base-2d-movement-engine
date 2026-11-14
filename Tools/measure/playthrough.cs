// Arena shooter playthrough. Run IN PLAY MODE on Assets/Scenes/ArenaShooter.unity:
//     unity command eval_file --file Tools/measure/playthrough.cs
// poll:  unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("pt_state") as string;'
// A bot plays the demo with the real Gun (the rapid weapon, with its reloads): it is put
// into room B, C, then area D (as the doors and the warp would take it), walks towards
// the nearest enemy in its room when far and shoots it. The player is given 999 health so the run is
// about the rooms, not the bot's skill. Checks each room locks, runs all its waves and
// clears (doors open), and that the HUD's win state is reached. Then it lets the enemies
// of a fresh dummy kill the player at normal health and checks the scene restarts.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on ArenaShooter first";
if (System.AppDomain.CurrentDomain.GetData("pt_state") as string == "running") return "already running";
var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var hp = player.GetComponent<TopDownKit.Health>();
var gun = player.GetComponent<TopDownKit.Gun>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var input = player.GetComponent<TopDownKit.PlayerShooter>();
var rooms = new[] { ("Room B", new Vector2(40f, 0f)), ("Room C", new Vector2(20f, 26f)), ("Area D", new Vector2(104f, 0f)) };
var results = new System.Collections.Generic.List<string>();
int failed = 0, index = -1; float start = 0f; string phase = "next"; string error = null;
int sceneLoads = 0; float deathTime = 0f;
TopDownKit.RoomLock lk = null; TopDownKit.RoomWaves rw = null; int maxWave = 0;
Application.runInBackground = true;
input.enabled = false; mover.readKeyboard = false;
hp.maxHealth = 999; hp.Restore();
gun.Select(2); gun.Refill();
UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => sceneLoads++;

void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
void Place(Vector2 p) { rb.position = p; rb.linearVelocity = Vector2.zero; player.transform.position = p; }

var original = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var systems = loop.subSystemList;
void Finish()
{
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(original);
    System.AppDomain.CurrentDomain.SetData("pt_state", error != null ? "error: " + error
        : "done: " + (failed == 0 ? "ALL PASS" : failed + " FAILED") + "\n" + string.Join("\n", results));
}

// Bot: aim at the nearest living enemy, walk to it when far, hold the trigger.
void OnFixed()
{
    if (phase != "fight" || player == null) return;
    TopDownKit.Health best = null; float bestD = float.MaxValue;
    foreach (var h in TopDownKit.Health.All)
    {
        if (h.team == 0 || h.Dead) continue;
        if (TopDownKit.Room.At(h.transform.position) != TopDownKit.Room.At(rb.position)) continue;
        float d = ((Vector2)h.transform.position - rb.position).sqrMagnitude;
        if (d < bestD) { bestD = d; best = h; }
    }
    if (best == null) { gun.SetTrigger(false); mover.SetMoveDirection(Vector2.zero); return; }
    Vector2 to = (Vector2)best.transform.position - rb.position;
    gun.SetAim(to);
    gun.SetTrigger(true);
    mover.SetMoveDirection(to.magnitude > 6f ? to.normalized : Vector2.zero);
}

string Where()
{
    string w = "";
    foreach (var h in TopDownKit.Health.All) if (h.team != 0 && !h.Dead) w += " enemy " + h.name + " " + (Vector2)h.transform.position + " hp " + h.Current;
    return w;
}

void OnFrame()
{
    try
    {
        float t = Time.time - start;
        if (phase == "next")
        {
            index++;
            if (index >= rooms.Length)
            {
                bool won = true;
                foreach (var r in UnityEngine.Object.FindObjectsByType<TopDownKit.RoomWaves>(FindObjectsSortMode.None))
                    won &= r.GetComponent<TopDownKit.RoomLock>().Cleared;
                Check("every wave room cleared (HUD shows the win)", won, "");
                // Death: normal health, enemy shots and touches with nobody shooting back.
                hp.maxHealth = 6; hp.Restore();
                gun.SetTrigger(false);
                var chaser = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Chaser.prefab");
                Place(new Vector2(-12f, 0f));
                for (int i = 0; i < 4; i++)
                {
                    var e = UnityEngine.Object.Instantiate(chaser, new Vector3(-8f, -3f + 2f * i, 0f), Quaternion.identity);
                    e.GetComponent<TopDownKit.EnemyBrain>().target = player.transform;
                }
                phase = "die"; start = Time.time;
                return;
            }
            var go = GameObject.Find(rooms[index].Item1);
            lk = go.GetComponent<TopDownKit.RoomLock>(); rw = go.GetComponent<TopDownKit.RoomWaves>(); maxWave = 0;
            Place(rooms[index].Item2);
            phase = "fight"; start = Time.time;
            return;
        }
        if (phase == "fight")
        {
            maxWave = Mathf.Max(maxWave, rw.WaveNumber);
            if (lk.Cleared)
            {
                bool doorsOpen = true;
                if (lk.doors != null) foreach (var d in lk.doors) doorsOpen &= !d.activeSelf;
                Check(rooms[index].Item1 + " cleared", doorsOpen && maxWave >= rw.WaveCount,
                    "waves seen " + maxWave + "/" + rw.WaveCount + ", doors open " + doorsOpen + ", " + t.ToString("0.0") + " s, player hit " + (999 - hp.Current) + " times");
                phase = "next";
            }
            else if (t > 30f)
            {
                Check(rooms[index].Item1 + " cleared", false, "timed out; locked " + lk.Locked + ", wave " + rw.WaveNumber + ", living " + TopDownKit.RoomWaves.Living(rw.waves[Mathf.Max(0, rw.WaveNumber - 1)]) + ", player " + rb.position + Where());
                phase = "next";
            }
            return;
        }
        if (phase == "die")
        {
            if (player == null || sceneLoads > 0)
            {
                var p2 = GameObject.Find("Player");
                var h2 = p2 != null ? p2.GetComponent<TopDownKit.Health>() : null;
                Check("death restarts the scene", sceneLoads > 0 && h2 != null && h2.Current == h2.maxHealth,
                    "loads " + sceneLoads + ", new player health " + (h2 != null ? h2.Current + "/" + h2.maxHealth : "none") + ", " + deathTime.ToString("0.0") + " s to die");
                Finish();
                return;
            }
            if (hp.Dead && deathTime == 0f) deathTime = t;
            if (t > 40f) { Check("death restarts the scene", false, "player health " + hp.Current); Finish(); }
        }
    }
    catch (System.Exception e) { error = e.ToString(); Finish(); }
}

for (int i = 0; i < systems.Length; i++)
{
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.FixedUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Insert(0, new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.RoomWaves), updateDelegate = OnFixed });
        systems[i].subSystemList = list.ToArray();
    }
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.PreLateUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Insert(0, new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.EnemyBrain), updateDelegate = OnFrame });
        systems[i].subSystemList = list.ToArray();
    }
}
loop.subSystemList = systems;
System.AppDomain.CurrentDomain.SetData("pt_state", "running");
UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
return "started, up to 4 minutes";

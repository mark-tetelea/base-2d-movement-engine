// Bullet allocation check. Run IN PLAY MODE on ArenaShooter, three times, a second apart:
//     unity command eval_file --file Tools/measure/bullet-alloc.cs
// Run 1 spawns 300 bullets with Instantiate (Bullet.reuse off), run 2 with the pool (fills
// it as they die), run 3 with the pool warm. Reports managed bytes and time per bullet,
// as managed heap growth plus Unity native memory growth around the spawn calls (a GC in
// between would hide some; repeat if a number looks odd).
// The bullets are fired into the west wall of room A, so they die within a few steps.
var player = GameObject.Find("Player");
var w = player.GetComponent<TopDownKit.Gun>().weapons[0];
string key = "alloc_run";
int run = (System.AppDomain.CurrentDomain.GetData(key) as int?) ?? 0;
TopDownKit.Bullet.reuse = run != 0;
var origin = new Vector2(-17f, 0f);
// Warm the code path once so the first-call JIT is not counted.
TopDownKit.Bullet.Spawn(origin, Vector2.left * w.bulletSpeed, w, 0);
var sw = System.Diagnostics.Stopwatch.StartNew();
long before = (System.GC.GetTotalMemory(false) + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
for (int i = 0; i < 300; i++) TopDownKit.Bullet.Spawn(origin, Vector2.left * w.bulletSpeed, w, 0);
long bytes = (System.GC.GetTotalMemory(false) + UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong()) - before;
sw.Stop();
System.AppDomain.CurrentDomain.SetData(key, (run + 1) % 3);
string mode = run == 0 ? "instantiate" : run == 1 ? "pool (cold)" : "pool (warm)";
TopDownKit.Bullet.reuse = true;
return mode + ": " + (bytes / 300) + " B and " + (sw.Elapsed.TotalMilliseconds * 1000.0 / 300).ToString("0.0") + " us per bullet";

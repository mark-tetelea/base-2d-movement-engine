// Phase 15 check: the phase 10 enemy additions. Run IN PLAY MODE on Assets/Scenes/Playground.unity
// (sh Tools/measure/run-check.sh Playground Tools/measure/enemies2.cs e2_state 120).
//   - Paths: a Chaser south of the tile garden's short wall, the player north of it behind the
//     closed key door (no straight way) - it walks round and reaches the player. Same for the
//     grid-step Stepper, which stands on tile centres whenever it is not mid-slide.
//   - Groups: the Leader's three followers keep near their places in the shape while the player
//     is far; with the player in their middle (each inside breakRange 4) they leave the shape and reach it
//     (the rule is per follower: one further than breakRange keeps its place).
//   - Trail: the Trailer leaves trail tiles and is not hurt by them; they are gone lastFor (3 s)
//     after it stops. Temporary ground hurts 1 a second (once at once, then every second) and
//     never hurts its owner.
// The player is made very tough for the run; everything is put back at the end.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on Playground first";
var D = System.AppDomain.CurrentDomain;
if (D.GetData("e2_state") as string == "running") return "already running";

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var php = player.GetComponent<TopDownKit.Health>();
int oldMax = php.maxHealth;
php.maxHealth = 9999; php.Restore();
mover.readKeyboard = false;
mover.SetMoveDirection(Vector2.zero);
Application.runInBackground = true;
var shooter = player.GetComponent<TopDownKit.PlayerShooter>(); shooter.enabled = false;

var brains = new System.Collections.Generic.List<GameObject>();
foreach (var b in UnityEngine.Object.FindObjectsByType<TopDownKit.EnemyBrain>(FindObjectsSortMode.None)) { brains.Add(b.gameObject); b.gameObject.SetActive(false); }
GameObject Named(string n) { foreach (var g in brains) if (g != null && g.name == n) return g; return null; }
var leader = Named("Leader");
var group = leader.GetComponent<TopDownKit.EnemyGroup>();
var trailer = Named("Trailer");
var trailTile = trailer.GetComponent<TopDownKit.TrailMaker>().tile;
var map = UnityEngine.Object.FindFirstObjectByType<TopDownKit.GroundMap>();

var results = new System.Collections.Generic.List<string>();
int failed = 0; string error = null;
void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
void Place(Rigidbody2D body, Vector2 p) { body.position = p; body.linearVelocity = Vector2.zero; body.transform.position = p; }
var made = new System.Collections.Generic.List<GameObject>();
GameObject Spawn(string prefab, Vector2 at)
{
    var g = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/" + prefab + ".prefab"), at, Quaternion.identity);
    g.GetComponent<TopDownKit.EnemyBrain>().target = player.transform;
    made.Add(g);
    return g;
}
int TrailCells(Vector2 around)
{
    int n = 0;
    var c0 = map.CellAt(around);
    for (int x = -15; x <= 15; x++) for (int y = -15; y <= 15; y++) if (map.TileAt(c0 + new Vector3Int(x, y, 0)) == trailTile) n++;
    return n;
}

var steps = new System.Collections.Generic.List<System.Func<int, bool>>();
int stepIndex = 0, stepFrame = 0, lastFrame = -1; float stepStart = 0f;
float T() => Time.time - stepStart;

// ---- paths round the wall (free walker, then grid walker) ----
foreach (var kind in new[] { "Chaser", "Stepper" })
{
    string k = kind;
    GameObject e = null; Rigidbody2D erb = null; int maxPath = 0; bool clearAtStart = true; int settled = 0, offCentre = 0;
    steps.Add(f =>
    {
        if (f == 0)
        {
            Place(rb, new Vector2(4.5f, 19.5f));
            e = Spawn(k, new Vector2(4.5f, 14.5f)); erb = e.GetComponent<Rigidbody2D>();
            maxPath = 0; settled = 0; offCentre = 0;
            return false;
        }
        var pw = e.GetComponent<TopDownKit.PathWalker>();
        if (f == 2) clearAtStart = pw.WayClear(new Vector2(4.5f, 14.5f), new Vector2(4.5f, 19.5f));
        if (pw.Path != null) maxPath = Mathf.Max(maxPath, pw.Path.Count);
        var em = e.GetComponent<TopDownKit.PlayerMover>();
        if (k == "Stepper" && f > 30 && !em.Sliding)
        {
            settled++;
            Vector2 sp = TopDownKit.GridStep.StandPoint(erb);
            if (Mathf.Abs(sp.x - (Mathf.Floor(sp.x) + 0.5f)) > 0.01f || Mathf.Abs(sp.y - (Mathf.Floor(sp.y) + 0.5f)) > 0.01f) offCentre++;
        }
        float dist = Vector2.Distance(erb.position, rb.position);
        bool reached = dist < (k == "Stepper" ? 1.6f : 1.3f);
        if (!reached && T() < 12f) return false;
        Check(k + " walks round the wall to the player (no straight way)", reached && !clearAtStart && maxPath > 0,
            "reached " + reached + " in " + T().ToString("F1") + " s, straight way clear " + clearAtStart + ", longest path " + maxPath + " tiles");
        if (k == "Stepper") Check("Stepper stands on a tile centre whenever it is not sliding", settled > 5 && offCentre == 0, settled + " settled frames, " + offCentre + " off centre");
        UnityEngine.Object.Destroy(e);
        return true;
    });
}

// ---- group ----
var slotDist = new System.Collections.Generic.List<float>();
float before = 0f; float[] closest = null;
float MeanToPlayer() { float s = 0f; foreach (var fo in group.followers) s += Vector2.Distance(fo.transform.position, rb.position); return s / group.followers.Length; }
steps.Add(f =>
{
    if (f == 0) { Place(rb, new Vector2(0f, -2f)); leader.SetActive(true); foreach (var fo in group.followers) fo.gameObject.SetActive(true); return false; }
    if (T() < 1f) return false;
    if (T() < 4f)
    {
        for (int i = 0; i < group.followers.Length; i++) slotDist.Add(Vector2.Distance(TopDownKit.GridStep.StandPoint(group.followers[i].GetComponent<Rigidbody2D>()), group.Place(i)));
        return false;
    }
    slotDist.Sort();
    float median = slotDist[slotDist.Count / 2], worst = slotDist[slotDist.Count - 1];
    Check("group: followers keep near their places while the player is far", slotDist.Count > 30 && median < 1f,
        slotDist.Count + " samples, median " + median.ToString("F2") + " tiles, worst " + worst.ToString("F2"));
    return true;
});
steps.Add(f =>
{
    if (f == 0) { Vector2 mid = Vector2.zero; foreach (var fo in group.followers) mid += (Vector2)fo.transform.position; mid /= group.followers.Length; Place(rb, mid); before = MeanToPlayer(); closest = new float[group.followers.Length]; for (int i = 0; i < closest.Length; i++) closest[i] = 99f; return false; }
    for (int i = 0; i < group.followers.Length; i++) closest[i] = Mathf.Min(closest[i], Vector2.Distance(group.followers[i].transform.position, rb.position));
    if (T() < 2f) return false;
    float after = MeanToPlayer();
    string each = ""; int broke = 0;
    for (int i = 0; i < group.followers.Length; i++)
    {
        var fb = group.followers[i].GetComponent<Rigidbody2D>();
        float toP = Vector2.Distance(fb.position, rb.position), toSlot = Vector2.Distance(TopDownKit.GridStep.StandPoint(fb), group.Place(i));
        if (closest[i] < 1.5f) broke++;
        each += " [" + toP.ToString("F1") + " / " + toSlot.ToString("F1") + ", closest " + closest[i].ToString("F1") + "]";
    }
    Check("group: inside breakRange every follower leaves the shape and reaches the player (within 1.5)", broke == group.followers.Length,
        "mean distance to the player " + before.ToString("F2") + " -> " + after.ToString("F2") + "; each follower [to player / to its place now, closest to the player]" + each);
    leader.SetActive(false); foreach (var fo in group.followers) fo.gameObject.SetActive(false);
    return true;
});

// ---- trail ----
int cellsWhileMoving = 0; Vector2 trailAt = Vector2.zero;
steps.Add(f =>
{
    if (f == 0) { Place(rb, new Vector2(0f, -2f)); trailer.SetActive(true); return false; }
    if (T() < 3f) return false;
    trailAt = trailer.transform.position;
    cellsWhileMoving = TrailCells(trailAt);
    var th = trailer.GetComponent<TopDownKit.Health>();
    Check("trail: the Trailer leaves trail tiles and is not hurt by its own trail", cellsWhileMoving >= 3 && th.Current == th.Max, cellsWhileMoving + " trail tiles, its health " + th.Current + "/" + th.Max);
    trailer.SetActive(false);
    return true;
});
steps.Add(f =>
{
    if (T() < 3.3f) return false;
    int left = TrailCells(trailAt);
    Check("trail: tiles are gone lastFor (3 s) after it stops", left == 0, left + " left after 3.3 s");
    return true;
});
TopDownKit.Health dummy = null, owner = null; int hpAt0 = 0, hpAt05 = 0;
steps.Add(f =>
{
    if (f == 0)
    {
        var a = Spawn("Chaser", new Vector2(10.5f, 25.5f)); UnityEngine.Object.DestroyImmediate(a.GetComponent<TopDownKit.EnemyBrain>());
        var b = Spawn("Chaser", new Vector2(14.5f, 25.5f)); UnityEngine.Object.DestroyImmediate(b.GetComponent<TopDownKit.EnemyBrain>());
        dummy = a.GetComponent<TopDownKit.Health>(); owner = b.GetComponent<TopDownKit.Health>();
        foreach (var h in new[] { dummy, owner }) { h.maxHealth = 10; h.invulnerableTime = 0f; h.Restore(); }
        bool p1 = map.PaintFor(map.CellAt(new Vector2(10.5f, 25.5f)), trailTile, 5f, null);
        bool p2 = map.PaintFor(map.CellAt(new Vector2(14.5f, 25.5f)), trailTile, 5f, owner);
        if (!p1 || !p2) Check("trail: temporary ground can be laid on plain floor", false, "laid " + p1 + " " + p2);
        return false;
    }
    if (f == 3) hpAt0 = dummy.Current;
    if (T() < 0.5f) return false;
    if (hpAt05 == 0) hpAt05 = dummy.Current;
    if (T() < 1.1f) return false;
    Check("trail ground hurts 1 at once, then 1 a second", hpAt0 == 9 && hpAt05 == 9 && dummy.Current == 8, "health 10 -> " + hpAt0 + " -> " + hpAt05 + " (0.5 s) -> " + dummy.Current + " (1.1 s)");
    Check("trail ground never hurts its owner", owner.Current == 10, "owner health " + owner.Current);
    return true;
});

void Finish()
{
    EditorApplication.update -= Tick;
    foreach (var g in made) if (g != null) UnityEngine.Object.Destroy(g);
    foreach (var g in brains) if (g != null) g.SetActive(true);
    php.maxHealth = oldMax; php.Restore();
    mover.readKeyboard = true; shooter.enabled = true;
    Place(rb, new Vector2(0f, -2f));
    D.SetData("e2_state", (error != null ? "error: " + error + "\n" : "done: " + (failed == 0 ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks\n") + string.Join("\n", results));
}
void Tick()
{
    try
    {
        if (!EditorApplication.isPlaying) { error = "play mode ended"; Finish(); return; }
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (steps[stepIndex](stepFrame)) { stepIndex++; stepFrame = 0; stepStart = Time.time; if (stepIndex >= steps.Count) { Finish(); return; } }
        else stepFrame++;
    }
    catch (System.Exception e) { error = e.ToString(); Finish(); }
}
stepStart = Time.time;
D.SetData("e2_state", "running");
EditorApplication.update += Tick;
return "started " + steps.Count + " steps";

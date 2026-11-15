// Smoke check of a built level (PLAY mode, Dungeon scene, ~1 s after Play):
//     unity command eval_file --file Tools/measure/dungeon-play.cs
// Walks the level over the real colliders: from the player's tile, flood over tiles with no
// solid collider (walls, closed doors, blue water block; crates and enemies count as open -
// crates break). Every room must have nearly all its floor reached; the player must be in
// the start room; the boss bar must be wired. One run = one seed (reported).
if (!EditorApplication.isPlaying) return "press Play on Dungeon first";
var gen = UnityEngine.Object.FindFirstObjectByType<TopDownKit.RoomGenerator>();
if (gen == null || !gen.Built) return "FAIL generator not built";
var player = gen.player;
string log = "seed " + gen.UsedSeed + ", rooms " + gen.Rooms.Count + "\n";
int fails = 0;
var hits = new Collider2D[16];
var filter = new ContactFilter2D(); filter.useTriggers = false;
bool Open(Vector2Int c)
{
    int n = Physics2D.OverlapBox(new Vector2(c.x + 0.5f, c.y + 0.5f), new Vector2(0.8f, 0.8f), 0f, filter, hits);
    for (int i = 0; i < n; i++)
    {
        if (hits[i].transform == player) continue;
        if (hits[i].GetComponentInParent<TopDownKit.Health>() != null) continue;
        return false;
    }
    return true;
}
Rect bounds = gen.Rooms[0].GetComponent<TopDownKit.Room>().Bounds;
foreach (var r in gen.Rooms) { var b = r.GetComponent<TopDownKit.Room>().Bounds; bounds.min = Vector2.Min(bounds.min, b.min); bounds.max = Vector2.Max(bounds.max, b.max); }
var start = new Vector2Int(Mathf.FloorToInt(player.position.x), Mathf.FloorToInt(player.position.y));
var seen = new System.Collections.Generic.HashSet<Vector2Int> { start };
var q = new System.Collections.Generic.Queue<Vector2Int>(); q.Enqueue(start);
while (q.Count > 0)
{
    var c = q.Dequeue();
    foreach (var s in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
    {
        var n = c + s;
        if (seen.Contains(n) || !bounds.Contains(n + Vector2.one * 0.5f) || !Open(n)) continue;
        seen.Add(n); q.Enqueue(n);
    }
}
var floor = gen.floor;
foreach (var r in gen.Rooms)
{
    var b = r.GetComponent<TopDownKit.Room>().Bounds;
    int walk = 0, reached = 0;
    for (int x = Mathf.RoundToInt(b.xMin) + 1; x < Mathf.RoundToInt(b.xMax) - 1; x++)
        for (int y = Mathf.RoundToInt(b.yMin) + 1; y < Mathf.RoundToInt(b.yMax) - 1; y++)
        {
            var c = new Vector2Int(x, y);
            if (!Open(c)) continue;
            walk++;
            if (seen.Contains(c)) reached++;
        }
    bool good = walk > 0 && reached >= walk * 0.95f;
    log += (good ? "ok   " : "FAIL ") + r.name + (r == gen.EndRoom ? " [end]" : "") + ": reached " + reached + "/" + walk + " open tiles\n";
    if (!good) fails++;
}
// The walls really collide: a wall tile of the start room's corner.
var sb = gen.Rooms[0].GetComponent<TopDownKit.Room>().Bounds;
bool wallHit = !Open(new Vector2Int(Mathf.RoundToInt(sb.xMin), Mathf.RoundToInt(sb.yMin)));
log += (wallHit ? "ok   " : "FAIL ") + "stamped walls have colliders\n"; if (!wallHit) fails++;
bool inStart = TopDownKit.Room.At(player.position) == gen.Rooms[0].GetComponent<TopDownKit.Room>();
log += (inStart ? "ok   " : "FAIL ") + "player in the start room\n"; if (!inStart) fails++;
var bp = gen.EndRoom.GetComponentInChildren<TopDownKit.BossPhases>(true);
// Phase 14: the generator no longer sets BossPhases.hud; the boss takes the level's HUD (Hud.Current) on its first step.
bool bar = bp == null || bp.hud != null || TopDownKit.Hud.Current != null;
log += (bar ? "ok   " : "FAIL ") + "boss bar wired (" + (bp != null ? "boss present" : "no boss") + ")\n"; if (!bar) fails++;
int doors = 0; foreach (var r in gen.Rooms) { var l = r.GetComponent<TopDownKit.RoomLock>(); if (l != null) doors += l.doors.Length; }
log += "door ground objects on locks: " + doors + "\n";
return (fails == 0 ? "ALL PASS\n" : fails + " FAILED\n") + log;

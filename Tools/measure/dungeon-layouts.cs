// The generator's vital check (EDIT mode, no play):
//     unity command eval_file --file Tools/measure/dungeon-layouts.cs
// Uses the RoomGenerator in Assets/Scenes/Dungeon.unity (its pieces and room count).
// 1. Pieces: inside each piece, every door spot's inner tiles are walkable ground and all
//    door spots connect to each other (flood fill over floor that is not wall, does not
//    block walking and does not kill; crates count as walkable - they break).
// 2. Layouts: seeds 1..200 at the scene's room count, plus 50 seeds at each count 2..12:
//    a layout is found, has exactly that many rooms, no overlaps, every link lines up,
//    every room is reachable from the start through doors, the end room exists, is the
//    end piece, is a dead end (one door) and is the deepest room or tied for it.
// Together: every room of every generated level can be walked to from the start.
if (EditorApplication.isPlaying) return "exit play mode first";
if (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().isDirty) return "the open scene has unsaved changes";
var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Dungeon.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
var gen = UnityEngine.Object.FindFirstObjectByType<TopDownKit.RoomGenerator>();
if (gen == null) return "FAIL no RoomGenerator in Dungeon";
var pieces = gen.AllPieces();
var shapes = TopDownKit.RoomGenerator.Shapes(pieces);
var allowed = gen.Allowed(pieces);
string log = "pieces " + pieces.Length + " (allowed middle " + allowed.Length + "), doorWidth " + gen.doorWidth + "\n";
int fails = 0;

// ---- 1. Pieces ----------------------------------------------------------------------
foreach (var p in pieces)
{
    var size = p.SizeTiles;
    bool Walkable(int x, int y)
    {
        if (x <= 0 || y <= 0 || x >= size.x - 1 || y >= size.y - 1) return false;
        var c = new Vector3Int(x, y, 0);
        if (p.walls.HasTile(c)) return false;
        var t = p.floor.GetTile(c);
        if (t == null) return false;
        if (t is TopDownKit.GroundTile g && (g.rules.blocksWalking || g.rules.kills)) return false;
        return true;
    }
    // Tiles just inside each door.
    var inner = new System.Collections.Generic.List<Vector2Int[]>();
    foreach (var d in p.doors)
    {
        var cells = TopDownKit.RoomPiece.DoorCells(d, p.cellTiles, size, gen.doorWidth);
        var step = -TopDownKit.Layout.Step(d.side);
        var ins = new Vector2Int[cells.Length];
        for (int i = 0; i < cells.Length; i++) ins[i] = cells[i] + step;
        inner.Add(ins);
    }
    string bad = "";
    for (int k = 0; k < inner.Count; k++)
        foreach (var c in inner[k]) if (!Walkable(c.x, c.y)) bad += " door " + k + " inside " + c + " blocked;";
    // Flood from the first door's inside.
    var seen = new System.Collections.Generic.HashSet<Vector2Int>();
    var q = new System.Collections.Generic.Queue<Vector2Int>();
    if (inner.Count > 0) { q.Enqueue(inner[0][0]); seen.Add(inner[0][0]); }
    while (q.Count > 0)
    {
        var c = q.Dequeue();
        foreach (var s in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
        {
            var n = c + s;
            if (seen.Contains(n) || !Walkable(n.x, n.y)) continue;
            seen.Add(n); q.Enqueue(n);
        }
    }
    for (int k = 0; k < inner.Count; k++)
    {
        bool any = false;
        foreach (var c in inner[k]) if (seen.Contains(c)) any = true;
        if (!any) bad += " door " + k + " not connected;";
    }
    if (p.doors.Length == 0) bad += " no door spots;";
    log += (bad == "" ? "ok   " : "FAIL ") + p.name + " " + p.cells.x + "x" + p.cells.y + ", " + p.doors.Length + " doors, " + seen.Count + " tiles reached" + bad + "\n";
    if (bad != "") fails++;
}

// ---- 2. Layouts ---------------------------------------------------------------------
string Check(int count, int seed)
{
    var r = new TopDownKit.Layout.Result();
    if (!TopDownKit.Layout.Generate(shapes, 0, 1, allowed, count, seed, r)) return "no layout";
    if (r.rooms.Count != System.Math.Max(2, count)) return "rooms " + r.rooms.Count;
    string prob = TopDownKit.Layout.Problems(shapes, r);
    if (prob != "") return prob;
    if (TopDownKit.Layout.Reachable(r) != r.rooms.Count) return "reachable " + TopDownKit.Layout.Reachable(r) + "/" + r.rooms.Count;
    if (r.endRoom < 0 || r.rooms[r.endRoom].shape != 1) return "no end room";
    if (r.rooms[0].shape != 0) return "start is not the start piece";
    int endLinks = 0; foreach (var l in r.links) if (l.a == r.endRoom || l.b == r.endRoom) endLinks++;
    if (endLinks != 1) return "end room has " + endLinks + " doors";
    int maxDepth = 0; foreach (var room in r.rooms) maxDepth = System.Math.Max(maxDepth, room.depth);
    if (r.rooms[r.endRoom].depth != maxDepth) return "end room not deepest";
    return "";
}
int ok = 0, tried = 0, attemptsMax = 0; string firstFail = "";
for (int seed = 1; seed <= 200; seed++)
{
    tried++;
    string e = Check(gen.roomCount, seed);
    if (e == "") ok++; else if (firstFail == "") firstFail = "count " + gen.roomCount + " seed " + seed + ": " + e;
}
log += (ok == tried ? "ok   " : "FAIL ") + "room count " + gen.roomCount + ": " + ok + "/" + tried + " seeds finishable\n";
if (ok != tried) fails++;
for (int count = 2; count <= 12; count++)
{
    int good = 0;
    for (int seed = 1; seed <= 50; seed++)
    {
        var r = new TopDownKit.Layout.Result();
        string e = Check(count, seed * 7919 + count);
        if (e == "") good++; else if (firstFail == "") firstFail = "count " + count + " seed " + (seed * 7919 + count) + ": " + e;
    }
    log += (good == 50 ? "ok   " : "FAIL ") + "room count " + count + ": " + good + "/50\n";
    if (good != 50) fails++;
}
// Same seed, same layout.
var a1 = new TopDownKit.Layout.Result(); var a2 = new TopDownKit.Layout.Result();
TopDownKit.Layout.Generate(shapes, 0, 1, allowed, gen.roomCount, 4242, a1);
TopDownKit.Layout.Generate(shapes, 0, 1, allowed, gen.roomCount, 4242, a2);
bool same = a1.rooms.Count == a2.rooms.Count;
for (int i = 0; same && i < a1.rooms.Count; i++) same = a1.rooms[i].origin == a2.rooms[i].origin && a1.rooms[i].shape == a2.rooms[i].shape;
log += (same ? "ok   " : "FAIL ") + "seed 4242 twice gives the same layout\n";
if (!same) fails++;
if (firstFail != "") log += "first failure: " + firstFail + "\n";
return (fails == 0 ? "ALL PASS\n" : fails + " FAILED\n") + log;

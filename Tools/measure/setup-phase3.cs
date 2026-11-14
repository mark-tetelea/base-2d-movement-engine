// One-off setup for phase 3 (run in EDIT mode with TestArena open):
//     unity command eval_file --file Tools/measure/setup-phase3.cs
// Grows the arena into a few rooms: A (the old arena), B to its right (screen size, doors
// lock until cleared), C above both (80 x 30, bigger than the screen), and a detached
// area D reached by a warp pad in C. Adds the CameraRig and the camera test keys.
// Safe to re-run: paints the same cells and updates what is already there.
// Second call (after this one): toggle colliders - see the end.
if (EditorApplication.isPlaying) return "exit play mode first";
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (!scene.path.EndsWith("TestArena.unity")) return "open TestArena first, not " + scene.path;
var walls = GameObject.Find("Walls").GetComponent<UnityEngine.Tilemaps.Tilemap>();
var floor = GameObject.Find("Floor").GetComponent<UnityEngine.Tilemaps.Tilemap>();
var wallTile = walls.GetTile(new Vector3Int(-20, -11, 0));
var floorTile = floor.GetTile(new Vector3Int(0, 0, 0));
if (wallTile == null || floorTile == null) return "tiles not found";
string log = "";

// Cells [x0, x1) x [y0, y1): wall ring, floor inside.
void Box(int x0, int y0, int x1, int y1)
{
    for (int x = x0; x < x1; x++)
        for (int y = y0; y < y1; y++)
        {
            var c = new Vector3Int(x, y, 0);
            bool edge = x == x0 || x == x1 - 1 || y == y0 || y == y1 - 1;
            if (edge) { walls.SetTile(c, wallTile); floor.SetTile(c, null); }
            else { floor.SetTile(c, floorTile); }
        }
}
void Open(int x0, int y0, int x1, int y1)
{
    for (int x = x0; x < x1; x++)
        for (int y = y0; y < y1; y++)
        {
            var c = new Vector3Int(x, y, 0);
            walls.SetTile(c, null);
            floor.SetTile(c, floorTile);
        }
}
void Block(int x0, int y0, int w, int h)
{
    for (int x = x0; x < x0 + w; x++)
        for (int y = y0; y < y0 + h; y++) walls.SetTile(new Vector3Int(x, y, 0), wallTile);
}

Box(20, -11, 60, 11);    // B
Box(-20, 11, 60, 41);    // C
Box(100, -6, 120, 6);    // D
Open(19, 6, 21, 9);      // A <-> B, three tiles high
Open(12, 10, 15, 12);    // A <-> C
Open(38, 10, 41, 12);    // B <-> C
// Something to walk around in C and B.
Block(0, 20, 2, 2); Block(10, 30, 2, 2); Block(30, 22, 4, 1); Block(-10, 34, 1, 4); Block(40, -4, 2, 2);

// Rooms.
var roomsRoot = GameObject.Find("Rooms");
if (roomsRoot == null) roomsRoot = new GameObject("Rooms");
TopDownKit.Room Room(string name, float cx, float cy, float w, float h)
{
    var t = roomsRoot.transform.Find(name);
    var go = t != null ? t.gameObject : new GameObject(name);
    go.transform.SetParent(roomsRoot.transform, false);
    go.transform.position = new Vector3(cx, cy, 0f);
    var r = go.GetComponent<TopDownKit.Room>();
    if (r == null) r = go.AddComponent<TopDownKit.Room>();
    r.size = new Vector2(w, h);
    EditorUtility.SetDirty(r);
    return r;
}
Room("Room A", 0f, 0f, 40f, 22f);
var b = Room("Room B", 40f, 0f, 40f, 22f);
var c = Room("Room C", 20f, 26f, 80f, 30f);
Room("Area D", 110f, 0f, 20f, 12f);

var square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/white-square.png");
var player = GameObject.Find("Player");
int playerOrder = player.GetComponentInChildren<SpriteRenderer>().sortingOrder;
Color Hex(string h, float a) { ColorUtility.TryParseHtmlString(h, out var col); col.a = a; return col; }

GameObject Thing(Transform parent, string name, float cx, float cy, float w, float h, bool trigger, Color color)
{
    var t = parent.Find(name);
    var go = t != null ? t.gameObject : new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.position = new Vector3(cx, cy, 0f);
    var box = go.GetComponent<BoxCollider2D>();
    if (box == null) box = go.AddComponent<BoxCollider2D>();
    box.isTrigger = trigger;
    box.size = new Vector2(w, h);
    var art = go.transform.Find("Art");
    var artGo = art != null ? art.gameObject : new GameObject("Art");
    artGo.transform.SetParent(go.transform, false);
    artGo.transform.localScale = new Vector3(w, h, 1f);
    var sr = artGo.GetComponent<SpriteRenderer>();
    if (sr == null) sr = artGo.AddComponent<SpriteRenderer>();
    sr.sprite = square;
    sr.color = color;
    sr.sortingOrder = trigger ? playerOrder - 1 : playerOrder + 1;
    return go;
}

// B's lock: two door blockers sitting in B's own wall cells.
var lockB = b.GetComponent<TopDownKit.RoomLock>();
if (lockB == null) lockB = b.gameObject.AddComponent<TopDownKit.RoomLock>();
lockB.player = player.transform;
Color doorCol = Hex("#b55088", 1f);
lockB.doors = new[] {
    Thing(b.transform, "Door west", 20.5f, 7.5f, 1f, 3f, false, doorCol),
    Thing(b.transform, "Door north", 39.5f, 10.5f, 3f, 1f, false, doorCol) };
EditorUtility.SetDirty(lockB);

// Warp pads C <-> D, with arrival points beside them.
var warpRoot = GameObject.Find("Warps");
if (warpRoot == null) warpRoot = new GameObject("Warps");
Transform Point(string name, float x, float y)
{
    var t = warpRoot.transform.Find(name);
    var go = t != null ? t.gameObject : new GameObject(name);
    go.transform.SetParent(warpRoot.transform, false);
    go.transform.position = new Vector3(x, y, 0f);
    return go.transform;
}
Color warpCol = Hex("#b86f50", 0.8f);
var toD = Thing(warpRoot.transform, "Warp C to D", 50f, 35f, 2f, 2f, true, warpCol);
var toC = Thing(warpRoot.transform, "Warp D to C", 116f, 0f, 2f, 2f, true, warpCol);
var w1 = toD.GetComponent<TopDownKit.Warp>(); if (w1 == null) w1 = toD.AddComponent<TopDownKit.Warp>();
var w2 = toC.GetComponent<TopDownKit.Warp>(); if (w2 == null) w2 = toC.AddComponent<TopDownKit.Warp>();
w1.destination = Point("Arrive in D", 104f, 0f);
w2.destination = Point("Arrive in C", 46f, 35f);
EditorUtility.SetDirty(w1); EditorUtility.SetDirty(w2);

// Camera rig and test keys.
var cam = GameObject.Find("Main Camera");
var rig = cam.GetComponent<TopDownKit.CameraRig>();
if (rig == null) rig = cam.AddComponent<TopDownKit.CameraRig>();
rig.target = player.transform;
rig.aim = player.GetComponent<TopDownKit.PlayerAim>();
rig.mover = player.GetComponent<TopDownKit.PlayerMover>();
EditorUtility.SetDirty(rig);
var keysGo = GameObject.Find("Feel Keys (test arena)");
var ck = keysGo.GetComponent<TopDownKit.TestArena.CameraKeys>();
if (ck == null) ck = keysGo.AddComponent<TopDownKit.TestArena.CameraKeys>();
ck.rig = rig;
EditorUtility.SetDirty(ck);

// Trap 74: toggle the wall collider so it rebuilds; read the shapes on the next call.
var tc = walls.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
tc.enabled = false; tc.enabled = true;
walls.CompressBounds(); floor.CompressBounds();
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return log + "walls " + walls.cellBounds + " aim " + (rig.aim != null) + "; scene saved";

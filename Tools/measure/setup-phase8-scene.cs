// One-off setup for phase 8, part 2 (EDIT mode, Playground open, after setup-phase8-assets.cs):
//     unity command eval_file --file Tools/measure/setup-phase8-scene.cs
// Puts every ground tile in the Playground: a tile garden in the lower left of room C (a red
// pool, a blue strip, a crumbling bridge over a red moat, a door in a short wall opened by a
// lever, a door opened with the chest's key), and room B's north door becomes door ground
// that opens when B is cleared. Adds the GroundMap to the floor and the Grid step row.
// Safe to re-run. Then (trap 74) a second call reads the wall collider back - see the end.
if (EditorApplication.isPlaying) return "exit play mode first";
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
if (!scene.path.EndsWith("Playground.unity")) return "open Playground first, not " + scene.path;
string log = "";
T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }
var red = Load<TopDownKit.GroundTile>("Assets/Data/Tiles/Red.asset");
var blue = Load<TopDownKit.GroundTile>("Assets/Data/Tiles/Blue.asset");
var crumble = Load<TopDownKit.GroundTile>("Assets/Data/Tiles/Crumbling.asset");
var door = Load<TopDownKit.GroundTile>("Assets/Data/Tiles/Door.asset");
var key = Load<TopDownKit.Item>("Assets/Data/Items/Key.asset");
var leverSprite = Load<Sprite>("Assets/Art/Generated/lever.png");
var grids = new[] { Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Grid.asset"),
    Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Grid 8.asset"), Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Grid tap.asset") };

var walls = GameObject.Find("Walls").GetComponent<UnityEngine.Tilemaps.Tilemap>();
var floor = GameObject.Find("Floor").GetComponent<UnityEngine.Tilemaps.Tilemap>();
var wallTile = walls.GetTile(new Vector3Int(-20, -11, 0));
var floorTile = floor.GetTile(new Vector3Int(0, 0, 0));
if (wallTile == null || floorTile == null) return "tiles not found";
Get<TopDownKit.GroundMap>(floor.gameObject);

void Paint(UnityEngine.Tilemaps.TileBase t, int x0, int y0, int w, int h)
{
    for (int x = x0; x < x0 + w; x++)
        for (int y = y0; y < y0 + h; y++)
        {
            var c = new Vector3Int(x, y, 0);
            if (walls.HasTile(c)) throw new System.Exception("wall at " + c);
            floor.SetTile(c, t);
        }
}
void Wall(int x0, int y0, int w, int h)
{
    for (int x = x0; x < x0 + w; x++)
        for (int y = y0; y < y0 + h; y++) { var c = new Vector3Int(x, y, 0); walls.SetTile(c, wallTile); floor.SetTile(c, null); }
}

// Room C's floor is x -19..58, y 12..39; the way in from A is x 12..14.
Paint(red, -18, 13, 3, 3);              // red pool
Paint(blue, -13, 12, 2, 6);             // blue strip: walk round it, shoot over it
Paint(red, -8, 12, 3, 8);               // red moat ...
Paint(crumble, -8, 15, 3, 1);           // ... with a crumbling bridge across
Wall(-4, 17, 1, 1); Wall(0, 17, 1, 1); Paint(door, -3, 17, 3, 1);   // lever door in a short wall
Wall(2, 17, 1, 1); Wall(6, 17, 1, 1); Paint(door, 3, 17, 3, 1);    // key door in a short wall
// Room B's north door (cells 38..40, 10) becomes door ground.
Paint(door, 38, 10, 3, 1);

var rootT = GameObject.Find("Playground").transform;
GameObject Thing(string name, Transform parent, float x, float y)
{
    var t = parent.Find(name);
    var go = t != null ? t.gameObject : new GameObject(name);
    go.transform.SetParent(parent, false);
    go.transform.position = new Vector3(x, y, 0f);
    return go;
}
TopDownKit.TileDoor TD(GameObject g, int w, int h) { var d = Get<TopDownKit.TileDoor>(g); d.size = new Vector2Int(w, h); EditorUtility.SetDirty(d); return d; }

var garden = Thing("Tile garden", rootT, 0f, 0f);
var leverDoor = Thing("Lever door ground", garden.transform, -2.5f, 17.5f); TD(leverDoor, 3, 1);
var keyDoor = Thing("Key door ground", garden.transform, 3.5f, 17.5f); TD(keyDoor, 3, 1);
leverDoor.SetActive(true); keyDoor.SetActive(true);

int order = GameObject.Find("Player").GetComponentInChildren<SpriteRenderer>().sortingOrder;
var leverGo = Thing("Lever", garden.transform, 1.5f, 15f);
var lsr = Get<SpriteRenderer>(leverGo); lsr.sprite = leverSprite; lsr.sortingOrder = order;
var lbox = Get<BoxCollider2D>(leverGo); lbox.size = new Vector2(0.75f, 0.4f); lbox.offset = new Vector2(0f, 0.25f);
var lever = Get<TopDownKit.Lever>(leverGo);
lever.verb = "Pull"; lever.art = lsr; lever.offWhenOn = new[] { leverDoor }; lever.onWhenOn = new GameObject[0];
lever.centre = new Vector2(0f, 0.4f); lever.on = false;
EditorUtility.SetDirty(lever);

var lockGo = Thing("Key door lock", garden.transform, 4.5f, 16f);
var ld = Get<TopDownKit.LockedDoor>(lockGo);
ld.verb = "Open"; ld.key = key; ld.consumeKey = true; ld.blockers = new[] { keyDoor };
ld.centre = new Vector2(0f, 0.5f); ld.promptOffset = new Vector2(0f, 2.2f);
ld.lockedText = "Locked. The key is in the chest in the first room.";
EditorUtility.SetDirty(ld);

// Room B: swap the north door blocker for door ground.
var roomB = GameObject.Find("Room B");
var lockB = roomB.GetComponent<TopDownKit.RoomLock>();
var oldNorth = roomB.transform.Find("Door north");
var north = Thing("Door north (ground)", roomB.transform, 38.5f, 10.5f); TD(north, 3, 1);
for (int i = 0; i < lockB.doors.Length; i++)
    if (lockB.doors[i] == null || (oldNorth != null && lockB.doors[i] == oldNorth.gameObject)) lockB.doors[i] = north;
if (System.Array.IndexOf(lockB.doors, north) < 0) { var l = new System.Collections.Generic.List<GameObject>(lockB.doors) { north }; lockB.doors = l.ToArray(); }
if (oldNorth != null) { UnityEngine.Object.DestroyImmediate(oldNorth.gameObject); log += "replaced Door north; "; }
EditorUtility.SetDirty(lockB);

var picker = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Playground.Picker>();
picker.gridFeels = grids;
EditorUtility.SetDirty(picker);

var tc = walls.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
tc.enabled = false; tc.enabled = true;
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
string doors = ""; foreach (var d in lockB.doors) doors += (d != null ? d.name : "null") + ", ";
return log + "B doors: " + doors + "saved";

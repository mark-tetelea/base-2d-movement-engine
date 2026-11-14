// One-off setup for phase 10 (EDIT mode, after the pxkit PNGs are imported):
//     unity command eval_file --file Tools/measure/setup-phase10.cs
// Assets: boss weapons (Bouncer, Charge, Sword kinds), BossRush and EnemyGrid feels, the
// Trail ground tile; PathWalker on the Chaser and Shooter prefabs; new prefabs Boss,
// Leader, Trailer, Stepper. ArenaShooter: room E east of B (walls, pillars, a door from B)
// holding the boss. Playground: a group in C, a trail enemy in C, a grid-step chaser in D.
// Safe to re-run: assets are updated, scene objects rebuilt.
if (EditorApplication.isPlaying) return "exit play mode first";
string log = "";
T Asset<T>(string path) where T : ScriptableObject
{
    var a = AssetDatabase.LoadAssetAtPath<T>(path);
    if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); log += "made " + path + "; "; }
    return a;
}
T Load<T>(string p) where T : UnityEngine.Object { var a = AssetDatabase.LoadAssetAtPath<T>(p); if (a == null) throw new System.Exception("missing " + p); return a; }
Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
C Get<C>(GameObject g) where C : Component { var c = g.GetComponent<C>(); if (c == null) c = g.AddComponent<C>(); return c; }

// ---- Assets -------------------------------------------------------------------------
var bullet = Load<Sprite>("Assets/Art/Generated/bullet.png");
var bouncer = Asset<TopDownKit.Weapon>("Assets/Data/Weapons/BossBouncer.asset");
bouncer.kind = TopDownKit.WeaponKind.Gun; bouncer.fireRate = 0.9f; bouncer.automatic = true; bouncer.bulletsPerShot = 3; bouncer.spread = 30f;
bouncer.bulletSpeed = 7f; bouncer.range = 22f; bouncer.damage = 1; bouncer.knockback = 5f; bouncer.recoil = 0f; bouncer.magazine = 0;
bouncer.bulletRadius = 0.2f; bouncer.bounces = 2; bouncer.shakeOnShot = 0f; bouncer.bulletSprite = bullet; bouncer.bulletColor = Hex("#b55088");
bouncer.chargeTime = 0f;
var charge = Asset<TopDownKit.Weapon>("Assets/Data/Weapons/BossCharge.asset");
charge.kind = TopDownKit.WeaponKind.Gun; charge.fireRate = 0.8f; charge.automatic = false; charge.bulletsPerShot = 1; charge.spread = 0f;
charge.bulletSpeed = 6f; charge.range = 20f; charge.damage = 1; charge.knockback = 8f; charge.recoil = 0f; charge.magazine = 0;
charge.bulletRadius = 0.2f; charge.bounces = 0; charge.bulletSprite = bullet; charge.bulletColor = Hex("#f77622");
charge.chargeTime = 0.9f; charge.chargeDamage = 2f; charge.chargeSize = 2.5f; charge.chargeSpeed = 1.5f;
var sword = Asset<TopDownKit.Weapon>("Assets/Data/Weapons/BossSword.asset");
sword.kind = TopDownKit.WeaponKind.Melee; sword.damage = 1; sword.knockback = 12f; sword.magazine = 0; sword.chargeTime = 0f;
sword.melee = new TopDownKit.MeleeSettings { arc = 160f, reach = 2.2f, swingTime = 0.3f, cooldown = 0.6f, width = 0.25f, color = Hex("#feae34") };
foreach (var w in new[] { bouncer, charge, sword }) EditorUtility.SetDirty(w);

var enemyWalk = Load<TopDownKit.MoveFeel>("Assets/Data/Movement/EnemyWalk.asset");
var rush = Asset<TopDownKit.MoveFeel>("Assets/Data/Movement/BossRush.asset");
rush.settings = enemyWalk.settings; rush.settings.speed = 4.2f;
EditorUtility.SetDirty(rush);
var gridFeel = Load<TopDownKit.MoveFeel>("Assets/Data/Movement/Grid.asset");
var enemyGrid = Asset<TopDownKit.MoveFeel>("Assets/Data/Movement/EnemyGrid.asset");
enemyGrid.settings = gridFeel.settings; enemyGrid.settings.speed = 3f; enemyGrid.settings.holdToWalk = true;
EditorUtility.SetDirty(enemyGrid);

var trail = Asset<TopDownKit.GroundTile>("Assets/Data/Tiles/Trail.asset");
trail.sprite = Load<Sprite>("Assets/Art/Generated/ground-trail.png");
trail.color = Color.white;
trail.rules = new TopDownKit.GroundRules { damagePerSecond = 1 };
EditorUtility.SetDirty(trail);
AssetDatabase.SaveAssets();

// PathWalker on the existing walkers that chase.
foreach (var name in new[] { "Chaser", "Shooter" })
{
    string p = "Assets/Prefabs/Enemies/" + name + ".prefab";
    var root = PrefabUtility.LoadPrefabContents(p);
    Get<TopDownKit.PathWalker>(root);
    PrefabUtility.SaveAsPrefabAsset(root, p);
    PrefabUtility.UnloadPrefabContents(root);
}
log += "PathWalker on Chaser, Shooter; ";

var noFriction = Load<PhysicsMaterial2D>("Assets/Data/Movement/NoFriction.physicsMaterial2D");
var enemyShot = Load<TopDownKit.Weapon>("Assets/Data/Weapons/EnemyShot.asset");
GameObject Enemy(string name, string sprite, TopDownKit.EnemyBehaviour b, int hp, float radius, TopDownKit.MoveFeel feel)
{
    var go = new GameObject(name);
    var rb = go.AddComponent<Rigidbody2D>();
    rb.gravityScale = 0f; rb.interpolation = RigidbodyInterpolation2D.Interpolate; rb.freezeRotation = true;
    var col = go.AddComponent<CircleCollider2D>();
    col.radius = radius; col.sharedMaterial = noFriction;
    var mover = go.AddComponent<TopDownKit.PlayerMover>();
    mover.feel = feel; mover.readKeyboard = false;
    var h = go.AddComponent<TopDownKit.Health>();
    h.maxHealth = hp; h.team = 1; h.invulnerableTime = 0f; h.flashTime = 0.08f; h.blinkWhileInvulnerable = false; h.destroyOnDeath = true;
    var brain = go.AddComponent<TopDownKit.EnemyBrain>();
    brain.behaviour = b;
    go.AddComponent<TopDownKit.PathWalker>();
    var art = new GameObject("Art");
    art.transform.SetParent(go.transform, false);
    var sr = art.AddComponent<SpriteRenderer>();
    sr.sprite = Load<Sprite>("Assets/Art/Generated/" + sprite + ".png");
    sr.sortingOrder = 2;
    return go;
}
GameObject Save(GameObject go)
{
    var prefab = PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemies/" + go.name + ".prefab");
    UnityEngine.Object.DestroyImmediate(go);
    return prefab;
}

var bossGo = Enemy("Boss", "boss", TopDownKit.EnemyBehaviour.KeepDistanceAndShoot, 40, 0.9f, enemyWalk);
{
    var g = bossGo.AddComponent<TopDownKit.Gun>(); g.weapons = new[] { bouncer }; g.team = 1; g.muzzleDistance = 1.1f;
    var brain = bossGo.GetComponent<TopDownKit.EnemyBrain>();
    brain.sightRange = 40f; brain.keepDistance = 7f; brain.touchRange = 1.4f; brain.touchDamage = 1; brain.touchKnockback = 12f;
    var bp = bossGo.AddComponent<TopDownKit.BossPhases>();
    bp.bossName = "Horned blob"; bp.switchPause = 0.6f;
    bp.phases = new[]
    {
        new TopDownKit.BossPhase { name = "", fromHealth = 1f, behaviour = TopDownKit.EnemyBehaviour.KeepDistanceAndShoot, weapon = bouncer, feel = enemyWalk, keepDistance = 7f },
        new TopDownKit.BossPhase { name = "charging", fromHealth = 0.6f, behaviour = TopDownKit.EnemyBehaviour.KeepDistanceAndShoot, weapon = charge, feel = enemyWalk, keepDistance = 8f },
        new TopDownKit.BossPhase { name = "enraged", fromHealth = 0.3f, behaviour = TopDownKit.EnemyBehaviour.Chase, weapon = sword, feel = rush, keepDistance = 0f },
    };
}
var bossPrefab = Save(bossGo);

var leaderGo = Enemy("Leader", "enemy-leader", TopDownKit.EnemyBehaviour.Wander, 4, 0.4f, enemyWalk);
var grp = leaderGo.AddComponent<TopDownKit.EnemyGroup>();
grp.shape = TopDownKit.GroupShape.Wedge; grp.spacing = 1.5f; grp.breakRange = 4f; grp.leaderPace = 0.8f;
var leaderPrefab = Save(leaderGo);

var trailerGo = Enemy("Trailer", "enemy-rust", TopDownKit.EnemyBehaviour.Wander, 3, 0.4f, enemyWalk);
var tm = trailerGo.AddComponent<TopDownKit.TrailMaker>(); tm.tile = trail; tm.lastFor = 3f;
trailerGo.GetComponent<TopDownKit.EnemyBrain>().turnEvery = 1.5f;
var trailerPrefab = Save(trailerGo);

var stepperGo = Enemy("Stepper", "enemy-fire", TopDownKit.EnemyBehaviour.Chase, 3, 0.4f, enemyGrid);
stepperGo.GetComponent<TopDownKit.EnemyBrain>().touchRange = 1.5f;   // grid steps stop on the next tile (diagonal 1.41)
var stepperPrefab = Save(stepperGo);
AssetDatabase.SaveAssets();
log += "prefabs Boss, Leader, Trailer, Stepper; ";

// ---- Scene helpers --------------------------------------------------------------------
GameObject Spawn(string prefabPath, Transform parent, float x, float y, Transform target, int seed)
{
    var e = (GameObject)PrefabUtility.InstantiatePrefab(Load<GameObject>(prefabPath), parent);
    e.transform.position = new Vector3(x, y, 0f);
    var brain = e.GetComponent<TopDownKit.EnemyBrain>();
    brain.target = target; brain.seed = seed;
    PrefabUtility.RecordPrefabInstancePropertyModifications(brain);
    return e;
}
void Clear(Transform parent, string prefix) { for (int i = parent.childCount - 1; i >= 0; i--) if (parent.GetChild(i).name.StartsWith(prefix)) UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject); }

// ---- ArenaShooter: room E with the boss ------------------------------------------------
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/ArenaShooter.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
    var square = Load<Sprite>("Assets/Art/Placeholders/white-square.png");
    var player = GameObject.Find("Player");
    var walls = GameObject.Find("Walls").GetComponent<UnityEngine.Tilemaps.Tilemap>();
    var floor = GameObject.Find("Floor").GetComponent<UnityEngine.Tilemaps.Tilemap>();
    var wallTile = walls.GetTile(new Vector3Int(-20, 0, 0));
    var floorTile = floor.GetTile(new Vector3Int(0, 0, 0));
    void W(int x, int y) => walls.SetTile(new Vector3Int(x, y, 0), wallTile);
    // E: x 60..98, y -10..9 inside; walls at y 10 and -11, x 99; the door gap in B's east wall.
    for (int x = 59; x <= 99; x++) { W(x, 10); W(x, -11); }
    for (int y = -11; y <= 10; y++) W(99, y);
    for (int x = 60; x <= 98; x++) for (int y = -10; y <= 9; y++) { floor.SetTile(new Vector3Int(x, y, 0), floorTile); walls.SetTile(new Vector3Int(x, y, 0), null); }
    for (int y = -1; y <= 1; y++) { walls.SetTile(new Vector3Int(59, y, 0), null); floor.SetTile(new Vector3Int(59, y, 0), floorTile); }
    void Block(int x0, int y0, int x1, int y1) { for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) W(x, y); }
    Block(66, 4, 67, 5); Block(66, -6, 67, -5);   // pillars by the door
    Block(76, -4, 77, 4);                         // the wall in the middle
    Block(86, 5, 87, 6); Block(86, -7, 87, -6);   // pillars at the far end
    var wc = walls.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
    wc.enabled = false; wc.enabled = true;        // trap 74
    wc.ProcessTilemapChanges();
    walls.GetComponent<CompositeCollider2D>().GenerateGeometry();

    GameObject Door(Transform parent, string name)
    {
        var t = parent.Find(name);
        var go = t != null ? t.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(59.5f, 0.5f, 0f);
        Get<BoxCollider2D>(go).size = new Vector2(1f, 3f);
        var art = go.transform.Find("Art");
        var artGo = art != null ? art.gameObject : new GameObject("Art");
        artGo.transform.SetParent(go.transform, false);
        artGo.transform.localScale = new Vector3(1f, 3f, 1f);
        var sr = Get<SpriteRenderer>(artGo);
        sr.sprite = square; sr.color = Hex("#b55088"); sr.sortingOrder = 3;
        return go;
    }
    var rooms = GameObject.Find("Rooms").transform;
    var roomB = GameObject.Find("Room B");
    var lockB = roomB.GetComponent<TopDownKit.RoomLock>();
    var doorB = Door(roomB.transform, "Door east");
    var doorsB = new System.Collections.Generic.List<GameObject>(lockB.doors ?? new GameObject[0]);
    doorsB.RemoveAll(d => d == null);
    if (!doorsB.Contains(doorB)) doorsB.Add(doorB);
    lockB.doors = doorsB.ToArray();
    EditorUtility.SetDirty(lockB);

    var eT = rooms.Find("Room E");
    var roomE = eT != null ? eT.gameObject : new GameObject("Room E");
    roomE.transform.SetParent(rooms, false);
    roomE.transform.position = new Vector3(80f, 0f, 0f);
    Get<TopDownKit.Room>(roomE).size = new Vector2(40f, 22f);
    var lockE = Get<TopDownKit.RoomLock>(roomE);
    lockE.player = player.transform;
    lockE.doors = new[] { Door(roomE.transform, "Door west") };
    var wavesE = Get<TopDownKit.RoomWaves>(roomE);
    Clear(roomE.transform, "Wave ");
    var wave = new GameObject("Wave 1");
    wave.transform.SetParent(roomE.transform, false);
    wave.transform.position = Vector3.zero;
    var boss = Spawn("Assets/Prefabs/Enemies/Boss.prefab", wave.transform, 92f, 0.5f, player.transform, 101);
    var gun = boss.GetComponent<TopDownKit.Gun>(); gun.seed = 102; PrefabUtility.RecordPrefabInstancePropertyModifications(gun);
    var bp = boss.GetComponent<TopDownKit.BossPhases>();
    bp.hud = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Hud>();
    PrefabUtility.RecordPrefabInstancePropertyModifications(bp);
    wave.SetActive(false);
    wavesE.waves = new[] { wave };
    EditorUtility.SetDirty(lockE); EditorUtility.SetDirty(wavesE);
    log += "Arena: room E, boss hud " + (bp.hud != null) + ", B doors " + lockB.doors.Length + "; ";
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}

// ---- Playground: a group and a trail enemy in C, a grid-step chaser in D ----------------
{
    var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Playground.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
    var player = GameObject.Find("Player").transform;
    var roomC = GameObject.Find("Room C").transform;
    var areaD = GameObject.Find("Area D").transform;
    foreach (var p in new[] { "Leader", "Chaser", "Trailer" }) Clear(roomC, p);
    Clear(areaD, "Stepper");
    var leader = Spawn("Assets/Prefabs/Enemies/Leader.prefab", roomC, 40f, 32f, player, 201);
    var followers = new TopDownKit.EnemyBrain[3];
    var spots = new[] { new Vector2(38.5f, 33.5f), new Vector2(38.5f, 30.5f), new Vector2(37f, 35f) };
    for (int i = 0; i < 3; i++)
        followers[i] = Spawn("Assets/Prefabs/Enemies/Chaser.prefab", roomC, spots[i].x, spots[i].y, player, 202 + i).GetComponent<TopDownKit.EnemyBrain>();
    var g = leader.GetComponent<TopDownKit.EnemyGroup>();
    g.followers = followers;
    PrefabUtility.RecordPrefabInstancePropertyModifications(g);
    Spawn("Assets/Prefabs/Enemies/Trailer.prefab", roomC, 50f, 17f, player, 210);
    Spawn("Assets/Prefabs/Enemies/Stepper.prefab", areaD, 116.5f, 0.5f, player, 220);
    log += "Playground: group of 4, trailer, stepper; player restarts on death " + (player.GetComponent<TopDownKit.RestartOnDeath>() != null) + "; ";
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
}
return log;

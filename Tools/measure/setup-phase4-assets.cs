// One-off setup for phase 4, part 1 (EDIT mode, after the PNGs from pxkit were imported):
//     unity command eval_file --file Tools/measure/setup-phase4-assets.cs
// Makes the weapon assets, the enemy feel and the three enemy prefabs. Safe to re-run:
// existing assets are updated, not duplicated. Part 2 (setup-phase4-scene.cs) builds the scene.
if (EditorApplication.isPlaying) return "exit play mode first";
string log = "";
T Asset<T>(string path) where T : ScriptableObject
{
    var a = AssetDatabase.LoadAssetAtPath<T>(path);
    if (a == null) { a = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(a, path); log += "made " + path + "; "; }
    return a;
}
void Folder(string parent, string name) { if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name); }
Folder("Assets/Data", "Weapons");
Folder("Assets", "Prefabs");
Folder("Assets/Prefabs", "Enemies");
Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

var bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/bullet.png");
if (bulletSprite == null) return "bullet.png not imported yet";

TopDownKit.Weapon W(string name, float rate, bool auto, int count, float spread, float speed, float range, int dmg, float kb, float recoil, int mag, float reload, float shake, string col)
{
    var w = Asset<TopDownKit.Weapon>("Assets/Data/Weapons/" + name + ".asset");
    w.fireRate = rate; w.automatic = auto; w.bulletsPerShot = count; w.spread = spread; w.bulletSpeed = speed;
    w.range = range; w.damage = dmg; w.knockback = kb; w.recoil = recoil; w.magazine = mag; w.reloadTime = reload;
    w.bulletRadius = 0.15f; w.shakeOnShot = shake; w.bulletSprite = bulletSprite; w.bulletColor = Hex(col);
    EditorUtility.SetDirty(w);
    return w;
}
//          name        rate  auto  n  spread speed range dmg kb   recoil mag reload shake colour
W("Pistol",     5f,  false, 1,  2f, 18f, 14f, 1,  6f,  1f,  12, 0.9f, 0f,    "#feae34");
W("Shotgun",    1.4f, false, 6, 30f, 15f,  7f, 1,  3f,  8f,   4, 1.2f, 0.25f, "#fee761");
W("Rapid",     12f,  true,  1,  8f, 20f, 12f, 1,  2f,  0.6f, 30, 1.5f, 0f,    "#2ce8f5");
var enemyShot = W("EnemyShot", 0.8f, true, 1, 0f, 7f, 14f, 1, 6f, 0f, 0, 0f, 0f, "#f77622");

var feel = Asset<TopDownKit.MoveFeel>("Assets/Data/Movement/EnemyWalk.asset");
var glide = AssetDatabase.LoadAssetAtPath<TopDownKit.MoveFeel>("Assets/Data/Movement/Glide.asset");
feel.settings.speed = 3f; feel.settings.timeToFullSpeed = 0.2f; feel.settings.timeToStop = 0.2f;
feel.settings.knockbackStopRate = glide.settings.knockbackStopRate;
EditorUtility.SetDirty(feel);
var noFriction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Data/Movement/NoFriction.physicsMaterial2D");
int order = 10;
var player = GameObject.Find("Player");
if (player != null) order = player.GetComponentInChildren<SpriteRenderer>().sortingOrder;

void Enemy(string name, string sprite, TopDownKit.EnemyBehaviour b, int hp, float speed, bool gun)
{
    var go = new GameObject(name);
    var rb = go.AddComponent<Rigidbody2D>();
    rb.gravityScale = 0f; rb.interpolation = RigidbodyInterpolation2D.Interpolate; rb.freezeRotation = true;
    var col = go.AddComponent<CircleCollider2D>();
    col.radius = 0.4f; col.sharedMaterial = noFriction;
    var mover = go.AddComponent<TopDownKit.PlayerMover>();
    mover.feel = feel; mover.readKeyboard = false;
    var h = go.AddComponent<TopDownKit.Health>();
    h.maxHealth = hp; h.team = 1; h.invulnerableTime = 0f; h.flashTime = 0.08f; h.blinkWhileInvulnerable = false; h.destroyOnDeath = true;
    if (gun) { var g = go.AddComponent<TopDownKit.Gun>(); g.weapons = new[] { enemyShot }; g.team = 1; }
    var brain = go.AddComponent<TopDownKit.EnemyBrain>();
    brain.behaviour = b;
    var art = new GameObject("Art");
    art.transform.SetParent(go.transform, false);
    var sr = art.AddComponent<SpriteRenderer>();
    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/" + sprite + ".png");
    sr.sortingOrder = order;
    log += name + " sprite " + (sr.sprite != null) + "; ";
    PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Enemies/" + name + ".prefab");
    UnityEngine.Object.DestroyImmediate(go);
}
Enemy("Chaser", "enemy-fire", TopDownKit.EnemyBehaviour.Chase, 3, 3f, false);
Enemy("Shooter", "enemy", TopDownKit.EnemyBehaviour.KeepDistanceAndShoot, 3, 3f, true);
Enemy("Wanderer", "enemy-green", TopDownKit.EnemyBehaviour.Wander, 2, 3f, false);
AssetDatabase.SaveAssets();
return log;

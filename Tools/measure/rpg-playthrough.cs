// Little RPG room playthrough. Run IN PLAY MODE on Assets/Scenes/LittleRpg.unity:
//     unity command eval_file --file Tools/measure/rpg-playthrough.cs
// poll:  unity command eval --code 'return System.AppDomain.CurrentDomain.GetData("rpg_state") as string;'
// A bot plays the demo through the real entry points (walk direction, the interact key as
// data, the chest screen's TakeAll - the mouse path is NOT driven, trap 39): talks to the
// Guide, picks up coins, tries the locked door, fetches the key from the chest in C, opens
// the door, takes the crown in B. Then drop and take back, a Hold-mode lever, and a heart
// and an ammo pickup feeding phase 4's Health and Gun.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on LittleRpg first";
if (System.AppDomain.CurrentDomain.GetData("rpg_state") as string == "running") return "already running";
var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var it = player.GetComponent<TopDownKit.Interactor>();
var hotbar = player.GetComponent<TopDownKit.Hotbar>();
var inv = player.GetComponent<TopDownKit.Bag>().Inventory;
var box = TopDownKit.TextBox.Current;
var screen = TopDownKit.ChestScreen.Current;
var hud = UnityEngine.Object.FindFirstObjectByType<TopDownKit.RpgGoal>();
var coin = AssetDatabase.LoadAssetAtPath<TopDownKit.Item>("Assets/Data/Items/Coin.asset");
var key = AssetDatabase.LoadAssetAtPath<TopDownKit.Item>("Assets/Data/Items/Key.asset");
var crown = AssetDatabase.LoadAssetAtPath<TopDownKit.Item>("Assets/Data/Items/Crown.asset");
var door = GameObject.Find("Locked door");
var doorRule = door.GetComponent<TopDownKit.LockedDoor>();
var gate = GameObject.Find("Gate");
var keyChest = GameObject.Find("Key chest").GetComponent<TopDownKit.Chest>();
var crownChest = GameObject.Find("Crown chest").GetComponent<TopDownKit.Chest>();
var results = new System.Collections.Generic.List<string>();
int failed = 0; string error = null;
Application.runInBackground = true;
mover.readKeyboard = false; it.readKeyboard = false; hotbar.readKeyboard = false;

void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
void Place(Vector2 p) { rb.position = p; rb.linearVelocity = Vector2.zero; player.transform.position = p; }
string Name(TopDownKit.Interactable i) => i == null ? "none" : i.name;

// Steps: each runs once per fixed step until it returns true.
var steps = new System.Collections.Generic.List<(string name, System.Func<bool> run)>();
int stepIndex = 0, stepFrames = 0; float stepStart = 0f;
bool releaseNext = false;
void Go(float x, float y) => steps.Add(("go " + x + "," + y, () =>
{
    Vector2 to = new Vector2(x, y) - rb.position;
    if (to.magnitude < 0.12f) { mover.SetMoveDirection(Vector2.zero); return true; }
    mover.SetMoveDirection(to.normalized);
    return false;
}));
void Face(Vector2 d) { int n = 0; steps.Add(("face " + d, () => { if (n++ == 0) { mover.SetMoveDirection(d); return false; } mover.SetMoveDirection(Vector2.zero); return n > 2; })); }
void Press() => steps.Add(("press", () => { it.SetUseHeld(true); releaseNext = true; return true; }));
void Wait(float s) { float until = -1f; steps.Add(("wait " + s, () => { if (until < 0f) until = Time.time + s; return Time.time >= until; })); }
void Do(string name, System.Action a) => steps.Add((name, () => { a(); return true; }));

// 1. The Guide.
int linesSeen = 0; bool pausedWhileOpen = true; Vector2 heldAt = Vector2.zero;
Go(-4f, 1.5f); Face(Vector2.up);
Do("guide targeted", () => Check("Guide is the target", it.Target != null && it.Target.name == "Guide", Name(it.Target)));
Press(); Wait(0.1f);
Do("text open", () => { Check("talking opens the text box", box.IsOpen && it.PanelOpen && mover.inputPaused, "open " + box.IsOpen + ", paused " + mover.inputPaused); heldAt = rb.position; mover.SetMoveDirection(Vector2.right); });
Wait(0.3f);
Do("no walking", () => { mover.SetMoveDirection(Vector2.zero); Check("no walking while it is open", (rb.position - heldAt).magnitude < 0.01f, "moved " + (rb.position - heldAt).magnitude.ToString("0.000")); });
for (int i = 0; i < 3; i++) { Do("line", () => { if (box.IsOpen) linesSeen++; pausedWhileOpen &= mover.inputPaused; }); Press(); Wait(0.1f); }
Do("text closed", () => Check("E steps through every line and closes", !box.IsOpen && !it.PanelOpen && !mover.inputPaused && linesSeen == 3 && pausedWhileOpen, "lines " + linesSeen + ", open " + box.IsOpen + ", paused " + mover.inputPaused));

// 2. Coins in A (the magnet pulls them in).
Go(-9f, -2.4f); Go(1f, -2.4f); Wait(0.3f);
Do("coins A", () => Check("walked-over coins collected", inv.Count(coin) == 5, "coins " + inv.Count(coin)));

// 3. The locked door without the key.
Go(16f, 7.1f); Go(18.2f, 7.1f); Face(Vector2.right);
Do("door targeted", () => Check("locked door is the target", it.Target == doorRule, Name(it.Target)));
Press(); Wait(0.1f);
Do("locked", () => Check("no key: says it is locked, stays shut", box.IsOpen && box.Line.StartsWith("Locked") && door.activeSelf, "line '" + box.Line + "'"));
Press(); Wait(0.1f);

// 4. The key chest in C.
Go(13.5f, 8.5f); Go(13.5f, 13.5f); Go(8f, 22.6f); Face(Vector2.up);
Do("chest targeted", () => Check("key chest is the target", it.Target == keyChest, Name(it.Target)));
Press(); Wait(0.1f);
Do("take all", () =>
{
    bool open = screen.IsOpen && screen.Chest == keyChest && mover.inputPaused;
    int moved = screen.TakeAll();
    Check("chest screen opens; take all moves key and coins", open && moved == 11 && inv.Has(key) && inv.Count(coin) == 15 && keyChest.Bag.Inventory.Count(coin) == 0,
        "open " + open + ", moved " + moved + ", coins " + inv.Count(coin));
    int back = screen.Put(0);
    int again = screen.Take(0);
    Check("put back and take again (same rule)", back > 0 && again == back && inv.Count(coin) == 15, "put " + back + ", took " + again);
});
Press(); Wait(0.1f);
Do("chest closed", () => Check("E closes the chest screen", !screen.IsOpen && !mover.inputPaused && keyChest.Opened, "open " + screen.IsOpen));

// 5. Open the door.
Go(13.5f, 13.5f); Go(13.5f, 8.5f); Go(18.2f, 7.1f); Face(Vector2.right);
Press(); Wait(0.1f);
Do("door open", () => Check("the key opens the door and is used up", doorRule.IsOpen && !door.activeSelf && !inv.Has(key) && !box.IsOpen, "open " + doorRule.IsOpen + ", key " + inv.Has(key)));

// 6. Room B: coins and the crown.
Go(22f, 7.1f); Go(34f, 7.1f); Wait(0.3f);
Do("coins B", () => Check("coins in B", inv.Count(coin) == 19, "coins " + inv.Count(coin)));
Go(52f, -1.3f); Face(Vector2.up);
Press(); Wait(0.1f);
Do("crown", () => { screen.TakeAll(); });
Press(); Wait(0.1f);
Do("won", () => Check("crown taken - the demo is won", inv.Has(crown) && hud.Won && inv.Count(coin) == 24, "crown " + inv.Has(crown) + ", coins " + inv.Count(coin)));

// 7. Drop one coin and take it back with E.
TopDownKit.Pickup dropped = null;
Do("drop", () =>
{
    int slot = System.Array.FindIndex(inv.slots, s => s.item == coin);
    hotbar.Select(slot);
    dropped = hotbar.Drop(1);
    Check("drop puts one on the floor", dropped != null && inv.Count(coin) == 23 && hotbar.Selected == slot, "slot " + slot + ", coins " + inv.Count(coin));
});
// Phase 15: the phase 13b rules. A coin is a Magnet item: never an E target, and not pulled
// back to its dropper until the dropper has been out of its reach once. A key is an Interact
// item: E takes it back at once.
Vector2 dropSpot = Vector2.zero;
Wait(0.8f);
Do("not yet", () => { dropSpot = rb.position; Check("a dropped coin stays down while its dropper stands by, and is no E target", dropped != null && inv.Count(coin) == 23 && it.Target != dropped, "coins " + inv.Count(coin) + ", target " + Name(it.Target)); });
Do("walk away", () => Place(dropSpot + new Vector2(-8f, 0f)));
Wait(0.3f);
Do("come back", () => Place(dropSpot));
Wait(1f);
Do("taken back", () => Check("after walking away and back, the magnet takes it", dropped == null && inv.Count(coin) == 24, "coins " + inv.Count(coin)));
TopDownKit.Pickup droppedKey = null;
Do("drop key", () =>
{
    inv.Add(key, 1);
    hotbar.Select(System.Array.FindIndex(inv.slots, s => s.item == key));
    droppedKey = hotbar.Drop(1);
});
Wait(0.6f);   // the hotbar's drop wait (0.5 s) applies to everyone
Do("key targeted", () => Check("a dropped key (Interact item) is the E target at once", droppedKey != null && it.Target == droppedKey, Name(it.Target)));
Press(); Wait(0.1f);
Do("key back", () => { Check("E takes the key back", droppedKey == null && inv.Has(key), "key " + inv.Has(key)); inv.Remove(key, 1); });

// 8. Hold mode on the lever in C (placed there, as walking needs the warp side).
int holdSteps = 0;
Do("to lever", () => { Place(new Vector2(45f, 32.9f)); it.settings.use = TopDownKit.UseMode.Hold; });
Wait(1.2f); Face(Vector2.up);
Do("lever targeted", () => Check("lever is the target", it.Target != null && it.Target.name == "Lever", Name(it.Target)));
steps.Add(("hold", () =>
{
    it.SetUseHeld(true);
    holdSteps++;
    if (holdSteps > 120) return true;
    return !gate.activeSelf;
}));
Do("held", () => { it.SetUseHeld(false); Check("Hold: the lever turns after holdTime (0.5 s = 30 steps)", !gate.activeSelf && holdSteps >= 30 && holdSteps <= 32, "steps " + holdSteps); });
Wait(0.1f);
Press(); Wait(0.2f);
Do("tap", () => Check("Hold: a tap does nothing", !gate.activeSelf, "gate " + gate.activeSelf));
Do("press mode", () => it.settings.use = TopDownKit.UseMode.Press);

// 9. Health and ammo pickups feed phase 4's systems.
TopDownKit.Health hp = null; TopDownKit.Gun gun = null; TopDownKit.Pickup ammo = null;
Do("hurt", () =>
{
    hp = player.AddComponent<TopDownKit.Health>(); hp.maxHealth = 6; hp.destroyOnDeath = false; hp.shakeOnHit = 0f; hp.Restore();
    hp.Hit(2, Vector2.zero);
    gun = player.AddComponent<TopDownKit.Gun>();
    gun.weapons = new[] { AssetDatabase.LoadAssetAtPath<TopDownKit.Weapon>("Assets/Data/Weapons/Pistol.asset") };
    gun.SetAim(Vector2.left); gun.SetTrigger(true);
});
Do("release", () => gun.SetTrigger(false));
Wait(0.2f);
Do("to heart", () =>
{
    Check("shot one", gun.Ammo == gun.Current.magazine - 1 && hp.Current == 4, "ammo " + gun.Ammo + ", health " + hp.Current);
    ammo = TopDownKit.Pickup.Spawn(null, 0, new Vector2(110f, -3f), 0, 0f);
    ammo.mode = TopDownKit.PickupChoice.FromItem; ammo.refillAmmo = true;
    Place(new Vector2(104f, -3f));
});
Wait(0.8f);
Go(111f, -3.3f); Wait(0.3f);
Do("healed", () => Check("heart heals 1, ammo pickup refills", hp.Current == 5 && gun.Ammo == gun.Current.magazine && GameObject.Find("Heart") == null && ammo == null,
    "health " + hp.Current + ", ammo " + gun.Ammo));
Do("full health", () =>
{
    var h2 = TopDownKit.Pickup.Spawn(null, 0, (Vector2)player.transform.position, 0, 0f);
    h2.mode = TopDownKit.PickupChoice.FromItem; h2.heal = 1; hp.Heal(5);
    dropped = h2;
});
Wait(0.3f);
Do("left lying", () => Check("at full health a heart is left lying", dropped != null && hp.Current == 6, "health " + hp.Current));

var original = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
var systems = loop.subSystemList;
void Finish()
{
    UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(original);
    Application.runInBackground = false;
    System.AppDomain.CurrentDomain.SetData("rpg_state", error != null ? "error: " + error + "\n" + string.Join("\n", results)
        : "done: " + (failed == 0 ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks\n" + string.Join("\n", results));
}

void OnFixed()
{
    try
    {
        if (stepIndex >= steps.Count) return;
        if (releaseNext) { it.SetUseHeld(false); releaseNext = false; }
        if (stepFrames == 0) stepStart = Time.time;
        stepFrames++;
        if (steps[stepIndex].run()) { stepIndex++; stepFrames = 0; }
        else if (Time.time - stepStart > 20f)
        {
            Check("step '" + steps[stepIndex].name + "'", false, "timed out at " + rb.position);
            stepIndex = steps.Count;
        }
        if (stepIndex >= steps.Count) Finish();
    }
    catch (System.Exception e) { error = steps[Mathf.Min(stepIndex, steps.Count - 1)].name + ": " + e; stepIndex = steps.Count; Finish(); }
}

for (int i = 0; i < systems.Length; i++)
{
    if (systems[i].type == typeof(UnityEngine.PlayerLoop.FixedUpdate))
    {
        var list = new System.Collections.Generic.List<UnityEngine.LowLevel.PlayerLoopSystem>(systems[i].subSystemList);
        list.Insert(0, new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(TopDownKit.Interactor), updateDelegate = OnFixed });
        systems[i].subSystemList = list.ToArray();
    }
}
loop.subSystemList = systems;
System.AppDomain.CurrentDomain.SetData("rpg_state", "running");
UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
return "started, " + steps.Count + " steps";

// Phase 15 check: pickup modes per item and dropping (phase 13b). Run IN PLAY MODE on
// Assets/Scenes/Playground.unity (sh Tools/measure/run-check.sh Playground Tools/measure/pickups.cs pk_state 90).
// Uses room A's "one item per mode" row (y -8): Potion (walk), Gem (magnet, its own reach 4.5),
// Crown (interact, cannot be dropped), a Coin overridden to interact. The player is placed by
// teleport and E is pressed as data (Interactor.SetUseHeld) - real keys are not used.
//   - Walk: not pulled from 2 tiles; taken by standing on it.
//   - Magnet with its own reach: pulled from 4 tiles (past the player's own magnet reach).
//   - Interact: not taken by standing next to it; it is the E target and E takes it.
//   - A scene override (Coin -> interact) beats the item's own mode (Magnet).
//   - Not droppable: the Crown stays in the bag.
//   - A dropped walk item lies 1 tile ahead: not taken back while the dropper stands still; stepping onto it takes it.
//   - Bag screen: opens as a panel (walking paused), drops a whole stack, closes.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on Playground first";
var D = System.AppDomain.CurrentDomain;
if (D.GetData("pk_state") as string == "running") return "already running";

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var it = player.GetComponent<TopDownKit.Interactor>();
var hotbar = player.GetComponent<TopDownKit.Hotbar>();
var bagScreen = player.GetComponent<TopDownKit.BagScreen>();
var collector = player.GetComponent<TopDownKit.Collector>();
var bag = player.GetComponent<TopDownKit.Bag>().Inventory;
TopDownKit.Item I(string n) => AssetDatabase.LoadAssetAtPath<TopDownKit.Item>("Assets/Data/Items/" + n + ".asset");
var potion = I("Potion"); var gem = I("Gem"); var crown = I("Crown"); var coin = I("Coin");
TopDownKit.Pickup Find(string name) { foreach (var p in TopDownKit.Pickup.All) if (p != null && p.name == name) return p; return null; }
var pPotion = Find("Potion (walk)"); var pGem = Find("Gem (magnet, own reach)"); var pCrown = Find("Crown (interact, no drop)"); var pCoin = Find("Coin (override: interact)");
if (pPotion == null || pGem == null || pCrown == null || pCoin == null) return "row items not found";
Application.runInBackground = true;
mover.readKeyboard = false; it.readKeyboard = false; hotbar.readKeyboard = false; if (bagScreen != null) bagScreen.readKeyboard = false;
mover.SetMoveDirection(Vector2.zero);

var results = new System.Collections.Generic.List<string>();
int failed = 0; string error = null;
void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
void Place(Vector2 p) { rb.position = p; rb.linearVelocity = Vector2.zero; player.transform.position = p; }
string Name(TopDownKit.Interactable x) => x == null ? "none" : x.name;

var steps = new System.Collections.Generic.List<System.Func<int, bool>>();
int stepIndex = 0, stepFrame = 0, lastFrame = -1; float stepStart = 0f;
float T() => Time.time - stepStart;
void Wait(float s) => steps.Add(f => T() >= s);
void Do(System.Action a) => steps.Add(f => { a(); return true; });
void Tap() { steps.Add(f => { it.SetUseHeld(true); return true; }); steps.Add(f => { it.SetUseHeld(false); return f >= 1; }); }

Do(() => Check("the player's own magnet reach is shorter than the gem's (setup)", collector.settings.magnetReach < 4f && gem.magnetReach >= 4.5f, "player " + collector.settings.magnetReach + ", gem " + gem.magnetReach));
// Walk
Do(() => Place(new Vector2(3f, -6f)));
Wait(0.6f);
Do(() => Check("walk item: not pulled in from 2 tiles", pPotion != null && bag.Count(potion) == 0, "potions in bag " + bag.Count(potion)));
Do(() => Place(new Vector2(3f, -8f)));
Wait(0.3f);
Do(() => Check("walk item: taken by standing on it", pPotion == null && bag.Count(potion) == 1, "potions in bag " + bag.Count(potion)));
// Magnet with its own reach
Do(() => Place(new Vector2(6f, -4f)));
Wait(1.2f);
Do(() => Check("magnet item with its own reach: pulled in from 4 tiles", pGem == null && bag.Count(gem) == 1, "gems in bag " + bag.Count(gem)));
// Interact
Do(() => Place(new Vector2(9f, -7f)));
Wait(0.6f);
Do(() => Check("interact item: not taken by standing next to it; it is the E target", pCrown != null && bag.Count(crown) == 0 && it.Target == pCrown, "crowns " + bag.Count(crown) + ", target " + Name(it.Target)));
Tap(); Wait(0.1f);
Do(() => Check("interact item: E takes it", pCrown == null && bag.Count(crown) == 1, "crowns " + bag.Count(crown)));
// Scene override
int coins0 = 0;
Do(() => { coins0 = bag.Count(coin); Place(new Vector2(12f, -7f)); });
Wait(0.6f);
Do(() => Check("a scene override (Coin set to interact) is not pulled by the magnet; it is the E target", pCoin != null && bag.Count(coin) == coins0 && it.Target == pCoin, "coins " + coins0 + " -> " + bag.Count(coin) + ", target " + Name(it.Target)));
Tap(); Wait(0.1f);
Do(() => Check("the overridden coin is taken with E", pCoin == null && bag.Count(coin) == coins0 + 1, "coins " + bag.Count(coin)));
// Not droppable
Do(() =>
{
    hotbar.Select(System.Array.FindIndex(bag.slots, s => s.item == crown));
    var d = hotbar.Drop(1);
    Check("not droppable: the crown stays in the bag", d == null && bag.Count(crown) == 1, "dropped " + (d != null) + ", crowns " + bag.Count(crown));
});
// A dropped walk item lands 1 tile ahead, outside its pick-up reach (0.6): standing still does
// not take it back; stepping onto it does (the dropper has been "away" from the start - 13b).
TopDownKit.Pickup dropped = null; Vector2 droppedAt = Vector2.zero;
Do(() => { Place(new Vector2(0f, -4f)); hotbar.Select(System.Array.FindIndex(bag.slots, s => s.item == potion)); dropped = hotbar.Drop(1); });
Wait(1f);
Do(() => { Check("a dropped walk item lies 1 tile off and is not taken back while its dropper stands still", dropped != null && bag.Count(potion) == 0 && Vector2.Distance(dropped.transform.position, rb.position) > 0.9f, "on the floor " + (dropped != null) + ", potions " + bag.Count(potion)); droppedAt = dropped != null ? (Vector2)dropped.transform.position : Vector2.zero; Place(droppedAt); });
Wait(0.3f);
Do(() => Check("stepping onto it takes it back", dropped == null && bag.Count(potion) == 1, "potions " + bag.Count(potion)));
// Bag screen
int coinSlot = -1, stack = 0; TopDownKit.Pickup pile = null;
Do(() => { bagScreen.Open(); });
Wait(0.1f);
Do(() =>
{
    Check("bag screen opens as a panel; walking paused", bagScreen.IsOpen && it.PanelOpen && mover.inputPaused, "open " + bagScreen.IsOpen + ", panel " + it.PanelOpen + ", paused " + mover.inputPaused);
    coinSlot = System.Array.FindIndex(bag.slots, s => s.item == coin);
    stack = coinSlot >= 0 ? bag.slots[coinSlot].count : 0;
    pile = coinSlot >= 0 ? bagScreen.Drop(coinSlot) : null;
});
Wait(0.1f);
Do(() =>
{
    Check("bag screen: a click drops the whole stack as one floor item", pile != null && pile.stack.count == stack && stack > 0 && bag.Count(coin) == 0, "stack " + stack + ", on the floor " + (pile != null ? pile.stack.count : 0) + ", left " + bag.Count(coin));
    bagScreen.Close();
});
Wait(0.1f);
Do(() => Check("bag screen closes; walking back", !bagScreen.IsOpen && !it.PanelOpen && !mover.inputPaused, "open " + bagScreen.IsOpen + ", paused " + mover.inputPaused));

void Finish()
{
    EditorApplication.update -= Tick;
    mover.readKeyboard = true; it.readKeyboard = true; hotbar.readKeyboard = true; if (bagScreen != null) { bagScreen.readKeyboard = true; if (bagScreen.IsOpen) bagScreen.Close(); }
    it.SetUseHeld(false);
    D.SetData("pk_state", (error != null ? "error: " + error + "\n" : "done: " + (failed == 0 ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks\n") + string.Join("\n", results));
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
D.SetData("pk_state", "running");
EditorApplication.update += Tick;
return "started " + steps.Count + " steps";

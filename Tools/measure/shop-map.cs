// Phase 15 check: the coin shop and the minimap reveal (phase 13), on Assets/Scenes/LittleRpg.unity
// (sh Tools/measure/run-check.sh LittleRpg Tools/measure/shop-map.cs sm_state 60).
// Shop (room A's shopkeeper: Potion x5 at 4 coins, Gem x2 at 15, sells back at half): through the
// trade entry points the shop screen's clicks call (BuyLine / SellSlot - trap 39), plus opening
// the screen with E as data. No money -> NoMoney; a buy takes the price and one off the line;
// a line runs out -> SoldOut; selling gives half the price back and refills the line; an item
// the shop does not sell -> NotWanted; a full bag -> NoRoom and nothing changes (no half trades).
// Minimap (corner, rooms appear once visited): only the start room at first; walking into C
// adds it (corner and full map); with reveal off every room shows.

if (!EditorApplication.isPlaying) return "not playing - enter play mode on LittleRpg first";
var D = System.AppDomain.CurrentDomain;
if (D.GetData("sm_state") as string == "running") return "already running";

var player = GameObject.Find("Player");
var rb = player.GetComponent<Rigidbody2D>();
var mover = player.GetComponent<TopDownKit.PlayerMover>();
var it = player.GetComponent<TopDownKit.Interactor>();
var bag = player.GetComponent<TopDownKit.Bag>().Inventory;
var shop = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Shop>();
var map = UnityEngine.Object.FindFirstObjectByType<TopDownKit.Minimap>();
TopDownKit.Item I(string n) => AssetDatabase.LoadAssetAtPath<TopDownKit.Item>("Assets/Data/Items/" + n + ".asset");
var coin = I("Coin"); var potion = I("Potion"); var gem = I("Gem"); var key = I("Key");
int pLine = shop.LineOf(potion), gLine = shop.LineOf(gem);
Application.runInBackground = true;
mover.readKeyboard = false; it.readKeyboard = false; mover.SetMoveDirection(Vector2.zero);
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
int RoomsDrawn() => (int)typeof(TopDownKit.Minimap).GetField("roomsUsed", flags).GetValue(map);
TopDownKit.Room RoomNamed(string n) { foreach (var r in TopDownKit.Room.All) if (r != null && r.name == n) return r; return null; }

var results = new System.Collections.Generic.List<string>();
int failed = 0; string error = null;
void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
void Place(Vector2 p) { rb.position = p; rb.linearVelocity = Vector2.zero; player.transform.position = p; }
string Bag() => "coins " + bag.Count(coin) + ", potions " + bag.Count(potion) + ", gems " + bag.Count(gem) + ", left P" + shop.Left(pLine) + " G" + shop.Left(gLine);

var steps = new System.Collections.Generic.List<System.Func<int, bool>>();
int stepIndex = 0, stepFrame = 0, lastFrame = -1; float stepStart = 0f;
float T() => Time.unscaledTime - stepStart;
void Wait(float s) => steps.Add(f => T() >= s);
void Do(System.Action a) => steps.Add(f => { a(); return true; });

// ---- minimap first (before walking anywhere) ----
Do(() => Check("map: at the start only the start room is drawn", RoomsDrawn() == 1 && RoomNamed("Room A").Visited && !RoomNamed("Room C").Visited, RoomsDrawn() + " drawn"));
Do(() => Place(new Vector2(8f, 20f)));
Wait(0.8f);
Do(() => { Check("map: walking into room C reveals it", RoomNamed("Room C").Visited && RoomsDrawn() == 2, RoomsDrawn() + " drawn"); map.ToggleFull(); });
Wait(0.1f);
Do(() => { Check("map: the full map shows the same visited rooms", map.FullOpen && RoomsDrawn() == 2, "open " + map.FullOpen + ", " + RoomsDrawn() + " drawn"); map.ToggleFull(); map.revealOnVisit = false; });
Wait(0.1f);
Do(() => { int all = 0; foreach (var r in TopDownKit.Room.All) if (r != null) all++; Check("map: with reveal off every room shows", RoomsDrawn() == all, RoomsDrawn() + " of " + all); map.revealOnVisit = true; });

// ---- shop ----
Do(() =>
{
    bag.Remove(coin, bag.Count(coin));
    var r = shop.BuyLine(bag, pLine);
    Check("shop: no money -> NoMoney, nothing changes", r == TopDownKit.TradeResult.NoMoney && bag.Count(potion) == 0 && shop.Left(pLine) == 5, r + "; " + Bag());
    bag.Add(coin, 20);
    r = shop.BuyLine(bag, pLine);
    Check("shop: buying a potion takes 4 coins and one off the line", r == TopDownKit.TradeResult.Done && bag.Count(coin) == 16 && bag.Count(potion) == 1 && shop.Left(pLine) == 4, r + "; " + Bag());
    bag.Add(coin, 30);
    var g1 = shop.BuyLine(bag, gLine); var g2 = shop.BuyLine(bag, gLine); var g3 = shop.BuyLine(bag, gLine);
    Check("shop: the gem line (2) sells out", g1 == TopDownKit.TradeResult.Done && g2 == TopDownKit.TradeResult.Done && g3 == TopDownKit.TradeResult.SoldOut && bag.Count(gem) == 2 && bag.Count(coin) == 16, g1 + " " + g2 + " " + g3 + "; " + Bag());
    int c0 = bag.Count(coin);
    r = shop.SellSlot(bag, System.Array.FindIndex(bag.slots, s => s.item == potion));
    Check("shop: selling a potion back gives half its price (2) and refills the line", r == TopDownKit.TradeResult.Done && bag.Count(coin) == c0 + 2 && bag.Count(potion) == 0 && shop.Left(pLine) == 5, r + "; " + Bag());
    bag.Add(key, 1);
    r = shop.SellSlot(bag, System.Array.FindIndex(bag.slots, s => s.item == key));
    Check("shop: an item it does not sell -> NotWanted", r == TopDownKit.TradeResult.NotWanted && bag.Has(key), r.ToString());
    bag.Add(key, 100000);   // every free slot
    c0 = bag.Count(coin);
    r = shop.BuyLine(bag, pLine);
    Check("shop: a full bag -> NoRoom, the coins are kept (no half trade)", bag.RoomFor(potion) == 0 && r == TopDownKit.TradeResult.NoRoom && bag.Count(coin) == c0 && shop.Left(pLine) == 5, r + "; " + Bag());
    bag.Remove(key, bag.Count(key));
});
// Walking in from room C fades the camera; the shop is opened during the fade. Phase 15 bug:
// the fade's end switched walking back on under the open screen (one inputPaused, two owners).
var rig = Camera.main.GetComponent<TopDownKit.CameraRig>();
bool fading = false;
Do(() => Place((Vector2)shop.transform.position + new Vector2(0f, -1.3f)));
Wait(0.3f);
Do(() => { fading = rig.InTransition; it.SetUseHeld(true); });
Wait(0.05f);
Do(() => { it.SetUseHeld(false); var sc = TopDownKit.ShopScreen.Current; Check("shop: E next to the shopkeeper opens the shop screen", sc != null && sc.IsOpen, "open " + (sc != null && sc.IsOpen) + ", during the camera fade " + fading); });
steps.Add(f => !rig.InTransition && f > 2);
Wait(0.2f);
Do(() => { var sc = TopDownKit.ShopScreen.Current; Check("a panel opened during a camera fade keeps walking paused after the fade ends", fading && sc != null && sc.IsOpen && mover.inputPaused, "fade seen " + fading + ", open " + (sc != null && sc.IsOpen) + ", walking paused " + mover.inputPaused); });
Do(() => it.SetUseHeld(true));
Wait(0.05f);
Do(() => { it.SetUseHeld(false); var sc = TopDownKit.ShopScreen.Current; Check("shop: E again closes it; walking back", sc != null && !sc.IsOpen && !mover.inputPaused, "open " + (sc != null && sc.IsOpen) + ", walking paused " + mover.inputPaused); });

void Finish()
{
    EditorApplication.update -= Tick;
    mover.readKeyboard = true; it.readKeyboard = true; it.SetUseHeld(false);
    D.SetData("sm_state", (error != null ? "error: " + error + "\n" : "done: " + (failed == 0 ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks\n") + string.Join("\n", results));
}
void Tick()
{
    try
    {
        if (!EditorApplication.isPlaying) { error = "play mode ended"; Finish(); return; }
        if (Time.frameCount == lastFrame) return;
        lastFrame = Time.frameCount;
        if (steps[stepIndex](stepFrame)) { stepIndex++; stepFrame = 0; stepStart = Time.unscaledTime; if (stepIndex >= steps.Count) { Finish(); return; } }
        else stepFrame++;
    }
    catch (System.Exception e) { error = e.ToString(); Finish(); }
}
stepStart = Time.unscaledTime;
D.SetData("sm_state", "running");
EditorApplication.update += Tick;
return "started " + steps.Count + " steps";

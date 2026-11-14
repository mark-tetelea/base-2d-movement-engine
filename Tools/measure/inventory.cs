// Checks the inventory rule and the best-target rule. Runs in EDIT mode (plain rules):
//     unity command eval_file --file Tools/measure/inventory.cs
// Items are made in memory and thrown away; nothing in the project changes.
var results = new System.Collections.Generic.List<string>();
int failed = 0;
void Check(string name, bool ok, string detail) { if (!ok) failed++; results.Add((ok ? "PASS " : "FAIL ") + name + " (" + detail + ")"); }
TopDownKit.Item Make(string n, int stack) { var i = ScriptableObject.CreateInstance<TopDownKit.Item>(); i.name = n; i.displayName = n; i.maxStack = stack; return i; }
var coin = Make("Coin", 99);
var key = Make("Key", 1);
var gem = Make("Gem", 10);
string Show(TopDownKit.Inventory inv) { var s = ""; foreach (var x in inv.slots) s += x.Empty ? "[ ]" : "[" + x.item.name + " " + x.count + "]"; return s; }

// Stacking: tops up an existing stack first, then empty slots, in order.
var a = new TopDownKit.Inventory(new TopDownKit.InventorySettings(4, 2, 0));
int left = a.Add(coin, 50);
left += a.Add(key, 1);
left += a.Add(coin, 60);   // 49 onto slot 0, 11 into slot 2
Check("stack tops up then spills", left == 0 && a.slots[0].count == 99 && a.slots[1].item == key && a.slots[2].item == coin && a.slots[2].count == 11, Show(a));
Check("count across stacks", a.Count(coin) == 110, "coins " + a.Count(coin));

// Non-stacking items take a slot each.
var b = new TopDownKit.Inventory(new TopDownKit.InventorySettings(3, 0, 0));
left = b.Add(key, 2);
Check("maxStack 1 takes a slot each", left == 0 && b.slots[0].count == 1 && b.slots[1].count == 1 && b.slots[2].Empty, Show(b));

// Full inventory: returns what did not fit, keeps what did.
var c = new TopDownKit.Inventory(new TopDownKit.InventorySettings(2, 0, 0));
left = c.Add(gem, 25);
Check("full returns leftover", left == 5 && c.Count(gem) == 20, "left " + left + " " + Show(c));
Check("room for when full", c.RoomFor(gem) == 0 && c.RoomFor(key) == 0, "gem " + c.RoomFor(gem) + " key " + c.RoomFor(key));
left = c.Add(key, 1);
Check("other item into full bag refused", left == 1 && !c.Has(key), Show(c));

// Stack limit setting caps every item.
var d = new TopDownKit.Inventory(new TopDownKit.InventorySettings(3, 0, 5));
left = d.Add(coin, 12);
Check("stackLimit setting caps stacks", left == 0 && d.slots[0].count == 5 && d.slots[1].count == 5 && d.slots[2].count == 2, Show(d));

// Remove takes from the last slots first.
left = d.Remove(coin, 3);
Check("remove from the back", left == 3 && d.slots[2].Empty && d.slots[1].count == 4 && d.Count(coin) == 9, Show(d));

// Chest transfer: same rule both ways, partial moves leave the rest.
var chest = new TopDownKit.Inventory(new TopDownKit.InventorySettings(6, 0, 0));
chest.Add(key, 1); chest.Add(coin, 30); chest.Add(gem, 15);
var bag = new TopDownKit.Inventory(new TopDownKit.InventorySettings(2, 2, 0));
bag.Add(coin, 90);
int moved = TopDownKit.Inventory.Move(chest, 1, bag);   // 30 coins: 9 top up slot 0, 21 into slot 1
Check("move a stack into the bag", moved == 30 && bag.Count(coin) == 120 && chest.slots[1].Empty, Show(bag) + " | " + Show(chest));
moved = TopDownKit.Inventory.Move(chest, 0, bag);       // key: bag full
Check("move into full bag moves nothing", moved == 0 && chest.slots[0].item == key, Show(bag) + " | " + Show(chest));
moved = TopDownKit.Inventory.Move(bag, 1, chest);       // put 21 coins back
Check("move back into the chest", moved == 21 && chest.Count(coin) == 21 && bag.slots[1].Empty, Show(bag) + " | " + Show(chest));
moved = TopDownKit.Inventory.MoveAll(chest, bag);       // key into slot 1; coins top up 99 (0 room); gem no room
Check("take all fills what fits", moved == 1 && bag.Has(key) && chest.Count(gem) == 15 && chest.Count(coin) == 21, "moved " + moved + " " + Show(bag) + " | " + Show(chest));
int total = bag.Count(coin) + chest.Count(coin);
Check("no coins made or lost", total == 120, "total " + total);
var taken = bag.TakeFrom(0, 5);
Check("take from a slot (drop)", taken.item == coin && taken.count == 5 && bag.slots[0].count == 94, Show(bag));

// Best target.
var s = new TopDownKit.InteractSettings(2f, TopDownKit.BestTarget.Closest, TopDownKit.UseMode.Press, 0.5f);
var pts = new System.Collections.Generic.List<Vector2> { new Vector2(1.5f, 0f), new Vector2(0f, -1f), new Vector2(3f, 0f) };
Check("closest in reach", TopDownKit.Interaction.Pick(Vector2.zero, Vector2.right, pts, s) == 1, "picked " + TopDownKit.Interaction.Pick(Vector2.zero, Vector2.right, pts, s));
s.best = TopDownKit.BestTarget.Facing;
Check("facing picks the one in front", TopDownKit.Interaction.Pick(Vector2.zero, Vector2.right, pts, s) == 0, "picked " + TopDownKit.Interaction.Pick(Vector2.zero, Vector2.right, pts, s));
Check("facing down picks the one below", TopDownKit.Interaction.Pick(Vector2.zero, Vector2.down, pts, s) == 1, "");
Check("nothing in front falls back to closest", TopDownKit.Interaction.Pick(Vector2.zero, Vector2.up, pts, s) == 1, "picked " + TopDownKit.Interaction.Pick(Vector2.zero, Vector2.up, pts, s));
var two = new System.Collections.Generic.List<Vector2> { new Vector2(1.8f, 0f), new Vector2(0.9f, 0f) };
Check("same angle: closer wins", TopDownKit.Interaction.Pick(Vector2.zero, Vector2.right, two, s) == 1, "");
s.reach = 1f;
Check("reach setting", TopDownKit.Interaction.Pick(Vector2.zero, Vector2.right, pts, s) == 1 && TopDownKit.Interaction.Pick(new Vector2(5f, 5f), Vector2.right, pts, s) == -1, "");
Check("empty list", TopDownKit.Interaction.Pick(Vector2.zero, Vector2.right, new System.Collections.Generic.List<Vector2>(), s) == -1, "");

foreach (var o in new UnityEngine.Object[] { coin, key, gem }) UnityEngine.Object.DestroyImmediate(o);
return (failed == 0 ? "ALL PASS " : failed + " FAILED ") + results.Count + " checks\n" + string.Join("\n", results);

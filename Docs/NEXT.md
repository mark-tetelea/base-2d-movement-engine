# Next

> Overwritten at every `/handoff`. Written 2026-11-15, end of phase 13b (item pickup pass).

## Do first

1. **Phase 14 of `Docs/ROADMAP.md` (make everything swappable)** - when the counselor starts it. Notes for it below
   ("Phase 14 notes").
2. The playtests from `CONTEXT.md`, "What is not done" - now including the pickup modes (Playground row in room A:
   potion walk, gem magnet, crown press E, coin overridden to press E), dropping (Q, I bag screen) and the hermit's
   spilling chest in LittleRpg.

## Half-done (for the phase 15 test pass)

- **Phase 13b was checked by** one scripted Playground / LittleRpg / Dungeon run each (entry points, not keys) and
  `Tools/measure/save.cs` (ALL PASS on the last run, 46 checks; LOG 2026-11-15). Phase 15 should add: a pickup check
  (each mode, item reach vs collector reach, "walk away first", droppable off, shop OnFloor, chest spill, enemy
  drops) driven by real keys; the bag screen by mouse clicks and on a capture (traps 39, 81).
- **`save.cs` failed once on a Dungeon layout** (seed 1931867546): "player moved 0.26 tiles" after loading. Probably the
  known phase 11 spawn overlap; not re-run on that seed. Try it with a fixed seed on the generator.
- **Unsure (13b):** a spilled chest throws each stack as one pile (25 coins = one pickup of 25), not 25 coins; the
  bag screen drops the whole stack, Q drops one; Shop OnFloor lands at `deliverAt` (0, -1.5) from the shop - inside
  a wall if a shop stands with a wall below it; dropped items land 1 tile in front even into a wall (as before);
  "walk away first" uses the collector's reach for that item (4.5 tiles for the gem), so a dropped gem needs a long walk;
  `Drops` seed 0 is by position in 1/16 tiles, so two enemies dying on the same spot drop the same; the LittleRpg shop
  stays IntoBag (OnFloor only checked by setting it at run time).
- **Phase 14 notes:** `World/Drops` now depends on `Inventory` (`Pickup.Spawn`, `Item`); `Shop` and `Chest` call
  `Pickup.Spawn`; `BagScreen` depends on `Hotbar`, `Interactor`, `KitUI`. The I key is hard-coded like Q and T.
- From phase 13: multi-touch with two fingers, touch Auto from a real touch, touch layout at 1366x768, minimap and
  shop screen on a capture, buying/selling by clicks, Save/Load by click. Known gaps: crates are not saved (coins can
  be farmed); enemies not in waves come back on load; heal-only pickups dropped at run time are not saved; a renamed
  or moved scene object loses its saved state; the Gun's ammo, selected weapon, hotbar selection and facing are not
  saved; no "Continue" on the start menu; potions and gems do nothing.
- **Found, not fixed (phase 11):** in some Dungeon layouts the player spawns overlapping something and is pushed ~1
  tile up in the first 3 steps.
- **Editor quirk:** in play mode the editor's mouse can read as held (trap 84); play checks that compare positions
  should add their own Mouse like `save.cs`.
- From before: phase 12/11/10/9 smoke-only items, re-run `combat.cs` and `bullet-alloc.cs`, camera check flake, broken
  crumbling in "blocks" mode, tiles closing on someone, `screens.cs` at WXGA, WebGL module + build, GitHub push,
  GIFs, `Assets/_Recovery/` to delete, default font, aim assist bends toward crates.

## Waiting on the user

- Playtests (everything so far, now also the pickup modes and dropping). WebGL module install, repo push.

## Don't

- Don't change `SaveData` (or any saved struct) without raising `SaveGame.Version` (now 2) and adding an upgrade step
  that works by what values mean; never let `JsonUtility` see a file `SaveFiles` has not checked.
- Don't put items on the floor any way but `Pickup.Spawn` (or a Pickup prefab); don't bring back a per-pickup `auto`.
- Don't put `Drops` on the shared enemy prefabs: the Arena's player has no bag or collector (drops go on scene /
  room-piece instances).
- Don't run play checks against the real save folder: set `SaveFiles.FolderOverride` (trap 50). Leaving play mode
  writes the settings file.
- Don't read components in `OnDisable` to save them (trap 86). Don't make the pause menu know about saving.
- Don't add Android/iOS build targets. Don't reorder `FeelEvent`. Don't make core code call `FeelPlayer`.
- Don't write or run checks per phase beyond the vital ones (user, 2026-11-14).
- From before: no hand-painted generated rooms, no second door system, no Crate class, no navmesh, no tile code in
  enemies, don't drive mouse presses through `Mouse.current` (trap 84), don't recompile while `isPlaying` (trap 83),
  don't change the licence, don't push or add a remote without the user saying so.

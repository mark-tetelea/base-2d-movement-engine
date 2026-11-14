# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 13 (save/load, shop, minimap, touch).

## Do first

1. **Phase 13b of `Docs/ROADMAP.md` (item pickup pass)** - when the counselor starts it. The shop is ready
   for it: `Shop.BuyLine` puts the item straight in the bag today; 13b decides whether a bought item can
   instead land on the floor / come with a pickup mode. Loose floor items are saved already
   (`SaveData.floorItems`: item, count, position, auto) - if 13b adds a per-item pickup mode, that is a
   save-shape change: raise `SaveGame.Version` and add an upgrade step (traps 46-47).
2. The playtests from `CONTEXT.md`, "What is not done" - now including the shop prices, the map and the
   touch controls on a real phone (nobody has touched them with a finger).

## Half-done (for the phase 15 test pass)

- **Phase 13 was checked by** `Tools/measure/save.cs` (the vital one, ALL PASS 4 runs) and one smoke run per
  scene (LOG 2026-11-14). Run `save.cs` again after any change to a saved system. Phase 15 should add:
  multi-touch with two fingers (move + aim at once) - UI Toolkit pointer ids through the EventSystem are
  unverified; touch Auto turning on from a real touch; the touch layout over the hotbar and HUD on a 16:9
  phone and at 1366x768; a look at the minimap (corner and full) and the shop screen on a capture (trap 81);
  buying/selling by mouse clicks (only entry points were driven, trap 39); Save/Load buttons by click.
- **Unsure / known gaps:** crates are not saved (a reload brings broken crates back, coins can be farmed);
  enemies not in a room's waves come back on load (the Playground's wanderers); heal-only pickups dropped at
  run time (crate hearts) are not saved; a renamed or moved scene object loses its saved state (warning in the
  console); save ids include `#k` for same-named siblings, worked out at scene start; settings are written
  from the values seen in the last half second of real time; the Gun's ammo and selected weapon, the hotbar
  selection and facing are not saved; no "Continue" on the start menu (load is from the pause menu only);
  the shopkeeper uses the NPC sprite tinted; potions and gems do nothing.
- **Found, not fixed (phase 11):** in some Dungeon layouts the player spawns overlapping something and is
  pushed ~1 tile up in the first 3 steps (seen on several seeds, spawn y 11 -> 12.0).
- **Editor quirk:** in play mode the editor's mouse can read as held (trap 84) - a fresh Dungeon's player
  then fires once on spawn. Any play check that compares positions should add its own Mouse like `save.cs`.
- From phase 12: timeScale after mixes of freezes/pauses/loads, hit-stop at 30 vs 144 fps, sound and effect
  pools; Lever plays DoorOpen when it closes; enemy shots use the Shot sound; Footsteps only on keyboard
  players; DayNight multiplies a coloured global light. Phase 14 notes: Feel now also listens to `Shop` and
  `SaveGame`; Save depends on Inventory, Combat, Interaction, Camera, World and Shop; Map and Touch depend
  only on core + Screens; the Playground picker has rows for Map and Touch.
- From before: phase 11/10/9 smoke-only items, re-run `combat.cs` and `bullet-alloc.cs`, camera check flake,
  broken crumbling in "blocks" mode, tiles closing on someone, `screens.cs` at WXGA, WebGL module + build,
  GitHub push, GIFs, `Assets/_Recovery/` to delete, default font, warning `Pickup.All` hides
  `Interactable.All`, aim assist bends toward crates.

## Waiting on the user

- Playtests (everything so far, now also the shop, the map and touch on a phone). WebGL module install,
  repo push.

## Don't

- Don't change `SaveData` (or any saved struct) without raising `SaveGame.Version` and adding an upgrade step
  that works by what values mean; never let `JsonUtility` see a file `SaveFiles` has not checked.
- Don't run play checks against the real save folder: set `SaveFiles.FolderOverride` (trap 50). Leaving play
  mode writes the settings file.
- Don't read components in `OnDisable` to save them (trap 86).
- Don't make the pause menu know about saving: add-ons use `PauseMenu.AddButton`.
- Don't add Android/iOS build targets (still on "Do NOT build yet").
- Don't reorder `FeelEvent` - add at the end. Don't make core code call `FeelPlayer`.
- Don't write or run checks per phase beyond the vital ones (user, 2026-11-14).
- From before: no hand-painted generated rooms, no second door system, no Crate class, no navmesh, no tile
  code in enemies, don't drive mouse presses through `Mouse.current` (trap 84), don't recompile while
  `isPlaying` (trap 83), don't change the licence, don't push or add a remote without the user saying so.

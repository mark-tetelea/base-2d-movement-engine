# Next

> Overwritten at every `/handoff`. Written 2026-11-15, end of phase 14 (make everything swappable).

## Do first

1. **Phase 15 of `Docs/ROADMAP.md` (the full test pass)** - when the counselor starts it. Start with
   `sh Tools/measure/compile.sh`, `sh Tools/measure/removal-check.sh` (ALL PASS, 17) and `save.cs` (ALL PASS, 47),
   then re-run every other check in `Tools/measure/` - none was re-run in phase 14 (notes below).
2. The playtests from `CONTEXT.md`, "What is not done" - now including the rebind screen (Esc > Controls) and
   the pixel theme (point `Resources/KitTheme` at the font, or swap in `Assets/UI/KitThemePixel`).

## Half-done (for the phase 15 test pass)

- **Phase 14 input path:** every key now goes through `Controls` (same `Keyboard.current` reads as before, so
  the virtual-keyboard checks should still drive it). Re-run `movement.cs` (CLAUDE.md: after any movement change),
  `combat.cs`, `tiles.cs` (fixed for `GetBag`, not run), `screens.cs`, `playthrough.cs`, `rpg-playthrough.cs`.
  Add a check that drives the rebind screen with real key presses and a mouse button (trap 84 for the mouse), and
  that touch buttons still fire/dash/use (they now go through `Controls.SetHeld/Tap`; only entry points were run).
- **Not tried:** deleting a folder for real in the editor (the check compiles without it; scenes then show
  "Missing script" - harmless, expected). Enemies with a `SpriteAnimSet` (the component supports it; no enemy has
  one). `Item.floorPrefab` (no item uses one). Theme sprites (box/button/slot art) - all empty, flat boxes as before.
- `Tools/measure/setup-phase14.cs`: its player-art part read fields that are now removed, so it no longer compiles
  as is (kept as the record); `setup-phase13.cs` sets `TouchControls` fields that are gone.
- The default-font theme `KitTheme` and `KitThemePixel` are separate copies: a colour changed in one is not in the
  other.
- From 13b / 13 / earlier (unchanged): pickup check by real keys, bag screen by clicks; `save.cs` once failed on
  Dungeon seed 1931867546 (spawn overlap, phase 11, not fixed); multi-touch; crates not saved; enemies outside
  waves come back on load; no "Continue"; potions and gems do nothing; WebGL module, GIFs, push; `Assets/_Recovery/`
  to delete; aim assist bends toward crates; crumbling in "blocks" mode; camera check flake.

## Waiting on the user

- Playtests (everything so far, now also rebinding and the pixel font). Which font the kit's default theme
  should use (default font kept for now). WebGL module install, repo push.

## Don't

- Don't add a reference between system asmdefs without checking the removal check still passes and the README's
  table still says what goes with what; Core (`Core`, `Movement`, `Facing`, `Camera`, `Screens`) references no
  other kit folder.
- Don't read keys or the mouse buttons directly (`Keyboard.current`) in kit code - use `Controls` (pointer position
  is fine). Don't play sounds outside Feel - raise `Cues`. Don't hard-code UI colours - use `KitUI` / the theme.
- Don't make Feel, Save, Map or Touch reference Combat / Inventory / Shop / World types again - use the promises in
  `Core/Contracts.cs` (or `IItemStock`).
- Don't change `SaveData` (or any saved struct) without raising `SaveGame.Version` (now 2) and an upgrade step by
  meaning. The settings file's "Controls" entry is a value, not a shape change.
- Don't reorder `FeelEvent` (now in `Core/Cues.cs`). Don't put items on the floor except through `Pickup.Spawn`.
- Don't run play checks against the real save folder (`SaveFiles.FolderOverride`, trap 50). New statics need a
  reset - domain reload is off (trap 88).
- From before: no gamepad, no Android/iOS targets, no navmesh, no second door system, don't push.

# Next

> Overwritten at every `/handoff`. Written 2026-11-15, end of phase 15 (full test pass, made lighter by the user).

## Do first

1. **The screens check at 1920x1080:** after an editor restart its virtual mouse presses never arrive (no shot even
   on the floor; 3 runs), while WXGA passes 8/8 and both passed before the restart. Run
   `sh Tools/measure/run-check.sh Playground Tools/measure/screens.cs - 120` at Full HD (`game-view-size.cs`) and
   find out why (Game view scaled to fit a smaller window? input routing after restart?). Not the font.
2. Look at the screens in the pixel font at both sizes (render the panel to a RenderTexture, trap 81) - nobody has
   since it became the default.
3. Any check: `sh Tools/measure/run-check.sh <Scene> <file> <state key>`; one-shot checks key "=", self-polling "-".

## Half-done / not checked

- Left on purpose by the lighter pass (still unsure, from earlier phases): deleting a folder for real in the
  editor; an enemy with a `SpriteAnimSet`; `Item.floorPrefab` (no item uses one); theme sprites (all empty);
  crumbling in "blocks" mode and a tile/door closing on someone (push-out); the Dungeon spawn overlap
  (save.cs once failed on seed 1931867546, phase 11); multi-touch / a real finger; bag screen by real clicks.
- `Assets/UI/KitThemePixel` is now identical to `Resources/KitTheme` (name only differs) - keep or delete.
- `setup-phase14.cs` and `setup-phase13.cs` no longer compile as is (fields removed); kept as records.
- Known gaps, not bugs: crates not saved; enemies outside waves come back on load; no "Continue"; potions and gems
  do nothing; `Assets/_Recovery/` to delete; group followers can trail up to ~7.7 tiles behind their place while
  the leader turns (enemies2.cs, feel).

## Waiting on the user

- **Aim assist and crates:** the rule bends shots toward any living Health of another team, so a crate (team 2)
  in the cone and nearer than an enemy wins. Keep, or skip crates (a setting)? Recommendation: skip team 2.
- Playtests (everything; nobody has played any of it). WebGL module, GIFs, repo push.

## Don't

- Don't set `PlayerMover.inputPaused` directly - call `PauseInput(owner, on)` (trap 91).
- Don't drive a virtual keyboard/mouse in a check without switching the real ones off (trap 90).
- Don't run play checks against the real save folder (`SaveFiles.FolderOverride`; run-check.sh clears it after).
- Don't read keys directly in kit code (`Controls`), don't play sounds outside Feel (`Cues`), don't hard-code UI
  colours (theme). Don't change a saved struct without raising `SaveGame.Version` + an upgrade step.
- Don't add a reference between system asmdefs without re-running `removal-check.sh` (ALL PASS, 17).
- From before: no gamepad, no Android/iOS targets, no navmesh, no second door system, don't push.

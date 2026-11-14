# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 5 (interaction, inventory, Little RPG room demo).

## Do first

1. **The user plays the Little RPG room** (`Assets/Scenes/LittleRpg.unity`, ~5 min): is reach
   1.5 tiles right; does "the one you face" pick what you expect (or prefer closest); hold
   vs press; magnet 2.5 tiles / 10 tiles/s; is the chest screen (click, T, E) clear? Also the
   Arena shooter playtest and the earlier phases' questions (LOG).
2. Then phase 6 of `Docs/ROADMAP.md`: Playground picker and the real screens (replace the
   OnGUI `TextBox`, `ChestScreen`, `RpgHud`, `ShooterHud`; read UI Toolkit traps 16-21, 32-38).

## Half-done

- `Assets/_Recovery/` (+ `.meta`): Unity's crash backup, not committed. Delete it in the Project window.
- `Tools/measure/out/combat.csv`: written by `combat.cs`, not committed.
- Screens are OnGUI placeholders; their look is unchecked (`capture_game_view` does not show OnGUI).
- Chest screen mouse clicks are not driven by any check (trap 39); `TakeAll`/`Take`/`Put` are.
- NPCs are the player sprite tinted; no NPC facing. Hotbar slots have no "use" action yet (a game adds it).

## Waiting on the user

- The playtests above.

## Don't

- Don't make a second inventory for chests: chests are a `Bag`, transfers go through `Inventory.Move`.
- Don't read keys inside rules: `Interactor.SetUseHeld`, `Hotbar.Select/Drop`, `ChestScreen.Take/Put/TakeAll` are the entry points.
- Don't time play-mode checks in frames (trap 78); end float timers with an epsilon (trap 79).
- Checks: `movement.cs`, `facing.cs`, `camera.cs` on TestArena; `combat.cs`, `playthrough.cs`,
  `bullet-alloc.cs` on ArenaShooter; `rpg-playthrough.cs` on LittleRpg (play mode);
  `inventory.cs` in edit mode. After one, check `runInBackground` is back to 0 (trap 77).

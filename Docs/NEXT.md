# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 6 (Playground picker, shared screens, start menu).

## Do first

1. **The user plays from the start menu** (`Assets/Scenes/StartMenu.unity`, ~10 min): is the
   text readable at their screen size; is the settings panel (Tab) clear; does any click on
   it fire a shot by mistake; Esc pause. Plus the earlier playtests (feel, facing, combat,
   RPG room - see `CONTEXT.md`, "What is not done").
2. Then phase 7 of `Docs/ROADMAP.md`: README, GIFs, WebGL build, licence. A WebGL build is
   the first real build - check the screens and the start menu in it (nothing has been built
   to a player yet).

## Half-done

- `Assets/_Recovery/` (+ `.meta`): Unity's crash backup, not committed. Delete it in the Project window.
- `Tools/measure/out/combat.csv`: written by `combat.cs`, not committed.
- The real mouse clicking real buttons is unverified (trap 39): checks call `Picker.Click`,
  `PauseMenu.Pause/Resume/MainMenu`, `StartMenu.Open`, `ChestScreen.Take/Put`, and queue mouse
  events for the shoot guard.
- The screens have been looked at only as a UI layer on its own (trap 81), never over the game.
- Placeholder font is Unity's default (smooth, not pixel). A pixel font is a look decision for later.
- Old warning: `Pickup.All` hides `Interactable.All` (CS0108).

## Waiting on the user

- The playtests above.

## Don't

- Don't add a second HUD: demos feed `Hud.Status` / `Hud.Banner` (`ArenaGoal`, `RpgGoal`).
- Don't bring back test keys the picker replaced (F/G/H/L/C, 1-4 feels); add a picker row.
- Don't read gameplay mouse without the UI guard (`KitUI.PointerOverUI` at the press), or
  keys without `PauseMenu.IsPaused`.
- Don't recompile until `EditorApplication.isPlaying` reads false (trap 83).
- Checks: `movement.cs`, `facing.cs`, `camera.cs`, `screens.cs` on Playground; `combat.cs`,
  `playthrough.cs`, `bullet-alloc.cs` on ArenaShooter; `rpg-playthrough.cs` on LittleRpg (play
  mode); `inventory.cs` in edit mode. Afterwards check `runInBackground` is back to 0 (trap 77).

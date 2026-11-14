# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 4 (combat, Arena shooter demo).

## Do first

1. **The user plays the Arena shooter** (`Assets/Scenes/ArenaShooter.unity`, Play, ~5 min):
   do the weapons feel different and good; is 6 health / 1 s invulnerable fair; are
   enemies too fast/slow (3 tiles/s), too tanky (2-3 hits); is the shotgun recoil (8) fun or
   annoying; shake on hit (0.3) too much? Also still open from phases 1-3 (see LOG).
2. Then phase 5 of `Docs/ROADMAP.md`: interaction, pickups, inventory, chests, NPC text ->
   RPG room demo. Ammo pickups can call `Gun.Refill` (or a per-weapon version) then.

## Half-done

- `Assets/_Recovery/` (+ `.meta`): Unity's crash backup, not committed. Delete it in the Project window.
- Enemies have no pathfinding: a chaser behind a pillar stays stuck. Fine for the demo
  (spawns placed clear); a game that needs it adds it as an add-on.
- HUD is an OnGUI developer label; real screens are phase 6.
- Enemy art: one blob in three colours (pxkit `enemy.js`), no facing; enemy shots use the
  same bullet sprite tinted orange.
- Not measured: the real mouse path (checks drive `Gun` as data); the look in a capture.

## Waiting on the user

- The playtest above (and the earlier phases' playtest questions).

## Don't

- Don't replace `ObjectPool` with a custom pool, or drop it: measured (LOG 2026-11-14).
- Don't let `Warp` move anything but the camera's target (enemies have PlayerMovers).
- Don't read the mouse outside `PlayerAim` / `PlayerShooter`; `Gun` takes data.
- Don't time play-mode checks in frames (trap 78); end float timers with an epsilon (trap 79).
- Checks: `movement.cs`, `facing.cs`, `camera.cs` on TestArena; `combat.cs`,
  `playthrough.cs`, `bullet-alloc.cs` on ArenaShooter, all in play mode. After one, check
  `runInBackground` is back to 0 (trap 77).

# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 9 (combat additions).

## Do first

1. **Phase 10 of `Docs/ROADMAP.md` (enemy additions)** - pathfinding around walls, a boss with phases, groups, trail-leaving enemies - when the counselor starts it. Enemies can use the new weapon settings (a melee enemy = a `Gun` with a Melee weapon; `EnemyBrain` only aims and pulls the trigger, so a charging enemy needs the brain to release).
2. The playtests from `CONTEXT.md`, "What is not done" - now including Sword, Charger, Bouncer and aim assist.

## Half-done (for the phase 15 test pass)

- **Phase 9 was checked by one smoke run only** (LOG 2026-11-14): sword hit once for 2, full charge hit for 4, a Bouncer bullet came back off a wall, `AimAssist.Nudge` angles right. Not checked: bounce into corners (4 passes per step, rest of the step dropped), bounce off door ground, melee blocked by walls / passing over `ShootOver`, charge bullet art at x2.5, melee at 30 vs 144 fps, charge at 30 vs 144 fps (charge counts `dt` per step, should match).
- **Re-run `combat.cs`, `playthrough.cs`, `bullet-alloc.cs`** - `Gun.FixedUpdate` was restructured (same order for old weapons: cooldown, reload, fire, ammo, cooldown) and `Bullet.FixedUpdate` now loops; old weapons have kind Gun, charge 0, bounces 0. Recoil now pushes along the assisted aim (same as the raw aim while assist is 0).
- Unsure: melee knockback points from the wielder to the target (not along the swing); a zero-swing-time melee hits the whole arc in one step and shows its blade one frame; aim assist sees through walls and picks the nearest target, not the one closest to the aim line; Charger bullets grow but their sprite is the same 6 px ball scaled (pixel size differs from other art).
- `Gun` names `MeleeSwing` - phase 14's "delete each folder and compile" must move them together or add a hook.
- From before: camera check flake (Open layout, B -> C, once), broken crumbling in "blocks" mode, tiles closing on someone, `screens.cs` at WXGA, WebGL module + build, GitHub push, GIFs, `Assets/_Recovery/` to delete, real-mouse clicks unverified (trap 39), default font, warning `Pickup.All` hides `Interactable.All`.

## Waiting on the user

- Playtests (all feels, grid step, tiles, the phase 9 weapons and aim assist strengths). WebGL module install, repo push.

## Don't

- Don't write or run checks per phase: everything is tested in one pass at the end of round 2 (user, 2026-11-14). Keep a check only if it is vital.
- Don't add a second hit path for melee: swings go through `Health.Hit` like bullets.
- Don't put aim assist on the weapon or in `PlayerShooter`: it is a `Gun` setting applied at the shot, so touch input gets it too.
- Don't add tile code to movers, bullets or enemies: tiles block through `GroundMap`'s runtime colliders; bullets skip `ShootOver`.
- Don't add a second door system: door ground is a `TileDoor` switched by `RoomLock` / `Lever` / `LockedDoor`.
- Don't drive mouse presses through `Mouse.current` in checks - use your own virtual mouse (trap 84).
- Don't change the licence to MIT; don't push or add a remote without the user saying so.
- Don't recompile until `EditorApplication.isPlaying` reads false (trap 83).

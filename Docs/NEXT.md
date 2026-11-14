# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 10 (enemy additions).

## Do first

1. **Phase 11 of `Docs/ROADMAP.md` (world additions)** - procedural rooms from room pieces, destructible crates - when the counselor starts it. Enemies in generated rooms get paths for free (`PathWalker` reads colliders, no baking); a crate with a static collider blocks paths, a crate with a dynamic body does not.
2. The playtests from `CONTEXT.md`, "What is not done" - now including the boss (too easy / too hard?), group spacing and the trail's 1 damage a second.

## Half-done (for the phase 15 test pass)

- **Phase 10 was checked by one Arena bot run (ALL PASS, boss 8.2 s, 3 phases) and one 8 s Playground smoke run** (LOG 2026-11-14). No saved check for paths, groups or trails yet - write them in phase 15: a path round a pillar for free and grid walkers, "no path -> walks straight", avoid red on/off, temporary tiles return to plain, owner not hurt by its trail, boss phase switches at 60% / 30%.
- `Tools/measure/playthrough.cs` changed: the bot now walks round walls (`PathWalker` on the player at runtime) when its shot is blocked, and room E is allowed 60 s. Without that the bot timed out in E shooting into the middle wall.
- `EnemyBrain` now runs at execution order -20 (before `PlayerMover`): enemy timing may differ by one step from phase 4's numbers - re-run `combat.cs`.
- Unsure: path cost with many enemies (not measured; open-tile lookups are cached per step, A* capped at 6000 nodes); enemies still see through walls (sight range only), they just walk round; a grid-step enemy on a "Grid tap" feel stalls (holding the same direction is not a new press - use hold-to-walk feels); grid enemies need touch range ~1.5 (they stop on the next tile); followers can drift several tiles from their place when the leader turns (Wedge turns with the leader); the boss's charge weapon releases at exactly full charge (no fake-outs); the trail overlay fades to 25% then vanishes (no "last flash").
- Phase 14 notes: `EnemyBrain` names `PathWalker` and `EnemyGroup` (same folder, fine); `GridPath` reads `GroundMap` (Tiles) for hurting ground; `BossPhases` writes `Hud` (Screens); `GroundMap.PaintFor` is generic temporary ground (`TrailMaker` is its only caller).
- From before: phase 9 smoke-only items (bounce into corners, melee vs walls, charge at 30 vs 144 fps), re-run `combat.cs` and `bullet-alloc.cs`, camera check flake (Open layout, B -> C, once), broken crumbling in "blocks" mode, tiles closing on someone, `screens.cs` at WXGA, WebGL module + build, GitHub push, GIFs, `Assets/_Recovery/` to delete, real-mouse clicks unverified (trap 39), default font, warning `Pickup.All` hides `Interactable.All`.

## Waiting on the user

- Playtests (all feels, grid step, tiles, the phase 9 weapons, the boss and the phase 10 enemies). WebGL module install, repo push.

## Don't

- Don't write or run checks per phase: everything is tested in one pass at the end of round 2 (user, 2026-11-14). Keep a check only if it is vital.
- Don't add a navmesh: paths are `GridPath` over tiles, reading the same colliders movement uses (LOG 2026-11-14).
- Don't give enemies tile code: they avoid hurting ground through `GridPath.Hurts`, and trails are ordinary `GroundTile`s laid with `GroundMap.PaintFor`.
- Don't make the trail kill by default (`Trail` hurts 1/s; Red is a drop-in if a game wants it).
- Don't add a second hit path for melee: swings go through `Health.Hit` like bullets.
- Don't put aim assist on the weapon or in `PlayerShooter`: it is a `Gun` setting applied at the shot.
- Don't add a second door system: door ground is a `TileDoor` switched by `RoomLock` / `Lever` / `LockedDoor`.
- Don't drive mouse presses through `Mouse.current` in checks - use your own virtual mouse (trap 84).
- Don't change the licence to MIT; don't push or add a remote without the user saying so.
- Don't recompile until `EditorApplication.isPlaying` reads false (trap 83).

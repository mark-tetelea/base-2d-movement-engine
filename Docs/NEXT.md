# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 11 (world additions).

## Do first

1. **Phase 12 of `Docs/ROADMAP.md` (feel additions)** - hit-stop, particles, sound slots, day/night tint - when the counselor starts it. Crates and room doors are new sound/particle events to give slots (crate break, door shut/open).
2. The playtests from `CONTEXT.md`, "What is not done" - now including the Dungeon (room size, waves, crate drop rate).

## Half-done (for the phase 15 test pass)

- **Phase 11 was checked by** `Tools/measure/dungeon-layouts.cs` (edit mode, ALL PASS: 200 seeds at 7 rooms + 50 at each count 2-12; every piece's doors connect) and `dungeon-play.cs` (play mode, ALL PASS on 3 random seeds), plus one manual run (lock, waves, door reopen, crate drop, coins, win banner). Keep both checks; re-run them after any change to a room piece or the layout rule. Not yet checked: Fade and Open camera layouts in the Dungeon (only slide seen), bullets stopping on a crate (not watched), Restart from the pause menu giving a new layout (by design: seed 0), load time with many rooms.
- Unsure: enemies in a waiting wave of a room next door are switched off, so they never see through walls - but loose enemies put in a piece outside a wave would chase through doors from the next room (put enemies in waves); aim assist bends shots toward crates (team 2 is "another team") - may want crates ignored by aim assist; enemies walk into the Pool's red corners only if chasing straight (PathWalker avoids hurting ground on paths, not on straight runs - same as Playground); `dungeon-play.cs` counts crates as walkable (they break), so a ring of crates could hide a sealed pocket - none in the kit's pieces.
- Layouts are trees (no loops); a "loop chance" setting is an idea, not built. Pieces bigger than one cell must be whole rectangles (no L shapes).
- Phase 14 notes: World depends on Camera (`Room`, `RoomLock`), Combat (`Health`, `Gun`, `RoomWaves`), Enemies (`EnemyBrain`, `BossPhases`), Tiles (`GroundTile`, `TileDoor`), Inventory (`Bag`, `Item`, for `DungeonGoal`), Screens (`Hud`). `Drops` is generic (any Health) and may belong with Combat/Inventory when 13b connects enemy drops. `StartMenu.cs` defaults now list Dungeon.
- From before: phase 10 smoke-only items (paths, groups, trails, boss phase switches), phase 9 smoke-only items, re-run `combat.cs` and `bullet-alloc.cs`, camera check flake (Open, B -> C, once), broken crumbling in "blocks" mode, tiles closing on someone, `screens.cs` at WXGA, WebGL module + build, GitHub push, GIFs, `Assets/_Recovery/` to delete, real-mouse clicks unverified (trap 39), default font, warning `Pickup.All` hides `Interactable.All`.

## Waiting on the user

- Playtests (all feels, grid step, tiles, phase 9 weapons, boss and phase 10 enemies, the Dungeon). WebGL module install, repo push.

## Don't

- Don't paint generated rooms into the scene by hand or give pieces their own colliders: pieces are stamped into the level's one floor and one wall tilemap at `Awake`.
- Don't add a second door system for generated rooms: doors are door ground + a `TileDoor` per room half, on that room's `RoomLock`.
- Don't make a Crate class: a crate is a prefab (sprite, static collider, `Health`, `Drops`).
- Don't write or run checks per phase beyond the vital ones (user, 2026-11-14).
- Don't add a navmesh; don't give enemies tile code; don't make the trail kill by default; don't add a second melee hit path; don't put aim assist on the weapon (all from phase 10 and earlier).
- Don't drive mouse presses through `Mouse.current` in checks (trap 84). Don't recompile until `EditorApplication.isPlaying` reads false (trap 83).
- Don't change the licence to MIT; don't push or add a remote without the user saying so.

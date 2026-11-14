# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 8 (ground tiles, grid-step).

## Do first

1. **Phase 9 of `Docs/ROADMAP.md` (combat additions)** - melee swing, charge shots, bouncing bullets, aim assist - when the counselor starts it. Bouncing bullets must keep flying over `ShootOver` (blue ground) and stop on "Ground blocks" (closed doors).
2. The playtests from `CONTEXT.md`, "What is not done" - now including grid step and the tile garden.

## Half-done

- **Camera check failed once after phase 8 - check in the final test pass.** `camera.cs`, Open layout, "walk B -> C and up the big room": the player stopped at y 17.54 (x 39.5) until the 15 s timeout; no collider anywhere along x 39.5, y 16-20; 7849 per-tick checks against 5668 on the passing run. The same run passed 11/11 straight after. Not chased (user's call: testing waits for the final pass).
- Not checked: broken crumbling tiles in "blocks" mode, and a tile or door closing on someone standing on it (physics should push them out). Same collider path as closed doors, which is checked.
- `screens.cs` was not re-run at WXGA 1366x768 this phase (UI code unchanged).
- No demo uses the tiles (the brief allowed it only if natural). A later demo could use red trails (phase 10's "enemies that leave a trail").
- From before: WebGL module + build, GitHub repo and push, GIFs, `Assets/_Recovery/` to delete, real-mouse clicks unverified (trap 39), default font, warning `Pickup.All` hides `Interactable.All`.

## Waiting on the user

- Playtests (all feels, grid step, tiles). WebGL module install, repo creation and push.

## Don't

- Don't write or run checks per phase: everything is tested in one pass at the end of round 2 (user, 2026-11-14). Keep a check only if it is vital - a silent failure would break other systems or the demos (e.g. grid steps landing exactly on tile centres, in `tiles.cs`).
- Don't add tile code to movers, bullets or enemies: tiles block through `GroundMap`'s runtime colliders; bullets skip `ShootOver`. A new tile kind is a new `GroundTile` asset.
- Don't add a second door system: door ground is a `TileDoor` object switched by `RoomLock` / `Lever` / `LockedDoor`.
- Don't drive mouse presses through `Mouse.current` in checks - use your own virtual mouse (trap 84).
- Don't change the licence to MIT; don't push or add a remote without the user saying so.
- Don't recompile until `EditorApplication.isPlaying` reads false (trap 83).

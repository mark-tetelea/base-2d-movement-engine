# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 8 (ground tiles, grid-step).

## Do first

1. **Phase 9 of `Docs/ROADMAP.md` (combat additions)** - melee swing, charge shots, bouncing bullets, aim assist - when the counselor starts it. Bouncing bullets must keep flying over `ShootOver` (blue ground) and stop on "Ground blocks" (closed doors).
2. If the camera check's Open-layout "walk B -> C" fails again (it did once this session, stopped at y 17.5 with nothing solid there, then passed 11/11), find out why before trusting it.
3. The playtests from `CONTEXT.md`, "What is not done" - now including grid step and the tile garden.

## Half-done

- Not checked: broken crumbling tiles in "blocks" mode, and a tile or door closing on someone standing on it (physics should push them out). Same collider path as closed doors, which is checked.
- `screens.cs` was not re-run at WXGA 1366x768 this phase (UI code unchanged).
- No demo uses the tiles (the brief allowed it only if natural). A later demo could use red trails (phase 10's "enemies that leave a trail").
- From before: WebGL module + build, GitHub repo and push, GIFs, `Assets/_Recovery/` to delete, real-mouse clicks unverified (trap 39), default font, warning `Pickup.All` hides `Interactable.All`.

## Waiting on the user

- Playtests (all feels, grid step, tiles). WebGL module install, repo creation and push.

## Don't

- Don't add tile code to movers, bullets or enemies: tiles block through `GroundMap`'s runtime colliders; bullets skip `ShootOver`. A new tile kind is a new `GroundTile` asset.
- Don't add a second door system: door ground is a `TileDoor` object switched by `RoomLock` / `Lever` / `LockedDoor`.
- Don't drive mouse presses through `Mouse.current` in checks - use your own virtual mouse (trap 84).
- Don't change the licence to MIT; don't push or add a remote without the user saying so.
- Don't recompile until `EditorApplication.isPlaying` reads false (trap 83).

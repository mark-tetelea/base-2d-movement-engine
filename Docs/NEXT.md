# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 3 (camera and rooms).

## Do first

1. **The user playtests phases 1-3** in `Assets/Scenes/TestArena.unity` (Play):
   - Phases 1-2 as before (keys 1-4, Space, K, F/G/H).
   - Phase 3: L cycles Open / Rooms slide / Rooms fade. Walk right into room B (doors
     lock; C clears), up into big room C, onto the brown pad for area D. K shakes.
     Questions: dead zone / smoothing / look-ahead feel; slide 0.5 s and fade 0.25 s;
     should keys pause during a slide; is shake (0.4 tiles) too much at high fps (it
     jitters every frame); is shake showing past room edges OK.
2. Then phase 4 of `Docs/ROADMAP.md` (interaction).

## Half-done

- `Assets/_Recovery/` (+ `.meta`): Unity's crash backup, not committed. Delete it in the Project window.
- Not measured: dash and knockback into walls (`PlayerMover.TakeWallLoss`).
- Fade quad is a runtime `SpriteRenderer` at sorting order 32767; once real UI exists it may belong there.

## Waiting on the user

- The playtest above. Still open: wall-slide speed (4.24 tiles/s).

## Don't

- Don't move the camera into `FixedUpdate`; it follows the interpolated player in `LateUpdate`.
- Don't clamp shake to rooms without asking (decided against, LOG 2026-11-14).
- Don't time play-mode checks in frames (trap 78).
- Don't read the mouse outside `PlayerAim`; don't push knockback into `Movement.Step`.
- Checks: `Tools/measure/movement.cs`, `facing.cs`, `camera.cs` in play mode. After one, put `runInBackground` back (trap 77).

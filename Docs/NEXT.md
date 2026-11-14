# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 2 (facing and aim).

## Do first

1. **The user playtests phases 1 and 2** in `Assets/Scenes/TestArena.unity` (Play):
   - Phase 1 list: keys 1-4 feels, Space dash, K knockback, the top strip zones.
   - Phase 2: walk all 8 ways; F (4/8 facings), G (face walk / aim), H (aim from mouse / walk).
     Is the walk cycle (8 fps) too fast? Does 4-facing diagonal behaviour feel right?
2. Then phase 3 of `Docs/ROADMAP.md` (camera and world layout).

## Half-done

- `Assets/_Recovery/` (+ `.meta`): Unity's crash backup, not committed. Delete it in the
  Project window.
- Not measured: dash and knockback into walls (`PlayerMover.TakeWallLoss`).

## Waiting on the user

- The playtest above. Still open: wall-slide speed (4.24 tiles/s).

## Don't

- Don't add an Animator for idle/walk; `DirectionalSprites` swaps sprites.
- Don't read the mouse outside `PlayerAim`; aim is data.
- Don't re-measure by argument: `Tools/measure/movement.cs` and `facing.cs` in play mode.
- After a check that sets `Application.runInBackground`, put the Player setting back (trap 77).
- Don't push knockback into `Movement.Step`; don't move dash into the core mover.

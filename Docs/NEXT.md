# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 1 (movement add-ons).

## Do first

1. **The user playtests phase 1** in `Assets/Scenes/TestArena.unity` (Play). Things to try:
   - Keys **1-4**: Instant, Glide, Ice, Heavy. Ice and Heavy values are guesses.
   - **Space** dash (3 tiles, 0.2 s, 0.4 s cooldown) with each feel. Too short/long/slow?
     Should Glide keep more speed after a dash (now capped at top speed)?
   - **K** with the mouse beside the player: is 14 tiles/s a good test hit? Does Ice slide
     too far (push stop 12), Heavy too little (80)?
   - Walk over the top strip: speed pad (x1.6), mud (x0.5), ice patch.
   Tune in `Assets/Data/Movement/*.asset` and the Player's `Dash` component; play-mode
   changes to the assets are kept, to the component are not (trap 3).
2. Then phase 2 of `Docs/ROADMAP.md`.

## Half-done

- `Assets/_Recovery/0.unity` (+ `.meta`): Unity's crash backup from opening the editor; it
  is the same TestArena. Not committed. Delete it in the Project window (deleting it from
  this session was blocked).
- Not measured: dash and knockback into walls (the wall-loss split in
  `PlayerMover.TakeWallLoss`). Looks right by reasoning only.

## Waiting on the user

- The playtest above. Also still open from 2026-10-18: wall-slide speed (4.24 tiles/s).

## Don't

- Don't push knockback into `Movement.Step`'s velocity; use `PlayerMover.AddPush`.
- Don't move dash into the core mover: it is an add-on driving `DriveThisStep`.
- Don't re-measure by argument - re-run `Tools/measure/movement.cs` (play mode, TestArena)
  and `node Tools/measure/analyse-movement.js` after any movement change (~90 s).
- Don't switch to the template's `InputSystem_Actions` asset (gamepad bindings).

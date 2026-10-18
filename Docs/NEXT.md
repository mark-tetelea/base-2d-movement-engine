# Next

> Overwritten at every `/handoff`. Written 2026-10-18, end of the movement session.

## Do first

1. **The user plays the test arena and judges the feel.** Open
   `Assets/Scenes/TestArena.unity`, press Play, move with WASD or the arrows; keys **1**
   (Instant) and **2** (Glide) swap the feel, and the top-left label names it. Tune by
   editing `Assets/Data/Movement/Instant.asset` / `Glide.asset` - changes made during play
   are kept. Questions for the user, one at a time:
   - Do Instant and Glide feel right? Starting values are guesses (6 tiles/s; Glide 0.4 s
     to full speed, 0.8 s to stop).
   - Sliding diagonally along a wall moves at 4.24 tiles/s (only the along-wall part of
     the diagonal). Keep that, or slide at full speed? Full speed would be a change to
     `PlayerMover` (project the direction onto the wall), not a setting - ask first.
2. **Then the next item on the kit's list: facing and animation** (`CONTEXT.md`). Ask the
   open question first: 8-direction art as eight drawings, or five with mirroring. Aim is
   separate from walking.
3. Boosts, slowdowns and knockback are designed but not built (LOG 2026-10-18, "How
   boosts, slowdowns and knockback will push"). Build them with their first real caller -
   knockback probably with shooting.

## Half-done

- Nothing. Not checked yet: the arena on the WXGA (1366x768) Game view size, where 640x360
  does not scale by a whole number - Windowbox should show black bars (trap 32's test).

## Waiting on the user

- The playtest above: feel of Instant and Glide, and the wall-slide speed.

## Don't

- Don't re-measure frame-rate independence by argument - re-run `Tools/measure/movement.cs`
  (in play mode on TestArena) and `node Tools/measure/analyse-movement.js` after any change
  to movement. It takes about a minute and checks 30 vs 144 fps, 8 directions, the tap
  latch and walls.
- Don't push knockback into `Movement.Step`'s velocity: an Instant feel cancels it in one
  step. It gets its own decaying velocity, added after `Step`.
- Don't switch the player to the template's `InputSystem_Actions` asset - it brings
  gamepad bindings, and the kit has no gamepad.
- Don't paint tilemaps in the same script before `OpenScene` (trap 73), and toggle the
  wall collider after painting (trap 74).
- Don't draw all eight player directions before one has been seen in the game.

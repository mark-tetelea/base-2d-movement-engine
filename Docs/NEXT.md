# Next

> Overwritten at every `/handoff`. Written 2026-10-18, end of the kickoff.

## Do first

1. **Movement, in a fresh chat.** Ask the user the open question first: do the feel
   settings live in a plain settings class on the component, or in one settings asset with
   presets ("instant", "glide")? Then build, in this order:
   - the movement rule: one static function (velocity, move direction, settings, dt) ->
     new velocity, never reading `Time.deltaTime` or input (Decisions 1, 2, 2a);
   - a player that reads keyboard input into a direction in `Update`, latches it, and
     applies the rule in `FixedUpdate` on a Rigidbody2D with interpolation on;
   - a test-arena scene: Pixel Perfect Camera at 640x360 / 16 PPU (traps 30-32), floor and
     wall placeholders and a player placeholder drawn with pxkit (`TO-DRAW.md`).
2. Then judge instant vs glide **by playing it** - that is a playtest for the user, not
   something to tune by measuring alone. Measure only what can be measured (distance per
   second at 30 vs 144 fps should match - Decision 1's promise).

## Half-done

- Nothing.

## Waiting on the user

- The settings-shape question above, at the start of the movement chat.

## Don't

- Don't look for MCP for Unity on port 8080 - the `unity` command line is the connection.
  Check which project answered when Jewelery Shop is open too (it holds 7800).
- Don't set the fixed timestep with `floatValue` (trap 71). It is already 1/60.
- Don't add managers, an event bus, a config system or pooling - not until three real
  callers exist (`CLAUDE.md`, How we work).
- Don't draw all eight player directions before one has been seen in the game.

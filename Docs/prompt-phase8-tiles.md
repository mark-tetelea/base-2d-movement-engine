# Brief: phase 8 - ground tiles + grid-step movement

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 8 of `Docs/ROADMAP.md` ("Round 2").

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (round 2,
including phase 14 - build phase 8 so it already fits "everything swappable"); search
`Docs/LOG.md` and `Docs/TRAPS.md` before risky work (tilemap traps 73-74). The user skipped
playtesting so far - don't block on it.

Scope (feel differences are settings, extras are add-on pieces, one copy of each rule):

1. **Tile types, each a small asset** (art + behaviour settings), painted on a tilemap:
   - **Normal** ground.
   - **Red**: kills whoever stands on it (uses phase 4's `Damage`/`Health`; whether it
     kills or deals damage per second is a setting; dash invulnerability: decide and log).
   - **Blue**: cannot be stepped on - blocks walking, but bullets and sight pass over it.
   - **Crumbling**: breaks a set time after you step on it (visible warning), comes back
     after a set time; while broken it acts as a hole (a setting: blocks, or kills like red).
   - **Door ground**: opens/closes on a switch/lever, a key, or a cleared room - reuse
     `RoomLock`, `Lever`, `LockedDoor` rather than adding a second door system.
   A game adds a new tile type by making a new asset, not by writing code where possible.
2. **Grid-step movement**: one tile per press, as a movement setting alongside Instant /
   Glide / Ice / Heavy. Settings: step time (or frames per step) for smoothness, hold to
   keep walking, 4 or 8 directions. It must respect the tile types (no stepping onto blue,
   etc.). Free movement and grid-step use the same tile rules.
3. Put every tile type in the Playground (and a Picker row for grid-step); use one or two
   in a demo only if it fits naturally.
4. Placeholder art via pxkit (`draw-sprite`), per `ART.md`. Update `TO-DRAW.md`.

Done means: compiles with 0 errors; all earlier checks and both demo bot runs still pass;
new checks: each tile type's rule (red kills, blue blocks walking but not bullets,
crumble timing breaks/returns at the set times, door ground opens on each trigger), and
grid-step lands exactly on tile centres at 30 and 144 fps; finish with `/handoff` (it
commits; include this brief and the `Docs/ROADMAP.md` changes). Do not push.

Feel needs the user's playtest - ship starting values. If something is the user's call,
stop and ask; the counselor chat relays it. Plain words, short.

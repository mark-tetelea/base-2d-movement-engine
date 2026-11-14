# Brief: phase 11 - world additions

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 11 of `Docs/ROADMAP.md` ("Round 2").

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (round 2,
including phase 14 - build so it already fits "everything swappable").

Scope:

1. **Procedural rooms**: a new layout every run, built from hand-made room pieces
   (prefabs or small tilemaps with marked door spots). Settings: number of rooms, seed
   (0 = random each run, fixed = same layout every time), which pieces are allowed, a
   start room and an end/boss room. Rooms connect through doors and use phase 3's `Room`
   + camera layouts (works with slide and fade) and `RoomLock`/`RoomWaves`. Enemies use
   phase 10 pathfinding. Adding a new room piece = making a new piece, no code.
2. **Destructible crates**: an add-on - a `Health` on a crate; when it breaks it can drop
   items (phase 5 `Pickup`), with a drop table as a setting. Blocks walking and bullets
   until broken.
3. A small **dungeon scene** that uses the generator, reachable from the start menu, so it
   can be tried. Keep it modest; it may grow into the third demo later.
4. Placeholder art via pxkit.

**Testing (user, 2026-11-14):** tests only when vital. Make sure it compiles with 0 errors
and the scenes open. One vital check here: the generator must always make a finishable
level - generate e.g. 200 seeds in edit mode and check every room is reachable from the
start and the end room exists. Note anything you were unsure of in `Docs/NEXT.md` for the
phase 15 test pass.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

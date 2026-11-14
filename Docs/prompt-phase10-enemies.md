# Brief: phase 10 - enemy additions

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 10 of `Docs/ROADMAP.md` ("Round 2").

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (round 2,
including phase 14 - build so it already fits "everything swappable").

Scope (enemies keep using the same movement, damage and tile rules as the player):

1. **Pathfinding**: enemies walk around walls, blue tiles and closed door ground instead
   of getting stuck behind pillars. Keep it plain: a grid path search (A* or BFS) over
   the tile grid, re-planned on a timer - no navmesh package unless that is clearly
   simpler; log the choice. Works for free movement and grid-step enemies. Settings:
   re-plan interval, give-up distance; enemies avoid red tiles by default (a setting).
2. **Boss with phases**: an add-on - phases switch at health thresholds; each phase
   swaps behaviour/weapon settings (reuse `EnemyBrain` behaviours and phase 9 weapons).
   A simple boss health bar on the shared `Hud`. Put a boss room in the Arena shooter.
3. **Groups**: enemies that move together (a leader + followers keeping spacing), as an
   add-on.
4. **Trail enemy**: leaves a fading trail of red ground behind it (reuse phase 8's tile
   rules, temporary tiles).
5. Placeholder art via pxkit.

**Testing (user, 2026-11-14):** tests only when vital. Make sure it compiles with 0 errors
and the scenes open. One vital check here: the Arena shooter must still be clearable
(re-run its bot `Tools/measure/playthrough.cs` once - the boss and pathfinding change it).
Note anything you were unsure of in `Docs/NEXT.md` for the phase 15 test pass.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

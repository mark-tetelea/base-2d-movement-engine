# Brief: phase 15 - full test pass

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-15 by the counselor chat.

Run phase 15 of `Docs/ROADMAP.md`: the one full test pass after round 2 (phases 8-14 were
built with only "vital" tests, by the user's choice).

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md` (it lists what each phase was
unsure of), `Docs/ROADMAP.md`. Search `Docs/TRAPS.md` before driving play mode.

Do:

1. **Re-run every existing check** in `Tools/measure/` (movement + analyse, facing,
   camera, combat 30 vs 144 fps, bullet alloc, tiles, screens at 1920x1080 and WXGA,
   inventory, save, removal check) and all demo bots (Arena shooter, RPG room, Dungeon).
   Fix what broke - the camera check failed once after phase 8; find out why.
2. **Add checks for round-2 systems that have none yet**, keeping each short: grid step,
   crumbling/door tiles, melee/charge/bounce, aim assist, pathfinding + boss phases +
   groups + trail, hit-stop vs the fixed step, shop buy/sell, minimap reveal, per-item
   pickup modes and dropping, rebinding a key, touch input path (driven as data).
3. Work through every "unsure" note in `Docs/NEXT.md`.
4. Report plainly: what passed, what was fixed, what still can't be verified without a
   human (real mouse, real finger, feel, sound).

The user has not playtested anything yet; feel is out of scope. If the counselor sends a
font choice for the default theme, apply it (`Resources/KitTheme`) as part of this pass.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

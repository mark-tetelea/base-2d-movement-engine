# Brief: phase 5 - interaction, pickups, inventory -> RPG room demo

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 5 of `Docs/ROADMAP.md` (sections 4 and 5, and the Little RPG room demo).

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md`; search
`Docs/LOG.md` and `Docs/TRAPS.md` before risky work (UI Toolkit traps 16-21, 32-38 if you
build UI). The user skipped playtesting so far - don't block on it.

Scope (feel differences are settings, extras are add-ons, one copy of each rule):

1. **Interaction**: one key (E) uses the best nearby target - one shared system.
   Settings: reach in tiles, "best" = closest or the one you face (phase 2 facing), press
   or hold to use. A small "E" prompt over the current target.
2. **Targets as add-on pieces**: chest, NPC, floor item, door/lever.
3. **NPC text**: a simple text box, lines from an asset, E to advance; player input
   paused while it is open.
4. **Pickups**: automatic (coins, ammo, health) with a magnet reach setting; manual
   (weapons, key items) through interaction. Ammo/health hook into phase 4's systems.
5. **Inventory**: size, stack sizes, hotbar slot count as settings; number keys pick the
   hotbar slot; drop an item back on the floor. **Chests are inventories too** - same
   system, a simple transfer screen.
6. **Little RPG room demo scene**: Instant feel, 4 facings, rooms with fade (phase 3), a
   couple of NPCs, chests, a key in one chest that opens a locked door, some coins.

Done means: compiles with 0 errors; earlier checks (movement, facing, camera, combat)
still pass; a check of the inventory rule (stacking, full inventory, chest transfer) and
of best-target picking; the demo is playable start to finish (a bot run like phase 4's);
finish with `/handoff` (it commits; include this brief).

Feel needs the user's playtest - ship starting values. If something is the user's call,
stop and ask; the counselor chat relays it. Plain words, short.

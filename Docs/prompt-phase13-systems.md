# Brief: phase 13 - systems: save/load, shop, minimap, touch

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 13 of `Docs/ROADMAP.md` ("Round 2"). Phase 13b (item pickup pass) is a
separate session after this one - don't do it here, but leave the shop ready for it.

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (round 2,
including phase 14). **Read traps 46-50 in `Docs/TRAPS.md` before any save code.** The
user asked for saving and touch controls: take them off `CLAUDE.md`'s "Do NOT build yet"
and log that.

Scope (settings and add-ons, one copy of each rule):

1. **Save and load**: an add-on. What gets saved is opt-in per system (player position
   and health, inventory/bag, chests, coins, opened doors and cleared rooms, current
   scene, settings). Versioned save shape, every field required, a backup before
   overwriting (traps 46-50). Save slots count as a setting. Save/Load in the pause menu.
2. **Coin shop**: a shop is an interactable (phase 5) that opens a buy/sell screen on the
   shared panel; stock is an asset (item, price, amount); sell price as a setting. Uses
   the same `Inventory` rule. Put one shop in the RPG room.
3. **Minimap**: a setting - off, corner map, or full map on a key. Shows rooms (phase 3 /
   phase 11 generated rooms), the player, optionally enemies and items; rooms appear
   when visited (a setting).
4. **Touch controls**: on-screen move stick, aim stick, and buttons (shoot, dash,
   interact) feeding the same input-as-data path. Shown automatically on touch devices,
   or forced on by a setting for testing on PC. Do not add Android/iOS build targets.

**Testing (user, 2026-11-14):** tests only when vital. Make sure it compiles with 0 errors
and the scenes open. One vital check here: save then load round-trips every saved system
exactly, and an old or broken save file does not crash or lose progress (traps 46-50).
Note anything else you were unsure of in `Docs/NEXT.md` for the phase 15 test pass.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

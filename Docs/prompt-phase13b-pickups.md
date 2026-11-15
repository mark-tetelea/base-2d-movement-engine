# Brief: phase 13b - item pickup pass

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, medium
> effort). Written 2026-11-14 by the counselor chat.

Build phase 13b of `Docs/ROADMAP.md` ("Round 2").

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (round 2,
including phase 14). Phase 5 already built auto/manual pickups, the magnet (`Collector`),
chests and dropping; phase 13 built the shop; phase 11 crate drops. Extend those - don't
add a second pickup system.

Scope (the user's words: "walk on it / come to it and press interact / like a magnet
attracted to you, from chests or shop or whatever, also in the inventory they should be
droppable"):

1. **Pickup mode is a per-item setting** on the item asset: walk over it, come close and
   press interact, or pulled in by a magnet (magnet reach/speed per item, falling back to
   the player's `Collector` settings). A scene object may override the item's default.
2. **Every source gives items the same way**: floor, chests, the shop, enemy drops (add a
   drop table add-on to enemies, same shape as the crate one), crates.
3. **Drop from the inventory**: every item can be dropped from the bag/hotbar back onto
   the floor (a setting per item can forbid it, e.g. quest items); dropped items use their
   pickup mode, with a short delay so they aren't picked straight back up.
4. Show each mode in the Playground (one item of each kind) and fix the demos to use it
   naturally.

**Testing (user, 2026-11-14):** tests only when vital. Make sure it compiles with 0 errors
and the scenes open; the save check from phase 13 must still pass if item data changed
shape. Note anything you were unsure of in `Docs/NEXT.md` for the phase 15 test pass.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

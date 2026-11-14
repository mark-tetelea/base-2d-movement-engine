# Brief: phase 12 - feel additions and sounds

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, medium
> effort). Written 2026-11-14 by the counselor chat.

Build phase 12 of `Docs/ROADMAP.md` ("Round 2").

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (round 2,
including phase 14 - build so it already fits "everything swappable").

Scope (every item is a setting and can be turned off):

1. **Hit-stop**: a tiny freeze on hits; length per hit kind as a setting (0 = off). It
   must not break the fixed 60 Hz step rules in `CLAUDE.md` - log how it is done.
2. **Particles**: small effects for hits, deaths, dash, crate breaks, pickups, crumbling
   tiles. Each effect is a slot a game can fill with its own effect or leave empty.
3. **Sounds - swappable**: each event (step, shot, melee, hit, death, pickup, door,
   crumble, dash, UI click, ...) points at a sound slot; a game fills it with its own clip
   or leaves it empty for silence. Volume per slot and master/music/effects volume as
   settings. Ship a few simple placeholder sounds (generated in code or tiny clips, no
   copyrighted audio) so the demos aren't silent.
4. **Day/night tint**: a setting - off, fixed tint, or a cycle with a length.
5. Rows for these in the Playground picker.

**Testing (user, 2026-11-14):** tests only when vital. Make sure it compiles with 0 errors
and the scenes open. Note anything you were unsure of in `Docs/NEXT.md` for the phase 15
test pass.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

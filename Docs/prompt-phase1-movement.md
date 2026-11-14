# Brief: phase 1 - movement add-ons

> Paste this as the first message of a new chat opened in `Base_2D_movement_engine`
> (Opus 5.5, high effort). Written 2026-11-14 by the counselor chat.

Build phase 1 of `Docs/ROADMAP.md`: the movement add-ons.

Instant and Glide are BOTH kept as options - the user confirmed this; never treat the
playtest as picking one. A separate "counselor" chat runs the phases and passes your
questions to the user.

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md` and `Docs/ROADMAP.md`, and
search `Docs/LOG.md` for "How boosts, slowdowns and knockback will push" (the design
already agreed on). Follow `CLAUDE.md`'s rules strictly: the design test (feel = setting,
extras = optional add-on pieces), one copy of each rule, input arrives as data,
everything in `FixedUpdate` taking `dt`, numbers in tiles, no managers or config systems.

Scope:

1. **Ice** and **Heavy** feel presets: new `MoveFeel` assets only, with starting values,
   reachable from the arena's feel keys.
2. **Dash / dodge roll**: an add-on component. Settings for distance (tiles), duration,
   cooldown, and whether you're invulnerable during it (store the flag now; damage comes
   in phase 4). Space in the test arena.
3. **Knockback / recoil**: its own decaying velocity, added after `Movement.Step` (NOT
   pushed into `Step`'s velocity, see `Docs/NEXT.md` "Don't"). One static rule enemies
   can use later. A test key in the arena (for example: knock the player away from the
   mouse).
4. **Boosts and slowdowns**: floor zones in the arena (speed pad, mud, an ice patch that
   swaps the feel), built the way the LOG design says.

Done means: it compiles cleanly; the measuring harness still passes (re-run
`Tools/measure/movement.cs` + `analyse-movement.js`) and is extended to check dash
distance and that knockback decays the same at 30 and 144 fps; the arena has all of it to
try; finish with `/handoff` (it commits - include the already-changed `Docs/ROADMAP.md`,
`Docs/IDEAS.md` and this brief).

Feel needs the user's playtest: ship starting values and list what to try, don't retune
by guess. If something is the user's call (feel, scope, a locked decision), stop and ask.
Plain words, short.

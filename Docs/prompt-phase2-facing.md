# Brief: phase 2 - facing, aim and animation

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 2 of `Docs/ROADMAP.md`: facing, aim and animation.

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` and
`ART.md`; search `Docs/LOG.md` and `Docs/TRAPS.md` before risky work.

**Decided by the user (2026-11-14):** 8-direction art is **5 drawings + mirroring** (up,
up-right, right, down-right, down; the left side is the right side flipped). Make it a
setting so a game can supply real left-side drawings and get true 8 drawings instead.
Log this decision and move the open question out of `CONTEXT.md`.

Scope (each difference is a setting, per the design test):

1. **Facing**: 4 or 8 directions. Face where you walk, or face where you aim.
2. **Aim, separate from walking**: from the mouse (world position under the cursor), or
   from the walk direction. Aim arrives as data (a direction), like movement input - the
   rule never reads the mouse. Keep the last facing when standing still.
3. **Animation**: idle and walk per direction, from the facing, with flip for the
   mirrored side. Keep it plain (sprite swap / Animator only if it earns it) - no
   framework.
4. **Placeholder art**: Claude draws it in code with `Tools/pxkit` (the `draw-sprite`
   skill), per `ART.md` - the user draws nothing for the kit. Draw ONE direction first,
   check it in the game, then the other four (`Docs/NEXT.md` "Don't"). Update `TO-DRAW.md`.
5. A small aim marker in the test arena so aim is visible, and a key to switch the
   facing settings for testing.

Done means: it compiles cleanly; the movement harness still passes; facing is checked
for all 8 directions (including that the left ones flip) and for aim-vs-walk; finish
with `/handoff` (it commits; include this brief).

Feel needs the user's playtest - ship starting values. If something is the user's call
(feel, scope, a locked decision), stop and ask; the counselor chat relays it. Plain words,
short.

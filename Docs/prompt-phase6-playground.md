# Brief: phase 6 - Playground picker and the screens

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 6 of `Docs/ROADMAP.md`: the Playground demo and the shared screens.

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md`; search
`Docs/TRAPS.md` for the UI Toolkit and Pixel Perfect traps (16-21, 32-38) before any UI.
The user skipped playtesting so far - don't block on it.

Scope:

1. **Playground scene**: built from the test arena. An on-screen picker (mouse-driven
   panel, toggled with Tab) that switches every choice the kit has, live: movement feel,
   dash on/off, facing 4/8, face walk/aim, aim source, camera layout, weapon, interaction
   "best" rule, magnet reach, etc. It REPLACES the scattered test keys where it can -
   delete what it replaces rather than keeping both.
2. **Shared screens** (one set, used by both demos and the Playground): health bar,
   hotbar, ammo, interaction prompt, pause menu (Esc; `Time.timeScale = 0`, UI unscaled).
   Unify whatever phases 4-5 built ad hoc; don't keep copies.
3. **A small start menu** that opens Playground, Arena shooter or RPG room, and a way
   back to it from each - this is what a browser build will show first.

Done means: compiles with 0 errors; all earlier checks and the two demo bot runs still
pass; UI checked at 1920x1080 AND WXGA 1366x768 (trap 32); a picker click does not also
fire gameplay (trap 21); finish with `/handoff` (it commits; include this brief).

If something is the user's call, stop and ask; the counselor chat relays it. Plain
words, short.

# Brief: phase 3 - camera and world layout

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 3 of `Docs/ROADMAP.md`: camera and world layout.

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (section 3);
search `Docs/LOG.md` and `Docs/TRAPS.md` before risky work (Pixel Perfect Camera traps
30-32 especially).

The user skipped playtesting phases 1-2 for now - don't block on it.

Scope (layout is a setting on one camera script; room pieces are add-ons):

1. **Open**: camera follows the player - smoothing, dead zone, look-ahead toward the aim
   (phase 2's aim direction). Stays pixel-perfect (no shimmer).
2. **Rooms, slide** (Isaac / Zelda style): a room is a marked rectangle in the scene; the
   camera stays inside the current room and slides to the next when the player crosses.
3. **Rooms, fade** (Undertale style): crossing a door fades out, moves camera/player to
   the next room or area, fades in. Player input paused during the fade.
4. Rooms bigger than the screen: follow inside, clamped to the room edges.
5. **Add-on: doors that lock until the room is cleared.** No enemies exist yet - build
   the door piece with a plain "room cleared" call, and a test key to clear it.
6. **Screen shake** setting (call it from knockback in the arena for now).
7. Extend the test arena (or add a small second scene) with a few rooms so all three
   layouts can be switched by a key and tried on the SAME level.

Done means: compiles with 0 errors; the movement and facing checks still pass; a check
that the camera never leaves a room in the room layouts and stays on whole pixels; finish
with `/handoff` (it commits; include this brief).

Feel needs the user's playtest - ship starting values. If something is the user's call,
stop and ask; the counselor chat relays it. Plain words, short.

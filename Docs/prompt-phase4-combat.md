# Brief: phase 4 - health, damage, shooting, enemies -> Arena shooter demo

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 4 of `Docs/ROADMAP.md` (sections 6 and 7, and the Arena shooter demo).

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md`; search
`Docs/LOG.md` and `Docs/TRAPS.md` before risky work. The user skipped playtesting so far -
don't block on it.

Scope (feel differences are settings, extras are add-ons, one copy of each rule):

1. **Health and damage**: one damage rule used by player and enemies; invulnerable time
   after a hit, hit flash, knockback on the target (phase 1's `Knockback`), screen shake
   (phase 3's `Shake`). Dash's stored invulnerable flag now really protects.
2. **Weapons**: a weapon asset - fire rate, bullet speed, spread, bullets per shot, range,
   damage, ammo + reload or infinite, recoil on the shooter. Shoot along the aim
   direction (phase 2). Two or three example weapons (pistol, shotgun, rapid).
3. **Bullets**: on the fixed step; hit walls and targets. "Reused bullets" is the
   user's spec vs "no pooling no measurement asked for": measure allocation with plain
   instantiate first; if it spikes, use Unity's built-in `UnityEngine.Pool.ObjectPool<T>`
   (no custom framework). Log the measurement either way.
4. **Enemies (kept tiny)**: same movement and damage rules as the player. Behaviour is a
   setting: chase, keep distance and shoot, wander. Placeholder art via pxkit.
5. **Rooms lock until cleared for real**: `RoomLock.Clear()` when a room's enemies die.
6. **Arena shooter demo scene**: Glide feel, mouse aim, dash, weapons, a few locking rooms
   with enemy waves, a simple health/ammo readout. Player death -> restart.

Done means: compiles with 0 errors; the movement, facing and camera checks still pass; a
check that damage, invulnerable time and bullet hits behave the same at 30 and 144 fps;
the demo is playable start to finish; finish with `/handoff` (it commits; include this
brief).

Feel needs the user's playtest - ship starting values. If something is the user's call,
stop and ask; the counselor chat relays it. Plain words, short.

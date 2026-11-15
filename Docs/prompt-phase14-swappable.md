# Brief: phase 14 - make everything swappable

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-15 by the counselor chat.

Build phase 14 of `Docs/ROADMAP.md` ("Round 2") - the final building pass before the
full test pass (phase 15).

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (phase 14 row
and the note under the table). Search `Docs/LOG.md` / `Docs/TRAPS.md` before risky work.

The user's words: "make everything highly customizable, easy to put in any types of
tiles, whichever animations needed, whichever character, enemies, walls, whichever number
of frames per move ... made as simple and versatile as possible so it is easy to get rid
of useless stuff and fully redo some aspects." Also: sounds, controls and UI customizable.

Scope:

1. **Art swaps, no code**: any tile, character, enemy, wall, weapon, item, crate, effect
   is changed by dropping in your own sprites/assets. Audit every place art is set and
   make it a field on an asset or prefab, not something built in code.
2. **Animations**: per action (idle, walk, attack, hurt, death, dash...), any number of
   frames, any frame rate, per direction with the existing 5 + mirroring / true 8 setting.
   Replace the fixed "3-frame sheet" assumption in `DirectionalSprites`.
3. **Sounds**: confirm every event uses phase 12's sound slots; no hard-coded clip.
4. **Controls**: every key is a setting, plus a rebind screen in the pause menu; saved
   with phase 13 save/settings. Input still arrives as data (touch keeps working).
5. **UI**: colours, fonts (including a pixel font option), box/button art and layout
   swapped through the theme and panel art - no code.
6. **Removable systems**: each system in its own folder with its own assembly definition
   where that helps, so a folder can be deleted without breaking the rest. Add an
   editor-side check that removes each optional system in turn (in a scratch copy or by
   excluding it) and confirms the rest compiles - this check is vital, keep it.
7. **README**: a short "how to replace / how to remove" section per system.

`CLAUDE.md` says "no config systems until three real callers exist" - the callers now
exist (Arena shooter, RPG room, Dungeon, Playground); name them when you add anything
shared, and log it.

**Testing (user):** tests only when vital - compile with 0 errors, scenes open, the
removable-systems check, and the phase 13 save check if save data changed shape. Note
anything else for phase 15 in `Docs/NEXT.md`.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

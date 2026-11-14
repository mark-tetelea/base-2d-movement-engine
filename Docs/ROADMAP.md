# Roadmap - the whole picture

> Written 2026-11-14. The plan for the whole kit, start to finish. `CLAUDE.md` wins over
> this file. Items marked **(new)** are not in the kit's list yet - they need the user's
> yes before they are built. Rewrite this file when the plan changes; don't append.

## What we are building

A **top-down 2D starter kit** for Unity (seen from above: Deltarune, Diep.io, Enter the
Gungeon, Don't Starve) that a new game starts from - plus a few **small demo games** that
prove it, so the GitHub page shows something you can play, not just code.

The selling point is **choice**: every system has settings, and changing a settings asset
gives a different-feeling game with no code change. The kit's design test still holds:
*a difference in feel is a setting; a feature only some games need is an add-on piece.*

## The systems, and the choices each one offers

### 1. Movement - built (core), add-ons to come

| Choice | Kind | Status |
|---|---|---|
| Speed, time to full speed, time to stop | setting | built |
| Presets: **Instant** (Deltarune), **Glide** (Diep.io) | setting | built |
| Presets: **Ice** (long stop), **Heavy** (slow start, quick stop) | setting | new assets only |
| Wall slide: keep the along-wall part, or full speed | setting | open question |
| **Dash / dodge roll** - distance, time, cooldown, invulnerable or not | add-on | to build |
| **Knockback / recoil** - own decaying push, added after `Step` | add-on | designed |
| **Boosts and slowdowns** - speed pads, mud, ice floor zones | add-on | designed |
| **Grid step** (one tile per press, Pokemon-style) **(new)** | add-on | idea |
| **Tank controls** (turn, then drive) **(new)** | add-on | idea |

### 2. Facing, aim and animation

| Choice | Kind |
|---|---|
| 4 or 8 facing directions | setting |
| Face where you walk, or where you aim | setting |
| Aim from the mouse, or from the walk direction | setting |
| 5 drawings + mirroring, or true 8 | setting (`mirrorLeft`); default 5 + mirroring (user, 2026-11-14) |
| Idle / walk animation, flip on left | built-in |

### 3. Camera and world layout **(new - every demo needs one)**

How the world is cut up, and how the camera shows it. One camera script; the layout is a
setting, room pieces are add-ons.

| Choice | Kind |
|---|---|
| **Open**: one big space, the camera follows (smoothing, dead zone, look-ahead toward the aim) | setting |
| **Rooms, slide**: the camera stops at room edges and slides to the next room when you walk through a door (Isaac / Zelda style) | setting |
| **Rooms, fade**: walking through a door fades out and loads the next room or area (Undertale style) | setting |
| Rooms can be bigger than the screen (camera follows inside, clamped to the room) | setting |
| **Doors lock until the room is cleared** (Isaac style) | add-on |
| Screen shake on hits | setting |

Pixel Perfect Camera stays. A room is just a marked rectangle in the scene, so the same
level can be played open or in rooms by switching the setting.

### 4. Interaction

One key uses the **best nearby target** - one shared system for everything.

| Choice | Kind |
|---|---|
| Reach (in tiles) | setting |
| "Best" means closest, or the one you face | setting |
| Press, or hold to use | setting |
| Targets: chest, NPC, item on the floor, door / lever **(new)** | add-on pieces |
| A small "press E" prompt over the target | built-in |
| NPC talk: a simple text box, lines from an asset **(new)** | add-on |

### 5. Pickups and inventory

| Choice | Kind |
|---|---|
| Auto pickups (coins, ammo, health) with a **magnet** reach | setting |
| Manual pickups (weapons, key items) via Interaction | built-in |
| Inventory size, stack sizes, hotbar slot count | setting |
| **Chests are inventories too** - same system | built-in |
| Drop an item back on the floor | built-in |

### 6. Shooting, health and damage

| Choice | Kind |
|---|---|
| Weapon asset: fire rate, bullet speed, spread, bullets per shot, range, damage | setting |
| Ammo and reload, or infinite | setting |
| Recoil on the shooter (uses knockback) | setting |
| Bullets reused (Unity's own `ObjectPool`, once a measurement asks) | built-in |
| Health, invulnerable time after a hit, hit flash, knockback on the target | setting |
| Melee swing **(new)** | add-on, later |

### 7. Enemies **(new - the shooter demo needs something to shoot)**

Kept tiny, and they use the **same** movement and damage rules as the player:
chase, keep distance and shoot, wander. Behaviour picked by a setting.

### 8. Screens

A health bar, the hotbar, ammo, the interaction prompt, a pause menu. Plus the
**feel picker** in the Playground (below).

## The demos (what GitHub visitors play)

| Demo | Shows off | Built from |
|---|---|---|
| **Playground** - the test arena plus an on-screen picker for every setting | that every choice is a setting | everything, live |
| **Arena shooter** | Glide, mouse aim, dash, weapons, enemies, knockback; rooms that lock until cleared | 1, 2, 3, 6, 7 |
| **Little RPG room** | Instant, 4 directions, NPCs, chests, a key and a locked door; rooms with a fade | 1, 2, 3, 4, 5 |
| Later: one more (dungeon rooms, or survival-lite) | the kit stretching | pick later |

The tycoon idea is parked in `Docs/IDEAS.md`.

## Making it look good on GitHub

- README with one short GIF per demo, a feature list, and "how to start a game from it".
- A browser build (Unity WebGL) on itch.io or GitHub Pages, so people can play without
  installing anything.
- MIT licence, clean folders, one namespace (`TopDownKit`).
- Later, maybe: install it as a Unity package.

## The order, one session per phase

Each phase is one fresh chat (`Docs/KEEPING-CONTEXT.md`, "One chat per job"), started by
the counselor chat, ending with `/handoff` and a commit.

| # | Phase | Notes |
|---|---|---|
| 0 | **Playtest**: tune the numbers of each feel, wall-slide speed | the user plays, ~5 minutes. Instant and Glide are both kept as options - this is tuning, not picking one (user, 2026-11-14) |
| 1 | Movement add-ons: dash, knockback, boosts/slowdowns, Ice/Heavy presets | dash and knockback get callers in phase 4 |
| 2 | Facing, aim and animation (+ the art question) | |
| 3 | Camera and world layout: open, rooms-slide, rooms-fade, locking doors | doors lock for real once enemies exist (phase 4) |
| 4 | Health, damage, shooting, enemies -> **Arena shooter demo** | first playable demo |
| 5 | Interaction, pickups, inventory, chests, NPC text -> **RPG room demo** | |
| 6 | Playground picker and the screens | |
| 7 | GitHub polish: README, GIFs, WebGL build, licence | |

## Still not doing

Gamepad, touch and mobile builds, saving, online play - until a game needs them
(`CLAUDE.md`, "Do NOT build yet"). Tycoon / management - parked.

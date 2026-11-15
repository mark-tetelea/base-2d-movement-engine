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
| **Grid step** (one tile per press, Pokemon-style) | setting (`MoveSettings.gridStep`; feels `Grid`, `Grid 8`, `Grid tap`) | built 2026-11-14 (phase 8) |
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
| Melee swing (`Weapon.kind` Melee), charge shots, bouncing bullets, aim assist | settings + add-on `MeleeSwing`, built phase 9 |

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
- Custom licence (use for own games, no redistributing the kit; LOG 2026-11-14), clean folders, one namespace (`TopDownKit`).
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
| 6 | Playground picker and the screens | built 2026-11-14: UI Toolkit screens, start menu |
| 7 | GitHub polish: README, GIFs, WebGL build, licence | 2026-11-14: README, LICENSE, PNG screenshots; WebGL blocked (module not installed) |

## Round 2 - asked for by the user, 2026-11-14

Same rules as round 1: each difference is a setting, each extra is an add-on piece, one
copy of each rule, checks before `/handoff`.

| # | Phase | What it adds |
|---|---|---|
| 8 | **Ground tiles + grid-step** - built 2026-11-14 | Tile types, each one a small asset: **normal** ground; **red** (kills you); **blue** (can't be stepped on - a wall you can see over and shoot across); **crumbling** (breaks a set time after you step on it, comes back after a set time); **door ground** (opens/closes on a switch, a key or a cleared room - reuses phase 3's `RoomLock`). Plus **grid-step movement**: one tile per press, as a movement setting, with a "frames per step" / step time setting for smoothness. |
| 9 | **Combat additions** - built 2026-11-14 | Melee swing, charge shots, bullets that bounce off walls, aim assist (strength as a setting, 0 = off). |
| 10 | **Enemy additions** - built 2026-11-14 | Pathfinding around walls (fixes the stuck-behind-pillar limit), a boss with phases, groups that move together, enemies that leave a trail (can use the red-tile rule). |
| 11 | **World additions** - built 2026-11-14 | Procedural rooms (a new layout every run, built from room pieces), destructible crates. |
| 12 | **Feel additions** - built 2026-11-14 | Hit-stop (tiny freeze on hits), particles, sound effects, day/night tint. All settings, all off-able. **Sounds are swappable**: each event (step, shot, hit, pickup, door, UI click...) points at a sound slot you fill with your own clip or leave empty. |
| 13 | **Systems** - built 2026-11-14 | Save and load, a coin shop, a minimap, touch controls for phones. These lift "saving" and "touch" off `CLAUDE.md`'s "Do NOT build yet" - the user asked for them. |
| 13b | **Item pickup pass** - built 2026-11-15 | How an item is picked up is a **per-item setting**: walk over it, come close and press interact, or pulled in by a magnet. Items come from any source the same way - the floor, chests, the shop, enemy drops. Every item can be **dropped** from the inventory back onto the floor. (Phase 5 built auto/manual pickups, the magnet, chests and dropping - this pass makes the mode a per-item choice and connects the shop and enemy drops.) |
| 14 | **Make everything swappable** | The final pass. Any tile, character, enemy, wall or weapon is swapped by dropping in your own art/asset - no code. Animations: any number of frames, any frame rate, per action (idle, walk, attack, hurt...). Sounds: any clip per event slot. **Controls**: every key is a setting, with a rebind screen in the pause menu (input still arrives as data, so touch keeps working). **UI**: colours, fonts, box and button art, and layout swapped through the theme and panel art - no code. Every system lives in its own folder and can be **deleted without breaking the rest** - a check deletes each one in turn and compiles. A short "how to replace / remove X" guide per system in the README. |
| 15 | **Full test pass** | One pass at the end of round 2 (user, 2026-11-14: from phase 8 on, per-phase tests only when vital - it compiles, plus a check only where a bug would be hard to find later (e.g. save/load, the generator always making a finishable level)). Re-run every check in `Tools/measure/`, add checks for the round-2 systems, run all demo bots, fix what breaks. |

**A note for phase 14:** `CLAUDE.md` says "no config systems until three real callers
exist". By then there are three demos plus the Playground, so the three callers exist -
the phase names them when it adds anything shared.

## Still not doing

Gamepad and online play - until a game needs them (`CLAUDE.md`, "Do NOT build yet").
Tycoon / management - parked. Third demo (roguelike dungeon) - an idea, not asked for yet.

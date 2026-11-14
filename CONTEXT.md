# Where the kit is now

> Rewritten, never appended. True as of **2026-11-14**, end of phase 12 (hit-stop, effects, sounds, day/night tint).

## How it starts

Press Play on `Assets/Scenes/StartMenu.unity` (first in the build list): a title and four
buttons - **Playground**, **Arena shooter**, **Little RPG room**, **Dungeon**. In every demo **Esc**
pauses (the game stops; Resume, Restart, Main menu).

## The Playground

`Assets/Scenes/Playground.unity` (the old test arena). A blue placeholder player in a stone
arena of four rooms: A (start), B to the right (its doors lock once you are inside), big
room C above, and area D reached by a brown warp pad in C. Along the top of A: a yellow
speed pad (x1.6), brown mud (x0.5) and a cyan ice patch. In A: a chest (a key and 10 coins),
the green Guide to talk to, a row of coins. In D: two green wanderers to shoot and a
red grid-step chaser that hops one tile at a time and goes round walls. In the upper part of C: a
group - a crowned leader wandering with three red chasers in a V behind it (they break off and
chase you when you come within 4 tiles) - and a brown trail enemy that leaves a fading trail of
red goo (1 damage a second to stand in).

The **tile garden** in the lower left of C has every kind of ground: a red pool (kills), a
blue strip (water you cannot walk through but can shoot over), a crumbling bridge across a
red moat (it cracks when stepped on, breaks 0.75 s later into a deadly hole, is back 3 s
after), a door in a short wall opened by a lever, and a door opened with the chest's key.
B's north door is door ground too: it shuts while B is locked and opens when B is cleared.

A **settings panel** on the left (Tab hides it) shows every choice the kit has, each as a
button; a click moves to the next value, live:

- Feel: Instant (like Deltarune) / Glide (like Diep.io) / Ice / Heavy
- Grid step: off / Grid (one tile at a time, hold to walk) / Grid 8 (diagonals too) /
  Grid tap (one press, one tile)
- Dash (Space): on / off - Facings: 4 / 8 - Face towards: walk / aim - Aim from: mouse / walk
- Camera: Open / Rooms slide / Rooms fade - Weapon: Pistol / Shotgun / Rapid / Sword / Charger / Bouncer
- Aim assist: off / 0.35 / 0.7 (bends each shot toward the nearest enemy in a 15 degree cone)
- Best target: facing / closest - Use: press / hold - Reach: 1 / 1.5 / 2.5 tiles
- Magnet: off / 2.5 / 5 tiles - Room: shows the room; click clears a locked one
- Hit-stop / Particles / Sounds: on / off - Volume: 100% / 50% / 25% - Day/night: Off / Fixed
  (a blue night tint) / Cycle (day, evening, night, morning over 60 s)

WASD move, mouse aims, left click shoots (not when the click lands on a screen), R reloads,
1-6 pick a weapon (Sword swings an arc; Charger: hold to charge, release to fire, up to x4
damage; Bouncer: bullets bounce off walls 3 times), E uses, K knocks the player away from the mouse. The HUD shows health,
weapon and ammo, the hotbar (click a slot) and the controls.

## The Arena shooter demo (phase 4)

`Assets/Scenes/ArenaShooter.unity`. Glide feel, faces the mouse, rooms slide. WASD, mouse aim,
left click shoots, R reloads, 1-3 or the wheel pick Pistol / Shotgun / Rapid, Space dashes
(invulnerable). Walking into B, C or D shuts the doors and sends waves (red chasers, purple
shooters, green wanderers); the last wave opens them. Chasers and shooters walk round pillars and
walls (shooters go round a wall that blocks their shot). Clearing B opens a door in its east wall
to **room E, the boss room** (pillars and a wall in the middle): a big horned blob with a health
bar at the top of the screen, three phases - bouncing 3-shot spread, then slow charged shots from
60% health, then from 30% it rushes you and swings a wide sword. HUD: health bar, weapon and ammo,
"wave 2/3", the boss bar, "Arena cleared - you win" (all of B, C, D and E), "You died" (the scene
restarts after 1.5 s).

## The Little RPG room demo (phase 5)

`Assets/Scenes/LittleRpg.unity`. Instant feel, 4 facings, rooms fade. E uses the best target
in front (an "E" shows over it), 1-5 pick a hotbar slot, Q drops one. Talk to the Guide,
pick up coins (magnet 2.5 tiles), find the key in a chest in C, open the pink door, take the
crown in B to win. Chests open a transfer screen (click a slot, T takes all, E closes).

## The Dungeon demo (phase 11)

`Assets/Scenes/Dungeon.unity`. Plays like the Arena shooter (glide, faces the mouse, rooms slide,
Pistol / Shotgun / Rapid, dash), but **the level is new every run**: 7 screen-sized rooms joined
by doors, built from hand-made room pieces - a start room with crates, Pillars, Storeroom (lots of
crates, wanderers), a long Hall (two screens wide), Pool (blue water to shoot over, red corners), a
tall Tower (two screens high), and always last, as far from the start as the layout allows, the
Boss room with the horned blob. Walking into a room shuts its doors (door ground) and sends two
waves; clearing it opens them. **Crates** (wooden boxes) block walking and bullets; three hits break
one, and it may drop a coin, three coins or a heart. HUD status: "Rooms 2/6  Coins 3  Seed 12345"
(the seed names the layout); "Dungeon cleared - you win" when the boss room is cleared; death or
Restart gives a new layout. A fixed seed on the generator gives the same layout every time.

## Sounds, effects, hit-stop, tint (phase 12)

Every scene, the start menu included, has placeholder sounds: steps, shots, sword swings, dash,
hits, a sword hit, the player getting hurt, deaths, a crate breaking, pickups, doors (room doors,
the key door, levers), crumbling ground and UI clicks. Hits, deaths, dashes, pickups, crate breaks
and crumbling ground throw a few pixel bits. Sword hits, the player getting hurt, deaths and crate
breaks freeze the game for a moment (0.04-0.1 s); bullet hits do not. The day/night tint is off in
every demo; the Playground's settings panel turns it on. No music (the slot is empty).

## Screens

One set, used by all four: a HUD (health bar, weapon + ammo, hotbar, a status line, a centre
banner, a help line - each demo shows only what it has), the text box, the chest screen, the
pause menu, the start menu, and the Playground's settings panel. Placeholder look: dark
Endesga 32 boxes, the default font, drawn in the game's 640x360 pixels and scaled by whole
numbers like the game (x3 at 1920x1080, x2 at 1366x768).

## What is measured

Same keys give the same movement step for step at 30 and 144 fps; combat too; grid steps
land exactly on tile centres and on the same tiles at both. Each tile rule is checked: red
kills (a dash over it protects until it ends there), blue blocks walking but not bullets,
crumbling breaks and returns on time, door ground opens by lever, key and cleared room. Reused bullets
cost ~4 us and ~150 B against ~24 us and ~4 KB. Bots clear the Arena shooter (boss room included; enemies
walk round walls) and finish the
RPG room. The UI scale matches the camera at 1920x1080 and 1366x768, and a click on the
settings panel does not shoot.

## What the kit has (the user's list)

1. Movement - 8 directions, instant-to-glide, Ice/Heavy, dash, knockback, speed zones: **built**.
2. Facing and animation - 4 or 8, aim separate from walking: **built**. Camera and rooms: **built**.
3. Interaction - one key, best nearby target: **built**.
4. Pickups and inventory - auto and manual, hotbar, chests as inventories: **built**.
5. Shooting - weapons, reused bullets, health, damage, recoil, tiny enemies: **built**.
6. Screens and the Playground picker: **built** (phase 6).
7. Ground tiles (normal, red, blue, crumbling, door) and grid-step movement: **built** (phase 8,
   round 2). Only the Playground uses them; no demo does.
8. Combat additions - melee swing, charge shots, bouncing bullets, aim assist: **built** (phase 9).
   Only the Playground has the Sword, Charger and Bouncer; the demos keep their weapons.
9. Enemy additions - pathfinding round walls (free and grid-step walkers), a boss with phases and a
   HUD bar, groups that move together, a trail enemy: **built** (phase 10). The boss is in the Arena;
   groups, trail and grid-step enemies are in the Playground.

10. World additions - generated levels from room pieces (start, end, allowed pieces, room count,
   seed), destructible crates with a drop table: **built** (phase 11). Only the Dungeon uses them.
11. Feel additions - hit-stop per hit kind, effect slots, swappable sound slots with placeholder
   sounds and volumes, day/night tint: **built** (phase 12). All scenes.

## What is not done

- Every playtest: movement feel (Instant vs Glide, wall slide), facing (8 directions, walk
  speed), combat numbers, the RPG room (reach, facing vs closest, magnet), and now the screens
  (sizes, readability, the settings panel), and now grid step (pace, slide time) and the
  tiles (crumble timing, how forgiving red is), and the phase 9 weapons (swing, charge, bounce, assist), and the phase 10 enemies (boss difficulty, group spacing, trail damage), and the Dungeon (room sizes, wave difficulty, how often crates drop), and the sounds, bits, hit-stop lengths and tint colours. Nobody has played or heard any of it yet.
- Real art for everything in `TO-DRAW.md` except the floor, wall, player and aim marker.
- WebGL build (module not installed), GIFs (only PNG screenshots in `Media/`), GitHub repo and push (the user's call).

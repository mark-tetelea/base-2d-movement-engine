# Where the kit is now

> Rewritten, never appended. True as of **2026-11-14**, end of phase 6 (Playground picker, shared screens, start menu).

## How it starts

Press Play on `Assets/Scenes/StartMenu.unity` (first in the build list): a title and three
buttons - **Playground**, **Arena shooter**, **Little RPG room**. In every demo **Esc**
pauses (the game stops; Resume, Restart, Main menu).

## The Playground

`Assets/Scenes/Playground.unity` (the old test arena). A blue placeholder player in a stone
arena of four rooms: A (start), B to the right (its doors lock once you are inside), big
room C above, and area D reached by a brown warp pad in C. Along the top of A: a yellow
speed pad (x1.6), brown mud (x0.5) and a cyan ice patch. In A: a chest (a key and 10 coins),
the green Guide to talk to, a row of coins. In D: two green wanderers to shoot.

A **settings panel** on the left (Tab hides it) shows every choice the kit has, each as a
button; a click moves to the next value, live:

- Feel: Instant (like Deltarune) / Glide (like Diep.io) / Ice / Heavy
- Dash (Space): on / off - Facings: 4 / 8 - Face towards: walk / aim - Aim from: mouse / walk
- Camera: Open / Rooms slide / Rooms fade - Weapon: Pistol / Shotgun / Rapid
- Best target: facing / closest - Use: press / hold - Reach: 1 / 1.5 / 2.5 tiles
- Magnet: off / 2.5 / 5 tiles - Room: shows the room; click clears a locked one

WASD move, mouse aims, left click shoots (not when the click lands on a screen), R reloads,
1-3 pick a weapon, E uses, K knocks the player away from the mouse. The HUD shows health,
weapon and ammo, the hotbar (click a slot) and the controls.

## The Arena shooter demo (phase 4)

`Assets/Scenes/ArenaShooter.unity`. Glide feel, faces the mouse, rooms slide. WASD, mouse aim,
left click shoots, R reloads, 1-3 or the wheel pick Pistol / Shotgun / Rapid, Space dashes
(invulnerable). Walking into B, C or D shuts the doors and sends waves (red chasers, purple
shooters, green wanderers); the last wave opens them. HUD: health bar, weapon and ammo,
"wave 2/3", "Arena cleared - you win", "You died" (the scene restarts after 1.5 s).

## The Little RPG room demo (phase 5)

`Assets/Scenes/LittleRpg.unity`. Instant feel, 4 facings, rooms fade. E uses the best target
in front (an "E" shows over it), 1-5 pick a hotbar slot, Q drops one. Talk to the Guide,
pick up coins (magnet 2.5 tiles), find the key in a chest in C, open the pink door, take the
crown in B to win. Chests open a transfer screen (click a slot, T takes all, E closes).

## Screens

One set, used by all three: a HUD (health bar, weapon + ammo, hotbar, a status line, a centre
banner, a help line - each demo shows only what it has), the text box, the chest screen, the
pause menu, the start menu, and the Playground's settings panel. Placeholder look: dark
Endesga 32 boxes, the default font, drawn in the game's 640x360 pixels and scaled by whole
numbers like the game (x3 at 1920x1080, x2 at 1366x768).

## What is measured

Same keys give the same movement step for step at 30 and 144 fps; combat too. Reused bullets
cost ~4 us and ~150 B against ~24 us and ~4 KB. Bots clear the Arena shooter and finish the
RPG room. The UI scale matches the camera at 1920x1080 and 1366x768, and a click on the
settings panel does not shoot.

## What the kit has (the user's list)

1. Movement - 8 directions, instant-to-glide, Ice/Heavy, dash, knockback, speed zones: **built**.
2. Facing and animation - 4 or 8, aim separate from walking: **built**. Camera and rooms: **built**.
3. Interaction - one key, best nearby target: **built**.
4. Pickups and inventory - auto and manual, hotbar, chests as inventories: **built**.
5. Shooting - weapons, reused bullets, health, damage, recoil, tiny enemies: **built**.
6. Screens and the Playground picker: **built** (phase 6).

## What is not done

- Every playtest: movement feel (Instant vs Glide, wall slide), facing (8 directions, walk
  speed), combat numbers, the RPG room (reach, facing vs closest, magnet), and now the screens
  (sizes, readability, the settings panel). Nobody has played any of it yet.
- Real art for everything in `TO-DRAW.md` except the floor, wall, player and aim marker.
- WebGL build (module not installed), GIFs (only PNG screenshots in `Media/`), GitHub repo and push (the user's call).

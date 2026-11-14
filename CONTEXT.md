# Where the kit is now

> Rewritten, never appended. True as of **2026-11-14**, end of phase 4 (combat and the Arena shooter demo).

## How it plays

Open `Assets/Scenes/TestArena.unity` and press Play. A blue placeholder player walks in 8
directions (WASD or the arrows) in a stone arena that fills the 640x360 view: a wall ring
at the screen edge, a pillar, a bar, a single block, diagonal steps and a one-tile
corridor. Walls stop the player; pushing diagonally into a wall slides along it.

Four feels, swapped with keys **1**-**4** (the top-left label names the one in use and the speed in force now):

- **Instant** (like Deltarune): 6 tiles/s, full speed at once, stops at once.
- **Glide** (like Diep.io): 6 tiles/s, 0.4 s to full speed, 0.8 s to stop.
- **Ice**: 6 tiles/s, 1.0 s to full speed, 2.0 s to stop; pushes slide far.
- **Heavy**: 4 tiles/s, 0.5 s to full speed, 0.15 s to stop; shrugs pushes off.

The player faces where it walks (8 directions, idle + a 2-step walk; the left side is the
right side flipped) and a yellow diamond shows the aim, towards the mouse. **F** switches
4 / 8 facings, **G** face walk / aim, **H** aim from mouse / walk; the second label line
names them. Standing still keeps the facing.

**Space** dashes 3 tiles in 0.2 s (0.4 s cooldown; hits do not land during it). **K** knocks the player away from the mouse. Along the top of the arena: a yellow
speed pad (x1.6), brown mud (x0.5) and a cyan ice patch (Ice feel while on it).

**Camera and rooms (phase 3):** the arena is now room A of a small level: room B to the
right (its doors lock once you are inside; **C** clears it), room C above (80x30, bigger
than the screen), and area D reached by a brown warp pad in C. **L** switches the camera:
Open (follows with a small dead zone, leans 1.5 tiles toward the aim), Rooms slide (stays
in the room, slides 0.5 s to the next), Rooms fade (fade out/in 0.25 s each). Keys pause
during slides and fades. **K** also shakes the screen. The camera is always on whole pixels.

All of it is starting guesses - nobody has judged it by playing yet. Each feel is an asset
in `Assets/Data/Movement/`; tuning during play is kept.

Measured: the same keys give the same movement step for step at 30 and 144 fps (LOG,
2026-10-18). Ready to use besides: the `unity` command line (port 7801), the 60 Hz fixed
step, the Endesga 32 palette, pxkit, the PNG import preset, and the movement measuring
harness in `Tools/measure/`.

## The Arena shooter demo (phase 4)

Open `Assets/Scenes/ArenaShooter.unity` and press Play. Glide feel, the player faces the
mouse, rooms slide. WASD move, mouse aims, left click shoots, R reloads, 1-3 or the wheel
pick Pistol (12 shots, semi), Shotgun (6 pellets, 4 shells, strong recoil) or Rapid (auto,
30), Space dashes (invulnerable). Player 6 health, 1 s invulnerable after a hit (blinks),
the screen shakes. Room A has two green wanderers to warm up on; walking into B, C or the
warp area D shuts the doors and sends waves: red chasers (touch damage), purple shooters
(keep 6 tiles away and fire), green wanderers. The last wave dead opens the doors. All
rooms cleared shows "Arena cleared - you win"; dying reloads the scene after 1.5 s. A
top-left label shows health, weapon, ammo and the wave. Every number is a starting guess.

Measured: combat behaves step for step the same at 30 and 144 fps; reused bullets
(Unity's `ObjectPool`) cost ~4 us and ~150 B against ~25 us and ~4 KB with Instantiate; a
bot clears every room and the death restart works. Enemies walk straight at you - they
can get stuck behind a pillar (no pathfinding).

## What the kit will have (the user's list, in the order to build it)

1. **Movement** - 8 directions and instant-to-glide feel: **built**. Ice/Heavy presets, dash,
   knockback and speed zones: **built** (phase 1, 2026-11-14).
2. **Facing and animation** - 4 or 8 directions, aim separate from walking: **built**
   (phase 2, 2026-11-14). 5 drawings + mirroring by default, true 8 as a setting.
   **Camera and world layout** (roadmap 3): **built** (phase 3, 2026-11-14).
3. **Interaction** - one key, best nearby target: chests, NPCs, items.
4. **Pickups and inventory** - auto (coins, ammo) and manual (weapons, key items),
   inventory with a hotbar, chests as inventories.
5. **Shooting** - weapon settings, reused bullets, health, damage, recoil: **built**, with
   tiny enemies and rooms that lock until cleared (phase 4, 2026-11-14).

The order is a suggestion, not a decision.

## Screens

None. The test arena has a one-line developer label (which feel is in use, and the facing settings), not kit UI.

## What is not done

- The feel playtest (Instant vs Glide, and whether a wall slide should keep full speed).
- Everything after movement in the list above, and every placeholder in `TO-DRAW.md`
  except the floor, wall, the player and the aim marker. The facing playtest (does 8 feel
  right, walk-cycle speed 8 fps).
- The combat playtest: weapon numbers, enemy speed and health, wave sizes, damage feel.

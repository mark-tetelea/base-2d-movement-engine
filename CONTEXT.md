# Where the kit is now

> Rewritten, never appended. True as of **2026-11-14**, end of phase 1 (movement add-ons).

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

**Space** dashes 3 tiles in 0.2 s (0.4 s cooldown; invulnerable flag stored for damage
later). **K** knocks the player away from the mouse. Along the top of the arena: a yellow
speed pad (x1.6), brown mud (x0.5) and a cyan ice patch (Ice feel while on it).

All of it is starting guesses - nobody has judged it by playing yet. Each feel is an asset
in `Assets/Data/Movement/`; tuning during play is kept.

Measured: the same keys give the same movement step for step at 30 and 144 fps (LOG,
2026-10-18). Ready to use besides: the `unity` command line (port 7801), the 60 Hz fixed
step, the Endesga 32 palette, pxkit, the PNG import preset, and the movement measuring
harness in `Tools/measure/`.

## What the kit will have (the user's list, in the order to build it)

1. **Movement** - 8 directions and instant-to-glide feel: **built**. Ice/Heavy presets, dash,
   knockback and speed zones: **built** (phase 1, 2026-11-14).
2. **Facing and animation** - 4 or 8 directions, aim separate from walking.
3. **Interaction** - one key, best nearby target: chests, NPCs, items.
4. **Pickups and inventory** - auto (coins, ammo) and manual (weapons, key items),
   inventory with a hotbar, chests as inventories.
5. **Shooting** - weapon settings, reused bullets, health, damage, recoil.

The order is a suggestion, not a decision.

## Screens

None. The test arena has a one-line developer label (which feel is in use), not kit UI.

## What is not done

- The feel playtest (Instant vs Glide, and whether a wall slide should keep full speed).
- Everything after movement in the list above, and every placeholder in `TO-DRAW.md`
  except the floor, wall and the player's one direction.
- **Open question for the facing work:** 8-direction art as eight drawings, or five with
  mirroring.
- **Open question for when shooting is built:** "reused bullets" (the user's spec) against
  the working agreement's "no pooling that no measurement asked for". Likely answer:
  Unity's built-in `UnityEngine.Pool.ObjectPool<T>` (no custom framework), justified by a
  stress measurement - mobile is a target, and allocation spikes there are real. Ask then.

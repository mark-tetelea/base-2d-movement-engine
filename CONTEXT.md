# Where the kit is now

> Rewritten, never appended. True as of **2026-10-18**, end of the kickoff.

## How it plays

It does not play yet. There is no game code and no art: the project is the Unity URP 2D
template (`Assets/Scenes/SampleScene.unity`, empty) plus the environment. The day-one
decisions are in `CLAUDE.md`, their arguments in `Docs/LOG.md` (2026-10-18).

Ready to use: the `unity` command line (port 7801), a 60 Hz fixed step in project settings,
the Full HD Game view, the Endesga 32 palette, pxkit for code-drawn sprites, and an import
preset that makes any PNG under `Assets/Art` a 16 PPU pixel sprite.

## What the kit will have (the user's list, in the order to build it)

1. **Movement** - 8 directions, instant-to-glide by settings, boosts, slowdowns,
   knockback/recoil. One movement function on the fixed 60 Hz step.
2. **Facing and animation** - 4 or 8 directions, aim separate from walking.
3. **Interaction** - one key, best nearby target: chests, NPCs, items.
4. **Pickups and inventory** - auto (coins, ammo) and manual (weapons, key items),
   inventory with a hotbar, chests as inventories.
5. **Shooting** - weapon settings, reused bullets, health, damage, recoil.

The order is a suggestion, not a decision: movement first because everything else rides
on it.

## Screens

None yet.

## What is not done

- Everything in the list above, and every placeholder in `TO-DRAW.md`.
- A test-arena scene with the Pixel Perfect Camera at 640x360 (comes with movement).
- **Open question for when movement is built:** where the feel settings live - a plain
  serialisable settings class on the component, or one settings asset with presets such as
  "instant" and "glide". The design test says feel is a setting; it does not yet say what
  shape the setting takes.
- **Open question for when shooting is built:** "reused bullets" (the user's spec) against
  the working agreement's "no pooling that no measurement asked for". Likely answer:
  Unity's built-in `UnityEngine.Pool.ObjectPool<T>` (no custom framework), justified by a
  stress measurement - mobile is a target, and allocation spikes there are real. Ask then.
- **Open question for the facing work:** 8-direction art as eight drawings, or five with
  mirroring.

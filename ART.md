# Art

> Read for art work only. `CLAUDE.md` wins over this file. Under 20 KB.

## Who draws what

**Claude draws all of the kit's art, in code, with `Tools/pxkit`** - placeholders that
prove the systems work. **The user draws nothing for the kit;** real art belongs to each
game built on it. If the user wants to draw something here, Claude makes an Aseprite
template to draw over (`node Tools/pxkit/template.js`, see `Docs/ASEPRITE.md`).
(LOG, 2026-10-18)

## Sizes

- **16 px = 1 world unit = 1 tile.** One pixels-per-unit for the whole kit: **16**.
- Base resolution **640x360** (Pixel Perfect Camera), about 40 x 22.5 tiles on screen.
- Gameplay numbers are in tiles and never read off a sprite (trap 8). Art is drawn to match.
- Per-sprite canvases are in `TO-DRAW.md`.

## Palette

`Assets/Art/Palette/endesga-32.gpl` - Endesga 32 (free, lospec.com), ordered into named
ramps, dark to light. **Load this file in Aseprite**, not Aseprite's built-in Endesga 32
preset: same colours, different slot order (trap 61).

| Ramp | Steps | For |
|---|---|---|
| `steel` | 7 | stone, metal, greys; steel-0 is the outline and the void, steel-6 is white |
| `wood` | 5 | wood, sand, leather, chests |
| `skin` | 2 | skin (with wood-3/wood-4 for light skin if needed) |
| `rust` | 2 | rust, clay, orange-brown |
| `fire` | 5 | red to yellow: fire, coins (fire-3/4), danger, health |
| `green` | 4 | grass, plants, "good" |
| `blue` | 3 | water, sky, mana, the player's colour |
| `plum` | 3 | purples and pinks: enemies, magic |
| `hot` | 1 | `#ff0044` accent: hits, alerts. Use sparingly |

Slot 0 (`#ff00ff`, "see-through") is reserved and never painted (trap 62).

## Drawing rules (pxkit's `lint.js` checks the ones marked *)

1. Hard pixels only - no anti-aliasing, no fractional scaling. *
2. Every colour from the palette. * Every sprite declares its ramps and uses only those. *
3. **The test-arena floor is `steel-2`; nothing on a silhouette is painted in it** - it
   would vanish against the floor (trap 66). * The void outside the arena is `steel-0`.
4. Outline in `steel-0` (`K.outline`). An edge reads if it is clearly darker or lighter
   than the floor. *
5. Light comes from the upper left.
6. No drawing guides in exported art - guides go on a hidden layer (trap 56).
7. Things that stand on the floor are drawn with their feet on the canvas's bottom art row,
   so the pivot can be bottom-centre and sorting by y works. Bullets, effects and icons
   pivot in the centre.

## Unity import settings

Set once per file and **checked by reading them back**, never assumed (traps 57, 58, 59):
Pixels Per Unit **16**, Filter **Point**, Compression **None**, **no mipmaps**, pivot as
rule 7. For `.aseprite` files, set them through the importer's public properties
(`spritePixelsPerUnit`, `filterMode`, `mipmapEnabled`, `SetImporterPlatformSettings`).

*Not yet set up in Unity (2026-10-18): waiting on the `unity` command line connection.*

## Templates

Guide colours in every template: deep blue `blue-0` = the canvas edge; hot red `hot-0` =
the margin or a zone that matters; yellow `fire-4` = where the art must sit (the feet
row), dashed for the centre line. Guide layers are saved hidden.

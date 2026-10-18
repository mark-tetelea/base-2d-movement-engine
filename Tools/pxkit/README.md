# pxkit

Code-drawn pixel sprites: draw in a small JS file, check it against `ART.md`'s rules,
render the one image anyone looks at, export PNGs and colour variants.

Copied from Jewelery Shop on 2026-10-18 and cut down: its sprites, people kit, scenery
helpers (bench, night sky, rock face, stone, water glitter), metal recolour tables and
C# palette writer were left behind, and two rules that were that game's art direction
("warm means lit", "items out-shine furniture") were removed from `lint.js`. The palette
is now `Assets/Art/Palette/endesga-32.gpl`.

## Commands (run from the project root, with bash)

    node Tools/pxkit/lint.js                         every sprite
    node Tools/pxkit/lint.js coin chest              named
    node Tools/pxkit/lint.js --png a.png --ramps wood,steel --anchor 15 --margin 1

    node Tools/pxkit/review.js coin                  cropped, largest scale inside 400 tokens
    node Tools/pxkit/review.js coin -b 900           raise the budget for a hard judgement
    node Tools/pxkit/review.js coin --full           whole canvas + anchor tick
    node Tools/pxkit/review.js --png a.png

    node Tools/pxkit/export.js                       all -> Tools/pxkit/out/
    node Tools/pxkit/export.js coin --dir Assets/Art/Generated
    node Tools/pxkit/export.js slime --swap plum:green,blue

    node Tools/pxkit/template.js player              an Aseprite file to draw over

`review.js` picks the zoom from a token budget, never by taste: a look at a picture costs
`width x height / 750` tokens after the long edge is capped at 1568 px, so it costs the
**square** of the zoom. It takes the largest whole scale that fits (400 tokens by default).
Review one sprite at a time.

## Files

| File | Job | Not allowed to become |
|---|---|---|
| `palette.js` | Parses the `.gpl` and recovers the ramps from slot names (`steel-0`..). | A second copy of the palette. It reads, never writes (trap 70). |
| `png.js` | PNG read and write, RGBA8, no dependencies. | An image library. |
| `kit.js` | Buffer and drawing primitives. Can only set a pixel to one literal palette colour. | A renderer. |
| `lint.js` | `ART.md`'s rules as executable tests. Text output only. Silence is a pass. | Chatty. |
| `review.js` | The one image you look at: cropped, scaled, on the floor colour. | A viewer. |
| `export.js` | 1x PNGs, and colour variants by ramp swap. | A build system. It does not touch Unity import settings. |
| `aseprite.js` | Runs a Lua script in Aseprite headless and returns what it wrote to a file. | - |
| `template.js` | Makes an Aseprite file to draw in: canvas, palette, Indexed, hidden guides. Never overwrites without `--force`. | - |
| `sprites/*.js` | One sprite each. | Shared. A helper used twice belongs in `kit.js`. |
| `templates/*.js` | One Aseprite template each. | - |

## Traps

- `package.json` in this folder exists only to stop Node walking up to the broken
  `D:\package.json` (trap 55). Do not delete it.
- Write sprite files with the Write/Edit tools, not heredocs (trap 24).
- An Aseprite script's `print()` never reaches the shell; `aseprite.js` writes a file.

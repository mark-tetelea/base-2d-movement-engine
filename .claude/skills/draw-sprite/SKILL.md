---
name: draw-sprite
description: Draw, revise or recolour a pixel-art placeholder sprite for the top-down kit with Tools/pxkit - the code-drawn sprite pipeline. Use when asked to generate, redraw, tweak, recolour or ramp-swap a sprite, tile, icon or placeholder, or to check a PNG or Aseprite export against the palette and ART.md's drawing rules. Also makes an Aseprite template when the user wants to draw something themselves.
---

# Drawing a sprite with pxkit

`Tools/pxkit/` draws sprites in code, checks them against `ART.md`'s rules, and renders the
one image you look at. The library is written; you write one small sprite file.

**The expensive part is looking, not drawing.** An image costs `width x height / 750`
tokens after its long edge is capped at 1568 px, so a look costs the SQUARE of the zoom.
`review.js` picks the largest whole scale inside a token budget. Never pick one by hand.

## Before you start

Read exactly two things:

1. **The sprite's row in `TO-DRAW.md`** - canvas, feet/pivot, ramps. Grep for it
   (`grep -n 'Chest' TO-DRAW.md`).
2. **`ART.md`** sections *Palette* and *Drawing rules*, if not already read this session.

Do not read `CLAUDE.md`, `Docs/LOG.md` or `Docs/TRAPS.md` whole for a drawing job; grep
for the one fact you need.

## The loop

    1. Write or edit ONE file: Tools/pxkit/sprites/<id>.js   (Write/Edit tool, not a heredoc)
    2. node Tools/pxkit/lint.js <id>          <- text, ~40 tokens. Fix every FAIL.
    3. node Tools/pxkit/review.js <id>        <- then Read the PNG it names. ONE look.
    4. One revision pass if it needs one, then ONE more look.
    5. Stop. Hand it over and say what you would change next.

Two looks per sprite. A third means tuning by eye at a price per glance - the user can
judge the third faster.

## The sprite file

```js
module.exports = {
  canvas: [16, 16],
  ramps: ['wood', 'steel'],  // lint fails on any other ramp
  anchorBottom: 15,          // bottom-most solid row must land exactly here (the feet)
  margin: 1,                 // empty px inside the canvas
  offset: [0, 0],            // translate the finished art instead of re-deriving coordinates
  floor: true,               // ONLY for floor tiles: they are the ground, so lint skips vanish/read-1x
  draw(b, K) { /* K is kit.js: rect, span, ellipse, roundRect, poly, grid, line, stroke,
                   ball, band, bay, rimLight, outline, rng; K.P.ramps.wood, K.P.OUTLINE */ }
};
```

`kit.js` can only set a pixel to one literal palette colour, so anti-aliasing cannot
happen. `bay()` dithering is for light falloff only, never to fake a shape. Use
`K.grid(b, rows, map)` for the smallest sprites, typed out pixel by pixel. Draws must be
deterministic: `K.rng(seed)`, never `Math.random`.

## 8-direction characters

One sprite file per direction and frame, or one file that takes a direction - decide when
the facing system is built and write the answer into `ART.md`. Do not draw all eight
before the first one has been looked at in the game.

## Exporting into Unity

    node Tools/pxkit/export.js <id> --dir Assets/Art/Generated
    node Tools/pxkit/export.js <id> --swap plum:green,blue

A new PNG under `Assets/Art/` gets PPU 16, Point, Uncompressed, no mips from the import
preset on its first import - **read it back** anyway, and set the pivot per `ART.md` rule 7
(bottom-centre for things with feet) by hand (traps 57-59, 45).

## When the user wants to draw it

Make a template instead: `Tools/pxkit/templates/<id>.js`, then
`node Tools/pxkit/template.js <id>`. It never overwrites an existing file without
`--force`, because once the user has drawn in it, it is their drawing. Never script an
edit to a file open in Aseprite.

## Do not

- Do not review before `lint.js` is clean.
- Do not render the whole set to check one sprite.
- Do not pass `-s` to "see it better"; raise `-b` and say why if the judgement needs it.
- Do not paint a silhouette in `steel-2` (the floor) or anything in slot 0.
- Do not edit `Assets/Art/Palette/endesga-32.gpl` from a script that also reads it (trap 70).

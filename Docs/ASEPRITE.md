# Drawing with Aseprite

How you and Claude share the drawing. Added 2026-10-18; everything marked "verified" was
proved on Jewelery Shop.

---

## Decide on day one: who draws what

Write the answer at the top of `ART.md`.

- **You draw** the things that need a hand: backdrops, the main characters, anything you
  care most about.
- **Claude draws** in code with `pxkit` (placeholders, icons, items, variations,
  recolours) - or makes you a **template** to draw over.

---

## Setup, once per project

1. **Unity packages:** `com.unity.2d.aseprite` and `com.unity.2d.psdimporter`. Unity then
   imports `.aseprite` files directly - no PNG export step. *Verified: the 3.0.2 importer
   reads Aseprite 1.3.7 files, 2026-09-14.*
2. **Save `.aseprite` files straight into `Assets/Art/...`.** Unity re-imports on every save.
3. **Pixel-art import settings, set once and checked by reading them back:** Filter
   Point, Compression None, no mipmaps, one pixels-per-unit for the whole game (decide it
   on day one - it is hard to change later).
4. **One palette.** Keep it as a file in `Assets/Art/` and load it in Aseprite. `ART.md`
   holds the palette rules.
5. **Copy `Tools/pxkit` from `D:\Jewelery Shop\Jewelery shop\`** if Claude will draw in code,
   and the `draw-sprite` skill from its `.claude/skills/`. Replace the palette, delete the
   Jewelery-Shop sprites. Its `lint.js` also checks your exported PNGs against the palette.

---

## How Claude drives Aseprite

Aseprite runs from the command line, headless:

```bash
"/d/Aseprite.v1.3.7/Aseprite.exe" -b --script make-template.lua
```

(or just `aseprite ...` - the shortcut in `~/.local/bin` was fixed 2026-10-18.)

It can export PNGs and sprite sheets, read layers and frames, make templates, recolour,
and resize canvases. **Traps, all found on Jewelery Shop:**

- **`print()` in a Lua script never reaches the shell on Windows.** Write results to a file.
- **Run it from bash, not PowerShell** - PowerShell's `-replace` tripped a safety check.
- **Never script an edit to a file you have open in Aseprite.** Your next Ctrl+S
  overwrites the change, silently. Claude asks first, or works on a copy.
- **A visible layer ships.** The importer skips *hidden* layers by default, so guide and
  reference layers must be saved **hidden**.
- **Layered backdrops:** import with Individual Layers and the pivot on the Canvas, and
  each layer becomes its own sprite that lines up exactly.
- **Read/Write** (needed when code reads the pixels) has no public setting on the Aseprite
  importer - it is `m_TextureImporterSettings.m_IsReadable`, set through `SerializedObject`.

---

## The handover loop

1. **Claude makes a template** in `Assets/Art/Templates/`: right canvas size, hidden guide
   layers showing the baseline, where people stand, what must stay clear.
2. **You draw over it** in Aseprite and save.
3. **You tell Claude** "drew the X". Claude checks it imported (size, pivot, units), runs
   the palette check, looks at it once, and ticks it off in `TO-DRAW.md`.

## Looking at pictures is the expensive part

A picture costs Claude tokens by the **square** of the zoom. `pxkit/review.js` picks the
zoom for a token budget - never pick one by hand. Run the palette check before looking,
look at one sprite at a time, and at most twice per sprite.

# Log

Everything dated: built, measured, decided - with the full argument. **Only ever added
to; never edit an older section.** Search it, do not read it whole
(`grep -n -A8 'timestep' Docs/LOG.md`). Where a later entry overturns an earlier one, it
says so out loud, and the later entry wins.

---

## 2026-10-18 - Kickoff: what the kit is, and the day-one decisions

Set up from the starter kit (`D:\Orbit\ClaudeStarterKit\`, kickoff rewritten the same day
after Jewelery Shop).

### What this project is

**A reusable top-down 2D starter kit, not one game.** View from above - the user's
references are Don't Starve Together, Deltarune, Diep.io and Enter the Gungeon. Future
top-down games will be built on it. The user's list of what it includes:

- Movement in 8 directions, with settings so it can feel instant (Deltarune) or glide
  (Diep.io). Speed boosts and slowdowns, knockback/recoil.
- Facing and animation in 4 or 8 directions, with aiming separate from walking.
- Interaction: one shared system for chests, NPCs and items. Press a key to use the best
  nearby target.
- Pickups and inventory: automatic pickups (coins, ammo) and manual ones (weapons, key
  items), an inventory with a hotbar, and chests that use the same inventory system.
- Shooting: weapon settings, reused bullets, health, damage, recoil.

**Target:** Unity 6000.3.23f1, 2D, URP 17.3 (already so in the template). Windows PC,
keyboard + mouse, **no gamepad**. Android and iOS games will be built on the kit later.

### Decided

**Decision 1 - gameplay runs on a fixed 60 Hz step.**
Movement, knockback, bullets and damage run in `FixedUpdate` with
`Time.fixedDeltaTime = 1/60`. The rule functions take the step length `dt` as a parameter
and are never allowed to read `Time.deltaTime` themselves. Unity's Rigidbody2D, with
interpolation on, has one job: stopping things going through walls. Rendering is smooth
on 144 Hz screens because of the interpolation, not because gameplay runs faster.
- **Why:** the feel this kit exists to provide - instant vs glide, knockback distance,
  dash distance - must come out the same at 30 fps and at 144 fps, and on PC and phones.
  On `Time.deltaTime` it does not, and Orbit showed what that costs: "the field replays and
  the run does not", with replays and leaderboards blocked on a change that is cheap in
  week one and a rewrite in month three. A fixed step is also the cheap version of keeping
  replays and online play possible for a future game.
- **Chosen over** Unity's default 50 Hz (fine, but 60 gives slightly finer input timing
  and matches the most common display rate) and variable `Time.deltaTime` (feel varies with
  frame rate).
- **Not promised:** bit-exact results across different machines. That matters only for
  lockstep online play, and would mean no Unity physics at all - too much for a starter
  kit. On the *Do NOT build yet* list until a game needs it.
- **Consequence - pause:** `Time.timeScale = 0` stops all gameplay. Only menus and UI may
  run on unscaled / real time; anything else on an unscaled clock keeps running behind
  the pause screen.
- **Consequence - input timing:** input is read every frame in `Update` and consumed by
  the next fixed step, so a press can wait up to one step (16.7 ms). Presses must be
  latched until a step consumes them, or a press between steps is lost.

**Decision 2 - one copy of each core rule.**
Movement, damage/knockback, inventory and interaction each live in one place, as static
functions or one plain class:
- movement: one function from (current velocity, move direction, settings, dt) to the new
  velocity - called by the player, enemies, an editor preview ("how far does this dash
  go") and tests;
- damage and knockback: one function, used by bullets, melee and anything else that hurts;
- inventory: one inventory type - the player's bag, the hotbar and chests are all
  inventories (the user's own spec already said chests share it);
- interaction: one "best target nearby" picker for chests, NPCs and items.
- **Why:** two implementations diverge within a day, and from then on you tune feel
  against a copy the game does not use (Orbit's `SimulateRelease` had five callers and was
  the single reason its generator could prove true things). In a kit it costs double:
  every game built on it inherits whichever copy is wrong.
- **Consequence:** no extra layers - managers, event bus, config systems - until three
  real callers of one exist, named out loud at the time.
- **Rejected:** "allow layers early because it is a kit meant for reuse". Reuse is the
  argument for one copy, not for scaffolding nobody calls yet.

**Decision 2a - input arrives as data.** Movement takes a move direction and shooting an
aim direction; the rules never read keys or the mouse.
- **Why:** the user will build Android and iOS games on the kit. A touch stick then
  produces the same two vectors, and nothing in the rules changes. Falls straight out of
  Decision 2: the input device must not become a second code path.

**Decision 3 - pixel art, 16 px = 1 world unit = 1 tile, base resolution 640x360.**
Unity's Pixel Perfect Camera at 640x360 reference resolution, 16 assets PPU. All gameplay
numbers (speed, ranges, knockback, pickup radius) are in tiles, never pixels.
- **Why 640x360:** it scales by a whole number to 1280x720 (2x), 1920x1080 (3x),
  2560x1440 (4x) and 3840x2160 (6x), so pixels stay crisp on every common PC screen.
  480x270 only manages 1080p (4x) and 4K (8x).
- **Why 16 px:** about 40 x 22.5 tiles on screen, which suits a shooter - Enter the
  Gungeon shows somewhat less. Small sprites keep placeholder drawing cheap.
- **Why tiles, not pixels:** a later game can switch to 32 px art, or to non-pixel art
  like Don't Starve, without retuning any feel.
- **Chosen over:** 16 px at 480x270 (bigger-looking sprites, crisp only at 1080p and 4K),
  32 px at 640x360 (about 20 x 11 tiles, more drawing per sprite), and no pixel art.
- **Consequence:** phones have other shapes (19.5:9 and so on). How 640x360 fits them -
  crop, expand or bars - is decided when the first mobile game starts, not now. Traps 30,
  31 and 32 (Pixel Perfect Camera, and UI scale that must equal its `pixelRatio`) apply
  from day one.

**Decision 4 - Claude draws all kit art in code (pxkit); the user draws nothing for the
kit.** Placeholders: the player in 8 directions, an enemy, bullet, coin, ammo, chest, NPC,
item icons, test floor and wall tiles.
- **Why:** the kit's job is to prove the systems, not to look final. Code-made
  placeholders are cheap to redo when a setting changes (4-way vs 8-way facing). The user's
  drawing time goes to the first real game. If the user ever wants to draw something for
  the kit, Claude makes a template to draw over (`Docs/ASEPRITE.md`).

**Skipped - score.** A kit has none; each game built on it decides its own (and should
test it against `DAY-ONE.md`'s three questions then).

**Skipped - origin and "forward".** There is no fixed world anchor to decide, and nothing
needs to infer a heading: aim comes straight from the mouse (or later a touch stick).
Each game decides its own anchor, if it has one.

**The kit's design test**, in the user's words:
*"A difference in how games feel is a setting. A feature only some games need is an
optional add-on piece. Never 'if this game, do X' inside the core."*
- **Why:** Claude proposed "a difference between two games is a setting, never a second
  code path". The user sharpened it into two cases: feel differences are settings, and
  features only some games want are optional add-ons - with game-specific branches banned
  from the core outright. It settles "copy this and tweak it?" and "add a flag for game X?"
  in one line, and it is how Decision 2 shows up day to day.

### Built

- Git repo at the Unity project root, branch `main`, Unity `.gitignore` (GitHub's, as used
  on Jewelery Shop, plus pxkit scratch output). First commit `7bcffb4` is the empty
  template, before any notes or code.
- Notes laid out as `Docs/KEEPING-CONTEXT.md` says: `CLAUDE.md`, `CONTEXT.md`,
  `Docs/LOG.md`, `Docs/NEXT.md`, `Docs/TRAPS.md`, `Docs/IDEAS.md`, plus copies of the kit's
  `WORKING-AGREEMENT.md`, `MEASURING.md`, `KEEPING-CONTEXT.md`, `ASEPRITE.md` and the
  `/handoff` skill in `.claude/skills/handoff/`.
- `Docs/TRAPS.md`: the kit's 24 traps plus 46 carried from Jewelery Shop's `CLAUDE.md`
  (its 25-76, renumbered 25-70 with "(JS nn)" tags). Left out as game-specific: JS 63, 67,
  71, 74, 76; JS 66 folded into 50.

## 2026-10-18 - Environment: unity command line, drawing, import preset

### Decided

**Palette for the kit's placeholders: Endesga 32** (lospec.com, free), stored as
`Assets/Art/Palette/endesga-32.gpl` with slot 0 reserved see-through and the 32 colours
ordered into named ramps (steel 7, wood 5, skin 2, rust 2, fire 5, green 4, blue 3, plum 3,
hot 1).
- **Why:** pxkit checks every sprite against one palette file, and Jewelery Shop's
  Mezzanine palette is that game's own. Endesga 32 is common for top-down pixel games and
  its ramps cover a player, chest, coin, bullets and floor. Chosen over Resurrect 64 (more
  shades than placeholders need) and PICO-8 (16 colours, very limiting).
- **Consequence:** each real game swaps in its own palette; pxkit reads whatever `.gpl`
  `palette.js` points at. The ramp names are load-bearing (pxkit recovers ramps from them).

**The test-arena floor is `steel-2`; `steel-0` is the void and the outline colour.**
- **Why:** pxkit's "vanish" rule needs one ground colour to test silhouettes against. In a
  top-down game sprites stand on the floor, not on a dark screen as in Jewelery Shop. Picked
  by Claude as a starting point, not discussed - change it in `palette.js` and `ART.md`
  together if the arena floor changes.

**PNG import settings as a Preset Manager default, not per-file edits.**
- **Why:** every placeholder Claude exports would otherwise need a per-file settings call
  (and the CLI's 5 s limit makes bulk reimports fail, trap 26). A preset filtered to
  `Assets/Art/**` sets them on first import with nothing to remember.
- **Consequence:** it does not change files already imported, does not set pivots, and does
  not cover `.aseprite` files (different importer).

### Built

- `com.unity.pipeline` 0.7.0-exp.1 in `Packages/manifest.json`; the `unity` command line
  connects on port 7801 (Jewelery Shop, also open, holds 7800). Autotick 200 ms.
- `ProjectSettings/TimeManager.asset`: fixed timestep 1/60 (Decision 1 applied).
- Game view set to the built-in Full HD (1920x1080) size.
- `Tools/pxkit` copied from Jewelery Shop and cut down: kept png, kit (minus bench, night
  sky, rock face, stone, glitter), lint (minus "warm means lit" and "items out-shine
  furniture"; read-1x now counts darker edges as readable), review, export, aseprite,
  template; new `palette.js` on the Endesga file. Left behind: sprites, people kit,
  drawings, recolour tables, C# palette writer.
- `.claude/skills/draw-sprite/` rewritten for this kit. `ART.md`, `TO-DRAW.md` (12
  placeholder rows, canvases marked as proposals).
- `Assets/Art/PixelSprite16.preset` + `ProjectSettings/PresetManager.asset` default.

### Measured / verified

- **Which editor answers:** `eval` returning `Application.dataPath` gave this project's path,
  both with `--project-path` and auto-detected from the project folder. One check each.
- **Build target** reads `StandaloneWindows64`. **`activeInputHandler: 1`** (Input System
  only) read from ProjectSettings.
- **Fixed timestep:** `Time.fixedDeltaTime` read back 0.0166666675 (= `1f/60f`), and the
  file holds `m_Count: 2352000` over 141120000. The first attempt silently failed (trap 71).
- **pxkit end to end, one run each:** `lint` on a code sprite and on its exported PNG
  (clean); `review` (8x for ~21 tokens); `export --swap fire:blue`; `template.js` made an
  `.aseprite` that read back as 16x16, Indexed, 33 palette colours with slot 1 = `#181425`
  and slot 32 = `#ff0044`, guide layer hidden. Throwaway files deleted.
- **Import preset:** a PNG imported after the preset existed read back PPU 16, Point,
  Uncompressed, no mips, rect 16x16, world size 1.0. A PNG imported before it kept Unity's
  defaults (PPU 100, Bilinear, Compressed) - presets apply on first import only. One file
  each. Throwaway files deleted.
- Console after setup: 0 errors once the trap-71 error was cleared.

### Found

- Traps 71 (fixed timestep is a rational in Unity 6; a "successful" eval can still log an
  error) and 72 (an unfocused editor ignores a manifest edit for as long as it stays
  unfocused - ten minutes here).

---

## 2026-10-18 - Movement: the rule, the player, the test arena

### Decided

**Feel settings live in an asset, one per feel (`MoveFeel`: `Instant.asset`,
`Glide.asset`).** The user's choice. Claude recommended a plain settings block on the
component (one caller today; "no config systems until three callers").
- **Why (the user's pick):** values tuned during play mode are kept afterwards, and
  several movers can share one feel.
- **Shape:** the asset only holds a plain `MoveSettings` struct, and the rule takes the
  struct, never the asset. Tests, editor previews and boosts make or copy a struct
  without touching an asset.
- **Consequence:** this one asset type is the user's explicit exception to "no
  ScriptableObject config systems". It is not a config system; nothing else gets an
  asset because movement has one.

**The feel knobs are speed (tiles/s), time to full speed (s) and time to stop (s); 0 is
instant.** Times are what a designer tunes by feel; the rule derives the rates
(speed / time). Velocity moves in a straight line toward its target (`MoveTowards`), so a
full reversal takes twice the time to full speed, and a 90-degree turn dips in speed
part-way. Starting values, not tuned: Instant 6 / 0 / 0, Glide 6 / 0.4 / 0.8.

**The player reads its current velocity back from the Rigidbody2D every step.**
- **Why:** after a physics step the body has already lost whatever a wall took, so
  pushing into a wall builds nothing up and turning away from it is immediate. A
  velocity kept in a field would keep pushing into the wall and make the player stick.

**How boosts, slowdowns and knockback will push (designed, not built - no caller yet):**
- **Boosts and slowdowns:** pass `Movement.Step` a copy of the settings with a scaled
  speed. The times stay the same, so acceleration scales too; speed 0 stops at once
  (guarded in the rule). If a game wants fixed acceleration under a boost, add a
  target-only multiplier then.
- **Knockback and recoil:** their own velocity, decayed by the knockback rule (Decision 2's
  "damage and knockback: one function"), added to the body after `Step`. Not pushed into
  `Step`'s velocity: an Instant feel would cancel it in one step. When it lands, the
  player reads its move velocity back as body velocity minus the push.

**The input latch keeps a NEW direction until a step uses it; a held key is not
latched.**
- **Why:** a tap seen by one frame still moves (Decision 1), but a release is never
  delayed. The obvious latch ("last non-zero direction since the last step") adds one
  extra step after every release at high frame rates - a frame-rate difference, the
  thing Decision 1 exists to prevent.

**Keys are read straight from `Keyboard.current` (WASD and arrows), not the template's
`InputSystem_Actions` asset.** That asset also binds gamepads and joysticks, and the kit
has no gamepad. Keys are read by physical position, so AZERTY works.

**Pixel Perfect Camera: Pixel Snapping, Windowbox.** The usual pixel-art pair: Windowbox
shows exactly 640x360 at any window size (bars where the scale is not whole - WXGA);
Pixel Snapping leaves post-processing and UI usable, which Upscale Render Texture does
not. Not yet looked at on WXGA (trap 32's test).

**Kit code lives in the `TopDownKit` namespace;** test-arena-only code in
`TopDownKit.TestArena`, so games built on the kit do not collide with it.

**Player collider:** a circle of radius 0.375 (6 px, the drawn body's width) at the feet,
with a frictionless material so sliding along a wall does not drag.

### Built

- `Assets/Scripts/Movement/MoveSettings.cs` - the feel, as a struct (tiles and seconds).
- `Assets/Scripts/Movement/Movement.cs` - the one rule:
  `Step(velocity, moveDirection, settings, dt)`. Cuts direction length to 1 (diagonals),
  reads no time, input or scene.
- `Assets/Scripts/Movement/MoveFeel.cs` - the asset wrapping the struct.
- `Assets/Scripts/Movement/PlayerMover.cs` - keys to a direction in `Update`, the latch,
  `Step` in `FixedUpdate` on the Rigidbody2D, reading the feel every step.
- `Assets/Scripts/TestArena/FeelKeys.cs` - test arena only: keys 1-9 swap the feel, and a
  top-left label names it (IMGUI, a dev overlay - not the kit's UI).
- `Assets/Data/Movement/` - `Instant.asset`, `Glide.asset`, `NoFriction.physicsMaterial2D`.
- `Assets/Scenes/TestArena.unity` - from the URP 2D template (camera, global light).
  Pixel Perfect Camera 640x360 / 16 PPU, background `steel-0` (the void). A `Grid` with a
  floor tilemap and a wall tilemap (TilemapCollider2D merged into a static
  CompositeCollider2D, polygons): a wall ring at the screen edge, a 2x2 pillar, a 6-tile
  bar, a single block, four diagonal steps and a one-tile corridor. Row y = 0 and column
  x = 0 are kept clear for measuring. Player starts at (0, -2) on Instant.
- Art (pxkit): `floor`, `wall`, `player` (one direction, facing the camera) in
  `Tools/pxkit/sprites/`, exported to `Assets/Art/Generated/`; tiles in
  `Assets/Art/Generated/Tiles/` (wall collider type Grid).
- `Tools/pxkit/lint.js`: a sprite spec may say `floor: true` - a floor tile IS the ground,
  so the vanish and read-at-1x rules skip it.
- `Tools/measure/` - the first measuring harness (CLAUDE.md said: the first time a number
  is needed). `movement.cs` runs through `eval_file` in play mode: a virtual keyboard fed
  with queued key events (the real `PlayerMover` key path), and a recorder appended to the
  FixedUpdate player loop that logs the body after every fixed step. It changes input
  routing, vsync and the frame cap for the run and restores them (read back afterwards).
  `analyse-movement.js` prints the results.

### Measured / verified

- **Compiles:** 0 errors, 0 warnings in the Unity console, after building and after
  every play session.
- **Imports read back:** PPU 16, Point, uncompressed, no mips; tiles 1x1 world units, the
  player 1x1.5 with its pivot at (8, 0) px - bottom-centre.
- **Pixel Perfect Camera read back through `SerializedObject`** (trap 30): `m_CropFrame`
  Windowbox, `m_GridSnapping` PixelSnapping, 16 PPU, 640x360.
- **Decision 1's promise - distance per second is the same at 30 and 144 fps.** 16 runs,
  in two passes of the harness (a scratch copy, then the saved `Tools/measure/movement.cs`):
  Instant and Glide, each at 30 and 144 fps, four times each, holding D for 2 s through the
  real key path. Frame rates measured 30.0 and 142.4-144.1.
  - First second after the first moving step: Instant 6.0000 tiles in all 8 runs, Glide
    4.8500 in all 8 (the rule predicts both exactly).
  - Steady speed 6.0000 tiles/s on the game clock in all 16 runs; 5.82-6.04 on the wall
    clock (editor frame-timing jitter; the game clock is what the step uses).
  - The first 120 moving steps (2 s) match step for step: maximum difference **0** between
    any two runs of the same feel, across frame rates, in both passes.
  - Glide stops in 2.35 tiles at both frame rates, all 8 runs.
  - Two of the four Instant runs at 144 fps moved 121 steps; every other Instant run moved
    120. The scripted release landed just after a step boundary. A held key's length is only resolved to one
    step (16.7 ms) - input timing, not the movement depending on frame rate.
- **8 directions (two runs, Instant, 60 fps):** all eight at speed 6.0000, at exactly
  0 / 45 / 90 / 135 / 180 degrees and their negatives; diagonals (4.243, 4.243).
- **The latch (two runs, ~144 fps, Instant):** 10 taps per run, each visible to exactly
  one frame; 20 of 20 moved exactly one step (first run's final x 1.0000001 = 10 x 0.1).
- **Walls (two runs per feel, 60 fps):** holding A into the left wall stops the player at
  x = -18.62 (wall face -19 + radius 0.375, less Unity's 0.005 contact gap), with velocity
  reading 0 while pushing. Holding W+A diagonally into the wall slides along it at
  **4.243 tiles/s** - only the along-wall part of the diagonal - with both feels.
- **Not judged - waiting on the user:** how Instant and Glide feel, and whether a wall
  slide should keep the full 6 tiles/s. Those are a playtest.

### Found

- Traps 73-76: assets loaded before `OpenScene(Single)` in the same script come back
  fake-null (tiles painted nothing); a script-painted tilemap collider stays empty until
  toggled and the scene saves it empty; decimal commas from this editor's locale;
  `capture_game_view --save_path` writes under `Assets/`.
- `eval` / `eval_file` compile the code as a method body: no `using` lines (write names
  in full), and `Object` is ambiguous (write `UnityEngine.Object`).

## 2026-11-14 - Phase 1: movement add-ons (Ice, Heavy, dash, knockback, zones)

**Decided:**
- **The mover keeps two velocities: walking (`Movement.Step`) and a knockback push
  (`Knockback.Step`); the body gets their sum.** Why: the 2026-10-18 design - Instant would
  cancel a push pushed into Step. Consequence: after physics, what a wall took is removed
  from the into-wall part of both (`PlayerMover.TakeWallLoss`); with no push the walking
  velocity is the body's velocity exactly, as before.
- **How fast a push dies away is a feel setting, `MoveSettings.knockbackStopRate`**
  (tiles/s lost per second; 0 = pushes stop at once). Why: a heavy mover shrugs hits off,
  ice slides on - a difference in feel, so a setting.
- **Dash is an add-on component that drives the mover's walking velocity for whole fixed
  steps** (`PlayerMover.DriveThisStep`), speed set so the distance is exact. At the end it
  hands back at most the feel's top speed in the dash direction. Why: exact distance at
  any frame rate; Glide would otherwise coast at dash speed. Knockback still adds on top.
- **Speed zones change the settings passed to Step** (`SpeedZone.Apply`): the last zone with
  a feel replaces the feel, then every zone's `speedScale` multiplies the speed. Why: the
  2026-10-18 design; mud on ice is slow ice.

**Built:**
- `Assets/Scripts/Movement/Knockback.cs` - the one push rule: `Away(source, target, speed)`, `Step(push, stopRate, dt)`.
- `Assets/Scripts/Movement/Dash.cs` - `DashSettings` (distance, duration, cooldown, invulnerable - stored for phase 4) and the `Dash` add-on; Space in the arena.
- `Assets/Scripts/Movement/SpeedZone.cs` - trigger zone add-on.
- `PlayerMover.cs` - push, zones, `CurrentSettings`, `FacingDirection` (last asked direction; dash with no keys uses it), add-on entry points.
- `Assets/Data/Movement/Ice.asset` (6 tiles/s, 1.0 s up, 2.0 s stop, push stop 12), `Heavy.asset` (4, 0.5, 0.15, push stop 80); Instant and Glide got push stop 40. Keys 3 and 4.
- `Assets/Scripts/TestArena/KnockKey.cs` - K pushes the player 14 tiles/s away from the mouse.
- TestArena: `Zones` (Speed pad x1.6, Mud x0.5, Ice patch -> Ice feel) in the free strip at the top, y 6-9; placeholder `Assets/Art/Placeholders/white-square.png` tinted.
- `Tools/measure/setup-phase1.cs` - the edit-mode script that made the above (re-runnable).
- Harness: `dash` and `knock` segments at 30 and 144 fps, Instant and Glide.

**Measured / verified (one harness run, 22 segments):**
- Old checks unchanged: lane runs identical step for step at 30/144 fps, 8 directions at 6.000, 10 of 10 taps moved, walls hold at x -18.62.
- Dash: 12 steps at 15.000 tiles/s, distance 3.0000 at 30 and 144 fps, Instant and Glide; max difference 0 between frame rates.
- Knockback 14 tiles/s, Instant: 21 steps (0.35 s), 2.5667 tiles; Glide 2.5910; 30 vs 144 fps max difference 0. (Stepped sum, so a bit above the continuous v*v/2a = 2.45.)
- Zones, one check each in play mode: speed pad 9.6 tiles/s, mud 3, ice patch gives Ice's 1 s / 2 s.
- Not verified: the feel of any of it (needs the user's playtest); dash and knockback into walls.

## 2026-11-14 - Phase 2: facing, aim and animation

**Decided:**
- **8-direction art is 5 drawings + mirroring; true 8 is a setting (`mirrorLeft`).** Why: the
  user's choice - fewer drawings for every game. Consequence: left facings 3/4/5 show 1/0/7 flipped.
- **Facing is an index 0-7 counter-clockwise from right; a zero direction keeps it.** Why:
  standing still keeps the last facing; one integer is cheap to test and to send online.
- **With 4 facings an exact diagonal keeps the current facing if it is one of the two
  nearest, else goes sideways.** Why: no flicker walking diagonally. Starting rule, playtest.
- **Animation is a plain sprite swap, no Animator.** Why: idle + 2 steps per direction did
  not earn one. Walk cycle step, idle, step, idle at 8 fps; idle under 0.1 tiles/s.
- `PlayerMover.FacingDirection` renamed `LastMoveDirection` (it is not facing); new `InputDirection`.

**Built:**
- `Assets/Scripts/Facing/Facing.cs` - the one facing rule + `FacingSettings`.
- `PlayerAim.cs` - mouse or walk -> aim direction as data; the rules never read the mouse.
- `PlayerFacing.cs` - runs the rule on the fixed step. `DirectionalSprites.cs` - sprite swap + flip.
- `TestArena/FacingKeys.cs` - F/G/H switches, aim marker, label line.
- `Tools/pxkit/sprites/player-{right,up-right,up,down-right,down}.js` (48x24 sheets, 3 frames)
  and `aim-marker.js`; `Tools/pxkit/slice-sheet.cs` slices sheets (bottom-centre pivots). Old
  `player.js`/`player.png` became `player-down`.
- `Tools/measure/facing.cs` - facing check through a virtual keyboard and mouse.

**Measured / verified:**
- Compiles with 0 errors, 0 warnings.
- `facing.cs`: 18/18 pass, one run - all 8 walk directions with sprite and flip (3/4/5 flip),
  standing keeps facing, 4-direction diagonals, face aim (mouse left) while walking right,
  face walk while mouse left, aim from walk.
- Movement harness re-run after the change: unchanged (0 tiles difference 30 vs 144 fps,
  8 directions 6.000, 10/10 taps, dash 3.0000, knockback 2.5667 / 2.5910).
- Down sprite judged in a game render (snapshot); others judged from pxkit review only.
- Not verified: the feel (needs a playtest); the real mouse path (the check used a virtual mouse).

## 2026-11-14 - Phase 3: camera and world layout

Brief: `Docs/prompt-phase3-camera.md`.

**Decided:**
- **One `CameraRig` on the camera; layout (Open / RoomsSlide / RoomsFade) is a setting; the maths is static `CameraFollow` (DeadZone, Smooth, Clamp, SnapToPixel).** Why: one copy of the rule. Consequence: the camera runs in `LateUpdate` on frame time (presentation, after the interpolated player), not on the fixed step.
- **The camera position is rounded to 1/16 tile by the rig itself**, not left to the Pixel Perfect Camera. Why: a checkable "whole pixels" rule.
- **Screen shake is added after the room clamp, so it may show a few pixels past a room edge.** Why: rooms exactly the screen size would otherwise kill shake. Playtest call.
- **Slides and fades pause keys through `PlayerMover.inputPaused`** (Dash respects it). Walking eases to a stop by the feel.
- **Warps (area doors) always fade, in every layout.**

**Built:**
- `Assets/Scripts/Camera/CameraFollow.cs` - the rule + `CameraLayout`, `FollowSettings` (smooth 0.12 s, dead zone 1 x 0.75 tiles, look-ahead 1.5 tiles toward `PlayerAim`).
- `CameraRig.cs` - follow, room clamp, slide (0.5 s), fade (0.25 s out + in, black quad made on first use), `Shake(tiles)` (scale 1, decay 2 tiles/s), `SetLayout` (snaps, cancels a transition).
- `Room.cs` - marked rectangle, static register, `Room.At(point)`. `RoomLock.cs` - add-on: doors shut when the player is 2 tiles inside, `Clear()` opens. `Warp.cs` - add-on area door.
- `TestArena`: rooms A (old arena), B (right, locks), C (80x30 above, bigger than the screen), area D (warp pad in C). Built by `Tools/measure/setup-phase3.cs`. `CameraKeys`: L cycles layout, C clears the room. K now also shakes (0.4 tiles).
- `Tools/measure/camera.cs` - camera check.

**Measured / verified (one run each):**
- Compiles, 0 console errors.
- Camera check, all three layouts: 11/11 PASS; 6,865 frames, 0 off whole pixels, 0 frames with the view outside the current room (or room != player's room) outside transitions. Lock holds the player (x 21.38 after 1.5 s walking at the door), Clear opens.
- Movement harness unchanged (0 tiles difference 30 vs 144 fps, dash 3.0000, knockback 2.5667 / 2.5910). Facing check 18/18.
- Shake: 30 frames, every frame moved, max 0.354 tiles, 0 off-pixel.
- First camera-check run failed on frame-count timeouts (editor ran ~300 fps): checks must time by `Time.time`, not frames.
- Not verified: feel of all of it (playtest); fade look; real keyboard (virtual one used).

## 2026-11-14 - Phase 4: health, damage, shooting, enemies, Arena shooter demo

Brief: `Docs/prompt-phase4-combat.md`.

**Decided:**
- **One damage rule, `Damage.Apply` / `Damage.Tick` (static), used through `Health.Hit(amount, push)` by bullets, enemy touches and tests.** Why: one copy of each core rule. Health is whole points; teams stop friendly fire (player 0, enemies 1).
- **A dash with `invulnerable` on now really protects** (`Health` asks `Dash.IsInvulnerable`).
- **Weapons are assets (`Weapon`, settings only), like `MoveFeel`.** Why: the roadmap names a weapon asset and several shooters share one. Not a config system: no behaviour in it.
- **Enemies walk with the same `PlayerMover`** (new `readKeyboard` off + `SetMoveDirection`, input as data), take damage through the same `Health`, shoot with the same `Gun`. Behaviour (Chase / KeepDistanceAndShoot / Wander) is a setting on `EnemyBrain`.
- **Bullets are reused with Unity's own `ObjectPool<Bullet>`** - the measurement asked for it (below). `Bullet.reuse = false` only exists to repeat the measurement.
- **`Warp` now only moves what the camera follows.** Why: enemies have PlayerMovers too; a chaser walked onto C's warp pad, landed in D, and room C could never clear.
- **Invulnerable time counts as over below 1e-4 s.** Why: 60 float steps of 1/60 do not sum to 1, so 1 s lasted 61 steps (trap 79).

**Built:**
- `Assets/Scripts/Combat/`: `Damage`, `Health` (invulnerable time, flash, blink, knockback on the target, optional shake, destroy on death), `Weapon`, `Gun` (fire rate exact across steps, spread fan / seeded random, bullets per shot, magazine + reload or infinite, recoil via knockback, `Refill`), `Bullet` (fixed step, circle sweep each step so nothing is skipped, stops on walls and other-team Health), `PlayerShooter` (mouse/R/1-9/wheel -> Gun), `EnemyBrain`, `RoomWaves` (add-on: waves start when the doors shut, `RoomLock.Clear()` after the last), `RestartOnDeath`, `ShooterHud` (OnGUI readout + win/death text).
- `PlayerMover`: `readKeyboard`, `SetMoveDirection`.
- Assets: `Assets/Data/Weapons/` Pistol, Shotgun, Rapid, EnemyShot; `Assets/Data/Movement/EnemyWalk`; `Assets/Prefabs/Enemies/` Chaser, Shooter, Wanderer. pxkit `enemy.js` (+ fire/green swaps) and `bullet.js`.
- `Assets/Scenes/ArenaShooter.unity` (copy of TestArena, built by `Tools/measure/setup-phase4-assets.cs` + `setup-phase4-scene.cs`): Glide, face the aim, Rooms slide; A has 2 wanderers; B 2 waves, C 3 waves (own doors), D 2 waves; first in the build list.
- Checks: `Tools/measure/combat.cs`, `bullet-alloc.cs`, `playthrough.cs`.

**Measured / verified (one run each unless said):**
- Compiles, 0 errors.
- Combat check, 30 vs 144 fps, 561 fixed steps each: **0 differing rows** (player hp, hits landed, dummy hp and x, bullets alive, ammo). Hits every step land at steps 300/360/420 (1 s invulnerable). 5 hits during a dash: none landed. A pistol shot at a wall 3.5 tiles away is gone 12 steps later (range would be 47).
- Bullet cost, 300 spawns: Instantiate ~4,150 B and ~21-27 us each; pool ~150 B and ~4 us (managed heap + Unity native growth; `GC.GetAllocatedBytesForCurrentThread` returned 0 in the editor). Frame-level "GC Allocated In Frame" was useless: ~110 KB/frame of editor/OnGUI noise either way.
- Playthrough bot (999 health, rapid gun): B, C, D each run all waves and open; win state reached; death at 6 health reloads the scene with full health. First runs found the warp bug and a chaser stuck behind C's pillar (no pathfinding; spawn moved).
- Movement harness unchanged (11 OK), facing 18/18, camera ALL PASS (after the warp change).
- Not verified: feel, numbers and look of any of it (playtest); the real mouse path (the checks drive the Gun as data).

## 2026-11-14 - Phase 5: interaction, pickups, inventory, chests, NPC text, Little RPG room demo

**Decided:**
- **Best target = a plain-points rule** (`Interaction.Pick`): Closest, or Facing (smallest angle in the front half; within 1 degree the closer wins; nothing in front -> closest). **Why:** checks, the player and later AI call one thing. Starting choice, not the user's.
- **One `Inventory` class (plain C#) for the bag and chests**, wrapped by the `Bag` component; the hotbar is the first `hotbarSlots` slots. Chest transfer = `Inventory.Move`. **Why:** one copy of each rule.
- **Text box, chest screen and RPG HUD are OnGUI placeholders** like `ShooterHud`. **Why:** real screens are phase 6; avoids UI Toolkit traps now.
- **Coins are an item in the bag** (stack 99), not a separate counter. Ammo pickup = `Gun.Refill` (phase 4 has magazines, not a reserve).

**Built:**
- `Assets/Scripts/Interaction/`: `Interaction` (rule + `InteractSettings`: reach, best, press/hold, holdTime), `Interactable` (base for add-on targets), `Interactor` (E as data, panels pause walking), `InteractPrompt` (E key cap + hold bar), `TextBox`, `DialogueLines`, targets `Talker`, `LockedDoor`, `Lever`.
- `Assets/Scripts/Inventory/`: `Item`, `Inventory` (+ `ItemStack`, `InventorySettings`: size, hotbar, stack limit), `Bag`, `Pickup` (auto/manual; items, heal, ammo refill; drop), `Collector` (magnet reach/speed, collect reach), `Chest`, `ChestScreen` (click / T take all / E close), `Hotbar` (1-N select, Q drop), `InventoryGui`, `RpgHud`. `Health.Heal` added.
- pxkit sprites coin, key, crown, heart, chest, chest-open, lever, prompt-e; items in `Assets/Data/Items/`, lines in `Assets/Data/Dialogue/`.
- `Assets/Scenes/LittleRpg.unity` (build index 1) via `Tools/measure/setup-phase5-assets.cs` + `setup-phase5-scene.cs`.
- Checks: `Tools/measure/inventory.cs` (edit mode), `rpg-playthrough.cs` (play mode, LittleRpg).

**Measured / verified:**
- `inventory.cs`: 21/21 pass (stacking, full bag leftover, stack limit, remove, chest move both ways, take all, no coins lost, drop; closest/facing/fallback/tie/reach). One run.
- `rpg-playthrough.cs`: 24/24 pass, one run (after fixing a test bug: Health maxHealth set after it started). Guide talk pauses walking, coins, locked door message, key chest take all, door opens and uses key, crown won, drop + take back, Hold lever at 31 steps, heart heals, ammo refills, full-health heart stays.
- Movement (all OK), facing 18/18, camera ALL PASS, combat identical 30/144, playthrough ALL PASS. Console 0 errors.
- Not verified: the mouse path in the chest screen (trap 39), OnGUI look (not in `capture_game_view`), feel of any number (playtest).

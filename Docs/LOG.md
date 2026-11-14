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

## 2026-11-14 - Phase 6: Playground picker, shared screens (UI Toolkit), start menu

Brief: `Docs/prompt-phase6-playground.md`.

**Decided:**
- **All screens are UI Toolkit built in code, hanging from one panel (`KitUI`)** in the game's own 640x360 pixels, scaled by the Pixel Perfect Camera's whole-number ratio. **Why:** one look and one scale for every demo; no UXML/USS files to keep in step. The panel's settings + theme are `Assets/Resources/KitPanel.asset` + `Assets/UI/KitTheme.tss` (a runtime copy is used, so play mode never writes the asset). `KitUI` is a shared helper, not a manager: its callers are `Hud`, `PauseMenu`, `TextBox`, `ChestScreen`, `StartMenu`, `Picker`.
- **One `Hud` for every demo; every part optional** (health bar, weapon + ammo, hotbar with clickable slots, status line, centre banner, help line). Demo rules feed it: `ShooterHud` -> `ArenaGoal`, `RpgHud` -> `RpgGoal` (renamed, same GUIDs), which only set `Hud.Status` / `Hud.Banner`. **Why:** "unify, don't keep copies".
- **The test arena became the Playground** (`TestArena.unity` moved to `Playground.unity`, same GUID; `Assets/Scripts/TestArena/` -> `Assets/Scripts/Playground/`, namespace `TopDownKit.Playground`). `FeelKeys`, `FacingKeys`, `CameraKeys` are **deleted**; the `Picker` (Tab) replaces them and places the aim marker. `KnockKey` (K) stays: it needs the mouse position, which a button click would always put on the panel.
- **Pause = `PauseMenu.IsPaused`** (Esc; `Time.timeScale = 0`). Keyboard readers (`Dash`, `Interactor`, `Hotbar`, `PlayerShooter`, `ChestScreen` T, `KnockKey`, `Picker` Tab) ignore keys while paused, so a key pressed in the menu is not kept for later.
- **A mouse press that starts on a screen never shoots, even while held after the screen closes** (`PlayerShooter.PressStartedOnUI`, read from `KitUI.PointerOverUI` in the frame of the press). Trap 21.
- **Build list: StartMenu (0), ArenaShooter (1), LittleRpg (2), Playground (3).** The pause menu's "Main menu" loads index 0. Death still reloads by the scene's own build index.
- In the Playground the number keys pick weapons, so its `Hotbar.readKeyboard` is off (slots are clicked). Its two wanderers live in area D: in room C they wandered into the measuring lanes and hit the player mid-run.

**Built:**
- `Assets/Scripts/Screens/`: `KitUI` (panel, scale, EventSystem, `PointerOverUI`, box/button/label/slot looks in Endesga colours), `Hud`, `PauseMenu`, `StartMenu`.
- `TextBox` and `ChestScreen` drawn with UI Toolkit; `InventoryGui` (OnGUI slot) deleted.
- `Assets/Scripts/Playground/Picker.cs`: rows Feel, Dash, Facings, Face towards, Aim from, Camera, Weapon, Best target, Use (press/hold), Reach, Magnet, Room (click clears a locked room). `Click(row)` is the entry point.
- `Assets/Scenes/StartMenu.unity`, `Playground.unity` (player now has gun, health, bag, hotbar, interactor, magnet; a chest, the Guide and coins in room A). Scene setup: `Tools/measure/setup-phase6.cs`.
- Check `Tools/measure/screens.cs` (play mode, Playground).

**Measured / verified (one run each):**
- Compiles, 0 errors (one old warning: `Pickup.All` hides `Interactable.All`).
- `screens.cs` at 1920x1080 AND WXGA 1366x768: UI scale = camera `pixelRatio` (3 and 2); a mouse press on a picker button does not shoot and is seen as "on UI"; a press on the floor shoots; picker click changes the feel and 4 clicks cycle back; pause/resume set timeScale 0/1. 7/7 at each size.
- Start menu has 3 buttons; it opens the Playground; pause shows Resume / Restart / Main menu; Main menu returns with timeScale 1.
- Looked at (UI rendered to a texture): picker + HUD at 1920x1080, text box, chest screen with icons. Not looked at over the game picture together.
- Earlier checks all pass on the new scenes: movement (all OK, 30 = 144 fps), facing 18/18, camera ALL PASS, combat identical 30/144, bullet pool 150 B / 3.7 us vs Instantiate 3,980 B / 24 us, Arena playthrough ALL PASS (win banner through the new Hud), RPG playthrough 24/24, inventory 21/21.
- Not verified: the real mouse clicking real buttons (trap 39 - checks call the entry points or queue mouse events); the look in a build; feel (playtest).

## 2026-11-14 - Phase 7: GitHub polish

**Decided:**
- **Custom licence, not MIT.** Copyright (c) 2026 mark-tetelea. Anyone may use and change the kit to make their own games, commercial included; nobody may redistribute or sell the kit itself or claim it. **Why:** the user's choice, relayed by the counselor. **Consequence:** ROADMAP's "MIT" line changed.
- **No GitHub repo, remote or push yet.** The user's call, via the counselor. Repo will live under https://github.com/mark-tetelea.

**Built:**
- `README.md` - what the kit is, choices per system, demos, controls, how to start a game, how it is built (measuring harness), folders, requirements, licence line.
- `LICENSE` - short asset-pack style licence.
- `Media/` - PNG screenshots at 1920x1080: `arena-shooter`, `little-rpg`, `start-menu`, `playground-picker` (settings panel open). No GIFs: the tools here capture single frames only.
- `.gitignore` - adds `Assets/_Recovery/` (+ `.meta`) and `Tools/measure/out/`. Library, Temp, Logs, UserSettings, Builds were already ignored.

**Measured / verified:**
- `BuildPipeline.IsBuildTargetSupported(WebGL)` = False: WebGL module not installed. No build made.
- `ScreenCapture.CaptureScreenshot` in play mode (absolute path outside `Assets/`) captures game AND UI Toolkit panel at the Game view size - unlike `capture_game_view` (trap 81). Play time stuck at 0.02 until `runInBackground = true` (trap 6). `ProjectSettings` unchanged afterwards (trap 77 checked by `git status`).
- README markdown read back once; not rendered on GitHub yet.

## 2026-11-14 - Phase 8: ground tiles and grid-step movement

**Decided:**
- **Ground tiles are one asset class, `GroundTile` (a `TileBase`), with settings: art, `GroundRules` (blocks walking, blocks bullets, kills, damage per second), crumbling (break after, back after, warning/broken art, rules while broken) and door (closed art, rules while closed).** Normal, red, blue, crumbling and door are five assets of it. **Why:** brief - a game adds a tile type by making an asset, not code. **Consequence:** a new kind that is a mix (a slow-killing crumbling tile) is just settings.
- **Tiles block with ordinary colliders, made by `GroundMap` at runtime on two invisible tilemaps beside the floor: "Ground blocks" (stops bullets too) and "Ground low blocks" (marked `ShootOver`, bullets fly over).** **Why:** free movement, grid steps and bullets then need no tile code of their own - one rule for all three, and walls and blue ground are blocked the same way.
- **`ShootOver` is a Combat add-on marker**, not tile code: `Bullet` skips colliders that carry it. Any low fence can use it. Sight: nothing checks line of sight yet (enemies see through walls), so blue blocks no sight.
- **Who stands where = anything with a `Health`, at its collider's centre (`GridStep.StandPoint`).** The body is 0.75 tiles wide, so brushing a red tile with an edge is safe; the centre must be on it.
- **Dash and deadly ground: a dash with `invulnerable` on protects from red and broken holes while it runs; ending the dash on one kills.** **Why:** it falls out of the one damage rule (`Health.Hit` already honours the dash), and it is the familiar "roll over a pit" rule. A dash with `invulnerable` off gives no protection.
- **Damage per second hits once on stepping in, then every 60 steps.** Kills use `Hit(maxHealth)` and are retried each step while invulnerable time blocks them.
- **Crumbling: stepped on -> warning for `breakAfter` -> broken for `backAfter` (0 = stays broken) -> whole.** Counted in whole fixed steps (trap 79). Shipped values: 0.75 s and 3 s, broken = kills. A tile breaking or a door closing under someone pushes them out by physics (not tested).
- **Door ground reuses the existing switches: a `TileDoor` is a plain object whose active state closes the door tiles under it.** Put it in a `RoomLock`'s doors, a `Lever`'s offWhenOn/onWhenOn or a `LockedDoor`'s blockers. No second door system.
- **Grid step is a movement setting in `MoveSettings` (`gridStep`, `stepTime`, `holdToWalk`, `eightDirections`), so it is a `MoveFeel` like Instant or Glide.** Pace = `speed` (tiles/s while walking); `stepTime` = how long the slide between tiles takes (smoothness, never longer than the pace; 0 = jump). Blocked = any solid collider in the target tile other than the mover itself (walls, blue, closed doors, other movers); diagonals may not cut corners and fall back to a single axis. A 4-way keyboard diagonal goes sideways first. Presses are buffered until the current slide ends.
- **Grid step and pushes / dashes: they move the body freely; once the push is over, the mover slides to the nearest free tile centre.** A zone never switches free <-> grid (`SpeedZone.Apply` keeps the mover's own grid settings; a speed zone changes the pace).
- **No demo uses the tiles yet.** Arena and RPG bot runs stay as they are; the tiles live in the Playground.
- **No more per-phase testing: everything is tested in one pass at the end of round 2; a check is kept or written only if it is vital** (a silent failure would break other systems or the demos - e.g. grid steps landing exactly on tile centres). **Why:** the user's call, relayed by the counselor after this phase's checks ran. **Consequence:** the one-off camera check failure is parked for that pass (`Docs/NEXT.md`).

**Built:**
- `Assets/Scripts/Tiles/`: `GroundTile` (+ `GroundRules`, `GroundState`), `Ground` (the one rule: `Crumble`, `Hurt`), `GroundMap` (on a floor tilemap: who stands where, crumbling, hurting, door state, runtime colliders, art by state), `TileDoor`.
- `Assets/Scripts/Movement/GridStep.cs` (the one grid rule: stand point, centres, direction + fallbacks, pace/slide steps, blocked/can-step/nearest-free); `PlayerMover` grid branch; `MoveSettings` grid fields; `SpeedZone.Apply` keeps grid settings.
- `Assets/Scripts/Combat/ShootOver.cs`; `Bullet` skips it. `Health.Hit` now looks up `Dash`/`PlayerMover` if they were added after it woke (a runtime-added dash never protected before - found by the check).
- Assets: `Assets/Data/Tiles/` Normal, Red, Blue, Crumbling, Door; `Assets/Data/Movement/` Grid (5 tiles/s, 0.2 s slide, hold, 4 dirs), Grid 8, Grid tap (8 tiles/s, 0.1 s, one press one tile). Art: `ground-red`, `ground-blue`, `crumble`, `crumble-warning`, `crumble-broken`, `door-open`, `door-closed` (pxkit, `Assets/Art/Generated/`).
- Playground: `GroundMap` on Floor; tile garden in the lower left of room C (red pool, blue strip, crumbling bridge over a red moat, a lever door and a key door - the key is in room A's chest); room B's north door is now door ground (closed while B is locked). Picker row "Grid step": off / Grid / Grid 8 / Grid tap. Setup: `Tools/measure/setup-phase8-assets.cs`, `setup-phase8-scene.cs`.
- `Tools/measure/tiles.cs` - the phase 8 check. `screens.cs` now presses its own virtual mouse (trap 84).

**Measured / verified (one run each unless said):**
- Compiles, 0 errors.
- `tiles.cs` 16/16: red kills at step 1; a dash over red lives through dash steps 2..13 and dies at step 14 when it ends on red; damage per second 2 -> hp 10 -> 4 after 121 steps (hits at 0, 60, 120); blue stops free walking at x -13.375 (radius 0.4 from the edge at -13) and grid steps one tile short of it; a bullet flies over blue to x -6.8; a closed door stops a bullet at y 16.6; crumbling breaks 45 steps after the step and is back 180 steps later at 30 AND 144 fps, the dummy on it dies, no collider while broken (kills mode); door ground closes/opens with a lever (both ways), stays shut without the key and opens with it (key used up), shuts when room B locks and opens on `Clear()`.
- Grid step at 30 vs 144 fps through the real key path: Grid 11 tiles, Grid 8 11 tiles (incl. 4 diagonal), Grid tap 5 tiles - the same tiles in the same order at both rates; 562 settled steps, 0 off a tile centre (max 0), every slide target a centre.
- Earlier checks: movement harness unchanged (Instant 12.0 / Glide 13.2 tiles, stop 2.35, dash 3.0000, knockback 2.5667 / 2.5910, 30 = 144 fps); facing 18/18; camera 11/11 on a second run (first run: Open layout "walk B -> C" stopped at y 17.5 - nothing solid there, not repeated; watch it); screens 8/8 at 1920x1080 (WXGA not re-run); combat 561 steps 0 differing 30 vs 144; bullets pooled 150 B / 4 us vs Instantiate 3,951 B / 38 us; Arena bot ALL PASS; RPG bot 24/24; inventory 21/21.
- Looked at once in game (screenshot): the tile garden reads - red, blue, crumbling, closed and open doors are told apart.
- Not verified: broken-crumble "blocks" mode and a door/tile closing on someone standing there (physics push-out); feel of grid step (playtest).

## 2026-11-14 - Phase 9: melee swing, charge shots, bouncing bullets, aim assist

**Decided:**
- **Melee is a weapon kind (`Weapon.kind` Gun / Melee) with a `MeleeSettings` block (arc, reach in tiles, swing time, cooldown, blade art/width/colour); damage and knockback are the weapon's own.** **Why:** brief - one weapon list, one picker, same `Health.Hit`. The swing itself is an add-on component, `MeleeSwing`, that `Gun` adds on first use. **Consequence:** `Gun` names `MeleeSwing` - phase 14's "delete a folder" check must keep them together (or give Gun a hook then).
- **A swing sweeps from -arc/2 to +arc/2 along the aim over `swingTime`, follows the wielder, keeps its starting aim, hits each target once per swing; walls between stop it, low barriers (`ShootOver`) do not.** Melee ignores `fireRate`: the next swing waits `swingTime + cooldown`. One rule: `Melee.BladeAngle` / `Melee.InSweep` (target size counted: reach to the collider edge, angle widened by its radius).
- **Charge is a weapon setting: `chargeTime` (0 = off), and multipliers at full charge `chargeDamage`, `chargeSize` (bullet radius + art; melee reach), `chargeSpeed`.** Hold to charge, release to fire; scale is linear in the charge (0..1). A press and release between two steps fires uncharged. Charging waits for the cooldown and stops on reload/switch. `automatic` is ignored while charging. `Gun.Charge01` is shown in the HUD's ammo line.
- **Bounces are a weapon setting, `Weapon.bounces` (0 = none).** A wall reflects the bullet inside the same step (no distance lost); range counts the whole path; at most 4 bounces per step. Bullets still fly over `ShootOver` and bounce off "Ground blocks" (closed doors); a Health of another team always stops it.
- **Aim assist is a shooter setting on `Gun` (`AimAssistSettings`: strength 0..1, cone degrees each side, range tiles; strength 0 = off), not a weapon setting.** The rule `AimAssist.Nudge` turns the aim toward the NEAREST living target of another team inside the cone by `strength` of the angle; applied at each shot and swing start (aim still arrives as data). It sees through walls, like enemy sight. Shipped off (0) everywhere; the Playground picker cycles off / 0.35 / 0.7.

**Built:**
- `Assets/Scripts/Combat/`: `Melee.cs` (the one melee rule), `MeleeSwing.cs` (add-on swing + blade art: a plain bar from `Texture2D.whiteTexture`, stretched to reach), `AimAssist.cs` (settings + rule). `Weapon` (kind, melee, charge, bounces), `Gun` (charge state, `Shoot` for both kinds, aim assist, recoil along the assisted aim), `Bullet` (`Spawn` takes damage/size scales; bounce loop; scale reset per launch).
- `Assets/Data/Weapons/`: `Sword` (melee: 2 dmg, arc 120, reach 1.5, swing 0.15 s, cooldown 0.2 s, knockback 10), `Charger` (1 s charge to x4 damage, x2.5 size, x1.5 speed; 6 shots), `Bouncer` (3 bounces, range 24; 8 shots).
- Playground: the player carries Pistol, Shotgun, Rapid, Sword, Charger, Bouncer (keys 1-6, picker "Weapon"); picker row "Aim assist"; help line says 1-6. `Hud` adds "charge NN%". No demo changed.

**Measured / verified (one smoke run in play mode, not a saved check):**
- Compiles, 0 errors; StartMenu, ArenaShooter, LittleRpg, Playground open with 0 console errors.
- Sword: one swing at a target 30 deg off the aim, 1.2 tiles away: hp 5 -> 3 (one hit of 2, not repeated in the swing).
- Charger held to full charge: one hit of 4 (x4).
- Bouncer fired down at the arena wall: came back flying up (y 6.2, dir (0, 1)).
- `AimAssist.Nudge`: target 30 deg off, strength 0.5, cone 40 -> 15.0 deg; strength 0 -> 0; cone 10 -> 0.
- Not verified: feel (playtest), bounce into corners and off door ground, melee through `ShootOver` and blocked by walls, charge bullet look, the existing combat/bot checks (old weapons have charge 0, bounces 0, kind Gun, so their path is the same code order - left for the phase 15 pass).

## 2026-11-14 - Phase 10: pathfinding, boss with phases, groups, trail enemy

**Decided:**
- **Pathfinding is a plain A* over the tile grid (`GridPath`), no navmesh package.** **Why:** the kit's levels are tile grids at 1 tile = 1 unit; a navmesh needs baking, does not know door ground / crumbling / temporary trail tiles, and does not give grid-step walkers tile-by-tile steps. A tile is open when no non-moving solid collider (no body, or a static one: walls, `GroundMap`'s runtime blocks, doors, chests) overlaps a box the walker's size at its centre; movers never block a path. Diagonals never cut corners; 4-way grid walkers search 4 ways. Open tiles are looked up once per fixed step. Start and goal always count as open.
- **Pathfinding is an add-on, `PathWalker` (settings: `replanEvery` 0.5 s, `giveUpDistance` 30 tiles, `avoidHurtingGround` on).** It walks straight when a circle cast of its body to the goal is clear (and crosses no hurting ground); else it follows the path, looking up to 3 tiles ahead and cutting across where clear. No path within give-up distance: straight at the goal (the old behaviour). Grid-step walkers get the direction to the next tile. On the Chaser and Shooter prefabs; Wanderer has none (it does not chase).
- **Shooters with a `PathWalker` walk round a wall that blocks their shot (`PathWalker.ShotClear`, `ShootOver` does not block) instead of firing into it.** Without a PathWalker, behaviour is as before.
- **Enemy code moved to its own folder `Assets/Scripts/Enemies/`** (EnemyBrain moved with its .meta - same GUID): `EnemyBrain`, `GridPath`, `PathWalker`, `EnemyGroup`, `BossPhases`, `TrailMaker`. `EnemyBrain` names `PathWalker` and `EnemyGroup` (same folder). `GridPath` reads `GroundMap` (Tiles) for hurting ground and `BossPhases` writes the `Hud` (Screens) - noted for phase 14.
- **`EnemyBrain` runs before `PlayerMover` (`DefaultExecutionOrder(-20)`).** **Why:** a group follower's direction must be the one used in the same step; it also removes a one-step order lottery. Enemy timing may differ by up to one step from before.
- **`EnemyBrain` now fires melee and charge weapons:** melee swings when the target is within reach + 0.5; a charge weapon is held to full charge, then let go; new setting `alwaysFire` (a gun fires in any behaviour). Old enemies unchanged (kind Gun, no charge, KeepDistanceAndShoot only).
- **Boss = add-on `BossPhases` on an ordinary enemy:** phases (name, `fromHealth` fraction, behaviour, weapon, feel, keepDistance, alwaysFire) switch at health levels; a `switchPause` (0.6 s) stands it still at each switch. Boss bar: `Hud.Boss` / `Hud.BossName` (top centre), filled by `BossPhases.hud`.
- **Groups = add-on `EnemyGroup` on the leader** (followers list, shape Line / Column / Wedge / Ring, spacing 1.5, turn with the leader, `breakRange` 4 = a follower fights on its own while the target is that close, `leaderPace` 0.8). Leader dead = followers on their own.
- **Trail = add-on `TrailMaker` + temporary ground `GroundMap.PaintFor(cell, tile, seconds, owner)`.** The temporary tile's rules apply through the one ground rule; its art is drawn on a runtime overlay tilemap above the floor, fading from 100% to 25%; only plain ground takes it (never holes, doors, crumbling); the owner is never hurt by its own trail. **The kit's trail tile `Trail` hurts 1 per second, it does not kill** (Red would kill on touch - harsh for a trail; a game can drop Red in, it is a setting).

**Built:**
- `Assets/Scripts/Enemies/`: `GridPath.cs`, `PathWalker.cs`, `EnemyGroup.cs`, `BossPhases.cs`, `TrailMaker.cs`; `EnemyBrain.cs` (moved; paths, group following, melee/charge triggers, `alwaysFire`). `GroundMap` temporary ground + overlay; `Hud` boss bar.
- Assets: weapons `BossBouncer` (3 bullets, 2 bounces), `BossCharge` (0.9 s charge to x2 damage, x2.5 size), `BossSword` (melee arc 160, reach 2.2); feels `BossRush` (4.2 tiles/s), `EnemyGrid` (grid step, 3 tiles/s, hold); tile `Trail`. Prefabs `Boss` (40 hp, radius 0.9; phases: bouncer at 100%, charge at 60%, chase with the sword at 30%), `Leader`, `Trailer`, `Stepper` (grid-step chaser, touch range 1.5). pxkit art: `boss` (32x32), `enemy-leader`, `ground-trail`, `enemy-rust` (enemy swap plum:rust).
- ArenaShooter: room E (40x22) east of B, door in B's east wall (closed while B or E is locked), pillars and a middle wall, one wave = the boss with the HUD bar. Playground: a Leader + 3 Chasers (wedge) and a Trailer in room C, a Stepper in area D. Setup: `Tools/measure/setup-phase10.cs` (re-runnable; its reply times out on the 5 s main-thread limit but it completes).
- `Tools/measure/playthrough.cs`: room E added (60 s allowed), the bot walks round walls with a `PathWalker` when its shot is blocked, reports boss phases seen.

**Measured / verified (one run each):**
- Compiles, 0 errors; all four scenes open with 0 missing scripts.
- Arena bot ALL PASS: B 4.2 s, C 27.6 s, D 4.3 s, E (boss) 8.2 s with all 3 phases seen, win shown, death restarts. The first run (bot walking straight) timed out in E with the boss at 11 hp - the bot was shooting into the middle wall; fixed in the bot, not the game.
- Boss probe: placed by the middle wall, the boss (radius 0.9) planned round the wall's end and reached the player in ~5 s.
- Playground smoke (8 s): followers 0.1-3 tiles from their places (5.6 once while turning); the trail had up to 8 tiles at once and the Trailer lived (before the owner fix it died in its own trail by 6 s); the Stepper stood on a tile centre in 336/336 settled steps and hit the player once.
- Not verified: feel and fairness of the boss (playtest), grid walkers with "Grid tap" feels (a held same direction is not a new press - use hold-to-walk feels), groups through narrow doors, path cost with many enemies (not measured).

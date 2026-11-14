# Base 2D movement engine - top-down starter kit

> **Start of every session: read `CONTEXT.md` and `Docs/NEXT.md`.** Search `Docs/LOG.md`,
> `Docs/TRAPS.md` and `Docs/IDEAS.md` - never read them whole. This file stays under
> **30 KB**: dated notes go in `Docs/LOG.md`, never here (`Docs/KEEPING-CONTEXT.md`).
> End a piece of work with `/handoff`.

A reusable **top-down 2D starter kit** (seen from above, like Don't Starve Together,
Deltarune, Diep.io, Enter the Gungeon) - not one game. Future top-down games are built on
it: PC (keyboard + mouse) first, Android and iOS later. It gives a game movement, facing,
interaction, pickups and inventory, and shooting; each game adds its own rules on top.

What the player meets, in order:

- **Move** in 8 directions. Feel ranges from instant (Deltarune) to glide (Diep.io) by
  settings. Speed boosts, slowdowns and knockback/recoil push on the same movement.
- **Face and animate** in 4 or 8 directions (a setting). **Aim is separate from walking**:
  walk one way, aim and shoot another.
- **Interact**: one key uses the best nearby target - chest, NPC or item, one shared system.
- **Pick up**: automatic (coins, ammo) or manual (weapons, key items). An inventory with a
  hotbar; **chests are inventories too**, same system.
- **Shoot**: weapon settings, bullets reused rather than created per shot, health,
  damage, recoil.

## How we work

- Plain words, recommendation first, one question at a time.
- Never `git checkout --` / `restore` / `reset --hard` / `clean` a tracked file; undo by
  the inverse edit. Commit at every point worth resuming from.
- Measure, don't argue. Say when a result is one run or unverified. Feel and fun need a
  playtest - say so instead of quietly retuning.
- No abstraction (managers, event bus, config systems) until three real callers exist;
  name the three when it happens. Prefer deleting over adding.
- **Design test:** *A difference in how games feel is a setting. A feature only some games
  need is an optional add-on piece. Never "if this game, do X" inside the core.*
- C# comments ASCII only. Write files directly, not through big shell heredocs.

## Locked decisions

- **Gameplay runs on a fixed 60 Hz step.** `Time.fixedDeltaTime = 1/60`; movement,
  knockback, bullets and damage run in `FixedUpdate`; rule functions take `dt` as a
  parameter and never read `Time.deltaTime`. Rigidbody2D (interpolated) only stops walls
  being walked through. Why: same feel at any frame rate on PC and phones; keeps replays
  and online play possible. Not promised: bit-exact results across machines. Pause is
  `Time.timeScale = 0`; only menus/UI may run unscaled. (LOG, 2026-10-18)
- **One copy of each core rule** - movement, damage/knockback, inventory, interaction -
  as static functions / one class each, called by the player, enemies, editor previews and
  tests alike. Why: copies drift, and every game built on the kit inherits the wrong one.
  (LOG, 2026-10-18)
- **Input arrives as data.** Movement takes a move direction and shooting an aim
  direction; the rules never read keys or the mouse. Why: touch sticks on Android/iOS
  must drive the same code later. (LOG, 2026-10-18)
- **Pixel art, 16 px = 1 world unit = 1 tile, base resolution 640x360** with the Pixel
  Perfect Camera. **Gameplay numbers are in tiles, never pixels.** Why: whole-number scaling
  to 720p/1080p/1440p/4K; a later game can change art size or style without retuning feel.
  (LOG, 2026-10-18)
- **Claude draws all kit art in code (pxkit) as placeholders; the user draws nothing for
  the kit.** Why: the kit proves systems; real art belongs to each game. (LOG, 2026-10-18)
- **The kit has no score and no world-origin rule.** Why: each game decides its own; aim
  comes straight from the mouse, so nothing infers "forward". (LOG, 2026-10-18)
- **Movement feel lives in a `MoveFeel` asset, one per feel (Instant, Glide); the asset
  only wraps a plain `MoveSettings` struct, and the rule takes the struct.** The user's
  choice: play-mode tuning is kept and movers share feels. An exception for movement
  feel, not a config system. (LOG, 2026-10-18)
- **Directional art is 5 drawings + mirroring** (right, up-right, up, down-right, down;
  left = right flipped). `DirectionalSprites.mirrorLeft` off lets a game supply true 8.
  The user's choice. (LOG, 2026-11-14)

## What exists

Movement, on the Unity URP 2D template (URP settings in `Assets/Settings/`;
`SampleScene.unity` is the template's empty scene). Kit code is in namespace `TopDownKit`.

- `Assets/Scripts/Movement/` - `Movement.Step(velocity, moveDirection, settings, dt)` is the
  one movement rule; `MoveSettings` (speed in tiles/s, time to full speed, time to stop;
  0 = instant), `MoveFeel` (the asset), `PlayerMover` (keys -> direction in `Update`,
  latched until a step uses it; `Step` in `FixedUpdate` on an interpolated Rigidbody2D,
  reading the body's velocity back so walls take speed away). The body gets walking
  velocity + a knockback push, kept apart. **Grid step** is a feel setting
  (`MoveSettings.gridStep`, `stepTime`, `holdToWalk`, `eightDirections`): `GridStep` is its
  one rule (tile centres at n + 0.5, `StandPoint` = collider centre, blocked = any solid
  collider in the target tile); pushes and dashes move freely, then it slides back onto a centre.
- Movement add-ons (phase 1): `Knockback` (the one push rule: `Away`, `Step`; decay is
  `MoveSettings.knockbackStopRate`), `Dash` (add-on component, `DashSettings`, exact distance
  in whole steps), `SpeedZone` (trigger; scales speed or swaps the feel).
- `Assets/Data/Movement/` - `Instant`, `Glide`, `Ice`, `Heavy`, `Grid`, `Grid 8`, `Grid tap` feels, enemy feels `EnemyWalk`, `EnemyGrid`, `BossRush`, `NoFriction` (player collider).
- `Assets/Scripts/Tiles/` (phase 8) - `GroundTile` (the asset: art + `GroundRules` blocks walking /
  blocks bullets / kills / damage per second, plus crumbling and door settings), `Ground` (the one
  rule: `Crumble`, `Hurt`), `GroundMap` (on the floor tilemap: hurts anything with a `Health` by
  where it stands, runs crumbling, door state, art by state; blocking = runtime collider tilemaps
  "Ground blocks" and "Ground low blocks" (`ShootOver`), so movement and bullets need no tile
  code), `TileDoor` (active = its door tiles closed; drive it with `RoomLock`/`Lever`/`LockedDoor`).
  Temporary ground: `GroundMap.PaintFor(cell, tile, seconds, owner)` lays a tile over plain ground
  (rules in force, art on a runtime overlay, fading; the owner is never hurt by it).
  Tiles in `Assets/Data/Tiles/`: Normal, Red, Blue, Crumbling, Door, Trail. A new kind = a new asset.
- `Assets/Scenes/Playground.unity` (was TestArena) - Pixel Perfect Camera 640x360 / 16 PPU,
  Pixel Snapping, Windowbox; floor and wall tilemaps (walls: one merged collider, trap 74);
  the player with every system. `Assets/Scripts/Playground/Picker.cs` (Tab) switches every
  setting live and places the aim marker; K knocks the player from the mouse (`KnockKey.cs`);
  speed pad, mud and ice patch under `Zones`; a chest, the Guide, coins in A; wanderers and a
  grid-step chaser (Stepper) in D; a group (Leader + 3 Chasers) and a trail enemy in upper C;
  the tile garden (every ground tile, a lever door, a key door) in the lower left of C; B's
  north door is door ground.
- `Assets/Scripts/Facing/` - `Facing` (the one facing rule: index 0-7 counter-clockwise
  from right, `Step`/`Snap`, zero direction keeps the facing; `FacingSettings`: 4 or 8,
  face walk or aim), `PlayerAim` (mouse or walk -> `Direction`, data), `PlayerFacing`
  (rule on the fixed step), `DirectionalSprites` (sprite swap idle / 2-step walk, flips the
  left side; no Animator). Player sprites are 48x24 sheets of 3 frames sliced by `Tools/pxkit/slice-sheet.cs`.
- `Assets/Scripts/Camera/` - `CameraFollow` (the one camera rule), `CameraRig` (on the camera;
  layout Open / RoomsSlide / RoomsFade is a setting; whole pixels; slide, fade, `Shake`),
  `Room` (marked rectangle), add-ons `RoomLock` (doors until `Clear()`) and `Warp`. Arena:
  rooms A, B (locks), C (big), area D, boss room E (east of B).
  `Tools/measure/camera.cs` checks pixels and room bounds in all layouts.
- `Assets/Scripts/Combat/` - `Damage` (the one damage rule), `Health` (teams, invulnerable
  time, flash, knockback on the target, dash protects), `Weapon` (asset, settings only),
  `Gun` (input as data: aim, trigger, reload, select; charge = hold/release; aim assist
  `AimAssist.Nudge`, strength 0 = off), `Bullet` (fixed step, circle sweep, `ObjectPool`;
  flies over `ShootOver` colliders; `Weapon.bounces` reflects off walls), melee = `Weapon.kind`
  Melee: `Melee` (the one swing rule) + add-on `MeleeSwing` (Gun adds it), `PlayerShooter` (mouse/keys -> Gun), add-ons `RoomWaves` (clears a
  `RoomLock`), `RestartOnDeath`; `ArenaGoal` (the demo's win/wave text into the `Hud`). Weapons in `Assets/Data/Weapons/`,
  enemies in `Assets/Prefabs/Enemies/` (Chaser, Shooter, Wanderer, Boss, Leader, Trailer, Stepper).
- `Assets/Scripts/Enemies/` (phase 10) - `EnemyBrain` (Chase / KeepDistanceAndShoot / Wander on a
  `PlayerMover` with `readKeyboard` off; runs before the mover; swings melee in reach, holds a charge
  weapon to full; `alwaysFire`), `GridPath` (the one path rule: A* over tiles, open = no static solid
  collider in a box the walker's size; optional "hurting ground is closed"), add-ons `PathWalker`
  (`Steer(goal)`: straight when clear, else a path re-planned on a timer; works for grid-step walkers;
  `ShotClear` - shooters go round walls), `EnemyGroup` (on a leader: followers hold a shape, break off
  near the target), `BossPhases` (phases at health levels swap behaviour, weapon, feel; boss bar
  `Hud.Boss`), `TrailMaker` (leaves temporary ground behind it).
- `Assets/Scripts/Interaction/` - `Interaction.Pick` (the one best-target rule, plain points),
  `Interactable` base + targets `Talker`, `LockedDoor`, `Lever` (and `Chest`, `Pickup`),
  `Interactor` (E as data; open panels pause walking), `InteractPrompt`, `TextBox` +
  `DialogueLines`. `Assets/Scripts/Inventory/` - `Inventory` (the one rule, plain C#, used by
  bag and chests), `Bag`, `Item`, `Pickup` (auto/manual, heal, ammo), `Collector` (magnet),
  `Hotbar`, `ChestScreen`, `RpgGoal` (coins and win text into the `Hud`).
- `Assets/Scripts/Screens/` - UI Toolkit built in code: `KitUI` (the one panel, 640x360 scaled
  by the camera's `pixelRatio`; EventSystem; `PointerOverUI`; shared box/button/slot looks),
  `Hud` (health, ammo, hotbar, status, banner, boss bar, help - all optional), `PauseMenu` (Esc,
  `IsPaused`; Resume / Restart / Main menu), `StartMenu`. `TextBox` and `ChestScreen` draw on
  the same panel. Panel asset: `Assets/Resources/KitPanel.asset` + `Assets/UI/KitTheme.tss`.
  Keyboard readers ignore keys while paused; a press that starts on a screen never shoots.
- Build list: `StartMenu` (0, buttons to the three demos), `ArenaShooter` (1), `LittleRpg` (2,
  the Little RPG room demo), `Playground` (3). Pause -> Main menu loads index 0.
- `Assets/Scenes/ArenaShooter.unity` - the Arena shooter demo (death reloads by build index). `Warp` moves only the camera's target.
- `Tools/measure/` - measuring harness: `movement.cs` (run with `eval_file` in play mode;
  drives the real key path through a virtual keyboard, logs every fixed step) and
  `analyse-movement.js`; it also checks dash distance and knockback decay at 30 vs 144 fps. Re-run it after any change to movement. `combat.cs` (30 vs 144 fps), `bullet-alloc.cs`, `playthrough.cs` on ArenaShooter (rooms B, C, D, boss room E; its bot paths round walls); `inventory.cs` (edit mode), `rpg-playthrough.cs` on LittleRpg; `screens.cs` on Playground (UI scale, click guard, picker, pause; run at 1920x1080 and WXGA); `tiles.cs` on Playground (every tile rule, grid step at 30 vs 144 fps). The movement/facing/camera checks run on Playground.
- `Assets/Art/Palette/endesga-32.gpl` - the one palette; pxkit and Aseprite both load it.
  Never written by a script that also reads it (trap 70).
- `Assets/Art/PixelSprite16.preset` - import default for PNGs under `Assets/Art/**`.
  Not a place for per-sprite settings (pivots are set per file).
- `Tools/pxkit/` - code-drawn sprites (`README.md` there). Must not grow game-specific
  helpers: a drawing used once belongs in its sprite file.

## Do NOT build yet

- **Gamepad support** - the user said no gamepad. Comes off when a game needs it.
- **Touch controls and mobile builds** - until the first mobile game starts. "Input
  arrives as data" keeps the door open.
- **Bit-exact determinism / lockstep netcode** - until a game needs online play.
- **Saving** - not in the kit's list. Comes off when a game needs it; read traps 46-50
  first.
- **Managers, event bus, ScriptableObject config systems, custom pooling frameworks** -
  until three real callers exist (see How we work). `MoveFeel` is the user's one exception.

## Traps

> One line each: symptom -> rule. The full story is in `Docs/TRAPS.md` under the same
> number. Add the day a trap is found; next free number is **86**.

**The editor lies about your own edits**
1. Code right, game wrong -> a changed `[SerializeField]` default does not change a scene that already saved it; edit the scene in edit mode and save.
2. File edit vanishes -> editing open `.unity` YAML on disk does nothing; use `SerializedObject` or reopen the scene.
3. It worked, then didn't -> play-mode changes are discarded on exit; check `Application.isPlaying` first.
4. Measured old code -> no compile during play mode; exit, compile, re-enter.
5. **Measurements report values the source no longer has** -> a compile error keeps the OLD assembly loaded; read the console first.
6. `frameCount` stuck while unfocused -> set `Application.runInBackground = true` at runtime.
7. Stale values in edit mode -> `[ExecuteAlways]` `LateUpdate` does not run continuously.

**Numbers from the wrong place**
8. Radius 5x off the art -> never derive gameplay numbers from `localScale`; data first, art scaled to match.
9. LineRenderer 12x too thick -> width scales with the transform; root at scale 1, art on a child.
10. Comments silently wrong after a tuning change -> grep the files for digits and recompute.
11. A formula quietly stopped being the formula -> "X cancels out" comments are dependencies; grep them.

**Object lifetime**
12. Tests hit destroyed objects -> no cached `FindObjectsByType`; static register in `OnEnable`/`OnDisable`.
13. Player hits scratch objects -> simulation instances must be INACTIVE; still instantiate the prefab.
14. Component permanently dead -> public entry points resolve their own references; `Start` order is not guaranteed.
15. Playing old numbers -> lazily built-from-inspector objects keep their first values all session.

**UI Toolkit**
16. **Every button dead, no warning** -> needs an `EventSystem` + `InputSystemUIInputModule.AssignDefaultActions()`.
17. Screen swallows clicks -> every `VisualElement` is pickable; `PickingMode.Ignore` on containers and labels.
18. UI vanishes, NRE every frame -> domain reload wipes elements; build lazily, rebuild when the panel is gone.
19. Zero-size or old position -> `worldBound` is stale in the call that changed `display`; read next call.
20. Hit test upside down -> `ScreenToPanel` does not flip Y; one shared helper.
21. **Button press also fires gameplay** -> guard on "pointer over pickable", test both halves separately.

**Rules that stop being true**
22. "Never fires" rule starts deciding play -> re-check every such argument when input changes.
23. Budget tied to player speed silently stops working -> use a rule relative to the moving thing, not a constant.
24. Non-ASCII mangled, heredoc "unexpected EOF", words vanish from `python -c` -> ASCII comments, write files directly, no backticks in shell strings.

**The `unity` command line**
25. Every `eval` times out at 5000 ms, CLI otherwise fine -> `set_autotick --enable true --interval_ms 200`.
26. Everything times out right after `recompile` -> still compiling; poll `console_status` until not compiling; one import per call.
27. Half a fix works after a play-mode recompile -> `Awake` is not rerun; set up on first use. "Already in play mode" = someone else is playing. `capture_game_view` is always 1280x720.
28. Play mode stuck on frame 1 -> the editor Pause button is on; check `EditorApplication.isPaused`.
29. Test switch set from code is gone after a recompile -> domain reload restores serialised values; set it again before Play.

**Pixel Perfect Camera and UI**
30. PPC settings "succeed" and change nothing -> set the enums (`m_CropFrame`, `m_GridSnapping`, `m_FilterMode`), not the legacy booleans.
31. PPC type not found, plain camera left -> it lives in `Unity.RenderPipelines.Universal.2D.Runtime`; scan loaded assemblies.
32. UI 1.5x too big off exact multiples -> UI scale must BE the camera's `pixelRatio`.
33. "No SpriteRenderer attached" after adding one -> `??`/`?.` miss Unity's fake null; use `if (x == null)`.
34. UI has labels and draws nothing -> assign `UIDocument.panelSettings` through `SerializedObject`, grep the scene.
35. 9-slice borders too thick -> `referenceSpritePixelsPerUnit = 16` on the PanelSettings.
36. 94% alpha UI is see-through -> judge on a capture; make hiding layers fully opaque.
37. `letterSpacing` does almost nothing -> use `<cspace=Npx>`; judge by width or capture.
38. Disabled text fails contrast -> disabled elements draw at 0.5 opacity; measure on a capture.

**Driving and measuring play mode**
39. Synthetic mouse clicks never press -> drive the game's own entry points / `NavigationSubmitEvent`; say the pointer path is unverified.
40. Fixed-clock play test fires steps in one frame -> wait on conditions, not timestamps.
41. Trace sees the same frame several times -> read durations off the time column, not sample count.
42. On-screen object measures 2,000 px away -> `Collider2D.bounds` stale in the call that activated it; read next call.
43. Scripted `Light2D` lights nothing -> toggle `enabled` off and on before judging.
44. Grown serialised array has a duplicate reference -> `arraySize` copies the last element; set every new slot, read all back.
45. Big sprite imports smaller with odd PPU -> 1024 default cap; set `maxTextureSize`, read back `rect` and PPU.

**Save files (when a game saves)**
46. Save with a part missing loads fine -> `JsonUtility` keeps initialisers; require every field, version the shape.
47. Array-upgrade step never runs -> upgrade by what values mean, not by length.
48. Never-saved field breaks loading -> `GetFields()` includes `[NonSerialized]`; skip `IsNotSerialized`.
49. New version number on old shape -> no recompiling while someone plays.
50. Old backup would delete progress -> fresh backup before every play session; play mode may save at once.

**Tooling outside Unity**
51. Passes outside, CS0171 inside Unity -> assign every struct field in constructors; let Unity compile.
52. `&&` status check passes whatever it says -> grep for the exact ANSWER, not the line.
53. Script truncated to 0 bytes -> never reopen for text write; temp file + rename; gate on `0 Error(s)`.
54. "Unchanged" when nothing was compared -> print what was compared; fail on empty input.
55. Node dies on `D:\package.json` -> every Node tool under `D:\` needs its own `package.json`.

**Art**
56. Drawing guide shows in game -> guides on a hidden Aseprite layer, never in the export.
57. `.aseprite` at the wrong size -> importer defaults to 100 PPU; set `spritePixelsPerUnit = 16` etc. via public properties.
58. Drawn item floats off its placeholder spot -> importer trims and centres; use canvas pivot.
59. Swapped sprite drops half a cell -> PNG pivots centre, `.aseprite` bottom-centre; match pivots.
60. Drawing entirely off-palette -> load the project palette; check colours on arrival.
61. Indexed conversion scrambles colours -> RGB, load `.gpl`, confirm, then Indexed, no dither.
62. A colour paints as an eraser -> palette slot 0 is reserved transparent.
63. Same wrong colour twice -> near-identical dark swatches; read indices from the file.
64. Drawings land in the palette folder -> check where every save landed.
65. Lua export all black -> pass `palette = spr.palettes[1]` to `saveAs`.
66. Half a sprite missing in game -> the background colour is not a paint (`lint.js` checks).
67. One ramp change moves unrelated objects -> a ramp must not do two jobs.
68. Grade destroys shadows -> measure deltaE between slots after any wholesale colour change.
69. Nearest-colour remap flattens art -> pin big areas by role first.
70. Migration does a third of the job -> never read from the path you write; a substitution table is not a pipeline.

**Found in this project**
71. Fixed timestep write "succeeds", changes nothing -> it is a rational in Unity 6; set `Fixed Timestep.m_Count` (2352000 = 60 Hz). Check `console_status` errors after any settings write.
72. Manifest edited, nothing happens for minutes -> an unfocused editor ignores disk changes; ask the user to click its window.
73. Script-painted tilemap is empty -> `OpenScene(Single)` unloads assets loaded before it; open the scene first, then load assets.
74. Wall tilemap has no collision -> toggle the TilemapCollider2D after painting, read shapes next call, then save.
75. Numbers split across CSV columns -> this editor writes decimal commas; use `CultureInfo.InvariantCulture`.
76. `capture_game_view --save_path` made an asset -> it writes under `Assets/`; copy out and `DeleteAsset` the folder.
77. `runInBackground: 1` appears in ProjectSettings after a test -> setting it at runtime writes the Player setting; put it back, check `git status`.
78. Play-mode check times out early -> the editor runs ~300 fps; time steps by `Time.time`, never frame counts.
79. 1 s timer lasts 61 steps -> 60 float steps of 1/60 never sum to 1; end timers below a small epsilon or count steps.
80. Allocation reads 0 bytes -> `GC.GetAllocatedBytesForCurrentThread` returns 0 in this editor; use heap + `Profiler.GetTotalAllocatedMemoryLong` growth.
81. `capture_game_view` shows no UI -> it renders the camera only; render the panel to a RenderTexture (`targetTexture`) to look at screens.
82. Scripted mouse reads (0,0) or the real mouse -> route all input to the Game view, `QueueStateEvent` a `MouseState`, judge "over UI" at the press, not by hovering.
83. Burst of exceptions after a recompile -> play mode was still stopping; wait for `isPlaying` false before `recompile`.
84. Scripted mouse press never lands -> the real mouse's events overwrite it; `AddDevice<Mouse>`, `MakeCurrent`, queue on that, remove after.
85. Long `eval_file` "Main thread operation timed out after 5000ms" -> the script still ran to the end; check its results (`git status`, an `eval`) before re-running it.

## Environment

- **Unity 6000.3.23f1**, 2D, **URP 17.3.0** (2D Renderer). Build target **Windows PC**
  (`StandaloneWindows64`, read back 2026-10-18), landscape 16:9, keyboard + mouse, no
  gamepad. Android and iOS later.
- **Fixed timestep is 1/60** in `ProjectSettings/TimeManager.asset` (`m_Count` 2352000 of
  141120000; `Time.fixedDeltaTime` read back 0.0166666675). Trap 71.
- **Game view** is on Unity's built-in "Full HD (1920x1080)" size. "WXGA (1366x768)" is
  also built in - the size where 640x360 does NOT scale by a whole number (trap 32's test).
- **Repo root is this folder** (`Base_2D_movement_engine`), branch `main`. Open this folder
  in Claude Code, not its parent, so this file loads.
- **Input System only** (`activeInputHandler: 1`, read from ProjectSettings 2026-10-18). The
  old `Input.GetMouseButtonDown` throws at runtime.
- `com.unity.2d.aseprite` 3.0.2 and `com.unity.2d.psdimporter` 12.0.2 come with the
  template, so `.aseprite` and layered `.psd` import directly.
- **Editor driven by the `unity` command line** through `com.unity.pipeline` (0.7.0-exp.1) -
  **not** MCP for Unity on `127.0.0.1:8080` (Orbit's setup; ConnectionRefused forever here).
  Check with `unity status` -> `ready`. Ports go by opening order: with Jewelery Shop open
  too, it holds **7800 and this project is 7801**. Run `unity command ...` from this folder
  (the project is auto-detected - verified by `eval` returning this `dataPath`) or pass
  `--project-path`. **Check which project answered before any write** when both are open.
  Autotick is on (200 ms), or `eval` times out (trap 25). Connected 2026-10-18.
- **Pixel-art import preset:** `Assets/Art/PixelSprite16.preset` is the Preset Manager
  default for every texture under `Assets/Art/**` - PPU 16, Point, Uncompressed, no mips,
  Single sprite, Clamp. Verified on a fresh PNG: 16x16 px = 1.0 world unit. It applies only
  on a file's FIRST import; files already in `Assets/Art` are not changed by it.
- **A C# compile error boots Unity into Safe Mode and the command line cannot connect at
  all.** Fix the compile error; never hand-edit scene files instead.
- `set_player_settings` silently ignores fields it does not know - check `applied` in its
  result, not `success`. Ask `BuildPipeline.IsBuildTargetSupported` about modules; the
  `install-modules --list` status is misleading. (Both from Jewelery Shop.)
- **Aseprite** 1.3.7 at `D:\Aseprite.v1.3.7\Aseprite.exe` (or `aseprite` from bash). Run it
  from bash, headless with `-b --script x.lua`; `print()` never reaches the shell, write
  results to a file. Never script an edit to a file open in Aseprite.
- `D:\package.json` is broken JSON: any Node tool under `D:\` needs its own `package.json`
  (trap 55).
- **pxkit** (`Tools/pxkit`, code-drawn sprites; `.claude/skills/draw-sprite`) reads the
  palette `Assets/Art/Palette/endesga-32.gpl`. Checked end to end 2026-10-18. Art rules:
  `ART.md`; drawing list: `TO-DRAW.md`.
- **`eval` / `eval_file` compile code as a method body:** no `using` lines (write names in
  full), and `Object` is ambiguous (write `UnityEngine.Object`). Number output uses decimal
  commas unless told otherwise (trap 75).
- **Not set up yet, on purpose:** an unlit URP material for LineRenderers (with the first
  LineRenderer - the default renders magenta under URP).

## Which file wins

This file wins over everything else. Then `Docs/LOG.md` (latest entry wins over earlier
ones), then `CONTEXT.md`, then `ART.md`. `Docs/IDEAS.md` is never a decision. The
starter kit's files (`Docs/WORKING-AGREEMENT.md`, `MEASURING.md`, `KEEPING-CONTEXT.md`,
`ASEPRITE.md`) describe other projects - where they disagree with this one, this one is
right.

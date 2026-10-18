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

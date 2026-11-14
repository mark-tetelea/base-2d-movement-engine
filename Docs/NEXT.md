# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 12 (feel additions).

## Do first

1. **Phase 13 of `Docs/ROADMAP.md` (systems: save/load, coin shop, minimap, touch controls)** - when the counselor starts it. A shop buy and a save are new feel events: add them at the END of `FeelEvent` and call `FeelPlayer.Play`.
2. The playtests from `CONTEXT.md`, "What is not done" - now including the sounds, the pixel bits, the hit-stop lengths and the day/night colours (nobody has heard or seen them).

## Half-done (for the phase 15 test pass)

- **Phase 12 was checked by** one smoke run only (LOG 2026-11-14): compiles, 0 errors, all scenes open; hit-stop freezes and returns, does not end a pause; DayNight Fixed tints the light. No check script was written (user: tests only when vital). Phase 15 should check: timeScale is 1 after any mix of freezes, pauses, restarts and scene loads; a freeze during a scene load (Restart from pause, death reload); hit-stop at 30 vs 144 fps (real-time length = different frame counts, same gameplay result); sound pool never allocates per play; effect pool reuses instances.
- Unsure: Lever pulls play DoorOpen even when the lever closes a door (it cannot tell); enemy shots play the Shot sound too (may be noisy with many shooters - a per-team or per-Gun "silent" option may be wanted); `Footsteps` only on keyboard players, not enemies; the trail enemy's goo and red tiles play Hit sounds every damage tick (no freeze by default); DayNight multiplies the global light's own colour (a scene with a coloured global light gets a mixed tint); `Burst` uses `UnityEngine.Random` (looks only, not seeded).
- Phase 14 notes: Feel depends on Combat (`Health`, `Gun`, `Weapon`), Movement (`Dash`), Inventory (`Pickup`), Camera (`RoomLock`), Interaction (`LockedDoor`, `Lever`), Tiles (`GroundMap`), Screens (`KitUI`, `PauseMenu`). Nothing depends on Feel except `Playground/Picker.cs` (rows). DayNight needs URP's `Light2D`.
- From before: phase 11 unchecked items (Fade/Open camera in the Dungeon, bullets on crates, load time with many rooms), phase 10/9 smoke-only items, re-run `combat.cs` and `bullet-alloc.cs`, camera check flake (Open, B -> C, once), broken crumbling in "blocks" mode, tiles closing on someone, `screens.cs` at WXGA, WebGL module + build, GitHub push, GIFs, `Assets/_Recovery/` to delete, real-mouse clicks unverified (trap 39), default font, warning `Pickup.All` hides `Interactable.All`, aim assist bends toward crates.

## Waiting on the user

- Playtests (all feels, grid step, tiles, phase 9 weapons, boss and phase 10 enemies, the Dungeon, and now sounds / hit-stop / effects / tint). WebGL module install, repo push.

## Don't

- Don't make core code call `FeelPlayer`: core raises a static event, Feel listens (keeps Feel deletable for phase 14).
- Don't reorder `FeelEvent` - slots are saved by number; add at the end.
- Don't make hit-stop count fixed steps or change dt: it is timeScale 0, like the pause.
- Don't commit generated audio from anywhere but `Tools/sfx/make-sounds.py`; no downloaded sounds.
- Don't write or run checks per phase beyond the vital ones (user, 2026-11-14).
- From before: no hand-painted generated rooms, no second door system, no Crate class, no navmesh, no tile code in enemies, don't drive mouse presses through `Mouse.current` (trap 84), don't recompile while `isPlaying` (trap 83), don't change the licence, don't push or add a remote without the user saying so.

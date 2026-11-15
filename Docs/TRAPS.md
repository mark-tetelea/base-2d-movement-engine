# Traps

The full story of every trap: symptom, cause, rule. `CLAUDE.md` has one line per trap with
the same number. **Search this file, do not read it whole** (`grep -n -A8 'Pixel Perfect' Docs/TRAPS.md`).

**Add a trap the day it is found**, with the next free number, symptom first. Read the end
of this file fresh before picking a number - two sessions can be working at once.

1-24 come from the starter kit's `TRAPS-unity.md` (Orbit). 25-70 come from Jewelery Shop.
Traps found in this project start at 71.

---

## The editor lies to you about your own edits

**1. Changing a `[SerializeField]`'s default does NOT change a scene that already has it
serialised.** The code reads right, the game behaves wrong, nothing warns. Edit the
component in the scene, in edit mode, then save the scene - or read the value back out
of the running scene and confirm.

**2. Editing a scene's `.unity` YAML on disk while Unity has that scene open does
nothing, and the file and the editor then disagree.** Worse: the next save writes the
editor's stale value back over your edit. Change it through `SerializedObject` in edit
mode and save, or reopen the scene from disk after touching the file.

**3. Anything changed during play mode is discarded on exit.** Adding a component or
editing a value while playing looks like it worked and then vanishes. Check
`Application.isPlaying` before making scene edits.

**4. Unity does not compile scripts while play mode is running.** Edit a script, refresh,
measure in the still-running session, and you have tested the old code. Exit play mode,
let it compile, re-enter.

**5. A compile error leaves the PREVIOUS assembly loaded, and it answers your questions
perfectly well - about the old code.** This is the nastiest one in the list, because the
symptom is "my measurements keep reporting the old values" and nothing suggests a
compile failure. It was hit when a text replacement spanning a comment block silently ate
the declaration underneath it. **When a measurement returns a value the source no longer
contains, read the console for compile errors before believing anything else.** Cheap
guard: reflect for the member you just added and confirm it exists in the loaded domain.

**6. Unity does not tick play mode while the editor window is unfocused - but
`Application.runInBackground = true` set AT RUNTIME fixes it immediately.** Measured:
`frameCount` stuck at 2 over several seconds of wall clock, then running freely the
instant it was set. This is a different thing from the PlayerSettings flag, which did
not work. Set it first and fall back to `EditorApplication.Step()` only when you need
frame-exact control. Being a play-mode assignment, it evaporates on exit like any other.

**7. `[ExecuteAlways]` `LateUpdate` does not run continuously in edit mode.** Anything
derived there can show stale in the Game view until play mode starts.

---

## Numbers derived from the wrong place

**8. Never derive a gameplay number from `transform.localScale`.** Sprites have arbitrary
pixels-per-unit - Unity's own built-in `UI/Skin/Knob.psd` is 0.2 world units at scale 1,
so a radius read off the transform disagreed with the art by 5x. Gameplay numbers derive
from data; the sprite is then scaled and tinted to match, never read back.

**9. A LineRenderer's width scales with its transform, even in world space.** A ring on
an object whose scale carried the art came out 12x too thick. Keep the root at scale 1
and put the sprite on a child - which conveniently also keeps every gameplay number away
from `localScale`.

**10. A number in a comment goes stale the day a tuning constant moves, and nothing
catches it.** One constant change silently falsified figures in six comments across two
files. Where comments carry the *argument* for a number, a wrong one is a defect rather
than untidiness. **When you change a tuning constant, grep the affected files for digits
and recompute** - through the shared accessor, not by hand.

**11. A comment that explains a constant by an algebraic cancellation is a dependency.**
"The host mass cancels out" stopped being true the day half of the relationship changed;
nothing broke, so nothing announced it, and the formula the thresholds were set from had
quietly stopped being the formula. Grep for such comments when you touch either term.

---

## Object lifetime and registries

**12. Do not cache a `FindObjectsByType` list when objects spawn and despawn.** It is
correct only while the set is hand-placed and permanent; the moment anything streams, the
snapshot goes stale within seconds and starts testing against destroyed objects. Use a
static register the component fills in `OnEnable` and clears in `OnDisable`. Mark it
`[ExecuteAlways]` if any edit-mode tool needs to see the same list.

**13. Instances you create for simulation must be INACTIVE, or they join that register
and start affecting the live game.** A content generator that instantiates real prefabs
to test against will otherwise have the player collide with scratch objects parked
wherever the generator last left them. Instantiating the prefab is still right - a bare
`new GameObject` silently picks up class defaults instead of the prefab's serialised
values.

**14. A `public` entry point called from another component's `Start` cannot rely on its
own `Start` having run.** Unity does not order `Start` between components. On the unlucky
ordering the entry point ran with every reference null, disabled itself, and a component
disabled before its own `Start` never gets one - permanently dead, one warning. Public
entry points resolve their own references.

**15. Anything constructed lazily from inspector values keeps the values it was
constructed with for the whole session.** `if (x == null) x = new X(settings)` means
changing an inspector value and restarting leaves you playing the old numbers while
reading the new ones. Every measurement taken that way measures something else, and
nothing looks wrong.

---

## UI Toolkit

**16. A runtime UI Toolkit panel receives NO input at all unless the scene has an
`EventSystem` with an `InputSystemUIInputModule`, and nothing tells you.** Every button
was dead: full press-and-release did nothing, empty console, no warning, and the panel
measured, laid out, drew and hit-tested perfectly. Adding one GameObject with
`EventSystem` + `InputSystemUIInputModule.AssignDefaultActions()` fixed it instantly. Do
not rely on UI Toolkit's internal fallback event system - it did not fire.

**17. Every `VisualElement` is pickable by default, labels included.** So a screen
swallows touches it never meant to. Set containers and labels to `PickingMode.Ignore` at
construction and leave only real controls pickable. A label added later without that line
is an invisible dead zone, and it will not look like one.

**18. A domain reload wipes every `VisualElement` and Unity never calls `Start` again.**
A VisualElement is a plain C# object, so saving a script during play mode - or Unity
restoring play mode after a crash - restores the MonoBehaviour with all its UI references
null and keeps calling `Update`. The interface vanishes and you get one
NullReferenceException per frame. **Build the tree lazily and rebuild whenever its panel
is gone**, never once in `Start`. Clear the root before rebuilding or a second copy picks
up touches the first no longer draws.

**19. `worldBound` read in the same call that changed a screen's `display` is stale.**
Layout has not run yet, so elements report zero size or their previous position. Change
visibility in one call, read geometry in the next.

**20. `RuntimePanelUtils.ScreenToPanel` scales but does NOT flip Y.** Screen coordinates
start bottom-left, panel coordinates top-left, and the conversion applies only the scale.
Without the flip a hit test is upside down - which reads as "the guard works at the top
of the screen and not at the bottom", a half-working that survives a quick look. Put the
flip in one shared helper so it cannot be remembered in one place and forgotten in
another.

**21. If input is "tap anywhere", a button press is also a tap.** The frame a button
exists, one press both presses it and fires the gameplay action. You need an explicit
guard that asks whether the pointer is over anything pickable - and then **test both
halves separately**: that the guard swallows the tap, and that the button still does
something. Only one of those was true for a while, and the working half made the broken
half harder to see.

---

## Rules that stop being true

**22. A rule justified by "it never fires" stops being justified the moment something
lets the player fire it on purpose.** A 3-second timeout was measured at zero firings
over 48 bot runs, and that was the entire argument for an invisible rule being
acceptable. A new input turned the same timer into a resource the player spends
deliberately, and it went from never firing to deciding most runs - so it had to become
visible. Re-check every "it never happens" argument when a new input is added.

**23. A threat or budget calibrated against how fast the player moves goes stale every
time player speed changes.** It happened twice in Orbit: 0.36 -> 1.75 -> 4.16 units per
second, each time from an unrelated change, each time silently, because nothing errors -
runs just stop ending. **The fix that held was a different SHAPE of rule, not a better
constant:** a floor expressed in the player's own terms (never more than N behind where
they have actually got to) cannot go stale, because it is defined relative to the thing
that keeps moving. Where a constant must be re-derived every time something upstream
moves, look for the relative rule instead.

**24. Editing C# with a Python heredoc on Windows mangles non-ASCII** (cp1252 vs UTF-8),
corrupting em dashes into invalid UTF-8. Keep comments ASCII or write bytes explicitly.
Large bash heredocs also fail outright on some payloads with "unexpected EOF" - writing
the file directly is the reliable path. And **a backtick inside a `python -c "..."`
string is shell command substitution** - it silently deleted three words from a generated
C# comment, leaving the code untouched and the comment nonsense. Single-quote the shell
string, or keep backticks out of it.

---

# Carried from Jewelery Shop (2026-10-18)

Copied from `D:\Jewelery Shop\Jewelery shop\CLAUDE.md`, section *Traps found*, entries
25-76. Each keeps its old number as "(JS nn)" so the original is one grep away. Lightly
rewritten so the jewellery game's names do not get in the way; the symptom is kept.
**Left out on purpose** because they were about that game's economy and save rules, not
Unity: JS 63, 67, 71, 74, 76 (JS 66 is folded into 50).

## The `unity` command line and the editor

**25. (JS 25) Every `unity command eval` fails with "Main thread operation timed out after
5000ms", and the CLI otherwise answers perfectly.** The editor does not tick while
unfocused, so the main-thread queue never drains, but `status`, `command` listings and
other non-main-thread calls all still work and look healthy. This is trap 6 wearing a
different hat. `unity command set_autotick --enable true --interval_ms 200` fixes it
immediately.

**26. (JS 43) Right after `recompile`, every `eval`, import and scene script fails with
"Main thread operation timed out after 5000ms", in a row, and looks like the editor has
hung.** It is still compiling, and a compile holds the main thread for longer than the
CLI's 5 s limit. `recompile_status` can already say "compiling" while `eval` calls fail,
and a script that chains straight on runs every step into the wall - on Jewelery Shop a
placeholder import and a whole scene script silently did not happen. Autotick (trap 25)
does not help; nothing is wrong except timing. Poll `console_status` until
`groundTruth.compiling` is false, then carry on. Also: a large `SaveAndReimport` loop in
one `eval` can hit the same 5 s limit on its own - import one file per call.

**27. (JS 41) A fix compiles cleanly, half of it works and the other half silently does
nothing.** If play mode is already running when a script recompiles, Unity 6 reloads the
code into the live game and keeps playing, but does NOT run `Awake` again. The half of
the fix that was rebuilt every frame worked; the half set up in `Awake` did nothing, and
it looked exactly like that half being wrong. Set up what a method needs inside that
method on first use, never only in `Awake` (trap 14 is the same shape). Two more from
the same run: `editor_play` answering "Already in play mode" means another session (or
the user) is already playing - check `EditorApplication.isPlaying` first, because
`editor_stop` then ends THEIR play; and `capture_game_view --source screen` returns
1280x720 whatever shape the Game view is, so a differently shaped view comes back
squashed and cannot be measured off the image - read sizes from the scene or from
`worldBound`. **Two sessions edited Jewelery Shop at once that day** and wrote traps
without seeing each other; read this file fresh before adding a trap number.

**28. (JS 46) Play mode sits on frame 1 with the clock at 0, no errors, and
`runInBackground` does not help.** The editor's Pause button was left on, so play mode
starts paused. It looks exactly like trap 6. Check `EditorApplication.isPaused` before
anything else; set it false to test, and put it back afterwards - it is the user's
toggle.

**29. (JS 75) A test switch set on the scene from code is gone after a recompile.** The
routine was: set a serialised test flag by reflection on a scene component, then Play.
Between two Plays a script was edited and Unity recompiled; the domain reload put the
serialised value back, so the second Play ran with the switch off - and wrote test data
into the user's real save. **After any recompile, set the switch again before Play, and
check what state you are about to touch before touching it.**

## Pixel Perfect Camera and UI Toolkit

**30. (JS 26) Pixel Perfect Camera settings report success and do nothing.** The
component carries BOTH the legacy booleans (`m_UpscaleRT`, `m_PixelSnapping`,
`m_CropFrameX`, `m_CropFrameY`, `m_StretchFill`) and the live enums (`m_CropFrame`,
`m_GridSnapping`, `m_FilterMode`). With `m_ComponentVersion = Version_1` the enums are
authoritative and the booleans are dead migration fodder, so writing all five booleans
succeeds, reports every field found and set, and changes nothing on screen. Iterate the
SerializedObject and set the enums.

**31. (JS 27) A type lookup fails and the whole feature silently degrades.**
`PixelPerfectCamera` lives in `Unity.RenderPipelines.Universal.2D.Runtime`, NOT
`Unity.RenderPipelines.Universal.Runtime`. The first build ran green and logged one line
about the type not being found, leaving a plain orthographic camera that looks almost
right. When resolving a type by assembly-qualified name, scan
`AppDomain.CurrentDomain.GetAssemblies()` for the short name rather than guessing the
assembly.

**32. (JS 40) Every menu comes out up to 1.5x too big and runs across the bars, and
nothing looks broken in the code.** The UI Toolkit panel used Scale With Screen Size
matched to one axis, while the Pixel Perfect Camera zooms the art by a whole number
limited by the other. In any window that is not an exact multiple of the base resolution
the two zooms disagree: the Game view put the art at 2x and the UI at about 3.1x. Exact
multiples happen to agree, which is what hides it. **The UI's scale must BE the camera's
`pixelRatio`, and the UI must live inside the area the art occupies.**

**33. (JS 36) An editor script dies with "There is no 'SpriteRenderer' attached" right
after the line that added one.** `GetComponent<T>() ?? AddComponent<T>()` never adds
anything, because a missing Unity component is a fake null that `??` does not see. Same
for `?.` on a `Transform`. Use an explicit `if (x == null)`.

**34. (JS 37) The UI exists, has the right labels, and draws nothing.** Every element
reports a NaN-sized `worldBound` because the `UIDocument` has no panel settings at
runtime. Setting `doc.panelSettings = ps` from an editor script ran without error and
saved `m_PanelSettings: {fileID: 0}` into the scene. Assign it through `SerializedObject`
(`m_PanelSettings`), then grep the scene file for the reference.

**35. (JS 38) Every drawn frame in UI Toolkit comes out several times too thick.** 9-slice
borders are scaled by the panel's Reference Sprite Pixels Per Unit (default 100) over the
sprite's own PPU, so a 12 px border drew at about 37 at 32 PPU. **Here, set
`referenceSpritePixelsPerUnit = 16` on the PanelSettings.**

**36. (JS 57) A page of "94% opaque" colour shows the world through it.** The background
read back at alpha 0.94 in `resolvedStyle`, with opacity 1 on every ancestor, and on the
capture the world under it was plainly visible - closer to 50%. The cause was not
isolated (the UI is composited through the Pixel Perfect Camera's upscaled render texture
in a linear project; either could do it). **Do not trust a UI alpha to mean what it says
- judge see-through on a capture, and make anything that must hide what is under it
fully opaque.**

**37. (JS 72) Wide-spaced capitals come out tight, and the style says they are spaced.**
`letterSpacing` 6 read back as 6 and the label was its unspaced width. **Measured: this
Unity draws `letterSpacing` at about a hundredth of the pixels asked for** - it behaves
like TextMeshPro's em/100 units. The Advanced text generator changed nothing. **The
rich-text tag `<cspace=Npx>` is exact.** Judge spacing on a capture or by width, never by
the style.

**38. (JS 73) A disabled button's dim text fails contrast, and the colours say it should
pass.** `resolvedStyle.opacity` read 0.5: Unity's default runtime theme draws any element
`SetEnabled(false)` at half opacity. **Measure contrast on a capture of the real state,
never from the two colours** - an inline `opacity` overrides the theme if wanted.

## Driving and measuring play mode

**39. (JS 39) Synthetic mouse clicks move the pointer and never press anything.**
`InputSystem.QueueStateEvent` on `Mouse.current` updated the position but no press
reached the game, with the editor unfocused. Switching `editorInputBehaviorInPlayMode` to
all-input-to-Game-view let the user's real cursor overwrite the queued position instead.
Do not spend another session on it: drive game actions through the game's own entry
points and UI buttons with `NavigationSubmitEvent`, and say plainly that the real pointer
path is unverified. (See `MEASURING.md`: a virtual `Touchscreen` is the other route.)

**40. (JS 70) A play-mode test on a fixed clock fires half its steps in one frame, and one
of them presses a button nobody can see.** A test had steps at 2.2 s, 2.6 s, 3.4 s... The
editor stalled for seven seconds after the first step, so every step up to 7 s ran in the
same frame, and one step pressed a button on a page that was never opened - it was laid
out, so `display` read Flex. **Drive play tests by relative waits and conditions (each
step waits for the one before and for what it needs to be true), and look for a button
inside the page that is showing, not anywhere in the tree.**

**41. (JS 61) A frame-by-frame trace from the CLI sees the same frame several times, and
a long first frame looks like a fast animation.** Driving a multi-second test through
`EditorApplication.update` works in play mode (it keeps running between CLI calls), but
the editor's update fires more often than the game renders, so consecutive samples share
one `Time.unscaledTime`, and one slow editor frame can swallow most of a 0.3 s animation.
Read durations off the time column, not the sample count.

**42. (JS 44) An object that is plainly on screen measures as 2,000 px off it.**
`Collider2D.bounds` read in the same call that `SetActive(true)`-ed the object (or its
parent) can still report where it was before it was moved and switched off, even though
`Physics2D.OverlapPoint` in that same call already hits it where it is. Switch objects on
in one call and read their bounds in the next - trap 19 is the same shape for UI.

**43. (JS 55) A 2D light added by an editor script reports every setting right and lights
nothing.** A `Light2D` created with `AddComponent` in edit mode read back as enabled,
right type, intensity, sorting layer and position, and captures showed only the global
light. It looked exactly like the numbers being too weak, and one retune was spent in
that direction. Toggling it (`l.enabled = false; l.enabled = true`) made it register at
once. Play mode enables lights freshly, so the real game is not affected - but after
scripting a light, toggle it before judging a capture.

**44. (JS 56) A slot set one way shows the other, with no error.** A serialised array had
3 slots; growing it to 5 with `SerializedProperty.arraySize` **copies the last element
into every new slot**. One new slot was set on purpose; the other silently kept a second
reference to an existing object, which was then updated twice per frame with opposite
values. It looked exactly like the display code being wrong. **After growing a serialised
array, set or clear every new slot and read the whole array back.**

**45. (JS 58) A large sprite imports at the wrong size and the wrong pixels per unit, and
nothing warns.** Unity's texture importer caps every texture at 1024 by default, so a
540x1200 image came in as 461x1024 and the importer quietly rescaled its PPU to keep the
world size. Read back `sprite.rect` and `pixelsPerUnit` after importing anything over
1024 on a side, and set `maxTextureSize` to 2048 on it.

## Save files (for when a game built on the kit saves)

**46. (JS 45) A save file with a whole part cut out loads as if nothing were wrong.**
`JsonUtility.FromJson` keeps a field's starting value when the file does not mention it,
so a save with a section missing loaded with a fresh empty one, passed every size check
(the initialiser is the right size) and raised nothing. The same is true of every field
added later and read from an older file. Refuse a file that does not name every field,
and bump a version number whenever the shape changes.

**47. (JS 54) A save-upgrade step that resizes an array can never run.** An array
declared `= new int[N]` keeps its full-length initialiser when the file does not mention
it, so an upgrade guarded on `Length < N` never fired - and a second mechanism happened
to give the right answer, so nothing failed. **Upgrade a field by what its values mean,
not by the array's length.**

**48. (JS 60) A field marked never to be saved stops every save from loading.** A
"file must name every field" check found fields with `GetFields()`, which returns
`[NonSerialized]` fields too. Skip `field.IsNotSerialized`. Any field added to saved
state is a save question, even one that is never saved.

**49. (JS 47) A save written during the session you recompiled in carries the new version
number on the old shape.** Unity 6 reloads changed code into a running play session
without calling `Awake` (trap 27), so the live state keeps the arrays it was built with
while the new version constant is what gets written. Do not recompile while somebody is
in play mode, and run upgrade steps on every file, not only older version numbers.

**50. (JS 65, 66) A save backup taken earlier in the session looks like the user's save
and is not any more.** Between two requests the user played and their save moved on;
restoring the hour-old backup "as usual" would have deleted their progress. Separately,
a game that saves on its first frame of play writes before any CLI call can switch
saving off. **Take a fresh backup right before every play session, compare checksums,
and assume play mode writes the save at once.**

**86. (2026-11-14, phase 13) A settings file written when a scene ends comes out empty.**
`SaveGame.OnDisable` read the settings (volume, minimap mode, touch mode) from their
components to write them on a scene change - but in a scene unload those components are
destroyed first, so it found none and wrote an empty list over the player's settings; the
next scene then had nothing to read. It was only seen by opening the real settings file
after a smoke run. **Write the last values seen while the scene was running (looked at on
a timer), merge with keys the file already has, and read components in `OnDisable` only
when every one of them is still there.**

## Tooling outside Unity

**51. (JS 62) Code that passes every check outside Unity fails to compile inside it.** A
.NET 10 harness auto-defaults a struct field a constructor forgot; Unity's C# does not,
and stopped on CS0171 after 36,000 checks had passed. Assign every field in a struct
constructor, and let Unity compile before believing an outside harness.

**52. (JS 64) A status check in a `&&` chain "passes" whatever it reports.**
`unity command eval ... | grep -o '"result":...' && cp ...` was meant to stop the copy
when Unity was in play mode. grep found the result line - which said `playing=True` - so
the chain went on and new code landed under the user's running session. **Grep for the
ANSWER you need (`"result":"playing=False"`), never for the line that carries it.**

**53. (JS 68) A one-line edit leaves a script empty, and the check meant to stop the copy
waves it through.** `open(p, "w", encoding="ascii")` truncates the file before `write`
raises the UnicodeEncodeError, and the chained check was `grep "Error(s)"`, which matched
"3 Error(s)" - trap 52's shape. **Write a file by replacing bytes in binary mode, or to a
temp file and then rename, never by re-opening it for text; gate on the exact answer
(`0 Error(s)`).** Also: the Write and Edit tools turn an escape like `\u00a0` typed in a
C# string into the raw character, so an escape that must stay ASCII has to be put in by
bytes.

**54. (JS 69) A check prints "unchanged" when nothing was compared at all.** Two
`node -e` reads were piped into `cmp`, which said they matched - both node calls had died
(trap 55) and printed nothing, and two empty outputs are equal. **A comparison must print
what it compared (sizes, counts) and fail on empty input.**

**55. (JS 53) Every Node script under `D:\` dies naming a file you have never heard of.**
`D:\package.json` exists and is not valid JSON. Node walks up from a script to the
nearest `package.json`, finds that one, and exits with `ERR_INVALID_PACKAGE_CONFIG
\\?\D:\package.json` before running a line. The fix is a `package.json` in the tool's own
folder to stop the walk (`Tools/pxkit/package.json` is there for this). Any Node tool
under `D:\` needs its own.

## Art: Aseprite, importers, palettes

**56. (JS 28) A drawing guide baked into a sprite renders in the game.** Safe-area ticks
were written into a placeholder PNG, landed exactly on the screen edges and shipped.
Guides belong on a hidden guide layer in Aseprite, never in the exported file.

**57. (JS 29) A drawn `.aseprite` shows up at the wrong size.** The Aseprite importer
defaults to 100 pixels per unit, not the project's (16 here), and it does NOT use the
`set_import_settings` field names: its serialized field is
`m_TextureImporterSettings.m_SpritePixelsToUnits`, so a search for "pixelsperunit" finds
nothing and reports zero changes. Set the importer's public properties
(`spritePixelsPerUnit = 16`, `filterMode = Point`, `mipmapEnabled = false`, and
`SetImporterPlatformSettings` for uncompressed) and `SaveAndReimport`. Every new
`.aseprite` file needs this once; it then lives in its `.meta`.

**58. (JS 32) A drawn item sits a few pixels off where its placeholder did.** The
Aseprite importer trims the empty border and centres the sprite on what is left, so a
32x32 item with more empty space below than above imported as 26x17 and floated. Place
drawn items by where the art actually is, or set the importer's `pivotSpace` to canvas so
the full cell stays the reference.

**59. (JS 42) A sprite swapped onto a renderer hangs half a cell below the one it
replaced.** A PNG placeholder imports with its pivot in the centre; an `.aseprite`
imports with its pivot at the bottom centre of the canvas. Swapping sprites on one
renderer keeps the transform, so the drawn one dropped. Any sprite that swaps into a
renderer another sprite set up must share its pivot: PNG placeholders standing in for
`.aseprite` art get `spriteAlignment = BottomCenter`. Read `Sprite.pivot` of both before
swapping.

**60. (JS 30) A drawing arrives entirely off-palette and looks fine.** The sprite was
created in RGB mode with Aseprite's built-in default palette, because the game palette
was never loaded. Check every new file's colours against the project palette on arrival.

**61. (JS 31) Converting to Indexed "works" and the colours are still wrong.** Indexed
mode converts to whatever palette is loaded at that moment, and a new sprite starts with
Aseprite's default palette. Loading the game palette into a sprite that is ALREADY
Indexed swaps colours slot by slot and scrambles the picture. The order that works: RGB,
then load the project `.gpl` and confirm the swatches changed, then Indexed with no
dithering.

**62. (JS 34) One palette colour paints as an eraser.** In Indexed mode Aseprite's
transparent colour is index 0, so whatever colour sits in slot 0 looks selectable, shows
`Idx-0`, and erases when painted. A palette for Indexed sprites must keep slot 0 as a
dedicated, never-painted transparent slot.

**63. (JS 33) The same wrong colour arrives twice from two different mistakes.** The
darkest swatches of several ramps look nearly identical on screen. Visual review cannot
catch it; only reading the indices out of the file can.

**64. (JS 35) Drawings keep landing in the palette folder.** Aseprite's Save dialog opens
in the last folder a file was loaded from - always the palette's. Check where a new file
landed on every save.

**65. (JS 59) An Aseprite script exports a frame, and every pixel comes out black at
exactly the right count.** In an Indexed sprite, `Image(spr.spec)` is an indexed image,
and `img:saveAs(path)` writes it with no palette. Pass the palette:
`img:saveAs{ filename = path, palette = spr.palettes[1] }`.

**66. (JS 52) Half a sprite is missing on screen and the file looks complete in every
viewer.** A palette slot was also exactly the colour of the screen behind every sprite;
art painted in it vanished in game while looking whole in an image viewer. **The colour
of the background is the ground, not a paint.** `pxkit/lint.js` checks this.

**67. (JS 48) Raising one ramp for one job raises unrelated objects with it.** One colour
ramp was doing two jobs (the product's metal and the furniture's brown); changing it for
one changed the other. **A palette cannot carry a meaning it never separated.** Check
every ramp for double duty before a wholesale re-palette.

**68. (JS 49) A colour grade "works", looks right, and quietly destroys the shadow end.**
Grading a palette collapsed 33 of 99 slots into one near-black band within deltaE 5 of
each other. Dark art on a dark background reads as intentional, so nothing looks wrong.
Measure pairwise deltaE between slots after any wholesale colour operation.

**69. (JS 50) Re-indexing by nearest colour flattens the picture.** Nearest-colour
matches the colour a slot *has*, not the job it *does*, so an error in the source is
preserved. Pin the biggest areas by role (measured off the file) first and let distance
handle only the leftovers. A colour count is not a measure of whether a remap worked, and
do not guess a slot's role from its name either.

**70. (JS 51) A migration script reads its own output, does a third of the job, and every
number it prints still looks reasonable.** The generator took its source palette from
the file it overwrites; the second run read its own 63-slot output and left every index
above 62 untouched. Two tells were printed and skimmed past (`63 slots (was 63)`, and a
count that went the impossible direction). **A generator must never read from the path it
writes to; freeze the source and assert its expected size.** Same run: substitution rules
applied as a sequence (`48 -> 47` then `47 -> 46`) moved pixels twice. A substitution
table is not a pipeline - break after the first match.

---

# Found in this project

**71. Setting the fixed timestep from code reports success and changes nothing.** Found
2026-10-18, applying Decision 1. `SerializedObject` on `ProjectSettings/TimeManager.asset`,
`FindProperty("Fixed Timestep").floatValue = 1f/60f`, apply, save: `unity command eval`
returned `success: true`, `Time.fixedDeltaTime` still read 0.02 and the file still said
0.02. The only sign was one error in the **Unity console** ("type is not a supported float
value") - nothing in the eval's own result. **Cause:** in Unity 6 `Fixed Timestep` is a
rational, not a float: `m_Count` over `m_Rate.m_Numerator / m_Rate.m_Denominator`
(141120000 / 1). **Rule:** set `Fixed Timestep.m_Count` (141120000 / 60 = **2352000** for
60 Hz) and read back both `Time.fixedDeltaTime` and the file. More generally: **an eval
that "succeeds" can still have logged an error - check `console_status` error counts after
any settings write**, and walk a property's children before writing a value whose type you
have not seen (trap 30 is the same shape).

**72. The manifest was edited and nothing happened for ten minutes.** Found 2026-10-18:
`com.unity.pipeline` was added to `Packages/manifest.json` while the editor sat in the
background behind another project's window. No package resolve, no import, no `.meta` for
new files under `Assets/` - the editor does not look at the disk while unfocused. One click
on its window and it resolved within seconds. **Rule:** before the command line is
connected, only the user clicking the editor window makes it notice disk changes - ask for
that click straight away rather than waiting. Once connected, `AssetDatabase.Refresh()`
through `eval` does it (then read results in the NEXT call, trap 42's shape).

**73. A script paints a tilemap, reports success, and the tilemap is empty.** Found
2026-10-18 building the test arena. One editor script made the Tile and MoveFeel assets,
then `EditorSceneManager.OpenScene(path, Single)`, then painted with `SetTile` and assigned
the feels to fields, then saved. The scene file had the feel references right, the
tilemaps had 0 tiles, and in memory the feel fields read null. **Cause:** opening a scene
in Single mode unloads assets nothing in the new scene uses, so every asset reference the
script held from before the call became Unity's fake null. Assigning one to a serialised
field still saved correctly (by its ID), which hid the problem; `SetTile` with it painted
nothing and said nothing. **Rule:** open the scene first, then load or make the assets the
script uses - or reload them with `AssetDatabase.LoadAssetAtPath` after the open. Count the
painted cells afterwards.

**74. A wall tilemap painted from a script has no collision, even in play mode.** Found
2026-10-18. Walls tilemap with `TilemapCollider2D` (composite operation Merge), a static
Rigidbody2D and a `CompositeCollider2D`: the tiles were there (145 cells, collider type
Grid), but the composite had 0 paths, `Physics2D.OverlapPoint` on a wall hit nothing, and
`ProcessTilemapChanges()` + `GenerateGeometry()` changed nothing. The composite had been
built while the tilemap was still empty and was SAVED that way, and play mode loaded the
saved empty paths. **Rule:** after painting from a script, toggle the TilemapCollider2D
(`enabled = false; enabled = true`), read `pathCount` / `shapeCount` in the NEXT call
(trap 42's shape; trap 43 is the same "toggle it" fix for lights), then save the scene.
Check one wall point with `OverlapPoint` in play mode.

**75. Every number in a CSV written from `eval` splits into two columns.** Found
2026-10-18. This editor's locale writes decimals with a comma (`34,4833336`), so a CSV
built with `ToString()` had whole numbers in one column and decimals in two, and the
analysis read garbage. **Rule:** format numbers with
`System.Globalization.CultureInfo.InvariantCulture` in anything a tool will read back.
`Tools/measure/movement.cs` does.

**76. `capture_game_view --save_path Temp/x.png` creates an asset.** Found 2026-10-18.
The path is taken relative to `Assets/`, so the capture landed at `Assets/Temp/x.png` with
a `.meta`, and a path outside the project is refused outright. **Rule:** copy the file out
and delete the folder with `AssetDatabase.DeleteAsset` (never leave it for git), or read
the inline image instead of saving.

## 77. Test run leaves a ProjectSettings change

**Symptom:** after a play-mode check, `ProjectSettings.asset` shows `runInBackground: 1`.
**Cause:** setting `Application.runInBackground = true` in the editor writes the Player
setting, not just the running session (trap 6's fix).
**Rule:** after any check that sets it, put `PlayerSettings.runInBackground = false` back
and look at `git status ProjectSettings` before committing.

(next number is 78)

## 78. Play-mode check times out early

Symptom: the camera check failed with the player stopped half-way. Cause: timeouts were
written in frames ("600 frames = 10 s"), but the unfocused editor ran ~300 fps, so they
lasted 2 s. Rule: time play-mode check steps with `Time.time`, never frame counts.

## 79. A 1 s timer lasts 61 steps

Symptom: invulnerable time of 1.0 s let the next hit land 61 fixed steps later, not 60.
Cause: subtracting 1/60 as a float sixty times leaves a tiny positive remainder, so the
"still running" test is true for one more step. Rule: end step timers below a small
epsilon (`Damage` uses 1e-4) or count whole steps. Found 2026-11-14 by `combat.cs`.

## 80. Allocation measures 0 bytes

Symptom: `System.GC.GetAllocatedBytesForCurrentThread()` around 300 Instantiate calls
reported 0 B. Cause: this editor's Mono does not implement it (returns 0). Rule: measure
`GC.GetTotalMemory(false) + Profiler.GetTotalAllocatedMemoryLong()` growth around the
calls, and don't trust frame-level "GC Allocated In Frame" in the editor (~110 KB/frame of
editor and OnGUI noise). Found 2026-11-14 (`bullet-alloc.cs`).

## 81. Capture shows no UI

Symptom: `capture_game_view` of the Playground showed the arena and none of the HUD or
picker. Cause: it renders the camera ("source: camera:Main Camera"); a UI Toolkit panel
in overlay mode is not drawn by the camera. Rule: to look at the screens, set the runtime
panel's `targetTexture` to a RenderTexture for one frame, `ReadPixels` it to a PNG, then
set `targetTexture` back to null. Found 2026-11-14 (phase 6).

## 82. Scripted mouse position reads (0, 0) or the real mouse

Symptom: `InputState.Change(mouse.position, p)` and then, a call later,
`KitUI.PointerOverUI` was false over a button; the mouse read (0, 0). Cause: with the Game
view unfocused the editor drops or resets device input, and the real mouse sends its own
position events between calls. Rule: set
`editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView` and `IgnoreFocus`
(put them back after), send `InputSystem.QueueStateEvent` with a `MouseState`, and judge
"over UI" in the frame the press arrives (`PlayerShooter.PressStartedOnUI`), not by
hovering. Found 2026-11-14 (`screens.cs`).

## 83. 99 exceptions in 1.4 s right after a recompile

Symptom: `TextBox.LateUpdate` IndexOutOfRange and `EnemyBrain.FixedUpdate` NullReference,
a burst of ~99, never seen in play. Cause: `recompile` sent 3 s after asking play mode to
stop, which was still running: the domain reload restored private fields - a null array
came back as an empty one, a `System.Random` as null. Rule: wait until
`EditorApplication.isPlaying` reads false before `recompile`; open-state tests read the
data they index (`TextBox.IsOpen` checks the index). Found 2026-11-14.

## 84. Scripted mouse press never lands; both press checks fail

Symptom: `screens.cs` reported "press on picker does not shoot (startedOnUI False)" and
"press on floor shoots (shots 0 -> 0)": no press arrived at all, though every setting from
trap 82 was in place. A queued press read `isPressed` false a second later. Cause: the real
mouse (`Mouse.current`) keeps sending its own state events, which overwrite a queued state
with the buttons up. A virtual mouse made current shot at once. Rule: drive presses through
your own device - `InputSystem.AddDevice<Mouse>("...")`, `MakeCurrent()`, queue on it,
`RemoveDevice` at the end. Found 2026-11-14 (phase 8).

## 85. Long eval_file reports a timeout, but the work is done

Symptom: `unity command eval_file --file Tools/measure/setup-phase10.cs` failed with "Pipeline
server returned 400 ... Main thread operation timed out after 5000ms". Cause: the reply has a
5 s main-thread limit, but the script keeps running on the main thread to the end (it made
every asset, prefab and saved both scenes). Rule: after this error, check what the script
was meant to leave (`git status`, an `eval` that reads it back) before re-running it; a
re-run of a non-idempotent script would do the work twice. Found 2026-11-14 (phase 10).

## 87. A poll loop on `unity command eval` never ends (or ends at once)
Symptom: a shell loop waiting for "RUNNING" to disappear from the reply never stopped.
Cause: the human output repeats your code in its Parameters column, so a word in the code
("RUNNING") is always in the reply. Rule: return a marker the code itself does not contain
as a whole (`"still-going"`), and match on `"result":"<marker>`, never a bare word. Found
2026-11-15 (phase 14).

## 88. Statics survive between Plays in this project
Symptom: none yet - found while adding static state (`Controls`, `GamePause`). Cause:
`EditorSettings.m_EnterPlayModeOptions: 1` = domain reload off when entering play mode, so
static fields keep last session's values (a rebind, a paused flag). Rule: every new static
that holds play state gets a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` reset
(see `Controls.ResetStatics`), or is cleared in `OnDisable`. Found 2026-11-15 (phase 14).

## 89. A "compile without X" check passes because X still compiles from elsewhere
Symptom (avoided): an `AssemblyBuilder` scratch build of the kit minus a folder can quietly
reference `Library/ScriptAssemblies/TopDownKit.*.dll`, so the removed types still resolve and
every removal "passes". Rule: put the kit's own dlls and `Assembly-CSharp*` in
`excludeReferences`, add only package dlls the asmdefs name, and keep a self-test that must
FAIL (Combat removed, its users kept). Found 2026-11-15 (phase 14).

## 90. A virtual keyboard's held key stops counting in the middle of a check
Symptom: a walk driven by a virtual keyboard stops at a random place; nothing solid there (the phase 8 camera
check, "stopped at y 17.5"). Cause: any key event from another keyboard (the user typing anywhere, with input
routed to the Game view) makes that device `Keyboard.current`; `Controls` reads `Keyboard.current`, so the
virtual hold is no longer seen. Rule: a check that drives a virtual keyboard or mouse disables the other
keyboards/mice while it runs and enables them at the end (`RealDevices` in the checks; `run-check.sh` turns all
back on). Found 2026-11-15 (phase 15).

## 91. One pause switch, two owners
Symptom: a shop screen opened during a camera room fade; when the fade ended the player could walk with the
screen open. Cause: `PlayerMover.inputPaused` was one bool set true/false by both the Interactor (panels) and
the CameraRig (fades/slides); whoever finished last won. Rule: anything that stops walking calls
`PlayerMover.PauseInput(itself, on)`; walking stays stopped while any owner holds it. Same for any future
shared flag. Found 2026-11-15 (phase 15).

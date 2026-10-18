# Measuring a running Unity game without lying to yourself

Orbit's whole method was *measure it, do not argue about it*. That worked, and it
produced real difficulty tables, real reachability proofs and real regressions caught.

It also produced six separate occasions where a measurement was confidently wrong and
looked completely right. Those are what this file is for. **Every entry below cost at
least one session, and three of them cost conclusions that were acted on.**

---

## Synthetic input: three things must be right at once

Each one failing looks exactly like the code under test being broken.

**Let the GAME's own frame flush the event.** Do not call `InputSystem.Update()` yourself
and do not drive it with `EditorApplication.Step()`. Either flushes the event in the
editor's input update rather than the dynamic one, so the game sees `isPressed` with **no
press EDGE** - `wasPressedThisFrame` is false, and a tap arrives as a hold nobody started.
That is indistinguishable from a broken input guard. `InputState.Change` sets state but no
edge either. Queue the event and let the running game pick it up.

**Inject through a virtual `Touchscreen`, not the mouse.** The editor keeps re-feeding the
user's real cursor into `Mouse.current`, so an injected mouse position is racing a physical
device and loses. `InputSystem.AddDevice<Touchscreen>()` has nothing behind it - and on a
mobile target, touch is what ships anyway.

**Freeze the clock before reading the result.** See the next section.

---

## The two clock traps, which are mirror images

**Too many frames: a measurement taken thousands of frames after the event is not a
measurement.** With frames running freely there are roughly 2,000 of them between two tool
calls - about 35 seconds of game time. "Press, then read the state" saw a run that had
already acted, flown, timed out and restarted several times, and reported everything calm
as though nothing had happened.

Fix: `Time.timeScale = 0` **first**. `Update` still runs, so input is still read and state
transitions still fire, but `dt` is 0 so nothing moves and no timer advances. The state
change caused by the press just sits there until you read it. This is what turns an
ambiguous result into a clean A/B.

**Too few frames: calling `Update` in a loop inside one frame does NOT advance
`Time.deltaTime`.** Every invocation reads the same value, whatever the editor happened to
be running at, so any time axis derived from the iteration count is fiction. This produced
a reading of 32.3 seconds against a promised 7.4 - which looks exactly like a rate being
wrong by 4x, and was not. **Drive a per-frame rule by calling it with the `dt` you want,
never by spinning `Update` and counting iterations.**

---

## A harness must step everything the rule depends on

Driving the simulation by reflection while leaving the camera parked meant a
camera-derived bounds check never moved, so an out-of-bounds rule fired the moment the run
left the first screen and silently reset the player to the start.

A harness watching only the state machine read that reset as a legitimate event. It looked
exactly like a ping-pong between two objects, and it produced **three rounds of confident,
completely wrong conclusions** about spacing and field sizes.

> If a measurement says the player keeps returning to the start, suspect the reset before
> the physics. Print positions and check the transition was geometrically possible.

---

## The generator's own bookkeeping is not a measurement of the game

A content generator records numbers as it works - how many candidates passed, how many
tests hit. Those are computed against the generator's restricted candidate set, not
against the real world, so they systematically disagree with the game.

Orbit read one such counter as the difficulty statistic. It was even documented in
wording identical to the real metric. It reported 36% where the truth was 45%, and 27%
where the truth was 46% - an apparent third of the difficulty vanishing from a change that
provably could not move it. **Two isolation runs were spent blaming the physics before the
method was suspected.**

Measure the game by instantiating the real thing and running the real rules against all of
it. An independent search reproduced the published table to the percentage point.

---

## Prove your samples are actually different

A seeded builder preferred an already-set `activeSeed` over the seed just passed to it, so
a loop of `UseSeed(s); Build()` rebuilt **one** field N times and silently became a repeat
count. Nothing looks wrong - the per-item numbers are real measurements of a real thing,
there is just only one of it.

It cost a whole session: a difficulty table reported "over 8 seeds", a parameter sweep, a
five-seed crossability run, and - worst - a **determinism check that passed vacuously**,
because it compared a field to itself.

> Print a content hash per seed and assert the hashes differ before believing any
> multi-seed result. The tell was five different seeds reporting identical counts.

---

## Ask the question you actually mean

**Sampling rate vs. the thing being sampled.** "How many of N evenly spaced attempts hit"
measures your sampling rate, not the game, once the target gets small - a target subtending
26 degrees is 1.1 of 16 samples. If the player's input is continuous, the real question is
usually *does a window exist at all*: sample finely (64) and require one hit. *How wide is
the window* is a different question and needs bracketing plus bisection at both edges. The
two read identically in code and must not be confused.

**A per-item test cannot see a global property.** A rule applied item by item, however
strict, cannot detect a wall - a perfectly reachable item on the far side of one passes.
Strictness makes it worse, not better. If the property you want is global (connectivity,
reachability, coverage), the test has to be global: run a search from the start, not a
count per item.

**A guarantee that counts the wrong direction is worse than no guarantee, and it will read
as working for months.** A fairness check accepted progress in any direction, including
sideways, so nothing ever asked whether the space could be *crossed*. Every measured seed
was impassable past a certain point and the reported number was healthy the whole time.
**When a rule is meant to guarantee progress, measure progress.**

**Calibrate a threshold against the statistic you will actually measure.** A floor was set
from "widest window onto anything" (300-900ms) and then applied to "window onto one
target" (78-300ms). The two have the same name, the same units, and differ by a factor of
four. The generator placed zero routes.

**Compare what the player sees.** A colour comparison that ignores alpha is not a
comparison. Two elements read 0.17 apart as raw colours - "too close" - and 0.92 apart
once composited at their real 30% alpha, which nearly caused a fix to something that was
not broken.

---

## Cost discipline

**Order acceptance tests by cost, not by how interesting they are.** An expensive
128-sample measurement ran before a free geometry check that would have rejected most
candidates outright. Deep cases rejected more than half that way, and it was most of the
time spent generating anything.

**Suspect the accessors before the algorithm.** Generating one field took 2.1 seconds, and
it was not the geometry - it was property getters and square roots in a 150-step inner
loop. Caching two values, comparing squared distances, and pre-filtering to the candidates
a straight line actually crosses took it to 137ms with **byte-identical output**.

**Budget per-frame work in work units, not in items.** A build step that took ~40ms
dropped a frame every time it ran. Splitting it into phases run one per call, counting work
units rather than items, put the worst single call at 10.5ms against a 16.7ms budget - and
because the phases were already separate methods in a fixed order, the output was verified
byte-identical at one phase per call, two, and all at once.

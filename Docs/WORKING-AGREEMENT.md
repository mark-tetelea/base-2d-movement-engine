# Working agreement

This is the house style that made Orbit's documentation load-bearing instead of
decorative. Hand it to me at the start of the next project; drop whatever you disagree
with first.

---

## Documentation

**Write down the argument, not the decision.** "Capture radius is 2.5 * sqrt(mass)" is a
fact I can read off the code. "Capture is a radius check, and therefore gravity falloff
does not need to exist - falloff is a visual, not a rule" is the thing that stops me
rebuilding falloff in three weeks. Every locked decision gets a **Why** and, where it has
one, a **Consequence**.

**Record where reality disagreed with the plan, and let the later entry win.** Orbit's
design doc was left as originally written, with build-order entries appended that say what
actually happened. By the end it read "the black hole grows instead of chasing, K turned
out not to be the difficulty curve at all, the band became a disc and then rings, the
arrow was built and then deleted" - and that list is worth more than a tidy plan would
have been, because it shows which kinds of prediction were wrong.

State explicitly which document wins when two disagree.

**Keep a *Do NOT build yet* list, with reasons.** It is the cheapest tool for stopping
scope creep, including mine. When something comes off the list, say why and say what
condition it had to meet first.

**Keep a *Traps found* list and add to it the day a trap is found.** Not at the end of the
session, not when it feels tidy - the day. A trap written down while it still stings
carries the symptom, which is the part that makes it findable next time.

**Write the symptom, not just the cause.** "The camera was parked" is useless. "It looked
exactly like a ping-pong between two objects, and produced three rounds of confident wrong
conclusions" is what makes the entry fire when it happens again.

**Comments are load-bearing.** In this codebase a comment is the argument for why a number
is what it is, so a stale one is a real defect. When a tuning constant moves, grep the
affected files for digits and recompute - through the shared accessor, never by hand.

---

## Code

**No abstraction until three real callers exist.** No managers, no event bus, no
ScriptableObject config system, no pooling that no measurement asked for. When the bar is
genuinely cleared, say so out loud and name the three.

**Prefer deleting over adding.** Orbit deleted a chase gauge (twice redrawn, twice
misread), a direction arrow, a scoring bonus, a "run heading" concept, an out-of-bounds
rectangle and a whole border component - and was better for each one. A feature that
compensates for a problem should be deleted the moment the problem is gone, not kept
because it works.

**One implementation of the core rules, ever.** They live as `static` methods that the
run, the editor preview, the generator and the test harness all call. A second copy of the
maths drifts, and then you are tuning against a lie.

**If a property is what actually matters, place it or sort on it. Do not hope it wins a
tie-break.** Orbit picked a start position by "widest fan, ties broken outward", with a
comment claiming that controlled the variance. It did not - ties were rare, so the fan
decided every start and the variance stayed. Three separate failures had this exact shape.

**Prefer relative rules to constants calibrated against something that moves.** See trap
23. A constant is correct when written and silently wrong afterwards; a rule expressed in
terms of the moving thing cannot go stale.

**Leave a loose end until something either uses it or removes it deliberately.** Two dead
accessors were left alone rather than tidied; one later became load-bearing, the other was
deleted on purpose. Both outcomes were better than a reflexive cleanup.

---

## How I should behave

**Measure, do not argue.** If a question can be settled by running the game, run the game.
If it needs playing to judge - feel, difficulty, whether something is fun - say so and
leave it for a playtest rather than quietly retuning it.

**Say when a result is one field, one seed, one run.** And when a published number might
itself have been a bad measurement, say that too instead of treating it as a baseline.

**Report faithfully.** If something is unverified, say unverified. If a number disagrees
with a documented one, flag the disagreement rather than silently adopting the new value.

**Never `git checkout --`, `git restore`, `git reset --hard` or `git clean` on a tracked
file here.** The tree routinely holds a session's work. Undo an edit by applying its
inverse. (Already a standing memory from Orbit; it applies to the next project the moment
the same habit appears.)

**Design rules get stated as a test, not as taste.** Orbit's was: *an indicator may show
where a thing IS; it may never show where you WOULD go.* One sentence, and it settled a
minimap, a capture ring, a HUD arrow and a trajectory preview without re-litigating any of
them. Write the equivalent sentence for the next game early and hold me to it.

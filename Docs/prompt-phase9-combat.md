# Brief: phase 9 - combat additions

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, medium
> effort). Written 2026-11-14 by the counselor chat.

Build phase 9 of `Docs/ROADMAP.md` ("Round 2").

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md` (round 2,
including phase 14 - build so it already fits "everything swappable").

Scope (feel differences are settings, extras are add-on pieces, one copy of each rule):

1. **Melee swing**: an add-on weapon kind on the same `Damage` rule - arc, reach in tiles,
   swing time, cooldown, knockback; swings along the aim direction.
2. **Charge shots**: a weapon setting - hold to charge, release to fire; charge time and
   what charging scales (damage, size, speed) are settings; 0 = off.
3. **Bouncing bullets**: a weapon setting - number of bounces off walls (0 = none).
4. **Aim assist**: a setting, strength 0 = off; nudges the aim toward the nearest enemy in
   a cone. Aim still arrives as data.
5. One example weapon for each in `Assets/Data/Weapons/`, reachable from the Playground
   picker; placeholder art via pxkit if needed.

**Testing (user, 2026-11-14):** tests only when vital. Make sure it compiles with 0 errors
and the scenes open; skip new checks and full harness runs - a full test pass comes at the
end of round 2 (phase 15). Note anything you were unsure of in `Docs/NEXT.md` for that pass.

Finish with `/handoff` (it commits; include this brief). Do not push. If something is
the user's call, stop and ask; the counselor chat relays it. Plain words, short.

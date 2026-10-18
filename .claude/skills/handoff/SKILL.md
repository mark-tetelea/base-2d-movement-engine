---
name: handoff
description: End a work session cleanly - log what was decided, built and measured into Docs/LOG.md, fix lines in CLAUDE.md and CONTEXT.md that are now wrong, record new traps, rewrite Docs/NEXT.md for the next session, check CLAUDE.md's size, and commit. Use when the user types /handoff, says "wrap up", "end the session", "save where we are", "write the handoff", or when a chat has got long and the work should continue in a fresh one.
---

# Handoff

The files carry the project between chats; the chat does not. This skill makes sure the
next chat starts with everything this one learned. The layout it maintains is described
in `KEEPING-CONTEXT.md` (copied from the starter kit into `Docs/`).

Do the steps in order. Edit the files directly; do not paste their contents into chat.

## 1. Log it - `Docs/LOG.md`

Add one dated section at the end: `## YYYY-MM-DD - <short title>`. Under it, only what
this session changed:

- **Decided:** each decision in bold, then **Why**, then **Consequence** if there is one.
- **Built:** what exists now that did not, by file - one line of *reason* each.
- **Measured / verified:** the claim, the number, how it was measured, how many samples.
  Say "one run" or "unverified" when that is the truth.
- **Overturned:** anything earlier in the log this session proved wrong - "LOG 2026-xx-xx
  said X; it was wrong because Y."

Skip any heading with nothing under it. Never edit an older section.

## 2. Fix what is now wrong - `CLAUDE.md` and `CONTEXT.md`

Read both, find every line this session made untrue, and rewrite those lines in place.

- `CLAUDE.md` gets **rules, not history**: a new locked decision is one or two lines plus
  `(LOG, YYYY-MM-DD)`. No dated build notes, no measurements - those stay in the log.
- `CONTEXT.md` describes the game **as it is now**, as if written today. Rewrite the
  section that changed; do not add a "since then" note at the bottom.

## 3. Traps

For each new trap found this session: one line in `CLAUDE.md` under *Traps* (symptom ->
rule), and the full story in `Docs/TRAPS.md` - symptom first, then cause, then the rule.

## 4. The next session - `Docs/NEXT.md`

Overwrite it. Keep it to one page:

- **Do first:** the next one to three things, concrete enough to start without asking.
- **Half-done:** anything left mid-way, with the file and what is missing.
- **Waiting on the user:** drawings, decisions, playtests. Say exactly what is needed.
- **Don't:** anything this session ruled out that the next might be tempted to redo.

## 5. Size check

Report `CLAUDE.md`'s size in KB. If it is over 30 KB, move its oldest detail into
`Docs/LOG.md` (keeping a one-line pointer) until it is under, and say what moved.

## 6. Commit

Stage the session's work and the notes, and commit with a message that names what was
done. Never `git checkout --`, `restore`, `reset --hard` or `clean` a tracked file.

## 7. Tell the user, in three lines or fewer

What was logged, `CLAUDE.md`'s size, and the first item of `Docs/NEXT.md`. If anything
is waiting on them, say that first.

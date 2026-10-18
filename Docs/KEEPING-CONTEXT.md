# Keeping context

How the project's notes are laid out so every new chat starts informed, and so the
notes never get too big to load. Added 2026-10-18, from what went wrong on Jewelery Shop.

---

## The lesson this file exists for

`CLAUDE.md` is loaded into **every** session, in full, before Claude does anything.

- Orbit's reached 1,346 lines and stayed useful.
- Jewelery Shop's reached **6,300+ lines / 584 KB (~146,000 tokens) by 2026-10-14**,
  because every "Built ..." and "Measured ..." note was appended to it. Every session -
  even "redraw one ring" - paid for all of it first.

The fix is not "write less". It is: **one small file that is always loaded, and a big
file that is only ever searched.**

---

## The files, and the one job each has

| File | Loaded when | Its job | Keep it under |
|---|---|---|---|
| `CLAUDE.md` | every session, automatically | The rules: the game in one paragraph, how we work, locked decisions in a line or two each, traps one line each, environment, which file wins | **30 KB** (~7,500 tokens) |
| `CONTEXT.md` | start of every session | Where the game is *right now*: how it plays, the screens, what is not done. Rewritten, never appended | 12 KB |
| `Docs/NEXT.md` | start of every session | The handoff: what to do first next time, anything half-done, anything waiting on you | one page |
| `Docs/LOG.md` | never read whole - searched | Everything dated: built, measured, decided - with the full argument. Only ever added to | no limit |
| `Docs/TRAPS.md` | searched before risky work | Each trap's full story: symptom, cause, rule | no limit |
| `Docs/IDEAS.md` | searched | Ideas that are not decisions. Nothing here is promised | no limit |
| `ART.md` | art work only | Palette, sizes, import settings, drawing rules (see `ASEPRITE.md`) | 20 KB |
| `TO-DRAW.md` | art work only | One line per drawing: what, size, who draws it, done or not | - |

**Claude's own memory** (`~/.claude/projects/<project>/memory/`, written automatically)
is for things about *you* and how you like to work. Things about *the game* go in the
files above - they are in git, and you can open and read them.

---

## Five rules that keep it small

1. **A dated note never goes in `CLAUDE.md`.** "Built 2026-10-12: upgrades are tools" goes
   in `Docs/LOG.md`. `CLAUDE.md` gets at most one line: *Upgrades are tools, not levels
   (LOG, 2026-10-12).*
2. **`CLAUDE.md` and `CONTEXT.md` are rewritten, not added to.** When something changes,
   fix the line that is now wrong. The old version is in git and the argument is in the log.
3. **Check the size at every handoff.** If `CLAUDE.md` is over 30 KB, move the oldest
   detail into `Docs/LOG.md` first, and say so.
4. **Search, don't read,** for `LOG.md`, `TRAPS.md`, `IDEAS.md`, `ART.md`: `grep -n -A5 'furnace' Docs/LOG.md`.
5. **Corrections still say so out loud** - "this used to say X, which was wrong" - but in
   the log. In `CLAUDE.md`, just fix the line.

---

## Starting a session

`CLAUDE.md` tells Claude to read `CONTEXT.md` and `Docs/NEXT.md` first, so you only have
to say what you want done. If `NEXT.md` lists something waiting on you, Claude says so
before starting.

## Ending a session: `/handoff`

Type `/handoff` when a piece of work is done, or when the chat has got long. It:

1. adds today's decisions, builds and measurements to `Docs/LOG.md`
2. fixes any line in `CLAUDE.md` / `CONTEXT.md` that is now wrong
3. adds any new trap (one line in `CLAUDE.md`, the story in `Docs/TRAPS.md`)
4. rewrites `Docs/NEXT.md`
5. checks `CLAUDE.md`'s size
6. commits

The skill is in this kit at `skills/handoff/SKILL.md`; the kickoff copies it into the
new project's `.claude/skills/`.

## One chat per job

- **Start a new chat for each piece of work** (one feature, one batch of drawings). Long
  chats get squashed into a summary and the summary loses detail. The files carry the
  project between chats - not the chat.
- **Batch art into one chat.** Looking at pictures is the expensive part.
- **Big jobs get a written brief first.** Jewelery Shop's `Docs/prompt-*.md` files worked:
  write the brief in one chat, do it in a fresh one.

# Brief: phase 7 - GitHub polish

> First message of a new chat opened in `Base_2D_movement_engine` (Opus 5.5, high
> effort). Written 2026-11-14 by the counselor chat.

Build phase 7 of `Docs/ROADMAP.md`: make the repo look good on GitHub.

Start by reading `CLAUDE.md`, `CONTEXT.md`, `Docs/NEXT.md`, `Docs/ROADMAP.md`. The user
skipped playtesting so far - don't block on it.

Scope:

1. **README.md** at the repo root: what the kit is (one paragraph), a feature list
   organised by system with the CHOICES each one offers (that is the selling point), the
   three demos + start menu, controls, "how to start a game from the kit", folder map,
   requirements (Unity 6000.3, URP 2D), the measuring harness as a quality point. Plain,
   confident, no hype.
2. **GIFs / screenshots**: one short clip per demo (Arena shooter, RPG room, Playground
   picker switching feels/layouts) into a `Media/` folder outside `Assets/` (trap 76:
   captures land under `Assets/` - move them out). Drive play mode with the bots/checks.
   Keep each GIF small (under ~5 MB). If you cannot make GIFs with the tools here, make
   PNG screenshots and say so.
3. **LICENSE**: MIT, copyright holder "the kit's author" placeholder - ASK the counselor
   for the name to put there before committing it, don't guess.
4. **WebGL build**: check `BuildPipeline.IsBuildTargetSupported` for WebGL first. If the
   module is NOT installed, stop and report - installing it is the user's call. If it is,
   build to a `Builds/WebGL/` folder that is git-ignored, and check it loads locally.
5. A `.gitignore` check: Library, Temp, Logs, UserSettings, Builds, `Assets/_Recovery`
   are ignored.

**Do NOT** create a GitHub repo, add a remote, or push - that is the user's call; the
counselor will ask them. Commit locally only.

Done means: README renders cleanly (check the markdown), media in place, licence ready,
WebGL either built and loading or clearly reported as blocked; finish with `/handoff`
(it commits; include this brief). Plain words, short.

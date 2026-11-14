# Next

> Overwritten at every `/handoff`. Written 2026-11-14, end of phase 7 (README, licence, screenshots).

## Do first

1. **The user decides on WebGL:** install the WebGL Build Support module in Unity Hub (the user's call), then build to `Builds/WebGL/` (git-ignored) and check it loads locally. It is the first real build: check the screens and start menu in it.
2. **The counselor + user create the GitHub repo** (https://github.com/mark-tetelea) and push. Check the README renders there (tables, images from `Media/`).
3. The playtests from `CONTEXT.md`, "What is not done".

## Half-done

- GIFs: only PNG screenshots in `Media/`. A GIF needs a screen recorder (e.g. ScreenToGif) or a frame-sequence script + converter; keep each under ~5 MB.
- `Assets/_Recovery/` still on disk (now git-ignored). Delete it in the Project window.
- The real mouse clicking real buttons is unverified (trap 39).
- Placeholder font is Unity's default (smooth, not pixel).
- Old warning: `Pickup.All` hides `Interactable.All` (CS0108).

## Waiting on the user

- WebGL module install, repo creation and push, playtests.

## Don't

- Don't change the licence to MIT: the user chose a custom one (LOG 2026-11-14).
- Don't push or add a remote without the user saying so.
- Screenshots: `ScreenCapture.CaptureScreenshot` with an absolute path in `Media/`, in play mode with `runInBackground = true`; not `capture_game_view` (no UI, lands in `Assets/`).
- Don't recompile until `EditorApplication.isPlaying` reads false (trap 83).

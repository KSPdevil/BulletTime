# Changelog

All notable changes to Bullet Time are documented here.

## 1.6.0 — final release

- Panel moved flush to the left edge of the screen (left margin 130 → 4 reference pixels).
  The panel sits below the left shortcut bar and above the character portraits, so the native
  UI is not covered.
- Fixed the hover tooltip appearing in the middle of the screen instead of next to the button.
  The game positions tooltips with `UICamera.WorldToViewportPoint(TooltipPlace.position)`; the
  buttons lived on a separate Screen Space Overlay canvas, so every candidate pivot was considered
  off-screen and the tooltip fell back to the screen centre. Each button now has an invisible
  anchor inside the game UI canvas, kept in sync with the button's screen position, and the
  tooltip uses that anchor as its placement source.
- Removed all debug code: no log file, no `debug.flag`, no heartbeat, no diagnostic dumps and no
  installation trace output. Only genuine error handling remains (init failures are reported to
  the UMM log, never to an extra file).
- Added English and Chinese README files, this changelog and the Nexus Mods description.
- Build script accepts `-GameDir` so the repository can be built outside the original machine.

## 1.5.0

- Rearranged the four presets into a single horizontal row placed in the bottom-left gap,
  between the left shortcut bar and the character portraits.
- Verified in a live combat log that the slowdown is actually applied
  (`PlayerTimeScale = 1`, `Time.timeScale = 0.5` while combat is active).

## 1.4.0

- Rebuilt the panel on its own Screen Space Overlay canvas with manual button placement.
  Buttons became reliably visible; the previous nested-canvas approach could render with
  `alpha = 1` yet remain invisible.

## 1.3.0

- Reworked the self-built panel; the buttons were still invisible because the panel was
  nested under the game's Screen Space - Camera canvas and the layout group did not run.

## 1.2.0

- Switched to a fully independent panel. No object owned by BubbleBuffs is read or modified.

## 1.1.0

- Attempted to append the buttons to the game's native sidebar grid. The grid was already
  full (12 of 12 cells), so new buttons were laid out outside the container and stayed invisible.

## 1.0.0

- Initial release: automatic bullet time in real-time combat, speed restoration after combat,
  pause/resume safe, four preset buttons, bilingual UI.

# Bullet Time

A small quality-of-life mod for **Pathfinder: Wrath of the Righteous** that automatically slows
down real-time combat and restores normal speed when the fight is over.

[中文说明](README.zh-CN.md)

---

## Features

- **Automatic bullet time** — when a real-time (non-turn-based) combat starts, the game speed is
  lowered to the selected multiplier. When combat ends, the original speed is restored.
- **Pause / resume safe** — pausing and unpausing does not reset the multiplier. The value is kept
  in the game's own player time scale, so resuming continues at the chosen speed.
- **Turn-based combat is untouched** — the mod only acts in real-time combat.
- **On-screen preset buttons** — four buttons in the bottom-left corner of the HUD let you switch
  the multiplier at any time. The panel follows the game HUD (hidden in menus, cutscenes and dialogs)
  and scales with the game UI.
- **Bilingual UI** — button labels and tooltips are provided in English and Simplified Chinese and
  follow the game language dynamically, including after changing it in the options.
- **No extra dependencies** — does not require, read or modify BubbleBuffs or any other mod.
  A native sidebar button is used only as an appearance template.
- **No log files** — release builds do not write any log or debug file.

## Speed presets

| Button | Game speed | Meaning |
|--------|-----------|---------|
| `0.25x` | 25% | Very slow, for precise real-time tactics |
| `0.5x`  | 50% | Half speed (default) |
| `0.75x` | 75% | Slightly slower than normal |
| `OFF`   | 100% | Bullet time disabled |

The multiplier is applied to the game world time scale. UI, menus and the mouse cursor are not
affected. The selected preset is saved to `settings.json` in the mod folder and restored on the
next launch.

## Installation

1. Install [Unity Mod Manager](https://www.nexusmods.com/site/mods/21) (UMM 0.32.x is recommended).
2. In UMM, open the **Mods** tab and click **Install Mod**.
3. Select `BulletTime-UMM.zip`.
4. Start the game. The four buttons appear in the bottom-left corner of the HUD.

Manual installation: unpack the zip so that the following files exist:

```
<GameFolder>\Mods\BulletTime\BulletTime.dll
<GameFolder>\Mods\BulletTime\Info.json
```

## Uninstallation

- In UMM: **Mods** tab → select **Bullet Time** → **Uninstall**.
- Or simply delete the `<GameFolder>\Mods\BulletTime` folder.

No save-game data is modified. `settings.json` (if present) can be deleted as well.

## Compatibility

- **Game**: Pathfinder: Wrath of the Righteous — Enhanced Edition (PC, Steam / GOG / Epic).
- **Loader**: Unity Mod Manager 0.32.x (tested with 0.32.2).
- **Other mods**: independent of BubbleBuffs and other UI mods. The mod only reads one native
  sidebar button as a visual template and does not modify any game or third-party objects.
- **Saves**: safe to add or remove at any time; the mod stores nothing in save files.
- **Multiplayer / co-op**: not tested.

## Known limitations

- Turn-based combat is intentionally not affected.
- Only real-time combat in the normal game mode is slowed. Scripted scenes, cutscenes and
  full-screen UI keep their own timing.
- If another mod also writes `TimeController.PlayerTimeScale` every frame, the two mods may
  override each other.
- The button panel is anchored to the bottom-left corner. Another mod that places its own UI in
  the same spot may overlap it.
- The panel is hidden together with the in-game HUD.

## Building from source

Requirements: Windows, PowerShell 5.1+, .NET SDK (for the bundled Roslyn compiler) or the
.NET Framework 4.x C# compiler, and a local installation of the game (for the managed assemblies).

```powershell
cd BulletTime
.\build.ps1 -GameDir "C:\Path\To\Pathfinder Wrath of the Righteous"
```

The script compiles `Main.cs` into `BulletTime.dll` and packages `BulletTime-UMM.zip`.
Use `-NoPause` for non-interactive builds. `BUILD.bat` is a convenience wrapper.

## Repository layout

```
.
├── BulletTime/
│   ├── Main.cs        # mod source
│   ├── Info.json      # Unity Mod Manager manifest
│   ├── build.ps1      # build + package script
│   └── BUILD.bat      # Windows convenience wrapper
├── README.md          # this file
├── README.zh-CN.md    # Chinese readme
├── CHANGELOG.md       # version history
├── LICENSE            # MIT license
└── NEXUS_DESCRIPTION.md  # Nexus Mods description (BBCode)
```

## License

Released under the [MIT License](LICENSE). You are free to use, modify and redistribute the
source code and the compiled release files, provided the copyright notice and license text are
kept.

## Credits

- Author: KSPdevil
- Built with Harmony 2.x and Unity Mod Manager.
- Button appearance is cloned from the game's own sidebar button at runtime.

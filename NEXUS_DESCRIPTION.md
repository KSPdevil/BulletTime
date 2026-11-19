[size=5][b]Bullet Time[/b][/size]

Automatic slow motion for real-time combat in [b]Pathfinder: Wrath of the Righteous[/b].
Combat starts — the game slows down. Combat ends — normal speed comes back. You choose how slow.

[size=4][b]Features[/b][/size]
[list]
[*][b]Automatic bullet time[/b] in real-time combat, with automatic speed restore when the fight ends.
[*][b]Pause / resume safe[/b] — pausing never resets your chosen multiplier.
[*][b]Turn-based combat is untouched.[/b]
[*][b]Four on-screen presets[/b] in the bottom-left corner: 0.25x, 0.5x, 0.75x and OFF (100%).
[*][b]Follows the HUD[/b] — the panel hides in menus, dialogs and cutscenes and scales with the game UI.
[*][b]English and Simplified Chinese[/b] UI, switched automatically with the game language.
[*][b]No dependencies[/b] — does not require or modify BubbleBuffs or any other mod.
[*][b]No log files[/b] and no save-game changes; safe to add or remove at any time.
[/list]

[size=4][b]Speed presets[/b][/size]
[list]
[*][b]0.25x[/b] — very slow, for precise real-time tactics
[*][b]0.5x[/b] — half speed (default)
[*][b]0.75x[/b] — slightly slower than normal
[*][b]OFF[/b] — bullet time disabled, combat runs at 100%
[/list]
The selected preset is saved to [i]settings.json[/i] in the mod folder and restored on the next launch.

[size=4][b]Installation[/b][/size]
[list=1]
[*]Install [b]Unity Mod Manager 0.32.x[/b].
[*]Open the [b]Mods[/b] tab and click [b]Install Mod[/b].
[*]Select [b]BulletTime-UMM.zip[/b].
[*]Start the game — the four buttons appear in the bottom-left corner of the HUD.
[/list]

[size=4][b]Uninstallation[/b][/size]
Uninstall the mod in Unity Mod Manager, or delete [i]Mods\\BulletTime[/i]. No save data is touched.

[size=4][b]Compatibility[/b][/size]
Pathfinder: Wrath of the Righteous — Enhanced Edition (PC). Unity Mod Manager 0.32.x.
Independent of other UI mods. Not tested in multiplayer.

[size=4][b]Known limitations[/b][/size]
[list]
[*]Turn-based combat is intentionally not affected.
[*]Scripted scenes and full-screen UI keep their own timing.
[*]Another mod that writes [i]TimeController.PlayerTimeScale[/i] every frame may override or be overridden by this mod.
[*]The panel is anchored bottom-left; UI from other mods in the same corner may overlap it.
[/list]

[size=4][b]Credits[/b][/size]
Author: KSPdevil. Built with Harmony 2.x and Unity Mod Manager.
The button look is cloned from the game's own sidebar button at runtime.

[hr]

[size=5][b]子弹时间（Bullet Time）[/b][/size]

即时制战斗自动减速，战斗结束自动恢复正常速度。倍率由你决定。

[size=4][b]功能[/b][/size]
[list]
[*][b]自动子弹时间[/b]：即时制战斗开始时减速，结束时自动恢复。
[*][b]暂停/继续不丢档位[/b]，不会重置倍率。
[*][b]不影响回合制战斗。[/b]
[*][b]左下角 4 个倍率按钮[/b]：0.25x、0.5x、0.75x、关闭（100%）。
[*][b]跟随 HUD[/b]：菜单、对话、过场时自动隐藏，随游戏 UI 缩放。
[*][b]中英双语[/b]，随游戏语言自动切换。
[*][b]无额外依赖[/b]：不依赖、不修改 BubbleBuffs 或其他 mod。
[*][b]不产生日志[/b]，不写入存档，可随时添加或移除。
[/list]

[size=4][b]倍率说明[/b][/size]
0.25x 很慢，适合精细操作；0.5x 半速（默认）；0.75x 略慢；关闭为 100%。
档位保存在 mod 目录的 [i]settings.json[/i]，下次启动自动恢复。

[size=4][b]安装[/b][/size]
[list=1]
[*]安装 [b]Unity Mod Manager 0.32.x[/b]。
[*]打开 [b]Mods[/b] 页，点击 [b]Install Mod[/b]。
[*]选择 [b]BulletTime-UMM.zip[/b]。
[*]启动游戏，HUD 左下角出现 4 个按钮。
[/list]

[size=4][b]卸载[/b][/size]
在 Unity Mod Manager 中卸载，或删除 [i]Mods\\BulletTime[/i]。不会修改存档。

[size=4][b]兼容性[/b][/size]
开拓者：正义之怒 增强版（PC），Unity Mod Manager 0.32.x。
与其他 UI mod 相互独立，多人模式未测试。

[size=4][b]已知限制[/b][/size]
[list]
[*]回合制战斗刻意不受影响。
[*]剧情脚本与全屏界面保持自己的时间设置。
[*]若其他 mod 也每帧写入 [i]TimeController.PlayerTimeScale[/i]，可能互相覆盖。
[*]面板固定在左下角，同一位置的其他 mod UI 可能重叠。
[/list]

[size=4][b]致谢[/b][/size]
作者：KSPdevil。基于 Harmony 2.x 与 Unity Mod Manager，按钮外观在运行时克隆自游戏原生侧边栏按钮。

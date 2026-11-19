# 子弹时间（Bullet Time）

《开拓者：正义之怒》的小型体验优化 mod：即时制战斗自动减速，战斗结束自动恢复正常速度。

[English README](README.md)

---

## 功能

- **自动子弹时间**：进入即时制（非回合制）战斗后，把游戏速度降到当前选择的倍率；战斗结束后
  恢复原来的速度。
- **暂停/继续不丢档位**：暂停再继续不会把倍率重置回 1 倍。倍率保存在游戏自身的
  `PlayerTimeScale` 中，继续游戏时会直接按所选速度运行。
- **不影响回合制战斗**：只在即时制战斗中生效。
- **屏幕按钮**：HUD 左下角有 4 个倍率按钮，可随时切换。面板跟随游戏 HUD 显隐（菜单、过场、
  对话时自动隐藏），并随游戏 UI 缩放。
- **中英双语**：按钮文字与悬停提示同时提供简体中文和英文，随游戏语言动态切换，在设置里
  改语言后也会立即生效。
- **无额外依赖**：不依赖、不读取、不修改 BubbleBuffs 或其他 mod。只把游戏原生侧边栏按钮
  当作外观模板克隆一份。
- **不产生日志**：正式版不写任何日志或调试文件。

## 倍率说明

| 按钮 | 游戏速度 | 说明 |
|------|---------|------|
| `0.25x` | 25% | 很慢，适合精细的即时制操作 |
| `0.5x`  | 50% | 半速（默认） |
| `0.75x` | 75% | 略慢于正常 |
| `关闭`  | 100% | 关闭子弹时间 |

倍率作用于游戏世界的时间缩放，不影响 UI、菜单和鼠标。所选档位保存在 mod 目录的
`settings.json` 中，下次启动自动恢复。

## 安装

1. 安装 [Unity Mod Manager](https://www.nexusmods.com/site/mods/21)（推荐 0.32.x）。
2. 打开 UMM 的 **Mods** 页，点击 **Install Mod**。
3. 选择 `BulletTime-UMM.zip`。
4. 启动游戏，HUD 左下角会出现 4 个按钮。

手动安装：解压 zip，确保存在以下文件：

```
<游戏目录>\Mods\BulletTime\BulletTime.dll
<游戏目录>\Mods\BulletTime\Info.json
```

## 卸载

- 在 UMM 的 **Mods** 页选中 **Bullet Time**，点击 **Uninstall**。
- 或直接删除 `<游戏目录>\Mods\BulletTime` 文件夹。

不会修改存档；如有 `settings.json` 也可以一并删除。

## 兼容性

- **游戏**：开拓者：正义之怒 增强版（PC / Steam / GOG / Epic）。
- **加载器**：Unity Mod Manager 0.32.x（实测 0.32.2）。
- **其他 mod**：与 BubbleBuffs 及其他 UI mod 相互独立。只读取一个原生侧边栏按钮作为
  外观模板，不修改任何游戏或第三方对象。
- **存档**：可随时添加或移除，mod 不向存档写入任何数据。
- **多人/联机**：未测试。

## 已知限制

- 回合制战斗刻意不受影响。
- 只对正常游戏模式下的即时制战斗减速；剧情脚本、过场和全屏界面保持自己的时间设置。
- 如果其他 mod 也每帧写入 `TimeController.PlayerTimeScale`，两者可能互相覆盖。
- 按钮面板固定在屏幕左下角，若其他 mod 在同一位置放置 UI，可能重叠。
- 游戏 HUD 隐藏时，面板一并隐藏。

## 从源码构建

需要：Windows、PowerShell 5.1+、.NET SDK（使用随附的 Roslyn 编译器）或 .NET Framework 4.x
的 C# 编译器，以及本机安装的游戏（提供托管程序集）。

```powershell
cd BulletTime
.\build.ps1 -GameDir "C:\Path\To\Pathfinder Wrath of the Righteous"
```

脚本会把 `Main.cs` 编译成 `BulletTime.dll`，并打包为 `BulletTime-UMM.zip`。
非交互构建可加 `-NoPause`；`BUILD.bat` 是 Windows 下的快捷入口。

## 仓库结构

```
.
├── BulletTime/
│   ├── Main.cs        # mod 源码
│   ├── Info.json      # Unity Mod Manager 清单
│   ├── build.ps1      # 构建 + 打包脚本
│   └── BUILD.bat      # Windows 快捷入口
├── README.md          # 英文说明
├── README.zh-CN.md    # 本文件
├── CHANGELOG.md       # 版本变更记录
├── LICENSE            # MIT 许可证
└── NEXUS_DESCRIPTION.md  # Nexus Mods 简介文案（BBCode）
```

## 许可证

本项目以 [MIT 许可证](LICENSE) 发布。在保留版权声明与许可证原文的前提下，可自由使用、修改与再分发源码及编译产物。

## 致谢

- 作者：KSPdevil
- 基于 Harmony 2.x 与 Unity Mod Manager。
- 按钮外观在运行时克隆自游戏自带的侧边栏按钮。

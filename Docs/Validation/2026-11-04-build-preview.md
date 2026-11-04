# 建造预览与坐标轴修复（2026-11-04）

- 工程：HY-Sandbox；Unity 6000.3.11f1；场景 Assets/Scenes/Main.unity。
- 历史定位：`d767c2b`（2026-09-23 14:10:02 +0800，Fix block connections and large-ghost placement）新增预览继承目标方块旋转；`a0274e3` 重构迁移到 BuildPreview，延续该行为。
- 修复：预览保持资源默认整格旋转，同时按真实连接点定位；公开 ResourcePath 变化时更换模型。坐标轴独立于选中方块，避免跟随删除/替换销毁；每帧跟随位置，按模式/选择恢复可见性，管理器禁用时隐藏、启用时恢复。首次显示不向 LookRotation 传入零向量。

## Unity 验证

使用已连接 Editor 的 `recompile` / `recompile_status` 确认脚本编译无错误；通过 `eval` 调用 `BuildPalettePlayProbe.Run()`，以实际 Main 场景的隔离 Play 会话运行，探针停用普通玩法更新和加载协程，不写用户蓝图或保存场景。

最终115项通过，错误0；原始报告见 [build-preview-play.json](2026-11-04-build-preview-play.json)。覆盖：

- 绕X/Y/Z轴旋转90°的目标不再带转预览，连接点仍重合且预览有效；2×2×2无半格漂移。
- 公开资源路径从2×2×2切换为1×1×1后模型同步更换。
- 移动/旋转按钮切换保留选择并显示匹配坐标轴；B视角解锁隐藏、重锁恢复。
- 选中方块销毁后轴仍存在且隐藏；重选后恢复；轴位置跟随选中对象的移动。
- 原分类、连接点可用性、距离和HUD相关检查继续通过。

首次运行在旧有UI悬停断言处失败（EventSystem detects palette hover）；原探针未改该步骤，后续完整运行通过。115项结果来自最后一次完整通过的会话，不将首次失败算为通过。删除回归使用隔离方块销毁模拟被删除/替换的生命周期，不点击会写入用户文件的删除入口；未验证人工连续拖拽手感或独立Player。

## 静态检查

本机只有.NET SDK 9.0.320，仓库global.json严格要求9.0.318；未修改仓库SDK约束。静态检查使用已安装9.0.320显式入口/仓库外工作目录执行，Unity导入和Play验证独立完成。

- CodeReadability：自有335脚本，格式和符号导航检查无错误。
- HY-Sandbox.sln --no-restore：0错误，4项既有引用冲突/过时API警告。
- git diff --check：通过。更新函数图和导航行号；未改场景、Prefab、脚本GUID或存档格式。

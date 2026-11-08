# Main 玩法合并第一阶段

日期：2026-11-08。环境：Unity 6000.6.8f1，Windows Editor，URP/VFX Graph 17.6.0，Input System 1.20.1。

## 结果与入口

- `Main.unity` 保留建造、公共 UI、摄像机、EventSystem 与共享服务。Simulate/P 打开模式选择。
- `Exploration.unity` 从 Main 原 WorldSession Prefab 实例迁移而来，保留内部引用与实例覆盖；清除对 Main 的序列化引用，加载后由 `GameModeController.LoadMode` 显式绑定。`prepareOnStart=false`，选择探索后才生成世界。
- `SiegeGameplay.unity` 是新的 Additive 制作场景，仅有场地、总览、基地/部署定位点、状态说明及返回。未迁移旧战斗、经济、部署或 HUD。
- 原 `Siege.unity` 与压力场景未重建，仍可独立运行。旧 Main 界面复制工具排除新模式组件，避免将 Main 模式入口复制进旧 Siege。
- Main 始终为活动场景；探索地形、建筑、运行舰船与据点驻军归 Exploration。炮弹与独立尾烟按实体所属场景取池，因此探索保存自己的炮弹池与 GPU 特效池；Main 的短时光束/灯光与 GPU 活动效果在返回时清理。
- `PlayDraft` 停用并保存原稿，探索创建副本；返回恢复同一个原稿及镜头，保留未保存的结构。货仓保存成功后再销毁运行舰船，按模块 ID 将返航货物恢复到原稿；死亡返回清货。

制作入口：`GameModeAuthoring.Install()`，菜单 **Tools/HY-Sandbox/Modes/Install gameplay subscenes**。重复执行不重建已有场景或 UI 布局。构建入口恢复 Main 首位并启用 Main、Exploration、SiegeGameplay；原 Siege 条目保留。

## 实际验证

| 检查 | 结果与边界 |
| --- | --- |
| 正式 Main 模式集成 `GameModePlayChecks.Run()` | **38 项通过，0 个非预期错误**。选择/关闭、加载取消、重复请求、两次 Siege 往返、失败重试、真实世界生成/激活、运行副本归属、特效池、货仓失败保留会话、正常返航、再次出发、真实驾驶舱死亡/重生、死亡清货 |
| 历史世界组件 `WorldSessionPlayChecks.Run()` | **57 项通过**。显式隔离夹具检查流式加载、地形/建筑、远景、进入/退出与资源释放；不把此项当作正式模式集成证据 |
| 保存重载/缺脚本检查 | Main、Exploration、SiegeGameplay 缺脚本均为 **0**；只有 Main 有 1 个 Camera 和 1 个 EventSystem；Exploration 有 1 个 WorldSession、1 个 VfxEffectPool、1 个 TurretProjectilePool |
| 实际 UI 按钮 | `MainUIButtons.playButton.onClick` 打开选择，新 Siege 按钮进入，副场景返回按钮回建造；已查看选择与场地 Game View 截图 |
| 旧 Siege 启动冒烟 | 独立 Play 后会话运行、第 0 波、基地存在、HUD 存在，且无 GameModeController 绑定；未运行完整战斗回归 |
| Windows Player 脚本 | `PlayerBuildInterface.CompilePlayerScripts`：Development 37 个、Release 35 个脚本程序集。仅脚本编译，不是完整 Player 打包或运行 |

正式模式报告：`Temp/GameModes/play-checks.json`；世界组件报告：`Temp/WorldSession/play-checks.json`；截图：`Temp/GameModes/mode-selection.png`、`Temp/GameModes/siege-stage.png`。截图来自 Editor Game View，保留了本机开启的 Gizmos；不将 Gizmo 线框视为 Player 画面。模式测试仅在唯一临时目录注入文件仓，不覆盖玩家存档；其中一次缺失蓝图错误是明确的故障注入，断言保存失败保留会话。

## 静态检查与环境差异

本机安装 .NET SDK 9.0.320，项目 `global.json` 严格锁定 9.0.318。未改项目 SDK 配置，改从项目外工作目录调用绝对路径，使用已安装 SDK 执行以下等价检查：

```powershell
dotnet run --project D:/git_projects/Unity/HY-Sandbox/Tools/CodeReadability -- --check D:/git_projects/Unity/HY-Sandbox
dotnet build D:/git_projects/Unity/HY-Sandbox/HY-Sandbox.sln --no-restore
```

CodeReadability：376 个脚本，格式差异 0、错误 0。解决方案构建：0 错误、30 个警告。`Docs/CodeIndex.md` 与 `Docs/RuntimeFlow.md` 由现有导航工具刷新。已有第三方/Unity API 过时和引用框架警告保留，不扩展整理范围。

本次暂存文件的 `git diff --cached --check` 通过；全工作区 `git diff --check` 仍报告用户原有 `UserSettings/Layouts/default-6000.dwlt` 的两处尾空格，未改动该文件或将其纳入提交。原有炮塔 Prefab、渲染/VFX 设置与用户设置修改保留。

历史世界 Player/Editor/性能工具显式调用 `WorldSessionValidationHost.Attach()`，通过保留原 WorldSession Prefab 的验证变体装配组件夹具；仅显式测试路径进入，正式模式不会自动实例化它。原世界 Player 工具验证的是组件生命周期，正式新模式仍以独立集成测试结果为准。

## 有意变化与剩余范围

Main 启动不再预热世界，也不等待世界生成显示启动遮罩；探索加载遮罩只在选择探索后出现。建造原稿在玩法期间停用，返航不再重新加载磁盘蓝图覆盖未保存修改。新 Siege 地面与定位点是可编辑场景骨架，不是完成的阵地战重做。

未验证：完整发布 Player 打包与实际操作、手动键鼠完整体验、所有旧 Siege 战斗和压力回归、长时间往返内存稳定性。下一阶段在 SiegeGameplay 内逐项实现部署、战斗、结算和专用 UI，再迁移/复用预研中已验证的规则。

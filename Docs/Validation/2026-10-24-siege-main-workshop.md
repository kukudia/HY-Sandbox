# Siege Main 工作台验证（2026-10-24）

## 范围

本次把 Main 的真实 `MainCanvas`、`MainUIButtons`、`BuildManager`、`SaveManager`、`InputManager`、镜头和建造 Gizmo 通过 Editor 工具复制到 `Assets/Scenes/Siege.unity`。Main 原场景未修改。`SiegeWorkshop` 只在整备阶段打开存档预览；战场舰船和大型建筑保持激活，但暂时隐藏 Renderer/Collider，避免加载器和选取命中它们。

## 真实 Play 证据

菜单 `HY-Sandbox/Siege/Check Main Workshop In Play` 在 Unity Editor Play 中通过 **22 项**：

- 真实 Saves 蓝图部署、损伤大型建筑和已部署舰队存活；工作台中拒绝开波、部署、接管和修缮。
- Main 异步蓝图加载、方块放置、写盘、Undo/Redo 和加载取消通过；测试只写入并清理唯一临时蓝图。
- P 返回、B 重新打开、实际鼠标点击工作台按钮、F1 推进器窗口和战斗 Alt 指针释放均通过。
- 返回时金币、基地损伤、已部署对象、驾驶权、预先禁用的 Renderer/Collider 都保持；部署时记录的舰体耐久上限用于 Main 状态条。
- 关闭工作台后实际开启波次，战斗状态拒绝再次打开工作台。

截图：`Temp/Siege/main-siege-final.png`。Unity Console ground truth 为 0 errors、0 warnings（性能记录器的正常日志除外）。

## 其他验证

- `SiegePlayChecks.Run()`：25 项通过，包含先基地后舰队自动修缮、精确报价、舰艇状态恢复及重复 Update 不重复扣款。
- `dotnet run --project Tools/CodeReadability -- --check .`：318 scripts，0 errors；更新 `Docs/RuntimeFlow.md` 与 `Docs/CodeIndex.md`。
- `dotnet build HY-Sandbox.sln --no-restore`：0 errors、4 个既有警告（3 个 MSB3277 依赖冲突、1 个过时 Profiler API）。
- `git diff --check`：通过。

## 边界

工作台流程在 Editor Play 和真实 Input System 设备上验证，尚未在独立 Windows Player 中做新的人工长时操作回归；万模块性能仍沿用既有压力记录，不能由这次 UI 检查推断。

# Siege 威胁预览验证

## 实现

- `SiegeMatch.NextWaveSize` 只读计算下一波规模，按当前波次、增长量和上限计算，不改变规则状态。
- `SiegeSession.AttackerPreview` 暴露当前Enemy蓝图或随机策略。
- `SiegeHud` 在整备阶段显示 `NEXT <数量>x <蓝图>`；战斗阶段不增加预览文本。

## 已执行

- `dotnet run --project Tools/CodeReadability -- --check .`：318 scripts，0 errors。
- `dotnet build HY-Sandbox.sln --no-restore`：0 errors，4 existing MSB3277 warnings。
- 连接 Unity `6000.3.11f1` 强制 recompile：completed，compilationFailed=false。
- 目标 Siege Play 中直接读取：`NextWaveSize=2`、`AttackerPreview=RANDOM ENEMY`；Unity console 在清理后为 0 errors/0 warnings。
- HUD screen capture 成功生成 1280×720 PNG；临时文件已删除。

## 边界

完整旧版 `SiegePlayChecks.Run()` 在本机用户蓝图目录配置下仍停在原有固定价格断言（`first two ships deploy through production economy`），因此本单元不把它标记为全量Play回归通过；预览直接读取与编译/静态检查已验证。固定压力夹具的 27 项回归证据仍见既有记录。

# 2026-10-26 阵地战跨启动战果记录

无尽波次缺少长期目标：一局失败后没有“上次打到第几波”的对照，也没有累计战果。本轮补最小可用的战果记录，不改单局规则与奖励。

## 改动

- 新增 `Assets/Scripts/Features/Siege/SiegeProgress.cs`：用本地 `PlayerPrefs` 记录 `最高波次`、`累计击毁`、`总场次`。`TryRecordResult(wave, kills)` 在失败时调用一次，只有更高波次才更新最高纪录，累计值总是累加，写入后 `Save()`。
- `SiegeSettings` 新增 `trackRecords`（默认开启）；关闭后不写本地记录，行为回到“只保留本局”。
- `SiegeSession.LoseBase()` 在失败时登记战果，并暴露 `NewRecord` / `BestWave` 给HUD。
- HUD：整备阶段金币行末尾显示 `BEST n`（没有历史时不显示）；失败横幅副标题显示 `NEW RECORD · WAVE n` 或 `BEST WAVE n`。
- 记录值全部来自公开的 `Match.Wave` 与 `Match.DefeatedAttackers`，不读取内部状态、不写用户蓝图目录。

## 验证

| 验证 | 结果与边界 |
| --- | --- |
| 规则检查 | `SiegeCombatFeedbackChecks.Run()` 新增4项：更高波次刷新纪录、累计击毁与场次累加、更短的一局不覆盖纪录但仍计击毁、探针结束后恢复原值。检查里写入的临时成绩会被 `RestoreForProbe` 原样还原，避免污染本机记录。 |
| 编译 | Unity 6000.3.11f1 编译通过（记录写入发生在失败分支，正常运行不触碰 PlayerPrefs）。 |
| 单元边界 | 记录只在本局失败时写一次；`trackRecords=false` 时不写；重启后通过 `PlayerPrefs` 读取。 |

## 未验证

- 没有做“连续多局 → 刷新纪录”的人工会话验证；只验证了规则层与写入/恢复路径。
- 未验证 PlayerPrefs 在发布 Player 与 Editor 之间是否隔离（两者都写 HKCU，通常共享），因此同一台机器上编辑器里的成绩会影响Player读数。
- 没有为记录增加UI列表或历史曲线，只有最高波次与两个累计值。

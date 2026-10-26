# 2026-10-26 阵地战蜂群波次

精英波次解决“更难更值钱”，但普通波次仍然是线性加一，节奏偏平。本轮加一个与精英错开的蜂群波：数量翻倍、生成更密，用压力换空间，鼓励玩家分散火力与提前修缮。

## 改动

- `SiegeSettings` 新增 `swarmEveryNWaves`（默认3）、`swarmSizeMultiplier`（默认2）、`swarmSpawnIntervalScale`（默认0.65）；都在 `DefaultSiege.asset` 可编辑。
- `SiegeMatch` 新增 `IsSwarmWave`：整除蜂群间隔且**不是精英波**时为真；`BeginNextWave` 在这类波次把名额乘以倍数（仍受 `maximumWaveSize` 上限约束）。精英与蜂群落在同一波时精英优先，避免两个倍率叠成不可玩的数量。
- `SiegeWaveDirector` 在蜂群波把生成间隔乘以 `swarmSpawnIntervalScale`（限制在0.2–1.0），实际同时在场仍受 `maximumActiveEnemies` 限制。
- HUD：蜂群波横幅 `SWARM WAVE n / k RAIDERS · FAST SPAWN`；顶部沿用 `BATTLE`。构造参数全部可选，既有测试与调用保持原行为。

## 验证

| 验证 | 结果与边界 |
| --- | --- |
| 规则检查 | `SiegeCombatFeedbackChecks.Run()` 增至 **21项**并全部通过：首波不是蜂群、第2波数量2→6、精英与蜂群同波时精英胜出且数量不加倍、间隔为0时保持线性。 |
| 真实Play | 自然推进到第3波：`swarm=True`、`pending=8`（线性应为4），横幅与进度条显示 `SWARM WAVE 3 / 8 RAIDERS`、`WAVE 3 · RAIDERS 8 / 8`，截屏 [swarm-wave](2026-10-26-siege-swarm-wave.png)。 |
| 上限 | 名额仍被 `maximumWaveSize`（默认48）截断；同时在场仍由 `maximumActiveEnemies`（默认6）限制，蜂群只缩短间隔，不提高同屏上限。 |

## 未验证

- 没有人工对局验证“蜂群+精英”连续出现时的节奏与难度；只验证了单次蜂群波与其规则优先级。
- 蜂群波的实际同时在场数量取决于生成间隔与敌舰存活时间，未做长时间自然波次的峰值统计。
- 未测量蜂群波对帧时间的影响；它提高同屏敌舰数量，理论上比普通波更重，万模块压力场未重跑。

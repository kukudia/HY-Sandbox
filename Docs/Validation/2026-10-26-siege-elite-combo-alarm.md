# 2026-10-26 阵地战精英波次、连击奖励与受击告警

在指挥栏重排之后，本轮给战斗补上“打得好有回报、挨打看得见”的反馈，同时保持规则核心不依赖场景对象。改动集中在 `SiegeMatch` / `SiegeSession` / `SiegeWaveDirector` / `SiegeHud` / `SiegeSettings`，加一个只读的镜头抖动入口。

## 玩法与表现

1. **精英波次**：`SiegeSettings.eliteEveryNWaves`（默认5）控制间隔，`eliteRewardMultiplier`（默认2）放大击毁与清波奖励。精英波次由 `SiegeWaveDirector.PickBlueprint` 从当前攻方目录里挑模块数最多的蓝图，不新增蓝图、不复制货物。HUD 在战斗阶段顶部显示 `BASTION · ELITE WAVE`，横幅显示 `ELITE WAVE n / k HEAVY RAIDERS · DOUBLE REWARD`。
2. **连击奖励**：会话每帧比较 `SiegeMatch.DefeatedAttackers`，在 `comboWindow`（默认5秒）内按实际击毁数累加，并按 `comboBonusCoins × (combo-1)` 发放金币。同一帧多架同时被击毁也按次数累计，不会因为一次齐射只算一次。波次最后一架敌舰被击毁时仍可结算连击（`GrantBonus` 接受战斗与整备两个阶段），部署阶段与失败后拒绝。
3. **核心受击告警**：顶部核心耐久条按比例转黄/红；屏幕四条边出现红色告警条，受击时短暂增强、核心低于30%保持低频脉动。同一帧顺带请求一次镜头抖动（`VisualEffectsManager.PlayCameraShake`，复用既有 `CameraShake` 实例，不创建对象）。
4. **性能约束**：告警条只有一个 `CanvasGroup`，每帧只写一次 alpha；抖动是既有协程；精英挑选只在生成时读一次 `BlockCount`；连击只在计数变化时计算。没有新增每帧查找、分配、物理查询或渲染对象批次。

## 验证

环境：Windows，Unity 6000.3.11f1，普通 `Assets/Scenes/Siege.unity`。

| 验证 | 结果与边界 |
| --- | --- |
| 规则检查 | 新增 `SiegeCombatFeedbackChecks.Run()`，13项通过：精英间隔、击毁/清波双倍、只放大一次、连击在整备/战斗可发放、部署与失败拒绝、间隔为0时保持线性。证据 `Temp/Siege/combat-feedback-20261026-160016.json`。 |
| 精英波次实测 | 用强制 `SiegeMatch` 夹具（`eliteEveryNWaves=1`，不写资产）进入第1波，`IsEliteWave=True`、`KillReward=240`，截屏确认 `ELITE WAVE 1 / 2 HEAVY RAIDERS · DOUBLE REWARD` 与顶部 `ELITE WAVE` 标记；清波后标记正确消失。 |
| 连击实测 | 同一波内击毁两架：`defeated=2 combo=2`，余额 775 → 2,015，等于 2×240（精英击毁）+60（连击）+700（精英清波）；截屏确认 `RAIDER DESTROYED · +240 CREDITS  COMBO x2 +60` 与 `WAVE 1 CLEARED`。 |
| 核心告警实测 | `ApplyDamage(380)` 把核心打到120/500后，读取 `damageGroup.alpha≈0.28`，截屏确认四条边红框、顶部核心文字与进度条转红，画面中心保持可读。 |
| 静态检查 | `CodeReadability --check`：323脚本、0格式差异、0错误；`dotnet build HY-Sandbox.sln --no-restore` 0错误/4既有警告；`git diff --check` 通过。 |
| 导航 | `generate_navigation.py` 重新生成43图/323脚本，新增“阵地战精英波次与连击奖励”函数图。 |

## 自然推进到第5波的补充证据

之前的精英验证用的是强制 `SiegeMatch` 夹具。本轮补了一次自然推进：部署 AirKing 后连续开波，用真实损伤打掉每波敌舰，直到第5波。

| 观察 | 结果 |
| --- | --- |
| 波次与精英标记 | 第1–4波 `IsEliteWave=False`；第5波自动变为 `elite=True`，`KillReward=240`（120×2）。 |
| 蓝图挑选 | 第5波生成的两架均为 `Raider 5 / Type06`，**219模块**，等于当前攻方目录（14项）里的最大模块数；不是玩家选定的随机舰型。 |
| 同帧画面 | 截屏 [elite-wave5](2026-10-26-siege-elite-wave5.png)：顶部 `BASTION · ELITE WAVE`、状态行 `COMBO x4 +180 coins`、横幅 `ELITE WAVE 5 / 6 HEAVY RAIDERS · DOUBLE REWARD`、底部 `WAVE 5 · RAIDERS 2 / 6`、右下两个来袭标记（35 m/36 m），以及前几波留下的残骸。 |
| 连击 | 连续击杀累计到 `COMBO x4`，状态行给出 `+180 coins`（60×3）。 |

这次推进仍然用“直接对驾驶舱施加伤害”触发真实死亡与结算链，不是人工驾驶交火；但波次计数、精英判定、最重蓝图挑选与奖励倍数是自然流程里发生的。

## 局限与未验证

- 镜头抖动只确认了代码路径被调用（核心掉血时请求 `PlayCameraShake`），静态截屏无法证明手感强度；需要在真实操作中调节。
- 精英波次尚未在“自然推进到第5波”的流程里验证，本轮用的是强制配置夹具；自然流程下的性能与可玩性仍待观察。
- 精英舰“最重蓝图”依据模块数，不包含火力/耐久权重；如果目录里最重的是低威胁船，观感会偏弱。
- 未重建 Player、未做长时自然多波采样；本轮改动没有物理或渲染批次变化，但也不等于性能已验证。
- 之前记录的 `SiegePlayChecks` 不稳定问题仍在，本轮的规则证据来自新增的独立检查与真实Play探针。

# 2026-10-27 阵地战波次结算战果

波次横幅此前只写 `REPAIR AND REINFORCE`，玩家看不到这一波打了多少、赚了多少。本轮把本波战果接到结算横幅上。

## 改动

- `SiegeSession` 在开波时记录 `waveStartCoins` 与 `waveStartDefeated`，进入整备时一次性算出 `LastWaveKills` 与 `LastWaveCoins`（金币净变化，统计点在自动修缮**之前**，所以是“打这一波赚到的”而不是“修完剩多少”）。
- 波次清空横幅副标题改为 `n KILLS · +x CREDITS`；若本组件先于会话拿到新阶段（同帧更新顺序不确定），退回原来的 `REPAIR AND REINFORCE`，不显示 0。
- 只读公开状态，不改奖励规则、不新增每帧计算（只在阶段切换时算一次）。

## 验证

| 验证 | 结果与边界 |
| --- | --- |
| 真实Play | 第1波清空后横幅读数为 `2 KILLS · +650 CREDITS`（2×120击毁+350清波+60连击=650），同帧状态行仍显示自动修缮结果；截屏 [wave-clear](2026-10-27-siege-wave-clear.png)。 |
| 回归 | `SiegePlayChecks` 28/28、`SiegeCombatFeedbackChecks` 23/23 在改动后重跑通过。 |
| 顺序兜底 | 会话与HUD同帧更新顺序不确定，因此用 `LastWaveKills > 0` 判断是否已有战果，否则显示原提示语。 |

## 未验证

- 没有验证“0 击毁但清波”（例如敌舰失踪）时的读数：此时退回 `REPAIR AND REINFORCE`，属于有意的降级，不是错误。
- 金币净变化不含随后的修缮支出；如果玩家期望“这一波净赚”，需要在修缮后再取一次差值，本轮刻意选择“战果”语义。

# 2026-10-26 阵地战UI整理后的稳定性与成本核对

本轮不是性能验收，而是把“简化UI”之后还能不能稳定跑、有没有引入每帧负担核对清楚。结论按证据分层：能测的写了数字，不能测的明确写成未验证。

## 稳定与日志

- 修掉 `CargoHoldView` 在蓝图恢复实例上的空引用：`LateUpdate` 现在在 `_statusLamp` 存在时先补齐 `_hold` 与 `MaterialPropertyBlock`。修复前同一场Play累计50条空引用，修复后全新Play会话 `consoleErrors=0 / consoleWarnings=0`。
- 全新Siege Play（编辑模式重排后重载场景）中，`RefsCheck` 读取的11个HUD序列化引用全部非空；之前两次“引用为null”都发生在场景未保存或制作工具在Play模式被拒绝执行之后，不是运行期丢引用。
- 独立Player验证包（`Builds/SiegeValidation/HY-Sandbox.exe`，D3D11，3200×2000）跑通 `-siege-checks-report`：**91项通过、0错误**，含HUD每个元素在屏幕内且文本有实际字形、两个可负担舰艇的部署、开波、真实敌舰死亡结算、驾驶舰损失接管、核心被毁结束与场景重开。该包同时断言精英波次节奏与连击门控，证明新规则在Player构建里同样生效。
- 本次修复了 Player 验证脚本的旧假设：它按 `Settings.Ships` 下标部署，而正常场景的部署选项来自用户蓝图目录，价格会变；现在改为按当前目录报价挑最便宜的可负担项，与 `SiegePlayChecks` 的既有做法一致。

## 波次结算与报价

- 整备阶段的两个修缮按钮现在显示**实际报价**（`REFIT BASE CORE 127` / `PILOT SHIP INTACT`），报价只在进入整备、点击修缮与状态变化时计算，不在每帧遍历方块。
- 新增“核心未受损”奖励：`flawlessBonusPerWave`（默认80）× 波次，在自动修缮**之前**判定，避免修好之后被误判为无伤；状态行显示 `Core intact +N coins`。真实Play实测：第1波 +80、第2波 +160，且与自动修缮同时生效。

## UI 规模与每帧成本

| 项目 | 旧界面（本轮改动前实测） | 当前 |
| --- | --- | --- |
| 战斗可见的Main遗留覆盖 | 世界准备×2、推进器窗口、物理遥测、返回提示，约44个激活Graphic | 0 |
| Siege HUD 激活Graphic | 约40 | 50（新增坐舰耐久6、波次进度5、横幅与提示、告警边条4；同时移除了重复的核心面板与常驻帮助） |
| 每帧脚本写入 | 旧HUD每0.25秒对每个按钮 `GetComponentInChildren<Text>()` | 标签在 `OnEnable` 缓存；战斗每个渲染帧只写2个 `anchorMax` 和1个 `CanvasGroup.alpha` |
| 文本重建 | 0.25秒一次 | 0.25秒一次（不变），未新增每帧字符串拼装 |

`SiegeHud.activeGraphics=50 / total=94`（含未激活的帮助面板、横幅、告警条）。MainCanvas 在战斗视图的激活Graphic为0，说明旧覆盖确实退出渲染，而不是只把alpha设为0。

## 未完成或不可靠的测量

- 尝试用外部逐帧读取 `Time.unscaledDeltaTime` 做“HUD开/关”对照，单次CLI往返开销远大于帧间隔，样本不具可比性，已放弃并记录；没有得出HUD帧成本数字。
- 编辑器内Game视图受vsync与编辑器开销影响，短时采样无法区分几十微秒级差异；**本轮不声称任何FPS提升或下降**。
- 未做同机位Player帧时间A/B；Player侧只做了功能冒烟（见下一条记录）。
- `SiegePlayChecks` 仍不稳定（两次不同断点、还原HUD脚本后同样失败），已作为独立问题记录，不并入本轮结论。

## 证据位置

- 修复前后日志对比：编辑器Console（`console_status.consoleErrors`）。
- UI规模：`Temp` 采样脚本输出（`activeGraphics`），旧界面数据来自 `2026-10-26-siege-command-hud.md` 记录的基线普查。

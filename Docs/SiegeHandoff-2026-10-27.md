# 阵地战 2026-10-26 夜间连续开发交接

本轮从 2026-10-26 23:04 开始，目标为“使用NightDesk，持续完善Siege玩法，简化多余UI，兼顾性能并更壮观”，默认只做本地提交、不推送。截止 2026-10-27 08:00（Asia/Kuala_Lumpur），07:30 起停止接新需求收尾。全部改动是 `main` 上的 **22 个本地提交**，未 push、未建PR、未发布、未改凭据。

## 完成的工作（按提交顺序）

| 提交 | 内容 | 关键证据 |
| --- | --- | --- |
| `a8beb2a` 指挥栏与战斗阶段HUD | 停用Main遗留的世界准备/推进器窗口/物理遥测/返回提示；HUD改为单条指挥栏+舰队坞+动作行；战斗阶段收起部署、压缩舰队坞、显示波次进度与坐舰耐久；F2帮助 | 9+10项Play检查；四张1080p截屏；场景重载后11个序列化引用全在 |
| `cf6d87c` 精英波次、连击与受击告警 | 每5波精英（最重蓝图、双倍奖励）、连击按次数加金币、四边红色告警条、核心受击镜头抖动 | 13→后续17项规则检查；精英横幅、`COMBO x2 +60`、告警alpha 0.28 |
| `be3b212` 夜色竞技场 | 程序化夜空、匹配雾色、冷色环境光与月光、地面扩到900×900 | 场景重载参数核对；三张截屏对照旧灰边 |
| `ea6f523` 修缮报价、核心无伤奖励与Player验证 | 修缮按钮显示真实报价、核心未受损按波次奖励、`CargoHoldView`空引用修复、Player检查按目录报价挑可负担舰 | 独立Windows D3D11 Player **91项通过/0错误** |
| `4b92a69` 回归套件恢复 | `SiegePlayChecks` 逐项记录失败；壳体断言改为对比部署基线（Swordfish出厂即46+7两组）；自动修缮扣费断言前临时归零连击单价 | **28/28**，此前每次停在不同断点 |
| `a990cc3` 进场信标 | 敌舰生成点橙色闪光+光柱+爆闪，复用瞬态池 | 直接调用产生range 34橙色光；日志确认两架生成都触发 |
| `ae9d1ab` 威胁标记 | 视野外/超90米敌舰的橙色菱形+距离，画布坐标夹边 | 实测 `88 m`/`123 m` 贴边标记；运行时坐标在画布内 |
| `090a5d0` 战果记录与失败结算 | `SiegeProgress` 写最高波次/累计击毁/场次（可关闭）；整备显示 `BEST n`；失败横幅显示纪录；失败隐藏舰队坞 | 21项规则检查；`BEST 7` 与 `BASTION LOST / BEST WAVE 7` 截屏 |
| `ee75898` 自然战斗帧基线 | 发布包90秒soak：中位9.99ms/P95 10.54/P99 10.93；JSON入库 | 86项检查通过、0错误 |
| `341ceeb` 自然精英第5波 | 自然推进到第5波：`elite=True`、`KillReward=240`、两架Type06各219模块（目录最大）；同帧截屏含精英横幅/连击/威胁标记 | 截屏 `2026-10-26-siege-elite-wave5.png` |
| `91ec956` 顶栏与下拉框打磨 | 隐藏无贴图的方块箭头；`ELITE IN n` 只在≤2波时出现 | 最终复核全绿 |
| `509489a` HUD开关A/B | 新增 `-siege-novsync`/`-siege-hud-off` 诊断开关；两次90秒对照中位10.00 vs 9.99ms | 该负载下HUD不是瓶颈，但仍受外部呈现上限 |
| `0094ddd` 壳体部件与横幅回弹 | 下拉框显示模块数；部署多连通蓝图时告警并在状态行报告部件数；横幅入场轻微放大回弹 | `Deployed Swordfish · 2 hull parts` + 警告日志 |
| `369b82d` 竞技场信标塔 | 12座250米环上的装饰塔（无碰撞体） | 场景读数12座、信标为单位缩放 |
| `3056d41` 蜂群波 | 每3波数量翻倍、生成间隔×0.65；与精英重叠时精英优先 | 规则检查增至21项；第3波 `swarm=True pending=8` 截屏 |
| `1d584c9` 预览纳入蜂群 | 整备预览按蜂群放大并标注 `SWARM`/`ELITE` | 23项规则检查；`NEXT 8 × RANDOM · SWARM` |
| `4b5442b` 战果清除菜单与交接文档 | 新增 `Clear Local Siege Records` 菜单、序列化蜂群默认值、重写交接文档 | 菜单把本机纪录清回0；最终矩阵全绿 |
| `3160ba3` 最终Player认证 | 把最终树的发布包报告入库 | 89项通过/0错误 |
| `7ede184` 连击上限与难度曲线 | `comboBonusCap`（默认300）、纯函数 `ComboBonusFor`、难度曲线表 | 规则检查增至25项 |
| `a5506ec` 后段波次采样与尖峰定位 | 无人驾驶推进到第5/6波：中位15.8–16.4ms、P95 31.9–34.4ms、P99 50.9–59.3ms、最大272–291ms；尖峰落在波次边界修缮与敌舰断裂重组 | 两份soak JSON入库 |

附加：`SiegeProgressAuthoring` 菜单 `HY-Sandbox/Siege/Clear Local Siege Records` 可清除本机战果记录；验证用的临时纪录已清回0。

## 最终验证矩阵（本轮最后状态）

| 套件 | 结果 |
| --- | --- |
| `SiegePlayChecks.Run()` | 28/28 PASS |
| `SiegeUsabilityChecks.RunDeployment()` | 9/9 |
| `SiegeUsabilityChecks.RunSelection()` | 10/10 |
| `SiegeCombatFeedbackChecks.Run()` | 25/25 |
| 独立Windows D3D11 Player `-siege-checks-report` | 89项通过/0错误，`scripted-loop-passed`（最终树重建后复验） |
| `CodeReadability --check` | 326脚本、0格式差异、0错误 |
| `dotnet build HY-Sandbox.sln --no-restore` | 0错误、4既有警告 |
| `git diff --check` | 通过 |

## 关键取舍

- UI重排走**制作工具**（`Apply Command HUD`），幂等、可复现；隐藏Main遗留对象只做 `SetActive(false)`，随时可恢复。
- 战斗HUD每帧只写两个 `anchorMax` 和一个 `CanvasGroup.alpha`，文本0.25秒节流；告警条只改一个CanvasGroup；威胁标记0.1秒节流、最多6个。
- 精英/蜂群/连击/无伤奖励都在规则层结算（`SiegeMatch`/`SiegeSession`），HUD只读公开状态。
- 新规则参数全部用**可选构造参数**，既有调用与测试行为不变。
- 夜色外观只改天空盒/雾/环境光/一个静态立方体缩放，`VisualEffectsManager` 默认值与原常量一致，Main外观不变。

## 已知缺口与未验证

- **后段性能未优化**：第5–6波中位15.8–16.4ms、P95约32–34ms、最大272–291ms。尖峰出现在波次边界（272ms，此时仅12个唤醒刚体）与敌舰断裂重组（291ms）。**实测更正**：给自动修缮加临时计时后，自然清一波的修缮只有 base 3ms + fleet 10ms，量级对不上272ms，所以边界尖峰不是修缮本身；剩余嫌疑是阶段切换时对全部刚体切换 kinematic 的物理冻结，未验证。工作计时是累计值、`precedingFrameWork` 为空，没有GPU/PhysX归因。
- **无A/B结论**：HUD开/关对照在噪声内相同，且中位仍卡在10ms（存在外部呈现上限），因此不能声称FPS提升，也不能测亚毫秒成本。
- 夜色下敌舰可读性只靠威胁标记补偿，未做人工对局；6个标记重叠、竖屏比例未验证。
- 进场信标、部分横幅的截图受CLI延迟限制（效果寿命<往返时间），只有直接调用与日志证据。
- 用户蓝图可能有半格偏移的独立吊舱（Swordfish 46+7）；本轮只做告警与状态提示，没有自动吸附或目录校验。
- 蜂群+精英连续出现的节奏、峰值的长时间统计、万模块压力场与混合压力场本轮未重跑。
- 编辑器在 01:00 左右出现一次脚本编译后台卡死（`Internal build system error`），重启后恢复；`Temp` 目录会被Unity在启动时清空，早期临时截图不再存在（入库证据不受影响）。
- 记录写在 `PlayerPrefs`（HKCU），编辑器与Player共享同一份；本轮验证用的临时成绩已清理。

## 下一步建议

1. 后段性能：优先处理“波次边界同步修缮”的272ms与“敌舰断裂重组”的291ms。前者可考虑把基地/舰队修缮分摊到多帧，后者可考虑减少重组时的全场刷新或复用既有批处理。
2. 给后段帧时间建立可复现基准：固定种子、固定舰型、`-siege-checks-profile`，并补齐单帧工作增量（当前只有累计值）。
3. 威胁标记加重叠避让与“正在攻击基地”的优先级；精英波次按威胁权重而非纯模块数选蓝图。
4. 蓝图目录校验：对“出厂即多连通”的蓝图给出提示或自动吸附，避免吊舱在战斗中表现异常。
5. 失败结算增加本局统计（击毁、金币、用时），与历史纪录一起展示。

## 恢复方式

- 项目 `D:\git_projects\Unity\HY-Sandbox`，分支 `main`，起始HEAD `b0d48d0`，本轮最后提交 `a5506ec`。
- 打开 `Assets/Scenes/Siege.unity` 进入Play；UI重排用 `HY-Sandbox/Siege/Apply Command HUD`，夜色外观用 `HY-Sandbox/Siege/Apply Night Arena Look`。
- 回归：Editor Play 中执行 `SiegePlayChecks.Run()`、`SiegeUsabilityChecks.RunDeployment()/RunSelection()`、`SiegeCombatFeedbackChecks.Run()`；或构建 `HY-Sandbox/Siege/Build Windows Validation Player` 后用 `-siege-checks-report` 运行。

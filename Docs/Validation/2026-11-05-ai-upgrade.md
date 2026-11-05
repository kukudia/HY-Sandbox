# Unity 6.6 飞行与战斗 AI 升级

## 实现与契约

- 敌机入口：`EnemyController.FixedUpdate`。自主目标用距离滞后稳定选择（Inspector `targetSwitchDistanceRatio=0.85`），明确任务仍最高优先级；目标停用、阵营/场景/警戒范围失效时在下个物理步清除并重选。新目标立即刷新移动意图，避免沿用旧目标的半秒移动缓存。
- 自主索敌首次按 EntityId 错峰；警戒返航同样受 `movementUpdateInterval` 限制。没有改供电、推进器、速度或悬停 PID 参数。错峰降低同批扫描同步发生的机会，不代表已经测出帧率收益。
- 敌机避障改用所属 PhysicsScene，忽略 Trigger；密集结果首次饱和时扩容至最多 256，线性选最近有效命中，不排序。支持面与起点重叠的伪法线不会把地板当水平墙；真正侧墙仍保留避障。这是局部反应式导航，不是三维全局路径规划，复杂凹陷仍需额外路线验证。
- 实体炮塔入口：`ProjectileTurret.GetAimPosition` → `ProjectileIntercept.TryGetTime`。按目标点速度减炮口继承点速度求匀速交会最早正根，并按炮弹寿命及世界路径射程约束；无解直接瞄准。Inspector `predictiveAim` 可关闭预测。激光保持即时瞄准；没有更改弹速、伤害、射程、功率或发射间隔。
- 炮塔在目标阵营改变、禁用或伤害模块重组离开时立即失效重选。`TurretTargetSelector` 移除永不刷新的数组缓存，索敌时复用 List 读取当前模块层级，能发现修复新增部件。此方案保证正确性并避免每次创建数组；大规模层级遍历的实际 CPU 成本仍需性能采样，不能宣称一定更快。
- 维修/运输机器人既有导航已通过当前引擎基线，本轮不改算法、固定导航策略或移动 Home 停靠契约。未引入 NavMesh、机器学习包、网络请求或额外服务。
- 保留既有类型、脚本 GUID、序列化字段与存档身份；新增字段使用默认值兼容现有 Prefab。新增运行求解器与 Editor 检查各配 `.meta`，连接 Editor 已导入，生成 csproj 已包含两者。改造前恢复点：本地分支 `codex/ai-upgrade-base-20261105`。

## 验证

Unity 6000.6.8f1，真实连接本工程 Editor。结果目录为本地临时证据，不提交缓存或截图日志。

| 检查 | 实际结果 |
| --- | --- |
| 机器人导航基线 | `Temp/BotNavigation/PlayValidation.json`：51 项、10 条 PhysX 路线通过，包含移动 Home、U 围挡、100m/s 薄墙、2000m 航段；不等同于两类机器人完整任务/停靠验收 |
| 新增 AI 边界 | `Temp/AIUpgrade/checks.json`：18 项通过，含目标滞后/失效、触发器/地板/局部物理场景/密集结果、修复与重组索敌、相对速度提前量及无解/寿命边界 |
| 炮塔旧回归 | `Temp/TurretRuntime/PlayValidation.json`：32 项通过，含激光零冲量、单双炮管、后座、实体弹扫掠与寿命 |
| 自然物理帧静止目标 | `Temp/TurretRuntime/FlightValidation.json`：10 项通过 |
| 自然物理帧横向移动目标 | `Temp/TurretRuntime/CrossingValidation.json`：10 项通过，12m/s 横移，双管首轮命中；不是命中率统计或加速目标保证 |
| 真实基地任务目标 | `Temp/Siege/priority-target-checks.json`：10 项通过，含发现半径外任务、警戒/阵营/启用边界 |
| 任务目标实飞 | `Temp/Siege/priority-target-flight.json`：12.01 秒、958 帧均有接近输入，距离 315.62m → 259.24m，passed=true；短时接近不代表复杂路线可达性 |
| 阵地战 | `Temp/Siege/play.json`：28 项通过，包含自主友舰、重组、驾驶权和失败流程 |
| 静态构建 | `dotnet build HY-Sandbox.sln --no-restore`：0 错误、109 警告；仍有 FindFirstObjectByType、Profiler 和 Unity 分析器弃用警告，不称零警告 |
| 规范与导航 | 345 脚本规范检查通过；45 张图及导航更新；9 份 Python 语法/模板检查通过；`git diff --check` 通过 |

新增 Editor 入口：`AiUpgradePlayChecks.Run()`；真实炮塔飞行入口：`TurretFlightPlayProbe.Run()` / `RunCrossing()`。后者原先使用禁用单位夹具，与有效目标过滤不符，本轮改为启用的自主单位并明确刷新引用/阵营，恢复其自然索敌验证意义。

回归曾发现地板重叠伪法线导致远程任务方向偏转，修复后基地检查通过；新增重组检查最初误将“模块被新组重新合法选中”当失败，现改为核对旧组与模块配对失效。CLI 一次同步任务检查主线程 5 秒超时，之后通过 delayCall 执行并读取真实报告；超时不记作通过。

尚未完成：Development Player 构建、大规模 AI CPU/物理查询/GC 基准、长时自然交战、全复杂地形敌机绕障、机器人完整维修/运输至移动母舰精确停靠、加速/急转目标命中率。NavMesh 更适合后续地面单位；本轮不能据此声称帧率提升或全部 AI 场景验收完成。

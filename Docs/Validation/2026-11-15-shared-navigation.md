# 共享导航与效率优先验证（2026-11-15）

## 目标和结构

Bot 和友/敌舰船共用 NavigationAvoidance；NavigationObstacleQuery 拥有单位物理场景查询、过滤、缓冲和重叠分离。Bot 保留任务/返航执行，使用26个三维候选；VesselNavigation 组合原整舰 A*、升降和动态预测，近场回退与逐步限速使用同一核心。旧 BotObstacleAvoidance 与 EnemyController 的斥力探测生产实现已清理；规划、预测、试验场和专属验证实际迁到 Navigation 功能目录。已有场景、Prefab和脚本GUID不重建。

用户明确以高效到达优先：通过条件是持续到达和有效恢复；接触次数是观测项，不以零接触判失败。近场仍根据实际惯性和可执行制动距离限速，舰船由推进器执行，不直接写速度。路线/绕行自己的方向承诺不再被战斗方向保持重复延迟。

## 实际 Unity Play 验证

Unity 6000.6.8f1，已连接目标 HY-Sandbox Editor。Bot 使用真实 Rigidbody；舰船使用场景 Scene Finch、生产 EnemyController、供电与推进器，五路线在3倍模拟时钟运行，固定物理步不变，表中为模拟秒。没有修改作者场景保存值。

- SharedNavigationPlayChecks：10项通过，覆盖形状差异、目标过滤差异、局部 PhysicsScene、实例状态隔离和采样预算。
- NavigationCombatPlayChecks：67项通过，包括静止翼展探测、近场制动、连续驾驶、整舰A*、上/下通道、动态包络和炮塔遮挡。
- EnemyPredictionPlayChecks：29项通过，包括动态横穿、远场规划、快照复用、7动作/12段预算及饱和处理。
- AiUpgradePlayChecks：21项通过，包括目标稳定、局部场景过滤、密集障碍和原战斗回归。

### Bot 的10条实际飞行路线

| 场景 | 到达时间（秒） | 航程（米） | 障碍接触 |
| --- | ---: | ---: | ---: |
| Bot wall crossing | 2.22 | 30.36 | 0 |
| Bot U enclosure | 2.06 | 25.91 | 0 |
| Bot inertia braking | 3.56 | 54.16 | 0 |
| Bot moving Home behind wall | 2.18 | 29.13 | 0 |
| Bot initial overlap escape | 2.64 | 27.01 | 0 |
| Bot open approach | 1.16 | 17.24 | 0 |
| Bot 200m flight | 4.38 | 199.22 | 0 |
| Bot 2000m flight | 14.36 | 1999.21 | 0 |
| Bot 100m/s thin wall | 4.22 | 163.98 | 1 |
| Bot target behind | 1.60 | 18.91 | 0 |

100m/s薄墙路线发生一次接触后仍在4.22秒到达；接触发生于2.98秒、墙顶边缘附近，碰撞后速度记录为约(0,7.08,0)，不是以100m/s正面撞墙。1.5倍制动前瞻的对照为4.70秒、167.61米、零接触；最终采用标准制动前瞻，4.22秒、163.98米、一次接触。其他路线两轮耗时接近；这只证明该探针的取舍，不等同所有场景的性能提升。

### 舰船完整链路

| 场景 | 到达时间（秒） | 航程（米） | 转向计数 | 障碍接触 |
| --- | ---: | ---: | ---: | ---: |
| Single wall | 19.35 | 87.35 | 2 | 0 |
| Roofed U trap | 20.83 | 90.96 | 3 | 0 |
| Tall wall bypass | 30.20 | 157.99 | 8 | 0 |
| Underpass | 31.77 | 164.48 | 3 | 0 |
| Complex obstacle district | 69.01 | 331.28 | 40 | 0 |

真实飞行记录保存在 Application.persistentDataPath/NavigationLabRecords/20261115-130054-637-8b0249/trial-0002 至 trial-0006，含metadata、timeline、blueprint和result。Bot结果为 Temp/BotNavigation/PlayValidation.json；隔离检查结果为 Temp/NavigationCombat 和 Temp/AIUpgrade。

## 静态与迁移检查

- 16份移动脚本的原GUID逐一核对一致；新导航文件/文件夹均有.meta。
- 原用户 HoverFlightController.prefab 与 Random5v5Arena.unity 的未提交diff与改造前一致；不提交这些文件及UserSettings。
- dotnet run --project Tools/CodeReadability -- --check .：通过；442份脚本，0格式差异、0错误。
- dotnet build HY-Sandbox.sln --no-restore：通过；0错误、37条框架引用/既有API警告。静态构建不替代上述Play验证。
- 本任务范围 git diff --check 通过；全工作区检查会报告未提交 UserSettings/Layouts 的Unity尾空格，未改写用户布局以消除这些无关报告。

## 边界

不保证封闭区域可达、任意玩家蓝图/动态障碍相位都通过，未做大规模多舰CPU压力对照。Bot候选从50降至26，已验证阻挡采样不超过28次查询（含直达/重叠）；不据此宣称总帧耗时降低同等比例。未用相同环境重跑旧舰船基线，不宣称完整导航普遍更快。预留的旧Inspector字段仅保留序列化契约，旧探针半径/斥力权重已隐藏，舰船体积来自真实整舰Bounds。

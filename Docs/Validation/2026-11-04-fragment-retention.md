# 2026-11-04 建筑断片瞬间消失修复

## 结果与问题定位

本轮根据用户澄清，将重点收敛到“建筑断裂后实体瞬间消失”。已移除建筑残骸生成前按预算直接销毁模块的分支，建筑、载具与Siege回收共同遵守 `FragmentLifetime` 的最短保留配置。生产配置 `Salvage/Settings.minimumFragmentLifetime` 为 **10秒**，原 `cleanupDelay` 保持 **5秒**。最短保留期限制预算/定时清理，不保证碎片永久存在，也不阻止真实伤害、重置或场景卸载。

静态检查覆盖343份自有脚本，格式差异0、错误0；方案构建0错误、4警告。Unity真实帧及受控夹具的本轮结果单独列在下文。此前共享机制报告记录的720项属于上一轮验收，不计为本轮全部重跑。

## 已确认的直接退场路径

| 原路径 | 原行为与本轮处理 |
| --- | --- |
| `WorldBuildingManager.CreateDebris` | 残骸已达默认12组，或单片超过默认32块时，先逐块 `SetActive(false)` / `Destroy`，直接返回，根本不生成物理残骸。这是建筑大块断裂瞬间消失的明确代码原因。现在先完整生成可见刚体；超预算只请求最短保留期后的回收。 |
| `SiegeWreckBudget.Tick` | 超组数/模块数预算时可在首次扫描即停用并销毁敌方无驾驶舱组。现在先满足共用最短保留期，再按原预算与批次退场。 |
| `SalvageCleanupSession` | 使用原单位5秒清理计时；原单位稍后产生的新碎片可能继承接近到期的旧清理任务。现在到期后继续等待相关碎片的剩余保护期，并在最终销毁前重新检查驾驶舱恢复。 |

建筑默认 `debrisLifetime=12` 仍保留；普通建筑残骸采用常规寿命和最短保留期的较大值，超预算残骸至少保留10秒。预算在保护期内允许暂时超出，不能将此变更描述为维持原硬上限；高频破坏可能增加短期刚体/碰撞体存量，本轮未量化最大规模负载。

## 共享机制与附带修复

- `FragmentLifetime.Mark` / `Remaining` / `CanRetire` 提供唯一最短保留规则；建筑残骸、载具无驾驶舱断组、Siege预算和延迟清理接入同一配置，未新增各场景独立寿命常量。
- `RuntimeGroupBuilder` 先将空碎片根移到源单位场景，再迁移模块，保留源 `PhysicsScene`，避免跨场景迁移模块重启生命周期、清空受伤或攻击者信息。
- 新载具组继承源单位剩余爆炸接触保护时间；被摧毁的桥接模块仍正常消失，两侧未损毁的断组继续存在。
- 最终清理仍检查当前驾驶舱；保护期内恢复驾驶舱的单位不被原有整组/原单位计时任务误删。

生产入口见 `Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs`、`Simulation/Groups/RuntimeGroupBuilder.cs`、`World/Buildings/WorldBuildingManager.cs`、`Combat/Damage/SalvageCleanupSession.cs` 和 `Siege/Battle/SiegeWreckBudget.cs`。源码关联导航更新为45张图，当前功能状态见 [DEVELOPMENT](../../DEVELOPMENT.md) 与 [RuntimeFlow](../RuntimeFlow.md)。

## 本轮实际验证

| 检查 | 本轮结果 | 证据与边界 |
| --- | --- | --- |
| FragmentRetentionChecks | 16通过 / 0错误 | Main Play真实帧：实际调用建筑 `CreateDebris`，33块完整保留、33个启用的可见网格、动态重力及真实PhysX下落；零预算仍生成；普通/超限/零预算三路均遵守最短保留；旧ScheduleGroup/ScheduleUnit计时、新生碎片及恢复驾驶舱。 |
| DetachedVehicleChecks | 7通过 / 0错误 | Main Play真实帧：桥接模块摧毁后两侧物理组存活，保留源场景、剩余接触保护及伤害/攻击者信息；隔离物理场景实际推进。 |
| SharedMechanismChecks | 40通过 / 0错误 | 最终重新执行共享几何、建筑双碎片运动及编辑动作恢复回归。 |
| SiegeRuntimeConnectionChecks | 38通过 | 最终重新执行连接缓存、显式断链、面开关、独立单位和停靠机器人边界。 |
| SiegeBuildingDamageChecks | 18通过 | 最终重新执行建筑未来残骸先受伤、连锁批次、支撑更新与快照边界。 |
| PhysicsSandboxProbe | 33通过 | 最终重新执行真实碰撞、伤害分摊、冲量/角运动、爆炸保护与撞击致死。 |
| SalvagePlayProbe | 71通过 / 0错误 | 本轮完整重跑建筑残骸转换、货仓/运输释放、回收幂等和延迟清理边界。 |
| SiegeWreckBudgetChecks | 15通过 | 同场景敌方无驾驶舱筛选、计数/模块预算、寿命/节流/批次及新增最短保留优先级；受控Tick时间夹具。 |
| SiegeBuildingChecks | 20通过 | 本轮重跑真实大型建筑布局、共用分组入口、支撑、独立核心、修缮与舰队边界。 |

本轮共9组、258条通过断言；数量含夹具与重叠边界，不等同于258个独立自然玩法场景。

FragmentRetentionChecks仅在内存将最短保留设为0.7秒、常规建筑残骸寿命设为0.1秒，以真实帧验证0.4秒仍完整存活、0.95秒自然清除；测试结束恢复原设置并验证dirty状态未改变。该证据验证共用时序机制，不声称已逐场景等待生产10秒或进行长时自然战斗。Siege预算夹具属于可控时间输入的策略验证，不能替代真实帧物理下落证据。

- `Temp/Reuse/fragment-check.log`：343脚本，格式差异0、错误0。
- `Temp/Reuse/fragment-build.log`：固定SDK9.0.318构建，0错误、4警告；警告包含Unity Pipeline引用冲突及既有弃用诊断。
- Unity 6000.3.11f1导入后实际运行上述探针；没有用dotnet构建代替Play验证。最终Editor回到Main Edit，场景dirty=false；所有场景磁盘未修改，生产最短保留设置恢复10秒。既有UserSettings内容原样恢复且不提交。
- `Temp/Reuse/fragment-duplicates.json`：343脚本，完全同体生产方法候选0组；只检查满足阈值的token相同候选，不代表没有语义重复。
- 紧凑检查名称、来源和SHA-256见 [本轮证据](2026-11-04-fragment-retention-evidence.json)。Temp报告仅作为源证据路径，不提交缓存或完整Editor日志。

## 保留边界

真实伤害致死的模块仍可即时销毁，仍可能引发爆炸或二次断裂；建筑重置/禁用、流式卸载、场景退出也继续释放实体。寿命到期、敌方预算回收和无驾驶舱清理在满足最短保留期后仍会生效。因此本次结论是“预算和旧计时器不再吞掉刚出生的断片”，不是“任何碎片永不消失”。本轮未完成Player发布构建、正式关卡长时多波战斗、最大规模刚体预算或人工手感的全面验收。
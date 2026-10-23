# 阵地战研究与实现

## 目标与行为边界

独立阵地战会话：玩家部署多艘蓝图飞船，接管其中一艘，其余执行友方AI；敌方按无限波次进攻，基地核心失守即结束。部署阶段和波次间隔允许花金币维修基地与舰队。驾驶舰损失不等于整局失败。

用户明确：**基地属于大型建筑蓝图**。当前基地使用32×32m、388模块的建筑蓝图，复用foundation、模块身份、BuildingRuntime加载/支撑/损毁和建筑原位修缮；飞船属于独立舰队体系。建筑没有驾驶舱，核心由配置中的模块ID指定，默认是中央发电机。旧32模块锚定驾驶舱原型已替换，历史性能证据仅对应该原型。压力验证同时覆盖大型建筑支撑断裂与多舰交火。

敌方无限指没有最终波次，不是无限同时在场。生成名额只有成功生成后才消耗；重复死亡不能重复奖励。失踪敌机可以释放波次占用，但不给击杀收益。手动开始下一波，避免维修界面被倒计时打断。

## 技术路线

- 保留 GameObject、Inspector、现有 PhysX 复合刚体和蓝图资产。阵地战规则不承担 Unity 场景副作用。
- 对齐玩家与AI的驾驶意图来源，阵营与驾驶权分离；友方不能全部接收玩家输入。
- 维修区分现存模块耐久恢复和部署蓝图缺件重建；补件保留唯一身份、验证空间占用，禁止复制货物。
- 已修复断链对称性并接入舰船主体刚体保留/局部刷新和双侧运行邻接；建筑仍按底座支撑分裂，范围伤害及连锁按最外层批次合并为一次支撑刷新。1024块同步夹具范围损毁中位879.25→40.49ms；自然战斗未验证该收益。先采样再决定 Jobs/Burst，暂不迁移整个项目至 ECS。
- 主线程应用物理重组与纯图计算分别计时；不能把图计算加速等同整帧加速。
- 建筑支撑使用专用几何邻接缓存，保留虚拟地面规则；局部删除重探附近，新增/配置变更重建。规范单碰撞体和直接连接子节点可缓存，复杂层级实时回退。43项实时几何对照与既有建筑/玩法回归通过；1024块夹具损毁31.37ms，非万块交火验收，见 [缓存验证](Validation/2026-10-23-siege-building-graph.md)。

## 分阶段验收

1. **规则核心**：部署/战斗/整备/失败，金币门控、生成预算、一次性奖励与波次上限。
2. **可玩纵向闭环**：独立可编辑场景和参数，真实蓝图部署、友方驾驶切换、真实敌舰攻击基地、金币维修、失败与重开。
3. **性能基线**：相同蓝图、固定生成种子，记录稳定飞行、集中断裂及多波残骸；重复采样，注明Editor与Player差别。
4. **底层优化**：在行为回归通过后比较连接图、批处理、增量拆分。规模逐级提升，不预先承诺万块帧率。

## 当前状态

独立场景已完成首个可玩闭环，性能基线与底层优化仍在进行。最新证据见 [验证记录](Validation/2026-10-23-siege.md)，不能将受控检查视为万块性能验收。

### 共用驾驶与蓝图恢复

- `Cockpit.ControlMode` 区分玩家输入和自主驾驶，阵营仍决定友敌。该状态随驾驶舱迁移，物理重组不会重置。
- `ControlUnit.AcceptsPlayerInput` 是输入门控；推进器手动控制与悬浮高度按键使用同一门控。自主友舰复用 `EnemyController` 的移动、索敌和避障，并可配置基地守卫范围。
- `ModularUnitFactory` 恢复任意阵营飞船；`EnemyBlueprintFactory` 保留原公开调用入口并委托共用实现。组装过程中保持刚体运动学，连接初始化后恢复动态，避免插值姿态偏移。
- 真实 Main Play 中 `SiegeFleetChecks.Run()` 的31项检查通过，旧敌机池6项回归通过。检查覆盖恢复、质量、生成姿态和控制门控；未宣称已验证多舰自然交火。
- 本轮构建0错误/4警告，包含3项依赖冲突和既有Profiler过时API警告；先前提交正文的3警告描述不完整，以此记录为准。

## 进入与操作

- `Build Windows Validation Player` 构建独立Siege入口Windows验证包，不改变Main默认启动；显式命令行检查、自然多波与显示边界见 [Player验证](Validation/2026-10-23-siege-player.md)。
- Editor菜单 `HY-Sandbox/Siege/Open Scene` 打开 `Assets/Scenes/Siege.unity`，进入Play。`Create Editable Scene` 只在不存在时创建，避免覆盖手工调整。
- 左侧按钮购买三种舰艇，六个泊位按钮切换驾驶；其余舰艇自主作战。沿用飞船WASD/QE/Space控制，右键按住观察、松开操作UI。
- 准备/整备阶段点击开始下一波。击毁敌舰与完成波次获得金币；维修基地或当前驾驶舰会恢复部署时的机体。泊位被其他对象占用时先清空，不会扣款。
- 驾驶舱被毁自动接管其余存活舰；全部舰艇损失后观察基地，基地核心被毁后失败。Restart重新载入本场景。

## 可编辑制作与会话边界

`DefaultSiege.asset` 管理金币、价格、蓝图、波次规模、同时在场上限、生成间隔/半径和种子。默认初始金币3000，首波2架、每波增加1架、单波上限48、同时最多6架；无限波次不代表无限同时在场。

基地388模块使用建筑虚拟底座支撑，损毁经BuildingRuntime分裂并服从建筑残骸预算；不会进入舰船驾驶舱重组。核心离开建筑主体或被毁判负。菜单 `Regenerate Default Base Blueprint` 在Temp备份后重建默认蓝图；`Upgrade Base To Building Blueprint` 迁移旧Siege场景的管理器、锚点与核心配置，保留舰船泊位/UI。蓝图GUID保留，不改用户建筑目录；Main仍为构建第一场景。

Siege不加载主世界存档，不复制蓝图货物；部署使用新的单位/模块ID。整备期间冻结场景载具，拒绝残余弹丸负向伤害；战斗恢复模拟。场景临时启用后台运行，退出恢复原值。

## 后续验证缺口

大型建筑首轮90秒真实战斗已记录，但发现角落炮塔不在中继供电范围；该失败布局保留为独立证据。修正后固定靶4秒/214推进帧四塔均供电并命中216伤害；修正布局90秒自然窗口4467观察，中位19.40/P95 29.18/P99 39.78ms，403190邻接查询；波次变化来源不能全部确认，不作为确定性对照。详见[建筑迁移验证](Validation/2026-10-23-siege-building.md)。已有真实帧、100/256/1024块同步分组采样、主体保留/局部刷新与运行邻接；未引入Jobs/Burst。建筑原位修缮，舰船回泊位恢复；未加入完整存档或战术指令。Editor Play检查不替代正式Player性能。

## 性能复现

短时战斗表现使用 `TransientEffectPool` 复用能量光束和点光源，闲置预算32/16，活动效果不裁剪；淡出和场景卸载负责释放。生命周期检查与大型基地自然回归见 [短时表现复用](Validation/2026-10-23-siege-transient.md)。

建筑虚拟底座接触现在与规范邻接节点共用几何失效生命周期，另验证底座尺寸；复杂连接层级保持实时。`SiegeFoundationCacheChecks.Run()` 在全新Siege部署Play对照实时几何并恢复姿态。2048热底座判定及真实六舰回归边界见 [底座缓存](Validation/2026-10-23-siege-foundation.md)。

逐次毁伤日志使用DestroyManager的 `Debug Destruction` Inspector开关，新建默认关闭。VisualEffectsManager的既有 `Debug Destruction Vfx` 字段保留，但新建默认关闭；Main场景已序列化true保持不变。不开日志仍执行原视觉、销毁与重组链，不全局禁用Unity错误/警告或修改StackTrace设置。`SiegeDestructionLogChecks.Run()` 是部署阶段的隔离ABBA日志对照，临时关闭效果并恢复开关，不作为自然FPS证据。

菜单 `Build Windows 2048-Module Profiling Player` 构建单独Development分析包到 `Builds/SiegeLargeProfiling`。它使用相同可编辑压力场景，不覆盖发布验证目录，不启用Deep Profiling或自动连接。显式profile诊断读取可用的引擎计时与GC计数（跨记录线程的合计），记录类别、单位、有效性、实际样本和缓冲覆盖窗口；读取后清空缓冲，避免把旧GC值重复算作新帧。缺失或没有新样本用-1表示。原生样本有发布延迟，嵌套/重叠阶段不能相加，也不能把worker合计当作主线程墙钟或GPU耗时；Development帧率不能与发布包直接比较。


显式自然Player检查现在每秒记录刚体根分类（建筑、有效驾驶舱舰船、无效驾驶舱分量、弹丸、其他）、运动学/睡眠状态、碰撞体数和普查CPU开销。只有额外传入 `-siege-checks-profile` 才在自然观察窗口开启已有嵌套工作计时，结束或销毁时恢复原开关。保留原 `census` 字段，新增 `survey` 汇总和最慢32条超过33.33ms的帧。帧delta是帧起点到起点，前一LateUpdate计时窗口仅近似关联；不能把嵌套阶段相加或据此归因PhysX/GPU。没有组件标记的建筑残骸计入other，闲置池对象排除。`SiegePlayerSurveyChecks.Run()` 要求全新未部署的Siege Play，检查结束会终止该测试会话；慢帧阈值部分用合成delta，不是自然性能证据。


明确指派基地目标的敌方增援可从自主索敌半径外接近；普通发现半径、守卫范围和阵营/运行校验保持。大基地六舰可见Player的最新规模及帧时间边界见 [任务目标与可见验证](Validation/2026-10-23-siege-priority.md)。

菜单 `Create Editable 2048-Module Stress Profile` 创建独立 `SiegeLargeStress.unity`、配置与 `BastionBase2048.json`。将原基地237块2m装甲各细分为8块1m装甲，加一块有地板支撑的顶层测试块，达到现有单栋2048上限；保留核心、发电机和炮塔。这是模块/碰撞体压力布局，细分改变独立耐久和损毁分量，不作为等价战斗难度或加速对照。已有压力资产不覆盖，可直接编辑保存。`Build Windows 2048-Module Stress Player` 输出 `Builds/SiegeLargeStress/HY-Sandbox.exe`，支持与六舰包相同的显式检查参数。

`SiegeBuildingBenchmark.Start("label", true)` 测量1024/2048块建筑的冷刷新、热刷新和局部损毁，增加邻接/底座/遍历/残骸分项；当前实际瓶颈及Jobs选择边界见 [2048块基线](Validation/2026-10-23-siege-building-stages.md)。这是同步夹具，不是千块交火FPS。

菜单 `Create Editable Stress Profile` 创建独立 `SiegeStress.unity` 与 `SiegeStress.asset`，保留正常Siege经济和已有压力场景的手工修改。`Build Windows Stress Player` 输出独立压力包，命令行 `-siege-checks-ships 6` 可验证实际六舰部署。首个六舰自然交火窗口与准确规模边界见 [压力验证](Validation/2026-10-23-siege-six.md)。

Siege Play部署两舰后调用 `SiegePerformanceCapture.StartCapture("label", 90f)`，再开始波次。到时、失败、停止Play或脚本重载时保存已有样本到 `Temp/Siege/Performance`；`Status` 可读状态，`Finish()` 可提前保存。CPU计时默认关闭，仅采样期间开启。各阶段嵌套，完整刷新计时从输入块枚举后开始。

`SiegeTopologyBaseline.Run()` 在远离战场的临时真实方块链上测量同步分组，并记录断链正序/倒序归属；不用物理步，不等于千块战斗性能。原始JSON可用 `Tools/Validation/summarize_siege_capture.py` 无损压缩并生成摘要。证据与限制见 [性能基线](Validation/2026-10-23-siege-performance.md)。

`SiegeRegroupChecks.Run()` 要求全新Siege Play且未部署舰艇，验证保留主体、运动/姿态、归属、局部刷新和维修。`SiegeRegroupBenchmark.Start()` 在部署状态延后运行同步ABBA对照，`Status` 返回完成报告路径；避免直接Run触发CLI独立5秒主线程请求超时。冻结旧迁移实现只保留在Editor基准工具中，生产运行时只有主体保留实现。

`SiegeRuntimeConnectionChecks.Run()` 验证无视觉双侧连接、刚体整体运动、显式断链零物理查询与零全场扫描、局部修改失效，以及不同单位接触不会自动焊接。战斗重组使用内部 `GroupRuntimeBlocks`，公开 `GroupBlocks` 保留实时几何探测契约。缓存只适用于直接挂在ControlUnit根下且规范碰撞体在模块根上的常规结构，复杂层级回退物理重建；制作/维修改变几何后须重建，不以视觉对象存在与否决定拓扑。

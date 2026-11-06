# HY-Sandbox 开发入口

Unity 6000.6.8f1（URP / VFX Graph 17.6.0，Input System 1.20.1）。本文只维护当前功能、契约与验证入口；历史变更和旧验收完整保存在 [DevelopmentHistory](Docs/DevelopmentHistory.md)。

## 特效与音频资源管理

GPU 特效已接入按场景、按模板复用的 VfxEffectPool：取消64个一次性效果与24条残骸尾烟的活动数量门槛，峰值按需扩容；爆炸、残骸、陨石共用生命周期，回收前保留尾烟。集中库只配置闲置缓存（每模板32、每场景128），不会拒绝活动请求。VFX Graph 粒子在 GPU 上模拟，CPU 负责对象与跟随；单Graph粒子容量仍为预分配预算。验证见[GPU特效池](Docs/Validation/2026-11-06-vfx-pool.md)。

残骸包裹范围、粒子密度与不透明度已降低，尾烟稍浅且更透明；火光颜色保持 10% 设定。残骸火光已减弱并收紧，新增头部灰黑烟遮叠与随机火焰朝向，尾烟加深加厚；见[烟火平衡验证](Docs/Validation/2026-11-06-fire-balance.md)。残骸新增按模型包围盒自适应的局部包裹火焰，跟随模型中心与旋转；[包裹火焰验证](Docs/Validation/2026-11-06-debris-envelope.md)。尾烟已提高不透明度并加入短促橙色余火：中低速烟迹保持对比，火光与烟雾共用粒子，仍由集中库 DebrisSmoke 配置。隔离 Play 近远景、销毁后消散通过；参数、截图与验证边界见 [尾烟可见性](Docs/Validation/2026-11-06-debris-visibility.md)。

战斗表现增强：残骸烟迹使用 `DebrisSmoke` 世界空间图，挂载点按刚体点速度跟随，残骸销毁/禁用后独立淡出；激光使用 `LaserBeam` 分层亮芯，宽度 0.16m、持续 0.2s；炮弹使用 `ProjectileTracer` 记录真实位置历史，宽度 0.1m、历史 0.09s、最长 7m，复用时清空。特效仍在集中库替换，伤害、命中和弹速不变。制作入口 `CombatVfxBaker.BakeMissing`，规格见 [CombatVfx](ArtSource/VfxGraph/CombatVfx.md)，真实 Play 近/远景与回收验证见 [战斗特效报告](Docs/Validation/2026-11-06-combat-vfx.md)。

Unity 菜单 **Tools → HY Sandbox → 资源管理 → 特效与音频** 打开统一管理入口。`PresentationResourceWindow` 支持名称/路径搜索、类型筛选、定位、原生预览、直接引用查询和保存；Prefab 通过原生 Prefab Mode 编辑完整层级。扫描覆盖 Assets 下的 VFX Graph、包含 VfxEffect/VisualEffect/ParticleSystem/TrailRenderer/LineRenderer 的 Prefab 和 BlockVfxLibrary，可额外显示递归依赖。

“运行时播放库”直接编辑既有 `Resources/VFX/BlockVfxLibrary.asset`。一次性效果以及 HoverJet、ThrusterJet、BotFlight、RepairContact、EnergyTrail、LootBurst 和保留外观变体全部集中配置。29 个项目 Prefab 的 54 个挂载点只保留效果编号、位置、朝向、缩放和自动播放标记；3 个组合控制器只引用子挂载点。`VfxEffect.EnsureContent` 在 Play 首次使用时从库实例化模板，保留对象生命周期。替换库中 Prefab 后，新实例使用新效果；已有实例需重新生成或重新进入 Play。

迁移保留原组件 fileID、Prefab GUID 和全部外观参数；两种特殊外观另存到 `Assets/Art/BlockVisuals/VFX/Variants`。`VfxCatalogMigration.Run` 可重复执行；`BlockArtIntegrator` / `BlockVfxBaker.Populate` 后续制作只建立编号挂载点，`PopulateTemplate` 只制作库模板。`BakeMissing` 不覆盖已有非空库选择。第三方原包未迁移，项目已使用的 Art/Resources 特效全部纳入。验证见 [集中引用迁移](Docs/Validation/2026-11-06-vfx-catalog.md)。

特效与音频两页共用 `PresentationAssetIndex` 的 GUID 索引、搜索与直接引用查询，项目变更后刷新、窗口关闭释放内嵌 Inspector。音频已支持 AudioClip 浏览及 Unity 原生预览，后续音频播放策略可独立接入，不将 GPU 特效生命周期套用到音频。直接引用不包含字符串动态加载；依赖资源也可能服务于其他功能，编辑会影响其所有使用者。

源码入口位于 `Assets/Editor/Presentation/Resources`，真实目录见 [CodeIndex](Docs/CodeIndex.md)，函数图见 [RuntimeFlow](Docs/RuntimeFlow.md)。集中迁移后资源索引为 215 个特效相关资产、410 个含依赖资产。初版编辑器验证见 [资源管理验证](Docs/Validation/2026-11-06-resource-manager.md)，最新 Play 与迁移保真验证见 [集中引用迁移](Docs/Validation/2026-11-06-vfx-catalog.md)。

## Unity 6.6 升级适配

已迁移禁用的对象/序列化整数身份 API 至 EntityId。后续场景服务使用 SceneObjectQuery 明确归属并拒绝重复实例；VFX 桥接适配 17.6 的 GetGraph/CreateGraph，喷口 FBX 用显式材质 remap 保留引用，Profiler 读取录制帧的类型化计数及可用性。CodeReadability 从真实工程读取条件符号。入口与实际验证/未覆盖边界见 [后续适配报告](Docs/Validation/2026-11-05-unity-6000.6-followup.md)，原始清单见 [升级报告](Docs/Validation/2026-11-05-unity-6000.6-upgrade.md)。函数图和导航已同步。

Player 回归发现并修复近景列提交与远景环刷新的同步空窗：列加载/卸载事件立即切换对应代理，退出解除订阅。最终 Main Editor 会话 57 项、Main Player 33 项、Siege Player 67 项通过，Windows Mono Development 构建 0 错误；隐藏 Player 的黑帧截图不作为画面验收，真实硬件交互、可见 Player 画面和长期压力仍待验证。

## 飞行与战斗 AI 升级

敌机新增稳定索敌、即时失效与错峰调度，避障过滤触发器和地板重叠伪法线；实体炮塔按目标/射手点速度预测交会，修复新增模块与重组变更在索敌时可见。机器人导航基线通过，本轮保留其现有算法。入口、Inspector 开关、真实 Play 证据和性能/长时验证缺口见 [AI 升级报告](Docs/Validation/2026-11-05-ai-upgrade.md)。

## 最新 Profiler 与战斗性能

已分析 2026-11-05 16:35 的 2,000 帧采样：炮塔固定步及伤害触发的连接视觉重复创建/销毁是主要可修复热点。`BlockConnections.CheckConnection` 保留未改变的双侧连接视觉；`ControlUnit.GetDamageTargets` 共享当前物理步的模块层级快照，炮塔仍实时验证位置、伤势和归属。新增同一步激活、移动、重组及视觉身份回归；没有降低物理频率、伤害或渲染质量。

相同隔离夹具连接刷新中位 25.51→1.10ms，40炮塔索敌批次 21.19→3.86ms；自然阵地战60秒 Editor 观察54.6→58.9帧/秒，但战斗轨迹不同，不作为确定性收益或Player帧率。采样局限、逐项证据及剩余Gizmos/渲染热点见 [性能报告](Docs/Validation/2026-11-05-profiler-performance.md) 与 [原始汇总](Docs/Validation/2026-11-05-profiler-performance.json)。

## 多舰表现性能优化

PC管线启用GPU Resident Drawer，并保留BRG着色器变体。`IndustrialPartMotion` 改为活动注册与单个 `IndustrialMotionDriver` 调度；保留 Inspector 配置、scaled旋转和unscaled浮动/发光。GPU绘制路径的 `IndustrialEmissionBinding` 按源材质及脉冲参数共享临时材质，固定8舰的2.79万个发光Renderer只需1个脉冲材质更新；传统渲染和外部PropertyBlock保留原路径，避免破坏自动实例化。建造/游玩高亮同步迁移，禁用、重配、销毁释放材质。关闭连接点调试时跳过 `Block.OnDrawGizmos` 的几何计算；标签复用GUIStyle。

同镜头、固定8艘CODEX_01_Bastion加基地、约1.39万动画组件，独立Play会话各40观察帧暖机和300帧采样：传统路径CPU帧时间中位60.42ms，GPU绘制与共享发光38.02ms，减少约37%。这是Editor静态舰队测试，不代表自然战斗或Player帧率。证据、回归、原生崩溃记录和Player验证缺口见 [多舰表现性能报告](Docs/Validation/2026-11-05-fleet-presentation.md)。`FleetPresentationBenchmark.CreateFixedFleet` / `StartCapture` 可重现夹具与采样；只读配置，不在运行中切换渲染系统。

## 本轮复用与精简

断片保留修复：建筑超过32模块或残骸数量预算时仍生成完整物理残骸，预算只请求延后退场。建筑、阵地战敌方碎片及载具延迟清理共用 `FragmentLifetime` 最短保留期（`Resources/Salvage/Settings.minimumFragmentLifetime`，当前10秒）；伤害销毁、卸载和会话退出不受此保护。载具重组保留源 PhysicsScene、损伤归因及剩余碰撞保护。验证见 [断片保留报告](Docs/Validation/2026-11-04-fragment-retention.md)。

建筑与载具共用 `BlockConnectionSnapshot` / `BlockConnections` 双侧连接、`BlockGroupManager` BFS 分组及 `RigidbodyMotion` 运动继承。`BuildingConnectionGraph` 旧实现已删除，建筑只保留虚拟地面支撑、地形脱锚、残骸预算和存档边界；载具保留驾驶舱选择、操控和组清理。删除节点只重探邻近模块；真正复杂碰撞布局继续实时探测，独立停靠机器人的碰撞体不使主体缓存失效。

共享规则同时收束到：`Thruster` 输入推进链、`BlockBounds` 几何、`BlockActionState` 撤销恢复、`SaveManager.LoadBlueprint` 加载协调、`WreckSalvage.ReleaseContents` 模块货物释放、`Bot.CanLaunchForWork` 起飞门槛、`BuildHandleAxis` 操作轴方向，以及存档列表的统一构建。清理失效的销毁延迟刷新、动态重命名按钮和重复特效状态；兼容入口只委托共享规则。

范围、保留差异、实际回归结果与限制见 [本轮报告](Docs/Validation/2026-11-04-reuse-refactor.md)。全仓 AST 检查辅助发现重复；同形方法和语义重复仍需人工核对，不以删行数代表质量或完整玩法验收。

## 功能入口

下列运行源码路径相对于 `Assets/Scripts/Features`；实际递归目录树、全部真实函数签名和源码行号见 [CodeIndex](Docs/CodeIndex.md#代码文件夹位置)，主要调用链见 [RuntimeFlow](Docs/RuntimeFlow.md)。制作/验证工具放在 `Assets/Editor`，不整理第三方源码。目录边界见 [CodeLayout](Docs/CodeLayout.md)。

| 功能 | 主入口 | 共享机制或独立策略 |
| --- | --- | --- |
| 启动 | `Bootstrap/GameManager.Init` | 当前存档、界面与会话组合 |
| 蓝图与文件 | `Building/SaveManager` / `BuildManager.LoadAllBlocks` | `BlueprintFileStore` / `BlueprintLoader` / `BlueprintBlockFactory` |
| 建造交互与动作 | `Building/BuildManager.Update` | Preview / Selection / Manipulation / `BlockActionState` |
| 建筑与据点制作 | `Building/BuildingEditor/BuildingOutpostEditor` | 地基只作制作网格；世界蓝图唯一来源为用户 BuildingBlueprints 文件仓 |
| 游玩与载具 | `Simulation/PlayManager` / `Units/ControlUnit` | `RuntimeGroupBuilder` / `RuntimeGroupCleanup` |
| 连接和分裂 | `Simulation/Connections/BlockConnectionGraph` | 双侧连接、几何快照、增量删除；`BlockGroupManager` 统一 BFS |
| 刚体与几何 | `Simulation/Physics/RigidbodyMotion` / `Geometry/BlockBounds` | 质心运动捕获/继承、统一八角包围盒 |
| 世界和建筑 | `World/WorldSessionManager` / `Buildings/BuildingRuntime` | 流式加载、支撑、脱锚、残骸预算和快照 |
| 据点驻军 | `World/Outposts/OutpostEncounterDirector` | 部署、有限驻军、休眠与会话内击败状态 |
| 阵地战 | `Siege/SiegeSession` | Fleet / Battle / Workshop / Interface，各自拥有状态与退出边界 |
| 供电 | `Power/Devices/PowerTransmissionDevice.Update` | `PowerNetwork` 拓扑缓存与需求分配 |
| 飞行 | `Flight/HoverFlightController` / `Thrusters/Thruster` | HoverControlSolver / ThrusterAllocator；主推进与通用推进共享生命周期 |
| 维修与运输 | `Bots/Repair/RepairBot` / `Collection/CollectionBot` | 导航、返航和起飞供电门槛共享，任务规则独立 |
| 战斗和损伤 | `Combat/Turrets/Turret` / `Damage/Durability` | 命中结算、碰撞能量与伤害入口共享 |
| 销毁和回收 | `Combat/DestroyManager` / `Salvage/Drops/WreckSalvage` | 有界爆炸冲量、运输取消、货物释放和残骸清理 |
| 镜头、表现与输入 | `Presentation` / `Player/InputManager` | 镜头模式、池化特效、UI 与 Input System |
| 诊断 | `Diagnostics` / `Simulation/Diagnostics/SimulationWorkMetrics` | 按需计时、物理遥测和异常记录 |

## 行为与数据契约

- MonoBehaviour 持有 Inspector、引擎回调和组合；共享算法/状态放普通 C# 类。没有新增全局服务容器、反射生产调度或第二套连接算法。
- 原组件类型、已有 GUID、Inspector 字段和存档结构保留；`RuntimeUnitMember` 仍与 `ControlUnit` 同文件。新增 Assets 配 `.meta`，迁移通过 Editor API 保持 GUID。
- 建筑和载具主体都使用 ControlUnit。建筑不进入载具驾驶舱重组或远距清理列表；脱落建筑模块停用功能脚本，预算前先释放已有货物，超预算也不吞货。卸载/休眠/退出不产生回收奖励。
- 连接由双侧可用连接面、位置和相反法线决定；视觉对象不决定邻接，建筑不生成载具接缝装饰。刚性整体移动保留缓存；模块姿态、连接面、层/Trigger、根缩放和实际模块附加碰撞体改变时失效。
- 分裂在层级迁移前捕获源速度、角速度和质心，所有碎片使用同一捕获状态；刚体参数由 GroupPhysicsSettings 统一应用。Main 建筑残骸使用 PlayManager 的阻尼及继承限幅，独立 WorldLab 从源刚体获取配置。
- 建筑范围伤害按批次/队列处理连锁，载具爆炸使用断链重组与有界冲量；启用 canExplode 的方块撞击致死仍爆炸。碰撞使用法向耗散能量，动态双方分摊；不将两类玩法策略机械合并。
- 蓝图校验和取消仍先于替换；同目标重复请求保留进度，切换使用版本标记隔离旧协程。撤销恢复共用工厂并保留精确世界姿态、稳定身份、尺寸/质量/碰撞及货仓。
- 电网按剩余需求比例分配，唯一负载去重，独立网络可补足需求；功率只限制机器人起飞，离仓后仍能完成任务或返航。
- 文件读写、加载取消、场景卸载和临时表现的生命周期仍由各领域入口拥有。独立冻结性能基准是验证对照，不接入生产链，也不为去重改写参考算法。

## 修改与验证

共用编辑规则由用户 `code-readability` Skill 维护；Unity 配合 `unity-editable-workflow` / `unity-cli`。修改前搜索现有能力和调用者；共享规则只维护一份生产实现，差异显式留在领域策略。迁移消费者并验证后清理旧实现。

Unity C# 使用项目 `.editorconfig`：Allman、4 空格、完整控制流和 CRLF。已有 GB18030/BOM 按真实编码保留；新增文本默认 UTF-8。新/改源码说明真实职责与入口，注释解释生命周期和易错不变量。

```powershell
dotnet run --project Tools/CodeReadability -- --write .
python Tools/CodeReadability/generate_navigation.py
dotnet run --project Tools/CodeReadability -- --check .
dotnet run --project Tools/CodeReadability -- --duplicates .
dotnet build HY-Sandbox.sln --no-restore
git diff --check
```

项目锁定 .NET SDK 9.0.318。`--duplicates` 只读报告生产方法中不少于50个 token 的完全相同方法体，忽略注释，排除 Editor/Validation 和整文件 Editor 条件基准；候选须人工复核，零候选不证明不存在语义重复。

本轮实际验证结果见 [复用治理报告](Docs/Validation/2026-11-04-reuse-refactor.md)。Unity 生成的 csproj 必须包含所有新路径；格式、静态编译、Unity 导入、Play 和实际性能分别记录。未做的 Player 构建、长时战斗、最大规模压力和手感验收不能写成通过。

外围工具说明见 [CodeReadability](Tools/CodeReadability/README.md) / [ToolIndex](Docs/ToolIndex.md)。旧快照隔离在 [Archive](Archive/README.md)，历史数据及过往验收不作为当前运行结果。

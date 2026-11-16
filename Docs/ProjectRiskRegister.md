# HY-Sandbox 风险登记与行为契约

基线：2026-11-15，源码 HEAD `385754c` 加当前工作区。覆盖与原始证据摘要见[审查记录](Validation/2026-11-15-project-risk-audit.md)。本文件保存当前状态；后续修复在对应ID更新，避免另建一份相互冲突的登记。

P1：优先处理的数据安全问题；P2：条件性行为/性能风险或关键验证缺口；P3：资源维护风险。确认缺陷可由确定的控制流或隔离实验成立；条件性风险尚未实战复现其后果；验证缺口不等于功能已经失败。以下23项均待处理，**2项P1、20项P2、1项P3**。本轮审查没有修复这些实现。

## 优先处理：存档边界

### R01 · P1 · 确认缺陷（静态）：玩家同名新建直接覆盖

- 触发/后果：普通建造UI输入已有玩家存档名，新建入口只检查非空，`CreateNewSave` 清空当前缓存/实体后直接覆盖文件，无已有文件检查或显式覆盖确认。与敌方新建已有 `File.Exists` 保护不同。
- 证据：[MainUIPanels.OnConfirmCreate](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs)、[SaveManager.CreateNewSave](../Assets/Scripts/Features/Building/SaveManager.cs)。未用真实玩家存档做破坏性复现。
- 下一步：共用不覆盖新建操作，先成功创建再切换会话；在临时目录用同名哨兵文件验证内容、当前选择与实体均保留，再验证正常新建。

### R02 · P1 · 确认缺陷（隔离实验）：文件名可越过存档目录

- 触发/后果：`BlueprintFileStore.GetPath` 直接拼接名称，只有重命名/复制检查文件名；新建UI只判非空。`../escaped` 或绝对路径可把读写指向预期目录之外，不应把UI显示名当作已验证路径。
- 证据：[BlueprintFileStore.GetPath/CanUseFileName](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintFileStore.cs)、[SaveManager](../Assets/Scripts/Features/Building/SaveManager.cs)。真实已编译类的临时目录实验确认 `fixture/Saves + ../escaped` 解析为 `fixture/escaped.json`，未写玩家文件。
- 下一步：在统一存储边界验证所有名称及规范化路径归属，创建/读取/删除/重命名沿用同一契约；测试分隔符、绝对路径、保留名称、空白、大小写同名和正常中文名，失败时不能改变当前会话。

### R03 · P2 · 条件性风险：写入失败后内存已经提交

- 触发/后果：建造保存或删除遇到磁盘满、权限/锁失败；`BuildBlueprintEditor` 先修改 `cachedData.blocks`，后写 `.pending` 并替换，异常无事务回滚。磁盘旧文件虽有替换保护，缓存却可能已包含失败修改；后续成功保存可能把它一起提交。
- 证据：[BuildBlueprintEditor.Save/RemoveData/Write](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs)。静态确认顺序，未故障注入真实存档。
- 下一步：候选数据写成功后再发布缓存/场景状态，或完整回滚；临时文件锁/只读故障测试新增、删除和重试，核对磁盘、缓存、连接、撤销状态一致。

### R04 · P2 · 条件性风险：加载校验前修改选择并清理其他单元

- 触发/后果：非Siege入口选择损坏/消失的蓝图，或多场景并存；`LoadBlueprint` 先改当前名称并全局查找销毁其他 `ControlUnit`，随后底层加载才校验目标。底层会保留原构造，但其他单位可能已经被清理，名称也已切换。
- 证据：[SaveManager.LoadBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs)、[BlueprintLoader.Begin/TryRead](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs)。底层已有加载版本/父节点归属保护，不应删除这些保护。
- 下一步：预检与会话提交分开，清理限定所属场景/目标域；损坏文件与Additive双单位夹具验证失败前后选择、实体和其他场景均不变。

### R05 · P2 · 条件性风险：蓝图数据缺少统一语义/规模约束

- 触发/后果：用户编辑或外部生成JSON含重复ID、异常尺寸/坐标、超大数组；`TryRead` 检查JSON形状及blocks数组，工厂直接恢复尺寸/变换/ID。单驾驶舱校验不负责这些字段，可能导致加载成本失控、身份冲突或异常几何。
- 证据：[BlueprintLoader.TryRead](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs)、[BlueprintBlockFactory.TryRestore](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs)、[ModularUnitValidator](../Assets/Scripts/Features/Simulation/Units/ModularUnitValidator.cs)。缺资源路径已有跳过逻辑，不能等同于完整数据验证。
- 下一步：定义合法规模、有限数值、尺寸和ID规则，在销毁旧构造前整体验证；用无效/超大临时蓝图测试可取消、诊断和旧状态保留。

## 控制、战斗和生命周期

### R06 · P2 · 条件性风险：导航制动模型与实际能力不同

- 触发/后果：低供电、非对称推进、转向慢或没有足够反推时，导航仍按配置制动加速度计算 `v²/(2a)`。新推进执行层已修正方向/油门语义，但不会自动让导航配置等于真实减速度，可能过晚减速或反复恢复。
- 证据：[VesselNavigation.Evaluate](../Assets/Scripts/Features/Navigation/Vessels/VesselNavigation.cs)、[FlightPropulsionControl](../Assets/Scripts/Features/Flight/FlightPropulsionControl.cs)、[UniversalThruster](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs)。执行层路径如迁移以CodeIndex为准。
- 下一步：对比可用反向推力、质量、喷口转向延迟与预测；Type03/单向/不对称三类真实舰分别测满电、低电、断电恢复，记录到达时间、航程、停车距离及方向反转次数。

### R07 · P2 · 条件性风险：供电帧与物理步不同步

- 触发/后果：低渲染帧率的一帧包含多个FixedUpdate，或供电需求在物理步变化；网络按 `Time.frameCount` 每Update重建一次，执行器消费上一份分配。可能出现陈旧容量判断和尾推；不能假定断电同一物理步立即生效。
- 证据：[PowerTransmissionDevice.Update/OnDisable](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs)、[FlightPropulsionControl](../Assets/Scripts/Features/Flight/FlightPropulsionControl.cs)。最后一个中继退出已有立即Clear保护。
- 下一步：明确允许延迟/响应平滑，分别记录需求、分配、实际力的步号；15/60/144fps与多个fixedDeltaTime下做加减负载、发电机损毁和恢复测试。

### R08 · P2 · 条件性风险：远距清理可删除仍有效的非玩家舰

- 触发/后果：正常游玩中有效友军/任务敌人持续超过清理距离；距离入口只排除玩家，延迟回调只核对ID及距离，未核对驾驶舱/任务身份。`ConvertGroup` 的有效舰保护不阻止随后 `Destroy`。
- 证据：[RuntimeGroupCleanup.EnforceDistance](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupCleanup.cs)、[SalvageCleanupSession.CleanupDistantGroupAfterDelay](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs)。据点休眠距离另有策略，不能据此断言当前据点必然触发。
- 下一步：明确碎片清理、有效AI退场、世界休眠各自所有者；有效友军跨阈值、回到阈值内、任务舰和无舱残骸分别验收，不绕过正常结算。

### R09 · P2 · 条件性风险：重新启用会重置耐久/攻击者

- 触发/后果：未来对象池、暂停或新场景停车只SetActive(false/true)，没有状态迁移；`Durability.OnEnable` 默认重置状态，可能把隐藏/恢复变为免费修复或丢失击杀归属。
- 证据：[Durability.OnEnable/PreserveStateForSceneTransfer](../Assets/Scripts/Features/Combat/Damage/Durability.cs)。已有场景迁移保留标记和世界休眠显式快照，这不是所有重新启用都存在的缺陷。
- 下一步：区分全新出生与原实例恢复；半血及LastAttacker夹具覆盖父节点启停、池复用、休眠、场景转移和最终死亡结算。

### R10 · P2 · 条件性风险：维修查询满载后静默截断

- 触发/后果：范围内超过1024个碰撞体；维修扫描扩容到上限后直接使用无序截断结果，没有对外饱和状态，部分受损模块可能持续不可见。
- 证据：[RepairTargetSearch.Scan](../Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs)。同目标多Collider已有去重，不能解决未进入缓冲的目标。
- 下一步：记录饱和并采用分区/轮换候选，测试1023/1024/1025及密集多舰，验证受损目标最终被处理且查询成本有界。

### R11 · P2 · 条件性风险：物理与全局注册表的场景归属不统一

- 触发/后果：引入LocalPhysicsMode或同时运行隔离测试世界；导航使用所属PhysicsScene，弹丸/维修仍有静态Physics查询，货仓候选使用全局Active。可能跨场景选目标或漏掉所在世界。
- 证据：[NavigationObstacleQuery](../Assets/Scripts/Features/Navigation/Local/NavigationObstacleQuery.cs)、[TurretProjectile](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs)、[RepairTargetSearch](../Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs)、[CargoHold.FindReceiver](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs)。当前共享默认物理场景不等于已发生跨世界错误。
- 2026-11-16：统一爆炸已限定源PhysicsScene，双隔离场景同坐标验证伤害及冲量均不串域；14项跨对象、18项建筑和33项物理回归通过。见 [统一爆炸验证](Validation/2026-11-16-shared-explosions.md)。
- 下一步：规定查询/注册归属，双物理场景同坐标夹具验证弹丸、维修、回收和导航只作用于所属域。

### R12 · P2 · 条件性风险：输入管理依赖完整场景服务

- 触发/后果：部分场景单独启动、销毁顺序变化或保留旧单例时，`InputManager.Update` 的依赖保护被注释，随后直接读PlayManager/BuildManager；键盘存在且keyInput为true时可触发空引用。
- 证据：[InputManager.Update](../Assets/Scripts/Features/Player/InputManager.cs)。部分其他管理器仅Awake赋instance，不能自动推导卸载时引用已安全释放。
- 下一步：明确服务装配与退出顺序，释放本实例单例，必要依赖缺失时可诊断；单场景、菜单往返、快速退出和禁用Domain Reload分别测试。

## 回收、世界与资源预算

### R13 · P2 · 条件性风险：货仓预留按包计、实际容量按数量计

- 触发/后果：多个Bot搬运amount>1的货包；预留每包加1，Store按Free与amount接收，先到的大包可占用其他任务预期容量，导致改派/反复运送。
- 证据：[CargoHold.CanReceive/Reserve/Store](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs)、[LootDrop.Deliver](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs)。Deliver会扣除已存数量并释放余包，**没有证据支持直接丢货的结论**。
- 下一步：明确预留的是包还是物品数量；两个大包竞争小仓、途中容量变化及部分交付验证总量守恒、预留释放和任务最终完成。

### R14 · P2 · 条件性风险：货币包上限合并改变掉落地点

- 触发/后果：达到maxCurrencyPackets后，Spawn在全局Drops中选择最近同类包合并，没有最大距离/场景条件；远处新掉落可能进入另一区域或另一场景已有包，甚至已搬运包。
- 证据：[LootDrop.Spawn](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs)。金额溢出已有保护，本项讨论位置与归属。
- 下一步：明确合并域、距离及已认领包策略；多区域/双场景/已搬运包达到上限后核对数量、位置及任务归属。

### R15 · P2 · 条件性风险：无关体素修改让区块网格持续重做

- 触发/后果：LoadColumn异步构网期间持续爆炸或编辑其他区域；任意Set增加全局Revision，加载循环对比全局版本后重做，远处修改也可能使本区块迟迟不能准备碰撞。
- 证据：[ProceduralVoxelWorld.LoadColumn](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs)、[VoxelWorldData.Set](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs)。已有加载版本/取消与资源释放保护；本项不是宣称后台任务写Unity对象。
- 下一步：区域版本或有界重试/局部更新，持续远处修改时测目标柱碰撞就绪时间、丢弃次数、CPU和内存上界。

### R16 · P2 · 条件性风险：流式保护缓存的舰体范围过期

- 触发/后果：玩家根Transform保留而修复/增减模块改变体积；StreamingGuard仅tracked对象变化时捕获localBounds。可能漏检新伸出的模块或让已缩小舰体等待多余区块。
- 证据：[WorldPlayerStreamingGuard.Tick/CaptureLocalBounds](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs)、[RuntimeGroupBuilder](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs)。重组可保留驾驶舱根，根引用不代表拓扑未变。
- 下一步：按拓扑版本失效缓存；在区块边缘对同根舰体修复外伸模块/损毁后验证就绪包围盒与真实Collider一致。

### R17 · P2 · 条件性风险：世界快照接受未知版本

- 触发/后果：未来版本快照被旧程序读取；LoadSnapshot仅特殊迁移version1，其余直接返回。字段默认化后可能把未知格式当成当前格式使用。
- 证据：[WorldLabPersistence.LoadSnapshot](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs)。已具备16MB上限、严格UTF8与临时写入/备份，不将它误报为裸写文件。
- 下一步：显式支持版本范围、逐版本迁移；旧版/当前/未来/损坏样本验证拒绝未知版本且不覆盖原文件。

### R18 · P2 · 条件性风险：对象池限制空闲数量但不限制活跃峰值

- 触发/后果：高射速、多舰齐射及连续爆炸；弹丸每Prefab空闲上限128，VFX空闲有界，活跃实例仍随负载增长。池存在不代表瞬时分配/内存峰值有界。
- 证据：[TurretProjectilePool](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs)、[VfxEffectPool](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs)。不据此称为内存泄漏。
- 下一步：代表性压力测试活跃数、GC、帧耗时、内存峰值及退场归零；视觉效果可降级，具有伤害语义的弹丸限额须有明确玩法策略。

## 尚缺的纵向验证

### R19 · P2 · 验证缺口：基地起降有独立执行契约

- 触发/后果：飞控/供电/悬浮公共改造只测EnemyController；SiegeReturnFlight会关闭AI/悬浮并按供电能力直接控制刚体，不能由普通直航通过推定起降通过。
- 证据：[SiegeReturnFlight.BeginLaunch/BeginReturn/Tick/Stop](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs)。已有推力请求释放、悬浮恢复和停靠容差；其独立执行方式不自动构成重复算法缺陷。
- 2026-11-16：修复辅助导引绕过推进输出导致无喷焰的问题。真实机库34项（含水平/升力喷口强度与停靠释放）、隔离返航/经济18项、悬浮16项通过；普通物理输出由导引独占防止重复加力。见 [验证](Validation/2026-11-16-siege-guidance.md)。
- 2026-11-16 P3a：真实机库34项再次通过，完整出库7.96秒/返航7.32秒；隔离返航18项通过，含断电宽限/工作请求释放。五场景原预设8/6通过Editor升级为12/12。
- 剩余缺口：完整发射→交战→返航、满尺寸/多舰、重组转交及人工视觉仍需继续验证；本轮不把原独立执行契约改为普通AI驾驶。

### R20 · P2 · 验证缺口：CI无法覆盖纯资产/配置及行为回归

- 触发/后果：只改Scenes/Resources/Packages/ProjectSettings时当前工作流路径不触发；现有job验证可读性/工具源码，不编译Unity项目或执行玩法。绿色CI不足以证明序列化和执行器契约。
- 证据：[code-readability.yml](../.github/workflows/code-readability.yml)、[已有Editor验证入口](../Assets/Editor)。项目有大量实测工具，问题是覆盖与自动触发，并非没有测试。
- 下一步：补资产/配置触发和轻量引用/数据检查，按环境接入代表性Unity/Player回归；用一次纯Prefab参数变更确认触发与报告范围。

### R21 · P2 · 验证缺口：专项导航成功不等于长期实战高效接敌

- 触发/后果：只看无碰撞/到达标志忽略接敌范围外徘徊、直航微调、供电变化和战斗移动目标。Lab关闭碰撞伤害，上一轮Type03等短回归不能证明多轮5v5效率。
- 证据：[推进执行层回归](Validation/2026-11-15-propulsion-input.md)、[竞技场追踪](Validation/2026-11-15-arena-navigation-debug.md)、[EnemyLabRecorder](../Assets/Scripts/Features/Navigation/Diagnostics/Lab/EnemyLabRecorder.cs)。记录器有约1秒Flush和IO异常保护，不能按10Hz采样误报每样本强制刷盘。
- 下一步：固定种子/蓝图的多轮5v5，按舰记录有效接敌时间、航程、停滞、范围外徘徊、喷口/命令变向次数及真实供电；效率优先，轻微接触单独观测，卡死和反复换向算失败。

### R22 · P2 · 验证缺口：制作工具批量重建与用户调整的长期兼容

- 2026-11-16材质：真实Prefab新增支持组件，空ID旧蓝图保留参数；四材质JSON往返、延后CSV初始化、建筑受损快照与材质造价共28项通过。真实Main保存材质选择器并检查截图；生成工具只补缺失资产，现有参数保留。详见 [材质验证](Validation/2026-11-16-structural-materials.md)。

- 触发/后果：CSV、Bake、场景升级或Prefab模板工具后续扩展时，只验生成成功，可能覆盖Inspector override、手工布局、GUID或运行时依赖；本轮167个Editor脚本为库存与关键入口抽样，未逐一执行所有菜单。
- 证据：[Assets/Editor](../Assets/Editor)、[项目制作约定](../AGENTS.md)、[现有验证记录](Validation)。未确认本轮存在某个具体工具破坏资产，不将通用关注点写成已复现缺陷。
- 下一步：受影响工具采用带手工override夹具，连续运行两次、保存重载，核对GUID、引用、参数和非工具拥有对象；新建资源必须由连接Editor导入后验收。

### R23 · P3 · 条件性风险：默认Volume遗留序列化脚本引用

- 触发/后果：DefaultVolumeProfile仍含4种无法解析的脚本GUID及测试组件遗留数据，URP GlobalSettings引用该配置；未来重存/管线升级时需要确认哪些数据会被丢弃。
- 证据：[DefaultVolumeProfile](../Assets/Settings/DefaultVolumeProfile.asset)、[URPGlobalSettings](../Assets/Settings/UniversalRenderPipelineGlobalSettings.asset)。AssetDatabase确认4种GUID不解析，但真实加载的19个Volume组件无null，**不认定当前渲染已经失败**。另18种未解析GUID在第三方演示资源，不纳入自有脚本修复。
- 下一步：备份后在Volume Inspector核对有效override、重存diff及PC/Mobile实际画面；只有确认废弃数据后再清理，不补造GUID或重写第三方资源。

## 未来修改前必须保持的行为契约

| 影响域 | 需要追踪的契约 | 最小可观察验收 |
| --- | --- | --- |
| 导航/推进/悬浮 | 目标→参考速度→方向+油门→供电/喷口→真实运动；无障碍直航不持续微调，效率优先 | 真实舰命令与喷口角、到达时间、航程、停滞及断电恢复 |
| 存档/建造 | 名称不越界，新建不覆盖；验证→写成功→提交缓存/场景；失败可重试 | 临时目录哨兵、故障写入、损坏加载，原数据/选择/实体保留 |
| 回收/返航 | 数量守恒、部分交付、预留释放；返航保存失败保留会话 | 两Bot竞争、余包、死亡/幸存与保存失败后重试 |
| 伤害/重组 | ID/阵营/攻击者与物理状态继承；同事件结算一次 | 断裂、修复、再启用、跨场景、重复回调后的总量/归属 |
| 世界/异步 | 结果属于当前会话/区域版本；过期结果不发布，任务/网格释放 | 加载中退出/重开、持续爆炸、同根舰体变形和碰撞就绪时间 |
| 基地/控制权 | AI、玩家、悬浮、机库只有一个执行所有者；退出释放请求 | 发射/返航/取消/损毁后门、控制器、功率及停靠状态 |
| 制作/序列化 | GUID、合法类型和仍有价值的手动数据迁移；重建可重复 | 有override资产两次Bake与保存重载、真实Editor导入 |
| 验证/交付 | 期望、命令和真实结果分开；静态、Editor、Play证据分开 | 每次提交简记影响风险、实际证据、未测边界，更新对应ID |

不把表中所有场景强制套到每次小改动。只检查本次受影响链；共享机制至少选两个真实消费者和差异边界。风险登记不是修复证明，修复后需记录版本、实际检查与残余限制。


### R24 · P2 · 模块机库预算与旧数据迁移

- 当前契约：机库尺寸偶数/步长2，真实2×2×1金属墙块进入建筑耐久和连接系统；损伤快照版本保留缺口，原设计付费维修补块。旧奇数设计内存向上迁移、不自动覆盖个人文件。官方example已替换且备份。
- 证据：Shell Play15、资产Edit22/Play22、工坊真实交互53、起降34、新宿主41项通过；实际游戏截图已查看。见[模块机库验证](Validation/2026-11-16-modular-hangars.md)。
- 剩余：六个默认机库可能超过2048模块并明确拒绝；预览层级仍有内存开销；Player构建、多库战斗性能与顶部门未验证/未实现。现有1m独立门面板作为控制器碰撞保留，固定墙体均为真实模块。

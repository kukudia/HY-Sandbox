# HY-Sandbox 风险登记与行为契约

### R42 · 特殊天气时序与风场（2026-11-25）

`WeatherEventDirector.Tick` 以模拟时间驱动预警与活动期。抽中天气的 Siege 波次由 `SiegeMatch.SetWaveObjectivePending` 挡住提前返航，战斗时限、失败与退出仍能结束并清理天气。Play Mode 重新加载两个正式场景，强制覆盖两种结果，检查 30 秒预警、归零时才生成、实例归属、取消和到期清理，以及 Siege 先清敌再完成天气时的一次性结算。

`WeatherTornado.ApplyWind` 只在单个活动龙卷风的物理步扫描 `ControlUnit.RuntimeUnits`，按场景、模拟状态、半径和高度筛选，向命中的非运动学刚体施力。按单位查询可在舰体拆分后命中新归属的刚体。单位退出注册、离开作用范围、转为运动学或天气清理后停止受力。尚未验证玩家视角的视觉强度、实际舰体受力曲线和高单位数性能。


### R41 · 编辑器运行日志与暂停覆盖层（2026-11-24）

异常冲量记录器与 20 个已保存的场景/Prefab 诊断组件相关，迁移后探针和记录器默认停用；制作工具不再补挂。关闭敌舰索敌和守卫线框球，保留小尺寸标记。暂停面板的 Canvas、排序与射线接收器由 Build 场景持久化；悬浮控制在运动学阶段停止输出。Unity Edit/Play 从主菜单进入正式 Siege 示例，验证暂停、恢复、70 秒战斗及返回菜单，共 8 项检查，运行窗口内无警告或错误。未遍历全部地图和长期对战组合。

### R40 · 激光光束末端与命中部件生命周期（2026-11-24）

`TurretHitscan.Fire` 在伤害处理前保存实际命中碰撞体的局部点和法线。`LaserTurret.LateUpdate` 仅在单次显示窗口内读取该碰撞体。失效后从最后有效位置快速淡出，炮塔停用立即清理。打空按原射程绘制，专用图在最后 13% 衰减。隔离 Unity Play 对真实炮塔 Prefab 的发射入口执行 15 项检查，覆盖旋转、位移、销毁、打空、粒子发射、复用和禁用。自然交战与大量炮塔同时射击的 GPU 成本尚未采样。



### R39 P2 · 工业美术重建覆盖基地结构外观（2026-11-23）

`IndustrialArtGenerator.RebuildPrefab` 曾删除基础结构的材质引用目标，并按整块尺寸重新生成已缩放的 CombatStructures 网格。已恢复 4 个材质绑定及 11 个网格比例，重建入口排除这两类资产。Unity Play 通过 `BlueprintBlockFactory.TryRestore` 验证 6 份 Siege 蓝图中的 18 种结构组合。整场 Siege 镜头观感待验证。

同批重建覆盖了 CargoHold、CoinHold、TechnologyHold 的货仓模型。已恢复原玻璃、状态灯及货物显示引用，并在工业美术重建入口保护 CargoHoldView 所属 Prefab。

地形失撑只在当前同步调用中移交 BFS 分组；合并地基会修改列表，调用前必须放弃缓存。两块建筑失撑的 Play 探针确认连接图准备一次、主体清空、碎块共享动态刚体。战斗长帧仍需同场景复录。

ControlUnit.UpdatePower 只在成员、工作请求或功率配置变化后遍历本单位发电机与负载。请求者退出时须撤销请求；没有周期校正。RefreshChildren 在结构变化时重新绑定后代，按最近的 ControlUnit 隔离嵌套成员。原本依赖邻近建筑或其他舰船发电的蓝图需要本机电源；独立模块无电。

BuildingRuntime.RefreshTerrainSupport 在地形脏区域命中后按建筑检查支撑：连接图版本变化才重建组清单，地面接触每次重新取样，任一组失撑仍走完整重组。连接组检查只适用于建筑主体；脱锚阈值仍由经理逐次判定。SiegeRefit 自动维修只在单舰同步调用内复用归属快照，不跨船缓存；阻挡、扣费规则不变。需用相同破坏与返航窗口复测耗时。

EnemyController.ConfigureHoverThrusters 遍历本机悬停器：移动决策刷新、数组替换、重新启用或每 0.25 模拟秒校正；保留外部改写恢复能力，需验证接管与返航切换。炮塔按同一射线最近距离判断遮挡，等距遮挡优先。建筑按单次爆炸的保守包围盒排队，可能包含空隙中的建筑；逐格存档事件保持原序。以上需 Player 复测。

BuildingRuntime.OnFragmentDetached：每次多块碎片分离时扫描一次该碎片与主体成员，保留存档顺序；临时集合在回调结束时清空。基线建筑回调峰值 162.06 ms，未证明全部来自移除操作；仍需大建筑断裂与存档回读复测。

Siege 地形变化按建筑去重，每物理步每个受影响建筑最多调用一次 RefreshTerrainSupport，重新分组需要检查该建筑连接与支撑；重算前清除支撑缓存。CheckFleetStalemate 在战斗中每 0.25 秒最多检查一次在场舰艇的缓存推进器及可用武器，遇到可行动舰立即终止，连续五秒才允许无击杀奖励退场；需要实际双失能场景验证。虚空检查每半秒只查询单位及 Debris 标记。

### R35 · 基础部件遍历（2026-11-20）

| 入口 | 触发、范围与处理 | 保留原因与验证缺口 |
| --- | --- | --- |
| ControlUnit.GetDamageTargets / TryGetTotalDurability | HUD 等调用者复用本单位成员名单；直接子节点变化、RefreshChildren 或耐久层级版本变化后重建。查询时仍逐成员汇总有效耐久。 | 当前耐久可直接写入，不能永久缓存总数；跨舰版本变化仍可能引起额外刷新。 |
| NavigationObstacleClimb.GetHullBounds | 爬升、下降、路径选择复用同单位同物理步边界；单位切换、结构变化、Clear 或定时刷新使缓存失效。物理步外仍实时合并。 | 使用真实、启用且属于本刚体的 Collider，避免简化外壳改变净空；同一步内几何变化可能延迟一步可见。 |
| BuildPreview.EnsureGhost / Poll | 新建 Ghost 时收集 Renderer 和 Collider；仅初次、有效性或高亮材质变化时更新 Renderer。Clear 丢弃名单。 | 预览假定同一实例的模型结构稳定；需目视验证动态模型及红绿切换。 |
| PowerTransmissionDevice.Update / PowerNetwork.Rebuild | 网络仅由脏标记或 0.25 秒低频校正触发；一次重建收集所有活动发电机、中继和负载，拓扑变化时更新连接组件，其余阶段仍按负载名单分配并提交状态。连接线按网络版本、配置、位姿和约 15Hz 动画刷新。 | 全局图保留独立网络共同补足负载的行为；需同场景对照确认供电启停、移动/范围变化、调试开关和低频校正，及网络/范围网格/连接线 marker 的峰值。 |
| RepairBot.FixedUpdate / SiegeReturnFlight.Tick | RepairBot 只有完成一次空目标扫描后才会在 Home 停靠时禁用 FixedUpdate；同 Home 的 Durability.Damaged 事件唤醒后会强制重新扫描。Home 组件按层级版本缓存。返航在 BindUnit 缓存有效推进器、类型和 Power，读取/发布各遍历一次。 | 事件唤醒依赖损伤事件归属正确；返航仍需覆盖重组、推进器启停、供电变化和失能结算。需比较全量机器人与仅活跃机器人调用次数及 Siege 子 marker。 |
| TurretProjectile.Launch / ClearIgnoredCollisions | 每枚实体弹遍历发射单位碰撞体建立忽略对，回收时解除。 | 现有实体物理依赖逐对忽略，不能只过滤伤害；大型母舰连射仍需度量。 |
| ExplosionPropagation、RuntimeGroupBuilder、ControlUnit.RefreshChildren | 爆炸、断裂、加载和重组时访问相关块、刚体或子组件。 | 保留逐块伤害、拓扑与所有权正确性；大型结构连续断裂需测峰值。 |
| ControlUnit.HasCombatCapability、BlockConnectionSnapshot、建造连接提示及推进器输出 | 能力查询扫描武器、连接验证读取碰撞体或连接面、推进控制逐执行器施力。 | 不把必要的单件操作计作可省略；建议按调用者、结构规模和调用频次采样。 |

审查覆盖自有运行时脚本的层级查询检索及上述调用链抽查，不等于逐行审查所有 LINQ、循环或 Editor 验证脚本。当前没有同场景 Player P99 对照。

2026-11-20 舰体索敌：Unity Play 的 18 项 AI 与 32 项炮塔检查通过。单舰全遮挡时，2 与 202 个带 Collider 的部件均为 4 次逻辑探测；缓冲饱和可重试物理查询。有限采样不保证发现任意细缝中露出的部件。未复现原波次测量 P99，缓存重建成本和密集碰撞命中遍历仍需单独采样。

### 战斗帧时间与响应延迟（2026-11-19）

分组只对已有单位断裂后新建、且没有有效驾驶舱的根赋予 Debris 层。建筑损毁 Play 检查 23 项、炮塔 Play 检查 32 项通过，覆盖碎片退出候选、仍遮挡射线和二次断裂。带驾驶舱的新根、重新接回及残骸密集波次的导航和 P99 尚未单独实测。

炮塔共享目标点和舰级包围盒只在物理步内复用；即时查询读实时位置，遮挡始终从各自炮口检测。Unity Play 炮塔 32 项与 AI 升级 27 项通过，覆盖舰首入射程、禁用、重组、自舰遮挡和备选目标。导航综合套件在无关的 CheckContinuousCourse 空引用处中断。未目视验收调试线，也未对原波次实测射线数量与 P99；全遮挡密集舰仍可能逐部件检查。

大批炮塔同时失去目标时按队列重新搜索，已失效的目标不能继续攻击；新锁定可能晚数帧。Play 已验证结构拆分、重连、单次探测和特效首次启用、复用挂载。需要以相同基地、敌舰和镜头对比 Player 帧时间，同时检查索敌等待和首次开火的特效创建尖峰。原采样不含有效 GPU 时间。

### R34 · 方块概率断链、地基共享锚点与残骸归属（2026-11-19）

- 概率来自爆炸源，范围按候选模块中心距离判定，仅影响同一结构；源块必定移除。连接失效不等于耐久归零；其他结构可能受冲量，不被抽签断链。
- 刷新时所有接地分量留在地基根，脱落分量保留活动 Block 和耐久，由原建筑从存档成员中移除。源块损毁事件通知一次；正常退场和场景卸载采用不同回收规则。
- Unity Play 已通过爆炸集成 30 项、连接图 50 项、地基缓存 16 项、建筑损毁 21 项。包含概率 0/1、跨单位隔离、共享地基、碎片二次断裂继承伤害会话及两代残骸卸载。未做长时间多舰战斗或独立 Player 性能测试。

### R33 · P2 · 夜班输入、暂停、加载与诊断迁移（2026-11-18，实施完成，剩余验收）

- 契约：RMB长按0.18秒临时退出编辑，释放恢复；UI起始不捕获、失焦/应用暂停释放且需新手势；uGUI/TMP文字选择阻止建造及场景快捷键，隐藏选择不阻塞；所有建造入口受同一距离约束。暂停保存并恢复原时间/音频/输入/相机状态，不把暂停写入蓝图。Restart走原模式生命周期，同设计重开。菜单快捷加载只改分帧策略，必须保留统计、取消及连接恢复。
- 拖拽：移动轴预览不逐格写盘，松开仅提交一次历史；取消/文字焦点/失焦/应用暂停/禁用回滚未提交姿态。128项操控Play通过含多格真实鼠标、单次撤销/重做、右键长按取消、三种生命周期中断及旋转按下不跳转/90度拖转/工具切换回滚；失焦/应用暂停使用生产回调，非OS切窗。
- 修复加载：未知材质/非法机库入口TryRestore返回false而非异常；工坊保留快照并继续，战斗创建失败清理。134项跨帧操控/加载Play通过，覆盖前后有效模块、JSON不变及坏舰根对象回收。
- 冷菜单：4260秒/426000帧/852连续样本0错误，分配5.706--5.709GB、预留固定10.268GB；仅冷菜单低负载，不能外推战斗长期内存或Player性能。最新菜单138项Play回归通过；07:30停止Play后场景无脏状态，脚本编译无失败。
- 碰撞：查询缓冲满额扩容重查，保持UseGlobal/范围/自身排除，172项Editor对照与88项操控Play回归通过；不宣称实测帧率提升。
- 战斗：仅真实敌方死亡奖励金币；爆炸只对体素造成破坏，不伤构造模块，仍有有限物理冲量。友舰接管必须唯一，返航/死亡后恢复观察；UI/暂停/非战斗中断清除双击待配对，23项生产交互Play含0.35秒内UI中断与错误捕获通过；阵营筛选应覆盖状态、电网、耐久标签及轨迹。轨迹32槽、每槽1024点，关闭清理，不无限增长。
- 证据：爆炸14项Play、网格774项Editor、真实输入RMB/方向键/ESC暂停恢复、生产Build→Siege20项、交互/筛选/轨迹/Restart20项通过，见[夜班验证](Validation/2026-11-18-night-development.md)。菜单/偏好/快捷加载/30米边界/六面真实放置113项Play及共享HUD四分辨率在纠正原GPU分配失败后重跑通过（仅边界/字形）；工坊反馈禁止抖动相机，避免等距连接点跳格。菜单130项及生产战斗HUD25项Play通过，包含实际投影名牌让位/关闭恢复和暂停动作/文字。六分辨率24页次离屏边界检查通过（非动态文本全量验收），Canvas Expand修复4:3越界与遮挡；工坊输入24项跨帧通过（含已复现的文字Ctrl-Z误撤销修复），Tab不切游戏视角；旧Siege Tab模拟未观测到pressed。Siege真实场景16排除区含专属摘要/时钟/动作/经济/失败/帮助，四比例16页预览及31项生产HUD Play通过，实际投影让位/透明恢复已验证。
- 残舱：仅原设计有船体、现剩无推进/武器单驾驶舱且12秒移动不足2米才按未击杀退场；恢复/重组/移动重置窗口，规则Play10项及真实损毁自然12.16秒等待7项通过，夹具冻结刚体隔离停滞，不代表自然飞行验收。
- 演出：已复现并修复异步卸载最后一帧补舰异常，先同步停用演出全部根对象再卸载；新增0/1/3/8帧提前退出后138项菜单Play通过、错误为空。主菜单入口、冷启动/取消/真实ESC返回与原稿/服务恢复已纳入130项菜单Play回归。修复制作工具删除Status Overlay导致Awake异常，真实场景绑定已保存；独立BattleAttract真实8舰/基地/四镜头已运行；演出单独降低并错峰索敌，VFX显式活动预算默认0不限其他场景。两级预算、回收/销毁/清场10项、AI遮挡等27项和炮塔32项通过。Editor采样仍约58ms主线程；326秒/4962帧/45镜头/40生成舰0新增错误，中位60.8ms，因低提交余量中止30分钟计划。Unity分配/预留存在增长，长期Player内存仍需Profiler，不宣称Player高帧率或最终美术验收。

### R32 · P2 · Siege时限、维修与核心失败结果（2026-11-17）

- 契约：每波默认5秒准备、180秒清场和90秒返航。返航舰停滞、导引失败或超过返航期限只计为无法入库；Core 保留当前耐久，在所有舰艇完成或报废后结算波次收益。真实核心损毁及战斗超时仍按基地失败结算。暂停不计时，准备不消耗战斗时限。
- 维修与交互：成功返航自动尝试原有付费舰艇维修，基地只允许手动维修；不足金币会保留损伤。波次按钮仅部署/整备可见；原施工面板及场内BackToBuild移除。Debug Toggle必须是实际射线目标，不能用直接赋值开关证明可点击。
- 证据及剩余：2026-11-19 Unity Play 返航专项26项通过，含停滞、推进失效、返航到期、核心耐久不变、收益结算及正常入库维修。[11月17日验证](Validation/2026-11-17-siege-flow-polish.md)记录旧行为。尚未进行长期自然多波、独立Player硬件输入与性能基准。

### R31 · P2 · 共享战斗HUD与场景生命周期（2026-11-17）

- 契约：四消费者共享Prefab/主题；名牌按战场与驾驶舱身份过滤，采样和逐帧投影分离，遮挡错峰查询。Siege战报只响应已成功结算的死亡事件，不从累计计数猜测身份；退出清空通知、关闭抽屉并恢复调试偏好。新版默认关注友舰，双击可临时授予驾驶权，推进器数值来自真实缓存执行器。
- 证据：真实Main→SiegeGameplay、5v5、探索Play验证及四分辨率离屏布局预览，详情见[共享HUD验收](Validation/2026-11-17-shared-combat-hud.md)。静态和Editor证据各自记录，不等同独立Player性能验收。
- 剩余：未做长时间多波通知压力/独立Player性能基准；任意中文舰名的字形覆盖、人工硬件输入体验仍需复查。容量默认24名牌/12方位标记，可配置但不承诺无界显示；生成器重建会覆盖其拥有的Prefab，手调应先备份。

基线：2026-11-15，源码 HEAD `385754c` 加当前工作区。覆盖与原始证据摘要见[审查记录](Validation/2026-11-15-project-risk-audit.md)。本文件保存当前状态；后续修复在对应ID更新，避免另建一份相互冲突的登记。

P1：优先处理的数据安全问题；P2：条件性行为/性能风险或关键验证缺口；P3：资源维护风险。确认缺陷可由确定的控制流或隔离实验成立；条件性风险尚未实战复现其后果；验证缺口不等于功能已经失败。原始审查登记R01–R23（2项P1、20项P2、1项P3）；修复与验证进展在各ID补记。R24起为后续开发新增契约及残余风险，不能将原始数量当作当前未修复项统计。

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
- 更新：2026-11-18已恢复Update必要依赖保护；菜单往返130项通过。单场景、快速退出和禁用Domain Reload仍未完整验证，单例销毁治理仍待后续。

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
- 2026-11-21：舰船遇障决策改为先尝试经整舰垂直/前向净空验证的升降通道，失败后才进入水平 A*；尚未以 Unity Play 对照记录证明到达时间、航程或停滞改善。
- 2026-11-24：`VesselNavigation.Evaluate` 沿期望航向优先检查静态越障通道，已有 A* 路线也可让位。每轮移动决策用复用 BoxCast 缓冲扫描所属物理场景的候选碰撞体，最多 1024 个；需过滤自舰、移动障碍并验证整舰净空。饱和时回退或制动。Lab Play Mode 验证侧滑、移动交通、缓存路线与顶棚；真实 Finch 14.6 秒无接触越过 19 米墙，舰底进入墙体前缘时为 20.93 米。多舰密集场景查询耗时、复杂城区全程及其他舰型仍待复测。
- 2026-11-21：EnemyController 高度配置改为决策/组件校正边界刷新，完全停止时跳过重复推进命令；运动和制动仍逐物理步更新，需在多舰场景比较 FixedUpdate、推进器和到达响应指标。
- 2026-11-21：Main/Universal 每个有权执行的喷口仍逐物理步输出，以保留各自方向、供电与作用点力矩；模拟油门只在舰级计算，取消逐喷口输出限速。Unity Play 隔离推进专项7项通过，包括不同旧限速值下双喷口同一0.4输出比例及归零清理。完整 ActivePowerPlayChecks 在后续 RepairBot 任务供电请求断言失败（Main/Universal 专项已通过），本轮未归因或改动机器人行为。失去驾驶舱、无油门、断供或导引接管时普通输出失效。玩家键盘输入立即生效，需实测接管、停车和低供电恢复。AI 的 `FlightPropulsionControl.AvailableAcceleration` 仍在活动步扫描舰船缓存推进器以估计方向相关的供电推力，应在 Profiler 中单独量化。
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

- 当前契约：机库尺寸偶数/步长2，真实2×2×1金属墙块进入建筑耐久和连接系统；损伤快照版本保留缺口，原设计付费维修补块。旧奇数设计内存向上迁移、水平整格对齐并保留底部高度，不自动覆盖个人文件。官方example已替换且备份。
- 证据：Shell Play15、资产Edit22/Play22、工坊真实交互53、起降34、新宿主41项通过；实际游戏截图已查看。见[模块机库验证](Validation/2026-11-16-modular-hangars.md)。
- 剩余：已移除单建筑2048/世界8192模块硬限，尺寸/身份/资源与建筑数量校验保留；预览层级仍有内存开销；独立Player单库流程已通过，多库战斗性能待验证；顶门已实现并通过真实单舰起降/偏轴返航与工坊方向保存验证。现有1m独立门面板作为控制器碰撞保留，固定墙体均为真实模块。


### R25 · P2 · Siege有限岛屿、支撑与边界

- 当前契约：岛屿在部署前按基地占地确定，复用World有限体素与DestroyManager破坏；移除旧平板，战斗不自动扩图/补坑。界外敌舰释放波次名额但不增加击杀，友舰报废，核心坠落失败。
- 证据：数据7/真实部署与破坏11/边界消费者6项；新版、宽基地、预研机库完整链；WorldLab Play16与180固定步地形接触。正式场景烘焙72个网格并保存重开，实际截图已查看。见[浮空岛验证](Validation/2026-11-16-siege-island.md)。
- 剩余：大基地部署前加粗体素会影响破坏粒度；自然坠落轨迹与长期战斗性能未验证；独立Player岛屿部署和短时自然战斗已覆盖。土岩没有全岛动态结构解算，不能将建筑脱锚等同于整岛坍塌。

### R26 · P2 · 建造快捷栏与个人UI配置

- 契约：稳定部件路径/材质ID只写独立UI偏好，未知值拒绝；战斗、加载、文字输入与模态不消费建造数字键。结构选材质，功能模块不被强改材质。蓝图、货仓、经济规则保持各自所有权。
- 证据：真实Main Play26项通过；InputSystem键盘、启用输入框焦点、EventSystem drop/swap及射线命中、真实uGUI模拟鼠标完整拖拽、偏好重载/损坏回退；实际截图排除统计卡遮挡。见[快捷栏验证](Validation/2026-11-16-build-hotbar.md)。
- 缺口：小屏、Player完整拖拽/配置交互与人工硬件鼠标复查尚未验证；独立Player返工坊保留8槽已覆盖；暂不代表有限背包/材料库存。

### R27 · P2 · 波次清场与回收归属

- 契约：扫描仅回收同场景全敌方无驾驶舱残骸；共用最短保留期、幂等掉落，不改Match金币/击杀/阶段与建筑伤势。旧删除协程在活动扫描持有合格残骸期间等待，取消立即释放。掉落显式归属来源场景，合并及接收器不跨场景。
- 证据：两宿主隔离残骸回收各15项、既有预算15项、完整起降/结算宿主回归；真实扫描截图发现标题重叠已移位。见[清场验证](Validation/2026-11-16-siege-cleanup.md)。
- 缺口：生产延迟协程等待/取消恢复2项与共用回收Play71项已通过；独立Player返航/结算/清场闭环及自然第一波后继续第二波已覆盖；长时间多波性能仍未验证。


### R28 · P2 · 可选机库开口与真实通道

- 契约：Front=0兼容旧蓝图，Roof=1随正常保存/复制/撤销链传递；部署只接受这两种值，受损展开快照不补墙。顶门墙块/门板/防夹查询与路线一致；物理导引仍依赖存活有电发动机，不无条件传送。
- 证据：顶门真实Play36项、工坊58项通过，截图确认顶面开口与方向标记；顶门出库4.72s、直返5.96s、偏轴返13.38s。验证入口及静态结果见[方向验证](Validation/2026-11-16-hangar-entrance.md)。
- 缺口：多机库同时通行、上方人为封堵、各舰型平衡及独立Player方向交互尚未覆盖；前门仍可旋转到各水平方向，未提供任意侧面独立开口。


### R29 · P2 · 浮岛非凸碰撞查询与Player观察

- 发现：自然Siege中避障/炮弹直接Collider.ClosestPoint接触非凸MeshCollider会持续产生Unity不支持API日志。已统一接入既有ImpactPhysics兼容查询；非凸网格使用bounds保守回退，不宣称精确三角面最近点。
- 证据：专门非凸接触5项、共用避障10项通过；独立全场景Windows包构建成功，前门/顶门脚本伤害闭环各22项通过。真实自然复跑与日志记录见[Player验证](Validation/2026-11-16-siege-gameplay-player.md)。
- 限制：首轮隐藏窗口截图全黑；其帧耗时不代表有渲染Player性能。首轮自然180秒未清首波；修复后181.83秒完成第一波进入第二波，ClosestPoint日志及捕获错误均为0，真实画面已检查；前门600秒目标观察于128.04秒在第二波核心摧毁而提前结束，正常退出且错误0；短时观察不能当作长时间多波验收，顶门600秒目标复跑也于248.90秒第二波核心摧毁而提前结束，错误0；示例平衡待调整，不能按两次随机战况比较门方向优劣。16:24 Editor在Pipeline Roslyn eval调用时崩溃且NightDesk记录OOM退出；未确定单一根因，已恢复Editor、避免eval启动构建并串行限制负载。


### R30 · P2 · 三套示例、自由观察与大型基地

- 契约：保留自由建造不限量和Siege部署/维修预算；三份示例共享配置目录与菜单/工坊出战链，功能模块必须具备真实支撑与供电。解除模块数量硬限不承诺无限性能。
- 连接修复：密集碰撞候选先验证相对连接面再稳定选取；战斗逻辑机库连接真实底板上表面内部，周边连接面留给墙体；禁用编辑选取盒的机库只保留仍满足双侧/同域/当前几何的既有连接。工坊原放置契约保留。
- 当前证据：三套示例Play18项、自由观察真实InputSystem输入22项通过；官方及个人10份基地生产恢复/支撑审计全部通过。55份蓝图结构审计未发现应隔离的损坏JSON，个人创作未删除。大型容量及独立Player结果见本轮验证记录。
- 边界：任意大基地仍受内存、导入与物理成本约束；长期多波、多舰同时通行与人工硬件输入体验不由隔离检查替代。

- Meridian Airworks 官方示例已通过 Unity 数据与 Play Mode 恢复检查：扩建后 612 个设计模块无重叠，所有结构有地基支撑，功能负载全开时供电充足。四库同时收放载具、受击后的设施存活率及完整守波尚未实测。

- Meridian Airworks 大型前门库至常规飞控的 AirKing 交接已在隔离 Play Mode 验证。交接后 3 秒最大掉高 0.36 米，最低垂直速度 -0.47 米/秒。带敌波和完整浮空岛的战斗仍待实测。

- FlightPropulsionControl.AvailableAcceleration 按方向与供电状态短时缓存，缓存期限 0.08 秒。Thruster 的 ControlUnit/Rigidbody 引用在重挂父级时失效，正常物理帧不再重复 GetComponentInParent。需用相同敌舰数量复测推进器单次和敌舰推进 marker。

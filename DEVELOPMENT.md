# HY-Sandbox 开发入口

单位供电按成员、请求和配置变化合并重算；无周期校正，负载不再执行 LateUpdate。CSV 列名保持不变。

当前运行时供电入口为 ControlUnit.UpdatePower。发电机与负载绑定最近的单位，按单位输出和请求比例分配。中继仅保留旧蓝图字段，不再跨单位供电或绘制范围。

地形变化后，建筑复核各连接组的地面支撑；全部仍受支撑就保留刚体。Profiler 的 HY.Building.CheckTerrainSupport 包含检查和可能的重组，HY.Building.RegroupAfterSupportLoss 单独计量重组。波间维修按舰复用报价的模块快照；供电分配直接读取连通组名单。

爆炸合并建筑支撑通知；炮塔复用候选边界，最近遮挡才查归属；敌机按名单变化与校正周期配置悬停器。实际帧耗时待复测。

建筑断裂按碎片批量移除主体成员；弹丸命中结算可通过 HY.Projectile.ResolveHit 采样。整帧收益待同场景复测。

Siege：战斗双击接管，Tab 或按住 Alt 点击 FREE VIEW 返回观察；Recovery 时限 60 秒。基地手动 Repair 同时补齐占地内已毁地形。波次清除显示 CLEAR，余额变化在 Credits 旁显示。

基础部件遍历规范见 AGENTS.md，现有热点与保留原因见 Docs/ProjectRiskRegister.md 的 R35。总耐久查询复用成员名单，避障同物理步复用碰撞边界，建造预览按状态变化更新外观。

2026-11-20 炮塔按 ControlUnit 选择舰体，不再枚举 Durability。局部包围盒与六个外侧采样点共享；每次重选每艘射程内候选舰最多探测四个方向，实际命中表面用于瞄准。激光与炮弹按最终命中 Collider 扣对应部件耐久。见 Docs/RuntimeFlow.md 的“炮塔舰体索敌到实际命中扣血”。

战斗断裂后，无有效驾驶舱的脱离分组使用 Debris（层 9）。远距预测和局部路径规划过滤残骸，近场避障和炮口遮挡继续查询实体。炮塔遍历 CombatUnits，清理与回收仍可遍历 RuntimeUnits。

炮塔索敌（2026-11-19）：每舰按物理步共享目标点包围盒，各炮塔按自身射程和炮口遮挡选择最近可见部件。DebugManager → Turret Debug 可显示青色目标包围盒、绿色可见射线及截至遮挡点的红线；性能采样时关闭。Profiler 的 `HY.Turret.SharedBounds` 表示实际重建，与 `HY.Turret.Search`、`HY.Turret.Visibility` 对照查看。

战斗慢帧（2026-11-19）：炮塔完整索敌共享每渲染帧 16 次预算；冷却中仍转向。生成舰体与损毁重建复用每个连接面的探测结果，未播放的挂载特效按需创建，停靠机器人停止刷新尾迹。Unity Play 已验证连接重建与挂载特效；原波次的 Player P95/P99 尚未复测。

## 方块结构损毁与重组（2026-11-19）

飞船、建筑、基地和独立 WorldLab 共用 `DestroyManager.DestroyTarget` → `ExplosionPropagation.DisconnectBlocksInExplosionRange` → `RuntimeGroupBuilder.RefreshUnit`。爆炸源的概率按同一 ControlUnit 内、半径内的候选块各判定一次；命中断开该块全部连接，不扣邻块耐久。重组完成后对附近刚体施加冲量，体素伤害仍由 explosionDamage 控制。

地基通过 `IBlockFoundation` 提供共享锚点：接地的多个分量保留在同一主体，其余分量取得独立刚体并继续参与损毁。建筑只消费销毁事件、更新存档成员和跟踪残骸寿命。新增无地基结构只需使用 Block 和 ControlUnit，不需要新增销毁或重组分支。验证记录见 [风险登记 R34](Docs/ProjectRiskRegister.md)。

本轮需求对照、恢复点和后续计划见[Siege阶段交接](Docs/Validation/2026-11-16-siege-stage-handoff.md)。

## 夜间交互与诊断改进（2026-11-18）

冷主菜单持续观察入口：`Assets/Editor/MainMenu/MainMenuIdleChecks.cs`，显式`Run(seconds)`每五秒记录真实帧、内存及错误，提前停止标为中断；不读写个人存档。长测4260秒/426000帧/852样本0错误，分配5.706--5.709GB、预留10.268GB；不能代替战斗长期或独立Player验收。07:30交接与具体缺口见[夜班验证](Docs/Validation/2026-11-18-night-development.md)。

工坊取消B切换：按住右键临时进入自由视角，WASD/方向键移动，释放恢复当前工具和选择。预览、移动、复制复用旋转的整数边界/尺寸奇偶锚点。基地建造默认30米，放置、选择及操纵均检查范围。Settings新增General/Preference导航，自动贴合受限连接面的旋转偏好默认关闭、独立持久化。主菜单进入蓝图按4毫秒帧预算批量恢复，不再走逐块计时；原加载统计、取消与连接重建链保留。

ESC及工坊顶部按钮进入真实暂停界面，冻结游戏时间、相机、建造及音频；Resume精确恢复，玩法提供Restart。Siege失败面板新增Restart。双击友舰接管、Tab恢复观察，死亡及返航退出驾驶。共享Debug抽屉改为六项开关，新增友方/敌方/全部筛选及有界历史轨迹/规划路线。友舰使用真实蓝图名称；移除与倒计时重叠的旧Heading。

11 月 18 日版本取消了模块径向伤害与随机断链；11 月 19 日恢复概率断链，当前规则见上方。

受损敌舰仅余无推进/武器的单驾驶舱时，连续12秒移动不足2米按未击杀退场，恢复/移动/重组重置窗口；不赠送击杀或金币，不影响原生纯驾驶舱设计。策略入口`Siege/Battle/SiegeStrandedCockpit.cs`，时限见`SiegeSettings`。

独立可编辑待机战场`BattleAttract.unity`复用8艘内置舰与Bastion基地，额外烟火无伤害；四镜头每7秒切换，65秒重开。演出专用1.2秒错峰索敌及VFX总160/单模板24预算，不改变其他场景默认无限活动效果契约。主菜单Watch live defence进入，ESC返回；冷启动、取消和工坊原稿恢复已纳入130项菜单Play回归。源工具`Assets/Editor/Presentation/Attract/BattleAttractAuthoring.cs`，规格见[资产说明](Docs/Art/BattleAttract.md)。

验证：Play爆炸14项、真实Prefab网格/六面贴合774项、Build→Siege生产流程20项、交互/接管/筛选/轨迹/Restart20项、残舱规则10项、VFX预算10项、AI27项及炮塔32项通过。真实输入观察到RMB/方向键/ESC暂停恢复；菜单/偏好/快捷加载/基地距离及六面真实放置113项Play通过；工坊反馈现禁止镜头抖动以免等距连接点跳格，共享HUD四分辨率在纠正原GPU分配失败后重跑通过（仅边界/字形）。菜单130项及生产战斗HUD25项Play回归通过。菜单/工坊Canvas Expand防止4:3越界和工具遮挡；暂停卡片可编辑，隐藏动作自动收拢。六分辨率24页次离屏布局及通知互斥通过，名牌避让可见工具/战报/击杀提示。工坊右键0.18秒长按、UI防捕获与失焦释放16项跨帧输入通过（回调触发失焦，非OS切窗）；自然受损残舱经12.16模拟秒生产导演退场且无奖励。旧Siege Tab模拟仍不作按键证据。文本输入统一由`TextInputFocus`屏蔽P/F、移动/删除/建造撤销与快捷栏，24项真实跨帧输入、58项Prefab移动/复制/距离检查通过。演出卸载前同步停止生成者，新增提前退出后138项菜单Play通过。Siege场景实例追加摘要/时钟等8个名牌避让区，4比例16页预览与生产HUD31项Play通过。鼠标轴移动改为单手势一次写盘/历史，134项跨帧操控及修复加载检查通过（含右键取消、生命周期回滚、旋转静止点击/90度拖转/撤销与工具切换）。碰撞查询复用可扩容缓冲区，172项原实现结果对照检查通过。双击友舰遇UI/暂停/非战斗中断后清除旧点击；23项生产交互Play通过。未知材质/非法机库入口不会中断整份工坊加载，原快照保留；战斗创建拒绝坏数据并清理。持续状态与证据见[夜班验证](Docs/Validation/2026-11-18-night-development.md)。仅本地提交，不推送。

## Siege战斗时限与界面修正（2026-11-17）

SiegeGameplay移除Construction Controls/SiegeConstructionPanel与Return按钮；底部居中只在部署/整备显示StartNextWave，基地有伤时显示手动维修按钮。顶部中央为准备/战斗/返航倒计时，左侧保留核心、关注友舰与精简经济状态。战斗界面改为半透明蓝灰底、青色友军与珊瑚红威胁；共享威胁箭头使用真实uGUI矢量组件。Debug已升级为六项开关、阵营筛选与轨迹，见本页夜间改进。

`SiegeGameplayStage.Clock`默认战斗180秒、返航90秒；导引舰连续15秒位移不足2米时只记为该舰无法入库。推进不足等导引失败同样只损失该舰；返航到期时将仍未完成的舰结为无法入库。所有舰艇有结果后结算波次收益并进入整备，返航失败不会损伤核心。暂停不计时；每波默认准备5秒，准备结束才开始计算战斗时限。战斗超时仍触发基地失败。已入库的舰艇可自动付费修复，基地仍需手动维修。

新增入口：`Siege/Gameplay/SiegePhaseClock.cs`负责时限与逐舰进展；`Siege/Gameplay/Interface/SiegeCombatHud.cs`负责战斗展示/动作；`Combat/Interface/Threats/CombatThreatIcon.cs`负责无贴图箭头；`Assets/Editor/Siege/Authoring/SiegeGameplayHudAuthoring.cs`原位迁移并保存Prefab/场景，不重建用户共享HUD实例。详情见[专项验证](Docs/Validation/2026-11-17-siege-flow-polish.md)。

## 共享战斗 HUD（2026-11-17）

Main探索、Siege、Random5v5Arena与SiegeGameplay复用真实可编辑的 [SharedCombatHud.prefab](Assets/Art/CombatHud/SharedCombatHud.prefab) 和 [Theme.asset](Assets/Art/CombatHud/Theme.asset)。沿用uGUI，战斗文字升级TMP；主题配置采样预算、配色及动画时长。新版Siege由Stage显式绑定副场景、镜头和关注友舰，默认自主守军/自由观察，双击友舰可临时接管。

敌舰方位逐帧更新，距离文字独立限频，包含后方标识、同向聚合和精英波强调；可见名牌与方位标记互斥。名牌显示舰体/驾驶舱耐久、延迟损伤条、受击闪烁和低血脉冲。共享战报滑入、重排、淡出；击杀庆祝使用有界队列，Siege仅响应规则确认死亡，重复致命伤、越界和卸载不重复计数。F1推进器、F2调试，抽屉互斥，Escape关闭；退出恢复调试偏好。新版左上角显示核心/关注友舰状态，可切换存活友舰；推进器面板读取该舰真实执行器输出和供电效率，区分AI/PILOT/GUIDANCE。

```text
Assets/Scripts/Features/
├─ Combat/Interface/
│  ├─ CombatHud.cs / CombatHudTheme.cs
│  ├─ Nameplates/CombatNameplate.cs
│  ├─ Threats/CombatThreatMarkers.cs / CombatThreatView.cs
│  ├─ Notifications/CombatNotifications.cs / CombatFeedItem.cs
│  └─ Panels/CombatHudPanels.cs
├─ Flight/Presentation/ThrusterInfoPanel.cs
└─ Siege/Gameplay/
   ├─ SiegePhaseClock.cs
   └─ Interface/SiegeCombatHud.cs
Assets/Editor/Combat/
├─ Interface/SharedCombatHudAuthoring.cs / SharedCombatHudPreview.cs
└─ Validation/CombatHudPlayProbe.cs
```

制作菜单 `Tools/HY-Sandbox/Combat/Install shared battle HUD` 幂等迁移四场景（先保存，工具自动备份）；`Rebuild shared HUD prefab` 显式重建自己拥有的Prefab。常规调整直接编辑Prefab/Theme，避免重建覆盖手调。验证入口及范围见[共享HUD验收](Docs/Validation/2026-11-17-shared-combat-hud.md)，主链见[函数图](Docs/RuntimeFlow.md)。

## 独立新版 Siege 验证与非凸地形接触（2026-11-16）

显式 `-siege-gameplay-report <绝对路径>` 启用新版菜单→Additive Siege→真实起降/死亡/返航结算→工坊→菜单检查；`-siege-gameplay-roof` 只在该验证进程内改变示例开口，`-siege-gameplay-soak` 只观察自然战斗，`-siege-gameplay-seconds` 控制30–900秒。不会创建个人示例副本或重写玩家蓝图。普通启动不创建此验证器。

独立前门/顶门脚本伤害闭环各22项通过；首轮自然180秒发现非凸地形ClosestPoint日志。避障与炮弹现复用已有 `ImpactPhysics.ClosestPoint` 的保守包围盒回退，专门接触5项/共用避障10项通过。修复后自然复跑181.83秒完成第一波并进入第二波，错误及ClosestPoint日志均为0；可见窗口截图已检查。首轮隐藏窗口全黑只计流程/物理证据，复跑混合隐藏/可见窗口的帧时不作为完整GPU性能基准。详见[Player验证](Docs/Validation/2026-11-16-siege-gameplay-player.md)。

## 可选前门／顶门机库（2026-11-16）

基地编辑中选择机库，四面尺寸面板的 `DOOR: FRONT / ROOF` 切换开口；前门可随机库绕竖轴旋转。方向随 `BlockData.hangarEntrance` 保存、复制与撤销，旧JSON默认前门。顶门复用2×2×1无重叠墙块规则并按实际数量报价；已展开损伤快照仍不补墙。Prefab真实门板、净空防夹和方向标记随方向刷新；战斗不允许修改设计。

顶门导引以真实刚体垂直升降，偏轴返航先升到门外上空并精确对齐再下降。验证入口 `SiegeHangarPlayChecks.RunRoof()` / `HangarWorkshopChecks.Run()`；当前顶门真实物理36项、工坊58项通过，单个Finch样例出库4.72秒／直返5.96秒／偏轴返13.38秒，不能作为全舰型保证。详见[方向验证](Docs/Validation/2026-11-16-hangar-entrance.md)。

## Siege 起降提速（2026-11-16）

自动导引在实际有电推力上限内补偿刚体线性阻尼，默认速度/加速度改12/12；已对准自身入口走廊的舰艇保留门外精确对齐后直入，省去爬升库顶的往返。真实机库34项通过，完整起降计时分别约7.96/7.32秒（原出库13.3秒、返航21.1秒）；受舰型、供电、姿态与场地影响，不作为所有舰艇保证。五个Siege场景的原8/6预设通过Editor迁移，手动非默认值保留。

出库成功后，常规悬浮接手前一物理步的升力与供电，并以飞控模块的当前高度为初始目标。自主驾驶从舰体当前航层开始检测可用下降路线。用 SiegeHangarPlayChecks.RunAirKing() 可从 Meridian Airworks 大机库部署本机 AirKing 存档，实测交接后三秒内的最低高度与下沉速度。

## 波次间场地清理（2026-11-16）

两套Siege宿主在返航/整备期启动真实 `SiegeCleanupSweep`：扫描线掠过浮岛，HUD显示扫描/保留/已回收数量，合格敌方无驾驶舱残骸过最短保留期后分批转为原回收掉落。友军、建筑、存活驾驶舱、混合归属与外场景保持。扫描不赠送Match金币、不增加击杀、不修复；完成波次收益与既有付费自动修缮仍由原流程结算。开始下一波、撤退和卸载取消扫描。

`EnemyWreckRetirement`共用预算与扫描的归属筛选/幂等退场；活动扫描接管合格残骸的旧延迟删除，结束/取消后释放。掉落与货仓释放显式归属原场景，包裹合并/接收器不跨场景。`SiegeCleanupAuthoring.Install()`备份并制作五场景真实LineRenderer、HUD与有界无碰撞粒子Prefab；Inspector可改速度/批量/材质。验收见[清场验证](Docs/Validation/2026-11-16-siege-cleanup.md)。

## 建造快捷栏（2026-11-16）

Main底部真实uGUI快捷栏提供8槽：1–8或点击选择，Shift+1–8记录当前方块/材质，目录拖入、槽位拖放互换、右键清空。状态行显示所选原始尺寸的耐久、质量和金币；功能模块沿用原材质。目录资产 `Resources/Building/BuildHotbar` 可编辑部件、图标和默认槽位；个人配置以稳定Resources路径/材质ID保存于独立PlayerPrefs，不改蓝图或货仓。`BuildHotbarAuthoring.Install()`制作/重装自己的层级并备份Main；原统计面板上移为快捷栏留空间。

快捷键服从建造、加载、模态及文本焦点边界，战斗拒绝编辑。材质切换清除旧预览并同步下拉框。这里的物品栏是无限建造目录/快捷选择，尚未引入有限资源背包；已有CargoHold运输系统保留。验收见[快捷栏验证](Docs/Validation/2026-11-16-build-hotbar.md)。

## 基础结构材质（2026-11-16）

四种基础结构方块支持金属/木材/石头/玻璃；Main目录底部材质下拉框影响随后放置。配置在 `Resources/Building/BlockMaterials`，每立方米密度、耐久、金币及表面/碎片Prefab均可编辑。玻璃透明且使用薄片粒子；所有材质破坏最多32个活跃效果，无伤害/碰撞小刚体。功能模块保持原配置，快捷栏见上方，有限背包另行评估。

`BlockData.materialId` 贯穿放置、撤销工厂、建筑/舰船恢复、快照、静态预览及Siege造价；空字段旧数据保留原参数，未知或不支持材质拒绝。修复禁用父级延后CSV初始化覆盖实例尺寸的问题；刷新保留健康比例。材质28项、既有建筑18项与CSV Play4项通过；四种表面及真实下拉菜单已截图检查。规格、生成来源与预算见 [资产说明](Docs/Art/StructuralMaterials.md)，验收见 [验证记录](Docs/Validation/2026-11-16-structural-materials.md)。

## 统一爆炸伤害（2026-11-16）

舰船与建筑爆炸统一由 `DestroyManager` / `ExplosionDamageSession` 排队结算：同 PhysicsScene 的模块按中心距离受一次径向伤害，体素地形同步受损；连锁结束后各建筑一次支撑刷新、各舰船一次重组，随后施加原有有限冲量。独立 WorldLab 使用同一会话实现。保留友伤、攻击者归因、碰撞保护与残骸生命周期，不添加遮挡规则。

`Block.explosionDamage` / CSV 爆炸伤害与 `explosionForce` 分开。此次制作迁移按原冲量×0.1且上限100设置50/80/100的初始伤害；这是有意的暂定平衡，原建筑直接使用500/800冲量作为伤害的行为已改变。跨对象14项、建筑18项、物理33项、CSV Play4项和Edit17项通过；实际多舰平衡与视觉仍待验证。详见 [验证记录](Docs/Validation/2026-11-16-shared-explosions.md)。

## Siege 起降推进反馈（2026-11-16）

`SiegeReturnFlight` 继续负责按实际供电能力辅助施力；导引通过 `Thruster.BeginGuidance/SetGuidanceOutput/EndGuidance` 独占推进执行，并将实际分配的水平/升力输出交给现有喷口表现。普通推进器物理回调在导引期间不重复加力、不覆盖喷焰；结束、取消及禁用释放归属和输出。通用推进器在导引时同步转动喷口，未改变普通驾驶输入、供电请求或悬浮算法。

真实机库34项、隔离返航/经济18项、悬浮生命周期16项通过；真实机库验证实际推进输出与喷口强度，不等同人工视觉验收。详见 [验证记录](Docs/Validation/2026-11-16-siege-guidance.md)。后续提速和机库方向改造见上方当前入口；本节记录最初反馈修复的验证范围。

## 工程质量与全项目风险审查

通用Skill主入口已更名为 `engineering-quality`（工程质量与行为验证），旧名称保留兼容；架构、可读性、函数图规范继续适用，新增跨层行为契约和提交前“风险—证据—缺口”复查。项目工具 `Tools/CodeReadability` 保留原名。

[当前风险登记与行为契约](Docs/ProjectRiskRegister.md) 是后续开发的维护入口；[2026-11-15审查覆盖及证据](Docs/Validation/2026-11-15-project-risk-audit.md) 保存本次快照。全量库存涵盖286个运行时脚本、167个Editor脚本及资产/配置，关键调用链采用抽样深入审查；记录23项（2项P1、20项P2、1项P3），不能视为全项目Play验收。高优先级发现是玩家同名新建覆盖和存档路径越界；本轮完成审查记录与Skill治理，未修改这些玩法实现。后续修改需更新受影响风险状态、真实验证证据和剩余缺口。

## 自主舰船直航稳定

`EnemyController.StabilizeDirection` 在普通直航阶段锁定方向，小于 `directionDeadZone`（默认12度）的目标偏移完全忽略，不做持续微调或插值。普通重选航向至少保持 `directionHoldTime`（默认1.5秒）；目标切换、进入绕障、首次接入路线或真实航点拐角可重新锁定，重规划本身不作为转弯理由。偏离锁定直线超过 `straightCourseTolerance`（默认1.5米）才允许恢复航线；近距离战斗横移及紧急避障及时接管。字段名和已有序列化值保留，Inspector说明已更新。

`Flight/FlightControlMath` 是普通C#飞控计算入口：自主参考航速独立限制角速度/加速度，悬浮航向响应继续由 `HoverControlSolver.StepHeading` 调用，状态与执行由各自控制器拥有。导航不直接写刚体，不新增自动组件。蓝图最大推力标定保持；推进执行另由 `Flight/FlightPropulsionControl` 将参考速度拆为朝向与比例油门，巡航沿锁定方向调速，显著转弯与明确制动才改变推力朝向。`ControlUnit.SetPropulsionInput` 允许零油门保留朝向；`UniversalThruster` 按幅值输出并在未对准时阻止侧向残余施力。执行层验证见 [模拟输入与实际喷口回归](Docs/Validation/2026-11-15-propulsion-input.md)，导航方向验证见 [直航锁定回归](Docs/Validation/2026-11-15-straight-course-lock.md)，[上一版平滑试验](Docs/Validation/2026-11-15-flight-course-stability.md)保留为历史记录。

## Random5v5Arena 实战导航追踪

场景已保存 `Arena Navigation Debug`，显式挂载 `SkirmishNavigationDebug` 和五组 `NavigationTrack`。进入 Play 后右侧 RED-1～5 切换红方敌舰，`Focus ship` 平移自由相机至所选舰，`Clear selected trace` 只清空当前舰历史；可切换全部历史线和所选未来路线。Inspector 可关闭面板、场景标签并调整采样间隔、轨迹容量和速度标尺。重新制作竞技场会自动安装；已有场景可用 `Tools/HY-Sandbox/Combat/Install Arena Navigation Debug` 幂等安装，不重建地图或舰队配置。

面板显示目标/距离、接敌与索敌阈值、航程、水平停滞时长、接触次数、期望/实际输入、独立推进方向/油门/反推制动、导航限速、A*失败/重规划及节点耗时、预测威胁/TTC、原始碰撞体数/容量和分别的感知/局部饱和状态。轨迹与 EnemyAvoidanceLab 共用 `Navigation/Diagnostics/Tracking/NavigationFlightTrace`：深蓝0至亮青8m/s，橙色仅表示尚未经过的规划航点；换轮清空、死亡保留最后记录、同驾驶舱重组续接。

实战发现原256碰撞体远场缓存被多模块舰船填满，触发持续 `SensorSaturated` 紧急停车；现按需扩容复用至8192上限，同刚体模块合并为运动包围盒，静态建筑仍独立保留通道。到达效率优先；不把缓存截断当作真实障碍，也不把不完整查询当成净空。验证与剩余边界见 [实战导航追踪记录](Docs/Validation/2026-11-15-arena-navigation-debug.md)。

## 实战偏航自转修复

`HoverFlightController` 在有电悬浮时独立执行航向保持，`HoverControlSolver.StepHeading` 保存接管时航向，按最短角误差平滑抑制偏航角速度。水平状态也持续稳定，避免偏置推进器转矩导致无休止自转；不改变移动路线或自动朝向敌人。Inspector 的 `Heading Response` 默认5（1/秒），0关闭；停供、禁用及重新归属后释放旧航向，下次接管重新捕获。每物理步仅常量计算，不增加物理查询或寻路。Combat Courtyard 两友舰实战65.39秒、每舰66次采样：Spaceship 航向173.292–186.931°，Swordfish 173.095–185.603°，未持续绕圈但仍有小幅摆动；16项悬浮运行回归通过。详见 [验证记录](Docs/Validation/2026-11-12-siege-heading-stability.md)，不等同整波战斗/返航验收。

## 共享 Navigation：Bot 与友敌舰船

导航生产实现统一到 `Assets/Scripts/Features/Navigation`。`EnemyController` 保留战术目标、接近/横移/撤退及推进器输入，降频调用 `VesselNavigation.Evaluate`；`BotFlightMotion` 保留任务飞行、实时移动 Home 点速度与停靠。双方复用同一份 `NavigationAvoidance` 的绕行承诺、无进展脱困、初始重叠分离和近场速度约束，不再保留 EnemyController 的侧向斥力算法。

**效率优先**：持续到达、到达时间和航程优先于零接触；轻微接触作为观测指标，卡死、反复换向、明显绕远及无法恢复仍属于失败。速度走廊按可执行制动距离逐物理步检查，不额外使用1.5倍保守前瞻。路线和局部绕行已有方向记忆，跟随期间不再等待战斗航向保持冷却；转弯仍由连续驾驶/推进器执行。导航不直接写舰船刚体速度。

- **Bot 轻量策略**：球形净空，受阻时8个水平角×3层高度+上下，共26个候选；默认0.1秒采样，高速缩短至物理步。无阻挡时仅直达及重叠查询，Bot间与任务目标过滤保持原语义；保留逐物理步速度走廊约束。规则由 BotNavigationPolicy 固定，不新增 Inspector/CSV 速度配置。
- **舰船完整策略**：整舰 Bounds，远场共享快照、有界三维 A*、上下通道及动态候选航迹保持；局部回退复用水平选向/脱困，不把 Bot 的目标忽略规则带入舰船。静止时也沿期望方向探测，近场危险允许真实推进器制动。A*默认1024节点/2秒重试；预测最多7个候选×12段，远场80米/8身位/10秒航程取最大并封顶160米。参数继续由 EnemyController Inspector 保存，既有 public 字段及GUID保留。
- **查询边界**：NavigationObstacleQuery 使用单位所属 PhysicsScene；球形/整舰查询、自身过滤、缓冲扩容与重叠计算集中管理，状态逐单位独立；查询饱和保守停止。舰船碰撞体缓存定期刷新，逐物理步更新整舰 Bounds。

```text
Assets/Scripts/Features/Navigation/
├── NavigationAvoidanceSettings.cs
├── NavigationAvoidanceResult.cs
├── Local/
│   ├── NavigationAvoidance.cs
│   └── NavigationObstacleQuery.cs
├── Planning/
│   ├── NavigationLocalPath.cs
│   └── NavigationObstacleClimb.cs
├── Prediction/
│   └── NavigationPrediction.cs
├── Vessels/
│   ├── VesselNavigation.cs
│   └── VesselNavigationSettings.cs
└── Diagnostics/
    ├── EnemyNavigationSnapshot.cs
    └── Lab/                         原可编辑试验场、记录器与相机，GUID保持
```

当前验证与具体边界见 [共享导航验证](Docs/Validation/2026-11-15-shared-navigation.md)。源码函数链见 [RuntimeFlow](Docs/RuntimeFlow.md)，真实方法与完整目录树见 [CodeIndex](Docs/CodeIndex.md)。过去各轮验证保留在 Docs/Validation，旧的“遇障只减推、不反推”已由当前近场安全制动替代。

**可编辑试验场：`Assets/Scenes/EnemyAvoidanceLab.unity`**（[操作和记录说明](Docs/Validation/2026-11-11-navigation-test-field.md)）。Play面板包含十二组互斥布局，默认加载已搭建的综合城区（高低楼群、桥梁、顶棚、隔墙和柱阵），并保留原十一组专项测试。综合城区现有地面街区和60米高架环形连桥，十个位置可独立选作起点/终点（含四个高架部署点），仍支持点选地面及高度调整。预挂自由相机：右键+WASD移动，Q/E升降，Shift加速；暂停仍可观察，可切换正交总览。[自由视角与综合场地验证](Docs/Validation/2026-11-11-complex-field-camera.md)。[双层与动态障碍扩建](Docs/Validation/2026-11-11-upper-deck-moving-obstacles.md)：四组橙色运动学障碍分别横移、升降、摆动和穿行，Inspector保留位移/周期/转轴/摆角；暂停冻结，重试及布局切换复位相位，metadata schema 2保存初始几何及运动参数。支持只读浏览内置/Saves/EnemyBlueprints蓝图、手动部署、点选起点/目标、调整高度、暂停、重试和连续测试；真实供电、推进器和碰撞保留，碰撞伤害关闭。轨迹按采样时实际三维速度由深蓝（慢）变浅青（快），默认0–8m/s固定标尺可在Inspector的Trace Fast Speed调整；沿累计路程编码为2048像素纹理保留历史变化。橙色显示规划路线。记录位于`Application.persistentDataPath/NavigationLabRecords/<session>/`，会话`operations.jsonl`覆盖键鼠和面板操作，各`trial-xxxx`保存metadata、蓝图快照、timeline.jsonl与result；默认10Hz记录运动、输入、净空、A*状态/耗时/节点/路线版本。停止Play和中断也结束记录并恢复时间设置。场地与轨迹Renderer通过Editor API保存，Upgrade Navigation Test Field、Build Complex Navigation Field及Add Upper Deck And Moving Obstacles菜单幂等扩展，不覆盖原障碍；记录不写玩家蓝图。

炮塔实墙遮挡修复保留：从炮口验证视线，触发器和炮塔自身不遮挡，同组墙板及其他单位遮挡。每物理步丢弃被遮挡锁定。验证入口`Assets/Editor/Navigation/Validation/NavigationCombatPlayChecks.cs`保留原名称，现检查稳定驾驶、爬升、视线；与`AiUpgradePlayChecks`共同验证。当前结果见 [移除NavMesh验证](Docs/Validation/2026-11-10-local-steering.md)，旧避障/NavMesh记录仅为历史。

**自动随机5v5观战：`Assets/Scenes/Random5v5Arena.unity`**（[循环与验收](Docs/Validation/2026-11-11-random-5v5-arena.md)）。双层避障地图独立副本；十个槽位分别从玩家Saves池及敌方EnemyBlueprints/内置池抽取五艘AI。驾驶舱全灭判胜负，双方连续失去所有可供电完好武器能力3秒则平局；结算5秒后清场重开。名牌遮挡检测降为每舰约8Hz，耐久总量统计复用当前物理步的组件缓存。蓝红阵营材质按源材质共享，避免MaterialPropertyBlock破坏GPU Resident Drawer批处理。地图建筑数量和尺寸不变，通过扩大间距将地面扩至480×480；自由相机、关闭Connector文字、探索模式击杀提示和阵容信息均保留。

## 本轮界面与基地修复

下拉列表统一保存足够行高、文字留白与深色高对比模板；主菜单及现有自有场景/Prefab均应用相同规则。制作入口 **Tools/HY-Sandbox/UI/Repair dropdown templates**。基地编辑隐藏普通建筑据点表单及其遮挡区域，结构建造距离独立可调（默认150m），进入时恢复建造输入，避免沿用载具15m距离与自由镜头状态导致无法放置。菜单72项、基地战48项、普通建筑73项真实Play回归、迁移6项保护检查通过；范围见 [本轮验证](Docs/Validation/2026-11-09-base-menu-fixes.md)。

## 主菜单与工坊导航

Player 首场景为 `Assets/Scenes/MainMenu.unity`。**Exploration** 选择载具，**Siege** 选择基地；**Vehicle / Base Workshop** 打开对应蓝图编辑器。菜单提供静态模块预览、有效性说明、刷新、新建、示例副本、当前工坊恢复与最近设计；不引入科技树或长线养成。界面沿用项目英文标签。示例基地直接出战只读；编辑示例或通过 **CREATE COPY & EXPLORE** 使用示例载具时，明确创建唯一命名的个人副本。

冷启动只载入菜单，不加载Main、不读取默认载具、不生成世界。首次打开工坊/出战才Additive加载Main。Main运行期间为活动场景；回菜单先通过既有模式主链保存货仓、结束战斗和卸载玩法，再停用Main根对象并记录原活动状态。恢复工坊重启同一批对象，保留原稿、镜头和缓存。菜单拥有唯一活动EventSystem；直接运行Main仍使用其原EventSystem。**MENU [ESC]**（或Escape）返回主菜单，直接Main开发入口也可按需加载菜单。

菜单选择其他载具探索时，`MenuPlayDraft` 暂停当前工坊对象及编辑上下文，加载所选载具，再走既有探索副本流程；结束后恢复之前的工坊原稿和Undo/Redo。Siege直接使用菜单指定的基地，不要求先载入载具。显式打开另一工坊设计会替换当前编辑视图，沿用工坊自动保存机制；有实际工坊原稿时才启用 **RESUME CURRENT WORKSHOP**，恢复和同名最近设计不重新加载。加载中 **CANCEL & RETURN** 等待安全载入/清理完成再回菜单。坏文件、缺模块和选中后被删除的文件拒绝打开；货仓保存失败继续保留活动探索会话并显示原因，不能被菜单强行暂停/卸载。

**Settings** 提供全局音量、项目画质档、全屏和鼠标灵敏度，偏好存于 `HY.Menu.*` PlayerPrefs，首次运行沿用项目默认值。按键帮助为说明，首版不提供重绑定。Editor内Quit只提示，Player中退出应用。菜单场景、原生uGUI控件、预览RenderTexture、灯光和相机均为真实可编辑资产；制作入口 **Tools/HY-Sandbox/Main Menu/Install editable menu**，已有菜单布局不会被重建。

源码按功能组织于 `Assets/Scripts/Features/MainMenu`：`MainMenuController` 为导航入口，`Navigation` 管理工坊暂停/临时载具，`Library` 管理只读目录/预览，`Interface` 绑定展示，`Settings` 管理本地偏好。Editor制作/验证入口在 `Assets/Editor/MainMenu`。函数主链见 [RuntimeFlow](Docs/RuntimeFlow.md)，方法导航见 [CodeIndex](Docs/CodeIndex.md)。验证记录见 [主菜单验收](Docs/Validation/2026-11-08-main-menu.md)。

## 地面自由建造与延后裁切

普通建筑和基地直接在y=0地面自由搭建，无需设置地基宽长。逐步放置、移动、旋转、调尺寸、删除和Undo/Redo只保存模块并设置FootprintDirty，不遍历计算占地、不重建整套地基连接。加载时重新计算内存预览，点击 **FINISH / CROP** 或退出/切换建筑编辑时计算并保存最终占地；缺省边距1格，Footprint Padding可配0–8。不移动模块、不删除玩家平台或缺资源模块快照，保留核心和驻军坐标。手动完成不是退出编辑，后续可继续搭建。

蓝图菜单编辑入口不再要求出战校验通过，可打开缺资源、未完成、超尺寸设计继续修复；损坏JSON仍拒绝。出战取消基地尺寸、预算、限高、整格旋转和内部重叠的设计门槛，占地不再上限64m；参考造价仅用于显示/修理。Siege使用有限浮空体素岛，部署时按基地占地保留中心平面与边缘缓冲；对局中不再扩展或重生地面。运行所需合法资源、身份/姿态、基地核心、载具驾驶舱、地面支撑与外部障碍检查保留；机库数量6与净空筛船规则保持。模块自身64m尺寸校验保留；已解除单建筑2048和世界8192模块硬上限，恢复继续分帧并支持取消回滚。自由建造不限库存，Siege部署/维修继续使用局内预算。世界建筑目录和Siege加载时从实际模块计算范围，不依赖未完成草稿的旧占地元数据。

BuildingFoundationData的offsetX/offsetZ缺省0兼容旧坐标。相关入口：BuildPreview.Poll → BuildBlueprintEditor.Write → BuildManager.MarkBuildingFootprintDirty；PrepareBuildingFoundation / CompleteBuildingBlueprint → BuildingFootprint.Calculate → RefreshBuildingFootprint。完成按钮保存于EditorPanel.prefab。

机库建造模式在+Z入口底板显示绿色 **▲ DOOR / FRONT**，随尺寸、旋转更新，战斗自动隐藏。标记为Hangar.prefab真实子节点，制作工具AddEntranceMarker可重复执行；旧透明长板顶棚已由真实模块顶板替换。

## Siege 有限浮空岛

两套Siege宿主与压力场通过真实场景组件 `SiegeIsland` / `ProceduralVoxelWorld` 共用World体素网格、碰撞与爆炸破坏。旧Arena Ground平板已删除。`Assets/Siege/FloatingIsland.asset`控制种子、最小宽192m、边缘32m、基础单元2m、厚度40m、坠落阈值80m；部署时按实际基地占地生成平整中心、起伏边缘和收尖岩底。宽设计保留World每轴128格的数据上限，必要时仅在部署前加粗格子。对局期间不按舰船位置扩张岛屿，不自动重建弹坑。

敌舰从岛内周边空中入口生成；超过坠落/远离界限后释放波次登记，不虚构击杀奖励。友舰复用Scrap退场，基地核心坠落触发失败。岛外没有隐藏接盘地板，正常飞行可越过岛缘，保留200m空中恢复缓冲。

制作：`SiegeIslandAuthoring.Install()`迁移五个场景并保存备份；**Preview Island For Current Base**按当前配置生成预览；**Bake Current Island Preview**保存真实Scene、体素数据与网格资产，重复烘焙保留Mesh GUID。正式SiegeGameplay已烘焙72个分块以便编辑查看；运行部署会按本次基地重新生成。新旧地形均经 `ProceduralVoxelWorld.RebuildFinite` 重建；WorldLab生成/差量存档流程保留。证据见[浮空岛验证](Docs/Validation/2026-11-16-siege-island.md)。

## 可编辑机库与舰队返航

基地编辑器 **PLACE HANGAR** 选择机库构件，沿用原有放置、选中、移动、90°竖轴旋转与复制。深色底板及半透明净空体在编辑时显示，中心标注净宽×净高×净深；选中后面向镜头的面板提供三轴 +/-，尺寸、姿态、身份随基地JSON保存，支持Undo/Redo。机库数量配置为 **6**（`Assets/Resources/Hangars/Limits.asset`），不是尺寸6m；尺寸受64m模块安全范围约束。默认外20×10×24m，净空18×8×22m；长宽高均为4–64的偶数，按钮步长2m。机库预算包含真实金属墙块材料费及外壳面积装配费，不按内部空体积计费，不能作为基地核心或载具模块。

战斗墙、顶、底板由现有2×2×1金属建筑方块组成，默认372块；每块独立耐久、爆炸受损、连接断裂和原设计维修。编辑器保存逻辑机库，复用同一铺块配方显示可调整的预览；建筑恢复前展开为扁平真实方块，战斗隐藏预览碰撞，门继续使用可控伸缩面板。损伤快照保存hangarShellVersion和hangarOwnerId，读档不会补回已毁墙体；旧奇数尺寸内存向上迁移为偶数，保留底部高度并将水平中心对齐整格，个人文件不自动重写。展开、加载与维修均不再受2048建筑/8192世界模块硬限制，仍检查合法数据和建筑数量。两套Siege通过 `ISiegeDeploymentHost` 共用 `SiegeFleet` 和部署卡片：点击空库DEPLOY，仅列各轴尺寸不超过净空的有效舰船蓝图；按蓝图真实包围盒居中，购买时再次验证物理净空与余额。一库一舰，舰船离库仍占用原槽位。整备阶段点击已占用机库的MANAGE可REPLACE或UNDEPLOY；完整舰按购买价退回，战损残值按购买价减现有维修报价且不低于0，更换抵扣该残值。尺寸、余额或外部障碍检查失败保留原舰与金币，取消同时清理原舰及其碎片，战斗/返航阶段禁用这两项操作。新版基地战支持自由观察：按住右键配合WASD/QE，Shift加速、滚轮调速、F回总览；UI/菜单/文字输入阻止捕获，退出恢复工坊镜头。舰船自主作战；预研保留玩家驾驶。

清波进入 **Returning**，击杀/清波/连击收益暂存PendingCoins，锁定购买和下一波。舰船按门外航点、对准、入库自动导航；门口占用阻止关门。持续失去供电/推进能力、机库损毁或返航超时的舰船自动报废。全部存活舰入库或报废后一次性结算，再按原设计和现有金币自动修复基地（含机库）及舰船；余额/净空不足不扣费。导航参数在两个宿主Inspector的Flight Settings中，使用实际有电发动机能力限制辅助刚体推力，仅在达到停泊位置/速度/角度容差后校准最终泊位，不远距离传送舰艇。战斗损伤不回写设计。

预研 `Siege.unity` 使用保留原基地布局的 `BastionHangars.json` 扩建副本，三个默认大机库替换方形占位，原 `BastionBase.json` 保留。正式Base Example共四套：Starter Bastion（前门堡垒）、Skyline Logistics（顶门物流站）、Twin Foundry（前门+顶门双库）与 Meridian Airworks（174×114 航空基地，四机库、24 段间隔外墙、24 座炮塔与 16 个后勤区，中心三层指挥楼，Core 与四库旁新增 6 个维修舱）。每套均使用金属/木材/石头/玻璃、三类Hold和维修/收集Bot，功能模块配置真实供电。`SiegeBaseExamplesAuthoring.Generate()`保存真实JSON与配置引用并备份被替换的官方示例；旧制作入口委托同一工具，个人基地库保留。玩家基地按设计实际机库数量生成，最多6；没有机库仍可用建筑炮塔守波。制作入口：`HangarPrefabAuthoring.Generate()`、`HangarSceneIntegration.Install()`、`SiegeHangarInterfaceAuthoring.Install()`；重新运行较早的基地制作工具后，应再运行机库接线工具恢复新增按钮。

```text
Assets/Scripts/Features/Siege/
├── Hangars/
│   ├── HangarModule.cs          净空、尺寸、支撑与安全门
│   ├── HangarShellBlueprint.cs  真实墙块展开与损伤快照版本
│   ├── HangarRules.cs           数量和姿态校验
│   ├── HangarLimits.cs          可编辑数量配置
│   ├── HangarFacePanel.cs       朝镜头的选中控件
│   ├── HangarResizeButton.cs    尺寸命令
│   └── ResizeHangarAction.cs    尺寸撤销/重做
├── Fleet/
│   ├── SiegeFleet.cs            机库绑定与舰队部署
│   ├── SiegeReturnFlight.cs     起飞和返航固定步导引
│   └── SiegeFlightSettings.cs   航行与报废参数
└── Interface/
    └── ISiegeDeploymentHost.cs  预研与新版共用入口
Assets/Editor/Siege/Hangars/      资产制作、接线和隔离验证
```

模块化墙体验收见 [2026-11-16模块机库](Docs/Validation/2026-11-16-modular-hangars.md)。前次更新验收见 [延后裁切与设计门槛调整](Docs/Validation/2026-11-10-deferred-crop.md)；前次验收见 [自由建造与部署调整](Docs/Validation/2026-11-09-free-building.md)；原机库验收见 [机库验证](Docs/Validation/2026-11-09-hangars.md)。

## Main 玩法合并入口

Main 的 **Simulate**（或 P）继续提供工坊内模式选择：探索按需 Additive 加载 `Assets/Scenes/Exploration.unity`，新阵地战加载 `Assets/Scenes/SiegeGameplay.unity`。玩法期间Main为活动场景，持有建造编辑器、共享管理器和摄像机；菜单启动时输入由菜单EventSystem统一管理，直接Main入口仍使用自身EventSystem。探索地形/建筑与运行舰船归探索副场景。未选择探索时不生成世界。原 `Assets/Scenes/Siege.unity` 保留独立预研入口、逻辑/UI及压力场，不由新模式自动启动。

模式状态由 `GameModeController` 管理，探索通过 `BindHost` 显式绑定 Main 的环境、加载 UI 与 HUD。场景异步加载支持取消，失败完整清理后重试；退出卸载玩法场景。Main 建造原稿停用保留，探索使用副本，返回恢复同一个原稿与镜头；正常返回保存货仓后迁移回原稿，死亡返回清空货仓。不会把战斗损毁覆盖到未保存的作品。SiegeGameplay 已接入基地蓝图部署、真实守波、整备修复与结算，见下节。

## SiegeGameplay 实战基地

本轮补齐结果见[三套示例、自由观察与蓝图修复](Docs/Validation/2026-11-16-siege-completion.md)：三套官方Example各通过23项独立Player闭环；自由视角22项真实输入、8400模块恢复/维修、10份基地支撑审计通过。旧Courtyard与奇数机库个人设计通过兼容/连接修复恢复可部署；55份蓝图审计未发现需隔离的损坏JSON，个人创作保留。以下旧制作记录中的验收缺口以该最新记录为准。

[交错防线基地与验证](Docs/Validation/2026-11-12-siege-combat-courtyard.md)：新增 `Assets/Siege/Construction/CombatCourtyard.json`，143模块、106×90米占地，四角炮台加中央双炮核心舱、分段外墙和错位内墙、两个28×12×30米机库，新增6米高实体承重台，机库中心高度12米。Editor菜单 **Tools/HY-Sandbox/Siege/Generate combat courtyard base** 保存独立蓝图和10种烘焙结构Prefab；已存在资产不覆盖，个人副本 `Combat Courtyard` 可从Base Workshop继续编辑。正常菜单加载SiegeGameplay后部署/完整支撑通过，显式同时工作刺激6/6炮供电效率1.0；首波Finch实战已生成并攻击、核心存活，但两艘个人守军未正常完成出库，未完成清波/返航验收。最终恢复完整基地整备。默认示例与战斗配置保持原样。

## 基地自由建造首版

Main 左下 **BASE EDITOR** 打开独立基地编辑目标（正式Player也可用，复用结构编辑能力）；可新建命名蓝图，或 **COPY STARTER** 复制可修改的示例。在地面自由放置/移动/旋转模块，选中有耐久的模块后 **MARK SELECTED CORE** 指定唯一核心。编辑自动保存到独立 `BaseBlueprints`，普通建筑仍使用 `BuildingBlueprints`；**BACK TO VEHICLE** 返回载具建造，再通过 **Simulate → Siege** 出战。旧建筑目录中已标记核心且非据点的文件会迁移到基地目录；同名冲突自动另取名，字节校验后将原文件归档到 `BlueprintMigrationBackups`，不覆盖任何已有设计。

Siege 使用菜单或 Simulate 面板进入前选定的基地，在副场景可编辑的 `Base Spawn` 自动生成；出生点表示实际占地中心。局内显示状态、开始下一波、修复重建和撤退/返回，并支持机库部署舰船；基地本身不再提供选择、编辑、位置、旋转和部署确认。普通模块参考造价按体积每格5、机库按外壳面积配置计价；出战不限制基地占地、预算与高度。拒绝无效身份/资源、缺核心及外部实体阻挡；自动生成时复用建筑真实连接图，所有分量必须具有连接地面的向下锚点。无发电机/单发电机/无炮塔只给风险提示。核心有显式标记，独立于驾驶舱。基地和敌机归属副场景，副场景保存自己的炮弹与GPU特效池、照明；Main普通刷怪器不参加模式战斗。

**START NEXT WAVE** 启动共享 `SiegeMatch` / `SiegeWaveDirector` 的真实敌机波次；先读取玩家敌方蓝图库，没有有效蓝图时使用配置中的内置敌机。发电、炮塔、毁伤和建筑断裂沿用生产机制。整备阶段 **REPAIR + REBUILD** 显示报价，复用 `SiegeBase` / `BuildingRefit` 按原设计恢复耐久和缺件；净空或余额不足不扣费。设计预算与局内维修金币分开，初始维修金币6000。首版不支持战中扩建或科技养成。核心丢失自动结算；撤退先展示本局波次、击毁和金币，再返回。原设计文件和Main载具原稿不受战斗损毁覆盖。

配置：`Assets/Siege/Construction/Settings.asset`、`Combat.asset`、`StarterHangar.json`（保留原`StarterBase.json`）。制作菜单 **Tools/HY-Sandbox/Siege/Install base construction**；只重建工具拥有的两个UI根，保留其他手工布局和原预研场景。验证入口 **Validate base construction**，报告与边界见 [基地建造验证](Docs/Validation/2026-11-08-base-construction.md)。

```text
Assets/Scripts/Features/
├── Simulation/
│   └── Modes/
│       ├── GameModeController.cs   模式/场景生命周期
│       ├── PlayDraft.cs            建造原稿与运行副本
│       └── Interface/
│           └── GameModePanel.cs    模式选择/重试
└── Siege/
    └── Gameplay/
        └── SiegeGameplayStage.cs   新阵地战制作入口
Assets/Editor/
└── Simulation/
    ├── GameModeAuthoring.cs         可重复制作/迁移入口
    └── GameModePlayChecks.cs        正式模式集成验证
```

菜单 **Tools → HY-Sandbox → Modes → Install gameplay subscenes** 保存真实场景与界面；重复执行不重建已有布局。**Validate Main mode transitions** 验证模式往返；函数图见 [RuntimeFlow](Docs/RuntimeFlow.md)。历史世界组件/性能套件通过 `WorldSessionValidationHost.Attach()` 显式装配隔离世界夹具，保留组件生命周期回归；这类通过不能替代正式副场景集成验证。验证记录见 [玩法合并第一阶段](Docs/Validation/2026-11-08-game-modes.md)。

## 显式组件与友敌 AI 调试

常驻服务、对象池、工业动画调度与调试组件已保存在各场景的 Managers/服务层级，可在编辑模式查看、调整和禁用。载具根 `Resources/BlocksParent/BlocksParent.prefab` 预挂 `EnemyController`，模块、机器人、武器和 UI 的依赖预挂在各自 Prefab；组装时只初始化或启停。

友方自主舰与敌机共用 `EnemyController`。运行前在场景 `DebugManager → AI Debug` 勾选 **Show Friendly AI / Show Enemy AI**，并开启 Scene/Game 的 Gizmos；载具 Prefab/实例的 **Draw Debug** 可单独开启。默认关闭，玩家驾驶、中立、禁用和非模拟状态隐藏。绿色/红色标记区分友敌，青色连线指向目标，紫色/白色表示期望/平滑输入；避障线显示最近一次采样，不额外执行物理查询。

数量由玩法决定的建筑、碎片、地形块、网格、标签、状态图标、临时光束/灯光与工坊服务，由 `Resources/Runtime/Objects.asset` 引用的真实 Prefab 实例化。制作/迁移菜单 **Tools → HY Sandbox → Architecture → Bake Editable Components**，重复执行保留已有参数与引用。动态挂载仅保留体素碎片按实际格子创建碰撞体，以及默认关闭的未知外部刚体诊断探针；编辑器制作和隔离测试另行保留。

已验证：205 个自有 Prefab、44 个 Block、7 个保存场景与模板引用；真实 Play 的 AI/载具、炮弹和特效池、断片生命周期、工坊开关及 WorldLab 碎片物理；37 个 Player 脚本程序集编译。完整 Player 打包/运行、全部场景完整玩法和长时压力未验证。入口、证据与静态检查见 [组件迁移报告](Docs/Validation/2026-11-07-editable-components.md)，函数图与导航已同步。

Unity 6000.6.8f1（URP / VFX Graph 17.6.0，Input System 1.20.1）。本文只维护当前功能、契约与验证入口；历史变更和旧验收完整保存在 [DevelopmentHistory](Docs/DevelopmentHistory.md)。

## 特效与音频资源管理

敌机出生现由 `EnemySpawner.SpawnBlockData` 与 `SiegeWaveDirector.Tick` 共用 `VisualEffectsManager.TryPlayEnemySpawnPortal`：成功生成/注册后播放金橙光环和紫色旋涡，替换 Siege 旧信标；不延迟生成，不影响据点驻军。传送门按舰体横截面缩放、固定在出生点后方，2.4 秒收拢、3 秒回池。集中库 `EnemySpawnPortal` 可替换 Prefab，管理器 Inspector 可调留白及默认 6–80m 直径。原生 Graph、纹理和补缺制作入口见 [资产规格](ArtSource/VfxGraph/EnemySpawnPortal.md)。真实 Siege 夹具两条生成路径及 GPU 回收通过；同时修复一次性回调略早触发后遗漏重调度的问题，验证及范围见 [传送门验证](Docs/Validation/2026-11-07-enemy-spawn-portal.md)。

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

### 2026-11-20 供电、维修机器人与 Siege 返航热点

`PowerTransmissionDevice.Update` 现在由网络脏标记或 0.25 秒低频校正触发 `PowerNetwork.Rebuild`；连接线关闭时直接跳过，开启时按网络版本、Debug 配置、位姿和低频动画刷新。网络收集、拓扑、供电分配、状态提交、范围网格、调试可视化和连接线分别有 `HY.*` ProfilerMarker。`PowerNetwork` 保留全局连通图和独立网络共同补足负载的行为契约，没有改成单向“首个对象”查找。

`RepairBot` 停靠且无目标时禁用自身 `FixedUpdate`，`Durability.Damaged` 事件唤醒同一 Home 的机器人；Home 所属 `ControlUnit` 与 `Power` 按层级版本缓存。`SiegeReturnFlight.BindUnit` 缓存有效推进器、推进器类型和 `Power` 引用，`Tick` 的推进器读取/发布以及 `SiegeIsland.RecoverFleet`、舰队结算步骤均有独立 marker。

本轮已完成静态格式检查和 `dotnet build HY-Sandbox.sln --no-restore`；Unity 6000.6.8f1 Editor 已连接并完成脚本重编译。`PowerPerformanceChecks.Run()` 在 Play Mode 完成 9 组中继/负载微基准与 300 次等价检查，结果见 `Docs/Validation/2026-10-14-power-performance.json`；该数据只证明网络重建子系统，不代表完整场景帧率。相同场景 Play/Profiler 对照（调用次数、P95、峰值及成本转移）尚未重录，不能据此宣称实际帧时间收益。风险登记与源码函数图已同步记录该缺口。

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
- 建筑和载具主体都使用 ControlUnit 和同一重组器。主体的地基、世界存档和碎片退场由世界宿主管理；碎片保留 Block 和耐久，正常退场结算回收，卸载/休眠/退出不产生回收奖励。
- 连接由双侧可用连接面、位置和相反法线决定；视觉对象不决定邻接，建筑不生成载具接缝装饰。刚性整体移动保留缓存；模块姿态、连接面、层/Trigger、根缩放和实际模块附加碰撞体改变时失效。
- 分裂在层级迁移前捕获源速度、角速度和质心，所有碎片使用同一捕获状态；刚体参数由 GroupPhysicsSettings 统一应用。Main 建筑残骸使用 PlayManager 的阻尼及继承限幅，独立 WorldLab 从源刚体获取配置。
- 显式伤害刷按批次处理目标耐久；方块爆炸复用断链、重组与有界冲量。启用 canExplode 的方块撞击致死仍爆炸。碰撞使用法向耗散能量，动态双方分摊。
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

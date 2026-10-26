# HY-Sandbox 开发与审查入口

## 2026-10-26 阵地战修缮报价、核心无伤奖励与Player验证

- 整备阶段两个修缮按钮显示真实报价（`REFIT BASE CORE 127` / `PILOT SHIP INTACT`），报价只在进入整备、点击修缮和状态变化时计算，避免在高模块基地上每帧遍历方块。玩家不用盲猜金币是否够。
- 新增“核心未受损”奖励（默认80×波次），在自动修缮之前判定，避免全额修好被误判为无伤；状态行写明 `Core intact +N`。实测第1波+80、第2波+160。
- 修复 `CargoHoldView` 在蓝图恢复实例上的 `MaterialPropertyBlock` 空引用（修复前同一场Play累计50条），修复后全新Play `consoleErrors=0`。修复 `SiegePlayerChecks` 的旧部署假设，改为按当前蓝图目录报价挑可负担项。
- 独立Player验证包实测 **91项通过/0错误**，覆盖HUD几何与文本、部署、开波、真实死亡结算、接管、核心被毁与重开，并断言精英节奏与连击门控在新构建中生效。详见 [稳定性与成本核对](Docs/Validation/2026-10-26-siege-hud-stability-cost.md)。HUD帧成本仍无可靠对照，未声称FPS变化。

## 2026-10-26 阵地战夜色竞技场外观

- 新增 `SiegeArenaLookAuthoring`（菜单 `HY-Sandbox/Siege/Apply Night Arena Look`）：程序化夜空材质 `Assets/Siege/NightArenaSky.mat`、与天空匹配的雾、冷色环境光与月光，并把竞技场地面扩到900×900使边缘落在雾程之外，去掉旧版生硬的直角地平线。地面仍是单个静态BoxCollider，没有新增渲染批次或碰撞体。
- `VisualEffectsManager` 的雾色/雾密度/环境光参数由硬编码改为可编辑字段，默认值与原常量一致，Main场景外观不变；Siege场景单独覆盖为夜色。截屏与边界见 [夜色竞技场](Docs/Validation/2026-10-26-siege-night-arena-look.md)。夜色下敌舰可读性、Player天空盒与同机位帧时间尚未验证。

## 2026-10-26 阵地战精英波次、连击奖励与受击告警

- 战斗反馈补上正向与负向两端：每5波出现精英波次（默认），使用攻方目录里模块数最多的蓝图，击毁与清波奖励翻倍，横幅与顶部栏给出明确标记；连击窗口内连续击毁按次数发金币（默认5秒、每次+60），同一帧多架同时被毁也按次数累计。
- 核心受击时四边红色告警条短暂增强，核心低于30%保持低频脉动；同一帧请求一次镜头抖动，复用既有 `CameraShake` 协程。告警每帧只写一个 `CanvasGroup.alpha`，连击只在计数变化时计算，精英挑选只在生成时读一次模块数。
- `SiegeMatch.GrantBonus` 接受战斗与整备两个阶段，以便波次最后一架仍可结算连击；部署阶段与失败后拒绝。新增13项规则检查与真实Play探针（精英标记、双倍奖励、连击x2与击杀提示、低核心告警），证据见 [精英与连击验证](Docs/Validation/2026-10-26-siege-elite-combo-alarm.md)。镜头抖动强度与自然推进到第5波的体验仍未验证。

## 2026-10-26 阵地战指挥栏与战斗阶段HUD

- 旧Siege界面把Main的世界准备遮罩、推进器窗口、物理遥测和整段按键说明一起常驻，右侧三分之一被浅色面板覆盖，部署与修缮按钮混排，核心耐久在顶部栏和独立面板重复。本轮先解决可见性与层级，再谈玩法。
- 新增 `SiegeCommandHudAuthoring`（菜单 `HY-Sandbox/Siege/Apply Command HUD`）：幂等重排为单条指挥栏、左侧舰队坞、底部动作行；停用Main遗留遮罩只做 `SetActive(false)`，场景保存重载后序列化引用全部保留。停用的对象随时可在Inspector恢复。
- `SiegeHud` 改为按阶段切换：整备显示部署组与修缮组，战斗收起两者、压缩舰队坞、显示 `WAVE n · RAIDERS k / total` 进度与新 `Ship Status` 面板；波次进入/清空/失败弹一次横幅，击毁敌舰显示短提示；`F2` 切换默认隐藏的按键说明。刷新仍0.25秒节流，战斗每帧只更新两个进度条锚点与透明度。
- 真实Play部署→开波、9项部署与10项选择Play检查、1920×1080截屏、静态检查与导航生成均已执行；`SiegePlayChecks` 在当前HEAD两次停在不同断点（还原HUD脚本后仍失败），因此本轮不以它作为通过证据，详见 [指挥栏验证](Docs/Validation/2026-10-26-siege-command-hud.md)。未重建Player，未做长时性能采样。

## 2026-10-25 奇偶中心规则更正：以驾驶舱实际网格为准

- 前几轮把奇偶映射反了，原自动化结果不能证明该映射正确。现按真实驾驶舱连接点及整数占格边界更正：世界尺寸奇数轴中心为半整数，偶数轴为整数。因此2×1×1绕Z转90°（世界尺寸1×2×1）中心应为`(.5,整数,.5)`。建造容器归零仍保留，以使Inspector与世界坐标一致。
- 全目录探针改为用真实驾驶舱连接点验证放置解的整数边界，独立约束尺寸奇偶期望；25种方块/24朝向/六个旋转方向共78,769条断言通过，原460项回归已同步新规则并通过。Unity编译、319脚本可读性和静态构建通过（4个既有警告）。不自动移动旧存档方块；再次旋转/重新放置时应用新规则。详见 [当前验证](Docs/Validation/2026-10-25-build-rotation-catalog.md)。

## 2026-10-25 Inspector坐标与建造网格统一

- 用户截图的差异来自BlocksParent制作偏移`(-2.5,-0.5,10.5)`：局部`(0.5,3,-10.5)`对应存档世界坐标`(-2,2.5,0)`。`BlueprintLoader.ReplaceParent`现在以零位置/零旋转/单位缩放实例化建造容器，逐块按原世界姿态恢复；不迁移用户JSON，不改Prefab与GUID。
- 全目录探针改为使用真实加载器创建的父对象，增加截图方块真实Factory恢复、局部/世界坐标严格相等和存档数据不变检查；72,577条断言通过，覆盖25种方块和全部旋转轴。见 [父对象与Inspector验证](Docs/Validation/2026-10-25-build-rotation-catalog.md)。

## 2026-10-25 全方块与全旋转轴覆盖

- 通用尺寸奇偶中心规则已覆盖当前建造栏全部25种方块（含Door、推进器、炮塔、货仓等），24种正交初始姿态和X/Y/Z三轴正反旋转。修复相机前/上轴可能重合及上轴初始评分使用错误方向的问题，确保操作坐标架始终正交；拖拽开始时捕获固定世界轴。
- 新增自动发现Prefab的隔离Play探针：72,570条断言通过，包含真实Prefab、六个键位、真实手柄射线和拖拽、四次回位、Undo/Redo、JSON姿态/位置往返；额外覆盖1–4格的64种尺寸组合。此前460项回归通过，Unity编译通过，实际覆盖及边界见 [全方块验证](Docs/Validation/2026-10-25-build-rotation-catalog.md)。人工鼠标体验与独立Player未验证。

## 2026-10-25 建造旋转网格锚点与尺寸奇偶对齐

- 修正此前“世界中心不变”的错误验收规则：锁定中心所在整数格的网格锚点，尺寸奇偶偏移随姿态换轴。当前以驾驶舱网格为准，奇数轴取正半格、偶数轴取整数；2×1×1绕Y轴由`(0,1.5,0.5)`转至`(0.5,1.5,0)`，180°回到原中心。位置由整数半格坐标重建，预览连接点与移动吸附只清理近网格尾差。
- 键盘/拖拽均采用世界轴旋转，姿态规范化，拖拽改为 90°步进，与既有蓝图存档一致；拖拽参考平面固定于开始位置，候选中心和姿态共同参与阻挡、保存与 Undo/Redo。
- Unity 6000.3.11f1 导入/编译通过；隔离空场景 Play Mode 460 项通过，覆盖四种尺寸、虚拟输入、逐坐标严格相等、正负/远处坐标连续100次旋转、阻挡、Undo/Redo、JSON与Awake往返。真实鼠标手感、完整文件仓加载及独立Player未验证，详见 [旋转验证](Docs/Validation/2026-10-25-build-rotation.md)。

## 2026-10-24 阵地战自主体验自测

- 真实 Siege Play 复现并修复两项旧闭环未覆盖的问题：首个空泊位受阻时拒绝所有后续空位部署；刷新或往返 Main 工作台丢失双方蓝图选择。
- `SiegeFleet.Deploy` 按顺序寻找空闲且净空的泊位，全部受阻时保留具体阻挡提示，不提前扣费；`SiegeSession` 持有双方选择，刷新时按同目录唯一文件名恢复，HUD 无通知赋值，删除/失效才回退。
- 原版失败证据、修复后部署9项/选择10项、既有闭环28项/工作台22项及验证范围见 [本轮自测](Docs/Validation/2026-10-24-siege-usability.md)。未改变 BFS、物理或战斗参数，不宣称万模块性能、完整平衡性或全部缺陷已经解决。

## 2026-10-24 阵地战自动整备（进行中）

- Siege 已保存 Main 的真实 UI、建造工具、存档列表和输入管理器，新增整备工作台：B/按钮进入独立蓝图预览，P返回；战斗P开始下一波、Alt释放鼠标、F1推进器面板。工作台编辑仅影响未来购买蓝图，保留已部署舰、建筑、金币及破损状态；取消加载后恢复原驾驶视图。真实Play检查与验证范围见 [Main工作台验证](Docs/Validation/2026-10-24-siege-main-workshop.md)。
- 每波间隔现按报价先自动修缮基地，再按驾驶舰优先顺序自动修复各存活舰；金币不足或泊位受阻时不扣款，HUD 汇报舰队修复数量/花费及首个失败原因。该选项可在 `DefaultSiege` 开关；25项Play回归检查覆盖报价扣款和多次Update幂等。
- 整备HUD现在显示下一波规模与当前攻方蓝图预览（例如 `NEXT 2x RANDOM ENEMY`），帮助玩家在部署/修缮前规划金币；预览只读规则状态，不参与战斗计算。SiegePlayChecks新增预览契约检查，并按当前用户蓝图价格选择可负担舰艇完成经济回归。
- 正常 Siege 已迁移为双方独立的只读蓝图目录选择：守方 `Saves`、攻方 `EnemyBlueprints`；有刷新按钮，攻方可选单一舰型或全目录随机，价格为模块数×Inspector单价。压力场保持固定夹具。Editor目录夹具6项通过；真实Play读到14/14项、HUD选取部署Swordfish、选择DSH_E01后生成对应97模块敌舰，23项既有闭环在固定测试配置副本下通过。场景保存重载与UGUI截屏已验证，尚未做真实鼠标点选或新Player回归。
- 新增默认开启的波次间基地与舰队自动修缮：战斗结束切换整备时各报价一次，沿用大型建筑/舰船蓝图净空检查、原位恢复与成功后扣款；无损不操作，余额不足或净空受阻时保留金币并报告。最新 Siege Play 25 项通过，含精确报价、多舰队报价和重复更新不重复扣款；探针使用明确损伤夹具，不代表自然战斗。

## 2026-10-24 飞行与机器人计算轻量化

- Bot 绕障在单次同步决策内缓存碰撞体过滤结果，复用保持方向及选中方向的净空距离；每次 Evaluate/ConstrainVelocity 都清空过滤缓存，保持货物脱离和目标变化即时生效。导航复用本物理帧目标距离，紧急制动与移动 Home 追踪不降频。
- HoverFlightController 对未激活推进器提前提交零目标，跳过姿态力臂计算；Thruster 的方向归一化移至可视化节流检查之后。保留供电、推力限速和序列化参数。
- 真实 Bot 导航29项、悬浮控制13项通过；静态构建0错误/4既有警告。未做Player/FPS对照，不宣称这些脚本已被证明是主瓶颈或万模块性能已改善。GPU与CPU Jobs/Burst分工见 [架构说明](Docs/SiegeArchitecture.md)，验证过程及缺口见 [轻量化验证](Docs/Validation/2026-10-24-flight-bot-lightening.md)。

## 2026-10-23 阵地战研究与实现（进行中）

- 阵地战增加敌方无驾驶舱碎片软预算：64组/512模块、15秒、每半秒最多退场8组；保留友方维修、驾驶舱、建筑与跨场景对象。Editor12项和既有玩法21项通过；最终发布构建停滞，未完成该版本Player回归，见 [残骸预算](Docs/Validation/2026-10-23-siege-wreck-budget.md)。用户要求当前单元后停止，未将整项目或万模块流畅性标记完成。

- 新增独立可编辑混合压力场：四栋2048模块建筑、六友舰、最多十二敌舰；真实峰10224模块，十分钟完成两波。Editor14项、取消5项、最终Player77项通过。P95约167ms未达流畅目标，近景和后台开发干扰已记录，不能归因BFS，见 [混合压力证据](Docs/Validation/2026-10-23-siege-mixed.md)。当前架构与Jobs/Burst/ECS选择见 [架构说明](Docs/SiegeArchitecture.md)。

- 显式Player真实键鼠驾驶路线：正常388模块基地两舰87项、2048基地六舰91项通过。实际WASD/QE/Space、HUD指针换舰、唯一驾驶权、旧舰恢复AI、死亡自动接管/镜头与设备恢复已验证；未替代人工体验或长期稳定性。首轮主推进器夹具假设失败已记录，未改蓝图绕过，见 [真实输入链](Docs/Validation/2026-10-23-siege-driving.md)。

- 短时能量光束/点光源改为场景有界复用，淡出后回收，场景卸载释放；生命周期16项、玩法21项、Player闭环75项与六舰大型基地自然72项检查通过。16条光束夹具约0.73→0.13ms、16次闪光约0.22→0.04ms，非自然FPS结论。182秒完成2波进入第3波，仍有13条原生D3D11呈现提示，见 [短时表现复用](Docs/Validation/2026-10-23-siege-transient.md)。

- 建筑底座接触复用已验证邻接节点，复杂连接层级实时回退。16缓存/43邻接/18损毁/20建筑/21玩法与75项Player闭环通过；2048同步夹具热底座2.638→0.143ms、完整热16.995→13.851ms，冷刷新未改善。182秒自然71项通过，有10条原生呈现提示，不作为FPS收益证明，见 [底座缓存](Docs/Validation/2026-10-23-siege-foundation.md)。

- 毁伤逐次日志改为默认关闭，保留Inspector开关及Main已有选择。20项日志/21项玩法/75项Player闭环通过；182秒自然完成2波，72项托管检查通过但有12条原生D3D11提示。隔离64次销毁日志开/关7.74/0.36ms，不代表自然FPS提升，见 [日志成本与回归](Docs/Validation/2026-10-23-siege-quiet.md)。

- 独立Development引擎计数能力校准：VFX/渲染/GC有实际样本，物理/Main Thread句柄虽有效仍无样本，停止此归因路线；16项Play和75项最终发布闭环通过。原生日志发现D3D11呈现诊断不进入托管errors数组，已修正旧性能报告边界，见 [能力与日志限制](Docs/Validation/2026-10-23-siege-engine.md)。

- 新增显式Player低频刚体分类/有界慢帧和可选嵌套计时：14项Play、75项闭环、182秒72项托管自然检查通过（后补查有5条原生D3D11呈现诊断）。峰3418模块/224刚体，其中204个无驾驶舱分量；最大182.92ms帧的相邻舰船刷新约4.94ms，不能归因BFS。普查中位4.51ms，计入诊断扰动；下一步观察引擎未覆盖阶段，见 [规模与慢帧证据](Docs/Validation/2026-10-23-siege-survey.md)。

- 修复明确基地任务目标仍受150m自主索敌范围限制：旧夹具9项失败3项，修复后10项通过；12秒真实实飞距目标315.62→257.52m。可见2048基地六舰Player约182秒无错，峰3504模块/249刚体，中位12.79/P95 27.04ms；75项闭环与21项玩法通过，未宣称解决所有长收尾或提升FPS。见 [任务目标修复](Docs/Validation/2026-10-23-siege-priority.md)。
- 新增独立SiegeLargeStress：2048模块建筑蓝图基地+六舰，20项基地/75项Player闭环通过；自然602秒完成2波、第3波中，无错误，峰3424模块/229刚体。前段Hidden截图全黑、后段已确认可见渲染，且第二波长收尾，整段帧分位数不作为可见渲染或持续高压基准。下一步验证明确任务目标仍受150m索敌边界影响，见 [大型基地压力证据](Docs/Validation/2026-10-23-siege-large.md)。
- BFS优化版六舰D3D11 Player回归：75项闭环、72项自然检查通过，约182秒完成2波/第3波中，无错误；峰1758模块/369刚体，中位11.05/P95 16.90ms。不同战况不作加速对照。单扫描源归属复用实验收益小且不一致，已撤回，见 [Player与实验记录](Docs/Validation/2026-10-23-siege-visited-player.md)。
- 建筑支撑BFS先判访问状态，避免每条回边重复查询Unity归属：2048夹具热遍历中位4.38→1.48ms，完整热刷新21.33→19.37ms；顺序窗口非严格ABBA，不代表自然FPS提升。43缓存/18损毁/20基地/21玩法Play回归通过。见 [遍历对照](Docs/Validation/2026-10-23-siege-building-visited-first.md)。
- 支撑新增邻接/底座/遍历/残骸分项计时：单栋上限2048块夹具冷刷新74.60ms、热刷新21.33ms，其中热遍历4.38ms；冷邻接57.59ms。43项缓存、18项损毁、20项基地检查通过。本轮是测量，不是加速；当前单栋2048/世界8192校验上限保持。见 [分项证据](Docs/Validation/2026-10-23-siege-building-stages.md)。
- 独立可编辑SiegeStress场景与配置验证六舰+大型建筑基地：真实D3D11 Player约182秒完成首波、第2波中，无错误；峰值普查1758模块/182刚体/6敌机，中位13.73/P95 22.67/P99 27.22ms。75项压力配置闭环通过；不是万块性能或双舰加速对照。见 [六舰证据](Docs/Validation/2026-10-23-siege-six.md)。
- 维修被阻挡时显示对象类型、模块/物体名称与坐标，提示清场后重试；不绕过净空或提前扣费。20项基地、21项玩法检查通过。缓存版可见D3D11 Player自然180秒完成2波、第三波中、无错，中位10.01/P95 11.07/P99 12.70ms；战况不同，不作为加速对照，见 [缓存验证](Docs/Validation/2026-10-23-siege-building-graph.md)。
- 建筑专用邻接缓存接入支撑遍历：局部删除仅重探附近模块，姿态/连接面/碰撞配置改变重建，复杂碰撞或深层连接变换回退实时几何。43项缓存对照、18项损毁、19项基地、20项闭环与43项WorldLab回归通过。1024块夹具损毁中位40.49→31.37ms、邻接查询5892→288；不是战斗帧率结论。见 [建筑邻接验证](Docs/Validation/2026-10-23-siege-building-graph.md)。
- 独立Windows D3D11验证包已构建，70项Player闭环/HUD边界及真实文字几何检查通过；1156×965可见截图已检查。HUD改为Expand并增加两处章节标题高度，修复窄视口裁切及文字不生成。初轮自然Player完成3波、第4波失守，约262秒无错误；中位11.46/P95 16.48/P99 20.34ms，非Editor对照、非万块验收。见 [Player验证](Docs/Validation/2026-10-23-siege-player.md)。
- 基地已迁移为**大型建筑蓝图**：32×32m、388模块，复用BuildingRuntime加载、虚拟底座支撑、损毁/残骸预算与金币原位修缮。无驾驶舱，独立发电核心被毁或脱离主体判负；飞船单独部署驾驶。19项建筑检查、20项玩法闭环、43项既有世界建筑回归及四塔供电命中实测通过，场景重载引用保留。见 [建筑迁移验证](Docs/Validation/2026-10-23-siege-building.md)。
- 独立可编辑场景 `Assets/Scenes/Siege.unity` 已接入部署、唯一驾驶权、友方AI、无限波次/在场预算、金币维修、核心失败与重开。参数入口 `Assets/Siege/DefaultSiege.asset`，规则和使用方式见 [阵地战](Docs/SiegeWarfare.md)。
- 维修恢复耐久及缺失模块，保留部署身份、复用存活断裂件并清空购买蓝图货物；占用泊位拒绝维修且不扣款。基地为单连通388模块建筑，主体保持锚定，驾驶舰损失自动接管其他舰或观察基地。
- 真实Editor规则19项、Main Play舰队31项/旧敌池6项、最新Siege Play闭环20项通过。初版场景首波自然交火完成；最新版已采集自然交火但多波和大规模性能尚待验证。证据与准确边界见 [本次验证](Docs/Validation/2026-10-23-siege.md)。
- 技术路线保留可编辑GameObject/复合刚体；建筑范围损毁已按最外层连锁批次合并支撑刷新。1024块同夹具摧毁42块，支撑42→1次、同步伤害中位879.25→40.49ms，主体存活数一致；18项损毁、19项基地、20项闭环及43项世界建筑回归通过。建筑套用舰船运行连接缓存反而更慢，已撤回该实验。见 [批处理证据与边界](Docs/Validation/2026-10-23-siege-building-batch.md)。
- 最新90秒自然交火5201观察，首波完成，基地剩368块；中位13.85/P95 30.18/P99 39.39ms。建筑支撑17次累计264.57ms，但建筑范围伤害0次，不能由此确认批处理的自然战斗收益；P95无改善证据。尚未引入ECS/Jobs，继续真实Player与多波验证。
- 历史32块原型：已建立真实Play性能采样器 `SiegePerformanceCapture` 和按需CPU计时 `SimulationWorkMetrics`。最新基地双舰90秒基线：6702次真实帧观察，连接累计19.89ms、包含它的重建90.49ms、完整刷新163.48ms；不可相加。断链链条夹具确认输入顺序改变模块归属，需先修正确性。见 [性能基线](Docs/Validation/2026-10-23-siege-performance.md)。
- 断链对称性已修复：规范碰撞体退出连接层后既不能被连接，也不能向外连接；修缮/底座重建显式恢复原碰撞层（包括多碰撞体及特殊子层）。12项拓扑/底座Play检查、20项Siege闭环及既有Main建造102项回归通过，100块断链夹具两种遍历顺序均形成正确3分量；Main场景检查后保持干净。
- `RuntimeGroupBuilder` 已改为保留驾驶舱主体/最大分量刚体，仅迁移分离模块，先捕获不可变源速度/质心，完成迁移后恢复点速度。`RefreshGroup` 只刷新受影响根，单块更新质量、空根立即清驾驶权；新分量即时登记，外层统一清理。19项重组契约检查及20项闭环通过；同几何ABBA对照有CPU收益，90秒自然窗口P95无改善证据，不声称整体帧率提升。
- 基线保留 `MainUIPanels.cs` 格式差异及Blocks.csv外部占用；不覆盖用户文件。夜班仅本地commit，不推送。
- 战斗BFS读取双侧运行连接，局部姿态/连接面/规范碰撞体/查询层精确快照失效才重新物理探测，整体刚体运动保持有效；显式断开直接清两侧，正常连接不全场查找。公开几何分组仍实时探测；不同ControlUnit不因接触焊接。最新18项连接、19项重组、20项闭环、12项拓扑Play检查通过。同布局1024块同步刷新旧/新中位30.73/13.97ms，尾部断裂31.52/14.86ms；不是千块战斗FPS。

## 2026-10-20 DSH 敌机装甲、机动与弹道炮塔改版

- 重做 `Blueprints/DSH_Enemies` 三份敌机蓝图：驾驶舱外围 8 个 2×2×2 方块封闭四侧四角、3×3 顶盖封闭上方，甲板作底面，驾驶舱六个面全部有装甲；顶盖安装 2 座 `ProjectileTurretDouble` + 2 座 `ProjectileTurretSingle`，并用两座冗余顶盖中继供电；两舷外挂 4/6/4 个 `UniversalThrusterBig` 吊舱。E02 增第四台发电机。三份均为原装 Prefab/CSV，未改生产 C#、AI、PID、Prefab、场景或 CSV。
- 规模：E01 97 块/538 kg/水平 10 000 N/8 炮塔；E02 164 块/899 kg/16 800 N/12 炮塔；E03 106 块/547 kg/10 800 N/12 炮塔。升重比 5.15/4.72/5.07，最坏静态均分功率 240/190/207（激光炮塔下限 150）；驾驶舱六面遮蔽 6/6、单连通、无重叠、无未接合外挂件。旧 README 的 2 000/4 000/2 600 N 是按过时 100 N/推进器模型算的，实际旧值 4 000/8 000/5 200 N；新推力为旧值 2.1–2.5 倍。
- 修正 `FlightProbe.cs` 旧缺陷：玩家靶 `ControlUnit.enabled=false`，`EnemyController` 会跳过禁用对象，旧 “chase” 阶段实际无目标，旧峰值速度主要来自冲量。现保持玩家靶启用、去掉靶碰撞体，新增追击峰值/平均与“已请求但供电不足”统计；旧 2.73/2.80/3.43 m/s 不再用于比较。用同一修正探针重跑旧/新：追击峰值 5.37/5.50/6.84 → 8.71/6.82/9.25 m/s（+62%/+24%/+35%），追击均值 4.41/4.46/5.39 → 7.20/5.29/7.11 m/s；悬停稳态误差 0.102/3.559/0.101 → 0.101/1.632/0.102 m，新倾角 3.33°/1.86°/3.47°。E02 推挤恢复误差 1.616 m（旧 0.797），是 899 kg + 固定高度积分增益的性质，垂直速度仍约 0。
- 新增 `ArmamentProbe.cs` 固定靶 Play 检查：三艘的 8/12/12 座炮塔全部转向锁定并供电，顶盖 4/4 弹道炮塔全部参与，空中弹道峰值 12 发，12 秒靶伤害 2568/3828/3816（激光+弹道，靶舱临时 100000 耐久）。逐 120 帧供电诊断在全部万向/悬浮推进器同时请求时 0 未工作负载；E02 飞行采样中的 5 个瞬态零值是 `EditorApplication.update` 早于电网 Update 的采样顺序，非饿死。
- 工具同步：`design_model.py` 更新 CSV 推力/炮塔功率并新增弹道炮塔；`write_generator.py` 更新生成头与说明；`Validate.cs` 新增驾驶舱六面、弹道炮塔、吊舱检查；`FlightProbe.cs`/`summarize.py` 修正目标、功率口径和追击指标；`README.md` 重写。`power_check.py` 重算 0 饿死/0 未覆盖。Unity 6000.3.11f1 生成/校验/实飞/炮塔探针均通过；未验证驾驶舱装甲在持续命中下的实际吸收、移动目标命中率、密集交火与正式 Player。

## 2026-10-18 蓝图加载连接批处理修正

- 蓝图加载连接重建统一使用 `BatchConnectionBuilder`：每个执行片段一次 `Physics.SyncTransforms()`，原地复用邻接列表并分帧检查连接，避免批处理路径重新调用逐块同步；敌机蓝图生成也复用同一批处理入口。
- `Block.CheckConnection()` 保持原有公开行为；新增的内部同步后入口只供批量流程使用，保留编辑器放置、爆炸和其他单块刷新路径的物理同步语义。`BlockPrefabCache` 缓存蓝图恢复过程中的重复预制体资源加载。
- Unity 6000.3.11f1 导入/编译、既有 ArchitecturePlayProbe 和隔离 Play 拓扑对照通过；100/256块连接重建总耗时与基线基本持平，尚无显著提升证据，因此不推送。dotnet build为0错误/4既有警告；全工程可读性检查仅剩原有 MainUIPanels 格式差异，本轮脚本通过。完整证据、执行片段预算和未覆盖范围见 [审查与复测](OPTIMIZATION_TESTING.md)。

## 2026-10-17 夜间：普通建造敌机刷怪门控收束

- 收束 `EnemySpawner` 的生命周期：普通建造游玩由 `PlayManager.PlayStart()` 设置生成锚点并按 `spawnInterval` 调度，退出时由 `PlayEnd()` 清理计时器与锚点；世界会话保持由据点导演独占敌机生成，避免全局计时器重复刷怪。
- 据点探针改为验证“世界会话不允许全局调度”，不再把组件禁用状态当作生产契约；`OutpostAuthoring` 不再通过编辑器永久关闭场景内 `EnemySpawner`，保留 Main 场景的可编辑生命周期。
- 本轮静态构建 0 错误、3 个既有依赖冲突警告，`git diff --check` 通过；连接 Unity Editor 的 Main 场景进入/退出 Play Mode 20 秒，错误日志为空，场景未保存脏化。完整边界与缺口见[验证记录](Docs/Validation/2026-10-17-enemy-spawner-gate.md)。

## 2026-10-16 建筑接地与区块边界美化

- 建筑平台改为逐蓝图实际占地、旋转与中心对齐，半体素支撑余量外6m接回自然坡面；不再把全库最大正方形平台套到小建筑。修复偶数候选网格导致部分建筑模板永不出现的哈希偏差。
- WorldGeneration加载半径4→6、卸载5→8、建筑候选概率10%→20%，初始169列、详细区宽约416m；近身两圈先准备地形碰撞，再恢复建筑，预算保持可编辑。
- 新增无碰撞体素视觉外环，草土侧壁和阶梯轮廓与近景一致，默认12圈再接8m远景与距离雾。按列缓存/分帧创建、与远景空洞原子切换；加载列排除视觉代理、退出释放网格。高位/低位实际相机渲染已检查，近身加载接缝明显减弱；高处仍可辨远方精度切换，并非无限无缝地形。
- 78个落点、6套模板、四方向的逐格支撑/过渡范围检查通过；外环资源与独立Player结果、性能对照、实际画面及体验评价见[本次验证](Docs/Validation/2026-10-16-terrain-integration.md)。函数图和源码导航同步。

## 2026-10-16 用户调整：敌机与匹配底座组合

- 据点改为敌机配套停机底座，建筑编辑器新增 `Create matching base for selected enemy`：根据所选敌机模块轮廓创建可编辑结构，完整网格/原材质与底座同时预览，组合JSON包含敌机快照及姿态。已有同名作品保留，新建自动加序号。
- Finch、Pike、Manta三套底座已导出至用户 `BuildingBlueprints` 目录，尺寸28×26、28×30、36×22m，替换项目内三套旧外围驻军模板并保留GUID。原28份用户JSON哈希不变；用户既有普通建筑、敌机源与六份工程/配置/布局差异均保留。
- 已加载据点先显示底座上的停机敌机，近处按同姿态启用真实实体，不再从外围空中部署；世界会话抑制EnemySpawner全局刷怪，普通建造游玩仍按配置间隔刷新。击败只在当前出征有效，重访不复活。远处显示没有伤害碰撞，暂不支持未激活目标的远射。
- Main真实隔离Play 61项通过，六套内容连接、三套净空/完整模型通过，实际组合画面已检查。独立D3D11 Development Player组合路线68类通过、errors为空、正常退出：Manta编辑保存/重载与Finch自然战斗、追击/脱离、受损休眠恢复、击败后重访及会话清理。过程中修复敌机锁定隐藏原载具的问题，索敌现在排除禁用对象。源码256脚本/30图、静态编译0错误/4既有警告通过。操作、运行证据及失败记录见[据点制作](Docs/OutpostAuthoring.md)与[本次验证](Docs/Validation/2026-10-16-matching-outpost-bases.md)。

## 2026-10-16 夜间：据点多轮稳定性与整批回收

- 普通D3D11 Player完成10次正式据点出征及隔离返航/死亡重生，75类检查通过。另以`-world-smoke-outpost-salvage-all`完成3次整批回收，78类检查通过，每轮8个原始模块均观察到机器人携带并入仓；退出后无活动或隐藏敌机/掉落实体，28份用户JSON哈希保持。
- 整批验收按自然战斗已有掉落增加真实测试货仓和中继，避免8格夹具先被其他残骸占满；生产容量/寻物规则不变。冻结夹具取消延迟刷新，重跑运动学刚体警告为0。Ready对象数13957/13909/13957，分配内存133.70–134.00MB；此受控路线不代替人工驾驶或GPU性能验收。见 [证据与范围](Docs/Validation/2026-10-16-outpost-endurance.md)。
- 新增移动目标追击路线：正式驻军追踪2m/s移动玩家，离开检测半径后释放目标，返回后重新索敌；67类Player检查通过。

## 2026-10-16 夜间：独立Development Player据点制作

- 增加独立Development制作验证包入口，不改变普通发布包的制作权限。显式`-world-smoke-outpost-editor`使用隔离缓存文件仓，真实快捷键进入/退出、点击模板复制和姿态保存、重载部署数据并检查构建后的Shader/预览清理。
- D3D11 Development Player 30类检查通过、errors为空、正常退出；包含编辑后恢复原载具及世界跨列/退出再准备。此包不作为发布性能比较。见 [独立Player操作证据](Docs/Validation/2026-10-16-outpost-editor-player.md)。

## 2026-10-16 夜间：据点驻军部署预览

- 等大小小方块标记替换为蓝图模块实际尺寸的线框、总体范围、朝向箭头和镜头朝向标签；选中驻军显示模块细节，其他驻军只显示范围。状态区显示尺寸，橙色保守范围重叠警告帮助调整净空，保持可保存。
- 预览只有显示组件，跟随蓝图局部根；切换槽位与退出编辑器释放临时Mesh/Material。Main真实隔离Play的编辑器51项通过，725×488实际画面已检查；不称独立Player面板验证。见 [验证与占用检测边界](Docs/Validation/2026-10-16-outpost-preview.md)。

## 2026-10-16 夜间：据点金币与科技收益

- 正式强据点实测发现金币/科技仓沿用敌方载具的内容不掉落开关，击毁后原有收益被丢弃。CargoHold现在识别所属世界建筑并释放实际存量；普通载具继续遵循阵营配置，正常卸载/退出不产生掉落。
- 新增显式`-world-smoke-outpost-salvage`路线，观察实际据点货仓释放、合并金币包、分类吸附及机器人真实拾取/停靠入仓；退出重新准备时同时检查据点进度清空。完整回收Editor71项通过，D3D11 Player同14741种子两次出征并返航/死亡重生75类通过，28用户JSON未变。复现、运输夹具修正与完整Player证据见 [资源收益验证](Docs/Validation/2026-10-16-outpost-cargo.md)。

## 2026-10-16 夜间：三驻军强据点自然交战覆盖

- 正式seed14741生成Supply Stronghold Outpost，逐名观察Pike、Manta、Finch的真实索敌、悬浮输出、物理移动和武器伤害归属，避免仅用首名驻军代表整个编队。生命周期夹具冻结时同时关闭悬浮控制器，避免向运动学刚体写角速度的验证噪声；生产玩法不变。
- D3D11独立Player的三驻军交战、受损重组、远处休眠、追击恢复、击败后重访和重新准备共51类通过，用户JSON保持不变。探针保留在同一功能文件内，共用既有据点路线；运行时函数图未变。见 [三驻军证据与范围](Docs/Validation/2026-10-16-outpost-stronghold.md)。

## 2026-10-16 夜间：重组保持世界坐标与据点战况排版

- 多种子实测捕获插值刚体重组问题：新组先在Prefab制作坐标创建，再移动Transform，下一渲染帧可能恢复旧插值姿态，将存活驾驶舱移回制作坐标附近。RuntimeGroupBuilder现在直接在目标世界坐标创建新组；据点部署抬升同样写Rigidbody.position，保持物理/显示姿态一致。
- 据点HUD改为读取结构化最近据点快照；显示名称/米数、剩余驻军与炮塔、会话清剿统计。名称按实际字形宽度省略，保留组合字符边界；72像素三行区域、禁用富文本并添加暗色阴影。Main真实世界725×488画面检查了120宽字符名称及清剿状态；本轮隐藏Player截图为黑图，没有作为视觉/GPU性能证据。
- 1080p D3D11 Player seed14722路哨据点40项通过；seed14721双塔据点叠加真实战斗回收、返航/死亡重生66项通过，两轮均errors为空且正常退出，28份用户JSON未变。249脚本/27图、静态编译0错误/4个既有警告、Windows构建0错误/1警告。见 [复现、排版与验收边界](Docs/Validation/2026-10-16-outpost-pose-hud.md)。

## 2026-10-16 夜间：据点外追击恢复

- 独立Player复现追击驻军在原据点外休眠后，玩家靠近它却无法恢复：原代码仍只检查原据点激活半径。现在休眠驻军按最后位置160m内恢复，与180m休眠距离形成缓冲；首次部署也检查部署点距玩家，防止远端反复创建/休眠。
- 修复后同seed14721正式Player35类通过；追加真实驻军自然武器命中与攻击归属后36类通过、errors为空、进程正常退出。249脚本与静态编译通过，Windows构建0错误/1警告，用户28份JSON未变。见 [复现和边界](Docs/Validation/2026-10-16-outpost-pursuit.md)。

## 2026-10-16 夜间：建筑编辑器敌人据点与会话内有限驻军

- 建筑编辑器新增 Enemy outpost：可从三套既有敌机蓝图选择驻军、添加/移除槽位、保存本地部署位置与朝向，并把模块快照嵌入建筑蓝图；Copy template 会生成独立用户文件。普通建筑与据点继续使用原建筑候选概率10%。
- 世界探索运行由 OutpostEncounterDirector 按距离、地形列和部署包围盒逐台生成，最多16台；EnemySpawner保留普通建造游玩的自动计时刷新，并在WorldSession存在时禁止调度。远离区块时保存活敌机模块、耐久、货物和驾驶舱身份，重访恢复原对象身份；驾驶舱死亡才消耗该槽位。击败记录只存在当前探索会话，返航/死亡后重新出发可重新生成，不写永久击败文件。
- 新增 Field Workshop、Abandoned Storehouse、Relay Ruins 三套普通建筑，以及 Road Watch、Relay Station、Supply Stronghold 三套据点，使用结构尺寸Prefab、既有模块和三套敌机快照。六套模板真实恢复后全模块连接、结构模型尺寸/连接网格、炮塔供电和据点标记均通过；预览见 `Temp/OutpostContent`，详细说明见 [据点制作](Docs/OutpostAuthoring.md)。
- BuildingEditorPlayProbe 45项通过；OutpostContentChecks 六套真实Play结构/供电检查通过；D3D11非Development Player seed14721 据点路线通过32项，组合战斗回收/返航/死亡重生路线57项通过。Windows构建0错误/1警告；249源码/27函数图、静态编译0错误/4个既有警告及diff通过，28份用户JSON未变。详见 [验证证据](Docs/Validation/2026-10-16-outpost.md)。

## 2026-10-15 用户调整：法向耗散碰撞伤害

- CollisionDamageResolver按法向相对速度及求解器冲量计算有限能量，读取双方PhysicsMaterial及bounceCombine，扣除弹性反弹保留的能量；动态双方按质心能量份额分摊，轻物体受到更大速度变化。接触点和复合碰撞回调不重复使用整机质量；擦碰的切向速度不计入撞击伤害。
- 25份方块CSV/真实Prefab统一为2m/s起伤、0.1耐久/J、倍率1、单次上限100%、冷却0；严重撞击可一次摧毁，独立撞击不再被0.5秒冷却吞掉。参数仍可在CSV/Inspector标定，保留已有字段和GUID。爆炸的零直接伤害、速度上限和碰撞保护保持。
- Main真实Play的25项隔离PhysX检查通过：双接触模块10kg以10m/s撞墙合计48耐久，20kg翻倍，20m/s为4.125倍；1m/s无伤，切向速度不增加撞伤，完全弹性反弹无永久伤害，恢复系数0.5为75%损伤；10/30kg真实对撞的损伤比例3:1，总预算36耐久。详见[验证与标定边界](Docs/Validation/2026-10-15-realistic-collision.md)。这是刚体能量近似，不包含材料变形、应力传播或经实测标定的车体结构。

## 2026-10-15 夜间：返航失败可见原因与重试提示

- 返航保存失败不再只写Console：Main新增真实可编辑Return Status状态栏，持续显示保存未完成、当前会话保留，以及恢复缺失/损坏存档或处理文件访问后重试的提示；不抢焦点、不挡射线，成功返航自动清除。保留原SaveReturnCargo(bool)调用契约，通过新增out原因重载向UI传递说明。
- Main真实Play四类故障与提示/非阻挡/清除共16项通过；725×488 Game视图实际检查通用长提示无溢出（文字需求72.67、可用84逻辑高度）。D3D11/720p独立Player返航/死亡/再出发29类通过，包含成功返航清除旧提示；Windows构建0错误/1警告，239源码、26函数图、静态构建0错误/4个既有警告通过，28份用户JSON未变。见 [报告及失败尝试](Docs/Validation/2026-10-15-night-return-status.md)。

## 2026-10-15 夜间：三轮组合Player回归

- 同一独立D3D11 Player叠加敌机恢复、输入战斗回收、自然Finch交火和末尾返航/死亡重生路线，固定14721完成3/3轮、58类检查、errors为空且正常退出；28份用户JSON未变。三轮内存采样139509983→139631567→139655055字节，短时趋势不等于无泄漏证明。见 [报告](Docs/Validation/2026-10-15-night-player-combined.md)。各夹具分阶段运行，不冒充一次完整动态驾驶巡航；本单元无新生产代码改动。

## 2026-10-15 夜间：独立Player返航写盘、死亡重生及再出发

- 显式 `-world-smoke-return` 在一般会话检查后切换唯一缓存目录文件仓，经生产Loader加载相连的驾驶舱/货仓；实际UI正常返航写盘和重载，第二次出发保留货物，销毁驾驶舱后通过真实重生按钮清货、重载、第三次出发及再次返航，最后只读恢复原载具。原子备份与文件内容实际检查，未覆盖用户存档。
- 非Development Windows构建0错误/2警告；D3D11/720p/seed14721 Player一轮28类检查通过、errors为空，进程正常退出，28份用户JSON哈希不变。237脚本、静态构建0错误/4个既有警告、工具与diff通过。见 [报告](Docs/Validation/2026-10-15-night-player-return.md)。货物为夹具恢复内容，载具固定用于持久化检查；此前真实战斗回收与本路线是分别验证，尚未合为一次完整动态巡航，不宣称P1整体完成。

## 2026-10-15 夜间：返航存档丢失时保留会话

- 复现命名存档文件不存在时SaveReturnCargo错误返回true，UI会继续清理载具/世界并丢失本次未写回货物。现在将缺失命名存档视为保存失败，保留会话；路径解析/目录创建也进入同一异常边界。未选存档的原行为、几何保留和原子备份不变。
- Main真实世界游玩中隔离文件仓，调用正式MainUIPanels.PlayEnd进行四类故障（丢失、坏JSON、独占锁、目录被文件占用），会话/载具均保持；正常写盘、原文件备份、全部几何及临时文件检查共10项通过。236脚本、Python工具、静态构建0错误/4个既有警告及diff通过；28份用户JSON未变。见 [证据](Docs/Validation/2026-10-15-night-cargo-return-files.md)。本单元为Editor真实Play及同步磁盘验证，尚未跑独立Player返航/重生路线。

## 2026-10-15 夜间：既有敌机自然飞行与单向交火

- 显式 `-world-smoke-enemy-flight CODEX_E01_Finch` 只读取现有蓝图、保持原AI/动力/武器参数；动态敌机在真实Player中自然索敌、供电悬浮、移动并伤害固定高耐久玩家靶。30秒内最大位移22.50m，驾驶舱保持有效；不直接施加伤害、不禁用AI或固定敌机。
- D3D11/720p/seed14721、建筑10%配置下，一轮26类检查通过且errors为空，跨区块与退出重新准备通过。Windows构建0错误/1警告；235脚本可读性、静态构建0错误/4个既有警告、导航与diff通过。首轮夹具未等ControlUnit启动导致空引用，已改为等真实生命周期；见 [报告与限制](Docs/Validation/2026-10-15-night-enemy-flight.md)。这不是双向驾驶、玩家返航写盘或长期压力验收，P1仍未整体完成。

## 2026-10-15 用户调整：相机无碰撞、建筑10%

- Main相机第三人称碰撞层设为None（0），新CameraController默认值同样为0；碰撞解析直接返回期望位置，不再做SphereCast或墙前收回。保留原缩放/跟随/镜头参数，Inspector仍可配置碰撞层。源码调用链没有改变，函数图仍适用；旧墙体回收断言已同步为关闭后的行为。
- 世界生成资产与新配置默认建筑候选概率从35%降为10%；这是每个候选位置概率，不保证任意视野恰好10%覆盖。用户已有建筑蓝图不变，新会话生效。
- Unity Main真实Play检查：collisionDisabled=true、wallDoesNotRetractCamera=true、会话BuildingChance=0.1；Main保存后退出Play。静态构建0错误、4个既有警告，源码格式及diff检查通过。此为配置/同步方法检查，不声称已完成镜头人工手感或10%新种子的大规模Player性能验收。NightDesk按本轮原截止重新启动，已核验黑屏与system+display请求；保留Esc接管。

## 2026-10-15 夜间：损坏敌机文件不再中断蓝图库

- 在真实InitializeEnemyBlueprintPool复现：一份截断JSON抛ArgumentException，中断后续敌机初始化。现在逐文件隔离JSON解析与IO/访问异常，警告后继续有效条目；不修改、删除或覆盖原文件，不吞掉其他编程异常。
- Main隔离Play回归6项通过：截断JSON、被独占锁定文件、空/空模块蓝图被跳过，有效同级文件均保留，初始化完成且再次调用不重复加入，文件内容与SaveManager状态保持。见 [证据](Docs/Validation/2026-10-15-night-enemy-pool.md)。这是同步真实生产入口验证，本单元未另跑Windows Player或目录权限故障。

## 2026-10-15 夜间：Player 输入、炮弹击毁与机器人入仓

- 新增显式 `-world-smoke-combat` 受控Player路线：Input System W键经启用的ControlUnit、真实电网和MainThruster推进动态刚体，松键清空意图；随后固定平台，以现有实体炮塔自然索敌/发射击毁携货敌舱，经过生产残骸转换、机器人拾取、真实返航停靠和供电货仓入库。没有直接调用伤害、手动供电或直接Store伪造结果。
- D3D11/720p/seed14721独立非Development Player完成一轮、28类检查且errors为空，随后跨区块、退出及再次准备通过；233份源码检查与静态编译通过（0错误、4个既有警告）。见 [证据及限制](Docs/Validation/2026-10-15-night-player-combat.md)。此为验证能力与证据补全，未改变正常游戏算法；敌方AI关闭、稳定平台不等于完整双向驾驶交战，尚未覆盖Player返航文件保存/死亡/再次出发。

## 2026-10-15 夜间：敌方蓝图完整恢复

- 修复敌机恢复尺寸后仍沿用 Prefab 碰撞体与质量：CSV Awake 后刷新实例几何、连接点和自动质量，再汇总整机质量。生成前校验所有模块资源及唯一驾驶舱，缺失/空模块、无驾驶舱或多驾驶舱均拒绝整机，避免发布残缺敌机。
- 相同独立D3D11 Player路线修复前碰撞体断言失败，修复后固定14721两轮会话、24类检查通过，errors为空；Windows非Development构建、232份脚本检查和静态编译通过（0错误、4个既有警告）。显式 `-world-smoke-enemies` 不影响正常启动、不写用户蓝图。见 [验证与边界](Docs/Validation/2026-10-15-night-enemy-restoration.md)。本次为跨帧生成恢复验证，尚未覆盖真实驾驶交战和损坏JSON文件解析。

## 2026-10-15 夜间：携货建筑卸载安全释放

- 真实起飞携货后复现了流式卸载错误：先停用建筑父层级，LootDrop.OnDisable 再试图将携货物移出 Cargo socket，触发 Unity 禁止在父对象启停过程中重挂接。建筑现在在单栋卸载、批量清理、世界根停用/失败前先取消运输与释放认领；仓内货物不洒落、不抽取奖励，快照仅保留已入仓物品。
- 完整回收 Play 回归 71 项通过，增加实际起飞/携货、断裂、单栋卸载、根层级停用及恢复不重复入仓；世界边界 23 项通过，无 Unity Error/Exception；D3D11 Windows 重建成功，固定14721两次一般会话烟测通过（不含Player携货/交战）。见 [验证与边界](Docs/Validation/2026-10-15-night-carried-unload.md)。这不等于完整 Player 人工驾驶战斗验收。

## 2026-10-15 夜间：回收全量探针适配场景模拟门槛

- 隔离 Main 后，旧探针禁用了全部 ControlUnit，导致 LootDrop.Update 的真实场景模拟门槛关闭。探针现在使用启用的中立运行单位提供模拟环境，避免读取玩家输入或启动敌机 AI；同时检查启停单位会切换场景模拟状态，未修改生产回收算法。
- Unity 6000.3.11f1 / Main / NightDesk 后台，完整真实 Play 62 项通过、errors 为空：包含本轮建筑 11 项、货币溢出转仓、机器人认领/运输/毁仓释放、隔离临时存档的返航/死亡持久化、残骸清理和移动母体返航。见 [完整报告](Docs/Validation/2026-10-15-night-salvage-full.json)。这解决前一专项报告记录的旧探针散射失败；仍非真实输入驾驶/完整 Player 巡航验收。

## 2026-10-15 夜间：建筑断裂接入回收

- 修复断裂模块被禁用/限时删除、绕过回收而吞掉货仓的缺口。WorldBuildingManager.CreateDebris 在重挂接和残骸预算判定前调用现有 WreckSalvage，保留原阵营与现有掉落概率；主体快照不再包含断裂模块，重复转换不重复奖励。普通流式卸载/恢复与退出不触发掉落。
- 建筑专项真实 Play 11 项通过：实际 Durability 伤害触发断裂、正常/零残骸预算、货物释放、敌方掉落、幂等、玩家无刷币、快照重载及会话清理；修复前在相同断裂货仓断言失败。见 [验证与限制](Docs/Validation/2026-10-15-night-building-salvage.md)。这不是手动驾驶/完整 Player 战斗返航验收。

## 夜间自主开发规划

每日 08:00（Asia/Kuala_Lumpur，UTC+8）收尾的路线、阶段验收和首晚工作顺序见 [夜间开发大纲](Docs/NightDevelopmentPlan.md)。本轮已启动 NightDesk 与当前聊天续跑，截止 2026-10-16 08:00，07:30 收尾；用户明确授权主要功能验证完成后推送 main。后续阶段内容仍为待开发，不代表已实现。

### 2026-10-15 夜间基线：悬浮物理时序回归

完整 ArchitecturePlayProbe 的旧断言只调用控制器 FixedUpdate 就要求实际推力立即改变，与现行推进器独立限速契约不符。HoverControlPlayChecks 现分别驱动控制器和推进器回调，验证无驾驶舱时不转向、推进器失效归属清零、供电后目标提交、缺电/控制器禁用后的单步衰减与最终归零、退出游玩及推进器禁用清零、归属转移保护；生产飞行代码未改。完整探针本轮 completed/passed，73 条检查，0 错误；报告见 [夜间悬浮回归](Docs/Validation/2026-10-15-night-hover.json)。悬浮部分为 Play 内显式驱动生产回调，不代表真实驾驶或自动物理顺序验收。dotnet build 0错误、4个既有警告。

### 2026-10-15 夜间基线：后台性能采样

WorldSessionPerformanceChecks 修复遮罩持焦点后第2帧停滞和无新帧时超时无效的问题：临时后台真实运行，完成/中止/超时均恢复原后台及Profiler设置，启动校验场景与报告名。完整路线在NightDesk下推进1830真实帧，主动退出和停帧超时路径均已验证恢复；Main不脏，用户数据保持。固定种子14721、28栋/1967模块的D3D12 Editor移动P95 27.69ms、最大81.31ms，退出单帧55.64ms；这是工具基线，不是生产优化或D3D11 Player性能结论。见 [报告与限制](Docs/Validation/2026-10-15-night-background-performance.md)。

## 2026-10-15 Windows启动修复与区块深度优化

2026-10-15夜间补充：显式WorldPlayerSmokeChecks新增固定种子、1–10轮会话和P99/尖峰/每轮内存记录；每轮等待世界与原载具恢复再重进，不写玩家数据。D3D11独立非Development包seed14721连续3轮通过、进程退出码0；28栋/1967模块，移动P95约9.46–9.49ms，首次峰值97.87ms、退出约60ms仍存在；无生产算法优化或长期无泄漏结论。cycles0按预期失败退出；28份用户JSON哈希未变。配置、复现和验证边界见 [重复Player报告](Docs/Validation/2026-10-15-night-player-repeat.md)。

- 两份旧Player日志记录D3D12原生资源处理崩溃；同一旧包D3D11对照可响应。Windows发布配置显式使用D3D11，新Release包已实际启动、加载外部建筑蓝图库、进入游玩、跨区块移动200m、退出清空并再次准备，未出现C#错误或原生崩溃。
- 建筑候选概率45%→35%；保留全部有效用户建筑蓝图、自然出生与600m视野。启动用现有可编辑面板显示载具/世界组合进度，载具恢复至95%后完成连接重建才到100%，两者完成开放建造；游玩进入继续使用原有遮罩。
- 区块一格halo快照只在主线程读取；后台任务做同材质贪心面合并，主线程上传16位/必要时32位索引和烘焙碰撞。提交检查请求/数据版本，取消丢弃结果。实时破坏重建加入毫秒预算；建筑休眠按毫秒分批，地形逐列卸载；静止满载减少重复扫描。WorldLab/Bake和显式同步FlushDirty保留。
- 相同种子14721、建筑概率0.45的162块网格对照：98,686→16,162三角形（-83.6%），197,372→32,324顶点；同步单次微基准135.10→83.74ms，包含采样/Mesh上传，不含PhysX，不能视作整局FPS提升。20,267项网格断言、Main及23项边界检查、WorldLab数据12项/破坏碰撞16项通过。实际Player和剩余峰值以[本轮报告](Docs/Validation/2026-10-15-world-player-performance.md)为准。
- 新包：`Builds/WorldPerformanceValidation/HY-Sandbox.exe`，与发布相同的非Development Windows配置；不覆盖旧Build或递增版本。图形驱动/其他显卡、长时间战斗、最大载具与4K性能仍未验收。源码函数图与研究来源见报告及[世界说明](Docs/WorldStreaming.md)。

## 2026-10-15 Main 大世界卡顿与加载遮罩

- 定位并移除数千个Durability独立OnGUI的隐含GUI开销，改为DebugManager装配单个标签绘制器；炮塔空目标遵守搜索间隔。固定种子33栋/2271模块，D3D11 Editor Profiler静止帧中位约30ms降至8ms，末尾100帧主线程均值30.57ms降至8.54ms；测量条件及局限见[性能报告](Docs/Validation/2026-10-15-world-performance.md)，不等同于正式Player性能。
- 建筑资源异步预热，创建/启用按WorldGeneration.asset的默认2ms预算分批；进入先显示不透明进度遮罩并暂停物理，完成/退出/死亡/失败恢复时间。途中缺地形按预测路径真实完成比例显示进度。远景复用网格/高度缓存，同父刚性装饰合并共享网格，保留炮塔及机械运动；大载具末尾连接重建也按预算跨帧。
- 已验证Main 53项、边界23项、WorldLab建筑43项/数据12项及炮塔回归；实际查看遮罩与建筑近景；Unity导入编译、228脚本格式/函数图导航、dotnet build（0错误/4个既有警告）、diff检查通过。正式Player、高分辨率、长时战斗和D3D12稳定性未验收。

## 2026-10-15 Main 大世界与建筑会话

- 本次修正：Main生成池改为读取persistentDataPath/BuildingBlueprints全部有效JSON（当前base1/base2/default_building），平台自动适配尺寸；出生区保留自然地形，按载具范围内最高地面抬升6m；近处加载4列半径/卸载5列半径，加入无碰撞低密度远景和默认270–600m距离雾，退出恢复建造环境。参数保存在WorldGeneration.asset，远景网格按列版本分帧更新。D3D11实际检查远景连续且渐隐；Main检查49项、边界23项回归通过；225份脚本格式/导航、dotnet build（0错误/4个既有警告）、diff检查通过。见[本次报告](Docs/Validation/2026-10-15-world-session-followup.json)与[视距画面](Docs/Validation/2026-10-15-world-session-distance.png)。Player/长时性能与D3D12稳定性仍未验收。

- Main建造时隐藏分帧预热地形和真实Buildings，Play等待Ready后显示世界、启用模拟并安全放置玩家；等待面板可取消。默认约4.1km有限大世界，81列预热，附近按列流式加载/卸载，不分配全图体素数组；生成Unity对象仍在主线程协程执行。
- 同次探索内地形差量、建筑损坏、剩余模块与货仓随区块休眠恢复；活动建筑预算满时延期实例化。玩家缺地形时暂存速度冻结、前方就绪再恢复。退出与死亡丢弃全部世界状态并重新准备，正常返回保留原有货仓转存流程。
- 真实可编辑入口为 `Assets/World/WorldGeneration.asset`、`WorldSession.prefab`、Main加载面板及现有WorldLab建筑目录；菜单可重建缺少资产而保留已有参数和布局。使用与限制见 [WorldStreaming](Docs/WorldStreaming.md)。WorldLab仍保留独立有限地图与快照流程。
- 已验证：Unity导入/编译；Main真实Play专项42项、边界23项通过，包括正负区块、退出/死亡清空、取消、损坏/货仓休眠恢复、81列预算和4000m/s真实物理步保护。WorldLab数据12项、建筑43项回归通过；223份脚本格式/函数图检查、dotnet build（0错误、4个既有警告）、diff检查通过。已观察地形和建筑总览；细节截图期间Editor发生D3D12 GPU等待超时并退出，临时以D3D11重新打开后完成加载面板与建筑近景检查，截图见Docs/Validation。未修改项目图形API设置；D3D12稳定性、正式Player构建及长时大地图性能未验收。

## 2026-10-15 WorldLab 建筑融合

- WorldLab新增真实建筑目录、Outpost模板和初始据点；读取BuildingBlueprints，B进入放置、Q/T旋转、左键确认、Esc取消。预览只复制Mesh且屏蔽采掘。按用户要求，WorldLab实例和预览不生成地基模型、碰撞体或底座方块，保留编辑器底座及尺寸元数据。建筑保留1m模块，地形保留2m体素；支撑、穿插和重叠校验失败拒绝放置。
- ControlUnit共用运行身份与战斗注册，建筑无需驾驶舱，也不加入Main载具清理。供电、激光与实体炮弹、货仓及机器人运行门槛已适配；方块死亡从虚拟地面连接点检查Connector连通，断裂组生成有界残骸；虚拟范围低于默认50%地形支撑时整栋脱锚。
- F5/F9保存/校验体素差量与建筑完整快照，包含尺寸、货仓、耐久和剩余模块；缺失资源或坏数据保留当前世界，兼容旧纯体素差量。动态主体恢复姿态但速度归零，残骸和飞行运输任务不持久化。使用、Inspector参数和边界见 [WorldBuildings](Docs/WorldBuildings.md)，源码图见 [RuntimeFlow](Docs/RuntimeFlow.md)。
- 已验证：Unity6000.3.11f1导入/编译；融合专项43项通过，覆盖生产Input System输入、三实例旋转、尺寸与耐久/货仓JSON恢复、异常保护、供电战斗、独立场景炮弹跨帧飞行、无实体地基、断裂、地形挖空真实帧下落、旧存档与重置。建筑编辑35项、建造目录102项、炮塔28项、供电/货仓/机器人工作请求32项、机器人真实物理导航11项回归通过；结果见 [专项报告](Docs/Validation/2026-10-15-world-buildings.json) 与 [回归报告](Docs/Validation/2026-10-15-world-buildings-regressions.json)。213份自有脚本格式/源码导航检查通过；dotnet build 0错误、4个既有警告。实际画面已检查。正式Player构建、大规模性能、机器人跨帧任务及玩家载具进入WorldLab尚未验收。

## 2026-10-15 开发者建筑蓝图编辑

- 开发者模式（Unity Editor / Development Build）下，`Ctrl+Alt+E` 进入/退出敌人编辑；`Ctrl+Alt+F` 进入/退出建筑编辑，支持左右 Ctrl/Alt。两种蓝图模式可直接互切；相同快捷键再次按下返回玩家存档。普通 F 聚焦不会同时触发。
- 建筑蓝图保存到 `Application.persistentDataPath/BuildingBlueprints`，列表、新建、复制、重命名、删除与文件大小均使用独立目录。底座尺寸以 `foundation.width/length` 跟随 JSON 保存；旧存档缺少该字段仍可读取。建筑不生成/允许放置驾驶舱，编辑时隐藏玩家 Play 入口。
- 底座源为 `Assets/Resources/BuildingEditor/Foundation.prefab`，默认 4×4、每格 1m，薄平面顶部为世界 y=0，每格中心一个向上 Connector。固定网格左下角为 (-2, 0, -2)，右下角尺寸面板支持 1–64 的整数长宽，点击 Apply size 自动保存。尺寸变化保留所有现有方块及坐标；缩小可使边缘方块失去底座支撑，扩大后重新检测连接，不删除方块。尺寸调整会清空当前撤销历史。
- 底座不加入普通块存档或撤销栈，禁止选择、删除、移动和旋转；现有放置预览、连接提示及连接检测可直接使用。薄底座碰撞采用与普通块相同的面内缩间隙；实例恢复使用世界坐标，不继承父 Prefab 的已有偏移。加载替换时先停用旧父对象，再延迟销毁，避免同步恢复误连旧碰撞体。
- 尺寸面板源为 `Assets/Resources/BuildingEditor/EditorPanel.prefab`，与底座、网格材质/贴图均为真实可编辑资产。`Tools > Building Editor > Create missing editable assets` 只生成缺少的资产，保留手动调整；`Run isolated Play Mode validation` 使用随机 Temp 目录回归，不改用户蓝图。函数图见 [开发者建筑蓝图编辑](Docs/RuntimeFlow.md)。
- 验证：Unity 6000.3.11f1 导入/编译通过；建筑专项 35 项及既有 BuildPalette 102 项 Play Mode 检查通过，实际画面已检查。203 份自有脚本格式/函数图、dotnet build（0错误、4个既有警告）、diff 检查通过。完整架构探针完成 39 项加载检查后停在既有悬浮控制时序断言：该探针仍要求控制器回调立即清零实际推力，而当前 HoverThruster.FixedUpdate 才执行输出更新；本任务未改动飞行生产代码，不将此完整探针列为通过。证据见 [建筑验证](Docs/Validation/2026-10-15-building-editor.json)。未覆盖正式 Player 构建、64×64 大底座性能和建筑运行时生成/战斗。 发布隔离工作区基于远程 main，184 份脚本检查通过；临时 csproj 引用本机现有 Library/被忽略第三方源码后静态编译同样 0 错误、4 警告，不提交这些依赖或临时配置。

2026-10-15 WorldLab：新增独立可编辑程序化体素世界实验场（128m范围、固定种子地形与遗迹、三材质硬度），支持采掘/范围爆破/填补及局部分块渲染与碰撞重建，现有炮塔命中入口已接入。F5/F9版本化差量保存/恢复，原子替换并保留上一份，校验失败不改活动世界；不复用玩家蓝图/货仓存档。生成网格通过Editor Bake为真实资产，不改Main。12项数据、16项Play、18项保存/损坏保护与28项炮塔回归通过；软件黑屏覆盖下真实落体180个物理帧后接触体素地面。首次后台更新关闭时冻结的探针失败已保留说明，实验场现临时启用后台运行并限60FPS。新增有限金属支撑/脱落实验：六邻接支撑，默认32格以内孤岛生成最多12个限时刚体碎片；8项数据、7项Play和101真实物理帧下落/再生清理检查通过。最终完整15分钟后台压力测试通过（53,955帧、1,794次爆破、14次再生，种子恢复成功），证据见Docs/Validation；大型结构与地形保持静态；仍为研究原型，无无限流式；详见 [WorldLab使用与边界](Docs/WorldLab.md)。

最近整理：2026-10-11；Unity 6000.3.11f1；URP；Input System；运行时类型保持全局命名空间。

2026-10-14 第三人称镜头环境碰撞：`CameraController` 在第三人称目标点到期望相机位置之间使用可复用的 `SphereCastNonAlloc`，忽略玩家自身碰撞体，在墙体/方块前收回相机并保留碰撞间隙；平滑恢复阶段再次校正，避免相机穿过障碍。碰撞层、半径和间隙均可在 Inspector 调整。墙体收回、自身过滤和障碍移除后恢复已通过隔离Play检查；未完成玩家手动视觉/手感验收。

2026-10-14 供电网络性能优化：`PowerNetwork.Rebuild` 保留每帧功率清零、分配和状态提交，但缓存由中继/发电机位置及连接距离决定的拓扑；拓扑未变化时跳过连接清空、发电机连接和中继两两检测。拓扑变化仍自动重建，供电行为契约不变。新增空间索引候选筛选和连通分量缓存；300轮新旧等价对照、32项玩法回归通过。100中继/500负载静态更新10.73→0.56ms，持续移动11.02→0.99ms。新增炮弹池、可增长查询缓冲及正常图标跳过投影；28项炮塔检查、10项真实飞行、4项复用及5项表现检查通过。完整条件、原始样本和限制见 Docs/Validation/2026-10-14-performance.md。

2026-10-23 CSV 方块数值精简：直接编辑 `Assets/Resources/BlockData/Blocks.csv`（当前 25 行、29 列：25 参数列及 4 个只读身份列，2026-10-25 移除 Bot 速度列）。Editor 导入后自动校验、备份并同步 Prefab；Block/绑定组件 OnValidate 延迟请求校验，绑定 Awake 在其他组件之前应用 Resources 中的 CSV。mass 留空使用 x × y × z，填写正数使用手动质量；不参与质量计算的 density 已从 CSV 移除，组件字段保留兼容。requiresPower 合并应用货仓、收集机器人和维修机器人；BotContainer 的最低/额定功率相等，断电只阻止起飞，已离仓 Bot 继续任务或返航；仅保留基础数值及核心性能；自定义说明、开关、碰撞损伤细节、表现及任务参数由 Prefab Inspector 管理；Bot 导航/停靠已固化为 BotNavigationPolicy，原组件值保留。Bot/Connector 辅助资源不纳入表。CSV/蓝图运行时刷新保留连接状态与实例引用，Bake 拒绝缺失供电依赖。使用与生命周期详见 [CSV说明](Docs/BlockCsv.md)，函数图见 [RuntimeFlow](Docs/RuntimeFlow.md)。 2026-10-23 精简验证：30 列/25 行及保留单元格逐项比对通过；Unity Edit Mode 16 项、Play Mode 4 项检查通过，最终 Bake 无 Prefab 差异；静态构建通过（4 个警告），git diff --check 通过。全仓可读性检查仍被未修改的 MainUIPanels.cs 既有格式问题阻断。[验证记录](Docs/Validation/2026-10-23-block-csv-slim.json)。

2026-10-11 推进器字体状态修正：ThrusterInfoPanel 读取 Power.CommitPowerStatus 确认的 SupplyEfficiency，不再读取物理帧中暂时清零的即时供电值。撤销工作请求立即恢复正常显示，重新申请后等待电网确认缺电；残余推力及关机衰减不参与字体颜色判断。实际施力仍使用即时供电效率。验证见 `Docs/Validation/2026-10-11-thruster-power-colors.json`。

2026-10-11 推进器关机：主/通用/悬浮推进器撤销功率请求后，目标输出为零，实际推力按 `maxThrustChangeRate × fixedDeltaTime` 衰减并继续施加尾段力；喷焰跟随实际推力。悬浮控制器只提交目标，限速统一在 HoverThruster.FixedUpdate 执行，避免直接清零或重复限速。退出游玩、组件禁用或悬浮归属失效仍清零。ThrusterInfoPanel 仅对请求工作的推进器判定缺电，空闲文字为白色。验证记录见 `Docs/Validation/2026-10-11-thruster-shutdown.json`。

2026-10-11 炮塔目录与敌机：单/双管按钮名称移除 `(Clone)`，目录 Prefab 和 Main 已保存条目采用原 TurretButton 的暗色描边、按钮状态颜色与缩放反馈；Awake 按 BlockName 复用场景条目并绑定一次点击，避免重复生成/监听。CODEX 三艘敌机前方保持双激光、后方改为单管与双管实体炮塔；仓库及 EnemyBlueprints 同步迁移，安装文件已有哈希备份。验证见 `Docs/Validation/2026-10-11-turret-palette-enemies.json`；历史飞行数据不代表本次布局实飞结果。

先按功能找入口，再沿函数图进入规则实现。全部自有运行时脚本位于 `Assets/Scripts/Features`，Editor 制作/验证工具按功能分目录。第三方素材和 Package 源码不属于本轮重构。

## 功能入口

| 功能 | 首先查看 | 独立实现 |
| --- | --- | --- |
| 启动 | `Bootstrap/GameManager.Init` | 现有存档选择、界面初始化 |
| 蓝图加载 | `Building/BuildManager.LoadAllBlocks` | `BlueprintLoading/BlueprintLoader.Load` → 校验、恢复、统计、连接与取消 |
| 建造交互 | `Building/BuildManager.Update` | `Interaction/BuildPreview` / `BuildSelection` / `BuildManipulation` |
| 存档编辑 | `Building/SaveManager` / `BuildManager.SaveBlock` | `BlueprintFileStore` 文件规则；`BuildBlueprintEditor` JSON与邻近连接 |
| 进入/退出游玩 | `Simulation/PlayManager.PlayStart` / `PlayEnd` | `RuntimeGroupBuilder` / `RuntimeGroupCleanup` / `PlayBlockSelection` |
| 大世界与建筑 | `World/WorldSessionManager` / `WorldGenerationSettings` | `WorldRegionStreaming` / `WorldBuildingStreaming` / `WorldPlayerStreamingGuard`；`ProceduralVoxelWorld`支持有限WorldLab与Main流式数据 |
| 连接 | `Simulation/Block.CheckConnection` | `BlockConnections` 双边连接几何和状态 |
| 供电 | `Power/PowerTransmissionDevice.Update` | `PowerNetwork.Rebuild`；`PowerConnectionVisuals` 连接线 |
| 飞行 | `Flight/HoverFlightController.FixedUpdate` | `HoverControlSolver` PID与姿态计算；`ThrusterAllocator` 分配求解；各推进器施力 |
| 维修/回收无人机 | `Bots/RepairBot.FixedUpdate` / `CollectionBot.FixedUpdate` | `BotFlightMotion` / `BotObstacleAvoidance` / `RepairTargetSearch` |
| 战斗/损伤 | `Combat/Turret.FixedUpdate` / `Durability.ApplyDamage` | `LaserTurret` / `ProjectileTurret` / `TurretProjectile` / `TurretHitResolver` / `CollisionDamageResolver` |
| 销毁/爆炸 | `Combat/DestroyManager.DestroyGameObject` | `ExplosionPropagation` / `SalvageCleanupSession` |
| 回收与持久化 | `Salvage/LootDrop.Deliver` / `CargoPersistence.SaveReturnCargo` | `CargoHold` 数据与约束；表现位于 Presentation |
| 镜头/特效/UI | `Presentation/CameraController` / `VisualEffectsManager` / `MainUIPanels` | `CameraFraming` / `CameraShake` / `TransientEffects` / `PanelTransitions` |
| 玩家/输入 | `Player/InputManager.Update` / `PlayerController` | 原 Input System及模式门限 |
| 诊断 | `Diagnostics/DebugManager` / `AbnormalImpulseRecorder` | 场景调试、物理遥测与记录 |

上述路径均相对于 `Assets/Scripts/Features`。完整脚本/真实函数签名见 [CodeIndex](Docs/CodeIndex.md)，可点击源码行号；主要运行链见 [RuntimeFlow](Docs/RuntimeFlow.md)。Editor制作工具入口同样在 CodeIndex 中；外围开发脚本见 [ToolIndex](Docs/ToolIndex.md)。

## 架构与行为契约

采用按功能组织、明确主流程、显式依赖和引擎原生可编辑组合。MonoBehaviour 持有 Inspector、引擎回调和功能装配；有实际独立职责的计算、文件规则及会话使用普通 C# 类。沿用当前栈，不新增 DI 容器或 Command 框架。

- 建造输入、选择、拖拽、放置、文件编辑、加载与 UI 统计各有自己的代码入口；UI文本刷新不再承担质量/功率扫描。
- 蓝图加载先读取有效 JSON，再替换原父对象；重复同目标保留进度，新目标/取消使用版本标记隔离旧协程。空蓝图可同步完成，不能留下已完成 Coroutine 句柄。缺失资源（含空路径）移出缓存及存档；配置是一次加载的显式快照。
- 块間隔保留 `max(minimum, initial * 0.5^(elapsed / halfLife))`，零间隔仍逐帧恢复。镜头环绕参考速度与加载衰减独立。
- 连接要求两侧 canConnect、位置与相反法线满足；分组只读取有效连接几何。重组先捕获源刚体，保留线速度，对附加切向速度和角速度限幅；不覆盖原始单位归属。
- 电网仍按轴对齐盒范围；中继对取较小距离，共享发电机合并网络；同分量负载去重均分，不相连电网可叠加；本帧先清零再完整提交状态。退出最后中继时清掉快照。
- 机器人保留 Home 局部停靠偏移、实时世界位置、刚体点速度和制动参数；任务和返航共用机体球形净空查询，稳定保持三维绕障方向，停滞时重选脱困路线；目标扫描、避障建议、实际施力与表现分开。货物只在停靠完成后入库，失效/停用/Home销毁释放认领。
- 射击先施冲量再伤害；爆炸先断链和重组，再施受限冲量；爆炸不额外造成范围伤害，碰撞致死不爆炸。碰撞伤害共享有效接触能量预算，动态双方分摊。
- 临时能量线/灯光/陨石尾迹使用独立同名脚本；镜头偏移在替换/禁用/销毁时移除，面板过渡取消句柄并释放状态。

原场景/Prefab布局、Inspector初值、存档结构和资源路径保留。原85个运行时脚本和27个Editor脚本的 GUID 随文件迁移，未重建；新 Assets 有独立 `.meta`。`RuntimeUnitMember` 仍与 ControlUnit 同文件，保持现有类型身份。原 public 字段作为既有契约保留，新参数使用 SerializeField；不对原字段机械改可见性。

## 可读性与修改流程

共用规范在用户 `game-code-readability` skill；引擎动作使用 `unity-editable-workflow` 和 `unity-cli`。项目 `.editorconfig` 为 Allman、4空格、完整控制流块及 CRLF。每个自有脚本顶部说明真实职责/入口，生命周期复杂时补取消或资源所有权。长参数列表按语义分行；注释解释不变量、坐标系和执行顺序。

文件大小仅为审查提示。BuildManager 的原Inspector与UI/外部API装配、Bot的导航状态与配置映射、CameraController的镜头模式/协程、VisualEffectsManager的具名效果入口仍有较多行；其独立规则已分离。StatusIcon 是单一状态展示组件，Editor生成器保留一项资产制作流程，不为了缩短文件制造转发层。

新增/移动脚本时更新图和导航，且核对序列化与调用者。不要引用旧目录或历史索引。统一执行：

```powershell
dotnet run --project Tools/CodeReadability -- --write .   # 显式整理格式
python Tools/CodeReadability/generate_navigation.py       # 刷新实际行号及符号索引
dotnet run --project Tools/CodeReadability -- --check .    # 本地与CI共用
dotnet run --project Tools/CodeReadability -- --check-tools .
python Tools/CodeReadability/check_tool_sources.py
python Tools/CodeReadability/generate_flow_browser.py
dotnet build HY-Sandbox.sln --no-restore
git diff --check
```

主格式器扫描 Assets/Scripts 与 Assets/Editor；--check-tools另查9份Blueprint C#顶层脚本，保留 UTF-8/BOM/GB18030，比较非大括号 token。当前4份GB18030为 GroupAction、Meteor、ThrusterAllocator和AutoBuildTool。旧 ActionManager/CameraController中已有替换字符的注释按代码重写，不声称恢复原文。

Unity生成的 csproj 必须包含迁移后的路径和全部新脚本；IDE静态构建不能代替 Unity 导入或 Play Mode。工具说明见 [README](Tools/CodeReadability/README.md)。

## 验证与缺口

本轮已完成下列验证（2026-10-11），原始证据汇总见 [验证记录](Docs/Validation/2026-10-11.json)。探针是限定范围的真实执行，不代表完整游戏验收。

| 检查 | 实际结果 |
| --- | --- |
| 自有 Unity 运行时/Editor 脚本 | 157份格式、介绍和函数图符号通过，0差异/0错误 |
| 外围工具 | 9份C# Script语法/格式，6份既有Python及3份导航/检查Python语法、入口和模板；PowerShell解析通过 |
| dotnet build HY-Sandbox.sln --no-restore | 0错误、4警告：3个MSB3277依赖版本冲突及已有Profiler过时API |
| Unity 6000.3.11f1导入/重编译 | 0错误、0警告 |
| ArchitecturePlayProbe | completed/passed，66条检查记录；蓝图、存档规则、悬浮控制及真实表现生命周期 |
| BuildPalettePlayProbe | passed，56条；真实UI、放置及连接 |
| SalvagePlayProbe | passed，31条；运输、认领释放、维修和移动Home返航 |
| CombatHudPlayProbe | passed，30条；战斗界面、遮挡及模式 |
| PhysicsSandboxProbe | ALL PASSED，17条；真实PhysX碰撞/冲量/爆炸保护 |
| 契约/数据 | 609原公开/序列化字段、337public方法、112原源码meta、37份用户持久文件保持 |
| 资源检查 | Main场景全子层及29个Resources Prefab全子层无Missing Script；场景已重新加载且不脏 |
| 函数图浏览器 | 8张图全部渲染、源码链接与缩放通过，无JS错误 |
| Git | diff --check通过；恢复分支及配置备份可用 |

- 已完成供电纯规则回归：实际 PowerNetwork 源码加最小 Unity 数据桩，独立DSU参考模型对照1000次随机重建，检查共享发电机、去重、独立网络叠加、盒边界、无中继清零和每负载仅提交一次。此项不证明Unity生命周期与视觉。
- ArchitecturePlayProbe 在随机 Temp 目录隔离存档，测试真实加载器与文件规则；BuildPalettePlayProbe覆盖真实建造UI/连接；SalvagePlayProbe覆盖运输及损伤释放；CombatHudPlayProbe覆盖模式与界面；PhysicsSandboxProbe使用真实PhysX。结果记录实际执行状态。
- 检查场景/Prefab无Missing Script、GUID/公共契约、用户持久数据未改变。修改前保留恢复分支 `codex/pre-readable-architecture-20261011`；配置skill有独立带哈希备份。
- 未覆盖所有输入设备、长期密集交火、1000块蓝图/大量中继压力、全部移动Home障碍组合和正式Player构建；不将有限探针称为完整玩法验收。

## 外围工具与历史归档

ArtSource/VfxGraph和Blueprints下全部16份自有工具已明确职责/入口。Python读写集中于main，路径按脚本目录解析；DSH生成器模板与输出同步。工具只做静态验收，没有运行生成器、覆盖现有蓝图或以历史GPU截图冒充本轮视觉验收。30份工具附带数据/说明文件哈希未变。

原嵌套HY-Sandbox目录缺Packages/ProjectSettings，属于不完整旧快照；已迁入Archive/LegacyPrototype/HY-Sandbox。811文件原始字节逐项哈希一致，旧Library仅本地保存且不重新提交缓存；源码/资产和迁移清单入库。详见 [Archive说明](Archive/README.md)。

## 持续风险

- 全局Manager单例仍作为既有场景组合入口，跨功能调用并未全部消除；新独立规则避免反向UI依赖。后续只有真实多场景装配需求才引入容器。
- 飞行PID保留现有增益及质量语义；大型敌机积分收敛/稳态高度误差仍需实飞调参。
- 供电范围并集网格、状态图标Canvas和大量透明VFX仍需CPU/GPU压力测量；本轮以职责和资源生命周期改善，不宣称性能提升。
- 同时存在uGUI和诊断IMGUI；尚无正式构建验收记录。

## 性能评估与后续优化顺序

以下结论来自当前源码、场景和 URP 配置的静态审查，不能替代 Unity Profiler、GPU Profiler 或 Play Mode 压力测试。

- 已确认的高风险：`StatusIcon` 和 `CombatHud` 为每个图标/敌方视图执行屏幕转换与遮挡射线；`TurretProjectile` 每个物理帧执行球形范围/扫掠查询，查询缓冲满时扩容重查并复用；炮弹池首次扩容、临时灯光和 VFX 仍可能产生 Instantiate/Destroy 峰值；蓝图逐块实例化和 `RuntimeGroupBuilder.Rebuild` 在大结构变更时会形成 CPU/物理尖峰；`PowerRangeUnionView` 的三维 cell 网格在范围和设备数增长时有内存与网格重建风险。供电拓扑重建已改为 dirty 条件触发，但功率分配和负载范围扫描仍需压力测量。
- 需要 Profiler 确认的渲染项：PC URP 启用 HDR、2x MSAA、2048 阴影贴图、55m 阴影距离、四级级联、软阴影和 SSAO；透明 VFX、状态 Canvas、附加阴影灯可能造成 overdraw、阴影和 Canvas rebuild 开销。`DefaultVolumeProfile` 含测试组件及多个后处理组件，需核对实际 active/override 状态后再判断成本。
- 替代写法：供电采用 topology dirty + 功率状态分离，并用空间分桶减少中继两两检查；UI 按距离/可见性裁剪、缓存遮挡结果并分帧更新；投射物/VFX 使用对象池和固定容量 NonAlloc 查询；蓝图采用分批实例化、加载期延迟连接重建、保存时异步/原子替换；Bot/Turret 将目标扫描、避障和施力分频并复用空间查询；渲染按质量档位下调阴影距离/分辨率、后处理和透明层。
- 分阶段方案：第一阶段建立空场景、100/500块建造、机器人、炮塔和密集特效压力档位，记录 CPU main thread、Physics、GC、Render thread、GPU frame time 和内存；第二阶段优先修复供电重建、UI 遮挡频率和对象创建峰值；第三阶段处理对象池、透明 overdraw、阴影/后处理质量分级；第四阶段验收大蓝图加载、断连重组、长期交火和多机器人场景，并以目标硬件帧时间而非单次平均 FPS 判定。

## 2026-10-11 变更记录

全自有代码按功能组织。拆分建造、蓝图恢复、供电、机器人、物理分组、碰撞/射击/爆炸、镜头和表现职责；清理被替代算法。所有原有运行时及Editor自有脚本统一介绍、格式和导航。新增共用skill并接入Unity/UE开发和Unity审查skill。函数图区分直接、异步、引擎回调与清理；节点关联真实源码。

完整历史需求、旧验证结果、美术集成记录与旧函数索引保存于 [DevelopmentHistory](Docs/DevelopmentHistory.md)，仅作为历史，不代表当前路径和实现。Git历史中的场景资源记录仍可追溯。

## 2026-10-17 多 Bot 防阻塞隔离

- `BotObstacleAvoidance` 的 SphereCast、OverlapSphere、ComputePenetration 和速度走廊查询统一忽略带有 `Bot` 父组件的动态机器人碰撞体，不再依赖子物体当前 Layer；目标、Home 和建造/地形障碍继续按原规则参与导航。这样多台 RepairBot/CollectionBot 交叉作业时不会互相触发脱困方向、减速或锁死。
- `BotNavigationPlayProbe` 新增 RepairBot/CollectionBot 两类 Bot 的 Default/Block 子碰撞体、起点重叠、墙后检测及脱离 Bot 层级后恢复为障碍的检查；Play Mode 共 29 条检查通过（含原有 11 条导航检查），无错误。证据见 [导航回归报告](Docs/Validation/2026-10-17-bot-filtering.json)。
- `dotnet build HY-Sandbox.sln --no-restore` 0 错误、4 个既有警告，Unity 6000.3.11f1 重新编译通过。全仓可读性检查仅报既有未提交的 `MainUIPanels.cs` 格式差异，本次未改该文件。Main 场景测试后仍为干净状态；Prefab 参数与 GUID 保留。
- 验证边界：Bot 过滤使用真实 PhysX 查询和禁用任务脚本的 Bot 夹具，原有 5 个飞行场景驱动生产导航/刚体；未重建视频完整探索会话或运行大量 Bot 压力测试，不代表物理碰撞、供电和全部任务交互的端到端验收。

## 2026-10-11 掉落与机器人阻塞修复

- 掉落文字在 LateUpdate 使用与 StatusIcon 相同的 CombatHud.IsWorldPointOccluded，检查屏幕边界、镜头前方及实体障碍。携带时忽略所属机器人层级，外部障碍仍遮挡。
- 敌方方块原必出金币改为 30% 金币包；白名单稀有部件概率从 20% 降为 8%。先抽部件再抽金币，已有货仓内容仍按原数量释放，不受随机爆率影响。参数在 Resources/Salvage/Settings。
- 掉落初始短时散落，球形路径检测阻止穿墙；显示节点悬浮和旋转，逻辑根保持稳定。认领后停止散落、搬运时还原姿态，保留吸附、认领和入库契约。参数已通过 Editor 保存到 LootDrop Prefab。
- Bot 从自身位置检查三维净空，起点重叠时通过 OverlapSphere/ComputePenetration 计算分离方向；目标阻挡时维持绕障方向，允许临时远离目标以脱困；目标连续可达后恢复直达。移除已替代的 Home 出发射线算法，原 Inspector 字段保留序列化身份。
- 静态检查：158份自有脚本格式/职责/函数图符号通过；dotnet build 0错误、4个既有警告（3个MSB3277及Profiler过时API）；git diff --check通过。Unity 6000.3.11f1导入和重编译通过。
- BotNavigationPlayProbe：5个真实PhysX场景、11条检查通过，覆盖Repair配置绕墙、Collection配置U形围挡（竖直逃逸）、惯性制动、移动Home墙后及起始重叠脱困。探针驱动生产BotFlightMotion，未替代任务流程验收。
- SalvagePlayProbe 最终49条检查通过，覆盖三类掉落文字遮挡/恢复/镜头后隐藏、散落旋转、携带姿态、掉率边界与重复转换、真实维修/运输/移动Home停靠和货物持久化。3个改动Prefab无Missing Script，Main场景不脏且已退出Play，Unity Console 0错误/0警告。完整证据见 [本次验证记录](Docs/Validation/2026-10-11-loot-bots.json)；探针只覆盖列明场景，未验证任意复杂封闭迷宫、所有移动/旋转Home组合或大量Bot性能。

## 2026-10-11 BuildType 与视角锁定

- SelectType 改为 BuildType，Select/Move/Rotate 保持 0/1/2，新增 Build=3。currentBuildType 用 FormerlySerializedAs 接收原 currentSelectType 场景值；方法与调用者统一为 SetBuildType，已有按钮事件入口保持。
- 选择方块资源进入 Build 类型，Update 按类型分派搭建预览或选择/轴操作；Build 不显示移动/旋转轴。切换到选择、移动或旋转会退出搭建并清理旧预览。
- SetBuildMode 处理 lockView 时只取消拖拽、清理预览并暂时隐藏操作轴，保留选中方块、高亮、BuildType 与待搭建资源；重新锁定时恢复对应操作轴；重新锁定后可继续原操作模式。
- Unity 6000.3.11f1重编译通过；BuildPalettePlayProbe 69条检查通过，包含四种类型解锁/重锁保持、目录切换进入Build、退出搭建及Gizmo隐藏；158份格式/函数图检查、git diff --check通过；dotnet build 0错误、4个既有警告。Main场景原有未保存修改保留，不自动保存或重载。探针直接调用lockView的模式回调，未模拟实际Tab按键。

### lockView 保留选择补充

切换视角不再 DeselectBlock；选中状态与高亮持续保留。操作轴仅在锁定视角且类型为 Move/Rotate 时显示，重新锁定即可恢复。Unity重编译及BuildPalettePlayProbe 78条检查通过，包含Select/Move/Rotate三种已选中状态的解锁/重锁、操作轴恢复和高亮保持。158份格式/函数图检查与git diff --check通过，dotnet build 0错误、4个既有警告。场景原有未保存修改保留；实际B键注入未覆盖。

### BuildType 按钮颜色

选择/移动/旋转工具按钮按BuildType显示Inspector配置的活动/非活动色；Build类型高亮当前资源条目。模式颜色同步Button的Normal/Highlighted/Selected状态，按下稍微压暗，避免鼠标悬停或键盘焦点遮掉真实模式色；按钮面板重新启用时从BuildManager同步。切换lockView不改变模式颜色。Unity重编译及BuildPalettePlayProbe 102条检查通过，包含四种类型对应按钮的Normal/Highlighted/Selected颜色、实际焦点渲染颜色、Build资源条目高亮及lockView颜色保持。158份格式/函数图检查、git diff --check和dotnet build通过（4个既有警告）。

## 2026-10-11 按需供电与炮塔分类

- Power.SetWorkRequested记录独立工作意图；空闲/关闭/禁用不占功率。电网按启动负载的额定剩余需求分配，功率不足时按比例分配，独立电网叠加但不超过额定需求。输入推进、悬浮开启、Bot任务/返航、货仓预订/吸附分别提交请求，断电不阻止第一次请求或恢复启动。空闲状态不显示缺电图标。
- Turret基类共用启停、索敌、双轴瞄准和供电门控。LaserTurret保留原Turret脚本GUID及Blocks/Turret资源身份，瞬时命中且不施加冲量；额定功率300。公用序列化字段迁入基类，旧冲量字段移除。
- 指定SpaceKit Single/Double源保存为ProjectileTurretSingle/Double，可编辑Prefab；按实际1/2个炮口每轮发射1/2发真实刚体炮弹。单管/双管额定功率100/150，速度42m/s、寿命2s、射程45m、单发冲量10Ns；支持载具点速度继承、自身过滤、球形扫掠防穿透及单次命中。
- 新建造条目与图标保存为Resources/BuildPaletteEntries UI Prefab，启动时只补缺失条目，遵守COMBAT分类与BuildType高亮。原场景手工条目优先；条目提示从真实方块配置刷新。资产制作菜单不会保存Main场景。制作源与调参说明见 [炮塔规格](ArtSource/Turrets.md)。
- Unity 6000.3.11f1导入/重编译和6个Prefab重载检查通过，无Missing Script/断引用。32条供电/推进/Bot/货仓检查、20条炮塔命中检查、8条真实帧自动双管飞行检查、3条目录点击/高亮/去重检查及6条悬浮控制检查通过。静态模型和真实双弹飞行截图已检查。证据见 [本轮记录](Docs/Validation/2026-10-11-active-power-turrets.json)。
- 166份自有脚本职责/格式/函数图检查通过；dotnet build 0错误、4个既有警告；git diff --check通过。Main原有未保存修改保留。未验证长期密集交火、大量实体弹性能、所有旋转载具组合或正式Player构建；同步扫掠检查与真实帧自动发射检查分别记录。

### 炮管后座与二维图标

ProjectileTurret发射后仅炮管模型沿炮口反向后退0.12m，0.04s快速后退、0.22s平滑恢复；参数和炮管引用在Inspector可编辑。LateUpdate独立于索敌与供电门控，停火继续复位，禁用立即恢复初始位置。单管/双管Prefab已通过Editor只补充引用，原其他参数保留，资产制作源同步。

两个新炮塔的透明二维图重新渲染，并修正Single Sprite导入及真实建造条目Preview引用；Play实例确认两个图标都非空。菜单Refresh turret thumbnails只更新图标，不重新搭建炮塔。28条炮塔/后座检查、10条真实帧飞行/同步后座/停机复位检查通过；Unity编译、166脚本格式/函数图、dotnet build和diff检查通过（4个既有构建警告）。Main及炮弹材质的期间外部改动保留并排除本次提交。见 [验证记录](Docs/Validation/2026-10-11-turret-recoil-icons.json)。

## 2026-10-25 Bot 寻路调优

- 入口：`BotFlightMotion.ApplyMovement` / `BotObstacleAvoidance.Evaluate`；维修与回收共用。直达任务按剩余距离制动（2026-10-25 已改用固化的 `BotNavigationPolicy.Acceleration`），避免高速越过目标再绕回；绕障阶段保留脱离速度。
- 连续两次采样确认直达走廊净空后恢复追踪，无需等满绕障保持时间；退出起点重叠后立即重新选路。停滞检测按当前进展窗口重置，绕远后不再必须突破整段任务历史最近距离才能确认进展。移动 Home 的实时点位/点速度、逐物理帧速度走廊制动及 Bot 过滤保留。
- 导航探针增加耗时、路线长度及空旷接近回归。Unity Play Mode 6 个真实 PhysX 场景、32 项检查通过，均到达且无墙体碰撞。相同条件单次前后对照：绕墙路线 56.09→37.71m，U 形围挡 50.80→33.42m，惯性制动 58.54→29.33m，移动 Home 25.11→23.95m，起点重叠 80.36→35.23m；不作为统计基准或任意迷宫保证。
- 真实任务 Play 回归 71 项通过，覆盖回收运输/货仓持久化、维修和移动 Home 插槽返航。静态构建通过（0 错误、4 个警告）。函数图及源码导航已同步；验证证据见 [调优对照](Docs/Validation/2026-10-25-bot-navigation-tuning.json)。大量 Bot 压力、任意封闭迷宫和全部 Home 运动组合未覆盖。


## 2026-10-25 Bot 无固定速度上限与参数固化

- BotNavigationPolicy 为唯一飞行/避障/停靠策略入口：加速度48m/s²、方向响应5rad/s、16方位三层净空、0.1s基础采样且随速度缩短到物理帧。任务与返航共用 BotFlightMotion.ApplyMovement，按剩余距离制动；转向前优先消除危险纵向速度，移动Home使用实时目标点和刚体点速度。
- 删除固定巡航速度、旧返航倍率和阻力终端速度，设置 Rigidbody.maxLinearVelocity=float.MaxValue；飞行启用 ContinuousDynamic，停靠前恢复 Discrete/运动学。无固定速度上限仍受加速度、目标距离和障碍净空约束，不向物理系统写入 Infinity。
- 导航、避障、返航和停靠参数从 Inspector 序列化及 CSV 中移除；搜索范围/扫描间隔、维修能力、拾取行为、模型插槽和表现继续可编辑。CSV 现为29列（25参数+4身份），所有保留单元格和UTF-8 BOM比对通过。BotNavigationPolicyMigration.Run() 通过 PrefabUtility 重存三个Bot资产，清理旧导航字段并比较剩余序列化数据/GUID；旧Bot基础资产显式保存原有 requiresPower=true 默认值。
- Unity Play：10个真实物理导航场景、51项检查全部通过且无障碍碰撞，含100m/s薄墙、目标在后方、起点重叠、移动Home；200m/2000m飞行峰值86.4/273.6m/s。绕墙/U形/重叠/移动Home耗时约2.23/2.06/2.44/2.14s；带初始30m/s的制动案例约3.24s，慢于上版2.18s，保留制动优先结果。真实任务71项、CSV Edit16项/Play4项通过，构建0错误/4个既有警告。
- 验证证据：[无上限策略记录](Docs/Validation/2026-10-25-bot-unlimited-policy.json)。仍为局部三维导航，不保证任意封闭迷宫；未做大量Bot压力、极端世界坐标和全部Home运动组合验证。

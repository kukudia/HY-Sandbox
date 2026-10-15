# HY-Sandbox 开发与审查入口

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

2026-10-11 CSV 方块数值：直接编辑 `Assets/Resources/BlockData/Blocks.csv`（25 行、80 参数列及4个只读身份列）。Editor 导入后自动校验、备份并同步 Prefab；Block/绑定组件 OnValidate 延迟请求校验，绑定 Awake 在其他组件之前应用 Resources 中的 CSV。mass 留空使用 x × y × z，填写正数使用手动质量；density 仅保留旧数据。requiresPower 合并应用货仓、收集机器人和维修机器人；Bot/Connector 辅助资源、悬浮控制和指定表现参数不纳入表。维修仓已补充 Power（50/100），CSV/蓝图运行时刷新保留连接状态与实例引用，Bake 拒绝缺失供电依赖。18 项 Edit Mode、4 项 Awake 和 8 项跨帧维修回归已通过，详见 `Docs/Validation/2026-10-11-block-csv-regression.json`。使用与生命周期详见 [CSV说明](Docs/BlockCsv.md)，函数图见 [RuntimeFlow](Docs/RuntimeFlow.md)，验证见 `Docs/Validation/2026-10-11-block-csv.json`。

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

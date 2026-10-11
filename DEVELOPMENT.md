# HY-Sandbox 开发与审查入口

最近整理：2026-10-11；Unity 6000.3.11f1；URP；Input System；运行时类型保持全局命名空间。

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
| 战斗/损伤 | `Combat/TurretWeapon.FixedUpdate` / `Durability.ApplyDamage` | `TurretTargetSelector` / `TurretHitscan` / `CollisionDamageResolver` |
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

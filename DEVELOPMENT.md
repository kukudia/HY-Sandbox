# HY-Sandbox 开发入口

Unity 6000.3.11f1。本文只维护当前功能、契约与验证入口；历史变更和旧验收完整保存在 [DevelopmentHistory](Docs/DevelopmentHistory.md)。

## 本轮复用与精简

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

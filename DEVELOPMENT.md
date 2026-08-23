# HY-Sandbox 项目开发文档

> 文档状态：持续维护中  
> 最近核对：2026-08-22  
> Unity 编辑器：6000.3.11f1（`ProjectSettings/ProjectVersion.txt`）  
> 当前分支：`main`

本文档以仓库当前 Git 跟踪的 `Assets/`、`Packages/`、`ProjectSettings/` 和历史日志为依据。历史日志中的功能描述可能来自旧版本，若没有当前脚本、场景或运行时证据，不视为已实现。

## 1. 项目定位

HY-Sandbox 是一个 Unity 三维模块化建造与飞行沙盒。核心循环是：创建或加载蓝图存档，在网格中放置和编辑模块，使用连接点组成可控制单元，进入游玩模式后由驾驶舱和推进器驱动载具，并通过敌方蓝图、陨石、炮塔、维修机器人等系统扩展玩法。

## 2. 技术栈与目录

| 范围 | 当前内容 |
| --- | --- |
| 引擎 | Unity 6000.3.11f1 |
| 渲染 | Universal Render Pipeline 17.3.0 |
| 输入 | Input System 1.19.0；代码同时直接读取 `Keyboard.current` / `Mouse.current` |
| UI | uGUI 2.0.0，部分系统仍使用 IMGUI（例如悬浮控制器诊断面板） |
| 数据 | `Application.persistentDataPath/Saves` 与 `EnemyBlueprints` 下的 JSON |
| 资源 | `Resources/Blocks` 下按资源路径加载模块 Prefab |
| 编辑器工具 | Windows 自动构建工具、Profiler 捕获分析工具 |

主要目录：

- `Assets/Scripts/Block`：方块尺寸、质量、碰撞、连接点和邻居关系。
- `Assets/Scripts/Manager`：建造、保存、游戏状态、游玩、破坏、陨石和视觉效果管理。
- `Assets/Scripts/Actions`：添加、移动、旋转、删除及组合操作的撤销/重做。
- `Assets/Scripts/InObject`：驾驶舱、控制单元、机架、炮塔、敌人、维修机器人等模块行为。
- `Assets/Scripts/Thrusters`：悬浮、主推进、全向推进、推力分配和推力视觉效果。
- `Assets/Scripts/UI`：建造/游玩面板、按钮、存档列表、动作计数和全局文字样式。
- `Assets/Resources/Blocks`：可动态发现的方块 Prefab；`MainUIButtons` 会从这里注册方块按钮。
- `Assets/Scenes/Main.unity`：当前 Git 跟踪的主场景。

## 3. 运行时架构

### 3.1 启动与模式切换

`GameManager` 在启动时初始化全局管理器和方块父节点。`MainUIPanels` 控制创建、删除、建造、游玩、死亡等面板的淡入淡出。`BuildManager` 负责建造上下文；`PlayManager` 负责进入/退出游玩模式及控制单元分组。`CameraController` 提供第一人称和自由飞行两种视角，`B` 切换视角锁定状态，`Tab` 切换相机模式。

`InputManager` 统一处理 `B`/`Tab`/`F`、敌方蓝图开发者快捷键和模式光标状态：建造锁定模式显示并限制鼠标，建造自由飞行模式隐藏并锁定鼠标，游玩模式默认隐藏并锁定鼠标，按住 Alt 时显示并限制鼠标。`PlayerCockpitHealthUI` 在 `PlayPanel` 左下角显示玩家驾驶舱耐久度，血条颜色按比例从红色过渡到绿色。

进入游玩模式前，`PlayManager.CanStartPlay` 会检查当前构造体是否存在有效驾驶舱；成功后由 `ControlUnit` 刷新子模块并取得运行时所有权。退出游玩模式时恢复建造状态并清理运行时分组。

### 3.2 建造、连接与碰撞

`BuildManager` 的主要流程：

1. 从 `Resources/Blocks` 选择资源并创建 Ghost 预览。
2. 射线检测方块或连接点，按网格和目标旋转计算吸附位置。
3. `Block.IsBlockedGhost` 和 `BuildManager.IsBlocked` 检查重叠，阻挡时禁止放置。
4. `CreateBlock` 实例化 Prefab，应用默认值并写入当前存档。
5. 选中方块后支持键盘移动、15 度旋转、移动/旋转轴拖拽、复制和删除。

`Block` 根据尺寸在六个方向生成连接点，通过位置和相反法线匹配相邻模块，维护 `neighbors`。连接成功后创建连接视觉对象；`DisConnectAllConnectors` 用于删除、拆分和游玩结束清理。

### 3.3 存档与加载

`SaveManager` 管理两个命名空间：玩家存档 `Saves` 与敌方蓝图 `EnemyBlueprints`，支持创建、读取、删除、重命名、复制和文件名校验。列表中的 Duplicate 按钮会在当前命名空间生成不覆盖已有文件的 `Copy` 名称，并刷新列表；复制不会切换当前加载目标。`BlockData` 保存资源路径、尺寸、位置、旋转、质量等重建所需数据。

`BuildManager.LoadAllBlocks` 使用协程逐个实例化，支持加载进度、取消旧加载、无法加载数据清理和可选的相机环绕。存档身份依赖文件中的模块数据，不应把运行时 `GetInstanceID()` 当作跨会话稳定 ID。

### 3.4 游玩与推进器

`ControlUnit` 聚合驾驶舱、主推进器和悬浮推进器，读取玩家输入并把世界方向传给推进系统。敌方 `EnemyController` 默认每 0.5 秒采样一次目标/避障方向，并以响应速度渐进更新模拟输入；敌方不再直接修改 Rigidbody 的旋转或力，转向和位移统一交给 `MainThruster`/`UniversalThruster` 根据 `MovementInput` 施加。`HoverFlightController` 使用高度、重力补偿和姿态 PID 逻辑分配悬浮推力。

`ThrusterAllocator.Solve` 将力与力矩目标组成 6 维约束，通过带阻尼的最小二乘和上下界迭代求解各推进器输出。`ThrusterVisualEffect` 使用粒子、光源和渐变颜色表达推力比例，并替代旧的 Line Renderer 视觉。

### 3.5 UI、敌人和效果

`MainUIButtons` 负责按钮事件、操作模式和动态方块按钮；`SaveUIPanel` 负责玩家/敌方蓝图列表；`ActionCounterUI` 显示撤销/重做数量；`GlobalTextStyler` 统一 Chakra Petch 字体与轻量阴影样式，避免小按钮文字因粗描边显得拥挤。`EnemySpawner`、`EnemyController`、`MeteorShower`、`TurretWeapon` 和 `RepairBot` 组成战斗与环境事件链。`DestroyManager` 在驾驶舱摧毁时按爆炸半径和概率断开同一运行时单元内的 Block，再重新分组并施加爆炸冲量（当前不造成伤害）；`VisualEffectsManager`、`StylizedBeamEffect` 和 `StylizedRingEffect` 负责放置、删除、移动、碰撞、爆炸和陨石冲击反馈。

## 4. 已确认实现的功能

- 主场景和 URP 项目配置可被 Unity 项目识别。
- 玩家存档与敌方蓝图存档的创建、加载、删除、重命名接口已存在。
- `Resources/Blocks` 提供多种尺寸方块、驾驶舱、推进器、悬浮控制器、机架、炮塔、维修机器人等 Prefab。
- 建造模式支持方块选择、高亮、Ghost 预览、网格吸附、碰撞阻挡、键盘移动、旋转、轴拖拽、复制和删除。
- 动作系统支持添加、移动、旋转、删除、组合操作的 Undo/Redo，并由 UI 显示计数。
- 方块连接点、邻居关系、连接/断开和连接器 Gizmos 已实现。
- 游玩模式会按控制单元聚合模块，并检查驾驶舱有效性。
- 主推进、全向推进、悬浮控制、推力分配及推力粒子/光效代码已存在。
- 敌人、陨石、炮塔、维修机器人和模块耐久相关脚本已纳入工程。
- 编辑器包含 Windows 构建入口和 Profiler 捕获分析入口。

## 5. 待改进与风险

优先级含义：P0 阻断主流程，P1 影响核心体验或数据安全，P2 可维护性/性能，P3 体验增强。

| 优先级 | 问题或改进方向 | 建议 |
| --- | --- | --- |
| P1 | 缺少自动化测试和稳定的 Play Mode 回归清单 | 为存档往返、连接匹配、阻挡、Undo/Redo、推进器和模式切换增加 EditMode/PlayMode 测试。 |
| P1 | 输入逻辑分散在直接读取设备与 Input Actions 两种方式 | 统一 Input Action，集中处理设备缺失、重绑定和 UI 输入焦点。 |
| P1 | 存档写入仍需关注中断、损坏和版本升级 | 使用临时文件+替换、JSON schema/version 字段、损坏存档备份和迁移策略。 |
| P1 | 运行时大量依赖单例和 Inspector 引用 | 增加启动依赖检查、缺失引用的用户提示，并逐步将纯逻辑从 MonoBehaviour 解耦。 |
| P2 | `Resources.Load` 和逐个 Instantiate 在大蓝图下会造成加载峰值 | 建立 Prefab 注册表或 Addressables，批量/异步加载并复用对象。 |
| P2 | 方块连接和阻挡检查依赖 Physics 查询 | 建立网格占用索引，旋转/删除时增量更新，减少全场景扫描。 |
| P2 | 推进器求解器缺少运行时可观测性 | 输出目标力矩、残差、饱和推进器数量和求解耗时，便于调参和性能分析。 |
| P2 | EnemyController 的 AI 输入平滑参数仍需 Play Mode 调校 | 根据敌我距离、载具规模和目标帧率调节 `movementUpdateInterval` 与 `movementResponseRate`。 |
| P2 | Block 爆炸当前仅实现范围断开、分组、物理冲量和粒子反馈 | 后续可在爆炸中心加入按距离衰减的伤害，并补充断开概率、冲量和半径的 Play Mode 调参记录。 |
| P2 | UI 同时存在 uGUI 与 IMGUI | 将诊断面板迁移到统一 UI 系统，避免分辨率、输入焦点和生命周期不一致。 |
| P2 | 历史日志包含旧版本功能描述 | 每次发布标记版本和验证日期，避免把日志中的“计划/旧实现”当作当前契约。 |
| P3 | 美术资源和材质命名仍有 `New Material` 等默认名称 | 按功能、模块和用途重命名，并建立资源命名约定。 |
| P3 | 缺少正式构建产物验收记录 | 记录目标平台、构建版本、场景、输入设备、帧率和已知缺陷。 |

## 6. 推荐验证清单

每次涉及核心逻辑时至少执行：

1. Unity Console 无新增 Error/Exception。
2. 新建存档 -> 放置方块 -> 保存 -> 重启/重新加载，位置、旋转、资源路径一致。
3. 连接点吸附、阻挡、删除断开、Undo/Redo 各操作至少执行一次。
4. 有效驾驶舱进入和退出游玩模式；无驾驶舱时确认阻止进入并给出提示。
5. 主推进、全向推进、悬浮控制及推力视觉在 Play Mode 下工作。
6. 大蓝图加载、取消加载、切换存档时无重复对象或残留引用。
7. `git diff --check` 通过，且只提交当前任务相关文件。

## 7. 文档目录

- [项目定位](#1-项目定位)
- [技术栈与目录](#2-技术栈与目录)
- [运行时架构](#3-运行时架构)
- [已确认实现的功能](#4-已确认实现的功能)
- [待改进与风险](#5-待改进与风险)
- [推荐验证清单](#6-推荐验证清单)
- [代码函数索引](#8-代码函数索引)
- [函数索引维护规则](#9-函数索引维护规则)
- [C# 代码规范](#91-c-代码规范)
- [变更日志](#10-变更日志)

## 8. 代码函数索引

本节按 Git 跟踪的 C# 文件列出函数签名和职责。描述依据当前源码整理；同名重载分别保留。生命周期回调、公共 API、内部计算和协程均列出，便于定位调用链。

### 8.1 索引目录

- Editor 工具
- Actions 操作
- Block 方块
- Camera 相机
- Data 数据
- Effect 特效
- InObject 模块
- Manager 管理器
- Player 玩家
- Thrusters 推进器
- UI 界面
- 模板/其他

### Editor 工具

#### `Assets/Editor/AutoBuildTool.cs`

- `public static void BuildWindows()`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private static string IncrementVersion(string version)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static string[] GetEnabledScenes()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Editor/ProfilerCaptureAnalysis.cs`

- `public static void AnalyzeLatest()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void Analyze(string capturePath, string reportPath)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static string GetLatestCapturePath()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static MarkerAggregate GetOrCreate(Dictionary<string, MarkerAggregate> map, string name)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static void WriteFrameStats(StringBuilder report, List<FrameSummary> frames)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private static void WriteCounters(StringBuilder report)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private static void WriteMarkers(StringBuilder report, string title, IEnumerable<MarkerAggregate> markers, int count)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private static void WriteWorstFrames(StringBuilder report, List<FrameSummary> frames, int count)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private static void WriteHierarchyDrilldowns(StringBuilder report, IEnumerable<int> frames)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private static void WriteHierarchyItem(StringBuilder report, HierarchyFrameDataView view, int id, int depth, int maxDepth)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private static float Percentile(float[] sorted, int pct)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static void WriteReport(string reportPath, StringBuilder report)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private static void Finish(string message)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static string F(float value)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static string F(double value)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static string Pad(string value, int width)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void Add(float ms, int frame)`： 创建几何、资源、操作记录、UI 项或运行时对象。


### Actions 操作

#### `Assets/Scripts/Actions/ActionManager.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void Push(IBlockAction action)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Undo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Redo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Clear()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void CountAction(IBlockAction action)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public int GetActionCount(string actionName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void ShowDebug()`： 触发游玩流程、UI 状态或视觉反馈的更新。

#### `Assets/Scripts/Actions/AddBlockAction.cs`

- `public CreateBlockAction(Block block)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `public void Undo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Redo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Actions/DeleteBlockAction.cs`

- `public DeleteBlockAction(Block deletedBlock)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void Undo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Redo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Actions/GroupAction.cs`

- `public GroupAction(IEnumerable<IBlockAction> actions)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Undo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Redo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Actions/MoveBlockAction.cs`

- `public MoveBlockAction(Block block, Vector3 oldPos, Vector3 newPos)`： 修改模块或方块的旋转/位置，并同步相关运行时数据。
- `public void Undo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Redo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Actions/RotateBlockAction.cs`

- `public RotateBlockAction(Block block, Vector3 oldPos, Vector3 newPos, Quaternion oldRot, Quaternion newRot)`： 修改模块或方块的旋转/位置，并同步相关运行时数据。
- `public void Undo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void Redo()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。


### Block 方块

#### `Assets/Scripts/Block/Block.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnValidate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void GenerateConnectionPoints()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void CreateConnectionPoint(ConnectType connectType, Vector3 localPos, Vector3 normal, int order)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `public Vector3 GetConnectorWorldPosition(Connector connector)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public Vector3 GetConnectorWorldNormal(Connector connector)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void CheckConnection()`： 处理碰撞、连接、耐久、维修或状态检查逻辑。
- `private Block FindBlockAcrossConnector(Connector connector)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Connector FindMatchingConnector(Block otherBlock, Connector connector)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void ConnectTo(Connector connector, Connector otherConnector)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearConnector(Connector connector)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public List<Block> Neighbors()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void DisConnectAllConnectors(bool refreshNeighbors = true)`： 断开连接器；可在批量爆炸拆分时延后邻居刷新，避免重复物理查询。
- `public bool IsBlockedGhost()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void OnDrawGizmos()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。

#### `Assets/Scripts/Block/Durability.cs`

- `void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnEnable()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void CollisionEnter(Collision collision)`： 处理碰撞、连接、耐久、维修或状态检查逻辑。
- `public void Repair(float amount)`： 处理碰撞、连接、耐久、维修或状态检查逻辑。
- `public void UpdateDurablility(float value)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void LateUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void OnGUI()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private bool ShouldShowDebugLabel()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。


### Camera 相机

#### `Assets/Scripts/Camera/CameraController.cs`

- `void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void LateUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void HandleModeSwitch()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void HandleLook()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void HandleMovement()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `public void FocusCameraOnBlock(GameObject obj)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void SmoothFocusCameraOnBlock(GameObject obj, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `public void SmoothFocusCameraOnBlockFramedBy(GameObject lookObj, GameObject frameObj, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `public void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `public void SmoothOrbitCameraAroundBlock(GameObject frameObj, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `public void SmoothOrbitCameraAroundBlock(GameObject frameObj, Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radiusMultiplier, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `public void StartContinuousOrbitCameraAroundBlock(GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `public void StopCameraMotion()`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `private IEnumerator SmoothFocusRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `private IEnumerator ContinuousOrbitRoutine(GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private float CalculateOrbitRadiusMultiplier(float yawDegrees, float radiusVariation, float radiusWaveDegrees)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private IEnumerator SmoothOrbitRoutine(Vector3 orbitCenter, float targetYaw, float targetPitch, float targetRadius, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `private void SetOrbitPose(Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radius)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private bool TryGetFocusPose(GameObject obj, out Vector3 targetPosition, out Quaternion targetRotation)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool TryGetFocusPose(Bounds frameBounds, Vector3 lookPoint, out Vector3 targetPosition, out Quaternion targetRotation)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool TryGetOrbitPose(Bounds frameBounds, float yawDegrees, float pitchDegrees, float radiusMultiplier, out Vector3 targetPosition, out Quaternion targetRotation)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool TryCalculateBlockBounds(GameObject obj, out Bounds bounds)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。


### Data 数据

#### `Assets/Scripts/Data/BlockData.cs`

- `public BlockData(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Data/BuildTargetContext.cs`

- `public BuildTargetContext(BuildTargetKind kind, string saveName, string enemyBlueprintName)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `public string GetSavePath(SaveManager saveManager)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public static BuildTargetContext PlayerSave(string saveName, string enemyBlueprintName)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public static BuildTargetContext EnemyBlueprint(string saveName, string enemyBlueprintName)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static string NormalizeName(string name, string fallback)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。


### Effect 特效

#### `Assets/Scripts/Effect/StylizedBeamEffect.cs`

- `public void Configure(float width, float glowMultiplier, int segments, float noise, float frequency, float speed)`： 配置该组件的几何、推进器、敌人或运行时参数。
- `public void SetEndpoints(Vector3 start, Vector3 end)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetColor(Color color)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetIntensity(float value)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetVisible(bool value)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void LateUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void EnsureInitialized()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void CreateLayer(string layerName, int sortingOrder, out Mesh mesh, out MeshRenderer meshRenderer)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void AllocateGeometry()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void UpdateGeometry()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void UpdateMesh(Mesh mesh, Vector3[] vertices)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ApplyColors()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private static void SetRendererColor(Renderer renderer, MaterialPropertyBlock properties, Color color)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void OnDestroy()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。

#### `Assets/Scripts/Effect/StylizedRingEffect.cs`

- `public void Configure(int segments, float width)`： 配置该组件的几何、推进器、敌人或运行时参数。
- `public void SetVisual(Color color, float width, float visualIntensity = 1f)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetIntensity(float visualIntensity)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetVisible(bool value)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void EnsureInitialized()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void CreateLayer(string layerName, int sortingOrder, out Mesh mesh, out MeshRenderer meshRenderer)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void RebuildMeshes()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void BuildRingMesh(Mesh mesh, float width, float heightOffset)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void ApplyColors()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private static void SetRendererColor(Renderer renderer, MaterialPropertyBlock properties, Color color)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void OnDestroy()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。


### InObject 模块

#### `Assets/Scripts/InObject/ControlUnit.cs`

- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void RefreshChildren()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void AssignRuntimeOwnershipToBlocks(bool overwriteExisting)`： 把模块、方块或控制单元分配或注册到对应运行时集合。
- `public void EnsureRuntimeUnitId()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public void PlayEnd()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void SetMovementInput(Vector3 worldDirection)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetTarget(Transform newTarget)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private Vector3 GetPlayerMovementInput()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void OnCollisionEnter(Collision collision)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void OnDestroy()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private IEnumerator StartCooldown()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static RuntimeUnitMember Ensure(GameObject obj, string unitId, UnitFaction faction)`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。

#### `Assets/Scripts/InObject/EnemyController.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private ControlUnit FindNearestPlayer()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void ConfigureHoverThrusters()`： 配置该组件的几何、推进器、敌人或运行时参数。
- `private Vector3 CalculateDesiredMovement(Vector3 flatDirection)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 GetStrafeDirection(Vector3 flatDirection)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 ApplyObstacleAvoidance(Vector3 desiredMove, Vector3 targetDirection)`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private Vector3 ProbeObstacle(Vector3 direction, float weight)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public int Compare(RaycastHit a, RaycastHit b)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/InObject/EnemySpawner.cs`

- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void BeginPlayMode(Transform anchor)`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `private void InitializeEnemyBlueprintPool()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public void SpawnRandomEnemy()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `public void SpawnEnemy()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `private bool IsValidBlueprint(BlockDataList dataList, string enemyBlueprint)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `private void ApplyBlueprintLocalTransform(Transform blockTransform, Vector3 localPosition, Quaternion localRotation)`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private Vector3 CleanIntegerPosition(Vector3 position, string blockName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Quaternion CleanRightAngleRotation(Quaternion rotation, string blockName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private float SnapNearInteger(float value)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private float SnapNearRightAngle(float angle)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool IsIntegerVector(Vector3 value)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool IsRightAngleVector(Vector3 euler)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool IsNearlyInteger(float value)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool IsNearlyRightAngle(float angle)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 GetSpawnPosition()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void PrepareRuntimeEnemy(ControlUnit enemy)`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `public EnemyBlueprintData(string name, BlockDataList dataList)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/InObject/Meteor.cs`

- `void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void OnCollisionEnter(Collision collision)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/InObject/ModularUnitValidator.cs`

- `public static bool TryGetSingleCockpit(Component root, out Cockpit cockpit, out string reason)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public static int CountCockpits(IEnumerable<Block> blocks)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public static int CountLoadedCockpits()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/InObject/RepairBot.cs`

- `void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void InitializeComponents()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `void InitializeTrail()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void NavigateToTarget(Transform target)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private AdvancedAvoidanceResult CalculateAdvancedAvoidance(Vector3 targetDirection)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 BlendDirections(Vector3 targetDir, Vector3 avoidanceDir, float avoidanceStrength)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void ReturnHome()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void LeaveHome()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void FindDamagedBlock()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private ControlUnit ResolveOwnerUnit()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void SetOwnerUnit(ControlUnit newOwner)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private bool IsOwnedTarget(Durability target)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `void CheckAndRepair()`： 处理碰撞、连接、耐久、维修或状态检查逻辑。
- `void UpdateRepairBeam(bool active)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void EnsureRepairBeamGradient()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public void ClearTarget()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `void OnDrawGizmosSelected()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void OnDrawGizmos()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。

#### `Assets/Scripts/InObject/TurretWeapon.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private UnitFaction GetEffectiveTargetFaction()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private ControlUnit FindNearestTarget(UnitFaction faction, out Durability nearestDurability)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Durability FindNearestDurability(ControlUnit unit)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Durability[] GetDurabilities(ControlUnit unit)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool ShouldPrioritizeEnemyCockpit()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Durability FindCockpitDurability(ControlUnit unit)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void AimAndFire(Durability aimTarget)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void AimAt(Vector3 worldDirection)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void Fire(Vector3 origin, Vector3 direction, UnitFaction faction)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 GetMuzzlePosition()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 GetAimForward()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static UnitFaction Opposite(UnitFaction faction)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public int Compare(RaycastHit a, RaycastHit b)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。


### Manager 管理器

#### `Assets/Scripts/Manager/BlockGroupManager.cs`

- `public static List<List<Block>> GroupBlocks(List<Block> allBlocks)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static Vector3 CalculateGroupCenter(List<Block> group)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/Manager/BuildManager.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void SetBuildMode()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void ToggleEnemyBlueprintBuildMode()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void EnterEnemyBlueprintBuildMode(string blueprintName)`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `public void ExitEnemyBlueprintBuildMode(bool reloadPlayerSave)`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `public void SetCurrentBlockResource(string resourcePath)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetCurrentSaveName(string saveName)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetCurrentEnemyBlueprintName(string blueprintName)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `void AlignAxisToNearestWorldDir()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void HandleSelection()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void SelectBlock(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void DeselectBlock()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void HandleMovement()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void HandleRotation()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void HandleMoveAxisDrag()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void HandleRotateAxisDrag()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void HandleDuplicate(Vector3 newPos, Quaternion newRot)`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `void HandleBuildingPreview()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `public void CreateBlock(GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `public void DeleteBlock()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void SaveBlock(Block block)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public void RemoveBlock(Block block)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void LoadAllBlocks()`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private IEnumerator LoadAllBlocksRoutine(int loadVersion, string loadSavePath, Transform loadParent, List<BlockData> blocksToLoad, double time0)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public void ClearUnloadableData(string id)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public bool IsLoadingBuildTarget(BuildTargetKind kind, string saveName, string enemyBlueprintName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool IsLoadingBuildTarget(BuildTargetContext context)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void ClearUnloadableData(string id, string targetSavePath)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `void InitialBlock()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `float GetMoveStep(Block block, Vector3 moveDir)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool CanCreateBlock(GameObject prefab, out string reason)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void ApplyBlockBuildDefaults(Block block)`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private int CountCockpitsInCurrentConstruct()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void ResetBuildState()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void SetBuildTarget(BuildTargetContext context)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void StopActiveBlockLoad()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private bool IsCurrentBlockLoad(int loadVersion, string loadSavePath, Transform loadParent)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void AbortBlockLoadIfCurrent(int loadVersion, Transform loadParent)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearLoadingBuildTarget()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void StartLoadingCameraOrbit(GameObject frameObject, int blockCount)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private float CalculateLoadingCameraOrbitDegreesPerSecond(int blockCount)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void StopLoadingCameraOrbit()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private CameraController GetMainCameraController()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private float GetCameraOrbitAngle(Vector3 orbitCenter)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 CalculateLoadingCameraOrbitCenter(List<BlockData> blocksToLoad, Vector3 fallbackCenter)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void ClearCurrentGhost()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private bool RemoveCachedBlockData(string id)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private bool RemoveCachedBlockData(string id, string targetSavePath)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void WriteCachedData()`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private void WriteCachedData(string targetSavePath)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public static string ConvertToResourcesPath(string fullPath)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Manager/DestroyManager.cs`

- `public void DestroyGameObject(GameObject obj)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void ExplodeBlock(Block block)`： 处理 Block 爆炸、重新分组和物理冲量，不直接造成伤害。
- `private void DisconnectBlocksInExplosionRange(ControlUnit unit, Block sourceBlock, Vector3 explosionPosition)`： 按爆炸半径和概率断开范围内 Block 的连接器。
- `private void ApplyExplosionForce(Block block, string ownerUnitId, UnitFaction ownerFaction, Vector3 explosionPosition)`：按爆炸范围内 Collider 和编组子方块收集 Rigidbody，并依据最近受击点施加衰减冲量。
- `private void ScheduleUnitCleanup(string ownerUnitId, UnitFaction ownerFaction)`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `private IEnumerator CleanupUnitAfterDelay(string ownerUnitId, UnitFaction ownerFaction)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void PlayDisappearEffect(GameObject obj)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void NotifyObjectDestroyed()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `private void ScheduleRefresh()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `private IEnumerator DelayedRefresh()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ExecuteRefresh()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。

#### `Assets/Scripts/Manager/GameManager.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public static void Init()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。

#### `Assets/Scripts/Manager/MeteorShower.cs`

- `void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `IEnumerator SpawnMeteors()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `void SpawnMeteor()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `public void MeteorDestroyed()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Manager/PerformanceMonitor.cs`

- `void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void UpdateCpuUsage()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void UpdateGpuFrameTime()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void OnGUI()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。

#### `Assets/Scripts/Manager/PlayManager.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void PlayStart()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public bool CanStartPlay(out string reason)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void RefreshGroup(ControlUnit unit)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void SetPlayMode()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void CalculateVelocity()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void HandleSelection()`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `private void SelectBlock(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void DeselectBlock()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void PlayEnd()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void AssignBlocksToParentGroups(List<Block> blocks)`： 把模块、方块或控制单元分配或注册到对应运行时集合。
- `public void RegisterControlUnit(ControlUnit unit)`： 把模块、方块或控制单元分配或注册到对应运行时集合。
- `public void UnregisterControlUnit(ControlUnit unit)`： 把模块、方块或控制单元分配或注册到对应运行时集合。
- `public IReadOnlyList<ControlUnit> GetControlUnits()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private EnemySpawner EnsureEnemySpawner()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。

#### `Assets/Scripts/Manager/InputManager.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Update()`： 处理模式快捷键、相机模式和光标状态。
- `public void EnterBuildMode()`： 将 Play Mode 退出状态恢复为建造锁定模式。
- `private void ApplyCursorState()`： 应用当前模式对应的鼠标显示与锁定状态。

#### `Assets/Scripts/Manager/SaveManager.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void EnsureSaveDirectories()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public void GetAllSaveNames()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void GetAllEnemyBlueprintNames()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void CreateNewSave(string saveName)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `public void CreateNewEnemyBlueprint(string blueprintName)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `public void LoadSave(string saveName)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public void LoadEnemyBlueprint(string blueprintName)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public void DeleteSave(string saveName)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void DeleteEnemyBlueprint(string blueprintName)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public bool RenameSave(string oldSaveName, string newSaveName)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public bool RenameEnemyBlueprint(string oldBlueprintName, string newBlueprintName)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public void DuplicateSave(string saveName)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private string GetDuplicateName(string sourceName, string directory)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool CanUseFileName(string fileName, out string reason)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public string GetSavePath(string saveName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public string GetEnemyBlueprintPath(string blueprintName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public string GetSaveFileSize(string saveName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/Manager/VisualEffectsManager.cs`

- `public static VisualEffectsManager EnsureInstance()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public static Material GetSharedParticleMaterial()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public static Material GetSharedLineMaterial()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public static void TryPlayBlockPlaced(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryPlayBlockRemoved(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryPlayBlockExplosion(Block block)`： 触发 Block 爆炸粒子、闪光、环形效果和镜头反馈。
- `public static void TryPlayObjectDestroyed(GameObject target)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryPlayBlockMoved(Block block, Vector3 from, Vector3 to)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryPlayBlockRotated(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryShowBlockSelection(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryClearBlockSelection(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryUpdateGhostPreview(GameObject ghost, bool isBlocked)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryClearGhostPreview(GameObject ghost)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryDecorateMeteor(Meteor meteor)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryPlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnEnable()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void ApplySceneLook()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private void PlayBlockPlaced(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockRemoved(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockExplosion(Block block)`：分阶段播放爆炸火花、烟雾、冲击波、闪光和镜头反馈。
- `private IEnumerator PlayExplosionAftershock(Vector3 center, float scale, Color emberColor, Color smokeColor)`：延迟播放受控数量的爆炸余震粒子与次级冲击环。
- `private void PlayObjectDestroyed(GameObject target)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockMoved(Block block, Vector3 from, Vector3 to)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockRotated(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void ShowBlockSelection(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void ClearBlockSelection(Block block)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void UpdateGhostPreview(Transform ghost, bool isBlocked)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearGhostPreview()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void DecorateMeteor(Meteor meteor)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void PlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void UpdateSelectionRing()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void UpdateGhostRing()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void EnsureSelectionRing()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void EnsureGhostRing()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private static GameObject CreateRingObject(string name, out StylizedRingEffect ringEffect)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void CreateTransientRing(string name, Vector3 position, Vector3 normal, Color color, float radius, float duration, float width)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void CreateLightFlash(Vector3 position, Color color, float intensity, float range, float duration)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void ShakeCamera(float amplitude, float duration)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private IEnumerator CameraShakeRoutine(float amplitude, float duration)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearCameraOffset()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private static Bounds GetBounds(GameObject target, Vector3 fallbackCenter, Vector3 fallbackSize)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static Vector3 GetBlockSize(Block block)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static Gradient MakeGradient(Color start, Color end)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static Color WithAlpha(Color color, float alpha)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static Texture2D GetSoftParticleTexture()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static void SetMaterialColor(Material material, Color color)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private static void SetMaterialTexture(Material material, Texture texture)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void Initialize(StylizedRingEffect effect, Color lineColor, float radius, float lifetime, float lineWidth)`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public void Initialize(StylizedBeamEffect effect, Color lineColor, float lifetime)`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public void Initialize(Light light, float lifetime, float intensity)`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void OnDestroy()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。


### Player 玩家

#### `Assets/Scripts/Player/PlayerController.cs`

- `void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。

#### `Assets/Scripts/Player/PlayerHealth.cs`

- `void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。


### Thrusters 推进器

#### `Assets/Scripts/Thrusters/HoverFlightController.cs`

- `public void Init()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void OnEnable()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnValidate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private bool EnsureControllerReady()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void RefreshCachedPhysicsValues()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void RefreshTiltLimitCache()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void RebuildThrusterCache()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private float CalculateHeightAdjustment(float heightError)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void UpdateDynamicHeightP(float heightError)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private float CalculateGravityCompensation(float absHeightError)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void CalculateTiltAdjustment(Vector3 currentUp)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void DistributeThrust(float heightAdjustment, Vector3 currentUp)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ApplyRotationCorrection(Vector3 currentUp)`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private void OnDrawGizmosSelected()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `void OnGUI()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void EnsureGuiStyles()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void RefreshUiText()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Thrusters/HoverThruster.cs`

- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public virtual void ApplyThrust()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `public override bool ShouldActivate()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/Thrusters/MainThruster.cs`

- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void ApplyThrust()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `public override bool ShouldActivate()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/Thrusters/Thruster.cs`

- `protected virtual void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `protected virtual void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnTransformChildrenChanged()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void SetRuntimeReferences(ControlUnit owner, Rigidbody ownerRigidbody)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `protected bool RefreshRuntimeReferences()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `protected bool TryEnsureRigidbody()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `protected bool IsPlayModeActive()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `protected bool HasValidRuntimeOwner()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `protected void CacheLocalReferences()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public virtual void ApplyThrustChangeRateLimit()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `public virtual void VisualizeThrust()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public virtual void VisualizeThrust(bool forceUpdate)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public virtual Vector3 GetInputDirection()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void DisableLegacyLineRenderer()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void EnsureVisualEffect()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private Vector3 GetNormalizedThrustDirection()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/Thrusters/ThrusterAllocator.cs`

- `static float[,] MultiplyAT_A(float[,] A, int rows, int cols)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `static float[] MultiplyAT_b(float[,] A, int rows, int cols, float[] b)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `static void AddDamping(float[,] H, int n, float lambda)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `static bool CholeskySolveInPlace(float[,] H, float[] rhs, int n)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Thrusters/ThrusterVisualEffect.cs`

- `public void Initialize(Thruster owner)`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public void SetThrust(float thrustRatio, Vector3 localThrustDirection)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void EnsureVfx()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void UpdateParticleModules(float ratio)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void UpdateGlow(float ratio)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static Vector3 GetStableUp(Vector3 direction)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。

#### `Assets/Scripts/Thrusters/UniversalThruster.cs`

- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void RotateThruster(Vector3 worldDir, bool active)`： 修改模块或方块的旋转/位置，并同步相关运行时数据。
- `public void ApplyThrust()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `public override bool ShouldActivate()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。


### UI 界面

#### `Assets/Scripts/UI/ActionCounterUI.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void UpdateUndoText(int count)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void UpdateRedoText(int count)`： 触发游玩流程、UI 状态或视觉反馈的更新。

#### `Assets/Scripts/UI/GlobalTextStyler.cs`

- `private static void CreateInstance()`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private IEnumerator Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private static void ApplyToSceneTexts()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。

#### `Assets/Scripts/UI/MainUIButtons.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnValidate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void SetDefault()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetMove()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetRotate()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetCurrentBlock(string fileName)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void RegisterDiscoveredBlockButtons()`： 把模块、方块或控制单元分配或注册到对应运行时集合。

#### `Assets/Scripts/UI/MainUIPanels.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private IEnumerator Fade(GameObject panel, bool show)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void ShowCreatePanel()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void ShowRenamePanel(string save)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void HideCreatePanel()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void SetInputPlaceholder(string text)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void ShowDeletePanel(string save)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void HideDeletePanel()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void OnConfirmCreate()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void OnConfirmDelete(string save)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public void PlayStart()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void PlayEnd()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void PlayerDeath()`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void SetPanelInteraction(GameObject panel, bool interactable)`：立即设置面板 CanvasGroup 的交互和射线拦截状态。
- `public void EnterEnemyBlueprintBuildMode()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `public void ExitEnemyBlueprintBuildMode()`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。

#### `Assets/Scripts/UI/PlayerCockpitHealthUI.cs`

- `private void Awake()`：初始化 PlayPanel 中的驾驶舱血条运行时 UI。
- `private void Update()`：刷新玩家驾驶舱引用、血量比例、数值文本和变色填充。
- `private void BuildHud()`：创建左下角驾驶舱血条及其文本、轨道和填充组件。
- `private void RefreshCockpitReference()`：从玩家运行时方块层级查找驾驶舱耐久组件。
- `private void UpdateHealthDisplay()`：根据当前耐久度更新填充比例、颜色和数值显示。
- `private static GameObject CreateRectObject(string objectName, Transform parent)`：创建并挂接 UI 矩形子对象。
- `private static Text CreateText(string objectName, Transform parent, string text, int fontSize, FontStyle style)`：创建运行时 UI 文本组件。

#### `Assets/Scripts/UI/SaveUIPanel.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void RefreshList()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ConfigureSaveItem(GameObject obj, string saveName, UnityEngine.Events.UnityAction onOpen)`： 配置该组件的几何、推进器、敌人或运行时参数。
- `private Button CreateRenameButton(Button deleteButton, Transform parent)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void OnSaveClicked(string saveName)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void OnEnemyBlueprintClicked(string blueprintName)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void OnDuplicateClicked(string saveName)`： 执行存档/文件的读取、写入、重命名或路径处理。


### 模板/其他

#### `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`

- `static ReadmeEditor()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `static void RemoveTutorial()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `static void SelectReadmeAutomatically()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `static void LoadLayout()`： 执行存档/文件的读取、写入、重命名或路径处理。
- `static Readme SelectReadme()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `protected override void OnHeaderGUI()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public override void OnInspectorGUI()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `void Init()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `bool LinkLabel(GUIContent label, params GUILayoutOption[] options)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

## 9. 函数索引维护规则

新增、删除、重命名或改变职责的函数，必须在同一提交更新本节；签名变化替换旧条目，行为变化同时修改描述和变更日志。索引以源码为准，自动提取遗漏的多行签名时手工补充。

### 9.1 C# 代码规范

具体执行规范见 [`Assets/Scripts/AGENTS.md`](./Assets/Scripts/AGENTS.md)。本轮全局审查已统一项目脚本中省略的私有访问修饰符，并在连接重建、建造加载、游玩分组、存档复制、摧毁爆炸、敌方移动、推进器视觉和输入模式切换等关键路径补充意图注释；未改动序列化字段名、场景层级或资源引用。

- **已通过代码/文件确认**：脚本访问修饰符扫描无遗漏（接口声明除外），本次触及脚本使用统一 CRLF，新增规范文件和注释已纳入 Git diff。
- **已通过构建确认**：`dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误；仅有既存 `ProfilerCaptureAnalysis.WriteCounters` 过时 API 警告）。
- **尚未验证**：Unity 编辑器导入、Inspector 序列化、Play Mode 交互、运行时日志和性能表现。

## 10. 变更日志

### 2026-08-23

- **修复玩家死亡后 Respawn 按钮延迟可交互**：死亡入口现在停止旧的面板淡入淡出协程，立即关闭 PlayPanel、启用 DeathPanel 的 `CanvasGroup` 射线与交互，并显式恢复 `respawnButton.interactable`；避免旧协程或 PlayPanel 的 CanvasGroup 在死亡界面上方继续拦截点击。
- **验证**：已完成代码检查；尚未在 Unity Play Mode 验证连续死亡、快速重生和鼠标焦点切换流程。

- **修复爆炸物理作用力未命中周围刚体**：原实现仅对同一 `ownerUnitId` 的编组 Rigidbody 调用 `AddExplosionForce`，而重新分组后的 Rigidbody 中心可能位于爆炸半径外，且外部附近刚体完全未被收集。现在通过 `Physics.OverlapSphere` 收集范围内刚体，并结合编组内实际子方块位置；按最近 Collider 受击点计算距离衰减后用 `AddForceAtPosition` 施加冲量。
- **验证**：`dotnet build HY-Sandbox.sln --no-restore` 与 `git diff --check` 已通过；尚未在 Unity Play Mode 验证不同编组、静态刚体和边界距离下的实际位移表现。

- **增强驾驶舱爆炸表现与持续时间**：延长爆炸火花、烟雾、主冲击环和闪光的生命周期，增加内圈冲击波，并在 0.22 秒后播放受控数量的余震火花与次级冲击环，使爆炸由单次瞬时效果变为分阶段表现。
- **验证**：已完成代码检查；尚未在 Unity 编辑器或 Play Mode 验证视觉时序、粒子观感与实际帧率。

- **建立 Unity C# 代码规范并完成全局脚本标准化**：新增 `Assets/Scripts/AGENTS.md`，补充根 `AGENTS.md` 的规范入口；为 20 个项目脚本补齐显式私有访问修饰符，统一本次触及脚本的 CRLF 换行，并在连接、建造加载、游玩分组、存档复制、摧毁爆炸、AI 输入、推进器视觉和输入管理关键路径增加职责/顺序/性能注释。
- **影响范围**：仅改变代码可读性、维护约束和注释，不改变序列化字段、场景/Prefab 引用或运行时算法。
- **验证**：已完成源码编码/换行检查、访问修饰符扫描、`git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既存过时 API 警告）。Unity 编辑器与 Play Mode 尚未验证。

### 2026-08-22

- **制作玩家驾驶舱变色血条**：新增 `PlayerCockpitHealthUI`，由 `MainUIPanels.Awake` 自动挂载到 `PlayPanel`，在左下角显示玩家 Cockpit 当前/最大耐久度；填充比例随耐久变化，颜色从红色过渡到绿色，并随游玩面板显示状态更新。
- **验证**：已通过脚本与场景引用的代码检查，填充组件使用 Unity `Image.Type.Filled`，文本使用 `LegacyRuntime.ttf`，并通过 `git diff --check`；尚未在 Unity 编辑器或 Play Mode 验证实际布局、字体显示和耐久变化效果。

- **集中输入与相机模式管理**：新增场景级 `InputManager`，统一处理建造/游玩模式的快捷键、相机模式和光标状态；补充 Play Mode 退出及玩家死亡后的建造锁定复位，并保护键盘、相机和目标对象为空的输入路径。
- **审查修复**：修正退出 Play Mode 后 `lockView` 未复位、死亡面板鼠标可见性未同步、F 键无目标时可能空引用等问题。
- **验证**：`dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误；仅有既存 Profiler API 过时警告）；已核对 `InputManager` 场景引用、脚本调用点和函数签名；尚未在 Unity Play Mode 验证完整快捷键与窗口焦点流程。

- **重写 EnemyController 的运动控制**：移除直接读取/修改 Rigidbody 的 `FaceTarget` 转矩逻辑，敌方只通过 `ControlUnit.SetMovementInput` 提供模拟输入；`MainThruster`/`UniversalThruster` 负责根据该输入施加推力和转向。
- **验证**：`EnemyController.cs` 已搜索确认不再包含 Rigidbody 或直接物理写入；`git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore` 已通过（0 错误；仅有既存 Profiler API 过时警告）；尚未在 Unity Play Mode 验证不同推进器配置下的转向手感。

### 2026-08-22

- **降低 EnemyController 模拟输入变化率**：新增 `movementUpdateInterval`（默认 0.5 秒）和 `movementResponseRate`，目标/避障方向按间隔采样，模拟输入在物理帧中渐进逼近，减少 AI 操控的突变和避障查询开销。
- **验证**：已通过代码检查和 `git diff --check`；Unity 编辑器/Play Mode 尚未验证 AI 操控手感与实际帧率收益。

### 2026-08-22

- **修复爆炸后连接器异常与性能回退**：`Block.CheckConnection` 和 `DisConnectAllConnectors` 增加销毁对象/空邻居保护；爆炸范围拆分使用 `DisConnectAllConnectors(false)` 批量断开，并对唯一邻居延后统一刷新，避免重复 `Physics.OverlapSphere`。
- **验证**：已根据 `ProfilerCaptures/HY-Sandbox_2026-08-22_13-23-48.data` 提供的异常堆栈完成代码修复；`git diff --check`（任务文件）和 `dotnet build HY-Sandbox.sln --no-restore` 已通过（0 错误；仅有既存 Profiler API 过时警告）；尚未在 Unity Play Mode 重现确认帧率。

### 2026-08-22

- **增加爆炸范围内随机断开**：驾驶舱爆炸现在使用 `_blockExplosionRadius` 筛选同一运行时单元中的邻近 Block，并按 `_blockExplosionDisconnectProbability` 决定是否调用 `Block.DisConnectAllConnectors()`；之后继续通过 `PlayManager.RefreshGroup` 重新分组。当前仍不造成伤害。
- **验证**：`git diff --check -- Assets/Scripts/Manager/DestroyManager.cs DEVELOPMENT.md` 和 `dotnet build HY-Sandbox.sln --no-restore` 已通过（0 错误、0 警告）；尚未在 Unity Play Mode 验证概率与范围的实际视觉表现。

### 2026-08-22

- **新增驾驶舱摧毁爆炸试用**：`DestroyManager.ExplodeBlock` 会断开并脱离被摧毁的驾驶舱，调用 `PlayManager.RefreshGroup` 重新生成模块组，对各组 Rigidbody 施加径向冲量；`VisualEffectsManager` 增加火花、烟雾、环形闪光和镜头震动。当前不造成伤害。
- **验证**：代码索引、`git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore` 已通过（0 错误；仅有既存 Profiler API 过时警告）；尚未在 Unity 编辑器或 Play Mode 验证驾驶舱摧毁时序、分组和物理表现。

### 2026-08-22

- **完成存档列表 Duplicate 按钮功能**：`SaveManager.DuplicateSave` 现在根据当前编辑模式复制玩家存档或敌方蓝图，自动生成 `Copy`/递增后缀名称，避免覆盖已有文件；成功后刷新 `SaveUIPanel` 列表，失败时记录警告且不改变当前加载目标。
- **验证**：已通过代码与文件检查；尚未在 Unity 编辑器或 Play Mode 中验证实际按钮点击和文件系统写入。

### 2026-08-22

- **UI 字体可读性调整**：将场景和 `SavePrefab` 的 UI 字体由 Rajdhani SemiBold 替换为 Chakra Petch Medium；`GlobalTextStyler` 停用粗描边并改为 1px 深色阴影，降低小字号按钮的笔画拥挤。字体资源、场景引用和脚本已通过文件检查，C# 编译验证通过；尚未在 Unity 编辑器 Play Mode 重新验证视觉效果。

### 2026-08-22

- 新增本开发文档，整理当前 Unity 版本、目录、运行时架构、已实现功能、风险和验证清单。
- 增加第 7 节文档目录和第 8 节代码函数索引，覆盖当前 Git 跟踪 C# 文件中的函数签名、重载和职责分类。
- 增加第 9 节函数索引维护规则，要求后续新增、删除、重命名或改变职责时同步更新索引。
- 新增根目录 `AGENTS.md`，规定以后每次功能、代码、场景、资源或配置修改必须同步更新本文件。
- 本次仅确认代码与仓库文件，未启动 Unity Play Mode；运行时行为仍需按第 6 节清单验证。

### 后续记录模板

```markdown
### YYYY-MM-DD
- 范围：`Assets/...` / `Packages/...` / `ProjectSettings/...`
- 修改：做了什么，以及为什么。
- 影响：对建造、存档、游玩、UI、性能或资源的影响。
- 验证：代码检查、Unity 编辑器、Play Mode、构建或测试结果。
- 未验证/遗留：明确尚未确认的内容。
```

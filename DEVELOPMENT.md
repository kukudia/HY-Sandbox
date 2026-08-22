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

`SaveManager` 管理两个命名空间：玩家存档 `Saves` 与敌方蓝图 `EnemyBlueprints`，支持创建、读取、删除、重命名和文件名校验。`BlockData` 保存资源路径、尺寸、位置、旋转、质量等重建所需数据。

`BuildManager.LoadAllBlocks` 使用协程逐个实例化，支持加载进度、取消旧加载、无法加载数据清理和可选的相机环绕。存档身份依赖文件中的模块数据，不应把运行时 `GetInstanceID()` 当作跨会话稳定 ID。

### 3.4 游玩与推进器

`ControlUnit` 聚合驾驶舱、主推进器和悬浮推进器，读取玩家输入并把世界方向传给推进系统。`MainThruster` 和 `UniversalThruster` 在 `FixedUpdate` 中应用推力并可对齐视觉模型；`HoverFlightController` 使用高度、重力补偿和姿态 PID 逻辑分配悬浮推力。

`ThrusterAllocator.Solve` 将力与力矩目标组成 6 维约束，通过带阻尼的最小二乘和上下界迭代求解各推进器输出。`ThrusterVisualEffect` 使用粒子、光源和渐变颜色表达推力比例，并替代旧的 Line Renderer 视觉。

### 3.5 UI、敌人和效果

`MainUIButtons` 负责按钮事件、操作模式和动态方块按钮；`SaveUIPanel` 负责玩家/敌方蓝图列表；`ActionCounterUI` 显示撤销/重做数量；`GlobalTextStyler` 统一文字描边样式。`EnemySpawner`、`EnemyController`、`MeteorShower`、`TurretWeapon` 和 `RepairBot` 组成战斗与环境事件链。`VisualEffectsManager`、`StylizedBeamEffect` 和 `StylizedRingEffect` 负责放置、删除、移动、碰撞和陨石冲击反馈。

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

## 7. 变更日志

### 2026-08-22

- 新增本开发文档，整理当前 Unity 版本、目录、运行时架构、已实现功能、风险和验证清单。
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


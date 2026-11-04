# 重构前开发记录（历史快照）

以下保存 b25ad9a 时期的原始文档。路径、函数索引与当前架构可能不一致，不能作为当前实现导航。当前入口见 ../DEVELOPMENT.md。

# HY-Sandbox 项目开发文档

> 文档状态：持续维护中
> 最近核对：2026-09-30
> Unity 编辑器：6000.3.11f1（`ProjectSettings/ProjectVersion.txt`）
> 当前分支：`main`

### 2026-10-04
- **DSH 敌机蓝图组**：新增 `Blueprints/DSH_Enemies`（`design_model.py` 布局模型、`write_generator.py` 生成器、`Generate.cs`/`Validate.cs`/`FlightProbe.cs`/`summarize.py`/`power_check.py`、三份 JSON 与验证记录），生成并安装 **`DSH_E01_Skimmer`**（70 部件宽体截击机）、**`DSH_E02_Halberd`**（136 部件双体炮舰）、**`DSH_E03_Talos`**（80 部件圆盘炮台）至本机 EnemyBlueprints；仓库保存同名副本。全部使用原装部件与数值，**不修改 Prefab、生产 C#、AI、PID、场景或部件数值**。三份设计的重心横向偏移 ≤0.007，升重比 3.81/3.02/3.71，水平推力 2000/4000/2600 N，供电余量 2.9/1.6/2.3 倍。文件名带 `DSH_` 前缀以标明来源，重新进入 Play Mode 后进入敌人池。
- **炮塔与维修舱**：每艘加装 4/8/8 座 `Turret` 与 2/3/2 个 `RepairBotContianer`，全部正立安装在甲板顶面（不占用甲板下安装位，因此不与悬浮环和继电器争抢连接点）。维修舱内的 `RepairBot` 以舱体为 home 停靠点，`targetRange` 10 m、`repairAmount` 20/秒，只要 10 m 球内出现同一 `ControlUnit` 的受损方块就脱坞修补后返航，因此每舱提供半径 10 m 的自修复覆盖。维修舱不耗电（Prefab 无 `Power`），只增加 1 质量与 100 耐久。**注意资源名在工程里就是拼错的 `RepairBotContianer`**，模型与生成器必须照抄，否则 `Resources.Load` 会静默跳过。
- **构造规则**：悬浮环吊装在甲板下方（`HoverThrusterBig` 中心 y=-2，其 Up 连接点 y=-1 正对甲板块 Down 连接点）；驾驶舱、发电机、炮塔、维修舱抬高一级至 (x, 2, z)，Down 连接点对上甲板块 Up 连接点；1×1×1 设备翻转 180° 吊在甲板下（甲板顶面被上述部件占据 y∈[1,3]）；`HoverFlightController` **必须正立**，因为它把自己的 `transform.up` 当作机体上方向；`UniversalThruster` 可倒装，因其每帧用 `LookRotation` 把模型转向世界空间移动方向。继电器按 5 m/10 m 方块距离先行布置，因为落在所有继电器 5 m 盒外的部件永远不工作。规则由原装连接点几何推导，并在 `Validate.cs` 中对真实 Prefab 复核。
- **校验**：`Validate.cs` 用游戏自身规则检查资源路径、尺寸、唯一 ID、恰好一个驾驶舱、炮塔与维修舱数量下限、启用连接点几何（含“接合但体积不相接”检测）、单连通分量、体积重叠、供电覆盖与均分、升重余量和控制器朝向。三份蓝图均通过：接合连接点 240/519/250，可达部件 70/70、136/136、80/80，炮塔 4/8/8，维修舱 2/3/2，每负载功率 286/160/231（下限 50，推力效率需要 100）。
- **Unity Play Mode 实飞**：Unity 6000.3.11f1 / Main，真实 `EnemySpawner.SpawnBlockData` + 真实 `EnemyController`，**一次只起飞一艘**（其余 `SetActive(false)`），每艘依次经历 40 秒无输入悬停、20 秒追逐固定玩家目标、冲量推挤后 20 秒恢复，每秒采样 5 次；三艘均关闭炮塔以保证数字可比（开火会改变目标高度进而改变 AI 推导的高度设定点）。结果：均单连通、无掉块；最大倾角 1.26/1.67/1.30°；悬停高度误差 0.102/3.559/0.102 m；稳态垂直速度 0.000/0.026/0.001 m/s；峰值水平速度 2.73/2.80/3.43 m/s；均能在 20 秒内从推挤恢复到 1 m 以内；无损伤时维修无人机全部留在坞内。另跑一轮仅 E03 保留炮塔，其 8 座炮塔全部转向并锁定目标。原始采样 `Flight.csv`，汇总见 `summarize.py` 与 `Flight-summary.json`。
- **已记录的三点局限**：① `HoverFlightController` 的高度积分增益为固定 0.1、**不随质量缩放**（只有比例项按 `mass * 0.5` 缩放），因此 703 kg 的 E02 稳定在 2～3.6 m 的固定偏差上（垂直速度 0.026 m/s，即停住而非振荡），而 364 kg 的 E01 稳定在 0.1 m；② 开局 AI 把高度设定点移交给玩家目标高度（约 9 m 落差），受上述固定增益影响收得很慢，20 秒窗口量到的仍是趋近尾巴（垂直速度仍有 1.2 m/s），悬停阶段因此设为 40 秒、稳态指标取每阶段最后 4 秒；③ 探针从 `EditorApplication.update` 采样，而 `PowerTransmissionDevice.Update` 在同一帧内先清零再分配，二者不同步会偶发读到瞬时 0，实测被标记方块 `currentPower` 恰为 0（从未出现均分后的中间值）且每次只有 0～4 个，`summarize.py` 因此把该列作为参考而非判据，供电覆盖由 `power_check.py` 与 `Validate.cs` 独立断言。
- **运行探针的前提**：Editor 窗口必须全程保持聚焦。窗口失焦时 Unity 会把 `runInBackground` 置为 false 并几乎停止播放循环（实测 145 秒仅 2 帧），此时飞机会表现为以终速下坠，属测量假象而非设计缺陷。
- **尚未验证**：受损结构与密集交火下的表现、维修无人机在实战损伤下的修复效率、与其他敌机的拥挤交互、正式构建性能。本轮未改动 `Assets/`，未重跑 `dotnet build`。

### 2026-10-01
- **简单稳定敌机蓝图**：新增 `Blueprints/CODEX_Enemies` 的脚本、JSON、截图与验证记录，安装 `CODEX_E01_Finch`（27 部件双炮）、`CODEX_E02_Pike`（35 部件纵长四炮）、`CODEX_E03_Manta`（35 部件宽翼四炮）至本机 EnemyBlueprints。使用原装部件，水平重心居中，4/6 个对称大型悬浮推进器，升重比 2.47/2.50；不修改 Prefab、场景或部件数值。配套修复 EnemyController 无目标时的高度设定点，每次失去目标锁定飞控高度，避免设定点跟随机体下降。
- **验证**：三份资源/尺寸/无重叠/唯一 ID/连接几何/供电/安装一致性检查通过。Unity 6000.3.11f1 实际 EnemySpawner 生成后均为单连通组；90 秒测试覆盖无目标悬停、15 秒获取目标、45 秒目标换向及75秒失去目标。追踪收敛段最大倾角 2.45/1.70/1.82°，高度误差 0.25/0.19/0.14m，无目标收敛段最大垂直速度均小于 0.055m/s，无失电或掉块。`dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误、4 个既有警告），本轮运行无新增 Console Error，差异检查通过。为隔离飞行关闭测试炮塔、远距离回收和自动刷怪，未覆盖实战受损及大量敌机拥挤场景；完整数据见目录内 Flight.csv/Flight-summary.json。

### 2026-09-22
- **消除 `PlayManager.RefreshGroup` 重组后的物理停顿**：重组前记录原 `ControlUnit` 的 Rigidbody，创建新分组后按新刚体质心迁移原刚体的点速度和角速度，并显式唤醒新刚体。断开方块或爆炸触发重新分组时，分离组不再因新 Rigidbody 默认速度为零而停顿一个物理步。
- **验证范围**：已通过代码差异检查、`git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore`；尚未在 Unity 6000.3.11f1 Play Mode 验证断开、爆炸、多分组和旋转运动下的连续性。

### 2026-09-23
- **蓝图加载间隔指数衰减**：`LoadAllBlocksRoutine` 每次加载重置计时，以 `max(0.005, 0.25 × 0.5^(elapsed / 2))` 秒逐步缩短块间等待；初始间隔、下限、半衰期可在 Inspector 调整。主场景初始间隔从 0.05 改为 0.25 秒；镜头独立保留原 0.05 秒/块参考时长，原有各蓝图环绕角速度不变，不再随加载节奏调整。零间隔仍每块让出一帧，保留加载切换与 UI 更新。
- **衰减加载验证**：`dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误、4 个既有依赖/过时 API 警告）；Unity 6000.3.11f1 Main Play Mode 验证 0/2/4/8/12 秒的间隔为 0.25/0.125/0.0625/0.015625/0.005 秒，737 块实际恢复完成耗时约 14.92 秒，镜头速度始终为原值 9.782609°/秒；`git diff --check` 通过。尚未系统测试不同帧率、Time.timeScale 与全部蓝图尺寸组合。
- **CODEX 生存蓝图组**：新增 `Blueprints/CODEX/Generate.cs`，通过连接 Editor 的 `eval_file` 直接读取原装 Prefab 生成堡垒、蝠鲼、双体、十字卫士、长矛五份玩家 JSON 蓝图，文件名均带 `CODEX_`。仓库保存副本，本机安装至 `Application.persistentDataPath/Saves`；不修改生产代码、场景或部件数值。生成器拒绝占用格重叠，对不同内容的同名文件先备份。
- **蓝图文件验证**：737/825/1081/981/1017 个部件均通过真实可用连接点几何连通、资源与尺寸、唯一 ID、供电覆盖及存档副本一致性检查；完整结构理论升重比 1.61～2.11。运行验证方法与局限见 `Blueprints/CODEX/README.md`，原始采样见 `Runtime.tsv`；短时自动游玩不等同于长时间密集火力生存排名。
- **蓝图 Play Mode 验证**：Unity 6000.3.11f1 / Main 中五艘逐一完成真实加载、单连通组检查和约 24 秒自动起飞/四向移动观察，驾驶舱最低 500，未供电部件 0，采样峰值速度 6.39～6.87。采用公开移动接口模拟输入，未覆盖键盘端到端和持续命中 DPS。未观察到 Console Error；维修无人机 ClosestPoint Warning 已记录至风险。退出后 Main 场景保持未修改状态。
- **BuildManager 连接刷新**：`SaveBlock` 在保存后先调用 `Physics.SyncTransforms`，按方块尺寸和 0.35 单位邻域收集受影响 Block，再去重重建连接；`Block.CheckConnection` 清理共享连接器在两侧的引用、刷新双方 `Info`，拒绝已占用对端 Connector，并兼容缺少连接器视觉 Prefab 的连接状态，解决移动/删除后状态图标和连接关系滞后一帧或残留的问题。
- **大型方块预览**：Ghost 的放置偏移改为按预览方块旋转后的方向半尺寸计算，网格吸附改用半网格边界以保持奇数尺寸方块的中心坐标；阻挡检测统一乘 `gridSize` 并同时检查实体 AABB 与连接点射线，2x1x1、2x2x1、2x2x2 等尺寸不会再固定按 1x1x1 处理。
- **验证范围**：已通过 `dotnet build HY-Sandbox.sln --no-restore`（0 错误，保留既有依赖版本和过时 API 警告）及 `git diff --check`；Unity 6000.3.11f1 `BuildPalettePlayProbe` 运行通过 47 项检查，包含 2x2x2 Ghost 连接点三轴重合、X/Z 整格坐标、绿色可放置预览，以及连接→移动断开→移回重连的双方状态刷新；大型蓝图压力仍未验证。

- **连接点双侧能力校验**：`Block.IsConnectorAvailableForPlacement` 和 `CheckConnection` 现在都要求当前 Connector 与对面匹配 Connector 同时 `canConnect=true`、未占用，并满足位置与相反法线条件；射线/OverlapSphere 只负责发现候选 Block，不再直接决定连接。已通过代码构建、差异检查和 Unity 6000.3.11f1 Play Mode 探针；48 项检查全部通过，包含“禁用对面 Connector 后双方保持断开”。
- **分组连接判定修复**：`Block.Neighbors` 现在只返回通过双侧 Connector 能力、位置、法线校验的邻居，不再把单纯射线命中的方块加入连通图；`BlockGroupManager` 过滤空对象、禁用对象和输入集合外的邻居。新增 Play Mode 回归检查覆盖正常连接、禁用对面 Connector、禁用当前 Connector 三种分组结果。Unity Play Mode 回归已通过；后续优化移除了分组前逐块重建连接的开销。
- **分组计算开销优化**：分组遍历只同步一次物理，复用 BFS 队列和邻居缓冲；`OverlapSphere` 改用可复用的 `OverlapSphereNonAlloc` 缓冲，匹配比较使用平方距离，避免每个 Block 重复分配数组和执行 `CheckConnection`/连接视觉重建。分组只读取当前有效 Connector 几何关系，因此同帧移动、禁用 Connector 和缺少连接视觉 Prefab 都能得到正确结果。Unity 6000.3.11f1 Play Mode 探针通过 54 项检查，新增覆盖“无视觉连接仍可分组”“同帧移动”“禁用面且连接标志过期”等场景；代码构建与 `git diff --check` 通过。

本文档以仓库当前 Git 跟踪的 `Assets/`、`Packages/`、`ProjectSettings/` 和历史日志为依据。历史日志中的功能描述可能来自旧版本，若没有当前脚本、场景或运行时证据，不视为已实现。

### 2026-09-30
- **重构 BuildManager 建造状态与轴操作**：建造交互统一为互斥的 `Select`、`Move`、`Rotate` 三态。按钮通过 `SetSelectType` 切换并以颜色标识当前状态；切换时清除拖拽句柄，只显示对应的 MoveAxis 或 RotateAxis，避免两套 Gizmo 共存。选择资源、退出建造和切换目标会回到 Select 状态。
- **修复箭头拖拽和复制无响应**：MoveAxis/RotateAxis 通过 `GetComponentInParent` 接收箭头碰撞体，轴向优先读取句柄序列化轴并对旧的零值配置按箭头位置/名称回退；主场景六个轴句柄已写入明确的 X/Y/Z 值。移动和复制保持当前模式，不再依赖句柄名称猜方向。
- **完善旋转操作**：RotateAxis 支持按轴拖拽、网格化角度和占用检测；旋转拖拽结束后统一写入存档、Undo 动作与视觉反馈。`RotateBlockAction` 的 Undo/Redo 现在同时恢复旋转和因尺寸吸附产生的位置变化。
- **验证范围**：已通过 `dotnet build HY-Sandbox.sln --no-restore --nologo`（0 错误；4 个既有依赖版本/过时 API 警告）、Unity 6000.3.11f1 Editor 重编译（`compilationFailed=false`、Console Error 0）、Unity CLI 场景轴向量写入与保存，以及 `git diff --check`（当前工作区既有 `UserSettings/Layouts/default-6000.dwlt` 生成布局改动包含空字段尾随空白，未纳入本次代码修改）。尚未在 Play Mode 完成鼠标拖拽、复制、旋转碰撞和按钮颜色的人工交互回归。

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
| 资源 | `Resources/Blocks` 下按资源路径加载模块 Prefab；建造目录在 Editor 中烘焙保存 |
| 编辑器工具 | Windows 自动构建工具、Profiler 捕获分析工具、工业美术资源重建工具；Unity CLI `1.0.0-beta.8` 与 Pipeline `0.7.0-exp.1` |

主要目录：

- `Assets/Scripts/Block`：方块尺寸、质量、碰撞、连接点和邻居关系。
- `Assets/Scripts/Manager`：建造、保存、游戏状态、游玩、破坏、陨石和视觉效果管理。
- `Assets/Scripts/Actions`：添加、移动、旋转、删除及组合操作的撤销/重做。
- `Assets/Scripts/InObject`：驾驶舱、控制单元、机架、炮塔、敌人、维修机器人等模块行为。
- `Assets/Scripts/Thrusters`：悬浮、主推进、全向推进、推力分配和推力视觉效果。
- `Assets/Scripts/UI`：建造/游玩面板、按钮、存档列表、动作计数和全局文字样式。
- `Assets/Resources/Blocks`：方块 Prefab 来源；`BuildPaletteBaker` 在 Editor 中发现可建造 Block 并保存分类按钮，运行时不再克隆文字按钮。
- `Assets/Art/SpaceKit`：两套本地科幻包筛选出的独立备用素材库，105 个 Prefab / 22 个用途类别，配套模型、URP 材质、贴图、碰撞和来源/验证清单；入口 `README.md` 与 `CATALOG.md`。
- `Assets/Art/Industrial`：工业玩具/霓虹工程舱风格规范、共享 Mesh、共享材质和独立预览场景。
- `Assets/Scenes/Main.unity`：当前 Git 跟踪的主场景。

## 3. 运行时架构

### 3.1 启动与模式切换

`GameManager` 在启动时初始化全局管理器和方块父节点。`MainUIPanels` 控制创建、删除、建造、游玩、死亡等面板的淡入淡出。`BuildManager` 负责建造上下文；`PlayManager` 负责进入/退出游玩模式及控制单元分组。`CameraController` 提供第一人称和自由飞行两种视角，`B` 切换视角锁定状态，`Tab` 切换相机模式。

`InputManager` 统一处理 `B`/`Tab`/`F`、敌方蓝图开发者快捷键和模式光标状态：建造锁定模式显示并限制鼠标，建造自由飞行模式隐藏并锁定鼠标；游玩模式仅按住 Alt 时显示并限制鼠标，F1 推进器信息面板打开时只锁定镜头，不显示鼠标。`PlayPanel` 左下角由 `MainUIPanels` 显示白色单元总血条和绿色驾驶舱血条。

编辑器菜单 `Tools/HY-Sandbox/Scene View/Follow Main Camera` 可切换 Scene 视角跟随当前场景的 MainCamera；开启时同步位置、朝向及透视视场角/正交尺寸，关闭后 Scene 视角恢复独立控制。开关状态按项目保存在本机 EditorPrefs，不写入场景或构建。

进入游玩模式前，`PlayManager.CanStartPlay` 会检查当前构造体是否存在有效驾驶舱；成功后由 `ControlUnit` 刷新子模块并取得运行时所有权。退出游玩模式时恢复建造状态并清理运行时分组。

### 3.2 建造、连接与碰撞

`BuildManager` 的主要流程：

1. 从 `Resources/Blocks` 选择资源并创建 Ghost 预览。
2. UI 上的指针输入先被拦截；射线检测方块并在自身 `canConnect`、未占用且对面没有实体方块的 Connector 上显示 0.9×0.9 白色圆角线框，按连接面的世界法线和预览方块旋转后的方向半尺寸计算放置偏移，再按半网格边界吸附；没有可用连接点、移出目标或退出模式时清除 Ghost 与提示。
3. `Block.IsBlockedGhost` 和 `BuildManager.IsBlocked` 同时检查连接点射线和按 `gridSize` 缩放的实体包围盒，阻挡时禁止放置。
4. `CreateBlock` 实例化 Prefab，应用默认值并写入当前存档。
5. 选中方块后支持键盘移动、45 度旋转、移动/旋转轴拖拽、复制和删除。

`Block` 根据尺寸在六个方向生成连接点，通过位置和相反法线匹配相邻模块，维护 `neighbors`。`CheckConnection` 会先同步物理变换并双向清理旧连接器，再建立未占用的匹配，连接/断开时同步双方 `Info` 状态；`Neighbors` 和 `BlockGroupManager.GroupBlocks` 使用同一套双侧连接规则，分组遍历复用物理查询缓冲和 BFS 工作区，避免射线命中、禁用 Connector、输入集合外对象或可选连接视觉造成错误合组和额外分配。`PlayManager.RefreshGroup` 将连接状态刷新留给实际连接变更路径，分组前只做一次物理同步。`IsConnectorAvailableForPlacement` 会同时检查本方 `canConnect`、占用状态和对面方块是否存在，避免对着 `canConnect=false` 的邻接面显示提示或放置。连接成功后创建连接视觉对象（缺少视觉 Prefab 时仍保留逻辑连接）；`DisConnectAllConnectors` 用于删除、拆分和游玩结束清理。

### 3.3 存档与加载

- 加载节奏从 0.25 秒开始，每经过 1 秒加载时间减半，最低 0.005 秒；实际间隔受帧率与实例化耗时限制。镜头继续使用独立固定角速度，加载变快后不保证恰好环绕一周。`_loadCameraReferenceSecondsPerBlock` 在 Main 场景保留为原 0.05，`_blockLoadIntervalHalfLifeSeconds` 与 `_minimumBlockLoadIntervalSeconds` 可调。

- 玩家可加载 `CODEX_01_Bastion`、`CODEX_02_Manta`、`CODEX_03_Catamaran`、`CODEX_04_Crossguard`、`CODEX_05_Lance`。`Blueprints/CODEX` 提供 JSON、可重复生成/校验脚本和操作说明；该目录位于 Assets 外，不被打包，也不自动安装到其他机器。开始游玩后按一次 Space 启动悬浮，WASD 移动，Q/E 调高。

`SaveManager` 管理两个命名空间：玩家存档 `Saves` 与敌方蓝图 `EnemyBlueprints`，支持创建、读取、删除、重命名、复制和文件名校验。列表中的 Duplicate 按钮会在当前命名空间生成不覆盖已有文件的 `Copy` 名称，并刷新列表；复制不会切换当前加载目标。`BlockData` 保存资源路径、尺寸、位置和旋转等重建所需数据。

`BuildManager.LoadAllBlocks` 使用协程逐个实例化，支持加载进度、取消旧加载、无法加载数据清理和可选的相机环绕；加载前会从完整 `BlockData` 计算含旋转尺寸的逻辑包围盒，加载镜头始终围绕该固定范围取景，完成后停留在新构造体的合适观察距离。方块数、总质量、用电模块 `standardWorkingPower` 总需求和发电机 `outputPower` 总输出在主加载 `for` 循环中按成功恢复的 Block 增量累计并同步到 `BlueprintUIPanel`，不额外遍历已加载方块。存档身份依赖文件中的模块数据，不应把运行时 `GetInstanceID()` 当作跨会话稳定 ID。

### 3.4 游玩、供电与推进器

`PowerGeneratingUnit` 提供 `outputPower`；`PowerTransmissionDevice` 每帧按 `maxConnectionDistance` 的世界坐标轴对齐立方体范围重建发电机/输电设备双向连接，通过设备和发电机共同组成的连通网络传递功率，并向任一设备 `powerRange` 立方体范围内带 `Power` 的 Block 供电。两种范围值均表示半边长，边界使用逐轴 `<= range` 判定。同一网络汇总所有发电机输出、对去重后的负载均分；不同网络同时覆盖同一负载时功率叠加，断连或禁用后旧功率会被清零。`DebugManager` 集中控制供电范围、网络连接以及 Block 连接/耐久/供电状态图标；`Info` 在连接、耐久或供电数据变化时由对应组件主动刷新状态，Normal 状态不显示，其余状态由场景中的 `IconManager` 统一提供。状态图标投影自 Block 的 `Center` 到独立 Screen Space Overlay Canvas，排序固定为普通 UI（0）高于图标（-1），并由 `IconManager` 用 `sin(Time.unscaledTime)` 统一驱动透明度。多个供电范围通过坐标压缩生成只含并集外表面的单个 Mesh，按孤立/已连接/有功率状态切换子网格颜色，内部重叠面不渲染，连接关系继续使用运行时复用的虚线 `LineRenderer` 表示。

`ControlUnit` 聚合驾驶舱、主推进器和悬浮推进器，读取玩家输入并把世界方向传给推进系统。敌方 `EnemyController` 默认每 0.5 秒采样一次目标/避障方向，并以响应速度渐进更新模拟输入；敌方不再直接修改 Rigidbody 的旋转或力，转向和位移统一交给 `MainThruster`/`UniversalThruster` 根据 `MovementInput` 施加。`Power.isWorking` 作为悬浮控制器、推进器和炮塔的硬启停条件；`Power.efficiency` 缩放悬浮推力/姿态修正、各推进器有效推力，以及炮塔伤害和射速。`HoverFlightController` 使用高度、重力补偿和姿态 PID 逻辑分配悬浮推力。

`ThrusterAllocator.Solve` 将力与力矩目标组成 6 维约束，通过带阻尼的最小二乘和上下界迭代求解各推进器输出。`ThrusterVisualEffect` 驱动已保存、完全解包到模型喷口的 UNI VFX Graph，持续发射密度保持稳定，亮度随真实推力平滑变化。主、全向和悬浮推进器使用同一无烟 `HoverJet` 模板，普通/大型喷口分别以 1.35/2.7 世界尺寸保存；局部模拟跟随喷头挂点，避免世界/局部方向混用。停机、失电或退出运行模式时停止发射；离开镜头后仍正常计时并结束。

### 3.5 UI、敌人和效果

**蓝图设计要点（悬挂面与朝向）**：`HoverThrusterBig` 只有启用的 Up 连接点，中心在 y = -2 时其连接点位于 y = -1，正好接上甲板块的 Down 连接点，所以悬浮环应吊装在甲板下方；驾驶舱、发电机、炮塔与维修舱只有 Down 连接点，需要抬高到甲板上一级 (x, 2, z) 才能与甲板块的 Up 连接点重合；1×1×1 设备若吊在甲板下必须翻转 180° 才能让 Down 连接点朝上。两处朝向敏感：`HoverFlightController` 把自己的 `transform.up` 当作机体上方向（`CalculateTiltAdjustment` / `ApplyRotationCorrection`），翻转安装会把机体压成倒扣；`UniversalThruster` 与 `HoverThruster` 则与自身旋转无关（前者每帧 `LookRotation` 转向世界空间移动方向，后者沿世界 up 施力），可以倒装。甲板顶面因此是驾驶舱/发电机/炮塔/维修舱的安装面，而甲板底面留给翻转后的继电器与万向推进器。供电方面，落在所有 `PowerTransmissionDevice` 的 `powerRange`（5 m 盒）之外的带 `Power` 部件永远不工作，因此继电器需要按距离先行布置，而不是事后补齐。

- `Blueprints/CODEX_Enemies` 提供三种 95～120 部件的多层敌机和可重复生成/校验脚本；本机 EnemyBlueprints 已安装，重新进入 Play Mode 后进入敌人池。已验证完整机体的无目标悬停、追踪、换向及失去目标后高度保持，不保证动力受损后稳定。增重后峰值速度低于上一版，数值见最新变更日志。

- `Blueprints/DSH_Enemies` 提供三种带 `DSH_` 前缀的敌机蓝图（70/136/80 部件，含 4/8/8 座炮塔与 2/3/2 个维修舱）及其布局模型、生成器、校验器与逐艘实飞记录，本机 EnemyBlueprints 已安装同名副本，重新进入 Play Mode 后进入敌人池。三份共用一套由原装连接点几何推导的构造规则：悬浮环吊装在甲板下方、驾驶舱/发电机/炮塔/维修舱抬高一级、1×1×1 设备翻转后吊在甲板下、`HoverFlightController` 保持正立、`UniversalThruster` 可倒装、继电器先行布置以满足 5 m/10 m 供电距离。维修舱内的 `RepairBot` 以舱体为停靠点、覆盖半径 10 m 的自修复范围（注意资源名拼写为 `RepairBotContianer`）。升重比 3.81/3.02/3.71，横向重心偏移 ≤0.007；实测最大倾角 ≤1.67°、稳态垂直速度 ≤0.026 m/s，E01/E03 悬停误差 0.10 m、E02 因原装高度积分增益不随质量缩放而稳定在 3.6 m。设计方法与三点已知局限见 `Blueprints/DSH_Enemies/README.md`；跑实飞探针时 Editor 窗口必须全程保持聚焦，否则播放循环被节流、读数全部失效。

`MainUIButtons` 负责按钮事件和操作模式，DebugSettingsPanel 提供供电范围、连接线、Block 连接状态、耐久状态和供电状态五个独立开关；`BuildPalette` / `BuildPaletteItem` 管理已保存的 23 个图标按钮、六类导航、滚动、悬停名称和选中颜色；`SaveUIPanel` 负责玩家/敌方蓝图列表；`BlueprintUIPanel` 显示当前建造目标名称、方块数量、总质量、需求功率和发电机总输出，并在搭建、拆除、Undo/Redo 时刷新，在存档或敌方蓝图异步加载期间逐块更新；`ActionCounterUI` 显示撤销/重做数量；`GlobalTextStyler` 统一 Chakra Petch 字体与轻量阴影样式，避免小按钮文字因粗描边显得拥挤。`EnemySpawner`、`EnemyController`、`MeteorShower`、`TurretWeapon` 和 `RepairBot` 组成战斗与环境事件链；RepairBot 只选择与 home 同属一个 ControlUnit、且位于 `targetRange` 球形范围内的受损方块，寻路避障按间隔采样并渐进转向，返航时对准停靠姿态后平滑减速归位，其飞行反馈由保存的双层青色尾迹和速度驱动喷口 Graph 组成，维修时从模型工具端发射双层能量束，并使用素材电弧与真实耐久恢复脉冲。`DestroyManager` 在驾驶舱摧毁时按爆炸半径和概率断开同一运行时单元内的 Block，再重新分组并施加爆炸冲量（当前不造成伤害）；`VisualEffectsManager` 和 `StylizedBeamEffect` 负责放置、删除、移动、碰撞、爆炸和陨石冲击反馈。Block 爆炸与普通物体销毁均播放 UNI Aerial Explosion Graph；前者放大最低尺寸并保留瞬时点光和镜头震动，可爆炸方块不再叠加拆除爆发。两条销毁路径均可通过 VisualEffectsManager 的 Destruction VFX 调试开关查看目标、边界、缩放和实际生成结果。陨石尾迹独立跟随位置并按速度定向，不继承陨石自转。失去驾驶舱的断裂 Rigidbody 会获得最长 6 秒、按速度衰减的烟雾/余烬拖尾，同时限制全局活动数量为 24。

`BuildPalette` 的 `HoveredBlockName` / `HoveredBlockInfo` 使用 Main 场景中现有的文字布局；23 个目录按钮烘焙 Block Prefab 的静态参数信息，包括尺寸、质量、成本、耐久、功率需求/输出、推进力与货仓容量（按组件存在情况显示）。默认 `Block.info` 根据当前参数生成；启用 `canEditInfo` 并填写文本后保留手写内容。编辑器菜单 `Tools/Build Palette/Refresh hovered block info` 只刷新文字引用和条目参数。

`MainUIPanels` 为每个面板只保留一条淡入淡出协程，并在删除确认关闭时移除临时按钮回调。`PlayPanel/CombatHud` 是保存于 Main 场景的 uGUI：复用敌方名牌实例，每 0.2 秒汇总敌方单元当前耐久，以生成时记录的最大耐久作为固定分母；按镜头视野和距离隐藏离屏目标。击杀提示位于画面居中偏下，以不透光深色背景、较大文字和短暂淡入淡出显示敌方蓝图名称及本轮击杀数；仅玩家攻击导致敌方驾驶舱摧毁时计数。按钮的悬停、按下和焦点缩放由 `UIInteractionFeedback` 处理。`Tools/HY-Sandbox/Setup Combat HUD` 可重复配置场景和悬浮控制器 Prefab；后者的 IMGUI 诊断窗默认关闭，仍可在 Prefab Inspector 中开启 `showUI` 调试。架构参考 [deVoid UI Framework](https://github.com/yankooliveira/uiframework) 与 [Unity-UI-Framework](https://github.com/MrNerverDie/Unity-UI-Framework) 的界面职责拆分及过渡管理，当前实现继续使用项目已有 uGUI。

玩家和敌人使用双血条：白色为含 Cockpit 的 `ControlUnit` 当前/出生总耐久，下方彩色条为驾驶舱当前/最大耐久（玩家绿色、敌人红色）。敌人的总耐久上限由生成器在实例化方块时累计并保存在 Cockpit 的 `EnemyIdentity`，后续分组和损毁不改变分母；玩家总耐久上限在 `PlayStart` 固定。敌人名牌保留 Screen Space Overlay 的清晰度，以相机到名牌锚点的 3D 射线过滤其它模型遮挡，并用 CanvasGroup 淡入淡出。`StatusIcon` 复用相同遮挡判定，淡入淡出透明度与原有脉冲相乘。`HoverFlightController` 的旧 IMGUI 已迁移到 PlayPanel Canvas：左下角显示目标高度、当前高度、高度 P、垂直速度和水平速度；F1 打开右上角 `ThrusterInfoPanel`，按 Main/Universal/Hover 分类显示拥有有效运行时归属的推进器、推力进度和供电状态颜色。Hover 控制器 Prefab 的状态灯为绿色表示被有效 ControlUnit 使用，红色表示未使用。推进器分类选中线在 Main 场景中默认保存为标签底部 3px 青色横线，面板初始化时同步当前分类，避免首次显示 Universal 标签附近出现 100×100 的深色方块。

### 3.6 物理模拟与性能

`ControlUnit.OnCollisionEnter` 以法向速度和 PhysX 实际冲量分摊复合碰撞体能量，`Durability.ApplyCollisionEnergy` 以伤害/焦耳系数乘 0.1 倍映射至耐久，单次上限为最大耐久的 8%，同模块伤害间隔 0.35 秒。`DestroyManager` 保持爆炸零直接伤害；基础冲量缩放至 0.15，单次附加平移速度上限 3 m/s，受影响模块及对方碰撞均受 3 秒保护，碰撞损毁不触发物理爆炸。上述参数可在 Inspector 调整。`ImpactPhysics.ClosestPoint` 为非凸碰撞体提供 bounds 回退。`PhysicsTelemetryHud` 的真实场景节点显示速度、质量、动能、旋转速率及撞击数据；`InterfaceBaker` 烘焙 Main 与动态存档 Prefab，面板底色 alpha 0.62，保留文字图标不透明。

`PlayManager.AssignBlocksToParentGroups` 为每个运行时控制单元（包括重组后的分离组）设置 Rigidbody 阻尼；线性阻尼默认 1.5（原 0.5），角阻尼默认 2，均由 PlayManager Inspector 调整。维修机器人与陨石保留各自独立物理参数。

项目使用 3D PhysX 作为运行时物理后端。为降低物理线程在大型构造体、敌人和爆炸冲量场景下的持续计算压力，当前项目设置为：固定物理步长约 0.02 秒（50 Hz；`ProjectSettings/TimeManager.asset` 使用 Unity 6000 的有理数格式保存）、单帧物理追赶上限 0.1 秒、默认位置求解迭代 4 次、默认速度求解迭代 1 次。碰撞回调复用已启用，Transform 自动同步保持关闭；2D 物理设置未改变。降低步频和迭代次数会减少 CPU 占用，但高速碰撞、堆叠稳定性和推进器控制手感需要在 Play Mode 复核。

### 3.7 Block 美术、挂点与渲染风格

当前功能模块使用 `Assets/Art/SpaceKit` 与用户整理的 `Assets/Art/Temp`：保留 Cube1 基础块、Battery 发电机/维修舱、无人机、炮塔、AirVent、主推进器和全向底座的手动选择，补全驾驶舱、门、控制器、机架、楼梯及大小推进器。替换实例全部 Unpack Completely；根 Block、BoxCollider、连接点及启用掩码、密度、耐久和 Resources 名称保持一致，视觉附带碰撞体移除。

`Assets/Art/BlockVisuals` 保存适配后的粒子 Prefab、材质、依赖迁移清单、Unity 验证结果与预览。`BlockArtIntegrator` 只处理没有 `ArtIntegration_SpaceKit_v1` 标记的模块；旧 `IndustrialArtGenerator` 跳过已经集成的模块，避免覆盖手动调整。基础块与 Connector 保留既有外观。

炮塔保留固定底座，水平轴绑定 `SM_Prop_Turret_Large_Top_01`，俯仰轴绑定 `SM_Prop_Turret_Large_Barrel_01`；aimPivot 与 Muzzle 已从停用的旧模型迁移，枪口按网格端环定位，开火闪光与射线共用此挂点。默认转速、俯仰限位、伤害和射速不变。全向推进器只旋转喷头，底座保持固定；主推进器分别覆盖 2/4 个物理喷口，悬浮特效朝风口下方发射。

RepairBot 的模型朝向与导航 +Z 对齐，维修束从 RepairOrigin 工具端发出，容器明确绑定 home/Outside。初始化停靠状态不再被冷却提前返回阻断，冷却结束前保持停靠；飞行和维修效果均为可编辑 VFX Graph 资产。发电机与维修舱各增加两盏无阴影状态灯。选中和 Ghost 高亮跳过粒子/尾迹，避免覆写特效材质。

实时反馈通过 `Resources/VFX/BlockVfxLibrary` 读取原生 VFX Graph。UNI Aerial/Small/Impact Explosion、Small Smoke Impact、Device Fire、无烟 Gas Fire Thruster 覆盖爆炸、破碎、命中、烟火和推进；能量光束、维修、建造、机器人与掉落拖尾也统一使用 GPU Graph。可编辑 Prefab 保留在 `Assets/Art/BlockVisuals/VFX`，Graph 及完整依赖位于 `Assets/Art/VFX`，不依赖被忽略的 UNI 原包。VfxEffect 保留低强度持续密度、停止后的尾烟及显式清空；瞬时上限 64、断裂烟迹上限 24、12 秒回收。爆炸优先占用非爆炸效果的槽位；若 64 槽全是爆炸，则跳过新爆炸及对应点光，避免只剩灯光。推进挂点 +Z 不变，适配原包 -X 轴；喷焰、烟火等使用 Local 模拟空间，能量拖尾以 World 模拟空间保留运动历史并从局部挂点发射。41 个旧粒子 Prefab 已迁移，新增 2 个能量模板；运行时代码无 ParticleSystem/TrailRenderer。目标平台需支持 Compute，编辑入口见 `Assets/Art/VFX/README.md`。

### 3.8 科幻工业备用素材库

`Assets/Art/SpaceKit` 从被 gitignore 忽略的 CUBE Spaceships Pack 01 与 PolygonSciFiSpace 复制筛选素材，包含驾驶舱、机身/机翼、推进器、起落架、能源/传感设备、炮塔/弹体、维修机器人、结构/舱壁、货物/控制台/灯具、陨石/残骸、整船参考及四类粒子效果。22 类 Prefab 每类 2–7 项，共 105 个；另选 10 个配色材质。完整依赖共 349 项（103 FBX、105 Prefab、29 材质、30 贴图、82 碰撞网格），约 19.02 MiB，不含 meta 和预览。

素材使用独立 GUID；备用 Graph 特效共享 Assets/Art/VFX 和 VfxEffect 控制器，其余依赖落在 SpaceKit 或 Unity 内置包内。旧材质通过 Editor API 转换为 URP Lit/Particles Unlit；飞船去除旧 Rim 算法，陨石简化为低光泽岩石基色。原包与 Main 场景保持原状。部分备用素材已经通过 BlockVisuals 适配到 Resources/Blocks 和运行时 VFX；逐项清单中的模型数据仍有效，旧粒子上限和效果缩略图只作历史记录。

`SpaceKitCurator` 提供初次创建（拒绝覆盖已存在库）、引用验证与独立场景预览菜单；Selection.json 记录选择与用途，Catalog.json 记录来源/GUID/SHA-256，Validation.json 记录实际 Unity 审核。已完成 Editor 导入、全部 Prefab 重载、粒子采样、105 项 URP 渲染与四张联系表检查；原始储备库不直接进行玩法测试；已集成模块的 Play Mode 结果另见 BlockVisuals/PlayModeValidation.json，并发性能仍未验证。

### 3.9 货仓、残骸回收与共享无人机导航

新增 `CargoHold`（2×2×2，8 个特殊零件）、`CoinHold` / `TechnologyHold`（1×1×1，100 单位）和 `CollectionBotContainer`（1×1×1）真实 Prefab。三种仓体的容量、供电要求、双方摧毁掉落策略均在 CargoHold 中编辑；爆炸行为沿用 Block 参数，特殊仓默认爆炸、资源仓默认不爆炸。透明框架、容量液面、货物静态陈列和空/部分装载/满状态灯由独立 CargoHoldView 驱动。

`Bot` 从原 RepairBot 提取飞行、采样避障、移动 Home 制动/停靠与飞行特效，保留旧序列化字段名/类型和 RepairBot GUID；RepairBot 保留维修工作，CollectionBot 负责预约、拾取、携带、返航交付。回收器只为同一有效 ControlUnit 的可用货仓工作，满仓/断电时不出动，失电/货仓被毁/任务超时释放预约，回收舱真实摧毁前会先释放携带物，避免 Unity 递归销毁吞掉货物。

残骸定时清理通过 WreckSalvage 幂等结算：普通敌方模块变金币，Settings 候选功能模块按概率变特殊零件；玩家自拆和仍有驾驶舱的远距离卸载不产币。LootDrop 与 Block/ControlUnit 生命周期分离，特殊零件不被普通清理规则删除。金币/科技值资源包寻找范围内有电且有空位的玩家对应仓，到达后才入账，剩余数量可换仓；默认最多 64 个资源包后合并金额，每包最多 16 粒子，不使用 Compute Shader。

`BlockData.cargo` 保存货仓内容，旧 JSON 兼容；建造删除 Undo / 创建 Redo 保留内容。现有“结束游玩”暂作安全返回入口，CargoPersistence 以临时文件替换方式保存有效玩家单元的货仓内容（保持蓝图几何）；死亡返回清空携带内容，未装载掉落物在会话结束清理。此版尚无独立撤离地图、商店、科技树、战利品限定建造库存。

使用、参数、资产规格及来源见 `Assets/Art/Salvage/README.md`。生成源 `SalvageAssetBaker` 默认仅创建缺失资源，不覆盖用户后续手调；独立渲染预览保存在 `Assets/Art/Salvage/Previews`。

## 4. 已确认实现的功能

- 主场景和 URP 项目配置可被 Unity 项目识别。
- 玩家存档与敌方蓝图存档的创建、加载、删除、重命名接口已存在。
- `Resources/Blocks` 提供多种尺寸方块、驾驶舱、推进器、悬浮控制器、机架、炮塔、维修机器人等 Prefab。
- 建造模式支持方块选择、高亮、Ghost 预览、网格吸附、碰撞阻挡、键盘移动、旋转、轴拖拽、复制和删除。
- 动作系统支持添加、移动、旋转、删除、组合操作的 Undo/Redo，并由 UI 显示计数。
- 方块连接点、邻居关系、连接/断开和连接器 Gizmos 已实现。
- 游玩模式会按控制单元聚合模块，并检查驾驶舱有效性。
- 无线供电调试支持无内部重叠面的立方体范围并集、状态着色和发电机/输电设备间的虚线 LineRenderer 连接，可由 DebugManager 开关控制。
- 主推进、全向推进、悬浮控制、推力分配及推力粒子/光效代码已存在。
- 敌人、陨石、炮塔、维修机器人和模块耐久相关脚本已纳入工程。
- 编辑器包含 Windows 构建入口和 Profiler 捕获分析入口。
- 20 个 `Resources/Blocks` Prefab 已使用共享工业 Mesh/材质替换占位渲染；功能件包含轻量 Transform 动画，当前全部禁用 LOD，并提供独立预览场景与可重复生成菜单。

- 已加入三种可编辑货仓、回收无人机舱、独立掉落物、金币汇聚、内容保存/返回及共享 Bot 导航；运行验证记录见 2026-09-22 变更日志。

## 5. 待改进与风险

- **2026-10-01 无目标悬浮下沉已修复**：EnemyController 无目标时原先每帧使用机体位置更新 targetHoverHeight，飞控无法追赶持续移动的高度设定点；即使把蓝图飞控放在根高度仍会下降。现改为在无目标状态开始时锁定飞控当前高度，再次获得目标时退出高度保持；由三种敌机的无目标及失去目标阶段回归检查验证，详见 Flight-summary.json。

- **已修复（2026-09-30）**：异常冲量记录器原先在遍历速度采样字典时更新字典值，Unity 会抛出 `InvalidOperationException` 并在每个物理步重复报错，造成日志刷屏和额外卡顿。现改用刚体快照遍历后再写回采样值；CSV 中的 PhysX 碰撞冲量仍保留为原始求解器数据，不能直接视为伤害值。

- **新增（2026-09-30）**：Editor Play Mode 自动启用 `AbnormalImpulseRecorder`，按刚体记录 PhysX 碰撞冲量和未归因速度突变；默认 `delta-v >= 8 m/s` 且等效冲量 `>= 250 N s` 时使用 `LogWarning` 提示，并写入 `Temp/AbnormalImpulseLogs/*.csv`。阈值与限频可在组件 Inspector 调整；仅用于编辑器测试，尚未替代目标硬件性能分析。

- **已修复（2026-09-30）**：DetachedSmoke 原先使用 `UNI_Device_Fire`，在小于 1 倍缩放时主要呈细火焰，低速断裂刚体还可能完全不越过速度门槛。现改用更厚的 `UNI_Steam_Leak` 烟团，Prefab Graph 缩放从 0.35 提高到 1.5，并加入 1.5 秒最低残烟与速度方向对齐；高并发断裂烟迹的透明 Overdraw 仍需目标硬件测量。

- **已修复（2026-09-30）**：瞬时 VFX 满 64 槽时，爆炸 Graph 曾被拒绝生成而点光仍闪烁；现优先清理旧非爆炸效果，全为爆炸时跳过新爆炸和点光。极端同帧超过 64 次爆炸时后续事件无爆炸视觉反馈；目标硬件上的并发 GPU/Overdraw 性能仍需专项测量。

- **已修复（2026-09-27）**：玩家 Cockpit 血条原 Filled Image 缺少 Sprite，导致数值变化但画面不裁切；已补资源引用并通过 uGUI 顶点宽度检查。大型战斗中血条可读性仍需实机观察。

- **已修复（2026-09-26）**：Bot 与爆炸最近点统一走 ImpactPhysics，非凸 MeshCollider/地形使用 bounds 回退；非凸探针通过。复杂凹模型回退为包围盒近似，精细避障效果仍需实机观察。
- **2026-09-23 蓝图设计观察**：固定逐块等待问题已由同日指数衰减加载改善（737 块约 14.92 秒）；旧蓝图报告中的 0.1 秒是原代码默认值，主场景原实际配置为 0.05 秒。连接重建与实例化开销仍待专项优化。不同体积装甲同为 100 耐久使小块密铺具有耐久优势且增加 CPU 负担；建造面板缺少失效后的升力/覆盖提示；自动生存对比需要固定种子、标准火力和实际受击/维修统计。其余建议见 `Blueprints/CODEX/README.md`，尚未实施平衡改造。

- **已解决（2026-09-23）**：禁用 Connector 被物理命中后错误合组，以及无视觉连接误拆组；54 项 Play Mode 探针通过。大型蓝图分组耗时和 GC 峰值尚未采样，不能据此量化帧率收益。

- **建造目录**：新增 Prefab 后需要执行 Build Palette Bake，将分类与图标保存入场景；不会在运行时自动复制按钮。尚未做超大目录性能或所有分辨率的手工操作测试。

- **战斗 HUD（2026-09-25）**：隔离 Play Mode 的九项逻辑检查通过，`Temp/CombatHud/EnemyNameplate.png` 与 `KillNotice.png` 已在探针结束后落盘并目视核对；真实大型蓝图战斗中的多名牌遮挡、各种分辨率和长名称排版仍需实机检查。

- **推进器 HUD（2026-09-25）**：隔离 Play Mode 探针扩展至 19 项并通过，覆盖 3D 遮挡隐藏/恢复、ControlUnit 总耐久、Canvas 飞行数据、F1 面板归属筛选和分类导航；`ThrusterInfoPanel.png` 已目视核对。正式构建和不同分辨率下的长列表滚动仍待验证；2026-09-30 已修复选中线初始尺寸异常，首次打开的视觉效果仍待 Play Mode 回归。

优先级含义：P0 阻断主流程，P1 影响核心体验或数据安全，P2 可维护性/性能，P3 体验增强。

| 优先级 | 问题或改进方向 | 建议 |
| --- | --- | --- |
| P1 | 自动化回归覆盖仍不完整 | 已新增 SalvagePlayProbe，覆盖货仓、掉落、真实回收/摧毁与维修回归；完整主场景战斗、建造输入与正式构建仍需覆盖。 |
| P2 | 大量透明货仓、并发无人机和长期特殊掉落的性能未测量 | 普通资源包有合并上限；仍需对 100+ 仓体、多人机拥挤绕障、移动母舰急转和大量特殊零件做压力验证。 |
| P2 | 回收首版使用原有结束游玩按钮结算 | 后续远征模式需加入撤离条件、局外仓库/经济和消耗规则；当前保留无限制沙盒建造。 |
| P1 | 输入逻辑分散在直接读取设备与 Input Actions 两种方式 | 统一 Input Action，集中处理设备缺失、重绑定和 UI 输入焦点。 |
| P1 | 存档写入仍需关注中断、损坏和版本升级 | 使用临时文件+替换、JSON schema/version 字段、损坏存档备份和迁移策略。 |
| P1 | 运行时大量依赖单例和 Inspector 引用 | 增加启动依赖检查、缺失引用的用户提示，并逐步将纯逻辑从 MonoBehaviour 解耦。 |
| P2 | `Resources.Load` 和逐个 Instantiate 在大蓝图下会造成加载峰值 | 建立 Prefab 注册表或 Addressables，批量/异步加载并复用对象。 |
| P2 | 方块连接和阻挡检查依赖 Physics 查询 | 建立网格占用索引，旋转/删除时增量更新，减少全场景扫描。 |
| P2 | 推进器求解器缺少运行时可观测性 | 输出目标力矩、残差、饱和推进器数量和求解耗时，便于调参和性能分析。 |
| P2 | 物理预算需要按目标设备调校 | 当前固定步长约 50 Hz、默认位置求解 4 次、追赶上限 0.1 秒；若出现高速穿透、堆叠抖动或重载时模拟变慢，应针对 Rigidbody 的碰撞检测、质量和局部求解迭代单独调参。 |
| P2 | 无线供电网络尚未经过 Play Mode 压力验证 | 需验证移动发电机/中继、跨网覆盖、运行时销毁、零负载与大量 Power Block 下的分配正确性和每帧重建开销。 |
| P2 | 供电调试线和范围显示依赖运行时动态材质/网格/子对象 | 需在 URP 下验证虚线纹理、并集透明度、不同状态交界和大量连接时的可读性；坐标压缩网格的单次构建规模随不同范围边界数量增长，需压力验证大量移动输电设备；调试开关关闭时应确认所有运行时 Renderer 已禁用。 |
| P2 | Block 状态图标在大型蓝图中会产生每个异常 Block 一组运行时 uGUI Image | 当前状态检查降频到 0.1 秒且 Normal 不创建视觉对象；仍需用 500/1000 Block、多个异常状态和不同分辨率验证 CPU、Canvas rebuild、图标重叠和可读性。 |
| P2 | EnemyController 的 AI 输入平滑参数仍需 Play Mode 调校 | 根据敌我距离、载具规模和目标帧率调节 `movementUpdateInterval` 与 `movementResponseRate`。 |
| P2 | Block 爆炸当前仅实现范围断开、分组、物理冲量和粒子反馈 | 后续可在爆炸中心加入按距离衰减的伤害，并补充断开概率、冲量和半径的 Play Mode 调参记录。 |
| P2 | 多个 RepairBot、放大的多喷口尾焰、连续大型爆炸和高速断裂部件会叠加透明 VFX 开销 | 瞬时效果上限 64、断裂烟迹上限 24；仍需在大型蓝图战斗中记录 GPU Overdraw、Batches 和主线程峰值。 |
| P2 | UI 同时存在 uGUI 与 IMGUI | 将诊断面板迁移到统一 UI 系统，避免分辨率、输入焦点和生命周期不一致。 |
| P2 | 历史日志包含旧版本功能描述 | 每次发布标记版本和验证日期，避免把日志中的“计划/旧实现”当作当前契约。 |
| P2 | SpaceKit 集成功能模块尚未在大型蓝图中完成 GPU/CPU 压力验证 | 使用 100、500、1000 模块蓝图记录 Batches、SetPass、粒子 Overdraw、动态灯和脚本耗时。 |
| P2 | 双轴炮塔尚未完成主场景战斗回归 | 在不同安装朝向、移动载具和高低目标下验证索敌、遮挡、俯仰边界、光束起点、命中判定与断电恢复。 |
| P2 | 原生 Graph 高并发 GPU/Overdraw 预算 | 已替换全部备用 Shuriken；保留原生 UNI 品质及并发实例限制。复杂战斗性能和正式构建尚未验证。 |
| 已解决 | 手动替换后的炮口引用旧模型、全向模型整体旋转及粒子方向错位 | 2026-09-21：保存新挂点/运动轴，Unity 引用检查与旋转推进、射击 Play Mode 探针验证。 |
| 已解决 | 尾焰短寿命加密度调强度导致低推力频繁断续；部分素材离屏冻结 | 2026-09-21：核心交叉淡化、持续效果材质透明度调节、AlwaysSimulate；105 组时序采样无空帧，23 项 Play Mode 检查通过。 |
| P3 | 仓库仍保留未被新模块视觉引用的 `New Material` 等历史资源 | 确认场景和旧 Prefab 无引用后再分批清理，避免误删用户资源。 |
| P3 | 缺少正式构建产物验收记录 | 记录目标平台、构建版本、场景、输入设备、帧率和已知缺陷。 |

## 6. 推荐验证清单

每次涉及核心逻辑时至少执行：

1. Unity Console 无新增 Error/Exception。
2. 新建存档 -> 放置方块 -> 保存 -> 重启/重新加载，位置、旋转、资源路径一致。
3. 连接点吸附、阻挡、删除断开、Undo/Redo 各操作至少执行一次。
4. 有效驾驶舱进入和退出游玩模式；无驾驶舱时确认阻止进入并给出提示。
5. 主推进、全向推进、悬浮控制及推力视觉在 Play Mode 下工作。
6. 发电机与中继在连接距离内正确组网；移动、断连和销毁后功率及时更新，范围内负载均分且范围外负载归零。
7. 大蓝图加载、取消加载、切换存档时无重复对象或残留引用。
8. `git diff --check` 通过，且只提交当前任务相关文件。

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

#### `Assets/Editor/SpaceKitCurator.cs`

| 函数 | 职责 |
| --- | --- |
| `CreateLibrary()` | 读取 Selection、解析依赖、复制并生成独立 GUID、重映射引用、转换材质、保存 Catalog；拒绝覆盖已存在库。 |
| `IsSource(string)` / `Destination(string, Dictionary<string, string>)` | 限定两套源包并按用途/类型映射目录。 |
| `EnsureFolder(string)` / `Hash(string)` | 通过 AssetDatabase 建目录；计算来源 SHA-256。 |
| `Remap(Object, Dictionary<Object, Object>)` | 通过 SerializedObject 重定向复制资产引用。 |
| `ConvertMaterial(Material)` | 将审核过的 Standard/旧粒子/飞船/陨石材质转换为 URP。 |
| `CleanLegacyArtifacts(GameObject)` / `RepairLegacyImportArtifacts()` | 清理副本无用 Animator 与粒子 mesh 残留，并验证。 |
| `ValidateLibrary()` | 重载资产，检查依赖、GUID、缺失引用、Shader，统计几何/粒子预算并写入 Validation。 |
| `RenderAllPreviews()` / `RenderPreviews(int, int)` | 在独立临时预览场景用 URP Camera 渲染，不触及当前用户场景。 |

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

#### `Assets/Editor/IndustrialArtGenerator.cs`

- `public static void RebuildAll()`：生成共享工业风格 Mesh、材质、渲染配置、全部模块视觉层和预览场景。
- `public static void RebuildTurret()`：只重建炮塔双轴模型并刷新预览场景，避免改炮塔时重写其他模块 Prefab。
- `private static string[] GetBlockPrefabPaths()`：按稳定顺序返回预览场景和全量重建使用的模块 Prefab 路径。
- `private static void RebuildConnectorPrefab()`：在保留 Connector Prefab 根对象和 GUID 的前提下重建轴对称工业连接接头及信号环动画。
- `private static bool RebuildPrefab(string prefabPath)`：在 Prefab 隔离阶段替换占位视觉并保持根组件、碰撞体、连接点和嵌套模块。
- `private static void ConfigureRenderStyle()`：配置 PC URP 的 MSAA、阴影距离和 Volume Profile 的 ACES/Bloom/色彩参数。
- `private static void CreatePreviewScene(IReadOnlyList<string> prefabPaths)`：创建工业美术展示场景、灯光、相机和截图用资产。


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
- `public bool IsConnectorAvailableForPlacement(Connector connector)`：检查连接点自身状态及对面是否已有实体方块，统一建造预览和放置的可用性判定。
- `public void CheckConnection()`：同步物理变换，双向清理旧连接引用并重建未占用的连接点，同时刷新双方状态图标。
- `private void ReleaseExistingConnections()`：清理当前 Block 持有的共享连接器，并移除场景中两侧的引用。
- `private void ReleaseConnectionPairWithoutVisual(Connector connector)`：没有连接器视觉对象时，按位置清理对端逻辑连接。
- `private void ReleaseConnectionReferences(GameObject connectorObject)`：清理指定共享连接器在所有 Block 上的引用并刷新状态。
- `private Block FindBlockAcrossConnector(Connector connector)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Connector FindMatchingConnector(Block otherBlock, Connector connector, bool requireAvailable = true)`：查询位置、法线和占用状态，返回匹配连接点。
- `private void ConnectTo(Connector connector, Block otherBlock, Connector otherConnector)`：建立双侧连接状态和可选的连接器视觉对象，并刷新双方状态。
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

#### `Assets/Scripts/Block/Info.cs`

- `private void Awake()`：缓存当前 Block 的连接、耐久和用电组件。
- `private void LateUpdate()`：仅在调试管理器变化时刷新图标显示，并每帧把可见图标投影到屏幕位置；状态值由对应组件主动通知更新。
- `public void CheckConnectionStatus()`：首次启用或重新启用时保持 Normal，完成首次连接刷新后只要存在一个已连接 Connector 即为 Normal，否则为 NoConnection；无连接点的对象保持 Normal。
- `public void CheckDurabilityStatus()`：按满耐久、受损和归零更新 Normal、Damaged、Broken。
- `public void CheckPowerStatus()`：无 Power 时为 Normal；低于最小工作功率为 NoPower，达到工作门槛但效率不足为 UnderPower。
- `private void RefreshIcons(DebugManager manager)`：按三项 DebugManager 开关组合非 Normal 图标。
- `private void EnsureIconVisuals(DebugManager manager)` / `CreateIcon(string)`：按需创建屏幕空间图标容器与三个 Image，不修改 Prefab 层级。
- `private void UpdateIconScreenPosition(DebugManager manager)`：将图标定位到 Block 的 `Center`、朝向相机并执行视口可见性检查。

#### `Assets/Scripts/Block/Power.cs`

- `private void OnEnable()`：注册当前启用的用电 Block，供无线网络统一发现和分配功率。
- `private void OnDisable()`：注销用电 Block，并清除停用前残留功率。
- `public void ResetPower()`：在网络重算前清空当前供电。
- `public void ReceivePower(float suppliedPower)`：累加一个无线网络提供的非负功率。


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
- `public void StartContinuousOrbitCameraAroundBounds(Bounds frameBounds, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`：围绕调用方提供的固定逻辑包围盒持续环绕，避免异步实例化过程改变取景范围。
- `public void StopCameraMotion()`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `private IEnumerator SmoothFocusRoutine(Vector3 targetPosition, Quaternion targetRotation, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `private IEnumerator ContinuousOrbitRoutine(GameObject frameObj, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private IEnumerator ContinuousOrbitRoutine(Bounds frameBounds, Vector3 orbitCenter, float startYawDegrees, float orbitDegreesPerSecond, float pitchDegrees, float radiusVariation, float radiusWaveDegrees, float radiusSmoothTime)`：使用稳定的存档逻辑范围驱动加载环绕。
- `private float CalculateOrbitRadiusMultiplier(float yawDegrees, float radiusVariation, float radiusWaveDegrees)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private IEnumerator SmoothOrbitRoutine(Vector3 orbitCenter, float targetYaw, float targetPitch, float targetRadius, float duration)`： 计算或执行相机聚焦、平滑移动和环绕控制。
- `private void SetOrbitPose(Vector3 orbitCenter, float yawDegrees, float pitchDegrees, float radius)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private Vector3 GetOrbitCameraDirection(float yawDegrees, float pitchDegrees)`：把环绕角转换为从观察中心指向相机的单位方向。
- `private bool TryGetFocusPose(GameObject obj, out Vector3 targetPosition, out Quaternion targetRotation)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool TryGetFocusPose(Bounds frameBounds, Vector3 lookPoint, out Vector3 targetPosition, out Quaternion targetRotation)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool TryGetOrbitPose(Bounds frameBounds, float yawDegrees, float pitchDegrees, float radiusMultiplier, out Vector3 targetPosition, out Quaternion targetRotation)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private float CalculateFramingDistance(Bounds bounds, Vector3 lookPoint, Vector3 cameraDirection)`：按当前视角逐角点计算满足水平、垂直视锥约束的最小取景距离。
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


### Block Art 集成工具与素材控制器

- `BlockArtDependencies.Resolve()`：通过 Editor API 复制缺失依赖、重映射 GUID/subasset 引用并完全解包。
- `BlockVfxBaker.BakeMissing()` / `Populate()`：从原生和补充 Graph 生成缺失模板，已有模板不覆盖。
- `NativeVfxLibraryBuilder.PrepareSources()` / `ImportSources()` / `CreateUtilityGraphs()` / `RebuildPositionSafeEffects()`：来源清单、依赖重映射、能量图生成及无烟/局部空间 Graph 重建。
- `VfxGraphAuthoring`：隔离 Unity 6000.3 内部 Graph Editor API，产物为正常可编辑 .vfx 资产，运行时无反射。
- `NativeVfxMigration.Run()` / `Validate()`：Editor API 迁移、绑定调用者，验证遗留组件和依赖。
- `NativeVfxPlaybackProbe.Run()` / `RunSuite()` / `RunPositionTest()` / `RunBlockTest()`：实际 Play Mode 自动渲染 GPU 生命周期、大坐标移动和 Block 喷口采样，结束恢复原场景。
- `BlockArtIntegrator.Integrate()` / `MatchThrusterFlamesToHover()`：保留模型与玩法契约，补齐挂点、灯光和 Graph 绑定，并迁移四种推进器喷口模板。
- `BlockArtValidation.Validate()`：检查引用、Shader、运动链与挂点；`BlockArtPreview.Render()` 只渲染静态模型。
- `BlockArtPlayProbe.Run()`：隔离 Play Mode 检查供电/推进/射击/维修与回收。
- `VfxEffect.SetIntensity()`：统一 Graph 强度、实例缩放和启停；空间变换由各 Graph 模拟空间处理。
- `VfxEffect.PlayOnce()` / `Clear()` / `ReleaseAfterPlayback()` / `StopAndRelease()`：重播、清空、实例上限及延迟回收。
- `VfxEffect.SetColor()` / `SetSpawnCount()`：设置能量反馈颜色与掉落数量。
- `BlockVfxLibrary.Play()`：实例化事件效果并返回生成或跳过原因，Beam 和 DetachedSmoke 提供持续模板。
- `BlockStatusLight.Update()`：更新发电机与维修舱状态灯。

### Effect 特效

#### `Assets/Scripts/Effect/DetachedPartSmokeTrail.cs`

- `public static void Attach(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`：为无驾驶舱断裂刚体挂接或刷新受全局数量限制的烟雾拖尾。
- `private void Initialize(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`：实例化 UNI Device Fire 局部空间烟火 Graph，并缓存目标刚体。
- `private void Refresh(Vector3 worldAnchor, float effectIntensity)`：重复受爆时刷新锚点、强度和剩余寿命，不重复创建组件。
- `private void Update()`：按线速度、角速度、爆炸强度和生命周期衰减实时控制发射。
- `private void StopAndRelease()`：停止发射、分离残留粒子并延迟销毁视觉对象。
- `private void OnDestroy()`：释放全局活动拖尾计数。

#### `Assets/Scripts/Effect/IndustrialPartMotion.cs`

- `private void Update()`：低频率更新功能件旋转、浮动和自发光脉冲，不修改物理或存档状态。
- `public void Configure(Transform[] newSpinTargets, Vector3 newSpinAxis, float newDegreesPerSecond, Transform newBobTarget, float newBobAmplitude, float newBobFrequency, Renderer[] newGlowRenderers, Color newBaseEmission, float newEmissionPulse)`：为生成的视觉层注入动画目标和共享材质参数。

#### `Assets/Scripts/Effect/StylizedBeamEffect.cs`

- `Configure()` / `SetEndpoints()`：将玩法宽度和世界端点映射到 GPU 网格粒子的 Transform。
- `SetColor()` / `SetIntensity()` / `SetVisible()`：驱动 Graph 颜色、强度和启停。
- `UpdateBeam()` / `OnDisable()`：同步位置/方向/长度，在禁用时清空 Graph。

### InObject 模块

#### `Assets/Scripts/InObject/ControlUnit.cs`

- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public bool HasAnyCockpit`：判断 ControlUnit 当前是否包含至少一个 Cockpit，供分组上限清理保护有效组。
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

#### `Assets/Scripts/InObject/PowerGeneratingUnit.cs`

- `private void OnEnable()`：注册启用的发电单元，供无线网络发现。
- `private void OnDisable()`：注销发电单元并移除现有设备反向连接。

#### `Assets/Scripts/InObject/PowerTransmissionDevice.cs`

- `private void Awake()`：在 Unity 生命周期内创建连接线共用的 MaterialPropertyBlock，避免在 MonoBehaviour 构造阶段调用原生渲染 API。
- `private void OnEnable()`：注册启用的输电设备。
- `private void Update()`：每个渲染帧只触发一次全局无线网络刷新。
- `private void OnDisable()`：注销并断开设备；最后一个设备停用时清空所有负载功率。
- `private void Disconnect()`：移除该设备与发电机、其他设备之间的双向连接。
- `private static void RefreshPowerNetwork()`：依次重建缓存、连接和网络功率分配。
- `private static void RebuildBuffers()`：收集当前启用的发电机、输电设备和 Power 负载。
- `private static void ResetConnectionsAndPower()`：清空上一帧连接数据和负载功率。
- `private static void BuildConnections()`：按最大连接距离建立发电机/设备和设备/设备双向连接。
- `private static void DistributeNetworkPower()`：遍历包含发电机节点的连通网络，汇总输出并对覆盖负载均分。
- `private static void CollectDeviceLoads(PowerTransmissionDevice device)`：收集单个设备世界坐标轴对齐立方体供电范围内的 Power 负载并去重。
- `private static bool IsWithinBoxRange(Vector3 firstPosition, Vector3 secondPosition, float range)`：逐轴比较两个世界坐标位置是否位于指定半边长的轴对齐立方体内。
- `private static void ResetAllPowerBlocks()`：在没有输电设备时清除所有残余供电。
- `private void CacheDebugReferences()`：缓存旧调试 Cube 的 Renderer，供全局并集网格复制透明材质。
- `internal Material DebugRangeMaterial`：向 DebugManager 提供旧调试 Cube 的共享材质作为并集透明材质模板。
- `private void UpdateDebugVisuals()`：隐藏旧单体范围 Cube，并同步连接虚线。
- `private void UpdatePowerRangeVisual()`：先将旧单体范围 Cube 的世界旋转同步到输电设备，再保持其隐藏，避免与全局并集网格重复绘制。
- `private void UpdateConnectionLines()`：为当前连通的发电机和相邻输电设备更新去重后的连接线。
- `private void DrawDashedConnection(int index, Vector3 start, Vector3 end, Color color)`：设置单条连接线端点、颜色、宽度和虚线滚动参数。
- `private void EnsureDebugProperties()`：按需创建连接线 MaterialPropertyBlock，兼容脚本热重载或异常初始化状态。
- `private LineRenderer GetOrCreateConnectionLine(int index)`：复用或创建设备拥有的连接线对象。
- `private static void ConfigureConnectionLine(LineRenderer connectionLine)`：配置世界空间、纹理拉伸、透明材质和阴影设置。
- `private static Material GetDashedLineMaterial()`：创建并缓存运行时虚线材质。
- `private static Texture2D CreateDashTexture()`：创建可重复采样的半透明虚线纹理。
- `private void SetConnectionLinesVisible(bool visible)`：批量切换连接线显示状态。
- `private void OnDrawGizmosSelected()`：在编辑器中显示供电范围和最大连接距离。

#### `Assets/Scripts/InObject/RepairBot.cs`

维修目标选择、有效性与范围校验、维修效果；移动委托 Bot。

- `protected override void Start()`
- `private void InitializeComponents()`
- `private void FixedUpdate()`
- `protected override void OnDisable()`
- `private void InitializeTargetsInRange()`
- `private void FindDamagedBlock()`
- `private bool IsValidRepairTarget(Durability target)`
- `private void CheckAndRepair()`
- `private void UpdateRepairBeam(bool active)`
- `private Vector3 GetRepairTargetPoint()`
- `private void EnsureRepairImpactVfx()`
- `private void UpdateRepairImpact(Vector3 targetPoint, Color color, float pulse)`
- `private void SetRepairImpactActive(bool active)`
- `private void EnsureRepairBeamGradient()`
- `public void ClearTarget()`

#### `Assets/Scripts/InObject/TurretWeapon.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnValidate()`：约束水平/垂直转速与俯仰角范围，防止 Inspector 输入无效配置。
- `private void ResolveAimingRig()`：优先解析生成的 `Horizontal/Vertical/Muzzle` 层级，并为旧 Prefab 保留单轴回退。
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

#### `Assets/Scripts/Manager/DebugManager.cs`

- `private void Awake()`：注册全局调试显示管理器。
- `private void OnDestroy()`：仅在当前实例销毁时清理静态引用。
- `public Color GetPowerRangeColor(PowerTransmissionDevice device)`：根据设备网络连接和可用功率返回范围显示颜色。
- `public void TogglePowerRange()`：切换供电范围调试显示。
- `public void TogglePowerConnections()`：切换供电连接虚线显示。
- `public void ToggleConnectionStatus()` / `ToggleDurabilityStatus()` / `TogglePowerStatus()`：分别切换 Block 连接、耐久和供电异常图标。
- `IconManager.Register/Unregister`：注册和注销运行时状态图标，并统一驱动 `sin(Time.unscaledTime)` 透明度。
- `internal void RefreshPowerRangeMesh(IList<PowerTransmissionDevice> devices)`：在范围调试开启时按设备快照签名决定是否重建并集网格。
- `internal void ClearPowerRangeMesh()`：最后一个输电设备停用时清空并隐藏并集网格。
- `private void EnsurePowerRangeObject()`：创建运行时 MeshFilter、MeshRenderer 和支持 32 位索引的动态 Mesh。
- `private void BuildPowerRangeMesh(IList<PowerTransmissionDevice> devices)`：坐标压缩所有立方体边界，标记覆盖状态并只生成并集外表面。
- `private void CollectCoordinates(IList<PowerTransmissionDevice> devices)`：收集并去重所有有效范围的世界坐标轴边界。
- `private void AddXFace(int x, int y, int z, bool positive, int materialIndex)`：生成并集外壳的 X 轴面。
- `private void AddYFace(int x, int y, int z, bool positive, int materialIndex)`：生成并集外壳的 Y 轴面。
- `private void AddZFace(int x, int y, int z, bool positive, int materialIndex)`：生成并集外壳的 Z 轴面。
- `private void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int materialIndex, bool reverse)`：向指定状态子网格追加正确绕序的四边形。
- `private void EnsurePowerRangeMaterials(IList<PowerTransmissionDevice> devices)`：复制旧范围透明材质并设置孤立、已连接和有功率三种颜色。
- `private static int CalculatePowerRangeSignature(IList<PowerTransmissionDevice> devices)`：汇总设备、位置、范围和网络状态，避免状态未变时重复生成网格。
- `private static int GetPowerState(PowerTransmissionDevice device)`：返回用于重叠范围优先级和子网格材质的设备状态。
- `private static int FindCoordinateIndex(List<float> values, float target)`：在压缩坐标中定位范围边界并容忍微小浮点误差。
- `private static void SortAndUnique(List<float> values)`：排序并合并近似相同的范围边界。
- `private void SetPowerRangeMeshVisible(bool visible)`：按调试开关和网格有效性切换并集对象。

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
- `public void SetSelectType(SelectType)` / `RefreshBuildGizmos()` / `HideBuildGizmos()`：互斥切换 Select、Move、Rotate 状态并同步当前 Gizmo 可见性。
- `void HandleDuplicate(Vector3 newPos, Quaternion newRot)`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `private void HandleBuildingPreview()`：拦截 UI 输入、筛选可用连接点、刷新线框与 Ghost；无连接点时清理，穿透搜索最多 64 步避免无限循环。
- `public void CreateBlock(GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `public void DeleteBlock()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void SaveBlock(Block block)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private void RefreshConnectionsAround(Block block)`：同步物理变换，收集尺寸包围盒附近的 Block 并去重重建连接和邻居。
- `public void RemoveBlock(Block block)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void LoadAllBlocks()`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private IEnumerator LoadAllBlocksRoutine(int loadVersion, string loadSavePath, Transform loadParent, List<BlockData> blocksToLoad, double time0)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public void ClearUnloadableData(string id)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public bool IsLoadingBuildTarget(BuildTargetKind kind, string saveName, string enemyBlueprintName)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool IsLoadingBuildTarget(BuildTargetContext context)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void ClearUnloadableData(string id, string targetSavePath)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `void InitialBlock()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `public Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private Vector3 GetBlockHalfExtents(Block block)`：按 Block 尺寸和 gridSize 返回局部半尺寸。
- `private float GetHalfExtentAlongDirection(Block block, Quaternion rotation, Vector3 worldDirection)`：计算旋转方块沿世界方向的支撑半尺寸。
- `private bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)`：按缩放后的方块包围盒查询实体阻挡。
- `float GetMoveStep(Block block, Vector3 moveDir)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool CanCreateBlock(GameObject prefab, out string reason)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public void ApplyBlockBuildDefaults(Block block)`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private int CountCockpitsInCurrentConstruct()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void ResetBuildState()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void SetBuildTarget(BuildTargetContext context)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void RefreshBlueprintUI()`：在建造数据变化或加载完成后刷新当前蓝图名称、方块数量和总质量。
- `private void StopActiveBlockLoad()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private bool IsCurrentBlockLoad(int loadVersion, string loadSavePath, Transform loadParent)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void AbortBlockLoadIfCurrent(int loadVersion, Transform loadParent)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearLoadingBuildTarget()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void StartLoadingCameraOrbit(GameObject frameObject, int blockCount)`：达到方块阈值时使用完整存档逻辑包围盒启动加载环绕。
- `private float CalculateLoadingCameraOrbitDegreesPerSecond(int blockCount)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void StopLoadingCameraOrbit()`：停止加载镜头协程，并保留围绕新构造体的最终取景姿态。
- `private CameraController GetMainCameraController()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private float GetCameraOrbitAngle(Vector3 orbitCenter)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private bool TryCalculateLoadingCameraOrbitBounds(List<BlockData> blocksToLoad, Vector3 fallbackCenter, out Bounds bounds)`：从存档中全部方块的数据合并加载镜头逻辑包围盒。
- `private Bounds CalculateBlockDataBounds(BlockData data)`：根据方块尺寸、世界位置和四元数旋转计算单个存档方块的轴对齐包围盒。
- `private void ClearCurrentGhost()`：无论 Ghost 是否存在，都清理连接提示及 hoveredConnector，再销毁预览。
- `private void OnDisable()`：禁用 BuildManager 时清理 Ghost 和连接提示。
- `private bool RemoveCachedBlockData(string id)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private bool RemoveCachedBlockData(string id, string targetSavePath)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void WriteCachedData()`： 执行存档/文件的读取、写入、重命名或路径处理。
- `private void WriteCachedData(string targetSavePath)`： 执行存档/文件的读取、写入、重命名或路径处理。
- `public static string ConvertToResourcesPath(string fullPath)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。

#### `Assets/Scripts/Manager/DestroyManager.cs`

- `public void EndSalvageSession()`：取消延迟清理任务并清空会话标记，防止跨次游玩结算。

- `public void DestroyGameObject(GameObject obj)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `public void ExplodeBlock(Block block)`： 处理 Block 爆炸、重新分组和物理冲量，不直接造成伤害。
- `private void DisconnectBlocksInExplosionRange(ControlUnit unit, Block sourceBlock, Vector3 explosionPosition)`： 按爆炸半径和概率断开范围内 Block 的连接器。
- `private void ApplyExplosionForce(Block block, string ownerUnitId, UnitFaction ownerFaction, Vector3 explosionPosition)`：按爆炸范围内 Collider 和编组子方块收集 Rigidbody，并依据最近受击点施加衰减冲量。
- `private void ScheduleUnitCleanup(string ownerUnitId, UnitFaction ownerFaction)`： 启动、准备或调度对应的生成、模式切换、刷新或事件流程。
- `public void ScheduleUnitCleanup(ControlUnit unit)`：延迟调度无 Cockpit ControlUnit 的 Group 清理。
- `public void ScheduleDistantGroupCleanup(ControlUnit unit, Transform reference, float maxDistance)`：延迟调度距离参考点过远的 Group 清理。
- `private IEnumerator CleanupGroupAfterDelay(ControlUnit unit, string ownerUnitId)`：延迟确认 Group 仍无 Cockpit 后销毁其根对象。
- `private IEnumerator CleanupDistantGroupAfterDelay(ControlUnit unit, string ownerUnitId, Transform reference, float maxDistanceSqr)`：延迟确认 Group 仍在距离阈值外后销毁其根对象。
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
- `private void EnforceControlUnitGroupLimit()`：统计场景 Group 数量，超出上限时调度无 Cockpit Group 清理。
- `private void EnforceDistantGroupCleanup()`：以玩家 `blocksParent` 为参考，调度超过距离阈值的非玩家 Group 清理。
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
- `public static Material GetSharedLineMaterial()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `public static void TryPlayBlockPlaced(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryPlayBlockRemoved(Block block)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `public static void TryPlayBlockExplosion(Block block)`：触发放大的 UNI Aerial Explosion Graph、瞬时灯光和镜头反馈。
- `public static void TryPlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`：在真实维修 tick 时触发收束线、目标脉冲、火花和小型闪光。
- `public static void TryAttachDetachedPartSmoke(Rigidbody body, Vector3 worldAnchor, float intensity)`：为爆炸后无驾驶舱的刚体挂接烟雾拖尾。
- `public static void TryPlayObjectDestroyed(GameObject target)`：触发普通物体销毁的 UNI Aerial Explosion Graph 和短闪光。
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
- `private void ApplySceneLook()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private void PlayBlockPlaced(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockRemoved(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockExplosion(Block block)`：在 Block 渲染边界中心播放放大的 UNI 原生爆炸，记录生成状态，并附加瞬时点光与镜头震动。
- `private void PlayObjectDestroyed(GameObject target)`：在目标边界中心播放 UNI 原生爆炸，记录生成状态，并附加短闪光。
- `private void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`：组合一次维修命中的加法火花、放射光痕、短束流和闪光。
- `private void PlayBlockMoved(Block block, Vector3 from, Vector3 to)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockRotated(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void ShowBlockSelection(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void ClearBlockSelection(Block block)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void UpdateGhostPreview(Transform ghost, bool isBlocked)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearGhostPreview()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void DecorateMeteor(Meteor meteor)`：建立不继承陨石自转的独立烟火尾迹和随陨石移动的点光。
- `MeteorTrailFollower.Initialize()` / `LateUpdate()` / `Follow()`：尾迹根对象只跟随陨石位置，按刚体速度定向，并在来源消失时停止释放。
- `private void PlayMeteorImpact(Vector3 position, Vector3 normal, float scale, float speed)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void CreateLineStreak(Vector3 from, Vector3 to, Color color, float duration, float width)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void CreateRadialStreakBurst(Vector3 center, Vector3 normal, Color color, int count, float length, float duration, float width)`：在球面或指定半球生成受控数量的放射能量光痕。
- `private void CreateLightFlash(Vector3 position, Color color, float intensity, float range, float duration)`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private void ShakeCamera(float amplitude, float duration)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private IEnumerator CameraShakeRoutine(float amplitude, float duration)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearCameraOffset()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private static Bounds GetBounds(GameObject target, Vector3 fallbackCenter, Vector3 fallbackSize)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static Vector3 GetBlockSize(Block block)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private static Gradient MakeGradient(Color start, Color end)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static Color WithAlpha(Color color, float alpha)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private static Color Brighten(Color color, float intensity, float whiteBlend = 0f)`：生成保持 Alpha 的 HDR 高亮颜色。
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
- `private void ClearHoverThrust()`：控制器断电时立即清空全部悬浮推进器的当前和历史推力。
- `private bool EnsureControllerReady()`： 创建或补齐该功能所需的对象、引用、缓存和初始状态。
- `private void RefreshCachedPhysicsValues()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void RefreshTiltLimitCache()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void RebuildThrusterCache()`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private float CalculateHeightAdjustment(float heightError)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void UpdateDynamicHeightP(float heightError)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private float CalculateGravityCompensation(float absHeightError)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void CalculateTiltAdjustment(Vector3 currentUp)`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。
- `private void DistributeThrust(float heightAdjustment, Vector3 currentUp)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ApplyRotationCorrection(Vector3 currentUp, float outputEfficiency)`：按控制器供电效率缩放并应用姿态角速度修正。
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
- `protected bool CanApplyThrust()`：检查推进器自身 Power 是否达到工作阈值。
- `private float GetPowerEfficiency()`：读取推进器自身有效供电效率，断电或缺少 Power 时返回零。
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

- `Initialize(Thruster)`：缓存推进器及 Power。
- `SetThrust(float, Vector3)`：设置推力目标；喷口方向由模型层级负责。
- `LateUpdate()` / `OnDisable()`：平滑发射量，并在停机、失电、禁用时停发。


#### `Assets/Scripts/Thrusters/UniversalThruster.cs`

- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void RotateThruster(Vector3 worldDir, bool active)`： 修改模块或方块的旋转/位置，并同步相关运行时数据。
- `public void ApplyThrust()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `public override bool ShouldActivate()`： 查询或计算辅助函数：读取运行时状态，执行校验、几何或数值计算，并返回结果。


### UI 界面

#### `Assets/Scripts/UI/BlueprintUIPanel.cs`

- `private void Awake()`：注册场景中的蓝图信息面板实例。
- `private void Start()`：场景启动后按当前建造目标初始化显示。
- `private void OnDestroy()`：面板销毁时清理静态实例引用。
- `public void Refresh()`：根据当前建造目标、缓存方块 ID 和已加载 Block 刷新名称、数量、总质量、需求功率和发电机输出。
- `public void UpdateCurrentSaveName(string newName)`：更新当前玩家存档或敌方蓝图名称。
- `public void UpdateStatistics(int blockCount, float mass, float requiredPower, float generatorOutput)`：同时更新四项数值，用于加载协程的增量进度显示。
- `public void UpdateTotalNumber(int newNumber)`：更新当前蓝图方块数量。
- `public void UpdateTotalMass(float newMass)`：更新当前蓝图总质量。
- `public void UpdateTotalRequiredPower(float newRequiredPower)`：更新所有用电 Block 的标准功率总需求。
- `public void UpdateTotalGeneratorOutput(float newGeneratorOutput)`：更新所有发电机的非负输出总和。

#### `Assets/Scripts/UI/ActionCounterUI.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void FixedUpdate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void UpdateUndoText(int count)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `public void UpdateRedoText(int count)`： 触发游玩流程、UI 状态或视觉反馈的更新。

#### `Assets/Scripts/UI/GlobalTextStyler.cs`

- `private static void CreateInstance()`： 创建几何、资源、操作记录、UI 项或运行时对象。
- `private IEnumerator Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private static void ApplyToSceneTexts()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。

#### 建造目录与连接提示（2026-09-22）

- `BuildPalette.Awake()` / `OnEnable()` / `OnDisable()`：缓存已保存的条目、显示初始分类、隐藏名称提示。
- `BuildPalette.SelectCategory(int)`：切换条目可见性、导航颜色并将内容滚动回顶部。
- `BuildPalette.LateUpdate()`：资源选择变化时更新选中边框。
- `BuildPalette.ShowTooltip(BuildPaletteItem)` / `HideTooltip(BuildPaletteItem)`：同步显示/隐藏悬停名称与参数信息。
- `BuildPalette.GetSelectedItem()`：返回当前选中目录条目，供指针离开时恢复显示。
- `Block.GetDisplayInfo()` / `GenerateInfo()`：生成可烘焙的静态参数文字，并保留显式手写信息。
- `BuildPaletteItem.SetSelected(bool, Color)`：更新边框颜色。
- `BuildPaletteItem.OnPointerEnter/Exit` / `OnSelect/Deselect` / `OnDisable`：鼠标、键盘焦点及禁用时的名称提示生命周期。
- `ConnectorPlacementHints.Show(Camera, float, LayerMask, GameObject)` / `Hide()` / `OnDisable()`：设置范围扫描上下文或清理提示状态。
- `ConnectorPlacementHints.ScanNearbyBlocks()` / `CanDisplay(Block, Connector)`：扫描相机附近方块，并复用 Block 的可放置连接点判定。
- `ConnectorPlacementHints.LateUpdate()`：过滤禁用/占用点，以共享 Mesh/Material 按世界坐标和法线提交线框。
- `BuildPaletteBaker.Bake()`：通过 Unity API 刷新 Main 的保存目录、透明图标、Mesh/Material 与组件引用。
- `BuildPaletteBaker.RefreshHoveredBlockInfo()`：只刷新 Main 场景悬停文字引用和 23 个条目参数，保留现有文字布局。
- `BuildPaletteBaker.ConfigureThumbnail(GameObject)`：只给渲染克隆填充金币/科技资源，保持真实初始库存。
- `BuildPaletteBaker.Category(string)` / `Child(...)` / `Rect(...)` / `Label(...)` / `Set(...)`：默认分类和可编辑 uGUI 资产布局/绑定。
- `BuildPaletteBaker.ImportSprite(string)` / `CreateBorder()` / `CreateHintAssets(...)`：Sprite 导入、九宫格边框和世界线框资产制作。
- `BuildPalettePlayProbe.Run()` / `StateChanged(...)` / `Scenario()` / `Tick()` / `Finish()`：运行实际 Main UI 与连接提示回归，报告与截图保存到 Temp/BuildPalette。
- `BuildPalettePlayProbe.Capture(...)` / `Check(...)` / `Invoke(...)` / `MovePointer(...)`：收集错误、断言及调用非公开建造入口验证。

#### `Assets/Scripts/UI/MainUIButtons.cs`

- `private void Awake()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void OnValidate()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Start()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `private void Update()`： Unity 生命周期回调：初始化、每帧/物理帧更新、编辑器校验、绘制调试信息或销毁清理。
- `public void SetDefault()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetMove()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void SetRotate()`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `public void RefreshBuildModeButtons(SelectType)` / `SetBuildButtonColor(Button, bool)`：同步三个建造状态按钮的选中色。
- `public void SetCurrentBlock(string fileName)`： 设置该对象、视觉效果或运行时引用的参数/状态。
- `private void ShowConnectionStatus()` / `ShowDurabilityStatus()` / `ShowPowerStatus()`：切换对应状态图标并同步按钮选中色。
- `private static void SetDebugButtonState(Button button, bool enabled)`：统一写入 Debug 按钮启用颜色。

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

### 货仓与残骸回收（2026-09-22）

#### `Assets/Scripts/InObject/Bot.cs`

共享导航、采样避障、Home 点速度补偿、停靠与飞行特效。

- `protected virtual void Start()`
- `protected bool TickNavigation()`
- `protected virtual void OnDisable()`
- `protected virtual void OnDestroy()`
- `public virtual void PrepareForHomeDestruction()`
- `private void InitializeTrail()`
- `private void LateUpdate()`
- `private void UpdateMotionVfx()`
- `protected void NavigateToTarget(Transform target)`
- `protected void NavigateHomeSmoothly()`
- `private void NavigateToPosition( Vector3 targetPosition, Transform targetReference, float avoidanceRangeScale = 1f, float maxAvoidanceAngle = 120f)`
- `private void ApplyReturnHomeMovement( Vector3 targetPosition, Vector3 targetDirection, float effectiveSpeed)`
- `private Vector3 GetRelativeHomeVelocity(Vector3 worldPosition)`
- `private Vector3 GetHomePointVelocity(Vector3 worldPosition)`
- `private AdvancedAvoidanceResult CalculateHomeNavigationGuidance( Vector3 targetDirection, float rangeScale)`
- `private float GetHomeGuidanceClearFraction( Vector3 origin, Vector3 direction, float range)`
- `private bool IsIgnoredHomeGuidanceCollider(Collider collider)`
- `private AdvancedAvoidanceResult CalculateAdvancedAvoidance( Vector3 targetDirection, float rangeScale)`
- `private void ProcessRaycastHit(RaycastHit hit, ref Vector3 emergency, ref Vector3 primary, ref Vector3 predictive, ref int emergencyCount, ref int primaryCount, ref int predictiveCount, float emergencyRange, float primaryRange, float predictiveRange)`
- `private void ProcessSphereCollider(Vector3 obstaclePos, float distance, ref Vector3 emergency, ref Vector3 primary, ref int emergencyCount, ref int primaryCount, float emergencyRange, float primaryRange)`
- `private Vector3 BlendDirections( Vector3 targetDir, Vector3 avoidanceDir, float avoidanceStrength, float maxAvoidanceAngle)`
- `private void ReturnHome()`
- `private void LeaveHome()`
- `private void SetNavigationState(NavigationState nextState)`
- `private void OnDrawGizmosSelected()`
- `private void OnDrawGizmos()`

#### `Assets/Scripts/InObject/CargoHold.cs`

容量/类型/供电验证、货位预约、入仓、序列化快照与幂等释放。

- `private void Awake()`
- `private void OnValidate()`
- `private void OnEnable()`
- `private void OnDisable()`
- `public bool CanReceive(CargoItem item, LootDrop reservation = null)`
- `public bool Reserve(LootDrop drop)`
- `public void ReleaseReservation(LootDrop drop)`
- `public int Store(CargoItem item, LootDrop reservation = null)`
- `public List<CargoItem> CaptureContents()`
- `public void RestoreContents(List<CargoItem> items)`
- `public void ReleaseContents(UnitFaction faction)`
- `public static CargoHold FindReceiver(CargoItem item, Vector3 position, ControlUnit owner = null, LootDrop reservation = null)`

#### `Assets/Scripts/InObject/CollectionBot.cs`

回收任务选择、预约、携带、返航交付与中断恢复。

- `protected override void Start()`
- `private void FixedUpdate()`
- `private void FindTarget(ControlUnit owner)`
- `private void Abandon()`
- `protected override void OnDisable()`
- `protected override void OnDestroy()`
- `public override void PrepareForHomeDestruction()`

#### `Assets/Scripts/InObject/LootDrop.cs`

独立掉落物生成、显示、声明/释放、货币汇聚与会话清理。

- `private void OnEnable()`
- `private void OnDisable()`
- `private void Start()`
- `public static LootDrop Spawn(CargoItem item, Vector3 position)`
- `public void Initialize(CargoItem item)`
- `private void RefreshLabel()`
- `public bool Claim(CollectionBot bot, CargoHold destination)`
- `public void Carry(Transform socket)`
- `public void Release()`
- `public bool Deliver(CargoHold hold)`
- `private void Update()`
- `public static void ClearSession()`

#### `Assets/Scripts/InObject/WreckSalvage.cs`

将普通敌方残骸结算为金币或候选特殊零件，防止重复结算。

- `public static void Convert(Block block)`
- `public static void ConvertGroup(ControlUnit unit)`

#### `Assets/Scripts/Effect/CargoHoldView.cs`

货物陈列、资源液位和仓体状态灯。

- `private void Awake()`
- `private void LateUpdate()`

#### `Assets/Scripts/Effect/CargoVisual.cs`

仅复制静态渲染几何，按包围盒归一化展示，避免运行被储存模块行为。

- `public static GameObject Create(string resourcePath, Transform parent, float size)`

#### `Assets/Scripts/Manager/CargoPersistence.cs`

返回时仅更新蓝图货仓内容，使用临时文件和备份进行原子替换。

- `public static bool SaveReturnCargo(bool survived)`

#### `Assets/Scripts/Data/CargoItem.cs`

货物类型、资源路径和数量的可序列化数据。

- `public CargoItem Copy(int count = -1)`

#### `Assets/Editor/SalvageAssetBaker.cs`

保存新 Prefab/材质/参数资产和独立渲染预览，保留已有手调。

- `public static void Bake()`
- `private static Material Material(string name, Color color, bool transparent, bool emissive)`
- `private static void Hold(string name, CargoKind kind, int size, int capacity, bool explosive, bool enemyDrops)`
- `private static void CollectionBay()`
- `private static void DropPrefab()`
- `private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)`
- `public static void Set(Object target, string property, Object value)`
- `public static void Preview()`

#### `Assets/Editor/SalvagePlayProbe.cs`

在 Play Mode 副本中执行隔离回归并写入验证报告。

- `public static void Run()`
- `private static void StateChanged(PlayModeStateChange state)`
- `private static void Capture(string condition, string stack, LogType type)`
- `private static void Tick()`
- `private static void Finish()`
- `private static void Check(string name, bool result)`
- `private static ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool cockpit = true)`
- `private static GameObject Spawn(string name, ControlUnit unit, Vector3 local)`
- `private static CargoItem Coins(int count)`
- `private static CargoItem Part()`
- `private static IEnumerator Scenario()`

### 战斗 HUD 与 UI 过渡（2026-09-25）

- `MainUIPanels.Transition(GameObject panel, bool show)`：取消同一面板的旧过渡并启动新过渡；`Fade` 完成后移除记录。`ShowDeletePanel` / `HideDeletePanel` 管理单个删除确认回调；`PlayStart` 重置本轮击杀提示。
- `Durability.ApplyDamage(float amount, ControlUnit attacker)`：记录本次伤害来源并更新耐久；`UpdateDurablility` 收到环境伤害时清除来源。
- `DestroyManager.DestroyGameObject(GameObject obj)`：仅玩家造成的敌方驾驶舱摧毁通知 HUD。
- `EnemyIdentity.SetDisplayName(string displayName)` / `SetSpawnMaxHealth(float maximum)`：保存敌方蓝图名及只初始化一次的出生最大血量；`EnemySpawner.SpawnBlockData` 在运行时驾驶舱上设置身份与血量快照。
- `ControlUnit.RefreshChildren()`：重组后重新绑定悬浮控制器，但不强制开启诊断窗；`HoverFlightController.OnGUI()` 仅在显式调试开关和有效 PlayManager 下绘制。
- `CombatHud.OnEnable()` / `OnDisable()` / `ResetHud()`：初始化或清理本轮名牌及提示；`ShowPlayerKill(string enemyName)` 更新击杀提示；`LateUpdate()` 投影名牌并驱动淡入淡出；`IsWorldPointOccluded()` 供名牌和状态图标共用 3D 遮挡判定；`RefreshEnemies()` 更新总耐久及驾驶舱双血条；`CreateView()` / `Recycle(EnemyView view)` 复用名牌实例。
- `UIInteractionFeedback.Awake()` / `OnDisable()`：记录和恢复按钮原始缩放；`OnPointerEnter` / `OnPointerExit` / `OnPointerDown` / `OnPointerUp` / `OnSelect` / `OnDeselect` 更新输入状态；`Animate()` / `AnimateScale()` 平滑改变按钮缩放。
- `CombatHudSetup.Apply()` / `UpgradeDualHealthHud()`：重建 Main 场景可编辑 HUD 与双血条布局；`Child()` / `Stretch()` / `Position()` / `BottomBox()` / `Image()` / `Text()` / `Set()` 为编辑器搭建辅助函数。
- `CombatHudPlayProbe.Run()` / `OnPlayModeChanged()` / `Tick()` / `Scenario()` / `CreateEnemy()` / `Check()` / `Finish()`：在隔离 Play Mode 中验证 HUD 绑定、名牌、耐久、击杀归因和环境伤害边界。

### 2026-09-26 物理与界面索引补充

- `ImpactPhysics.ReducedMass / DamageEnergy / ClosestPoint / ReportPlayerImpact / ResetEvents`：质量、能量、支持的最近点、撞击遥测与静态事件重置。
- `Durability.ApplyCollisionEnergy / ProtectFromExplosionCollisions`：碰撞能量映射、限幅冷却与短时保护，替代旧 CollisionEnter；`ControlUnit.ProtectExplosionContacts / OnCollisionEnter`：双侧保护、实际冲量分摊。
- `PhysicsTelemetryHud.OnEnable / OnDisable / OnImpact / Update`：订阅和解除撞击事件，维护本轮遥测与低频 UI 刷新。
- `InterfaceBaker.Apply / Style / Primary / Label / Child / AddRule / BakeTelemetry / TextAt`：统一场景和动态模板，持久化 HUD 绑定并保存。
- `PhysicsSandboxProbe.Run / Collide / Body / Check / Save`：独立物理场景的 17 项验证、清理与报告，Main Play Mode 下通过 eval 调用 Run。

## 9. 函数索引维护规则

新增、删除、重命名或改变职责的函数，必须在同一提交更新本节；签名变化替换旧条目，行为变化同时修改描述和变更日志。索引以源码为准，自动提取遗漏的多行签名时手工补充。

### 9.1 C# 代码规范

具体执行规范见 [`Assets/Scripts/AGENTS.md`](./Assets/Scripts/AGENTS.md)。本轮全局审查已统一项目脚本中省略的私有访问修饰符，并在连接重建、建造加载、游玩分组、存档复制、摧毁爆炸、敌方移动、推进器视觉和输入模式切换等关键路径补充意图注释；未改动序列化字段名、场景层级或资源引用。

- **已通过代码/文件确认**：脚本访问修饰符扫描无遗漏（接口声明除外），本次触及脚本使用统一 CRLF，新增规范文件和注释已纳入 Git diff。
- **已通过构建确认**：`dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误；仅有既存 `ProfilerCaptureAnalysis.WriteCounters` 过时 API 警告）。
- **尚未验证**：Unity 编辑器导入、Inspector 序列化、Play Mode 交互、运行时日志和性能表现。


## 10. 变更日志

### 2026-10-04（敌机多层结构与零件丰富化）

- **实现**：CODEX 三种敌机加入底层悬浮甲板、中层设备与炮位、上层对称背脊和阶梯细节，使用 TechnologyHold、Rack、Stairs、不同尺寸结构块及第二台发电机。当前部件数为 95/120/116。
- **验证**：资源、连接、供电检查通过；Unity 6000.3.11f1 的 90 秒实际飞行通过，最大倾角 0.63°、高度误差 0.11m，无失电或掉块。相对上一版峰值水平速度为 0.39/0.41/0.42 倍。原始数据见 `Blueprints/CODEX_Enemies`。
- **边界**：完整机体悬停、追踪和换向已验证；受损多层结构及密集交火未验证。

### 2026-10-04（敌机扩容与机动增强）

- **实现**：CODEX_E01/E02/E03 从 27/35/35 扩至 41/53/53 部件（约 +51%），扩展甲板、悬浮推进器及供电节点。水平推进改为四大四小万向推进器，大型推进器侧装于接近重心的高度；使用原装部件，不改 AI、Prefab 或全局数值。三份蓝图已更新本机敌人池，重新进入 Play Mode 后加载。
- **验证**：资源/尺寸/连接点/供电/安装一致性检查通过；Unity 6000.3.11f1 实际生成均为单连通组，90 秒悬停、追踪、换向与失去目标测试通过。25～75 秒峰值水平速度为 7.63/5.67/5.68m/s，相比旧探针记录为 4.64/5.08/5.06 倍；最大倾角 1.67/1.99/2.56°，最大高度误差均低于 0.25m，无失电或掉块。测试退出后场景未脏；生成器在 Editor 执行，不涉及生产 C#，未重跑 dotnet build。原始数据、对比脚本、截图见 Blueprints/CODEX_Enemies。
- **边界**：为隔离飞行关闭测试炮塔、自动刷怪和远距离回收；受损飞行、密集交火及大量敌机性能仍未验证。峰值速度不是所有战斗场景的平均机动收益。


### 2026-09-30（推进器分类初始黑色方块）

- **原因与修复**：Main 场景 ThrusterInfoPanel/Window/Tabs/SelectionLine 的 RectTransform 保存为居中的 100×100 占位 Image；首次显示时遮挡 Universal 标签。将场景初始值和 ThrusterHudSetup 重建逻辑统一为首个标签底部 3px 青色选中线，并在 ThrusterInfoPanel.Awake 初始化当前分类。
- **代码/文件确认**：已核对 SelectionLine 的场景 fileID、Image 颜色、RectTransform 几何及生成脚本；未改变标签尺寸、按钮绑定或其他场景节点。
- **验证范围**：git diff --check 通过；dotnet build HY-Sandbox.sln --no-restore --nologo 为 0 错误、4 个既有警告。Unity CLI 当前未发现已连接的目标 Editor，首次打开面板的实际 Play Mode 视觉回归尚未完成。

### 2026-09-30（编辑器异常冲量记录器）

- **实现**：新增 `Assets/Scripts/Debug/AbnormalImpulseRecorder.cs` 和 `AbnormalImpulseBodyProbe.cs`，用 `UNITY_EDITOR` 限定仅在编辑器编译。进入 Play Mode 后自动创建隐藏诊断对象，发现 Rigidbody 并监听碰撞；同时采样相邻物理步的速度变化，以捕获没有直接来源回调的异常冲量。达到阈值时调用 `LogWarning`，并追加时间、帧号、来源、刚体层级、质量、冲量、速度变化、受击点和速度到 CSV。销毁刚体时清理其速度采样与告警限频状态，避免长时间测试积累无效条目。
- **文件确认**：记录文件输出到项目 `Temp/AbnormalImpulseLogs`，不修改场景、Prefab 或正式运行时逻辑；诊断对象使用 `DontSave`，退出 Play Mode 后不保留。
- **验证范围**：Unity 6000.3.11f1 Play Mode 中确认记录器自动创建；临时 100 kg 刚体产生 20 m/s 速度突变后，Editor.log 出现 `LogWarning`，CSV 写出一条 2000 N·s、20 m/s 的完整记录，测试对象已销毁并退出 Play Mode。真实战斗碰撞样本与长时间运行开销尚未验证。

### 2026-09-30（异常冲量 CSV 复核）

- **原因**：`20260930-154936.csv` 中的 `PhysX collision` 行来自碰撞求解器的原始冲量；例如 `Group_1` 与 `Plane` 的 `17071.9 N·s` 是整组刚体碰撞回调的求解结果，不能直接等同于碰撞伤害，也不能仅凭此数值判定爆炸施力异常。项目实际伤害仍使用 `ImpactPhysics` 的约化质量、法向速度和能量阈值计算。
- **修复**：修复 `AbnormalImpulseRecorder.FixedUpdate` 遍历 `_previousVelocities` 时的集合修改异常，避免长时间 Play Mode 中重复 `InvalidOperationException`。
- **物理调整**：刚体重组时保留原有质心平动速度，但限制从旧角速度传递到新碎片质心的额外切向速度为 6 m/s、继承角速度为 2 rad/s，并将重组刚体的最大解穿透速度设为 4 m/s；这些值可在 `PlayManager` Inspector 调整，减少远离原质心的碎片被甩出以及重叠接触的速度尖峰。陨石随机角速度从度/秒转换为 Unity 刚体要求的弧度/秒，避免接触点切向速度被放大约 57 倍。
- **验证状态**：CSV 数据已完成静态复核；Unity 6000.3.11f1 重编译成功，Play Mode 中临时刚体自动挂载探针，持续运行约 2 秒后 Console 新增错误为 0，测试对象已销毁并退出 Play Mode；`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误、4 个既有警告。高冲量场景的视觉效果和真实碰撞伤害仍待人工复测。

### 2026-09-30（DetachedSmoke 残骸烟迹）

- **原因**：断裂烟迹原来绑定 `UNI_Device_Fire`，Prefab Graph 缩放为 0.35，视觉上偏细且偏火焰；爆炸冲量降低后，低速残骸的速度比例可能为零，导致 `SetIntensity(0)` 不发射。
- **实现**：DetachedSmoke 改用 `UNI_Steam_Leak`，Graph 缩放为 1.5；`DetachedPartSmokeTrail` 增加可编辑的初始烟雾持续时间/强度，低速时仍先产生短暂烟团，再按速度和生命周期衰减；烟迹朝向与残骸线速度相反。
- **验证**：Unity 6000.3.11f1 重新编译通过；独立 VFX 预览确认原 Graph 为细火焰，Steam Leak 为明显厚烟团，Prefab 当前绑定 `UNI_Steam_Leak`，Main 场景保持未保存。`dotnet build HY-Sandbox.sln --no-restore --nologo` 0 错误、4 个既有警告。
- **尚未验证**：主场景中真实断裂组在不同摄像机距离、速度和大量并发下的最终表现。

### 2026-09-30（爆炸特效槽位与灯光同步）

- **实现**：将瞬时 VFX 上限由单纯计数改为活动实例管理。爆炸在 64 槽满时优先停用并销毁最早的非爆炸效果；若全是爆炸，则拒绝新效果。方块、普通物体和陨石爆炸仅在 Graph 成功生成时播放对应点光，避免只闪灯而没有爆炸图形。
- **代码/文件确认**：Explosion 资源引用有效；命中、建造和爆炸共用 64 槽、12 秒回收，是密集事件触发拒绝生成的原因。未改 Prefab、场景或 `.meta`。
- **Unity 编辑器/Play Mode 验证**：Unity 6000.3.11f1 重新编译通过。64 个命中特效占满时新爆炸替换旧命中，活动数保持 64；64 个爆炸占满时新爆炸与普通命中都返回 `BurstLimit`，旧爆炸保持激活。普通物体销毁路径在饱和时新增点光为 0，未饱和时为 1。独立镜头下 Explosion Graph 在 0.4 秒有 116 个活粒子，截图已目视核对；测试后 Main 场景未变脏。`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误、4 个既有警告，`git diff --check` 通过。
- **尚未验证**：正式构建和目标硬件上的高并发爆炸表现与 GPU/Overdraw 性能。

### 2026-09-27（玩家 Cockpit 血条与 Scene 摄像机预览）

- **实现**：Main 场景玩家 Cockpit 血条的 Filled Image 补上 UI Sprite，使 `fillAmount` 真正裁切填充图像；数值绑定和 0.2 秒刷新周期保持不变。新增编辑器菜单 `Tools/HY-Sandbox/Scene View/Follow Main Camera`，按项目记住开关状态，开启时 Scene 视角跟随 MainCamera，关闭时不再接管视角。
- **代码/文件确认**：Main 场景 Cockpit Fill 引用保留原 Image 组件与绑定，仅为 Filled Image 指定与主血条相同的 UISprite；场景差异只涉及该资源引用。`SceneCameraPreviewSync` 仅位于 Editor 目录，开关不修改摄像机或场景资产。
- **Unity 编辑器/Play Mode 验证**：Unity 6000.3.11f1 中，受伤后玩家 Cockpit 数值从 `500 / 500` 更新为 `375 / 500`，`fillAmount` 从 1 更新为 0.75；修复后使用该场景 Image 的副本生成 uGUI 顶点，填充 1→0.5 时右边界由 149.75 缩至 0，确认可视宽度会变化。Scene 跟随开关开启时与 MainCamera 位置/旋转误差为 0、FOV 均为 60；关闭后移动 Scene 视角 3 米未被拉回。Unity 脚本编译通过。
- **尚未验证**：正式构建和不同编辑器布局下所有 Scene View 实例的同步效果。

### 2026-09-26（刚体阻尼与推进器面板光标）

- **实现**：运行时控制单元 Rigidbody 线性阻尼默认提高至 1.5，并新增 PlayManager Inspector 可调的线性/角阻尼；F1 打开 ThrusterInfoPanel 时保持光标隐藏与锁定镜头，只有长按 Alt 才显示光标，松开后立即隐藏。PlayManager 不再与 InputManager 争用光标锁定状态。
- **已通过代码/文件确认**：所有运行时控制单元重新分组都经同一个阻尼赋值路径；建造模式仍沿用原有光标逻辑。
- **Unity 编辑器或 Play Mode 验证**：Unity 6000.3.11f1 重新编译通过。Main 场景 Play Mode 实际开始游玩后，玩家刚体 `linearDamping=1.5`；打开推进器面板时光标隐藏且 Locked，镜头为 ThirdPersonLock。临时 Input System 键盘模拟 Alt 按下/松开，分别验证光标显示且 Confined、隐藏且 Locked；测试设备已移除。Play Mode 结束前 Console 为 0 error / 0 warning，编辑器未保存已有的场景脏状态。
- **尚未验证**：正式构建和长时间飞行手感。

### 2026-09-26（半透明统一界面与温和物理反馈）

- **UI**：统一 Main 面板、57 个按钮、动态 SavePrefab、模态弹窗、血条和推进器窗；面板 alpha 0.62，按钮半透明，文字图标保留清晰度。新增可编辑 PhysicsTelemetry 节点；返回按钮移至右上避免遮挡。修复 GlobalTextStyler 把 Outline 当成 Shadow 重新启用的错误。编辑入口 `Tools/HY-Sandbox/Polish Interface and Physics HUD`；原 Main 未保存状态备份于本地 `Temp/UIArcade/Main-before.unity`，恢复分支 `codex/pre-ui-arcade-20260926`。
- **物理**：碰撞能量使用 PhysX 冲量约束有效质量，避免复合 Collider 回调重复计能。伤害为本轮初始能量伤害的 10%，单次最多 8% 最大耐久，0.35 秒冷却；炮击命中点施加可调 45 N s 冲量。爆炸冲量为原值 15%，单次附加速度最高 3 m/s；保持无直接伤害，3 秒碰撞双侧保护，碰撞摧毁不触发物理爆炸。没有连击、过载或射速增益。
- **验证**：Unity 6000.3.11f1 Main Play Mode 的隔离 PhysX 探针 17 项通过，涵盖能量预算、质量/速度关系、偏心旋转、伤害限幅/冷却、爆炸速度上限/零直接伤害/碰撞保护；主场景按钮事件进入模拟成功，动态存档列表、建造/HUD/删除弹窗截图已目视检查。静态 build 0 错误（4 项既有依赖/过时 API 警告）；最终 diff 检查见交付。旧探针期间暴露的 Durability.OnEnable 对未初始化 MainUIPanels 的空引用已加保护。
- **限制**：长时间密集爆炸/敌人压力与正式构建未测，不能宣称完全消除卡顿；多分辨率完整鼠标输入回归仍待后续。

### 2026-09-25（双血条与遮挡过渡）

- **范围与实现**：Main 场景的玩家 HUD 增加绿色驾驶舱血条，敌方名牌增加红色驾驶舱血条；总血条统一为白色。敌方名牌遮挡与进入视野时渐隐渐显，状态图标沿用同一射线遮挡判断，并将可见度过渡与原有脉冲叠加。`Upgrade Dual Health HUD` 可重建双血条布局，常规 Combat HUD 搭建也会应用该布局。
- **文件/静态确认**：场景引用、代码编译、差异检查见本次交付。
- **Unity Editor/Play Mode 验证**：Unity 6000.3.11f1 隔离 Combat HUD 探针通过 30 项检查，含状态图标遮挡与恢复；Build Palette 回归 55 项通过。玩家 HUD 的 1280×720、1920×1080、2560×1440、3440×1440 四档布局预览已生成，1280×720 已目视检查。大型实战场景中的多图标视觉效果尚未实测。
- **尚未验证**：大型敌方蓝图、多图标重叠与正式构建。

### 2026-09-25（敌方血条出生血量快照）

- **范围与实现**：敌人生成时累计已实例化方块的最大耐久，保存到 Cockpit 的 `EnemyIdentity`；敌方名牌继续实时汇总当前血量，分母在方块损毁或重新分组后保持出生值。
- **已通过代码/文件确认**：`dotnet build HY-Sandbox.sln --no-restore --nologo -m:1 -p:UseSharedCompilation=false` 为 0 错误、4 个既有警告；本次文件的 `git diff --check` 通过。整个工作区的差异检查仍报告用户现有 `PlayManager.cs` 的一处行尾空格。
- **Unity 编辑器与 Play Mode 验证**：Unity 6000.3.11f1 重新编译成功；隔离 Combat HUD 探针 24 项检查通过，确认方块脱离后血量由 `120 / 150` 变为 `80 / 150`，并保留遮挡、击杀、玩家 HUD 和推进器面板检查。
- **尚未验证**：大型蓝图实战与正式构建。

### 2026-09-25（战斗 HUD 与 UI 交互）

- **范围与实现**：PlayPanel 新增可编辑的敌方名牌、聚合耐久条和居中偏下击杀提示；敌方蓝图身份随驾驶舱保存，伤害来源决定击杀归因。面板过渡取消重复协程，删除确认回调不再累积，现有按钮获得轻量输入缩放。根据实机截图加深并放大击杀提示；悬浮控制器 Prefab 默认关闭遮挡画面的 IMGUI 诊断窗，Inspector 开关仍可启用。
- **已通过代码/文件确认**：Main 场景保存 HUD 及脚本引用，Prefab `showUI: 0`，Unity 6000.3.11f1 重新编译成功。
- **Unity Play Mode 验证**：隔离探针 9 项检查通过，涵盖名牌、聚合耐久、蓝图名称、玩家驾驶舱击杀一次计数和环境伤害不计数。Play Mode 两张截图已目视核对，击杀提示文字与背景可读，悬浮诊断窗默认未出现；大蓝图实机排版和正式构建尚未验证。
- **静态验证**：`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误、4 个既有警告；`git diff --check` 通过，新增脚本的 `.meta` 齐全。

### 2026-09-25（推进器 Canvas HUD 与遮挡）

- **范围与实现**：敌人名牌增加 3D 射线遮挡判断；玩家/敌人血条统一显示含 Cockpit 的 ControlUnit 总耐久。HoverFlightController 的飞行信息迁移到左下角 Canvas，新增 F1 推进器状态面板、三类导航、推力进度和供电颜色，Prefab 增加 ControlUnit 使用状态灯。
- **验证**：Unity 6000.3.11f1 隔离 Play Mode 19 项检查通过；静态 `dotnet build HY-Sandbox.sln --no-restore --nologo -m:1 -p:UseSharedCompilation=false` 为 0 错误、3 个既有警告。正式构建与多分辨率视觉回归尚未执行。

### 2026-09-24（建造目录悬停参数）

- **范围与实现**：补齐 `HoveredBlockInfo` 场景引用和 23 个已保存按钮的参数信息；保留编辑器内现有的名称/信息文字位置与列表布局。Block 默认按当前 Prefab 参数生成 `info`，修正旧质量字符串多拼接 `00` 的问题；显式开启 `canEditInfo` 后可保留手写内容。`BuildPaletteBaker` 增加定向刷新菜单，并同步完整烘焙路径。
- **已通过代码/文件确认**：Main 场景 23 个条目均已写入信息；抽查基础块、发电机、货仓、推进器的尺寸、质量及对应组件参数。`dotnet build HY-Sandbox.sln --no-restore --nologo` 0 错误、4 个现有警告；Unity 6000.3.11f1 脚本重新编译成功，定向刷新菜单执行成功。
- **Play Mode 验证**：`BuildPalettePlayProbe` 完整复跑通过，新增断言确认货仓悬停显示容量信息、指针离开时隐藏信息，原有目录、分类、EventSystem 悬停及连接提示检查也通过；结果位于 `Temp/BuildPalette/Validation.json`。第一次运行中途重启测试造成的失败结果已被完整复跑覆盖。信息文本在不同分辨率下的可读性及正式构建尚未验证。

### 2026-09-24（销毁爆炸可见度与调试）

- **范围与实现**：Block 爆炸的最小缩放由 0.7 提升至 1.6，按边界尺寸放大比例提高；普通物体销毁从 Small Explosion/程序化光痕改为 UNI Aerial Explosion Graph。可爆炸方块只播放爆炸，不再先叠加拆除效果。资源库返回生成状态，两个销毁函数通过默认开启的 Inspector 调试开关输出目标、中心、边界、缩放和结果（包含缺失资源与并发上限）。
- **Unity 验证**：6000.3.11f1 编辑器重编译无错误；Main 场景 Play Mode 分别触发两条路径，生成两个 Explosion Graph 实例，调试日志均报告 `Spawned`。延时截图错过瞬时火球，视觉醒目程度尚未由有效帧确认。退出 Play Mode 后确认 Main 场景原有参数调整仍在，未纳入本次提交。
- **静态验证**：`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误、4 个既有警告；`git diff --check` 通过。

### 2026-09-24（推进尾焰、陨石尾迹与 Block 爆炸）

- **范围与实现**：MainThruster、MainThrusterBig、UniversalThruster、UniversalThrusterBig 的 10 个已解包喷口换为无烟 `HoverJet` 实例，普通/大型世界尺寸对齐对应悬浮推进器，保留挂点、方向、Prefab GUID 与推进控制数组；集成器默认值和显式迁移菜单同步更新。陨石烟火尾迹脱离旋转的陨石层级，仅跟随位置并按速度反向定向，碰撞或来源消失后停止释放。`PlayBlockExplosion` 直接使用资源库中的 UNI Aerial Explosion Graph，移除程序化碎光与二次烟爆，保留短闪光和震屏。
- **Unity 验证**：四种推进器 Prefab 重载后喷口数量/控制器引用完整，普通/大型 Graph 有效尺寸分别为 0.81/1.62，喷射轴仍反向于推力。28 项隔离 Play Mode 检查通过，包括陨石旋转时尾迹独立、原生 Block 爆炸、推进输出与回收；43 个效果 Prefab、68 个 Graph 实例资产检查无错误。实际 Play Mode 采样并目视检查大型主推进器与大型悬浮推进器的喷焰；Main 场景恢复。
- **静态验证**：`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误、4 个既有警告；资产与截图检查、`git diff --check` 通过。
- **尚未验证**：正式构建和大量同时运行的放大尾焰的 GPU/Overdraw 性能。

### 2026-09-24（特效位置稳定与无烟推进尾焰）

- **范围与实现**：从 UNI Gas Fire 建立可编辑的无烟推进器 Graph，删除完整烟雾 Spawn/Initialize/Update/Output 链，仅保留三套原生火焰；35 个项目推进 VFX 实例改为引用此变体。推进、Device Fire 和 Steam Leak 改用 Local 模拟空间，移除运行时旧世界烟流旋转补偿。EnergyTrail 保留 World 模拟空间的运动历史，发射位置仍从局部挂点转换。保持喷口层级、Prefab GUID 和原生素材依赖。
- **Unity 验证**：编辑器重建并重载 Graph 后，确认无烟图各有 3 个 Spawn/Initialize/Update/Output、无烟雾贴图输出，35 个推进实例全部引用新图；43 个效果 Prefab、68 个 Graph 实例的缺失脚本、旧组件、引用与依赖检查无错误。实际 Play Mode 在约 `(1000, 0, 1000)` 移动目标并自动渲染推进尾焰、能量拖尾、燃烧烟迹和爆炸，28 张采样帧通过可见/清空像素检查；目视确认效果出现在目标附近，Main 场景已恢复且退出 Play Mode。
- **静态验证**：`verify_assets.py` 通过（170 个跟踪 Prefab/场景、来源哈希、10 类原有生命周期和 4 类大坐标采样）；`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误、4 个既有警告；`git diff --check` 通过。
- **尚未验证**：正式构建、极远坐标精度及大规模并发 GPU/Overdraw 性能。原始含烟 UNI Gas Fire 仅保留为来源资产，项目 Prefab 不再引用。

### 2026-09-24（全量原生 VFX Graph 迁移）

- **范围与实现**：重新引入 VFX Graph 17.3.0；直接复制并闭合 UNI 原生爆炸、烟火、气焰和蒸汽图依赖到 Assets/Art/VFX。41 个含旧粒子的项目 Prefab（含备用飞船）全部迁移，新增能量拖尾/光束模板；共 43 个 Prefab、68 个 Graph 实例。删除上一版翻页 Particle System 合成资产和工具。
- **程序与编辑**：AssetParticleEffect 保留脚本 GUID 改名为 VfxEffect；所有调用者、机器人/掉落/陨石拖尾及实时能量光束统一接入 Graph。迁移保持原有挂点和 Prefab GUID，修正 Gas Fire -X 轴、世界空间烟流和 GPU 位置参数；原生节点/运动向量/纹理保留。附可复现转换、生成、迁移和实际 GPU 采样工具。
- **Unity 验证**：原生与补充效果 10 类、70 张实际 Play Mode 自动渲染帧，覆盖发射、停止、清空及重播；清空截图为纯背景。26 项玩法集成检查通过（低推力、炮塔伤害、维修回舱、掉落、陨石尾烟与回收），无运行时错误。项目 Graph 引用/遗留组件检查、Blocks/Salvage/效果模板以及 105 个 SpaceKit Prefab 的引用和依赖检查通过。GPU 清空后的负计数作为未读回处理，并以实际像素验证清空。
- **静态验证**：dotnet build HY-Sandbox.sln --no-restore --nologo：0 错误，4 个既存警告；verify_assets.py 检查跟踪的 Prefab/场景和运行时代码无 ParticleSystem/TrailRenderer、来源哈希及 Git 可见性。git diff --check 通过。
- **边界**：全量旧 BlockArtValidation 默认范围额外发现 Art/Temp 两个已有 CenterTube 直接引用原包，本次保留未修改；交付检查明确列出受影响目录。正式构建、大规模战斗 GPU/Overdraw 与低端硬件性能尚未验证。测试过程中 Windows 提交内存一度不足，释放闲置资源后恢复并完成验证。


### 2026-09-24（UNI 爆炸与燃烧烟迹，已由后续原生 Graph 迁移取代）

- **范围**：替换 Art/BlockVisuals/VFX 中 Explosion、BreakBurst、ImpactBurst、SmokeBurst、DetachedSmoke；保持 Prefab GUID、AssetParticleEffect 根组件和资源库引用。使用 UNI 火球、火焰、两张烟雾序列及 glow，组合七层爆炸和三层烟火拖尾；新增柔边冲击环。
- **实现**：原生 URP Particle System，保持现有触发、烟迹强度和全局实例限制；瞬时效果 3.2 秒回收。五张贴图无损转 PNG 并逐像素核对，保存复现脚本和 SHA-256 清单，Unity 导入上限 2048。新增 Bake/依赖验证/渲染/Play Mode 探针菜单；旧连续性修复跳过新 UNI 材质。
- **Editor 与画面**：Unity 6000.3.11f1 保存重载、URP Shader/贴图及依赖闭包检查通过；25 张实际 URP 分时预览已检查；发射、消散、重播和禁用清理通过。Play Mode 七项检查通过：工厂触发、全部发射、自动销毁、停止消散、25% 强度重启、残骸烟迹回收和容量归还。
- **静态验证**：`dotnet build HY-Sandbox.sln --no-restore --nologo` 通过，0 错误、4 个既有警告；`git diff --check` 通过。
- **尚未验证**：复杂战斗镜头、大量同时爆炸的 GPU/Overdraw 性能和正式构建；现有压力测试风险继续保留。证据见 `Assets/Art/BlockVisuals/UNI/Preview/Validation.json` 与 `PlayModeValidation.json`。

- 2026-09-23：状态图标资源按状态名重命名并统一为白色，Prefab 不再各自保存 Status 列表，改由场景 `IconManager` 集中管理；按要求回退 World Space Canvas 深度遮挡策略，恢复独立 Screen Space Overlay 图标层，图标继续投影自 Block 的 `Center`，`IconManager` 统一使用 `sin(Time.unscaledTime)` 控制透明度。已通过 C# 静态构建，Unity 视觉效果尚需运行时验证。

- 2026-09-22：移除 `Info.LateUpdate` 中每 0.1 秒轮询更新 Status 的逻辑。`Block.CheckConnection`、`Durability.UpdateDurablility` 与 `Power` 的供电写入点分别主动通知 `Info` 更新状态，Debug 状态开关变化时集中刷新图标；`LateUpdate` 仅保留屏幕位置投影。已通过 C# 静态构建，Unity Play Mode 尚需验证事件触发覆盖范围。
- 2026-09-22：状态图标增加基于相机距离的缩放，近距离保持 42px 基准，远距离按 `iconReferenceDistance` 比例缩小并受 `iconMinimumScale` 限制，降低大型蓝图远景下图标过密问题；已完成代码静态确认，Unity 运行时视觉密度尚需验证。

- 2026-09-22：修正 `Info.CheckConnectionStatus` 的初始化时序。Block 首次启用或重新启用时先保持 `ConnectionStatus.Normal`，待连接探针完成后再判断 `NoConnection`，避免连接状态尚未刷新时误显示断连图标；已完成代码静态确认，Unity 运行时需重新验证首次启用与实际断连场景。

### 2026-09-22（Block 状态图标与蓝图功率统计）

- **状态系统**：为 `Assets/Resources/Blocks` 内全部 23 个带 `Block` 根组件的 Prefab 添加 `Info`，由 `IconManager` 统一管理 `NoConnection`、`Damaged`、`Broken`、`UnderPower`、`NoPower` 五个非 Normal 状态图标；连接、耐久、供电状态分别由 Connector、Durability、Power 实时判定。Normal 不显示，无对应数据组件时不误报。
- **显示与调试 UI**：`DebugManager` 和 DebugSettingsPanel 新增连接状态、耐久状态、供电状态三个独立开关。异常图标由场景 `IconManager` 统一管理，使用 Block `Center` 的屏幕投影和独立 Overlay Canvas，并由 `sin(Time.unscaledTime)` 控制透明度。新增 `BlockStatusSetup` 可重复迁移工具，负责 Prefab 配置和 Main 场景 UI 引用。
- **蓝图统计**：BlueprintInfoPanel 新增 Required power 与 Generator output 两行；`Refresh` 与异步恢复主循环均累计 `Power.standardWorkingPower` 和 `PowerGeneratingUnit.outputPower`，更新时机与既有数量/质量一致。
- **已通过代码/文件确认**：Unity API 检查为 `Blocks=23; Infos=23; BadStatusLists=0; BlueprintRefs=True; DebugRefs=True`；Bot 与 Connector 因根对象没有 Block 组件未添加 Info。`dotnet build HY-Sandbox.sln --no-restore --nologo` 0 错误、4 个既有警告。
- **已在 Unity 编辑器/运行时验证**：Unity 6000.3.11f1 导入编译 `failed=false`、Console 0 Error；Play Mode 探针得到 `NoConnection / Damaged / UnderPower`，三按钮点击后开关均为 True，创建 3 个有效 Sprite Image，图标 Canvas 排序为 -1。1280×720 Game View 检查后上移 DebugSettingsPanel、BlueprintInfoPanel，避免新增行侵入工具栏或屏幕下缘。
- **尚未验证**：大型蓝图同时显示大量异常图标的 Canvas rebuild/重叠可读性、所有分辨率、遮挡关系和正式构建；已记录到待改进与风险。

### 2026-09-22（建造分类目录与连接点放置提示）

- **实现**：Main/BuildPanel/ButtonContent 保存 23 个建造块，包括货仓、金币仓、科技仓、回收及维修无人机舱和炮塔。六类导航、四列滚动目录、透明底模型图标与圆角线框，悬停显示名称，选中边框高亮；移除运行时克隆文字按钮的旧注册路径。新增 Editor Bake 工具和来源/编辑说明。
- **连接交互**：BuildManager 的预览流程显示当前指向方块所有可用 Connector 的 0.9×0.9 白色圆角线框；遵循世界法线、深度遮挡和 0.015 外偏移，不显示禁用或占用点。补齐 UI 防穿透、无有效连接点/资源时清理、禁用/退出清理，并将穿透搜索限制为最多 64 步。
- **验证**：Unity 6000.3.11f1 导入编译、实际 Play Mode 25 项检查通过，无 Error/Exception；包含六类筛选、真实指针事件与资源选择、滚动复位、尺寸/过滤、旋转面视觉、销毁/退出清理、InputSystem 虚拟鼠标驱动的世界→UI 阻断和失效连接点恢复。透明 PNG Alpha 已检查，界面与世界线框截图见 Assets/Art/BuildPalette/Interaction-preview.png，报告为同目录 Validation.json。
- **静态与重载**：`dotnet build HY-Sandbox.sln --no-restore --nologo` 0 错误、4 个既有警告；场景重载后 23 个按钮引用完整、缺失脚本为 0、Main 无未保存修改，`git diff --check` 通过。重载过程中的 CLI 超时由 Unity 外部文件更改对话框造成，已通过 Reload 解除并重新查询确认。
- **边界**：验证为主场景探针和 1833×966 Game View 实际截图；未做所有分辨率、长时间手工搭建或超大目录压力测试。新增模块后需执行 Tools/Build Palette/Bake thumbnails and refresh Main；分类和图片均为真实可编辑场景/资产。


### 2026-09-22 货仓与残骸回收首版

- **范围与实现**：新增 CargoHold、CoinHold、TechnologyHold、CollectionBotContainer 四个方块，透明仓体、容量液面、8 格零件陈列和三态状态灯；新增货物数据/掉落物、供电容量约束、特殊零件预约与回收、金币爆散/汇聚及资源包数量控制。普通敌方残骸清理产币，特殊零件独立保留；真实摧毁释放货仓和无人机携带内容；非爆炸模块在延迟 Destroy 前先从连接图移除，避免被再次编组。
- **架构与兼容**：RepairBot 的飞行/避障/返航提取到 Bot 基类，序列化字段及旧脚本 GUID 保留，BlockStatusLight 改为接收 Bot；新 CollectionBot 独立负责回收工作。BlockData 扩展 cargo，加载、敌方蓝图、Undo/Redo、返回结算同步接入；存档原子替换保留备份。未改已有用户场景、素材摆放或项目设置。
- **保护**：修改前创建 `codex/salvage-backup-20260922`；当前未保存的素材预览场景另外保存在 `Temp/SalvageWork/BeforeSalvage.unity`，回归只操作 Play Mode 副本。收尾将原未保存预览内容恢复到独立 `Assets/Art/Temp/Overview-Recovered-20260922.unity`，并另留带哈希清单的 `.codex-backups` 副本；这些恢复文件不提交，原场景文件未覆盖。新增资产均由 Unity API 保存并生成 .meta。
- **验证状态**：Unity 6000.3.11f1 已导入/编译，容量、断电、溢出分流、两机预约、真实无人机往返、特殊掉落不被清理、仓体/回收舱真实摧毁、旧 JSON 和维修机器人回归已执行；最终报告见 `Assets/Art/Salvage/Validation.json`。31 项隔离 Play Mode 检查通过，包含默认避障配置下回收和实际文件返回/死亡保存；没有捕获到新增 Error/Exception。四个 Prefab 重载无缺失脚本/材质，已检查空仓/装载仓 URP 渲染；`dotnet build HY-Sandbox.sln --no-restore --nologo` 0 错误、4 个既有警告。`git diff --cached --check` 通过；生成元数据仅规范化行尾空白，未改变 GUID。
- **边界**：沿用沙盒结束游玩入口返回，尚无独立撤离任务、局外库存消耗、商店或科技树。未完成大型蓝图战斗、并发透明渲染/导航压力和目标设备帧率验证。


### 2026-09-22（连接点放置判定与供电调试旋转修复）

- **修复连接点可放置判定**：新增 `Block.IsConnectorAvailableForPlacement`，同时检查本方 `canConnect`、占用状态和连接面外侧是否已有实体方块；`BuildManager` 的 Ghost 预览与 `ConnectorPlacementHints` 白色线框统一使用该判定，因此对着 `canConnect=false` 的邻接连接点不会显示提示，也不能放置。
- **修复供电调试立方体旋转**：`PowerTransmissionDevice` 更新旧 `DebugCube` 时显式同步宿主对象的世界 `Transform.rotation`，即使该单体调试对象保持隐藏，也不会保留错误的局部/世界朝向。
- **验证范围**：已通过代码检查、`dotnet build HY-Sandbox.sln --no-restore`（0 错误；4 个既有程序集版本/过时 API 警告）和任务文件 `git diff --check`。Unity Editor 已连接且版本为 6000.3.11f1，待完成本轮建造 Play Mode 探针验证。

### 2026-09-21（尾焰连续性及粒子生命周期修复）

- **原因与修改**：原喷焰核心寿命 0.075 秒、18 粒子/秒，强度只乘发射量；10% 推力基线采样 240 帧中 205 帧为空。改为 0.3 秒寿命、20 粒子/秒、恒定尺寸交叉淡化，通过 MaterialPropertyBlock 调节持续尾焰/维修接触亮度；保留原材质和喷口方向，单喷口容量不增加。修复快速停止后仍有存活粒子时的重新发射判断。
- **其他特效**：维修电弧补充淡出并让轨迹继承粒子颜色，烟尘及爆炸光痕补充透明度包络；项目适配粒子统一离屏继续计时，修复残焰和瞬时闪光离屏冻结、重入视野补播。12 个模板及 9 个涉及粒子的 Block Prefab 已通过 Unity API 保存；生成器同步新参数，完全解包和模型/玩法绑定不变。
- **文件与 Editor 验证**：Unity 6000.3.11f1 导入/编译、44 个 Prefab 引用/挂点检查通过；105 组连续采样覆盖 2%/5%/10%/25%/100% 强度与 30/60/120 FPS，无空帧，核心 Alpha 变异系数不超过 0.42%；7 种瞬时粒子结束和重播通过。URP 实际连续帧已检查，10%/100% 尾焰屏幕亮度总量变异系数 2.38%/3.18%，GIF 保存在 Preview 中。
- **运行验证**：隔离 Play Mode 23 项检查通过，包括低推力连续发射、离屏停止/自然结束、炮塔伤害、Bot 维修和返航。返回 Main 场景且无未保存修改。静态 build 为 0 错误/4 个既有警告；git diff --check 通过。未做大型蓝图透明 Overdraw 和 AlwaysSimulate 的并发性能测量。

### 2026-09-21 Block 模型与素材 VFX 集成

- **范围与结果**：完成 16 个功能 Prefab 的 SpaceKit/Temp 适配，保留 4 个基础块与 Connector；替换内容完全解包，补齐被忽略目录的依赖。修复炮塔 aimPivot/Muzzle、全向喷头轴、Bot home/Outside/工具端、初始停靠冷却问题；发电机/维修舱增加状态灯。
- **粒子**：12 个可编辑适配效果，替代运行时程序化 ParticleSystem；多喷口按实际模型端环定位，炮口闪光/命中、电弧维修、爆炸烟尘与建造反馈接入资产库；选中/Ghost 保留特效材质。
- **验证**：Unity 6000.3.11f1 导入编译；全部 44 个 Block/Temp/适配 VFX 重载与引用/Shader/解包检查；19 个 Block 数据与连接掩码前后无差异。独立 Play Mode 探针 20 项通过，覆盖供电、旋转推力、炮塔伤害/闪光、灯光、Bot 工具端/离舱修复归位，以及瞬时效果/烟迹释放，结果见 PlayModeValidation.json。URP 联系表与喷口特写已渲染检查。`dotnet build HY-Sandbox.sln --no-restore --nologo`：0 错误、4 个既存警告；`git diff --check` 通过。
- **未验证**：复杂移动母舰返航、真实玩家长时间操作及大型蓝图并发性能；未将隔离探针冒充完整实机压力测试。


### 2026-09-21

- **筛选并整理科幻备用素材**：新增 `Assets/Art/SpaceKit`、`Assets/Editor/SpaceKitCurator.cs` 及 Editor csproj 编译入口；105 个 Prefab 分为 22 个用途类别，每类 2–7 个，额外 10 个调色板材质，含依赖共 349 项/约 19.02 MiB。全部副本使用独立 GUID，修复静态道具空 Animator 与无效粒子 mesh 字段，旧材质转换 URP。提供来源/用途/尺寸/预算清单、四张实际渲染预览、导入与验证工具；保留两套源包及现有玩法资产。`.gitattributes` 仅对 SpaceKit 的 Unity 原生 `.asset/.mat` 空字段尾空格设例外，避免改写供应商序列化数据。
- **验证范围**：Unity 6000.3.11f1 导入和 C# 编译通过；349 项依赖检查、105 个 Prefab 重载及粒子编辑器采样通过，无外部 Assets 依赖、缺失引用/mesh/material/script；105 项 URP 预览与四张联系表已检查。源文件及 meta 哈希不变、二进制资产一致、GUID 独立和 meta 完整检查通过；`dotnet build HY-Sandbox.sln --no-restore --nologo` 为 0 错误/4 个既有警告，`git diff --check` 通过。最终 Console 无新增错误，保留既有 ProfilerCaptureAnalysis 弃用警告；Main 场景未弄脏。未做玩法接入、Play Mode 或压力验证。

- **迁移 CUBE Spaceships Pack 材质到 URP**：通过已连接的 Unity 6000.3.11f1 Editor 将 `Assets/CUBE - Spaceships Pack 01/Materials` 下 16 个材质从内置管线 Shader 切换到 URP；`Spaceship colorA-L` 和 `Demo Ground` 使用 `Universal Render Pipeline/Lit`，船体材质重新绑定对应 `Spaceship color*.psd` BaseMap 与 `Spaceship glow*.psd` EmissionMap，`Demo Ground` 保持原深灰色；`FX Blue`、`FX Exhaust` 和 `FX Smoke` 使用 `Universal Render Pipeline/Particles/Unlit`，分别保留加法蓝色/喷焰和透明烟雾语义。Unity 同步补全 `ProjectSettings/URPProjectSettings.asset` 的 URP 默认资源路径字段。
- **验证范围**：已通过 Unity Editor `AssetDatabase` 读取确认 16 个目标材质的 Shader、贴图绑定、渲染队列和关键颜色；`FX Exhaust`/`FX Smoke` 位于透明队列并绑定原粒子贴图，12 个船体材质均启用对应发光贴图。Console 当前仍有打开素材包旧 Demo Scene 时产生的既存 `GUI Layer` 缺失错误和 2 条相关警告；本次未进入 Play Mode，也未在场景中逐一视觉检查所有飞船 Prefab。

- **重新修复 LoadSave 相机取景**：移除“加载结束后恢复旧相机姿态”的错误方案；`BuildManager` 在实例化前从完整 `BlockData` 计算含旋转尺寸的固定逻辑包围盒，`CameraController` 按当前环绕方向逐角点求满足水平/垂直视锥的最小距离，不再使用逐帧增长的 Renderer 包围盒或包围球半径。加载完成后镜头停留在以新构造体为中心的正确观察位置。
- **验证范围**：Unity 6000.3.11f1 Editor 重编译通过；主场景 Play Mode 真实加载 `tftftf`（159 个 Block）和 `SpaceShip`（265 个 Block），最终相机到逻辑 Bounds 中心的距离分别为 `19.33`、`31.34`，均低于旧包围球算法的 `23.37`、`35.41`。两组 Bounds 八角点全部位于视口内，`tftftf` 从自定义姿态开始后没有恢复旧位置；Game View 截图确认 `SpaceShip` 完整可见且构图围绕模型中心。Console 为 0 error / 0 warning；`dotnet build HY-Sandbox.sln --no-restore --nologo` 通过（0 错误，4 个既有警告）。

### 2026-09-20

- **移除所有平面环形特效**：删除 `StylizedRingEffect` 及其 `.meta`，清理选中、Ghost、放置、旋转、拆除、普通摧毁、爆炸、陨石冲击和 RepairBot 维修命中的全部环形创建与生命周期。选中/Ghost 继续由原有材质反馈负责，其余事件改用粒子爆发、放射能量光痕、短束流和点光。
- **提高特效亮度与辨识度**：新增共享加法粒子材质，火花、爆炸火球、维修能量和推进器尾焰使用 HDR 色值并驱动 Bloom；烟尘继续使用普通 Alpha Blend。同步提高推进器三层粒子发射量、点光强度/范围，以及放置、拆除、爆炸、维修和陨石冲击的粒子数量、速度与瞬时光照。
- **验证范围**：Unity 6000.3.11f1 Editor 重编译通过；隔离触发选中、放置、旋转、爆炸、维修脉冲和陨石冲击后，运行时禁用环形对象计数为 `0`，截图确认无平面环，Console 为 `0 error / 0 warning`。仍需在大量同时发生的爆炸、维修和推进器尾焰下用目标硬件验证透明 Overdraw 与 Bloom 峰值。
- **强化运行时特效张力（历史实现，环形层已由本日后续修改移除）**：推进器升级为外层喷流、高温核心、拉伸火花和闪烁点光三层尾焰；RepairBot 增加双层速度驱动尾迹、飞行微粒，以及维修命中反馈和真实维修 tick 闪光。
- **强化摧毁与断裂反馈（历史实现，冲击环已由本日后续修改替换）**：Block 爆炸增加白热闪光、火球、放射碎片光痕和延迟滚动烟尘；普通物品摧毁补充火花与烟尘。爆炸后无驾驶舱的断裂刚体会自动挂接速度/旋转驱动的烟雾和余烬拖尾，最长 6 秒且全局最多 24 条。
- **验证范围**：代码已在 Unity 6000.3.11f1 Editor 中成功导入并通过 C# 编译；`dotnet build HY-Sandbox.sln --no-restore --nologo` 通过（0 错误，4 个既有程序集版本冲突/Profiler 过时 API 警告）。已在主场景 Play Mode 使用不保存的隔离探针实际触发三层尾焰、双层维修束/脉冲、分层爆炸、RepairBot 双 Trail 和断裂烟迹；截图确认软粒子透明边缘、束流方向与叠加关系正常，并据此修复 URP 粒子材质不透明及烟迹速度曲线模式不一致。最终烟迹回归 Console 为 0 error/0 warning，退出后 `Main.unity` 保持 `dirty=False`；大型蓝图下多 RepairBot/连续爆炸的透明 Overdraw 与峰值性能仍未验证。
- **炮塔拆分为水平/垂直轴**：`Turret.prefab` 的视觉层级改为固定底座、`Horizontal` 回转平台、嵌套 `Vertical` 俯仰机匣和 `Muzzle` 发射点；模型重做为带轴承环、双侧支架、配重、双联炮管、枪口制退器和青色瞄准镜的方正卡通工业炮塔。生成器会保存并校验全部轴引用，重复重建不会再把双轴结构覆盖成单层模型。
- **双轴瞄准代码**：`TurretWeapon` 将旧 `turnSpeed` 序列化值迁移为 `horizontalTurnSpeed`，新增独立 `verticalTurnSpeed` 和 -15 至 65 度俯仰限制；水平轴依据安装面的局部 Up 回转，垂直轴只修改局部 X 角，枪口位置和方向在本帧转动后重新计算。
- **验证范围**：`dotnet build HY-Sandbox.sln --no-restore --nologo` 通过（0 错误，仅既有程序集版本冲突与 Profiler 过时 API 警告）；Unity 6000.3.11f1 batch Editor 先完成全量生成验证，再通过炮塔专用入口重建 `Turret.prefab` 与预览场景。连接 Editor 确认脚本编译成功、Console 0 error，并完成近景视觉检查；行为探针确认引用为 `Horizontal/Vertical/Muzzle`，上仰/下俯分别钳制到 65/15 度。尚未在主场景 Play Mode 验证自动索敌、开火和移动载具上的实际表现。

### 2026-09-19

- **接入 Unity CLI Editor 连接环境**：通过 `unity pipeline install` 为项目添加 `com.unity.pipeline@0.7.0-exp.1`，同步更新 `Packages/packages-lock.json` 和 Unity 生成的 C# 工程引用，使 CLI 可以发现并控制正在运行的 Unity 6000.3.11f1 Editor。
- **环境验证**：已确认 Unity CLI `1.0.0-beta.8` 在 PATH，Unity 6000.3.11f1 已安装，Unity Personal license active；Codex 使用的 `unity-cli` skill 已刷新并在刷新前备份。重新打开 HY-Sandbox 后，`unity status` 显示 Editor `ready`、端口 `7801`，`unity command --project-path ... --query editor` 和 `unity list --project-path ...` 均成功返回项目命令目录；当前未在 Play Mode 验证运行时行为。账号未登录，但不影响本机 Editor 连接。
- **美化驾驶舱、发电机与维修舱**：驾驶舱加入分离式挡风玻璃框、前鼻装甲、侧舱、驾驶台和青/琥珀仪表灯带；发电机加入方形基座、四角硬边立柱、前后左右交叉支撑与顶部护栏；维修舱移除遮挡 RepairBot 的实心外壳，改为低底座、后框、侧导轨、维修夹具和工具梁，保持 +Z 正面开放。RepairBot 主体改用沙金工具色并保留青色视觉传感器。
- **验证范围**：通过已连接 Unity Editor 的 `Tools/HY Sandbox/Rebuild Industrial Art` 菜单成功重建 20 个模块 Prefab、Connector、共享材质/Mesh 和预览场景；Editor Console 无新增 Error，仅有既存 `ProfilerCaptureAnalysis.WriteCounters` 过时 API 警告。已在 `IndustrialArtPreview` Scene View 对驾驶舱、发电机和维修舱完成近景截图核对；尚未在主工程 Play Mode 验证实际驾驶视角、RepairBot 运行时进出舱和大型蓝图性能。
- **打包验证**：使用 `AutoBuildTool.BuildWindows` 在 Unity 6000.3.11f1 batch Editor 中完成 Windows x64 构建和 ZIP 压缩，输出 `Builds/HY-Sandbox_v0.1.25_Win64.zip`（约 64.0 MB，版本 0.1.25）；构建日志记录 `Build Finished, Result: Success`。连接编辑器触发的第一次尝试曾在 Shader 变体编译阶段崩溃，随后 batch 重试成功；该异常不影响最终包生成。

### 2026-09-15

- **暂时禁用全部 LOD**：`IndustrialArtGenerator` 不再为模块创建 `LODGroup`，重新生成后 20 个模块只保留一套 `LOD0` 视觉层；这样卡通材质、色块和轮廓在镜头远近变化时不会跳变。
- **切换为卡通工业材质配色**：共享材质从统一黑色/冷灰方案改为接近真实工业材料的分类色：蓝灰喷涂钢（普通结构）、工程黄/黄铜（装甲与安全件）、铝银（边缘/连接件）、设备绿（供电）、高温橙（推进器）、信号红（武器）、海军蓝（驾驶舱）和沙金（工具/维修）。功能模块的基础外壳通过类别材质替换，青色/琥珀/红色发光件仍保留为状态与工作端提示。
- **Connector 资源路径兼容**：生成器优先处理当前 `Assets/Resources/Blocks/Connector.prefab`，若旧路径仍存在则兼容 `Assets/Connector.prefab`；不改变既有 Connector GUID 和 Block 的序列化引用。
- **验证范围**：已通过 `dotnet build HY-Sandbox.sln --no-restore --nologo`（0 错误，仅既有 Profiler 过时 API 警告）；Unity 6000.3.11f1 隔离工程成功导入并执行生成器（20 个模块 Prefab、共享材质/Mesh、预览场景保存成功，退出码 0）；文件检查确认模块无 `LODGroup`、类别材质已写入 Prefab、Connector GUID 引用保持一致。尚未在主工程 Play Mode 复核实际镜头下的卡通观感、选中高亮与大型蓝图性能。

### 2026-09-18

- **改为方正模块化外形**：新增共享 `M_Box` Mesh，普通结构块从圆角外壳改为硬边长方体/正方体，六面仍保持同一轮廓和材质；驾驶舱、发电机、输电中继、陀螺控制器、推进器、炮塔、维修舱等主体机匣同步改用盒体骨架，保留楔体、圆柱、环和发光件表达功能。
- **强化卡通工业配色**：提高蓝灰结构钢、橙色推进器、青绿供电、信号红武器、海军蓝驾驶舱和沙金工具件的色差与明度，保持金属度/粗糙度差异以模拟涂装钢、铝、玻璃和热端材料；状态灯仍使用青色、琥珀和红色自发光。
- **验证范围**：已通过 Unity 6000.3.11f1 批处理重新生成 20 个 Prefab、Connector、共享材质/Mesh 和预览场景；`AutoBuildTool.BuildWindows` 成功生成 `Builds/HY-Sandbox_v0.1.23_Win64` 及 ZIP（约 66.9 MB），并按工具逻辑将版本递增到 `0.1.23`。已确认普通结构引用 `M_Box` 且所有模块无 `LODGroup`；主工程 Play Mode 下的实际视觉观感、选中高亮和大型蓝图性能仍待验证。

### 2026-09-14

- **建立工业玩具 / 霓虹工程舱渲染风格并替换占位方块**：新增 `Assets/Editor/IndustrialArtGenerator.cs`、`Assets/Scripts/Effect/IndustrialPartMotion.cs` 与 `Assets/Art/Industrial` 资源。生成器在 Prefab 隔离阶段保留根 `Block`、尺寸/质量、碰撞体、`Connectors`、`Debug`、嵌套 RepairBot 和 `Resources` 路径，只重建 `Model/IndustrialVisual` 视觉层；20 个模块 Prefab 已生成圆角装甲、角撑、楔体、低面数环体、发光能源/推进/武器部件。共享材质开启 GPU Instancing；普通结构块不加 LOD，2x2x2 与功能模块使用两级 LOD；发电机环、输电中继信号环、陀螺、涡轮、炮塔轴承和 RepairBot 转子使用轻量 Transform 动画，发光脉冲通过 `MaterialPropertyBlock`。
- **渲染与预览**：PC URP MSAA 调整为 2x；`SampleSceneProfile` 启用 ACES、Bloom、轻量对比度/暗角和运动模糊。新增 `Assets/Art/Industrial/Preview/IndustrialArtPreview.unity` 及截图，包含暗色展示台、冷白主光、青色轮廓光和 16 类模块展示。选中/取消选中逻辑改为暂存并恢复 `Model` 下所有 Renderer 材质，适配多 Renderer 功能件。
- **验证范围**：已通过 Unity 6000.3.11f1 Editor 导入、生成器执行（20 个 Prefab）、独立 Camera 截图和 Play Mode 动画探针（发电机环旋转）；本次验证 Console 无新增 Error，脚本重编译 `failed=false`，`dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误；包含既有 Profiler 过时 API 与 Unity 生成项目引用警告）。已确认输电设备用户现有 `Debug/Cube` 缩放 `(5,5,5)` 保留。尚未完成大型蓝图 GPU/CPU 压力、LOD 远近切换、移动设备画质和完整建造/存档/游玩回归；预览截图仅证明渲染资源可见，不替代完整 Play Mode 验收。

### 2026-09-01

- **修复连接虚线运行时异常**：`PowerTransmissionDevice` 不再通过 MonoBehaviour 实例字段初始化器创建 `MaterialPropertyBlock`，改由 `Awake` 在 Unity 允许的生命周期阶段初始化，并在绘制虚线前执行空值兜底；解决构造阶段 `CreateImpl` 异常及其后续传入空 PropertyBlock 导致的 `ArgumentNullException`。
- **无线范围改为轴对齐立方体**：`PowerTransmissionDevice` 对发电机、相邻输电设备和 `Power` 负载统一使用世界坐标逐轴范围判断，`maxConnectionDistance` 与 `powerRange` 均表示半边长；Scene Gizmo 同步显示边长为 `2 * range` 的真实判定范围。
- **合并供电 DebugCube**：旧单体 DebugCube 在运行时保持隐藏；`DebugManager` 使用所有范围的 X/Y/Z 边界做坐标压缩，重叠单元按有功率、已连接、孤立的优先级归属状态，仅为没有相邻占用单元的一侧生成 Mesh 面。结果是一个含三个颜色子网格的并集外壳，内部重叠面不再参与透明渲染；只有设备位置、范围或网络状态签名变化时才重建。
- **验证范围**：已通过代码差异检查、`git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 `ProfilerCaptureAnalysis.WriteCounters` 过时 API 警告）；Unity CLI 未发现安装 Pipeline 的 Editor 实例，尚未在 Unity 6000.3.11f1 Editor/Play Mode 验证立方体边界连接、并集网格透明效果、三种状态交界和大量移动设备时的构建开销。

### 2026-08-27

- **补全无线输电网络**：`PowerGeneratingUnit` 注册并提供非负 `outputPower`；`PowerTransmissionDevice` 按设备最大连接距离建立发电机/中继双向图，遍历包含共享发电机的连通网络，将同网发电功率汇总后均分给所有供电半径内去重的 `Power` 负载。多个独立网络可对同一负载叠加供电，网络每帧先清空旧状态，因此移动、禁用、销毁或断连不会永久保留旧功率；Scene 视图选中设备时显示供电和连接半径。
- **接入用电设备状态与效率**：`HoverFlightController` 断电时立即清空悬浮推力，效率同时缩放悬浮输出和姿态修正；`Thruster` 基类统一缓存 Power、按效率缩放推力，并在断电时绕过变化率限制直接清零；`TurretWeapon` 断电时停止索敌/开火并隐藏光束，效率降低时按比例缩放伤害并延长开火间隔。
- **验证范围**：已通过代码差异检查、`git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 `ProfilerCaptureAnalysis.WriteCounters` 过时 API 警告）；尚未在 Unity 6000.3.11f1 Editor/Play Mode 验证移动中继、多网络覆盖、运行时断连、大量负载性能、推力手感和炮塔射速。

### 2026-08-29

- **完善供电调试显示**：新增 `DebugManager` 的范围/连接开关、状态颜色和虚线参数；旧版本 `PowerTransmissionDevice` 曾将供电范围调试对象按球形半径显示并按孤立、已连接和有功率状态变色，修复全局网络刷新提前返回导致只有首个设备更新视觉的问题（范围几何已于 2026-09-01 改为立方体并集）。
- **显示无线网络连接**：输电设备为每条相邻 `PowerGeneratingUnit` 或 `PowerTransmissionDevice` 复用运行时 `LineRenderer`，使用重复半透明纹理形成虚线并按时间滚动；设备/设备连接只绘制一次，断开或关闭调试时隐藏全部连接线。旧版本输电 Prefab 的调试网格曾为球体，当前由 DebugManager 统一生成无重叠立方体外壳，主体网格保持不变。
- **验证范围**：已通过代码检查、`dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 `ProfilerCaptureAnalysis.WriteCounters` 过时 API 警告）；尚未在 Unity Editor/Play Mode 验证 URP 透明虚线材质、移动设备连接线刷新、范围颜色在不同功率状态下的视觉效果和大量网络节点性能。

### 2026-08-25

- **BlueprintUIPanel 显示逐块加载进度**：`LoadAllBlocksRoutine` 在既有主 `for` 循环中按成功实例化的 Block 累加数量和质量，每恢复一个方块立即更新面板；加载开始先归零，完成后不再调用层级扫描刷新。坏数据清理和连接重建的原有遍历保持不变，不参与 UI 统计。
- **验证范围**：已通过代码检查、`git diff --check`（任务脚本与文档）和 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 Profiler API 过时警告）；尚未在 Unity Play Mode 验证大蓝图逐项更新、缺失 Prefab、空蓝图初始化和快速切换文件时的显示。

- **接入 BlueprintUIPanel 实时统计**：主场景中新建的蓝图信息面板显示当前玩家存档或敌方蓝图名称、缓存方块数量及已加载方块总质量；`BuildManager` 在搭建、拆除、Undo/Redo、建造目标切换和异步加载完成后刷新显示。统计按缓存 ID 筛选已加载方块，避免 Unity 延迟销毁导致删除当帧仍被计入。
- **验证范围**：已通过代码与场景序列化引用检查、`git diff --check`（脚本与文档）和 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 Profiler API 过时警告）；尚未在 Unity 编辑器或 Play Mode 验证实际文本刷新、不同质量 Prefab、连续 Undo/Redo 和加载中快速切换文件。

- **优化 RepairBot ReturningHome 减速**：返航不再持续加速后仅硬截断总速度；改为相对 Home 的速度控制，按 `sqrt(2ad)` 根据剩余距离计算可停车速度，并结合 `returnBrakeDistance`、朝向一致性和 Home Rigidbody 接近点速度平滑施加受限加速度。保留 `returnStopSpeed` 的最低接近速度，避免接近点前悬停锁死；Docking 捕获范围改用相对 Home 接近点的速度，防止移动中的 Home 导致过早停靠。
- **验证**：已通过 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 Profiler API 过时警告）和 `git diff --check`；尚未在 Unity Play Mode 验证静止/移动 Home、高速返航、急转弯和不同刚体质量下的制动距离。

### 2026-08-24

- **降低 RepairBot 返航计算压力**：`ReturningHome` 改用 Home 停靠点的低频局部引导，Home 以少量 `RaycastNonAlloc` 射线评估各方向开阔度和对当前 RepairBot 的方向一致性，返航不再执行 RepairBot 全量环形 Raycast、OverlapSphere 和速度预测检测；Home 自身与 RepairBot 碰撞体会被过滤。
- **验证**：已通过代码检查；尚未在 Unity Play Mode 验证移动 Home、Home 被复杂结构包围和多 RepairBot 同时返航时的路线质量与计算耗时。

- **修复 RepairBot 返航与避障冲突**：`ReturningHome` 保留避障，但紧急、主要和预测检测半径会随动态接近点距离缩短，返航避障方向最多偏离回家方向 65 度，避免母体排斥与返航目标形成平衡锁死；进入 Docking 的捕获距离同时考虑配置容差和当前速度在一个物理步内的位移，避免高速越过接近点。
- **验证**：已通过 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 Profiler API 过时警告）和任务文件差异检查；尚未在 Unity Play Mode 验证移动中的 home、高速返航和复杂载具外形下的停靠表现。

- **RepairBot 导航状态机**：用 `NavigationState` 替代独立的精确停靠布尔值和 `SetDockedState`，统一管理 Idle、NavigatingToTarget、ReturningHome、Docking 四种状态，以及 Rigidbody 物理模拟、碰撞和 TrailRenderer 的启停。
- **验证**：已通过代码检查、`dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既有 Profiler API 过时警告）和暂存差异检查；尚未在 Unity Play Mode 验证状态切换与离家/返航交互。

- **降低 3D 物理模拟计算压力**：将 `ProjectSettings/TimeManager.asset` 的固定物理步长从约 0.01 秒（100 Hz）调整为约 0.02 秒（50 Hz），把单帧物理追赶上限从 0.333 秒收紧到 0.1 秒；将 `ProjectSettings/DynamicsManager.asset` 的默认位置求解迭代从 6 次降为 4 次，速度求解迭代保持 1 次。2D 物理设置、碰撞层矩阵、重力和碰撞回调复用未改动。
- **影响**：正常帧下物理步数约减半，单步求解开销降低；极端卡顿时最多追赶 5 个 0.02 秒物理步，避免物理追赶长时间占满主线程。高速碰撞、方块堆叠稳定性和推进器响应仍需按目标设备校准。
- **验证**：已通过项目设置文件检查，确认 Unity 版本为 6000.3.11f1、固定步长有理数配置对应约 0.02 秒、3D 求解迭代为 4/1，并完成 `git diff --check`；尚未在 Unity 编辑器或 Play Mode 进行实际帧率、物理稳定性和碰撞穿透验证。

### 2026-08-23

- **降低 RepairBot 寻路方向变化率并平滑返航**：新增 `NavigationState`（Idle、NavigatingToTarget、ReturningHome、Docking）管理目标导航、返航和精确停靠状态；避障 Raycast/OverlapSphere 结果按间隔缓存，但目标方向每个物理帧使用最新世界坐标计算，以跟随移动中的 home；返航先导航到 `homeOffset` 上方一格的接近点，再切换运动学状态并用代码精确移动到动态停靠点，停靠在 home 时禁用 Rigidbody 物理模拟和 TrailRenderer，离家时恢复两者。
- **验证**：已通过代码检查；尚未在 Unity 编辑器或 Play Mode 验证 Inspector 参数、不同停靠偏移和拥挤障碍场景下的实际运动表现。

- **限制无用 Group 数量**：`PlayManager.maxUselessControlUnitGroups`（默认 32）只统计没有 Cockpit 的 `ControlUnit`；有效 Group 不占用该上限。超过上限时仅将超出的无 Cockpit Group 交给 `DestroyManager.ScheduleUnitCleanup`，延迟清理前再次确认 Group 仍无 Cockpit。
- **验证**：已通过代码检查；尚未在 Unity Play Mode 验证大量断开、敌方生成和清理延迟期间重新分组的实际表现。

- **主动清理远距离 Group**：`PlayManager.groupCleanupDistance`（默认 200）以玩家 `blocksParent` 为参考，排除玩家自身 Group，每秒检查一次并对超出距离的其他 Group 调度 `DestroyManager.ScheduleDistantGroupCleanup`；等待现有清理延迟后再次确认仍在范围外才销毁，返回范围内的 Group 会被保留。
- **验证**：已通过代码检查；尚未在 Unity Play Mode 验证敌方 Group 往返边界、玩家移动和清理延迟期间重新分组的实际表现。

- **修复 RespawnButton 被死亡标题拦截点击**：确认延迟并非 Fade 导致，而是 `DeathPanel/You Died!` Text 位于 RespawnButton 上层，其 RectTransform 下边界与按钮上半部分重叠，且 `raycastTarget` 开启。现已关闭该纯展示文本的射线接收，避免 `GraphicRaycaster` 将点击发送给标题而非按钮。
- **验证**：已通过场景 YAML 检查确认 DeathPanel、RespawnButton 和标题的层级、矩形范围及 `raycastTarget` 配置；`git diff --check` 与 `dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误、0 警告）；尚未在 Unity Play Mode 验证死亡后按钮全区域点击。

- **审查并完善 RepairBot 离家修复范围**：修复 `InitializeTargetsInRange` 在 `home` 赋值前访问导致的空引用；将一次性分配的 `OverlapBox` 快照改为按 `findTargetInterval` 执行的 `OverlapSphereNonAlloc` 动态扫描，只保留与 home 同属一个 `ControlUnit` 且中心位于 `targetRange` 内的方块。当前目标修满、越界或重组到其他单元后会立即取消。
- **性能处理**：复用 Collider 缓冲区、List 和 HashSet；缓冲区仅在饱和时扩容且上限 1024；扫描间隔运行时至少为 0.25 秒，目标选择及范围验证使用平方距离，避免重复数组分配、无上限高频查询与距离开方。
- **验证**：已通过 `git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore`（0 错误；仅有既存 Profiler API 过时警告）；尚未在 Unity Play Mode 验证加载中生成、运行时重新分组、多个 RepairBot 同时扫描及超大型构造体的目标覆盖情况。

- **修复玩家死亡后 Respawn 按钮延迟可交互**：死亡入口现在停止旧的面板淡入淡出协程，立即关闭 PlayPanel、启用 DeathPanel 的 `CanvasGroup` 射线与交互，并显式恢复 `respawnButton.interactable`；避免旧协程或 PlayPanel 的 CanvasGroup 在死亡界面上方继续拦截点击。
- **验证**：已完成代码检查；尚未在 Unity Play Mode 验证连续死亡、快速重生和鼠标焦点切换流程。

- **修复爆炸物理作用力未命中周围刚体**：原实现仅对同一 `ownerUnitId` 的编组 Rigidbody 调用 `AddExplosionForce`，而重新分组后的 Rigidbody 中心可能位于爆炸半径外，且外部附近刚体完全未被收集。现在通过 `Physics.OverlapSphere` 收集范围内刚体，并结合编组内实际子方块位置；按最近 Collider 受击点计算距离衰减后用 `AddForceAtPosition` 施加冲量。
- **验证**：`dotnet build HY-Sandbox.sln --no-restore` 与 `git diff --check` 已通过；尚未在 Unity Play Mode 验证不同编组、静态刚体和边界距离下的实际位移表现。

- **增强驾驶舱爆炸表现与持续时间（历史实现，平面冲击环已于 2026-09-20 移除）**：延长爆炸火花、烟雾和闪光的生命周期，并在 0.22 秒后播放受控数量的余震火花，使爆炸由单次瞬时效果变为分阶段表现。
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

- **新增驾驶舱摧毁爆炸试用（历史实现，平面环形闪光已于 2026-09-20 移除）**：`DestroyManager.ExplodeBlock` 会断开并脱离被摧毁的驾驶舱，调用 `PlayManager.RefreshGroup` 重新生成模块组，对各组 Rigidbody 施加径向冲量；`VisualEffectsManager` 增加火花、烟雾、瞬时点光和镜头震动。当前不造成伤害。
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

---

# 2026-11-04 前的开发入口快照

以下完整保留本轮复用治理前的 DEVELOPMENT，含历史验收及已被后续修改替代的说明；当前状态以 [开发入口](../DEVELOPMENT.md) 为准。

# HY-Sandbox 开发与审查入口

## 2026-11-04 撞击致死恢复爆炸

`DestroyManager.DestroyGameObject` 和 `BuildingRuntime.DestroyBlock` 不再用 `Durability.DestroyedByCollision` 排除爆炸。启用 `canExplode` 的方块撞毁时恢复既有爆炸特效/后续处理；单位继续使用断链、重组、有界冲量和碎片碰撞保护，建筑继续使用范围伤害与地形破坏批次。普通方块及编辑态销毁规则保留。

Unity Main Play 中 `PhysicsSandboxProbe.Run()` 33项通过，覆盖撞击致死爆炸、普通块不爆炸、重复销毁不重复爆炸、建筑撞毁爆炸，以及现有真实PhysX能量与爆炸保护检查。详情见 [验证记录](Validation/2026-11-04-impact-explosion.md)。

## 2026-11-04 建造预览朝向与坐标轴生命周期

`BuildPreview.Poll` 恢复资源默认整格朝向，不再继承瞄准方块的旋转；保留真实连接点对齐和大尺寸吸附。额外跟转来自 2026-09-23 的 `d767c2b`，10月重构仅迁移该实现。直接更改公开资源路径时会重建 Ghost，避免旧模型与实际放置资源不一致。

`BuildSelection` 不再将移动/旋转轴挂到可销毁的方块下；`BuildManager.Update` 刷新坐标轴位置、朝向与可见性，模式切换保留选择，失去选择时隐藏，重新选择后恢复。禁用/启用管理器同步轴显示；未初始化方向时避免零向量 LookRotation。

Main 隔离 Play 探针115项通过，覆盖三轴90°目标、连接点对齐、资源切换、移动/旋转按钮、视角切换、选择销毁和重选、轴位置跟随。验证细节见 [本轮记录](Validation/2026-11-04-build-preview.md)，函数图见 [放置与操作](RuntimeFlow.md#放置选中操作和持久化)。

## 2026-11-02 建筑生成唯一来源

`BuildingBlueprints`现在是World生成与WorldLab建筑选择的唯一蓝图来源。WorldBuildingLibrary不再读取Resources内置建筑，也没有同名回退；空目录或全部无效时不生成建筑。WorldLab初始布局保留Catalog中的位置/朝向，但仅按名称解析共用文件仓，文件缺失或无效则跳过。制作参考资源和建筑编辑器的显式复制入口保留，复制到该目录后才能参与生成。

每次世界会话重新读取文件；删除后重新开始会话清空旧模板、实例和休眠数据。当前会话已生成的建筑及WorldLab手动恢复的实例快照保留其独立状态，文件删除不作为实时拆除命令。11项隔离Editor检查、4项真实Main Play会话检查通过，详见[验证记录](Validation/2026-11-02-building-source.md)。

## 2026-11-02 建筑删除与显式新建

建筑编辑不再把打开当作新建，也不自动补回`default_building`。快捷键进入时优先恢复仍存在的上次选择，否则打开按名称排序的第一份建筑；目录为空则保留建筑模式并显示新建界面。只有明确新建才写文件。删除当前项会取消旧恢复、清理操作状态后打开剩余项；删空则清除名称、父对象、底座和缓存。删除其他建筑不切换当前项，建筑删除确认不再统一跳回玩家存档。空目标禁止加载/放置，尺寸与据点面板隐藏，拒绝空名称路径。

使用原建筑隔离Play探针扩展覆盖新建、删除、重新进入与缺失文件打开；验证见 [本轮记录](Validation/2026-11-02-building-delete.md)。实际用户已有default_building文件保留，未替用户执行删除。


## 2026-11-02 建筑蓝图共用读取入口

World与WorldLab通过SaveManager的建筑文件仓获取用户蓝图：`GetBuildingBlueprintFiles()`在Main返回现有`BuildingFiles`，独立WorldLab没有管理器时复用同一路径规则创建文件仓。WorldBuildingLibrary统一列表、解析、4MB限制和坏文件隔离；WorldBuildingPalette不再独立扫描目录和解析JSON。内置合并已由上方“建筑生成唯一来源”改动移除，世界资源异步预热与生成平台约束继续归World。

9项隔离Editor检查通过，覆盖注入文件仓、名称、合并、坏/锁定/超大文件、WorldLab共用及只读源文件；真实世界会话回归与静态检查详见 [本轮验证](Validation/2026-11-02-building-catalog.md)。


## 2026-10-31 Siege 场景部署与三轮精简

- 入口：`Assets/Scenes/Siege.unity`；整备点击场景平台或其 + 标记，选择蓝图并预览，确认后才部署并扣费。固定Bays列表已移除；点击友舰打开驾驶/维修卡片，Tab切换存活舰船，战斗中Alt释放指针。
- `SiegeHud` 负责阶段与顶栏；`SiegeDeploymentView` 负责按需交互，`SiegeBattleFeedback` 负责波次/击毁/受击反馈。攻方选择和目录刷新移入F3调试页；F2显示操作帮助。
- `SiegeSession.TryDeployAt` 精确验证指定位置，不自动换位；舰船身份仍为独立部署ID，场景位置仅用于部署和修缮回位。`TryDeploy` 自动选空位入口保留给既有压力夹具，委托同一部署实现。
- 驾驶镜头与物理边界分别在 `SiegeFleetView` / `SiegeFleetPhysics`；修缮由Session统一入口发起，明确 `RefitChanged` 事件刷新报价，不再依赖Status文本。原经济/货物/蓝图身份约束保持。
- `SiegeWorkshopStage` 将战场暂存于不模拟的LocalPhysicsScene，蓝图在独立预览场景编辑；显式保存/恢复层、镜头掩码、父节点和场景，跨场景迁移不初始化Durability伤势。取消异步加载后再卸载预览场景。
- 制作入口 `HY-Sandbox/Siege/Apply Scene Deployment Interface` 保存真实UGUI；旧HUD/选择/布局菜单委托同一入口，四个Siege场景同步迁移，保留原GUID和竞技场布置。
- `SimulationWorkMetrics` 增加Siege阶段切换、友舰物理、波间修缮、工作台计时；Editor采样增加观测区间增量，Player survey同步记录，嵌套阶段不能相加为整帧耗时。
- Editor功能回归135项通过；独立Windows D3D11 Player在1080p/720p各67项通过（含工坊隔离），显式驾驶输入83项通过，三个压力场景仅验证加载/引用。静态构建0错误/4项既有警告，格式检查通过。90秒Editor自然样本只到首波，不声称FPS改善或后期长帧解决。
- 验证与已知边界见 [三轮验证记录](Validation/2026-10-31-siege-streamline.md)。源码函数图由 `Tools/CodeReadability/generate_navigation.py` 维护。


## 2026-10-27 连击上限与难度曲线

- 新增 `comboBonusCap`（默认300，0不限制）给单次连击奖励封顶，避免蜂群后期20连击一次拿1,140金币打穿经济；连击计算抽成纯函数 `SiegeSession.ComboBonusFor`，规则检查可直接覆盖起点与上限。
- `Docs/SiegeWarfare.md` 补“难度曲线（默认配置）”表：蜂群3/6/9/12/18/21/24波、精英5/10/15/20/25波、第15波精英优先、第24波起蜂群被48上限截断，以及单波收益构成。
- `SiegeCombatFeedbackChecks` 增至 **25项**并全部通过（新增连击起点与上限2项）。详见 [连击上限验证](Validation/2026-10-27-siege-combo-cap.md)。上限数值未做长时平衡验证。

## 2026-10-27 阵地战波次结算战果

- 波次清空横幅现在显示本波战果 `n KILLS · +x CREDITS`：会话在开波时记录金币与击毁基线，进入整备时一次性算出差值（统计点在自动修缮之前，语义是“这一波赚到的”）。同帧顺序不确定时退回原提示语，不显示0。
- 真实Play第1波读数 `2 KILLS · +650 CREDITS`（120×2+350+连击60）；`SiegePlayChecks` 28/28、`SiegeCombatFeedbackChecks` 23/23 重跑通过。详见 [波次战果验证](Validation/2026-10-27-siege-wave-result-banner.md)。未做长期多波读数抽样。

## 2026-10-26 阵地战蜂群波次

- 新增蜂群波：每 `swarmEveryNWaves`（默认3）波数量翻倍、生成间隔×0.65，与精英波错开；同波重叠时精英优先，避免倍率叠加不可玩。名额仍受 `maximumWaveSize` 与同屏上限约束。
- HUD 横幅 `SWARM WAVE n / k RAIDERS · FAST SPAWN`；构造参数可选，既有规则测试行为不变。检查增至21项全部通过；真实Play第3波 `swarm=True pending=8` 并截屏。详见 [蜂群波验证](Validation/2026-10-26-siege-swarm-wave.md)。未做蜂群+精英连续节奏与帧时间对照。

## 2026-10-26 后段波次性能采样与尖峰定位

- 自然观察窗口改为优先买最贵的可负担舰，无人驾驶下推进到第5波精英才失守：164.8秒/9,266帧，中位15.82ms、P95 34.36ms、P99 59.29ms、最大272ms（495帧>33ms、15帧>100ms）。第5波规模：约1,894模块/2,176碰撞体/341刚体。
- 最慢帧集中在**波次边界自动修缮**（272ms，此时仅12个唤醒刚体）与**敌舰断裂重组**（291ms）。带 `-siege-checks-profile` 的累计计时显示 refresh 556ms/190次、bodyRebuild 398ms/178次、connectivity 60ms/178次、约11.9万次邻接查询；计时为累计值，不能把单帧直接归因。
- 更正：给 `RepairBaseBetweenWaves` 加临时计时后实测自然清波的修缮只有 base 3ms + fleet 10ms（359模块基地、1艘待修舰），量级对不上272ms，**边界尖峰不是修缮本身**，剩余嫌疑是阶段切换时的物理冻结，未验证；计时器已移除。结论只到“后段成本显著高于首波、尖峰在边界与重组”，未优化、无GPU/PhysX归因。数据与边界见 [Player帧记录](Validation/2026-10-26-siege-player-soak.md)，两份JSON已入库。
## 2026-10-26 HUD开关同场景对照与诊断开关

- 新增两个仅 `-siege-checks-report` 生效的诊断开关：`-siege-novsync`（关闭垂直同步与目标帧率）、`-siege-hud-off`（停用HUD对象，同时跳过界面几何断言）。它们不改变正常游戏路径。
- 同场景各90秒自然战斗对照：HUD开启 8,990帧/中位10.00ms/P95 10.50/P99 10.88；HUD关闭 8,986帧/中位9.99ms/P95 10.50/P99 11.02。**该负载下HUD不是瓶颈**；中位仍卡在10.00ms，说明存在外部呈现上限，本次对照不能测出亚毫秒差异，也不声称FPS提升。数据见 [Player帧记录](Validation/2026-10-26-siege-player-soak.md) 与仓库内两份AB JSON。

## 2026-10-26 自然战斗Player帧记录

- 独立Windows D3D11发布包（非Development，3200×2000）跑 `-siege-checks-soak`：**86项通过/0错误**，9,234帧/93.83秒，中位 **9.99ms**、P95 **10.54ms**、P99 **10.93ms**，最大帧86.84ms（生成期），>100ms长帧0个。第1波核心被击毁，只跑到93.8秒。
- 中位与P99贴着10ms，说明该负载下帧时间被钳制，**不能据此判断性能余量，也不声称UI简化带来FPS提升**；未开启嵌套工作计时，没有BFS/刷新分项。数据与边界见 [自然战斗帧记录](Validation/2026-10-26-siege-player-soak.md)，原始JSON已随仓库保存。

## 2026-10-26 阵地战战果记录与失败结算

- 新增 `SiegeProgress`：失败时把最高波次、累计击毁、总场次写入本地 `PlayerPrefs`，`SiegeSettings.trackRecords` 可关闭。HUD 在整备阶段金币行显示 `BEST n`，失败横幅显示 `NEW RECORD · WAVE n` 或 `BEST WAVE n`。
- 失败后不再显示舰队坞（没有可执行操作），只保留 `RESTART SIEGE` 与按键提示；横幅与顶部 `BASTION · LOST` 保留。真实Play确认 `BEST 7` 显示与 `BASTION LOST / BEST WAVE 7` 横幅，验证用的临时纪录已还原为原值。
- `SiegeCombatFeedbackChecks` 增至17项（新增4项记录写入/累计/不覆盖/恢复），17项全部通过。详见 [战果记录验证](Validation/2026-10-26-siege-progress-record.md)。连续多局刷纪录与Player/编辑器PlayerPrefs共享行为未验证。

## 2026-10-26 阵地战来袭方向标记

- 夜色场景暴露了可读性问题：敌舰在130米外进场，默认视角看不到。新增 `SiegeThreatMarkers`（挂在HUD，由 `Apply Command HUD` 幂等装配）：固定6个橙色菱形+距离标记，只在敌人不在视野内或超过90米时显示，0.1秒节流，画布坐标经 `RectTransformUtility` 换算后夹到边缘。
- 真实Play确认第1波两架视野外敌舰显示 `88 m`/`123 m`；运行时位置读数证实标记贴边且在画布内（画布宽2347而非1920，直接混用屏幕坐标会顶出屏幕）。参数均为Inspector字段。详见 [威胁标记验证](Validation/2026-10-26-siege-threat-markers.md)。6舰重叠、竖屏比例与CPU占用未验证。

## 2026-10-26 阵地战修缮报价、核心无伤奖励与Player验证

- 整备阶段两个修缮按钮显示真实报价（`REFIT BASE CORE 127` / `PILOT SHIP INTACT`），报价只在进入整备、点击修缮和状态变化时计算，避免在高模块基地上每帧遍历方块。玩家不用盲猜金币是否够。
- 新增“核心未受损”奖励（默认80×波次），在自动修缮之前判定，避免全额修好被误判为无伤；状态行写明 `Core intact +N`。实测第1波+80、第2波+160。
- 修复 `CargoHoldView` 在蓝图恢复实例上的 `MaterialPropertyBlock` 空引用（修复前同一场Play累计50条），修复后全新Play `consoleErrors=0`。修复 `SiegePlayerChecks` 的旧部署假设，改为按当前蓝图目录报价挑可负担项。
- 独立Player验证包实测 **91项通过/0错误**，覆盖HUD几何与文本、部署、开波、真实死亡结算、接管、核心被毁与重开，并断言精英节奏与连击门控在新构建中生效。详见 [稳定性与成本核对](Validation/2026-10-26-siege-hud-stability-cost.md)。HUD帧成本仍无可靠对照，未声称FPS变化。
- 修回 `SiegePlayChecks` 回归套件：逐项记录失败不再首错中断；壳体断言改为对比部署时的连通分组基线（用户蓝图可以有半格偏移的独立吊舱）；自动修缮的精确扣费断言先把连击单价临时归零。修复后**28项全部通过**，此前“每次停在不同断点”的状态结束。

## 2026-10-26 阵地战夜色竞技场外观

- 新增 `SiegeArenaLookAuthoring`（菜单 `HY-Sandbox/Siege/Apply Night Arena Look`）：程序化夜空材质 `Assets/Siege/NightArenaSky.mat`、与天空匹配的雾、冷色环境光与月光，并把竞技场地面扩到900×900使边缘落在雾程之外，去掉旧版生硬的直角地平线。地面仍是单个静态BoxCollider，没有新增渲染批次或碰撞体。
- `VisualEffectsManager` 的雾色/雾密度/环境光参数由硬编码改为可编辑字段，默认值与原常量一致，Main场景外观不变；Siege场景单独覆盖为夜色。截屏与边界见 [夜色竞技场](Validation/2026-10-26-siege-night-arena-look.md)。夜色下敌舰可读性、Player天空盒与同机位帧时间尚未验证。

## 2026-10-26 阵地战精英波次、连击奖励与受击告警

- 战斗反馈补上正向与负向两端：每5波出现精英波次（默认），使用攻方目录里模块数最多的蓝图，击毁与清波奖励翻倍，横幅与顶部栏给出明确标记；连击窗口内连续击毁按次数发金币（默认5秒、每次+60），同一帧多架同时被毁也按次数累计。
- 核心受击时四边红色告警条短暂增强，核心低于30%保持低频脉动；同一帧请求一次镜头抖动，复用既有 `CameraShake` 协程。告警每帧只写一个 `CanvasGroup.alpha`，连击只在计数变化时计算，精英挑选只在生成时读一次模块数。
- `SiegeMatch.GrantBonus` 接受战斗与整备两个阶段，以便波次最后一架仍可结算连击；部署阶段与失败后拒绝。新增13项规则检查与真实Play探针（精英标记、双倍奖励、连击x2与击杀提示、低核心告警），证据见 [精英与连击验证](Validation/2026-10-26-siege-elite-combo-alarm.md)。镜头抖动强度与自然推进到第5波的体验仍未验证。

## 2026-10-26 阵地战指挥栏与战斗阶段HUD

- 旧Siege界面把Main的世界准备遮罩、推进器窗口、物理遥测和整段按键说明一起常驻，右侧三分之一被浅色面板覆盖，部署与修缮按钮混排，核心耐久在顶部栏和独立面板重复。本轮先解决可见性与层级，再谈玩法。
- 新增 `SiegeCommandHudAuthoring`（菜单 `HY-Sandbox/Siege/Apply Command HUD`）：幂等重排为单条指挥栏、左侧舰队坞、底部动作行；停用Main遗留遮罩只做 `SetActive(false)`，场景保存重载后序列化引用全部保留。停用的对象随时可在Inspector恢复。
- `SiegeHud` 改为按阶段切换：整备显示部署组与修缮组，战斗收起两者、压缩舰队坞、显示 `WAVE n · RAIDERS k / total` 进度与新 `Ship Status` 面板；波次进入/清空/失败弹一次横幅，击毁敌舰显示短提示；`F2` 切换默认隐藏的按键说明。刷新仍0.25秒节流，战斗每帧只更新两个进度条锚点与透明度。
- 真实Play部署→开波、9项部署与10项选择Play检查、1920×1080截屏、静态检查与导航生成均已执行；`SiegePlayChecks` 在当前HEAD两次停在不同断点（还原HUD脚本后仍失败），因此本轮不以它作为通过证据，详见 [指挥栏验证](Validation/2026-10-26-siege-command-hud.md)。未重建Player，未做长时性能采样。

## 2026-10-25 奇偶中心规则更正：以驾驶舱实际网格为准

- 前几轮把奇偶映射反了，原自动化结果不能证明该映射正确。现按真实驾驶舱连接点及整数占格边界更正：世界尺寸奇数轴中心为半整数，偶数轴为整数。因此2×1×1绕Z转90°（世界尺寸1×2×1）中心应为`(.5,整数,.5)`。建造容器归零仍保留，以使Inspector与世界坐标一致。
- 全目录探针改为用真实驾驶舱连接点验证放置解的整数边界，独立约束尺寸奇偶期望；25种方块/24朝向/六个旋转方向共78,769条断言通过，原460项回归已同步新规则并通过。Unity编译、319脚本可读性和静态构建通过（4个既有警告）。不自动移动旧存档方块；再次旋转/重新放置时应用新规则。详见 [当前验证](Validation/2026-10-25-build-rotation-catalog.md)。

## 2026-10-25 Inspector坐标与建造网格统一

- 用户截图的差异来自BlocksParent制作偏移`(-2.5,-0.5,10.5)`：局部`(0.5,3,-10.5)`对应存档世界坐标`(-2,2.5,0)`。`BlueprintLoader.ReplaceParent`现在以零位置/零旋转/单位缩放实例化建造容器，逐块按原世界姿态恢复；不迁移用户JSON，不改Prefab与GUID。
- 全目录探针改为使用真实加载器创建的父对象，增加截图方块真实Factory恢复、局部/世界坐标严格相等和存档数据不变检查；72,577条断言通过，覆盖25种方块和全部旋转轴。见 [父对象与Inspector验证](Validation/2026-10-25-build-rotation-catalog.md)。

## 2026-10-25 全方块与全旋转轴覆盖

- 通用尺寸奇偶中心规则已覆盖当前建造栏全部25种方块（含Door、推进器、炮塔、货仓等），24种正交初始姿态和X/Y/Z三轴正反旋转。修复相机前/上轴可能重合及上轴初始评分使用错误方向的问题，确保操作坐标架始终正交；拖拽开始时捕获固定世界轴。
- 新增自动发现Prefab的隔离Play探针：72,570条断言通过，包含真实Prefab、六个键位、真实手柄射线和拖拽、四次回位、Undo/Redo、JSON姿态/位置往返；额外覆盖1–4格的64种尺寸组合。此前460项回归通过，Unity编译通过，实际覆盖及边界见 [全方块验证](Validation/2026-10-25-build-rotation-catalog.md)。人工鼠标体验与独立Player未验证。

## 2026-10-25 建造旋转网格锚点与尺寸奇偶对齐

- 修正此前“世界中心不变”的错误验收规则：锁定中心所在整数格的网格锚点，尺寸奇偶偏移随姿态换轴。当前以驾驶舱网格为准，奇数轴取正半格、偶数轴取整数；2×1×1绕Y轴由`(0,1.5,0.5)`转至`(0.5,1.5,0)`，180°回到原中心。位置由整数半格坐标重建，预览连接点与移动吸附只清理近网格尾差。
- 键盘/拖拽均采用世界轴旋转，姿态规范化，拖拽改为 90°步进，与既有蓝图存档一致；拖拽参考平面固定于开始位置，候选中心和姿态共同参与阻挡、保存与 Undo/Redo。
- Unity 6000.3.11f1 导入/编译通过；隔离空场景 Play Mode 460 项通过，覆盖四种尺寸、虚拟输入、逐坐标严格相等、正负/远处坐标连续100次旋转、阻挡、Undo/Redo、JSON与Awake往返。真实鼠标手感、完整文件仓加载及独立Player未验证，详见 [旋转验证](Validation/2026-10-25-build-rotation.md)。

## 2026-10-24 阵地战自主体验自测

- 真实 Siege Play 复现并修复两项旧闭环未覆盖的问题：首个空泊位受阻时拒绝所有后续空位部署；刷新或往返 Main 工作台丢失双方蓝图选择。
- `SiegeFleet.Deploy` 按顺序寻找空闲且净空的泊位，全部受阻时保留具体阻挡提示，不提前扣费；`SiegeSession` 持有双方选择，刷新时按同目录唯一文件名恢复，HUD 无通知赋值，删除/失效才回退。
- 原版失败证据、修复后部署9项/选择10项、既有闭环28项/工作台22项及验证范围见 [本轮自测](Validation/2026-10-24-siege-usability.md)。未改变 BFS、物理或战斗参数，不宣称万模块性能、完整平衡性或全部缺陷已经解决。

## 2026-10-24 阵地战自动整备（进行中）

- Siege 已保存 Main 的真实 UI、建造工具、存档列表和输入管理器，新增整备工作台：B/按钮进入独立蓝图预览，P返回；战斗P开始下一波、Alt释放鼠标、F1推进器面板。工作台编辑仅影响未来购买蓝图，保留已部署舰、建筑、金币及破损状态；取消加载后恢复原驾驶视图。真实Play检查与验证范围见 [Main工作台验证](Validation/2026-10-24-siege-main-workshop.md)。
- 每波间隔现按报价先自动修缮基地，再按驾驶舰优先顺序自动修复各存活舰；金币不足或泊位受阻时不扣款，HUD 汇报舰队修复数量/花费及首个失败原因。该选项可在 `DefaultSiege` 开关；25项Play回归检查覆盖报价扣款和多次Update幂等。
- 整备HUD现在显示下一波规模与当前攻方蓝图预览（例如 `NEXT 2x RANDOM ENEMY`），帮助玩家在部署/修缮前规划金币；预览只读规则状态，不参与战斗计算。SiegePlayChecks新增预览契约检查，并按当前用户蓝图价格选择可负担舰艇完成经济回归。
- 正常 Siege 已迁移为双方独立的只读蓝图目录选择：守方 `Saves`、攻方 `EnemyBlueprints`；有刷新按钮，攻方可选单一舰型或全目录随机，价格为模块数×Inspector单价。压力场保持固定夹具。Editor目录夹具6项通过；真实Play读到14/14项、HUD选取部署Swordfish、选择DSH_E01后生成对应97模块敌舰，23项既有闭环在固定测试配置副本下通过。场景保存重载与UGUI截屏已验证，尚未做真实鼠标点选或新Player回归。
- 新增默认开启的波次间基地与舰队自动修缮：战斗结束切换整备时各报价一次，沿用大型建筑/舰船蓝图净空检查、原位恢复与成功后扣款；无损不操作，余额不足或净空受阻时保留金币并报告。最新 Siege Play 25 项通过，含精确报价、多舰队报价和重复更新不重复扣款；探针使用明确损伤夹具，不代表自然战斗。

## 2026-10-24 飞行与机器人计算轻量化

- Bot 绕障在单次同步决策内缓存碰撞体过滤结果，复用保持方向及选中方向的净空距离；每次 Evaluate/ConstrainVelocity 都清空过滤缓存，保持货物脱离和目标变化即时生效。导航复用本物理帧目标距离，紧急制动与移动 Home 追踪不降频。
- HoverFlightController 对未激活推进器提前提交零目标，跳过姿态力臂计算；Thruster 的方向归一化移至可视化节流检查之后。保留供电、推力限速和序列化参数。
- 真实 Bot 导航29项、悬浮控制13项通过；静态构建0错误/4既有警告。未做Player/FPS对照，不宣称这些脚本已被证明是主瓶颈或万模块性能已改善。GPU与CPU Jobs/Burst分工见 [架构说明](SiegeArchitecture.md)，验证过程及缺口见 [轻量化验证](Validation/2026-10-24-flight-bot-lightening.md)。

## 2026-10-23 阵地战研究与实现（进行中）

- 阵地战增加敌方无驾驶舱碎片软预算：64组/512模块、15秒、每半秒最多退场8组；保留友方维修、驾驶舱、建筑与跨场景对象。Editor12项和既有玩法21项通过；最终发布构建停滞，未完成该版本Player回归，见 [残骸预算](Validation/2026-10-23-siege-wreck-budget.md)。用户要求当前单元后停止，未将整项目或万模块流畅性标记完成。

- 新增独立可编辑混合压力场：四栋2048模块建筑、六友舰、最多十二敌舰；真实峰10224模块，十分钟完成两波。Editor14项、取消5项、最终Player77项通过。P95约167ms未达流畅目标，近景和后台开发干扰已记录，不能归因BFS，见 [混合压力证据](Validation/2026-10-23-siege-mixed.md)。当前架构与Jobs/Burst/ECS选择见 [架构说明](SiegeArchitecture.md)。

- 显式Player真实键鼠驾驶路线：正常388模块基地两舰87项、2048基地六舰91项通过。实际WASD/QE/Space、HUD指针换舰、唯一驾驶权、旧舰恢复AI、死亡自动接管/镜头与设备恢复已验证；未替代人工体验或长期稳定性。首轮主推进器夹具假设失败已记录，未改蓝图绕过，见 [真实输入链](Validation/2026-10-23-siege-driving.md)。

- 短时能量光束/点光源改为场景有界复用，淡出后回收，场景卸载释放；生命周期16项、玩法21项、Player闭环75项与六舰大型基地自然72项检查通过。16条光束夹具约0.73→0.13ms、16次闪光约0.22→0.04ms，非自然FPS结论。182秒完成2波进入第3波，仍有13条原生D3D11呈现提示，见 [短时表现复用](Validation/2026-10-23-siege-transient.md)。

- 建筑底座接触复用已验证邻接节点，复杂连接层级实时回退。16缓存/43邻接/18损毁/20建筑/21玩法与75项Player闭环通过；2048同步夹具热底座2.638→0.143ms、完整热16.995→13.851ms，冷刷新未改善。182秒自然71项通过，有10条原生呈现提示，不作为FPS收益证明，见 [底座缓存](Validation/2026-10-23-siege-foundation.md)。

- 毁伤逐次日志改为默认关闭，保留Inspector开关及Main已有选择。20项日志/21项玩法/75项Player闭环通过；182秒自然完成2波，72项托管检查通过但有12条原生D3D11提示。隔离64次销毁日志开/关7.74/0.36ms，不代表自然FPS提升，见 [日志成本与回归](Validation/2026-10-23-siege-quiet.md)。

- 独立Development引擎计数能力校准：VFX/渲染/GC有实际样本，物理/Main Thread句柄虽有效仍无样本，停止此归因路线；16项Play和75项最终发布闭环通过。原生日志发现D3D11呈现诊断不进入托管errors数组，已修正旧性能报告边界，见 [能力与日志限制](Validation/2026-10-23-siege-engine.md)。

- 新增显式Player低频刚体分类/有界慢帧和可选嵌套计时：14项Play、75项闭环、182秒72项托管自然检查通过（后补查有5条原生D3D11呈现诊断）。峰3418模块/224刚体，其中204个无驾驶舱分量；最大182.92ms帧的相邻舰船刷新约4.94ms，不能归因BFS。普查中位4.51ms，计入诊断扰动；下一步观察引擎未覆盖阶段，见 [规模与慢帧证据](Validation/2026-10-23-siege-survey.md)。

- 修复明确基地任务目标仍受150m自主索敌范围限制：旧夹具9项失败3项，修复后10项通过；12秒真实实飞距目标315.62→257.52m。可见2048基地六舰Player约182秒无错，峰3504模块/249刚体，中位12.79/P95 27.04ms；75项闭环与21项玩法通过，未宣称解决所有长收尾或提升FPS。见 [任务目标修复](Validation/2026-10-23-siege-priority.md)。
- 新增独立SiegeLargeStress：2048模块建筑蓝图基地+六舰，20项基地/75项Player闭环通过；自然602秒完成2波、第3波中，无错误，峰3424模块/229刚体。前段Hidden截图全黑、后段已确认可见渲染，且第二波长收尾，整段帧分位数不作为可见渲染或持续高压基准。下一步验证明确任务目标仍受150m索敌边界影响，见 [大型基地压力证据](Validation/2026-10-23-siege-large.md)。
- BFS优化版六舰D3D11 Player回归：75项闭环、72项自然检查通过，约182秒完成2波/第3波中，无错误；峰1758模块/369刚体，中位11.05/P95 16.90ms。不同战况不作加速对照。单扫描源归属复用实验收益小且不一致，已撤回，见 [Player与实验记录](Validation/2026-10-23-siege-visited-player.md)。
- 建筑支撑BFS先判访问状态，避免每条回边重复查询Unity归属：2048夹具热遍历中位4.38→1.48ms，完整热刷新21.33→19.37ms；顺序窗口非严格ABBA，不代表自然FPS提升。43缓存/18损毁/20基地/21玩法Play回归通过。见 [遍历对照](Validation/2026-10-23-siege-building-visited-first.md)。
- 支撑新增邻接/底座/遍历/残骸分项计时：单栋上限2048块夹具冷刷新74.60ms、热刷新21.33ms，其中热遍历4.38ms；冷邻接57.59ms。43项缓存、18项损毁、20项基地检查通过。本轮是测量，不是加速；当前单栋2048/世界8192校验上限保持。见 [分项证据](Validation/2026-10-23-siege-building-stages.md)。
- 独立可编辑SiegeStress场景与配置验证六舰+大型建筑基地：真实D3D11 Player约182秒完成首波、第2波中，无错误；峰值普查1758模块/182刚体/6敌机，中位13.73/P95 22.67/P99 27.22ms。75项压力配置闭环通过；不是万块性能或双舰加速对照。见 [六舰证据](Validation/2026-10-23-siege-six.md)。
- 维修被阻挡时显示对象类型、模块/物体名称与坐标，提示清场后重试；不绕过净空或提前扣费。20项基地、21项玩法检查通过。缓存版可见D3D11 Player自然180秒完成2波、第三波中、无错，中位10.01/P95 11.07/P99 12.70ms；战况不同，不作为加速对照，见 [缓存验证](Validation/2026-10-23-siege-building-graph.md)。
- 建筑专用邻接缓存接入支撑遍历：局部删除仅重探附近模块，姿态/连接面/碰撞配置改变重建，复杂碰撞或深层连接变换回退实时几何。43项缓存对照、18项损毁、19项基地、20项闭环与43项WorldLab回归通过。1024块夹具损毁中位40.49→31.37ms、邻接查询5892→288；不是战斗帧率结论。见 [建筑邻接验证](Validation/2026-10-23-siege-building-graph.md)。
- 独立Windows D3D11验证包已构建，70项Player闭环/HUD边界及真实文字几何检查通过；1156×965可见截图已检查。HUD改为Expand并增加两处章节标题高度，修复窄视口裁切及文字不生成。初轮自然Player完成3波、第4波失守，约262秒无错误；中位11.46/P95 16.48/P99 20.34ms，非Editor对照、非万块验收。见 [Player验证](Validation/2026-10-23-siege-player.md)。
- 基地已迁移为**大型建筑蓝图**：32×32m、388模块，复用BuildingRuntime加载、虚拟底座支撑、损毁/残骸预算与金币原位修缮。无驾驶舱，独立发电核心被毁或脱离主体判负；飞船单独部署驾驶。19项建筑检查、20项玩法闭环、43项既有世界建筑回归及四塔供电命中实测通过，场景重载引用保留。见 [建筑迁移验证](Validation/2026-10-23-siege-building.md)。
- 独立可编辑场景 `Assets/Scenes/Siege.unity` 已接入部署、唯一驾驶权、友方AI、无限波次/在场预算、金币维修、核心失败与重开。参数入口 `Assets/Siege/DefaultSiege.asset`，规则和使用方式见 [阵地战](SiegeWarfare.md)。
- 维修恢复耐久及缺失模块，保留部署身份、复用存活断裂件并清空购买蓝图货物；占用泊位拒绝维修且不扣款。基地为单连通388模块建筑，主体保持锚定，驾驶舰损失自动接管其他舰或观察基地。
- 真实Editor规则19项、Main Play舰队31项/旧敌池6项、最新Siege Play闭环20项通过。初版场景首波自然交火完成；最新版已采集自然交火但多波和大规模性能尚待验证。证据与准确边界见 [本次验证](Validation/2026-10-23-siege.md)。
- 技术路线保留可编辑GameObject/复合刚体；建筑范围损毁已按最外层连锁批次合并支撑刷新。1024块同夹具摧毁42块，支撑42→1次、同步伤害中位879.25→40.49ms，主体存活数一致；18项损毁、19项基地、20项闭环及43项世界建筑回归通过。建筑套用舰船运行连接缓存反而更慢，已撤回该实验。见 [批处理证据与边界](Validation/2026-10-23-siege-building-batch.md)。
- 最新90秒自然交火5201观察，首波完成，基地剩368块；中位13.85/P95 30.18/P99 39.39ms。建筑支撑17次累计264.57ms，但建筑范围伤害0次，不能由此确认批处理的自然战斗收益；P95无改善证据。尚未引入ECS/Jobs，继续真实Player与多波验证。
- 历史32块原型：已建立真实Play性能采样器 `SiegePerformanceCapture` 和按需CPU计时 `SimulationWorkMetrics`。最新基地双舰90秒基线：6702次真实帧观察，连接累计19.89ms、包含它的重建90.49ms、完整刷新163.48ms；不可相加。断链链条夹具确认输入顺序改变模块归属，需先修正确性。见 [性能基线](Validation/2026-10-23-siege-performance.md)。
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
- Unity 6000.3.11f1 导入/编译、既有 ArchitecturePlayProbe 和隔离 Play 拓扑对照通过；100/256块连接重建总耗时与基线基本持平，尚无显著提升证据，因此不推送。dotnet build为0错误/4既有警告；全工程可读性检查仅剩原有 MainUIPanels 格式差异，本轮脚本通过。完整证据、执行片段预算和未覆盖范围见 [审查与复测](../OPTIMIZATION_TESTING.md)。

## 2026-10-17 夜间：普通建造敌机刷怪门控收束

- 收束 `EnemySpawner` 的生命周期：普通建造游玩由 `PlayManager.PlayStart()` 设置生成锚点并按 `spawnInterval` 调度，退出时由 `PlayEnd()` 清理计时器与锚点；世界会话保持由据点导演独占敌机生成，避免全局计时器重复刷怪。
- 据点探针改为验证“世界会话不允许全局调度”，不再把组件禁用状态当作生产契约；`OutpostAuthoring` 不再通过编辑器永久关闭场景内 `EnemySpawner`，保留 Main 场景的可编辑生命周期。
- 本轮静态构建 0 错误、3 个既有依赖冲突警告，`git diff --check` 通过；连接 Unity Editor 的 Main 场景进入/退出 Play Mode 20 秒，错误日志为空，场景未保存脏化。完整边界与缺口见[验证记录](Validation/2026-10-17-enemy-spawner-gate.md)。

## 2026-10-16 建筑接地与区块边界美化

- 建筑平台改为逐蓝图实际占地、旋转与中心对齐，半体素支撑余量外6m接回自然坡面；不再把全库最大正方形平台套到小建筑。修复偶数候选网格导致部分建筑模板永不出现的哈希偏差。
- WorldGeneration加载半径4→6、卸载5→8、建筑候选概率10%→20%，初始169列、详细区宽约416m；近身两圈先准备地形碰撞，再恢复建筑，预算保持可编辑。
- 新增无碰撞体素视觉外环，草土侧壁和阶梯轮廓与近景一致，默认12圈再接8m远景与距离雾。按列缓存/分帧创建、与远景空洞原子切换；加载列排除视觉代理、退出释放网格。高位/低位实际相机渲染已检查，近身加载接缝明显减弱；高处仍可辨远方精度切换，并非无限无缝地形。
- 78个落点、6套模板、四方向的逐格支撑/过渡范围检查通过；外环资源与独立Player结果、性能对照、实际画面及体验评价见[本次验证](Validation/2026-10-16-terrain-integration.md)。函数图和源码导航同步。

## 2026-10-16 用户调整：敌机与匹配底座组合

- 据点改为敌机配套停机底座，建筑编辑器新增 `Create matching base for selected enemy`：根据所选敌机模块轮廓创建可编辑结构，完整网格/原材质与底座同时预览，组合JSON包含敌机快照及姿态。已有同名作品保留，新建自动加序号。
- Finch、Pike、Manta三套底座已导出至用户 `BuildingBlueprints` 目录，尺寸28×26、28×30、36×22m，替换项目内三套旧外围驻军模板并保留GUID。原28份用户JSON哈希不变；用户既有普通建筑、敌机源与六份工程/配置/布局差异均保留。
- 已加载据点先显示底座上的停机敌机，近处按同姿态启用真实实体，不再从外围空中部署；世界会话抑制EnemySpawner全局刷怪，普通建造游玩仍按配置间隔刷新。击败只在当前出征有效，重访不复活。远处显示没有伤害碰撞，暂不支持未激活目标的远射。
- Main真实隔离Play 61项通过，六套内容连接、三套净空/完整模型通过，实际组合画面已检查。独立D3D11 Development Player组合路线68类通过、errors为空、正常退出：Manta编辑保存/重载与Finch自然战斗、追击/脱离、受损休眠恢复、击败后重访及会话清理。过程中修复敌机锁定隐藏原载具的问题，索敌现在排除禁用对象。源码256脚本/30图、静态编译0错误/4既有警告通过。操作、运行证据及失败记录见[据点制作](OutpostAuthoring.md)与[本次验证](Validation/2026-10-16-matching-outpost-bases.md)。

## 2026-10-16 夜间：据点多轮稳定性与整批回收

- 普通D3D11 Player完成10次正式据点出征及隔离返航/死亡重生，75类检查通过。另以`-world-smoke-outpost-salvage-all`完成3次整批回收，78类检查通过，每轮8个原始模块均观察到机器人携带并入仓；退出后无活动或隐藏敌机/掉落实体，28份用户JSON哈希保持。
- 整批验收按自然战斗已有掉落增加真实测试货仓和中继，避免8格夹具先被其他残骸占满；生产容量/寻物规则不变。冻结夹具取消延迟刷新，重跑运动学刚体警告为0。Ready对象数13957/13909/13957，分配内存133.70–134.00MB；此受控路线不代替人工驾驶或GPU性能验收。见 [证据与范围](Validation/2026-10-16-outpost-endurance.md)。
- 新增移动目标追击路线：正式驻军追踪2m/s移动玩家，离开检测半径后释放目标，返回后重新索敌；67类Player检查通过。

## 2026-10-16 夜间：独立Development Player据点制作

- 增加独立Development制作验证包入口，不改变普通发布包的制作权限。显式`-world-smoke-outpost-editor`使用隔离缓存文件仓，真实快捷键进入/退出、点击模板复制和姿态保存、重载部署数据并检查构建后的Shader/预览清理。
- D3D11 Development Player 30类检查通过、errors为空、正常退出；包含编辑后恢复原载具及世界跨列/退出再准备。此包不作为发布性能比较。见 [独立Player操作证据](Validation/2026-10-16-outpost-editor-player.md)。

## 2026-10-16 夜间：据点驻军部署预览

- 等大小小方块标记替换为蓝图模块实际尺寸的线框、总体范围、朝向箭头和镜头朝向标签；选中驻军显示模块细节，其他驻军只显示范围。状态区显示尺寸，橙色保守范围重叠警告帮助调整净空，保持可保存。
- 预览只有显示组件，跟随蓝图局部根；切换槽位与退出编辑器释放临时Mesh/Material。Main真实隔离Play的编辑器51项通过，725×488实际画面已检查；不称独立Player面板验证。见 [验证与占用检测边界](Validation/2026-10-16-outpost-preview.md)。

## 2026-10-16 夜间：据点金币与科技收益

- 正式强据点实测发现金币/科技仓沿用敌方载具的内容不掉落开关，击毁后原有收益被丢弃。CargoHold现在识别所属世界建筑并释放实际存量；普通载具继续遵循阵营配置，正常卸载/退出不产生掉落。
- 新增显式`-world-smoke-outpost-salvage`路线，观察实际据点货仓释放、合并金币包、分类吸附及机器人真实拾取/停靠入仓；退出重新准备时同时检查据点进度清空。完整回收Editor71项通过，D3D11 Player同14741种子两次出征并返航/死亡重生75类通过，28用户JSON未变。复现、运输夹具修正与完整Player证据见 [资源收益验证](Validation/2026-10-16-outpost-cargo.md)。

## 2026-10-16 夜间：三驻军强据点自然交战覆盖

- 正式seed14741生成Supply Stronghold Outpost，逐名观察Pike、Manta、Finch的真实索敌、悬浮输出、物理移动和武器伤害归属，避免仅用首名驻军代表整个编队。生命周期夹具冻结时同时关闭悬浮控制器，避免向运动学刚体写角速度的验证噪声；生产玩法不变。
- D3D11独立Player的三驻军交战、受损重组、远处休眠、追击恢复、击败后重访和重新准备共51类通过，用户JSON保持不变。探针保留在同一功能文件内，共用既有据点路线；运行时函数图未变。见 [三驻军证据与范围](Validation/2026-10-16-outpost-stronghold.md)。

## 2026-10-16 夜间：重组保持世界坐标与据点战况排版

- 多种子实测捕获插值刚体重组问题：新组先在Prefab制作坐标创建，再移动Transform，下一渲染帧可能恢复旧插值姿态，将存活驾驶舱移回制作坐标附近。RuntimeGroupBuilder现在直接在目标世界坐标创建新组；据点部署抬升同样写Rigidbody.position，保持物理/显示姿态一致。
- 据点HUD改为读取结构化最近据点快照；显示名称/米数、剩余驻军与炮塔、会话清剿统计。名称按实际字形宽度省略，保留组合字符边界；72像素三行区域、禁用富文本并添加暗色阴影。Main真实世界725×488画面检查了120宽字符名称及清剿状态；本轮隐藏Player截图为黑图，没有作为视觉/GPU性能证据。
- 1080p D3D11 Player seed14722路哨据点40项通过；seed14721双塔据点叠加真实战斗回收、返航/死亡重生66项通过，两轮均errors为空且正常退出，28份用户JSON未变。249脚本/27图、静态编译0错误/4个既有警告、Windows构建0错误/1警告。见 [复现、排版与验收边界](Validation/2026-10-16-outpost-pose-hud.md)。

## 2026-10-16 夜间：据点外追击恢复

- 独立Player复现追击驻军在原据点外休眠后，玩家靠近它却无法恢复：原代码仍只检查原据点激活半径。现在休眠驻军按最后位置160m内恢复，与180m休眠距离形成缓冲；首次部署也检查部署点距玩家，防止远端反复创建/休眠。
- 修复后同seed14721正式Player35类通过；追加真实驻军自然武器命中与攻击归属后36类通过、errors为空、进程正常退出。249脚本与静态编译通过，Windows构建0错误/1警告，用户28份JSON未变。见 [复现和边界](Validation/2026-10-16-outpost-pursuit.md)。

## 2026-10-16 夜间：建筑编辑器敌人据点与会话内有限驻军

- 建筑编辑器新增 Enemy outpost：可从三套既有敌机蓝图选择驻军、添加/移除槽位、保存本地部署位置与朝向，并把模块快照嵌入建筑蓝图；Copy template 会生成独立用户文件。普通建筑与据点继续使用原建筑候选概率10%。
- 世界探索运行由 OutpostEncounterDirector 按距离、地形列和部署包围盒逐台生成，最多16台；EnemySpawner保留普通建造游玩的自动计时刷新，并在WorldSession存在时禁止调度。远离区块时保存活敌机模块、耐久、货物和驾驶舱身份，重访恢复原对象身份；驾驶舱死亡才消耗该槽位。击败记录只存在当前探索会话，返航/死亡后重新出发可重新生成，不写永久击败文件。
- 新增 Field Workshop、Abandoned Storehouse、Relay Ruins 三套普通建筑，以及 Road Watch、Relay Station、Supply Stronghold 三套据点，使用结构尺寸Prefab、既有模块和三套敌机快照。六套模板真实恢复后全模块连接、结构模型尺寸/连接网格、炮塔供电和据点标记均通过；预览见 `Temp/OutpostContent`，详细说明见 [据点制作](OutpostAuthoring.md)。
- BuildingEditorPlayProbe 45项通过；OutpostContentChecks 六套真实Play结构/供电检查通过；D3D11非Development Player seed14721 据点路线通过32项，组合战斗回收/返航/死亡重生路线57项通过。Windows构建0错误/1警告；249源码/27函数图、静态编译0错误/4个既有警告及diff通过，28份用户JSON未变。详见 [验证证据](Validation/2026-10-16-outpost.md)。

## 2026-10-15 用户调整：法向耗散碰撞伤害

- CollisionDamageResolver按法向相对速度及求解器冲量计算有限能量，读取双方PhysicsMaterial及bounceCombine，扣除弹性反弹保留的能量；动态双方按质心能量份额分摊，轻物体受到更大速度变化。接触点和复合碰撞回调不重复使用整机质量；擦碰的切向速度不计入撞击伤害。
- 25份方块CSV/真实Prefab统一为2m/s起伤、0.1耐久/J、倍率1、单次上限100%、冷却0；严重撞击可一次摧毁，独立撞击不再被0.5秒冷却吞掉。参数仍可在CSV/Inspector标定，保留已有字段和GUID。爆炸的零直接伤害、速度上限和碰撞保护保持。
- Main真实Play的25项隔离PhysX检查通过：双接触模块10kg以10m/s撞墙合计48耐久，20kg翻倍，20m/s为4.125倍；1m/s无伤，切向速度不增加撞伤，完全弹性反弹无永久伤害，恢复系数0.5为75%损伤；10/30kg真实对撞的损伤比例3:1，总预算36耐久。详见[验证与标定边界](Validation/2026-10-15-realistic-collision.md)。这是刚体能量近似，不包含材料变形、应力传播或经实测标定的车体结构。

## 2026-10-15 夜间：返航失败可见原因与重试提示

- 返航保存失败不再只写Console：Main新增真实可编辑Return Status状态栏，持续显示保存未完成、当前会话保留，以及恢复缺失/损坏存档或处理文件访问后重试的提示；不抢焦点、不挡射线，成功返航自动清除。保留原SaveReturnCargo(bool)调用契约，通过新增out原因重载向UI传递说明。
- Main真实Play四类故障与提示/非阻挡/清除共16项通过；725×488 Game视图实际检查通用长提示无溢出（文字需求72.67、可用84逻辑高度）。D3D11/720p独立Player返航/死亡/再出发29类通过，包含成功返航清除旧提示；Windows构建0错误/1警告，239源码、26函数图、静态构建0错误/4个既有警告通过，28份用户JSON未变。见 [报告及失败尝试](Validation/2026-10-15-night-return-status.md)。

## 2026-10-15 夜间：三轮组合Player回归

- 同一独立D3D11 Player叠加敌机恢复、输入战斗回收、自然Finch交火和末尾返航/死亡重生路线，固定14721完成3/3轮、58类检查、errors为空且正常退出；28份用户JSON未变。三轮内存采样139509983→139631567→139655055字节，短时趋势不等于无泄漏证明。见 [报告](Validation/2026-10-15-night-player-combined.md)。各夹具分阶段运行，不冒充一次完整动态驾驶巡航；本单元无新生产代码改动。

## 2026-10-15 夜间：独立Player返航写盘、死亡重生及再出发

- 显式 `-world-smoke-return` 在一般会话检查后切换唯一缓存目录文件仓，经生产Loader加载相连的驾驶舱/货仓；实际UI正常返航写盘和重载，第二次出发保留货物，销毁驾驶舱后通过真实重生按钮清货、重载、第三次出发及再次返航，最后只读恢复原载具。原子备份与文件内容实际检查，未覆盖用户存档。
- 非Development Windows构建0错误/2警告；D3D11/720p/seed14721 Player一轮28类检查通过、errors为空，进程正常退出，28份用户JSON哈希不变。237脚本、静态构建0错误/4个既有警告、工具与diff通过。见 [报告](Validation/2026-10-15-night-player-return.md)。货物为夹具恢复内容，载具固定用于持久化检查；此前真实战斗回收与本路线是分别验证，尚未合为一次完整动态巡航，不宣称P1整体完成。

## 2026-10-15 夜间：返航存档丢失时保留会话

- 复现命名存档文件不存在时SaveReturnCargo错误返回true，UI会继续清理载具/世界并丢失本次未写回货物。现在将缺失命名存档视为保存失败，保留会话；路径解析/目录创建也进入同一异常边界。未选存档的原行为、几何保留和原子备份不变。
- Main真实世界游玩中隔离文件仓，调用正式MainUIPanels.PlayEnd进行四类故障（丢失、坏JSON、独占锁、目录被文件占用），会话/载具均保持；正常写盘、原文件备份、全部几何及临时文件检查共10项通过。236脚本、Python工具、静态构建0错误/4个既有警告及diff通过；28份用户JSON未变。见 [证据](Validation/2026-10-15-night-cargo-return-files.md)。本单元为Editor真实Play及同步磁盘验证，尚未跑独立Player返航/重生路线。

## 2026-10-15 夜间：既有敌机自然飞行与单向交火

- 显式 `-world-smoke-enemy-flight CODEX_E01_Finch` 只读取现有蓝图、保持原AI/动力/武器参数；动态敌机在真实Player中自然索敌、供电悬浮、移动并伤害固定高耐久玩家靶。30秒内最大位移22.50m，驾驶舱保持有效；不直接施加伤害、不禁用AI或固定敌机。
- D3D11/720p/seed14721、建筑10%配置下，一轮26类检查通过且errors为空，跨区块与退出重新准备通过。Windows构建0错误/1警告；235脚本可读性、静态构建0错误/4个既有警告、导航与diff通过。首轮夹具未等ControlUnit启动导致空引用，已改为等真实生命周期；见 [报告与限制](Validation/2026-10-15-night-enemy-flight.md)。这不是双向驾驶、玩家返航写盘或长期压力验收，P1仍未整体完成。

## 2026-10-15 用户调整：相机无碰撞、建筑10%

- Main相机第三人称碰撞层设为None（0），新CameraController默认值同样为0；碰撞解析直接返回期望位置，不再做SphereCast或墙前收回。保留原缩放/跟随/镜头参数，Inspector仍可配置碰撞层。源码调用链没有改变，函数图仍适用；旧墙体回收断言已同步为关闭后的行为。
- 世界生成资产与新配置默认建筑候选概率从35%降为10%；这是每个候选位置概率，不保证任意视野恰好10%覆盖。用户已有建筑蓝图不变，新会话生效。
- Unity Main真实Play检查：collisionDisabled=true、wallDoesNotRetractCamera=true、会话BuildingChance=0.1；Main保存后退出Play。静态构建0错误、4个既有警告，源码格式及diff检查通过。此为配置/同步方法检查，不声称已完成镜头人工手感或10%新种子的大规模Player性能验收。NightDesk按本轮原截止重新启动，已核验黑屏与system+display请求；保留Esc接管。

## 2026-10-15 夜间：损坏敌机文件不再中断蓝图库

- 在真实InitializeEnemyBlueprintPool复现：一份截断JSON抛ArgumentException，中断后续敌机初始化。现在逐文件隔离JSON解析与IO/访问异常，警告后继续有效条目；不修改、删除或覆盖原文件，不吞掉其他编程异常。
- Main隔离Play回归6项通过：截断JSON、被独占锁定文件、空/空模块蓝图被跳过，有效同级文件均保留，初始化完成且再次调用不重复加入，文件内容与SaveManager状态保持。见 [证据](Validation/2026-10-15-night-enemy-pool.md)。这是同步真实生产入口验证，本单元未另跑Windows Player或目录权限故障。

## 2026-10-15 夜间：Player 输入、炮弹击毁与机器人入仓

- 新增显式 `-world-smoke-combat` 受控Player路线：Input System W键经启用的ControlUnit、真实电网和MainThruster推进动态刚体，松键清空意图；随后固定平台，以现有实体炮塔自然索敌/发射击毁携货敌舱，经过生产残骸转换、机器人拾取、真实返航停靠和供电货仓入库。没有直接调用伤害、手动供电或直接Store伪造结果。
- D3D11/720p/seed14721独立非Development Player完成一轮、28类检查且errors为空，随后跨区块、退出及再次准备通过；233份源码检查与静态编译通过（0错误、4个既有警告）。见 [证据及限制](Validation/2026-10-15-night-player-combat.md)。此为验证能力与证据补全，未改变正常游戏算法；敌方AI关闭、稳定平台不等于完整双向驾驶交战，尚未覆盖Player返航文件保存/死亡/再次出发。

## 2026-10-15 夜间：敌方蓝图完整恢复

- 修复敌机恢复尺寸后仍沿用 Prefab 碰撞体与质量：CSV Awake 后刷新实例几何、连接点和自动质量，再汇总整机质量。生成前校验所有模块资源及唯一驾驶舱，缺失/空模块、无驾驶舱或多驾驶舱均拒绝整机，避免发布残缺敌机。
- 相同独立D3D11 Player路线修复前碰撞体断言失败，修复后固定14721两轮会话、24类检查通过，errors为空；Windows非Development构建、232份脚本检查和静态编译通过（0错误、4个既有警告）。显式 `-world-smoke-enemies` 不影响正常启动、不写用户蓝图。见 [验证与边界](Validation/2026-10-15-night-enemy-restoration.md)。本次为跨帧生成恢复验证，尚未覆盖真实驾驶交战和损坏JSON文件解析。

## 2026-10-15 夜间：携货建筑卸载安全释放

- 真实起飞携货后复现了流式卸载错误：先停用建筑父层级，LootDrop.OnDisable 再试图将携货物移出 Cargo socket，触发 Unity 禁止在父对象启停过程中重挂接。建筑现在在单栋卸载、批量清理、世界根停用/失败前先取消运输与释放认领；仓内货物不洒落、不抽取奖励，快照仅保留已入仓物品。
- 完整回收 Play 回归 71 项通过，增加实际起飞/携货、断裂、单栋卸载、根层级停用及恢复不重复入仓；世界边界 23 项通过，无 Unity Error/Exception；D3D11 Windows 重建成功，固定14721两次一般会话烟测通过（不含Player携货/交战）。见 [验证与边界](Validation/2026-10-15-night-carried-unload.md)。这不等于完整 Player 人工驾驶战斗验收。

## 2026-10-15 夜间：回收全量探针适配场景模拟门槛

- 隔离 Main 后，旧探针禁用了全部 ControlUnit，导致 LootDrop.Update 的真实场景模拟门槛关闭。探针现在使用启用的中立运行单位提供模拟环境，避免读取玩家输入或启动敌机 AI；同时检查启停单位会切换场景模拟状态，未修改生产回收算法。
- Unity 6000.3.11f1 / Main / NightDesk 后台，完整真实 Play 62 项通过、errors 为空：包含本轮建筑 11 项、货币溢出转仓、机器人认领/运输/毁仓释放、隔离临时存档的返航/死亡持久化、残骸清理和移动母体返航。见 [完整报告](Validation/2026-10-15-night-salvage-full.json)。这解决前一专项报告记录的旧探针散射失败；仍非真实输入驾驶/完整 Player 巡航验收。

## 2026-10-15 夜间：建筑断裂接入回收

- 修复断裂模块被禁用/限时删除、绕过回收而吞掉货仓的缺口。WorldBuildingManager.CreateDebris 在重挂接和残骸预算判定前调用现有 WreckSalvage，保留原阵营与现有掉落概率；主体快照不再包含断裂模块，重复转换不重复奖励。普通流式卸载/恢复与退出不触发掉落。
- 建筑专项真实 Play 11 项通过：实际 Durability 伤害触发断裂、正常/零残骸预算、货物释放、敌方掉落、幂等、玩家无刷币、快照重载及会话清理；修复前在相同断裂货仓断言失败。见 [验证与限制](Validation/2026-10-15-night-building-salvage.md)。这不是手动驾驶/完整 Player 战斗返航验收。

## 夜间自主开发规划

每日 08:00（Asia/Kuala_Lumpur，UTC+8）收尾的路线、阶段验收和首晚工作顺序见 [夜间开发大纲](NightDevelopmentPlan.md)。本轮已启动 NightDesk 与当前聊天续跑，截止 2026-10-16 08:00，07:30 收尾；用户明确授权主要功能验证完成后推送 main。后续阶段内容仍为待开发，不代表已实现。

### 2026-10-15 夜间基线：悬浮物理时序回归

完整 ArchitecturePlayProbe 的旧断言只调用控制器 FixedUpdate 就要求实际推力立即改变，与现行推进器独立限速契约不符。HoverControlPlayChecks 现分别驱动控制器和推进器回调，验证无驾驶舱时不转向、推进器失效归属清零、供电后目标提交、缺电/控制器禁用后的单步衰减与最终归零、退出游玩及推进器禁用清零、归属转移保护；生产飞行代码未改。完整探针本轮 completed/passed，73 条检查，0 错误；报告见 [夜间悬浮回归](Validation/2026-10-15-night-hover.json)。悬浮部分为 Play 内显式驱动生产回调，不代表真实驾驶或自动物理顺序验收。dotnet build 0错误、4个既有警告。

### 2026-10-15 夜间基线：后台性能采样

WorldSessionPerformanceChecks 修复遮罩持焦点后第2帧停滞和无新帧时超时无效的问题：临时后台真实运行，完成/中止/超时均恢复原后台及Profiler设置，启动校验场景与报告名。完整路线在NightDesk下推进1830真实帧，主动退出和停帧超时路径均已验证恢复；Main不脏，用户数据保持。固定种子14721、28栋/1967模块的D3D12 Editor移动P95 27.69ms、最大81.31ms，退出单帧55.64ms；这是工具基线，不是生产优化或D3D11 Player性能结论。见 [报告与限制](Validation/2026-10-15-night-background-performance.md)。

## 2026-10-15 Windows启动修复与区块深度优化

2026-10-15夜间补充：显式WorldPlayerSmokeChecks新增固定种子、1–10轮会话和P99/尖峰/每轮内存记录；每轮等待世界与原载具恢复再重进，不写玩家数据。D3D11独立非Development包seed14721连续3轮通过、进程退出码0；28栋/1967模块，移动P95约9.46–9.49ms，首次峰值97.87ms、退出约60ms仍存在；无生产算法优化或长期无泄漏结论。cycles0按预期失败退出；28份用户JSON哈希未变。配置、复现和验证边界见 [重复Player报告](Validation/2026-10-15-night-player-repeat.md)。

- 两份旧Player日志记录D3D12原生资源处理崩溃；同一旧包D3D11对照可响应。Windows发布配置显式使用D3D11，新Release包已实际启动、加载外部建筑蓝图库、进入游玩、跨区块移动200m、退出清空并再次准备，未出现C#错误或原生崩溃。
- 建筑候选概率45%→35%；保留全部有效用户建筑蓝图、自然出生与600m视野。启动用现有可编辑面板显示载具/世界组合进度，载具恢复至95%后完成连接重建才到100%，两者完成开放建造；游玩进入继续使用原有遮罩。
- 区块一格halo快照只在主线程读取；后台任务做同材质贪心面合并，主线程上传16位/必要时32位索引和烘焙碰撞。提交检查请求/数据版本，取消丢弃结果。实时破坏重建加入毫秒预算；建筑休眠按毫秒分批，地形逐列卸载；静止满载减少重复扫描。WorldLab/Bake和显式同步FlushDirty保留。
- 相同种子14721、建筑概率0.45的162块网格对照：98,686→16,162三角形（-83.6%），197,372→32,324顶点；同步单次微基准135.10→83.74ms，包含采样/Mesh上传，不含PhysX，不能视作整局FPS提升。20,267项网格断言、Main及23项边界检查、WorldLab数据12项/破坏碰撞16项通过。实际Player和剩余峰值以[本轮报告](Validation/2026-10-15-world-player-performance.md)为准。
- 新包：`Builds/WorldPerformanceValidation/HY-Sandbox.exe`，与发布相同的非Development Windows配置；不覆盖旧Build或递增版本。图形驱动/其他显卡、长时间战斗、最大载具与4K性能仍未验收。源码函数图与研究来源见报告及[世界说明](WorldStreaming.md)。

## 2026-10-15 Main 大世界卡顿与加载遮罩

- 定位并移除数千个Durability独立OnGUI的隐含GUI开销，改为DebugManager装配单个标签绘制器；炮塔空目标遵守搜索间隔。固定种子33栋/2271模块，D3D11 Editor Profiler静止帧中位约30ms降至8ms，末尾100帧主线程均值30.57ms降至8.54ms；测量条件及局限见[性能报告](Validation/2026-10-15-world-performance.md)，不等同于正式Player性能。
- 建筑资源异步预热，创建/启用按WorldGeneration.asset的默认2ms预算分批；进入先显示不透明进度遮罩并暂停物理，完成/退出/死亡/失败恢复时间。途中缺地形按预测路径真实完成比例显示进度。远景复用网格/高度缓存，同父刚性装饰合并共享网格，保留炮塔及机械运动；大载具末尾连接重建也按预算跨帧。
- 已验证Main 53项、边界23项、WorldLab建筑43项/数据12项及炮塔回归；实际查看遮罩与建筑近景；Unity导入编译、228脚本格式/函数图导航、dotnet build（0错误/4个既有警告）、diff检查通过。正式Player、高分辨率、长时战斗和D3D12稳定性未验收。

## 2026-10-15 Main 大世界与建筑会话

- 本次修正：Main生成池改为读取persistentDataPath/BuildingBlueprints全部有效JSON（当前base1/base2/default_building），平台自动适配尺寸；出生区保留自然地形，按载具范围内最高地面抬升6m；近处加载4列半径/卸载5列半径，加入无碰撞低密度远景和默认270–600m距离雾，退出恢复建造环境。参数保存在WorldGeneration.asset，远景网格按列版本分帧更新。D3D11实际检查远景连续且渐隐；Main检查49项、边界23项回归通过；225份脚本格式/导航、dotnet build（0错误/4个既有警告）、diff检查通过。见[本次报告](Validation/2026-10-15-world-session-followup.json)与[视距画面](Validation/2026-10-15-world-session-distance.png)。Player/长时性能与D3D12稳定性仍未验收。

- Main建造时隐藏分帧预热地形和真实Buildings，Play等待Ready后显示世界、启用模拟并安全放置玩家；等待面板可取消。默认约4.1km有限大世界，81列预热，附近按列流式加载/卸载，不分配全图体素数组；生成Unity对象仍在主线程协程执行。
- 同次探索内地形差量、建筑损坏、剩余模块与货仓随区块休眠恢复；活动建筑预算满时延期实例化。玩家缺地形时暂存速度冻结、前方就绪再恢复。退出与死亡丢弃全部世界状态并重新准备，正常返回保留原有货仓转存流程。
- 真实可编辑入口为 `Assets/World/WorldGeneration.asset`、`WorldSession.prefab`、Main加载面板及现有WorldLab建筑目录；菜单可重建缺少资产而保留已有参数和布局。使用与限制见 [WorldStreaming](WorldStreaming.md)。WorldLab仍保留独立有限地图与快照流程。
- 已验证：Unity导入/编译；Main真实Play专项42项、边界23项通过，包括正负区块、退出/死亡清空、取消、损坏/货仓休眠恢复、81列预算和4000m/s真实物理步保护。WorldLab数据12项、建筑43项回归通过；223份脚本格式/函数图检查、dotnet build（0错误、4个既有警告）、diff检查通过。已观察地形和建筑总览；细节截图期间Editor发生D3D12 GPU等待超时并退出，临时以D3D11重新打开后完成加载面板与建筑近景检查，截图见Docs/Validation。未修改项目图形API设置；D3D12稳定性、正式Player构建及长时大地图性能未验收。

## 2026-10-15 WorldLab 建筑融合

- WorldLab新增真实建筑目录、Outpost模板和初始据点；读取BuildingBlueprints，B进入放置、Q/T旋转、左键确认、Esc取消。预览只复制Mesh且屏蔽采掘。按用户要求，WorldLab实例和预览不生成地基模型、碰撞体或底座方块，保留编辑器底座及尺寸元数据。建筑保留1m模块，地形保留2m体素；支撑、穿插和重叠校验失败拒绝放置。
- ControlUnit共用运行身份与战斗注册，建筑无需驾驶舱，也不加入Main载具清理。供电、激光与实体炮弹、货仓及机器人运行门槛已适配；方块死亡从虚拟地面连接点检查Connector连通，断裂组生成有界残骸；虚拟范围低于默认50%地形支撑时整栋脱锚。
- F5/F9保存/校验体素差量与建筑完整快照，包含尺寸、货仓、耐久和剩余模块；缺失资源或坏数据保留当前世界，兼容旧纯体素差量。动态主体恢复姿态但速度归零，残骸和飞行运输任务不持久化。使用、Inspector参数和边界见 [WorldBuildings](WorldBuildings.md)，源码图见 [RuntimeFlow](RuntimeFlow.md)。
- 已验证：Unity6000.3.11f1导入/编译；融合专项43项通过，覆盖生产Input System输入、三实例旋转、尺寸与耐久/货仓JSON恢复、异常保护、供电战斗、独立场景炮弹跨帧飞行、无实体地基、断裂、地形挖空真实帧下落、旧存档与重置。建筑编辑35项、建造目录102项、炮塔28项、供电/货仓/机器人工作请求32项、机器人真实物理导航11项回归通过；结果见 [专项报告](Validation/2026-10-15-world-buildings.json) 与 [回归报告](Validation/2026-10-15-world-buildings-regressions.json)。213份自有脚本格式/源码导航检查通过；dotnet build 0错误、4个既有警告。实际画面已检查。正式Player构建、大规模性能、机器人跨帧任务及玩家载具进入WorldLab尚未验收。

## 2026-10-15 开发者建筑蓝图编辑

- 开发者模式（Unity Editor / Development Build）下，`Ctrl+Alt+E` 进入/退出敌人编辑；`Ctrl+Alt+F` 进入/退出建筑编辑，支持左右 Ctrl/Alt。两种蓝图模式可直接互切；相同快捷键再次按下返回玩家存档。普通 F 聚焦不会同时触发。
- 建筑蓝图保存到 `Application.persistentDataPath/BuildingBlueprints`，列表、新建、复制、重命名、删除与文件大小均使用独立目录。底座尺寸以 `foundation.width/length` 跟随 JSON 保存；旧存档缺少该字段仍可读取。建筑不生成/允许放置驾驶舱，编辑时隐藏玩家 Play 入口。
- 底座源为 `Assets/Resources/BuildingEditor/Foundation.prefab`，默认 4×4、每格 1m，薄平面顶部为世界 y=0，每格中心一个向上 Connector。固定网格左下角为 (-2, 0, -2)，右下角尺寸面板支持 1–64 的整数长宽，点击 Apply size 自动保存。尺寸变化保留所有现有方块及坐标；缩小可使边缘方块失去底座支撑，扩大后重新检测连接，不删除方块。尺寸调整会清空当前撤销历史。
- 底座不加入普通块存档或撤销栈，禁止选择、删除、移动和旋转；现有放置预览、连接提示及连接检测可直接使用。薄底座碰撞采用与普通块相同的面内缩间隙；实例恢复使用世界坐标，不继承父 Prefab 的已有偏移。加载替换时先停用旧父对象，再延迟销毁，避免同步恢复误连旧碰撞体。
- 尺寸面板源为 `Assets/Resources/BuildingEditor/EditorPanel.prefab`，与底座、网格材质/贴图均为真实可编辑资产。`Tools > Building Editor > Create missing editable assets` 只生成缺少的资产，保留手动调整；`Run isolated Play Mode validation` 使用随机 Temp 目录回归，不改用户蓝图。函数图见 [开发者建筑蓝图编辑](RuntimeFlow.md)。
- 验证：Unity 6000.3.11f1 导入/编译通过；建筑专项 35 项及既有 BuildPalette 102 项 Play Mode 检查通过，实际画面已检查。203 份自有脚本格式/函数图、dotnet build（0错误、4个既有警告）、diff 检查通过。完整架构探针完成 39 项加载检查后停在既有悬浮控制时序断言：该探针仍要求控制器回调立即清零实际推力，而当前 HoverThruster.FixedUpdate 才执行输出更新；本任务未改动飞行生产代码，不将此完整探针列为通过。证据见 [建筑验证](Validation/2026-10-15-building-editor.json)。未覆盖正式 Player 构建、64×64 大底座性能和建筑运行时生成/战斗。 发布隔离工作区基于远程 main，184 份脚本检查通过；临时 csproj 引用本机现有 Library/被忽略第三方源码后静态编译同样 0 错误、4 警告，不提交这些依赖或临时配置。

2026-10-15 WorldLab：新增独立可编辑程序化体素世界实验场（128m范围、固定种子地形与遗迹、三材质硬度），支持采掘/范围爆破/填补及局部分块渲染与碰撞重建，现有炮塔命中入口已接入。F5/F9版本化差量保存/恢复，原子替换并保留上一份，校验失败不改活动世界；不复用玩家蓝图/货仓存档。生成网格通过Editor Bake为真实资产，不改Main。12项数据、16项Play、18项保存/损坏保护与28项炮塔回归通过；软件黑屏覆盖下真实落体180个物理帧后接触体素地面。首次后台更新关闭时冻结的探针失败已保留说明，实验场现临时启用后台运行并限60FPS。新增有限金属支撑/脱落实验：六邻接支撑，默认32格以内孤岛生成最多12个限时刚体碎片；8项数据、7项Play和101真实物理帧下落/再生清理检查通过。最终完整15分钟后台压力测试通过（53,955帧、1,794次爆破、14次再生，种子恢复成功），证据见Docs/Validation；大型结构与地形保持静态；仍为研究原型，无无限流式；详见 [WorldLab使用与边界](WorldLab.md)。

最近整理：2026-10-11；Unity 6000.3.11f1；URP；Input System；运行时类型保持全局命名空间。

2026-10-14 第三人称镜头环境碰撞：`CameraController` 在第三人称目标点到期望相机位置之间使用可复用的 `SphereCastNonAlloc`，忽略玩家自身碰撞体，在墙体/方块前收回相机并保留碰撞间隙；平滑恢复阶段再次校正，避免相机穿过障碍。碰撞层、半径和间隙均可在 Inspector 调整。墙体收回、自身过滤和障碍移除后恢复已通过隔离Play检查；未完成玩家手动视觉/手感验收。

2026-10-14 供电网络性能优化：`PowerNetwork.Rebuild` 保留每帧功率清零、分配和状态提交，但缓存由中继/发电机位置及连接距离决定的拓扑；拓扑未变化时跳过连接清空、发电机连接和中继两两检测。拓扑变化仍自动重建，供电行为契约不变。新增空间索引候选筛选和连通分量缓存；300轮新旧等价对照、32项玩法回归通过。100中继/500负载静态更新10.73→0.56ms，持续移动11.02→0.99ms。新增炮弹池、可增长查询缓冲及正常图标跳过投影；28项炮塔检查、10项真实飞行、4项复用及5项表现检查通过。完整条件、原始样本和限制见 Docs/Validation/2026-10-14-performance.md。

2026-10-23 CSV 方块数值精简：直接编辑 `Assets/Resources/BlockData/Blocks.csv`（当前 25 行、29 列：25 参数列及 4 个只读身份列，2026-10-25 移除 Bot 速度列）。Editor 导入后自动校验、备份并同步 Prefab；Block/绑定组件 OnValidate 延迟请求校验，绑定 Awake 在其他组件之前应用 Resources 中的 CSV。mass 留空使用 x × y × z，填写正数使用手动质量；不参与质量计算的 density 已从 CSV 移除，组件字段保留兼容。requiresPower 合并应用货仓、收集机器人和维修机器人；BotContainer 的最低/额定功率相等，断电只阻止起飞，已离仓 Bot 继续任务或返航；仅保留基础数值及核心性能；自定义说明、开关、碰撞损伤细节、表现及任务参数由 Prefab Inspector 管理；Bot 导航/停靠已固化为 BotNavigationPolicy，原组件值保留。Bot/Connector 辅助资源不纳入表。CSV/蓝图运行时刷新保留连接状态与实例引用，Bake 拒绝缺失供电依赖。使用与生命周期详见 [CSV说明](BlockCsv.md)，函数图见 [RuntimeFlow](RuntimeFlow.md)。 2026-10-23 精简验证：30 列/25 行及保留单元格逐项比对通过；Unity Edit Mode 16 项、Play Mode 4 项检查通过，最终 Bake 无 Prefab 差异；静态构建通过（4 个警告），git diff --check 通过。全仓可读性检查仍被未修改的 MainUIPanels.cs 既有格式问题阻断。[验证记录](Validation/2026-10-23-block-csv-slim.json)。

2026-10-11 推进器字体状态修正：ThrusterInfoPanel 读取 Power.CommitPowerStatus 确认的 SupplyEfficiency，不再读取物理帧中暂时清零的即时供电值。撤销工作请求立即恢复正常显示，重新申请后等待电网确认缺电；残余推力及关机衰减不参与字体颜色判断。实际施力仍使用即时供电效率。验证见 `Docs/Validation/2026-10-11-thruster-power-colors.json`。

2026-10-11 推进器关机：主/通用/悬浮推进器撤销功率请求后，目标输出为零，实际推力按 `maxThrustChangeRate × fixedDeltaTime` 衰减并继续施加尾段力；喷焰跟随实际推力。悬浮控制器只提交目标，限速统一在 HoverThruster.FixedUpdate 执行，避免直接清零或重复限速。退出游玩、组件禁用或悬浮归属失效仍清零。ThrusterInfoPanel 仅对请求工作的推进器判定缺电，空闲文字为白色。验证记录见 `Docs/Validation/2026-10-11-thruster-shutdown.json`。

2026-10-11 炮塔目录与敌机：单/双管按钮名称移除 `(Clone)`，目录 Prefab 和 Main 已保存条目采用原 TurretButton 的暗色描边、按钮状态颜色与缩放反馈；Awake 按 BlockName 复用场景条目并绑定一次点击，避免重复生成/监听。CODEX 三艘敌机前方保持双激光、后方改为单管与双管实体炮塔；仓库及 EnemyBlueprints 同步迁移，安装文件已有哈希备份。验证见 `Docs/Validation/2026-10-11-turret-palette-enemies.json`；历史飞行数据不代表本次布局实飞结果。

先按功能找入口，再沿函数图进入规则实现。全部自有运行时脚本位于 `Assets/Scripts/Features`，Editor 制作/验证工具按功能分目录。第三方素材和 Package 源码不属于本轮重构。

## 代码目录位置

源码按递归功能树实际组织：每层保留主要入口、协调器与核心契约，独立职责进入子功能目录，子功能内部继续细分。分类、主入口和边界见 [CodeLayout](CodeLayout.md)，全部实际文件树见 [CodeIndex](CodeIndex.md#代码文件夹位置)。函数图与源码链接由原导航命令刷新。

2026-11-02 更正：上一轮仅添加文档树，没有完成源码分类。本轮通过 Unity AssetDatabase 实际迁移275份运行时及Editor脚本，保留GUID、类型、源码字节和序列化契约；仅 SpaceKitCurator 的脚本依赖白名单跟随新路径。迁移与验证结果见 [目录分类验证](Validation/2026-11-02-code-layout.md)。

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
| 供电 | `Power/Devices/PowerTransmissionDevice.Update` | `PowerNetwork.Rebuild`；`PowerConnectionVisuals` 连接线 |
| 飞行 | `Flight/HoverFlightController.FixedUpdate` | `HoverControlSolver` PID与姿态计算；`ThrusterAllocator` 分配求解；各推进器施力 |
| 维修/回收无人机 | `Bots/Repair/RepairBot.FixedUpdate` / `CollectionBot.FixedUpdate` | `BotFlightMotion` / `BotObstacleAvoidance` / `RepairTargetSearch` |
| 战斗/损伤 | `Combat/Turrets/Turret.FixedUpdate` / `Durability.ApplyDamage` | `LaserTurret` / `ProjectileTurret` / `TurretProjectile` / `TurretHitResolver` / `CollisionDamageResolver` |
| 销毁/爆炸 | `Combat/DestroyManager.DestroyGameObject` | `ExplosionPropagation` / `SalvageCleanupSession` |
| 回收与持久化 | `Salvage/Drops/LootDrop.Deliver` / `CargoPersistence.SaveReturnCargo` | `CargoHold` 数据与约束；表现位于 Presentation |
| 镜头/特效/UI | `Presentation/Camera/CameraController` / `VisualEffectsManager` / `MainUIPanels` | `CameraFraming` / `CameraShake` / `TransientEffects` / `PanelTransitions` |
| 玩家/输入 | `Player/InputManager.Update` / `PlayerController` | 原 Input System及模式门限 |
| 诊断 | `Diagnostics/DebugManager` / `AbnormalImpulseRecorder` | 场景调试、物理遥测与记录 |

上述路径均相对于 `Assets/Scripts/Features`。完整脚本/真实函数签名见 [CodeIndex](CodeIndex.md)，可点击源码行号；主要运行链见 [RuntimeFlow](RuntimeFlow.md)。Editor制作工具入口同样在 CodeIndex 中；外围开发脚本见 [ToolIndex](ToolIndex.md)。

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

Unity生成的 csproj 必须包含迁移后的路径和全部新脚本；IDE静态构建不能代替 Unity 导入或 Play Mode。工具说明见 [README](../Tools/CodeReadability/README.md)。

## 验证与缺口

本轮已完成下列验证（2026-10-11），原始证据汇总见 [验证记录](Validation/2026-10-11.json)。探针是限定范围的真实执行，不代表完整游戏验收。

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

原嵌套HY-Sandbox目录缺Packages/ProjectSettings，属于不完整旧快照；已迁入Archive/LegacyPrototype/HY-Sandbox。811文件原始字节逐项哈希一致，旧Library仅本地保存且不重新提交缓存；源码/资产和迁移清单入库。详见 [Archive说明](../Archive/README.md)。

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

完整历史需求、旧验证结果、美术集成记录与旧函数索引保存于 [DevelopmentHistory](DevelopmentHistory.md)，仅作为历史，不代表当前路径和实现。Git历史中的场景资源记录仍可追溯。

## 2026-10-17 多 Bot 防阻塞隔离

- `BotObstacleAvoidance` 的 SphereCast、OverlapSphere、ComputePenetration 和速度走廊查询统一忽略带有 `Bot` 父组件的动态机器人碰撞体，不再依赖子物体当前 Layer；目标、Home 和建造/地形障碍继续按原规则参与导航。这样多台 RepairBot/CollectionBot 交叉作业时不会互相触发脱困方向、减速或锁死。
- `BotNavigationPlayProbe` 新增 RepairBot/CollectionBot 两类 Bot 的 Default/Block 子碰撞体、起点重叠、墙后检测及脱离 Bot 层级后恢复为障碍的检查；Play Mode 共 29 条检查通过（含原有 11 条导航检查），无错误。证据见 [导航回归报告](Validation/2026-10-17-bot-filtering.json)。
- `dotnet build HY-Sandbox.sln --no-restore` 0 错误、4 个既有警告，Unity 6000.3.11f1 重新编译通过。全仓可读性检查仅报既有未提交的 `MainUIPanels.cs` 格式差异，本次未改该文件。Main 场景测试后仍为干净状态；Prefab 参数与 GUID 保留。
- 验证边界：Bot 过滤使用真实 PhysX 查询和禁用任务脚本的 Bot 夹具，原有 5 个飞行场景驱动生产导航/刚体；未重建视频完整探索会话或运行大量 Bot 压力测试，不代表物理碰撞、供电和全部任务交互的端到端验收。

## 2026-10-11 掉落与机器人阻塞修复

- 掉落文字在 LateUpdate 使用与 StatusIcon 相同的 CombatHud.IsWorldPointOccluded，检查屏幕边界、镜头前方及实体障碍。携带时忽略所属机器人层级，外部障碍仍遮挡。
- 敌方方块原必出金币改为 30% 金币包；白名单稀有部件概率从 20% 降为 8%。先抽部件再抽金币，已有货仓内容仍按原数量释放，不受随机爆率影响。参数在 Resources/Salvage/Settings。
- 掉落初始短时散落，球形路径检测阻止穿墙；显示节点悬浮和旋转，逻辑根保持稳定。认领后停止散落、搬运时还原姿态，保留吸附、认领和入库契约。参数已通过 Editor 保存到 LootDrop Prefab。
- Bot 从自身位置检查三维净空，起点重叠时通过 OverlapSphere/ComputePenetration 计算分离方向；目标阻挡时维持绕障方向，允许临时远离目标以脱困；目标连续可达后恢复直达。移除已替代的 Home 出发射线算法，原 Inspector 字段保留序列化身份。
- 静态检查：158份自有脚本格式/职责/函数图符号通过；dotnet build 0错误、4个既有警告（3个MSB3277及Profiler过时API）；git diff --check通过。Unity 6000.3.11f1导入和重编译通过。
- BotNavigationPlayProbe：5个真实PhysX场景、11条检查通过，覆盖Repair配置绕墙、Collection配置U形围挡（竖直逃逸）、惯性制动、移动Home墙后及起始重叠脱困。探针驱动生产BotFlightMotion，未替代任务流程验收。
- SalvagePlayProbe 最终49条检查通过，覆盖三类掉落文字遮挡/恢复/镜头后隐藏、散落旋转、携带姿态、掉率边界与重复转换、真实维修/运输/移动Home停靠和货物持久化。3个改动Prefab无Missing Script，Main场景不脏且已退出Play，Unity Console 0错误/0警告。完整证据见 [本次验证记录](Validation/2026-10-11-loot-bots.json)；探针只覆盖列明场景，未验证任意复杂封闭迷宫、所有移动/旋转Home组合或大量Bot性能。

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
- 新建造条目与图标保存为Resources/BuildPaletteEntries UI Prefab，启动时只补缺失条目，遵守COMBAT分类与BuildType高亮。原场景手工条目优先；条目提示从真实方块配置刷新。资产制作菜单不会保存Main场景。制作源与调参说明见 [炮塔规格](../ArtSource/Turrets.md)。
- Unity 6000.3.11f1导入/重编译和6个Prefab重载检查通过，无Missing Script/断引用。32条供电/推进/Bot/货仓检查、20条炮塔命中检查、8条真实帧自动双管飞行检查、3条目录点击/高亮/去重检查及6条悬浮控制检查通过。静态模型和真实双弹飞行截图已检查。证据见 [本轮记录](Validation/2026-10-11-active-power-turrets.json)。
- 166份自有脚本职责/格式/函数图检查通过；dotnet build 0错误、4个既有警告；git diff --check通过。Main原有未保存修改保留。未验证长期密集交火、大量实体弹性能、所有旋转载具组合或正式Player构建；同步扫掠检查与真实帧自动发射检查分别记录。

### 炮管后座与二维图标

ProjectileTurret发射后仅炮管模型沿炮口反向后退0.12m，0.04s快速后退、0.22s平滑恢复；参数和炮管引用在Inspector可编辑。LateUpdate独立于索敌与供电门控，停火继续复位，禁用立即恢复初始位置。单管/双管Prefab已通过Editor只补充引用，原其他参数保留，资产制作源同步。

两个新炮塔的透明二维图重新渲染，并修正Single Sprite导入及真实建造条目Preview引用；Play实例确认两个图标都非空。菜单Refresh turret thumbnails只更新图标，不重新搭建炮塔。28条炮塔/后座检查、10条真实帧飞行/同步后座/停机复位检查通过；Unity编译、166脚本格式/函数图、dotnet build和diff检查通过（4个既有构建警告）。Main及炮弹材质的期间外部改动保留并排除本次提交。见 [验证记录](Validation/2026-10-11-turret-recoil-icons.json)。

## 2026-10-25 Bot 寻路调优

- 入口：`BotFlightMotion.ApplyMovement` / `BotObstacleAvoidance.Evaluate`；维修与回收共用。直达任务按剩余距离制动（2026-10-25 已改用固化的 `BotNavigationPolicy.Acceleration`），避免高速越过目标再绕回；绕障阶段保留脱离速度。
- 连续两次采样确认直达走廊净空后恢复追踪，无需等满绕障保持时间；退出起点重叠后立即重新选路。停滞检测按当前进展窗口重置，绕远后不再必须突破整段任务历史最近距离才能确认进展。移动 Home 的实时点位/点速度、逐物理帧速度走廊制动及 Bot 过滤保留。
- 导航探针增加耗时、路线长度及空旷接近回归。Unity Play Mode 6 个真实 PhysX 场景、32 项检查通过，均到达且无墙体碰撞。相同条件单次前后对照：绕墙路线 56.09→37.71m，U 形围挡 50.80→33.42m，惯性制动 58.54→29.33m，移动 Home 25.11→23.95m，起点重叠 80.36→35.23m；不作为统计基准或任意迷宫保证。
- 真实任务 Play 回归 71 项通过，覆盖回收运输/货仓持久化、维修和移动 Home 插槽返航。静态构建通过（0 错误、4 个警告）。函数图及源码导航已同步；验证证据见 [调优对照](Validation/2026-10-25-bot-navigation-tuning.json)。大量 Bot 压力、任意封闭迷宫和全部 Home 运动组合未覆盖。


## 2026-10-25 Bot 无固定速度上限与参数固化

- BotNavigationPolicy 为唯一飞行/避障/停靠策略入口：加速度48m/s²、方向响应5rad/s、16方位三层净空、0.1s基础采样且随速度缩短到物理帧。任务与返航共用 BotFlightMotion.ApplyMovement，按剩余距离制动；转向前优先消除危险纵向速度，移动Home使用实时目标点和刚体点速度。
- 删除固定巡航速度、旧返航倍率和阻力终端速度，设置 Rigidbody.maxLinearVelocity=float.MaxValue；飞行启用 ContinuousDynamic，停靠前恢复 Discrete/运动学。无固定速度上限仍受加速度、目标距离和障碍净空约束，不向物理系统写入 Infinity。
- 导航、避障、返航和停靠参数从 Inspector 序列化及 CSV 中移除；搜索范围/扫描间隔、维修能力、拾取行为、模型插槽和表现继续可编辑。CSV 现为29列（25参数+4身份），所有保留单元格和UTF-8 BOM比对通过。BotNavigationPolicyMigration.Run() 通过 PrefabUtility 重存三个Bot资产，清理旧导航字段并比较剩余序列化数据/GUID；旧Bot基础资产显式保存原有 requiresPower=true 默认值。
- Unity Play：10个真实物理导航场景、51项检查全部通过且无障碍碰撞，含100m/s薄墙、目标在后方、起点重叠、移动Home；200m/2000m飞行峰值86.4/273.6m/s。绕墙/U形/重叠/移动Home耗时约2.23/2.06/2.44/2.14s；带初始30m/s的制动案例约3.24s，慢于上版2.18s，保留制动优先结果。真实任务71项、CSV Edit16项/Play4项通过，构建0错误/4个既有警告。
- 验证证据：[无上限策略记录](Validation/2026-10-25-bot-unlimited-policy.json)。仍为局部三维导航，不保证任意封闭迷宫；未做大量Bot压力、极端世界坐标和全部Home运动组合验证。

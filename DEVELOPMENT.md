# HY-Sandbox 项目开发文档

> 文档状态：持续维护中  
> 最近核对：2026-09-22
> Unity 编辑器：6000.3.11f1（`ProjectSettings/ProjectVersion.txt`）  
> 当前分支：`main`

### 2026-09-22
- **消除 `PlayManager.RefreshGroup` 重组后的物理停顿**：重组前记录原 `ControlUnit` 的 Rigidbody，创建新分组后按新刚体质心迁移原刚体的点速度和角速度，并显式唤醒新刚体。断开方块或爆炸触发重新分组时，分离组不再因新 Rigidbody 默认速度为零而停顿一个物理步。
- **验证范围**：已通过代码差异检查、`git diff --check` 和 `dotnet build HY-Sandbox.sln --no-restore`；尚未在 Unity 6000.3.11f1 Play Mode 验证断开、爆炸、多分组和旋转运动下的连续性。

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

`InputManager` 统一处理 `B`/`Tab`/`F`、敌方蓝图开发者快捷键和模式光标状态：建造锁定模式显示并限制鼠标，建造自由飞行模式隐藏并锁定鼠标，游玩模式默认隐藏并锁定鼠标，按住 Alt 时显示并限制鼠标。`PlayerCockpitHealthUI` 在 `PlayPanel` 左下角显示玩家驾驶舱耐久度，血条颜色按比例从红色过渡到绿色。

进入游玩模式前，`PlayManager.CanStartPlay` 会检查当前构造体是否存在有效驾驶舱；成功后由 `ControlUnit` 刷新子模块并取得运行时所有权。退出游玩模式时恢复建造状态并清理运行时分组。

### 3.2 建造、连接与碰撞

`BuildManager` 的主要流程：

1. 从 `Resources/Blocks` 选择资源并创建 Ghost 预览。
2. UI 上的指针输入先被拦截；射线检测方块并在自身 `canConnect`、未占用且对面没有实体方块的 Connector 上显示 0.9×0.9 白色圆角线框，按连接面的世界法线向外偏移 0.015。按网格和目标旋转计算吸附位置；没有可用连接点、移出目标或退出模式时清除 Ghost 与提示。
3. `Block.IsBlockedGhost` 和 `BuildManager.IsBlocked` 检查重叠，阻挡时禁止放置。
4. `CreateBlock` 实例化 Prefab，应用默认值并写入当前存档。
5. 选中方块后支持键盘移动、15 度旋转、移动/旋转轴拖拽、复制和删除。

`Block` 根据尺寸在六个方向生成连接点，通过位置和相反法线匹配相邻模块，维护 `neighbors`。`IsConnectorAvailableForPlacement` 会同时检查本方 `canConnect`、占用状态和对面方块是否存在，避免对着 `canConnect=false` 的邻接面显示提示或放置。连接成功后创建连接视觉对象；`DisConnectAllConnectors` 用于删除、拆分和游玩结束清理。

### 3.3 存档与加载

`SaveManager` 管理两个命名空间：玩家存档 `Saves` 与敌方蓝图 `EnemyBlueprints`，支持创建、读取、删除、重命名、复制和文件名校验。列表中的 Duplicate 按钮会在当前命名空间生成不覆盖已有文件的 `Copy` 名称，并刷新列表；复制不会切换当前加载目标。`BlockData` 保存资源路径、尺寸、位置和旋转等重建所需数据。

`BuildManager.LoadAllBlocks` 使用协程逐个实例化，支持加载进度、取消旧加载、无法加载数据清理和可选的相机环绕；加载前会从完整 `BlockData` 计算含旋转尺寸的逻辑包围盒，加载镜头始终围绕该固定范围取景，完成后停留在新构造体的合适观察距离。方块数、总质量、用电模块 `standardWorkingPower` 总需求和发电机 `outputPower` 总输出在主加载 `for` 循环中按成功恢复的 Block 增量累计并同步到 `BlueprintUIPanel`，不额外遍历已加载方块。存档身份依赖文件中的模块数据，不应把运行时 `GetInstanceID()` 当作跨会话稳定 ID。

### 3.4 游玩、供电与推进器

`PowerGeneratingUnit` 提供 `outputPower`；`PowerTransmissionDevice` 每帧按 `maxConnectionDistance` 的世界坐标轴对齐立方体范围重建发电机/输电设备双向连接，通过设备和发电机共同组成的连通网络传递功率，并向任一设备 `powerRange` 立方体范围内带 `Power` 的 Block 供电。两种范围值均表示半边长，边界使用逐轴 `<= range` 判定。同一网络汇总所有发电机输出、对去重后的负载均分；不同网络同时覆盖同一负载时功率叠加，断连或禁用后旧功率会被清零。`DebugManager` 集中控制供电范围、网络连接以及 Block 连接/耐久/供电状态图标；`Info` 在连接、耐久或供电数据变化时由对应组件主动刷新状态，Normal 状态不显示，其余状态由场景中的 `IconManager` 统一提供。状态图标投影自 Block 的 `Center` 到独立 Screen Space Overlay Canvas，排序固定为普通 UI（0）高于图标（-1），并由 `IconManager` 用 `sin(Time.unscaledTime)` 统一驱动透明度。多个供电范围通过坐标压缩生成只含并集外表面的单个 Mesh，按孤立/已连接/有功率状态切换子网格颜色，内部重叠面不渲染，连接关系继续使用运行时复用的虚线 `LineRenderer` 表示。

`ControlUnit` 聚合驾驶舱、主推进器和悬浮推进器，读取玩家输入并把世界方向传给推进系统。敌方 `EnemyController` 默认每 0.5 秒采样一次目标/避障方向，并以响应速度渐进更新模拟输入；敌方不再直接修改 Rigidbody 的旋转或力，转向和位移统一交给 `MainThruster`/`UniversalThruster` 根据 `MovementInput` 施加。`Power.isWorking` 作为悬浮控制器、推进器和炮塔的硬启停条件；`Power.efficiency` 缩放悬浮推力/姿态修正、各推进器有效推力，以及炮塔伤害和射速。`HoverFlightController` 使用高度、重力补偿和姿态 PID 逻辑分配悬浮推力。

`ThrusterAllocator.Solve` 将力与力矩目标组成 6 维约束，通过带阻尼的最小二乘和上下界迭代求解各推进器输出。`ThrusterVisualEffect` 驱动已保存、完全解包到模型喷口的 SpaceKit 喷焰资产，持续发射密度保持稳定，亮度随真实推力平滑变化；核心使用重叠粒子的淡入淡出保持连续，粒子跟随喷头挂点，避免世界/局部方向混用。停机、失电或退出运行模式时停止发射；离开镜头后仍正常计时并结束。

### 3.5 UI、敌人和效果

`MainUIButtons` 负责按钮事件和操作模式，DebugSettingsPanel 提供供电范围、连接线、Block 连接状态、耐久状态和供电状态五个独立开关；`BuildPalette` / `BuildPaletteItem` 管理已保存的 23 个图标按钮、六类导航、滚动、悬停名称和选中颜色；`SaveUIPanel` 负责玩家/敌方蓝图列表；`BlueprintUIPanel` 显示当前建造目标名称、方块数量、总质量、需求功率和发电机总输出，并在搭建、拆除、Undo/Redo 时刷新，在存档或敌方蓝图异步加载期间逐块更新；`ActionCounterUI` 显示撤销/重做数量；`GlobalTextStyler` 统一 Chakra Petch 字体与轻量阴影样式，避免小按钮文字因粗描边显得拥挤。`EnemySpawner`、`EnemyController`、`MeteorShower`、`TurretWeapon` 和 `RepairBot` 组成战斗与环境事件链；RepairBot 只选择与 home 同属一个 ControlUnit、且位于 `targetRange` 球形范围内的受损方块，寻路避障按间隔采样并渐进转向，返航时对准停靠姿态后平滑减速归位，其飞行反馈由保存的双层青色尾迹和速度驱动喷口粒子组成，维修时从模型工具端发射双层能量束，并使用素材电弧与真实耐久恢复脉冲。`DestroyManager` 在驾驶舱摧毁时按爆炸半径和概率断开同一运行时单元内的 Block，再重新分组并施加爆炸冲量（当前不造成伤害）；`VisualEffectsManager` 和 `StylizedBeamEffect` 负责放置、删除、移动、碰撞、爆炸和陨石冲击反馈。所有选中、Ghost、放置、旋转、维修、摧毁、爆炸和陨石冲击中的平面环形元素均已移除，改用 HDR 加法粒子、放射光痕、短束流和动态点光；烟尘继续使用普通 Alpha Blend 保持暗部层次。失去驾驶舱的断裂 Rigidbody 会获得最长 6 秒、按速度衰减的烟雾/余烬拖尾，同时限制全局活动数量为 24。

### 3.6 物理模拟与性能

项目使用 3D PhysX 作为运行时物理后端。为降低物理线程在大型构造体、敌人和爆炸冲量场景下的持续计算压力，当前项目设置为：固定物理步长约 0.02 秒（50 Hz；`ProjectSettings/TimeManager.asset` 使用 Unity 6000 的有理数格式保存）、单帧物理追赶上限 0.1 秒、默认位置求解迭代 4 次、默认速度求解迭代 1 次。碰撞回调复用已启用，Transform 自动同步保持关闭；2D 物理设置未改变。降低步频和迭代次数会减少 CPU 占用，但高速碰撞、堆叠稳定性和推进器控制手感需要在 Play Mode 复核。

### 3.7 Block 美术、挂点与渲染风格

当前功能模块使用 `Assets/Art/SpaceKit` 与用户整理的 `Assets/Art/Temp`：保留 Cube1 基础块、Battery 发电机/维修舱、无人机、炮塔、AirVent、主推进器和全向底座的手动选择，补全驾驶舱、门、控制器、机架、楼梯及大小推进器。替换实例全部 Unpack Completely；根 Block、BoxCollider、连接点及启用掩码、密度、耐久和 Resources 名称保持一致，视觉附带碰撞体移除。

`Assets/Art/BlockVisuals` 保存适配后的粒子 Prefab、材质、依赖迁移清单、Unity 验证结果与预览。`BlockArtIntegrator` 只处理没有 `ArtIntegration_SpaceKit_v1` 标记的模块；旧 `IndustrialArtGenerator` 跳过已经集成的模块，避免覆盖手动调整。基础块与 Connector 保留既有外观。

炮塔保留固定底座，水平轴绑定 `SM_Prop_Turret_Large_Top_01`，俯仰轴绑定 `SM_Prop_Turret_Large_Barrel_01`；aimPivot 与 Muzzle 已从停用的旧模型迁移，枪口按网格端环定位，开火闪光与射线共用此挂点。默认转速、俯仰限位、伤害和射速不变。全向推进器只旋转喷头，底座保持固定；主推进器分别覆盖 2/4 个物理喷口，悬浮特效朝风口下方发射。

RepairBot 的模型朝向与导航 +Z 对齐，维修束从 RepairOrigin 工具端发出，容器明确绑定 home/Outside。初始化停靠状态不再被冷却提前返回阻断，冷却结束前保持停靠；飞行和维修粒子均为可编辑资产。发电机与维修舱各增加两盏无阴影状态灯。选中和 Ghost 高亮跳过粒子/尾迹，避免覆写特效材质。

实时反馈通过 `Resources/VFX/BlockVfxLibrary` 读取适配的建造、电弧、爆炸、烟尘、炮口与命中粒子。运行时不再 AddComponent 创建 ParticleSystem；光束仍用实时端点网格，材质保存为资产。瞬时粒子最多同时 64 个实例，断裂烟迹最多 24 个；原始 SpaceKit 储备效果保留，项目实际使用减量版本。完整编辑说明见 `Assets/Art/BlockVisuals/README.md`。

### 3.8 科幻工业备用素材库

`Assets/Art/SpaceKit` 从被 gitignore 忽略的 CUBE Spaceships Pack 01 与 PolygonSciFiSpace 复制筛选素材，包含驾驶舱、机身/机翼、推进器、起落架、能源/传感设备、炮塔/弹体、维修机器人、结构/舱壁、货物/控制台/灯具、陨石/残骸、整船参考及四类粒子效果。22 类 Prefab 每类 2–7 项，共 105 个；另选 10 个配色材质。完整依赖共 349 项（103 FBX、105 Prefab、29 材质、30 贴图、82 碰撞网格），约 19.02 MiB，不含 meta 和预览。

素材使用独立 GUID，所有依赖落在 SpaceKit 或 Unity/URP 内置包内。旧材质通过 Editor API 转换为 URP Lit/Particles Unlit；飞船去除旧 Rim 算法，陨石简化为低光泽岩石基色。原包与 Main 场景保持原状。部分备用素材已经通过 BlockVisuals 适配到 Resources/Blocks 和运行时 VFX；SpaceKit 原始储备的导入尺寸、面数、材质槽和粒子上限仍见逐项清单。

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

- **建造目录**：新增 Prefab 后需要执行 Build Palette Bake，将分类与图标保存入场景；不会在运行时自动复制按钮。尚未做超大目录性能或所有分辨率的手工操作测试。

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
| P2 | 多个 RepairBot、连续大型爆炸和大量高速断裂部件会叠加透明粒子开销 | 已限制单个粒子系统粒子数并将断裂烟迹全局上限设为 24；仍需在大型蓝图战斗中记录透明 Overdraw、Batches 和主线程峰值。 |
| P2 | UI 同时存在 uGUI 与 IMGUI | 将诊断面板迁移到统一 UI 系统，避免分辨率、输入焦点和生命周期不一致。 |
| P2 | 历史日志包含旧版本功能描述 | 每次发布标记版本和验证日期，避免把日志中的“计划/旧实现”当作当前契约。 |
| P2 | SpaceKit 集成功能模块尚未在大型蓝图中完成 GPU/CPU 压力验证 | 使用 100、500、1000 模块蓝图记录 Batches、SetPass、粒子 Overdraw、动态灯和脚本耗时。 |
| P2 | 双轴炮塔尚未完成主场景战斗回归 | 在不同安装朝向、移动载具和高低目标下验证索敌、遮挡、俯仰边界、光束起点、命中判定与断电恢复。 |
| P2 | SpaceKit 原始储备的粒子/整船预算偏高 | 已集成的 VFX 已降低容量并限制瞬时实例数；原始 3100 容量效果与 16602 面运输船仍只作储备，来源许可沿用原包条款。 |
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
- `BlockVfxBaker.BakeMissing()`：从 SpaceKit 派生并保存缺失的效果 Prefab 与 BlockVfxLibrary；已有资产不覆盖。
- `BlockVfxBaker.RepairContinuity()` / `ConfigureContinuity()`：同步模板和已解包副本的尾焰、电弧、烟尘曲线及离屏计时；配置持续密度/透明度强度模式。
- `BlockVfxTemporalValidation.Validate()` / `RenderSequence()`：隔离 PreviewScene 中进行多帧率、多强度采样、启停/重播检查，并导出 URP 连续帧。
- `BlockArtIntegrator.Integrate()`：保留既有模型选择与 Block 契约，补全视觉、挂点、灯光和组件绑定。
- `BlockArtValidation.Validate()`：检查来源依赖、嵌套实例、丢失引用、粒子材质、运动链与挂点方向，保存报告。
- `BlockArtPreview.Render()` / `RenderBlocks()`：在独立 URP PreviewScene 中渲染并导出图片；可选 transparent 参数输出 RGBA 透明背景。
- `BlockArtPlayProbe.Run()`：在隔离 Play Mode 场景检查供电/推进/射击/维修完整链路并返回原场景。
- `AssetParticleEffect.SetIntensity()` / `PlayOnce()`：持续尾焰/接触按材质透明度调强度，烟迹按发射密度调节，瞬时效果显式重播；以 isEmitting 判断快速重新启动，禁用时清空粒子及灯光；`ReleaseAfterPlayback()` 管理瞬时效果数量与销毁。
- `BlockVfxLibrary.Play()`：按事件实例化 Resources 资产库引用的效果。
- `BlockStatusLight.Update()`：更新发电机与维修舱状态灯。

### Effect 特效

#### `Assets/Scripts/Effect/DetachedPartSmokeTrail.cs`

- `public static void Attach(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`：为无驾驶舱断裂刚体挂接或刷新受全局数量限制的烟雾拖尾。
- `private void Initialize(Rigidbody body, Vector3 worldAnchor, float effectIntensity)`：创建世界空间烟雾和短余烬 TrailRenderer，并缓存目标刚体。
- `private void Refresh(Vector3 worldAnchor, float effectIntensity)`：重复受爆时刷新锚点、强度和剩余寿命，不重复创建组件。
- `private static void ConfigureEmberTrail(TrailRenderer trail)`：配置高速碎片的短橙色余烬拖尾。
- `private void Update()`：按线速度、角速度、爆炸强度和生命周期衰减实时控制发射。
- `private void StopAndRelease()`：停止发射、分离残留粒子并延迟销毁视觉对象。
- `private void OnDestroy()`：释放全局活动拖尾计数。
- `private static Gradient CreateEmberGradient()`：创建白热到暗红的余烬透明渐变。

#### `Assets/Scripts/Effect/IndustrialPartMotion.cs`

- `private void Update()`：低频率更新功能件旋转、浮动和自发光脉冲，不修改物理或存档状态。
- `public void Configure(Transform[] newSpinTargets, Vector3 newSpinAxis, float newDegreesPerSecond, Transform newBobTarget, float newBobAmplitude, float newBobFrequency, Renderer[] newGlowRenderers, Color newBaseEmission, float newEmissionPulse)`：为生成的视觉层注入动画目标和共享材质参数。

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
- `void HandleDuplicate(Vector3 newPos, Quaternion newRot)`： 处理对应的输入、选择、拖拽、移动、旋转或建造交互。
- `private void HandleBuildingPreview()`：拦截 UI 输入、筛选可用连接点、刷新线框与 Ghost；无连接点时清理，穿透搜索最多 64 步避免无限循环。
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
- `public static void TryPlayBlockExplosion(Block block)`：触发 Block 的 HDR 爆炸粒子、放射光痕、闪光和镜头反馈。
- `public static void TryPlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`：在真实维修 tick 时触发收束线、目标脉冲、火花和小型闪光。
- `public static void TryAttachDetachedPartSmoke(Rigidbody body, Vector3 worldAnchor, float intensity)`：为爆炸后无驾驶舱的刚体挂接烟雾拖尾。
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
- `private void ApplySceneLook()`： 将计算结果或配置应用到 Unity 组件、材质、物理对象或模块。
- `private void PlayBlockPlaced(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockRemoved(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockExplosion(Block block)`：分阶段播放爆炸火花、火球、放射光痕、烟雾、闪光和镜头反馈。
- `private IEnumerator PlayExplosionAftershock(Vector3 center, float scale, Color emberColor, Color smokeColor)`：延迟播放受控数量的爆炸余震粒子、次级光痕与闪光。
- `private void PlayObjectDestroyed(GameObject target)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayRepairPulse(Vector3 origin, Vector3 target, Color color, float width)`：组合一次维修命中的加法火花、放射光痕、短束流和闪光。
- `private void CreateExplosionShrapnel(Vector3 center, float scale, Color color)`：生成受控数量的放射碎片光痕。
- `private void PlayBlockMoved(Block block, Vector3 from, Vector3 to)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void PlayBlockRotated(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void ShowBlockSelection(Block block)`： 触发游玩流程、UI 状态或视觉反馈的更新。
- `private void ClearBlockSelection(Block block)`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void UpdateGhostPreview(Transform ghost, bool isBlocked)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
- `private void ClearGhostPreview()`： 删除、清理或重置对象、缓存、连接、存档或运行时状态。
- `private void DecorateMeteor(Meteor meteor)`： 封装该类型的内部流程，连接调用方与 Unity 组件或数据状态。
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
- `BuildPalette.ShowTooltip(BuildPaletteItem)` / `HideTooltip(BuildPaletteItem)`：共享名称栏显示与归属控制。
- `BuildPaletteItem.SetSelected(bool, Color)`：更新边框颜色。
- `BuildPaletteItem.OnPointerEnter/Exit` / `OnSelect/Deselect` / `OnDisable`：鼠标、键盘焦点及禁用时的名称提示生命周期。
- `ConnectorPlacementHints.Show(Camera, float, LayerMask, GameObject)` / `Hide()` / `OnDisable()`：设置范围扫描上下文或清理提示状态。
- `ConnectorPlacementHints.ScanNearbyBlocks()` / `CanDisplay(Block, Connector)`：扫描相机附近方块，并复用 Block 的可放置连接点判定。
- `ConnectorPlacementHints.LateUpdate()`：过滤禁用/占用点，以共享 Mesh/Material 按世界坐标和法线提交线框。
- `BuildPaletteBaker.Bake()`：通过 Unity API 刷新 Main 的保存目录、透明图标、Mesh/Material 与组件引用。
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

## 9. 函数索引维护规则

新增、删除、重命名或改变职责的函数，必须在同一提交更新本节；签名变化替换旧条目，行为变化同时修改描述和变更日志。索引以源码为准，自动提取遗漏的多行签名时手工补充。

### 9.1 C# 代码规范

具体执行规范见 [`Assets/Scripts/AGENTS.md`](./Assets/Scripts/AGENTS.md)。本轮全局审查已统一项目脚本中省略的私有访问修饰符，并在连接重建、建造加载、游玩分组、存档复制、摧毁爆炸、敌方移动、推进器视觉和输入模式切换等关键路径补充意图注释；未改动序列化字段名、场景层级或资源引用。

- **已通过代码/文件确认**：脚本访问修饰符扫描无遗漏（接口声明除外），本次触及脚本使用统一 CRLF，新增规范文件和注释已纳入 Git diff。
- **已通过构建确认**：`dotnet build HY-Sandbox.sln --no-restore` 通过（0 错误；仅有既存 `ProfilerCaptureAnalysis.WriteCounters` 过时 API 警告）。
- **尚未验证**：Unity 编辑器导入、Inspector 序列化、Play Mode 交互、运行时日志和性能表现。


## 10. 变更日志

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

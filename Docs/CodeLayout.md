# HY-Sandbox 源码目录分类

运行时代码位于 `Assets/Scripts/Features`。目录本身按递归功能树组织：父目录保留主要入口、协调器和核心契约；独立职责进入具名子目录，内部存在不同生命周期或独立算法时继续细分。类型沿用原名称和全局命名空间，源码位置不改变 Unity 脚本身份。

## 主入口与子功能

以下路径相对于 `Assets/Scripts/Features`；全部文件见 [真实源码树](CodeIndex.md#代码文件夹位置)，入口调用链见 [运行时函数图](RuntimeFlow.md)。

| 功能根目录 | 根目录保留 | 细分职责 |
| --- | --- | --- |
| Bootstrap | GameManager | 单一启动入口，保持简单 |
| Bots | Bot | Navigation；Repair（含维修专属搜索/表现）；Collection；Presentation |
| Building | BuildManager、SaveManager | Actions；BlueprintLoading；BlockDataTables；BuildingEditor；Interaction；Interface |
| Combat | DestroyManager | Damage；Enemies；Meteors；Turrets（再分 Projectiles）；Interface |
| Diagnostics | DebugManager | Physics；Bots；Interface |
| Flight | HoverFlightController | Hover；Thrusters；Presentation |
| Player | InputManager、Player、PlayerController、PlayerHealth | 输入与玩家主体4份脚本内聚，暂不机械分层 |
| Power | Power、PowerNetwork | Devices；Network；Presentation |
| Presentation | VisualEffectsManager | Camera；Blocks；Effects；Interface |
| Salvage | SalvageSettings | Cargo；Drops；Economy |
| Siege | SiegeSession、SiegeMatch、SiegeSettings、SiegeProgress | Battle；Fleet；Interface；Workshop；Validation |
| Simulation | PlayManager、Block | Connections；Groups；Units；Physics；Presentation；Diagnostics |
| World | WorldSessionManager、WorldGenerationSettings | Terrain；Buildings；Outposts；Streaming；Persistence；Presentation；WorldLab；Validation |

## World 的递归结构

```text
World/
├── WorldSessionManager.cs          会话生命周期、准备/游玩/退出协调
├── WorldGenerationSettings.cs      会话的可编辑生成配置
├── Terrain/
│   ├── ProceduralVoxelWorld.cs     地形主体、重建队列与世界对象所有权
│   ├── StreamingWorldTerrain.cs    确定性流式地形采样
│   ├── VoxelWorldData.cs           体素状态
│   ├── VoxelWorldChunk.cs          单块对象与网格提交
│   ├── Meshing/                    纯网格算法及工作线程快照
│   └── Destruction/                支撑、塌落与碎片生命周期
├── Buildings/
│   ├── WorldBuildingManager.cs     建筑集合与生命周期
│   ├── BuildingRuntime.cs          单栋建筑运行主体
│   ├── Topology/                   邻接、损伤与修缮
│   ├── Placement/                  放置、净空、预览选择与验证
│   ├── Data/                       模板、文件库与建筑快照
│   └── Presentation/               渲染缓存
├── Outposts/                       驻军数据、遭遇调度与据点HUD
├── Streaming/                      地形列/建筑加载、休眠与玩家保护
├── Persistence/                    世界差量和WorldLab文件存储
├── Presentation/                   远景、过渡网格与加载面板
├── WorldLab/                       独立实验场入口
└── Validation/                     显式Player路线
    └── WorldLab/                   实验场物理/碎片探针
```

目录代表职责归属，不引入新的程序集、命名空间、反射装配或运行顺序。仅有一个文件且职责独立的子功能可以保留单文件目录；小而内聚的顶层功能不强制拆分。运行时Player验证仍位于运行时编译范围，只有显式参数启用，不移入 Editor 导致正式Player失去验证入口。

## Editor 与维护

`Assets/Editor` 按相同功能映射组织。Art、Building、Combat、Presentation、Siege、World 将制作、预览、领域验证、基准采样与Player构建入口分开；SiegeSceneAuthoring/SiegePlayChecks、WorldSessionAuthoring等主装配或总验证入口保留根目录。其他小目录沿用内聚结构；不处理第三方工具。

新增脚本先确定业务归属和主入口，再放入相应子功能。移动资产使用 AssetDatabase，保留原 `.meta` 与 GUID；同步所有工具硬编码路径与当前文档链接，不改写历史验证报告。不得新建 `Misc`、`Other` 等未分类堆放目录。

```powershell
python Tools/CodeReadability/generate_navigation.py
python Tools/CodeReadability/generate_flow_browser.py
dotnet run --project Tools/CodeReadability -- --check .
dotnet build HY-Sandbox.sln --no-restore
git diff --check
```

2026-11-02迁移清单、字节/GUID/序列化资产基线和实际验证范围见 [验证记录](Validation/2026-11-02-code-layout.md)。

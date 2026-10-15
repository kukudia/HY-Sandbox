# 可交互、可破坏程序化世界实验场

2026-10-15：研究原型，独立于 Main，不改玩家蓝图/货仓存档。不代表完整开放世界系统。

研究结论、后续载具玩法切片和扩展地图的测量门槛见 [WorldResearch](WorldResearch.md)。

建筑融合已加入本实验场，操作、目录配置和边界见 [WorldBuildings](WorldBuildings.md)。默认生成Outpost据点，支持放置用户建筑、供电战斗、方块破坏、断裂残骸和地形失稳脱锚。WorldLab实例和放置预览不生成实体地基，编辑器底座仅保留为制作网格及支撑范围元数据。

## 使用

打开 `Assets/Scenes/WorldLab.unity` 或 Tools → HY-Sandbox → World → Create or open World Lab，然后 Play。

- 右键拖动观察，WASD飞行、Space/Ctrl升降、Shift加速，F回到初始视角。
- 鼠标指向地表：左键持续采掘，E范围爆破，中键填补金属单元，R恢复当前种子世界。
- B选择建筑放置，Q/T旋转90°，左键确认，Esc取消；右侧面板可选模板和阵营。放置模式下暂停采掘。R同时恢复种子地形与配置的初始建筑。
- F5保存种子、体素差量与建筑快照，F9校验后恢复；路径为 `Application.persistentDataPath/WorldLab/seed-14721.json`，替换前保留 `.previous`。版本、种子、尺寸、生成器指纹、材质、边界、重复单元，以及建筑资源/身份/耐久全部校验后才替换活动世界。地形只保存完整单元变化，不保存尚未达到硬度的局部伤害；旧纯体素差量迁移为空建筑列表。
- 选中 ProceduralWorld，在Inspector调 seed、dimensions、cellSize、chunkSize、rebuildsPerFrame；Preview重生成，再Bake保存真实Mesh资产与场景。烘焙操作写 `Assets/WorldLab/Meshes`，只适用于这个实验场，不能让多个独立世界共用此目录。
- 实验场运行时保持后台更新并限制60FPS；禁用时恢复原后台/帧率设置。这样软件黑屏遮罩持有焦点时仍可做真实帧物理验证。

## 表达与边界

体素是数据，不是逐方块GameObject。默认64×24×64单元，每格2m；16格一块，共32个MeshRenderer/MeshCollider。三种材质：土20、岩60、金属100硬度。伤害按球形距离衰减并累计；破坏后清除单元和部分伤害记录。仅重建修改单元所在块及六邻居所在块，每帧默认最多2块。视觉与碰撞使用同一网格，排队期间可能短暂保留旧碰撞（最多取决于队列长度）。没有每体素刚体，也不产生无限碎片。

`TurretHitResolver.Apply`已接入体素命中；已有方块伤害/阵营/冲量规则沿用。实验场独立飞行工具验证编辑和碰撞，尚未把体素地形放入 Main 或完成载具交战验收。

金属支撑实验：破坏后按六邻接检查金属连通片，邻接土/岩或接触世界底层视为有支撑。默认仅不超过32格的无支撑片段转换为刚体，最多12片、12秒寿命；每格BoxCollider组成复合碰撞，网格只含外露面。分离时立即刷新源碰撞，可能形成一次同步重建开销，不能套用普通每帧2块预算；参数在VoxelStructureCollapse Inspector中。较大孤岛保持静态；只在发生破坏时检查，不是完整结构力学。碎片不进入玩家Block/供电/蓝图，也不保存动态姿态；F5保存的是脱落后静态单元差量。R/F9及禁用组件清理碎片。

现阶段不含：全地形倒塌、碎片回收经济、开采掉落经济、无限流式加载、LOD、自动保存/加载、联网同步。重新进入Play恢复Bake数据，F9可恢复已保存差量，R重新生成会丢弃未保存会话破坏；世界差量与载具蓝图JSON完全分离。

## 资产规格与来源

Unity6000.3.11f1 / URP / Windows；研究假设为128m×48m×128m有限区域，最近观察约2m，适合方块原型而非光滑自然地形。坐标Y-up、米、根Pivot为世界最小角；材质使用自建URP Lit，不依赖外部贴图、骨骼或音频。静态非凸MeshCollider，不可直接把整个世界设成动态刚体；不设置无依据LOD。

源是 `Features/World/VoxelWorldData.cs`、`VoxelSurfaceMesher.cs` 和Editor制作工具；种子14721。地形、遗迹规则、材质均为本次项目内程序化制作，无外部AI或第三方美术输入。生成规则、Inspector参数和Bake入口保留可编辑性。所有资产有meta，已有脚本GUID不改。

## 验证

本次融合专项和回归证据见 [WorldBuildings](WorldBuildings.md)。独立实验场已实际通过建筑放置、存档、供电、伤害与真实帧脱锚验证；正式Player构建和大型世界性能未验收。

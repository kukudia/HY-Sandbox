# Block 美术集成

本目录是 SpaceKit / Temp 素材的项目适配层。模型、灯光、喷口和维修工具挂点最终保存在 `Assets/Resources/Blocks`；所有嵌套替换实例均已 Unpack Completely，仍共享 Art 下的 Mesh、Material 和 Texture。

## 规格与保留项

- Unity 6000.3.11f1 / URP 17.3，1 单位 = 1 米；保留 Block 格子尺寸、根 BoxCollider、密度、耐久、连接点及启用掩码、Prefab GUID 和 Resources 名称。`RepairBotContianer` 保留原拼写以兼容加载。
- 保留用户选定的基础块 Cube1 材质、发电机 Battery03、维修舱 Battery01、无人机、炮塔、AirVent、四喷口大推进器和双喷口全向底座；完成其他功能块的 SpaceKit 外观。
- 视觉模型的附带 MeshCollider 已移除。Bot 独立 Rigidbody / BoxCollider 和 Block 根碰撞体保留；炮塔射线仍只结算一次伤害。
- 性能初稿：喷口每组 72 粒子容量；普通瞬时效果 48；爆炸四层共 320；最多同时 64 个瞬时实例，断裂烟迹最多 24 个。无阴影状态灯每个发电机/维修舱 2 盏。大型构造体的性能仍需 Profiler 验证。

## 编辑入口

- 在各 Block Prefab 内编辑 `Model` 下的模型、`Nozzle_*` / `Muzzle` / `RepairOrigin` 挂点及粒子。挂点的局部 **+Z 指向喷射方向**；推进喷口始终反向于推力。
- 主推进器 Engine03 喷口按源网格端环定位：局部 X = ±1.0675，Z = -2.9854；全向喷头 X = ±0.588，Y = -0.019，Z = -0.787；悬浮风口 Z = +0.5257；炮管末端 `(0, 0.0645, 4.479)`。
- Bot 导入模型原本朝 -Z，模型子节点旋转 180° 使导航 +Z 与机头一致；维修端点位于网格前端 `(-0.0205, 0.14, -0.998)`。容器明确绑定 `home=Model` 和 `outside=Outside`。
- `PowerGeneratingUnit`、`RepairBotContianer` 根上的 `BlockStatusLight` 控制状态灯。维修舱在 Bot 工作时切换绿色脉冲。
- `VFX/` 是保存的独立效果 Prefab，`Assets/Resources/VFX/BlockVfxLibrary.asset` 引用这些资产。调整模板不会自动覆盖已经完全解包到 Block 的粒子；需要同时修改对应实例，这是完全解包后的预期行为。
- `Tools/HY Sandbox/Block Art` 提供补齐缺失集成、依赖迁移、验证和 Play Mode 探针。已有 `ArtIntegration_SpaceKit_v1` 标记会跳过自动重建，避免覆盖后续手调。旧 Industrial 重建同样跳过已集成模块。

## 来源和处理

来源仍为用户本地 CUBE Spaceships Pack 01 与 PolygonSciFiSpace；许可沿用原包条款，工具不授予额外授权。SpaceKit 来源/GUID/哈希见其 Catalog.json。`Dependencies.json` 记录 Temp 新增模型/碰撞依赖的源路径与 Art 副本；复用现有 SpaceKit 材质与贴图，不依赖被 gitignore 忽略的目录。

- ThrusterJet / HoverJet / BotFlight：Polygon `FX_Exhaust_Trail` 与 `FX_Flame_Booster_Round`，校正 mesh 轴向、统一尺寸曲线、蓝青渐隐和发射预算，流束使用已有 `FX_SphereGlow` 材质。
- BuildBurst / RepairPulse / RepairContact：`FX_Electricity`，缩短寿命、减少发射数，区分瞬时与持续效果。
- MuzzleFlash / ImpactBurst：`FX_Laser_Shot` 的闪光层，移除演示弹体和碰撞子发射器；真实命中由原有 hitscan 逻辑负责。
- Explosion / BreakBurst：`FX_Explosion`，四层容量限制，原始爆炸贴图、网格和材质保留。
- DetachedSmoke / SmokeBurst：`FX_Steam`，世界空间烟尘、短寿命与灰色 Alpha Blend。
- Beam and Trail：保存为材质资产，供实时端点光束和 TrailRenderer 使用。光束网格仍按端点动态更新，运行时不再创建 ParticleSystem。

## 验证

`Validation.json`：全部 Blocks、Temp 与适配 VFX 的依赖、解包、引用、Shader、挂点和预算检查。
`PlayModeValidation.json`：隔离场景中的实际供电、炮塔伤害/枪口闪光、旋转推力/喷口、全向头与底座、Bot 离舱/维修/回家、灯光检查。探针结束自动返回原场景。
`Preview/`：Unity URP 实际渲染联系表。未覆盖真实玩家长时间操作、复杂移动母舰返航和大规模并发性能。

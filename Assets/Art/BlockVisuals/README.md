# Block 美术集成

本目录是 SpaceKit / Temp 素材的项目适配层。模型、灯光、喷口和维修工具挂点最终保存在 `Assets/Resources/Blocks`；所有嵌套替换实例均已 Unpack Completely，仍共享 Art 下的 Mesh、Material 和 Texture。

## 规格与保留项

- Unity 6000.3.11f1 / URP 17.3，1 单位 = 1 米；保留 Block 格子尺寸、根 BoxCollider、密度、耐久、连接点及启用掩码、Prefab GUID 和 Resources 名称。`RepairBotContianer` 保留原拼写以兼容加载。
- 保留用户选定的基础块 Cube1 材质、发电机 Battery03、维修舱 Battery01、无人机、炮塔、AirVent、四喷口大推进器和双喷口全向底座；完成其他功能块的 SpaceKit 外观。
- 视觉模型的附带 MeshCollider 已移除。Bot 独立 Rigidbody / BoxCollider 和 Block 根碰撞体保留；炮塔射线仍只结算一次伤害。
- GPU 效果保留 UNI 原生模拟结构；最多同时 64 个瞬时实例、24 个断裂烟迹。高并发 GPU/Overdraw 仍需实机场景分析。

## 编辑入口

- 在各 Block Prefab 内编辑 `Model` 下的模型、`Nozzle_*` / `Muzzle` / `RepairOrigin` 挂点及 Graph。挂点的局部 **+Z 指向喷射方向**；推进喷口始终反向于推力。
- 主推进器 Engine03 喷口按源网格端环定位：局部 X = ±1.0675，Z = -2.9854；全向喷头 X = ±0.588，Y = -0.019，Z = -0.787；悬浮风口 Z = +0.5257；炮管末端 `(0, 0.0645, 4.479)`。
- Bot 导入模型原本朝 -Z，模型子节点旋转 180° 使导航 +Z 与机头一致；维修端点位于网格前端 `(-0.0205, 0.14, -0.998)`。容器明确绑定 `home=Model` 和 `outside=Outside`。
- `PowerGeneratingUnit`、`RepairBotContianer` 根上的 `BlockStatusLight` 控制状态灯。维修舱在 Bot 工作时切换绿色脉冲。
- `VFX/` 是保存的独立效果 Prefab，`Assets/Resources/VFX/BlockVfxLibrary.asset` 引用这些资产。调整模板不会自动覆盖已经完全解包到 Block 的效果实例；需要同时修改对应实例，这是完全解包后的预期行为。
- `Tools/HY Sandbox/Block Art` 提供补齐缺失集成、依赖迁移、验证和 Play Mode 探针。已有 `ArtIntegration_SpaceKit_v1` 标记会跳过自动重建，避免覆盖后续手调。旧 Industrial 重建同样跳过已集成模块。
- `VfxEffect` 绑定 VisualEffect 数组，Intensity 在 Graph 输出层统一调节透明度，保持低推力下的连续密度。Stop 保留尾烟，Clear 清空 GPU 模拟，瞬时效果 12 秒回收。
- 推进喷口保留 +Z 挂点，内部将 UNI Gas Fire 的 -X 轴旋转到 +Z，并同步世界空间烟流方向。ScaleWSP 跟随实际缩放。
- 所有游戏特效均使用 VFX Graph，包括能量光束、机器人/掉落拖尾、陨石和残骸烟火。电力网络诊断 LineRenderer 保留。

## 来源和处理

爆炸、破碎、命中、烟雾、推进和燃烧直接使用 UNI 原生 Graph 项目副本，保留纹理、运动向量、网格和 Shader Graph。映射、来源与编辑入口见 `../VFX/README.md`。

旧 UNI 翻页粒子合成层与 Shuriken Bake/测试工具已移除。Prefab 和控制器脚本 GUID 保留，运行时类重命名为 VfxEffect；数组与 Bot/Loot 字段通过 Editor API 迁移。

## 验证

`Validation.json`：全部 Blocks、Temp 与适配 VFX 的依赖、解包、引用、Shader、挂点和预算检查。
`PlayModeValidation.json`：隔离场景中的实际供电、炮塔伤害/枪口闪光、旋转推力/喷口、全向头与底座、Bot 离舱/维修/回家、灯光检查。探针结束自动返回原场景。
`Preview/`：Unity URP 实际渲染联系表。未覆盖真实玩家长时间操作、复杂移动母舰返航和大规模并发性能。

`Assets/Art/VFX/Preview`：实际 Play Mode 自动相机渲染下的发射、停止、清空、重播采样。旧 Block Preview 联系表仅作模型/挂点历史参考，不代表当前特效。

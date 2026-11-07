# EnemySpawnPortal 资产规格与制作源

- 用途：EnemySpawner 成功生成，以及 SiegeWaveDirector 成功注册敌舰后播放；不延迟生成、不改变敌舰 AI/物理。据点驻军等直接使用工厂的调用者不自动增加特效。
- 环境：Unity 6000.6.8f1 / URP、VFX Graph 17.6，Windows PC。保守假设典型观察距离 15–150m。
- 外观：金橙光环、断续符文、紫色旋涡；0.2s 展开，保持后收拢，总长 2.4s。无音频、碰撞或动态灯。
- 空间：Prefab 原点为门中心，局部 XY 平面、+Z 朝敌舰前方，双面可见；固定在出生时舰体后侧，不随舰体运动。单位直径 1m，运行时按舰体包围盒横截面自适应，默认 6–80m。
- 预算：每门 1 个 GPU 粒子、1 个透明四边形、1 张 256×256 RGBA 纹理（线性、Clamp、Mipmaps），复用现有 VfxEffectPool，无新增活动数量上限；屏幕覆盖由最大直径控制。
- 源码：`Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs`；确定性数学纹理，不使用随机种子或外部图像，属于本项目原创程序化资源。Graph 起点为项目已安装 Unity VFX Graph 的 Minimal_System 模板。
- 交付：`Assets/Art/VFX/Graphs/EnemySpawnPortal.vfx` 与 `Assets/Art/BlockVisuals/VFX/EnemySpawnPortal.prefab`、同目录 PNG。
- 制作入口：Tools → HY Sandbox → 资源管理 → 制作敌机传送门。只补缺失资源，不覆盖手动编辑；“重制敌机传送门纹理”菜单会按源码公式重新写入 PNG，保留 GUID。需要重制 Graph/Prefab 时在版本控制下删除该目标后重新执行制作菜单。
- 编辑/预览：集中资源管理窗口选择 `EnemySpawnPortal`，通过原生 VFX Graph 或 Prefab Inspector 预览编辑。Graph 暴露 Lifetime / Tint / Intensity，尺寸曲线、旋转曲线均可直接编辑；延长 Lifetime 时同步提高 Prefab VfxEffect 的 Release After，避免提前回收。
- 游戏参数：VisualEffectsManager 的 Enemy Spawn Portal 分组控制尺寸留白及最小/最大直径，enableRuntimeVfx 统一关闭效果。
- 验收：见 DEVELOPMENT.md 对应记录；静态检查不代表视觉验收。

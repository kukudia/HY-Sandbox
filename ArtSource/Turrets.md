# 炮塔制作与参数

目标为 Unity 6000.3.11f1 / URP 的模块化可搭建炮塔。保持原 Turret 方块的连接器、1m 建造网格、耐久与核心碰撞契约。美术比例沿用低多边形 SpaceKit，近景可识别单管/双管。

源资源为项目内 `Assets/Art/SpaceKit/Prefabs/Modules/Turrets/Polygon/SM_Prop_Turret_Base_Single_01.prefab` 和 `SM_Prop_Turret_Base_Double_01.prefab`，保留其网格、材质、贴图和本地炮管变换；许可沿用项目原供应商资源许可，本次未重新审计授权。

`Assets/Editor/Combat/TurretAssetBaker.cs` 是可复现制作源。菜单 **Tools/HY Sandbox/Combat/Bake projectile turrets** 创建并保存：

- `Resources/Blocks/ProjectileTurretSingle`：一炮口，每轮一发。
- `Resources/Blocks/ProjectileTurretDouble`：两炮口，每轮两发。
- `Assets/Art/Combat/TurretShell.prefab` 与 URP 材质：独立刚体、球形触发器、可见胶囊弹体。
- `Resources/BuildPaletteEntries` 中真实 uGUI Prefab 与图标：启动时补充缺失条目，手工场景条目优先，不保存场景。

模型缩放为0.55；根偏移Y=-0.26m，底座保持固定。Horizontal在原炮架枢轴，Vertical在其下俯仰，前端网格环顶点均值确定各炮口；脚本以+Z发射。没有额外骨骼/LOD；沿用源网格与UV。碰撞沿用原方块基础盒，炮管没有额外网格碰撞体。

| 参数 | 激光 Turret | Single | Double |
| --- | --- | --- | --- |
| 额定功率 | 300 | 100 | 150 |
| 最低工作功率 | 150 | 50 | 75 |
| 单发伤害 | 12 | 12 | 12 |
| 发射间隔 | 0.45s | 0.6s | 0.6s |
| 飞行速度 | 即时命中 | 42m/s | 42m/s |
| 冲量 | 0 | 10Ns | 10Ns |
| 射程 | 45m | 45m | 45m |
| 炮弹寿命 | 无实体弹 | 2s | 2s |

所有工作参数在各 Block Prefab 的 Turret/Power Inspector 可编辑。炮弹外观、刚体与半径在 TurretShell 编辑。短缺时伤害/射速按效率下降，实体弹速度保持稳定并继承载具炮口点速度。

实体炮塔的 Barrel Recoil 配置控制炮管模型后座：世界距离0.12m、后退0.04s、复位0.22s。每轮齐射同步后座，停止后继续复位，禁用立即恢复；按初始局部位置计算，重复发射不积累漂移。Configure barrel recoil菜单只补缺失的炮管引用，保留已有参数。Refresh turret thumbnails菜单只重新渲染透明二维图和更新目录图标；PNG使用Single Sprite导入，避免Multiple无切片时图片为空。

旧 `Blocks/Turret` 资源路径和脚本GUID保留，组件迁移为 LaserTurret，公用字段由 Turret 基类接收。旧蓝图无需改资源名；旧炮塔的冲量字段移除。无人目标时自动炮塔不申报功率，SetStarted(false)立即关闭并释放功率。

制作工具再次Bake会重建自己拥有的上述炮塔、炮弹、目录和图标，覆盖这些资产的手工参数；普通游玩不运行Bake。编辑后可以直接保存Prefab，无需重新制作。

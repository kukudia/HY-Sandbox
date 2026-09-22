# 残骸回收首版

## 使用

- 在主场景的方块列表选择 `CargoHold`、`CoinHold`、`TechnologyHold`、`CollectionBotContainer`，保存蓝图后进入游玩。
- 货仓及回收舱默认要求供电。接入现有发电机/输电设备；每个新设备最低功率 10、标准功率 20，可在 Power 组件调整。
- 特殊零件货仓 2×2×2，容量 8；金币仓和科技值仓 1×1×1，容量 100。容量、是否要求供电、玩家/敌人摧毁掉落开关在 CargoHold；爆炸开关/半径/冲量在 Block。
- 特殊零件仓默认双方掉落；金币/科技值仓默认仅玩家掉落。失电只停止接收，不删除已装货物。预设货物可在 CargoHold 的 Contents 列表编辑，也随蓝图 JSON 保存。
- 蓝色灯=空，绿色灯=已有货物未满，橙色灯=满，失电变暗。资源仓以液面高度表示占比；特殊仓按容量自动安排三维格子，展示尺寸默认为每格的 0.8 倍（模型按包围盒适配）。
- 回收无人机自动预约 60 米范围内特殊零件，每次搬运一件，返回原回收舱后存入同一 ControlUnit 的可用货仓。没有仓位或回收舱失电不出动；目的仓被毁/断电、任务超时或无人机被禁用时释放物品与预约。
- 普通敌方残骸清理转金币；玩家自拆残骸不产金币，防止循环刷取。仍有驾驶舱的敌方单位因距离卸载不产奖励。
- `Resources/Salvage/Settings` 控制清理延迟、每块金币、特殊零件候选资源及掉率。默认每块 2 金币、功能件 20% 概率成为特殊掉落、10 秒后清理。只有幸存到残骸清理的模块参与回收。
- 金币/科技值是独立的资源包，会在 100 米内寻找玩家有驾驶舱、供电且未满的对应仓，实际抵达才入账。满仓时分流剩余数量；无接收仓时保留资源包。特殊掉落不受普通清理规则影响。
- 当前使用原有“结束游玩”作为安全返回入口：只保存仍在有效玩家单元中的货仓内容，保持原蓝图几何。死亡后返回丢失本次携带内容；未入仓的掉落物会随会话清理。本版没有独立撤离地图、局外商店或科技树，也不限制原有沙盒建造库存。

## 资产和可编辑源

| 资产 | 规格 |
| --- | --- |
| CargoHold | 中心 Pivot，2 米立方格，透明侧板、金属框架、单个基础 BoxCollider；默认爆炸 |
| CoinHold / TechnologyHold | 中心 Pivot，1 米立方格，共享透明/金属材质，金色/蓝色液体模型；默认不爆炸 |
| CollectionBotContainer | 从现有 RepairBotContianer 派生的独立功能变体，保留已制作的船坞/无人机外观和挂点，以金色识别条区分 |
| LootDrop | 独立数据对象，无 Block、ControlUnit 或游戏部件脚本；普通资源包与特殊零件共用显示/标记 |

可复现源为 `Assets/Editor/SalvageAssetBaker.cs`。`Tools/Salvage/Bake assets (create missing)` 只创建缺失资源；已有 Prefab、材质及 Inspector 手调不会被覆盖。要重做某一个生成资产，应先保存备份，再仅移走该目标并重新 Bake。`Tools/Salvage/Render prefab previews` 可重新生成独立渲染预览，不修改当前场景。

框架、玻璃及液面使用 Unity 基础网格，无外部贴图依赖；模型来源为本项目程序化制作。回收舱沿用项目已有 SpaceKit 资源，许可证与来源继承 `Assets/Art/SpaceKit` 清单，本次未引入外部素材。目标为 Windows/URP，沿用项目当前无 LOD 约定；后续大量透明仓体场景仍需 GPU 实测。

金币爆散每包最多 16 粒子，飞行用单个资源包与短尾迹；默认超过 64 个资源包合并同类金额，特殊零件不会因此删除。无需 Compute Shader。此限制控制视觉对象，不削减金币数。

## 实现与数据

- `Bot` 负责原有导航、避障、移动 Home 制动与停靠、飞行特效；`RepairBot` 和 `CollectionBot` 分别负责工作。原 RepairBot 的字段名/类型及脚本 GUID 保留，继承迁移后仍可编辑原参数。
- `CargoHold` 是内容与容量的唯一写入入口；`CargoHoldView` 只显示；`CargoVisual` 只复制静态 MeshRenderer，不在货仓或掉落物中实例化推进器、炮塔或嵌套无人机的行为。
- `LootDrop` 拥有预约、携带、交付状态。资源包只在抵达时扣除已接收数量，失电、满仓及仓体销毁不会提前记账。
- 新增的 `BlockData.cargo` 对旧 JSON 向后兼容；删除 Undo / 创建 Redo 保留货物。返回保存先写临时文件再替换，保留 `.cargo.bak`；保存失败会阻止结束游玩，避免静默丢失现场。
- `DestroyManager` 在真实摧毁前释放仓体内容；定时清理通过 `WreckSalvage` 幂等结算，不使用 OnDestroy 生成奖励，避免加载/退出重复掉落。

验证入口：`Tools/Salvage/Run Play Mode validation`。测试只在当前场景的 Play Mode 副本中停用原根对象并创建隔离测试对象，不保存场景；报告写入 `Temp/SalvageWork/PlayValidation.json`。验证结果详见 DEVELOPMENT.md。

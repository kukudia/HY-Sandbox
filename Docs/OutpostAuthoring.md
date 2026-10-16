# 建筑与敌人据点

据点是“敌机 + 适配机体的装饰底座”组合。敌机模块快照、相对位置与朝向和底座方块共同保存在建筑JSON中。世界中先显示底座上的敌机，接近后原位启用物理与战斗。普通建筑不包含敌机。

## 制作与保存

建筑编辑器快捷键 `Ctrl+Alt+F`。`Enemy blueprint` 选择敌机；`Create matching base for selected enemy` 根据模块轮廓和尺寸创建独立据点文件并加载完整组合。重复创建使用带序号的新文件，保留已有作品。

保存目录为 `Application.persistentDataPath/BuildingBlueprints`，当前用户对应 `C:\Users\12046\AppData\LocalLow\DefaultCompany\HY-Sandbox\BuildingBlueprints`。本轮已导出：

| 蓝图 | 对应敌机 | 底座范围 | 底座模块 / 敌机模块 |
| --- | --- | --- | --- |
| CODEX_E01_Finch Outpost.json | CODEX_E01_Finch | 28 × 26m | 113 / 95 |
| CODEX_E02_Pike Outpost.json | CODEX_E02_Pike | 28 × 30m | 133 / 120 |
| CODEX_E03_Manta Outpost.json | CODEX_E03_Manta | 36 × 22m | 125 / 116 |

底座按敌机投影拼接平台，中央通道连接两翼，外围矮护栏、供电服务角和资源箱组成停机设施。最低模块在平台顶面上方0.15m，留出碰撞间隙。建筑块均为真实可编辑块，可以继续添加、移动、拆除；薄网格底面仅作为编辑/支撑原点。

`Copy template` 可复制三套普通建筑及三套组合模板。添加驻军默认放到当前建筑上方；X/Height/Z/Yaw可继续调整。多敌机需要自行设计足够的平台和净空。移除最后一个驻军后成为普通建筑；驻军使用嵌入快照，后续修改原EnemyBlueprints文件不会悄悄改变已保存据点。世界加载优先使用用户同名有效版本。

预览同时显示底座与所有敌机的完整静态网格、原材质、轮廓框和朝向箭头。模型复制只含Transform/MeshFilter/MeshRenderer，不运行AI、炮塔、物理或电力，也不被建筑保存误计为建筑块。预览与建筑制作世界坐标对齐，忽略父Prefab偏移。模块级AABB检查底座净空，其他敌机采用总体范围；橙色警告是保守近似，保存不受警告阻止。

## 世界行为

EnemySpawner自动刷新继续停用；普通建筑与据点共同使用10%建筑候选概率。已加载且就绪的据点在激活范围外也显示停机敌机，接近后按同一姿态切换成真实物理敌机，每帧最多激活一台、同时最多16台。显示层不带伤害碰撞，远处尚未激活的模型暂不支持远射命中；这是当前实体预算策略的限制。

存活敌机远于180m或地形卸载时保存剩余模块、耐久和货物，之后在最后位置160m内恢复；已加载位置可显示休眠外形。首次激活必须有就绪底座，直接采用制作姿态，不人为抬离底座；恢复飞行快照仍进行地形净空修正。激活、卸载、击败与退出会话会清理显示模型。

击败记录只在本次出征存在，区块重访不复活；新出征可重新生成，不保存永久击败状态。HUD继续显示剩余驻军、炮台和清剿状态。每套单机底座含100金币和20科技，来自真实可破坏货仓，没有额外清剿奖励。

## 可编辑源与规格

- Unity6000.3.11f1 / URP / Windows；米、Y-up，建筑网格左下角(-2,0,-2)。敌机沿用既有蓝图及材质，不修改敌机源文件。
- 生成入口 `OutpostBaseDesigner.Create`；内容制作/导出 `OutpostContentAuthoring`。菜单重建六套内置模板会覆盖项目模板；导出只补用户目录缺失的三份据点文件。
- 项目蓝图位于 `Assets/Resources/WorldBlueprints/Buildings`，敌机源位于 `Blueprints/CODEX_Enemies`。底座复用真尺寸 `Structure_4x1x2` Prefab和现有方块，不依赖运行时缩放模拟大结构。
- 旧Road Watch/Relay Station/Supply Stronghold外围部署模板由匹配底座替换，通过AssetDatabase移动保留GUID。历史验收文档保留历史事实；用户既有建筑不自动迁移或覆盖。
- 沿用现有素材，没有引入新第三方或AI素材。未新增贴图、骨骼、音效或LOD；观察距离假设20–120m，模块数是内容预算，不代表帧率保证。

## 验证

`BuildingEditorPlayProbe.Run`检查真实按钮、保存/重载和模式清理；`OutpostContentChecks.Run`检查六套建筑连接与三套敌机净空并渲染预览；`OutpostResidentChecks.Run`检查真实地形上的显示/激活姿态、死亡重访、卸载和新会话。制作快捷键仅在Editor/Development Player开放。见[本次验证记录](Validation/2026-10-16-matching-outpost-bases.md)。

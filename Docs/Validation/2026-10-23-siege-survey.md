# 2026-10-23 大型基地Player规模分类与慢帧观察

## 改动和边界

新增 `SiegePlayerSurvey`，只由显式 `-siege-checks-report` 的自然模式调用。原 `census` JSON字段保留，每秒额外记录碰撞体、刚体根组件分类及kinematic/awake/sleeping分区、普查CPU开销。闲置池对象排除；建筑残骸没有可靠根标记，归other。类别是当前根的结构，不代表完整原始舰船数量。

显式 `-siege-checks-profile` 在观察期间开启既有 `SimulationWorkMetrics`，用累计差而不重置全局计数，结束或销毁恢复开关。每帧读取/相减使用值类型；最多保留32条超过33.33ms的慢帧，不在每次慢帧全场查询。start-to-start帧delta与前一LateUpdate-to-LateUpdate计时窗口仅近似关联，不能当作严格嵌套调用轨迹或PhysX/GPU分析器。第一观察不进入慢帧统计，原帧分位数仍包含它。

## 实际验证

- Unity真实Siege Play 14项通过：真实基地/舰船、无驾驶舱分量、弹丸、无标记物体、inactive/null过滤、物理状态分区、计时开关启停恢复及幂等释放。排序、阈值及32条上限使用合成delta，不能当作性能数据。
- 同版独立2048基地六舰Player smoke 75项通过，errors为空。该窗口Hidden，只验证玩法闭环，不作可见画面证据。
- Unity导入新脚本及meta；Windows D3D11构建0错误/2警告。解决方案静态构建0错误/4既有警告。工具Python检查9项通过；可读性38图/299脚本，仅既有MainUIPanels格式差异；diff检查通过。

## 可见自然Player

同版非Development D3D11，1280×720，初始2048建筑模块与六舰，profile显式开启。进程5912于17:53:09.101启动、17:53:09.982恢复其UnityWndClass窗口；未操作NightDesk。部署和终态原生PNG均已查看、正常渲染。

`-siege-checks-report <absolute>/survey-profile-natural.json -siege-checks-ships 6 -siege-checks-soak -siege-checks-seconds 180 -siege-checks-capture -siege-checks-profile`

约182.07秒、8993帧、72检查通过，完成2波，第3波中；终态建筑1947/2048，托管errors为空，但后续补查原生日志发现5条D3D11呈现时序诊断；不能称为完全无错的性能基准，见[native记录](2026-10-23-siege-survey-native-graphics.txt)。中位19.31/P95 30.43/P99 41.76ms，最大182.92ms；超过33.33/50/100/250ms分别247/51/9/0帧。战况及诊断与旧窗口不同，不能作为帧率退化或优化的对照。

178次普查峰3418模块、224刚体、284006398 bytes Unity allocated；刚体峰所在同一快照是1建筑+9有效驾驶舱根+204无驾驶舱分量+9弹丸+1其他，212醒动态、11睡眠动态、1运动学。弹丸独立峰38，不能与其他独立峰相加。所有快照的根类别、状态分区及body总数相等，慢帧引用索引有效。

普查自身中位4.51ms、最大7.78ms、累计808.63ms，约占总墙钟0.44%；它仍能影响采样后的单帧，不能声称无扰动。最慢32帧中4帧前一观察执行了普查。

| 嵌套阶段 | 调用数 | 累计CPU ms |
| --- | ---: | ---: |
| 舰船连通遍历 | 234 | 63.30 |
| 舰船刚体重建 | 234 | 309.53 |
| 舰船刷新 | 235 | 422.29 |
| 建筑支撑 | 140 | 1000.39 |
| 建筑邻接 | 140 | 451.90 |
| 建筑底座 | 140 | 293.88 |
| 建筑遍历 | 143 | 242.39 |
| 建筑残骸 | 3 | 1.58 |

**嵌套阶段不能相加**。BuildingDamage为0，只说明这批路径未进入对应范围伤害计时，不代表建筑没有受损。邻接物理查询144287，legacy全场引用扫描0。

最大182.92ms帧的前一工作窗口记录舰船刷新4.94ms、连通0.20ms、建筑支撑0；159.24ms帧对应刷新5.79ms；142.39ms帧这些计时均0。最慢32条中单窗口支撑最高38.62ms、舰船刷新6.22ms、连通1.37ms。这不足以归因慢帧；现阶段没有依据把纯BFS当作主要帧尖峰来源。下一步观察引擎物理/渲染/VFX等未覆盖阶段，再选择生产优化。

## 证据

- [Play检查](2026-10-23-siege-survey-play.json)、[Player闭环](2026-10-23-siege-survey-smoke.json)
- [完整自然报告](2026-10-23-siege-survey-natural.json)
- [部署截图](2026-10-23-siege-survey-deployment.png)、[终态截图](2026-10-23-siege-survey-final.png)

仍未验收万块实际交火、长期内存、完整人工驾驶和所有长收尾情况；不迁移整个项目至ECS或给每块增加刚体/FixedJoint。

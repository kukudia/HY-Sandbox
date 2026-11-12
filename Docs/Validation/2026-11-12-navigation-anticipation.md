# EnemyAvoidanceLab 少拐点与有界预判

入口：`Assets/Scenes/EnemyAvoidanceLab.unity`；Unity 6000.6.8f1，连接本项目 Editor 实际 Play 验证。生产入口为 `EnemyController.ApplyObstacleAvoidance` → `EnemyLocalPath.TryFollow`；源码关联图见 [RuntimeFlow](../RuntimeFlow.md)。没有修改场地或用户的 Random5v5Arena 改动。

## 规则与预算

- 六方向栅格保留，候选队列改为复用 List 的二叉最小堆；更优代价可重复入队，弹出时丢弃过期记录。默认扩展1024节点、半径96米、重试2秒，硬限制64–4096节点；不是无限搜索或逐帧重规划。
- `Path Turn Penalty` 默认2米，范围0–6米，按相邻方向变化附加代价。节点仍按位置合并，属于减少折返的启发式偏好，不保证包含航向状态的全局最优或最少转弯路线。
- 成功后先保留窗口出口航层，再原地压缩航点；最多128次额外 ClearFlight 测试，预算耗尽保留相邻原段。ClearFlight 同时验证垂直升降和目标高度水平段，继续使用旋转保守整舰包络，不能将视线直通替代舰体净空。
- 当前段安全时额外检查后续两段。发现受阻且冷却到期，以 `FutureBlocked` 请求替换；失败保留当前安全段，下次决策仍重新验证当前段。当前段受阻立即失效，不能以预判失败为由继续使用它。
- `Path Prediction Time` 默认1.5秒、上限3秒；运动学障碍按当前线速度合并当前位置和预测位置，平移限12米；角速度乘包围半径估计旋转余量，另限12米。捕获范围因此增加24米。0关闭运动提前量。自由动力学载具仍由即时避障处理，没有把所有敌舰当静态墙。
- 预测只是短期保守包络，没有周期/加速度/到达时刻模型；可能扩大封路区域，也无法预知闸门反向。原有真实速度扫掠、升降走廊限速、下降保高和实体物理全部保留。

## 实船结果

使用已有 `EnemyLabEfficiencyChecks.Begin(label)`，固定 Finch、布局顺序、0.02秒物理步长、3倍时间速度。到达沿用22米水平半径并等待2秒，不代表精确停靠。表内时间是模拟秒；转向为控制器接受航向变更的累计次数，包含正常接敌与避障，不等同规划折点数。

| 路线 | 改前秒 / 转向 | 最后完整轮秒 / 转向 | 改前 / 后墙体接触 | 成功规划航点数改前 / 后 |
| --- | ---: | ---: | ---: | --- |
| Single wall | 21.06 / 2 | 20.49 / 2 | 0 / 0 | 16 / 1 |
| Roofed U trap | 21.28 / 3 | 20.97 / 3 | 0 / 0 | 18 / 3 |
| Tall wall bypass | 32.21 / 12 | 32.29 / 11 | 0 / 0 | 18 / 3 |
| Underpass | 23.12 / 0 | 23.10 / 0 | 0 / 0 | 无成功A*路线 / 无成功A*路线 |
| 城区 West → East | 150.01超时 / 79 | 69.12 / 21 | 15 / 5 | 14–22 / 2–3 |
| 城区 Northwest → Northeast | 40.90 / 5 | 40.73 / 4 | 0 / 0 | 无成功A*路线 / 无成功A*路线 |

对应报告：`Temp/EnemyAvoidanceLab/efficiency-anticipation-baseline.json`、`efficiency-anticipation-v1.json`、`efficiency-anticipation-final.json`、`efficiency-anticipation-clean.json`。每份保存逐轮持久化记录目录及 result；原始 timeline、蓝图和几何保存在 `Application.persistentDataPath/NavigationLabRecords/`，Temp不提交。最后完整轮为 `anticipation-clean`，6/6到达；单墙仍有初始地面接触，不能称为全程零接触。

多轮结果不完全一致：高墙实际转向10–13次，城区东西向约69–76秒且4–8次墙体接触；`anticipation-final` 北线曾150秒超时并接触高架平台4次，不能只引用最后成功轮声称稳定解决。北线失败轨迹在进入平台前一直为 Direct、无成功A*路线，后续为 NoRoute；不能据此认定最小堆或运动包络是唯一原因。`anticipation-repeat` 首轮0.87秒被 stopped，中途发生脚本导入/重载的尝试不计完整验收轮。

规划点压缩是稳定结果，实际驾驶转向不保证全面减少。综合城区运动闸门仍可能将船推入障碍膨胀区，导致 NoRoute 与碰撞；本轮没有实现动态时空规划。后续应单独验证闸门相位、等待/让行与初始重叠脱离策略。

## 性能口径

使用既有10Hz导航快照中 routeChanged 的 routeRevision 去重，统计 `planMilliseconds`；它包含规划时 Capture、节点扩展和简化，不包含每次缓存跟随的总决策开销，也不是每帧CPU/GC或多舰压力测试。采样可能漏掉两个采样之间的瞬时规划，以下仅为观察值。

| 路线 | 改前平均 / 最大ms | 最后完整轮平均 / 最大ms |
| --- | ---: | ---: |
| 高墙 | 4.085 / 15.895 | 0.662 / 2.059 |
| 下穿 | 17.542 / 17.813 | 2.157 / 2.195 |
| 城区东西 | 1.668 / 17.196 | 2.629 / 5.720 |

城区平均值上升而最大值下降，不能声称每项性能都改善。观察到的最大扩展均不超过1024；未测独立Player、多人/多舰帧预算和GC分配。

## 验证

- 73项 NavigationCombatPlayChecks 实际 Play 通过，包含简化段整舰净空、出口保高、封闭地图拒绝、缓存失效、运动学提前占道/12米上限、后续受阻预判及重规划冷却。
- Unity实际脚本导入与Play成功；CLI首次读取回归结果曾超过5秒超时，随后读取 Pending 为 RanToCompletion，报告73项PASS。脚本热重载后用全新Play会话重跑，避免将空静态Pending当完成。
- `dotnet run --project Tools/CodeReadability -- --check .`：435脚本、0差异、0错误；导航与受影响函数图已刷新。
- `dotnet build HY-Sandbox.sln --no-restore --nologo`：0错误、37条既有警告。
- 本任务文件 `git diff --check` 通过；全工作区检查另有用户已有/Editor生成的布局尾随空白，保留未清理。
- 未运行独立Player、全蓝图、全部12布局及所有起终点排列，也没有证明动态闸门无碰撞。

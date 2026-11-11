# EnemyController 局部三维 A* 与墙边停滞修复

## 实现

入口为 EnemyController.FixedUpdate → CalculateDesiredMovement / GetApproachDirection → ApplyObstacleAvoidance。高度控制仍由 HoverFlightController、水平施力仍由推进器负责，未使用 NavMesh，也未新增运行时自挂组件。

- 保留当前实际速度方向的舰体扫掠和随速度增长的预测距离；BoxCast 本身包含舰头，不再平移整个扫描盒造成贴墙漏检。
- 高度未到位时收掉水平推力；最终输入缩减沿速度的正向分量并剔除反向分量。依赖真实阻尼减速，不瞬移或写速度。
- EnemyObstacleClimb 将垂直速度与水平越障进度分开，低速和爬升中的速度倾角变化不取消仍有效的高度任务，舰尾越障后释放。高度求解器模块与舰体根节点的高度差得到补偿。
- EnemyLocalPath 在上下通道不可用时执行有界三维栅格 A*，默认半径96m、单元6m、最多1024次扩展、重试间隔2s。按整舰水平旋转包络及0.75m余量扩张静态/运动学几何。触发器、其他动态载具和自身不进入静态规划。
- 搜索和跳点验证整段通道；跟随时先调整高度再沿水平段推进。每次决策复查缓存，目标明显移动或新几何阻塞时失效；中间点不生成到点停车意图。橙色 Gizmos 显示路线。
- 保留旧序列化字段和 GUID。工作区原有收油调整已整合；原前移扫描盒的实现改为从实际舰体包围盒中心扫掠，保留舰头前方预判的目标。

## 可编辑测试入口

Assets/Scenes/EnemyAvoidanceLab.unity 中的 EnemyAvoidanceLab 提供五个布局与 Run all。新增 Roofed U trap 带真实顶棚，不能沿旧的直线爬升通道翻越，需绕到侧面。通过 Editor API 保存，现有布局保持不变。菜单 Tools/HY-Sandbox/Combat/Add Roofed A Star Trial 可幂等补充该布局，新建试验场也自动包含。

## 实际 Play Mode

Unity6000.6.8f1，Finch真实舰体、供电、惯性及推进器；固定目标、无武器、碰撞伤害禁用但接触仍计数。TimeScale=1，FixedDeltaTime=0.02。五项完整实船复测均到达。

| 布局 | 用时 | 接触 | 航向调整 |
| --- | --- | --- | --- |
| Single wall | 32.9s | 0 | 1 |
| U trap | 31.7s | 0 | 1 |
| Slalom | 29.2s | 0 | 1 |
| Narrow gate | 28.8s | 0 | 0 |
| Roofed U trap | 47.7s | 0 | 9 |

到达判据为进入22m水平接敌半径后观察2秒，并非精确悬停或航点间零速度。Roofed U trap 确认生产路径处于Active状态，观测到扩展371节点；两次前期独立复测亦零接触到达（49.2s及49.0s）。原四布局最终复测仍零接触。本轮结果解决了此前墙边停滞的已复现场景，不证明所有任意障碍均可达。

## 验证边界

55项隔离 Play 回归全部通过，覆盖上下净空、高度保持、低速/纯垂直速度、连续航向、不反推、A*侧绕、搜索预算、缓存复用、新障碍失效、封闭区域失败及重试限频、炮塔遮挡。Unity重新导入/编译通过，保存后重载确认五个布局与顶棚引用持久化且场景无脏状态。dotnet build HY-Sandbox.sln --no-restore：0错误、37警告（36项既有警告和1项保留旧序列化字段climbForwardSpeedRatio的未使用警告）；CodeReadability检查418脚本无差异/错误；任务文件git diff --check通过。

未进行真实多人战斗、移动目标全套追逐、极限高速贴墙及大规模舰队性能验收；局部搜索有半径与节点上限，AABB膨胀较保守，可能拒绝本可斜穿的狭窄通道。完全封闭或超出局部搜索范围的地图不保证有路线，失败时回退即时身位避障，限频重试。障碍规划忽略动态载具，实时扫掠仍检查实际航迹；不能承诺避开所有高速来袭物体。

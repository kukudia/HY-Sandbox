# 自主舰船直航稳定与飞控复用

## 行为与边界

普通接近阶段从旧的12度死区/1.5秒方向冷却切换为连续指数响应：小幅目标偏移被柔和吸收，超过柔和角度时加快修正，避免积累角误差再突然切线。`directionHoldTime`、`directionDeadZone` 的字段名和已有序列化值保留，Tooltip说明新语义。原`nextDirectionChange`计时状态删除。

普通修正在水平面按最短偏航角平滑；参考航速通过 `FlightControlMath.StepCourse` 独立限制角速度和加速度，折返不因线性向量插值穿过零速。近距离接敌横移、明确路线转角、紧急避障及时接管；路线上的细小偏移同样柔和跟随。真实制动和每物理步净空约束保留。

`Flight/FlightControlMath.ResponseBlend` 同时由 `EnemyController` 和 `HoverControlSolver.StepHeading` 使用。响应数学共用，导航与悬浮各自持有状态，`HoverFlightController`继续执行机体姿态稳定；没有新增临时MonoBehaviour、场景参数或刚体写入。保留原推进输出标定，未调整用户已修改的悬浮控制Prefab。

开发中发现通用推进器只按输入是否非零输出满推力；曾尝试按幅度输出及速度补偿，但真实到达回归出现欠速和近距绕圈。该试验未纳入最终代码。本次仅交付航向稳定与计算复用，未把降低航速称为导航改善。

## 验证证据

Unity 6000.6.8f1连接本工程Editor。基线及最终两船直线均使用EnemyAvoidanceLab的Open flight、内置蓝图0/2、正常时间倍率，开启生产AI和真实推进器，独立部署后运行至试验场正式到达判定。

| 舰船 | 基线到达 | 最终到达 | 最终航程 | 最终接触 |
| --- | --- | --- | --- | --- |
| CODEX_E01_Finch | 23.57秒 | 23.74秒 | 120.54米 | 0 |
| CODEX_E03_Manta | 27.2秒 | 27.61秒 | 118.51米 | 0 |

这是单次路线样本，不是统计显著的性能提升。最终到达时间与基线接近；两船初段直航的生产意图维持同方向。理想固定目标本来没有频繁换向，因此不能以此宣称直线抖动减少百分比。

`FlightCourseChecks.Validate` 对真实EnemyController施加小幅偏移：8度目标修正在首个采样连续产生0.1～7.5度响应；20次正负8度交替指令的累计实际修正小于65度（原始指令累计312度）；紧急指令立即接管；180度折返时参考速度仍为6m/s、0.02秒步长转角不超过1.81度。检查保存并恢复运行状态，不保存到场景。

`HoverControlPlayChecks.Run` 原16项通过：有效归属、无驾驶舱、停供、禁用、移交清理、水平抑制偏航及跨北向最短角修正。覆盖共用响应计算的另一真实消费者。

最终生产代码的Manta单墙路线实际到达：21.1秒、19.3米终距、0接触。Random5v5Arena真实开火与正式换轮的18项回归通过，五敌舰标签/阵营、轨迹、切换、文本、独立清空、结算重开及对象数量均正确。最终独立会话无新增Console错误；Editor导入编译完成。CodeReadability检查451脚本、0差异/错误；`dotnet build HY-Sandbox.sln --no-restore`为0错误，本次增量输出36条既有API弃用警告。暂存内容的`git diff --cached --check`通过；全工作区检查仍有既有UserSettings布局尾随空格，未改动这些配置来消除告警。

本地证据：`Temp/FlightCourse/component-final.json`、`hover-regression.json`、`final-verified.txt`、构建和格式日志。原始轨迹/输入记录位于返回的 `NavigationLabRecords` 会话目录：基线 `20261115-140014-011-0c80a8/trial-0003` 与 `20261115-140410-011-d76913/trial-0003`；最终 `20261115-142303-477-051836/trial-0003` 和 `trial-0005`。

本次不宣称解决所有动态避障换向、密集多舰拥堵、受损推进器或全蓝图池的稳定性。Player构建、长期多轮压力及完整性能基准未运行。

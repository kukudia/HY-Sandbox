# 飞行与机器人计算轻量化

本轮检查真实HoverFlightController、HoverThruster/Thruster、Bot、BotFlightMotion及BotObstacleAvoidance。没有Profiler证据证明这些类是万模块压力场的首要热点，因此改动只消除明确重复工作，不迁移GPU、ECS或Jobs，不承诺FPS收益。

## 改动与不变量

- BotObstacleAvoidance在每次同步Evaluate/ConstrainVelocity内缓存Collider过滤判断，避免多条候选球形检测反复GetComponentInParent/IsChildOf。入口及Reset清空，避免跨物理帧保留货物、Bot和目标层级的旧身份。Dictionary首次扩容可能分配，不宣称绝对零GC。
- 保持绕障方向时复用其净空结果；选新方向时返回评分时已有的净空，减少一次重复SphereCast。候选方向、选择顺序和评分不变。
- BotFlightMotion本物理帧只计算一次目标距离，供方向、采样和返航制动使用；下一个物理帧仍读取实时目标，不缓存移动Home世界位置。方向阈值使用平方距离。
- HoverFlightController对ShouldActivate为false的推进器直接提交零目标，不再计算其姿态力臂；Thruster先检查原有可视化节流，再归一化方向。物理施力、供电、推力变化率、组件身份和Inspector契约不变。

## 实际验证

Unity 6000.3.11f1连接工程HY-Sandbox，原场景Siege，检查前未运行且无脏场景。BotNavigationPlayProbe完整29项通过：两类Bot及其子碰撞体过滤、脱离后恢复障碍、墙面、U形围挡、惯性制动、移动Home、初始重叠脱困。使用真实刚体更新。

HoverControlPlayChecks在真实Play会话中同步调用13项通过：无驾驶舱不输出、恢复有效归属后推进/角速度、停电与禁用按原变化率衰减、退出游玩清零、旧控制器不清除已转移推进器。该夹具主动调用物理回调，不能代表玩家完整飞行体验。结束恢复Edit Mode，未保存测试对象或修改作者场景。

首次导航运行遇到脚本重载后静态驱动状态丢失（deadline=0）；再次运行完成前四类27项时被本轮人工提前退出，报告明确记录interrupted，未当作通过。确认编译完成后完整重跑29项通过，保留中断证据。

dotnet build HY-Sandbox.sln --no-restore：0错误、4既有警告。Unity脚本编译无错误。导航已刷新40图/312脚本；代码检查仅剩既有MainUIPanels格式差异，本轮源码格式通过。git diff --check通过。没有发布Player、GPU/CPU归因、优化前后计时、GC对照或万模块FPS结果；后续需同场景/镜头/负载的Player Profiler对照，再决定Jobs/Burst与GPU迁移。

证据：[导航完整结果](2026-10-24-flight-bot-navigation.json)、[中断记录](2026-10-24-flight-bot-navigation-interrupted.json)、[悬浮检查](2026-10-24-flight-hover-checks.json)。

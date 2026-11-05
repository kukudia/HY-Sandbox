# 最新 Profiler 分析与战斗性能优化

## 结论与采样范围

已分析 `ProfilerCaptures/HY-Sandbox_2026-11-05_16-35-27.data`（2,666,602,368 字节，2,000 帧，Unity 6000.6.8f1 Editor）。主要可修复成本是炮塔索敌及伤害链中的连接视觉反复销毁/重建；编辑器 Gizmos 和 EditorLoop 也是显著开销，不能全部归为运行时代码。

原采样 CPU 帧间隔平均70.981ms、P50 26.151ms、P95 237.112ms、最大701.175ms。逐帧FPS算术平均32.585不等于总帧数/总时间（约14.09帧/秒）；大量尖峰使两种口径明显不同。没有有效GPU计时，不能判断GPU瓶颈。

| 捕获证据 | 含义 |
| --- | --- |
| Turret.FixedUpdate 共45,729.25ms，450,319次调用，平均每录制帧22.865ms | 最大游戏脚本热点；含索敌、开火及其同步伤害副作用 |
| FixedUpdate.PhysicsFixedUpdate 共4,479.463ms，平均每录制帧2.240ms | 含碰撞回调和伤害链，不能全部算纯PhysX求解 |
| 帧1222：炮塔117.558ms，其中self116.272ms | 正常Profiler无法区分未打标记的C#子方法；代码检查和隔离测试定位重复层级扫描 |
| 帧1374：炮塔136.493ms，其中Instantiate79.627ms、1,031次；稍后Destroy34.160ms、1,035次 | 连接刷新重复创建接缝装饰，与新工业视觉Awake及渲染器注册叠加 |
| 帧1417：炮塔280.543ms，其中Instantiate60.905ms、819次，Destroy25.736ms、845次 | 成批视觉重建放大卡顿，低帧率又触发5次物理补帧 |
| 帧1569：物理事件派发192.2ms，内部Instantiate114.529ms、1,495次 | 碰撞后的损伤副作用，同样不是单纯求解器慢 |
| GizmoManager.DrawGizmos共14,071.16ms，约7.04ms/帧 | 与DrawGizmos父标记重叠，不能把所有层级耗时相加 |
| 帧1998 EditorLoop self656.501ms；帧1999 self488.620ms | 最大两帧主要是编辑器活动，不能靠改AI解释或消除 |

URP主相机CPU提交平均约11.24ms，IndustrialPartMotion.Update共4250ms（约2.13ms/帧）；它们是后续候选，不在本轮未经视觉验证就改渲染质量或机械动画。原分析器按名称搜索GC会误把`NonAlloc`、`CollectNeighbors`等包含词算入相关列表；本轮不把该列表当GC占比。GC.Collect仅14次共76.674ms，不能解释主要尖峰。

## 实现

- `BlockConnections.CheckConnection` 每次仍执行真实几何探测及双侧可连接性校验。匹配对象、双侧运行引用和启用状态都未改变时保留原接缝对象；连接改变/断开才清理。删除不再使用的旧无视觉释放分支。没有用关掉连接装饰、延迟伤害或吞掉残骸换性能。
- `ControlUnit.GetDamageTargets` 让多炮塔共享当前渲染帧/物理步的模块层级列表；`Durability`启用、禁用、直接换父级更新版本，单位子对象变更和`RefreshChildren`使本地缓存失效。模块位置、激活、生命和实际归属仍实时验证；同帧多物理步不会沿用上一物理步缓存。
- `TurretTargetSelector` 缓存单次查询的炮塔位置，传递当前最近距离，仅对可能更近的候选检查父级归属。列表按索引读取，无接口foreach装箱。驾驶舱优先规则、范围边界和目标过滤保持原契约。
- 新增回归覆盖连接视觉身份复用、同一步模块激活、移动、祖先重组；既有修复新增/模块直接重组检查继续通过。
- 未修改任何Prefab、序列化字段、GUID、伤害、射程、物理频率或画质。用户两个炮塔Prefab的`predictiveAim: 1`未纳入提交。恢复点为本地`codex/profiler-base-20261105`（daffcee）。

## 实测

CPU为Intel Core Ultra 5 225H。原始批次和自然战斗规模汇总保存在同目录 `2026-11-05-profiler-performance.json`。

| 相同隔离夹具（8次，预热不计） | 优化前中位 | 优化后中位 | 降低 |
| --- | ---: | ---: | ---: |
| 128个真实1x1x1链式模块完整连接重查 | 25.513ms | 1.103ms | 95.7% |
| 40座炮塔搜索12组×96伤害模块，每模块8个视觉子节点 | 21.186ms | 3.860ms | 81.8% |

连接测试优化前每批127个现有连接全部更换，优化后127个全部保留；已检查每个视觉仍恰有两个连接引用。测试同步执行、不推进物理帧；第二项为同物理步多武器复用的热场景，不代表每步首次缓存构建无成本。不是ABBA随机交替实验，可能有JIT/温度/编辑器负载影响。两项均调用真实生产实现，未给生产加入测试开关。分配计数在分配明显的基线也返回0，因此不将它当零GC证明。

自然战斗使用新鲜`Siege.unity`，分别60秒、`Random.InitState(4721)`、配置种子14741、部署两个最便宜的防守舰、第一波、相同镜头/画质。通过`SiegePerformanceCapture.StartCapture`记录：

| Editor观察指标 | 优化前 | 优化后 |
| --- | ---: | ---: |
| 观察帧数 / 持续秒数 | 3277 / 60.002 | 3537 / 60.010 |
| 观察吞吐帧率 | 54.62 FPS | 58.94 FPS（+7.9%） |
| 帧间隔P50 | 15.827ms | 14.357ms |
| 帧间隔P95 | 45.557ms | 42.922ms |
| 帧间隔P99 | 55.929ms | 54.075ms |

这是自然运行观察，不是确定性重放：伤害/残骸/敌机数量随物理轨迹不同，不能把7.9%认定为严格因果提升；原2.67GB捕获的战斗规模也不同，不能拿原文件FPS直接与该复测相除。Editor调度、Profiler、渲染和每秒census分配都计入，未验证独立Player性能/GPU。

原始本地文件：

- `Temp/profiler_analysis_report.txt`、`Temp/hot-hierarchy.txt`：原捕获层级分析。
- `Temp/connector-benchmark-before.csv` / `after.csv`；`Temp/search-benchmark-before.csv` / `after.csv`：隔离批次。
- `Temp/Siege/Performance/20261105-084710-889-profiler-before.json`；`20261105-085619-016-profiler-after.json`：60秒自然战斗逐帧数据。
- 加载原捕获前的Editor现场采样已保存`Temp/profiler_before_analysis.data`；源捕获未修改。

## 验证与复测入口

- 连接Editor实际导入/编译；`SiegeRuntimeConnectionChecks.Run()`：39项通过；`AiUpgradePlayChecks.Run()`：21项通过；`TurretRuntimeChecks.RunInCurrentPlayMode()`：32项通过。
- 炮塔检查第一次在Siege部署阶段运行，3项伤害断言失败，原因是生产规则在非Battle阶段拒绝伤害；在真实Battle阶段重跑32项全部通过，没有以第一次失败算通过。
- 新鲜Siege Play还执行`SiegePlayChecks.Run()`：28项通过、failures为空；该检查会修改临时战局，需在性能录制结束后运行。
- `dotnet run --project Tools/CodeReadability -- --check .`；`dotnet build HY-Sandbox.sln --no-restore`；`git diff --check`。345脚本检查0格式差异/0错误，构建0错误、109条既有弃用/分析器警告，diff检查通过。
- 复测自然战斗：打开Siege、进入Play、固定随机种子、部署相同舰船；`SiegePerformanceCapture.StartCapture(label,60f)`后调用`SiegeSession.BeginNextWave()`。比较完整报告及census，不仅比较FPS。
- 未完成：同一存档/规模的Development Player重放、GPU采样、长时高波次压力。Gizmos是Editor负担，可在实际使用时关闭Game/Scene视图Gizmos对照；本轮没有悄悄更改用户编辑器显示开关。

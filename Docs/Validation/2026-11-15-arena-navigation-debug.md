# Random5v5Arena 实战导航追踪与感知误饱和修复

## 交付与使用

- 场景保存 `Arena Navigation Debug`，五组红方 `NavigationTrack` 和各自 Flight Trace / Planned Route；不是 Play 时临时补挂组件。
- 右侧 RED-1～5 选舰，Focus ship 平移自由相机，Clear selected trace 独立清空历史；可显示全部轨迹和所选未来路线。
- Inspector 配置面板、场景标签、采样间隔、每舰4096点上限、0.4米最小间距及8m/s固定速度标尺。
- 历史线按速度从深蓝至亮青；规划线橙色，只显示剩余航点。EnemyAvoidanceLab 复用同一 `NavigationFlightTrace`，保留原序列化引用与12000点容量。
- 面板读10Hz缓存，显示战术目标、真实距离、接敌/索敌阈值、航程/水平停滞、接触、输入、制动、A*状态/节点/耗时、预测威胁/TTC和两类饱和。死亡保留最后快照，换轮清空并复用五组对象。
- 制作入口：`Tools/HY-Sandbox/Combat/Install Arena Navigation Debug`。竞技场生成器也调用相同安装入口；不重新生成现有地图。
- 源码分类：共享轨迹/文字在 `Navigation/Diagnostics/Tracking`；竞技场协调器在 `Combat/Skirmish/Diagnostics`；制作和验证在 `Assets/Editor/Combat/Diagnostics`。

## 实战故障与修复

修复前实战及用户截图：目标仍距314米，水平停滞20.6秒，`SensorSaturated`、紧急制动、参考速度0、A*仅展开1节点。远场原始OverlapBox固定256项，自身和多舰模块在过滤之前填满缓存；饱和状态又给规划注入覆盖整个传感区的阻塞盒，造成持续停车。

远场现在按需扩容并复用缓存，8192项硬上限；同一刚体的模块合并为一个运动Bounds，静态建筑逐碰撞体保留通道。既减少逐候选预测开销，也消除把模块数量当成真实危险的误停；达到硬上限仍明确报告不完整查询。未更改18米接敌距离或800米竞技场索敌距离，未直接写舰船速度。

实际修复后样本：Type12在272个原始碰撞体、512容量下，参考速度6m/s、实际4.2m/s，Brake=False。另一轮五艘红舰早期距离365～388米、实际3.7～5.0m/s；42.7秒时距离分别86.0、179.1、113.9、74.8、39.2米，均无感知饱和。最终独立会话Pike在45.2秒时已距目标22.8米、航程197.4米，随后实战被击毁；不是停在远距边界。

同刚体合并采用保守包围盒，不能表达舰体内部孔洞；不合并静态建筑。后续密集残骸、凹形舰船和最大舰队压力仍需持续验证，不把此次样本视为所有导航拥堵已解决。实战仍能观察到 A* NoRoute / BudgetExceeded、绕障与短暂停顿。

## 实际验证

Unity 6000.6.8f1，连接本工程Editor：

| 检查 | 结果 |
| --- | --- |
| 场景保存、重载与重复安装，5组引用及速度配置；环形容量/顺序、纹理独立、清空与释放 | 15项通过 |
| 隔离PhysicsScene：600个同刚体运动模块+2面静态墙，重复采集 | 602项完整捕获、3个观测、1个运动体、不饱和；两次一致 |
| Random5v5真实开火、5槽阵营/标签、轨迹、切换、文本、独立清空、正式淘汰/结算/重开、无对象增长 | 18项通过，修复后再次运行通过 |
| EnemyAvoidanceLab真实Play单墙路线 | ARRIVED，20.3秒，153个轨迹点及实例速度纹理，0墙接触 |
| 最终独立Play会话Console | 无新增错误；groundTruth consoleErrors=0 |
| CodeReadability | 449脚本，0格式差异，0错误 |
| dotnet build HY-Sandbox.sln --no-restore | 0错误，37条既有API弃用警告 |
| git diff --cached --check | 本任务暂存内容通过；全工作区检查仍报告既有UserSettings布局尾随空格，未整理用户配置 |

开发中尝试在Edit Mode创建隔离PhysicsScene被Unity拒绝，检查已改为Play Mode执行。开发期在Play中格式化导致脚本热重载，旧运行对象丢失非序列化状态并出现OnDisable异常；停止后重新进入独立Play复验，未再出现。本次不宣称支持战斗中脚本热重载。

实际截图检查面板字号/布局、速度轨迹、击杀栏及死亡快照。Game视图关闭Gizmos可避免已有AI范围圆/连线越过IMGUI文字；真实LineRenderer轨迹仍显示。源码场景标签仅在Scene视图绘制。证据保留于本地 `Temp/ArenaNavigation`：`sensor-checks.json`、`saved-final.json`、`uninterrupted-play.json`、`lab-trace-check.json`、`battle-early.json`、`battle-later.json`、`final-session-report.json`、`final-readable.png`、构建/格式日志。

未运行Player构建、长时间多轮压力、完整蓝图池或性能基准。既有场景对象文档与任务前备份逐项比较，除新增调试根的SceneRoots引用外无变化；提交仅包含48个新对象文档及一个根引用，保留用户原有地图编辑。

# 导航试验场：场地、蓝图部署与过程记录

## 使用

打开 Assets/Scenes/EnemyAvoidanceLab.unity 并进入 Play。左侧面板选择布局、点击蓝图条目，再点“部署所选蓝图”。目录只读聚合内置 Finch / Pike / Manta、Application.persistentDataPath 下的 Saves 与 EnemyBlueprints；不写、不重命名、不覆盖用户蓝图。缺少模块、没有单一驾驶舱或无效蓝图由正式目录/部署校验拒绝。未强制蓝图必须具备飞行能力，缺少推进器的设计可能无法完成测试。

成功部署后才退役旧测试舰，场中保留测试舰和固定目标；复用 ModularUnitFactory、真实供电、推进器和惯性。武器不启动、碰撞伤害关闭，但真实接触照常记录。布局与源舰参数由真实场景保存，新增资产均有meta。

- 点“点选起点/目标”后，在场地左键选择位置；高度单独使用面板加减按钮。起点变更重试，运行中的目标变更保留当前位置继续导航。结束后设目标会开始新轮。
- R重试、Space暂停/继续、Esc取消点选；中键拖动镜头，场地区域滚轮缩放，相机复位恢复总览。
- 青色线是实际轨迹，橙色线是规划路线；LineRenderer不依赖Game Gizmos。顶棚默认只显示轮廓，面板可恢复顶棚表面，碰撞体始终保留。
- “结束并保存记录”关闭本轮文件并停用测试舰AI；“全部测试”按布局顺序运行，每轮单独保存。退出Play会结束未完成测试、关闭文件并恢复原TimeScale/后台运行设置。
- Inspector可在运行前关闭recordTrials或调整默认0.1秒采样周期。记录功能仅作用于该试验场，不在其他场景采集键鼠。

## 十一种可编辑布局

| 布局 | 用途 |
| --- | --- |
| Single wall / U trap / Slalom / Narrow gate | 原有基础越障、凹形、交错和窄门 |
| Roofed U trap | 封住直线抬升通道，观察A*侧绕 |
| Open flight | 无障碍对照 |
| Tall wall bypass | 超过爬升高度的宽高墙，检查局部规划边界 |
| Underpass | 顶棚限制下穿与高度恢复 |
| Pillar field | 多柱密集查询和航向切换 |
| Switchback corridor | 连续水平折返 |
| Sealed chamber | 封闭失败测试，预期超时，不要求到达 |

新增布局保存EnemyAvoidanceCourse推荐起点、目标、时限和说明；原有五布局保留。地面扩为300×300米。制作菜单 Tools/HY-Sandbox/Combat/Upgrade Navigation Test Field 幂等添加缺失布局/轨迹组件，不重建已存在的障碍对象，用户可以继续在场景中调整。

## 文件结构与字段

面板“打开记录目录”打开 Application.persistentDataPath/NavigationLabRecords。每次Play创建独立UTC时间和随机后缀会话，各轮使用trial-0001等子目录：

- operations.jsonl：整个会话的键按下/松开、鼠标按下/松开、滚轮、镜头操作、选蓝图、部署、切布局、起点/目标修改、暂停、重试、接触与结束事件；保留两轮之间的操作。包含frame、会话真实时间、轮次及本轮模拟时间。
- trial-xxxx/metadata.json：schema、Unity版本、蓝图名称、起点/目标、AI序列化参数、采样周期、TimeScale、FixedDeltaTime、屏幕/镜头配置及静态场地几何。
- trial-xxxx/blueprint.json：本轮部署源JSON快照；不写回蓝图目录。
- trial-xxxx/timeline.jsonl：操作及默认10Hz导航采样。包含位置/姿态、线速度/角速度、期望输入、参考速度、最终MovementInput、航向、高度指令、舰体Bounds、扫掠距离、推进限制比例、紧急/高度/路线状态、转向计数、A*耗时、扩展数、重规划原因、航点索引与路线版本。
- trial-xxxx/result.json：到达/超时/停止/替换/中断原因、时长、距离、行程、转向、接触统计。contacts为所有接触，obstacleContacts / groundContacts / targetContacts分别统计墙体、地面和目标；与旧版仅统计墙体的contacts口径不同。

routeChanged=true时route为完整路线快照；false时应沿用上一快照，不把空数组理解为清空。跟随过程中waypoint持续更新，不必重复整条路线。pathStatus描述上一次规划结果，当前驾驶模式结合followingPath、changingHeight、avoiding判断；例如A*预算用尽后，局部下穿仍可能成功。

JSONL为一行一个JSON对象，不能把整个文件当单个JSON解析。写入器约每秒Flush，结束时关闭本轮并保存结果；实时读取需允许共享读写，忽略尚未写完的末行。正常停止/退出后文件可完整逐行解析。磁盘错误显示在面板并停止记录，导航继续；未承诺进程崩溃、断电后的完整落盘。

这是一套操作与遥测记录，不是确定性输入回放器。物理执行仍可能受帧调度影响。

## 验证

Unity6000.6.8f1已真实导入并运行：

- EnemyLabPlayChecks：28项通过，包括两个蓝图替换、旧舰回收、仅两艘有效单位、暂停恢复、虚拟键盘按下/松开、虚拟鼠标点选世界目标、鼠标边沿记录、跨轮操作、唯一文件目录、最终落盘以及十一布局逐一启动记录。测试设备在finally移除并恢复原键鼠。
- NavigationCombatPlayChecks：55项原生产导航/炮塔回归通过。
- 实际目录识别28个有效选项；比对25个用户蓝图文件SHA256，未发生修改。
- Pike在带顶棚U形布局53.0秒到达、无接触。Finch在下穿布局31.5秒到达、无接触。另一次带顶棚U形47.3秒到达，日志查到一次目标舰接触，非墙体接触。
- 更复杂场地确实暴露寻路缺口：高墙测试49.0秒时手动结束，累计11次接触且未到达，日志包含NoRoute；封闭布局30.0秒超时、6次接触，属于预期不可达测试。未声称所有新布局或全部蓝图均能通关。
- 已检查界面截图、可见飞行轨迹/规划路线/顶棚轮廓。正常关闭后逐行解析包含539条导航采样和3次非空路线快照的会话，键鼠事件均存在。

编译、格式及差异检查见本次提交。柱阵和折返通道完成布局切换/记录冒烟，未进行完整多蓝图导航成功率或大规模性能验收；高墙局部终点/保守包络等规划问题留作后续优化，本轮不改变规划算法。

## 入口

运行时功能集中于 Assets/Scripts/Features/Combat/Enemies/Testing；EnemyAvoidanceLab协调试验、EnemyLabPanel负责操作界面、EnemyLabBlueprints复用正式目录、EnemyLabRecorder负责增量落盘。EnemyController.CaptureNavigation公开只读快照；ControlUnit.CollisionEntered提供接触事件，替换旧的动态/预挂试验接触探针。Editor制作与验证位于Assets/Editor/Combat，函数关联见Docs/RuntimeFlow.md与Docs/CodeIndex.md。

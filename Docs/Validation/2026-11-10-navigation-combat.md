# AI 导航与炮塔视线验证

日期：2026-11-10；项目 HY-Sandbox；Unity 6000.6.8f1，AI Navigation 2.0.16。

## 实现与配置

- EnemyController 接近/警戒返航接入 EnemyNavigationPath；使用 NavMeshCalculatePath 拐点驱动现有物理输入，不配置移动 Transform 的 NavMeshAgent。近距离目标被走廊阻隔时先绕行，不进入原地横移。局部避障、悬浮、阵营/任务优先级和目标距离滞后保持。
- Main、Siege、WorldLab 已由连接 Editor 的 EnemyNavigationAuthoring.Install 保存共享 EnemyNavigationWorld。新增脚本 .meta 由 Unity 导入生成。Main 保存时 Unity 同时补序列化已有缺省字段 `_footprintPadding: 1`、foundation offsetX/offsetZ=0，未迁移已有布局。
- 导航默认按舰队中心240m半径、160m垂直范围，每3秒异步更新，净空半径2m、净高3m，0.5m体素；静态地形/建筑入网格，移动舰体和动态碎片不入网格。无覆盖回退局部避障，断路停止于可达边缘。独立 PhysicsScene 用根对象收集几何，因为 Unity 的 Bounds 收集重载仅查询默认物理场景。
- TurretTargetSelector 统一从炮口按 hitLayers 检查视线，过滤遮挡候选；当前锁定每个物理步再验证。只忽略炮塔自身 Collider，所属舰体/基地其他墙板也阻挡；触发器不阻挡。目标外壳属于同一可攻击单位，不将其当成外部障碍。

## 实际执行

连接本工程 Editor 导入/编译成功。Main Play Mode 中执行独立物理场景夹具 `NavigationCombatPlayChecks.Begin()`，异步完成后读取 Pending；**17项通过**：

1. 运行时碰撞生成并注册 NavMesh。
2. 追踪方向绕开实墙。
3. 导航不直接传送物理单位。
4. 战斗距离内隔墙目标仍绕行。
5. 可见较远敌人优于隔墙近驾驶舱。
6. 独立物理场景实墙阻挡视线。
7. 触发器不遮挡。
8. 配置命中掩码用于视线。
9. 激光炮塔获取无遮挡目标。
10. 激光炮塔尊重同组墙板遮挡。
11. 激光炮塔在索敌间隔前丢弃被遮挡锁定。
12. 实体炮塔获取无遮挡目标。
13. 实体炮塔尊重同组墙板遮挡。
14. 实体炮塔在索敌间隔前丢弃被遮挡锁定。
15. 断路重建后停在可达边缘，不横越缺口。
16. 禁用导航注销其网格。
17. 无网格回退原物理驾驶方向。

随后 `AiUpgradePlayChecks.Run()` **21项通过**：目标滞后/明确任务/失效即时替换、地板重叠/触发器/局部PhysicsScene/密集球扫、修复和重组后的目标缓存、模块实时位置/启用、炮弹相对速度提前量及无解/寿命/射程边界。旧夹具移除避障段留下的墙体和密集障碍后再验证选敌缓存，避免新视线规则使无关测试互相干扰。

## 静态检查及限制

- Roslyn CodeReadability：416脚本，format differing=0，errors=0；导航生成61张源码图。
- `dotnet build .../HY-Sandbox.sln --no-restore -v:q`：0错误、36警告（现有 .NET Framework/nunit 引用和过时 Unity API 等）。仓库 global.json 严格指定9.0.318，本机仅9.0.320；从仓库父目录执行绝对项目路径，使用已安装9.0.320完成构建和检查；没有修改全局配置。
- 本任务文件 `git diff --check` 通过。全工作区检查因已有 UserSettings 布局尾空格失败，未清理或提交该无关改动。
- 测试完成退出Play并恢复原MainMenu场景。CLI求值异步任务曾阻塞主线程，重启后改用Begin/Pending；恢复副本移至忽略的 `Logs/NavigationRecovery-20261110` 保留，未提交。
- 未完成实际大型舰队飞行观感/性能采样、完整探索/基地守波流程与Player构建。地面NavMesh辅助不是三维空域寻路；大型舰艇需按实际尺寸调整净空。周期刷新对动态建筑/破坏有最多一个刷新周期及烘焙时长的延迟，期间局部球扫继续工作。

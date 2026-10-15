# 敌方蓝图恢复验证（2026-10-15 夜间）

生产缺陷：EnemySpawner 在 Prefab Awake/CSV 初始化后覆盖模块 x/y/z，却未刷新几何与质量。3×2×1 模块仍带模板碰撞体和自动质量。缺失资源原先直接跳过，可能留下只有驾驶舱的残缺敌机。

修复：恢复尺寸后调用现有 Block.RefreshRuntimeData，再汇总 Rigidbody 质量；生成前确认全部模块资源有效且恰好一个驾驶舱。所有异常模块均整机拒绝，局部位置/旋转和敌方归属保留。不改变用户蓝图库、敌机数量、AI或武器参数。

## 实际证据

- [修复前独立Player](2026-10-15-night-enemy-restoration-before.json) 在真实一帧后碰撞体尺寸断言失败，completedCycles=0。不是仅从源码推测。
- [修复后独立Player](2026-10-15-night-enemy-restoration.json)：Direct3D11，1280×720，固定种子14721，2/2会话完成、24类检查、errors为空。覆盖3×2×1实例真实BoxCollider=(2.9,1.9,0.9)、自动质量6与整机质量汇总、局部姿态与阵营，以及缺失资源/null模块/零驾驶舱/重复驾驶舱的整机拒绝。
- 使用 `-world-smoke-report <临时报告路径> -world-smoke-seed 14721 -world-smoke-cycles 2 -world-smoke-enemies`。验证参数只有显式报告入口才启用，正常游戏行为不变；测试蓝图仅在内存中创建，不写玩家文件。
- Unity6000.3.11f1 Windows非Development构建成功：0错误、1警告。Main返回干净Edit Mode，Editor未编译中且无脚本编译错误。
- 232份源码可读性检查、24张源码函数图/导航、Python工具9项及git diff --check通过；dotnet build 0错误、4个既有警告。
- 28份用户JSON SHA256未变；起始六份工程/配置/布局差异保留且不提交。测试Player已完成，保留用户原有Unity、Codex及Player。

## 边界

生成测试敌机使用运动学刚体并经过真实Player帧，证明恢复契约；没有验收输入驾驶、AI交火或战斗回收返航。一般烟测的移动仍是脚本路线、随机敌机关闭。损坏JSON文件解析、长时大规模交战与其他GPU未覆盖。报告帧时间含测试夹具活动，不作为优化前后对照。

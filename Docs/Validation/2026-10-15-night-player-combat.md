# 受控 Player 输入、交战和回收入仓（2026-10-15 夜间）

本工作单元补全实际运行证据与可重复入口，没有发现需要修改生产武器/回收算法的新故障。此前一般烟测关闭随机敌机且使用运动学载具，不能证明输入和战斗回收衔接。

## 已实际验证

Unity6000.3.11f1 Windows非Development构建成功（0错误、1警告）。独立Direct3D11 Player、1280×720、世界seed14721，命令为 `HY-Sandbox.exe -world-smoke-report <临时报告路径> -world-smoke-seed 14721 -world-smoke-cycles 1 -world-smoke-combat -screen-width 1280 -screen-height 720 -logFile <临时日志路径>`。结果为 [passed、1/1轮、28类检查、errors为空](2026-10-15-night-player-combat.json)，测试进程已正常退出。

- 注入虚拟键盘W键事件，生产ControlUnit.Update读取，真实PowerTransmissionDevice/PowerNetwork分配功率，MainThruster.FixedUpdate施力；动态刚体前进超过0.1m。松键后移动意图归零。未直接写入移动意图或位置完成这个断言。
- 生产EnemySpawner恢复携带一件特定部件的敌机。玩家现有ProjectileTurretSingle自然索敌、真实跨帧炮弹飞行并产生伤害，最终击毁500耐久驾驶舱；未直接调用Fire/ApplyDamage。
- 生产销毁/残骸清理释放蓝图中的货物。CollectionBot扫描、真实飞行拾取、携货回航、停靠到作者home后入仓；检查实际CargoHold内容。未手动发射任务、传送Bot、灌入功率或直接调用Store。
- 战斗后停止炮塔撤销功率，清理临时模块，继续一般跨区块200m脚本路线、退出清空与再次准备。
- 233份脚本可读性检查通过；25张源码函数图、真实方法导航与Python工具9项通过；dotnet build 0错误/4个既有警告，git diff --check通过。新脚本已由连接Editor导入并生成真实meta。
- 用户28份JSON哈希未变，起始六份工程/配置/布局差异保留且不提交。原有Player、Unity、Codex保留；不改NightDesk遮罩控制。

## 条件与缺口

夹具仅在显式参数与报告入口同时存在时执行。测试复制现有真实模块和内存蓝图，不保存游戏文件。输入阶段为无重力、固定高度/旋转的动态平台；战斗阶段固定平台并关闭敌机AI，避免飞行调参掩盖武器/回收链的验证结果。模块布局是测试布置，不是交付玩家蓝图。

这证明输入→推进与炮弹→击毁→回收→机器人返航入仓衔接；没有验收人工驾驶、双向交火、地形导航/悬浮飞行、Player返航文件保存、死亡及再次出发，不宣称P1整个闭环完成。保存/死亡仍只有既有隔离Editor探针证据。性能数据包含夹具与输入活动，不作为性能优化对照或支持上限。

# 撞击致死爆炸修复（2026-11-04）

Unity 6000.3.11f1，Assets/Scenes/Main.unity，已连接 Editor。

原因：DestroyManager 与 BuildingRuntime 将 DestroyedByCollision 为 true 的方块排除出爆炸分支。前者来自 2026-09-26 的 `5a4468a`（Unify translucent UI and soften physics destruction）；世界建筑保留了同类排除条件。启用 canExplode 的方块即使遭致命撞击也只走拆除效果。

修复仅移除上述排除条件。保留伤害来源标记、canExplode配置、单位爆炸速度增量上限、碎片双向碰撞保护、非游玩态规则，以及建筑爆炸批次/范围伤害。未修改Prefab、CSV、场景、存档或资源GUID。

## 已执行验证

- Unity recompile / recompile_status：脚本编译完成，无错误。
- Main Play 调用 PhysicsSandboxProbe.Run()：33项全部通过。报告见 [原始结果](2026-11-04-impact-explosion.txt)。
- 新增8项：普通单位撞击能量耗尽耐久进入销毁；canExplode=true生成且只生成一个真实爆炸VfxEffect；canExplode=false不生成爆炸；重复销毁均不重复生成；世界建筑撞击致死生成一次爆炸，重复删除不重复生成。
- 原25项继续通过：真实PhysX碰撞的法向/切向速度、质量、恢复系数和动态双方能量分摊；爆炸速度增量不超过3m/s；爆炸不直接伤害单位装甲；碎片碰撞受到保护。
- 静态构建：使用本机已安装SDK 9.0.320显式入口构建HY-Sandbox.sln --no-restore，0错误、4项既有警告。仓库global.json仍严格要求9.0.318，未修改约束。
- CodeReadability：335脚本，格式/导航检查无错误；函数图及源码导航已同步；git diff --check通过。

探针在独立LocalPhysicsScene创建临时对象，未改写用户存档或保存Main。致死爆炸回归直接调用生产ApplyCollisionEnergy入口，结合原真实PhysX碰撞检查；未进行带大型舰船的实际撞毁画面对照、连锁爆炸压力测试或独立Player验证。检测到真实爆炸Graph实例生成，不将其等同于GPU渲染外观验收。

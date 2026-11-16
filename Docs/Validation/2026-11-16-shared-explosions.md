# 统一爆炸伤害验证

Unity 6000.6.8f1，Main，2026-11-16；NightDesk自主开发P1。

## 行为与迁移

- DestroyManager和独立WorldLab共用ExplosionDamageSession。模块中心距离线性衰减，每模块一次；同源去重，同PhysicsScene，友伤开启，无遮挡。建筑刷子DamageSphere仍仅影响其显式建筑集合。
- 连锁非递归排队。源建筑删除前建立外层批次；所有伤害结束后刷新支撑/舰船拓扑，再给新残骸冲量。碰撞保护不抵消直接爆炸伤害。
- 原WorldBuildingDamage源码/meta移动为ExplosionDamageSession，保留GUID，旧实现已删除。独立场景采用相同默认冲量参数。
- 新增Block.explosionDamage及严格CSV列，独立于冲量。Editor迁移幂等且使用原有备份/Bake流程。旧值×0.1、上限100，初始50/80/100；原建筑伤害曾等于500/800冲量，本次有意调整，仍需实战平衡。

## 已运行

- ExplosionIntegrationChecks.Run：14项通过。舰→舰、舰→建筑、建筑→舰、舰→体素，多Collider不重伤，攻击者传播，连锁越过首爆半径，重复源去重，同坐标另一PhysicsScene无伤害/冲量。
- SiegeBuildingDamageChecks.Run：18项通过。双向目标顺序、单次支撑遍历/伤害计量、普通损坏立即刷新。
- PhysicsSandboxProbe.Run：33项通过，含源场景冲量与碰撞保护。隔离夹具现显式配置VFX池与烟迹，与生产Prefab相同依赖。
- BlockCsvChecks.RunPlayMode：4项通过；RunEditMode：17项通过，新增伤害43的持久化、重复同步零改动、非法整批拒绝及原始CSV/Prefab字节恢复。
- Unity真实导入/编译、Play与Console已检查；制作过程中缺列错误已修复，末次groundTruth为0错误、无编译失败。

## 缺口

人工视觉、代表性大规模连锁耗时/GC、完整多舰交战平衡及Player构建未运行。注册表按每次爆炸扫描活跃耐久模块，暂不宣称性能提升。静态构建与格式结果记录在本次提交Validation中。

# 敌机蓝图库逐文件异常隔离（2026-10-15 夜间）

缺陷：EnemySpawner.InitializeEnemyBlueprintPool直接读取并解析每份外部JSON，损坏JSON或读取冲突会中断初始化，后续有效敌机无法进入缓存；调用发生在Start/BeginPlayMode，因而影响进入游戏。

修复只隔离每个文件的IOException、UnauthorizedAccessException和ArgumentException，报告带蓝图名称的Warning并继续。其余编程异常仍正常暴露。完整模块/驾驶舱校验沿用上一单元；不修改原始文件、不引入存档迁移。

- [修复前真实Main Play入口](2026-10-15-night-enemy-pool-before.json)：ArgumentException: JSON parse error: Invalid value，栈明确指向InitializeEnemyBlueprintPool。测试finally成功恢复原SaveManager，不污染用户目录。
- [修复后真实Main Play入口](2026-10-15-night-enemy-pool.json)：6项检查通过、errors为空。独立GUID临时目录包含两个有效单舱蓝图、截断JSON、独占FileStream锁定文件、空对象及null模块；确认两个有效名称恰好都在缓存、初始化完成、重复调用不加倍、所有原文件文本不变、原私有文件存储和列表引用恢复。
- 探针通过Editor反射仅临时替换SaveManager的文件存储，调用真实私有初始化方法；同步执行、不跨引擎帧、不保存场景。专用EnemySpawner随后销毁；不触碰用户EnemyBlueprints。
- Unity6000.3.11f1导入/编译通过，Main回到无脏场景Edit Mode。234份脚本检查、25张源码函数图及导航、Python工具9项、静态构建（0错误、4个既有警告）与diff检查通过。用户28份JSON哈希未变；六份起始差异未提交。

边界：此单元没有重新构建Windows Player。覆盖单文件解析与独占锁读取，不覆盖整个目录权限丢失、磁盘满、进程崩溃或网络文件系统。无效文件修复后的实时热重载未新增；沿用已有缓存生命周期。

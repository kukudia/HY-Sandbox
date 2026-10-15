# 既有敌机自然飞行与交火验证（2026-10-15夜间）

使用现有CODEX_E01_Finch蓝图原始95模块、质量592，生产EnemySpawner恢复，保留动态刚体、EnemyController、真实电网、悬浮和武器。不修改作者蓝图及参数；玩家为固定高耐久烟测靶。

## 实际结果

Unity6000.3.11f1独立Windows非Development Player、Direct3D11、1280×720、seed14721；最新建筑候选概率10%，初始9栋/494模块。运行参数为 `-world-smoke-report <临时目录>/enemy-flight-v2.json -world-smoke-seed 14721 -world-smoke-cycles 1 -world-smoke-enemy-flight CODEX_E01_Finch -screen-width 1280 -screen-height 720 -logFile <临时目录>/enemy-flight-v2.log`。

[实际报告](2026-10-15-night-enemy-flight.json)：passed、1/1轮、26类检查、errors为空；进程正常退出。自然运行30秒，日志记录最大位移22.50m，target/intent/power/hover/hostileDamage均True；敌机驾驶舱持续有效，世界坐标有限。伤害由实际敌方武器产生并保留Enemy攻击者归属；无直接Fire/伤害调用、手动供电或传送敌机。随后200m脚本跨区块、退出清空与再次准备完成。

自然飞行阶段包含等待玩家初始化和敌机创建共3825帧/32.01s，P95 8.91ms、P99 9.20ms、max100.38ms；此为单个敌机短时条件记录，不作为同条件性能优化结论。不能由此推导1080p或大量敌机支持上限。

## 验证与失败记录

- 首次新路线因同帧创建的玩家ControlUnit尚未Start注册/延迟RefreshChildren，读取cockpit时空引用。改为最多等3秒真实初始化并明确检查；未修改生产生命周期规避失败。
- 首次构建期间系统发生重启，旧Editor任务中断；重启后的直接构建调用缺少Temp报告目录，返回异常。显式创建报告目录后构建成功。最终包含夹具修正的构建0错误/1警告，不能把中断或异常尝试计作通过。重启原因未确定。
- 连接Editor已导入新增脚本和meta；Main干净EditMode，无脚本编译错误。235份源码可读性、25张源码函数图及Python工具9项通过；dotnet build 0错误/4个既有警告；git diff --check通过。
- 运行后28份用户JSON与起始SHA256一致；六份起始工程/配置/布局差异保留不提交。

## 边界

玩家是固定靶，不含人工驾驶、双向动态交战、敌机跨区块导航、玩家返航写盘/死亡再次出发或长期压力。没有发现本单元需要修改生产AI/悬浮/武器算法的问题；本提交补全验证入口与真实证据，不宣称整个P1完成。当前10%配置不可与旧35%性能报告作直接前后比较。

# 三轮组合Player回归（2026-10-15夜间）

使用58d90a8源码构建的现有Windows非Development/D3D11包，1280×720、seed14721、建筑候选概率10%，不重新构建。参数同时启用 `-world-smoke-cycles 3 -world-smoke-enemies -world-smoke-combat -world-smoke-enemy-flight CODEX_E01_Finch -world-smoke-return`，配合唯一临时report/log路径。

[实际结果](2026-10-15-night-player-combined.json)：passed、3/3轮、58类检查、errors为空，Player正常退出。每轮依次完成敌机恢复/非法蓝图拒绝、Input System推进/实体炮弹击毁/机器人拾取返航入仓、既有Finch自然AI飞行交火30秒、200m脚本跨区块及退出再准备；三轮后追加隔离文件仓返航写盘/重载、舱毁重生清货、第三次出发及恢复原存档。没有新错误或额外生产修复。

各轮退出前Unity已分配内存分别139509983、139631567、139655055字节，第一到第三轮增145072字节（约0.14MiB）。只是三个采样点，不能证明无泄漏或长期稳定。用户28份JSON起始哈希未变，六份未提交工程/配置/布局差异保留。

该组合证明不同显式路线在同一进程反复运行未出现已捕获回归；各夹具仍分阶段创建/清理，返航货物另由RestoreContents提供。它不等于一次完整的双向动态驾驶、交战、同件战利品返航巡航，也不是压力上限/1080p验收。源码和静态检查沿用58d90a8已通过结果；本提交只有新运行证据和入口记录，未无意义重跑编译。

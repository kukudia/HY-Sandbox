# 独立Player返航、死亡重生与再次出发（2026-10-15夜间）

## 实际路线

Unity6000.3.11f1 Windows非Development构建成功（0错误/2警告）；Direct3D11、1280×720、seed14721、建筑候选概率10%。命令参数：`-world-smoke-report <临时目录>/return-player.json -world-smoke-seed 14721 -world-smoke-cycles 1 -world-smoke-return -screen-width 1280 -screen-height 720 -logFile <临时目录>/return-player.log`。

[结果](2026-10-15-night-player-return.json)：passed、1/1轮、28类检查、errors为空，进程正常退出。前16类为一般加载/200m跨区块/退出再准备，随后追加12类返航检查。

1. 使用仅本次Player拥有的唯一Application.temporaryCachePath子目录，通过反射交换SaveManager的私有BlueprintFileStore。正常运行没有覆盖入口；夹具finally恢复原文件仓、缓存、名称和名称列表。唯一目录保留作本地复查，不进入仓库。
2. 写入两模块测试蓝图，通过SaveManager.LoadSave/实际Loader恢复驾驶舱和相连货仓；经MainUIPanels.PlayStart进入真实世界。检查货仓归属有效玩家。固定载具刚体，RestoreContents放入一个UniversalThruster部件作为持久化输入。
3. 正常MainUIPanels.PlayEnd先经CargoPersistence写入，再真实结束世界和重载两模块蓝图；检查文件中货物、原子备份、无tmp和恢复后的货仓内容。第二次出发恰好保留一件货物。
4. 调用生产DestroyManager销毁驾驶舱，走PlayerDeath链，检查可用的重生按钮；通过respawnButton.onClick.Invoke触发实际绑定的PlayEnd，清空持久货物且保留两模块原蓝图。重载后货仓为空、世界Ready。
5. 第三次出发实际进入Playing，正常返航后回到Ready；恢复原文件仓，只读加载原AirKing，442模块恢复完成，世界Ready。

## 验证与限制

新增脚本和meta经连接Editor导入；237份源码可读性、25张源码函数图、Python工具9项通过；dotnet build 0错误/4个既有警告；git diff --check通过。28份用户JSON与起始SHA256一致，六份起始工程/配置/布局差异不提交。Editor保持干净EditMode；未关闭用户Unity/Codex，也未改变NightDesk接管策略。

本单元未发现需要修改生产返航/重生算法的新缺陷；验证包包含此前3523741的缺失存档保护修复。故障注入的四类文件失败仍以该修复的Editor真实Play报告为证，没有在本Player路线注入预期错误。

货物通过夹具RestoreContents提供，驾驶舱销毁由测试直接发起，载具固定；没有人工驾驶、敌方自然击杀玩家或将此前战斗机器人回收的同一件货物直接串到本次文件返航。各子链有真实生产证据，完整连续动态巡航和长期多敌机压力仍未验收，P1不标整体完成。性能采样包含测试活动，不能用来声称优化幅度。

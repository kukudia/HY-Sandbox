# 2026-10-23 建筑底座支撑缓存

BuildingConnectionGraph的规范布局节点复用已验证几何快照，缓存虚拟底座接触判定。底座尺寸另作缓存键；模块、连接面、根缩放变化随节点重建失效。复杂连接层级保持实时判定，不改变建筑支撑、残骸或维修规则。

真实Siege Play对照缓存/实时几何16项、邻接43项、损毁18项、建筑20项、玩法21项通过。对照覆盖底座缩放、根平移/倾斜/缩放、模块姿态、连接面开关/位置/法线、深层连接祖先变换及恢复。检查后恢复全部值，不保存测试姿态。

## 同步夹具

同一Editor、两层1m模块夹具，一次预热、每尺寸三份新夹具。before先于after，非ABBA；不推进物理步，嵌套阶段不能相加。这些结果仅证明同步路径开销，不是自然FPS收益。

| 模块 | 热完整刷新 before/after | 热底座 before/after | 范围损毁 before/after |
| --- | --- | --- | --- |
| 1024 | 7.128 / 4.605ms | 1.221 / 0.073ms | 34.097 / 30.272ms |
| 2048 | 16.995 / 13.851ms | 2.638 / 0.143ms | 45.794 / 43.183ms |

2048冷完整刷新71.119→71.269ms，未改善；损毁内底座2.562→0.118ms。原始[before](2026-10-23-siege-foundation-before.json)、[after](2026-10-23-siege-foundation-after.json)。

## 真实Player回归

D3D11发布包，2048模块建筑基地、六舰、显式嵌套计时。182.05秒10102帧，71项检查通过；完成首波，第2波仍1敌机，基地1932/2048。峰3418模块/193刚体，中位16.75/P95 28.05/P99 34.78ms。最长169.42ms帧相邻舰船完整刷新5.13ms，不归因BFS。战况与旧窗口不同，不作帧率加速对照。

进程27808于18:41:34.743启动、18:41:35.637恢复本次UnityWndClass窗口；部署与终态PNG已实际查看。低频普查仍分配并扰动时间。托管errors为空，但原生日志含10条D3D11 PresentFrame时序提示，不能称引擎完全无诊断。尝试bitblt呈现参数的30秒校准仍复现同类提示，未修改项目图形配置，不沿此路线继续重复。

同包Player闭环75项通过。发布构建0错误/1警告；dotnet静态构建0错误/4既有警告；Python工具9通过；源码302脚本/38图仅既有MainUIPanels格式差异；git diff --check通过。

[缓存16项](2026-10-23-siege-foundation-cache.json)、[邻接43项](2026-10-23-siege-foundation-graph.json)、[损毁18项](2026-10-23-siege-foundation-damage.json)、[建筑20项](2026-10-23-siege-foundation-building.json)、[玩法21项](2026-10-23-siege-foundation-play.json)、[Player闭环](2026-10-23-siege-foundation-smoke.json)、[自然报告](2026-10-23-siege-foundation-natural.json)、[原生诊断](2026-10-23-siege-foundation-native.txt)、[部署](2026-10-23-siege-foundation-deployment.png)、[终态](2026-10-23-siege-foundation-final.png)。

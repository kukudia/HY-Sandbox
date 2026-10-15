# 建筑携货卸载验证（2026-10-15 夜间）

复现：在 Main 的隔离 Play 对象中恢复包含货仓和收集机器人仓的真实 BuildingRuntime，提供固定供电，让 CollectionBot 通过真实物理帧起飞、拾取特殊部件。运输中调用生产 WorldBuildingManager.RemoveBuilding。修复前出现 Unity Error：Cannot set the parent ... while activating or deactivating ... Cargo socket，随后掉落未能移出被删除的建筑层级。

初始“起飞后不再是建筑后代”的假设经实测否定：Prefab 的 outside 仍在母仓内。测试已按真实离开 home 的父关系判断起飞；战斗断裂本来就正确释放，未为不存在的问题新增机器人全局注册表。真正缺口位于层级停用时序。

实现：BuildingRuntime.PrepareForUnload 只通知子机器人取消运输；WorldBuildingManager 在单栋卸载和批量清理前调用它，WorldSessionManager 在准备新会话、失败和停用世界根前批量通知。复用现有 CollectionBot.Abandon/LootDrop.Release，释放认领和目的仓预订，不调用残骸转换、不洒落仓内货物、不把飞行中物品写入快照。

## 实际验证

- Unity 6000.3.11f1 / Main / NightDesk 后台，[完整回收 Play 71 项通过](2026-10-15-night-carried-unload.json)，errors 为空。增加真实起飞携货、打断母仓、单栋 RemoveBuilding、PrepareForUnload 后停用整个建筑根、重新恢复仍只有两件已入仓货物；原有溢出入仓、机器人销毁/释放、临时存档返航/死亡和移动母体返航继续通过。
- [世界边界 23 项通过](2026-10-15-night-carried-unload-edges.json)：最大建筑预算延期、高速冻结/真实物理步恢复、脱锚建筑缺地形休眠、恢复位置/耐久/速度。该旧探针含显式 Guard.Tick/Physics.Simulate，不冒充人工驾驶；仍排除故障注入。
- 232 份源码可读性检查、源码导航/23 张图、工具 Python 9 项、git diff --check 通过；dotnet build 0 错误、4 个既有警告。
- Windows 非 Development D3D11 构建成功，0 错误、1 个警告；[固定种子 14721 的两次 Player 进退会话](2026-10-15-night-carried-unload-player.json)通过，16 类检查、2/2 completedCycles、errors 为空，验证进退会话/区块流式一般回归。这个烟测仍是临时运动学驾驶舱、关闭随机敌机，不验证 Player 携货或交战，不用于性能前后对照。
- 测试自启 Player 已结束；用户原有 Player、Unity、Codex 均保留。28 份用户 JSON SHA256 未变，Main 已回到无脏场景的 Edit Mode。起始六份工程/配置/布局差异不提交。
- 构建阶段短暂报告 Prefab/URP/GraphicsSettings/TMP 差异，完成后这些文件均与 HEAD 一致；未执行还原或纳入提交。

## 验证边界

探针隔离 Main 根、手动提供测试供电，运输/拾取/返航依赖真实 FixedUpdate 与时间推进。未对其他 GPU、长时大规模 Bot、任意外部脚本直接停用任意携货祖先、故障注入或完整 Player 人工驾驶/交战/返航作验收。用户原有场景与存档不作为测试对象。

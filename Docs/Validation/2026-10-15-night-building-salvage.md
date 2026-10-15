# 建筑断裂回收验证（2026-10-15 夜间）

需求证据：BuildingRuntime.SplitDisconnected 将失去虚拟地面连接的模块移出主体快照，WorldBuildingManager.CreateDebris 随后禁用其脚本并限时删除；这条路径未调用既有 WreckSalvage，导致货仓与运输物消失，敌方残骸也不参与既有回收。

实现：在断裂模块重挂接和预算判断前调用 WreckSalvage.Convert。保留 RuntimeUnitMember 原阵营、既有奖励概率/白名单、货仓释放策略及每模块幂等标记；不对直接被摧毁的模块额外发奖，不新增经济规则。普通流式卸载和会话清理不调用转换。WorldLab 没有 Main PlayManager 时沿用原先无回收奖励行为。

## 实际验证

- Unity 6000.3.11f1 / Main / NightDesk 后台，菜单 Tools > Salvage > Validate building debris in Play Mode。隔离 Main 根对象，在临时 Play 对象上用真实 Durability.ApplyDamage 触发生产 BuildingRuntime.DestroyBlock/SplitDisconnected/CreateDebris；没有手动调用转换来替代首次断裂结算。
- 修复前相同专项通过“快照移除”后，在“断裂货仓释放并抽取奖励”断言失败；修复后 [11 项全部通过](2026-10-15-night-building-salvage.json)，errors 为空。
- 覆盖正常与零物理残骸预算、货仓内两件特殊部件、保证概率下敌方金币、重复转换、空主体快照重新恢复、玩家货仓返还但无金币、休眠卸载/重载保留货物且不掉落、退出清理不掉落。测试临时修改奖励概率，并在 finally 恢复；不保存资源或场景。
- 232 份源码可读性检查通过；导航/23 张函数图重新生成，工具 Python 检查 9 项通过。dotnet build HY-Sandbox.sln --no-restore --nologo：0 错误、4 个既有警告。git diff --check 通过。
- Main 已退出 Play，场景无未保存修改，Unity 编译无错误；28 份用户 JSON 对照起始 SHA256 无变化。起始六份配置/布局/工程差异不纳入提交。

## 限制与未通过项

旧完整 SalvagePlayProbe 首次尝试在 Coins scatters and rotates 断言失败（此前仅通过标签可见检查）。该结果不能标为完整回归通过，后续需区分探针采样时序与真实散射问题；新建筑专项独立入口避免以此无关前置项阻止定位建筑缺口。初次新增探针存在 ApplyDamage 缺少 attacker 参数的编译错误，补齐 null 后 Unity 编译通过，未将失败运行算作验证。

本次只有 Editor Play 隔离生命周期回归和静态编译。尚未覆盖真实输入驾驶、建筑机器人在飞行中断裂的组合、正式 Player 完整交战/回收/返航、特殊奖励概率统计或压力性能；不据此宣称 P1 闭环验收完成。

# 大型建筑蓝图基地迁移验证

2026-10-23，Unity 6000.3.11f1，连接本工程Editor实际执行。

基地由32块驾驶舱原型迁移为32×32m、388模块建筑蓝图，复用BuildingRuntime及虚拟底座支撑。独立发电模块bastion-core作为失败目标；飞船部署、驾驶权和舰船维修保持独立。新增BuildingRefit按原蓝图原位补件，保留存活对象/货物，净空和金币检查成功后扣费。

## 实际验证

- 19项建筑Play检查通过：有效foundation、无驾驶舱、388模块单连通/无占格重叠、支撑刷新不进入舰船重组、整备伤害门控、健康与缺件维修、阻挡拒修不扣款、模块ID唯一、独立舰船部署、战斗拒修、真实核心损毁失败。报告：siege-building-checks.json。
- 最新20项玩法闭环通过，SiegeBuildingPlayStatus=Passed，实际报告已归档为siege-building-play.json。采用Editor delayCall避免CLI独立5秒主线程超时；此前一次CLI超时但Editor实际完成，未把超时本身算通过。
- 既有WorldLab建筑43项回归通过，报告siege-building-world-regression.json；旧日期报告原bytes已恢复。
- 修正中继位置后，真实4秒/214推进帧固定敌方靶检查四塔均请求并获电，命中216伤害；报告siege-building-power.json。这是受控靶，不是自然波次验收。
- Siege场景保存后重新打开：scene.isDirty=false，建筑管理器存在、核心ID正确、蓝图388块。既有蓝图GUID和舰船泊位/UI保留。

## 自然交火与压力边界

首轮失败供电布局独立保留为siege-building-base-unpowered.json及原始gzip；中继超范围已修。该90秒窗口4649观察，中位17.66/P95 29.01/P99 40.08ms，421538次物理邻接查询。

修正后部署Finch/Pike并开始波次，90.007秒、4467观察、0跳帧观察，中位19.40/P95 29.18/P99 39.78ms，403190次物理邻接查询、0遗留全场引用扫描。峰值927块、130刚体。舰船完整刷新52次累计165.82ms，嵌套重建145.19ms、连接24.69ms，不相加。报告siege-building-powered.json及原始gzip。两份gzip均解压校验SHA256及样本数一致。

两窗口均记录到波次1和2；工具只显式开始一次，不能确定第二波触发来源，不能称为纯无人操作确定性对照。自然战斗不具确定性，不能据两窗口断言供电修复改善帧率。采样含Editor调度/渲染和每秒场景普查，窗口内一次截图也可能扰动。现有阶段计时只覆盖舰船重组，不含BuildingRuntime.SplitDisconnected；全局邻接计数包含建筑。下一步需补建筑分项计时及同夹具对照。

GameView截图已查看，部署舰/战斗HUD可见，但右侧核心状态在当前自由比例视口出现裁切；后续单独修复响应布局。尚未验证正式Player、万块战斗性能、长时多波平衡和完整保存系统。

## 静态检查

dotnet build HY-Sandbox.sln --no-restore --nologo：0错误、4既有警告（3项MSB3277及Profiler过时API）。git diff --check通过。代码可读性检查仅剩原有MainUIPanels格式差异，本次源码通过；已刷新函数图与真实方法导航。

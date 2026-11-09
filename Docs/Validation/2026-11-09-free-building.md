# 地面自由建造、舰队调整和透明顶棚验证（2026-11-09）

项目HY-Sandbox，Unity 6000.6.8f1；连接本地Editor执行。保留开始时三处UserSettings未提交修改，未改写玩家蓝图；测试注入独立Temp文件仓。

## 交付行为

- 普通建筑和基地可直接在y=0地面放置，不再设置地基宽长。已有模块继续连接点吸附；空地使用整格最小角吸附，奇偶尺寸都能和地面锚点对齐。地面射线受当前建造距离及UI遮挡限制，载具模式维持原连接式建造。
- 每次实际编辑保存（包括移动、旋转、尺寸、删除、Undo/Redo）按旋转包围盒裁切空白网格。Footprint Padding在BuildManager Inspector可配0–8，默认1；只更新占地元数据，不删除模块或移动坐标。玩家铺设的平台、核心、驻军位置保留。已有64m单边部署安全上限和Siege预算/限高保留；超过上限可继续编辑保存，不截断实体。
- BuildingFoundationData新增offsetX/offsetZ，缺失默认0对应旧(-2,-2)原点。旧蓝图打开仅计算内存预览，下一次真实编辑才写入；世界地形生成、采样、地面锚点、流送、损伤、维修及基地出生点均适配实际偏移。写入使用.pending再替换。
- 两套Siege整备时已占机库显示MANAGE，卡片可REPLACE SHIP或UNDEPLOY / REFUND；战斗及返航禁用。完整舰按购买价退回；战损残值=max(0,购买价−原维修报价)，换购抵扣残值。新舰尺寸、余额、外部障碍或创建失败保留原舰和金币。取消/更换清理原舰及所属断裂件，重复取消不能重复退款；预研换购继承驾驶权。
- 保存的Roof材质alpha=0.045，透明且不投阴影，实体顶棚碰撞保留。不是仅运行时改色。普通建筑面板移除旧宽长输入及Apply Size按钮，保留据点制作手工布局。新版、预研及三个Stress场景补全序列化部署管理按钮，旧制作工具也同步。

## 实际验证

| 入口 | 已执行结果 |
| --- | --- |
| HangarAssetChecks.RunEdit | 19项通过，包括透明Roof/实体碰撞/无阴影、旧无偏移JSON兼容、净空和容量规则 |
| WorldBuildingFootprintChecks.RunEdit | 5项通过，包括280个真实地形生成站点、四种旋转、逐格支撑和偏移JSON |
| WorldBuildingFootprintChecks.RunRuntime | 6项通过，真实BuildingRuntime分离落地群组支撑、旋转占地、快照和偏移维修保护 |
| HangarWorkshopChecks.Run | 48项通过，真实InputSystem普通建筑/基地鼠标地面放置、负方向裁切、正方向扩展、删除/Undo/Redo、坐标身份保留、重载、机库尺寸/移动/旋转/复制/数量限制 |
| BuildingEditorPlayProbe.Run | 67项通过，普通建筑/敌人/载具切换、旧文件保护、驻军数据保留、复制/重命名、真实连接、自动范围UI及删空生命周期 |
| SiegeHangarHostChecks.RunStage | 41项通过，实际MainMenu入口、新版保存UI按钮、替换/取消/退款、过滤、失败原子性、清理、阶段锁定、返航结算与付费修理 |
| SiegeHangarHostChecks.RunPrototype | 37项通过，预研实际入口及同等操作，另含驾驶继承 |

已审看Temp/Hangars/host-stage-actions.png和Temp/BuildingEditor/BuildingEditor.png：管理按钮、退款说明和自动占地文字可读，屋顶能透视库内舰体。自动占地首次世界fixture检查把含高度包围盒的Y也与地面中心比较，已修正为XZ后完整重测通过；部署首次运行遇到并发源码重载，退出后统一导入并完整重跑通过。

静态检查：可读性412脚本，0差异/0错误；解决方案构建0错误、36条既有警告；函数图与导航刷新为60张图。任务范围diff whitespace通过，全工作区仍有开始时即存在的UserSettings布局空格，不修改或提交这些设置。SDK9.0.320从临时目录用绝对项目路径运行，未修改global.json。

额外只读验证：正式StarterHangar自动裁切后占地61×34m，389模块，16992/20000预算，实际配置Validate通过。

报告与截图位于Temp/FreeBuilding、Temp/BuildingEditor、Temp/Hangars，不提交临时运行数据。未执行独立Player打包或长时间多舰压力测试；世界流送本轮有Editor导入及源码检查，偏移地形和建筑实体已有实际运行验证，未跑完整长距离探索流送压力回归。

# 基地、列表与 Siege 界面修复

Unity 6000.6.8f1，连接本机 Editor，2026-11-09。

## 行为与入口

- 下拉模板原有 20px 行高与放大的白色文字不匹配；统一增加行高、留白与深色背景/选择色。修改已保存到菜单、预研 Siege/压力场和建筑面板 Prefab。公共制作入口：`DropdownTemplateAuthoring.Apply/UpgradeAll`，原制作工具复用规则。
- `BuildTargetKind.BaseBlueprint` 追加枚举值，保持旧类型序号。基地使用 `BaseBlueprints`，普通建筑使用 `BuildingBlueprints`，共用结构编辑/连接算法，列表与文件操作按显式目标隔离。`MenuPlayDraft` 同时保留基地和建筑最近名称。
- `BaseBlueprintMigration.Run` 迁移旧建筑库中已指定核心且非据点的文件；同名不覆盖，复制字节校验后在 `BlueprintMigrationBackups` 保留原件，再移出建筑目录。未标核心的普通建筑与据点不迁移。
- 结构建造距离独立配置为 150m，进入结构编辑恢复输入；基地面板隐藏普通据点控件，并缩小背景射线区域。底座/模块/核心仍为可编辑资源与既有生产规则。
- `SiegeGameplayStage.Enter` 读取进入前选定的基地快照，在场景 `Base Spawn` 自动生成。出生点表示底座中心，地面高度取竞技场顶面；所有连接分量仍须具有真实地基支撑。无效/丢失设计清理副场景并恢复原稿；重试保留同一选项。
- 新 Siege 局内只保留基地/波次状态、下一波、修复重建、撤退/返回。菜单和 Main 的 Simulate 面板承担进入前选项；旧 Siege 预研玩法保留，仅修正其下拉模板。

## 验证

- `MainMenuPlayChecks`：72 项通过。覆盖冷启动、实际展开列表文字网格、菜单导航、基地目录与同名建筑 CRUD 隔离、Vehicle→Base 后超过 15m 的实际鼠标预览/放置/保存、原稿恢复及货仓保存失败保护。
- `SiegeConstructionPlayChecks`：48 项通过。自动出生、缺文件/悬空设计拒绝、重试、真实支撑与核心、战斗/整备、修复报价/净空/余额、真实敌机、结算和原稿恢复；包括 Simulate 进入前选项。
- `BuildingEditorPlayProbe`：73 项通过。覆盖普通建筑底座与连接、编辑/保存/复制/删除、据点部署、居民和跨模式恢复。迁移后测试调用者使用新的 `Outpost Controls` 路径。
- `BaseLibraryChecks.Run`：6 项通过，包括同名保护、原始字节、目录隔离、普通建筑/据点排除、备份与中断后重复执行。
- 保存重载：MainMenu、Main、SiegeGameplay、Siege 与三个压力场无缺失脚本；场景下拉项行高与字体匹配。建筑面板的标题、据点分组引用已保存。
- 静态检查：CodeReadability、解决方案编译与本次差异空白检查。解决方案编译 0 错误、36 个框架引用/弃用 API 警告；Editor 编译成功。全仓空白检查仅已有用户布局文件有尾随空格，本次暂存差异检查通过。

运行报告和实际画面位于本机 `Temp/MainMenu`、`Temp/BaseConstruction`、`Temp/BuildingEditor`；测试文件库均为隔离临时目录。函数图与导航见 `Docs/RuntimeFlow.md`、`Docs/CodeIndex.md`。

本轮验证为 Editor 导入、保存重载与真实 Play；未进行完整独立 Player 打包、各分辨率人工巡检或长时间压力验收。

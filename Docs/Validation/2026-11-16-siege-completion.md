# Siege补齐：示例、观察相机、容量与旧设计

## 交付入口与规则

- 主菜单 **SIEGE** 与工坊 **Simulate → Siege** 共用 `SiegeConstructionSettings.examples`，显示三套独立Example，所选资产贯穿模式切换；编辑示例仍先创建个人副本。
- Starter Bastion：72个逻辑模块、444个战斗模块，前门堡垒。
- Skyline Logistics：82个逻辑模块、391个战斗模块，顶门物流站与独立仓储供电。
- Twin Foundry：75个逻辑模块、756个战斗模块，前门和顶门双机库。
- 每套都包含金属、木材、石头、玻璃结构，以及Cargo/Coin/Technology Hold、RepairBot/CollectionBot容器；示例不附赠货物。配方由 `SiegeBaseExamplesAuthoring.Generate()` 保存为真实JSON并接线配置，替换前备份至 `Library/SiegeBaseExamples/<timestamp>`。
- SiegeGameplay场景预挂 `SiegeObserverCamera`：按住右键配合WASD/QE移动，Shift加速，滚轮调速，F回总览。UI起点、菜单、文字焦点和失焦阻止/释放捕获，退出恢复工坊相机。
- 保留自由建造不限量、Siege部署和维修金币预算；未引入有限背包。解除单建筑2048/世界8192模块硬限；单模块合法尺寸、资源、身份、姿态、货仓及建筑数量校验保留。解除硬限不是无限性能保证。

## 错误蓝图治理

55份官方/个人蓝图完成结构审计，确认损坏JSON为0、隔离移动为0。审计工具提供哈希、原路径、独立备份和可恢复隔离清单；未完成草稿不按损坏文件处理。实际生成的个人审计清单位于 `Application.persistentDataPath/BlueprintQuarantine/20261116T110718411Z-23893b81194643c9ba7c288654920076/manifest.json`。

旧设计不能部署的实际原因已修复：旧奇数机库尺寸偶数化产生半格偏移；密集碰撞查询首个候选没有匹配连接面；逻辑机库与真实底板竞争相同承重点，以及底板无法通过禁用选取盒重新发现逻辑机库。当前只保留满足双侧引用、同域、存活和当前几何的真实机库连接；内部底板承载逻辑控制器，外周连接面承载墙体。未放宽连接距离、跳过支撑检查或删除个人创作。

官方/个人10份基地通过生产恢复和实际支撑图审计，包括个人3076模块Starter Bastion 2、824模块旧Starter Bastion、1411模块Combat Courtyard及官方1413模块Courtyard。奇数尺寸在内存兼容迁移，个人文件未自动重写。

## 实际验证

| 层次 | 结果 |
| --- | --- |
| Unity导入/编译 | 目标6000.6.8f1 Editor，0错误 |
| 示例数据 | 25项：目录、真实资产引用、四材质、Hold/Bot、不同机库布局、无逻辑重叠、无免费货物 |
| 示例Play | 18项：三套真实恢复、完整支撑、机器人/货仓、所有负载请求工作时满供电、材质身份 |
| 自由视角 | 6项生命周期；22项真实InputSystem输入，包括六方向位移、旋转、Shift、滚轮、总览、UI/菜单/文字焦点及Esc释放 |
| 模块机库Play | 26项：旧尺寸迁移、展开、独立墙体毁伤、缺口快照、付费维修生产恢复、逻辑底板连接和真实抬高平台 |
| 共用连接图 | 50项：缓存/失效/删除/运动/视觉策略；建筑与载具均在12个无匹配连接面碰撞体中找到有效连接 |
| 容量数据 | 8项：9001模块数据合法、原身份/资源/草稿保护与非法输入拒绝 |
| 容量Play | 11项：取消回滚；四座各2100模块实际恢复并激活；8400模块世界中损毁后按原ID补回，最终总数准确 |
| 蓝图支撑 | 10份全部supported，使用生产prepared连接图而非仅JSON检查 |

证据原始报告：`Temp/SiegeBaseExamples/{data,play}.json`、`Temp/SiegeCamera/input.json`、`Temp/Hangars/shell-checks.json`、`Temp/Siege/building-graph-checks.json`、`Temp/BlueprintAuditChecks/{capacity-play,support-audit}.json`。

初次大型检查300秒时限在完成8400模块恢复后、激活期间超时；调整夹具为2ms加载预算/600秒时限，随后完整通过，未改生产行为绕过验证。输入测试曾用新鼠标按下取消文字焦点，修正为保持鼠标捕获后获取真实输入焦点，验证正常UI时序。未通过的中间结果不作为验收证据。

Windows Development全场景构建成功（0错误、3条构建警告）。三套示例分别经真实主菜单进入Siege，购买Finch、实际有电出库、敌方出生与显式驾驶舱伤害、返航、结算、清场、卸载回工坊及主菜单，每套23项通过且捕获错误0。三次最终报告耗时约30.0/24.9/25.2秒；这是脚本伤害闭环，不是自然清波平衡证明。双库方向按每个机库ID匹配校验，不假设层级枚举顺序等于蓝图顺序；流程部署一艘Finch，不代表两艘同时通行已经验收。

最终证据保存在本目录的 [SiegeCompletion](SiegeCompletion/)。Editor实际画面已查看三套布局及观察相机提示；截图含Editor调试线，隐藏Player黑帧截图未用于视觉或帧率判断。真实场景进出确认观察组件启用、返回卸载并恢复工坊镜头。静态格式检查495脚本0差异/0错误，`dotnet build HY-Sandbox.sln --no-restore` 0错误（37条既有过时API警告），`git diff --check`本次文件通过。

长时间多波、多舰同时通行及人工硬件体验仍需专门实测，隔离检查不代表这些场景已经完成性能验收。

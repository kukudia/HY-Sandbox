# 2026-11-04 共享机制与代码精简

## 结果与范围

建筑与载具已复用同一连接缓存、连通分组和刚体运动继承；删除旧 `BuildingConnectionGraph`，其消费者迁入 `Simulation/Connections/BlockConnectionGraph`。同时统一输入推进器生命周期、逻辑包围盒、编辑动作恢复、蓝图加载协调、模块货物释放、机器人起飞门槛和操作轴方向。没有以减少文件数或机械合并玩法作为目标。

本轮以项目自有源码为范围，清单/AST扫描覆盖 **340份脚本（229份运行时、111份Editor）、2,172个方法**，并按建造/存档、世界/建筑/据点、Siege、仿真/物理、战斗、飞行、机器人、供电、回收、表现/界面、输入与启动主链检查职责和消费者。第三方资产与包源码不纳入重写。全量清单和静态扫描不等于每个方法、每个玩法组合均已实际运行或逐行证明无缺陷。

当前功能入口见 [DEVELOPMENT](../../DEVELOPMENT.md)，实际方法导航见 [CodeIndex](../CodeIndex.md)，源码关联主链见 [RuntimeFlow](../RuntimeFlow.md)。本报告记录本轮证据，不替代长期维护入口。

## 已实施的机制统一

| 机制 | 统一后的入口 | 消费者与保留差异 |
| --- | --- | --- |
| 连接几何、失效与双侧邻接 | `BlockConnectionSnapshot` / `BlockConnections` / `BlockConnectionGraph` | 建筑和载具共用；刚性根移动保留缓存，模块姿态、连接面、层/Trigger、根缩放或真实附加碰撞体变化时刷新。独立停靠机器人的碰撞体不计入模块结构。复杂层级仍回退实时几何。 |
| 连通分组 | `BlockGroupManager` | 同一BFS处理运行双侧邻接与已有几何入口；建筑从地面支撑筛选保留组，载具仍按驾驶舱或最大分量保留原根。 |
| 刚体参数与运动继承 | `GroupPhysicsSettings.ApplyTo` / `RigidbodyMotion` | 拆分前捕获源速度、角速度和质心，迁移后统一继承。Main建筑残骸使用PlayManager参数，独立WorldLab从源刚体获取配置。 |
| 八角点变换包围盒 | `BlockBounds` | 底座制作、驻军预览、世界入场、玩家流式保护、驻军激活和加载镜头复用；空输入、坐标空间、出生原点纳入、加载网格下限和旋转容错仍归调用者。 |
| 编辑动作的删除/恢复 | `BlockActionState` → `BlueprintBlockFactory.TryRestore` | Create/Delete只选择方向；保留稳定ID、精确世界姿态、尺寸、质量/碰撞和货物。创建撤销时捕获后续装入货物，删除保持删除时快照。 |
| 蓝图加载与删除 | `SaveManager.LoadBlueprint` / `DeleteBlueprintFile` | 玩家和敌方共用门控、去重、其他单位清理和加载协调；建筑删除后的继续选择/空目标策略保留，Siege工坊仅替换隔离预览。 |
| 输入推进器的物理步 | `Thruster.TickInputThrust` / `ApplyDirectionalThrust` | Main/Universal共用运行状态、输入、供电请求、变化率、施力和表现；方向性输入判定独立，Hover目标提交规则保留。 |
| 模块运输取消与货物释放 | `WreckSalvage.ReleaseContents` | 载具销毁、建筑模块销毁和残骸回收统一先处理部署机器人，再释放货仓；阵营掉落概率和一次性转换策略保留。 |
| 机器人起飞门槛 | `Bot.CanLaunchForWork` | 维修和采集共用请求/供电门槛；离仓后的导航、任务、返航及停靠不被机械合并。 |
| 操作轴与存档列表 | `BuildHandleAxis` / `SaveUIPanel.RefreshList` | 移动/旋转共享方向推断，三类存档共享列表构建；各自操作、名称/目录和界面选择规则保留。 |

删除无调用的销毁延迟刷新链、运行时补重命名按钮、BeamFade只写不读的颜色状态与无效参数。现有组件类型、已有GUID、Inspector字段、JSON结构与有效手工配置继续保留；新增源码配套 `.meta`。没有新增生产反射调度、万能管理器或与原主链并存的第二套连接算法。

DEVELOPMENT已收束为当前架构入口，旧逐日记录迁入 `Docs/DevelopmentHistory.md`；44张源码关联图及方法导航同步生成。用户维护的 `code-readability` Skill补充“先检索消费者、单一生产实现、显式策略差异、迁移并清理旧版本、至少两个真实消费者回归”的规则，未修改插件缓存或内置Skill。

## 已执行的静态与导入检查

- 项目固定SDK **9.0.318**，保留在 `C:/Users/12046/.codex/tools/dotnet-9.0.318`。全局默认裸 `dotnet` 仍选9.0.320，不能满足当前 `global.json`；本会话没有更改全局PATH或版本约束，验证只在进程内设置以下环境后运行命令。

```powershell
$env:DOTNET_ROOT = 'C:\Users\12046\.codex\tools\dotnet-9.0.318'
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
dotnet run --project Tools/CodeReadability -- --check .
dotnet run --project Tools/CodeReadability -- --duplicates .
dotnet build HY-Sandbox.sln --no-restore
```

- `dotnet run --project Tools/CodeReadability -- --check .`：340脚本，格式差异0、错误0（最终 `Temp/Reuse/check-final.log`）。
- `dotnet build HY-Sandbox.sln --no-restore`：0错误、4警告（最终 `Temp/Reuse/build-final.log`）；警告为Unity Pipeline引用冲突及既有弃用诊断，不写成无警告通过。
- 导航生成：44张图、340脚本索引。符号/文件存在检查通过不证明所有调用时序均已执行。
- 新增 `--duplicates` 只读检查：不少于50个token、忽略注释的完全同体生产方法候选为0组；排除Editor/Validation及独立Editor条件基准。工具自身3项检查通过。算法不归一化变量、不推断语义，0候选不等于没有语义重复。
- 新增源码已通过目标Unity Editor导入并用于下列真实Play探针；最终重新编译返回 `completed` / `failed=false` / `errors=[]`。探针使用Unity **6000.3.11f1**；没有以dotnet编译替代引擎验证。

## 已执行的Unity回归

下表来自本轮读取的真实报告。检查数量包含夹具与边界断言，跨套件存在覆盖重叠，不作为独立玩法场景数量。

| 套件 | 结果 | 已覆盖的主要契约 |
| --- | --- | --- |
| ArchitecturePlayProbe | 73通过 / 0错误 | 蓝图文件规则、逐帧恢复、取消/去重、坏文件保护、ID/货物/阵营、Hover控制及表现生命周期 |
| BuildingEditorPlayProbe | 73通过 / 0错误 | 真实输入进入建筑制作、空目录/显式创建、地基连接/尺寸、驻军预览、保存/切换、当前/最后建筑删除 |
| BuildPalettePlayProbe | 115通过 / 0错误 | 资源选择、分类、真实UI/连接预览、旋转目标、可复用操作轴、连接分组和耐久HUD |
| SalvagePlayProbe | 71通过 / 0错误 | 建筑断裂预算前掉货、一次转换、机器人运输/禁用/返航、货仓/存档及清理边界 |
| SharedMechanismChecks | 40通过 / 0错误 | 六处几何调用、旋转/缩放/空输入/原点差异、真实建筑双碎片运动、共享限幅、Create/Delete各3轮真实延迟Destroy及恢复 |
| SiegeBuildingGraphChecks | 48通过 | 几何失效、删除后的局部重探、复杂布局、层/Trigger/根缩放、连接根变化与冷/热路径；真实建筑无Connector装饰和载具装饰保留边界 |
| SiegeRuntimeConnectionChecks | 38通过 | 无视觉连接分组、显式断链、根刚性运动、相对运动、面开关、独立单位隔离及停靠机器人结构边界 |
| SiegeBuildingDamageChecks | 18通过 | 连锁范围伤害批次、一次支撑遍历、未来残骸先受伤、快照剔除与单发即时支撑 |
| PhysicsSandboxProbe | 33通过 | 真实PhysX法向耗散/动态质量分摊、有限冲量、角运动、爆炸保护及撞击致死爆炸 |
| ActivePowerPlayChecks | 32通过 | 按请求供电/停工清零、电网去重、输入推进器、货仓/维修/采集请求生命周期 |
| SiegeFleetChecks | 31通过 | 同工厂实体恢复、阵营/驾驶身份、自动支援输入隔离、质量/世界姿态与刷新身份 |
| SiegeTransientEffectChecks | 16通过 | 池租借/到期/重复归还、光束与闪光复用参数、闲置预算、场景卸载清理 |
| SiegeFoundationCacheChecks | 16通过 | 真实大型基地上缓存/直接支撑一致，根运动、尺寸、连接面与底座足迹变化的失效 |
| SiegeTopologyChecks | 12通过 | 真方块断链、连接面对称、碰撞层恢复、重连与模块排除 |
| SiegeBuildingChecks | 20通过 | 真实大型建筑基地的单连通体、独立核心、支撑/净空、原位修缮与舰队边界 |
| SiegeRegroupChecks | 19通过 | 双舰真实部署后重组，保留驾驶主体/姿态/源点速度/归属，局部缓存刷新、修缮回收、空组与无驾驶舱策略 |
| SiegeWorkshopChecks | 22通过 | 真实输入/指针打开工坊、异步加载与取消、撤销重做、舰队/受伤核心/驾驶者保留、退出与战斗门控 |
| WorldBuildingPlayProbe | 43通过 / 0错误 | 独立WorldLab真实输入/多实例/旋转、事务式存档保护、炮塔供电/命中、断裂残骸、真实帧地形脱锚下落、动态姿态恢复及重置 |


以上18组累计记录720条通过断言；完整检查名称、来源路径与源报告SHA-256保存在 [紧凑证据](2026-11-04-reuse-refactor-evidence.json)，不提交庞大Editor日志或临时用户数据。

连接图夹具验证了不变布局的无查询热路径、以及从8对增长到128对远处分组时一次局部删除仍为24次探测。这仅证明该夹具和输入下的工作范围，不等于正式战斗FPS、内存或最大规模性能改善。

## 保留的领域边界与验证缺口

建筑依旧拥有虚拟地面支撑、体素接触脱锚、残骸数量/寿命预算和快照边界；载具拥有驾驶舱选择、操控与组清理。建筑范围伤害队列和载具有界爆炸冲量是不同玩法策略，继续共用模块/耐久/连接/物理机制，不强行改成同一破坏规则。地形体素支撑不与模块Connector图混为一套数据结构。

本次没有证明整个项目不存在技术债，也没有完成所有关卡、输入设备、资源组合、存档版本与长时间自然战斗的穷举验收。正式Player构建、长时多波战斗、最大规模世界/舰队压力、GPU/PhysX成本归因和人工手感尚未形成本轮验收结论。既有独立算法基准用于对照，不纳入生产去重删除。

Siege重组夹具需要同时部署当前目录前两舰（价格2,225和3,685）；首次默认预算不足，部署前置失败。最终在全新Play中只把本次运行的 `Match.Coins` 调为1,000,000后重跑19项通过，未保存、未改生产经济，不能据此声称默认开局经济路径通过。工坊临时蓝图由探针清理。

WorldLab旧探针依赖原先内置目录初始蓝图，与现有唯一用户文件仓规则不一致，首次缺少夹具前置而失败。最终探针改为自包含的Temp文件仓和临时Catalog，在真实WorldLab运行43项通过并恢复原状态；没有重启旧内置回退规则，也未改写2026-10-15历史报告。端到端覆盖仍是受控验收路径，不能替代所有自然游玩组合。

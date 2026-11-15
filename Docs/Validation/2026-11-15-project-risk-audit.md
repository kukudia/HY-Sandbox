# 全项目风险审查快照 · 2026-11-15

## 结论与范围

完成全项目库存、配置/资产引用检查和关键行为链抽样审查；当前登记见[ProjectRiskRegister](../ProjectRiskRegister.md)：23项，2项P1、20项P2、1项P3。其中R01为静态确认缺陷，R02经真实类隔离调用确认；其他条目明确标注条件性风险或验证缺口。登记不等于已修复，本轮不变更运行时、Prefab或场景实现。

源码基线 `385754c`。工作区原有的 `Assembly-CSharp-Editor.csproj`、HoverFlightController.prefab、Random5v5Arena.unity与3个UserSettings文件保持原状并排除提交。资源审查采用当前磁盘工作区，竞技场含用户未提交调整，不能声称全部证据来自干净HEAD。

## 覆盖清单

全量枚举自有运行时286个C#脚本、Editor167个，共453个；扫描物理写入、磁盘写入、协程/异步、NonAlloc查询、运行时组件装配、全局注册表等信号。信号只用于选择深入调用链，**未逐行审查全部453个脚本，也未执行所有玩法/工具入口**。

| 域 | 运行脚本数 | 深入检查的主要链/边界 | 本轮未做的验证 |
| --- | ---: | --- | --- |
| Bootstrap | 3 | 启动服务、场景装配、管理器引用 | 全平台启动/退出、禁用Domain Reload |
| Bots | 9 | 维修扫描、回收任务/容量、移动Home与导航调用 | 密集目标公平性、长期多Bot竞争 |
| Building | 45 | UI新建→文件存储、加载预检→重建、增删写入→缓存 | 真实存档故障注入、所有蓝图/Bake入口 |
| Combat | 25 | 耐久启停、攻击者/重组、延迟清理、弹丸池与查询 | 所有伤害组合、长时间齐射和结算 |
| Diagnostics | 7 | 验证/探针装配与运行入口 | 全部诊断开关在Player中的成本 |
| Flight | 11 | 参考速度→推进方向/幅值→供电/喷口→刚体、悬浮 | 本轮没有新增飞行Play回归 |
| MainMenu | 7 | 工坊/游玩切换、保留场景和数据入口 | 快速重复进入/退出及各平台交互 |
| Navigation | 20 | 整舰查询、制动距离、A*/预测、Lab记录与实际指标 | 多轮5v5、低电/不对称/慢喷口组合 |
| Player | 4 | 输入依赖与模式切换、玩家控制服务 | 单独场景启动与卸载竞态 |
| Power | 8 | 注册→每渲染帧网络重建→物理执行器、退出清理 | 多物理步/渲染帧、供电波动压力 |
| Presentation | 30 | 新建/加载UI、VFX生命周期与池、调试显示 | 全画面/音频、活跃峰值和移动端 |
| Salvage | 7 | 掉落→认领/预留→部分交付、返航持久化 | 多大包竞争、跨域合并、失败重试Play |
| Siege | 38 | 出入机库、控制权/供电请求释放、战斗与返航边界 | 本轮未重跑完整战役/往返 |
| Simulation | 26 | 分组身份/保留根、远距清理与有效舰、生命周期 | 任意拓扑/任务舰离域组合 |
| World | 46 | 流式加载→异步构网→碰撞就绪、缓存舰体范围、快照版本 | 连续爆炸并发加载、取消压力与旧版数据集 |
| Editor工具 | 167（另计） | 制作/升级/验证入口库存，场景/资源工具与GUID契约 | 没有逐一执行全部菜单、批量重建用户资产 |

引擎/构建配置实际读取：Unity6000.6.8f1，URP17.6.0、Input System1.20.1、Pipeline0.7.0-exp.1、Test Framework1.8.0。启用构建场景为MainMenu/Main/Siege/Exploration/SiegeGameplay；Arena/Lab为诊断场景，未入构建列表本身不记为缺陷。CI当前只执行自有源码/工具检查，不等于Unity行为验证。

## 资产与隔离实验

1. 扫描Assets下4385份meta，未发现重复GUID；自有C#未发现缺少对应meta。文本脚本引用先按Assets候选筛查，再由连接Editor的 `AssetDatabase.GUIDToAssetPath` 解析，避免把Packages脚本误报为缺失。
2. 77种候选GUID中55种在Unity内解析为包资源；22种仍不解析，其中4种位于自有DefaultVolumeProfile，其他18种位于第三方演示资源。本轮未对第三方代码/资产做整理。候选数量不是缺失组件数量。
3. 当前DefaultVolumeProfile引用的无法解析脚本GUID为 `60f3b30c03e6ba64d9a27dc9dba8f28d`、`0fd9ee276a1023e439cf7a9c393195fa`、`5a00a63fdd6bd2a45ab1f2d869305ffd`、`74955a4b0b4243bc87231e8b59ed9140`；另有fileID0的测试组件序列化残留。URP GlobalSettings确实引用该Profile；Unity实际LoadAsset返回19个Volume组件、0个null，因此记录维护风险，不能称当前渲染损坏。未重存该资源。
4. 通过反射调用当前Assembly-CSharp中的真实 `BlueprintFileStore.GetPath`，传入临时目录 `Temp/ProjectRiskAudit/fixture/Saves` 及名称 `../escaped`，`Path.GetFullPath` 返回同级 `fixture/escaped.json`，证实目录越界。仅创建测试目录、计算路径，没有写入真实存档或越界文件。
5. 同次Editor查询 `isPlaying=false`、`scriptCompilationFailed=false`；未切换活动场景、未保存用户场景、未启动Play。

复查方法：重跑资产库存时必须同时解析Packages；路径实验使用临时目录并调用生产类，不复制实现。临时原始输出在 `Temp/ProjectRiskAudit/`，不作为需提交的日志依赖；以上摘要保存本轮可审查证据。GUID唯一不证明fileID、Inspector语义或所有资源运行效果正确。

## 已有保护与已修复问题

- 蓝图Loader已经先校验基本JSON形状、用版本与父节点归属阻止旧会话继续发布；R04讨论上层提前清理，R05讨论更深数据约束，不否定这些保护。
- 返航货仓保存使用临时文件/备份，写失败返回false并保留会话；LootDrop部分交付会保留余包。不能将所有File.Write或amount变化都当作裸写/丢货。
- WorldLab持久化已有大小、编码与写入保护；流式网格有异步版本/释放保护。剩余风险在版本接受范围、全局修订导致重试和局部范围缓存。
- 清理回调已有runtimeUnitId复核，普通残骸路径核对无舱；远距路径策略更宽，故单独记录R08。
- 供电最后一个中继退出会Clear；悬浮/机库有释放与恢复；对象池有空闲预算。这些保护不等于所有时序/活跃预算已覆盖。
- 先前推进方向/幅值混用已在 `385754c` 修复。此前Type03专项实测约30.90s/184.57m变为21.08s/119.16m，实际喷口直航角差显著降低；这是[上一轮证据](2026-11-15-propulsion-input.md)，不是本轮重测或所有舰型保证。后续R06/R07/R19/R21关注剩余边界，不把已修复旧问题再次登记为当前缺陷。

## Skill治理

主Skill由 `code-readability` 更名为 `engineering-quality`（工程质量与行为验证）：保留架构、格式、意图注释、源码关联运行图，新增 `references/behavior-contracts-and-risk.md`，将“生产者→转换→消费者→用户可见结果”、单位/坐标/方向幅值/时序/所有权、失败恢复及提交前风险复查纳入流程。避免把一次飞行问题的阈值变成所有项目硬规则。

本机主入口：`C:/Users/12046/.agents/skills/engineering-quality/SKILL.md`。原code-readability与game-code-readability保留兼容跳转；用户自有Unity/UE/连续开发等Skill引用迁移到新入口。`.agents`及`.codex`的Unity可编辑Skill补充物理/控制链真实执行验证。未修改内置Skill、插件缓存或项目运行时组件；`Tools/CodeReadability`与既有CI工具名保留。

修改前外部配置备份：`C:/Users/12046/.codex/backups/engineering-quality-20261115-234610-590723/manifest.json`，包含原路径、唯一备份和SHA256清单。外部Skill不属于本项目Git，项目提交只保存规则引用和审查结果。

## 验证与交付状态

本轮始于11月15日，收尾检查于11月16日完成。范围为文档/规则与外部Skill；本轮不改运行时源码，RuntimeFlow/CodeIndex现有函数位置不受影响，不无理由重新生成。

- 已执行：全量库存/引用候选筛查、Editor GUID解析、真实存储类临时路径实验、Volume只读加载。
- `dotnet run --project Tools/CodeReadability -- --check .`：453脚本，0格式差异、0错误；长文件仅审查信号，不是失败。
- `dotnet build HY-Sandbox.sln --no-restore --nologo -p:UseSharedCompilation=false`：0错误、36警告，含框架/程序集引用及废弃API警告；这仅是当前工作区静态构建，不是Unity Player构建。
- 新Skill、旧兼容入口及两份Unity可编辑Skill均通过skill-creator的quick_validate；新增文档和DEVELOPMENT相对链接均存在。
- 全工作区 `git diff --check` 被原有UserSettings布局尾空白阻断；这些无关文件保持原状。本任务文件单独检查通过，不把全工作区检查写为通过。
- 远程同步阻塞：两次 `git fetch origin` 均无法连接github.com:443；本地main与缓存origin/main在提交前为0/0，但缓存不是实时远程证据。保留本地提交，网络恢复并确认实时远程差异后再推送。
- 未执行：全项目Play、全部Editor菜单、Player/平台构建、真实玩家存档破坏性测试。上述风险后果的未测项保留在登记中。
- 推荐处理顺序：R01/R02数据边界 → R03/R04事务/加载 → R08有效舰生命周期 → R06/R07/R21真实控制效率 → 按后续功能触及范围处理其他ID。审查通过不代表这些风险已经消除。

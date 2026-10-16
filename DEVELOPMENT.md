# HY-Sandbox 开发与审查入口

## 2026-10-16 用户调整：敌机与匹配底座组合

- 据点改为敌机配套停机底座，建筑编辑器新增 `Create matching base for selected enemy`：根据所选敌机模块轮廓创建可编辑结构，完整网格/原材质与底座同时预览，组合JSON包含敌机快照及姿态。已有同名作品保留，新建自动加序号。
- Finch、Pike、Manta三套底座已导出至用户 `BuildingBlueprints` 目录，尺寸28×26、28×30、36×22m，替换项目内三套旧外围驻军模板并保留GUID。原28份用户JSON哈希不变；用户既有普通建筑、敌机源与六份工程/配置/布局差异均保留。
- 已加载据点先显示底座上的停机敌机，近处按同姿态启用真实实体，不再从外围空中部署；EnemySpawner继续停用。击败只在当前出征有效，重访不复活。远处显示没有伤害碰撞，暂不支持未激活目标的远射。
- Main真实隔离Play 61项通过，六套内容连接、三套净空/完整模型通过，实际组合画面已检查。独立D3D11 Development Player组合路线68类通过、errors为空、正常退出：Manta编辑保存/重载与Finch自然战斗、追击/脱离、受损休眠恢复、击败后重访及会话清理。过程中修复敌机锁定隐藏原载具的问题，索敌现在排除禁用对象。源码256脚本/30图、静态编译0错误/4既有警告通过。操作、运行证据及失败记录见[据点制作](Docs/OutpostAuthoring.md)与[本次验证](Docs/Validation/2026-10-16-matching-outpost-bases.md)。

## 2026-10-16 夜间：据点多轮稳定性与整批回收

- 普通D3D11 Player完成10次正式据点出征及隔离返航/死亡重生，75类检查通过。另以`-world-smoke-outpost-salvage-all`完成3次整批回收，78类检查通过，每轮8个原始模块均观察到机器人携带并入仓；退出后无活动或隐藏敌机/掉落实体，28份用户JSON哈希保持。
- 整批验收按自然战斗已有掉落增加真实测试货仓和中继，避免8格夹具先被其他残骸占满；生产容量/寻物规则不变。冻结夹具取消延迟刷新，重跑运动学刚体警告为0。Ready对象数13957/13909/13957，分配内存133.70–134.00MB；此受控路线不代替人工驾驶或GPU性能验收。见 [证据与范围](Docs/Validation/2026-10-16-outpost-endurance.md)。
- 新增移动目标追击路线：正式驻军追踪2m/s移动玩家，离开检测半径后释放目标，返回后重新索敌；67类Player检查通过。

## 2026-10-16 夜间：独立Development Player据点制作

- 增加独立Development制作验证包入口，不改变普通发布包的制作权限。显式`-world-smoke-outpost-editor`使用隔离缓存文件仓，真实快捷键进入/退出、点击模板复制和姿态保存、重载部署数据并检查构建后的Shader/预览清理。
- D3D11 Development Player 30类检查通过、errors为空、正常退出；包含编辑后恢复原载具及世界跨列/退出再准备。此包不作为发布性能比较。见 [独立Player操作证据](Docs/Validation/2026-10-16-outpost-editor-player.md)。

## 2026-10-16 夜间：据点驻军部署预览

- 等大小小方块标记替换为蓝图模块实际尺寸的线框、总体范围、朝向箭头和镜头朝向标签；选中驻军显示模块细节，其他驻军只显示范围。状态区显示尺寸，橙色保守范围重叠警告帮助调整净空，保持可保存。
- 预览只有显示组件，跟随蓝图局部根；切换槽位与退出编辑器释放临时Mesh/Material。Main真实隔离Play的编辑器51项通过，725×488实际画面已检查；不称独立Player面板验证。见 [验证与占用检测边界](Docs/Validation/2026-10-16-outpost-preview.md)。

## 2026-10-16 夜间：据点金币与科技收益

- 正式强据点实测发现金币/科技仓沿用敌方载具的内容不掉落开关，击毁后原有收益被丢弃。CargoHold现在识别所属世界建筑并释放实际存量；普通载具继续遵循阵营配置，正常卸载/退出不产生掉落。
- 新增显式`-world-smoke-outpost-salvage`路线，观察实际据点货仓释放、合并金币包、分类吸附及机器人真实拾取/停靠入仓；退出重新准备时同时检查据点进度清空。完整回收Editor71项通过，D3D11 Player同14741种子两次出征并返航/死亡重生75类通过，28用户JSON未变。复现、运输夹具修正与完整Player证据见 [资源收益验证](Docs/Validation/2026-10-16-outpost-cargo.md)。

## 2026-10-16 夜间：三驻军强据点自然交战覆盖

- 正式seed14741生成Supply Stronghold Outpost，逐名观察Pike、Manta、Finch的真实索敌、悬浮输出、物理移动和武器伤害归属，避免仅用首名驻军代表整个编队。生命周期夹具冻结时同时关闭悬浮控制器，避免向运动学刚体写角速度的验证噪声；生产玩法不变。
- D3D11独立Player的三驻军交战、受损重组、远处休眠、追击恢复、击败后重访和重新准备共51类通过，用户JSON保持不变。探针保留在同一功能文件内，共用既有据点路线；运行时函数图未变。见 [三驻军证据与范围](Docs/Validation/2026-10-16-outpost-stronghold.md)。

## 2026-10-16 夜间：重组保持世界坐标与据点战况排版

- 多种子实测捕获插值刚体重组问题：新组先在Prefab制作坐标创建，再移动Transform，下一渲染帧可能恢复旧插值姿态，将存活驾驶舱移回制作坐标附近。RuntimeGroupBuilder现在直接在目标世界坐标创建新组；据点部署抬升同样写Rigidbody.position，保持物理/显示姿态一致。
- 据点HUD改为读取结构化最近据点快照；显示名称/米数、剩余驻军与炮塔、会话清剿统计。名称按实际字形宽度省略，保留组合字符边界；72像素三行区域、禁用富文本并添加暗色阴影。Main真实世界725×488画面检查了120宽字符名称及清剿状态；本轮隐藏Player截图为黑图，没有作为视觉/GPU性能证据。
- 1080p D3D11 Player seed14722路哨据点40项通过；seed14721双塔据点叠加真实战斗回收、返航/死亡重生66项通过，两轮均errors为空且正常退出，28份用户JSON未变。249脚本/27图、静态编译0错误/4个既有警告、Windows构建0错误/1警告。见 [复现、排版与验收边界](Docs/Validation/2026-10-16-outpost-pose-hud.md)。

## 2026-10-16 夜间：据点外追击恢复

- 独立Player复现追击驻军在原据点外休眠后，玩家靠近它却无法恢复：原代码仍只检查原据点激活半径。现在休眠驻军按最后位置160m内恢复，与180m休眠距离形成缓冲；首次部署也检查部署点距玩家，防止远端反复创建/休眠。
- 修复后同seed14721正式Player35类通过；追加真实驻军自然武器命中与攻击归属后36类通过、errors为空、进程正常退出。249脚本与静态编译通过，Windows构建0错误/1警告，用户28份JSON未变。见 [复现和边界](Docs/Validation/2026-10-16-outpost-pursuit.md)。

## 2026-10-16 夜间：建筑编辑器敌人据点与会话内有限驻军

- 建筑编辑器新增 Enemy outpost：可从三套既有敌机蓝图选择驻军、添加/移除槽位、保存本地部署位置与朝向，并把模块快照嵌入建筑蓝图；Copy template 会生成独立用户文件。普通建筑与据点继续使用原建筑候选概率10%。
- 生产运行停用 EnemySpawner 自动计时刷新，由 OutpostEncounterDirector 按距离、地形列和部署包围盒逐台生成，最多16台。远离区块时保存活敌机模块、耐久、货物和驾驶舱身份，重访恢复原对象身份；驾驶舱死亡才消耗该槽位。击败记录只存在当前探索会话，返航/死亡后重新出发可重新生成，不写永久击败文件。
- 新增 Field Workshop、Abandoned Storehouse、Relay Ruins 三套普通建筑，以及 Road Watch、Relay Station、Supply Stronghold 三套据点，使用结构尺寸Prefab、既有模块和三套敌机快照。六套模板真实恢复后全模块连接、结构模型尺寸/连接网格、炮塔供电和据点标记均通过；预览见 `Temp/OutpostContent`，详细说明见 [据点制作](Docs/OutpostAuthoring.md)。
- BuildingEditorPlayProbe 45项通过；OutpostContentChecks 六套真实Play结构/供电检查通过；D3D11非Development Player seed14721 据点路线通过32项，组合战斗回收/返航/死亡重生路线57项通过。Windows构建0错误/1警告；249源码/27函数图、静态编译0错误/4个既有警告及diff通过，28份用户JSON未变。详见 [验证证据](Docs/Validation/2026-10-16-outpost.md)。

## 2026-10-15 用户调整：法向耗散碰撞伤害

- CollisionDamageResolver按法向相对速度及求解器冲量计算有限能量，读取双方PhysicsMaterial及bounceCombine，扣除弹性反弹保留的能量；动态双方按质心能量份额分摊，轻物体受到更大速度变化。接触点和复合碰撞回调不重复使用整机质量；擦碰的切向速度不计入撞击伤害。
- 25份方块CSV/真实Prefab统一为2m/s起伤、0.1耐久/J、倍率1、单次上限100%、冷却0；严重撞击可一次摧毁，独立撞击不再被0.5秒冷却吞掉。参数仍可在CSV/Inspector标定，保留已有字段和GUID。爆炸的零直接伤害、速度上限和碰撞保护保持。
- Main真实Play的25项隔离PhysX检查通过：双接触模块10kg以10m/s撞墙合计48耐久，20kg翻倍，20m/s为4.125倍；1m/s无伤，切向速度不增加撞伤，完全弹性反弹无永久伤害，恢复系数0.5为75%损伤；10/30kg真实对撞的损伤比例3:1，总预算36耐久。详见[验证与标定边界](Docs/Validation/2026-10-15-realistic-collision.md)。这是刚体能量近似，不包含材料变形、应力传播或经实测标定的车体结构。

## 2026-10-15 夜间：返航失败可见原因与重试提示

- 返航保存失败不再只写Console：Main新增真实可编辑Return Status状态栏，持续显示保存未完成、当前会话保留，以及恢复缺失/损坏存档或处理文件访问后重试的提示；不抢焦点、不挡射线，成功返航自动清除。保留原SaveReturnCargo(bool)调用契约，通过新增out原因重载向UI传递说明。
- Main真实Play四类故障与提示/非阻挡/清除共16项通过；725×488 Game视图实际检查通用长提示无溢出（文字需求72.67、可用84逻辑高度）。D3D11/720p独立Player返航/死亡/再出发29类通过，包含成功返航清除旧提示；Windows构建0错误/1警告，239源码、26函数图、静态构建0错误/4个既有警告通过，28份用户JSON未变。见 [报告及失败尝试](Docs/Validation/2026-10-15-night-return-status.md)。

## 2026-10-15 夜间：三轮组合Player回归

- 同一独立D3D11 Player叠加敌机恢复、输入战斗回收、自然Finch交火和末尾返航/死亡重生路线，固定14721完成3/3轮、58类检查、errors为空且正常退出；28份用户JSON未变。三轮内存采样139509983→139631567→139655055字节，短时趋势不等于无泄漏证明。见 [报告](Docs/Validation/2026-10-15-night-player-combined.md)。各夹具分阶段运行，不冒充一次完整动态驾驶巡航；本单元无新生产代码改动。

## 2026-10-15 夜间：独立Player返航写盘、死亡重生及再出发

- 显式 `-world-smoke-return` 在一般会话检查后切换唯一缓存目录文件仓，经生产Loader加载相连的驾驶舱/货仓；实际UI正常返航写盘和重载，第二次出发保留货物，销毁驾驶舱后通过真实重生按钮清货、重载、第三次出发及再次返航，最后只读恢复原载具。原子备份与文件内容实际检查，未覆盖用户存档。
- 非Development Windows构建0错误/2警告；D3D11/720p/seed14721 Player一轮28类检查通过、errors为空，进程正常退出，28份用户JSON哈希不变。237脚本、静态构建0错误/4个既有警告、工具与diff通过。见 [报告](Docs/Validation/2026-10-15-night-player-return.md)。货物为夹具恢复内容，载具固定用于持久化检查；此前真实战斗回收与本路线是分别验证，尚未合为一次完整动态巡航，不宣称P1整体完成。

## 2026-10-15 夜间：返航存档丢失时保留会话

- 复现命名存档文件不存在时SaveReturnCargo错误返回true，UI会继续清理载具/世界并丢失本次未写回货物。现在将缺失命名存档视为保存失败，保留会话；路径解析/目录创建也进入同一异常边界。未选存档的原行为、几何保留和原子备份不变。
- Main真实世界游玩中隔离文件仓，调用正式MainUIPanels.PlayEnd进行四类故障（丢失、坏JSON、独占锁、目录被文件占用），会话/载具均保持；正常写盘、原文件备份、全部几何及临时文件检查共10项通过。236脚本、Python工具、静态构建0错误/4个既有警告及diff通过；28份用户JSON未变。见 [证据](Docs/Validation/2026-10-15-night-cargo-return-files.md)。本单元为Editor真实Play及同步磁盘验证，尚未跑独立Player返航/重生路线。

## 2026-10-15 夜间：既有敌机自然飞行与单向交火

- 显式 `-world-smoke-enemy-flight CODEX_E01_Finch` 只读取现有蓝图、保持原AI/动力/武器参数；动态敌机在真实Player中自然索敌、供电悬浮、移动并伤害固定高耐久玩家靶。30秒内最大位移22.50m，驾驶舱保持有效；不直接施加伤害、不禁用AI或固定敌机。
- D3D11/720p/seed14721、建筑10%配置下，一轮26类检查通过且errors为空，跨区块与退出重新准备通过。Windows构建0错误/1警告；235脚本可读性、静态构建0错误/4个既有警告、导航与diff通过。首轮夹具未等ControlUnit启动导致空引用，已改为等真实生命周期；见 [报告与限制](Docs/Validation/2026-10-15-night-enemy-flight.md)。这不是双向驾驶、玩家返航写盘或长期压力验收，P1仍未整体完成。

## 2026-10-15 用户调整：相机无碰撞、建筑10%

- Main相机第三人称碰撞层设为None（0），新CameraController默认值同样为0；碰撞解析直接返回期望位置，不再做SphereCast或墙前收回。保留原缩放/跟随/镜头参数，Inspector仍可配置碰撞层。源码调用链没有改变，函数图仍适用；旧墙体回收断言已同步为关闭后的行为。
- 世界生成资产与新配置默认建筑候选概率从35%降为10%；这是每个候选位置概率，不保证任意视野恰好10%覆盖。用户已有建筑蓝图不变，新会话生效。
- Unity Main真实Play检查：collisionDisabled=true、wallDoesNotRetractCamera=true、会话BuildingChance=0.1；Main保存后退出Play。静态构建0错误、4个既有警告，源码格式及diff检查通过。此为配置/同步方法检查，不声称已完成镜头人工手感或10%新种子的大规模Player性能验收。NightDesk按本轮原截止重新启动，已核验黑屏与system+display请求；保留Esc接管。

## 2026-10-15 夜间：损坏敌机文件不再中断蓝图库

- 在真实InitializeEnemyBlueprintPool复现：一份截断JSON抛ArgumentException，中断后续敌机初始化。现在逐文件隔离JSON解析与IO/访问异常，警告后继续有效条目；不修改、删除或覆盖原文件，不吞掉其他编程异常。
- Main隔离Play回归6项通过：截断JSON、被独占锁定文件、空/空模块蓝图被跳过，有效同级文件均保留，初始化完成且再次调用不重复加入，文件内容与SaveManager状态保持。见 [证据](Docs/Validation/2026-10-15-night-enemy-pool.md)。这是同步真实生产入口验证，本单元未另跑Windows Player或目录权限故障。

## 2026-10-15 夜间：Player 输入、炮弹击毁与机器人入仓

- 新增显式 `-world-smoke-combat` 受控Player路线：Input System W键经启用的ControlUnit、真实电网和MainThruster推进动态刚体，松键清空意图；随后固定平台，以现有实体炮塔自然索敌/发射击毁携货敌舱，经过生产残骸转换、机器人拾取、真实返航停靠和供电货仓入库。没有直接调用伤害、手动供电或直接Store伪造结果。
- D3D11/720p/seed14721独立非Development Player完成一轮、28类检查且errors为空，随后跨区块、退出及再次准备通过；233份源码检查与静态编译通过（0错误、4个既有警告）。见 [证据及限制](Docs/Validation/2026-10-15-night-player-combat.md)。此为验证能力与证据补全，未改变正常游戏算法；敌方AI关闭、稳定平台不等于完整双向驾驶交战，尚未覆盖Player返航文件保存/死亡/再次出发。

## 2026-10-15 夜间：敌方蓝图完整恢复

- 修复敌机恢复尺寸后仍沿用 Prefab 碰撞体与质量：CSV Awake 后刷新实例几何、连接点和自动质量，再汇总整机质量。生成前校验所有模块资源及唯一驾驶舱，缺失/空模块、无驾驶舱或多驾驶舱均拒绝整机，避免发布残缺敌机。
- 相同独立D3D11 Player路线修复前碰撞体断言失败，修复后固定14721两轮会话、24类检查通过，errors为空；Windows非Development构建、232份脚本检查和静态编译通过（0错误、4个既有警告）。显式 `-world-smoke-enemies` 不影响正常启动、不写用户蓝图。见 [验证与边界](Docs/Validation/2026-10-15-night-enemy-restoration.md)。本次为跨帧生成恢复验证，尚未覆盖真实驾驶交战和损坏JSON文件解析。

## 2026-10-15 夜间：携货建筑卸载安全释放

- 真实起飞携货后复现了流式卸载错误：先停用建筑父层级，LootDrop.OnDisable 再试图将携货物移出 Cargo socket，触发 Unity 禁止在父对象启停过程中重挂接。建筑现在在单栋卸载、批量清理、世界根停用/失败前先取消运输与释放认领；仓内货物不洒落、不抽取奖励，快照仅保留已入仓物品。
- 完整回收 Play 回归 71 项通过，增加实际起飞/携货、断裂、单栋卸载、根层级停用及恢复不重复入仓；世界边界 23 项通过，无 Unity Error/Exception；D3D11 Windows 重建成功，固定14721两次一般会话烟测通过（不含Player携货/交战）。见 [验证与边界](Docs/Validation/2026-10-15-night-carried-unload.md)。这不等于完整 Player 人工驾驶战斗验收。

## 2026-10-15 夜间：回收全量探针适配场景模拟门槛

- 隔离 Main 后，旧探针禁用了全部 ControlUnit，导致 LootDrop.Update 的真实场景模拟门槛关闭。探针现在使用启用的中立运行单位提供模拟环境，避免读取玩家输入或启动敌机 AI；同时检查启停单位会切换场景模拟状态，未修改生产回收算法。
- Unity 6000.3.11f1 / Main / NightDesk 后台，完整真实 Play 62 项通过、errors 为空：包含本轮建筑 11 项、货币溢出转仓、机器人认领/运输/毁仓释放、隔离临时存档的返航/死亡持久化、残骸清理和移动母体返航。见 [完整报告](Docs/Validation/2026-10-15-night-salvage-full.json)。这解决前一专项报告记录的旧探针散射失败；仍非真实输入驾驶/完整 Player 巡航验收。

## 2026-10-15 夜间：建筑断裂接入回收

- 修复断裂模块被禁用/限时删除、绕过回收而吞掉货仓的缺口。WorldBuildingManager.CreateDebris 在重挂接和残骸预算判定前调用现有 WreckSalvage，保留原阵营与现有掉落概率；主体快照不再包含断裂模块，重复转换不重复奖励。普通流式卸载/恢复与退出不触发掉落。
- 建筑专项真实 Play 11 项通过：实际 Durability 伤害触发断裂、正常/零残骸预算、货物释放、敌方掉落、幂等、玩家无刷币、快照重载及会话清理；修复前在相同断裂货仓断言失败。见 [验证与限制](Docs/Validation/2026-10-15-night-building-salvage.md)。这不是手动驾驶/完整 Player 战斗返航验收。

## 夜间自主开发规划

每日 08:00（Asia/Kuala_Lumpur，UTC+8）收尾的路线、阶段验收和首晚工作顺序见 [夜间开发大纲](Docs/NightDevelopmentPlan.md)。本轮已启动 NightDesk 与当前聊天续跑，截止 2026-10-16 08:00，07:30 收尾；用户明确授权主要功能验证完成后推送 main。后续阶段内容仍为待开发，不代表已实现。

### 2026-10-15 夜间基线：悬浮物理时序回归

完整 ArchitecturePlayProbe 的旧断言只调用控制器 FixedUpdate 就要求实际推力立即改变，与现行推进器独立限速契约不符。HoverControlPlayChecks 现分别驱动控制器和推进器回调，验证无驾驶舱时不转向、推进器失效归属清零、供电后目标提交、缺电/控制器禁用后的单步衰减与最终归零、退出游玩及推进器禁用清零、归属转移保护；生产飞行代码未改。完整探针本轮 completed/passed，73 条检查，0 错误；报告见 [夜间悬浮回归](Docs/Validation/2026-10-15-night-hover.json)。悬浮部分为 Play 内显式驱动生产回调，不代表真实驾驶或自动物理顺序验收。dotnet build 0错误、4个既有警告。

### 2026-10-15 夜间基线：后台性能采样

WorldSessionPerformanceChecks 修复遮罩持焦点后第2帧停滞和无新帧时超时无效的问题：临时后台真实运行，完成/中止/超时均恢复原后台及Profiler设置，启动校验场景与报告名。完整路线在NightDesk下推进1830真实帧，主动退出和停帧超时路径均已验证恢复；Main不脏，用户数据保持。固定种子14721、28栋/1967模块的D3D12 Editor移动P95 27.69ms、最大81.31ms，退出单帧55.64ms；这是工具基线，不是生产优化或D3D11 Player性能结论。见 [报告与限制](Docs/Validation/2026-10-15-night-background-performance.md)。

## 2026-10-15 Windows启动修复与区块深度优化

2026-10-15夜间补充：显式WorldPlayerSmokeChecks新增固定种子、1–10轮会话和P99/尖峰/每轮内存记录；每轮等待世界与原载具恢复再重进，不写玩家数据。D3D11独立非Development包seed14721连续3轮通过、进程退出码0；28栋/1967模块，移动P95约9.46–9.49ms，首次峰值97.87ms、退出约60ms仍存在；无生产算法优化或长期无泄漏结论。cycles0按预期失败退出；28份用户JSON哈希未变。配置、复现和验证边界见 [重复Player报告](Docs/Validation/2026-10-15-night-player-repeat.md)。

- 两份旧Player日志记录D3D12原生资源处理崩溃；同一旧包D3D11对照可响应。Windows发布配置显式使用D3D11，新Release包已实际启动、加载外部建筑蓝图库、进入游玩、跨区块移动200m、退出清空并再次准备，未出现C#错误或原生崩溃。
- 建筑候选概率45%→35%；保留全部有效用户建筑蓝图、自然出生与600m视野。启动用现有可编辑面板显示载具/世界组合进度，载具恢复至95%后完成连接重建才到100%，两者完成开放建造；游玩进入继续使用原有遮罩。
- 区块一格halo快照只在主线程读取；后台任务做同材质贪心面合并，主线程上传16位/必要时32位索引和烘焙碰撞。提交检查请求/数据版本，取消丢弃结果。实时破坏重建加入毫秒预算；建筑休眠按毫秒分批，地形逐列卸载；静止满载减少重复扫描。WorldLab/Bake和显式同步FlushDirty保留。
- 相同种子14721、建筑概率0.45的162块网格对照：98,686→16,162三角形（-83.6%），197,372→32,324顶点；同步单次微基准135.10→83.74ms，包含采样/Mesh上传，不含PhysX，不能视作整局FPS提升。20,267项网格断言、Main及23项边界检查、WorldLab数据12项/破坏碰撞16项通过。实际Player和剩余峰值以[本轮报告](Docs/Validation/2026-10-15-world-player-performance.md)为准。
- 新包：`Builds/WorldPerformanceValidation/HY-Sandbox.exe`，与发布相同的非Development Windows配置；不覆盖旧Build或递增版本。图形驱动/其他显卡、长时间战斗、最大载具与4K性能仍未验收。源码函数图与研究来源见报告及[世界说明](Docs/WorldStreaming.md)。

## 2026-10-15 Main 大世界卡顿与加载遮罩

- 定位并移除数千个Durability独立OnGUI的隐含GUI开销，改为DebugManager装配单个标签绘制器；炮塔空目标遵守搜索间隔。固定种子33栋/2271模块，D3D11 Editor Profiler静止帧中位约30ms降至8ms，末尾100帧主线程均值30.57ms降至8.54ms；测量条件及局限见[性能报告](Docs/Validation/2026-10-15-world-performance.md)，不等同于正式Player性能。
- 建筑资源异步预热，创建/启用按WorldGeneration.asset的默认2ms预算分批；进入先显示不透明进度遮罩并暂停物理，完成/退出/死亡/失败恢复时间。途中缺地形按预测路径真实完成比例显示进度。远景复用网格/高度缓存，同父刚性装饰合并共享网格，保留炮塔及机械运动；大载具末尾连接重建也按预算跨帧。
- 已验证Main 53项、边界23项、WorldLab建筑43项/数据12项及炮塔回归；实际查看遮罩与建筑近景；Unity导入编译、228脚本格式/函数图导航、dotnet build（0错误/4个既有警告）、diff检查通过。正式Player、高分辨率、长时战斗和D3D12稳定性未验收。

## 2026-10-15 Main 大世界与建筑会话

- 本次修正：Main生成池改为读取persistentDataPath/BuildingBlueprints全部有效JSON（当前base1/base2/default_building），平台自动适配尺寸；出生区保留自然地形，按载具范围内最高地面抬升6m；近处加载4列半径/卸载5列半径，加入无碰撞低密度远景和默认270–600m距离雾，退出恢复建造环境。参数保存在WorldGeneration.asset，远景网格按列版本分帧更新。D3D11实际检查远景连续且渐隐；Main检查49项、边界23项回归通过；225份脚本格式/导航、dotnet build（0错误/4个既有警告）、diff检查通过。见[本次报告](Docs/Validation/2026-10-15-world-session-followup.json)与[视距画面](Docs/Validation/2026-10-15-world-session-distance.png)。Player/长时性能与D3D12稳定性仍未验收。

- Main建造时隐藏分帧预热地形和真实Buildings，Play等待Ready后显示世界、启用模拟并安全放置玩家；等待面板可取消。默认约4.1km有限大世界，81列预热，附近按列流式加载/卸载，不分配全图体素数组；生成Unity对象仍在主线程协程执行。
- 同次探索内地形差量、建筑损坏、剩余模块与货仓随区块休眠恢复；活动建筑预算满时延期实例化。玩家缺地形时暂存速度冻结、前方就绪再恢复。退出与死亡丢弃全部世界状态并重新准备，正常返回保留原有货仓转存流程。
- 真实可编辑入口为 `Assets/World/WorldGeneration.asset`、`WorldSession.prefab`、Main加载面板及现有WorldLab建筑目录；菜单可重建缺少资产而保留已有参数和布局。使用与限制见 [WorldStreaming](Docs/WorldStreaming.md)。WorldLab仍保留独立有限地图与快照流程。
- 已验证：Unity导入/编译；Main真实Play专项42项、边界23项通过，包括正负区块、退出/死亡清空、取消、损坏/货仓休眠恢复、81列预算和4000m/s真实物理步保护。WorldLab数据12项、建筑43项回归通过；223份脚本格式/函数图检查、dotnet build（0错误、4个既有警告）、diff检查通过。已观察地形和建筑总览；细节截图期间Editor发生D3D12 GPU等待超时并退出，临时以D3D11重新打开后完成加载面板与建筑近景检查，截图见Docs/Validation。未修改项目图形API设置；D3D12稳定性、正式Player构建及长时大地图性能未验收。

## 2026-10-15 WorldLab 建筑融合

- WorldLab新增真实建筑目录、Outpost模板和初始据点；读取BuildingBlueprints，B进入放置、Q/T旋转、左键确认、Esc取消。预览只复制Mesh且屏蔽采掘。按用户要求，WorldLab实例和预览不生成地基模型、碰撞体或底座方块，保留编辑器底座及尺寸元数据。建筑保留1m模块，地形保留2m体素；支撑、穿插和重叠校验失败拒绝放置。
- ControlUnit共用运行身份与战斗注册，建筑无需驾驶舱，也不加入Main载具清理。供电、激光与实体炮弹、货仓及机器人运行门槛已适配；方块死亡从虚拟地面连接点检查Connector连通，断裂组生成有界残骸；虚拟范围低于默认50%地形支撑时整栋脱锚。
- F5/F9保存/校验体素差量与建筑完整快照，包含尺寸、货仓、耐久和剩余模块；缺失资源或坏数据保留当前世界，兼容旧纯体素差量。动态主体恢复姿态但速度归零，残骸和飞行运输任务不持久化。使用、Inspector参数和边界见 [WorldBuildings](Docs/WorldBuildings.md)，源码图见 [RuntimeFlow](Docs/RuntimeFlow.md)。
- 已验证：Unity6000.3.11f1导入/编译；融合专项43项通过，覆盖生产Input System输入、三实例旋转、尺寸与耐久/货仓JSON恢复、异常保护、供电战斗、独立场景炮弹跨帧飞行、无实体地基、断裂、地形挖空真实帧下落、旧存档与重置。建筑编辑35项、建造目录102项、炮塔28项、供电/货仓/机器人工作请求32项、机器人真实物理导航11项回归通过；结果见 [专项报告](Docs/Validation/2026-10-15-world-buildings.json) 与 [回归报告](Docs/Validation/2026-10-15-world-buildings-regressions.json)。213份自有脚本格式/源码导航检查通过；dotnet build 0错误、4个既有警告。实际画面已检查。正式Player构建、大规模性能、机器人跨帧任务及玩家载具进入WorldLab尚未验收。

## 2026-10-15 开发者建筑蓝图编辑

- 开发者模式（Unity Editor / Development Build）下，`Ctrl+Alt+E` 进入/退出敌人编辑；`Ctrl+Alt+F` 进入/退出建筑编辑，支持左右 Ctrl/Alt。两种蓝图模式可直接互切；相同快捷键再次按下返回玩家存档。普通 F 聚焦不会同时触发。
- 建筑蓝图保存到 `Application.persistentDataPath/BuildingBlueprints`，列表、新建、复制、重命名、删除与文件大小均使用独立目录。底座尺寸以 `foundation.width/length` 跟随 JSON 保存；旧存档缺少该字段仍可读取。建筑不生成/允许放置驾驶舱，编辑时隐藏玩家 Play 入口。
- 底座源为 `Assets/Resources/BuildingEditor/Foundation.prefab`，默认 4×4、每格 1m，薄平面顶部为世界 y=0，每格中心一个向上 Connector。固定网格左下角为 (-2, 0, -2)，右下角尺寸面板支持 1–64 的整数长宽，点击 Apply size 自动保存。尺寸变化保留所有现有方块及坐标；缩小可使边缘方块失去底座支撑，扩大后重新检测连接，不删除方块。尺寸调整会清空当前撤销历史。
- 底座不加入普通块存档或撤销栈，禁止选择、删除、移动和旋转；现有放置预览、连接提示及连接检测可直接使用。薄底座碰撞采用与普通块相同的面内缩间隙；实例恢复使用世界坐标，不继承父 Prefab 的已有偏移。加载替换时先停用旧父对象，再延迟销毁，避免同步恢复误连旧碰撞体。
- 尺寸面板源为 `Assets/Resources/BuildingEditor/EditorPanel.prefab`，与底座、网格材质/贴图均为真实可编辑资产。`Tools > Building Editor > Create missing editable assets` 只生成缺少的资产，保留手动调整；`Run isolated Play Mode validation` 使用随机 Temp 目录回归，不改用户蓝图。函数图见 [开发者建筑蓝图编辑](Docs/RuntimeFlow.md)。
- 验证：Unity 6000.3.11f1 导入/编译通过；建筑专项 35 项及既有 BuildPalette 102 项 Play Mode 检查通过，实际画面已检查。203 份自有脚本格式/函数图、dotnet build（0错误、4个既有警告）、diff 检查通过。完整架构探针完成 39 项加载检查后停在既有悬浮控制时序断言：该探针仍要求控制器回调立即清零实际推力，而当前 HoverThruster.FixedUpdate 才执行输出更新；本任务未改动飞行生产代码，不将此完整探针列为通过。证据见 [建筑验证](Docs/Validation/2026-10-15-building-editor.json)。未覆盖正式 Player 构建、64×64 大底座性能和建筑运行时生成/战斗。 发布隔离工作区基于远程 main，184 份脚本检查通过；临时 csproj 引用本机现有 Library/被忽略第三方源码后静态编译同样 0 错误、4 警告，不提交这些依赖或临时配置。

2026-10-15 WorldLab：新增独立可编辑程序化体素世界实验场（128m范围、固定种子地形与遗迹、三材质硬度），支持采掘/范围爆破/填补及局部分块渲染与碰撞重建，现有炮塔命中入口已接入。F5/F9版本化差量保存/恢复，原子替换并保留上一份，校验失败不改活动世界；不复用玩家蓝图/货仓存档。生成网格通过Editor Bake为真实资产，不改Main。12项数据、16项Play、18项保存/损坏保护与28项炮塔回归通过；软件黑屏覆盖下真实落体180个物理帧后接触体素地面。首次后台更新关闭时冻结的探针失败已保留说明，实验场现临时启用后台运行并限60FPS。新增有限金属支撑/脱落实验：六邻接支撑，默认32格以内孤岛生成最多12个限时刚体碎片；8项数据、7项Play和101真实物理帧下落/再生清理检查通过。最终完整15分钟后台压力测试通过（53,955帧、1,794次爆破、14次再生，种子恢复成功），证据见Docs/Validation；大型结构与地形保持静态；仍为研究原型，无无限流式；详见 [WorldLab使用与边界](Docs/WorldLab.md)。

最近整理：2026-10-11；Unity 6000.3.11f1；URP；Input System；运行时类型保持全局命名空间。

2026-10-14 第三人称镜头环境碰撞：`CameraController` 在第三人称目标点到期望相机位置之间使用可复用的 `SphereCastNonAlloc`，忽略玩家自身碰撞体，在墙体/方块前收回相机并保留碰撞间隙；平滑恢复阶段再次校正，避免相机穿过障碍。碰撞层、半径和间隙均可在 Inspector 调整。墙体收回、自身过滤和障碍移除后恢复已通过隔离Play检查；未完成玩家手动视觉/手感验收。

2026-10-14 供电网络性能优化：`PowerNetwork.Rebuild` 保留每帧功率清零、分配和状态提交，但缓存由中继/发电机位置及连接距离决定的拓扑；拓扑未变化时跳过连接清空、发电机连接和中继两两检测。拓扑变化仍自动重建，供电行为契约不变。新增空间索引候选筛选和连通分量缓存；300轮新旧等价对照、32项玩法回归通过。100中继/500负载静态更新10.73→0.56ms，持续移动11.02→0.99ms。新增炮弹池、可增长查询缓冲及正常图标跳过投影；28项炮塔检查、10项真实飞行、4项复用及5项表现检查通过。完整条件、原始样本和限制见 Docs/Validation/2026-10-14-performance.md。

2026-10-11 CSV 方块数值：直接编辑 `Assets/Resources/BlockData/Blocks.csv`（25 行、80 参数列及4个只读身份列）。Editor 导入后自动校验、备份并同步 Prefab；Block/绑定组件 OnValidate 延迟请求校验，绑定 Awake 在其他组件之前应用 Resources 中的 CSV。mass 留空使用 x × y × z，填写正数使用手动质量；density 仅保留旧数据。requiresPower 合并应用货仓、收集机器人和维修机器人；Bot/Connector 辅助资源、悬浮控制和指定表现参数不纳入表。维修仓已补充 Power（50/100），CSV/蓝图运行时刷新保留连接状态与实例引用，Bake 拒绝缺失供电依赖。18 项 Edit Mode、4 项 Awake 和 8 项跨帧维修回归已通过，详见 `Docs/Validation/2026-10-11-block-csv-regression.json`。使用与生命周期详见 [CSV说明](Docs/BlockCsv.md)，函数图见 [RuntimeFlow](Docs/RuntimeFlow.md)，验证见 `Docs/Validation/2026-10-11-block-csv.json`。

2026-10-11 推进器字体状态修正：ThrusterInfoPanel 读取 Power.CommitPowerStatus 确认的 SupplyEfficiency，不再读取物理帧中暂时清零的即时供电值。撤销工作请求立即恢复正常显示，重新申请后等待电网确认缺电；残余推力及关机衰减不参与字体颜色判断。实际施力仍使用即时供电效率。验证见 `Docs/Validation/2026-10-11-thruster-power-colors.json`。

2026-10-11 推进器关机：主/通用/悬浮推进器撤销功率请求后，目标输出为零，实际推力按 `maxThrustChangeRate × fixedDeltaTime` 衰减并继续施加尾段力；喷焰跟随实际推力。悬浮控制器只提交目标，限速统一在 HoverThruster.FixedUpdate 执行，避免直接清零或重复限速。退出游玩、组件禁用或悬浮归属失效仍清零。ThrusterInfoPanel 仅对请求工作的推进器判定缺电，空闲文字为白色。验证记录见 `Docs/Validation/2026-10-11-thruster-shutdown.json`。

2026-10-11 炮塔目录与敌机：单/双管按钮名称移除 `(Clone)`，目录 Prefab 和 Main 已保存条目采用原 TurretButton 的暗色描边、按钮状态颜色与缩放反馈；Awake 按 BlockName 复用场景条目并绑定一次点击，避免重复生成/监听。CODEX 三艘敌机前方保持双激光、后方改为单管与双管实体炮塔；仓库及 EnemyBlueprints 同步迁移，安装文件已有哈希备份。验证见 `Docs/Validation/2026-10-11-turret-palette-enemies.json`；历史飞行数据不代表本次布局实飞结果。

先按功能找入口，再沿函数图进入规则实现。全部自有运行时脚本位于 `Assets/Scripts/Features`，Editor 制作/验证工具按功能分目录。第三方素材和 Package 源码不属于本轮重构。

## 功能入口

| 功能 | 首先查看 | 独立实现 |
| --- | --- | --- |
| 启动 | `Bootstrap/GameManager.Init` | 现有存档选择、界面初始化 |
| 蓝图加载 | `Building/BuildManager.LoadAllBlocks` | `BlueprintLoading/BlueprintLoader.Load` → 校验、恢复、统计、连接与取消 |
| 建造交互 | `Building/BuildManager.Update` | `Interaction/BuildPreview` / `BuildSelection` / `BuildManipulation` |
| 存档编辑 | `Building/SaveManager` / `BuildManager.SaveBlock` | `BlueprintFileStore` 文件规则；`BuildBlueprintEditor` JSON与邻近连接 |
| 进入/退出游玩 | `Simulation/PlayManager.PlayStart` / `PlayEnd` | `RuntimeGroupBuilder` / `RuntimeGroupCleanup` / `PlayBlockSelection` |
| 大世界与建筑 | `World/WorldSessionManager` / `WorldGenerationSettings` | `WorldRegionStreaming` / `WorldBuildingStreaming` / `WorldPlayerStreamingGuard`；`ProceduralVoxelWorld`支持有限WorldLab与Main流式数据 |
| 连接 | `Simulation/Block.CheckConnection` | `BlockConnections` 双边连接几何和状态 |
| 供电 | `Power/PowerTransmissionDevice.Update` | `PowerNetwork.Rebuild`；`PowerConnectionVisuals` 连接线 |
| 飞行 | `Flight/HoverFlightController.FixedUpdate` | `HoverControlSolver` PID与姿态计算；`ThrusterAllocator` 分配求解；各推进器施力 |
| 维修/回收无人机 | `Bots/RepairBot.FixedUpdate` / `CollectionBot.FixedUpdate` | `BotFlightMotion` / `BotObstacleAvoidance` / `RepairTargetSearch` |
| 战斗/损伤 | `Combat/Turret.FixedUpdate` / `Durability.ApplyDamage` | `LaserTurret` / `ProjectileTurret` / `TurretProjectile` / `TurretHitResolver` / `CollisionDamageResolver` |
| 销毁/爆炸 | `Combat/DestroyManager.DestroyGameObject` | `ExplosionPropagation` / `SalvageCleanupSession` |
| 回收与持久化 | `Salvage/LootDrop.Deliver` / `CargoPersistence.SaveReturnCargo` | `CargoHold` 数据与约束；表现位于 Presentation |
| 镜头/特效/UI | `Presentation/CameraController` / `VisualEffectsManager` / `MainUIPanels` | `CameraFraming` / `CameraShake` / `TransientEffects` / `PanelTransitions` |
| 玩家/输入 | `Player/InputManager.Update` / `PlayerController` | 原 Input System及模式门限 |
| 诊断 | `Diagnostics/DebugManager` / `AbnormalImpulseRecorder` | 场景调试、物理遥测与记录 |

上述路径均相对于 `Assets/Scripts/Features`。完整脚本/真实函数签名见 [CodeIndex](Docs/CodeIndex.md)，可点击源码行号；主要运行链见 [RuntimeFlow](Docs/RuntimeFlow.md)。Editor制作工具入口同样在 CodeIndex 中；外围开发脚本见 [ToolIndex](Docs/ToolIndex.md)。

## 架构与行为契约

采用按功能组织、明确主流程、显式依赖和引擎原生可编辑组合。MonoBehaviour 持有 Inspector、引擎回调和功能装配；有实际独立职责的计算、文件规则及会话使用普通 C# 类。沿用当前栈，不新增 DI 容器或 Command 框架。

- 建造输入、选择、拖拽、放置、文件编辑、加载与 UI 统计各有自己的代码入口；UI文本刷新不再承担质量/功率扫描。
- 蓝图加载先读取有效 JSON，再替换原父对象；重复同目标保留进度，新目标/取消使用版本标记隔离旧协程。空蓝图可同步完成，不能留下已完成 Coroutine 句柄。缺失资源（含空路径）移出缓存及存档；配置是一次加载的显式快照。
- 块間隔保留 `max(minimum, initial * 0.5^(elapsed / halfLife))`，零间隔仍逐帧恢复。镜头环绕参考速度与加载衰减独立。
- 连接要求两侧 canConnect、位置与相反法线满足；分组只读取有效连接几何。重组先捕获源刚体，保留线速度，对附加切向速度和角速度限幅；不覆盖原始单位归属。
- 电网仍按轴对齐盒范围；中继对取较小距离，共享发电机合并网络；同分量负载去重均分，不相连电网可叠加；本帧先清零再完整提交状态。退出最后中继时清掉快照。
- 机器人保留 Home 局部停靠偏移、实时世界位置、刚体点速度和制动参数；任务和返航共用机体球形净空查询，稳定保持三维绕障方向，停滞时重选脱困路线；目标扫描、避障建议、实际施力与表现分开。货物只在停靠完成后入库，失效/停用/Home销毁释放认领。
- 射击先施冲量再伤害；爆炸先断链和重组，再施受限冲量；爆炸不额外造成范围伤害，碰撞致死不爆炸。碰撞伤害共享有效接触能量预算，动态双方分摊。
- 临时能量线/灯光/陨石尾迹使用独立同名脚本；镜头偏移在替换/禁用/销毁时移除，面板过渡取消句柄并释放状态。

原场景/Prefab布局、Inspector初值、存档结构和资源路径保留。原85个运行时脚本和27个Editor脚本的 GUID 随文件迁移，未重建；新 Assets 有独立 `.meta`。`RuntimeUnitMember` 仍与 ControlUnit 同文件，保持现有类型身份。原 public 字段作为既有契约保留，新参数使用 SerializeField；不对原字段机械改可见性。

## 可读性与修改流程

共用规范在用户 `game-code-readability` skill；引擎动作使用 `unity-editable-workflow` 和 `unity-cli`。项目 `.editorconfig` 为 Allman、4空格、完整控制流块及 CRLF。每个自有脚本顶部说明真实职责/入口，生命周期复杂时补取消或资源所有权。长参数列表按语义分行；注释解释不变量、坐标系和执行顺序。

文件大小仅为审查提示。BuildManager 的原Inspector与UI/外部API装配、Bot的导航状态与配置映射、CameraController的镜头模式/协程、VisualEffectsManager的具名效果入口仍有较多行；其独立规则已分离。StatusIcon 是单一状态展示组件，Editor生成器保留一项资产制作流程，不为了缩短文件制造转发层。

新增/移动脚本时更新图和导航，且核对序列化与调用者。不要引用旧目录或历史索引。统一执行：

```powershell
dotnet run --project Tools/CodeReadability -- --write .   # 显式整理格式
python Tools/CodeReadability/generate_navigation.py       # 刷新实际行号及符号索引
dotnet run --project Tools/CodeReadability -- --check .    # 本地与CI共用
dotnet run --project Tools/CodeReadability -- --check-tools .
python Tools/CodeReadability/check_tool_sources.py
python Tools/CodeReadability/generate_flow_browser.py
dotnet build HY-Sandbox.sln --no-restore
git diff --check
```

主格式器扫描 Assets/Scripts 与 Assets/Editor；--check-tools另查9份Blueprint C#顶层脚本，保留 UTF-8/BOM/GB18030，比较非大括号 token。当前4份GB18030为 GroupAction、Meteor、ThrusterAllocator和AutoBuildTool。旧 ActionManager/CameraController中已有替换字符的注释按代码重写，不声称恢复原文。

Unity生成的 csproj 必须包含迁移后的路径和全部新脚本；IDE静态构建不能代替 Unity 导入或 Play Mode。工具说明见 [README](Tools/CodeReadability/README.md)。

## 验证与缺口

本轮已完成下列验证（2026-10-11），原始证据汇总见 [验证记录](Docs/Validation/2026-10-11.json)。探针是限定范围的真实执行，不代表完整游戏验收。

| 检查 | 实际结果 |
| --- | --- |
| 自有 Unity 运行时/Editor 脚本 | 157份格式、介绍和函数图符号通过，0差异/0错误 |
| 外围工具 | 9份C# Script语法/格式，6份既有Python及3份导航/检查Python语法、入口和模板；PowerShell解析通过 |
| dotnet build HY-Sandbox.sln --no-restore | 0错误、4警告：3个MSB3277依赖版本冲突及已有Profiler过时API |
| Unity 6000.3.11f1导入/重编译 | 0错误、0警告 |
| ArchitecturePlayProbe | completed/passed，66条检查记录；蓝图、存档规则、悬浮控制及真实表现生命周期 |
| BuildPalettePlayProbe | passed，56条；真实UI、放置及连接 |
| SalvagePlayProbe | passed，31条；运输、认领释放、维修和移动Home返航 |
| CombatHudPlayProbe | passed，30条；战斗界面、遮挡及模式 |
| PhysicsSandboxProbe | ALL PASSED，17条；真实PhysX碰撞/冲量/爆炸保护 |
| 契约/数据 | 609原公开/序列化字段、337public方法、112原源码meta、37份用户持久文件保持 |
| 资源检查 | Main场景全子层及29个Resources Prefab全子层无Missing Script；场景已重新加载且不脏 |
| 函数图浏览器 | 8张图全部渲染、源码链接与缩放通过，无JS错误 |
| Git | diff --check通过；恢复分支及配置备份可用 |

- 已完成供电纯规则回归：实际 PowerNetwork 源码加最小 Unity 数据桩，独立DSU参考模型对照1000次随机重建，检查共享发电机、去重、独立网络叠加、盒边界、无中继清零和每负载仅提交一次。此项不证明Unity生命周期与视觉。
- ArchitecturePlayProbe 在随机 Temp 目录隔离存档，测试真实加载器与文件规则；BuildPalettePlayProbe覆盖真实建造UI/连接；SalvagePlayProbe覆盖运输及损伤释放；CombatHudPlayProbe覆盖模式与界面；PhysicsSandboxProbe使用真实PhysX。结果记录实际执行状态。
- 检查场景/Prefab无Missing Script、GUID/公共契约、用户持久数据未改变。修改前保留恢复分支 `codex/pre-readable-architecture-20261011`；配置skill有独立带哈希备份。
- 未覆盖所有输入设备、长期密集交火、1000块蓝图/大量中继压力、全部移动Home障碍组合和正式Player构建；不将有限探针称为完整玩法验收。

## 外围工具与历史归档

ArtSource/VfxGraph和Blueprints下全部16份自有工具已明确职责/入口。Python读写集中于main，路径按脚本目录解析；DSH生成器模板与输出同步。工具只做静态验收，没有运行生成器、覆盖现有蓝图或以历史GPU截图冒充本轮视觉验收。30份工具附带数据/说明文件哈希未变。

原嵌套HY-Sandbox目录缺Packages/ProjectSettings，属于不完整旧快照；已迁入Archive/LegacyPrototype/HY-Sandbox。811文件原始字节逐项哈希一致，旧Library仅本地保存且不重新提交缓存；源码/资产和迁移清单入库。详见 [Archive说明](Archive/README.md)。

## 持续风险

- 全局Manager单例仍作为既有场景组合入口，跨功能调用并未全部消除；新独立规则避免反向UI依赖。后续只有真实多场景装配需求才引入容器。
- 飞行PID保留现有增益及质量语义；大型敌机积分收敛/稳态高度误差仍需实飞调参。
- 供电范围并集网格、状态图标Canvas和大量透明VFX仍需CPU/GPU压力测量；本轮以职责和资源生命周期改善，不宣称性能提升。
- 同时存在uGUI和诊断IMGUI；尚无正式构建验收记录。

## 性能评估与后续优化顺序

以下结论来自当前源码、场景和 URP 配置的静态审查，不能替代 Unity Profiler、GPU Profiler 或 Play Mode 压力测试。

- 已确认的高风险：`StatusIcon` 和 `CombatHud` 为每个图标/敌方视图执行屏幕转换与遮挡射线；`TurretProjectile` 每个物理帧执行球形范围/扫掠查询，查询缓冲满时扩容重查并复用；炮弹池首次扩容、临时灯光和 VFX 仍可能产生 Instantiate/Destroy 峰值；蓝图逐块实例化和 `RuntimeGroupBuilder.Rebuild` 在大结构变更时会形成 CPU/物理尖峰；`PowerRangeUnionView` 的三维 cell 网格在范围和设备数增长时有内存与网格重建风险。供电拓扑重建已改为 dirty 条件触发，但功率分配和负载范围扫描仍需压力测量。
- 需要 Profiler 确认的渲染项：PC URP 启用 HDR、2x MSAA、2048 阴影贴图、55m 阴影距离、四级级联、软阴影和 SSAO；透明 VFX、状态 Canvas、附加阴影灯可能造成 overdraw、阴影和 Canvas rebuild 开销。`DefaultVolumeProfile` 含测试组件及多个后处理组件，需核对实际 active/override 状态后再判断成本。
- 替代写法：供电采用 topology dirty + 功率状态分离，并用空间分桶减少中继两两检查；UI 按距离/可见性裁剪、缓存遮挡结果并分帧更新；投射物/VFX 使用对象池和固定容量 NonAlloc 查询；蓝图采用分批实例化、加载期延迟连接重建、保存时异步/原子替换；Bot/Turret 将目标扫描、避障和施力分频并复用空间查询；渲染按质量档位下调阴影距离/分辨率、后处理和透明层。
- 分阶段方案：第一阶段建立空场景、100/500块建造、机器人、炮塔和密集特效压力档位，记录 CPU main thread、Physics、GC、Render thread、GPU frame time 和内存；第二阶段优先修复供电重建、UI 遮挡频率和对象创建峰值；第三阶段处理对象池、透明 overdraw、阴影/后处理质量分级；第四阶段验收大蓝图加载、断连重组、长期交火和多机器人场景，并以目标硬件帧时间而非单次平均 FPS 判定。

## 2026-10-11 变更记录

全自有代码按功能组织。拆分建造、蓝图恢复、供电、机器人、物理分组、碰撞/射击/爆炸、镜头和表现职责；清理被替代算法。所有原有运行时及Editor自有脚本统一介绍、格式和导航。新增共用skill并接入Unity/UE开发和Unity审查skill。函数图区分直接、异步、引擎回调与清理；节点关联真实源码。

完整历史需求、旧验证结果、美术集成记录与旧函数索引保存于 [DevelopmentHistory](Docs/DevelopmentHistory.md)，仅作为历史，不代表当前路径和实现。Git历史中的场景资源记录仍可追溯。

## 2026-10-11 掉落与机器人阻塞修复

- 掉落文字在 LateUpdate 使用与 StatusIcon 相同的 CombatHud.IsWorldPointOccluded，检查屏幕边界、镜头前方及实体障碍。携带时忽略所属机器人层级，外部障碍仍遮挡。
- 敌方方块原必出金币改为 30% 金币包；白名单稀有部件概率从 20% 降为 8%。先抽部件再抽金币，已有货仓内容仍按原数量释放，不受随机爆率影响。参数在 Resources/Salvage/Settings。
- 掉落初始短时散落，球形路径检测阻止穿墙；显示节点悬浮和旋转，逻辑根保持稳定。认领后停止散落、搬运时还原姿态，保留吸附、认领和入库契约。参数已通过 Editor 保存到 LootDrop Prefab。
- Bot 从自身位置检查三维净空，起点重叠时通过 OverlapSphere/ComputePenetration 计算分离方向；目标阻挡时维持绕障方向，允许临时远离目标以脱困；目标连续可达后恢复直达。移除已替代的 Home 出发射线算法，原 Inspector 字段保留序列化身份。
- 静态检查：158份自有脚本格式/职责/函数图符号通过；dotnet build 0错误、4个既有警告（3个MSB3277及Profiler过时API）；git diff --check通过。Unity 6000.3.11f1导入和重编译通过。
- BotNavigationPlayProbe：5个真实PhysX场景、11条检查通过，覆盖Repair配置绕墙、Collection配置U形围挡（竖直逃逸）、惯性制动、移动Home墙后及起始重叠脱困。探针驱动生产BotFlightMotion，未替代任务流程验收。
- SalvagePlayProbe 最终49条检查通过，覆盖三类掉落文字遮挡/恢复/镜头后隐藏、散落旋转、携带姿态、掉率边界与重复转换、真实维修/运输/移动Home停靠和货物持久化。3个改动Prefab无Missing Script，Main场景不脏且已退出Play，Unity Console 0错误/0警告。完整证据见 [本次验证记录](Docs/Validation/2026-10-11-loot-bots.json)；探针只覆盖列明场景，未验证任意复杂封闭迷宫、所有移动/旋转Home组合或大量Bot性能。

## 2026-10-11 BuildType 与视角锁定

- SelectType 改为 BuildType，Select/Move/Rotate 保持 0/1/2，新增 Build=3。currentBuildType 用 FormerlySerializedAs 接收原 currentSelectType 场景值；方法与调用者统一为 SetBuildType，已有按钮事件入口保持。
- 选择方块资源进入 Build 类型，Update 按类型分派搭建预览或选择/轴操作；Build 不显示移动/旋转轴。切换到选择、移动或旋转会退出搭建并清理旧预览。
- SetBuildMode 处理 lockView 时只取消拖拽、清理预览并暂时隐藏操作轴，保留选中方块、高亮、BuildType 与待搭建资源；重新锁定时恢复对应操作轴；重新锁定后可继续原操作模式。
- Unity 6000.3.11f1重编译通过；BuildPalettePlayProbe 69条检查通过，包含四种类型解锁/重锁保持、目录切换进入Build、退出搭建及Gizmo隐藏；158份格式/函数图检查、git diff --check通过；dotnet build 0错误、4个既有警告。Main场景原有未保存修改保留，不自动保存或重载。探针直接调用lockView的模式回调，未模拟实际Tab按键。

### lockView 保留选择补充

切换视角不再 DeselectBlock；选中状态与高亮持续保留。操作轴仅在锁定视角且类型为 Move/Rotate 时显示，重新锁定即可恢复。Unity重编译及BuildPalettePlayProbe 78条检查通过，包含Select/Move/Rotate三种已选中状态的解锁/重锁、操作轴恢复和高亮保持。158份格式/函数图检查与git diff --check通过，dotnet build 0错误、4个既有警告。场景原有未保存修改保留；实际B键注入未覆盖。

### BuildType 按钮颜色

选择/移动/旋转工具按钮按BuildType显示Inspector配置的活动/非活动色；Build类型高亮当前资源条目。模式颜色同步Button的Normal/Highlighted/Selected状态，按下稍微压暗，避免鼠标悬停或键盘焦点遮掉真实模式色；按钮面板重新启用时从BuildManager同步。切换lockView不改变模式颜色。Unity重编译及BuildPalettePlayProbe 102条检查通过，包含四种类型对应按钮的Normal/Highlighted/Selected颜色、实际焦点渲染颜色、Build资源条目高亮及lockView颜色保持。158份格式/函数图检查、git diff --check和dotnet build通过（4个既有警告）。

## 2026-10-11 按需供电与炮塔分类

- Power.SetWorkRequested记录独立工作意图；空闲/关闭/禁用不占功率。电网按启动负载的额定剩余需求分配，功率不足时按比例分配，独立电网叠加但不超过额定需求。输入推进、悬浮开启、Bot任务/返航、货仓预订/吸附分别提交请求，断电不阻止第一次请求或恢复启动。空闲状态不显示缺电图标。
- Turret基类共用启停、索敌、双轴瞄准和供电门控。LaserTurret保留原Turret脚本GUID及Blocks/Turret资源身份，瞬时命中且不施加冲量；额定功率300。公用序列化字段迁入基类，旧冲量字段移除。
- 指定SpaceKit Single/Double源保存为ProjectileTurretSingle/Double，可编辑Prefab；按实际1/2个炮口每轮发射1/2发真实刚体炮弹。单管/双管额定功率100/150，速度42m/s、寿命2s、射程45m、单发冲量10Ns；支持载具点速度继承、自身过滤、球形扫掠防穿透及单次命中。
- 新建造条目与图标保存为Resources/BuildPaletteEntries UI Prefab，启动时只补缺失条目，遵守COMBAT分类与BuildType高亮。原场景手工条目优先；条目提示从真实方块配置刷新。资产制作菜单不会保存Main场景。制作源与调参说明见 [炮塔规格](ArtSource/Turrets.md)。
- Unity 6000.3.11f1导入/重编译和6个Prefab重载检查通过，无Missing Script/断引用。32条供电/推进/Bot/货仓检查、20条炮塔命中检查、8条真实帧自动双管飞行检查、3条目录点击/高亮/去重检查及6条悬浮控制检查通过。静态模型和真实双弹飞行截图已检查。证据见 [本轮记录](Docs/Validation/2026-10-11-active-power-turrets.json)。
- 166份自有脚本职责/格式/函数图检查通过；dotnet build 0错误、4个既有警告；git diff --check通过。Main原有未保存修改保留。未验证长期密集交火、大量实体弹性能、所有旋转载具组合或正式Player构建；同步扫掠检查与真实帧自动发射检查分别记录。

### 炮管后座与二维图标

ProjectileTurret发射后仅炮管模型沿炮口反向后退0.12m，0.04s快速后退、0.22s平滑恢复；参数和炮管引用在Inspector可编辑。LateUpdate独立于索敌与供电门控，停火继续复位，禁用立即恢复初始位置。单管/双管Prefab已通过Editor只补充引用，原其他参数保留，资产制作源同步。

两个新炮塔的透明二维图重新渲染，并修正Single Sprite导入及真实建造条目Preview引用；Play实例确认两个图标都非空。菜单Refresh turret thumbnails只更新图标，不重新搭建炮塔。28条炮塔/后座检查、10条真实帧飞行/同步后座/停机复位检查通过；Unity编译、166脚本格式/函数图、dotnet build和diff检查通过（4个既有构建警告）。Main及炮弹材质的期间外部改动保留并排除本次提交。见 [验证记录](Docs/Validation/2026-10-11-turret-recoil-icons.json)。

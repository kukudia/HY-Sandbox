# HY-Sandbox 开发入口

## 本轮界面与基地修复

下拉列表统一保存足够行高、文字留白与深色高对比模板；主菜单及现有自有场景/Prefab均应用相同规则。制作入口 **Tools/HY-Sandbox/UI/Repair dropdown templates**。基地编辑隐藏普通建筑据点表单及其遮挡区域，结构建造距离独立可调（默认150m），进入时恢复建造输入，避免沿用载具15m距离与自由镜头状态导致无法放置。菜单72项、基地战48项、普通建筑73项真实Play回归、迁移6项保护检查通过；范围见 [本轮验证](Docs/Validation/2026-11-09-base-menu-fixes.md)。

## 主菜单与工坊导航

Player 首场景为 `Assets/Scenes/MainMenu.unity`。**Exploration** 选择载具，**Siege** 选择基地；**Vehicle / Base Workshop** 打开对应蓝图编辑器。菜单提供静态模块预览、有效性说明、刷新、新建、示例副本、当前工坊恢复与最近设计；不引入科技树或长线养成。界面沿用项目英文标签。示例基地直接出战只读；编辑示例或通过 **CREATE COPY & EXPLORE** 使用示例载具时，明确创建唯一命名的个人副本。

冷启动只载入菜单，不加载Main、不读取默认载具、不生成世界。首次打开工坊/出战才Additive加载Main。Main运行期间为活动场景；回菜单先通过既有模式主链保存货仓、结束战斗和卸载玩法，再停用Main根对象并记录原活动状态。恢复工坊重启同一批对象，保留原稿、镜头和缓存。菜单拥有唯一活动EventSystem；直接运行Main仍使用其原EventSystem。**MENU [ESC]**（或Escape）返回主菜单，直接Main开发入口也可按需加载菜单。

菜单选择其他载具探索时，`MenuPlayDraft` 暂停当前工坊对象及编辑上下文，加载所选载具，再走既有探索副本流程；结束后恢复之前的工坊原稿和Undo/Redo。Siege直接使用菜单指定的基地，不要求先载入载具。显式打开另一工坊设计会替换当前编辑视图，沿用工坊自动保存机制；有实际工坊原稿时才启用 **RESUME CURRENT WORKSHOP**，恢复和同名最近设计不重新加载。加载中 **CANCEL & RETURN** 等待安全载入/清理完成再回菜单。坏文件、缺模块和选中后被删除的文件拒绝打开；货仓保存失败继续保留活动探索会话并显示原因，不能被菜单强行暂停/卸载。

**Settings** 提供全局音量、项目画质档、全屏和鼠标灵敏度，偏好存于 `HY.Menu.*` PlayerPrefs，首次运行沿用项目默认值。按键帮助为说明，首版不提供重绑定。Editor内Quit只提示，Player中退出应用。菜单场景、原生uGUI控件、预览RenderTexture、灯光和相机均为真实可编辑资产；制作入口 **Tools/HY-Sandbox/Main Menu/Install editable menu**，已有菜单布局不会被重建。

源码按功能组织于 `Assets/Scripts/Features/MainMenu`：`MainMenuController` 为导航入口，`Navigation` 管理工坊暂停/临时载具，`Library` 管理只读目录/预览，`Interface` 绑定展示，`Settings` 管理本地偏好。Editor制作/验证入口在 `Assets/Editor/MainMenu`。函数主链见 [RuntimeFlow](Docs/RuntimeFlow.md)，方法导航见 [CodeIndex](Docs/CodeIndex.md)。验证记录见 [主菜单验收](Docs/Validation/2026-11-08-main-menu.md)。

## 可编辑机库与舰队返航

基地编辑器 **PLACE HANGAR** 选择机库构件，沿用原有放置、选中、移动、90°竖轴旋转与复制。深色底板及半透明净空体在编辑时显示，中心标注净宽×净高×净深；选中后面向镜头的面板提供三轴 +/-，尺寸、姿态、身份随基地JSON保存，支持Undo/Redo。机库数量配置为 **6**（`Assets/Resources/Hangars/Limits.asset`），不是尺寸6m；尺寸受基地边界与64m模块安全范围约束。默认外20×9×24m，净空18×8×22m；机库预算按外壳面积计费，不按内部空体积计费，不能作为基地核心或载具模块。

战斗保留实体墙、顶、底板和伸缩滑门，隐藏编辑净空及控件。两套Siege通过 `ISiegeDeploymentHost` 共用 `SiegeFleet` 和部署卡片：点击空库DEPLOY，仅列各轴尺寸不超过净空的有效舰船蓝图；按蓝图真实包围盒居中，购买时再次验证物理净空与余额。一库一舰，舰船离库仍占用原槽位。新版基地战保持俯视镜头，舰船自主作战；预研保留玩家驾驶。

清波进入 **Returning**，击杀/清波/连击收益暂存PendingCoins，锁定购买和下一波。舰船按门外航点、对准、入库自动导航；门口占用阻止关门。持续失去供电/推进能力、机库损毁或返航超时的舰船自动报废。全部存活舰入库或报废后一次性结算，再按原设计和现有金币自动修复基地（含机库）及舰船；余额/净空不足不扣费。导航参数在两个宿主Inspector的Flight Settings中，使用实际有电发动机能力限制辅助刚体推力，仅在达到停泊位置/速度/角度容差后校准最终泊位，不远距离传送舰艇。战斗损伤不回写设计。

预研 `Siege.unity` 使用保留原基地布局的 `BastionHangars.json` 扩建副本，三个默认大机库替换方形占位，原 `BastionBase.json` 保留。正式示例`StarterHangar.json`在原示例旁增设一库并扩展底座；原示例文件保留。玩家基地按设计实际机库数量生成，最多6；没有机库仍可用建筑炮塔守波。制作入口：`HangarPrefabAuthoring.Generate()`、`HangarSceneIntegration.Install()`、`SiegeHangarInterfaceAuthoring.Install()`；重新运行较早的基地制作工具后，应再运行机库接线工具恢复新增按钮。

```text
Assets/Scripts/Features/Siege/
├── Hangars/
│   ├── HangarModule.cs          净空、尺寸、支撑与安全门
│   ├── HangarRules.cs           数量和姿态校验
│   ├── HangarLimits.cs          可编辑数量配置
│   ├── HangarFacePanel.cs       朝镜头的选中控件
│   ├── HangarResizeButton.cs    尺寸命令
│   └── ResizeHangarAction.cs    尺寸撤销/重做
├── Fleet/
│   ├── SiegeFleet.cs            机库绑定与舰队部署
│   ├── SiegeReturnFlight.cs     起飞和返航固定步导引
│   └── SiegeFlightSettings.cs   航行与报废参数
└── Interface/
    └── ISiegeDeploymentHost.cs  预研与新版共用入口
Assets/Editor/Siege/Hangars/      资产制作、接线和隔离验证
```

本次验收记录见 [机库验证](Docs/Validation/2026-11-09-hangars.md)。

## Main 玩法合并入口

Main 的 **Simulate**（或 P）继续提供工坊内模式选择：探索按需 Additive 加载 `Assets/Scenes/Exploration.unity`，新阵地战加载 `Assets/Scenes/SiegeGameplay.unity`。玩法期间Main为活动场景，持有建造编辑器、共享管理器和摄像机；菜单启动时输入由菜单EventSystem统一管理，直接Main入口仍使用自身EventSystem。探索地形/建筑与运行舰船归探索副场景。未选择探索时不生成世界。原 `Assets/Scenes/Siege.unity` 保留独立预研入口、逻辑/UI及压力场，不由新模式自动启动。

模式状态由 `GameModeController` 管理，探索通过 `BindHost` 显式绑定 Main 的环境、加载 UI 与 HUD。场景异步加载支持取消，失败完整清理后重试；退出卸载玩法场景。Main 建造原稿停用保留，探索使用副本，返回恢复同一个原稿与镜头；正常返回保存货仓后迁移回原稿，死亡返回清空货仓。不会把战斗损毁覆盖到未保存的作品。SiegeGameplay 已接入基地蓝图部署、真实守波、整备修复与结算，见下节。

## 基地自由建造首版

Main 左下 **BASE EDITOR** 打开独立基地编辑目标（正式Player也可用，复用结构编辑能力）；可新建命名蓝图，或 **COPY STARTER** 复制可修改的示例。调整底座、放置/移动/旋转模块，选中有耐久的模块后 **MARK SELECTED CORE** 指定唯一核心。编辑自动保存到独立 `BaseBlueprints`，普通建筑仍使用 `BuildingBlueprints`；**BACK TO VEHICLE** 返回载具建造，再通过 **Simulate → Siege** 出战。旧建筑目录中已标记核心且非据点的文件会迁移到基地目录；同名冲突自动另取名，字节校验后将原文件归档到 `BlueprintMigrationBackups`，不覆盖任何已有设计。

Siege 使用菜单或 Simulate 面板进入前选定的基地，在副场景可编辑的 `Base Spawn` 自动生成；出生点表示底座中心。局内显示状态、开始下一波、修复重建和撤退/返回，并支持机库部署舰船；基地本身不再提供选择、编辑、位置、旋转和部署确认。默认预算20000，普通模块按体积每格5计价、机库按配置的外壳面积单价计价；底座最大64×64，限高32m。拒绝越界、穿地、模块重叠、无效身份/资源、缺核心、超预算；自动生成时复用建筑真实连接图，所有分量必须连接底座向下锚点。无发电机/单发电机/无炮塔只给风险提示。核心有显式标记，独立于驾驶舱。基地和敌机归属副场景，副场景保存自己的炮弹与GPU特效池、照明；Main普通刷怪器不参加模式战斗。

**START NEXT WAVE** 启动共享 `SiegeMatch` / `SiegeWaveDirector` 的真实敌机波次；先读取玩家敌方蓝图库，没有有效蓝图时使用配置中的内置敌机。发电、炮塔、毁伤和建筑断裂沿用生产机制。整备阶段 **REPAIR + REBUILD** 显示报价，复用 `SiegeBase` / `BuildingRefit` 按原设计恢复耐久和缺件；净空或余额不足不扣费。设计预算与局内维修金币分开，初始维修金币6000。首版不支持战中扩建或科技养成。核心丢失自动结算；撤退先展示本局波次、击毁和金币，再返回。原设计文件和Main载具原稿不受战斗损毁覆盖。

配置：`Assets/Siege/Construction/Settings.asset`、`Combat.asset`、`StarterHangar.json`（保留原`StarterBase.json`）。制作菜单 **Tools/HY-Sandbox/Siege/Install base construction**；只重建工具拥有的两个UI根，保留其他手工布局和原预研场景。验证入口 **Validate base construction**，报告与边界见 [基地建造验证](Docs/Validation/2026-11-08-base-construction.md)。

```text
Assets/Scripts/Features/
├── Simulation/
│   └── Modes/
│       ├── GameModeController.cs   模式/场景生命周期
│       ├── PlayDraft.cs            建造原稿与运行副本
│       └── Interface/
│           └── GameModePanel.cs    模式选择/重试
└── Siege/
    └── Gameplay/
        └── SiegeGameplayStage.cs   新阵地战制作入口
Assets/Editor/
└── Simulation/
    ├── GameModeAuthoring.cs         可重复制作/迁移入口
    └── GameModePlayChecks.cs        正式模式集成验证
```

菜单 **Tools → HY-Sandbox → Modes → Install gameplay subscenes** 保存真实场景与界面；重复执行不重建已有布局。**Validate Main mode transitions** 验证模式往返；函数图见 [RuntimeFlow](Docs/RuntimeFlow.md)。历史世界组件/性能套件通过 `WorldSessionValidationHost.Attach()` 显式装配隔离世界夹具，保留组件生命周期回归；这类通过不能替代正式副场景集成验证。验证记录见 [玩法合并第一阶段](Docs/Validation/2026-11-08-game-modes.md)。

## 显式组件与友敌 AI 调试

常驻服务、对象池、工业动画调度与调试组件已保存在各场景的 Managers/服务层级，可在编辑模式查看、调整和禁用。载具根 `Resources/BlocksParent/BlocksParent.prefab` 预挂 `EnemyController`，模块、机器人、武器和 UI 的依赖预挂在各自 Prefab；组装时只初始化或启停。

友方自主舰与敌机共用 `EnemyController`。运行前在场景 `DebugManager → AI Debug` 勾选 **Show Friendly AI / Show Enemy AI**，并开启 Scene/Game 的 Gizmos；载具 Prefab/实例的 **Draw Debug** 可单独开启。默认关闭，玩家驾驶、中立、禁用和非模拟状态隐藏。绿色/红色标记区分友敌，青色连线指向目标，紫色/白色表示期望/平滑输入；避障线显示最近一次采样，不额外执行物理查询。

数量由玩法决定的建筑、碎片、地形块、网格、标签、状态图标、临时光束/灯光与工坊服务，由 `Resources/Runtime/Objects.asset` 引用的真实 Prefab 实例化。制作/迁移菜单 **Tools → HY Sandbox → Architecture → Bake Editable Components**，重复执行保留已有参数与引用。动态挂载仅保留体素碎片按实际格子创建碰撞体，以及默认关闭的未知外部刚体诊断探针；编辑器制作和隔离测试另行保留。

已验证：205 个自有 Prefab、44 个 Block、7 个保存场景与模板引用；真实 Play 的 AI/载具、炮弹和特效池、断片生命周期、工坊开关及 WorldLab 碎片物理；37 个 Player 脚本程序集编译。完整 Player 打包/运行、全部场景完整玩法和长时压力未验证。入口、证据与静态检查见 [组件迁移报告](Docs/Validation/2026-11-07-editable-components.md)，函数图与导航已同步。

Unity 6000.6.8f1（URP / VFX Graph 17.6.0，Input System 1.20.1）。本文只维护当前功能、契约与验证入口；历史变更和旧验收完整保存在 [DevelopmentHistory](Docs/DevelopmentHistory.md)。

## 特效与音频资源管理

敌机出生现由 `EnemySpawner.SpawnBlockData` 与 `SiegeWaveDirector.Tick` 共用 `VisualEffectsManager.TryPlayEnemySpawnPortal`：成功生成/注册后播放金橙光环和紫色旋涡，替换 Siege 旧信标；不延迟生成，不影响据点驻军。传送门按舰体横截面缩放、固定在出生点后方，2.4 秒收拢、3 秒回池。集中库 `EnemySpawnPortal` 可替换 Prefab，管理器 Inspector 可调留白及默认 6–80m 直径。原生 Graph、纹理和补缺制作入口见 [资产规格](ArtSource/VfxGraph/EnemySpawnPortal.md)。真实 Siege 夹具两条生成路径及 GPU 回收通过；同时修复一次性回调略早触发后遗漏重调度的问题，验证及范围见 [传送门验证](Docs/Validation/2026-11-07-enemy-spawn-portal.md)。

GPU 特效已接入按场景、按模板复用的 VfxEffectPool：取消64个一次性效果与24条残骸尾烟的活动数量门槛，峰值按需扩容；爆炸、残骸、陨石共用生命周期，回收前保留尾烟。集中库只配置闲置缓存（每模板32、每场景128），不会拒绝活动请求。VFX Graph 粒子在 GPU 上模拟，CPU 负责对象与跟随；单Graph粒子容量仍为预分配预算。验证见[GPU特效池](Docs/Validation/2026-11-06-vfx-pool.md)。

残骸包裹范围、粒子密度与不透明度已降低，尾烟稍浅且更透明；火光颜色保持 10% 设定。残骸火光已减弱并收紧，新增头部灰黑烟遮叠与随机火焰朝向，尾烟加深加厚；见[烟火平衡验证](Docs/Validation/2026-11-06-fire-balance.md)。残骸新增按模型包围盒自适应的局部包裹火焰，跟随模型中心与旋转；[包裹火焰验证](Docs/Validation/2026-11-06-debris-envelope.md)。尾烟已提高不透明度并加入短促橙色余火：中低速烟迹保持对比，火光与烟雾共用粒子，仍由集中库 DebrisSmoke 配置。隔离 Play 近远景、销毁后消散通过；参数、截图与验证边界见 [尾烟可见性](Docs/Validation/2026-11-06-debris-visibility.md)。

战斗表现增强：残骸烟迹使用 `DebrisSmoke` 世界空间图，挂载点按刚体点速度跟随，残骸销毁/禁用后独立淡出；激光使用 `LaserBeam` 分层亮芯，宽度 0.16m、持续 0.2s；炮弹使用 `ProjectileTracer` 记录真实位置历史，宽度 0.1m、历史 0.09s、最长 7m，复用时清空。特效仍在集中库替换，伤害、命中和弹速不变。制作入口 `CombatVfxBaker.BakeMissing`，规格见 [CombatVfx](ArtSource/VfxGraph/CombatVfx.md)，真实 Play 近/远景与回收验证见 [战斗特效报告](Docs/Validation/2026-11-06-combat-vfx.md)。

Unity 菜单 **Tools → HY Sandbox → 资源管理 → 特效与音频** 打开统一管理入口。`PresentationResourceWindow` 支持名称/路径搜索、类型筛选、定位、原生预览、直接引用查询和保存；Prefab 通过原生 Prefab Mode 编辑完整层级。扫描覆盖 Assets 下的 VFX Graph、包含 VfxEffect/VisualEffect/ParticleSystem/TrailRenderer/LineRenderer 的 Prefab 和 BlockVfxLibrary，可额外显示递归依赖。

“运行时播放库”直接编辑既有 `Resources/VFX/BlockVfxLibrary.asset`。一次性效果以及 HoverJet、ThrusterJet、BotFlight、RepairContact、EnergyTrail、LootBurst 和保留外观变体全部集中配置。29 个项目 Prefab 的 54 个挂载点只保留效果编号、位置、朝向、缩放和自动播放标记；3 个组合控制器只引用子挂载点。`VfxEffect.EnsureContent` 在 Play 首次使用时从库实例化模板，保留对象生命周期。替换库中 Prefab 后，新实例使用新效果；已有实例需重新生成或重新进入 Play。

迁移保留原组件 fileID、Prefab GUID 和全部外观参数；两种特殊外观另存到 `Assets/Art/BlockVisuals/VFX/Variants`。`VfxCatalogMigration.Run` 可重复执行；`BlockArtIntegrator` / `BlockVfxBaker.Populate` 后续制作只建立编号挂载点，`PopulateTemplate` 只制作库模板。`BakeMissing` 不覆盖已有非空库选择。第三方原包未迁移，项目已使用的 Art/Resources 特效全部纳入。验证见 [集中引用迁移](Docs/Validation/2026-11-06-vfx-catalog.md)。

特效与音频两页共用 `PresentationAssetIndex` 的 GUID 索引、搜索与直接引用查询，项目变更后刷新、窗口关闭释放内嵌 Inspector。音频已支持 AudioClip 浏览及 Unity 原生预览，后续音频播放策略可独立接入，不将 GPU 特效生命周期套用到音频。直接引用不包含字符串动态加载；依赖资源也可能服务于其他功能，编辑会影响其所有使用者。

源码入口位于 `Assets/Editor/Presentation/Resources`，真实目录见 [CodeIndex](Docs/CodeIndex.md)，函数图见 [RuntimeFlow](Docs/RuntimeFlow.md)。集中迁移后资源索引为 215 个特效相关资产、410 个含依赖资产。初版编辑器验证见 [资源管理验证](Docs/Validation/2026-11-06-resource-manager.md)，最新 Play 与迁移保真验证见 [集中引用迁移](Docs/Validation/2026-11-06-vfx-catalog.md)。

## Unity 6.6 升级适配

已迁移禁用的对象/序列化整数身份 API 至 EntityId。后续场景服务使用 SceneObjectQuery 明确归属并拒绝重复实例；VFX 桥接适配 17.6 的 GetGraph/CreateGraph，喷口 FBX 用显式材质 remap 保留引用，Profiler 读取录制帧的类型化计数及可用性。CodeReadability 从真实工程读取条件符号。入口与实际验证/未覆盖边界见 [后续适配报告](Docs/Validation/2026-11-05-unity-6000.6-followup.md)，原始清单见 [升级报告](Docs/Validation/2026-11-05-unity-6000.6-upgrade.md)。函数图和导航已同步。

Player 回归发现并修复近景列提交与远景环刷新的同步空窗：列加载/卸载事件立即切换对应代理，退出解除订阅。最终 Main Editor 会话 57 项、Main Player 33 项、Siege Player 67 项通过，Windows Mono Development 构建 0 错误；隐藏 Player 的黑帧截图不作为画面验收，真实硬件交互、可见 Player 画面和长期压力仍待验证。

## 飞行与战斗 AI 升级

敌机新增稳定索敌、即时失效与错峰调度，避障过滤触发器和地板重叠伪法线；实体炮塔按目标/射手点速度预测交会，修复新增模块与重组变更在索敌时可见。机器人导航基线通过，本轮保留其现有算法。入口、Inspector 开关、真实 Play 证据和性能/长时验证缺口见 [AI 升级报告](Docs/Validation/2026-11-05-ai-upgrade.md)。

## 最新 Profiler 与战斗性能

已分析 2026-11-05 16:35 的 2,000 帧采样：炮塔固定步及伤害触发的连接视觉重复创建/销毁是主要可修复热点。`BlockConnections.CheckConnection` 保留未改变的双侧连接视觉；`ControlUnit.GetDamageTargets` 共享当前物理步的模块层级快照，炮塔仍实时验证位置、伤势和归属。新增同一步激活、移动、重组及视觉身份回归；没有降低物理频率、伤害或渲染质量。

相同隔离夹具连接刷新中位 25.51→1.10ms，40炮塔索敌批次 21.19→3.86ms；自然阵地战60秒 Editor 观察54.6→58.9帧/秒，但战斗轨迹不同，不作为确定性收益或Player帧率。采样局限、逐项证据及剩余Gizmos/渲染热点见 [性能报告](Docs/Validation/2026-11-05-profiler-performance.md) 与 [原始汇总](Docs/Validation/2026-11-05-profiler-performance.json)。

## 多舰表现性能优化

PC管线启用GPU Resident Drawer，并保留BRG着色器变体。`IndustrialPartMotion` 改为活动注册与单个 `IndustrialMotionDriver` 调度；保留 Inspector 配置、scaled旋转和unscaled浮动/发光。GPU绘制路径的 `IndustrialEmissionBinding` 按源材质及脉冲参数共享临时材质，固定8舰的2.79万个发光Renderer只需1个脉冲材质更新；传统渲染和外部PropertyBlock保留原路径，避免破坏自动实例化。建造/游玩高亮同步迁移，禁用、重配、销毁释放材质。关闭连接点调试时跳过 `Block.OnDrawGizmos` 的几何计算；标签复用GUIStyle。

同镜头、固定8艘CODEX_01_Bastion加基地、约1.39万动画组件，独立Play会话各40观察帧暖机和300帧采样：传统路径CPU帧时间中位60.42ms，GPU绘制与共享发光38.02ms，减少约37%。这是Editor静态舰队测试，不代表自然战斗或Player帧率。证据、回归、原生崩溃记录和Player验证缺口见 [多舰表现性能报告](Docs/Validation/2026-11-05-fleet-presentation.md)。`FleetPresentationBenchmark.CreateFixedFleet` / `StartCapture` 可重现夹具与采样；只读配置，不在运行中切换渲染系统。

## 本轮复用与精简

断片保留修复：建筑超过32模块或残骸数量预算时仍生成完整物理残骸，预算只请求延后退场。建筑、阵地战敌方碎片及载具延迟清理共用 `FragmentLifetime` 最短保留期（`Resources/Salvage/Settings.minimumFragmentLifetime`，当前10秒）；伤害销毁、卸载和会话退出不受此保护。载具重组保留源 PhysicsScene、损伤归因及剩余碰撞保护。验证见 [断片保留报告](Docs/Validation/2026-11-04-fragment-retention.md)。

建筑与载具共用 `BlockConnectionSnapshot` / `BlockConnections` 双侧连接、`BlockGroupManager` BFS 分组及 `RigidbodyMotion` 运动继承。`BuildingConnectionGraph` 旧实现已删除，建筑只保留虚拟地面支撑、地形脱锚、残骸预算和存档边界；载具保留驾驶舱选择、操控和组清理。删除节点只重探邻近模块；真正复杂碰撞布局继续实时探测，独立停靠机器人的碰撞体不使主体缓存失效。

共享规则同时收束到：`Thruster` 输入推进链、`BlockBounds` 几何、`BlockActionState` 撤销恢复、`SaveManager.LoadBlueprint` 加载协调、`WreckSalvage.ReleaseContents` 模块货物释放、`Bot.CanLaunchForWork` 起飞门槛、`BuildHandleAxis` 操作轴方向，以及存档列表的统一构建。清理失效的销毁延迟刷新、动态重命名按钮和重复特效状态；兼容入口只委托共享规则。

范围、保留差异、实际回归结果与限制见 [本轮报告](Docs/Validation/2026-11-04-reuse-refactor.md)。全仓 AST 检查辅助发现重复；同形方法和语义重复仍需人工核对，不以删行数代表质量或完整玩法验收。

## 功能入口

下列运行源码路径相对于 `Assets/Scripts/Features`；实际递归目录树、全部真实函数签名和源码行号见 [CodeIndex](Docs/CodeIndex.md#代码文件夹位置)，主要调用链见 [RuntimeFlow](Docs/RuntimeFlow.md)。制作/验证工具放在 `Assets/Editor`，不整理第三方源码。目录边界见 [CodeLayout](Docs/CodeLayout.md)。

| 功能 | 主入口 | 共享机制或独立策略 |
| --- | --- | --- |
| 启动 | `Bootstrap/GameManager.Init` | 当前存档、界面与会话组合 |
| 蓝图与文件 | `Building/SaveManager` / `BuildManager.LoadAllBlocks` | `BlueprintFileStore` / `BlueprintLoader` / `BlueprintBlockFactory` |
| 建造交互与动作 | `Building/BuildManager.Update` | Preview / Selection / Manipulation / `BlockActionState` |
| 建筑与据点制作 | `Building/BuildingEditor/BuildingOutpostEditor` | 地基只作制作网格；世界蓝图唯一来源为用户 BuildingBlueprints 文件仓 |
| 游玩与载具 | `Simulation/PlayManager` / `Units/ControlUnit` | `RuntimeGroupBuilder` / `RuntimeGroupCleanup` |
| 连接和分裂 | `Simulation/Connections/BlockConnectionGraph` | 双侧连接、几何快照、增量删除；`BlockGroupManager` 统一 BFS |
| 刚体与几何 | `Simulation/Physics/RigidbodyMotion` / `Geometry/BlockBounds` | 质心运动捕获/继承、统一八角包围盒 |
| 世界和建筑 | `World/WorldSessionManager` / `Buildings/BuildingRuntime` | 流式加载、支撑、脱锚、残骸预算和快照 |
| 据点驻军 | `World/Outposts/OutpostEncounterDirector` | 部署、有限驻军、休眠与会话内击败状态 |
| 阵地战 | `Siege/SiegeSession` | Fleet / Battle / Workshop / Interface，各自拥有状态与退出边界 |
| 供电 | `Power/Devices/PowerTransmissionDevice.Update` | `PowerNetwork` 拓扑缓存与需求分配 |
| 飞行 | `Flight/HoverFlightController` / `Thrusters/Thruster` | HoverControlSolver / ThrusterAllocator；主推进与通用推进共享生命周期 |
| 维修与运输 | `Bots/Repair/RepairBot` / `Collection/CollectionBot` | 导航、返航和起飞供电门槛共享，任务规则独立 |
| 战斗和损伤 | `Combat/Turrets/Turret` / `Damage/Durability` | 命中结算、碰撞能量与伤害入口共享 |
| 销毁和回收 | `Combat/DestroyManager` / `Salvage/Drops/WreckSalvage` | 有界爆炸冲量、运输取消、货物释放和残骸清理 |
| 镜头、表现与输入 | `Presentation` / `Player/InputManager` | 镜头模式、池化特效、UI 与 Input System |
| 诊断 | `Diagnostics` / `Simulation/Diagnostics/SimulationWorkMetrics` | 按需计时、物理遥测和异常记录 |

## 行为与数据契约

- MonoBehaviour 持有 Inspector、引擎回调和组合；共享算法/状态放普通 C# 类。没有新增全局服务容器、反射生产调度或第二套连接算法。
- 原组件类型、已有 GUID、Inspector 字段和存档结构保留；`RuntimeUnitMember` 仍与 `ControlUnit` 同文件。新增 Assets 配 `.meta`，迁移通过 Editor API 保持 GUID。
- 建筑和载具主体都使用 ControlUnit。建筑不进入载具驾驶舱重组或远距清理列表；脱落建筑模块停用功能脚本，预算前先释放已有货物，超预算也不吞货。卸载/休眠/退出不产生回收奖励。
- 连接由双侧可用连接面、位置和相反法线决定；视觉对象不决定邻接，建筑不生成载具接缝装饰。刚性整体移动保留缓存；模块姿态、连接面、层/Trigger、根缩放和实际模块附加碰撞体改变时失效。
- 分裂在层级迁移前捕获源速度、角速度和质心，所有碎片使用同一捕获状态；刚体参数由 GroupPhysicsSettings 统一应用。Main 建筑残骸使用 PlayManager 的阻尼及继承限幅，独立 WorldLab 从源刚体获取配置。
- 建筑范围伤害按批次/队列处理连锁，载具爆炸使用断链重组与有界冲量；启用 canExplode 的方块撞击致死仍爆炸。碰撞使用法向耗散能量，动态双方分摊；不将两类玩法策略机械合并。
- 蓝图校验和取消仍先于替换；同目标重复请求保留进度，切换使用版本标记隔离旧协程。撤销恢复共用工厂并保留精确世界姿态、稳定身份、尺寸/质量/碰撞及货仓。
- 电网按剩余需求比例分配，唯一负载去重，独立网络可补足需求；功率只限制机器人起飞，离仓后仍能完成任务或返航。
- 文件读写、加载取消、场景卸载和临时表现的生命周期仍由各领域入口拥有。独立冻结性能基准是验证对照，不接入生产链，也不为去重改写参考算法。

## 修改与验证

共用编辑规则由用户 `code-readability` Skill 维护；Unity 配合 `unity-editable-workflow` / `unity-cli`。修改前搜索现有能力和调用者；共享规则只维护一份生产实现，差异显式留在领域策略。迁移消费者并验证后清理旧实现。

Unity C# 使用项目 `.editorconfig`：Allman、4 空格、完整控制流和 CRLF。已有 GB18030/BOM 按真实编码保留；新增文本默认 UTF-8。新/改源码说明真实职责与入口，注释解释生命周期和易错不变量。

```powershell
dotnet run --project Tools/CodeReadability -- --write .
python Tools/CodeReadability/generate_navigation.py
dotnet run --project Tools/CodeReadability -- --check .
dotnet run --project Tools/CodeReadability -- --duplicates .
dotnet build HY-Sandbox.sln --no-restore
git diff --check
```

项目锁定 .NET SDK 9.0.318。`--duplicates` 只读报告生产方法中不少于50个 token 的完全相同方法体，忽略注释，排除 Editor/Validation 和整文件 Editor 条件基准；候选须人工复核，零候选不证明不存在语义重复。

本轮实际验证结果见 [复用治理报告](Docs/Validation/2026-11-04-reuse-refactor.md)。Unity 生成的 csproj 必须包含所有新路径；格式、静态编译、Unity 导入、Play 和实际性能分别记录。未做的 Player 构建、长时战斗、最大规模压力和手感验收不能写成通过。

外围工具说明见 [CodeReadability](Tools/CodeReadability/README.md) / [ToolIndex](Docs/ToolIndex.md)。旧快照隔离在 [Archive](Archive/README.md)，历史数据及过往验收不作为当前运行结果。

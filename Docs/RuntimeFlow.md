# HY-Sandbox 运行时函数图

    这些图根据当前源码和调用者整理，用于寻找功能入口，不是采样得到的实际执行轨迹。实线表示直接调用（标注“经”时包含明确中间函数）；虚线标出协程、事件或注入动作。Unity Update/FixedUpdate 的独立入口不代表脚本执行顺序。

    每个图节点可点击对应源码；渲染器禁用 click 时使用图下链接。导航由 Roslyn 校验文件与方法存在，语义顺序由修改者审查。过载函数链接到第一个声明，完整过载见 CodeIndex。

    维护：业务变化更新受影响图和 DEVELOPMENT；纯格式变化重新生成行号。`dotnet run --project Tools/CodeReadability -- --check .` 会拒绝失效符号。

    ## 主菜单、工坊暂停与蓝图出战

源码推导：MainMenu冷启动只展示菜单；选择后只读校验蓝图，再按需Additive加载Main。Main跳过默认存档启动，菜单拥有唯一EventSystem。显式编辑示例才创建副本；探索临时选择别的载具时保留工坊对象/上下文/Undo。返回先走模式清理与货仓保存；失败保留活动会话并提示，成功才恢复原稿、暂停Main根对象。取消等待安全载入/卸载完成，不中途丢弃工坊。

```mermaid
flowchart TD
    A["MainMenuController.Start()"]
    click A "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L62" "打开源码"
    B["MainMenuController.Choose()"]
    click B "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L91" "打开源码"
    C["MenuBlueprintLibrary.Inspect()"]
    click C "../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L54" "打开源码"
    D["MainMenuController.Launch()"]
    click D "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L202" "打开源码"
    E["MainMenuController.EnsureMain()"]
    click E "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L407" "打开源码"
    F["MenuPlayDraft.Park()"]
    click F "../Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs#L20" "打开源码"
    G["GameModeController.LaunchSiege()"]
    click G "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L88" "打开源码"
    H["GameModeController.SelectExploration()"]
    click H "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L77" "打开源码"
    I["WorkshopNavigation.OpenMenu()"]
    click I "../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L54" "打开源码"
    J["MainMenuController.Return()"]
    click J "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L359" "打开源码"
    K["GameModeController.ReturnToBuild()"]
    click K "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L201" "打开源码"
    L["MenuPlayDraft.Restore()"]
    click L "../Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs#L37" "打开源码"
    M["MainMenuController.Resume()"]
    click M "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L321" "打开源码"
    N["MainMenuController.CancelNavigation()"]
    click N "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L495" "打开源码"
    O["MenuSettings.Initialize()"]
    click O "../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L19" "打开源码"
    P["MainMenuController.OpenBlueprint()"]
    click P "../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L298" "打开源码"
    A --> O
    A -.->|用户选择| B
    B --> C
    C -.->|用户确认| D
    D --> C
    D --> E
    D -->|编辑或临时载具| P
    D -->|保留其他工坊原稿| F
    D -->|所选基地| G
    D -->|所选载具| H
    I --> J
    J --> K
    K -.->|保存及卸载成功后| L
    J -.->|保存失败保留活动会话| I
    A -.->|恢复工坊| M
    M --> E
    N -.->|安全载入或玩法清理完成| J
```

- [MainMenuController.Start](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L62)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|Start -->
- [MainMenuController.Choose](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L91)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|Choose -->
- [MenuBlueprintLibrary.Inspect](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L54)
<!-- source: Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs|Inspect -->
- [MainMenuController.Launch](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L202)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|Launch -->
- [MainMenuController.EnsureMain](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L407)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|EnsureMain -->
- [MenuPlayDraft.Park](../Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs#L20)
<!-- source: Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs|Park -->
- [GameModeController.LaunchSiege](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L88)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|LaunchSiege -->
- [GameModeController.SelectExploration](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L77)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|SelectExploration -->
- [WorkshopNavigation.OpenMenu](../Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs#L54)
<!-- source: Assets/Scripts/Features/MainMenu/Navigation/WorkshopNavigation.cs|OpenMenu -->
- [MainMenuController.Return](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L359)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|Return -->
- [GameModeController.ReturnToBuild](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L201)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|ReturnToBuild -->
- [MenuPlayDraft.Restore](../Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs#L37)
<!-- source: Assets/Scripts/Features/MainMenu/Navigation/MenuPlayDraft.cs|Restore -->
- [MainMenuController.Resume](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L321)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|Resume -->
- [MainMenuController.CancelNavigation](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L495)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|CancelNavigation -->
- [MenuSettings.Initialize](../Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs#L19)
<!-- source: Assets/Scripts/Features/MainMenu/Settings/MenuSettings.cs|Initialize -->
- [MainMenuController.OpenBlueprint](../Assets/Scripts/Features/MainMenu/MainMenuController.cs#L298)
<!-- source: Assets/Scripts/Features/MainMenu/MainMenuController.cs|OpenBlueprint -->

## 基地设计、部署与守波

源码推导：Main保存基地设计；基地使用独立BaseBlueprints目录；副场景只读进入前选定的快照，在可编辑出生点自动生成并验证地面支撑。新版与预研共用波次和修缮；净空/余额通过才恢复原设计并扣费。核心事件或撤退汇入结算，返回停止模拟、卸载副场景、恢复Main原稿。

```mermaid
flowchart TD
    A["SiegeBaseWorkshop.DesignateCore()"]
    click A "../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L68" "打开源码"
    B["GameModeController.LoadMode()"]
    click B "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L121" "打开源码"
    C["SiegeGameplayStage.Enter()"]
    click C "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L80" "打开源码"
    D["SaveManager.GetBaseBlueprintPath()"]
    click D "../Assets/Scripts/Features/Building/SaveManager.cs#L104" "打开源码"
    E["SiegeConstructionSettings.Validate()"]
    click E "../Assets/Scripts/Features/Siege/Gameplay/SiegeConstructionSettings.cs#L30" "打开源码"
    F["SiegeGameplayStage.Deploy()"]
    click F "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L177" "打开源码"
    G["SiegeBase.Create()"]
    click G "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L48" "打开源码"
    H["BuildingRuntime.HasFullFoundationSupport()"]
    click H "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L307" "打开源码"
    I["SiegeGameplayStage.BeginWave()"]
    click I "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L244" "打开源码"
    J["SiegeWaveDirector.Tick()"]
    click J "../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46" "打开源码"
    K["SiegeBase.Repair()"]
    click K "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151" "打开源码"
    L["BuildingRefit.Apply()"]
    click L "../Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs#L9" "打开源码"
    M["SiegeGameplayStage.Settle()"]
    click M "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L491" "打开源码"
    N["GameModeController.ReturnRoutine()"]
    click N "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L289" "打开源码"
    O["SiegeGameplayStage.Exit()"]
    click O "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L536" "打开源码"
    P["PlayDraft.Restore()"]
    click P "../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L55" "打开源码"
    A -.->|保存后选择模式| B
    B --> C
    C --> D
    C --> E
    C -->|出生点自动部署| F
    F --> G
    F --> H
    F -.->|用户开始| I
    I -.->|Unity Update| J
    J -.->|整备阶段用户维修| K
    K --> L
    G -.->|核心销毁事件| M
    I -.->|用户撤退| M
    M -.->|用户返回| N
    N --> O
    N --> P
```

- [SiegeBaseWorkshop.DesignateCore](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L68)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs|DesignateCore -->
- [GameModeController.LoadMode](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L121)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|LoadMode -->
- [SiegeGameplayStage.Enter](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L80)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|Enter -->
- [SaveManager.GetBaseBlueprintPath](../Assets/Scripts/Features/Building/SaveManager.cs#L104)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|GetBaseBlueprintPath -->
- [SiegeConstructionSettings.Validate](../Assets/Scripts/Features/Siege/Gameplay/SiegeConstructionSettings.cs#L30)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeConstructionSettings.cs|Validate -->
- [SiegeGameplayStage.Deploy](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L177)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|Deploy -->
- [SiegeBase.Create](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L48)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Create -->
- [BuildingRuntime.HasFullFoundationSupport](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L307)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|HasFullFoundationSupport -->
- [SiegeGameplayStage.BeginWave](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L244)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|BeginWave -->
- [SiegeWaveDirector.Tick](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs|Tick -->
- [SiegeBase.Repair](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Repair -->
- [BuildingRefit.Apply](../Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs#L9)
<!-- source: Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs|Apply -->
- [SiegeGameplayStage.Settle](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L491)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|Settle -->
- [GameModeController.ReturnRoutine](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L289)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|ReturnRoutine -->
- [SiegeGameplayStage.Exit](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L536)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|Exit -->
- [PlayDraft.Restore](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L55)
<!-- source: Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs|Restore -->

## 基地独立库与结构编辑

源码推导：菜单冷读与Main目录初始化复用同一迁移；仅已标核心且非据点的旧建筑复制到BaseBlueprints，校验字节并归档原件，同名不覆盖。基地和普通建筑共用结构编辑与连接规则，显式目标决定列表、文件路径和CRUD；进入结构编辑恢复输入，使用独立建造距离，基地隐藏据点表单。

```mermaid
flowchart TD
    A["SaveManager.EnsureSaveDirectories()"]
    click A "../Assets/Scripts/Features/Building/SaveManager.cs#L67" "打开源码"
    B["BaseBlueprintMigration.Run()"]
    click B "../Assets/Scripts/Features/Building/BlueprintLoading/BaseBlueprintMigration.cs#L11" "打开源码"
    C["SaveManager.GetBaseBlueprintFiles()"]
    click C "../Assets/Scripts/Features/Building/SaveManager.cs#L94" "打开源码"
    D["MenuBlueprintLibrary.Files()"]
    click D "../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L49" "打开源码"
    E["SaveManager.LoadBaseBlueprint()"]
    click E "../Assets/Scripts/Features/Building/SaveManager.cs#L138" "打开源码"
    F["BuildManager.EnterBaseBlueprintBuildMode()"]
    click F "../Assets/Scripts/Features/Building/BuildManager.cs#L320" "打开源码"
    G["BuildManager.EnterStructureBlueprintBuildMode()"]
    click G "../Assets/Scripts/Features/Building/BuildManager.cs#L325" "打开源码"
    H["SaveManager.GetAllStructureBlueprintNames()"]
    click H "../Assets/Scripts/Features/Building/SaveManager.cs#L126" "打开源码"
    I["SaveManager.GetEditedStructurePath()"]
    click I "../Assets/Scripts/Features/Building/SaveManager.cs#L114" "打开源码"
    J["BuildingEditorPanel.Refresh()"]
    click J "../Assets/Scripts/Features/Building/BuildingEditor/BuildingEditorPanel.cs#L32" "打开源码"
    K["BuildManager.LoadAllBlocks()"]
    click K "../Assets/Scripts/Features/Building/BuildManager.cs#L701" "打开源码"
    L["SaveManager.RenameBuildingBlueprint()"]
    click L "../Assets/Scripts/Features/Building/SaveManager.cs#L221" "打开源码"
    M["SaveManager.DeleteBuildingBlueprint()"]
    click M "../Assets/Scripts/Features/Building/SaveManager.cs#L191" "打开源码"
    N["InputManager.EnterBuildMode()"]
    click N "../Assets/Scripts/Features/Player/InputManager.cs#L163" "打开源码"
    O["SaveManager.RenameBlueprint()"]
    click O "../Assets/Scripts/Features/Building/SaveManager.cs#L460" "打开源码"
    P["SaveManager.FilesFor()"]
    click P "../Assets/Scripts/Features/Building/SaveManager.cs#L32" "打开源码"
    A --> B
    D --> C
    C --> B
    E --> F
    F --> G
    G --> H
    G --> I
    G --> N
    G --> K
    K -.->|底座准备后刷新| J
    L --> O
    O --> P
    M --> I
```

- [SaveManager.EnsureSaveDirectories](../Assets/Scripts/Features/Building/SaveManager.cs#L67)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|EnsureSaveDirectories -->
- [BaseBlueprintMigration.Run](../Assets/Scripts/Features/Building/BlueprintLoading/BaseBlueprintMigration.cs#L11)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BaseBlueprintMigration.cs|Run -->
- [SaveManager.GetBaseBlueprintFiles](../Assets/Scripts/Features/Building/SaveManager.cs#L94)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|GetBaseBlueprintFiles -->
- [MenuBlueprintLibrary.Files](../Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs#L49)
<!-- source: Assets/Scripts/Features/MainMenu/Library/MenuBlueprintLibrary.cs|Files -->
- [SaveManager.LoadBaseBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L138)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadBaseBlueprint -->
- [BuildManager.EnterBaseBlueprintBuildMode](../Assets/Scripts/Features/Building/BuildManager.cs#L320)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|EnterBaseBlueprintBuildMode -->
- [BuildManager.EnterStructureBlueprintBuildMode](../Assets/Scripts/Features/Building/BuildManager.cs#L325)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|EnterStructureBlueprintBuildMode -->
- [SaveManager.GetAllStructureBlueprintNames](../Assets/Scripts/Features/Building/SaveManager.cs#L126)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|GetAllStructureBlueprintNames -->
- [SaveManager.GetEditedStructurePath](../Assets/Scripts/Features/Building/SaveManager.cs#L114)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|GetEditedStructurePath -->
- [BuildingEditorPanel.Refresh](../Assets/Scripts/Features/Building/BuildingEditor/BuildingEditorPanel.cs#L32)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingEditorPanel.cs|Refresh -->
- [BuildManager.LoadAllBlocks](../Assets/Scripts/Features/Building/BuildManager.cs#L701)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|LoadAllBlocks -->
- [SaveManager.RenameBuildingBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L221)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|RenameBuildingBlueprint -->
- [SaveManager.DeleteBuildingBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L191)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|DeleteBuildingBlueprint -->
- [InputManager.EnterBuildMode](../Assets/Scripts/Features/Player/InputManager.cs#L163)
<!-- source: Assets/Scripts/Features/Player/InputManager.cs|EnterBuildMode -->
- [SaveManager.RenameBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L460)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|RenameBlueprint -->
- [SaveManager.FilesFor](../Assets/Scripts/Features/Building/SaveManager.cs#L32)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|FilesFor -->

## 机库建造、独占部署与返航结算

源码推导：机库尺寸作为Block数据随基地保存。编辑按钮经几何/占位检查提交可撤销尺寸动作；生成真实外壳与底部支撑。两宿主共用容量筛选、居中部署和独占槽位。固定物理步按实际有电推进力导航、开门及入库；失能、损毁机库或超时则报废。全部返回结束才一次性入账，再执行原蓝图付费修理；修理恢复的机库按原Block身份重连槽位。退出解除动力请求和导航，销毁归属副场景的舰队。

```mermaid
flowchart TD
    A["SiegeBaseWorkshop.SelectHangar()"]
    click A "../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L43" "打开源码"
    B["BuildManager.CreateBlock()"]
    click B "../Assets/Scripts/Features/Building/BuildManager.cs#L597" "打开源码"
    C["HangarResizeButton.Resize()"]
    click C "../Assets/Scripts/Features/Siege/Hangars/HangarResizeButton.cs#L30" "打开源码"
    D["HangarModule.TryResize()"]
    click D "../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L289" "打开源码"
    E["HangarModule.RefreshGeometry()"]
    click E "../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L67" "打开源码"
    F["BuildManager.SaveBlock()"]
    click F "../Assets/Scripts/Features/Building/BuildManager.cs#L675" "打开源码"
    G["SiegeDeploymentView.RefreshOptions()"]
    click G "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L246" "打开源码"
    H["SiegeFleet.FitsHangar()"]
    click H "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L90" "打开源码"
    I["SiegeFleet.DeployAt()"]
    click I "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L164" "打开源码"
    J["HangarModule.GetDockPose()"]
    click J "../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L148" "打开源码"
    K["SiegeReturnFlight.BeginLaunch()"]
    click K "../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L68" "打开源码"
    L["SiegeMatch.ResolveAttacker()"]
    click L "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142" "打开源码"
    M["SiegeReturnFlight.BeginReturn()"]
    click M "../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L73" "打开源码"
    N["SiegeReturnFlight.Tick()"]
    click N "../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L267" "打开源码"
    O["HangarModule.FixedUpdate()"]
    click O "../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L212" "打开源码"
    P["SiegeFleet.Scrap()"]
    click P "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L113" "打开源码"
    Q["SiegeMatch.CompleteReturn()"]
    click Q "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L189" "打开源码"
    R["SiegeBase.Repair()"]
    click R "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151" "打开源码"
    S["SiegeFleet.GetHangar()"]
    click S "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L66" "打开源码"
    T["SiegeReturnFlight.Stop()"]
    click T "../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L465" "打开源码"
    U["SiegeSession.FixedUpdate()"]
    click U "../Assets/Scripts/Features/Siege/SiegeSession.cs#L417" "打开源码"
    V["SiegeGameplayStage.FixedUpdate()"]
    click V "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L405" "打开源码"
    A -.->|确认放置| B
    B --> F
    C --> D
    D --> E
    D --> F
    G --> H
    I --> H
    I --> J
    I -.->|开始波次| K
    L -.->|清波后Returning| M
    K -.->|固定物理步| N
    M -.->|固定物理步| N
    U --> N
    V --> N
    N -.->|门请求| O
    N -->|失能或超时| P
    N -.->|全部完成后| Q
    Q -.->|付费整备| R
    R -.->|缺件恢复| S
    T -.->|解除动力请求与门请求| O
```

- [SiegeBaseWorkshop.SelectHangar](../Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs#L43)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeBaseWorkshop.cs|SelectHangar -->
- [BuildManager.CreateBlock](../Assets/Scripts/Features/Building/BuildManager.cs#L597)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|CreateBlock -->
- [HangarResizeButton.Resize](../Assets/Scripts/Features/Siege/Hangars/HangarResizeButton.cs#L30)
<!-- source: Assets/Scripts/Features/Siege/Hangars/HangarResizeButton.cs|Resize -->
- [HangarModule.TryResize](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L289)
<!-- source: Assets/Scripts/Features/Siege/Hangars/HangarModule.cs|TryResize -->
- [HangarModule.RefreshGeometry](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L67)
<!-- source: Assets/Scripts/Features/Siege/Hangars/HangarModule.cs|RefreshGeometry -->
- [BuildManager.SaveBlock](../Assets/Scripts/Features/Building/BuildManager.cs#L675)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|SaveBlock -->
- [SiegeDeploymentView.RefreshOptions](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L246)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|RefreshOptions -->
- [SiegeFleet.FitsHangar](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L90)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|FitsHangar -->
- [SiegeFleet.DeployAt](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L164)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|DeployAt -->
- [HangarModule.GetDockPose](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L148)
<!-- source: Assets/Scripts/Features/Siege/Hangars/HangarModule.cs|GetDockPose -->
- [SiegeReturnFlight.BeginLaunch](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L68)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs|BeginLaunch -->
- [SiegeMatch.ResolveAttacker](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|ResolveAttacker -->
- [SiegeReturnFlight.BeginReturn](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L73)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs|BeginReturn -->
- [SiegeReturnFlight.Tick](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L267)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs|Tick -->
- [HangarModule.FixedUpdate](../Assets/Scripts/Features/Siege/Hangars/HangarModule.cs#L212)
<!-- source: Assets/Scripts/Features/Siege/Hangars/HangarModule.cs|FixedUpdate -->
- [SiegeFleet.Scrap](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L113)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|Scrap -->
- [SiegeMatch.CompleteReturn](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L189)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|CompleteReturn -->
- [SiegeBase.Repair](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Repair -->
- [SiegeFleet.GetHangar](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L66)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|GetHangar -->
- [SiegeReturnFlight.Stop](../Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs#L465)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeReturnFlight.cs|Stop -->
- [SiegeSession.FixedUpdate](../Assets/Scripts/Features/Siege/SiegeSession.cs#L417)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|FixedUpdate -->
- [SiegeGameplayStage.FixedUpdate](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L405)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|FixedUpdate -->

## GPU特效对象池与释放

源码推导：VFX Graph 在GPU模拟粒子；场景池按模板复用对象，没有活动对象数量硬上限，只限制闲置缓存。爆炸、传送门、残骸和陨石尾迹共用Rent，复位参数后播放，保留尾烟延时，再归还或清理。FinishPlayback若略早于截止时间触发会重新预约，避免丢失回收；场景卸载释放池及子实例。

```mermaid
flowchart TD
    A["BlockVfxLibrary.Play()"]
    click A "../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112" "打开源码"
    B["VfxEffectPool.Rent()"]
    click B "../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L25" "打开源码"
    C["VfxEffect.ResetForRent()"]
    click C "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L209" "打开源码"
    D["VfxEffect.PlayOnce()"]
    click D "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L182" "打开源码"
    E["VfxEffect.ReleaseAfterPlayback()"]
    click E "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252" "打开源码"
    F["VfxEffect.FinishPlayback()"]
    click F "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L275" "打开源码"
    G["VfxEffectPool.Return()"]
    click G "../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L72" "打开源码"
    H["DetachedPartSmokeTrail.Initialize()"]
    click H "../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L62" "打开源码"
    I["VfxEffectPool.OnDestroy()"]
    click I "../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L112" "打开源码"
    J["BlockVfxLibrary.PlayTemplate()"]
    click J "../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L135" "打开源码"
    A --> J
    J --> B
    B --> C
    J --> D
    J --> E
    H --> B
    E -.->|到期| F
    F -.->|未到截止时间时补约| F
    F -->|到期| G
    G -.->|场景卸载| I
```

- [BlockVfxLibrary.Play](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs|Play -->
- [VfxEffectPool.Rent](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L25)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs|Rent -->
- [VfxEffect.ResetForRent](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L209)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|ResetForRent -->
- [VfxEffect.PlayOnce](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L182)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|PlayOnce -->
- [VfxEffect.ReleaseAfterPlayback](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|ReleaseAfterPlayback -->
- [VfxEffect.FinishPlayback](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L275)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|FinishPlayback -->
- [VfxEffectPool.Return](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L72)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs|Return -->
- [DetachedPartSmokeTrail.Initialize](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L62)
<!-- source: Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs|Initialize -->
- [VfxEffectPool.OnDestroy](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L112)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs|OnDestroy -->
- [BlockVfxLibrary.PlayTemplate](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L135)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs|PlayTemplate -->

## 残骸烟迹、激光与炮弹曳光

源码推导：烟迹按刚体点速度和局部锚点跟随，中低速保留最低不透明度；Graph 共用烟粒绘制短促余火；局部火焰按同刚体模型包围盒包裹并跟随中心/旋转，停止后独立老化。激光有独立库模板和显示持续时间；炮弹物理仍由 TurretProjectile 负责，LateUpdate 只读取实际位置历史绘制短迹，池回收清空。

```mermaid
flowchart TD
    A["DetachedPartSmokeTrail.Attach()"]
    click A "../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L37" "打开源码"
    B["DetachedPartSmokeTrail.Initialize()"]
    click B "../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L62" "打开源码"
    C["DetachedPartSmokeTrail.Update()"]
    click C "../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L142" "打开源码"
    D["DetachedPartSmokeTrail.StopAndRelease()"]
    click D "../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L179" "打开源码"
    M["DetachedPartSmokeTrail.ConfigureEnvelope()"]
    click M "../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L84" "打开源码"
    N["DetachedPartSmokeTrail.UpdateEnvelope()"]
    click N "../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L114" "打开源码"
    E["VfxEffect.ReleaseAfterPlayback()"]
    click E "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252" "打开源码"
    F["LaserTurret.Fire()"]
    click F "../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L35" "打开源码"
    G["StylizedBeamEffect.SetVisible()"]
    click G "../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L54" "打开源码"
    H["TurretProjectile.Launch()"]
    click H "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L65" "打开源码"
    I["ProjectileTracer.Begin()"]
    click I "../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L21" "打开源码"
    J["ProjectileTracer.LateUpdate()"]
    click J "../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L39" "打开源码"
    K["StylizedBeamEffect.SetEndpoints()"]
    click K "../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L29" "打开源码"
    L["ProjectileTracer.OnDisable()"]
    click L "../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L68" "打开源码"
    A --> B
    B --> M
    M --> N
    C --> N
    B -.->|Unity Update| C
    C -->|超时/对象失效| D
    D --> E
    F --> G
    H --> I
    I -.->|Unity LateUpdate| J
    J --> K
    J --> G
    L --> G
```

- [DetachedPartSmokeTrail.Attach](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L37)
<!-- source: Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs|Attach -->
- [DetachedPartSmokeTrail.Initialize](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L62)
<!-- source: Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs|Initialize -->
- [DetachedPartSmokeTrail.Update](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L142)
<!-- source: Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs|Update -->
- [DetachedPartSmokeTrail.StopAndRelease](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L179)
<!-- source: Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs|StopAndRelease -->
- [DetachedPartSmokeTrail.ConfigureEnvelope](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L84)
<!-- source: Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs|ConfigureEnvelope -->
- [DetachedPartSmokeTrail.UpdateEnvelope](../Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs#L114)
<!-- source: Assets/Scripts/Features/Presentation/Effects/DetachedPartSmokeTrail.cs|UpdateEnvelope -->
- [VfxEffect.ReleaseAfterPlayback](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|ReleaseAfterPlayback -->
- [LaserTurret.Fire](../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L35)
<!-- source: Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs|Fire -->
- [StylizedBeamEffect.SetVisible](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L54)
<!-- source: Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs|SetVisible -->
- [TurretProjectile.Launch](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L65)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs|Launch -->
- [ProjectileTracer.Begin](../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L21)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs|Begin -->
- [ProjectileTracer.LateUpdate](../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L39)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs|LateUpdate -->
- [StylizedBeamEffect.SetEndpoints](../Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs#L29)
<!-- source: Assets/Scripts/Features/Presentation/Effects/StylizedBeamEffect.cs|SetEndpoints -->
- [ProjectileTracer.OnDisable](../Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs#L68)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/ProjectileTracer.cs|OnDisable -->

## 特效集中配置与挂载生命周期

源码推导：制作工具只创建效果编号挂载点，统一库持有模板引用。首次启用/设置参数生成一次内容；组合控制器聚合子挂载点，禁用清理并保留内容供复用，销毁随拥有者释放。替换库影响新实例，不做逐帧热更新。

```mermaid
flowchart TD
    A["BlockVfxBaker.Populate()"]
    click A "../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L64" "打开源码"
    B["VfxCatalogMigration.Run()"]
    click B "../Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs#L18" "打开源码"
    C["VfxEffect.OnEnable()"]
    click C "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L97" "打开源码"
    D["VfxEffect.EnsureContent()"]
    click D "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L47" "打开源码"
    E["BlockVfxLibrary.Resolve()"]
    click E "../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L53" "打开源码"
    F["VfxEffect.SetIntensity()"]
    click F "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L106" "打开源码"
    G["VfxEffect.Clear()"]
    click G "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L167" "打开源码"
    H["VfxEffect.OnDisable()"]
    click H "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L302" "打开源码"
    I["VfxEffect.StopAndRelease()"]
    click I "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L264" "打开源码"
    J["VfxEffect.ReleaseAfterPlayback()"]
    click J "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252" "打开源码"
    A -.->|保存挂载点后实例化| C
    B -.->|迁移资产后实例化| C
    C --> D
    D --> E
    C -->|自动播放| F
    F --> D
    H --> G
    I --> F
    I --> J
```

- [BlockVfxBaker.Populate](../Assets/Editor/Presentation/Effects/BlockVfxBaker.cs#L64)
<!-- source: Assets/Editor/Presentation/Effects/BlockVfxBaker.cs|Populate -->
- [VfxCatalogMigration.Run](../Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs#L18)
<!-- source: Assets/Editor/Presentation/Resources/VfxCatalogMigration.cs|Run -->
- [VfxEffect.OnEnable](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L97)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|OnEnable -->
- [VfxEffect.EnsureContent](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L47)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|EnsureContent -->
- [BlockVfxLibrary.Resolve](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L53)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs|Resolve -->
- [VfxEffect.SetIntensity](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L106)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|SetIntensity -->
- [VfxEffect.Clear](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L167)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|Clear -->
- [VfxEffect.OnDisable](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L302)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|OnDisable -->
- [VfxEffect.StopAndRelease](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L264)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|StopAndRelease -->
- [VfxEffect.ReleaseAfterPlayback](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|ReleaseAfterPlayback -->

## 特效与音频资源管理（Editor）

源码推导：窗口共用 GUID 资源索引、名称/路径搜索及直接引用查询；原始资产通过 Unity Inspector/Prefab Mode 编辑。项目变更使索引失效，关闭窗口释放内嵌 Inspector。运行时继续使用现有 BlockVfxLibrary.Play。

```mermaid
flowchart TD
    A["PresentationResourceWindow.Open()"]
    click A "../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L25" "打开源码"
    B["PresentationResourceWindow.OnGUI()"]
    click B "../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L59" "打开源码"
    C["PresentationAssetIndex.Scan()"]
    click C "../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L37" "打开源码"
    D["PresentationAssetIndex.Matches()"]
    click D "../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L83" "打开源码"
    E["PresentationResourceWindow.DrawDetails()"]
    click E "../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L149" "打开源码"
    F["PresentationAssetIndex.FindReferencers()"]
    click F "../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L88" "打开源码"
    G["BlockVfxLibraryEditor.OnInspectorGUI()"]
    click G "../Assets/Editor/Presentation/Resources/BlockVfxLibraryEditor.cs#L10" "打开源码"
    H["PresentationResourceWindow.OnDisable()"]
    click H "../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L38" "打开源码"
    I["PresentationResourceWindow.ReleaseInspector()"]
    click I "../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L51" "打开源码"
    J["BlockVfxLibrary.Play()"]
    click J "../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112" "打开源码"
    K["PresentationResourceWindow.Invalidate()"]
    click K "../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L44" "打开源码"
    A -.->|Unity GUI| B
    B --> C
    B -->|DrawList| D
    B --> E
    E -->|按需查询| F
    E -.->|播放库 Inspector| G
    G -.->|保存后的资源配置| J
    K -.->|下一次 GUI 刷新| B
    H --> I
```

- [PresentationResourceWindow.Open](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L25)
<!-- source: Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs|Open -->
- [PresentationResourceWindow.OnGUI](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L59)
<!-- source: Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs|OnGUI -->
- [PresentationAssetIndex.Scan](../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L37)
<!-- source: Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs|Scan -->
- [PresentationAssetIndex.Matches](../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L83)
<!-- source: Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs|Matches -->
- [PresentationResourceWindow.DrawDetails](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L149)
<!-- source: Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs|DrawDetails -->
- [PresentationAssetIndex.FindReferencers](../Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs#L88)
<!-- source: Assets/Editor/Presentation/Resources/PresentationAssetIndex.cs|FindReferencers -->
- [BlockVfxLibraryEditor.OnInspectorGUI](../Assets/Editor/Presentation/Resources/BlockVfxLibraryEditor.cs#L10)
<!-- source: Assets/Editor/Presentation/Resources/BlockVfxLibraryEditor.cs|OnInspectorGUI -->
- [PresentationResourceWindow.OnDisable](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L38)
<!-- source: Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs|OnDisable -->
- [PresentationResourceWindow.ReleaseInspector](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L51)
<!-- source: Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs|ReleaseInspector -->
- [BlockVfxLibrary.Play](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs|Play -->
- [PresentationResourceWindow.Invalidate](../Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs#L44)
<!-- source: Assets/Editor/Presentation/Resources/PresentationResourceWindow.cs|Invalidate -->

## Unity 6.6 兼容查询与制作验证

源码推导：运行服务限定拥有者场景，制作工具限定活动场景；重复服务拒绝依赖对象顺序。VFX 17.6 桥接仅在 Editor，使用当前程序集和 GetGraph/CreateGraph；验证只修改临时副本。

```mermaid
flowchart TD
    A["PlayManager.PlayStart()"]
    click A "../Assets/Scripts/Features/Simulation/PlayManager.cs#L109" "打开源码"
    B["SceneObjectQuery.FindInScene()"]
    click B "../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L15" "打开源码"
    C["SceneObjectQuery.FindInActiveScene()"]
    click C "../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L9" "打开源码"
    D["SiegeThreatMarkers.Start()"]
    click D "../Assets/Scripts/Features/Siege/Interface/SiegeThreatMarkers.cs#L43" "打开源码"
    E["UpgradeCompatibilityChecks.CheckSceneQueries()"]
    click E "../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L131" "打开源码"
    F["UpgradeCompatibilityChecks.CheckVfxBridge()"]
    click F "../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L181" "打开源码"
    G["VfxGraphAuthoring.Graph()"]
    click G "../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L71" "打开源码"
    H["VfxGraphAuthoring.Save()"]
    click H "../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L79" "打开源码"
    I["UpgradeCompatibilityChecks.MigrateExhaustImporter()"]
    click I "../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L227" "打开源码"
    J["ProfilerCaptureAnalysis.Analyze()"]
    click J "../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L27" "打开源码"
    K["ProfilerCaptureAnalysis.AddCounters()"]
    click K "../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L204" "打开源码"
    L["ProfilerCaptureAnalysis.WriteCounters()"]
    click L "../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L260" "打开源码"
    A --> B
    C --> B
    D --> B
    E --> B
    E --> C
    F -->|Main模式入口| U
    F -->|旧Siege或组件夹具| G
    F --> H
    J --> K
    J --> L
```

- [PlayManager.PlayStart](../Assets/Scripts/Features/Simulation/PlayManager.cs#L109)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayStart -->
- [SceneObjectQuery.FindInScene](../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L15)
<!-- source: Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs|FindInScene -->
- [SceneObjectQuery.FindInActiveScene](../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L9)
<!-- source: Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs|FindInActiveScene -->
- [SiegeThreatMarkers.Start](../Assets/Scripts/Features/Siege/Interface/SiegeThreatMarkers.cs#L43)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeThreatMarkers.cs|Start -->
- [UpgradeCompatibilityChecks.CheckSceneQueries](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L131)
<!-- source: Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs|CheckSceneQueries -->
- [UpgradeCompatibilityChecks.CheckVfxBridge](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L181)
<!-- source: Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs|CheckVfxBridge -->
- [VfxGraphAuthoring.Graph](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L71)
<!-- source: Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs|Graph -->
- [VfxGraphAuthoring.Save](../Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs#L79)
<!-- source: Assets/Editor/Presentation/Effects/VfxGraphAuthoring.cs|Save -->
- [UpgradeCompatibilityChecks.MigrateExhaustImporter](../Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs#L227)
<!-- source: Assets/Editor/Diagnostics/UpgradeCompatibilityChecks.cs|MigrateExhaustImporter -->
- [ProfilerCaptureAnalysis.Analyze](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L27)
<!-- source: Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs|Analyze -->
- [ProfilerCaptureAnalysis.AddCounters](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L204)
<!-- source: Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs|AddCounters -->
- [ProfilerCaptureAnalysis.WriteCounters](../Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs#L260)
<!-- source: Assets/Editor/Diagnostics/ProfilerCaptureAnalysis.cs|WriteCounters -->

## 断片最短物理保留期

源码推导：建筑无论组大小与预算均先创建完整物理残骸。建筑、载具与阵地战敌方预算共用FragmentLifetime；计时器到期重查新碎片年龄，最短期内允许暂超软预算。该保护不阻止实际伤害、主动卸载或退出。载具新刚体保持源PhysicsScene和剩余碰撞保护。

```mermaid
flowchart TD
    A["WorldBuildingManager.CreateDebris()"]
    click A "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L431" "打开源码"
    B["RuntimeGroupBuilder.Rebuild()"]
    click B "../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L16" "打开源码"
    C["FragmentLifetime.Mark()"]
    click C "../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L16" "打开源码"
    D["FragmentLifetime.Remaining()"]
    click D "../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L31" "打开源码"
    E["SiegeWreckBudget.Tick()"]
    click E "../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L34" "打开源码"
    F["FragmentLifetime.CanRetire()"]
    click F "../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L37" "打开源码"
    G["SalvageCleanupSession.CleanupGroupAfterDelay()"]
    click G "../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L95" "打开源码"
    H["SalvageCleanupSession.CleanupUnitAfterDelay()"]
    click H "../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L108" "打开源码"
    I["SalvageCleanupSession.WaitForFragment()"]
    click I "../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L161" "打开源码"
    A --> C
    A -->|安排清理至少等待保护期| D
    B --> C
    E --> F
    G -.-> I
    I --> D
    H -.->|旧单位计时器重查新片段| D
```

- [WorldBuildingManager.CreateDebris](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L431)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|CreateDebris -->
- [RuntimeGroupBuilder.Rebuild](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L16)
<!-- source: Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs|Rebuild -->
- [FragmentLifetime.Mark](../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L16)
<!-- source: Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs|Mark -->
- [FragmentLifetime.Remaining](../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L31)
<!-- source: Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs|Remaining -->
- [SiegeWreckBudget.Tick](../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L34)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs|Tick -->
- [FragmentLifetime.CanRetire](../Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs#L37)
<!-- source: Assets/Scripts/Features/Simulation/Physics/FragmentLifetime.cs|CanRetire -->
- [SalvageCleanupSession.CleanupGroupAfterDelay](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L95)
<!-- source: Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs|CleanupGroupAfterDelay -->
- [SalvageCleanupSession.CleanupUnitAfterDelay](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L108)
<!-- source: Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs|CleanupUnitAfterDelay -->
- [SalvageCleanupSession.WaitForFragment](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L161)
<!-- source: Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs|WaitForFragment -->

## 共享几何与编辑恢复

源码推导：六处包围盒消费者共用八角变换，原点包含和镜头网格下限留在领域入口。创建/删除动作共享稳定身份、精确世界姿态、尺寸和货仓恢复；生产蓝图工厂是唯一实体恢复机制。

```mermaid
flowchart TD
    A["OutpostBaseDesigner.BoundsOf()"]
    click A "../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L9" "打开源码"
    B["OutpostDeploymentPreview.CalculateBounds()"]
    click B "../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L161" "打开源码"
    C["WorldSessionManager.ConstructBounds()"]
    click C "../Assets/Scripts/Features/World/WorldSessionManager.cs#L458" "打开源码"
    D["WorldPlayerStreamingGuard.CaptureLocalBounds()"]
    click D "../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L133" "打开源码"
    E["OutpostEncounterDirector.DeploymentBounds()"]
    click E "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L279" "打开源码"
    F["BlueprintLoadCamera.CalculateBlockDataBounds()"]
    click F "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L136" "打开源码"
    G["BlockBounds.FromBlueprint()"]
    click G "../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L8" "打开源码"
    H["BlockBounds.FromConstruct()"]
    click H "../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L29" "打开源码"
    I["BlockBounds.Transform()"]
    click I "../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L42" "打开源码"
    J["CreateBlockAction.Redo()"]
    click J "../Assets/Scripts/Features/Building/Actions/AddBlockAction.cs#L18" "打开源码"
    K["DeleteBlockAction.Undo()"]
    click K "../Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs#L13" "打开源码"
    L["BlockActionState.Restore()"]
    click L "../Assets/Scripts/Features/Building/Actions/BlockActionState.cs#L43" "打开源码"
    M["BlueprintBlockFactory.TryRestore()"]
    click M "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7" "打开源码"
    N["CreateBlockAction.Undo()"]
    click N "../Assets/Scripts/Features/Building/Actions/AddBlockAction.cs#L13" "打开源码"
    O["DeleteBlockAction.Redo()"]
    click O "../Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs#L18" "打开源码"
    P["BlockActionState.Remove()"]
    click P "../Assets/Scripts/Features/Building/Actions/BlockActionState.cs#L25" "打开源码"
    Q["SaveManager.LoadSave()"]
    click Q "../Assets/Scripts/Features/Building/SaveManager.cs#L356" "打开源码"
    R["SaveManager.LoadEnemyBlueprint()"]
    click R "../Assets/Scripts/Features/Building/SaveManager.cs#L361" "打开源码"
    S["SaveManager.LoadBlueprint()"]
    click S "../Assets/Scripts/Features/Building/SaveManager.cs#L366" "打开源码"
    T["Bot.CanLaunchForWork()"]
    click T "../Assets/Scripts/Features/Bots/Bot.cs#L128" "打开源码"
    U["Bot.SetHomePowerRequested()"]
    click U "../Assets/Scripts/Features/Bots/Bot.cs#L114" "打开源码"
    V["RepairBot.FixedUpdate()"]
    click V "../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L37" "打开源码"
    W["CollectionBot.FixedUpdate()"]
    click W "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31" "打开源码"
    A --> G
    B --> G
    C --> H
    D --> H
    E --> G
    F --> I
    G --> I
    H --> I
    J --> L
    K --> L
    L --> M
    N --> P
    O --> P
    Q --> S
    R --> S
    V --> T
    W --> T
    T --> U
```

- [OutpostBaseDesigner.BoundsOf](../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L9)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs|BoundsOf -->
- [OutpostDeploymentPreview.CalculateBounds](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L161)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs|CalculateBounds -->
- [WorldSessionManager.ConstructBounds](../Assets/Scripts/Features/World/WorldSessionManager.cs#L458)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|ConstructBounds -->
- [WorldPlayerStreamingGuard.CaptureLocalBounds](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L133)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs|CaptureLocalBounds -->
- [OutpostEncounterDirector.DeploymentBounds](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L279)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|DeploymentBounds -->
- [BlueprintLoadCamera.CalculateBlockDataBounds](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L136)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs|CalculateBlockDataBounds -->
- [BlockBounds.FromBlueprint](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L8)
<!-- source: Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs|FromBlueprint -->
- [BlockBounds.FromConstruct](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L29)
<!-- source: Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs|FromConstruct -->
- [BlockBounds.Transform](../Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs#L42)
<!-- source: Assets/Scripts/Features/Simulation/Geometry/BlockBounds.cs|Transform -->
- [CreateBlockAction.Redo](../Assets/Scripts/Features/Building/Actions/AddBlockAction.cs#L18)
<!-- source: Assets/Scripts/Features/Building/Actions/AddBlockAction.cs|Redo -->
- [DeleteBlockAction.Undo](../Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs#L13)
<!-- source: Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs|Undo -->
- [BlockActionState.Restore](../Assets/Scripts/Features/Building/Actions/BlockActionState.cs#L43)
<!-- source: Assets/Scripts/Features/Building/Actions/BlockActionState.cs|Restore -->
- [BlueprintBlockFactory.TryRestore](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs|TryRestore -->
- [CreateBlockAction.Undo](../Assets/Scripts/Features/Building/Actions/AddBlockAction.cs#L13)
<!-- source: Assets/Scripts/Features/Building/Actions/AddBlockAction.cs|Undo -->
- [DeleteBlockAction.Redo](../Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs#L18)
<!-- source: Assets/Scripts/Features/Building/Actions/DeleteBlockAction.cs|Redo -->
- [BlockActionState.Remove](../Assets/Scripts/Features/Building/Actions/BlockActionState.cs#L25)
<!-- source: Assets/Scripts/Features/Building/Actions/BlockActionState.cs|Remove -->
- [SaveManager.LoadSave](../Assets/Scripts/Features/Building/SaveManager.cs#L356)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadSave -->
- [SaveManager.LoadEnemyBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L361)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadEnemyBlueprint -->
- [SaveManager.LoadBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L366)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadBlueprint -->
- [Bot.CanLaunchForWork](../Assets/Scripts/Features/Bots/Bot.cs#L128)
<!-- source: Assets/Scripts/Features/Bots/Bot.cs|CanLaunchForWork -->
- [Bot.SetHomePowerRequested](../Assets/Scripts/Features/Bots/Bot.cs#L114)
<!-- source: Assets/Scripts/Features/Bots/Bot.cs|SetHomePowerRequested -->
- [RepairBot.FixedUpdate](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L37)
<!-- source: Assets/Scripts/Features/Bots/Repair/RepairBot.cs|FixedUpdate -->
- [CollectionBot.FixedUpdate](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|FixedUpdate -->

## CSV 方块配置与质量

源码推导：CSV 仅管理 25 个常用参数（另有 4 个身份列），高级参数由 Prefab Inspector 管理；Editor 导入后延迟校验、备份并同步 Prefab。旧 density 列已移除，Block 字段保留兼容；BotContainer 要求最低功率等于额定功率，供电只作为起飞门槛，离仓 Bot 继续任务/返航。OnValidate 只排队，不在导入线程写资产；Awake 提前应用配置，随后各组件初始化。空 mass 为体积质量，存档恢复保留实例尺寸再刷新派生数据；运行时不执行制作期组件创建和连接重建，尺寸变化时才重建连接。

```mermaid
flowchart TD
    A["BlockCsvPostprocessor.OnPostprocessAllAssets()"]
    click A "../Assets/Editor/Building/BlockDataTables/BlockCsvPostprocessor.cs#L8" "打开源码"
    B["BlockCsvAuthoring.QueueBake()"]
    click B "../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L239" "打开源码"
    C["BlockCsvAuthoring.Update()"]
    click C "../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L287" "打开源码"
    D["BlockCsvAuthoring.Bake()"]
    click D "../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L128" "打开源码"
    E["BlockCsvDocument.Parse()"]
    click E "../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L33" "打开源码"
    F["BlockCsvDocument.Apply()"]
    click F "../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L73" "打开源码"
    G["Block.OnValidate()"]
    click G "../Assets/Scripts/Features/Simulation/Block.cs#L106" "打开源码"
    H["BlockCsvBinding.RequestValidation()"]
    click H "../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L33" "打开源码"
    I["BlockCsvBinding.Awake()"]
    click I "../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L23" "打开源码"
    J["BlockCsvBinding.Apply()"]
    click J "../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L41" "打开源码"
    K["Block.SetMassSettings()"]
    click K "../Assets/Scripts/Features/Simulation/Block.cs#L198" "打开源码"
    L["Block.RefreshMass()"]
    click L "../Assets/Scripts/Features/Simulation/Block.cs#L185" "打开源码"
    M["Block.RefreshAuthoringData()"]
    click M "../Assets/Scripts/Features/Simulation/Block.cs#L123" "打开源码"
    N["BlueprintBlockFactory.TryRestore()"]
    click N "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7" "打开源码"
    O["Block.RefreshRuntimeData()"]
    click O "../Assets/Scripts/Features/Simulation/Block.cs#L145" "打开源码"
    P["BlockCsvAuthoring.ValidatePowerDependencies()"]
    click P "../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L246" "打开源码"
    A --> B
    B -.->|Editor update| C
    C -->|有待同步请求| D
    D -->|先完整校验| E
    D -->|逐资源应用并保存，失败恢复| F
    G --> H
    H -.->|编辑器校验队列| C
    C -->|非持久实例| J
    I --> J
    J -->|首次读取缓存| E
    J --> F
    F --> K
    K --> L
    F -->|制作期| M
    F -->|运行时| O
    M --> O
    O --> L
    N -->|存档尺寸恢复后| O
    D -->|保存前检查供电依赖| P
```

- [BlockCsvPostprocessor.OnPostprocessAllAssets](../Assets/Editor/Building/BlockDataTables/BlockCsvPostprocessor.cs#L8)
<!-- source: Assets/Editor/Building/BlockDataTables/BlockCsvPostprocessor.cs|OnPostprocessAllAssets -->
- [BlockCsvAuthoring.QueueBake](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L239)
<!-- source: Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs|QueueBake -->
- [BlockCsvAuthoring.Update](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L287)
<!-- source: Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs|Update -->
- [BlockCsvAuthoring.Bake](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L128)
<!-- source: Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs|Bake -->
- [BlockCsvDocument.Parse](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L33)
<!-- source: Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs|Parse -->
- [BlockCsvDocument.Apply](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs#L73)
<!-- source: Assets/Scripts/Features/Building/BlockDataTables/BlockCsvDocument.cs|Apply -->
- [Block.OnValidate](../Assets/Scripts/Features/Simulation/Block.cs#L106)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|OnValidate -->
- [BlockCsvBinding.RequestValidation](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L33)
<!-- source: Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs|RequestValidation -->
- [BlockCsvBinding.Awake](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L23)
<!-- source: Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs|Awake -->
- [BlockCsvBinding.Apply](../Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs#L41)
<!-- source: Assets/Scripts/Features/Building/BlockDataTables/BlockCsvBinding.cs|Apply -->
- [Block.SetMassSettings](../Assets/Scripts/Features/Simulation/Block.cs#L198)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|SetMassSettings -->
- [Block.RefreshMass](../Assets/Scripts/Features/Simulation/Block.cs#L185)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|RefreshMass -->
- [Block.RefreshAuthoringData](../Assets/Scripts/Features/Simulation/Block.cs#L123)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|RefreshAuthoringData -->
- [BlueprintBlockFactory.TryRestore](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs|TryRestore -->
- [Block.RefreshRuntimeData](../Assets/Scripts/Features/Simulation/Block.cs#L145)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|RefreshRuntimeData -->
- [BlockCsvAuthoring.ValidatePowerDependencies](../Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs#L246)
<!-- source: Assets/Editor/Building/BlockDataTables/BlockCsvAuthoring.cs|ValidatePowerDependencies -->

## 启动与建造/游玩会话

引擎自动调用入口独立；按按钮进入业务主链。返回建造前先保存货仓；命名存档丢失、路径解析/读取/替换失败均拒绝退出并保留当前会话，失败原因由可编辑ReturnStatusPanel持续显示，不挡操作；成功后清除提示再清理。选择方块资源进入 BuildType.Build；InputManager 切换 lockView 仅取消拖拽/预览并隐藏操作轴，保留选中方块、高亮、BuildType 与资源，重锁恢复操作轴。工具按钮按BuildType着色，Build高亮当前资源条目；焦点不改变模式色。

```mermaid
flowchart TD
    A["GameManager.Start()"]
    click A "../Assets/Scripts/Features/Bootstrap/GameManager.cs#L17" "打开源码"
    B["GameManager.Init()"]
    click B "../Assets/Scripts/Features/Bootstrap/GameManager.cs#L25" "打开源码"
    C["SaveUIPanel.RefreshList()"]
    click C "../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L17" "打开源码"
    D["SaveManager.LoadSave()"]
    click D "../Assets/Scripts/Features/Building/SaveManager.cs#L356" "打开源码"
    E["BuildManager.LoadAllBlocks()"]
    click E "../Assets/Scripts/Features/Building/BuildManager.cs#L701" "打开源码"
    F["MainUIPanels.PlayStart()"]
    click F "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233" "打开源码"
    G["PlayManager.CanStartPlay()"]
    click G "../Assets/Scripts/Features/Simulation/PlayManager.cs#L158" "打开源码"
    H["PlayManager.PlayStart()"]
    click H "../Assets/Scripts/Features/Simulation/PlayManager.cs#L109" "打开源码"
    I["PlayManager.RefreshGroup()"]
    click I "../Assets/Scripts/Features/Simulation/PlayManager.cs#L193" "打开源码"
    J["OutpostEncounterDirector.Tick()"]
    click J "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51" "打开源码"
    K["MainUIPanels.PlayEnd()"]
    click K "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L287" "打开源码"
    L["CargoPersistence.SaveReturnCargo()"]
    click L "../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L11" "打开源码"
    M["PlayManager.PlayEnd()"]
    click M "../Assets/Scripts/Features/Simulation/PlayManager.cs#L295" "打开源码"
    N["MainUIPanels.PlayerDeath()"]
    click N "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L323" "打开源码"
    O["WorldSessionManager.RequestPlay()"]
    click O "../Assets/Scripts/Features/World/WorldSessionManager.cs#L188" "打开源码"
    P["WorldSessionManager.CompletePendingPlay()"]
    click P "../Assets/Scripts/Features/World/WorldSessionManager.cs#L212" "打开源码"
    Q["MainUIPanels.CompletePlayStart()"]
    click Q "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L267" "打开源码"
    R["MainUIPanels.CompletePlayEnd()"]
    click R "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304" "打开源码"
    S["WorldSessionManager.EndSession()"]
    click S "../Assets/Scripts/Features/World/WorldSessionManager.cs#L327" "打开源码"
    T["SaveManager.LoadBlueprint()"]
    click T "../Assets/Scripts/Features/Building/SaveManager.cs#L366" "打开源码"
    U["GameModeController.ShowSelection()"]
    click U "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L59" "打开源码"
    A -->|直接Main启动且无菜单宿主| B
    B --> C
    B --> D
    D --> T
    T --> E
    F --> G
    F -->|无模式宿主的组件夹具| O
    O -->|世界Ready| P
    P --> Q
    Q -->|再次校验载具| H
    F -->|未绑定世界的兼容入口| Q
    H --> I
    H -.->|后续游玩帧由WorldSessionManager.Update驱动| J
    K --> L
    K -->|保存成功| R
    R --> M
    M --> S
    M -->|无原稿时经 GameManager.Init| D
    N -.->|重生后才执行清理| R
    N -.->|用户重生/结束按钮| K
```

- [GameManager.Start](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L17)
<!-- source: Assets/Scripts/Features/Bootstrap/GameManager.cs|Start -->
- [GameManager.Init](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L25)
<!-- source: Assets/Scripts/Features/Bootstrap/GameManager.cs|Init -->
- [SaveUIPanel.RefreshList](../Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs#L17)
<!-- source: Assets/Scripts/Features/Building/Interface/SaveUIPanel.cs|RefreshList -->
- [SaveManager.LoadSave](../Assets/Scripts/Features/Building/SaveManager.cs#L356)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadSave -->
- [BuildManager.LoadAllBlocks](../Assets/Scripts/Features/Building/BuildManager.cs#L701)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|LoadAllBlocks -->
- [MainUIPanels.PlayStart](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayStart -->
- [PlayManager.CanStartPlay](../Assets/Scripts/Features/Simulation/PlayManager.cs#L158)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|CanStartPlay -->
- [PlayManager.PlayStart](../Assets/Scripts/Features/Simulation/PlayManager.cs#L109)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayStart -->
- [PlayManager.RefreshGroup](../Assets/Scripts/Features/Simulation/PlayManager.cs#L193)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|RefreshGroup -->
- [OutpostEncounterDirector.Tick](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Tick -->
- [MainUIPanels.PlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L287)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayEnd -->
- [CargoPersistence.SaveReturnCargo](../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L11)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs|SaveReturnCargo -->
- [PlayManager.PlayEnd](../Assets/Scripts/Features/Simulation/PlayManager.cs#L295)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayEnd -->
- [MainUIPanels.PlayerDeath](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L323)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayerDeath -->
- [WorldSessionManager.RequestPlay](../Assets/Scripts/Features/World/WorldSessionManager.cs#L188)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|RequestPlay -->
- [WorldSessionManager.CompletePendingPlay](../Assets/Scripts/Features/World/WorldSessionManager.cs#L212)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|CompletePendingPlay -->
- [MainUIPanels.CompletePlayStart](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L267)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|CompletePlayStart -->
- [MainUIPanels.CompletePlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|CompletePlayEnd -->
- [WorldSessionManager.EndSession](../Assets/Scripts/Features/World/WorldSessionManager.cs#L327)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|EndSession -->
- [SaveManager.LoadBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L366)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadBlueprint -->
- [GameModeController.ShowSelection](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L59)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|ShowSelection -->

## Main模式选择与副场景往返

源码推导：Main始终为活动场景并拥有共享服务。探索按需加载、绑定后准备；运行副本归Exploration，原稿在Main停用保留。新SiegeGameplay只提供可编辑场地及返回，不启动旧Siege。取消等待加载结束后卸载；失败清理后显示重试。货仓保存成功才返回，恢复未保存原稿与镜头。

```mermaid
flowchart TD
    A["MainUIPanels.PlayStart()"]
    click A "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233" "打开源码"
    B["GameModeController.ShowSelection()"]
    click B "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L59" "打开源码"
    C["GameModeController.Select()"]
    click C "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L101" "打开源码"
    D["GameModeController.GuardTransition()"]
    click D "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L235" "打开源码"
    E["GameModeController.LoadMode()"]
    click E "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L121" "打开源码"
    F["WorldSessionManager.BindHost()"]
    click F "../Assets/Scripts/Features/World/WorldSessionManager.cs#L53" "打开源码"
    G["WorldSessionManager.BeginPreparation()"]
    click G "../Assets/Scripts/Features/World/WorldSessionManager.cs#L99" "打开源码"
    H["WorldSessionManager.RequestPlay()"]
    click H "../Assets/Scripts/Features/World/WorldSessionManager.cs#L188" "打开源码"
    I["PlayManager.PlayStart()"]
    click I "../Assets/Scripts/Features/Simulation/PlayManager.cs#L109" "打开源码"
    J["GameModeController.CreateExplorationCopy()"]
    click J "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L191" "打开源码"
    K["PlayDraft.CreateRuntimeCopy()"]
    click K "../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L44" "打开源码"
    L["PlayDraft.Park()"]
    click L "../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L17" "打开源码"
    M["SiegeGameplayStage.Enter()"]
    click M "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L80" "打开源码"
    N["GameModeController.ReturnToBuild()"]
    click N "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L201" "打开源码"
    O["MainUIPanels.PlayEnd()"]
    click O "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L287" "打开源码"
    P["CargoPersistence.SaveReturnCargo()"]
    click P "../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L11" "打开源码"
    Q["GameModeController.CaptureReturnCargo()"]
    click Q "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L196" "打开源码"
    R["MainUIPanels.CompletePlayEnd()"]
    click R "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304" "打开源码"
    S["GameModeController.ExplorationEnded()"]
    click S "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L221" "打开源码"
    T["GameModeController.BeginReturn()"]
    click T "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L282" "打开源码"
    U["GameModeController.ReturnRoutine()"]
    click U "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L289" "打开源码"
    V["WorldSessionManager.StopSession()"]
    click V "../Assets/Scripts/Features/World/WorldSessionManager.cs#L545" "打开源码"
    W["SiegeGameplayStage.Exit()"]
    click W "../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L536" "打开源码"
    X["PlayDraft.Restore()"]
    click X "../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L55" "打开源码"
    Y["GameModeController.Retry()"]
    click Y "../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L230" "打开源码"
    Z["MainUIPanels.RestoreBuildPanels()"]
    click Z "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L315" "打开源码"
    A --> B
    C -.->|StartCoroutine| D
    D -->|推进并捕获异常| E
    E -->|探索绑定| F
    E --> G
    E --> H
    H -.->|Ready后经CompletePlayStart| I
    I --> J
    J --> K
    K --> L
    E -->|新Siege| L
    E --> M
    N -->|探索| O
    O --> P
    O -->|保存成功| Q
    O -->|保存成功| R
    R --> S
    S -->|无加载协程| T
    N -->|无加载协程| T
    D -->|取消或失败| T
    T -.->|StartCoroutine| U
    U --> V
    U --> W
    U -.->|卸载完成后| X
    U --> Z
    Y --> C
```

- [MainUIPanels.PlayStart](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayStart -->
- [GameModeController.ShowSelection](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L59)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|ShowSelection -->
- [GameModeController.Select](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L101)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|Select -->
- [GameModeController.GuardTransition](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L235)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|GuardTransition -->
- [GameModeController.LoadMode](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L121)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|LoadMode -->
- [WorldSessionManager.BindHost](../Assets/Scripts/Features/World/WorldSessionManager.cs#L53)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|BindHost -->
- [WorldSessionManager.BeginPreparation](../Assets/Scripts/Features/World/WorldSessionManager.cs#L99)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|BeginPreparation -->
- [WorldSessionManager.RequestPlay](../Assets/Scripts/Features/World/WorldSessionManager.cs#L188)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|RequestPlay -->
- [PlayManager.PlayStart](../Assets/Scripts/Features/Simulation/PlayManager.cs#L109)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayStart -->
- [GameModeController.CreateExplorationCopy](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L191)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|CreateExplorationCopy -->
- [PlayDraft.CreateRuntimeCopy](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L44)
<!-- source: Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs|CreateRuntimeCopy -->
- [PlayDraft.Park](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L17)
<!-- source: Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs|Park -->
- [SiegeGameplayStage.Enter](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L80)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|Enter -->
- [GameModeController.ReturnToBuild](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L201)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|ReturnToBuild -->
- [MainUIPanels.PlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L287)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayEnd -->
- [CargoPersistence.SaveReturnCargo](../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L11)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs|SaveReturnCargo -->
- [GameModeController.CaptureReturnCargo](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L196)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|CaptureReturnCargo -->
- [MainUIPanels.CompletePlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|CompletePlayEnd -->
- [GameModeController.ExplorationEnded](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L221)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|ExplorationEnded -->
- [GameModeController.BeginReturn](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L282)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|BeginReturn -->
- [GameModeController.ReturnRoutine](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L289)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|ReturnRoutine -->
- [WorldSessionManager.StopSession](../Assets/Scripts/Features/World/WorldSessionManager.cs#L545)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|StopSession -->
- [SiegeGameplayStage.Exit](../Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs#L536)
<!-- source: Assets/Scripts/Features/Siege/Gameplay/SiegeGameplayStage.cs|Exit -->
- [PlayDraft.Restore](../Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs#L55)
<!-- source: Assets/Scripts/Features/Simulation/Modes/PlayDraft.cs|Restore -->
- [GameModeController.Retry](../Assets/Scripts/Features/Simulation/Modes/GameModeController.cs#L230)
<!-- source: Assets/Scripts/Features/Simulation/Modes/GameModeController.cs|Retry -->
- [MainUIPanels.RestoreBuildPanels](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L315)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|RestoreBuildPanels -->

## 开发者建筑蓝图编辑

源码推导：Ctrl+Alt+F 进入/退出建筑，Ctrl+Alt+E 切换敌人。建筑使用独立目录；进入/打开不创建文件，只由明确新建写入。删除当前项切换剩余建筑，删空后清除目标/父对象并显示新建；不再使用default_building回退。加载父对象后由注入回调创建底座；底座不参与方块存档和操作栈。地面放置可越过当前范围；加载或完成时由旋转模块包围盒收紧占地并记录偏移，再刷新预览网格和连接，块坐标不变。

```mermaid
flowchart TD
    A["InputManager.Update()"]
    click A "../Assets/Scripts/Features/Player/InputManager.cs#L40" "打开源码"
    B["BuildManager.ToggleBuildingBlueprintBuildMode()"]
    click B "../Assets/Scripts/Features/Building/BuildManager.cs#L287" "打开源码"
    C["BuildManager.EnterBuildingBlueprintBuildMode()"]
    click C "../Assets/Scripts/Features/Building/BuildManager.cs#L302" "打开源码"
    D["SaveManager.CreateNewBuildingBlueprint()"]
    click D "../Assets/Scripts/Features/Building/SaveManager.cs#L162" "打开源码"
    E["BuildManager.LoadAllBlocks()"]
    click E "../Assets/Scripts/Features/Building/BuildManager.cs#L701" "打开源码"
    F["BlueprintLoader.Load()"]
    click F "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43" "打开源码"
    G["BuildManager.PrepareBuildingFoundation()"]
    click G "../Assets/Scripts/Features/Building/BuildManager.cs#L392" "打开源码"
    H["BuildingFoundation.Configure()"]
    click H "../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs#L17" "打开源码"
    I["BuildManager.CompleteBuildingBlueprint()"]
    click I "../Assets/Scripts/Features/Building/BuildManager.cs#L421" "打开源码"
    J["BuildingFootprint.Calculate()"]
    click J "../Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs#L8" "打开源码"
    K["BuildManager.RefreshBuildingFootprint()"]
    click K "../Assets/Scripts/Features/Building/BuildManager.cs#L453" "打开源码"
    L["Block.CheckConnection()"]
    click L "../Assets/Scripts/Features/Simulation/Block.cs#L361" "打开源码"
    M["BuildManager.ExitDeveloperBlueprintBuildMode()"]
    click M "../Assets/Scripts/Features/Building/BuildManager.cs#L267" "打开源码"
    N["SaveManager.LoadSave()"]
    click N "../Assets/Scripts/Features/Building/SaveManager.cs#L356" "打开源码"
    O["MainUIPanels.OnConfirmCreate()"]
    click O "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L167" "打开源码"
    P["MainUIPanels.OnConfirmDelete()"]
    click P "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L207" "打开源码"
    Q["SaveManager.DeleteBuildingBlueprint()"]
    click Q "../Assets/Scripts/Features/Building/SaveManager.cs#L191" "打开源码"
    R["MainUIPanels.ShowCreatePanel()"]
    click R "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L90" "打开源码"
    A -->|Ctrl+Alt+F| B
    B -->|进入| C
    O -->|明确新建| D
    O -->|经LoadBuildingBlueprint打开| C
    C -->|已有文件| E
    C -->|目录为空| R
    P -->|建筑删除| Q
    Q -->|删除当前项后重选或清空| C
    E --> F
    F -.->|prepareParent 回调| G
    G --> H
    I --> J
    I -->|写入成功| K
    K --> H
    K -->|刷新所有块| L
    B -->|再次按下退出| M
    M --> N
```

- [InputManager.Update](../Assets/Scripts/Features/Player/InputManager.cs#L40)
<!-- source: Assets/Scripts/Features/Player/InputManager.cs|Update -->
- [BuildManager.ToggleBuildingBlueprintBuildMode](../Assets/Scripts/Features/Building/BuildManager.cs#L287)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|ToggleBuildingBlueprintBuildMode -->
- [BuildManager.EnterBuildingBlueprintBuildMode](../Assets/Scripts/Features/Building/BuildManager.cs#L302)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|EnterBuildingBlueprintBuildMode -->
- [SaveManager.CreateNewBuildingBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L162)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|CreateNewBuildingBlueprint -->
- [BuildManager.LoadAllBlocks](../Assets/Scripts/Features/Building/BuildManager.cs#L701)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|LoadAllBlocks -->
- [BlueprintLoader.Load](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|Load -->
- [BuildManager.PrepareBuildingFoundation](../Assets/Scripts/Features/Building/BuildManager.cs#L392)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|PrepareBuildingFoundation -->
- [BuildingFoundation.Configure](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs#L17)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs|Configure -->
- [BuildManager.CompleteBuildingBlueprint](../Assets/Scripts/Features/Building/BuildManager.cs#L421)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|CompleteBuildingBlueprint -->
- [BuildingFootprint.Calculate](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs#L8)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs|Calculate -->
- [BuildManager.RefreshBuildingFootprint](../Assets/Scripts/Features/Building/BuildManager.cs#L453)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|RefreshBuildingFootprint -->
- [Block.CheckConnection](../Assets/Scripts/Features/Simulation/Block.cs#L361)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|CheckConnection -->
- [BuildManager.ExitDeveloperBlueprintBuildMode](../Assets/Scripts/Features/Building/BuildManager.cs#L267)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|ExitDeveloperBlueprintBuildMode -->
- [SaveManager.LoadSave](../Assets/Scripts/Features/Building/SaveManager.cs#L356)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadSave -->
- [MainUIPanels.OnConfirmCreate](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L167)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|OnConfirmCreate -->
- [MainUIPanels.OnConfirmDelete](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L207)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|OnConfirmDelete -->
- [SaveManager.DeleteBuildingBlueprint](../Assets/Scripts/Features/Building/SaveManager.cs#L191)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|DeleteBuildingBlueprint -->
- [MainUIPanels.ShowCreatePanel](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L90)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|ShowCreatePanel -->

## 地面自由建造、自动占地与舰队调整

源码推导：普通建筑与基地共享虚拟y=0地面放置，保留模块连接吸附。编辑保存只标记占地待更新；加载、完成或退出编辑时计算旋转包围盒和边距，不搬动模块。世界地形支撑、流送与基地出生点读取相同范围。整备时管理已部署舰船，残值复用维修报价；换购先检查余额、尺寸和外部障碍，生成成功才替换旧舰并原子结算，取消清理原舰和碎片，禁止重复退款与战斗/返航操作。

```mermaid
flowchart TD
    A["BuildPreview.Poll()"]
    click A "../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L49" "打开源码"
    B["BuildManager.CreateBlock()"]
    click B "../Assets/Scripts/Features/Building/BuildManager.cs#L597" "打开源码"
    C["BuildManager.CompleteBuildingBlueprint()"]
    click C "../Assets/Scripts/Features/Building/BuildManager.cs#L421" "打开源码"
    D["BuildingFootprint.Calculate()"]
    click D "../Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs#L8" "打开源码"
    E["BuildManager.RefreshBuildingFootprint()"]
    click E "../Assets/Scripts/Features/Building/BuildManager.cs#L453" "打开源码"
    F["BuildingFoundation.Configure()"]
    click F "../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs#L17" "打开源码"
    G["BuildingRuntime.HasFullFoundationSupport()"]
    click G "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L307" "打开源码"
    H["WorldBuildingPlacement.LocalAnchor()"]
    click H "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L106" "打开源码"
    I["WorldBuildingStreaming.HasLoadedGround()"]
    click I "../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L151" "打开源码"
    J["SiegeDeploymentView.Confirm()"]
    click J "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L360" "打开源码"
    K["SiegeFleet.ReplaceAt()"]
    click K "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L283" "打开源码"
    L["SiegeFleet.CancelDeployment()"]
    click L "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L263" "打开源码"
    M["SiegeFleet.CancellationCredit()"]
    click M "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L257" "打开源码"
    N["SiegeMatch.TryExchange()"]
    click N "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L236" "打开源码"
    O["SiegeFleet.Scrap()"]
    click O "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L113" "打开源码"
    A -->|鼠标确认| B
    B -.->|完成或退出编辑| C
    C --> D
    C -->|磁盘写入成功后刷新| E
    E --> F
    G --> H
    I -.->|读取同一占地| H
    J --> K
    K --> M
    L --> M
    K -->|成功生成再结算| N
    L --> N
    L --> O
    K --> O
```

- [BuildPreview.Poll](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L49)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildPreview.cs|Poll -->
- [BuildManager.CreateBlock](../Assets/Scripts/Features/Building/BuildManager.cs#L597)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|CreateBlock -->
- [BuildManager.CompleteBuildingBlueprint](../Assets/Scripts/Features/Building/BuildManager.cs#L421)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|CompleteBuildingBlueprint -->
- [BuildingFootprint.Calculate](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs#L8)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingFootprint.cs|Calculate -->
- [BuildManager.RefreshBuildingFootprint](../Assets/Scripts/Features/Building/BuildManager.cs#L453)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|RefreshBuildingFootprint -->
- [BuildingFoundation.Configure](../Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs#L17)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingFoundation.cs|Configure -->
- [BuildingRuntime.HasFullFoundationSupport](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L307)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|HasFullFoundationSupport -->
- [WorldBuildingPlacement.LocalAnchor](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L106)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs|LocalAnchor -->
- [WorldBuildingStreaming.HasLoadedGround](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L151)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs|HasLoadedGround -->
- [SiegeDeploymentView.Confirm](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L360)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|Confirm -->
- [SiegeFleet.ReplaceAt](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L283)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|ReplaceAt -->
- [SiegeFleet.CancelDeployment](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L263)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|CancelDeployment -->
- [SiegeFleet.CancellationCredit](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L257)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|CancellationCredit -->
- [SiegeMatch.TryExchange](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L236)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|TryExchange -->
- [SiegeFleet.Scrap](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L113)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|Scrap -->

## 蓝图恢复、统计和取消

Load 先验证文件，再以单位变换创建建造父对象，使Inspector局部坐标与BlockData世界坐标一致；逐块保留原世界姿态，协程按请求版本检查所有权。统计事件指向显示。取消使旧版本失效；缺资源移出缓存和JSON。

```mermaid
flowchart TD
    A["BuildManager.LoadAllBlocks()"]
    click A "../Assets/Scripts/Features/Building/BuildManager.cs#L701" "打开源码"
    B["BlueprintLoader.Load()"]
    click B "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43" "打开源码"
    C["BlueprintLoader.TryRead()"]
    click C "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L189" "打开源码"
    D["BlueprintLoader.ReplaceParent()"]
    click D "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L228" "打开源码"
    E["BlueprintLoadCamera.Begin()"]
    click E "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L15" "打开源码"
    F["BlueprintLoader.LoadRoutine()"]
    click F "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L107" "打开源码"
    G["BlueprintLoader.OwnsParent()"]
    click G "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L249" "打开源码"
    H["BlueprintBlockFactory.TryRestore()"]
    click H "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7" "打开源码"
    I["BlueprintStatistics.Accumulate()"]
    click I "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L26" "打开源码"
    J["BlueprintUIPanel.DisplayStatistics()"]
    click J "../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L44" "打开源码"
    K["BlueprintLoader.RemoveMissingData()"]
    click K "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L254" "打开源码"
    L["BlueprintLoader.RebuildConnections()"]
    click L "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265" "打开源码"
    P["BatchConnectionBuilder.RebuildBatchRoutine()"]
    click P "../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L18" "打开源码"
    M["BlueprintLoader.FinishSession()"]
    click M "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L303" "打开源码"
    N["BlueprintLoader.Cancel()"]
    click N "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L94" "打开源码"
    O["BlueprintLoadCamera.Stop()"]
    click O "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L26" "打开源码"
    A --> B
    B --> C
    B -->|文件有效| D
    B --> E
    B -.->|StartCoroutine| F
    F --> G
    F --> H
    F -->|恢复成功| I
    F -.->|StatisticsChanged| J
    F -->|完成| K
    F --> L
    L --> P
    P --> M
    F -->|finally| M
    N -->|版本递增 / 停协程| M
    M --> O
```

- [BuildManager.LoadAllBlocks](../Assets/Scripts/Features/Building/BuildManager.cs#L701)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|LoadAllBlocks -->
- [BlueprintLoader.Load](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|Load -->
- [BlueprintLoader.TryRead](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L189)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|TryRead -->
- [BlueprintLoader.ReplaceParent](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L228)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|ReplaceParent -->
- [BlueprintLoadCamera.Begin](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L15)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs|Begin -->
- [BlueprintLoader.LoadRoutine](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L107)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|LoadRoutine -->
- [BlueprintLoader.OwnsParent](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L249)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|OwnsParent -->
- [BlueprintBlockFactory.TryRestore](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs|TryRestore -->
- [BlueprintStatistics.Accumulate](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs#L26)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintStatistics.cs|Accumulate -->
- [BlueprintUIPanel.DisplayStatistics](../Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs#L44)
<!-- source: Assets/Scripts/Features/Building/Interface/BlueprintUIPanel.cs|DisplayStatistics -->
- [BlueprintLoader.RemoveMissingData](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L254)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|RemoveMissingData -->
- [BlueprintLoader.RebuildConnections](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|RebuildConnections -->
- [BatchConnectionBuilder.RebuildBatchRoutine](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L18)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs|RebuildBatchRoutine -->
- [BlueprintLoader.FinishSession](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L303)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|FinishSession -->
- [BlueprintLoader.Cancel](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L94)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|Cancel -->
- [BlueprintLoadCamera.Stop](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs#L26)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoadCamera.cs|Stop -->

## 蓝图资源缓存与连接批处理

源码推导：玩家/建筑恢复和敌机校验/创建共享预制体缓存；SubsystemRegistration 清理缓存。连接批处理保留逐块邻接及连接检测，同步版本只在入口同步物理，分帧版本每次恢复重新同步。2ms仅为单块完成后的让出阈值，不保证帧耗时上限。

```mermaid
flowchart TD
    A["BlueprintBlockFactory.TryRestore()"]
    click A "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7" "打开源码"
    B["EnemyBlueprintFactory.IsValidBlueprint()"]
    click B "../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L7" "打开源码"
    C["EnemyBlueprintFactory.Spawn()"]
    click C "../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12" "打开源码"
    D["BlockPrefabCache.GetPrefab()"]
    click D "../Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs#L13" "打开源码"
    E["BlockPrefabCache.Clear()"]
    click E "../Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs#L37" "打开源码"
    F["BatchConnectionBuilder.RebuildBatchImmediate()"]
    click F "../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L40" "打开源码"
    G["BlueprintLoader.RebuildConnections()"]
    click G "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265" "打开源码"
    H["BatchConnectionBuilder.RebuildBatchRoutine()"]
    click H "../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L18" "打开源码"
    I["BatchConnectionBuilder.RefreshBlock()"]
    click I "../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L54" "打开源码"
    J["Block.CollectNeighbors()"]
    click J "../Assets/Scripts/Features/Simulation/Block.cs#L379" "打开源码"
    K["Block.CheckConnectionAfterPhysicsSync()"]
    click K "../Assets/Scripts/Features/Simulation/Block.cs#L366" "打开源码"
    L["BlockConnections.CheckConnection()"]
    click L "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L53" "打开源码"
    A --> D
    B --> D
    C --> B
    C --> D
    C --> F
    E -.->|启动时清理资源引用| D
    G -.->|所有权检查后MoveNext| H
    F --> I
    H -.->|每片段统一同步物理| I
    I --> J
    I --> K
    K -->|跳过重复同步，仍执行探测| L
```

- [BlueprintBlockFactory.TryRestore](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs|TryRestore -->
- [EnemyBlueprintFactory.IsValidBlueprint](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L7)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs|IsValidBlueprint -->
- [EnemyBlueprintFactory.Spawn](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs|Spawn -->
- [BlockPrefabCache.GetPrefab](../Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs#L13)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs|GetPrefab -->
- [BlockPrefabCache.Clear](../Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs#L37)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlockPrefabCache.cs|Clear -->
- [BatchConnectionBuilder.RebuildBatchImmediate](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L40)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs|RebuildBatchImmediate -->
- [BlueprintLoader.RebuildConnections](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|RebuildConnections -->
- [BatchConnectionBuilder.RebuildBatchRoutine](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L18)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs|RebuildBatchRoutine -->
- [BatchConnectionBuilder.RefreshBlock](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L54)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs|RefreshBlock -->
- [Block.CollectNeighbors](../Assets/Scripts/Features/Simulation/Block.cs#L379)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|CollectNeighbors -->
- [Block.CheckConnectionAfterPhysicsSync](../Assets/Scripts/Features/Simulation/Block.cs#L366)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|CheckConnectionAfterPhysicsSync -->
- [BlockConnections.CheckConnection](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L53)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|CheckConnection -->

## 放置、选中、操作和持久化

源码推导：预览保持资源默认整格朝向，不继承目标旋转；切换资源时重建 Ghost，按真实连接点确定中心并清理尾差。以驾驶舱整数边界为准：奇数尺寸轴中心半整数、偶数轴中心整数。坐标轴独立于可销毁的选中方块，每帧刷新位置/朝向/可见性；视角解锁时隐藏、重锁时恢复。相机轴正交选取，拖拽捕获世界轴；90°旋转锁定整数锚点并换轴半格偏移；阻挡拒绝保护姿态与位置，Undo/Redo记录两者。

```mermaid
flowchart TD
    A["BuildManager.Update()"]
    click A "../Assets/Scripts/Features/Building/BuildManager.cs#L139" "打开源码"
    B["BuildManager.HandleBuildingPreview()"]
    click B "../Assets/Scripts/Features/Building/BuildManager.cs#L563" "打开源码"
    C["BuildPreview.Poll()"]
    click C "../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L49" "打开源码"
    AB["BuildSelection.RefreshBuildGizmos()"]
    click AB "../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L152" "打开源码"
    D["BuildPlacementGeometry.TryCalculateConnectorAlignedPosition()"]
    click D "../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L64" "打开源码"
    E["BuildPlacementGeometry.IsBlocked()"]
    click E "../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L171" "打开源码"
    F["BuildManager.CreateBlock()"]
    click F "../Assets/Scripts/Features/Building/BuildManager.cs#L597" "打开源码"
    G["BuildManager.SaveBlock()"]
    click G "../Assets/Scripts/Features/Building/BuildManager.cs#L675" "打开源码"
    H["BuildBlueprintEditor.Save()"]
    click H "../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L17" "打开源码"
    I["BuildBlueprintEditor.RefreshConnectionsAround()"]
    click I "../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L79" "打开源码"
    J["BuildSelection.Poll()"]
    click J "../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L42" "打开源码"
    K["BuildSelection.SelectBlock()"]
    click K "../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L80" "打开源码"
    L["BuildManipulation.Poll()"]
    click L "../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L60" "打开源码"
    M["ActionManager.Push()"]
    click M "../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L26" "打开源码"
    N["ActionManager.Undo()"]
    click N "../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L37" "打开源码"
    O["ActionManager.Redo()"]
    click O "../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L50" "打开源码"
    P["Block.CheckConnection()"]
    click P "../Assets/Scripts/Features/Simulation/Block.cs#L361" "打开源码"
    Q["BuildPalette.Awake()"]
    click Q "../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L27" "打开源码"
    R["BuildPaletteItem.ApplyButtonStyle()"]
    click R "../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L45" "打开源码"
    S["BuildPaletteItem.BindPalette()"]
    click S "../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L35" "打开源码"
    T["BuildManager.SetCurrentBlockResource()"]
    click T "../Assets/Scripts/Features/Building/BuildManager.cs#L479" "打开源码"
    U["BuildManipulation.HandleRotation()"]
    click U "../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L241" "打开源码"
    V["BuildManipulation.HandleRotateAxisDrag()"]
    click V "../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L370" "打开源码"
    W["BuildPlacementGeometry.CalculateRotationPosition()"]
    click W "../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L19" "打开源码"
    X["BuildPlacementGeometry.SnapRotationToGrid()"]
    click X "../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L10" "打开源码"
    Y["BuildPlacementGeometry.RemoveGridNoise()"]
    click Y "../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L109" "打开源码"
    Z["BuildManipulation.AlignAxisToNearestWorldDir()"]
    click Z "../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L126" "打开源码"
    AA["BuildManipulation.TryBeginDrag()"]
    click AA "../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L81" "打开源码"
    A --> B
    B --> C
    C --> D
    D --> Y
    C --> E
    C -.->|确认后调用注入动作| F
    F --> G
    G --> H
    H --> I
    I --> P
    F --> M
    A --> J
    J --> K
    J --> AA
    A --> L
    A --> AB
    K --> AB
    L --> Z
    AA -.->|捕获起始世界轴及姿态供后续帧使用| V
    L --> U
    L --> V
    U --> X
    V --> X
    U --> W
    V --> W
    W --> X
    U --> E
    V --> E
    U -.->|有效旋转保存| G
    V -.->|释放拖拽后保存| G
    U --> M
    V --> M
    A --> N
    A --> O
    Q --> R
    Q --> S
    S -.->|按钮点击| T
```

- [BuildManager.Update](../Assets/Scripts/Features/Building/BuildManager.cs#L139)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|Update -->
- [BuildManager.HandleBuildingPreview](../Assets/Scripts/Features/Building/BuildManager.cs#L563)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|HandleBuildingPreview -->
- [BuildPreview.Poll](../Assets/Scripts/Features/Building/Interaction/BuildPreview.cs#L49)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildPreview.cs|Poll -->
- [BuildSelection.RefreshBuildGizmos](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L152)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildSelection.cs|RefreshBuildGizmos -->
- [BuildPlacementGeometry.TryCalculateConnectorAlignedPosition](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L64)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs|TryCalculateConnectorAlignedPosition -->
- [BuildPlacementGeometry.IsBlocked](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L171)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs|IsBlocked -->
- [BuildManager.CreateBlock](../Assets/Scripts/Features/Building/BuildManager.cs#L597)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|CreateBlock -->
- [BuildManager.SaveBlock](../Assets/Scripts/Features/Building/BuildManager.cs#L675)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|SaveBlock -->
- [BuildBlueprintEditor.Save](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L17)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs|Save -->
- [BuildBlueprintEditor.RefreshConnectionsAround](../Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs#L79)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildBlueprintEditor.cs|RefreshConnectionsAround -->
- [BuildSelection.Poll](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L42)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildSelection.cs|Poll -->
- [BuildSelection.SelectBlock](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L80)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildSelection.cs|SelectBlock -->
- [BuildManipulation.Poll](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L60)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs|Poll -->
- [ActionManager.Push](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L26)
<!-- source: Assets/Scripts/Features/Building/Actions/ActionManager.cs|Push -->
- [ActionManager.Undo](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L37)
<!-- source: Assets/Scripts/Features/Building/Actions/ActionManager.cs|Undo -->
- [ActionManager.Redo](../Assets/Scripts/Features/Building/Actions/ActionManager.cs#L50)
<!-- source: Assets/Scripts/Features/Building/Actions/ActionManager.cs|Redo -->
- [Block.CheckConnection](../Assets/Scripts/Features/Simulation/Block.cs#L361)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|CheckConnection -->
- [BuildPalette.Awake](../Assets/Scripts/Features/Building/Interface/BuildPalette.cs#L27)
<!-- source: Assets/Scripts/Features/Building/Interface/BuildPalette.cs|Awake -->
- [BuildPaletteItem.ApplyButtonStyle](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L45)
<!-- source: Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs|ApplyButtonStyle -->
- [BuildPaletteItem.BindPalette](../Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs#L35)
<!-- source: Assets/Scripts/Features/Building/Interface/BuildPaletteItem.cs|BindPalette -->
- [BuildManager.SetCurrentBlockResource](../Assets/Scripts/Features/Building/BuildManager.cs#L479)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|SetCurrentBlockResource -->
- [BuildManipulation.HandleRotation](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L241)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs|HandleRotation -->
- [BuildManipulation.HandleRotateAxisDrag](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L370)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs|HandleRotateAxisDrag -->
- [BuildPlacementGeometry.CalculateRotationPosition](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L19)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs|CalculateRotationPosition -->
- [BuildPlacementGeometry.SnapRotationToGrid](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L10)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs|SnapRotationToGrid -->
- [BuildPlacementGeometry.RemoveGridNoise](../Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs#L109)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildPlacementGeometry.cs|RemoveGridNoise -->
- [BuildManipulation.AlignAxisToNearestWorldDir](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L126)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs|AlignAxisToNearestWorldDir -->
- [BuildManipulation.TryBeginDrag](../Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs#L81)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildManipulation.cs|TryBeginDrag -->

## 供电网络与可视范围

消费者先申报工作意图，空闲不分配；每帧仅一个中继刷新功率，拓扑仅在设备或发电机位置/连接参数变化时重建。CommitPowerStatus确认SupplyEfficiency供推进器字体读取，瞬时清零不提交缺电；无中继时LateUpdate补充确认。

```mermaid
flowchart TD
    A["PowerTransmissionDevice.Update()"]
    click A "../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L61" "打开源码"
    B["PowerNetwork.Rebuild()"]
    click B "../Assets/Scripts/Features/Power/PowerNetwork.cs#L50" "打开源码"
    C["PowerNetwork.ResetConnections()"]
    click C "../Assets/Scripts/Features/Power/PowerNetwork.cs#L134" "打开源码"
    D["PowerNetwork.BuildConnections()"]
    click D "../Assets/Scripts/Features/Power/PowerNetwork.cs#L206" "打开源码"
    E["PowerNetwork.DistributeNetworkPower()"]
    click E "../Assets/Scripts/Features/Power/PowerNetwork.cs#L308" "打开源码"
    F["PowerNetwork.CollectConnectedComponent()"]
    click F "../Assets/Scripts/Features/Power/PowerNetwork.cs#L331" "打开源码"
    G["PowerNetwork.SupplyConnectedComponent()"]
    click G "../Assets/Scripts/Features/Power/PowerNetwork.cs#L382" "打开源码"
    H["PowerNetwork.CommitPowerStatuses()"]
    click H "../Assets/Scripts/Features/Power/PowerNetwork.cs#L419" "打开源码"
    I["Power.CommitPowerStatus()"]
    click I "../Assets/Scripts/Features/Power/Power.cs#L111" "打开源码"
    J["PowerConnectionVisuals.Refresh()"]
    click J "../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L19" "打开源码"
    K["PowerTransmissionDevice.OnDisable()"]
    click K "../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L74" "打开源码"
    L["PowerNetwork.Clear()"]
    click L "../Assets/Scripts/Features/Power/PowerNetwork.cs#L71" "打开源码"
    M["DebugManager.RefreshPowerRangeMesh()"]
    click M "../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L127" "打开源码"
    N["PowerRangeUnionView.Refresh()"]
    click N "../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L36" "打开源码"
    O["DebugManager.TogglePowerRange()"]
    click O "../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L81" "打开源码"
    P["PowerRangeUnionView.Hide()"]
    click P "../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L66" "打开源码"
    Q["DebugManager.OnDestroy()"]
    click Q "../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L57" "打开源码"
    R["PowerRangeUnionView.Dispose()"]
    click R "../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L79" "打开源码"
    S["Power.SetWorkRequested()"]
    click S "../Assets/Scripts/Features/Power/Power.cs#L74" "打开源码"
    T["Power.ReceivePower()"]
    click T "../Assets/Scripts/Features/Power/Power.cs#L106" "打开源码"
    U["Power.LateUpdate()"]
    click U "../Assets/Scripts/Features/Power/Power.cs#L64" "打开源码"
    V["PowerNetwork.CaptureTopology()"]
    click V "../Assets/Scripts/Features/Power/PowerNetwork.cs#L149" "打开源码"
    W["PowerNetwork.CacheConnectedComponents()"]
    click W "../Assets/Scripts/Features/Power/PowerNetwork.cs#L261" "打开源码"
    X["PowerNetwork.PrepareLoads()"]
    click X "../Assets/Scripts/Features/Power/PowerNetwork.cs#L291" "打开源码"
    Y["PowerNetwork.CollectDeviceLoads()"]
    click Y "../Assets/Scripts/Features/Power/PowerNetwork.cs#L366" "打开源码"
    Z["PowerPositionIndex.Query()"]
    click Z "../Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs#L37" "打开源码"
    A -->|首个本帧中继| B
    B --> V
    V -.->|身份、位置或连接范围变化| C
    B -->|拓扑变化| D
    B -->|拓扑变化| W
    D --> Z
    W --> F
    B -->|每帧| X
    B --> E
    E --> Y
    Y --> Z
    E --> G
    S -.->|下轮网络读取需求| G
    G --> T
    B --> H
    H --> I
    U -->|本帧尚未提交| I
    A --> J
    K -->|最后中继| L
    A -->|完成网络后| M
    M --> N
    O -->|关闭范围显示| P
    Q --> R
```

- [PowerTransmissionDevice.Update](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L61)
<!-- source: Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs|Update -->
- [PowerNetwork.Rebuild](../Assets/Scripts/Features/Power/PowerNetwork.cs#L50)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|Rebuild -->
- [PowerNetwork.ResetConnections](../Assets/Scripts/Features/Power/PowerNetwork.cs#L134)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|ResetConnections -->
- [PowerNetwork.BuildConnections](../Assets/Scripts/Features/Power/PowerNetwork.cs#L206)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|BuildConnections -->
- [PowerNetwork.DistributeNetworkPower](../Assets/Scripts/Features/Power/PowerNetwork.cs#L308)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|DistributeNetworkPower -->
- [PowerNetwork.CollectConnectedComponent](../Assets/Scripts/Features/Power/PowerNetwork.cs#L331)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|CollectConnectedComponent -->
- [PowerNetwork.SupplyConnectedComponent](../Assets/Scripts/Features/Power/PowerNetwork.cs#L382)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|SupplyConnectedComponent -->
- [PowerNetwork.CommitPowerStatuses](../Assets/Scripts/Features/Power/PowerNetwork.cs#L419)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|CommitPowerStatuses -->
- [Power.CommitPowerStatus](../Assets/Scripts/Features/Power/Power.cs#L111)
<!-- source: Assets/Scripts/Features/Power/Power.cs|CommitPowerStatus -->
- [PowerConnectionVisuals.Refresh](../Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs#L19)
<!-- source: Assets/Scripts/Features/Power/Presentation/PowerConnectionVisuals.cs|Refresh -->
- [PowerTransmissionDevice.OnDisable](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L74)
<!-- source: Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs|OnDisable -->
- [PowerNetwork.Clear](../Assets/Scripts/Features/Power/PowerNetwork.cs#L71)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|Clear -->
- [DebugManager.RefreshPowerRangeMesh](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L127)
<!-- source: Assets/Scripts/Features/Diagnostics/DebugManager.cs|RefreshPowerRangeMesh -->
- [PowerRangeUnionView.Refresh](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L36)
<!-- source: Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs|Refresh -->
- [DebugManager.TogglePowerRange](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L81)
<!-- source: Assets/Scripts/Features/Diagnostics/DebugManager.cs|TogglePowerRange -->
- [PowerRangeUnionView.Hide](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L66)
<!-- source: Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs|Hide -->
- [DebugManager.OnDestroy](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L57)
<!-- source: Assets/Scripts/Features/Diagnostics/DebugManager.cs|OnDestroy -->
- [PowerRangeUnionView.Dispose](../Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs#L79)
<!-- source: Assets/Scripts/Features/Power/Presentation/PowerRangeUnionView.cs|Dispose -->
- [Power.SetWorkRequested](../Assets/Scripts/Features/Power/Power.cs#L74)
<!-- source: Assets/Scripts/Features/Power/Power.cs|SetWorkRequested -->
- [Power.ReceivePower](../Assets/Scripts/Features/Power/Power.cs#L106)
<!-- source: Assets/Scripts/Features/Power/Power.cs|ReceivePower -->
- [Power.LateUpdate](../Assets/Scripts/Features/Power/Power.cs#L64)
<!-- source: Assets/Scripts/Features/Power/Power.cs|LateUpdate -->
- [PowerNetwork.CaptureTopology](../Assets/Scripts/Features/Power/PowerNetwork.cs#L149)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|CaptureTopology -->
- [PowerNetwork.CacheConnectedComponents](../Assets/Scripts/Features/Power/PowerNetwork.cs#L261)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|CacheConnectedComponents -->
- [PowerNetwork.PrepareLoads](../Assets/Scripts/Features/Power/PowerNetwork.cs#L291)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|PrepareLoads -->
- [PowerNetwork.CollectDeviceLoads](../Assets/Scripts/Features/Power/PowerNetwork.cs#L366)
<!-- source: Assets/Scripts/Features/Power/PowerNetwork.cs|CollectDeviceLoads -->
- [PowerPositionIndex.Query](../Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs#L37)
<!-- source: Assets/Scripts/Features/Power/Network/PowerPositionIndex.cs|Query -->

## 物理分组、连接与推进

连接图是组重建输入；先捕获不可变源速度/质心，优先保留含驾驶舱分量的刚体，无驾驶舱时保留最大分量。只迁移分离模块，所有迁移完成后恢复点速度并局部刷新；不重扫无关单位。重新探测时仍有效的双侧连接保留原视觉，只释放改变的连接，避免重建时成批Instantiate/Destroy。几何连接要求两侧规范碰撞体都参与Block层查询，断链不能单向向外连接；修缮/底座重建显式恢复原层。FixedUpdate由引擎调度；悬浮控制器提交目标，推进器每物理帧限速一次。关机撤销功率请求但残余推力按maxThrustChangeRate衰减；退出游玩或组件禁用清零。

```mermaid
flowchart TD
    A["PlayManager.RefreshGroup()"]
    click A "../Assets/Scripts/Features/Simulation/PlayManager.cs#L193" "打开源码"
    B["PlayManager.AssignBlocksToParentGroups()"]
    click B "../Assets/Scripts/Features/Simulation/PlayManager.cs#L330" "打开源码"
    C["RuntimeGroupBuilder.Rebuild()"]
    click C "../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L16" "打开源码"
    D["BlockGroupManager.GroupRuntimeBlocks()"]
    click D "../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L15" "打开源码"
    E["BlockConnections.CollectRuntimeNeighbors()"]
    click E "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L420" "打开源码"
    F["RigidbodyMotion.Restore()"]
    click F "../Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs#L17" "打开源码"
    G["ControlUnit.RefreshChildren()"]
    click G "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185" "打开源码"
    H["PlayManager.FixedUpdate()"]
    click H "../Assets/Scripts/Features/Simulation/PlayManager.cs#L77" "打开源码"
    I["RuntimeGroupCleanup.EnforceDistance()"]
    click I "../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupCleanup.cs#L51" "打开源码"
    J["HoverFlightController.FixedUpdate()"]
    click J "../Assets/Scripts/Features/Flight/HoverFlightController.cs#L103" "打开源码"
    K["HoverFlightController.DistributeThrust()"]
    click K "../Assets/Scripts/Features/Flight/HoverFlightController.cs#L348" "打开源码"
    L["MainThruster.FixedUpdate()"]
    click L "../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L10" "打开源码"
    M["Thruster.ApplyThrustChangeRateLimit()"]
    click M "../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L167" "打开源码"
    N["HoverControlSolver.StepHeight()"]
    click N "../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L29" "打开源码"
    O["HoverControlSolver.CalculateTilt()"]
    click O "../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L55" "打开源码"
    P["HoverFlightController.StopControlOutput()"]
    click P "../Assets/Scripts/Features/Flight/HoverFlightController.cs#L170" "打开源码"
    Q["HoverControlSolver.Reset()"]
    click Q "../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L23" "打开源码"
    R["HoverFlightController.ClearHoverThrust()"]
    click R "../Assets/Scripts/Features/Flight/HoverFlightController.cs#L177" "打开源码"
    S["HoverFlightController.OnDisable()"]
    click S "../Assets/Scripts/Features/Flight/HoverFlightController.cs#L91" "打开源码"
    T["HoverThruster.SetControlledThrust()"]
    click T "../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L11" "打开源码"
    U["HoverThruster.FixedUpdate()"]
    click U "../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L16" "打开源码"
    V["UniversalThruster.FixedUpdate()"]
    click V "../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L10" "打开源码"
    W["BlockConnections.DisConnectAllConnectors()"]
    click W "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L329" "打开源码"
    X["Block.RestoreConnectionParticipation()"]
    click X "../Assets/Scripts/Features/Simulation/Block.cs#L390" "打开源码"
    Y["BlockConnections.RestoreParticipation()"]
    click Y "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L375" "打开源码"
    Z["RuntimeGroupBuilder.CaptureSourceBodies()"]
    click Z "../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L131" "打开源码"
    AA["RuntimeGroupBuilder.SelectRetainedGroups()"]
    click AA "../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L191" "打开源码"
    AB["PlayManager.RegisterControlUnitWithoutCleanup()"]
    click AB "../Assets/Scripts/Features/Simulation/PlayManager.cs#L357" "打开源码"
    AC["BlockConnectionSnapshot.Capture()"]
    click AC "../Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs#L71" "打开源码"
    AD["BlockConnections.ReleaseRuntimePair()"]
    click AD "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L394" "打开源码"
    AE["BatchConnectionBuilder.RebuildBatchImmediate()"]
    click AE "../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L40" "打开源码"
    AF["BuildingRuntime.RefreshStructure()"]
    click AF "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L55" "打开源码"
    AG["BlockConnectionGraph.PrepareBlocks()"]
    click AG "../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L109" "打开源码"
    AH["Thruster.TickInputThrust()"]
    click AH "../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L89" "打开源码"
    AI["Thruster.ApplyDirectionalThrust()"]
    click AI "../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L119" "打开源码"
    AJ["GroupPhysicsSettings.ApplyTo()"]
    click AJ "../Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs#L12" "打开源码"
    AK["BlockConnections.CheckConnection()"]
    click AK "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L53" "打开源码"
    AL["BlockConnections.ConnectTo()"]
    click AL "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L232" "打开源码"
    AM["BlockConnections.ClearConnector()"]
    click AM "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L259" "打开源码"
    A -->|建筑按底座支撑分裂| AF
    A -->|舰船| B
    B --> C
    C --> D
    C --> Z
    C --> AA
    C -->|新分量即时登记，外层统一清理| AB
    D -->|有效缓存BFS，经 Block 入口| E
    D --> AG
    AG -->|任一快照失效才物理重建| AE
    AE --> AK
    AK -->|几何与双侧身份未变时复用视觉| AL
    AK -->|连接改变或失效| AM
    AE -.->|每块连接检查后保存| AC
    W -->|直接清理双侧引用| AD
    W -.->|两侧排除断开块| E
    X --> Y
    C --> F
    C --> G
    H --> I
    J -->|有效游玩/供电/归属| N
    J --> O
    J --> K
    K --> T
    R --> T
    U --> M
    V --> AH
    L --> AH
    AH --> M
    AH --> AI
    C --> AJ
    J -->|停止或失效| P
    S --> P
    P --> Q
    P --> R
```

- [PlayManager.RefreshGroup](../Assets/Scripts/Features/Simulation/PlayManager.cs#L193)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|RefreshGroup -->
- [PlayManager.AssignBlocksToParentGroups](../Assets/Scripts/Features/Simulation/PlayManager.cs#L330)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|AssignBlocksToParentGroups -->
- [RuntimeGroupBuilder.Rebuild](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L16)
<!-- source: Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs|Rebuild -->
- [BlockGroupManager.GroupRuntimeBlocks](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L15)
<!-- source: Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs|GroupRuntimeBlocks -->
- [BlockConnections.CollectRuntimeNeighbors](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L420)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|CollectRuntimeNeighbors -->
- [RigidbodyMotion.Restore](../Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs#L17)
<!-- source: Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs|Restore -->
- [ControlUnit.RefreshChildren](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|RefreshChildren -->
- [PlayManager.FixedUpdate](../Assets/Scripts/Features/Simulation/PlayManager.cs#L77)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|FixedUpdate -->
- [RuntimeGroupCleanup.EnforceDistance](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupCleanup.cs#L51)
<!-- source: Assets/Scripts/Features/Simulation/Groups/RuntimeGroupCleanup.cs|EnforceDistance -->
- [HoverFlightController.FixedUpdate](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L103)
<!-- source: Assets/Scripts/Features/Flight/HoverFlightController.cs|FixedUpdate -->
- [HoverFlightController.DistributeThrust](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L348)
<!-- source: Assets/Scripts/Features/Flight/HoverFlightController.cs|DistributeThrust -->
- [MainThruster.FixedUpdate](../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L10)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs|FixedUpdate -->
- [Thruster.ApplyThrustChangeRateLimit](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L167)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/Thruster.cs|ApplyThrustChangeRateLimit -->
- [HoverControlSolver.StepHeight](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L29)
<!-- source: Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs|StepHeight -->
- [HoverControlSolver.CalculateTilt](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L55)
<!-- source: Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs|CalculateTilt -->
- [HoverFlightController.StopControlOutput](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L170)
<!-- source: Assets/Scripts/Features/Flight/HoverFlightController.cs|StopControlOutput -->
- [HoverControlSolver.Reset](../Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs#L23)
<!-- source: Assets/Scripts/Features/Flight/Hover/HoverControlSolver.cs|Reset -->
- [HoverFlightController.ClearHoverThrust](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L177)
<!-- source: Assets/Scripts/Features/Flight/HoverFlightController.cs|ClearHoverThrust -->
- [HoverFlightController.OnDisable](../Assets/Scripts/Features/Flight/HoverFlightController.cs#L91)
<!-- source: Assets/Scripts/Features/Flight/HoverFlightController.cs|OnDisable -->
- [HoverThruster.SetControlledThrust](../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L11)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs|SetControlledThrust -->
- [HoverThruster.FixedUpdate](../Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs#L16)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/HoverThruster.cs|FixedUpdate -->
- [UniversalThruster.FixedUpdate](../Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs#L10)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/UniversalThruster.cs|FixedUpdate -->
- [BlockConnections.DisConnectAllConnectors](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L329)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|DisConnectAllConnectors -->
- [Block.RestoreConnectionParticipation](../Assets/Scripts/Features/Simulation/Block.cs#L390)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|RestoreConnectionParticipation -->
- [BlockConnections.RestoreParticipation](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L375)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|RestoreParticipation -->
- [RuntimeGroupBuilder.CaptureSourceBodies](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L131)
<!-- source: Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs|CaptureSourceBodies -->
- [RuntimeGroupBuilder.SelectRetainedGroups](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L191)
<!-- source: Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs|SelectRetainedGroups -->
- [PlayManager.RegisterControlUnitWithoutCleanup](../Assets/Scripts/Features/Simulation/PlayManager.cs#L357)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|RegisterControlUnitWithoutCleanup -->
- [BlockConnectionSnapshot.Capture](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs#L71)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnectionSnapshot.cs|Capture -->
- [BlockConnections.ReleaseRuntimePair](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L394)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|ReleaseRuntimePair -->
- [BatchConnectionBuilder.RebuildBatchImmediate](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L40)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs|RebuildBatchImmediate -->
- [BuildingRuntime.RefreshStructure](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L55)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|RefreshStructure -->
- [BlockConnectionGraph.PrepareBlocks](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L109)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs|PrepareBlocks -->
- [Thruster.TickInputThrust](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L89)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/Thruster.cs|TickInputThrust -->
- [Thruster.ApplyDirectionalThrust](../Assets/Scripts/Features/Flight/Thrusters/Thruster.cs#L119)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/Thruster.cs|ApplyDirectionalThrust -->
- [GroupPhysicsSettings.ApplyTo](../Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs#L12)
<!-- source: Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs|ApplyTo -->
- [BlockConnections.CheckConnection](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L53)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|CheckConnection -->
- [BlockConnections.ConnectTo](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L232)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|ConnectTo -->
- [BlockConnections.ClearConnector](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L259)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|ClearConnector -->

## 战斗、损伤、爆炸与残骸清理

源码推导：Turret独立索敌后申报功率；激光即时命中且无冲量，实体炮塔先按相对速度求寿命/射程内的提前量，再每炮口生成一发并由物理扫掠命中。碰撞以法向冲量限制质量，按恢复系数扣除反弹能量，动态双方按质心能量份额分摊；CSV默认无半血限幅或碰撞冷却。启用canExplode的单位/建筑方块遭撞击致死同样爆炸，不按DestroyedByCollision排除；单位爆炸仍限制冲量并保护碎片碰撞，建筑沿用范围伤害和地形破坏批次。延迟清理保留原生命周期。

```mermaid
flowchart TD
    A["Turret.FixedUpdate()"]
    click A "../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71" "打开源码"
    B["TurretTargetSelector.FindNearestTarget()"]
    click B "../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L91" "打开源码"
    C["TurretHitscan.Fire()"]
    click C "../Assets/Scripts/Features/Combat/Turrets/TurretHitscan.cs#L10" "打开源码"
    D["Durability.ApplyDamage()"]
    click D "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103" "打开源码"
    E["ControlUnit.OnCollisionEnter()"]
    click E "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L336" "打开源码"
    F["CollisionDamageResolver.ApplyContactDamage()"]
    click F "../Assets/Scripts/Features/Combat/Damage/CollisionDamageResolver.cs#L10" "打开源码"
    G["Durability.ApplyCollisionEnergy()"]
    click G "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L114" "打开源码"
    H["DestroyManager.DestroyGameObject()"]
    click H "../Assets/Scripts/Features/Combat/DestroyManager.cs#L28" "打开源码"
    I["DestroyManager.ExplodeBlock()"]
    click I "../Assets/Scripts/Features/Combat/DestroyManager.cs#L135" "打开源码"
    J["ExplosionPropagation.DisconnectBlocksInExplosionRange()"]
    click J "../Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs#L9" "打开源码"
    K["PlayManager.RefreshGroup()"]
    click K "../Assets/Scripts/Features/Simulation/PlayManager.cs#L193" "打开源码"
    L["ExplosionPropagation.ApplyExplosionForce()"]
    click L "../Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs#L53" "打开源码"
    M["SalvageCleanupSession.ScheduleUnit()"]
    click M "../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L17" "打开源码"
    N["SalvageCleanupSession.CleanupUnitAfterDelay()"]
    click N "../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L108" "打开源码"
    O["CargoHold.ReleaseContents()"]
    click O "../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L206" "打开源码"
    P["LaserTurret.Fire()"]
    click P "../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L35" "打开源码"
    Q["ProjectileTurret.Fire()"]
    click Q "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L164" "打开源码"
    R["TurretProjectile.Launch()"]
    click R "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L65" "打开源码"
    S["TurretProjectile.FixedUpdate()"]
    click S "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L99" "打开源码"
    T["TurretHitResolver.Apply()"]
    click T "../Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs#L7" "打开源码"
    U["Power.SetWorkRequested()"]
    click U "../Assets/Scripts/Features/Power/Power.cs#L74" "打开源码"
    V["ProjectileTurret.LateUpdate()"]
    click V "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L77" "打开源码"
    W["ProjectileTurret.UpdateRecoil()"]
    click W "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L86" "打开源码"
    X["ProjectileTurret.ApplyRecoilPose()"]
    click X "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L104" "打开源码"
    Y["ProjectileTurret.OnDisable()"]
    click Y "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L125" "打开源码"
    Z["TurretProjectilePool.Rent()"]
    click Z "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L12" "打开源码"
    AA["TurretProjectile.Retire()"]
    click AA "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L226" "打开源码"
    AB["TurretProjectilePool.Return()"]
    click AB "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L58" "打开源码"
    AC["TurretProjectile.OnDisable()"]
    click AC "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L32" "打开源码"
    AD["TurretProjectilePool.Update()"]
    click AD "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L71" "打开源码"
    AE["TurretProjectilePool.OnDestroy()"]
    click AE "../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L87" "打开源码"
    AF["BuildingRuntime.DestroyBlock()"]
    click AF "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238" "打开源码"
    AG["ProjectileTurret.GetAimPosition()"]
    click AG "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L133" "打开源码"
    AH["ProjectileIntercept.TryGetTime()"]
    click AH "../Assets/Scripts/Features/Combat/Turrets/ProjectileIntercept.cs#L8" "打开源码"
    A --> B
    A -->|实体炮塔预测目标| AG
    AG --> AH
    A --> U
    A -->|供电、瞄准且冷却满足| P
    P --> C
    A -->|供电、瞄准且冷却满足| Q
    Q -->|每炮口一发| R
    Q --> Z
    S -->|命中或到期| AA
    AA -->|池实例| AB
    AB -.->|SetActive false| AC
    AD -.->|模拟结束销毁池根| AE
    Q -.->|发射触发后座| V
    V --> W
    W --> X
    Y -->|禁用恢复初始炮管位置| X
    R -.->|后续物理步| S
    S -->|首次实体命中| T
    C -->|零冲量| T
    T --> D
    E --> F
    F --> G
    D -->|载具耐久为0| H
    G -->|载具耐久为0| H
    D -->|建筑耐久为0| AF
    G -->|建筑耐久为0| AF
    H -->|可爆炸且非碰撞致死| I
    I --> J
    I --> K
    I --> L
    H --> O
    H -->|敌方驾驶舱| M
    M -.->|协程等待并复核| N
```

- [Turret.FixedUpdate](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Turret.cs|FixedUpdate -->
- [TurretTargetSelector.FindNearestTarget](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L91)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs|FindNearestTarget -->
- [TurretHitscan.Fire](../Assets/Scripts/Features/Combat/Turrets/TurretHitscan.cs#L10)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretHitscan.cs|Fire -->
- [Durability.ApplyDamage](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|ApplyDamage -->
- [ControlUnit.OnCollisionEnter](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L336)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|OnCollisionEnter -->
- [CollisionDamageResolver.ApplyContactDamage](../Assets/Scripts/Features/Combat/Damage/CollisionDamageResolver.cs#L10)
<!-- source: Assets/Scripts/Features/Combat/Damage/CollisionDamageResolver.cs|ApplyContactDamage -->
- [Durability.ApplyCollisionEnergy](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L114)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|ApplyCollisionEnergy -->
- [DestroyManager.DestroyGameObject](../Assets/Scripts/Features/Combat/DestroyManager.cs#L28)
<!-- source: Assets/Scripts/Features/Combat/DestroyManager.cs|DestroyGameObject -->
- [DestroyManager.ExplodeBlock](../Assets/Scripts/Features/Combat/DestroyManager.cs#L135)
<!-- source: Assets/Scripts/Features/Combat/DestroyManager.cs|ExplodeBlock -->
- [ExplosionPropagation.DisconnectBlocksInExplosionRange](../Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs#L9)
<!-- source: Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs|DisconnectBlocksInExplosionRange -->
- [PlayManager.RefreshGroup](../Assets/Scripts/Features/Simulation/PlayManager.cs#L193)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|RefreshGroup -->
- [ExplosionPropagation.ApplyExplosionForce](../Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs#L53)
<!-- source: Assets/Scripts/Features/Combat/Damage/ExplosionPropagation.cs|ApplyExplosionForce -->
- [SalvageCleanupSession.ScheduleUnit](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L17)
<!-- source: Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs|ScheduleUnit -->
- [SalvageCleanupSession.CleanupUnitAfterDelay](../Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs#L108)
<!-- source: Assets/Scripts/Features/Combat/Damage/SalvageCleanupSession.cs|CleanupUnitAfterDelay -->
- [CargoHold.ReleaseContents](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L206)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs|ReleaseContents -->
- [LaserTurret.Fire](../Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs#L35)
<!-- source: Assets/Scripts/Features/Combat/Turrets/LaserTurret.cs|Fire -->
- [ProjectileTurret.Fire](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L164)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|Fire -->
- [TurretProjectile.Launch](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L65)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs|Launch -->
- [TurretProjectile.FixedUpdate](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L99)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs|FixedUpdate -->
- [TurretHitResolver.Apply](../Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs#L7)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs|Apply -->
- [Power.SetWorkRequested](../Assets/Scripts/Features/Power/Power.cs#L74)
<!-- source: Assets/Scripts/Features/Power/Power.cs|SetWorkRequested -->
- [ProjectileTurret.LateUpdate](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L77)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|LateUpdate -->
- [ProjectileTurret.UpdateRecoil](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L86)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|UpdateRecoil -->
- [ProjectileTurret.ApplyRecoilPose](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L104)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|ApplyRecoilPose -->
- [ProjectileTurret.OnDisable](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L125)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|OnDisable -->
- [TurretProjectilePool.Rent](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L12)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs|Rent -->
- [TurretProjectile.Retire](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L226)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs|Retire -->
- [TurretProjectilePool.Return](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L58)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs|Return -->
- [TurretProjectile.OnDisable](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs#L32)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectile.cs|OnDisable -->
- [TurretProjectilePool.Update](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L71)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs|Update -->
- [TurretProjectilePool.OnDestroy](../Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs#L87)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Projectiles/TurretProjectilePool.cs|OnDestroy -->
- [BuildingRuntime.DestroyBlock](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|DestroyBlock -->
- [ProjectileTurret.GetAimPosition](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L133)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|GetAimPosition -->
- [ProjectileIntercept.TryGetTime](../Assets/Scripts/Features/Combat/Turrets/ProjectileIntercept.cs#L8)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileIntercept.cs|TryGetTime -->

## 返航持久化与失败提示

源码推导：UI通过带failureReason的SaveReturnCargo重载保存货仓；原bool重载仍供已有调用者使用。失败保留会话并持续显示可操作原因，CanvasGroup和图形不阻挡输入；成功才清除提示并退出/重载。状态栏由Editor工具保存为Main可编辑对象，不在运行时动态生成。

```mermaid
flowchart TD
    A["MainUIPanels.PlayEnd()"]
    click A "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L287" "打开源码"
    B["CargoPersistence.SaveReturnCargo()"]
    click B "../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L11" "打开源码"
    C["ReturnStatusPanel.ShowFailure()"]
    click C "../Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs#L20" "打开源码"
    D["MainUIPanels.CompletePlayEnd()"]
    click D "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304" "打开源码"
    E["ReturnStatusPanel.Clear()"]
    click E "../Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs#L37" "打开源码"
    F["PlayManager.PlayEnd()"]
    click F "../Assets/Scripts/Features/Simulation/PlayManager.cs#L295" "打开源码"
    G["GameManager.Init()"]
    click G "../Assets/Scripts/Features/Bootstrap/GameManager.cs#L25" "打开源码"
    A --> B
    A -->|失败：保持会话| C
    A -->|成功| D
    D --> E
    D --> F
    F -->|清理后| G
```

- [MainUIPanels.PlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L287)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayEnd -->
- [CargoPersistence.SaveReturnCargo](../Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs#L11)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoPersistence.cs|SaveReturnCargo -->
- [ReturnStatusPanel.ShowFailure](../Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs#L20)
<!-- source: Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs|ShowFailure -->
- [MainUIPanels.CompletePlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|CompletePlayEnd -->
- [ReturnStatusPanel.Clear](../Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs#L37)
<!-- source: Assets/Scripts/Features/Presentation/Interface/ReturnStatusPanel.cs|Clear -->
- [PlayManager.PlayEnd](../Assets/Scripts/Features/Simulation/PlayManager.cs#L295)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayEnd -->
- [GameManager.Init](../Assets/Scripts/Features/Bootstrap/GameManager.cs#L25)
<!-- source: Assets/Scripts/Features/Bootstrap/GameManager.cs|Init -->

## 维修、回收和移动 Home 返航

机器人使用 Home 实时停靠点与刚体点速度；任务与返航均从机体做净空检测，持续绕障并在停滞后脱困。动态 Bot 及其子碰撞体从净空、重叠和速度走廊查询中过滤，墙体等真实障碍仍参与制动。单次同步决策复用碰撞体过滤与净空结果，入口清空过滤缓存；导航同帧复用实时目标距离，不降低紧急制动频率。BotNavigationPolicy 固化导航规则，不接受 Inspector/CSV 速度或寻路参数；任务/返航共用无固定速度上限的加速/到达控制，按距离与净空制动并优先消除危险纵向速度；连续两次净空后恢复直达，退出重叠后立即重选路线；停滞窗口在脱困决策后重置，避免历史最近距离触发重复换向。取消任务释放认领。

```mermaid
flowchart TD
    A["RepairBot.FixedUpdate()"]
    click A "../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L37" "打开源码"
    B["Bot.TickNavigation()"]
    click B "../Assets/Scripts/Features/Bots/Bot.cs#L75" "打开源码"
    C["RepairTargetSearch.Scan()"]
    click C "../Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs#L15" "打开源码"
    D["Bot.NavigateToTarget()"]
    click D "../Assets/Scripts/Features/Bots/Bot.cs#L171" "打开源码"
    E["BotFlightMotion.NavigateToTarget()"]
    click E "../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L43" "打开源码"
    F["BotObstacleAvoidance.Evaluate()"]
    click F "../Assets/Scripts/Features/Bots/Navigation/BotObstacleAvoidance.cs#L69" "打开源码"
    G["RepairBot.CheckAndRepair()"]
    click G "../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L108" "打开源码"
    H["RepairBeamView.Update()"]
    click H "../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L44" "打开源码"
    I["CollectionBot.FixedUpdate()"]
    click I "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31" "打开源码"
    J["CollectionBot.FindTarget()"]
    click J "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L103" "打开源码"
    K["LootDrop.Claim()"]
    click K "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L188" "打开源码"
    L["CollectionBot.CollectTarget()"]
    click L "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L79" "打开源码"
    M["LootDrop.Carry()"]
    click M "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L200" "打开源码"
    N["CollectionBot.ReturnCargo()"]
    click N "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L88" "打开源码"
    O["Bot.NavigateHomeSmoothly()"]
    click O "../Assets/Scripts/Features/Bots/Bot.cs#L191" "打开源码"
    P["BotFlightMotion.NavigateHome()"]
    click P "../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L48" "打开源码"
    Q["BotObstacleAvoidance.ConstrainVelocity()"]
    click Q "../Assets/Scripts/Features/Bots/Navigation/BotObstacleAvoidance.cs#L258" "打开源码"
    R["Bot.ReturnHome()"]
    click R "../Assets/Scripts/Features/Bots/Bot.cs#L234" "打开源码"
    S["LootDrop.Deliver()"]
    click S "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L231" "打开源码"
    T["CollectionBot.Abandon()"]
    click T "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L140" "打开源码"
    A --> B
    A -->|扫描目标时| C
    A --> D
    D --> E
    E -->|经 SampleGuidance| F
    E -->|每物理帧经 ApplyMovement| Q
    A --> G
    A --> H
    I --> B
    I --> J
    J --> K
    I --> L
    L --> D
    L --> M
    I --> N
    N --> O
    O --> P
    P -->|每物理帧经 ApplyMovement| Q
    P -->|机体净空与脱困| F
    O -->|可停靠| R
    N -->|完成停靠| S
    N --> T
```

- [RepairBot.FixedUpdate](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L37)
<!-- source: Assets/Scripts/Features/Bots/Repair/RepairBot.cs|FixedUpdate -->
- [Bot.TickNavigation](../Assets/Scripts/Features/Bots/Bot.cs#L75)
<!-- source: Assets/Scripts/Features/Bots/Bot.cs|TickNavigation -->
- [RepairTargetSearch.Scan](../Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs#L15)
<!-- source: Assets/Scripts/Features/Bots/Repair/RepairTargetSearch.cs|Scan -->
- [Bot.NavigateToTarget](../Assets/Scripts/Features/Bots/Bot.cs#L171)
<!-- source: Assets/Scripts/Features/Bots/Bot.cs|NavigateToTarget -->
- [BotFlightMotion.NavigateToTarget](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L43)
<!-- source: Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs|NavigateToTarget -->
- [BotObstacleAvoidance.Evaluate](../Assets/Scripts/Features/Bots/Navigation/BotObstacleAvoidance.cs#L69)
<!-- source: Assets/Scripts/Features/Bots/Navigation/BotObstacleAvoidance.cs|Evaluate -->
- [RepairBot.CheckAndRepair](../Assets/Scripts/Features/Bots/Repair/RepairBot.cs#L108)
<!-- source: Assets/Scripts/Features/Bots/Repair/RepairBot.cs|CheckAndRepair -->
- [RepairBeamView.Update](../Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs#L44)
<!-- source: Assets/Scripts/Features/Bots/Repair/RepairBeamView.cs|Update -->
- [CollectionBot.FixedUpdate](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|FixedUpdate -->
- [CollectionBot.FindTarget](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L103)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|FindTarget -->
- [LootDrop.Claim](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L188)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Claim -->
- [CollectionBot.CollectTarget](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L79)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|CollectTarget -->
- [LootDrop.Carry](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L200)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Carry -->
- [CollectionBot.ReturnCargo](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L88)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|ReturnCargo -->
- [Bot.NavigateHomeSmoothly](../Assets/Scripts/Features/Bots/Bot.cs#L191)
<!-- source: Assets/Scripts/Features/Bots/Bot.cs|NavigateHomeSmoothly -->
- [BotFlightMotion.NavigateHome](../Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs#L48)
<!-- source: Assets/Scripts/Features/Bots/Navigation/BotFlightMotion.cs|NavigateHome -->
- [BotObstacleAvoidance.ConstrainVelocity](../Assets/Scripts/Features/Bots/Navigation/BotObstacleAvoidance.cs#L258)
<!-- source: Assets/Scripts/Features/Bots/Navigation/BotObstacleAvoidance.cs|ConstrainVelocity -->
- [Bot.ReturnHome](../Assets/Scripts/Features/Bots/Bot.cs#L234)
<!-- source: Assets/Scripts/Features/Bots/Bot.cs|ReturnHome -->
- [LootDrop.Deliver](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L231)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Deliver -->
- [CollectionBot.Abandon](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L140)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|Abandon -->

## 掉落概率、表现与文字遮挡

敌方残骸先抽稀有部件，再抽金币；货仓存量不受爆率影响。Update散落/吸附，LateUpdate更新表现和与Icon共用的遮挡查询；Carry停止散落并还原显示姿态。

```mermaid
flowchart TD
    A["WreckSalvage.Convert()"]
    click A "../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10" "打开源码"
    B["LootDrop.Spawn()"]
    click B "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L83" "打开源码"
    C["LootDrop.Initialize()"]
    click C "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L132" "打开源码"
    D["LootDrop.Update()"]
    click D "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L261" "打开源码"
    E["LootDrop.LateUpdate()"]
    click E "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L320" "打开源码"
    F["CombatHud.IsWorldPointOccluded()"]
    click F "../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L154" "打开源码"
    G["LootDrop.Carry()"]
    click G "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L200" "打开源码"
    H["LootDrop.ResetDisplayPose()"]
    click H "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L364" "打开源码"
    I["LootDrop.Deliver()"]
    click I "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L231" "打开源码"
    J["LootDrop.Release()"]
    click J "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L214" "打开源码"
    A -->|通过掉落抽签| B
    B -->|新建时| C
    D -->|货币吸附到达| I
    E -->|屏幕内文字| F
    E -->|携带或非游玩| H
    G --> H
    I --> J
```

- [WreckSalvage.Convert](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10)
<!-- source: Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs|Convert -->
- [LootDrop.Spawn](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L83)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Spawn -->
- [LootDrop.Initialize](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L132)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Initialize -->
- [LootDrop.Update](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L261)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Update -->
- [LootDrop.LateUpdate](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L320)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|LateUpdate -->
- [CombatHud.IsWorldPointOccluded](../Assets/Scripts/Features/Combat/Interface/CombatHud.cs#L154)
<!-- source: Assets/Scripts/Features/Combat/Interface/CombatHud.cs|IsWorldPointOccluded -->
- [LootDrop.Carry](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L200)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Carry -->
- [LootDrop.ResetDisplayPose](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L364)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|ResetDisplayPose -->
- [LootDrop.Deliver](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L231)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Deliver -->
- [LootDrop.Release](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L214)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Release -->

## 表现、镜头与 UI 生命周期

游戏事件选择效果；新建管理器的逐次毁伤/VFX日志默认关闭。短时光束/点光源按场景复用，闲置有界、活动不裁剪；到期回收，场景卸载释放。GPU突发仍沿原预算释放。镜头取景是计算，输入及协程由 CameraController 持有。

```mermaid
flowchart TD
    A["VisualEffectsManager.TryPlayBlockExplosion()"]
    click A "../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L84" "打开源码"
    B["VisualEffectsManager.PlayBlockExplosion()"]
    click B "../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L335" "打开源码"
    C["BlockVfxLibrary.Play()"]
    click C "../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112" "打开源码"
    D["TransientEffects.CreateLightFlash()"]
    click D "../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L48" "打开源码"
    E["LightFade.Update()"]
    click E "../Assets/Scripts/Features/Presentation/Effects/LightFade.cs#L28" "打开源码"
    F["CameraShake.Play()"]
    click F "../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L29" "打开源码"
    G["CameraShake.CameraShakeRoutine()"]
    click G "../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L57" "打开源码"
    H["VisualEffectsManager.OnDisable()"]
    click H "../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L213" "打开源码"
    I["CameraShake.Cancel()"]
    click I "../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L17" "打开源码"
    J["CameraController.SmoothFocusCameraOnBlock()"]
    click J "../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L225" "打开源码"
    K["CameraFraming.TryGetFocusPose()"]
    click K "../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L26" "打开源码"
    L["CameraController.SmoothFocusRoutine()"]
    click L "../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L402" "打开源码"
    M["MainUIPanels.ShowCreatePanel()"]
    click M "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L90" "打开源码"
    N["PanelTransitions.Show()"]
    click N "../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L29" "打开源码"
    O["PanelTransitions.Fade()"]
    click O "../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L53" "打开源码"
    P["MainUIPanels.OnDisable()"]
    click P "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L76" "打开源码"
    Q["PanelTransitions.CancelAll()"]
    click Q "../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L16" "打开源码"
    R["CameraController.LateUpdate()"]
    click R "../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L63" "打开源码"
    S["CameraController.ResolveThirdPersonCollision()"]
    click S "../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L126" "打开源码"
    T["StatusIcon.UpdateIconScreenPosition()"]
    click T "../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L317" "打开源码"
    U["TransientEffectPool.RentLight()"]
    click U "../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L52" "打开源码"
    V["TransientEffectPool.Return()"]
    click V "../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L73" "打开源码"
    W["TransientEffects.CreateLineStreak()"]
    click W "../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L7" "打开源码"
    X["TransientEffectPool.RentBeam()"]
    click X "../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L32" "打开源码"
    Y["BeamFade.Update()"]
    click Y "../Assets/Scripts/Features/Presentation/Effects/BeamFade.cs#L26" "打开源码"
    Z["TransientEffectPool.OnDestroy()"]
    click Z "../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L111" "打开源码"
    A --> B
    B --> C
    B -->|成功生成| D
    D -.->|引擎Update| E
    D --> U
    E -->|到期| V
    W --> X
    W -.->|引擎Update| Y
    Y -->|到期| V
    Z -.->|随场景销毁子对象| V
    B --> F
    F -.->|协程| G
    H --> I
    J --> K
    J -.->|协程| L
    M --> N
    N -.->|协程| O
    P --> Q
    R -->|第三人称期望与平滑位置| S
```

- [VisualEffectsManager.TryPlayBlockExplosion](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L84)
<!-- source: Assets/Scripts/Features/Presentation/VisualEffectsManager.cs|TryPlayBlockExplosion -->
- [VisualEffectsManager.PlayBlockExplosion](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L335)
<!-- source: Assets/Scripts/Features/Presentation/VisualEffectsManager.cs|PlayBlockExplosion -->
- [BlockVfxLibrary.Play](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs|Play -->
- [TransientEffects.CreateLightFlash](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L48)
<!-- source: Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs|CreateLightFlash -->
- [LightFade.Update](../Assets/Scripts/Features/Presentation/Effects/LightFade.cs#L28)
<!-- source: Assets/Scripts/Features/Presentation/Effects/LightFade.cs|Update -->
- [CameraShake.Play](../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L29)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraShake.cs|Play -->
- [CameraShake.CameraShakeRoutine](../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L57)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraShake.cs|CameraShakeRoutine -->
- [VisualEffectsManager.OnDisable](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L213)
<!-- source: Assets/Scripts/Features/Presentation/VisualEffectsManager.cs|OnDisable -->
- [CameraShake.Cancel](../Assets/Scripts/Features/Presentation/Camera/CameraShake.cs#L17)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraShake.cs|Cancel -->
- [CameraController.SmoothFocusCameraOnBlock](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L225)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraController.cs|SmoothFocusCameraOnBlock -->
- [CameraFraming.TryGetFocusPose](../Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs#L26)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraFraming.cs|TryGetFocusPose -->
- [CameraController.SmoothFocusRoutine](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L402)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraController.cs|SmoothFocusRoutine -->
- [MainUIPanels.ShowCreatePanel](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L90)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|ShowCreatePanel -->
- [PanelTransitions.Show](../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L29)
<!-- source: Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs|Show -->
- [PanelTransitions.Fade](../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L53)
<!-- source: Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs|Fade -->
- [MainUIPanels.OnDisable](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L76)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|OnDisable -->
- [PanelTransitions.CancelAll](../Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs#L16)
<!-- source: Assets/Scripts/Features/Presentation/Interface/PanelTransitions.cs|CancelAll -->
- [CameraController.LateUpdate](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L63)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraController.cs|LateUpdate -->
- [CameraController.ResolveThirdPersonCollision](../Assets/Scripts/Features/Presentation/Camera/CameraController.cs#L126)
<!-- source: Assets/Scripts/Features/Presentation/Camera/CameraController.cs|ResolveThirdPersonCollision -->
- [StatusIcon.UpdateIconScreenPosition](../Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs#L317)
<!-- source: Assets/Scripts/Features/Presentation/Interface/StatusIcon.cs|UpdateIconScreenPosition -->
- [TransientEffectPool.RentLight](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L52)
<!-- source: Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs|RentLight -->
- [TransientEffectPool.Return](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L73)
<!-- source: Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs|Return -->
- [TransientEffects.CreateLineStreak](../Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs#L7)
<!-- source: Assets/Scripts/Features/Presentation/Effects/TransientEffects.cs|CreateLineStreak -->
- [TransientEffectPool.RentBeam](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L32)
<!-- source: Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs|RentBeam -->
- [BeamFade.Update](../Assets/Scripts/Features/Presentation/Effects/BeamFade.cs#L26)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BeamFade.cs|Update -->
- [TransientEffectPool.OnDestroy](../Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs#L111)
<!-- source: Assets/Scripts/Features/Presentation/Effects/TransientEffectPool.cs|OnDestroy -->

## 探索副场景与世界组件会话

源码推导：Main选择探索后Additive加载Exploration，绑定后显式准备；Start预热仅用于独立组件夹具。资源异步读取、建筑按毫秒预算创建。请求游玩可取消；Ready后暂停物理并显示不透明遮罩，下一帧启用根并按预算激活建筑，完成后恢复原timeScale。退出、死亡或激活中断均释放暂停状态并丢弃会话；失败下一Update回到统一退出链。

```mermaid
flowchart TD
    A["WorldSessionManager.Start()"]
    click A "../Assets/Scripts/Features/World/WorldSessionManager.cs#L79" "打开源码"
    B["WorldSessionManager.BeginPreparation()"]
    click B "../Assets/Scripts/Features/World/WorldSessionManager.cs#L99" "打开源码"
    C["WorldSessionManager.StopPreparation()"]
    click C "../Assets/Scripts/Features/World/WorldSessionManager.cs#L493" "打开源码"
    D["WorldSessionManager.RunGuarded()"]
    click D "../Assets/Scripts/Features/World/WorldSessionManager.cs#L339" "打开源码"
    E["WorldSessionManager.StreamWorld()"]
    click E "../Assets/Scripts/Features/World/WorldSessionManager.cs#L370" "打开源码"
    F["WorldSessionManager.CompletePendingPlay()"]
    click F "../Assets/Scripts/Features/World/WorldSessionManager.cs#L212" "打开源码"
    G["WorldSessionManager.RequestPlay()"]
    click G "../Assets/Scripts/Features/World/WorldSessionManager.cs#L188" "打开源码"
    H["WorldSessionManager.CancelPlayRequest()"]
    click H "../Assets/Scripts/Features/World/WorldSessionManager.cs#L205" "打开源码"
    I["MainUIPanels.CompletePlayStart()"]
    click I "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L267" "打开源码"
    J["PlayManager.PlayStart()"]
    click J "../Assets/Scripts/Features/Simulation/PlayManager.cs#L109" "打开源码"
    K["WorldSessionManager.EnterPlay()"]
    click K "../Assets/Scripts/Features/World/WorldSessionManager.cs#L252" "打开源码"
    L["WorldSessionManager.EnableWorld()"]
    click L "../Assets/Scripts/Features/World/WorldSessionManager.cs#L284" "打开源码"
    M["BuildingRuntime.ActivateRoutine()"]
    click M "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L138" "打开源码"
    N["WorldSessionManager.EndSession()"]
    click N "../Assets/Scripts/Features/World/WorldSessionManager.cs#L327" "打开源码"
    O["WorldSessionManager.Fail()"]
    click O "../Assets/Scripts/Features/World/WorldSessionManager.cs#L525" "打开源码"
    P["WorldSessionManager.Update()"]
    click P "../Assets/Scripts/Features/World/WorldSessionManager.cs#L443" "打开源码"
    Q["MainUIPanels.CompletePlayEnd()"]
    click Q "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304" "打开源码"
    R["WorldSessionManager.OnDisable()"]
    click R "../Assets/Scripts/Features/World/WorldSessionManager.cs#L540" "打开源码"
    S["WorldBuildingManager.ClearSession()"]
    click S "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L235" "打开源码"
    T["ProceduralVoxelWorld.ClearStreaming()"]
    click T "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L339" "打开源码"
    U["WorldSessionManager.PrepareWorld()"]
    click U "../Assets/Scripts/Features/World/WorldSessionManager.cs#L139" "打开源码"
    V["WorldSessionManager.FinishEntering()"]
    click V "../Assets/Scripts/Features/World/WorldSessionManager.cs#L318" "打开源码"
    W["WorldLoadingPanel.Show()"]
    click W "../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L39" "打开源码"
    X["WorldBuildingManager.PrepareForUnload()"]
    click X "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L224" "打开源码"
    Y["BuildingRuntime.PrepareForUnload()"]
    click Y "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L181" "打开源码"
    Z["CollectionBot.PrepareForHomeDestruction()"]
    click Z "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L163" "打开源码"
    AA["LootDrop.Release()"]
    click AA "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L214" "打开源码"
    AB["WorldSessionManager.StopSession()"]
    click AB "../Assets/Scripts/Features/World/WorldSessionManager.cs#L545" "打开源码"
    A --> B
    B --> C
    B --> S
    B --> T
    B -->|世界根停用前| X
    O -->|故障停用前| X
    R -->|世界根停用前| X
    X --> Y
    Y --> Z
    Z -->|经Abandon| AA
    B -.->|StartCoroutine| D
    D --> U
    U --> E
    D -->|捕获异常| O
    E -->|全部初始列完成后Ready| F
    G -->|已Ready| F
    H -.->|清除请求，阻止后续切换| F
    F -->|存在有效请求| I
    I --> J
    J --> K
    K -->|暂停物理，先呈现遮罩| W
    K -.->|下一帧| L
    L -.->|逐批激活| M
    L --> V
    L -->|完成隐藏遮罩| W
    C --> V
    N -->|独立组件夹具| B
    N -->|Main模式宿主| AB
    O -.->|游玩中故障，下一Update| P
    P --> Q
    Q -->|经PlayManager.PlayEnd| N
    R --> C
    R --> S
    R --> T
```

- [WorldSessionManager.Start](../Assets/Scripts/Features/World/WorldSessionManager.cs#L79)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|Start -->
- [WorldSessionManager.BeginPreparation](../Assets/Scripts/Features/World/WorldSessionManager.cs#L99)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|BeginPreparation -->
- [WorldSessionManager.StopPreparation](../Assets/Scripts/Features/World/WorldSessionManager.cs#L493)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|StopPreparation -->
- [WorldSessionManager.RunGuarded](../Assets/Scripts/Features/World/WorldSessionManager.cs#L339)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|RunGuarded -->
- [WorldSessionManager.StreamWorld](../Assets/Scripts/Features/World/WorldSessionManager.cs#L370)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|StreamWorld -->
- [WorldSessionManager.CompletePendingPlay](../Assets/Scripts/Features/World/WorldSessionManager.cs#L212)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|CompletePendingPlay -->
- [WorldSessionManager.RequestPlay](../Assets/Scripts/Features/World/WorldSessionManager.cs#L188)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|RequestPlay -->
- [WorldSessionManager.CancelPlayRequest](../Assets/Scripts/Features/World/WorldSessionManager.cs#L205)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|CancelPlayRequest -->
- [MainUIPanels.CompletePlayStart](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L267)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|CompletePlayStart -->
- [PlayManager.PlayStart](../Assets/Scripts/Features/Simulation/PlayManager.cs#L109)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayStart -->
- [WorldSessionManager.EnterPlay](../Assets/Scripts/Features/World/WorldSessionManager.cs#L252)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|EnterPlay -->
- [WorldSessionManager.EnableWorld](../Assets/Scripts/Features/World/WorldSessionManager.cs#L284)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|EnableWorld -->
- [BuildingRuntime.ActivateRoutine](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L138)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|ActivateRoutine -->
- [WorldSessionManager.EndSession](../Assets/Scripts/Features/World/WorldSessionManager.cs#L327)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|EndSession -->
- [WorldSessionManager.Fail](../Assets/Scripts/Features/World/WorldSessionManager.cs#L525)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|Fail -->
- [WorldSessionManager.Update](../Assets/Scripts/Features/World/WorldSessionManager.cs#L443)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|Update -->
- [MainUIPanels.CompletePlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|CompletePlayEnd -->
- [WorldSessionManager.OnDisable](../Assets/Scripts/Features/World/WorldSessionManager.cs#L540)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|OnDisable -->
- [WorldBuildingManager.ClearSession](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L235)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|ClearSession -->
- [ProceduralVoxelWorld.ClearStreaming](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L339)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|ClearStreaming -->
- [WorldSessionManager.PrepareWorld](../Assets/Scripts/Features/World/WorldSessionManager.cs#L139)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|PrepareWorld -->
- [WorldSessionManager.FinishEntering](../Assets/Scripts/Features/World/WorldSessionManager.cs#L318)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|FinishEntering -->
- [WorldLoadingPanel.Show](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L39)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs|Show -->
- [WorldBuildingManager.PrepareForUnload](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L224)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|PrepareForUnload -->
- [BuildingRuntime.PrepareForUnload](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L181)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|PrepareForUnload -->
- [CollectionBot.PrepareForHomeDestruction](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L163)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|PrepareForHomeDestruction -->
- [LootDrop.Release](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L214)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Release -->
- [WorldSessionManager.StopSession](../Assets/Scripts/Features/World/WorldSessionManager.cs#L545)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|StopSession -->

## 独立世界组件夹具的启动加载遮罩

源码推导：仅独立组件夹具prepareOnStart启用时，Start开始后台世界准备后显示启动遮罩；正式Main不预热世界。BlueprintLoader按恢复数量报告0..95%，连接重建完成才到100%。面板Update等权合成载具/世界进度，两者完成才隐藏。用户请求游玩、取消或新会话显式Show会结束启动遮罩所有权，之后使用原来的世界进入遮罩；不改变蓝图加载间隔和动画镜头。

```mermaid
flowchart TD
    A["WorldSessionManager.Start()"]
    click A "../Assets/Scripts/Features/World/WorldSessionManager.cs#L79" "打开源码"
    B["WorldSessionManager.BeginPreparation()"]
    click B "../Assets/Scripts/Features/World/WorldSessionManager.cs#L99" "打开源码"
    C["WorldLoadingPanel.ShowStartup()"]
    click C "../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L63" "打开源码"
    D["WorldLoadingPanel.Update()"]
    click D "../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L69" "打开源码"
    E["WorldLoadingPanel.Show()"]
    click E "../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L39" "打开源码"
    F["BlueprintLoader.LoadRoutine()"]
    click F "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L107" "打开源码"
    G["BlueprintLoader.RebuildConnections()"]
    click G "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265" "打开源码"
    A --> B
    A --> C
    C --> E
    C -.->|后续UI帧| D
    F -.->|已恢复数量进度| D
    F -.->|95%后连接重建| G
    G -.->|完成后100%| D
    D -->|载具与世界就绪，隐藏| E
```

- [WorldSessionManager.Start](../Assets/Scripts/Features/World/WorldSessionManager.cs#L79)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|Start -->
- [WorldSessionManager.BeginPreparation](../Assets/Scripts/Features/World/WorldSessionManager.cs#L99)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|BeginPreparation -->
- [WorldLoadingPanel.ShowStartup](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L63)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs|ShowStartup -->
- [WorldLoadingPanel.Update](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L69)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs|Update -->
- [WorldLoadingPanel.Show](../Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs#L39)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldLoadingPanel.cs|Show -->
- [BlueprintLoader.LoadRoutine](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L107)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|LoadRoutine -->
- [BlueprintLoader.RebuildConnections](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|RebuildConnections -->

## 用户建筑库、自然出生和远景

源码推导：World和WorldLab通过SaveManager.GetBuildingBlueprintFiles共用建筑文件仓；每次会话只读取BuildingBlueprints中的有效JSON，不合并内置模板；目录为空不生成建筑，单独报告坏文件并快照每套蓝图实际占地；落点按同一模板索引居中，半格支撑边距外6m接回自然坡面。进入时按载具覆盖范围最高地面加净空抬升。详细地形外分帧缓存无碰撞体素列，保持同样阶梯及草土材质，按列版本隐藏已加载列代理；默认12圈后衔接8m采样远景，空洞与外环原子切换，裙边遮缝、距离雾过渡背景。跨四列重采样，淘汰外环缓存并释放网格；退出恢复相机/雾并销毁自有网格。

```mermaid
flowchart TD
    A["WorldSessionManager.PrepareWorld()"]
    click A "../Assets/Scripts/Features/World/WorldSessionManager.cs#L139" "打开源码"
    B["WorldBuildingLibrary.LoadRoutine()"]
    click B "../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L19" "打开源码"
    C["WorldBuildingValidation.ValidateBlueprint()"]
    click C "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L11" "打开源码"
    D["WorldBuildingFootprints.Get()"]
    click D "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingFootprints.cs#L32" "打开源码"
    E["ProceduralVoxelWorld.InitializeStreaming()"]
    click E "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L180" "打开源码"
    F["WorldDistanceView.Refresh()"]
    click F "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L69" "打开源码"
    G["WorldDistanceView.Rebuild()"]
    click G "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L95" "打开源码"
    H["StreamingWorldTerrain.SurfaceHeight()"]
    click H "../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L26" "打开源码"
    I["WorldSessionManager.EnterPlay()"]
    click I "../Assets/Scripts/Features/World/WorldSessionManager.cs#L252" "打开源码"
    J["ProceduralVoxelWorld.FindSpawnSurface()"]
    click J "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L54" "打开源码"
    K["WorldDistanceView.Enter()"]
    click K "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L340" "打开源码"
    L["WorldSessionManager.StopPreparation()"]
    click L "../Assets/Scripts/Features/World/WorldSessionManager.cs#L493" "打开源码"
    M["WorldDistanceView.Dispose()"]
    click M "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L367" "打开源码"
    N["WorldDistanceView.BuildRingColumn()"]
    click N "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L286" "打开源码"
    O["WorldTerrainTransitionMesh.Build()"]
    click O "../Assets/Scripts/Features/World/Presentation/WorldTerrainTransitionMesh.cs#L9" "打开源码"
    P["WorldDistanceView.UpdateRingVisibility()"]
    click P "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L248" "打开源码"
    Q["WorldDistanceView.ReleaseExpiredRing()"]
    click Q "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L266" "打开源码"
    R["SaveManager.GetBuildingBlueprintFiles()"]
    click R "../Assets/Scripts/Features/Building/SaveManager.cs#L84" "打开源码"
    S["ProceduralVoxelWorld.LoadColumn()"]
    click S "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L236" "打开源码"
    T["ProceduralVoxelWorld.UnloadColumn()"]
    click T "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L318" "打开源码"
    U["WorldDistanceView.OnColumnVisibilityChanged()"]
    click U "../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L234" "打开源码"
    A -->|取得共用文件仓| R
    A --> B
    B -->|异步预热后经ReadSources校验| C
    A --> E
    H -->|经Height按所选蓝图取范围| D
    A -.->|初始列完成才刷新| F
    F -->|列版本更新索引，跨四列才重采样| G
    G --> H
    I --> J
    J --> H
    I --> K
    L -->|先停止远景协程| M
    G -.->|预算内逐列缓存| N
    N --> O
    O -->|同近景2m阶梯轮廓| H
    F -->|列版本变化隐藏详细列代理| P
    G -->|孔洞和外环同帧切换| P
    G --> Q
    S -.->|发布列同步通知| U
    T -.->|卸载列同步通知| U
    M -->|取消订阅| U
```

- [WorldSessionManager.PrepareWorld](../Assets/Scripts/Features/World/WorldSessionManager.cs#L139)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|PrepareWorld -->
- [WorldBuildingLibrary.LoadRoutine](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L19)
<!-- source: Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs|LoadRoutine -->
- [WorldBuildingValidation.ValidateBlueprint](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L11)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs|ValidateBlueprint -->
- [WorldBuildingFootprints.Get](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingFootprints.cs#L32)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingFootprints.cs|Get -->
- [ProceduralVoxelWorld.InitializeStreaming](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L180)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|InitializeStreaming -->
- [WorldDistanceView.Refresh](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L69)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|Refresh -->
- [WorldDistanceView.Rebuild](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L95)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|Rebuild -->
- [StreamingWorldTerrain.SurfaceHeight](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L26)
<!-- source: Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs|SurfaceHeight -->
- [WorldSessionManager.EnterPlay](../Assets/Scripts/Features/World/WorldSessionManager.cs#L252)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|EnterPlay -->
- [ProceduralVoxelWorld.FindSpawnSurface](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L54)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|FindSpawnSurface -->
- [WorldDistanceView.Enter](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L340)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|Enter -->
- [WorldSessionManager.StopPreparation](../Assets/Scripts/Features/World/WorldSessionManager.cs#L493)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|StopPreparation -->
- [WorldDistanceView.Dispose](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L367)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|Dispose -->
- [WorldDistanceView.BuildRingColumn](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L286)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|BuildRingColumn -->
- [WorldTerrainTransitionMesh.Build](../Assets/Scripts/Features/World/Presentation/WorldTerrainTransitionMesh.cs#L9)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldTerrainTransitionMesh.cs|Build -->
- [WorldDistanceView.UpdateRingVisibility](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L248)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|UpdateRingVisibility -->
- [WorldDistanceView.ReleaseExpiredRing](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L266)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|ReleaseExpiredRing -->
- [SaveManager.GetBuildingBlueprintFiles](../Assets/Scripts/Features/Building/SaveManager.cs#L84)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|GetBuildingBlueprintFiles -->
- [ProceduralVoxelWorld.LoadColumn](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L236)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|LoadColumn -->
- [ProceduralVoxelWorld.UnloadColumn](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L318)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|UnloadColumn -->
- [WorldDistanceView.OnColumnVisibilityChanged](../Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs#L234)
<!-- source: Assets/Scripts/Features/World/Presentation/WorldDistanceView.cs|OnColumnVisibilityChanged -->

## 区块流式与建筑休眠

源码推导：纯种子采样和稀疏体素差量不分配全图。加载按列，一格邻域快照交单个后台任务合并面，主线程提交碰撞，完整列才发布；先快照仓内货物、释放机器人携货/认领，再停用休眠建筑并卸载远处地形；不转换残骸奖励。静止满载时每0.2秒维护，跨列和列版本变化立即唤醒；两圈内先补近身地形再恢复建筑，详细加载6圈、卸载8圈。建筑实例受预算限制，超限快照延期；物理帧按载具尺寸与预测路径冻结等待，完整路径有碰撞后恢复。

```mermaid
flowchart TD
    A["WorldSessionManager.StreamWorld()"]
    click A "../Assets/Scripts/Features/World/WorldSessionManager.cs#L370" "打开源码"
    B["WorldRegionStreaming.LoadNext()"]
    click B "../Assets/Scripts/Features/World/Streaming/WorldRegionStreaming.cs#L32" "打开源码"
    C["WorldBuildingStreaming.UnloadDistantRoutine()"]
    click C "../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L105" "打开源码"
    D["BuildingRuntime.Capture()"]
    click D "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L189" "打开源码"
    E["WorldBuildingManager.RemoveBuilding()"]
    click E "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L211" "打开源码"
    F["ProceduralVoxelWorld.UnloadColumn()"]
    click F "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L318" "打开源码"
    G["ProceduralVoxelWorld.LoadColumn()"]
    click G "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L236" "打开源码"
    H["VoxelWorldData.Get()"]
    click H "../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L32" "打开源码"
    I["StreamingWorldTerrain.Sample()"]
    click I "../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L55" "打开源码"
    J["WorldBuildingStreaming.LoadColumn()"]
    click J "../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L34" "打开源码"
    K["WorldBuildingStreaming.RestoreAvailable()"]
    click K "../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L61" "打开源码"
    L["WorldBuildingManager.RestoreBuildingRoutine()"]
    click L "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L145" "打开源码"
    M["WorldSessionManager.FixedUpdate()"]
    click M "../Assets/Scripts/Features/World/WorldSessionManager.cs#L418" "打开源码"
    N["WorldPlayerStreamingGuard.Tick()"]
    click N "../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L31" "打开源码"
    O["WorldPlayerStreamingGuard.HasTerrainForBounds()"]
    click O "../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L99" "打开源码"
    P["WorldPlayerStreamingGuard.Release()"]
    click P "../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L138" "打开源码"
    Q["BuildingRuntime.PrepareForUnload()"]
    click Q "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L181" "打开源码"
    R["CollectionBot.PrepareForHomeDestruction()"]
    click R "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L163" "打开源码"
    S["LootDrop.Release()"]
    click S "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L214" "打开源码"
    A --> B
    B -.->|按毫秒预算休眠，跨帧| C
    C --> D
    C --> E
    B -.->|每列让出帧| F
    E -->|停用层级前| Q
    Q --> R
    R -->|经Abandon| S
    B -->|最近未加载列| G
    G -->|经网格构建采样| H
    H -->|无本次编辑差量| I
    B -->|完整列加载后登记快照| J
    B --> K
    K -.->|有地形且有活动预算，分帧恢复| L
    M --> N
    N --> O
    N -->|就绪后恢复刚体| P
    N -.->|预测路径中点Focus，下一渲染帧| A
```

- [WorldSessionManager.StreamWorld](../Assets/Scripts/Features/World/WorldSessionManager.cs#L370)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|StreamWorld -->
- [WorldRegionStreaming.LoadNext](../Assets/Scripts/Features/World/Streaming/WorldRegionStreaming.cs#L32)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldRegionStreaming.cs|LoadNext -->
- [WorldBuildingStreaming.UnloadDistantRoutine](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L105)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs|UnloadDistantRoutine -->
- [BuildingRuntime.Capture](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L189)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|Capture -->
- [WorldBuildingManager.RemoveBuilding](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L211)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|RemoveBuilding -->
- [ProceduralVoxelWorld.UnloadColumn](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L318)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|UnloadColumn -->
- [ProceduralVoxelWorld.LoadColumn](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L236)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|LoadColumn -->
- [VoxelWorldData.Get](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L32)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs|Get -->
- [StreamingWorldTerrain.Sample](../Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs#L55)
<!-- source: Assets/Scripts/Features/World/Terrain/StreamingWorldTerrain.cs|Sample -->
- [WorldBuildingStreaming.LoadColumn](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L34)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs|LoadColumn -->
- [WorldBuildingStreaming.RestoreAvailable](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L61)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs|RestoreAvailable -->
- [WorldBuildingManager.RestoreBuildingRoutine](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L145)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|RestoreBuildingRoutine -->
- [WorldSessionManager.FixedUpdate](../Assets/Scripts/Features/World/WorldSessionManager.cs#L418)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|FixedUpdate -->
- [WorldPlayerStreamingGuard.Tick](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L31)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs|Tick -->
- [WorldPlayerStreamingGuard.HasTerrainForBounds](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L99)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs|HasTerrainForBounds -->
- [WorldPlayerStreamingGuard.Release](../Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs#L138)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldPlayerStreamingGuard.cs|Release -->
- [BuildingRuntime.PrepareForUnload](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L181)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|PrepareForUnload -->
- [CollectionBot.PrepareForHomeDestruction](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L163)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|PrepareForHomeDestruction -->
- [LootDrop.Release](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L214)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Release -->

## 体素快照与贪心网格

源码推导：仅主线程读取可变体素/稀疏修改及调用Unity Mesh和PhysX；一格halo消除跨块内部面。后台任务只拥有byte快照和独立结果，合并同方向同材质矩形并保留平铺UV。提交前检查列请求及数据版本，取消结果丢弃，实时编辑变化重新采样。每个列加载器仅一个在途任务；WorldLab和显式FlushDirty保留同步路径，LateUpdate同时受块数/毫秒预算限制。

```mermaid
flowchart TD
    A["ProceduralVoxelWorld.LoadColumn()"]
    click A "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L236" "打开源码"
    B["VoxelChunkSnapshot.Capture()"]
    click B "../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L56" "打开源码"
    C["VoxelWorldData.Get()"]
    click C "../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L32" "打开源码"
    D["VoxelSurfaceMesher.Generate()"]
    click D "../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L67" "打开源码"
    E["VoxelSurfaceMesher.MergeSlice()"]
    click E "../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L113" "打开源码"
    F["VoxelSurfaceMesher.AddQuad()"]
    click F "../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L180" "打开源码"
    G["VoxelMeshData.Upload()"]
    click G "../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L18" "打开源码"
    H["VoxelWorldChunk.ApplyMesh()"]
    click H "../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L23" "打开源码"
    I["ProceduralVoxelWorld.UnloadColumn()"]
    click I "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L318" "打开源码"
    J["ProceduralVoxelWorld.ClearStreaming()"]
    click J "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L339" "打开源码"
    K["VoxelSurfaceMesher.Build()"]
    click K "../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L61" "打开源码"
    L["VoxelWorldChunk.Rebuild()"]
    click L "../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L18" "打开源码"
    M["ProceduralVoxelWorld.FlushDirty()"]
    click M "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543" "打开源码"
    A --> B
    B --> C
    A -.->|Task.Run，独立不可变快照| D
    D --> E
    E --> F
    D -.->|完成后恢复主线程，验证请求/数据版本| A
    A --> G
    A --> H
    I -.->|撤销列请求，结果丢弃| A
    J -.->|清空会话，结果丢弃| A
    M --> L
    L --> K
    K --> B
    K --> D
    K --> G
    L --> H
```

- [ProceduralVoxelWorld.LoadColumn](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L236)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|LoadColumn -->
- [VoxelChunkSnapshot.Capture](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L56)
<!-- source: Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs|Capture -->
- [VoxelWorldData.Get](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L32)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs|Get -->
- [VoxelSurfaceMesher.Generate](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L67)
<!-- source: Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs|Generate -->
- [VoxelSurfaceMesher.MergeSlice](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L113)
<!-- source: Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs|MergeSlice -->
- [VoxelSurfaceMesher.AddQuad](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L180)
<!-- source: Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs|AddQuad -->
- [VoxelMeshData.Upload](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs#L18)
<!-- source: Assets/Scripts/Features/World/Terrain/Meshing/VoxelMeshData.cs|Upload -->
- [VoxelWorldChunk.ApplyMesh](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L23)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs|ApplyMesh -->
- [ProceduralVoxelWorld.UnloadColumn](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L318)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|UnloadColumn -->
- [ProceduralVoxelWorld.ClearStreaming](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L339)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|ClearStreaming -->
- [VoxelSurfaceMesher.Build](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L61)
<!-- source: Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs|Build -->
- [VoxelWorldChunk.Rebuild](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L18)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs|Rebuild -->
- [ProceduralVoxelWorld.FlushDirty](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|FlushDirty -->

## 工业零件统一调度与共享发光

源码推导：活动工业零件注册到列表，由各场景 Managers 中的 IndustrialMotionDriver 调度本场景实例，旋转按scaled delta、浮动与发光按unscaled time；同源材质/颜色/频率/幅度只更新一个临时共享材质。既有或后加PropertyBlock保留兼容路径。建造与游玩选择保存源材质，取消高亮恢复当前有效绑定；禁用/重配释放引用，最后一个使用者销毁临时材质。

```mermaid
flowchart TD
    A["IndustrialPartMotion.OnEnable()"]
    click A "../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L37" "打开源码"
    B["IndustrialPartMotion.AnimateScene()"]
    click B "../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L79" "打开源码"
    C["IndustrialMotionDriver.Update()"]
    click C "../Assets/Scripts/Features/Presentation/Blocks/IndustrialMotionDriver.cs#L7" "打开源码"
    D["IndustrialPartMotion.AnimateScene()"]
    click D "../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L79" "打开源码"
    E["IndustrialEmissionBinding.Tick()"]
    click E "../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L96" "打开源码"
    F["IndustrialPartMotion.Animate()"]
    click F "../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L93" "打开源码"
    G["IndustrialEmissionBinding.Apply()"]
    click G "../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L73" "打开源码"
    H["IndustrialPartMotion.OnDisable()"]
    click H "../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L52" "打开源码"
    I["IndustrialPartMotion.ReleaseEmission()"]
    click I "../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L159" "打开源码"
    J["IndustrialEmissionBinding.Dispose()"]
    click J "../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L130" "打开源码"
    K["IndustrialEmissionBinding.CaptureSelection()"]
    click K "../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L111" "打开源码"
    L["IndustrialEmissionBinding.RestoreSelection()"]
    click L "../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L124" "打开源码"
    M["BuildSelection.SelectBlock()"]
    click M "../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L80" "打开源码"
    N["PlayBlockSelection.Highlight()"]
    click N "../Assets/Scripts/Features/Simulation/Presentation/PlayBlockSelection.cs#L9" "打开源码"
    A -.->|注册列表| B
    C --> D
    D --> E
    D --> F
    F --> G
    H --> I
    I --> J
    M --> K
    N --> K
    K -.->|取消选择| L
```

- [IndustrialPartMotion.OnEnable](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L37)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs|OnEnable -->
- [IndustrialPartMotion.AnimateScene](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L79)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs|AnimateScene -->
- [IndustrialMotionDriver.Update](../Assets/Scripts/Features/Presentation/Blocks/IndustrialMotionDriver.cs#L7)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialMotionDriver.cs|Update -->
- [IndustrialPartMotion.AnimateScene](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L79)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs|AnimateScene -->
- [IndustrialEmissionBinding.Tick](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L96)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs|Tick -->
- [IndustrialPartMotion.Animate](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L93)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs|Animate -->
- [IndustrialEmissionBinding.Apply](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L73)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs|Apply -->
- [IndustrialPartMotion.OnDisable](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L52)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs|OnDisable -->
- [IndustrialPartMotion.ReleaseEmission](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L159)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs|ReleaseEmission -->
- [IndustrialEmissionBinding.Dispose](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L130)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs|Dispose -->
- [IndustrialEmissionBinding.CaptureSelection](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L111)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs|CaptureSelection -->
- [IndustrialEmissionBinding.RestoreSelection](../Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs#L124)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialEmissionBinding.cs|RestoreSelection -->
- [BuildSelection.SelectBlock](../Assets/Scripts/Features/Building/Interaction/BuildSelection.cs#L80)
<!-- source: Assets/Scripts/Features/Building/Interaction/BuildSelection.cs|SelectBlock -->
- [PlayBlockSelection.Highlight](../Assets/Scripts/Features/Simulation/Presentation/PlayBlockSelection.cs#L9)
<!-- source: Assets/Scripts/Features/Simulation/Presentation/PlayBlockSelection.cs|Highlight -->

## 世界建筑加载与共享渲染

源码推导：流式建筑以可取消枚举分帧建立，完成才发布；部分实例取消即回滚。模块按毫秒预算创建和启用。同父刚性装饰网格按材质合并，同资源/尺寸/层级共享Mesh；炮塔轴、后坐炮管、机械动画与发光Renderer保持身份，方块碰撞/耐久仍独立。会话退出释放缓存。载具蓝图末尾连接重建也按2ms批次让出帧。

```mermaid
flowchart TD
    A["WorldBuildingManager.RestoreBuildingRoutine()"]
    click A "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L145" "打开源码"
    B["BuildingRuntime.InitializeRoutine()"]
    click B "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L86" "打开源码"
    C["BlueprintBlockFactory.TryRestore()"]
    click C "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7" "打开源码"
    D["WorldBuildingManager.OptimizeVisuals()"]
    click D "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L192" "打开源码"
    E["WorldBuildingRenderCache.Apply()"]
    click E "../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L10" "打开源码"
    F["WorldBuildingRenderCache.Build()"]
    click F "../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L111" "打开源码"
    G["BuildingRuntime.ActivateRoutine()"]
    click G "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L138" "打开源码"
    H["IndustrialPartMotion.ControlsVisual()"]
    click H "../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L32" "打开源码"
    I["ProjectileTurret.MovesVisual()"]
    click I "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L36" "打开源码"
    J["WorldBuildingManager.ClearSession()"]
    click J "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L235" "打开源码"
    K["WorldBuildingRenderCache.Clear()"]
    click K "../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L191" "打开源码"
    L["BlueprintLoader.LoadRoutine()"]
    click L "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L107" "打开源码"
    M["BlueprintLoader.RebuildConnections()"]
    click M "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265" "打开源码"
    A -.->|创建预算| B
    B --> C
    B --> D
    D --> E
    E --> H
    E --> I
    E -->|缓存缺失| F
    A -.->|活动世界分批激活| G
    J --> K
    L -.->|连接重建跨帧| M
```

- [WorldBuildingManager.RestoreBuildingRoutine](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L145)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|RestoreBuildingRoutine -->
- [BuildingRuntime.InitializeRoutine](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L86)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|InitializeRoutine -->
- [BlueprintBlockFactory.TryRestore](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs|TryRestore -->
- [WorldBuildingManager.OptimizeVisuals](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L192)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|OptimizeVisuals -->
- [WorldBuildingRenderCache.Apply](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L10)
<!-- source: Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs|Apply -->
- [WorldBuildingRenderCache.Build](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L111)
<!-- source: Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs|Build -->
- [BuildingRuntime.ActivateRoutine](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L138)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|ActivateRoutine -->
- [IndustrialPartMotion.ControlsVisual](../Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs#L32)
<!-- source: Assets/Scripts/Features/Presentation/Blocks/IndustrialPartMotion.cs|ControlsVisual -->
- [ProjectileTurret.MovesVisual](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L36)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|MovesVisual -->
- [WorldBuildingManager.ClearSession](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L235)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|ClearSession -->
- [WorldBuildingRenderCache.Clear](../Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs#L191)
<!-- source: Assets/Scripts/Features/World/Buildings/Presentation/WorldBuildingRenderCache.cs|Clear -->
- [BlueprintLoader.LoadRoutine](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L107)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|LoadRoutine -->
- [BlueprintLoader.RebuildConnections](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L265)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|RebuildConnections -->

## 耐久调试标签与炮塔搜索预算

源码推导：DebugManager只装配一个耐久标签绘制器；各Durability的启用/禁用只维护活动注册，关闭标签时不扫描模块，不再为每个方块注册OnGUI/LateUpdate。炮塔目标失效时立即重搜，空目标沿用targetRefreshInterval，先按阵营排除不相关单位；每个ControlUnit共享当前物理步的层级快照，启用/禁用/换父级及RefreshChildren使其失效，候选的伤势、位置和真实归属仍实时检查。

```mermaid
flowchart TD
    A["DebugManager.Awake()"]
    click A "../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L50" "打开源码"
    B["Durability.OnEnable()"]
    click B "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L73" "打开源码"
    C["Durability.OnDisable()"]
    click C "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L93" "打开源码"
    D["DurabilityDebugLabels.OnGUI()"]
    click D "../Assets/Scripts/Features/Diagnostics/Interface/DurabilityDebugLabels.cs#L9" "打开源码"
    E["Turret.FixedUpdate()"]
    click E "../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71" "打开源码"
    F["Turret.RefreshTarget()"]
    click F "../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L107" "打开源码"
    G["TurretTargetSelector.FindNearestTarget()"]
    click G "../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L91" "打开源码"
    H["ControlUnit.GetDamageTargets()"]
    click H "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L19" "打开源码"
    I["Durability.OnTransformParentChanged()"]
    click I "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L14" "打开源码"
    J["ControlUnit.RefreshChildren()"]
    click J "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185" "打开源码"
    A -.->|装配单个标签组件，UnityGUI回调| D
    B -.->|活动注册| D
    C -.->|移除注册| D
    E --> F
    F -->|到达搜索间隔或已有目标失效| G
    G --> H
    B -.->|版本失效| H
    C -.->|版本失效| H
    I -.->|版本失效| H
    J -.->|快照失效| H
```

- [DebugManager.Awake](../Assets/Scripts/Features/Diagnostics/DebugManager.cs#L50)
<!-- source: Assets/Scripts/Features/Diagnostics/DebugManager.cs|Awake -->
- [Durability.OnEnable](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L73)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|OnEnable -->
- [Durability.OnDisable](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L93)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|OnDisable -->
- [DurabilityDebugLabels.OnGUI](../Assets/Scripts/Features/Diagnostics/Interface/DurabilityDebugLabels.cs#L9)
<!-- source: Assets/Scripts/Features/Diagnostics/Interface/DurabilityDebugLabels.cs|OnGUI -->
- [Turret.FixedUpdate](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Turret.cs|FixedUpdate -->
- [Turret.RefreshTarget](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L107)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Turret.cs|RefreshTarget -->
- [TurretTargetSelector.FindNearestTarget](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L91)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs|FindNearestTarget -->
- [ControlUnit.GetDamageTargets](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L19)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|GetDamageTargets -->
- [Durability.OnTransformParentChanged](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L14)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|OnTransformParentChanged -->
- [ControlUnit.RefreshChildren](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|RefreshChildren -->

## WorldLab 程序化世界与破坏

源码推导：独立场景不初始化Main存档；固定种子和序列化单元是数据源。采掘/炮塔命中积累伤害，破坏只标记相邻受影响分块；LateUpdate按预算更新渲染和真实MeshCollider。装配建筑组件后F5/F9走下一图的完整快照；纯地形配置仍可保存旧差量。Inspector显式Bake保存网格资产。无无限流式加载。

```mermaid
flowchart TD
    A["WorldLabAuthoring.CreateOrOpen()"]
    click A "../Assets/Editor/World/WorldLab/WorldLabAuthoring.cs#L13" "打开源码"
    B["ProceduralVoxelWorld.Generate()"]
    click B "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101" "打开源码"
    C["VoxelWorldData.Generate()"]
    click C "../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L119" "打开源码"
    D["VoxelWorldChunk.Rebuild()"]
    click D "../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L18" "打开源码"
    E["VoxelSurfaceMesher.Build()"]
    click E "../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L61" "打开源码"
    F["WorldLabAuthoring.BakeMeshes()"]
    click F "../Assets/Editor/World/WorldLab/WorldLabAuthoring.cs#L99" "打开源码"
    G["WorldLabController.Update()"]
    click G "../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L57" "打开源码"
    H["ProceduralVoxelWorld.DamageSphere()"]
    click H "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L451" "打开源码"
    I["ProceduralVoxelWorld.SetCell()"]
    click I "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L400" "打开源码"
    J["ProceduralVoxelWorld.QueueCell()"]
    click J "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L510" "打开源码"
    K["ProceduralVoxelWorld.LateUpdate()"]
    click K "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L538" "打开源码"
    L["ProceduralVoxelWorld.FlushDirty()"]
    click L "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543" "打开源码"
    M["TurretHitResolver.Apply()"]
    click M "../Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs#L7" "打开源码"
    N["VoxelWorldChunk.OnDestroy()"]
    click N "../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L46" "打开源码"
    O["VoxelWorldChunk.ReleaseMesh()"]
    click O "../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L66" "打开源码"
    P["WorldLabController.TransferEdits()"]
    click P "../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L183" "打开源码"
    Q["WorldLabPersistence.Save()"]
    click Q "../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L10" "打开源码"
    R["WorldLabPersistence.Load()"]
    click R "../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L57" "打开源码"
    S["ProceduralVoxelWorld.RestoreEdits()"]
    click S "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L423" "打开源码"
    T["VoxelWorldEdits.Restore()"]
    click T "../Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs#L52" "打开源码"
    A --> B
    B --> C
    B --> D
    D --> E
    A --> F
    G -->|采掘与爆破| H
    G -->|填补| I
    M -->|命中WorldChunk| H
    H -->|达到材质硬度| I
    I --> J
    J -.->|排队，不立即烘焙碰撞| K
    K --> L
    L -->|仅脏块，预算2块每帧| D
    N --> O
    G -->|F5或F9| P
    P -->|无建筑组件时F5| Q
    P -->|无建筑组件时F9| R
    P -->|纯地形加载后| S
    S -->|先校验独立数据| T
    S -.->|提交数据后排队全部分块| K
```

- [WorldLabAuthoring.CreateOrOpen](../Assets/Editor/World/WorldLab/WorldLabAuthoring.cs#L13)
<!-- source: Assets/Editor/World/WorldLab/WorldLabAuthoring.cs|CreateOrOpen -->
- [ProceduralVoxelWorld.Generate](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|Generate -->
- [VoxelWorldData.Generate](../Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs#L119)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldData.cs|Generate -->
- [VoxelWorldChunk.Rebuild](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L18)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs|Rebuild -->
- [VoxelSurfaceMesher.Build](../Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs#L61)
<!-- source: Assets/Scripts/Features/World/Terrain/Meshing/VoxelSurfaceMesher.cs|Build -->
- [WorldLabAuthoring.BakeMeshes](../Assets/Editor/World/WorldLab/WorldLabAuthoring.cs#L99)
<!-- source: Assets/Editor/World/WorldLab/WorldLabAuthoring.cs|BakeMeshes -->
- [WorldLabController.Update](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L57)
<!-- source: Assets/Scripts/Features/World/WorldLab/WorldLabController.cs|Update -->
- [ProceduralVoxelWorld.DamageSphere](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L451)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|DamageSphere -->
- [ProceduralVoxelWorld.SetCell](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L400)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|SetCell -->
- [ProceduralVoxelWorld.QueueCell](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L510)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|QueueCell -->
- [ProceduralVoxelWorld.LateUpdate](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L538)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|LateUpdate -->
- [ProceduralVoxelWorld.FlushDirty](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|FlushDirty -->
- [TurretHitResolver.Apply](../Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs#L7)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs|Apply -->
- [VoxelWorldChunk.OnDestroy](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L46)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs|OnDestroy -->
- [VoxelWorldChunk.ReleaseMesh](../Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs#L66)
<!-- source: Assets/Scripts/Features/World/Terrain/VoxelWorldChunk.cs|ReleaseMesh -->
- [WorldLabController.TransferEdits](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L183)
<!-- source: Assets/Scripts/Features/World/WorldLab/WorldLabController.cs|TransferEdits -->
- [WorldLabPersistence.Save](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L10)
<!-- source: Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs|Save -->
- [WorldLabPersistence.Load](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L57)
<!-- source: Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs|Load -->
- [ProceduralVoxelWorld.RestoreEdits](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L423)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|RestoreEdits -->
- [VoxelWorldEdits.Restore](../Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs#L52)
<!-- source: Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs|Restore -->

## WorldLab 建筑放置与快照

源码推导：BuildingBlueprints是建筑库唯一来源；Catalog仅保留制作参考和初始布局，布局按名称解析共用文件仓，缺失或无效跳过。预览只复制Mesh，B/Q/T/左键触发放置，所有实例使用底座局部坐标、独立身份和ControlUnit战斗注册。F5保存体素差量与当前建筑快照；F9校验资源、身份、耐久和地形后暂存禁用层级，成功才替换活动世界。快照恢复保留已放置实例，不用于生成新建筑。旧差量迁移为空建筑列表。

```mermaid
flowchart TD
    A["WorldBuildingAuthoring.Install()"]
    click A "../Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs#L12" "打开源码"
    B["WorldBuildingPalette.RefreshCatalog()"]
    click B "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L42" "打开源码"
    C["WorldBuildingPalette.Update()"]
    click C "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L56" "打开源码"
    D["WorldBuildingPalette.BeginPlacement()"]
    click D "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L131" "打开源码"
    E["WorldBuildingManager.CanPlace()"]
    click E "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L76" "打开源码"
    F["WorldBuildingPlacement.Check()"]
    click F "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L15" "打开源码"
    G["WorldBuildingManager.Place()"]
    click G "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L96" "打开源码"
    H["WorldBuildingManager.Stage()"]
    click H "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L247" "打开源码"
    I["BuildingRuntime.Initialize()"]
    click I "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L78" "打开源码"
    J["BuildingRuntime.Activate()"]
    click J "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L130" "打开源码"
    K["WorldLabController.TransferEdits()"]
    click K "../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L183" "打开源码"
    L["WorldBuildingManager.Capture()"]
    click L "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L265" "打开源码"
    M["BuildingRuntime.Capture()"]
    click M "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L189" "打开源码"
    N["WorldLabPersistence.SaveSnapshot()"]
    click N "../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L15" "打开源码"
    O["WorldLabPersistence.LoadSnapshot()"]
    click O "../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L68" "打开源码"
    P["WorldBuildingManager.Restore()"]
    click P "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L285" "打开源码"
    Q["WorldBuildingValidation.ValidateState()"]
    click Q "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L71" "打开源码"
    R["VoxelWorldEdits.Restore()"]
    click R "../Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs#L52" "打开源码"
    S["ProceduralVoxelWorld.RestoreEdits()"]
    click S "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L423" "打开源码"
    T["WorldBuildingManager.ClearBuildings()"]
    click T "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L382" "打开源码"
    U["WorldBuildingManager.RestoreBuilding()"]
    click U "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L126" "打开源码"
    V["SaveManager.GetBuildingBlueprintFiles()"]
    click V "../Assets/Scripts/Features/Building/SaveManager.cs#L84" "打开源码"
    W["WorldBuildingLibrary.Load()"]
    click W "../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L51" "打开源码"
    X["WorldBuildingManager.ResetBuildings()"]
    click X "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L338" "打开源码"
    A -.->|保存场景/目录供Start读取| B
    B --> V
    B --> W
    C -->|B键| D
    C --> E
    E --> F
    C -->|有效预览左键| G
    G --> U
    U --> H
    H --> I
    U -->|活动根下| J
    K -->|F5| L
    L --> M
    K -->|原子写盘| N
    K -->|F9| O
    K -->|完整快照| P
    P --> R
    P -->|全部实例| Q
    P -->|校验后暂存禁用层级，失败销毁暂存| H
    P -->|暂存全部成功| S
    P --> T
    P -->|启用后恢复尺寸与耐久| J
    X --> V
    X --> W
    X -->|仅存在且有效的同名文件| G
```

- [WorldBuildingAuthoring.Install](../Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs#L12)
<!-- source: Assets/Editor/World/Buildings/WorldBuildingAuthoring.cs|Install -->
- [WorldBuildingPalette.RefreshCatalog](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L42)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs|RefreshCatalog -->
- [WorldBuildingPalette.Update](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L56)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs|Update -->
- [WorldBuildingPalette.BeginPlacement](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs#L131)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPalette.cs|BeginPlacement -->
- [WorldBuildingManager.CanPlace](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L76)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|CanPlace -->
- [WorldBuildingPlacement.Check](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L15)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs|Check -->
- [WorldBuildingManager.Place](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L96)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|Place -->
- [WorldBuildingManager.Stage](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L247)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|Stage -->
- [BuildingRuntime.Initialize](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L78)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|Initialize -->
- [BuildingRuntime.Activate](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L130)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|Activate -->
- [WorldLabController.TransferEdits](../Assets/Scripts/Features/World/WorldLab/WorldLabController.cs#L183)
<!-- source: Assets/Scripts/Features/World/WorldLab/WorldLabController.cs|TransferEdits -->
- [WorldBuildingManager.Capture](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L265)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|Capture -->
- [BuildingRuntime.Capture](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L189)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|Capture -->
- [WorldLabPersistence.SaveSnapshot](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L15)
<!-- source: Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs|SaveSnapshot -->
- [WorldLabPersistence.LoadSnapshot](../Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs#L68)
<!-- source: Assets/Scripts/Features/World/Persistence/WorldLabPersistence.cs|LoadSnapshot -->
- [WorldBuildingManager.Restore](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L285)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|Restore -->
- [WorldBuildingValidation.ValidateState](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L71)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs|ValidateState -->
- [VoxelWorldEdits.Restore](../Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs#L52)
<!-- source: Assets/Scripts/Features/World/Persistence/VoxelWorldEdits.cs|Restore -->
- [ProceduralVoxelWorld.RestoreEdits](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L423)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|RestoreEdits -->
- [WorldBuildingManager.ClearBuildings](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L382)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|ClearBuildings -->
- [WorldBuildingManager.RestoreBuilding](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L126)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|RestoreBuilding -->
- [SaveManager.GetBuildingBlueprintFiles](../Assets/Scripts/Features/Building/SaveManager.cs#L84)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|GetBuildingBlueprintFiles -->
- [WorldBuildingLibrary.Load](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L51)
<!-- source: Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs|Load -->
- [WorldBuildingManager.ResetBuildings](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L338)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|ResetBuildings -->

## WorldLab 建筑支撑与损坏

源码推导：地形修改通知只标记附近固定建筑，FixedUpdate读取体素数据计算虚拟范围的地形支撑比例（不生成地基实体）；按WorldBuildingManager序列化支撑阈值脱锚，先刷新源碰撞再启用刚体。方块死亡后共用载具的双侧Connector缓存和BFS分组，以虚拟地面连接点判断分量是否保留，局部删除重探附近、配置失效重建、复杂层级实时回退；虚拟底座判定按共享图Revision及底座尺寸缓存，图变化或底座尺寸改变时失效；断裂组完整生成物理残骸；32块/12片为软回收阈值，正常寿命12秒，所有预算与寿命均等待共用最短保留期；Main游玩态先按原阵营释放货仓/运输物并转换掉落，再判定残骸预算。普通卸载/会话清理不转换。爆炸连锁排队结算，同轮命中先完成伤害，按脏建筑各执行一次支撑刷新，避免先脱落禁用导致漏伤；单块直接死亡仍同步完成支撑，残骸不写快照，R重生成恢复初始布局。

```mermaid
flowchart TD
    A["ProceduralVoxelWorld.SetCell()"]
    click A "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L400" "打开源码"
    B["WorldBuildingManager.TerrainChanged()"]
    click B "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L398" "打开源码"
    C["WorldBuildingManager.FixedUpdate()"]
    click C "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L409" "打开源码"
    D["WorldBuildingPlacement.Support()"]
    click D "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L116" "打开源码"
    E["BuildingRuntime.ReleaseAnchor()"]
    click E "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L362" "打开源码"
    F["ProceduralVoxelWorld.FlushDirty()"]
    click F "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543" "打开源码"
    G["Durability.ApplyDamage()"]
    click G "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103" "打开源码"
    H["BuildingRuntime.DestroyBlock()"]
    click H "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238" "打开源码"
    I["BuildingRuntime.SplitDisconnected()"]
    click I "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L280" "打开源码"
    J["Block.CollectNeighbors()"]
    click J "../Assets/Scripts/Features/Simulation/Block.cs#L379" "打开源码"
    K["WorldBuildingManager.CreateDebris()"]
    click K "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L431" "打开源码"
    L["WorldBuildingManager.DamageSphere()"]
    click L "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L426" "打开源码"
    M["WorldBuildingDamage.Apply()"]
    click M "../Assets/Scripts/Features/World/Buildings/Topology/WorldBuildingDamage.cs#L18" "打开源码"
    W["BuildingRuntime.EndDamageBatch()"]
    click W "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L68" "打开源码"
    X["BlockConnectionGraph.Prepare()"]
    click X "../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L19" "打开源码"
    Y["BlockGroupManager.GroupPreparedBlocks()"]
    click Y "../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L27" "打开源码"
    N["ProceduralVoxelWorld.Generate()"]
    click N "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101" "打开源码"
    O["WorldBuildingManager.ResetBuildings()"]
    click O "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L338" "打开源码"
    P["WorldBuildingManager.ClearDebris()"]
    click P "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L469" "打开源码"
    Q["WorldBuildingManager.Place()"]
    click Q "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L96" "打开源码"
    R["BuildingRuntime.HasGroundConnector()"]
    click R "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L341" "打开源码"
    S["WreckSalvage.Convert()"]
    click S "../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10" "打开源码"
    T["CargoHold.ReleaseContents()"]
    click T "../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L206" "打开源码"
    U["Bot.PrepareForHomeDestruction()"]
    click U "../Assets/Scripts/Features/Bots/Bot.cs#L134" "打开源码"
    V["LootDrop.Spawn()"]
    click V "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L83" "打开源码"
    Z["BuildingRuntime.HasPreparedGroundConnector()"]
    click Z "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L322" "打开源码"
    AA["BlockConnectionGraph.PrepareBlocks()"]
    click AA "../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L109" "打开源码"
    AB["BlockConnections.CollectRuntimeNeighbors()"]
    click AB "../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L420" "打开源码"
    AC["BuildingRuntime.RefreshStructure()"]
    click AC "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L55" "打开源码"
    AD["BatchConnectionBuilder.RebuildBatchImmediate()"]
    click AD "../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L40" "打开源码"
    AE["RigidbodyMotion.Restore()"]
    click AE "../Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs#L17" "打开源码"
    AF["GroupPhysicsSettings.ApplyTo()"]
    click AF "../Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs#L12" "打开源码"
    AG["WreckSalvage.ReleaseContents()"]
    click AG "../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L53" "打开源码"
    A -.->|TerrainChanged事件| B
    B -.->|待检查集合| C
    C --> D
    C -->|支撑低于阈值| E
    E --> F
    G -->|建筑耐久为0| H
    H -->|标记脏结构| W
    W -->|最外层批次结束| AC
    AC --> I
    I -->|虚拟地面连接点| Z
    Z -->|缓存缺失| R
    I --> X
    X --> AA
    AA -->|几何失效或成员改变| AD
    AD -->|每块准备几何邻接| J
    I -->|与载具共用BFS| Y
    Y -->|经Traverse和Block| AB
    I -->|断裂组| K
    K --> AE
    K --> AF
    H -->|爆炸方块| L
    L --> M
    M -->|顺序结算，连锁入队| G
    K -->|禁用/重挂接及预算判断前| S
    S --> AG
    H --> AG
    AG --> U
    AG --> T
    S -->|敌方残骸概率| V
    T -->|建筑既有存量或载具阵营配置| V
    N -.->|Regenerated事件| O
    O --> P
    O -->|有效初始布局| Q
```

- [ProceduralVoxelWorld.SetCell](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L400)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|SetCell -->
- [WorldBuildingManager.TerrainChanged](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L398)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|TerrainChanged -->
- [WorldBuildingManager.FixedUpdate](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L409)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|FixedUpdate -->
- [WorldBuildingPlacement.Support](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs#L116)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingPlacement.cs|Support -->
- [BuildingRuntime.ReleaseAnchor](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L362)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|ReleaseAnchor -->
- [ProceduralVoxelWorld.FlushDirty](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|FlushDirty -->
- [Durability.ApplyDamage](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|ApplyDamage -->
- [BuildingRuntime.DestroyBlock](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|DestroyBlock -->
- [BuildingRuntime.SplitDisconnected](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L280)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|SplitDisconnected -->
- [Block.CollectNeighbors](../Assets/Scripts/Features/Simulation/Block.cs#L379)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|CollectNeighbors -->
- [WorldBuildingManager.CreateDebris](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L431)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|CreateDebris -->
- [WorldBuildingManager.DamageSphere](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L426)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|DamageSphere -->
- [WorldBuildingDamage.Apply](../Assets/Scripts/Features/World/Buildings/Topology/WorldBuildingDamage.cs#L18)
<!-- source: Assets/Scripts/Features/World/Buildings/Topology/WorldBuildingDamage.cs|Apply -->
- [BuildingRuntime.EndDamageBatch](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L68)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|EndDamageBatch -->
- [BlockConnectionGraph.Prepare](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L19)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs|Prepare -->
- [BlockGroupManager.GroupPreparedBlocks](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L27)
<!-- source: Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs|GroupPreparedBlocks -->
- [ProceduralVoxelWorld.Generate](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|Generate -->
- [WorldBuildingManager.ResetBuildings](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L338)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|ResetBuildings -->
- [WorldBuildingManager.ClearDebris](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L469)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|ClearDebris -->
- [WorldBuildingManager.Place](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L96)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|Place -->
- [BuildingRuntime.HasGroundConnector](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L341)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|HasGroundConnector -->
- [WreckSalvage.Convert](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10)
<!-- source: Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs|Convert -->
- [CargoHold.ReleaseContents](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L206)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs|ReleaseContents -->
- [Bot.PrepareForHomeDestruction](../Assets/Scripts/Features/Bots/Bot.cs#L134)
<!-- source: Assets/Scripts/Features/Bots/Bot.cs|PrepareForHomeDestruction -->
- [LootDrop.Spawn](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L83)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Spawn -->
- [BuildingRuntime.HasPreparedGroundConnector](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L322)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|HasPreparedGroundConnector -->
- [BlockConnectionGraph.PrepareBlocks](../Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs#L109)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnectionGraph.cs|PrepareBlocks -->
- [BlockConnections.CollectRuntimeNeighbors](../Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs#L420)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BlockConnections.cs|CollectRuntimeNeighbors -->
- [BuildingRuntime.RefreshStructure](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L55)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|RefreshStructure -->
- [BatchConnectionBuilder.RebuildBatchImmediate](../Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs#L40)
<!-- source: Assets/Scripts/Features/Simulation/Connections/BatchConnectionBuilder.cs|RebuildBatchImmediate -->
- [RigidbodyMotion.Restore](../Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs#L17)
<!-- source: Assets/Scripts/Features/Simulation/Physics/RigidbodyMotion.cs|Restore -->
- [GroupPhysicsSettings.ApplyTo](../Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs#L12)
<!-- source: Assets/Scripts/Features/Simulation/Groups/GroupPhysicsSettings.cs|ApplyTo -->
- [WreckSalvage.ReleaseContents](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L53)
<!-- source: Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs|ReleaseContents -->

## WorldLab 金属支撑与碎片

源码推导：成功破坏后请求六邻接支撑检查，等待脏块刷新。土/岩邻接或世界底层视为支撑；仅32格以内金属孤岛变为刚体，最多12个、12秒寿命。大结构保留静态。分离后立即刷新源碰撞；重生成/恢复清除碎片。不是全地形结构力学。

```mermaid
flowchart TD
    A["ProceduralVoxelWorld.DamageSphere()"]
    click A "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L451" "打开源码"
    B["VoxelStructureCollapse.RequestCheck()"]
    click B "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L33" "打开源码"
    C["VoxelStructureCollapse.LateUpdate()"]
    click C "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L38" "打开源码"
    D["VoxelStructureCollapse.ResolveDetached()"]
    click D "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L51" "打开源码"
    E["VoxelStructureSupport.FindDetached()"]
    click E "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureSupport.cs#L17" "打开源码"
    F["VoxelDebrisPiece.Initialize()"]
    click F "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L11" "打开源码"
    G["ProceduralVoxelWorld.SetCell()"]
    click G "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L400" "打开源码"
    H["ProceduralVoxelWorld.FlushDirty()"]
    click H "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543" "打开源码"
    I["VoxelDebrisPiece.Update()"]
    click I "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L49" "打开源码"
    J["VoxelDebrisPiece.OnDestroy()"]
    click J "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L57" "打开源码"
    K["ProceduralVoxelWorld.Generate()"]
    click K "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101" "打开源码"
    L["VoxelStructureCollapse.ClearDebris()"]
    click L "../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L88" "打开源码"
    M["ProceduralVoxelWorld.RestoreEdits()"]
    click M "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L423" "打开源码"
    A -->|移除单元后| B
    B -.->|请求标记| C
    C -->|队列空且到间隔| D
    D --> E
    D -->|每片| F
    D -->|清除源单元| G
    D --> H
    I -.->|寿命到销毁| J
    K --> L
    M -->|校验成功后| L
```

- [ProceduralVoxelWorld.DamageSphere](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L451)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|DamageSphere -->
- [VoxelStructureCollapse.RequestCheck](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L33)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs|RequestCheck -->
- [VoxelStructureCollapse.LateUpdate](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L38)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs|LateUpdate -->
- [VoxelStructureCollapse.ResolveDetached](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L51)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs|ResolveDetached -->
- [VoxelStructureSupport.FindDetached](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureSupport.cs#L17)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureSupport.cs|FindDetached -->
- [VoxelDebrisPiece.Initialize](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L11)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs|Initialize -->
- [ProceduralVoxelWorld.SetCell](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L400)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|SetCell -->
- [ProceduralVoxelWorld.FlushDirty](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L543)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|FlushDirty -->
- [VoxelDebrisPiece.Update](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L49)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs|Update -->
- [VoxelDebrisPiece.OnDestroy](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs#L57)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelDebrisPiece.cs|OnDestroy -->
- [ProceduralVoxelWorld.Generate](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|Generate -->
- [VoxelStructureCollapse.ClearDebris](../Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs#L88)
<!-- source: Assets/Scripts/Features/World/Terrain/Destruction/VoxelStructureCollapse.cs|ClearDebris -->
- [ProceduralVoxelWorld.RestoreEdits](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L423)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|RestoreEdits -->

## 据点编辑与有限驻军会话

源码推导：建筑可选outpost数据，嵌入敌机快照与本地部署点；不写永久击败状态。每次探索拥有独立Director，区块卸载不清除击败集合，远处活敌保留模块/耐久/货物休眠；显式驾驶舱死亡才标记击败。退出探索清空整次会话。

```mermaid
flowchart TD
    A["BuildingOutpostEditor.SetOutpost()"]
    click A "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L163" "打开源码"
    B["BuildingOutpostEditor.AddDefender()"]
    click B "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L180" "打开源码"
    C["BuildingOutpostEditor.SavePose()"]
    click C "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L233" "打开源码"
    D["SaveManager.SaveBuildingOutpost()"]
    click D "../Assets/Scripts/Features/Building/SaveManager.cs#L246" "打开源码"
    E["EnemyOutpostData.Validate()"]
    click E "../Assets/Scripts/Features/World/Outposts/EnemyOutpostData.cs#L13" "打开源码"
    F["WorldBuildingLibrary.ReadSource()"]
    click F "../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L82" "打开源码"
    G["WorldBuildingStreaming.LoadColumn()"]
    click G "../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L34" "打开源码"
    H["BuildingRuntime.InitializeRoutine()"]
    click H "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L86" "打开源码"
    I["WorldSessionManager.Update()"]
    click I "../Assets/Scripts/Features/World/WorldSessionManager.cs#L443" "打开源码"
    J["OutpostEncounterDirector.Tick()"]
    click J "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51" "打开源码"
    K["OutpostEncounterDirector.Spawn()"]
    click K "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L170" "打开源码"
    L["EnemyBlueprintFactory.Spawn()"]
    click L "../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12" "打开源码"
    M["OutpostEncounterDirector.Hibernate()"]
    click M "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L291" "打开源码"
    N["DestroyManager.DestroyGameObject()"]
    click N "../Assets/Scripts/Features/Combat/DestroyManager.cs#L28" "打开源码"
    O["EnemyIdentity.ReportDefeated()"]
    click O "../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L9" "打开源码"
    P["OutpostEncounterDirector.Clear()"]
    click P "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L337" "打开源码"
    Q["WorldSessionManager.BeginPreparation()"]
    click Q "../Assets/Scripts/Features/World/WorldSessionManager.cs#L99" "打开源码"
    R["OutpostHud.Update()"]
    click R "../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L19" "打开源码"
    S["OutpostHud.Format()"]
    click S "../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L31" "打开源码"
    T["OutpostHud.FitName()"]
    click T "../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L46" "打开源码"
    U["WorldBuildingValidation.ValidateWorldTemplate()"]
    click U "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L109" "打开源码"
    A --> B
    B --> D
    C --> D
    D --> E
    F --> E
    G -.->|分帧建筑恢复| H
    I --> J
    J -->|未击败且近距离| K
    K --> L
    J -->|远离或地形卸载| M
    N -->|敌方驾驶舱死亡| O
    O -.->|Defeated事件修改该驻军记录| J
    Q --> P
    R -.->|只读状态| J
    R --> S
    S -->|按字形宽度缩短名称| T
    F -->|世界生成范围约束| U
```

- [BuildingOutpostEditor.SetOutpost](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L163)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|SetOutpost -->
- [BuildingOutpostEditor.AddDefender](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L180)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|AddDefender -->
- [BuildingOutpostEditor.SavePose](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L233)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|SavePose -->
- [SaveManager.SaveBuildingOutpost](../Assets/Scripts/Features/Building/SaveManager.cs#L246)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|SaveBuildingOutpost -->
- [EnemyOutpostData.Validate](../Assets/Scripts/Features/World/Outposts/EnemyOutpostData.cs#L13)
<!-- source: Assets/Scripts/Features/World/Outposts/EnemyOutpostData.cs|Validate -->
- [WorldBuildingLibrary.ReadSource](../Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs#L82)
<!-- source: Assets/Scripts/Features/World/Buildings/Data/WorldBuildingLibrary.cs|ReadSource -->
- [WorldBuildingStreaming.LoadColumn](../Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs#L34)
<!-- source: Assets/Scripts/Features/World/Streaming/WorldBuildingStreaming.cs|LoadColumn -->
- [BuildingRuntime.InitializeRoutine](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L86)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|InitializeRoutine -->
- [WorldSessionManager.Update](../Assets/Scripts/Features/World/WorldSessionManager.cs#L443)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|Update -->
- [OutpostEncounterDirector.Tick](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Tick -->
- [OutpostEncounterDirector.Spawn](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L170)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Spawn -->
- [EnemyBlueprintFactory.Spawn](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs|Spawn -->
- [OutpostEncounterDirector.Hibernate](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L291)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Hibernate -->
- [DestroyManager.DestroyGameObject](../Assets/Scripts/Features/Combat/DestroyManager.cs#L28)
<!-- source: Assets/Scripts/Features/Combat/DestroyManager.cs|DestroyGameObject -->
- [EnemyIdentity.ReportDefeated](../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L9)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs|ReportDefeated -->
- [OutpostEncounterDirector.Clear](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L337)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Clear -->
- [WorldSessionManager.BeginPreparation](../Assets/Scripts/Features/World/WorldSessionManager.cs#L99)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|BeginPreparation -->
- [OutpostHud.Update](../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L19)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostHud.cs|Update -->
- [OutpostHud.Format](../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L31)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostHud.cs|Format -->
- [OutpostHud.FitName](../Assets/Scripts/Features/World/Outposts/OutpostHud.cs#L46)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostHud.cs|FitName -->
- [WorldBuildingValidation.ValidateWorldTemplate](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L109)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs|ValidateWorldTemplate -->

## 据点驻军部署范围预览

源码推导：保存位置或切换驻军后，按建筑制作世界坐标同时显示底座和完整敌机模型；忽略父Prefab偏移。模块级AABB检查净空，合计范围只用作选择框。预览复制Mesh/Material引用，没有战斗/物理组件；退出面板释放显示对象和临时线框。

```mermaid
flowchart TD
    A["BuildingOutpostEditor.SelectDefender()"]
    click A "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L210" "打开源码"
    B["BuildingOutpostEditor.ShowPreview()"]
    click B "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L290" "打开源码"
    C["OutpostDeploymentPreview.CalculateBounds()"]
    click C "../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L161" "打开源码"
    D["OutpostDeploymentPreview.DrawGuard()"]
    click D "../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L91" "打开源码"
    E["BuildingOutpostEditor.SavePose()"]
    click E "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L233" "打开源码"
    F["BuildingOutpostEditor.LateUpdate()"]
    click F "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L314" "打开源码"
    G["OutpostDeploymentPreview.FaceLabels()"]
    click G "../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L133" "打开源码"
    H["BuildingOutpostEditor.OnDisable()"]
    click H "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L309" "打开源码"
    I["BuildingOutpostEditor.ClearPreview()"]
    click I "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L300" "打开源码"
    J["OutpostDeploymentPreview.Dispose()"]
    click J "../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L193" "打开源码"
    K["BlueprintModelPreview.Create()"]
    click K "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L8" "打开源码"
    L["OutpostDeploymentPreview.OverlapsBuilding()"]
    click L "../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L141" "打开源码"
    A --> B
    E -->|经Save与SelectDefender| B
    B --> I
    B -->|经预览构造器| C
    B -->|经预览构造器| D
    F --> G
    H --> I
    I --> J
    B -->|经预览构造器| K
    B -->|经预览构造器| L
```

- [BuildingOutpostEditor.SelectDefender](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L210)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|SelectDefender -->
- [BuildingOutpostEditor.ShowPreview](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L290)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|ShowPreview -->
- [OutpostDeploymentPreview.CalculateBounds](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L161)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs|CalculateBounds -->
- [OutpostDeploymentPreview.DrawGuard](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L91)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs|DrawGuard -->
- [BuildingOutpostEditor.SavePose](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L233)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|SavePose -->
- [BuildingOutpostEditor.LateUpdate](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L314)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|LateUpdate -->
- [OutpostDeploymentPreview.FaceLabels](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L133)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs|FaceLabels -->
- [BuildingOutpostEditor.OnDisable](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L309)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|OnDisable -->
- [BuildingOutpostEditor.ClearPreview](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L300)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|ClearPreview -->
- [OutpostDeploymentPreview.Dispose](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L193)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs|Dispose -->
- [BlueprintModelPreview.Create](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L8)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs|Create -->
- [OutpostDeploymentPreview.OverlapsBuilding](../Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs#L141)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostDeploymentPreview.cs|OverlapsBuilding -->

## 匹配底座与世界停机显示

源码推导：用户选择敌机后生成轮廓平台与服务设施，独立保存组合JSON；已有用户文件不覆盖。世界据点未激活时显示同姿态的完整模型，接近后替换为真实敌机；卸载、击败与会话退出清理显示。

```mermaid
flowchart TD
    A["BuildingOutpostEditor.CreateMatchingBase()"]
    click A "../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L96" "打开源码"
    B["OutpostBaseDesigner.Create()"]
    click B "../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L14" "打开源码"
    C["OutpostBaseDesigner.BoundsOf()"]
    click C "../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L9" "打开源码"
    D["SaveManager.SaveNewBuilding()"]
    click D "../Assets/Scripts/Features/Building/SaveManager.cs#L288" "打开源码"
    E["BuildManager.EnterBuildingBlueprintBuildMode()"]
    click E "../Assets/Scripts/Features/Building/BuildManager.cs#L302" "打开源码"
    F["OutpostContentAuthoring.ExportMatchingBases()"]
    click F "../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L60" "打开源码"
    G["OutpostEncounterDirector.Tick()"]
    click G "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51" "打开源码"
    H["OutpostEncounterDirector.UpdateParkedModel()"]
    click H "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L248" "打开源码"
    I["BlueprintModelPreview.Create()"]
    click I "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L8" "打开源码"
    J["OutpostEncounterDirector.Spawn()"]
    click J "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L170" "打开源码"
    K["OutpostEncounterDirector.ClearParkedModel()"]
    click K "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L269" "打开源码"
    L["OutpostEncounterDirector.Clear()"]
    click L "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L337" "打开源码"
    A --> B
    B --> C
    A --> E
    H -->|经Height按所选蓝图取范围| D
    F --> B
    G --> H
    H --> I
    G -->|有底座且近距离| J
    H -->|活动或击败或卸载| K
    L --> K
```

- [BuildingOutpostEditor.CreateMatchingBase](../Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs#L96)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/BuildingOutpostEditor.cs|CreateMatchingBase -->
- [OutpostBaseDesigner.Create](../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L14)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs|Create -->
- [OutpostBaseDesigner.BoundsOf](../Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs#L9)
<!-- source: Assets/Scripts/Features/Building/BuildingEditor/OutpostBaseDesigner.cs|BoundsOf -->
- [SaveManager.SaveNewBuilding](../Assets/Scripts/Features/Building/SaveManager.cs#L288)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|SaveNewBuilding -->
- [BuildManager.EnterBuildingBlueprintBuildMode](../Assets/Scripts/Features/Building/BuildManager.cs#L302)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|EnterBuildingBlueprintBuildMode -->
- [OutpostContentAuthoring.ExportMatchingBases](../Assets/Editor/World/Outposts/OutpostContentAuthoring.cs#L60)
<!-- source: Assets/Editor/World/Outposts/OutpostContentAuthoring.cs|ExportMatchingBases -->
- [OutpostEncounterDirector.Tick](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Tick -->
- [OutpostEncounterDirector.UpdateParkedModel](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L248)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|UpdateParkedModel -->
- [BlueprintModelPreview.Create](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L8)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs|Create -->
- [OutpostEncounterDirector.Spawn](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L170)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Spawn -->
- [OutpostEncounterDirector.ClearParkedModel](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L269)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|ClearParkedModel -->
- [OutpostEncounterDirector.Clear](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L337)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Clear -->

## 敌方蓝图完整恢复

源码推导：普通建造游玩由PlayManager进入EnemySpawner的按间隔调度；WorldSession存在时由模式门控抑制全局刷怪，探索据点调用EnemyBlueprintFactory生成。生成前检查每个模块资源和恰好一个驾驶舱，缺失或空模块整份拒绝。模块Awake应用CSV后恢复实例尺寸并刷新真实碰撞/连接点/质量，再累加刚体质量、设置敌方身份和运行归属；不以跳过模块生成残缺敌机。

```mermaid
flowchart TD
    A["PlayManager.PlayStart()"]
    click A "../Assets/Scripts/Features/Simulation/PlayManager.cs#L109" "打开源码"
    B["EnemySpawner.BeginPlayMode()"]
    click B "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L41" "打开源码"
    C["EnemySpawner.Update()"]
    click C "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L26" "打开源码"
    D["EnemySpawner.SpawnRandomEnemy()"]
    click D "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L110" "打开源码"
    E["WorldSessionManager.Update()"]
    click E "../Assets/Scripts/Features/World/WorldSessionManager.cs#L443" "打开源码"
    F["OutpostEncounterDirector.Tick()"]
    click F "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51" "打开源码"
    G["OutpostEncounterDirector.Spawn()"]
    click G "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L170" "打开源码"
    H["ModularUnitFactory.IsValidBlueprint()"]
    click H "../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L8" "打开源码"
    I["EnemyBlueprintFactory.Spawn()"]
    click I "../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12" "打开源码"
    J["BlueprintBlockFactory.TryRestore()"]
    click J "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7" "打开源码"
    K["Block.RefreshRuntimeData()"]
    click K "../Assets/Scripts/Features/Simulation/Block.cs#L145" "打开源码"
    L["ModularUnitFactory.Spawn()"]
    click L "../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L41" "打开源码"
    M["ControlUnit.RefreshChildren()"]
    click M "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185" "打开源码"
    N["ControlUnit.AssignRuntimeOwnershipToBlocks()"]
    click N "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L244" "打开源码"
    O["EnemyIdentity.SetSpawnMaxHealth()"]
    click O "../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L31" "打开源码"
    P["OutpostEncounterDirector.Hibernate()"]
    click P "../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L291" "打开源码"
    Q["PlayManager.PlayEnd()"]
    click Q "../Assets/Scripts/Features/Simulation/PlayManager.cs#L295" "打开源码"
    R["EnemySpawner.EndPlayMode()"]
    click R "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L57" "打开源码"
    S["EnemySpawner.SpawnBlockData()"]
    click S "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144" "打开源码"
    A -->|普通模式| B
    B -.->|引擎Update| C
    C -->|间隔到期| D
    D --> S
    S --> I
    E --> F
    F -->|未击败、近距离且预算允许| G
    F -->|远离或地形卸载| P
    G --> I
    I --> L
    L -->|生成任何实体前| H
    L --> J
    J -->|恢复尺寸后| K
    L -->|完整构造后| O
    L --> M
    L --> N
    Q --> R
```

- [PlayManager.PlayStart](../Assets/Scripts/Features/Simulation/PlayManager.cs#L109)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayStart -->
- [EnemySpawner.BeginPlayMode](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L41)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|BeginPlayMode -->
- [EnemySpawner.Update](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L26)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|Update -->
- [EnemySpawner.SpawnRandomEnemy](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L110)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|SpawnRandomEnemy -->
- [WorldSessionManager.Update](../Assets/Scripts/Features/World/WorldSessionManager.cs#L443)
<!-- source: Assets/Scripts/Features/World/WorldSessionManager.cs|Update -->
- [OutpostEncounterDirector.Tick](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L51)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Tick -->
- [OutpostEncounterDirector.Spawn](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L170)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Spawn -->
- [ModularUnitFactory.IsValidBlueprint](../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L8)
<!-- source: Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs|IsValidBlueprint -->
- [EnemyBlueprintFactory.Spawn](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs|Spawn -->
- [BlueprintBlockFactory.TryRestore](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs#L7)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintBlockFactory.cs|TryRestore -->
- [Block.RefreshRuntimeData](../Assets/Scripts/Features/Simulation/Block.cs#L145)
<!-- source: Assets/Scripts/Features/Simulation/Block.cs|RefreshRuntimeData -->
- [ModularUnitFactory.Spawn](../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L41)
<!-- source: Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs|Spawn -->
- [ControlUnit.RefreshChildren](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|RefreshChildren -->
- [ControlUnit.AssignRuntimeOwnershipToBlocks](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L244)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|AssignRuntimeOwnershipToBlocks -->
- [EnemyIdentity.SetSpawnMaxHealth](../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L31)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs|SetSpawnMaxHealth -->
- [OutpostEncounterDirector.Hibernate](../Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs#L291)
<!-- source: Assets/Scripts/Features/World/Outposts/OutpostEncounterDirector.cs|Hibernate -->
- [PlayManager.PlayEnd](../Assets/Scripts/Features/Simulation/PlayManager.cs#L295)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|PlayEnd -->
- [EnemySpawner.EndPlayMode](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L57)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|EndPlayMode -->
- [EnemySpawner.SpawnBlockData](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|SpawnBlockData -->

## 敌机出生传送门

源码推导：EnemySpawner的随机/批量入口共用SpawnBlockData，SiegeWaveDirector在注册成功后调用同一表现入口；失败生成、未到生成时机或关闭enableRuntimeVfx时不播放。传送门固定在舰体出生包围盒后侧，局部XY面朝舰体前方，尺寸按横截面投影自适应。具名集中库模板共用GPU池，播放后按Prefab时限归还；不会延迟生成，不改据点工厂的其他消费者。

```mermaid
flowchart TD
    A["EnemySpawner.SpawnBlockData()"]
    click A "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144" "打开源码"
    B["SiegeWaveDirector.Tick()"]
    click B "../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46" "打开源码"
    C["EnemyBlueprintFactory.Spawn()"]
    click C "../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12" "打开源码"
    D["SiegeMatch.RegisterAttacker()"]
    click D "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L130" "打开源码"
    E["VisualEffectsManager.TryPlayEnemySpawnPortal()"]
    click E "../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L225" "打开源码"
    F["VisualEffectsManager.PlayEnemySpawnPortal()"]
    click F "../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L235" "打开源码"
    G["BlockVfxLibrary.Play()"]
    click G "../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112" "打开源码"
    H["BlockVfxLibrary.PlayTemplate()"]
    click H "../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L135" "打开源码"
    I["VfxEffectPool.Rent()"]
    click I "../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L25" "打开源码"
    J["VfxEffect.PlayOnce()"]
    click J "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L182" "打开源码"
    K["VfxEffect.ReleaseAfterPlayback()"]
    click K "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252" "打开源码"
    L["VfxEffect.FinishPlayback()"]
    click L "../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L275" "打开源码"
    M["VfxEffectPool.Return()"]
    click M "../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L72" "打开源码"
    N["EnemySpawnPortalBaker.BakeMissing()"]
    click N "../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L17" "打开源码"
    A --> C
    A -->|返回后，空引用跳过| E
    B --> C
    B --> D
    B -->|注册成功| E
    E --> F
    F -->|VFX开启| G
    G --> H
    H -->|模板存在| I
    H --> J
    H --> K
    K -.->|Invoke到期| L
    L -.->|略早触发时按剩余时间补约| L
    L -->|到期| M
    N -.->|保存Graph、Prefab和库配置| G
```

- [EnemySpawner.SpawnBlockData](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|SpawnBlockData -->
- [SiegeWaveDirector.Tick](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs|Tick -->
- [EnemyBlueprintFactory.Spawn](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs|Spawn -->
- [SiegeMatch.RegisterAttacker](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L130)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|RegisterAttacker -->
- [VisualEffectsManager.TryPlayEnemySpawnPortal](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L225)
<!-- source: Assets/Scripts/Features/Presentation/VisualEffectsManager.cs|TryPlayEnemySpawnPortal -->
- [VisualEffectsManager.PlayEnemySpawnPortal](../Assets/Scripts/Features/Presentation/VisualEffectsManager.cs#L235)
<!-- source: Assets/Scripts/Features/Presentation/VisualEffectsManager.cs|PlayEnemySpawnPortal -->
- [BlockVfxLibrary.Play](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L112)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs|Play -->
- [BlockVfxLibrary.PlayTemplate](../Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs#L135)
<!-- source: Assets/Scripts/Features/Presentation/Effects/BlockVfxLibrary.cs|PlayTemplate -->
- [VfxEffectPool.Rent](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L25)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs|Rent -->
- [VfxEffect.PlayOnce](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L182)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|PlayOnce -->
- [VfxEffect.ReleaseAfterPlayback](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L252)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|ReleaseAfterPlayback -->
- [VfxEffect.FinishPlayback](../Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs#L275)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffect.cs|FinishPlayback -->
- [VfxEffectPool.Return](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L72)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs|Return -->
- [EnemySpawnPortalBaker.BakeMissing](../Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs#L17)
<!-- source: Assets/Editor/Presentation/Effects/EnemySpawnPortalBaker.cs|BakeMissing -->

## 显式 Player 世界验证

源码推导：仅-world-smoke-report显式启用；可指定固定种子和1–10次会话。临时单驾驶舱载具沿200m脚本路线跨区块，通过正式退出链清空会话并等待世界与原载具恢复后重复；一般循环不经过返航货仓保存入口。可选-world-smoke-enemies只在临时运行对象中验证敌机尺寸/质量和非法模块拒绝，不保存蓝图。可选-world-smoke-combat注入Input System键盘、验证真实推进后固定战斗平台，等待炮弹击毁和机器人回收入仓；不包含人工驾驶或返航文件保存。可选-world-smoke-enemy-flight保留指定既有敌机的动态刚体、AI和全部参数，等待自然追踪/悬浮/武器伤害；玩家仍为固定烟测目标。可选-world-smoke-return在一般循环结束后隔离缓存目录文件仓，真实UI返航写盘/重载、驾驶舱销毁/重生清货与再次出发；最后只读恢复原载具。仅测试进程内改变配置，阶段报告包含P99、尖峰及每轮内存采样，不代表真实驾驶或长期战斗。

```mermaid
flowchart TD
    A["WorldPlayerSmokeChecks.Initialize()"]
    click A "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L69" "打开源码"
    B["WorldPlayerSmokeChecks.ReadInteger()"]
    click B "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L136" "打开源码"
    C["WorldPlayerSmokeChecks.Start()"]
    click C "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L146" "打开源码"
    D["WorldPlayerSmokeChecks.LateUpdate()"]
    click D "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L164" "打开源码"
    E["WorldPlayerSmokeChecks.Scenario()"]
    click E "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L193" "打开源码"
    F["MainUIPanels.PlayStart()"]
    click F "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233" "打开源码"
    G["MainUIPanels.CompletePlayEnd()"]
    click G "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304" "打开源码"
    H["WorldPlayerSmokeChecks.Next()"]
    click H "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L576" "打开源码"
    I["WorldPlayerSmokeChecks.Save()"]
    click I "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L597" "打开源码"
    J["WorldPlayerSmokeChecks.Finish()"]
    click J "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L602" "打开源码"
    K["WorldPlayerSmokeChecks.CaptureError()"]
    click K "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L589" "打开源码"
    L["WorldPlayerSmokeChecks.ValidateEnemyBlueprints()"]
    click L "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L503" "打开源码"
    M["WorldPlayerSmokeChecks.EnemyFixture()"]
    click M "../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L467" "打开源码"
    N["EnemySpawner.SpawnBlockData()"]
    click N "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144" "打开源码"
    O["WorldCombatPlayerChecks.Run()"]
    click O "../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L14" "打开源码"
    P["EnemyFlightPlayerChecks.Run()"]
    click P "../Assets/Scripts/Features/World/Validation/EnemyFlightPlayerChecks.cs#L12" "打开源码"
    Q["WorldReturnPlayerChecks.Run()"]
    click Q "../Assets/Scripts/Features/World/Validation/WorldReturnPlayerChecks.cs#L13" "打开源码"
    R["OutpostPlayerChecks.Run()"]
    click R "../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L12" "打开源码"
    S["OutpostEditorPlayerChecks.Run()"]
    click S "../Assets/Scripts/Features/World/Validation/OutpostEditorPlayerChecks.cs#L17" "打开源码"
    T["OutpostPursuitPlayerChecks.Run()"]
    click T "../Assets/Scripts/Features/World/Validation/OutpostPursuitPlayerChecks.cs#L10" "打开源码"
    A -->|显式参数校验| B
    A -.->|创建组件后引擎Start| C
    C -.->|后续引擎Update| D
    D -->|推进枚举器| E
    E --> F
    E -->|显式-world-smoke-enemies| L
    L --> M
    L -->|实例尺寸与拒绝非法模块| N
    E -->|显式-world-smoke-combat| O
    E -->|显式-world-smoke-enemy-flight指定既有蓝图| P
    E -->|循环完成后显式-world-smoke-return| Q
    E -->|显式据点参数| R
    E -->|仅Development制作验证参数| S
    E -->|显式据点移动目标参数| T
    E -->|跨区块后正式退出| G
    E -->|阶段结束| H
    H --> I
    E -.->|等待Ready与载具恢复后重复| D
    K -.->|记录错误| J
    D -->|完成或超时异常| J
    J --> H
    J -->|最终状态及正常退出| I
```

- [WorldPlayerSmokeChecks.Initialize](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L69)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|Initialize -->
- [WorldPlayerSmokeChecks.ReadInteger](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L136)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|ReadInteger -->
- [WorldPlayerSmokeChecks.Start](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L146)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|Start -->
- [WorldPlayerSmokeChecks.LateUpdate](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L164)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|LateUpdate -->
- [WorldPlayerSmokeChecks.Scenario](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L193)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|Scenario -->
- [MainUIPanels.PlayStart](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayStart -->
- [MainUIPanels.CompletePlayEnd](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L304)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|CompletePlayEnd -->
- [WorldPlayerSmokeChecks.Next](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L576)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|Next -->
- [WorldPlayerSmokeChecks.Save](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L597)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|Save -->
- [WorldPlayerSmokeChecks.Finish](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L602)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|Finish -->
- [WorldPlayerSmokeChecks.CaptureError](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L589)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|CaptureError -->
- [WorldPlayerSmokeChecks.ValidateEnemyBlueprints](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L503)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|ValidateEnemyBlueprints -->
- [WorldPlayerSmokeChecks.EnemyFixture](../Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs#L467)
<!-- source: Assets/Scripts/Features/World/Validation/WorldPlayerSmokeChecks.cs|EnemyFixture -->
- [EnemySpawner.SpawnBlockData](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|SpawnBlockData -->
- [WorldCombatPlayerChecks.Run](../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L14)
<!-- source: Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs|Run -->
- [EnemyFlightPlayerChecks.Run](../Assets/Scripts/Features/World/Validation/EnemyFlightPlayerChecks.cs#L12)
<!-- source: Assets/Scripts/Features/World/Validation/EnemyFlightPlayerChecks.cs|Run -->
- [WorldReturnPlayerChecks.Run](../Assets/Scripts/Features/World/Validation/WorldReturnPlayerChecks.cs#L13)
<!-- source: Assets/Scripts/Features/World/Validation/WorldReturnPlayerChecks.cs|Run -->
- [OutpostPlayerChecks.Run](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L12)
<!-- source: Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs|Run -->
- [OutpostEditorPlayerChecks.Run](../Assets/Scripts/Features/World/Validation/OutpostEditorPlayerChecks.cs#L17)
<!-- source: Assets/Scripts/Features/World/Validation/OutpostEditorPlayerChecks.cs|Run -->
- [OutpostPursuitPlayerChecks.Run](../Assets/Scripts/Features/World/Validation/OutpostPursuitPlayerChecks.cs#L10)
<!-- source: Assets/Scripts/Features/World/Validation/OutpostPursuitPlayerChecks.cs|Run -->

## Player 输入交战回收入仓验证

源码推导：显式测试专用临时模块与内存蓝图；W键设备事件经过生产ControlUnit.Update与推进器/电网/PhysX后产生位移。随后固定平台，炮塔自然索敌、发射、击毁敌舱，等待生产残骸转换与CollectionBot真实拾取返航入仓；不手动提供功率，不保存玩家蓝图。敌方AI关闭、固定平台不代表完整驾驶交战巡航。

```mermaid
flowchart TD
    A["WorldCombatPlayerChecks.Run()"]
    click A "../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L14" "打开源码"
    B["WorldCombatPlayerChecks.Spawn()"]
    click B "../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L135" "打开源码"
    C["ControlUnit.Update()"]
    click C "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L138" "打开源码"
    D["MainThruster.FixedUpdate()"]
    click D "../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L10" "打开源码"
    E["PowerTransmissionDevice.Update()"]
    click E "../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L61" "打开源码"
    F["WorldCombatPlayerChecks.EnemyCargoFixture()"]
    click F "../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L144" "打开源码"
    G["EnemySpawner.SpawnBlockData()"]
    click G "../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144" "打开源码"
    H["Turret.FixedUpdate()"]
    click H "../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71" "打开源码"
    I["ProjectileTurret.Fire()"]
    click I "../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L164" "打开源码"
    J["TurretHitResolver.Apply()"]
    click J "../Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs#L7" "打开源码"
    K["Durability.ApplyDamage()"]
    click K "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103" "打开源码"
    L["DestroyManager.DestroyGameObject()"]
    click L "../Assets/Scripts/Features/Combat/DestroyManager.cs#L28" "打开源码"
    M["WreckSalvage.Convert()"]
    click M "../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10" "打开源码"
    N["CollectionBot.FixedUpdate()"]
    click N "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31" "打开源码"
    O["CollectionBot.ReturnCargo()"]
    click O "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L88" "打开源码"
    P["CargoHold.Store()"]
    click P "../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L128" "打开源码"
    A --> B
    A -.->|键盘事件由引擎消费| C
    C -.->|输入意图| D
    E -.->|每帧真实分配供电| D
    A --> F
    A --> G
    A -.->|自然物理帧| H
    H --> I
    I -.->|实体炮弹跨帧命中| J
    J --> K
    K --> L
    L -.->|清理会话延迟转换| M
    M -.->|释放货物后机器人扫描| N
    N --> O
    O -.->|Deliver入库| P
```

- [WorldCombatPlayerChecks.Run](../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L14)
<!-- source: Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs|Run -->
- [WorldCombatPlayerChecks.Spawn](../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L135)
<!-- source: Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs|Spawn -->
- [ControlUnit.Update](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L138)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|Update -->
- [MainThruster.FixedUpdate](../Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs#L10)
<!-- source: Assets/Scripts/Features/Flight/Thrusters/MainThruster.cs|FixedUpdate -->
- [PowerTransmissionDevice.Update](../Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs#L61)
<!-- source: Assets/Scripts/Features/Power/Devices/PowerTransmissionDevice.cs|Update -->
- [WorldCombatPlayerChecks.EnemyCargoFixture](../Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs#L144)
<!-- source: Assets/Scripts/Features/World/Validation/WorldCombatPlayerChecks.cs|EnemyCargoFixture -->
- [EnemySpawner.SpawnBlockData](../Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs#L144)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemySpawner.cs|SpawnBlockData -->
- [Turret.FixedUpdate](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Turret.cs|FixedUpdate -->
- [ProjectileTurret.Fire](../Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs#L164)
<!-- source: Assets/Scripts/Features/Combat/Turrets/ProjectileTurret.cs|Fire -->
- [TurretHitResolver.Apply](../Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs#L7)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretHitResolver.cs|Apply -->
- [Durability.ApplyDamage](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|ApplyDamage -->
- [DestroyManager.DestroyGameObject](../Assets/Scripts/Features/Combat/DestroyManager.cs#L28)
<!-- source: Assets/Scripts/Features/Combat/DestroyManager.cs|DestroyGameObject -->
- [WreckSalvage.Convert](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10)
<!-- source: Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs|Convert -->
- [CollectionBot.FixedUpdate](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|FixedUpdate -->
- [CollectionBot.ReturnCargo](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L88)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|ReturnCargo -->
- [CargoHold.Store](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L128)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs|Store -->

## Player 正式据点资源回收验证

源码推导：显式-world-smoke-outpost-salvage或-world-smoke-outpost-salvage-all启用。正式生成据点完成驻军/炮塔生命周期验证后，临时玩家平台提供真实电网、分类货仓和机器人；直接施加伤害击毁实际据点货仓，按释放前后物品总量验证包含合并包的掉落，再观察自然吸附/携带/停靠。普通路线至少运回一个原始模块；all路线给出300秒运输上限，要求每个原始模块都观察到机器人携带并全部入仓。每轮退出后另检查包含隐藏对象的敌机/掉落实体清理，记录Ready阶段对象数量和分配内存。不是玩家输入自然攻破据点的完整路线，不写永久进度。

```mermaid
flowchart TD
    A["OutpostPlayerChecks.Scenario()"]
    click A "../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L63" "打开源码"
    B["OutpostSalvagePlayerChecks.Run()"]
    click B "../Assets/Scripts/Features/World/Validation/OutpostSalvagePlayerChecks.cs#L12" "打开源码"
    C["Durability.ApplyDamage()"]
    click C "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103" "打开源码"
    D["BuildingRuntime.DestroyBlock()"]
    click D "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238" "打开源码"
    E["CargoHold.ReleaseContents()"]
    click E "../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L206" "打开源码"
    F["LootDrop.Spawn()"]
    click F "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L83" "打开源码"
    G["LootDrop.Update()"]
    click G "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L261" "打开源码"
    H["CollectionBot.FixedUpdate()"]
    click H "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31" "打开源码"
    I["CollectionBot.ReturnCargo()"]
    click I "../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L88" "打开源码"
    J["LootDrop.Deliver()"]
    click J "../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L231" "打开源码"
    K["CargoHold.Store()"]
    click K "../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L128" "打开源码"
    A -->|显式回收验证| B
    B --> C
    C --> D
    D --> E
    E --> F
    F -.->|货币逐帧吸附| G
    G -->|到达货仓| J
    F -.->|模块由机器人扫描和拾取| H
    H --> I
    I -->|实际停靠后| J
    J --> K
```

- [OutpostPlayerChecks.Scenario](../Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs#L63)
<!-- source: Assets/Scripts/Features/World/Validation/OutpostPlayerChecks.cs|Scenario -->
- [OutpostSalvagePlayerChecks.Run](../Assets/Scripts/Features/World/Validation/OutpostSalvagePlayerChecks.cs#L12)
<!-- source: Assets/Scripts/Features/World/Validation/OutpostSalvagePlayerChecks.cs|Run -->
- [Durability.ApplyDamage](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L103)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|ApplyDamage -->
- [BuildingRuntime.DestroyBlock](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|DestroyBlock -->
- [CargoHold.ReleaseContents](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L206)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs|ReleaseContents -->
- [LootDrop.Spawn](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L83)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Spawn -->
- [LootDrop.Update](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L261)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Update -->
- [CollectionBot.FixedUpdate](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L31)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|FixedUpdate -->
- [CollectionBot.ReturnCargo](../Assets/Scripts/Features/Bots/Collection/CollectionBot.cs#L88)
<!-- source: Assets/Scripts/Features/Bots/Collection/CollectionBot.cs|ReturnCargo -->
- [LootDrop.Deliver](../Assets/Scripts/Features/Salvage/Drops/LootDrop.cs#L231)
<!-- source: Assets/Scripts/Features/Salvage/Drops/LootDrop.cs|Deliver -->
- [CargoHold.Store](../Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs#L128)
<!-- source: Assets/Scripts/Features/Salvage/Cargo/CargoHold.cs|Store -->

## 阵地战阶段与经济规则

源码推导：独立规则核心仅在部署和整备阶段允许支出。实际生成成功才登记敌机并消耗名额；登记ID去重，敌机结算一次，最后一个名额完成后发放整波奖励。基地失败清除名额并封锁后续操作。场景执行与驾驶链见后续阵地战图。

```mermaid
flowchart TD
    A["SiegeMatch.BeginNextWave()"]
    click A "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L109" "打开源码"
    B["SiegeMatch.CanSpawn()"]
    click B "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L125" "打开源码"
    C["SiegeMatch.RegisterAttacker()"]
    click C "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L130" "打开源码"
    D["SiegeMatch.ResolveAttacker()"]
    click D "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142" "打开源码"
    E["SiegeMatch.TrySpend()"]
    click E "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L219" "打开源码"
    F["SiegeMatch.LoseBase()"]
    click F "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L247" "打开源码"
    G["SiegeRulesChecks.Run()"]
    click G "../Assets/Editor/Siege/Validation/Rules/SiegeRulesChecks.cs#L17" "打开源码"
    A -.->|等待场景生成器| B
    C --> B
    C -.->|等待死亡或移除结算| D
    D -.->|整备后可开始下一波| A
    E -.->|仅部署或整备| A
    F -.->|失败后拒绝| A
    G -->|规则检查| A
    G --> E
    G --> F
```

- [SiegeMatch.BeginNextWave](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L109)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|BeginNextWave -->
- [SiegeMatch.CanSpawn](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L125)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|CanSpawn -->
- [SiegeMatch.RegisterAttacker](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L130)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|RegisterAttacker -->
- [SiegeMatch.ResolveAttacker](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|ResolveAttacker -->
- [SiegeMatch.TrySpend](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L219)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|TrySpend -->
- [SiegeMatch.LoseBase](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L247)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|LoseBase -->
- [SiegeRulesChecks.Run](../Assets/Editor/Siege/Validation/Rules/SiegeRulesChecks.cs#L17)
<!-- source: Assets/Editor/Siege/Validation/Rules/SiegeRulesChecks.cs|Run -->

## 阵地战部署、驾驶与修缮

源码推导：独立Siege场景Start以大型建筑蓝图创建基地，场景操作卡片调用舰船部署和接管。建筑核心由蓝图模块ID指定，无驾驶舱；建筑由WorldBuildingManager管理。舰船部署清空货仓快照并赋予部署ID，成功生成才扣费；驾驶权保存在舰船Cockpit。整备修缮先验证净空和报价，基地复用建筑模块并补件，舰船复用同源断裂件并恢复泊位姿态；成功后扣费。不写用户蓝图。

```mermaid
flowchart TD
    A["SiegeSession.Start()"]
    click A "../Assets/Scripts/Features/Siege/SiegeSession.cs#L190" "打开源码"
    B["SiegeSession.Begin()"]
    click B "../Assets/Scripts/Features/Siege/SiegeSession.cs#L200" "打开源码"
    C["SiegeFleet.CreateBase()"]
    click C "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L145" "打开源码"
    D["SiegeSession.TryDeployAt()"]
    click D "../Assets/Scripts/Features/Siege/SiegeSession.cs#L259" "打开源码"
    E["SiegeFleet.DeployAt()"]
    click E "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L164" "打开源码"
    F["ModularUnitFactory.Spawn()"]
    click F "../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L41" "打开源码"
    G["SiegeFleet.TakeControl()"]
    click G "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L342" "打开源码"
    H["Cockpit.SetControlMode()"]
    click H "../Assets/Scripts/Features/Simulation/Units/Cockpit.cs#L21" "打开源码"
    I["ControlUnit.Update()"]
    click I "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L138" "打开源码"
    J["EnemyController.FixedUpdate()"]
    click J "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117" "打开源码"
    K["SiegeFleet.RefreshBindings()"]
    click K "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L439" "打开源码"
    L["SiegeSession.TryRepairShip()"]
    click L "../Assets/Scripts/Features/Siege/SiegeSession.cs#L317" "打开源码"
    M["SiegeRefit.Repair()"]
    click M "../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L135" "打开源码"
    N["SiegeRefit.IsClear()"]
    click N "../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L9" "打开源码"
    O["SiegeMatch.TrySpend()"]
    click O "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L219" "打开源码"
    P["SiegeDeploymentView.OnEnable()"]
    click P "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L57" "打开源码"
    Q["SiegeBase.Create()"]
    click Q "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L48" "打开源码"
    R["WorldBuildingManager.RestoreBuilding()"]
    click R "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L126" "打开源码"
    S["SiegeBase.Repair()"]
    click S "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151" "打开源码"
    T["BuildingRuntime.TryRefit()"]
    click T "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L41" "打开源码"
    U["BuildingRefit.Apply()"]
    click U "../Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs#L9" "打开源码"
    V["SiegeSession.RefreshBlueprints()"]
    click V "../Assets/Scripts/Features/Siege/SiegeSession.cs#L119" "打开源码"
    W["SiegeBlueprintCatalog.Load()"]
    click W "../Assets/Scripts/Features/Siege/Fleet/SiegeBlueprintCatalog.cs#L10" "打开源码"
    X["SiegeDeploymentView.RefreshOptions()"]
    click X "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L246" "打开源码"
    Y["SiegeDeploymentView.SelectAttacker()"]
    click Y "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L241" "打开源码"
    Z["SiegeSession.SelectAttacker()"]
    click Z "../Assets/Scripts/Features/Siege/SiegeSession.cs#L103" "打开源码"
    AA["SiegeDeploymentView.SelectOption()"]
    click AA "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L231" "打开源码"
    AB["SiegeSession.SelectDefender()"]
    click AB "../Assets/Scripts/Features/Siege/SiegeSession.cs#L111" "打开源码"
    AC["SiegeSession.FindOption()"]
    click AC "../Assets/Scripts/Features/Siege/SiegeSession.cs#L153" "打开源码"
    AD["SiegeDeploymentView.Confirm()"]
    click AD "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L360" "打开源码"
    AE["SiegeDeploymentView.Pilot()"]
    click AE "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L395" "打开源码"
    AF["SiegeDeploymentView.Repair()"]
    click AF "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L401" "打开源码"
    AG["SiegeSession.TryTakeControl()"]
    click AG "../Assets/Scripts/Features/Siege/SiegeSession.cs#L295" "打开源码"
    AH["SiegeHud.RepairCore()"]
    click AH "../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L115" "打开源码"
    A --> B
    B --> C
    C --> Q
    Q --> R
    P -.->|确认监听| AD
    AD --> D
    D --> E
    B -->|启动时只读快照| V
    V -->|双方独立目录| W
    P -.->|下拉选项事件| Y
    Y --> Z
    P -.->|守方选项事件| AA
    AA --> AB
    V -->|按名称恢复或回退| AC
    E -->|指定泊位净空| N
    E -->|净空成功经Spawn| F
    E --> O
    P -.->|接管按钮| AE
    AE --> AG
    AG --> G
    G --> H
    H -.->|门控玩家输入| I
    H -.->|门控自主驾驶| J
    K -.->|新组的AI与镜头| J
    P -.->|友舰修缮按钮| AF
    AF --> L
    AH --> L
    L --> M
    M --> N
    M --> O
    L -->|建筑基地| S
    S --> T
    T --> U
    S --> O
```

- [SiegeSession.Start](../Assets/Scripts/Features/Siege/SiegeSession.cs#L190)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|Start -->
- [SiegeSession.Begin](../Assets/Scripts/Features/Siege/SiegeSession.cs#L200)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|Begin -->
- [SiegeFleet.CreateBase](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L145)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|CreateBase -->
- [SiegeSession.TryDeployAt](../Assets/Scripts/Features/Siege/SiegeSession.cs#L259)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryDeployAt -->
- [SiegeFleet.DeployAt](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L164)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|DeployAt -->
- [ModularUnitFactory.Spawn](../Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs#L41)
<!-- source: Assets/Scripts/Features/Simulation/Units/ModularUnitFactory.cs|Spawn -->
- [SiegeFleet.TakeControl](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L342)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|TakeControl -->
- [Cockpit.SetControlMode](../Assets/Scripts/Features/Simulation/Units/Cockpit.cs#L21)
<!-- source: Assets/Scripts/Features/Simulation/Units/Cockpit.cs|SetControlMode -->
- [ControlUnit.Update](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L138)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|Update -->
- [EnemyController.FixedUpdate](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|FixedUpdate -->
- [SiegeFleet.RefreshBindings](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L439)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|RefreshBindings -->
- [SiegeSession.TryRepairShip](../Assets/Scripts/Features/Siege/SiegeSession.cs#L317)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryRepairShip -->
- [SiegeRefit.Repair](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L135)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs|Repair -->
- [SiegeRefit.IsClear](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L9)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs|IsClear -->
- [SiegeMatch.TrySpend](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L219)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|TrySpend -->
- [SiegeDeploymentView.OnEnable](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L57)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|OnEnable -->
- [SiegeBase.Create](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L48)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Create -->
- [WorldBuildingManager.RestoreBuilding](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L126)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|RestoreBuilding -->
- [SiegeBase.Repair](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Repair -->
- [BuildingRuntime.TryRefit](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L41)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|TryRefit -->
- [BuildingRefit.Apply](../Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs#L9)
<!-- source: Assets/Scripts/Features/World/Buildings/Topology/BuildingRefit.cs|Apply -->
- [SiegeSession.RefreshBlueprints](../Assets/Scripts/Features/Siege/SiegeSession.cs#L119)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|RefreshBlueprints -->
- [SiegeBlueprintCatalog.Load](../Assets/Scripts/Features/Siege/Fleet/SiegeBlueprintCatalog.cs#L10)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeBlueprintCatalog.cs|Load -->
- [SiegeDeploymentView.RefreshOptions](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L246)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|RefreshOptions -->
- [SiegeDeploymentView.SelectAttacker](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L241)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|SelectAttacker -->
- [SiegeSession.SelectAttacker](../Assets/Scripts/Features/Siege/SiegeSession.cs#L103)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|SelectAttacker -->
- [SiegeDeploymentView.SelectOption](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L231)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|SelectOption -->
- [SiegeSession.SelectDefender](../Assets/Scripts/Features/Siege/SiegeSession.cs#L111)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|SelectDefender -->
- [SiegeSession.FindOption](../Assets/Scripts/Features/Siege/SiegeSession.cs#L153)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|FindOption -->
- [SiegeDeploymentView.Confirm](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L360)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|Confirm -->
- [SiegeDeploymentView.Pilot](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L395)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|Pilot -->
- [SiegeDeploymentView.Repair](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L401)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|Repair -->
- [SiegeSession.TryTakeControl](../Assets/Scripts/Features/Siege/SiegeSession.cs#L295)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryTakeControl -->
- [SiegeHud.RepairCore](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L115)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeHud.cs|RepairCore -->

## 阵地战精英波次与连击奖励

源码推导：波次总监在精英波次挑出目录里模块数最多的敌方蓝图；规则核心按倍数放大击毁与清波奖励，并拒绝部署阶段与失败后的额外奖励。会话每帧比较公开击毁计数，在连击窗口内按实际击毁数累加并发放金币；HUD只读Combo/LastComboBonus显示战果，核心受击时驱动告警条与一次镜头抖动。不改蓝图、不生成额外敌舰、不新增物理查询。

```mermaid
flowchart TD
    A["SiegeWaveDirector.Tick()"]
    click A "../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46" "打开源码"
    B["SiegeWaveDirector.PickBlueprint()"]
    click B "../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L106" "打开源码"
    C["SiegeMatch.ResolveAttacker()"]
    click C "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142" "打开源码"
    D["SiegeMatch.GrantBonus()"]
    click D "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L165" "打开源码"
    E["SiegeSession.TickKillStreak()"]
    click E "../Assets/Scripts/Features/Siege/SiegeSession.cs#L444" "打开源码"
    F["SiegeSession.Update()"]
    click F "../Assets/Scripts/Features/Siege/SiegeSession.cs#L374" "打开源码"
    G["SiegeBattleFeedback.Update()"]
    click G "../Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs#L22" "打开源码"
    H["SiegeSettings.CreateMatch()"]
    click H "../Assets/Scripts/Features/Siege/SiegeSettings.cs#L108" "打开源码"
    A -->|精英波次| B
    A --> C
    F --> E
    E --> D
    H -->|传入间隔与倍数| C
    G -->|读取连击| E
```

- [SiegeWaveDirector.Tick](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs|Tick -->
- [SiegeWaveDirector.PickBlueprint](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L106)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs|PickBlueprint -->
- [SiegeMatch.ResolveAttacker](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|ResolveAttacker -->
- [SiegeMatch.GrantBonus](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L165)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|GrantBonus -->
- [SiegeSession.TickKillStreak](../Assets/Scripts/Features/Siege/SiegeSession.cs#L444)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TickKillStreak -->
- [SiegeSession.Update](../Assets/Scripts/Features/Siege/SiegeSession.cs#L374)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|Update -->
- [SiegeBattleFeedback.Update](../Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs#L22)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs|Update -->
- [SiegeSettings.CreateMatch](../Assets/Scripts/Features/Siege/SiegeSettings.cs#L108)
<!-- source: Assets/Scripts/Features/Siege/SiegeSettings.cs|CreateMatch -->

## 阵地战场景泊位交互与阶段HUD

源码推导：场景平台与投影标记选择部署位置，静态模型预览没有碰撞或战斗脚本；确认时重新验证指定位置净空与余额，成功生成后扣费。友舰通过部署ID选择和接管，Tab切换存活友舰。HUD按阶段显示整备动作与失败结算，0.25秒刷新文字；报价由明确操作事件失效，不再依赖Status字符串。战斗反馈独立读取波次与损伤。场景迁移由Editor工具保存，旧菜单统一委托新工具。

```mermaid
flowchart TD
    A["SiegeDeploymentView.Update()"]
    click A "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L106" "打开源码"
    B["SiegeDeploymentView.SelectDock()"]
    click B "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L198" "打开源码"
    C["SiegeDeploymentView.RefreshSelection()"]
    click C "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L297" "打开源码"
    D["BlueprintModelPreview.Create()"]
    click D "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L8" "打开源码"
    E["SiegeDeploymentView.Confirm()"]
    click E "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L360" "打开源码"
    F["SiegeSession.TryDeployAt()"]
    click F "../Assets/Scripts/Features/Siege/SiegeSession.cs#L259" "打开源码"
    G["SiegeFleet.DeployAt()"]
    click G "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L164" "打开源码"
    H["SiegeDeploymentView.SelectShip()"]
    click H "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L218" "打开源码"
    I["SiegeSession.TryTakeControl()"]
    click I "../Assets/Scripts/Features/Siege/SiegeSession.cs#L295" "打开源码"
    J["SiegeFleetView.Focus()"]
    click J "../Assets/Scripts/Features/Siege/Interface/SiegeFleetView.cs#L7" "打开源码"
    K["SiegeSession.TryRepairShip()"]
    click K "../Assets/Scripts/Features/Siege/SiegeSession.cs#L317" "打开源码"
    L["SiegeSession.NotifyRefitChanged()"]
    click L "../Assets/Scripts/Features/Siege/SiegeSession.cs#L254" "打开源码"
    M["SiegeHud.Refresh()"]
    click M "../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L63" "打开源码"
    N["SiegeBattleFeedback.Update()"]
    click N "../Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs#L22" "打开源码"
    O["SiegeInterfaceAuthoring.Apply()"]
    click O "../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L17" "打开源码"
    P["SiegeDeploymentView.Cancel()"]
    click P "../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L422" "打开源码"
    Q["SiegeFleetPhysics.Park()"]
    click Q "../Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs#L7" "打开源码"
    R["SiegeFleetPhysics.Activate()"]
    click R "../Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs#L25" "打开源码"
    A -->|场景/标记点击| B
    B --> C
    C -->|纯模型| D
    E --> F
    F -->|指定位置| G
    G --> Q
    F --> L
    A -->|友舰ID| H
    H --> C
    I -->|Fleet驾驶权绑定| J
    K --> L
    L -.->|RefitChanged| C
    L -.->|报价失效后按阶段读取| M
    A -->|阶段变化/取消/编辑| P
    O -.->|保存序列化绑定| A
    R -.->|战斗物理与AI| N
```

- [SiegeDeploymentView.Update](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L106)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|Update -->
- [SiegeDeploymentView.SelectDock](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L198)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|SelectDock -->
- [SiegeDeploymentView.RefreshSelection](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L297)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|RefreshSelection -->
- [BlueprintModelPreview.Create](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs#L8)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintModelPreview.cs|Create -->
- [SiegeDeploymentView.Confirm](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L360)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|Confirm -->
- [SiegeSession.TryDeployAt](../Assets/Scripts/Features/Siege/SiegeSession.cs#L259)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryDeployAt -->
- [SiegeFleet.DeployAt](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L164)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|DeployAt -->
- [SiegeDeploymentView.SelectShip](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L218)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|SelectShip -->
- [SiegeSession.TryTakeControl](../Assets/Scripts/Features/Siege/SiegeSession.cs#L295)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryTakeControl -->
- [SiegeFleetView.Focus](../Assets/Scripts/Features/Siege/Interface/SiegeFleetView.cs#L7)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeFleetView.cs|Focus -->
- [SiegeSession.TryRepairShip](../Assets/Scripts/Features/Siege/SiegeSession.cs#L317)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryRepairShip -->
- [SiegeSession.NotifyRefitChanged](../Assets/Scripts/Features/Siege/SiegeSession.cs#L254)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|NotifyRefitChanged -->
- [SiegeHud.Refresh](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L63)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeHud.cs|Refresh -->
- [SiegeBattleFeedback.Update](../Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs#L22)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeBattleFeedback.cs|Update -->
- [SiegeInterfaceAuthoring.Apply](../Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs#L17)
<!-- source: Assets/Editor/Siege/Authoring/SiegeInterfaceAuthoring.cs|Apply -->
- [SiegeDeploymentView.Cancel](../Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs#L422)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeDeploymentView.cs|Cancel -->
- [SiegeFleetPhysics.Park](../Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs#L7)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs|Park -->
- [SiegeFleetPhysics.Activate](../Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs#L25)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleetPhysics.cs|Activate -->

## 阵地战Main工作台与输入边界

源码推导：Siege保存Main真实UI与建造依赖，禁用Main自动开档和世界加载。整备B或工作台按钮进入独立存档预览，将战场移至不模拟的独立PhysicsScene并临时剔除其渲染层，蓝图在独立预览场景编辑；跨场景迁移显式保留Durability伤势及归因；Session跳过战场刷新并拒绝部署、修缮、接管与开波。SaveManager不再全局删除Siege载具，异步BlueprintLoader仅替换预览。P经MainUIPanels返回，取消加载与撤销历史、销毁预览、恢复原父节点、场景、渲染层、镜头掩码及驾驶视图、刷新蓝图目录；编辑不会免费更新已部署机体。战斗中Alt/F1沿用Main鼠标与推进器窗口操作。

```mermaid
flowchart TD
    A["InputManager.Update()"]
    click A "../Assets/Scripts/Features/Player/InputManager.cs#L40" "打开源码"
    B["InputManager.PollSiege()"]
    click B "../Assets/Scripts/Features/Player/InputManager.cs#L179" "打开源码"
    C["SiegeWorkshop.Open()"]
    click C "../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L47" "打开源码"
    D["SaveManager.LoadSave()"]
    click D "../Assets/Scripts/Features/Building/SaveManager.cs#L356" "打开源码"
    E["BuildManager.LoadAllBlocks()"]
    click E "../Assets/Scripts/Features/Building/BuildManager.cs#L701" "打开源码"
    F["BlueprintLoader.Load()"]
    click F "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43" "打开源码"
    G["BuildManager.CreateBlock()"]
    click G "../Assets/Scripts/Features/Building/BuildManager.cs#L597" "打开源码"
    H["BuildManager.SaveBlock()"]
    click H "../Assets/Scripts/Features/Building/BuildManager.cs#L675" "打开源码"
    I["MainUIPanels.PlayStart()"]
    click I "../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233" "打开源码"
    J["SiegeWorkshop.Close()"]
    click J "../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L88" "打开源码"
    K["BuildManager.CancelBlueprintLoad()"]
    click K "../Assets/Scripts/Features/Building/BuildManager.cs#L910" "打开源码"
    L["BlueprintLoader.Cancel()"]
    click L "../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L94" "打开源码"
    M["SiegeFleet.RestoreView()"]
    click M "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L546" "打开源码"
    N["SiegeSession.RefreshBlueprints()"]
    click N "../Assets/Scripts/Features/Siege/SiegeSession.cs#L119" "打开源码"
    O["SiegeSession.Update()"]
    click O "../Assets/Scripts/Features/Siege/SiegeSession.cs#L374" "打开源码"
    P["ThrusterInfoPanel.Toggle()"]
    click P "../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L62" "打开源码"
    Q["SiegeWorkshop.Start()"]
    click Q "../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L28" "打开源码"
    R["SiegeWorkshop.OpenSelected()"]
    click R "../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L42" "打开源码"
    S["SiegeWorkshopStage.Open()"]
    click S "../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L21" "打开源码"
    T["SiegeWorkshopStage.Dispose()"]
    click T "../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L107" "打开源码"
    U["SiegeWorkshopStage.Transfer()"]
    click U "../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L83" "打开源码"
    A --> B
    B -->|整备B| C
    Q -.->|按钮监听| R
    R --> C
    C --> D
    D --> E
    E --> F
    G --> H
    A -->|工作台P| I
    I --> J
    J --> K
    K --> L
    J --> M
    J --> N
    C -.->|编辑状态门控| O
    B -->|战斗F1| P
    C --> S
    S --> U
    J --> T
    T -->|保留伤势迁回| U
```

- [InputManager.Update](../Assets/Scripts/Features/Player/InputManager.cs#L40)
<!-- source: Assets/Scripts/Features/Player/InputManager.cs|Update -->
- [InputManager.PollSiege](../Assets/Scripts/Features/Player/InputManager.cs#L179)
<!-- source: Assets/Scripts/Features/Player/InputManager.cs|PollSiege -->
- [SiegeWorkshop.Open](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L47)
<!-- source: Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs|Open -->
- [SaveManager.LoadSave](../Assets/Scripts/Features/Building/SaveManager.cs#L356)
<!-- source: Assets/Scripts/Features/Building/SaveManager.cs|LoadSave -->
- [BuildManager.LoadAllBlocks](../Assets/Scripts/Features/Building/BuildManager.cs#L701)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|LoadAllBlocks -->
- [BlueprintLoader.Load](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L43)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|Load -->
- [BuildManager.CreateBlock](../Assets/Scripts/Features/Building/BuildManager.cs#L597)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|CreateBlock -->
- [BuildManager.SaveBlock](../Assets/Scripts/Features/Building/BuildManager.cs#L675)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|SaveBlock -->
- [MainUIPanels.PlayStart](../Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs#L233)
<!-- source: Assets/Scripts/Features/Presentation/Interface/MainUIPanels.cs|PlayStart -->
- [SiegeWorkshop.Close](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L88)
<!-- source: Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs|Close -->
- [BuildManager.CancelBlueprintLoad](../Assets/Scripts/Features/Building/BuildManager.cs#L910)
<!-- source: Assets/Scripts/Features/Building/BuildManager.cs|CancelBlueprintLoad -->
- [BlueprintLoader.Cancel](../Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs#L94)
<!-- source: Assets/Scripts/Features/Building/BlueprintLoading/BlueprintLoader.cs|Cancel -->
- [SiegeFleet.RestoreView](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L546)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|RestoreView -->
- [SiegeSession.RefreshBlueprints](../Assets/Scripts/Features/Siege/SiegeSession.cs#L119)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|RefreshBlueprints -->
- [SiegeSession.Update](../Assets/Scripts/Features/Siege/SiegeSession.cs#L374)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|Update -->
- [ThrusterInfoPanel.Toggle](../Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs#L62)
<!-- source: Assets/Scripts/Features/Flight/Presentation/ThrusterInfoPanel.cs|Toggle -->
- [SiegeWorkshop.Start](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L28)
<!-- source: Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs|Start -->
- [SiegeWorkshop.OpenSelected](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs#L42)
<!-- source: Assets/Scripts/Features/Siege/Workshop/SiegeWorkshop.cs|OpenSelected -->
- [SiegeWorkshopStage.Open](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L21)
<!-- source: Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs|Open -->
- [SiegeWorkshopStage.Dispose](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L107)
<!-- source: Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs|Dispose -->
- [SiegeWorkshopStage.Transfer](../Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs#L83)
<!-- source: Assets/Scripts/Features/Siege/Workshop/SiegeWorkshopStage.cs|Transfer -->

## 阵地战波次、伤害与失败

源码推导：波次开始启用物理；会话Update串行处理生成预算和真实死亡事件。敌舰Defeated只记事实，后续Tick一次性结算。最后一敌舰结算进入整备并冻结模拟；非战斗阶段拒绝伤害。过渡只发生一次：基地先按建筑蓝图报价检查净空并自动原位修缮，再从驾驶舰开始轮询存活友舰，沿用身份/货舱保护、泊位净空和报价支付；余额不足/占位不扣费并汇报舰队统计，选项可独立关闭。驾驶舰死亡接管其他舰艇，无舰则观察基地；核心丢失终止波次。销毁会话解绑敌舰事件并恢复后台设置，重开通过实际SceneManager重载。

```mermaid
flowchart TD
    A["SiegeSession.BeginNextWave()"]
    click A "../Assets/Scripts/Features/Siege/SiegeSession.cs#L225" "打开源码"
    B["SiegeMatch.BeginNextWave()"]
    click B "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L109" "打开源码"
    C["SiegeSession.SetSimulationActive()"]
    click C "../Assets/Scripts/Features/Siege/SiegeSession.cs#L512" "打开源码"
    D["SiegeSession.Update()"]
    click D "../Assets/Scripts/Features/Siege/SiegeSession.cs#L374" "打开源码"
    E["SiegeWaveDirector.Tick()"]
    click E "../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46" "打开源码"
    F["EnemyBlueprintFactory.Spawn()"]
    click F "../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12" "打开源码"
    G["EnemyIdentity.ReportDefeated()"]
    click G "../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L9" "打开源码"
    H["SiegeMatch.ResolveAttacker()"]
    click H "../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142" "打开源码"
    I["Durability.ChangeDurability()"]
    click I "../Assets/Scripts/Features/Combat/Damage/Durability.cs#L163" "打开源码"
    J["DestroyManager.DestroyGameObject()"]
    click J "../Assets/Scripts/Features/Combat/DestroyManager.cs#L28" "打开源码"
    K["SiegeSession.NotifyCockpitDestroyed()"]
    click K "../Assets/Scripts/Features/Siege/SiegeSession.cs#L368" "打开源码"
    L["SiegeFleet.NotifyLost()"]
    click L "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L465" "打开源码"
    M["SiegeSession.LoseBase()"]
    click M "../Assets/Scripts/Features/Siege/SiegeSession.cs#L348" "打开源码"
    N["SiegeSession.OnDestroy()"]
    click N "../Assets/Scripts/Features/Siege/SiegeSession.cs#L504" "打开源码"
    O["SiegeWaveDirector.Stop()"]
    click O "../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L126" "打开源码"
    P["SiegeHud.Restart()"]
    click P "../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L120" "打开源码"
    Q["BuildingRuntime.DestroyBlock()"]
    click Q "../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238" "打开源码"
    R["SiegeBase.OnBlockDestroyed()"]
    click R "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L176" "打开源码"
    S["SiegeBase.Dispose()"]
    click S "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L184" "打开源码"
    T["SiegeSession.RepairBaseBetweenWaves()"]
    click T "../Assets/Scripts/Features/Siege/SiegeSession.cs#L469" "打开源码"
    U["SiegeBase.Quote()"]
    click U "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L123" "打开源码"
    V["SiegeBase.Repair()"]
    click V "../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151" "打开源码"
    W["SiegeFleet.RepairBetweenWaves()"]
    click W "../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L399" "打开源码"
    X["SiegeRefit.Quote()"]
    click X "../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L103" "打开源码"
    Y["SiegeRefit.Repair()"]
    click Y "../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L135" "打开源码"
    A --> B
    A --> C
    D --> E
    E --> F
    E --> H
    G -.->|Attacker事件标记后由Tick读取| E
    D -->|波次结束| C
    D -->|进入波次间隔后仅一次| T
    T -->|基地有损伤| U
    T -->|基地成功后扣费| V
    T -->|基地完成/失败后| W
    W -->|每艘存活舰先报价| X
    X -->|损伤且余额足够| Y
    I -->|舰船| J
    J --> G
    J --> K
    K --> L
    I -->|建筑| Q
    Q -.->|BlockDestroyed事件| R
    R -->|指定核心被摧毁| M
    D -->|核心失活或离开建筑主体| M
    N --> S
    M --> C
    M --> O
    N --> O
    P -.->|场景重载销毁旧会话| N
```

- [SiegeSession.BeginNextWave](../Assets/Scripts/Features/Siege/SiegeSession.cs#L225)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|BeginNextWave -->
- [SiegeMatch.BeginNextWave](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L109)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|BeginNextWave -->
- [SiegeSession.SetSimulationActive](../Assets/Scripts/Features/Siege/SiegeSession.cs#L512)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|SetSimulationActive -->
- [SiegeSession.Update](../Assets/Scripts/Features/Siege/SiegeSession.cs#L374)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|Update -->
- [SiegeWaveDirector.Tick](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs|Tick -->
- [EnemyBlueprintFactory.Spawn](../Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs#L12)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyBlueprintFactory.cs|Spawn -->
- [EnemyIdentity.ReportDefeated](../Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs#L9)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyIdentity.cs|ReportDefeated -->
- [SiegeMatch.ResolveAttacker](../Assets/Scripts/Features/Siege/SiegeMatch.cs#L142)
<!-- source: Assets/Scripts/Features/Siege/SiegeMatch.cs|ResolveAttacker -->
- [Durability.ChangeDurability](../Assets/Scripts/Features/Combat/Damage/Durability.cs#L163)
<!-- source: Assets/Scripts/Features/Combat/Damage/Durability.cs|ChangeDurability -->
- [DestroyManager.DestroyGameObject](../Assets/Scripts/Features/Combat/DestroyManager.cs#L28)
<!-- source: Assets/Scripts/Features/Combat/DestroyManager.cs|DestroyGameObject -->
- [SiegeSession.NotifyCockpitDestroyed](../Assets/Scripts/Features/Siege/SiegeSession.cs#L368)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|NotifyCockpitDestroyed -->
- [SiegeFleet.NotifyLost](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L465)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|NotifyLost -->
- [SiegeSession.LoseBase](../Assets/Scripts/Features/Siege/SiegeSession.cs#L348)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|LoseBase -->
- [SiegeSession.OnDestroy](../Assets/Scripts/Features/Siege/SiegeSession.cs#L504)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|OnDestroy -->
- [SiegeWaveDirector.Stop](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L126)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs|Stop -->
- [SiegeHud.Restart](../Assets/Scripts/Features/Siege/Interface/SiegeHud.cs#L120)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeHud.cs|Restart -->
- [BuildingRuntime.DestroyBlock](../Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs#L238)
<!-- source: Assets/Scripts/Features/World/Buildings/BuildingRuntime.cs|DestroyBlock -->
- [SiegeBase.OnBlockDestroyed](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L176)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|OnBlockDestroyed -->
- [SiegeBase.Dispose](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L184)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Dispose -->
- [SiegeSession.RepairBaseBetweenWaves](../Assets/Scripts/Features/Siege/SiegeSession.cs#L469)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|RepairBaseBetweenWaves -->
- [SiegeBase.Quote](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L123)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Quote -->
- [SiegeBase.Repair](../Assets/Scripts/Features/Siege/Battle/SiegeBase.cs#L151)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeBase.cs|Repair -->
- [SiegeFleet.RepairBetweenWaves](../Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs#L399)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeFleet.cs|RepairBetweenWaves -->
- [SiegeRefit.Quote](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L103)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs|Quote -->
- [SiegeRefit.Repair](../Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs#L135)
<!-- source: Assets/Scripts/Features/Siege/Fleet/SiegeRefit.cs|Repair -->

## 阵地战独立Player验证

源码推导：制作工具只构建Siege入口到独立验证目录，不改默认BuildSettings。Player仅显式命令行参数启动；受控smoke使用真实伤害刺激验证闭环，自然soak只执行整备维修和下一波，两者分开报告。LateUpdate记录帧间隔，每秒普查按刚体根组件分类并测量自身开销，保留有限慢帧及前一观察窗口的嵌套工作量；显式profile才启用计时并读取可用引擎已完成样本并保留线程覆盖限制，缺失或空样本不冒充零耗时；Development分析包与发布包独立，完成、异常或销毁释放记录器并恢复开关。

```mermaid
flowchart TD
    A["SiegePlayerBuild.Build()"]
    click A "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L16" "打开源码"
    B["SiegePlayerBuild.BuildNow()"]
    click B "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L83" "打开源码"
    C["SiegePlayerChecks.Initialize()"]
    click C "../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L30" "打开源码"
    D["SiegePlayerChecks.Update()"]
    click D "../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L93" "打开源码"
    E["SiegePlayerChecks.Run()"]
    click E "../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L140" "打开源码"
    F["SiegePlayerChecks.LateUpdate()"]
    click F "../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L114" "打开源码"
    G["SiegePlayerChecks.Finish()"]
    click G "../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L502" "打开源码"
    H["SiegeSession.TryRepair()"]
    click H "../Assets/Scripts/Features/Siege/SiegeSession.cs#L312" "打开源码"
    I["SiegeSession.BeginNextWave()"]
    click I "../Assets/Scripts/Features/Siege/SiegeSession.cs#L225" "打开源码"
    J["SiegeSession.TryDeploy()"]
    click J "../Assets/Scripts/Features/Siege/SiegeSession.cs#L268" "打开源码"
    K["SiegePlayerChecks.CaptureError()"]
    click K "../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L494" "打开源码"
    L["SiegePlayerBuild.BuildStress()"]
    click L "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L22" "打开源码"
    M["SiegeStressAuthoring.Create()"]
    click M "../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L18" "打开源码"
    N["SiegePlayerBuild.Schedule()"]
    click N "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L63" "打开源码"
    O["SiegePlayerSurvey.Observe()"]
    click O "../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L39" "打开源码"
    P["SiegePlayerSurvey.Classify()"]
    click P "../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L75" "打开源码"
    Q["SiegePlayerSurvey.Dispose()"]
    click Q "../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L214" "打开源码"
    R["SiegePlayerChecks.OnDestroy()"]
    click R "../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L132" "打开源码"
    S["SiegePlayerSurvey.TakeCensus()"]
    click S "../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L128" "打开源码"
    T["SiegePlayerSurvey.KeepSlowFrame()"]
    click T "../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L156" "打开源码"
    U["SiegeEngineCounters.Read()"]
    click U "../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L60" "打开源码"
    V["SiegeEngineCounters.ReadCounter()"]
    click V "../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L81" "打开源码"
    W["SiegeEngineCounters.Dispose()"]
    click W "../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L122" "打开源码"
    X["SiegePlayerBuild.BuildLargeProfiling()"]
    click X "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L42" "打开源码"
    Y["SiegeDrivingPlayerChecks.Run()"]
    click Y "../Assets/Scripts/Features/Siege/Validation/SiegeDrivingPlayerChecks.cs#L15" "打开源码"
    Z["SiegeSession.TryTakeControl()"]
    click Z "../Assets/Scripts/Features/Siege/SiegeSession.cs#L295" "打开源码"
    A --> N
    L --> M
    L --> N
    N -.->|一次性Editor update| B
    C -.->|MonoBehaviour Update| D
    D -->|推进显式迭代器| E
    E --> H
    E --> I
    E --> J
    C -.->|真实帧 LateUpdate| F
    C -.->|日志事件| K
    D -->|结束或异常| G
    F --> O
    O --> S
    S --> P
    O --> T
    G --> Q
    R --> Q
    O --> U
    U --> V
    Q --> W
    X --> N
    E -.->|显式驾驶迭代器，finally恢复虚拟设备| Y
    Y -.->|真实HUD指针点击| Z
```

- [SiegePlayerBuild.Build](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L16)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|Build -->
- [SiegePlayerBuild.BuildNow](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L83)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|BuildNow -->
- [SiegePlayerChecks.Initialize](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L30)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs|Initialize -->
- [SiegePlayerChecks.Update](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L93)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs|Update -->
- [SiegePlayerChecks.Run](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L140)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs|Run -->
- [SiegePlayerChecks.LateUpdate](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L114)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs|LateUpdate -->
- [SiegePlayerChecks.Finish](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L502)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs|Finish -->
- [SiegeSession.TryRepair](../Assets/Scripts/Features/Siege/SiegeSession.cs#L312)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryRepair -->
- [SiegeSession.BeginNextWave](../Assets/Scripts/Features/Siege/SiegeSession.cs#L225)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|BeginNextWave -->
- [SiegeSession.TryDeploy](../Assets/Scripts/Features/Siege/SiegeSession.cs#L268)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryDeploy -->
- [SiegePlayerChecks.CaptureError](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L494)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs|CaptureError -->
- [SiegePlayerBuild.BuildStress](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L22)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|BuildStress -->
- [SiegeStressAuthoring.Create](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L18)
<!-- source: Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs|Create -->
- [SiegePlayerBuild.Schedule](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L63)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|Schedule -->
- [SiegePlayerSurvey.Observe](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L39)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs|Observe -->
- [SiegePlayerSurvey.Classify](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L75)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs|Classify -->
- [SiegePlayerSurvey.Dispose](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L214)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs|Dispose -->
- [SiegePlayerChecks.OnDestroy](../Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs#L132)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerChecks.cs|OnDestroy -->
- [SiegePlayerSurvey.TakeCensus](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L128)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs|TakeCensus -->
- [SiegePlayerSurvey.KeepSlowFrame](../Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs#L156)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegePlayerSurvey.cs|KeepSlowFrame -->
- [SiegeEngineCounters.Read](../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L60)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs|Read -->
- [SiegeEngineCounters.ReadCounter](../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L81)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs|ReadCounter -->
- [SiegeEngineCounters.Dispose](../Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs#L122)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegeEngineCounters.cs|Dispose -->
- [SiegePlayerBuild.BuildLargeProfiling](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L42)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|BuildLargeProfiling -->
- [SiegeDrivingPlayerChecks.Run](../Assets/Scripts/Features/Siege/Validation/SiegeDrivingPlayerChecks.cs#L15)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegeDrivingPlayerChecks.cs|Run -->
- [SiegeSession.TryTakeControl](../Assets/Scripts/Features/Siege/SiegeSession.cs#L295)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|TryTakeControl -->

## 阵地战性能观察与分项计时

源码推导：采样器仅在Siege真实Play显式启动；Editor update观察推进的游戏帧，保留跳帧及游戏delta。连接遍历、物理组重建和完整刷新是嵌套累计CPU计时，不可相加。每秒一次场景查询本身有分配；停止Play或脚本重载会保存已取得的样本并解绑，报告不代表Player/GPU性能。

```mermaid
flowchart TD
    A["SiegePerformanceCapture.StartCapture()"]
    click A "../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L35" "打开源码"
    B["SiegePerformanceCapture.Tick()"]
    click B "../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L77" "打开源码"
    C["SiegePerformanceCapture.TakeCensus()"]
    click C "../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L114" "打开源码"
    D["SiegePerformanceCapture.Finish()"]
    click D "../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L146" "打开源码"
    E["SimulationWorkMetrics.Measure()"]
    click E "../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L64" "打开源码"
    F["SimulationWorkMetrics.Read()"]
    click F "../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L69" "打开源码"
    G["BlockGroupManager.GroupBlocks()"]
    click G "../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L9" "打开源码"
    H["RuntimeGroupBuilder.Rebuild()"]
    click H "../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L16" "打开源码"
    I["PlayManager.RefreshGroup()"]
    click I "../Assets/Scripts/Features/Simulation/PlayManager.cs#L193" "打开源码"
    J["SiegePerformanceCapture.BeforeReload()"]
    click J "../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L197" "打开源码"
    K["SiegeTopologyBaseline.Run()"]
    click K "../Assets/Editor/Siege/Benchmarks/SiegeTopologyBaseline.cs#L13" "打开源码"
    A -.->|Editor update| B
    B --> C
    B --> F
    B -->|超时或停止| D
    J --> D
    I --> E
    I -->|经AssignBlocksToParentGroups| H
    H --> E
    H --> G
    G --> E
    K -->|隔离夹具重复同步调用| G
```

- [SiegePerformanceCapture.StartCapture](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L35)
<!-- source: Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs|StartCapture -->
- [SiegePerformanceCapture.Tick](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L77)
<!-- source: Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs|Tick -->
- [SiegePerformanceCapture.TakeCensus](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L114)
<!-- source: Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs|TakeCensus -->
- [SiegePerformanceCapture.Finish](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L146)
<!-- source: Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs|Finish -->
- [SimulationWorkMetrics.Measure](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L64)
<!-- source: Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs|Measure -->
- [SimulationWorkMetrics.Read](../Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs#L69)
<!-- source: Assets/Scripts/Features/Simulation/Diagnostics/SimulationWorkMetrics.cs|Read -->
- [BlockGroupManager.GroupBlocks](../Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs#L9)
<!-- source: Assets/Scripts/Features/Simulation/Groups/BlockGroupManager.cs|GroupBlocks -->
- [RuntimeGroupBuilder.Rebuild](../Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs#L16)
<!-- source: Assets/Scripts/Features/Simulation/Groups/RuntimeGroupBuilder.cs|Rebuild -->
- [PlayManager.RefreshGroup](../Assets/Scripts/Features/Simulation/PlayManager.cs#L193)
<!-- source: Assets/Scripts/Features/Simulation/PlayManager.cs|RefreshGroup -->
- [SiegePerformanceCapture.BeforeReload](../Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs#L197)
<!-- source: Assets/Editor/Siege/Benchmarks/SiegePerformanceCapture.cs|BeforeReload -->
- [SiegeTopologyBaseline.Run](../Assets/Editor/Siege/Benchmarks/SiegeTopologyBaseline.cs#L13)
<!-- source: Assets/Editor/Siege/Benchmarks/SiegeTopologyBaseline.cs|Run -->

## 阵地战压力资产制作

源码推导：首次创建独立压力Scene/配置，已有资产不覆盖；2048布局细分237块2m装甲并加一块受支撑顶层块，保留核心与炮塔，但独立耐久和损毁分量不同，不能作为等价战况。Editor API保存后恢复原活动场景；独立构建不改变全局构建列表，Player按配置核对完整模块数。

```mermaid
flowchart TD
    A["SiegeStressAuthoring.Create()"]
    click A "../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L18" "打开源码"
    B["SiegeStressAuthoring.CreateLarge()"]
    click B "../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L24" "打开源码"
    C["SiegeStressAuthoring.CreateProfile()"]
    click C "../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L31" "打开源码"
    D["SiegeStressAuthoring.GenerateLargeBlueprint()"]
    click D "../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L124" "打开源码"
    E["WorldBuildingValidation.ValidateBlueprint()"]
    click E "../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L11" "打开源码"
    F["SiegePlayerBuild.BuildLargeStress()"]
    click F "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L32" "打开源码"
    G["SiegePlayerBuild.Schedule()"]
    click G "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L63" "打开源码"
    H["SiegePlayerBuild.BuildNow()"]
    click H "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L83" "打开源码"
    A --> C
    B --> A
    B --> C
    C -->|首次创建大基地配置| D
    D --> E
    F --> B
    F --> G
    G -.->|一次性Editor update| H
```

- [SiegeStressAuthoring.Create](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L18)
<!-- source: Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs|Create -->
- [SiegeStressAuthoring.CreateLarge](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L24)
<!-- source: Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs|CreateLarge -->
- [SiegeStressAuthoring.CreateProfile](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L31)
<!-- source: Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs|CreateProfile -->
- [SiegeStressAuthoring.GenerateLargeBlueprint](../Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs#L124)
<!-- source: Assets/Editor/Siege/Authoring/SiegeStressAuthoring.cs|GenerateLargeBlueprint -->
- [WorldBuildingValidation.ValidateBlueprint](../Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs#L11)
<!-- source: Assets/Scripts/Features/World/Buildings/Placement/WorldBuildingValidation.cs|ValidateBlueprint -->
- [SiegePlayerBuild.BuildLargeStress](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L32)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|BuildLargeStress -->
- [SiegePlayerBuild.Schedule](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L63)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|Schedule -->
- [SiegePlayerBuild.BuildNow](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L83)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|BuildNow -->

## 阵地战混合规模压力场

源码推导：独立Scene包含三个可编辑建筑锚点，分帧沿生产恢复链加载6144附加模块，与主基地合计8192建筑预算；六舰和十二敌舰另计，是否超过万模块以实际普查为准，不代表万动态刚体。加载时暂停HUD，成功/失败/销毁恢复交互；主基地核心仍是唯一失败目标。

```mermaid
flowchart TD
    A["SiegeMixedStressAuthoring.Create()"]
    click A "../Assets/Editor/Siege/Authoring/SiegeMixedStressAuthoring.cs#L15" "打开源码"
    B["SiegePlayerBuild.BuildMixedStress()"]
    click B "../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L53" "打开源码"
    C["SiegeStressPopulation.Start()"]
    click C "../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L40" "打开源码"
    D["WorldBuildingManager.RestoreBuildingRoutine()"]
    click D "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L145" "打开源码"
    E["WorldBuildingManager.HasCapacity()"]
    click E "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L200" "打开源码"
    F["SiegeStressPopulation.RestoreHud()"]
    click F "../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L154" "打开源码"
    G["SiegeStressPopulation.OnDestroy()"]
    click G "../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L138" "打开源码"
    H["WorldBuildingManager.RemoveBuilding()"]
    click H "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L211" "打开源码"
    B --> A
    C --> E
    C -.->|显式推进分帧迭代器| D
    C -->|finally| F
    G --> F
    G -->|仅拥有的卫星| H
```

- [SiegeMixedStressAuthoring.Create](../Assets/Editor/Siege/Authoring/SiegeMixedStressAuthoring.cs#L15)
<!-- source: Assets/Editor/Siege/Authoring/SiegeMixedStressAuthoring.cs|Create -->
- [SiegePlayerBuild.BuildMixedStress](../Assets/Editor/Siege/Player/SiegePlayerBuild.cs#L53)
<!-- source: Assets/Editor/Siege/Player/SiegePlayerBuild.cs|BuildMixedStress -->
- [SiegeStressPopulation.Start](../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L40)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs|Start -->
- [WorldBuildingManager.RestoreBuildingRoutine](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L145)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|RestoreBuildingRoutine -->
- [WorldBuildingManager.HasCapacity](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L200)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|HasCapacity -->
- [SiegeStressPopulation.RestoreHud](../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L154)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs|RestoreHud -->
- [SiegeStressPopulation.OnDestroy](../Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs#L138)
<!-- source: Assets/Scripts/Features/Siege/Validation/SiegeStressPopulation.cs|OnDestroy -->
- [WorldBuildingManager.RemoveBuilding](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L211)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|RemoveBuilding -->

## 阵地战敌方碎片预算

源码推导：会话每半秒检查同场景敌方无驾驶舱组及每个模块归属；按首次观察时间优先退场，超过组数/模块软预算或寿命且已过共用最短保留期时，每次最多回收配置组数。复用既有幂等掉落转换，先停用再延迟销毁。友舰维修件、建筑、存活驾驶舱和其他场景不参与。

```mermaid
flowchart TD
    A["SiegeSession.Update()"]
    click A "../Assets/Scripts/Features/Siege/SiegeSession.cs#L374" "打开源码"
    B["SiegeWreckBudget.Tick()"]
    click B "../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L34" "打开源码"
    C["SiegeWreckBudget.Eligible()"]
    click C "../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L111" "打开源码"
    D["WreckSalvage.ConvertGroup()"]
    click D "../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L63" "打开源码"
    E["WreckSalvage.Convert()"]
    click E "../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10" "打开源码"
    A --> B
    B --> C
    B -->|预算或寿命到期| D
    D --> E
```

- [SiegeSession.Update](../Assets/Scripts/Features/Siege/SiegeSession.cs#L374)
<!-- source: Assets/Scripts/Features/Siege/SiegeSession.cs|Update -->
- [SiegeWreckBudget.Tick](../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L34)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs|Tick -->
- [SiegeWreckBudget.Eligible](../Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs#L111)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWreckBudget.cs|Eligible -->
- [WreckSalvage.ConvertGroup](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L63)
<!-- source: Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs|ConvertGroup -->
- [WreckSalvage.Convert](../Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs#L10)
<!-- source: Assets/Scripts/Features/Salvage/Drops/WreckSalvage.cs|Convert -->

## 阵地战任务目标与自主索敌

源码推导：波次明确指派的有效基地任务目标不受自主索敌半径限制，远程任务也生成接近输入；未指定任务仍只发现半径内敌对单位。所有目标继续校验启用、阵营、运行/模拟状态、场景和守卫范围；物理步即时丢弃失效目标，自主索敌错峰并带距离滞后，警戒返航避障同样降频。FixedUpdate只提交平滑意图，实际施力仍由推进器和电网负责。

```mermaid
flowchart TD
    A["SiegeWaveDirector.Tick()"]
    click A "../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46" "打开源码"
    B["EnemyController.SetPriorityTarget()"]
    click B "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L275" "打开源码"
    C["EnemyController.FixedUpdate()"]
    click C "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117" "打开源码"
    D["EnemyController.FindNearestOpponent()"]
    click D "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L282" "打开源码"
    E["EnemyController.IsValidOpponent()"]
    click E "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L333" "打开源码"
    F["EnemyController.CalculateDesiredMovement()"]
    click F "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L189" "打开源码"
    G["ControlUnit.SetMovementInput()"]
    click G "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L283" "打开源码"
    H["EnemyController.GetApproachDirection()"]
    click H "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L217" "打开源码"
    I["EnemyController.StabilizeDirection()"]
    click I "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L243" "打开源码"
    J["EnemyController.ApplyObstacleAvoidance()"]
    click J "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L442" "打开源码"
    K["EnemyObstacleClimb.TryGetHeight()"]
    click K "../Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs#L23" "打开源码"
    L["EnemyController.ConfigureHoverThrusters()"]
    click L "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L367" "打开源码"
    M["EnemyObstacleClimb.Blocked()"]
    click M "../Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs#L142" "打开源码"
    N["EnemyObstacleClimb.GetHullBounds()"]
    click N "../Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs#L120" "打开源码"
    O["EnemyController.ProbeObstacle()"]
    click O "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L489" "打开源码"
    A -->|生成或经Attacker.RefreshTarget重新绑定| B
    C -->|目标更新周期| D
    D --> E
    C -->|有目标且到移动周期| F
    C -->|每物理步平滑| G
    F -->|接近目标| H
    C -->|警戒返航| H
    J --> I
    F --> J
    J -->|整舰边界与速度预测| N
    K --> N
    J --> O
    H -->|先检查静态障碍翻越| K
    K -->|整舰上升与高处通道净空| M
    C -->|决策后逐物理步应用高度| L
    K -.->|爬升期间减小前进并保持舰尾净空| L
```

- [SiegeWaveDirector.Tick](../Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs#L46)
<!-- source: Assets/Scripts/Features/Siege/Battle/SiegeWaveDirector.cs|Tick -->
- [EnemyController.SetPriorityTarget](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L275)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|SetPriorityTarget -->
- [EnemyController.FixedUpdate](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|FixedUpdate -->
- [EnemyController.FindNearestOpponent](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L282)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|FindNearestOpponent -->
- [EnemyController.IsValidOpponent](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L333)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|IsValidOpponent -->
- [EnemyController.CalculateDesiredMovement](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L189)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|CalculateDesiredMovement -->
- [ControlUnit.SetMovementInput](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L283)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|SetMovementInput -->
- [EnemyController.GetApproachDirection](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L217)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|GetApproachDirection -->
- [EnemyController.StabilizeDirection](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L243)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|StabilizeDirection -->
- [EnemyController.ApplyObstacleAvoidance](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L442)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|ApplyObstacleAvoidance -->
- [EnemyObstacleClimb.TryGetHeight](../Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs#L23)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs|TryGetHeight -->
- [EnemyController.ConfigureHoverThrusters](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L367)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|ConfigureHoverThrusters -->
- [EnemyObstacleClimb.Blocked](../Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs#L142)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs|Blocked -->
- [EnemyObstacleClimb.GetHullBounds](../Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs#L120)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyObstacleClimb.cs|GetHullBounds -->
- [EnemyController.ProbeObstacle](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L489)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|ProbeObstacle -->

## 炮塔视线与遮挡锁定

源码推导：炮塔每物理步验证炮口视线，仅锁定可见敌对模块；自身炮塔碰撞忽略，同组墙板和其他单位遮挡。

```mermaid
flowchart TD
    A["Turret.FixedUpdate()"]
    click A "../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71" "打开源码"
    B["Turret.RefreshTarget()"]
    click B "../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L107" "打开源码"
    C["TurretTargetSelector.ConfigureVisibility()"]
    click C "../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L20" "打开源码"
    D["TurretTargetSelector.HasLineOfSight()"]
    click D "../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L26" "打开源码"
    E["TurretTargetSelector.FindNearestTarget()"]
    click E "../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L91" "打开源码"
    A --> B
    B --> C
    B --> D
    B -->|索敌周期| E
    E --> D
```

- [Turret.FixedUpdate](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L71)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Turret.cs|FixedUpdate -->
- [Turret.RefreshTarget](../Assets/Scripts/Features/Combat/Turrets/Turret.cs#L107)
<!-- source: Assets/Scripts/Features/Combat/Turrets/Turret.cs|RefreshTarget -->
- [TurretTargetSelector.ConfigureVisibility](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L20)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs|ConfigureVisibility -->
- [TurretTargetSelector.HasLineOfSight](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L26)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs|HasLineOfSight -->
- [TurretTargetSelector.FindNearestTarget](../Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs#L91)
<!-- source: Assets/Scripts/Features/Combat/Turrets/TurretTargetSelector.cs|FindNearestTarget -->

## EnemyController 可编辑避障试验场

源码推导：EnemyAvoidanceLabAuthoring.Create 保存舰体、静态目标、服务、四套互斥障碍与摄像机；EnableDebug 保存根Prefab和玩法场景调试开关。EnemyAvoidanceLab.Start 接入PlayManager和真实推进器，ResetTrial仅在新试次重置刚体/意图；正常运行不写舰体位姿或外加推力。FixedUpdate记录位移/距离/超时；EnemyAvoidanceContact只计数实体障碍接触。BeginSuite连续运行四布局，结果写Temp。EnemyController每0.5秒采样局部障碍、锁定绕行侧、按角度死区和冷却限制方向切换；逐物理步抵消侧滑，目标扫描仍独立降频。

```mermaid
flowchart TD
    A["EnemyAvoidanceLabAuthoring.Create()"]
    click A "../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L13" "打开源码"
    B["EnemyAvoidanceLabAuthoring.EnableDebug()"]
    click B "../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L219" "打开源码"
    C["EnemyAvoidanceLab.Start()"]
    click C "../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L39" "打开源码"
    D["EnemyAvoidanceLab.ResetTrial()"]
    click D "../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L51" "打开源码"
    E["EnemyAvoidanceLab.FixedUpdate()"]
    click E "../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L92" "打开源码"
    F["EnemyAvoidanceLab.BeginSuite()"]
    click F "../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L134" "打开源码"
    G["EnemyAvoidanceContact.OnCollisionEnter()"]
    click G "../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceContact.cs#L9" "打开源码"
    H["EnemyAvoidanceLab.RecordContact()"]
    click H "../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L126" "打开源码"
    I["EnemyController.GetApproachDirection()"]
    click I "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L217" "打开源码"
    J["EnemyController.StabilizeDirection()"]
    click J "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L243" "打开源码"
    K["EnemyController.GetMovementInterval()"]
    click K "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L232" "打开源码"
    L["EnemyController.GetVelocityInput()"]
    click L "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L237" "打开源码"
    M["EnemyController.FixedUpdate()"]
    click M "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117" "打开源码"
    N["EnemyController.ApplyObstacleAvoidance()"]
    click N "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L442" "打开源码"
    A -.->|保存可编辑场景| C
    B -.->|序列化调试配置| I
    C --> D
    F --> D
    M -->|逐物理步补偿速度| L
    M --> K
    M -->|经目标或警戒移动计算| I
    M -->|经目标或警戒移动计算| N
    N --> J
    E -->|到达或超时后切下一布局| D
    G --> H
    K -.->|驾驶采样周期| I
```

- [EnemyAvoidanceLabAuthoring.Create](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L13)
<!-- source: Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs|Create -->
- [EnemyAvoidanceLabAuthoring.EnableDebug](../Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs#L219)
<!-- source: Assets/Editor/Combat/EnemyAvoidanceLabAuthoring.cs|EnableDebug -->
- [EnemyAvoidanceLab.Start](../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L39)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs|Start -->
- [EnemyAvoidanceLab.ResetTrial](../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L51)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs|ResetTrial -->
- [EnemyAvoidanceLab.FixedUpdate](../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L92)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs|FixedUpdate -->
- [EnemyAvoidanceLab.BeginSuite](../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L134)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs|BeginSuite -->
- [EnemyAvoidanceContact.OnCollisionEnter](../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceContact.cs#L9)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceContact.cs|OnCollisionEnter -->
- [EnemyAvoidanceLab.RecordContact](../Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs#L126)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyAvoidanceLab.cs|RecordContact -->
- [EnemyController.GetApproachDirection](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L217)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|GetApproachDirection -->
- [EnemyController.StabilizeDirection](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L243)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|StabilizeDirection -->
- [EnemyController.GetMovementInterval](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L232)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|GetMovementInterval -->
- [EnemyController.GetVelocityInput](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L237)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|GetVelocityInput -->
- [EnemyController.FixedUpdate](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|FixedUpdate -->
- [EnemyController.ApplyObstacleAvoidance](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L442)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|ApplyObstacleAvoidance -->

## 友敌 AI 调试绘制

源码推导：EnemyController 同时驱动友方自主舰和敌机。DebugManager 的友方/敌方开关可在场景中预配，EnemyController 保留逐实例 Draw Debug；启用后仅在模拟运行且未被玩家驾驶时绘制。FixedUpdate 的移动周期清空探测快照，ProbeObstacle 记录实际整舰BoxCast终点与有效法线；绘制只读快照，不重新查询物理或改变驾驶输入。禁用控制器清空快照。

```mermaid
flowchart TD
    A["EnemyController.FixedUpdate()"]
    click A "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117" "打开源码"
    B["EnemyController.ApplyObstacleAvoidance()"]
    click B "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L442" "打开源码"
    C["EnemyController.ProbeObstacle()"]
    click C "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L489" "打开源码"
    D["EnemyController.OnDrawGizmos()"]
    click D "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L631" "打开源码"
    E["EnemyController.DrawDebug()"]
    click E "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L636" "打开源码"
    F["EnemyController.DrawDebugCircle()"]
    click F "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L684" "打开源码"
    G["EnemyController.OnDisable()"]
    click G "../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L348" "打开源码"
    A -->|经移动计算或警戒返航| B
    B --> C
    D --> E
    E -->|接近及撤退距离| F
    C -.->|缓存整舰扫掠结果| E
    G -.->|清空缓存| E
```

- [EnemyController.FixedUpdate](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L117)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|FixedUpdate -->
- [EnemyController.ApplyObstacleAvoidance](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L442)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|ApplyObstacleAvoidance -->
- [EnemyController.ProbeObstacle](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L489)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|ProbeObstacle -->
- [EnemyController.OnDrawGizmos](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L631)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|OnDrawGizmos -->
- [EnemyController.DrawDebug](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L636)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|DrawDebug -->
- [EnemyController.DrawDebugCircle](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L684)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|DrawDebugCircle -->
- [EnemyController.OnDisable](../Assets/Scripts/Features/Combat/Enemies/EnemyController.cs#L348)
<!-- source: Assets/Scripts/Features/Combat/Enemies/EnemyController.cs|OnDisable -->

## 显式组件与运行对象模板

源码推导：常驻驱动、对象池、文字样式与诊断组件由各场景 Managers 序列化持有；AI 参数预挂 BlocksParent，RefreshChildren 仅在驾驶资格改变时启停，不打断阵地战 Park。对象数量与几何动态变化，组件组成由 Runtime/Objects 模板保存。建筑与地形仍按既有异步生命周期组装和释放；体素碎片的逐格碰撞体及第三方诊断观测器是明确例外。Editor Bake 补齐资产依赖、保存场景与模板，重复执行保留已有参数和引用。

```mermaid
flowchart TD
    A["EditableComponentBaker.Bake()"]
    click A "../Assets/Editor/Architecture/EditableComponentBaker.cs#L21" "打开源码"
    B["EditableComponentBaker.BakeTemplates()"]
    click B "../Assets/Editor/Architecture/EditableComponentBaker.cs#L197" "打开源码"
    C["EditableComponentBaker.MigrateTree()"]
    click C "../Assets/Editor/Architecture/EditableComponentBaker.cs#L316" "打开源码"
    D["EditableComponentBaker.ConfigureServices()"]
    click D "../Assets/Editor/Architecture/EditableComponentBaker.cs#L478" "打开源码"
    E["ControlUnit.RefreshChildren()"]
    click E "../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185" "打开源码"
    F["WorldBuildingManager.Stage()"]
    click F "../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L247" "打开源码"
    G["ProceduralVoxelWorld.Generate()"]
    click G "../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101" "打开源码"
    H["VfxEffectPool.Rent()"]
    click H "../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L25" "打开源码"
    I["SceneObjectQuery.FindInScene()"]
    click I "../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L15" "打开源码"
    J["SiegeThreatMarkers.Build()"]
    click J "../Assets/Scripts/Features/Siege/Interface/SiegeThreatMarkers.cs#L52" "打开源码"
    A --> B
    A --> C
    A --> D
    D -->|查找场景服务| I
    H -->|池缓存失效时| I
    C -.->|保存的 AI 组件| E
    B -.->|保存的建筑模板| F
    B -.->|保存的地形模板| G
    C -.->|保存的标记池| J
```

- [EditableComponentBaker.Bake](../Assets/Editor/Architecture/EditableComponentBaker.cs#L21)
<!-- source: Assets/Editor/Architecture/EditableComponentBaker.cs|Bake -->
- [EditableComponentBaker.BakeTemplates](../Assets/Editor/Architecture/EditableComponentBaker.cs#L197)
<!-- source: Assets/Editor/Architecture/EditableComponentBaker.cs|BakeTemplates -->
- [EditableComponentBaker.MigrateTree](../Assets/Editor/Architecture/EditableComponentBaker.cs#L316)
<!-- source: Assets/Editor/Architecture/EditableComponentBaker.cs|MigrateTree -->
- [EditableComponentBaker.ConfigureServices](../Assets/Editor/Architecture/EditableComponentBaker.cs#L478)
<!-- source: Assets/Editor/Architecture/EditableComponentBaker.cs|ConfigureServices -->
- [ControlUnit.RefreshChildren](../Assets/Scripts/Features/Simulation/Units/ControlUnit.cs#L185)
<!-- source: Assets/Scripts/Features/Simulation/Units/ControlUnit.cs|RefreshChildren -->
- [WorldBuildingManager.Stage](../Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs#L247)
<!-- source: Assets/Scripts/Features/World/Buildings/WorldBuildingManager.cs|Stage -->
- [ProceduralVoxelWorld.Generate](../Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs#L101)
<!-- source: Assets/Scripts/Features/World/Terrain/ProceduralVoxelWorld.cs|Generate -->
- [VfxEffectPool.Rent](../Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs#L25)
<!-- source: Assets/Scripts/Features/Presentation/Effects/VfxEffectPool.cs|Rent -->
- [SceneObjectQuery.FindInScene](../Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs#L15)
<!-- source: Assets/Scripts/Features/Bootstrap/SceneObjectQuery.cs|FindInScene -->
- [SiegeThreatMarkers.Build](../Assets/Scripts/Features/Siege/Interface/SiegeThreatMarkers.cs#L52)
<!-- source: Assets/Scripts/Features/Siege/Interface/SiegeThreatMarkers.cs|Build -->

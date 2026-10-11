# 职责：由实际 Roslyn 符号生成函数导航；图规格在本文件按功能维护。
# 入口：python Tools/CodeReadability/generate_navigation.py
from pathlib import Path
import json, re, subprocess


def main():
    """Write navigation from the current source inventory and reviewed graph specifications."""
    repo = Path(__file__).resolve().parents[2]
    result = subprocess.run(
        [
            "dotnet",
            "run",
            "--project",
            str(repo / "Tools/CodeReadability"),
            "--",
            "--inventory",
            str(repo),
        ],
        capture_output=True,
        text=True,
        encoding="utf-8",
        check=True,
    )
    inventory = json.loads(result.stdout.lstrip("\ufeff"))
    symbols = {}
    for script in inventory:
        for m in script["methods"]:
            symbols.setdefault(m["type"] + "." + m["name"], (script["file"], m["line"]))

    def link(symbol):
        file, line = symbols[symbol]
        return f"[{symbol}](../{file}#L{line})"

    graphs = []

    def graph(title, purpose, nodes, edges):
        code = ["flowchart TD"]
        nav = []
        for key, symbol in nodes.items():
            assert symbol in symbols, symbol
            file, line = symbols[symbol]
            code.append(f'    {key}["{symbol}()"]')
            code.append(f'    click {key} "../{file}#L{line}" "打开源码"')
            nav.append(
                f'- {link(symbol)}\n<!-- source: {file}|{symbol.split(".")[-1]} -->'
            )
        code.extend("    " + edge for edge in edges)
        graphs.append(
            f"## {title}\n\n{purpose}\n\n```mermaid\n"
            + "\n".join(code)
            + "\n```\n\n"
            + "\n".join(nav)
            + "\n"
        )

    graph(
        "启动与建造/游玩会话",
        "引擎自动调用入口独立；按按钮进入业务主链。返回建造前先保存货仓。选择方块资源进入 BuildType.Build；InputManager 切换 lockView 仅取消拖拽/预览并隐藏操作轴，保留选中方块、高亮、BuildType 与资源，重锁恢复操作轴。",
        {
            "A": "GameManager.Start",
            "B": "GameManager.Init",
            "C": "SaveUIPanel.RefreshList",
            "D": "SaveManager.LoadSave",
            "E": "BuildManager.LoadAllBlocks",
            "F": "MainUIPanels.PlayStart",
            "G": "PlayManager.CanStartPlay",
            "H": "PlayManager.PlayStart",
            "I": "PlayManager.RefreshGroup",
            "J": "EnemySpawner.BeginPlayMode",
            "K": "MainUIPanels.PlayEnd",
            "L": "CargoPersistence.SaveReturnCargo",
            "M": "PlayManager.PlayEnd",
            "N": "MainUIPanels.PlayerDeath",
        },
        [
            "A --> B",
            "B --> C",
            "B --> D",
            "D --> E",
            "F --> G",
            "F -->|满足条件| H",
            "H --> I",
            "H --> J",
            "K --> L",
            "K -->|保存成功| M",
            "M -->|经 GameManager.Init| D",
            "N -->|用户重生/结束按钮| K",
        ],
    )
    graph(
        "蓝图恢复、统计和取消",
        "Load 先验证文件，再替换父对象；协程按请求版本检查所有权。统计事件指向显示。取消使旧版本失效；缺资源移出缓存和JSON。",
        {
            "A": "BuildManager.LoadAllBlocks",
            "B": "BlueprintLoader.Load",
            "C": "BlueprintLoader.TryRead",
            "D": "BlueprintLoader.ReplaceParent",
            "E": "BlueprintLoadCamera.Begin",
            "F": "BlueprintLoader.LoadRoutine",
            "G": "BlueprintLoader.OwnsParent",
            "H": "BlueprintBlockFactory.TryRestore",
            "I": "BlueprintStatistics.Accumulate",
            "J": "BlueprintUIPanel.DisplayStatistics",
            "K": "BlueprintLoader.RemoveMissingData",
            "L": "BlueprintLoader.RebuildConnections",
            "M": "BlueprintLoader.FinishSession",
            "N": "BlueprintLoader.Cancel",
            "O": "BlueprintLoadCamera.Stop",
        },
        [
            "A --> B",
            "B --> C",
            "B -->|文件有效| D",
            "B --> E",
            "B -.->|StartCoroutine| F",
            "F --> G",
            "F --> H",
            "F -->|恢复成功| I",
            "F -.->|StatisticsChanged| J",
            "F -->|完成| K",
            "F --> L",
            "F -->|finally| M",
            "N -->|版本递增 / 停协程| M",
            "M --> O",
        ],
    )
    graph(
        "放置、选中、操作和持久化",
        "Update 按输入模式调用预览或选择；预览确认放置调用显式注入的创建动作。输入/Undo、几何规则和JSON各有审查位置。",
        {
            "A": "BuildManager.Update",
            "B": "BuildManager.HandleBuildingPreview",
            "C": "BuildPreview.Poll",
            "D": "BuildPlacementGeometry.TryCalculateConnectorAlignedPosition",
            "E": "BuildPlacementGeometry.IsBlocked",
            "F": "BuildManager.CreateBlock",
            "G": "BuildManager.SaveBlock",
            "H": "BuildBlueprintEditor.Save",
            "I": "BuildBlueprintEditor.RefreshConnectionsAround",
            "J": "BuildSelection.Poll",
            "K": "BuildSelection.SelectBlock",
            "L": "BuildManipulation.Poll",
            "M": "ActionManager.Push",
            "N": "ActionManager.Undo",
            "O": "ActionManager.Redo",
            "P": "Block.CheckConnection",
        },
        [
            "A --> B",
            "B --> C",
            "C --> D",
            "C --> E",
            "C -.->|确认后调用注入动作| F",
            "F --> G",
            "G --> H",
            "H --> I",
            "I --> P",
            "F --> M",
            "A --> J",
            "J --> K",
            "A --> L",
            "L -.->|保存动作| G",
            "L --> M",
            "A --> N",
            "A --> O",
        ],
    )
    graph(
        "供电网络与可视范围",
        "每帧仅一个中继重建共享网络；先清零再提交本帧汇总。DebugManager 的范围表现与网络规则分开。",
        {
            "A": "PowerTransmissionDevice.Update",
            "B": "PowerNetwork.Rebuild",
            "C": "PowerNetwork.ResetConnectionsAndPower",
            "D": "PowerNetwork.BuildConnections",
            "E": "PowerNetwork.DistributeNetworkPower",
            "F": "PowerNetwork.CollectConnectedComponent",
            "G": "PowerNetwork.SupplyConnectedComponent",
            "H": "PowerNetwork.CommitPowerStatuses",
            "I": "Power.CommitPowerStatus",
            "J": "PowerConnectionVisuals.Refresh",
            "K": "PowerTransmissionDevice.OnDisable",
            "L": "PowerNetwork.Clear",
            "M": "DebugManager.RefreshPowerRangeMesh",
            "N": "PowerRangeUnionView.Refresh",
            "O": "DebugManager.TogglePowerRange",
            "P": "PowerRangeUnionView.Hide",
            "Q": "DebugManager.OnDestroy",
            "R": "PowerRangeUnionView.Dispose",
        },
        [
            "A -->|首个本帧中继| B",
            "B --> C",
            "B --> D",
            "B --> E",
            "E --> F",
            "E --> G",
            "B --> H",
            "H --> I",
            "A --> J",
            "K -->|最后中继| L",
            "A -->|完成网络后| M",
            "M --> N",
            "O -->|关闭范围显示| P",
            "Q --> R",
        ],
    )
    graph(
        "物理分组、连接与推进",
        "连接图是组重建输入；重组先捕获刚体速度，再转移方块与运行归属。三个 FixedUpdate 由引擎调度，图中无先后承诺。",
        {
            "A": "PlayManager.RefreshGroup",
            "B": "PlayManager.AssignBlocksToParentGroups",
            "C": "RuntimeGroupBuilder.Rebuild",
            "D": "BlockGroupManager.GroupBlocks",
            "E": "BlockConnections.CollectNeighbors",
            "F": "RuntimeGroupBuilder.RestoreGroupPhysics",
            "G": "ControlUnit.RefreshChildren",
            "H": "PlayManager.FixedUpdate",
            "I": "RuntimeGroupCleanup.EnforceDistance",
            "J": "HoverFlightController.FixedUpdate",
            "K": "HoverFlightController.DistributeThrust",
            "L": "MainThruster.FixedUpdate",
            "M": "Thruster.ApplyThrustChangeRateLimit",
            "N": "HoverControlSolver.StepHeight",
            "O": "HoverControlSolver.CalculateTilt",
            "P": "HoverFlightController.StopControlOutput",
            "Q": "HoverControlSolver.Reset",
            "R": "HoverFlightController.ClearHoverThrust",
            "S": "HoverFlightController.OnDisable",
        },
        [
            "A --> B",
            "B --> C",
            "C --> D",
            "D -->|BFS 经 Block.CollectNeighbors| E",
            "C --> F",
            "C --> G",
            "H --> I",
            "J -->|有效游玩/供电/归属| N",
            "J --> O",
            "J --> K",
            "K --> M",
            "L --> M",
            "J -->|停止或失效| P",
            "S --> P",
            "P --> Q",
            "P --> R",
        ],
    )
    graph(
        "战斗、损伤、爆炸与残骸清理",
        "射击冲量先于伤害；爆炸断链后重组再施加受限冲量。延迟清理在真正删除前再次检查身份与驾驶舱。",
        {
            "A": "TurretWeapon.FixedUpdate",
            "B": "TurretTargetSelector.FindNearestTarget",
            "C": "TurretHitscan.Fire",
            "D": "Durability.ApplyDamage",
            "E": "ControlUnit.OnCollisionEnter",
            "F": "CollisionDamageResolver.ApplyContactDamage",
            "G": "Durability.ApplyCollisionEnergy",
            "H": "DestroyManager.DestroyGameObject",
            "I": "DestroyManager.ExplodeBlock",
            "J": "ExplosionPropagation.DisconnectBlocksInExplosionRange",
            "K": "PlayManager.RefreshGroup",
            "L": "ExplosionPropagation.ApplyExplosionForce",
            "M": "SalvageCleanupSession.ScheduleUnit",
            "N": "SalvageCleanupSession.CleanupUnitAfterDelay",
            "O": "CargoHold.ReleaseContents",
        },
        [
            "A --> B",
            "A -->|瞄准且冷却满足| C",
            "C --> D",
            "E --> F",
            "F --> G",
            "D -->|耐久为0| H",
            "G -->|耐久为0| H",
            "H -->|可爆炸且非碰撞致死| I",
            "I --> J",
            "I --> K",
            "I --> L",
            "H --> O",
            "H -->|敌方驾驶舱| M",
            "M -.->|协程等待并复核| N",
        ],
    )
    graph(
        "维修、回收和移动 Home 返航",
        "机器人使用 Home 实时停靠点与刚体点速度；任务与返航均从机体做净空检测，持续绕障并在停滞后脱困。取消任务释放认领。",
        {
            "A": "RepairBot.FixedUpdate",
            "B": "Bot.TickNavigation",
            "C": "RepairTargetSearch.Scan",
            "D": "Bot.NavigateToTarget",
            "E": "BotFlightMotion.NavigateToTarget",
            "F": "BotObstacleAvoidance.Evaluate",
            "G": "RepairBot.CheckAndRepair",
            "H": "RepairBeamView.Update",
            "I": "CollectionBot.FixedUpdate",
            "J": "CollectionBot.FindTarget",
            "K": "LootDrop.Claim",
            "L": "CollectionBot.CollectTarget",
            "M": "LootDrop.Carry",
            "N": "CollectionBot.ReturnCargo",
            "O": "Bot.NavigateHomeSmoothly",
            "P": "BotFlightMotion.NavigateHome",
            "Q": "BotObstacleAvoidance.ConstrainVelocity",
            "R": "Bot.ReturnHome",
            "S": "LootDrop.Deliver",
            "T": "CollectionBot.Abandon",
        },
        [
            "A --> B",
            "A -->|扫描目标时| C",
            "A --> D",
            "D --> E",
            "E -->|经 SampleGuidance| F",
            "E -->|每物理帧经 ApplyMovement| Q",
            "A --> G",
            "A --> H",
            "I --> B",
            "I --> J",
            "J --> K",
            "I --> L",
            "L --> D",
            "L --> M",
            "I --> N",
            "N --> O",
            "O --> P",
            "P -->|每物理帧经 ApplyReturnHomeMovement| Q",
            "P -->|机体净空与脱困| F",
            "O -->|可停靠| R",
            "N -->|完成停靠| S",
            "N --> T",
        ],
    )
    graph(
        "掉落概率、表现与文字遮挡",
        "敌方残骸先抽稀有部件，再抽金币；货仓存量不受爆率影响。Update散落/吸附，LateUpdate更新表现和与Icon共用的遮挡查询；Carry停止散落并还原显示姿态。",
        {
            "A": "WreckSalvage.Convert",
            "B": "LootDrop.Spawn",
            "C": "LootDrop.Initialize",
            "D": "LootDrop.Update",
            "E": "LootDrop.LateUpdate",
            "F": "CombatHud.IsWorldPointOccluded",
            "G": "LootDrop.Carry",
            "H": "LootDrop.ResetDisplayPose",
            "I": "LootDrop.Deliver",
            "J": "LootDrop.Release",
        },
        [
            "A -->|通过掉落抽签| B",
            "B -->|新建时| C",
            "D -->|货币吸附到达| I",
            "E -->|屏幕内文字| F",
            "E -->|携带或非游玩| H",
            "G --> H",
            "I --> J",
        ],
    )
    graph(
        "表现、镜头与 UI 生命周期",
        "游戏事件选择效果；临时表现自行按寿命释放。镜头取景是计算，输入及协程由 CameraController 持有。",
        {
            "A": "VisualEffectsManager.TryPlayBlockExplosion",
            "B": "VisualEffectsManager.PlayBlockExplosion",
            "C": "BlockVfxLibrary.Play",
            "D": "TransientEffects.CreateLightFlash",
            "E": "LightFade.Update",
            "F": "CameraShake.Play",
            "G": "CameraShake.CameraShakeRoutine",
            "H": "VisualEffectsManager.OnDisable",
            "I": "CameraShake.Cancel",
            "J": "CameraController.SmoothFocusCameraOnBlock",
            "K": "CameraFraming.TryGetFocusPose",
            "L": "CameraController.SmoothFocusRoutine",
            "M": "MainUIPanels.ShowCreatePanel",
            "N": "PanelTransitions.Show",
            "O": "PanelTransitions.Fade",
            "P": "MainUIPanels.OnDisable",
            "Q": "PanelTransitions.CancelAll",
        },
        [
            "A --> B",
            "B --> C",
            "B -->|成功生成| D",
            "D -.->|引擎Update| E",
            "B --> F",
            "F -.->|协程| G",
            "H --> I",
            "J --> K",
            "J -.->|协程| L",
            "M --> N",
            "N -.->|协程| O",
            "P --> Q",
        ],
    )
    doc = """# HY-Sandbox 运行时函数图

    这些图根据当前源码和调用者整理，用于寻找功能入口，不是采样得到的实际执行轨迹。实线表示直接调用（标注“经”时包含明确中间函数）；虚线标出协程、事件或注入动作。Unity Update/FixedUpdate 的独立入口不代表脚本执行顺序。

    每个图节点可点击对应源码；渲染器禁用 click 时使用图下链接。导航由 Roslyn 校验文件与方法存在，语义顺序由修改者审查。过载函数链接到第一个声明，完整过载见 CodeIndex。

    维护：业务变化更新受影响图和 DEVELOPMENT；纯格式变化重新生成行号。`dotnet run --project Tools/CodeReadability -- --check .` 会拒绝失效符号。

    """ + "\n".join(graphs)
    (repo / "Docs").mkdir(exist_ok=True)
    (repo / "Docs/RuntimeFlow.md").write_text(doc, encoding="utf-8")
    index = [
        "# 自有代码导航\n\n由 Roslyn 实际语法节点生成；脚本介绍定位职责，方法链接定位实现。不是行为测试报告。完整入口链见 [RuntimeFlow](RuntimeFlow.md)。\n"
    ]
    for x in sorted(inventory, key=lambda x: x["file"]):
        index.append(f'## {x["file"]}\n\n{x["introduction"]}\n')
        methods = x["methods"]
        if methods:
            index.append("| 类型 | 方法签名 | 可见性 |\n| --- | --- | --- |")
            for m in methods:
                sig = " ".join(m["signature"].split()).replace("|", "\\|")
                index.append(
                    f'| {m["type"]} | [`{sig}`](../{x["file"]}#L{m["line"]}) | {m["access"]} |'
                )
        else:
            index.append("数据/配置/接口定义，无方法体。")
        index.append("")
    (repo / "Docs/CodeIndex.md").write_text("\n".join(index), encoding="utf-8")
    print(len(graphs), "graphs;", len(inventory), "scripts indexed")


if __name__ == "__main__":
    main()

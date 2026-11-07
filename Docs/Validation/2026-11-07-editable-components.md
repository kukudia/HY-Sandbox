# 显式组件与运行对象模板迁移

日期：2026-11-07；Unity 6000.6.8f1，连接本项目 Editor。

## 交付与复现

- 场景常驻服务预放 Managers/服务层级；实体能力预挂 Prefab，运行时按资格启停。EnemyController 不再在 RefreshChildren 中添加，DebugManager 提供友敌全局调试开关。
- RuntimeObjectTemplates / Resources/Runtime/Objects.asset 提供动态数量对象的可编辑组成；WorkshopServices 为动态工坊场景显式提供灯光和场景服务。
- EditableComponentBaker.Bake（Tools/HY Sandbox/Architecture/Bake Editable Components）迁移场景和自有 Prefab；执行前在 Temp/EditableComponentsBackup 写带哈希清单的备份。重复 Bake 返回 0 additions，清单内资产字节变化为 0。
- RuntimeUnitMember 原先只有运行时类型，没有已保存的组件引用；拆分独立 MonoScript 后在模块上预挂。已有脚本/资产 GUID 保留。正式 Player 场景处理移除 Editor 诊断，Resources 中探针保留无行为类型以避免缺失脚本。
- 修正预挂后的生命周期：FragmentLifetime 从 Mark 开始保留期；烟迹停止后禁用且可重新初始化；ProjectileTracer 显式取得已有光束；体素/建筑碎片 Instantiate 时直接传世界位置和旋转；AI 层级刷新保留外部 Park 禁用状态。

## 已实际运行的验证

以下结果来自 Editor 写盘报告；CLI 超时本身不视为通过。相应检查由 EditableComponentChecks、SiegeBuildingChecks 等入口复现，原始会话输出位于 Temp/EditableComponentsChecks（不提交缓存）。

### assets

```text
PASS Assets/Scenes/Main.unity
PASS Assets/Scenes/Siege.unity
PASS Assets/Scenes/SiegeLargeStress.unity
PASS Assets/Scenes/SiegeMixedStress.unity
PASS Assets/Scenes/SiegeStress.unity
PASS Assets/Scenes/WorldLab.unity
PASS Assets/Art/Industrial/Preview/IndustrialArtPreview.unity
PASS 205 owned prefabs, 44 blocks, 7 saved scenes and all object template references
```

### play

```text
{
    "checks": [
        "Small distance crossing retains current target",
        "Clearly nearer opponent replaces current target",
        "Explicit mission overrides proximity hysteresis",
        "Inactive target is replaced before scheduled search",
        "Initial ground overlap does not reverse horizontal flight",
        "Trigger volumes do not redirect enemy flight",
        "Enemy avoidance queries its local PhysicsScene",
        "Dense hull query grows buffer and still avoids nearest obstacle",
        "Initial scan acquires original damage module",
        "Existing selector discovers newly repaired module",
        "Reparented damage module is removed from old target",
        "Same-step activation invalidates shared hierarchy snapshot",
        "Shared membership still reads live module positions",
        "Ancestor regroup cannot leave a module selectable by its old unit",
        "Projectile lead matches crossing target with shooter velocity",
        "Faction change invalidates turret target before scheduled scan",
        "Regrouped module immediately stops being the old turret aim target",
        "Out-of-range intercept falls back to direct aim",
        "Faster receding target has no intercept",
        "Equal-speed approaching target solves linear case",
        "Intercept beyond projectile lifetime is rejected"
    ]
}
{
    "checks": [
        "PASS shared factory restores exact module count",
        "PASS player faction is restored on cockpit and owner",
        "PASS legacy player cockpit defaults to player control",
        "PASS autonomous ally stays player faction without reading player input",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: HoverThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThrusterBig(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThruster(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThruster(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThruster(Clone)",
        "PASS autonomous thruster rejects manual control: UniversalThruster(Clone)",
        "PASS taking control immediately restores manual source",
        "PASS rigidbody mass equals restored module mass",
        "PASS spawn initializes rigidbody at requested world pose",
        "PASS legacy enemy factory preserves autonomous enemy behavior",
        "PASS legacy enemy identity and spawn health survive delegation",
        "PASS refresh does not reset cockpit control identity"
    ]
}
PASS: actual projectile pool reuses instance, clears tracer history, resets launch location and preserves velocity/collider.
PASS: 176 bursts + 40 trails without rejection/eviction; bounded idle cache; reused instance resets scale, graph pose, parameters, timer and meteor follower.
PASS authored smoke adapter activate / stop / reinitialize
PASS fragment retention starts at Mark, not prefab Awake
PASS transient beam/light templates and pool reuse
PASS faction debug switches and scene-owned service isolation
```

### workshop

```text
PASS workshop opens with scene-owned VFX, transient effects, motion and visual effects services
PASS workshop closes and restores original scene and UI without saving player data
```

### world

```text
PASS terrain generation instantiates chunk templates with valid render/collision meshes
PASS detached voxel template preserves compound colliders, culled internal faces and source collision removal
```

### world-motion

```text
PASS authored voxel debris falls across real Play frames
```

### player-scripts

```text
PASS: 37 Player script assemblies
```

SiegeBuildingChecks：20 项通过（Temp/Siege/building-checks.json）。Player 脚本编译使用 PlayerBuildInterface.CompilePlayerScripts、StandaloneWindows64 DevelopmentBuild；这不等于完整 Player 构建。

## 保留例外与范围

生产源码扫描 AddComponent / 带类型的 new GameObject：VoxelDebrisPiece 按实际体素逐格创建 BoxCollider；AbnormalImpulseRecorder 仅在 observeUnconfiguredBodies 显式开启时补挂外部刚体探针，默认关闭。Features 下 Validation 是隔离测试，不属于生产自举。Editor 制作/迁移工具仍可添加组件。

两份用户 unity-editable-workflow 技能已同步增加显式组件规则并通过 quick_validate；本地技能不属于项目 Git。修改前备份在用户 .codex/backups/unity-editable-components-20261107-155940-927，另建 Git 恢复分支。原有炮塔参数、管线/项目设置及 UserSettings 改动保留且不纳入本任务提交。

## 验证边界

已运行资产保存/重载审计及上述真实 Play 检查。未验证完整 Player 打包/运行、所有场景完整玩法、长时压力和全部视听效果。SDK 锁定 9.0.318 未安装，静态命令从项目外使用已安装 9.0.320，未修改 global.json。全仓 diff 检查中的原有布局尾随空白与本次提交检查分开记录。

最终静态检查：CodeReadability 369 scripts / 0 differences / 0 errors；生成 54 张函数图与 369 脚本导航；解决方案构建 0 错误、22 个既有警告。C# 源码统一 CRLF。任务暂存 diff 检查通过后提交。

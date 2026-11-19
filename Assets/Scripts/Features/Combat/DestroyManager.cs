// 职责：承接通用的销毁主链，按固定顺序协调耐久失效、移除表现、爆炸/断链、残骸回收、驾驶舱死亡与延迟清理。
// 入口：DestroyTarget() / DestroyGameObject() / ExplodeBlock()
// 以及 ScheduleUnitCleanup() / ScheduleDistantGroupCleanup() / EndSalvageSession()。
// 生命周期：各玩法场景各自挂载本组件，Awake() 覆盖静态 instance，因此静态引用只代表当前场景；清理协程由本对象承载。
// 副作用：禁用耐久、断开连接、脱离父级、移除对象、发放残骸掉落，并把胜负/击杀转交 Skirmish、Siege 与 MainUIPanels。
using UnityEngine;

public class DestroyManager : MonoBehaviour
{
    // 场景内唯一入口：Durability 在耐久归零时回调这里的实例；探针与战斗脚本也用它触发同一套销毁规则。
    // Centralizes destruction side effects so visuals, unit cleanup, and regrouping happen in a known order.
    public static DestroyManager instance;
    // 诊断开关：只影响日志输出，默认关闭，避免大规模战斗里每次销毁都产生堆栈与日志 I/O。
    [SerializeField, Tooltip("Emit per-object destruction diagnostics. Disabled by default to avoid stack traces and log I/O during large battles.")]
    private bool _debugDestruction;
    // 兜底清理延迟：SalvageSettings 缺失时才使用的默认秒数；真正生效的延迟由清理会话向配置查询。
    private float _unitCleanupDelay = 10f;
    // 以下四项是块爆炸的可编辑覆盖值，初值与 ExplosionPropagation 的共享默认值同源，保证不配置时行为与通用默认一致。
    // 向上抬升分量：在径向方向上叠加竖直分量，让碎片被掀离地面，而不是贴着地面水平飞散。
    [SerializeField]
    private float _blockExplosionUpwardsModifier = ExplosionPropagation.DefaultUpwardsModifier;
    // 冲量缩放：最终冲量取 min(爆炸力·距离衰减·缩放, 质量·最大速度变化)，让远近目标的差异保持可读。
    [SerializeField, Range(0f, 1f)]
    private float _explosionImpulseScale = ExplosionPropagation.DefaultImpulseScale;
    // 单次爆炸允许的最大速度变化：按刚体质量封顶冲量，防止轻碎片获得远超重部件的速度。
    [SerializeField, Min(0f)]
    private float _maximumExplosionSpeedChange = ExplosionPropagation.DefaultMaximumSpeedChange;
    // 爆炸后免碰撞伤害的秒数：冲量造成的即时接触不得再次结算耐久，避免一次爆炸连锁摧毁邻近模块。
    [SerializeField, Min(0f)]
    private float _explosionCollisionProtection = ExplosionPropagation.DefaultCollisionProtection;
    // 延迟清理会话：按原始单位 ID 去重并计划残骸回收；首次使用时以本组件作为协程宿主创建。
    private SalvageCleanupSession _cleanupSession;
    // 一次同步损毁批次在连通分组稳定后施加冲量；独立模拟宿主可给其单位提供同一实现的会话。
    private readonly ExplosionDamageSession explosions = new ExplosionDamageSession();
    private SalvageCleanupSession CleanupSession => _cleanupSession ?? (_cleanupSession = new SalvageCleanupSession(this));

    private void Awake()
    {
        // 不跨场景保留、也不自动创建：每个玩法场景各自登记自己的实例，新场景加载时覆盖旧引用。
        instance = this;
    }

    // 统一销毁入口：耐久归零、建筑结构损坏、探针与战斗脚本都从这里进入。
    // 顺序约定：先冻结耐久并快照归属，再播放表现、结算爆炸或断链、发放残骸，最后才处理驾驶舱死亡与延迟清理；
    // 这样断链产生的碎片已带上 ownerUnitId/faction，且流程中途重入也不会二次触发死亡结算。
    public void DestroyGameObject(GameObject obj)
    {
        DestroyTarget(obj);
    }

    public static void DestroyTarget(GameObject obj)
    {
        DestroyTarget(obj, false);
    }

    private static void DestroyTarget(GameObject obj, bool forceExplosion)
    {
        if (obj == null)
        {
            return;
        }

        Durability health = obj.GetComponent<Durability>();
        // 耐久已禁用表示对象已进入销毁流程或被其他系统显式关闭：两种情况下都不应再次结算一次死亡。
        if (health != null && !health.enabled)
        {
            return;
        }

        if (instance != null && instance._debugDestruction)
        {
            Debug.Log($"{obj.name} durability reached 0, destroying object.");
        }

        // 立即关闭耐久：随后断链、换父级、移除对象时的碰撞与伤害回调不应再进入本流程。
        // 只关闭回调，不清空 LastAttacker，死亡归属与击杀提示仍然可读。
        if (health != null)
        {
            health.enabled = false;
        }

        Cockpit cockpit = obj.GetComponent<Cockpit>();
        // 驾驶状态必须在移除前快照：对象随后会被销毁，死亡面板不能再依赖当时的控制权。
        bool playerWasDriving = cockpit != null && cockpit.ControlMode == UnitControlMode.Player;
        // 观战对局在模块脱离/重组前按驾驶舱身份认领死亡，避免误触玩家游戏结束。
        bool skirmishOwnsDeath = cockpit != null && PlayManager.instance != null && PlayManager.instance.Skirmish != null && PlayManager.instance.Skirmish.NotifyCockpitDestroyed(cockpit, health != null ? health.LastAttacker : null);
        // 战斗模式的敌方驾驶舱上报一次“被击败”，供据点/波次进度统计；它与玩家死亡面板互不替代。
        if (cockpit != null && cockpit.faction == UnitFaction.Enemy && PlayManager.instance != null && PlayManager.instance.playMode)
        {
            cockpit.GetComponent<EnemyIdentity>()?.ReportDefeated();
        }

        // 取父级单位并补齐运行时身份：碎片断链后仍是独立 ControlUnit，必须让每块记住原 ownerUnitId 与阵营，
        // 残骸归属、掉落与延迟清理都按这份快照工作；已有归属的块保留原值（overwriteExisting = false）。
        ControlUnit unit = obj.GetComponentInParent<ControlUnit>();
        if (unit != null)
        {
            unit.EnsureRuntimeUnitId();
            unit.AssignRuntimeOwnershipToBlocks(false);
        }

        // 归属快照：块上的 RuntimeUnitMember 记录断裂前所属单位，优先于当前父级单位，避免刚脱离的块被归到新组。
        RuntimeUnitMember member = obj.GetComponent<RuntimeUnitMember>();
        string ownerUnitId = member != null ? member.ownerUnitId : unit?.runtimeUnitId;
        // 阵营按 块记忆 → 驾驶舱 → 父级单位 → 敌方 依次回退；全部缺失时按敌方残骸处理，避免游离块被当成玩家资产。
        UnitFaction ownerFaction = member != null ? member.ownerFaction : cockpit != null ? cockpit.faction : unit != null ? unit.faction : UnitFaction.Enemy;
        Block block = obj.GetComponent<Block>();
        // 非爆炸驾驶舱沿用通用销毁表现，爆炸驾驶舱仍使用块爆裂效果。
        bool shouldExplodeCockpit = cockpit != null && block != null && PlayManager.instance != null && PlayManager.instance.playMode;
        // Explosive blocks react to every lethal damage source; fragment contacts are protected separately.
        bool simulated = unit != null || (PlayManager.instance != null && PlayManager.instance.playMode);
        bool willExplode = block != null && (block.canExplode || forceExplosion) && simulated;
        // 爆裂在断链前播放一次，这里只处理普通移除。
        if (!willExplode)
        {
            if (block != null && !shouldExplodeCockpit)
            {
                VisualEffectsManager.TryPlayBlockRemoved(block);
            }
            else
            {
                VisualEffectsManager.TryPlayObjectDestroyed(obj);
            }
        }

        // 工坊中尚未归属运行单位的对象只需普通移除。
        if (!simulated)
        {
            Destroy(obj);
            return;
        }

        // 先释放货舱与车内单位归属，再处理结构：内容必须在对象被移除前结算，否则运输/货物会随对象一起丢失；
        // 掉落奖励仍由清理阶段的 WreckSalvage.Convert 按阵营与爆率统一抽取。
        WreckSalvage.ReleaseContents(obj, ownerFaction);
        if (block != null)
        {
            ExplosionDamageSession session = unit?.DamageSession ?? instance?.explosions ?? new ExplosionDamageSession();
            Vector3 center = block.transform.position;
            if (willExplode)
            {
                block.ExplosionQueued = true;
                VisualEffectsManager.TryPlayBlockExplosion(block);
                ExplosionPropagation.DisconnectBlocksInExplosionRange(unit, block, center);
            }

            // 延迟 Destroy 前立刻移出连接图；存档和胜负监听者接收同一次失效事件。
            block.DisConnectAllConnectors();
            block.transform.SetParent(null, true);
            unit?.NotifyBlockDestroyed(block);
            block.gameObject.SetActive(false);
            if (willExplode)
            {
                DestroyManager settings = instance;
                session.ApplyExplosion(
                    block,
                    unit,
                    () => ExplosionPropagation.ApplyExplosionForce(
                        block,
                        ownerUnitId,
                        ownerFaction,
                        center,
                        settings != null ? settings._blockExplosionUpwardsModifier : ExplosionPropagation.DefaultUpwardsModifier,
                        settings != null ? settings._explosionImpulseScale : ExplosionPropagation.DefaultImpulseScale,
                        settings != null ? settings._maximumExplosionSpeedChange : ExplosionPropagation.DefaultMaximumSpeedChange,
                        settings != null ? settings._explosionCollisionProtection : ExplosionPropagation.DefaultCollisionProtection));
            }
            else
            {
                session.RefreshUnit(unit);
            }
        }

        if (cockpit != null && PlayManager.instance != null && PlayManager.instance.playMode)
        {
            // 两类对局分别认领驾驶舱死亡：观战（Skirmish）与攻城（Siege）自己结算胜负与 HUD；
            // 认领后不再触发玩家死亡面板，避免把观战/攻城对局误判为玩家失败。
            bool siegeOwnsDeath = PlayManager.instance.SiegeSession != null || PlayManager.instance.Modes?.State == GameModeState.Siege;
            if (PlayManager.instance.SiegeSession != null)
            {
                // 攻城记录舰队损失与返航：即使死亡的是玩家驾驶的驾驶舱，也由攻城会话决定本局是否继续。
                PlayManager.instance.SiegeSession.NotifyCockpitDestroyed(cockpit);
            }

            // 仅普通战斗里由玩家击杀的敌方驾驶舱弹出击杀提示；已被模式认领的死亡由模式自己的 HUD 负责。
            if (!skirmishOwnsDeath && !siegeOwnsDeath && PlayManager.instance.Modes?.State != GameModeState.Siege && ownerFaction == UnitFaction.Enemy && health != null && health.LastAttacker != null && health.LastAttacker.faction == UnitFaction.Player && unit != null)
            {
                EnemyIdentity identity = cockpit.GetComponent<EnemyIdentity>();
                MainUIPanels.instance?.CombatHud?.ShowPlayerKill(identity != null ? identity.DisplayName : unit.name);
            }

            Destroy(obj);
            // 先移除对象再切换界面：PlayerDeath 会关闭 playMode 并切到重生/建造输入，不能再依赖该对象存活。
            // 只有“玩家阵营 + 玩家亲自驾驶 + 未被对局认领”的驾驶舱才结束本局。
            if (ownerFaction == UnitFaction.Player && playerWasDriving && PlayManager.instance.playMode && !siegeOwnsDeath && !skirmishOwnsDeath)
            {
                //MainUIPanels.instance.PlayEnd();
                MainUIPanels.instance?.PlayerDeath();
                return;
            }

            // 驾驶舱已移除，但同单位碎片仍可能继续断裂；按 ownerUnitId 计划延迟清理，重复计划会被去重。
            if (instance != null)
            {
                instance.CleanupSession.ScheduleUnit(obj, ownerUnitId, instance._unitCleanupDelay);
            }

            return;
        }

        // 普通块：结构与回收已在上方完成，此处只需移除自身。
        Destroy(obj);
    }

    // 显式引爆也使用同一套移除、通知、重组及冲量顺序。
    public void ExplodeBlock(Block block)
    {
        if (block != null && !block.ExplosionQueued)
        {
            DestroyTarget(block.gameObject, true);
        }
    }

    // 手动计划整单位清理：无驾驶舱的单位延迟后转为残骸并移除；重复计划按 runtimeUnitId 去重。
    public void ScheduleUnitCleanup(ControlUnit unit)
    {
        CleanupSession.ScheduleGroup(unit, _unitCleanupDelay);
    }

    // 按距离计划清理：玩家远离后才回收无驾驶舱残骸；距离在延迟结束时重新核对，玩家中途返回就放弃这次清理。
    public void ScheduleDistantGroupCleanup(ControlUnit unit, Transform reference, float maxDistance)
    {
        CleanupSession.ScheduleDistantGroup(unit, reference, maxDistance, _unitCleanupDelay);
    }

    // 结束清理会话：先停止本对象承载的协程，再清空已计划单位；顺序不能反，
    // 否则残留协程会在会话结束（如 PlayEnd 正在拆除场景）后继续回收残骸。本对象只承载清理协程。
    public void EndSalvageSession()
    {
        StopAllCoroutines();
        _cleanupSession?.Clear();
    }
}

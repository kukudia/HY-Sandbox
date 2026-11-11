// 职责：协调销毁主链、资源释放与驾驶舱死亡；配置并调用爆炸规则及残骸清理会话。
// 入口：DestroyGameObject() / ExplodeBlock() / ScheduleUnitCleanup()。
using UnityEngine;

public class DestroyManager : MonoBehaviour
{
    // Centralizes destruction side effects so visuals, unit cleanup, and regrouping happen in a known order.
    public static DestroyManager instance;
    [SerializeField, Tooltip("Emit per-object destruction diagnostics. Disabled by default to avoid stack traces and log I/O during large battles.")]
    private bool _debugDestruction;
    private float _unitCleanupDelay = 10f;
    [SerializeField]
    private float _blockExplosionUpwardsModifier = 1.2f;
    [SerializeField, Range(0f, 1f)]
    private float _explosionImpulseScale = 0.15f;
    [SerializeField, Min(0f)]
    private float _maximumExplosionSpeedChange = 3f;
    [SerializeField, Min(0f)]
    private float _explosionCollisionProtection = 3f;
    private SalvageCleanupSession _cleanupSession;
    private SalvageCleanupSession CleanupSession => _cleanupSession ?? (_cleanupSession = new SalvageCleanupSession(this));

    private void Awake()
    {
        instance = this;
    }

    public void DestroyGameObject(GameObject obj)
    {
        if (obj == null)
        {
            return;
        }

        Durability health = obj.GetComponent<Durability>();
        if (health != null && !health.enabled)
        {
            return;
        }

        if (_debugDestruction)
        {
            Debug.Log($"{obj.name} durability reached 0, destroying object.");
        }

        if (health != null)
        {
            health.enabled = false;
        }

        Cockpit cockpit = obj.GetComponent<Cockpit>();
        // 观战对局在模块脱离/重组前按驾驶舱身份认领死亡，避免误触玩家游戏结束。
        bool skirmishOwnsDeath = cockpit != null && PlayManager.instance != null && PlayManager.instance.Skirmish != null && PlayManager.instance.Skirmish.NotifyCockpitDestroyed(cockpit, health != null ? health.LastAttacker : null);
        if (cockpit != null && cockpit.faction == UnitFaction.Enemy && PlayManager.instance != null && PlayManager.instance.playMode)
        {
            cockpit.GetComponent<EnemyIdentity>()?.ReportDefeated();
        }

        ControlUnit unit = obj.GetComponentInParent<ControlUnit>();
        if (unit != null)
        {
            unit.EnsureRuntimeUnitId();
            unit.AssignRuntimeOwnershipToBlocks(false);
        }

        RuntimeUnitMember member = obj.GetComponent<RuntimeUnitMember>();
        string ownerUnitId = member != null ? member.ownerUnitId : unit?.runtimeUnitId;
        UnitFaction ownerFaction = member != null ? member.ownerFaction : cockpit != null ? cockpit.faction : unit != null ? unit.faction : UnitFaction.Enemy;
        Block block = obj.GetComponent<Block>();
        bool shouldExplodeCockpit = cockpit != null && block != null && PlayManager.instance != null && PlayManager.instance.playMode;
        // Explosive blocks react to every lethal damage source; fragment contacts are protected separately.
        bool willExplode = block != null && block.canExplode && PlayManager.instance != null && PlayManager.instance.playMode;
        // ExplodeBlock owns the single destruction burst for explosive blocks.
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

        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            Destroy(obj);
            return;
        }

        WreckSalvage.ReleaseContents(obj, ownerFaction);
        if (willExplode)
        {
            ExplodeBlock(block);
        }
        else if (block != null)
        {
            // Deferred Destroy must not leave a destroyed non-explosive hold in the next connectivity graph.
            block.DisConnectAllConnectors();
            block.transform.SetParent(null, true);
            if (unit != null)
            {
                PlayManager.instance.RefreshGroup(unit);
            }
        }

        if (cockpit != null)
        {
            bool siegeOwnsDeath = PlayManager.instance.SiegeSession != null;
            if (siegeOwnsDeath)
            {
                PlayManager.instance.SiegeSession.NotifyCockpitDestroyed(cockpit);
            }

            if (!skirmishOwnsDeath && ownerFaction == UnitFaction.Enemy && health != null && health.LastAttacker != null && health.LastAttacker.faction == UnitFaction.Player && unit != null)
            {
                EnemyIdentity identity = cockpit.GetComponent<EnemyIdentity>();
                MainUIPanels.instance?.CombatHud?.ShowPlayerKill(identity != null ? identity.DisplayName : unit.name);
            }

            Destroy(obj);
            if (ownerFaction == UnitFaction.Player && PlayManager.instance.playMode && !siegeOwnsDeath && !skirmishOwnsDeath)
            {
                //MainUIPanels.instance.PlayEnd();
                MainUIPanels.instance.PlayerDeath();
                return;
            }

            CleanupSession.ScheduleUnit(obj, ownerUnitId, _unitCleanupDelay);
            return;
        }

        Destroy(obj);
    }

    public void ExplodeBlock(Block block)
    {
        // Detach the block, selectively break nearby links, regroup the unit, then apply impulse to its groups.
        if (block == null || !PlayManager.instance.playMode)
        {
            return;
        }

        Vector3 explosionPosition = block.transform.position;
        ControlUnit unit = block.GetComponentInParent<ControlUnit>();
        if (unit != null)
        {
            unit.EnsureRuntimeUnitId();
        }

        string ownerUnitId = unit != null ? unit.runtimeUnitId : string.Empty;
        UnitFaction ownerFaction = unit != null ? unit.faction : UnitFaction.Enemy;
        VisualEffectsManager.TryPlayBlockExplosion(block);
        ExplosionPropagation.DisconnectBlocksInExplosionRange(unit, block, explosionPosition);
        block.DisConnectAllConnectors();
        block.transform.SetParent(null, true);
        if (unit != null)
        {
            PlayManager.instance.RefreshGroup(unit);
        }

        ExplosionPropagation.ApplyExplosionForce(
            block,
            ownerUnitId,
            ownerFaction,
            explosionPosition,
            _blockExplosionUpwardsModifier,
            _explosionImpulseScale,
            _maximumExplosionSpeedChange,
            _explosionCollisionProtection);
    }

    public void ScheduleUnitCleanup(ControlUnit unit)
    {
        CleanupSession.ScheduleGroup(unit, _unitCleanupDelay);
    }

    public void ScheduleDistantGroupCleanup(ControlUnit unit, Transform reference, float maxDistance)
    {
        CleanupSession.ScheduleDistantGroup(unit, reference, maxDistance, _unitCleanupDelay);
    }

    public void EndSalvageSession()
    {
        StopAllCoroutines();
        _cleanupSession?.Clear();
    }
}

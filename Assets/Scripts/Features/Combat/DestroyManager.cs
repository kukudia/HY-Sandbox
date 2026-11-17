// 职责：协调销毁主链、资源释放与驾驶舱死亡；配置并调用爆炸规则及残骸清理会话。
// 入口：DestroyGameObject() / ExplodeBlock() / ScheduleUnitCleanup()。
using UnityEngine;
using System.Collections.Generic;

public class DestroyManager : MonoBehaviour
{
    // Centralizes destruction side effects so visuals, unit cleanup, and regrouping happen in a known order.
    public static DestroyManager instance;
    [SerializeField, Tooltip("Emit per-object destruction diagnostics. Disabled by default to avoid stack traces and log I/O during large battles.")]
    private bool _debugDestruction;
    private float _unitCleanupDelay = 10f;
    [SerializeField]
    private float _blockExplosionUpwardsModifier = ExplosionPropagation.DefaultUpwardsModifier;
    [SerializeField, Range(0f, 1f)]
    private float _explosionImpulseScale = ExplosionPropagation.DefaultImpulseScale;
    [SerializeField, Min(0f)]
    private float _maximumExplosionSpeedChange = ExplosionPropagation.DefaultMaximumSpeedChange;
    [SerializeField, Min(0f)]
    private float _explosionCollisionProtection = ExplosionPropagation.DefaultCollisionProtection;
    private SalvageCleanupSession _cleanupSession;
    private readonly ExplosionDamageSession explosions = new ExplosionDamageSession();
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
                explosions.RefreshUnit(unit);
            }
        }

        if (cockpit != null)
        {
            bool siegeOwnsDeath = PlayManager.instance.SiegeSession != null || PlayManager.instance.Modes?.State == GameModeState.Siege;
            if (PlayManager.instance.SiegeSession != null)
            {
                PlayManager.instance.SiegeSession.NotifyCockpitDestroyed(cockpit);
            }

            if (!skirmishOwnsDeath && !siegeOwnsDeath && PlayManager.instance.Modes?.State != GameModeState.Siege && ownerFaction == UnitFaction.Enemy && health != null && health.LastAttacker != null && health.LastAttacker.faction == UnitFaction.Player && unit != null)
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
        if (block == null || block.ExplosionQueued)
        {
            return;
        }

        bool building = block.GetComponentInParent<BuildingRuntime>() != null;
        if (!building && (PlayManager.instance == null || !PlayManager.instance.playMode))
        {
            return;
        }

        block.ExplosionQueued = true;
        Vector3 explosionPosition = block.transform.position;
        ControlUnit unit = block.GetComponentInParent<ControlUnit>();
        if (unit != null)
        {
            unit.EnsureRuntimeUnitId();
        }

        string ownerUnitId = unit != null ? unit.runtimeUnitId : string.Empty;
        UnitFaction ownerFaction = unit != null ? unit.faction : UnitFaction.Enemy;
        ControlUnit attacker = block.GetComponent<Durability>()?.LastAttacker;
        VisualEffectsManager.TryPlayBlockExplosion(block);
        if (!building)
        {
            ExplosionPropagation.DisconnectBlocksInExplosionRange(unit, block, explosionPosition);
            block.DisConnectAllConnectors();
            block.transform.SetParent(null, true);
        }

        explosions.ApplyExplosion(
            block,
            attacker,
            building ? null : unit,
            () => ExplosionPropagation.ApplyExplosionForce(
                block,
                ownerUnitId,
                ownerFaction,
                explosionPosition,
                _blockExplosionUpwardsModifier,
                _explosionImpulseScale,
                _maximumExplosionSpeedChange,
                _explosionCollisionProtection));
    }

    internal void DamageBuildings(IReadOnlyList<BuildingRuntime> buildings, Vector3 center, float radius, float damage)
    {
        explosions.ApplyBuildings(buildings, center, radius, damage);
    }

    internal void ApplyBuildingLoss(BuildingRuntime building, System.Action destroy)
    {
        explosions.ApplyBuildingLoss(building, destroy);
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

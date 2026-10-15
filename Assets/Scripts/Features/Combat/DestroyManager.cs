// 职责：协调销毁主链、资源释放与驾驶舱死亡；配置并调用爆炸规则及残骸清理会话。
// 入口：DestroyGameObject() / ExplodeBlock() / ScheduleUnitCleanup()。
using System.Collections;
using UnityEngine;

public class DestroyManager : MonoBehaviour
{
    // Centralizes destruction side effects so visuals, unit cleanup, and regrouping happen in a known order.
    public static DestroyManager instance;
    private float _refreshDelay = 0.2f;
    private float _unitCleanupDelay = 10f;
    [SerializeField]
    private float _blockExplosionUpwardsModifier = 1.2f;
    [SerializeField, Range(0f, 1f)]
    private float _explosionImpulseScale = 0.15f;
    [SerializeField, Min(0f)]
    private float _maximumExplosionSpeedChange = 3f;
    [SerializeField, Min(0f)]
    private float _explosionCollisionProtection = 3f;
    private bool _isRefreshScheduled;
    private int _destroyedCount;
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

        Debug.Log($"{obj.name} durability reached 0, destroying object.");
        Durability health = obj.GetComponent<Durability>();
        if (health != null && !health.enabled)
        {
            return;
        }

        if (health != null)
        {
            health.enabled = false;
        }

        Cockpit cockpit = obj.GetComponent<Cockpit>();
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
        bool willExplode = block != null && block.canExplode && (health == null || !health.DestroyedByCollision) && PlayManager.instance != null && PlayManager.instance.playMode;
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

        CargoHold destroyedHold = obj.GetComponent<CargoHold>();
        foreach (Bot bot in obj.GetComponentsInChildren<Bot>(true))
        {
            bot.PrepareForHomeDestruction();
        }

        if (destroyedHold != null)
        {
            destroyedHold.ReleaseContents(ownerFaction);
        }

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
            if (ownerFaction == UnitFaction.Enemy && health != null && health.LastAttacker != null && health.LastAttacker.faction == UnitFaction.Player && unit != null)
            {
                EnemyIdentity identity = cockpit.GetComponent<EnemyIdentity>();
                MainUIPanels.instance?.CombatHud?.ShowPlayerKill(identity != null ? identity.DisplayName : unit.name);
            }

            Destroy(obj);
            if (ownerFaction == UnitFaction.Player && PlayManager.instance.playMode)
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
        _isRefreshScheduled = false;
        _destroyedCount = 0;
    }

    public void NotifyObjectDestroyed()
    {
        _destroyedCount++;
        ScheduleRefresh();
    }

    private void ScheduleRefresh()
    {
        if (_isRefreshScheduled)
        {
            return;
        }

        _isRefreshScheduled = true;
        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return new WaitForSeconds(_refreshDelay);
        ExecuteRefresh();
        _isRefreshScheduled = false;
    }

    private void ExecuteRefresh()
    {
        Debug.Log($"Refreshing after {_destroyedCount} objects destroyed");
        _destroyedCount = 0;
    }
}

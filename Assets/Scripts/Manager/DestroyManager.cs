using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyManager : MonoBehaviour
{
    // Centralizes destruction side effects so visuals, unit cleanup, and regrouping happen in a known order.
    private static DestroyManager _instance;
    public static DestroyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.FindFirstObjectByType<DestroyManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("DestroyManager");
                    _instance = go.AddComponent<DestroyManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    private float _refreshDelay = 0.2f;
    private float _unitCleanupDelay = 10f;
    [SerializeField] private float _blockExplosionUpwardsModifier = 1.2f;
    private bool _isRefreshScheduled;
    private int _destroyedCount;
    private HashSet<string> _scheduledUnitCleanups = new HashSet<string>();

    public void DestroyGameObject(GameObject obj)
    {
        if (obj == null) return;
        Debug.Log($"{obj.name} durability reached 0, destroying object.");
        Durability health = obj.GetComponent<Durability>();
        if (health != null && !health.enabled) return;
        if (health != null) health.enabled = false;
        Cockpit cockpit = obj.GetComponent<Cockpit>();
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
        bool shouldExplodeCockpit = cockpit != null
            && block != null
            && PlayManager.instance != null
            && PlayManager.instance.playMode;
        bool willExplode = block != null && block.canExplode
            && PlayManager.instance != null && PlayManager.instance.playMode;
        // ExplodeBlock owns the single destruction burst for explosive blocks.
        if (!willExplode)
        {
            if (block != null && !shouldExplodeCockpit)
                VisualEffectsManager.TryPlayBlockRemoved(block);
            else
                VisualEffectsManager.TryPlayObjectDestroyed(obj);
        }
        if (PlayManager.instance == null || !PlayManager.instance.playMode)
        {
            Destroy(obj);
            return;
        }

        CargoHold destroyedHold = obj.GetComponent<CargoHold>();
        foreach (Bot bot in obj.GetComponentsInChildren<Bot>(true)) bot.PrepareForHomeDestruction();
        if (destroyedHold != null) destroyedHold.ReleaseContents(ownerFaction);

        if (block != null && block.canExplode)
        {
            ExplodeBlock(block);
        }
        else if (block != null)
        {
            // Deferred Destroy must not leave a destroyed non-explosive hold in the next connectivity graph.
            block.DisConnectAllConnectors();
            block.transform.SetParent(null, true);
            if (unit != null) PlayManager.instance.RefreshGroup(unit);
        }

        if (cockpit != null)
        {
            if (ownerFaction == UnitFaction.Enemy && health != null && health.LastAttacker != null
                && health.LastAttacker.faction == UnitFaction.Player && unit != null)
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

            ScheduleUnitCleanup(obj, ownerUnitId, ownerFaction);
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

        DisconnectBlocksInExplosionRange(unit, block, explosionPosition);
        block.DisConnectAllConnectors();
        block.transform.SetParent(null, true);

        if (unit != null)
        {
            PlayManager.instance.RefreshGroup(unit);
        }

        ApplyExplosionForce(block, ownerUnitId, ownerFaction, explosionPosition);
    }

    private void DisconnectBlocksInExplosionRange(ControlUnit unit, Block block, Vector3 explosionPosition)
    {
        if (unit == null || block == null)
        {
            return;
        }

        float radiusSqr = block.explosionRadius * block.explosionRadius;
        float probability = Mathf.Clamp01(block.explosionDisconnectProbability);
        Block[] blocks = unit.GetComponentsInChildren<Block>(true);
        HashSet<Block> neighborsToRefresh = new HashSet<Block>();
        foreach (Block candidate in blocks)
        {
            if (candidate == null || candidate == block)
            {
                continue;
            }

            Vector3 offset = candidate.transform.position - explosionPosition;
            if (offset.sqrMagnitude > radiusSqr || Random.value > probability)
            {
                continue;
            }

            foreach (Block neighbor in candidate.neighbors)
            {
                if (neighbor != null && neighbor != block)
                {
                    neighborsToRefresh.Add(neighbor);
                }
            }

            candidate.DisConnectAllConnectors(false);
        }

        foreach (Block neighbor in neighborsToRefresh)
        {
            if (neighbor != null)
            {
                neighbor.CheckConnection();
            }
        }
    }

    private void ApplyExplosionForce(Block block, string ownerUnitId, UnitFaction ownerFaction, Vector3 explosionPosition)
    {
        if (block == null || block.explosionForce <= 0f || block.explosionRadius <= 0f)
        {
            return;
        }

        float radius = block.explosionRadius;
        HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();
        Collider[] colliders = Physics.OverlapSphere(
            explosionPosition,
            radius,
            Physics.AllLayers,
            QueryTriggerInteraction.Ignore);
        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            Rigidbody body = collider.attachedRigidbody;
            if (body != null)
            {
                bodies.Add(body);
            }
        }

        // A grouped Rigidbody can be centered outside the radius while one of its child blocks is inside it.
        if (!string.IsNullOrEmpty(ownerUnitId))
        {
            RuntimeUnitMember[] members = Object.FindObjectsByType<RuntimeUnitMember>(FindObjectsSortMode.None);
            foreach (RuntimeUnitMember member in members)
            {
                if (member == null || member.ownerUnitId != ownerUnitId)
                {
                    continue;
                }

                Block memberBlock = member.GetComponent<Block>();
                ControlUnit group = member.GetComponentInParent<ControlUnit>();
                if (memberBlock == null || group == null)
                {
                    continue;
                }

                if ((memberBlock.transform.position - explosionPosition).sqrMagnitude <= radius * radius)
                {
                    Rigidbody body = group.GetComponent<Rigidbody>();
                    if (body != null)
                    {
                        bodies.Add(body);
                        group.faction = ownerFaction;
                    }
                }
            }
        }

        foreach (Rigidbody rb in bodies)
        {
            if (rb == null || rb.isKinematic)
            {
                continue;
            }

            Vector3 forcePoint = rb.worldCenterOfMass;
            float nearestDistance = Vector3.Distance(forcePoint, explosionPosition);
            Collider[] bodyColliders = rb.GetComponentsInChildren<Collider>();
            foreach (Collider bodyCollider in bodyColliders)
            {
                if (bodyCollider == null) continue;

                Vector3 candidatePoint = bodyCollider.ClosestPoint(explosionPosition);
                float candidateDistance = Vector3.Distance(candidatePoint, explosionPosition);
                if (candidateDistance < nearestDistance)
                {
                    forcePoint = candidatePoint;
                    nearestDistance = candidateDistance;
                }
            }

            float falloff = 1f - Mathf.Clamp01(nearestDistance / radius);
            if (falloff <= 0f)
            {
                continue;
            }

            Vector3 direction = forcePoint - explosionPosition;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = rb.worldCenterOfMass - explosionPosition;
            }

            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            Vector3 impulse = (direction + Vector3.up * _blockExplosionUpwardsModifier).normalized
                * (block.explosionForce * falloff);
            rb.AddForceAtPosition(impulse, forcePoint, ForceMode.Impulse);

            ControlUnit detachedGroup = rb.GetComponent<ControlUnit>();
            if (detachedGroup != null && !detachedGroup.HasAnyCockpit)
            {
                // Only disposable fragments receive smoke; intact cockpit groups keep a clean combat silhouette.
                VisualEffectsManager.TryAttachDetachedPartSmoke(rb, forcePoint, falloff);
            }
        }
    }

    private void ScheduleUnitCleanup(GameObject obj, string ownerUnitId, UnitFaction ownerFaction)
    {
        if (string.IsNullOrEmpty(ownerUnitId) || _scheduledUnitCleanups.Contains(ownerUnitId))
        {
            return;
        }

        _scheduledUnitCleanups.Add(ownerUnitId);

        Debug.Log($"Scheduling cleanup for unit {obj} after {_unitCleanupDelay} seconds.");

        StartCoroutine(CleanupUnitAfterDelay(ownerUnitId, ownerFaction));
    }

    public void ScheduleUnitCleanup(ControlUnit unit)
    {
        if (unit == null || unit.HasAnyCockpit)
        {
            return;
        }

        unit.EnsureRuntimeUnitId();
        string ownerUnitId = unit.runtimeUnitId;
        if (string.IsNullOrEmpty(ownerUnitId) || _scheduledUnitCleanups.Contains(ownerUnitId))
        {
            return;
        }

        _scheduledUnitCleanups.Add(ownerUnitId);
        StartCoroutine(CleanupGroupAfterDelay(unit, ownerUnitId));
    }

    public void ScheduleDistantGroupCleanup(ControlUnit unit, Transform reference, float maxDistance)
    {
        if (unit == null || reference == null || maxDistance <= 0f)
        {
            return;
        }

        unit.EnsureRuntimeUnitId();
        string ownerUnitId = unit.runtimeUnitId;
        if (string.IsNullOrEmpty(ownerUnitId) || _scheduledUnitCleanups.Contains(ownerUnitId))
        {
            return;
        }

        _scheduledUnitCleanups.Add(ownerUnitId);
        StartCoroutine(CleanupDistantGroupAfterDelay(unit, ownerUnitId, reference, maxDistance * maxDistance));
    }

    private IEnumerator CleanupDistantGroupAfterDelay(
        ControlUnit unit,
        string ownerUnitId,
        Transform reference,
        float maxDistanceSqr)
    {
        yield return new WaitForSeconds(WreckSalvage.Settings != null ? WreckSalvage.Settings.cleanupDelay : _unitCleanupDelay);

        if (unit != null
            && reference != null
            && unit.runtimeUnitId == ownerUnitId
            && (unit.transform.position - reference.position).sqrMagnitude > maxDistanceSqr)
        {
            WreckSalvage.ConvertGroup(unit);
            Destroy(unit.gameObject);
        }

        _scheduledUnitCleanups.Remove(ownerUnitId);
    }

    private IEnumerator CleanupGroupAfterDelay(ControlUnit unit, string ownerUnitId)
    {
        yield return new WaitForSeconds(WreckSalvage.Settings != null ? WreckSalvage.Settings.cleanupDelay : _unitCleanupDelay);

        if (unit != null
            && unit.runtimeUnitId == ownerUnitId
            && !unit.HasAnyCockpit)
        {
            WreckSalvage.ConvertGroup(unit);
            Destroy(unit.gameObject);
        }

        _scheduledUnitCleanups.Remove(ownerUnitId);
    }

    private IEnumerator CleanupUnitAfterDelay(string ownerUnitId, UnitFaction ownerFaction)
    {
        yield return new WaitForSeconds(WreckSalvage.Settings != null ? WreckSalvage.Settings.cleanupDelay : _unitCleanupDelay);

        RuntimeUnitMember[] members = Object.FindObjectsByType<RuntimeUnitMember>(FindObjectsSortMode.None);
        foreach (RuntimeUnitMember member in members)
        {
            if (member == null || member.ownerUnitId != ownerUnitId) continue;

            WreckSalvage.Convert(member.GetComponent<Block>());
            Destroy(member.gameObject);
        }

        _scheduledUnitCleanups.Remove(ownerUnitId);
    }

    private void PlayDisappearEffect(GameObject obj)
    {
        VisualEffectsManager.TryPlayObjectDestroyed(obj);
    }

    public void EndSalvageSession()
    {
        StopAllCoroutines();
        _scheduledUnitCleanups.Clear();
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
        if (_isRefreshScheduled) return;

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

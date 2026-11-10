// 职责：从炮口可见的敌对组中选择最近伤害目标，支持玩家优先攻击可见驾驶舱。
// 入口：FindNearestTarget()；由 Turret 按索敌周期调用，不依赖当前供电。
// 生命周期：共享单位当前物理步的层级快照，实时验证伤势、位置和归属；修复/重组使快照失效。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class TurretTargetSelector
{
    private readonly Transform _weapon;
    private Vector3 _origin;
    private int _hitLayers = ~0;
    private RaycastHit[] _visibilityHits = new RaycastHit[16];
    internal TurretTargetSelector(Transform weapon)
    {
        _weapon = weapon;
        _origin = weapon.position;
    }

    internal void ConfigureVisibility(Vector3 origin, LayerMask hitLayers)
    {
        _origin = origin;
        _hitLayers = hitLayers;
    }

    internal bool HasLineOfSight(ControlUnit owner, ControlUnit target, Durability durability)
    {
        if (durability == null)
        {
            return false;
        }

        Vector3 offset = durability.transform.position - _origin;
        float distance = offset.magnitude;
        if (distance < 0.001f)
        {
            return true;
        }

        PhysicsScene physics = _weapon.gameObject.scene.GetPhysicsScene();
        int count;
        while (true)
        {
            count = physics.Raycast(
                _origin,
                offset / distance,
                _visibilityHits,
                distance,
                _hitLayers,
                QueryTriggerInteraction.Ignore);
            if (count < _visibilityHits.Length)
            {
                break;
            }

            // NonAlloc 结果无序；饱和时不能假定墙或目标已包含在返回值中。
            if (_visibilityHits.Length >= 4096)
            {
                return false;
            }

            System.Array.Resize(ref _visibilityHits, _visibilityHits.Length * 2);
        }

        float targetDistance = distance;
        float blockingDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _visibilityHits[i];
            ControlUnit hitUnit = hit.collider.GetComponentInParent<ControlUnit>();
            // 只忽略炮塔自身。所属基地/舰体的其他墙板仍遮挡，避免库内炮塔透过自家墙锁敌。
            if (owner != null && hitUnit == owner && (hit.collider.transform == _weapon || hit.collider.transform.IsChildOf(_weapon)))
            {
                continue;
            }

            if (hitUnit == target)
            {
                // 目标外壳会先于驾驶舱中心命中；它属于同一个可攻击单位。
                targetDistance = Mathf.Min(targetDistance, hit.distance);
            }
            else
            {
                blockingDistance = Mathf.Min(blockingDistance, hit.distance);
            }
        }

        return blockingDistance > targetDistance;
    }

    internal ControlUnit FindNearestTarget(
        IReadOnlyList<ControlUnit> units,
        ControlUnit owner,
        UnitFaction faction,
        float range,
        bool prioritizeCockpit,
        out Durability nearestDurability)
    {
        ControlUnit nearest = null;
        nearestDurability = null;
        float nearestSqrDistance = range * range;
        Vector3 origin = _origin;
        if (units == null)
        {
            return null;
        }

        if (prioritizeCockpit)
        {
            foreach (ControlUnit candidate in units)
            {
                if (candidate == null || candidate == owner)
                {
                    continue;
                }

                if (!candidate.isActiveAndEnabled || !candidate.HasValidCockpit || !candidate.IsSimulationActive || (owner != null && candidate.gameObject.scene != owner.gameObject.scene) || candidate.faction != faction)
                {
                    continue;
                }

                Durability cockpitDurability = FindCockpitDurability(candidate);
                if (cockpitDurability == null || !cockpitDurability.isActiveAndEnabled || cockpitDurability.currentDurability <= 0f)
                {
                    continue;
                }

                float sqrDistance = (cockpitDurability.transform.position - origin).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance && HasLineOfSight(owner, candidate, cockpitDurability))
                {
                    nearest = candidate;
                    nearestDurability = cockpitDurability;
                    nearestSqrDistance = sqrDistance;
                }
            }

            if (nearestDurability != null)
            {
                return nearest;
            }
        }

        foreach (ControlUnit candidate in units)
        {
            if (candidate == null || candidate == owner)
            {
                continue;
            }

            if (!candidate.isActiveAndEnabled || candidate.faction != faction || !candidate.IsSimulationActive || !candidate.IsOperational || (owner != null && candidate.gameObject.scene != owner.gameObject.scene))
            {
                continue;
            }

            Durability candidateDurability = FindNearestDurability(candidate, owner, origin, nearestSqrDistance);
            if (candidateDurability == null)
            {
                continue;
            }

            float sqrDistance = (candidateDurability.transform.position - origin).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = candidate;
                nearestDurability = candidateDurability;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    private Durability FindNearestDurability(ControlUnit unit, ControlUnit owner, Vector3 origin, float nearestSqrDistance)
    {
        Durability nearest = null;
        IReadOnlyList<Durability> targets = unit.GetDamageTargets();
        for (int index = 0; index < targets.Count; index++)
        {
            Durability durability = targets[index];
            if (durability == null || !durability.isActiveAndEnabled || durability.currentDurability <= 0f)
            {
                continue;
            }

            float sqrDistance = (durability.transform.position - origin).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance && durability.GetComponentInParent<ControlUnit>() == unit && HasLineOfSight(owner, unit, durability))
            {
                nearest = durability;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    private Durability FindCockpitDurability(ControlUnit unit)
    {
        if (unit == null || unit.cockpit == null)
        {
            return null;
        }

        return unit.cockpit.GetComponentInParent<Durability>();
    }
}

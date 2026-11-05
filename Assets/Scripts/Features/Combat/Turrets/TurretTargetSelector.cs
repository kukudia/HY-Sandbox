// 职责：从有效敌对组中选择范围内最近的伤害目标，支持玩家优先攻击敌方驾驶舱。
// 入口：FindNearestTarget()；由 Turret 按索敌周期调用，不依赖当前供电。
// 生命周期：共享单位当前物理步的层级快照，实时验证伤势、位置和归属；修复/重组使快照失效。
using System.Collections.Generic;
using UnityEngine;

internal sealed class TurretTargetSelector
{
    private readonly Transform _weapon;
    internal TurretTargetSelector(Transform weapon)
    {
        _weapon = weapon;
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
        Vector3 origin = _weapon.position;
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
                if (sqrDistance < nearestSqrDistance)
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

            Durability candidateDurability = FindNearestDurability(candidate, origin, nearestSqrDistance);
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

    private Durability FindNearestDurability(ControlUnit unit, Vector3 origin, float nearestSqrDistance)
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
            if (sqrDistance < nearestSqrDistance && durability.GetComponentInParent<ControlUnit>() == unit)
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

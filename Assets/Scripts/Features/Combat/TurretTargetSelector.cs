// 职责：从有效敌对组中选择范围内最近的伤害目标，支持玩家优先攻击敌方驾驶舱。
// 入口：FindNearestTarget()；由 TurretWeapon 按既有索敌周期调用。
// 生命周期：每炮塔独立缓存模块引用；销毁引用在距离计算时跳过。
using System.Collections.Generic;
using UnityEngine;

internal sealed class TurretTargetSelector
{
    private readonly Transform _weapon;
    private readonly Dictionary<ControlUnit, Durability[]> _durabilityCache = new Dictionary<ControlUnit, Durability[]>();
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

                if (!candidate.HasValidCockpit || candidate.faction != faction)
                {
                    continue;
                }

                Durability cockpitDurability = FindCockpitDurability(candidate);
                if (cockpitDurability == null)
                {
                    continue;
                }

                float sqrDistance = (cockpitDurability.transform.position - _weapon.position).sqrMagnitude;
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

            if (!candidate.HasValidCockpit || candidate.faction != faction)
            {
                continue;
            }

            Durability candidateDurability = FindNearestDurability(candidate);
            if (candidateDurability == null)
            {
                continue;
            }

            float sqrDistance = (candidateDurability.transform.position - _weapon.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = candidate;
                nearestDurability = candidateDurability;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    private Durability FindNearestDurability(ControlUnit unit)
    {
        Durability[] durabilities = GetDurabilities(unit);
        Durability nearest = null;
        float nearestSqrDistance = float.MaxValue;
        foreach (Durability durability in durabilities)
        {
            if (durability == null)
            {
                continue;
            }

            float sqrDistance = (durability.transform.position - _weapon.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearest = durability;
                nearestSqrDistance = sqrDistance;
            }
        }

        return nearest;
    }

    private Durability[] GetDurabilities(ControlUnit unit)
    {
        if (unit == null)
        {
            return System.Array.Empty<Durability>();
        }

        if (!_durabilityCache.TryGetValue(unit, out Durability[] durabilities) || durabilities == null)
        {
            durabilities = unit.GetComponentsInChildren<Durability>();
            _durabilityCache[unit] = durabilities;
        }

        return durabilities;
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

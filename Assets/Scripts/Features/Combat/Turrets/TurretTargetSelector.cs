// 职责：从炮口可见的敌对组中选择最近伤害目标，支持玩家优先攻击可见驾驶舱。
// 入口：FindNearestTarget()；由 Turret 按索敌周期调用，不依赖当前供电。
// 生命周期：共享单位当前物理步的层级快照，实时验证伤势、位置和归属；修复/重组使快照失效。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class TurretTargetSelector
{
    private static readonly Unity.Profiling.ProfilerMarker SearchMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.Search");
    private static readonly Unity.Profiling.ProfilerMarker VisibilityMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.Visibility");
    private static readonly Unity.Profiling.ProfilerMarker CandidateMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.Candidates");
    // 补算多个物理步时共享渲染帧预算；FIFO 保证长期无目标的炮塔不会挤掉后面的炮塔。
    private const int SearchesPerFrame = 16;
    private static readonly LinkedList<TurretTargetSelector> waiting = new LinkedList<TurretTargetSelector>();
    private static int budgetFrame = -1;
    private static int remaining;
    private readonly LinkedListNode<TurretTargetSelector> _request;
    private int _grantedFrame = -1;
    private readonly Transform _weapon;
    private Vector3 _origin;
    private int _hitLayers = ~0;
    private RaycastHit[] _visibilityHits = new RaycastHit[16];
    private readonly List<Candidate> candidates = new List<Candidate>();
    private struct Candidate
    {
        internal Durability health;
        internal float distanceSquared;
        internal int hierarchyOrder;
    }

    private static readonly System.Comparison<Candidate> NearestFirst = (left, right) =>
    {
        int distance = left.distanceSquared.CompareTo(right.distanceSquared);
        return distance != 0 ? distance : left.hierarchyOrder.CompareTo(right.hierarchyOrder);
    };
    internal TurretTargetSelector(Transform weapon)
    {
        _weapon = weapon;
        _origin = weapon.position;
        _request = new LinkedListNode<TurretTargetSelector>(this);
    }

    internal bool TryScheduleSearch()
    {
        int frame = Time.frameCount;
        if (budgetFrame != frame)
        {
            budgetFrame = frame;
            remaining = SearchesPerFrame;
            while (remaining > 0 && waiting.First != null)
            {
                waiting.First.Value._grantedFrame = frame;
                waiting.RemoveFirst();
                remaining--;
            }
        }

        if (_grantedFrame == frame)
        {
            _grantedFrame = -1;
            return true;
        }

        if (remaining > 0)
        {
            remaining--;
            return true;
        }

        if (_request.List == null)
        {
            waiting.AddLast(_request);
        }

        return false;
    }

    internal void CancelScheduledSearch()
    {
        if (_request.List != null)
        {
            waiting.Remove(_request);
        }

        _grantedFrame = -1;
    }

    internal void ConfigureVisibility(Vector3 origin, LayerMask hitLayers)
    {
        _origin = origin;
        _hitLayers = hitLayers;
    }

    internal bool HasLineOfSight(ControlUnit owner, ControlUnit target, Durability durability)
    {
        using var sample = VisibilityMarker.Auto();
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

        float nearestDistance = float.PositiveInfinity;
        Collider nearestCollider = null;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _visibilityHits[i];
            // 只忽略炮塔自身。所属基地/舰体的其他墙板仍遮挡，避免库内炮塔透过自家墙锁敌。
            if (owner != null && hit.collider.transform.IsChildOf(_weapon) && hit.collider.GetComponentInParent<ControlUnit>() == owner)
            {
                continue;
            }

            if (hit.distance < nearestDistance || (hit.distance == nearestDistance && hit.collider.GetComponentInParent<ControlUnit>() != target))
            {
                nearestDistance = hit.distance;
                nearestCollider = hit.collider;
            }
        }

        // NonAlloc结果无序；仅最近非自身命中决定可见性，不必逐个遍历其组件父链。
        // 目标外壳会先于驾驶舱中心命中，它属于同一个可攻击单位。
        return nearestCollider == null || nearestCollider.GetComponentInParent<ControlUnit>() == target;
    }

    internal ControlUnit FindNearestTarget(
        IReadOnlyList<ControlUnit> units,
        ControlUnit owner,
        UnitFaction faction,
        float range,
        bool prioritizeCockpit,
        out Durability nearestDurability)
    {
        using var sample = SearchMarker.Auto();
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
            for (int i = 0; i < units.Count; i++)
            {
                ControlUnit candidate = units[i];
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

        for (int i = 0; i < units.Count; i++)
        {
            ControlUnit candidate = units[i];
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
        candidates.Clear();
        int closest = -1;
        float closestDistance = nearestSqrDistance;
        using (CandidateMarker.Auto())
        {
            IReadOnlyList<Durability> targets = unit.GetDamageTargets();
            for (int index = 0; index < targets.Count; index++)
            {
                Durability durability = targets[index];
                if (durability == null || !durability.isActiveAndEnabled || durability.currentDurability <= 0f)
                {
                    continue;
                }

                float sqrDistance = (durability.transform.position - origin).sqrMagnitude;
                if (sqrDistance < nearestSqrDistance)
                {
                    if (sqrDistance < closestDistance)
                    {
                        closest = candidates.Count;
                        closestDistance = sqrDistance;
                    }

                    candidates.Add(new Candidate { health = durability, distanceSquared = sqrDistance, hierarchyOrder = index });
                }
            }
        }

        // 层级顺序与空间距离无关；先检查最近模块，避免对后续必然更远的模块发射可见性射线。
        // 遮挡时仍继续寻找，不能以最近模块不可见推断整艘船都不可见。
        if (closest < 0)
        {
            return null;
        }

        Durability first = candidates[closest].health;
        if (first.GetComponentInParent<ControlUnit>() == unit && HasLineOfSight(owner, unit, first))
        {
            return first;
        }

        // 最近模块被挡才排序其余候选；比较器用原层级序号打破同距平局。
        int last = candidates.Count - 1;
        candidates[closest] = candidates[last];
        candidates.RemoveAt(last);
        candidates.Sort(NearestFirst);
        for (int index = 0; index < candidates.Count; index++)
        {
            Durability health = candidates[index].health;
            if (health.GetComponentInParent<ControlUnit>() == unit && HasLineOfSight(owner, unit, health))
            {
                return health;
            }
        }

        return null;
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

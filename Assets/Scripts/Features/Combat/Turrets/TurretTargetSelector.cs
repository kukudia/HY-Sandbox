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
    internal Vector3 SurfacePoint
    {
        get; private set;
    }

    private Vector3 localHit;
    internal int LastSearchProbes
    {
        get; private set;
    }

    private int phase;
    private const int ProbeCount = 4;
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

    internal bool HasLineOfSight(ControlUnit owner, ControlUnit target, float range)
    {
        return Probe(owner, target, target.transform.TransformPoint(localHit), range);
    }

    private bool Probe(ControlUnit owner, ControlUnit target, Vector3 point, float range)
    {
        using var sample = VisibilityMarker.Auto();
        Vector3 delta = point - _origin;
        if (delta.sqrMagnitude < 0.000001f || range <= 0f)
        {
            return false;
        }

        Vector3 direction = delta.normalized;
        PhysicsScene physics = _weapon.gameObject.scene.GetPhysicsScene();
        int count;
        while (true)
        {
            count = physics.Raycast(
                _origin,
                direction,
                _visibilityHits,
                range,
                _hitLayers,
                QueryTriggerInteraction.Ignore);
            if (count < _visibilityHits.Length)
            {
                break;
            }

            if (count >= 4096)
            {
                return false;
            }

            System.Array.Resize(ref _visibilityHits, count * 2);
        }

        float distance = float.PositiveInfinity;
        bool visible = false;
        Vector3 end = _origin + direction * range;
        // Only the foremost surface can determine visibility. Preserve blocker priority on ties.
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _visibilityHits[i];
            if (hit.distance < distance && !hit.collider.transform.IsChildOf(_weapon))
            {
                distance = hit.distance;
            }
        }

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _visibilityHits[i];
            if (hit.distance != distance || hit.collider.transform.IsChildOf(_weapon))
            {
                continue;
            }

            bool belongs = hit.collider.GetComponentInParent<ControlUnit>() == target;
            visible = belongs;
            end = hit.point;
            if (!visible)
            {
                break;
            }
        }

        DebugManager debug = DebugManager.instance;
        if (debug != null && debug.ShowTurretTargeting && debug.Includes(_weapon))
        {
            Debug.DrawLine(_origin, end, visible ? Color.green : Color.red, Time.fixedDeltaTime);
        }

        if (visible)
        {
            SurfacePoint = end;
            localHit = target.transform.InverseTransformPoint(end);
        }

        return visible;
    }

    internal ControlUnit FindNearestTarget(
        IReadOnlyList<ControlUnit> units,
        ControlUnit owner,
        UnitFaction faction,
        float range,
        bool prioritizeCockpit)
    {
        using var sample = SearchMarker.Auto();
        LastSearchProbes = 0;
        if (units == null || range <= 0f)
        {
            return null;
        }

        ControlUnit best = null;
        float bestDistance = float.PositiveInfinity;
        Vector3 bestPoint = default;
        int start = phase;
        phase = (phase + 1) % 6;
        foreach (ControlUnit unit in units)
        {
            if (unit == null || unit == owner || unit.faction != faction || !unit.isActiveAndEnabled || !unit.IsOperational || !unit.IsSimulationActive || unit.gameObject.scene != _weapon.gameObject.scene || !unit.TryGetTargetBounds(out Bounds bounds))
            {
                continue;
            }

            Vector3 near = unit.transform.TransformPoint(bounds.ClosestPoint(unit.transform.InverseTransformPoint(_origin)));
            float lowerBound = (near - _origin).sqrMagnitude;
            if (lowerBound > range * range)
            {
                continue;
            }

            for (int i = 0; i < ProbeCount; i++)
            {
                Vector3 point;
                if (i == 0 && prioritizeCockpit && unit.HasValidCockpit)
                {
                    point = unit.cockpit.transform.position;
                }
                else if (i == 0)
                {
                    point = unit.transform.TransformPoint(bounds.center);
                }
                else
                {
                    int side = (start + i - 1) % 6;
                    point = unit.HullSample(side);
                }

                LastSearchProbes++;
                if (!Probe(owner, unit, point, range))
                {
                    continue;
                }

                float distance = (SurfacePoint - _origin).sqrMagnitude;
                if (distance < bestDistance)
                {
                    best = unit;
                    bestDistance = distance;
                    bestPoint = SurfacePoint;
                }

                break;
            }
        }

        if (best != null)
        {
            SurfacePoint = bestPoint;
            localHit = best.transform.InverseTransformPoint(bestPoint);
        }

        return best;
    }
}

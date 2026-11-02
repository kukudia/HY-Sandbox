// 职责：激光沿射线瞬时命中最近的非自身障碍，结算伤害但不施加冲量。
// 入口：Fire()；由 LaserTurret 调用并返回光束终点。
using System.Collections.Generic;
using UnityEngine;

internal sealed class TurretHitscan
{
    private const int MaxRaycastHits = 16;
    private readonly RaycastHit[] _hits = new RaycastHit[MaxRaycastHits];
    internal Vector3 Fire(
        Vector3 origin,
        Vector3 direction,
        ControlUnit owner,
        UnitFaction targetFaction,
        float range,
        LayerMask hitLayers,
        float damage)
    {
        Vector3 end = origin + direction * range;
        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction,
            _hits,
            range,
            hitLayers,
            QueryTriggerInteraction.Ignore);
        // A saturated NonAlloc query is unordered and may omit the nearest obstacle.
        RaycastHit[] hits = hitCount == _hits.Length ? Physics.RaycastAll(origin, direction, range, hitLayers, QueryTriggerInteraction.Ignore) : _hits;
        hitCount = hits == _hits ? hitCount : hits.Length;
        System.Array.Sort(hits, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = hits[i];
            ControlUnit hitUnit = hit.collider.GetComponentInParent<ControlUnit>();
            if (owner != null && hitUnit == owner)
            {
                continue;
            }

            end = hit.point;
            TurretHitResolver.Apply(
                hit.collider,
                hit.point,
                hit.normal,
                direction,
                owner,
                targetFaction,
                damage,
                0f);
            break;
        }

        return end;
    }

    private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
    {
        internal static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();
        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }
}

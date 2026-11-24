// 职责：激光沿射线瞬时命中最近的非自身障碍，结算伤害但不施加冲量。
// 入口：Fire()；由 LaserTurret 调用并返回显示所需的表面锚点。
using System.Collections.Generic;
using UnityEngine;

internal sealed class TurretHitscan
{
    private const int MaxRaycastHits = 16;
    private readonly RaycastHit[] _hits = new RaycastHit[MaxRaycastHits];
    internal LaserShotEndpoint Fire(
        Vector3 origin,
        Vector3 direction,
        ControlUnit owner,
        UnitFaction targetFaction,
        float range,
        LayerMask hitLayers,
        float damage)
    {
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

            // 伤害可能立即拆组或禁用部件；先记录射线实际接触的表面坐标。
            var endpoint = new LaserShotEndpoint(hit.collider, hit.point, hit.normal);
            TurretHitResolver.Apply(
                hit.collider,
                hit.point,
                hit.normal,
                direction,
                owner,
                targetFaction,
                damage,
                0f,
                playImpact: false);
            return endpoint;
        }

        return new LaserShotEndpoint(null, origin + direction * range, Vector3.zero);
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

internal readonly struct LaserShotEndpoint
{
    internal readonly Collider Surface;
    internal readonly Vector3 Point;
    internal readonly Vector3 Normal;
    private readonly Vector3 _localPoint;
    private readonly Vector3 _localNormal;
    internal bool IsHit => !ReferenceEquals(Surface, null);

    internal LaserShotEndpoint(Collider surface, Vector3 point, Vector3 normal)
    {
        Surface = surface;
        Point = point;
        Normal = normal;
        Transform target = surface != null ? surface.transform : null;
        _localPoint = target != null ? target.InverseTransformPoint(point) : default;
        _localNormal = target != null ? target.localToWorldMatrix.transpose.MultiplyVector(normal) : default;
    }

    internal bool TryGetSurfacePose(out Vector3 point, out Vector3 normal)
    {
        point = Point;
        normal = Normal;
        if (Surface == null || !Surface.enabled || !Surface.gameObject.activeInHierarchy)
        {
            return false;
        }

        Transform target = Surface.transform;
        point = target.TransformPoint(_localPoint);
        normal = target.worldToLocalMatrix.transpose.MultiplyVector(_localNormal).normalized;
        return true;
    }
}

// 职责：为自主舰按真实航迹优先选择上/下避障，验证整舰净空并保持高度直到舰尾越障。
// 入口：TryGetHeight()；仅输出目标高度，不写刚体，不替代顶棚下的水平寻路。
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnemyObstacleClimb
{
    private RaycastHit[] hits = new RaycastHit[64];
    private Collider[] hull;
    private Rigidbody _body;
    private float nextHullRefresh;
    private float height;
    private float exitProjection;
    private Vector3 heading;
    private bool active;
    internal void Clear()
    {
        active = false;
        hull = null;
        _body = null;
    }

    internal bool TryGetHeight(
        ControlUnit unit,
        Vector3 direction,
        float baseHeight,
        float lookAhead,
        float maxRise,
        float clearance,
        LayerMask layers,
        out float targetHeight,
        float maxDrop = 0f)
    {
        targetHeight = baseHeight;
        if (direction.sqrMagnitude < 0.01f)
        {
            Clear();
            return false;
        }

        direction.Normalize();
        Bounds bounds = GetHullBounds(unit);
        if (active && (Vector3.Dot(direction, heading) < 0.5f || Vector3.Dot(unit.transform.position, heading) > exitProjection))
        {
            active = false;
        }

        PhysicsScene physics = unit.gameObject.scene.GetPhysicsScene();
        // 提前覆盖恢复常规高度后的走廊，连续矮墙之间不能先下降再重新发现障碍。
        float bottomOffset = unit.transform.position.y - bounds.min.y;
        Vector3 scanCenter = bounds.center;
        scanCenter.y += Mathf.Min(0f, baseHeight - unit.transform.position.y);
        int count = Cast(
            physics,
            scanCenter,
            bounds.extents,
            direction,
            lookAhead,
            layers);
        Collider obstacle = null;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (IsGeometry(hit.collider, unit) && hit.collider.bounds.max.y > baseHeight - bottomOffset + 0.25f && hit.distance < nearest)
            {
                nearest = hit.distance;
                obstacle = hit.collider;
            }
        }

        float candidate = active ? height : baseHeight;
        if (obstacle != null)
        {
            candidate = obstacle.bounds.max.y + bottomOffset + clearance;
            if (active && height > baseHeight)
            {
                candidate = Mathf.Max(height, candidate);
            }
        }

        bool upward = candidate > baseHeight + 0.1f && candidate <= baseHeight + maxRise;
        if (!upward || !CanPass(
            physics,
            bounds,
            direction,
            lookAhead,
            candidate - unit.transform.position.y,
            layers,
            unit))
        {
            float topOffset = bounds.max.y - unit.transform.position.y;
            candidate = obstacle != null ? obstacle.bounds.min.y - topOffset - clearance : height;
            if (maxDrop <= 0f || candidate >= baseHeight - 0.1f || candidate < baseHeight - maxDrop || candidate < 2f || !CanPass(
                physics,
                bounds,
                direction,
                lookAhead,
                candidate - unit.transform.position.y,
                layers,
                unit))
            {
                active = false;
                return false;
            }
        }

        if (obstacle != null)
        {
            Bounds wall = obstacle.bounds;
            heading = direction;
            float wallExtent = Mathf.Abs(direction.x) * wall.extents.x + Mathf.Abs(direction.z) * wall.extents.z;
            float hullExtent = Mathf.Abs(direction.x) * bounds.extents.x + Mathf.Abs(direction.z) * bounds.extents.z;
            exitProjection = Vector3.Dot(wall.center, heading) + wallExtent + hullExtent + clearance;
        }

        active = true;
        height = candidate;
        targetHeight = candidate;
        return true;
    }

    private bool CanPass(
        PhysicsScene physics,
        Bounds bounds,
        Vector3 direction,
        float distance,
        float deltaHeight,
        LayerMask layers,
        ControlUnit unit)
    {
        Vector3 vertical = deltaHeight >= 0f ? Vector3.up : Vector3.down;
        return !Blocked(
            physics,
            bounds.center,
            bounds.extents,
            vertical,
            Mathf.Abs(deltaHeight),
            layers,
            unit) && !Blocked(
                physics,
                bounds.center + Vector3.up * deltaHeight,
                bounds.extents,
                direction,
                distance,
                layers,
                unit);
    }

    internal Bounds GetHullBounds(ControlUnit unit)
    {
        if (hull == null || Time.time >= nextHullRefresh)
        {
            hull = unit.GetComponentsInChildren<Collider>();
            _body = unit.GetComponent<Rigidbody>();
            nextHullRefresh = Time.time + 1f;
        }

        Rigidbody body = _body;
        Bounds bounds = new Bounds(unit.transform.position, Vector3.one * 0.2f);
        foreach (Collider collider in hull)
        {
            if (collider != null && collider.enabled && !collider.isTrigger && collider.attachedRigidbody == body && collider.transform.IsChildOf(unit.transform))
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        return bounds;
    }

    private bool Blocked(
        PhysicsScene physics,
        Vector3 center,
        Vector3 extents,
        Vector3 direction,
        float distance,
        LayerMask layers,
        ControlUnit owner)
    {
        if (distance <= 0.05f)
        {
            return false;
        }

        int count = Cast(
            physics,
            center,
            extents,
            direction,
            distance,
            layers);
        for (int i = 0; i < count; i++)
        {
            if (IsGeometry(hits[i].collider, owner))
            {
                return true;
            }
        }

        return false;
    }

    private int Cast(
        PhysicsScene physics,
        Vector3 center,
        Vector3 extents,
        Vector3 direction,
        float distance,
        LayerMask layers)
    {
        int count;
        do
        {
            count = physics.BoxCast(
                center,
                extents,
                direction,
                hits,
                Quaternion.identity,
                distance,
                layers,
                QueryTriggerInteraction.Ignore);
            if (count < hits.Length || hits.Length >= 256)
            {
                return count;
            }

            System.Array.Resize(ref hits, hits.Length * 2);
        }
        while (true);
    }

    private static bool IsGeometry(Collider collider, ControlUnit owner)
    {
        ControlUnit unit = collider.GetComponentInParent<ControlUnit>();
        return unit != owner && (unit == null || unit.Building != null) && (collider.attachedRigidbody == null || collider.attachedRigidbody.isKinematic);
    }
}

// 职责：为自主舰选择优先翻越的静态障碍，验证上升净空并保持高度直到舰尾越障。
// 入口：TryGetHeight()；仅输出目标高度，不写刚体，不替代顶棚下的水平寻路。
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class EnemyObstacleClimb
{
    private RaycastHit[] hits = new RaycastHit[32];
    private Collider[] hull;
    private float nextHullRefresh;
    private float height;
    private float exitProjection;
    private Vector3 heading;
    private bool active;
    internal void Clear()
    {
        active = false;
        hull = null;
    }

    internal bool TryGetHeight(
        ControlUnit unit,
        Vector3 direction,
        float baseHeight,
        float lookAhead,
        float maxRise,
        float clearance,
        LayerMask layers,
        out float targetHeight)
    {
        targetHeight = baseHeight;
        if (direction.sqrMagnitude < 0.01f)
        {
            Clear();
            return false;
        }

        direction.Normalize();
        if (hull == null || Time.time >= nextHullRefresh)
        {
            hull = unit.GetComponentsInChildren<Collider>();
            nextHullRefresh = Time.time + 1f;
        }

        Rigidbody body = unit.GetComponent<Rigidbody>();
        Bounds bounds = new Bounds(unit.transform.position, Vector3.one * 0.2f);
        foreach (Collider collider in hull)
        {
            if (collider != null && collider.enabled && !collider.isTrigger && collider.attachedRigidbody == body && collider.transform.IsChildOf(unit.transform))
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (active && (Vector3.Dot(direction, heading) < 0.5f || Vector3.Dot(unit.transform.position, heading) > exitProjection))
        {
            active = false;
        }

        PhysicsScene physics = unit.gameObject.scene.GetPhysicsScene();
        int count = Cast(
            physics,
            bounds.center,
            bounds.extents,
            direction,
            lookAhead,
            layers);
        Collider obstacle = null;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (IsGeometry(hit.collider, unit) && Mathf.Abs(hit.normal.y) < 0.7f && hit.distance < nearest)
            {
                nearest = hit.distance;
                obstacle = hit.collider;
            }
        }

        float candidate = active ? height : baseHeight;
        if (obstacle != null)
        {
            float bottomOffset = unit.transform.position.y - bounds.min.y;
            candidate = Mathf.Max(candidate, obstacle.bounds.max.y + bottomOffset + clearance);
        }

        if (candidate <= baseHeight + 0.1f || candidate > baseHeight + maxRise)
        {
            active = false;
            return false;
        }

        float rise = Mathf.Max(0f, candidate - unit.transform.position.y);
        // 全舰包围盒验证垂直扫掠和高处前进走廊；顶棚/高墙不能被当作可直接翻越。
        if (Blocked(
            physics,
            bounds.center,
            bounds.extents,
            Vector3.up,
            rise,
            layers,
            unit) || Blocked(
                physics,
                bounds.center + Vector3.up * rise,
                bounds.extents,
                direction,
                lookAhead,
                layers,
                unit))
        {
            active = false;
            return false;
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

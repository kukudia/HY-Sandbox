// 职责：执行单位所属 PhysicsScene 的球形/整舰查询、碰撞体过滤与重叠分离。
// 入口：Configure()/BeginQuery()、Cast()/TryGetOverlapEscape()；缓存仅属于本单位。
// 饱和查询保守阻止通行；动态舰体缓存定期刷新，忽略条件每次公开决策/物理步失效。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class NavigationObstacleQuery
{
    private readonly Transform _transform;
    private readonly Rigidbody _body;
    private RaycastHit[] _hits = new RaycastHit[32];
    private Collider[] _overlaps = new Collider[32];
    private Collider[] _bodyColliders;
    private readonly Dictionary<Collider, bool> _ignoredColliders = new Dictionary<Collider, bool>();
    private NavigationAvoidanceSettings _settings;
    private Transform _target;
    private float _nextBodyRefresh;
    private Vector3 _samplePosition;
    private Collider _overlapObstacle;
    private Vector3 _overlapEscape;
    internal int QueryCount
    {
        get; private set;
    }
    internal bool Saturated
    {
        get; private set;
    }

    internal NavigationObstacleQuery(Transform owner)
    {
        _transform = owner;
        _body = owner.GetComponent<Rigidbody>();
        _bodyColliders = owner.GetComponentsInChildren<Collider>(true);
    }

    internal void Configure(NavigationAvoidanceSettings settings, Transform target)
    {
        _settings = settings;
        _target = target;
        _samplePosition = _transform.position;
        QueryCount = 0;
        Saturated = false;
        if (settings.UseHull && Time.time >= _nextBodyRefresh)
        {
            _bodyColliders = _transform.GetComponentsInChildren<Collider>(true);
            _nextBodyRefresh = Time.time + 1f;
        }

        BeginQuery();
    }

    internal void BeginQuery()
    {
        _ignoredColliders.Clear();
    }

    internal void UpdateHull(Bounds bounds)
    {
        _settings.HullBounds = bounds;
        _samplePosition = _transform.position;
    }

    internal void ClearOverlap()
    {
        _overlapObstacle = null;
        _overlapEscape = Vector3.zero;
    }

    internal bool Cast(Vector3 direction, float range, out RaycastHit nearest)
    {
        nearest = default;
        if (direction.sqrMagnitude < 0.01f || range <= 0f)
        {
            return false;
        }

        PhysicsScene physics = _transform.gameObject.scene.GetPhysicsScene();
        Bounds bounds = CurrentBounds();
        int count;
        do
        {
            QueryCount++;
            count = _settings.UseHull ? physics.BoxCast(
                bounds.center,
                bounds.extents,
                direction.normalized,
                _hits,
                Quaternion.identity,
                range,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore) : physics.SphereCast(
                    _transform.position,
                    Mathf.Max(0.05f, _settings.ClearanceRadius),
                    direction.normalized,
                    _hits,
                    range,
                    _settings.ObstacleMask,
                    QueryTriggerInteraction.Ignore);
            if (count < _hits.Length || _hits.Length >= 1024)
            {
                break;
            }

            System.Array.Resize(ref _hits, _hits.Length * 2);
        }
        while (true);
        if (count == _hits.Length)
        {
            // NonAlloc 饱和不保证包含最近障碍；停止而不是把截断结果误认为净空。
            Saturated = true;
            nearest = new RaycastHit
            {
                distance = 0f,
                normal = -direction.normalized
            };
            return true;
        }

        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];
            if (IsIgnored(hit.collider) || hit.distance >= nearestDistance || hit.collider == _overlapObstacle && Vector3.Dot(direction, _overlapEscape) > 0.05f)
            {
                continue;
            }

            if (_settings.UseHull && hit.distance <= 0f)
            {
                // BoxCast 的初始重叠法线可能只是 -direction；只在朝实际接触面运动时限速。
                Vector3 separation = bounds.center - ImpactPhysics.ClosestPoint(hit.collider, bounds.center);
                if (separation.sqrMagnitude < 0.0001f)
                {
                    Bounds obstacle = hit.collider.bounds;
                    Vector3 offset = bounds.center - obstacle.center;
                    Vector3 face = obstacle.extents - new Vector3(Mathf.Abs(offset.x), Mathf.Abs(offset.y), Mathf.Abs(offset.z));
                    separation = face.y <= face.x && face.y <= face.z ? Vector3.up * (offset.y >= 0f ? 1f : -1f) : face.x <= face.z ? Vector3.right * (offset.x >= 0f ? 1f : -1f) : Vector3.forward * (offset.z >= 0f ? 1f : -1f);
                }

                if (Mathf.Abs(separation.normalized.y) > 0.7f || Vector3.Dot(direction, separation) >= -0.001f)
                {
                    continue;
                }

                hit.normal = separation.normalized;
            }

            nearest = hit;
            nearestDistance = hit.distance;
        }

        return nearestDistance < float.PositiveInfinity;
    }

    // SphereCast 不报告起点内部的形状；用重叠查询和物理最小分离方向补全该边界。
    internal bool TryGetOverlapEscape(out Vector3 escape)
    {
        escape = Vector3.zero;
        _overlapObstacle = null;
        _overlapEscape = Vector3.zero;
        float radius = Mathf.Max(0.05f, _settings.ClearanceRadius);
        PhysicsScene physics = _transform.gameObject.scene.GetPhysicsScene();
        Bounds bounds = CurrentBounds();
        int count;
        do
        {
            QueryCount++;
            count = _settings.UseHull ? physics.OverlapBox(
                bounds.center,
                bounds.extents,
                _overlaps,
                Quaternion.identity,
                _settings.ObstacleMask,
                QueryTriggerInteraction.Ignore) : physics.OverlapSphere(_transform.position, radius, _overlaps, _settings.ObstacleMask, QueryTriggerInteraction.Ignore);
            if (count < _overlaps.Length || _overlaps.Length >= 1024)
            {
                break;
            }

            System.Array.Resize(ref _overlaps, _overlaps.Length * 2);
        }
        while (true);
        Saturated |= count == _overlaps.Length;
        float deepest = 0f;
        for (int i = 0; i < count; i++)
        {
            Collider obstacle = _overlaps[i];
            if (IsIgnored(obstacle))
            {
                continue;
            }

            Vector3 offset = _transform.position - ImpactPhysics.ClosestPoint(obstacle, _transform.position);
            float distance = offset.magnitude;
            Vector3 direction = distance > 0.001f ? offset / distance : Vector3.zero;
            float depth = !_settings.UseHull && distance > 0.001f ? radius - distance : 0f;
            for (int j = 0; j < _bodyColliders.Length; j++)
            {
                Collider body = _bodyColliders[j];
                if (body == null || !body.enabled || body.isTrigger || !body.gameObject.activeInHierarchy || !body.transform.IsChildOf(_transform))
                {
                    continue;
                }

                if (Physics.ComputePenetration(
                    body,
                    body.transform.position,
                    body.transform.rotation,
                    obstacle,
                    obstacle.transform.position,
                    obstacle.transform.rotation,
                    out Vector3 separation,
                    out float penetration) && penetration > depth)
                {
                    direction = separation;
                    depth = penetration;
                }
            }

            if (_settings.UseHull)
            {
                direction.y = 0f;
                direction = direction.normalized;
            }

            if (depth > deepest && direction.sqrMagnitude > 0.01f)
            {
                deepest = depth;
                escape = direction;
                _overlapObstacle = obstacle;
            }
        }

        _overlapEscape = escape;
        return deepest > 0.01f;
    }

    private Bounds CurrentBounds()
    {
        Bounds bounds = _settings.HullBounds;
        bounds.center += _transform.position - _samplePosition;
        return bounds;
    }

    private bool IsIgnored(Collider collider)
    {
        if (collider == null)
        {
            return true;
        }

        if (!_ignoredColliders.TryGetValue(collider, out bool ignored))
        {
            ignored = ShouldIgnoreCollider(collider);
            _ignoredColliders.Add(collider, ignored);
        }

        return ignored;
    }

    private bool ShouldIgnoreCollider(Collider collider)
    {
        if (collider == null || collider.transform == _transform || collider.transform.IsChildOf(_transform))
        {
            return true;
        }

        // Bot 是动态交通对象，不应进入静态净空和脱困判断。仅依赖 LayerMask
        // 不可靠：运行时生成的子碰撞体可能继承 Block/Default 层，仍会把另一台 Bot
        // 当成墙，导致多台机器人互相选绕障方向并持续制动。
        if (_settings.UseHull)
        {
            Bounds obstacle = collider.bounds;
            Bounds body = CurrentBounds();
            if (obstacle.max.y <= body.min.y + 0.05f || obstacle.min.y >= body.max.y - 0.05f)
            {
                return true;
            }

            // 自身由刚体归属过滤，目标舰和其他交通参与者仍是真实障碍。
            return _body != null && collider.attachedRigidbody == _body;
        }

        if (collider.GetComponentInParent<Bot>() != null)
        {
            return true;
        }

        // 携带物属于机器人层级；任务目标或 Home 自身可接近，其他建造部件仍是真实障碍。
        return _target != null && (collider.transform == _target || collider.transform.IsChildOf(_target));
    }
}

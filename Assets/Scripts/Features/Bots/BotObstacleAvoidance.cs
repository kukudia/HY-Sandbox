// 职责：用机器人半径探测三维净空，保持绕障方向，并在没有目标进展时选择脱困路线。
// 入口：Evaluate() 采样导航，ConstrainVelocity() 在每个物理帧阻止继续朝近处障碍加速。
// 生命周期：每台机器人拥有独立缓冲和绕障记忆；切换目标时 Reset() 清理旧路线。
using System.Collections.Generic;
using UnityEngine;

public sealed class BotObstacleAvoidance
{
    public enum HazardLevel
    {
        Emergency,
        Primary,
        Predictive
    }

    public struct DebugHit
    {
        public Vector3 HitPoint;
        public Vector3 AvoidanceVector;
        public float Distance;
        public HazardLevel Level;
    }

    private readonly Transform _transform;
    private readonly RaycastHit[] _hits = new RaycastHit[64];
    private readonly Collider[] _overlaps = new Collider[32];
    private readonly Collider[] _bodyColliders;
    private Collider _overlapObstacle;
    private Vector3 _overlapEscape;
    private readonly List<DebugHit> _debugHits = new List<DebugHit>();
    private Transform _target;
    private BotAvoidanceSettings _settings;
    private Vector3 _detourDirection;
    private float _detourUntil;
    private float _bestDistance = float.PositiveInfinity;
    private float _lastProgressTime;
    private int _clearSamples;
    public IReadOnlyList<DebugHit> DebugHits => _debugHits;

    public BotObstacleAvoidance(Transform transform)
    {
        _transform = transform;
        _bodyColliders = transform.GetComponentsInChildren<Collider>(true);
    }

    public void Reset()
    {
        _detourDirection = Vector3.zero;
        _overlapObstacle = null;
        _overlapEscape = Vector3.zero;
        _bestDistance = float.PositiveInfinity;
        _lastProgressTime = Time.time;
        _clearSamples = 0;
        _debugHits.Clear();
    }

    public void ClearDebugHits()
    {
        _debugHits.Clear();
    }

    /// <summary>优先使用真实目标净空；墙面与凹陷前允许临时离开目标方向，避免斥力相消。</summary>
    public BotAvoidanceResult Evaluate(
        Transform target,
        Vector3 velocity,
        Vector3 targetDirection,
        float rangeScale,
        BotAvoidanceSettings settings,
        float targetDistance = float.PositiveInfinity)
    {
        if (_target != target)
        {
            Reset();
        }

        _target = target;
        _settings = settings;
        _debugHits.Clear();
        if (TryGetOverlapEscape(out Vector3 escape))
        {
            _detourDirection = escape;
            _detourUntil = Time.time + Mathf.Max(0.2f, settings.DetourHoldTime);
            _lastProgressTime = Time.time;
            return new BotAvoidanceResult
            {
                Direction = escape,
                Strength = 1f,
                OverrideDirection = true,
                SpeedMultiplier = 0.4f,
                ObstacleCount = 1
            };
        }

        float radius = Mathf.Max(0.05f, settings.ClearanceRadius);
        // 返航不能缩掉本机的碰撞体半径；rangeScale 只缩小远处前瞻距离。
        float range = Mathf.Max(radius * 3f, settings.PredictiveRange * Mathf.Max(0.2f, rangeScale));
        range = Mathf.Max(range, velocity.sqrMagnitude / (2f * Mathf.Max(0.1f, settings.BrakingAcceleration)) + radius * 2f);
        float directRange = Mathf.Min(range, targetDistance);
        bool blocked = Cast(targetDirection, directRange, out RaycastHit directHit);
        bool stalled = CheckProgress(targetDistance);
        // 移动 Home 与机器人同速时距离可保持不变；仅有实际阻挡或已有绕障才触发脱困。
        stalled &= blocked || _detourDirection.sqrMagnitude > 0.01f;
        bool detouring = _detourDirection.sqrMagnitude > 0.01f;
        if (!blocked && !stalled)
        {
            _clearSamples++;
            if (!detouring || Time.time >= _detourUntil && _clearSamples >= 3)
            {
                _detourDirection = Vector3.zero;
                _overlapObstacle = null;
                _overlapEscape = Vector3.zero;
                return new BotAvoidanceResult
                {
                    SpeedMultiplier = 1f
                };
            }
        }
        else
        {
            _clearSamples = 0;
        }

        // 保持一次绕障选择，直到目标连续可见；目标拉力不再逐帧把机器人吸回墙面。
        if (!detouring || stalled && Time.time >= _detourUntil || GetClearDistance(_detourDirection, range) < radius * 2f)
        {
            _detourDirection = ChooseClearDirection(targetDirection, range, stalled);
            _detourUntil = Time.time + Mathf.Max(0.2f, settings.DetourHoldTime);
            _lastProgressTime = Time.time;
        }

        float clearDistance = GetClearDistance(_detourDirection, range);
        if (blocked)
        {
            _debugHits.Add(new DebugHit { HitPoint = directHit.point, AvoidanceVector = _detourDirection, Distance = directHit.distance, Level = directHit.distance < settings.EmergencyRange ? HazardLevel.Emergency : HazardLevel.Predictive });
        }

        return new BotAvoidanceResult
        {
            Direction = _detourDirection,
            Strength = 1f,
            OverrideDirection = true,
            SpeedMultiplier = Mathf.Lerp(0.35f, 0.85f, Mathf.Clamp01(clearDistance / range)),
            ObstacleCount = blocked ? 1 : 0
        };
    }

    private bool CheckProgress(float targetDistance)
    {
        if (targetDistance + Mathf.Max(0.05f, _settings.ProgressDistance) < _bestDistance)
        {
            _bestDistance = targetDistance;
            _lastProgressTime = Time.time;
        }

        return Time.time - _lastProgressTime >= Mathf.Max(0.5f, _settings.StuckTimeout);
    }

    private Vector3 ChooseClearDirection(Vector3 targetDirection, float range, bool stalled)
    {
        Vector3 forward = Vector3.ProjectOnPlane(targetDirection, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }

        Vector3 bestDirection = Vector3.up;
        float bestScore = float.NegativeInfinity;
        int rayCount = Mathf.Clamp(_settings.RayCount, 8, 24);
        for (int elevation = -1; elevation <= 1; elevation++)
        {
            for (int i = 0; i < rayCount; i++)
            {
                Vector3 horizontal = Quaternion.AngleAxis(360f * i / rayCount, Vector3.up) * forward;
                Vector3 candidate = (horizontal + Vector3.up * elevation).normalized;
                ConsiderDirection(
                    candidate,
                    targetDirection,
                    range,
                    stalled,
                    ref bestScore,
                    ref bestDirection);
            }
        }

        ConsiderDirection(
            Vector3.up,
            targetDirection,
            range,
            stalled,
            ref bestScore,
            ref bestDirection);
        ConsiderDirection(
            Vector3.down,
            targetDirection,
            range,
            stalled,
            ref bestScore,
            ref bestDirection);
        return bestDirection;
    }

    private void ConsiderDirection(
        Vector3 candidate,
        Vector3 targetDirection,
        float range,
        bool stalled,
        ref float bestScore,
        ref Vector3 bestDirection)
    {
        float clearance = GetClearDistance(candidate, range) / range;
        float alignment = Vector3.Dot(candidate, targetDirection);
        float continuity = Vector3.Dot(candidate, _detourDirection);
        // 净空优先；略偏向爬升，地面附近和 U 形围挡不会只在水平面来回试探。
        float score = clearance * 4f + alignment * (stalled ? 0.1f : 0.6f) + continuity * (stalled ? -0.4f : 0.3f) + candidate.y * (stalled ? 0.5f : 0.15f);
        if (score > bestScore)
        {
            bestScore = score;
            bestDirection = candidate;
        }
    }

    /// <summary>每个物理帧检查实际速度走廊；删除撞向墙面的速度分量，保留沿墙运动。</summary>
    public Vector3 ConstrainVelocity(Vector3 desiredVelocity, float deltaTime, Vector3 currentVelocity, float brakingAcceleration)
    {
        float speed = desiredVelocity.magnitude;
        if (speed < 0.01f)
        {
            return desiredVelocity;
        }

        if (TryGetOverlapEscape(out Vector3 escape) && Vector3.Dot(desiredVelocity, escape) <= 0f)
        {
            desiredVelocity = escape * Mathf.Clamp(speed, 1f, 3f);
        }

        float acceleration = Mathf.Max(0.1f, brakingAcceleration);
        float margin = Mathf.Max(0.1f, _settings.ClearanceRadius * 0.5f);
        float stoppingSpeed = Mathf.Max(speed, currentVelocity.magnitude);
        float lookAhead = stoppingSpeed * stoppingSpeed / (2f * acceleration) + stoppingSpeed * Mathf.Max(0.02f, deltaTime) + margin;
        desiredVelocity = LimitApproachSpeed(desiredVelocity, desiredVelocity.normalized, lookAhead, margin, acceleration);
        if (currentVelocity.sqrMagnitude > 0.25f)
        {
            desiredVelocity = LimitApproachSpeed(desiredVelocity, currentVelocity.normalized, lookAhead, margin, acceleration);
        }

        return desiredVelocity;
    }

    private Vector3 LimitApproachSpeed(Vector3 desiredVelocity, Vector3 corridor, float lookAhead, float margin, float acceleration)
    {
        if (!Cast(corridor, lookAhead, out RaycastHit hit))
        {
            return desiredVelocity;
        }

        float approachSpeed = -Vector3.Dot(desiredVelocity, hit.normal);
        float safeSpeed = Mathf.Sqrt(2f * acceleration * Mathf.Max(0f, hit.distance - margin)) * 0.6f;
        if (approachSpeed > safeSpeed)
        {
            desiredVelocity += hit.normal * (approachSpeed - safeSpeed);
        }

        return desiredVelocity;
    }

    private float GetClearDistance(Vector3 direction, float range)
    {
        return Cast(direction, range, out RaycastHit hit) ? hit.distance : range;
    }

    private bool Cast(Vector3 direction, float range, out RaycastHit nearest)
    {
        nearest = default;
        if (direction.sqrMagnitude < 0.01f || range <= 0f)
        {
            return false;
        }

        int count = Physics.SphereCastNonAlloc(
            _transform.position,
            Mathf.Max(0.05f, _settings.ClearanceRadius),
            direction.normalized,
            _hits,
            range,
            _settings.ObstacleMask,
            QueryTriggerInteraction.Ignore);
        float nearestDistance = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _hits[i];
            if (IsIgnored(hit.collider) || hit.distance >= nearestDistance || hit.collider == _overlapObstacle && Vector3.Dot(direction, _overlapEscape) > 0.05f)
            {
                continue;
            }

            nearest = hit;
            nearestDistance = hit.distance;
        }

        return nearestDistance < float.PositiveInfinity;
    }

    // SphereCast 不报告起点内部的形状；用重叠查询和物理最小分离方向补全该边界。
    private bool TryGetOverlapEscape(out Vector3 escape)
    {
        escape = Vector3.zero;
        _overlapObstacle = null;
        _overlapEscape = Vector3.zero;
        float radius = Mathf.Max(0.05f, _settings.ClearanceRadius);
        int count = Physics.OverlapSphereNonAlloc(_transform.position, radius, _overlaps, _settings.ObstacleMask, QueryTriggerInteraction.Ignore);
        float deepest = 0f;
        for (int i = 0; i < count; i++)
        {
            Collider obstacle = _overlaps[i];
            if (IsIgnored(obstacle))
            {
                continue;
            }

            Vector3 offset = _transform.position - obstacle.ClosestPoint(_transform.position);
            float distance = offset.magnitude;
            Vector3 direction = distance > 0.001f ? offset / distance : Vector3.zero;
            float depth = distance > 0.001f ? radius - distance : 0f;
            for (int j = 0; j < _bodyColliders.Length; j++)
            {
                Collider body = _bodyColliders[j];
                if (body == null || !body.enabled || body.isTrigger || !body.gameObject.activeInHierarchy)
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

    private bool IsIgnored(Collider collider)
    {
        if (collider == null || collider.transform == _transform || collider.transform.IsChildOf(_transform))
        {
            return true;
        }

        // Bot 是动态交通对象，不应进入静态净空和脱困判断。仅依赖 LayerMask
        // 不可靠：运行时生成的子碰撞体可能继承 Block/Default 层，仍会把另一台 Bot
        // 当成墙，导致多台机器人互相选绕障方向并持续制动。
        if (collider.GetComponentInParent<Bot>() != null)
        {
            return true;
        }

        // 携带物属于机器人层级；任务目标或 Home 自身可接近，其他建造部件仍是真实障碍。
        return _target != null && (collider.transform == _target || collider.transform.IsChildOf(_target));
    }
}

// 职责：为 Bot 与舰船探测球形/整舰净空，保持绕障方向，并在没有目标进展时选择脱困路线。
// 入口：Evaluate() 采样导航，ConstrainVelocity() 在每个物理帧阻止继续朝近处障碍加速。
// 生命周期：每个单位拥有独立缓冲和绕障记忆；切换目标时 Reset() 清理旧路线。
using System.Collections.Generic;
using UnityEngine;

public sealed class NavigationAvoidance
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

    private readonly NavigationObstacleQuery _query;
    private readonly List<DebugHit> _debugHits = new List<DebugHit>();
    private Transform _target;
    private NavigationAvoidanceSettings _settings;
    private Vector3 _detourDirection;
    private float _detourUntil;
    private float _bestDistance = float.PositiveInfinity;
    private float _lastProgressTime;
    private int _clearSamples;
    private bool _escapingOverlap;
    public IReadOnlyList<DebugHit> DebugHits => _debugHits;
    public int QueryCount => _query.QueryCount;
    public bool Saturated => _query.Saturated;
    public bool Detouring => _detourDirection.sqrMagnitude > 0.01f;

    public NavigationAvoidance(Transform owner)
    {
        _query = new NavigationObstacleQuery(owner);
    }

    public void UpdateHull(Bounds bounds)
    {
        _query.UpdateHull(bounds);
    }

    public void Configure(NavigationAvoidanceSettings settings)
    {
        _settings = settings;
        _query.Configure(settings, _target);
    }

    public void Reset()
    {
        _detourDirection = Vector3.zero;
        _query.ClearOverlap();
        _bestDistance = float.PositiveInfinity;
        _lastProgressTime = Time.time;
        _clearSamples = 0;
        _escapingOverlap = false;
        _debugHits.Clear();
        _query.BeginQuery();
    }

    public void ClearDebugHits()
    {
        _debugHits.Clear();
    }

    /// <summary>优先使用真实目标净空；墙面与凹陷前允许临时离开目标方向，避免斥力相消。</summary>
    public NavigationAvoidanceResult Evaluate(
        Transform target,
        Vector3 velocity,
        Vector3 targetDirection,
        float rangeScale,
        NavigationAvoidanceSettings settings,
        float targetDistance = float.PositiveInfinity)
    {
        if (_target != target)
        {
            Reset();
        }

        _target = target;
        Configure(settings);
        _query.BeginQuery();
        _debugHits.Clear();
        if (_query.TryGetOverlapEscape(out Vector3 escape))
        {
            _escapingOverlap = true;
            _detourDirection = escape;
            _detourUntil = Time.time + Mathf.Max(0.2f, settings.DetourHoldTime);
            _lastProgressTime = Time.time;
            return new NavigationAvoidanceResult
            {
                Direction = escape,
                Strength = 1f,
                OverrideDirection = true,
                SpeedMultiplier = 0.4f,
                ObstacleCount = 1
            };
        }

        // 分离方向只用于退出重叠；退出后重新选路，避免继续背离目标整段保持时间。
        if (_escapingOverlap)
        {
            _escapingOverlap = false;
            _detourDirection = Vector3.zero;
            _bestDistance = targetDistance;
            _lastProgressTime = Time.time;
        }

        float radius = Mathf.Max(0.05f, settings.ClearanceRadius);
        // 返航不能缩掉本机的碰撞体半径；rangeScale 只缩小远处前瞻距离。
        float range = Mathf.Max(radius * 3f, settings.PredictiveRange * Mathf.Max(0.2f, rangeScale));
        range = Mathf.Max(range, velocity.sqrMagnitude / (2f * Mathf.Max(0.1f, settings.BrakingAcceleration)) + radius * 2f);
        float directRange = Mathf.Min(range, targetDistance);
        bool blocked = _query.Cast(targetDirection, directRange, out RaycastHit directHit);
        bool stalled = CheckProgress(targetDistance);
        // 移动 Home 与机器人同速时距离可保持不变；仅有实际阻挡或已有绕障才触发脱困。
        stalled &= blocked || _detourDirection.sqrMagnitude > 0.01f;
        bool detouring = _detourDirection.sqrMagnitude > 0.01f;
        if (!blocked && !stalled)
        {
            _clearSamples++;
            if (!detouring || _clearSamples >= 2)
            {
                _detourDirection = Vector3.zero;
                _query.ClearOverlap();
                return new NavigationAvoidanceResult
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
        bool chooseNewDirection = !detouring || stalled && Time.time >= _detourUntil;
        float clearDistance = chooseNewDirection ? 0f : GetClearDistance(_detourDirection, range);
        if (chooseNewDirection || clearDistance < radius * 2f)
        {
            _detourDirection = ChooseClearDirection(targetDirection, range, stalled, out clearDistance);
            _detourUntil = Time.time + Mathf.Max(0.2f, settings.DetourHoldTime);
            _lastProgressTime = Time.time;
            _bestDistance = targetDistance;
        }

        if (blocked)
        {
            _debugHits.Add(new DebugHit { HitPoint = directHit.point, AvoidanceVector = _detourDirection, Distance = directHit.distance, Level = directHit.distance < settings.EmergencyRange ? HazardLevel.Emergency : HazardLevel.Predictive });
        }

        return new NavigationAvoidanceResult
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

        float timeout = Mathf.Max(0.5f, _settings.StuckTimeout);
        if (Time.time - _lastProgressTime < timeout)
        {
            return false;
        }

        // 绕路或目标移动后重建进展窗口，不要求先突破整段任务的历史最近距离。
        _bestDistance = targetDistance;
        _lastProgressTime = Time.time;
        return true;
    }

    private Vector3 ChooseClearDirection(Vector3 targetDirection, float range, bool stalled, out float bestClearDistance)
    {
        Vector3 forward = Vector3.ProjectOnPlane(targetDirection, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }

        Vector3 bestDirection = Vector3.up;
        float bestScore = float.NegativeInfinity;
        bestClearDistance = 0f;
        int rayCount = Mathf.Clamp(_settings.RayCount, 8, 24);
        int minElevation = _settings.UseHull ? 0 : -1;
        int maxElevation = _settings.UseHull ? 0 : 1;
        for (int elevation = minElevation; elevation <= maxElevation; elevation++)
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
                    ref bestDirection,
                    ref bestClearDistance);
            }
        }

        if (!_settings.UseHull)
        {
            ConsiderDirection(
                Vector3.up,
                targetDirection,
                range,
                stalled,
                ref bestScore,
                ref bestDirection,
                ref bestClearDistance);
            ConsiderDirection(
                Vector3.down,
                targetDirection,
                range,
                stalled,
                ref bestScore,
                ref bestDirection,
                ref bestClearDistance);
        }

        return bestDirection;
    }

    private void ConsiderDirection(
        Vector3 candidate,
        Vector3 targetDirection,
        float range,
        bool stalled,
        ref float bestScore,
        ref Vector3 bestDirection,
        ref float bestClearDistance)
    {
        float clearDistance = GetClearDistance(candidate, range);
        float clearance = clearDistance / range;
        float alignment = Vector3.Dot(candidate, targetDirection);
        float continuity = Vector3.Dot(candidate, _detourDirection);
        // 净空优先；略偏向爬升，地面附近和 U 形围挡不会只在水平面来回试探。
        float score = clearance * 4f + alignment * (stalled ? 0.1f : 0.6f) + continuity * (stalled ? -0.4f : 0.3f) + candidate.y * (stalled ? 0.5f : 0.15f);
        if (score > bestScore)
        {
            bestScore = score;
            bestDirection = candidate;
            bestClearDistance = clearDistance;
        }
    }

    /// <summary>每个物理帧检查实际速度走廊；删除撞向墙面的速度分量，保留沿墙运动。</summary>
    public Vector3 ConstrainVelocity(Vector3 desiredVelocity, float deltaTime, Vector3 currentVelocity, float brakingAcceleration)
    {
        _query.BeginQuery();
        float speed = desiredVelocity.magnitude;
        if (Saturated)
        {
            return Vector3.zero;
        }

        if (speed < 0.01f)
        {
            return desiredVelocity;
        }

        if (_query.TryGetOverlapEscape(out Vector3 escape) && Vector3.Dot(desiredVelocity, escape) <= 0f)
        {
            desiredVelocity = escape * Mathf.Clamp(speed, 1f, 3f);
        }

        float acceleration = Mathf.Max(0.1f, brakingAcceleration);
        float margin = Mathf.Max(0.1f, _settings.ClearanceRadius * 0.5f);
        float stoppingSpeed = Mathf.Max(speed, currentVelocity.magnitude);
        // 按可执行制动距离介入，不额外放大走廊；持续到达优先，近场仍逐物理步限速。
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
        if (!_query.Cast(corridor, lookAhead, out RaycastHit hit))
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
        return _query.Cast(direction, range, out RaycastHit hit) ? hit.distance : range;
    }
}

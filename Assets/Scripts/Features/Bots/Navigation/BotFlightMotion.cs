// 职责：缓存导航采样、平滑飞行方向，执行任务飞行与移动 Home 的相对速度制动。
// 入口：NavigateToTarget()/NavigateHome()；只能由机器人 FixedUpdate 导航路径调用。
// 不变量：Home 目标点每次重新计算；只缓存避障建议，不缓存移动目标的世界位置。
using System.Collections.Generic;
using UnityEngine;

public sealed class BotFlightMotion
{
    private readonly Transform _transform;
    private readonly Rigidbody _rigidbody;
    private readonly BotObstacleAvoidance _obstacles;
    private Transform _cachedTarget;
    private BotAvoidanceResult _cachedResult;
    private Vector3 _direction;
    private Vector3 _avoidanceDirection;
    private float _nextSampleTime;
    private float _cachedRangeScale = 1f;
    private readonly BotAvoidanceSettings _avoidance = BotNavigationPolicy.CreateAvoidanceSettings();
    public Vector3 Direction => _direction;
    public Vector3 AvoidanceDirection => _avoidanceDirection;
    public IReadOnlyList<BotObstacleAvoidance.DebugHit> DebugHits => _obstacles.DebugHits;

    public BotFlightMotion(Transform transform, Rigidbody rigidbody)
    {
        _transform = transform;
        _rigidbody = rigidbody;
        _obstacles = new BotObstacleAvoidance(transform);
    }

    public void ResetDirection()
    {
        _direction = _transform.forward;
    }

    public void ClearTarget()
    {
        _cachedTarget = null;
        _nextSampleTime = 0f;
        _cachedResult = default;
        _obstacles.Reset();
    }

    public void NavigateToTarget(Transform target)
    {
        NavigateToPosition(target.position, target, null, false, 1f);
    }

    public void NavigateHome(Transform home, Vector3 approachPosition, Rigidbody homeRigidbody, float rangeScale)
    {
        NavigateToPosition(approachPosition, home, homeRigidbody, true, rangeScale);
    }

    private void NavigateToPosition(
        Vector3 targetPosition,
        Transform targetReference,
        Rigidbody homeRigidbody,
        bool returningHome,
        float avoidanceRangeScale)
    {
        avoidanceRangeScale = Mathf.Clamp01(avoidanceRangeScale);
        // 计算目标方向
        Vector3 targetOffset = targetPosition - _transform.position;
        float targetDistance = targetOffset.magnitude;
        Vector3 targetDirection = targetDistance > 1e-5f ? targetOffset / targetDistance : Vector3.zero;
        SampleGuidance(targetReference, targetDirection, targetDistance, avoidanceRangeScale);
        UpdateDirectionAndSpeed(targetDirection);
        ApplyMovement(targetPosition, targetDirection, targetDistance, homeRigidbody, returningHome);
        _avoidanceDirection = _cachedResult.Direction;
    }

    private void SampleGuidance(
        Transform targetReference,
        Vector3 targetDirection,
        float targetDistance,
        float avoidanceRangeScale)
    {
        // 高速时缩短采样间隔，前瞻距离另由实际速度的制动距离扩展。
        if (targetReference != _cachedTarget || Time.time >= _nextSampleTime || Mathf.Abs(avoidanceRangeScale - _cachedRangeScale) >= 0.15f)
        {
            _cachedTarget = targetReference;
            _cachedRangeScale = avoidanceRangeScale;
            _nextSampleTime = Time.time + Mathf.Clamp(
                2f / Mathf.Max(1f, _rigidbody.linearVelocity.magnitude),
                Time.fixedDeltaTime,
                BotNavigationPolicy.SampleInterval);
            _cachedResult = _obstacles.Evaluate(
                targetReference,
                _rigidbody.linearVelocity,
                targetDirection,
                avoidanceRangeScale,
                _avoidance,
                targetDistance);
        }
    }

    private void UpdateDirectionAndSpeed(Vector3 targetDirection)
    {
        // 目标方向使用当前世界坐标实时计算；避障查询可以降频，但移动中的 home 不能使用旧位置。
        Vector3 finalDirection = _cachedResult.OverrideDirection ? _cachedResult.Direction : targetDirection;
        if (_direction.sqrMagnitude < 0.001f)
        {
            _direction = _transform.forward;
        }

        _direction = Vector3.RotateTowards(_direction, finalDirection, BotNavigationPolicy.DirectionResponseRate * Time.fixedDeltaTime, 0f);
        // 旋转朝向移动方向
        if (_direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_direction);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, BotNavigationPolicy.RotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void ApplyMovement(
        Vector3 targetPosition,
        Vector3 targetDirection,
        float targetDistance,
        Rigidbody homeRigidbody,
        bool returningHome)
    {
        Vector3 frameVelocity = returningHome && homeRigidbody != null ? homeRigidbody.GetPointVelocity(targetPosition) : Vector3.zero;
        float remainingDistance = Mathf.Max(0f, targetDistance - (returningHome ? BotNavigationPolicy.DockingApproachTolerance : 0f));
        // 没有巡航速度上限：航段越长可加速越高；0.8 为离散物理/方向采样留出制动余量。
        float desiredSpeed = Mathf.Sqrt(2f * BotNavigationPolicy.Acceleration * remainingDistance) * 0.8f;
        if (_cachedResult.OverrideDirection)
        {
            desiredSpeed *= _cachedResult.SpeedMultiplier;
        }
        else
        {
            // 急转时先消除旧方向速度，避免高速画圈或从目标侧面掠过。
            desiredSpeed *= Mathf.Clamp01(Vector3.Dot(_direction, targetDirection));
        }

        Vector3 desiredVelocity = frameVelocity + _direction.normalized * desiredSpeed;
        desiredVelocity = _obstacles.ConstrainVelocity(desiredVelocity, Time.fixedDeltaTime, _rigidbody.linearVelocity, BotNavigationPolicy.Acceleration);
        Vector3 requestedAcceleration = (desiredVelocity - _rigidbody.linearVelocity) / Mathf.Max(Time.fixedDeltaTime, 0.001f);
        Vector3 acceleration = Vector3.ClampMagnitude(requestedAcceleration, BotNavigationPolicy.Acceleration);
        if (_rigidbody.linearVelocity.sqrMagnitude > 1f)
        {
            // 制动优先；横向急转不能挤占高速走廊所依赖的纵向减速度。
            Vector3 heading = _rigidbody.linearVelocity.normalized;
            float braking = Mathf.Clamp(Vector3.Dot(requestedAcceleration, heading), -BotNavigationPolicy.Acceleration, 0f);
            float turningBudget = Mathf.Sqrt(Mathf.Max(0f, BotNavigationPolicy.Acceleration * BotNavigationPolicy.Acceleration - braking * braking));
            if (braking < 0f)
            {
                acceleration = heading * braking + Vector3.ClampMagnitude(Vector3.ProjectOnPlane(requestedAcceleration, heading), turningBudget);
            }
        }

        _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
    }
}

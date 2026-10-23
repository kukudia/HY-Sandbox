// 职责：缓存导航采样、平滑飞行方向，执行任务飞行与移动 Home 的相对速度制动。
// 入口：NavigateToTarget()/NavigateHome()；只能由机器人 FixedUpdate 导航路径调用。
// 不变量：Home 目标点每次重新计算；只缓存避障建议，不缓存移动目标的世界位置。
using System.Collections.Generic;
using UnityEngine;

public sealed class BotFlightMotion
{
    public struct Settings
    {
        public float SampleInterval;
        public float DirectionResponseRate;
        public float RotationSpeed;
        public float MovementSpeed;
        public float DockingApproachTolerance;
        public float ReturnBrakingAcceleration;
        public float ReturnStopSpeed;
        public float ReturnBrakeDistance;
        public BotAvoidanceSettings Avoidance;
    }

    private readonly Transform _transform;
    private readonly Rigidbody _rigidbody;
    private readonly BotObstacleAvoidance _obstacles;
    private Transform _cachedTarget;
    private BotAvoidanceResult _cachedResult;
    private Vector3 _direction;
    private Vector3 _avoidanceDirection;
    private float _speedMultiplier = 1f;
    private float _nextSampleTime;
    private float _cachedRangeScale = 1f;
    private Settings _settings;
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
        _speedMultiplier = 1f;
        _obstacles.Reset();
    }

    public void NavigateToTarget(Transform target, Settings settings)
    {
        NavigateToPosition(
            target.position,
            target,
            null,
            false,
            1f,
            settings);
    }

    public void NavigateHome(
        Transform home,
        Vector3 approachPosition,
        Rigidbody homeRigidbody,
        float rangeScale,
        Settings settings)
    {
        NavigateToPosition(
            approachPosition,
            home,
            homeRigidbody,
            true,
            rangeScale,
            settings);
    }

    private void NavigateToPosition(
        Vector3 targetPosition,
        Transform targetReference,
        Rigidbody homeRigidbody,
        bool returningHome,
        float avoidanceRangeScale,
        Settings settings)
    {
        _settings = settings;
        avoidanceRangeScale = Mathf.Clamp01(avoidanceRangeScale);
        // 计算目标方向
        Vector3 targetOffset = targetPosition - _transform.position;
        float targetDistance = targetOffset.magnitude;
        Vector3 targetDirection = targetDistance > 1e-5f ? targetOffset / targetDistance : Vector3.zero;
        SampleGuidance(targetReference, targetDirection, targetDistance, avoidanceRangeScale);
        UpdateDirectionAndSpeed(targetDirection, avoidanceRangeScale);
        ApplyMovement(targetPosition, targetDirection, targetDistance, homeRigidbody, returningHome);
        _avoidanceDirection = _cachedResult.Direction;
    }

    private void SampleGuidance(
        Transform targetReference,
        Vector3 targetDirection,
        float targetDistance,
        float avoidanceRangeScale)
    {
        // 原 Prefab 配置一秒采样会错过快速飞行中的墙；保留 Inspector 值但限定安全最大间隔。
        if (targetReference != _cachedTarget || Time.time >= _nextSampleTime || Mathf.Abs(avoidanceRangeScale - _cachedRangeScale) >= 0.15f)
        {
            _cachedTarget = targetReference;
            _cachedRangeScale = avoidanceRangeScale;
            _nextSampleTime = Time.time + Mathf.Clamp(_settings.SampleInterval, 0.05f, 0.2f);
            _cachedResult = _obstacles.Evaluate(
                targetReference,
                _rigidbody.linearVelocity,
                targetDirection,
                avoidanceRangeScale,
                _settings.Avoidance,
                targetDistance);
        }
    }

    private void UpdateDirectionAndSpeed(Vector3 targetDirection, float avoidanceRangeScale)
    {
        // 目标方向使用当前世界坐标实时计算；避障查询可以降频，但移动中的 home 不能使用旧位置。
        Vector3 finalDirection = _cachedResult.OverrideDirection ? _cachedResult.Direction : targetDirection;
        if (_direction.sqrMagnitude < 0.001f)
        {
            _direction = _transform.forward;
        }

        _direction = Vector3.RotateTowards(
            _direction,
            finalDirection,
            Mathf.Max(0.1f, _settings.DirectionResponseRate) * Time.fixedDeltaTime,
            0f);
        // 根据障碍物密度调整速度
        float targetSpeedMultiplier = _cachedResult.OverrideDirection ? _cachedResult.SpeedMultiplier : Mathf.Lerp(1f, _cachedResult.SpeedMultiplier, avoidanceRangeScale);
        _speedMultiplier = Mathf.Lerp(_speedMultiplier, targetSpeedMultiplier, Time.fixedDeltaTime * 3f);
        // 旋转朝向移动方向
        if (_direction.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_direction);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, _settings.RotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void ApplyMovement(
        Vector3 targetPosition,
        Vector3 targetDirection,
        float targetDistance,
        Rigidbody homeRigidbody,
        bool returningHome)
    {
        // 应用移动力
        float effectiveSpeed = _settings.MovementSpeed * _speedMultiplier;
        if (returningHome)
        {
            ApplyReturnHomeMovement(targetPosition, targetDirection, targetDistance, effectiveSpeed, homeRigidbody);
        }
        else
        {
            // 导航方向决定平移，模型转向只影响外观；否则机身转向滞后时会持续顶墙。
            Vector3 desiredVelocity = _direction.normalized * effectiveSpeed;
            desiredVelocity = _obstacles.ConstrainVelocity(
                desiredVelocity,
                Time.fixedDeltaTime,
                _rigidbody.linearVelocity,
                _settings.ReturnBrakingAcceleration);
            Vector3 acceleration = Vector3.ClampMagnitude(
                (desiredVelocity - _rigidbody.linearVelocity) / Mathf.Max(0.001f, Time.fixedDeltaTime),
                Mathf.Max(1f, _settings.ReturnBrakingAcceleration, effectiveSpeed * 2f));
            _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
        }
    }

    private void ApplyReturnHomeMovement(
        Vector3 targetPosition,
        Vector3 targetDirection,
        float targetDistance,
        float effectiveSpeed,
        Rigidbody homeRigidbody)
    {
        float remainingDistance = Mathf.Max(0f, targetDistance - _settings.DockingApproachTolerance);
        float brakingAcceleration = Mathf.Max(0.1f, _settings.ReturnBrakingAcceleration);
        Vector3 homeVelocity = homeRigidbody != null ? homeRigidbody.GetPointVelocity(targetPosition) : Vector3.zero;
        // 追赶移动中的载体时，Bot 需要比载体保留相对速度；否则载体一加速，返航距离会持续增加。
        float chaseSpeed = homeVelocity.magnitude * 1.25f;
        float maxReturnSpeed = Mathf.Max(_settings.ReturnStopSpeed, effectiveSpeed * 2f, chaseSpeed);
        float stoppingLimitedSpeed = Mathf.Sqrt(2f * brakingAcceleration * remainingDistance);
        float distanceLimitedSpeed = Mathf.Lerp(
            _settings.ReturnStopSpeed,
            maxReturnSpeed,
            Mathf.Clamp01(remainingDistance / Mathf.Max(0.1f, _settings.ReturnBrakeDistance)));
        float facingAlignment = Mathf.Clamp01(Vector3.Dot(_transform.forward, targetDirection));
        float desiredRelativeSpeed = Mathf.Min(stoppingLimitedSpeed, distanceLimitedSpeed, maxReturnSpeed);
        desiredRelativeSpeed = Mathf.Max(_settings.ReturnStopSpeed, desiredRelativeSpeed * Mathf.Lerp(0.35f, 1f, facingAlignment));
        Vector3 desiredWorldVelocity = homeVelocity + _direction.normalized * desiredRelativeSpeed;
        desiredWorldVelocity = _obstacles.ConstrainVelocity(desiredWorldVelocity, Time.fixedDeltaTime, _rigidbody.linearVelocity, brakingAcceleration);
        Vector3 velocityDelta = desiredWorldVelocity - _rigidbody.linearVelocity;
        Vector3 acceleration = Vector3.ClampMagnitude(
            velocityDelta / Mathf.Max(Time.fixedDeltaTime, 0.001f),
            Mathf.Max(brakingAcceleration, chaseSpeed * 2f));
        _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
    }
}

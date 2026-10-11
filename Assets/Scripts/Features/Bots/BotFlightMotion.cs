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
        public float TargetDirectionWeight;
        public float DockingApproachTolerance;
        public float ReturnBrakingAcceleration;
        public float ReturnStopSpeed;
        public float ReturnBrakeDistance;
        public BotAvoidanceSettings Avoidance;
        public BotHomeGuidance.Settings HomeGuidance;
    }

    private readonly Transform _transform;
    private readonly Rigidbody _rigidbody;
    private readonly BotObstacleAvoidance _obstacles;
    private readonly BotHomeGuidance _homeGuidance;
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
        _homeGuidance = new BotHomeGuidance(transform);
    }

    public void ResetDirection()
    {
        _direction = _transform.forward;
    }

    public void ClearTarget()
    {
        _cachedTarget = null;
    }

    public void NavigateToTarget(Transform target, Settings settings)
    {
        NavigateToPosition(
            target.position,
            target,
            Vector3.zero,
            null,
            false,
            1f,
            120f,
            settings);
    }

    public void NavigateHome(
        Transform home,
        Vector3 homeOffset,
        Vector3 approachPosition,
        Rigidbody homeRigidbody,
        float rangeScale,
        float maxAvoidanceAngle,
        Settings settings)
    {
        NavigateToPosition(
            approachPosition,
            home,
            homeOffset,
            homeRigidbody,
            true,
            rangeScale,
            maxAvoidanceAngle,
            settings);
    }

    private void NavigateToPosition(
        Vector3 targetPosition,
        Transform targetReference,
        Vector3 homeOffset,
        Rigidbody homeRigidbody,
        bool returningHome,
        float avoidanceRangeScale,
        float maxAvoidanceAngle,
        Settings settings)
    {
        _settings = settings;
        avoidanceRangeScale = Mathf.Clamp01(avoidanceRangeScale);
        // 计算目标方向
        Vector3 targetDirection = (targetPosition - _transform.position).normalized;
        SampleGuidance(targetReference, homeOffset, targetDirection, returningHome, avoidanceRangeScale);
        UpdateDirectionAndSpeed(targetDirection, avoidanceRangeScale, maxAvoidanceAngle);
        ApplyMovement(targetPosition, targetDirection, homeRigidbody, returningHome);
        _avoidanceDirection = Vector3.Lerp(Vector3.zero, _cachedResult.Direction, avoidanceRangeScale);
    }

    private void SampleGuidance(
        Transform targetReference,
        Vector3 homeOffset,
        Vector3 targetDirection,
        bool returningHome,
        float avoidanceRangeScale)
    {
        // 避障查询按固定间隔采样，物理帧之间只渐进跟随缓存结果，避免方向高频抖动和重复 Raycast。
        if (targetReference != _cachedTarget || Time.time >= _nextSampleTime || Mathf.Abs(avoidanceRangeScale - _cachedRangeScale) >= 0.15f)
        {
            _cachedTarget = targetReference;
            _cachedRangeScale = avoidanceRangeScale;
            _nextSampleTime = Time.time + Mathf.Max(0.05f, _settings.SampleInterval);
            if (returningHome)
            {
                _obstacles.ClearDebugHits();
            }

            _cachedResult = returningHome ? _homeGuidance.Evaluate(targetReference, homeOffset, targetDirection, avoidanceRangeScale, _settings.HomeGuidance) : _obstacles.Evaluate(
                targetReference,
                _rigidbody.linearVelocity,
                targetDirection,
                avoidanceRangeScale,
                _settings.Avoidance);
        }
    }

    private void UpdateDirectionAndSpeed(Vector3 targetDirection, float avoidanceRangeScale, float maxAvoidanceAngle)
    {
        // 目标方向使用当前世界坐标实时计算；避障查询可以降频，但移动中的 home 不能使用旧位置。
        Vector3 finalDirection = BlendDirections(
            targetDirection,
            _cachedResult.Direction,
            _cachedResult.Strength * avoidanceRangeScale,
            maxAvoidanceAngle);
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
        float targetSpeedMultiplier = Mathf.Lerp(1f, _cachedResult.SpeedMultiplier, avoidanceRangeScale);
        _speedMultiplier = Mathf.Lerp(_speedMultiplier, targetSpeedMultiplier, Time.fixedDeltaTime * 3f);
        // 旋转朝向移动方向
        if (_direction.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_direction);
            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, _settings.RotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void ApplyMovement(Vector3 targetPosition, Vector3 targetDirection, Rigidbody homeRigidbody, bool returningHome)
    {
        // 应用移动力
        float effectiveSpeed = _settings.MovementSpeed * _speedMultiplier;
        if (returningHome)
        {
            ApplyReturnHomeMovement(targetPosition, targetDirection, effectiveSpeed, homeRigidbody);
        }
        else
        {
            _rigidbody.AddForce(_transform.forward * effectiveSpeed, ForceMode.Acceleration);
            // 限制最大速度
            if (_rigidbody.linearVelocity.magnitude > effectiveSpeed * 2f)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * effectiveSpeed * 2f;
            }
        }
    }

    private void ApplyReturnHomeMovement(Vector3 targetPosition, Vector3 targetDirection, float effectiveSpeed, Rigidbody homeRigidbody)
    {
        float remainingDistance = Mathf.Max(0f, Vector3.Distance(_transform.position, targetPosition) - _settings.DockingApproachTolerance);
        float brakingAcceleration = Mathf.Max(0.1f, _settings.ReturnBrakingAcceleration);
        float maxReturnSpeed = Mathf.Max(_settings.ReturnStopSpeed, effectiveSpeed * 2f);
        float stoppingLimitedSpeed = Mathf.Sqrt(2f * brakingAcceleration * remainingDistance);
        float distanceLimitedSpeed = Mathf.Lerp(
            _settings.ReturnStopSpeed,
            maxReturnSpeed,
            Mathf.Clamp01(remainingDistance / Mathf.Max(0.1f, _settings.ReturnBrakeDistance)));
        float facingAlignment = Mathf.Clamp01(Vector3.Dot(_transform.forward, targetDirection));
        float desiredRelativeSpeed = Mathf.Min(stoppingLimitedSpeed, distanceLimitedSpeed, maxReturnSpeed);
        desiredRelativeSpeed = Mathf.Max(_settings.ReturnStopSpeed, desiredRelativeSpeed * Mathf.Lerp(0.35f, 1f, facingAlignment));
        Vector3 homeVelocity = homeRigidbody != null ? homeRigidbody.GetPointVelocity(targetPosition) : Vector3.zero;
        Vector3 desiredWorldVelocity = homeVelocity + _direction.normalized * desiredRelativeSpeed;
        Vector3 velocityDelta = desiredWorldVelocity - _rigidbody.linearVelocity;
        Vector3 acceleration = Vector3.ClampMagnitude(velocityDelta / Mathf.Max(Time.fixedDeltaTime, 0.001f), brakingAcceleration);
        _rigidbody.AddForce(acceleration, ForceMode.Acceleration);
    }

    private Vector3 BlendDirections(Vector3 targetDir, Vector3 avoidanceDir, float avoidanceStrength, float maxAvoidanceAngle)
    {
        if (avoidanceStrength < 0.01f)
        {
            return targetDir;
        }

        // 动态调整权重
        float targetWeight = _settings.TargetDirectionWeight * (1f - avoidanceStrength * 0.5f);
        float avoidanceWeight = Mathf.Lerp(1f, 10f, avoidanceStrength);
        Vector3 blended = (targetDir * targetWeight + avoidanceDir * avoidanceWeight).normalized;
        // 确保不会完全偏离目标
        float maxDeviation = Mathf.Min(Mathf.Lerp(90f, 120f, avoidanceStrength), Mathf.Clamp(maxAvoidanceAngle, 0f, 180f));
        float angle = Vector3.Angle(targetDir, blended);
        if (angle > maxDeviation)
        {
            blended = Vector3.RotateTowards(targetDir, blended, maxDeviation * Mathf.Deg2Rad, 0f);
        }

        return blended.normalized;
    }
}

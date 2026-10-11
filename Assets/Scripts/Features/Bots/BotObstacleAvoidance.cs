// 职责：采样环形射线、近邻碰撞体和速度前方障碍，并合成原有三档避障结果。
// 入口：Evaluate()；BotFlightMotion 按固定间隔调用，不修改刚体或导航状态。
// 生命周期：每台机器人拥有自己的非分配缓冲；DebugHits 只描述最近一次障碍采样。
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

    private struct Forces
    {
        public Vector3 Emergency;
        public Vector3 Primary;
        public Vector3 Predictive;
        public int EmergencyCount;
        public int PrimaryCount;
        public int PredictiveCount;
    }

    private readonly Transform _transform;
    private readonly Collider[] _nearbyColliders = new Collider[32];
    private readonly List<DebugHit> _debugHits = new List<DebugHit>();
    private Transform _target;
    private BotAvoidanceSettings _settings;
    private float _emergencyRange;
    private float _primaryRange;
    private float _predictiveRange;
    public IReadOnlyList<DebugHit> DebugHits => _debugHits;

    public BotObstacleAvoidance(Transform transform)
    {
        _transform = transform;
    }

    public void ClearDebugHits()
    {
        _debugHits.Clear();
    }

    /// <summary>在当前机器人位置采样；返回避障建议，不写入刚体。</summary>
    public BotAvoidanceResult Evaluate(
        Transform target,
        Vector3 velocity,
        Vector3 targetDirection,
        float rangeScale,
        BotAvoidanceSettings settings)
    {
        _target = target;
        _settings = settings;
        _debugHits.Clear();
        rangeScale = Mathf.Clamp01(rangeScale);
        if (rangeScale <= 0.01f)
        {
            return new BotAvoidanceResult
            {
                SpeedMultiplier = 1f
            };
        }

        _emergencyRange = settings.EmergencyRange * rangeScale;
        _primaryRange = settings.PrimaryRange * rangeScale;
        _predictiveRange = settings.PredictiveRange * rangeScale;
        Forces forces = new Forces();
        SampleRadialRays(ref forces);
        SampleNearbyColliders(ref forces);
        SampleForwardPrediction(velocity, targetDirection, ref forces);
        return CombineForces(forces);
    }

    private void SampleRadialRays(ref Forces forces)
    {
        float angleStep = 360f / _settings.RayCount;
        for (int i = 0; i < _settings.RayCount; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(angleStep * i, Vector3.up) * Vector3.forward;
            if (!Physics.Raycast(_transform.position, direction, out RaycastHit hit, _predictiveRange, _settings.ObstacleMask))
            {
                continue;
            }

            // 保持任务目标与地面的原有过滤语义；前方预测使用单独的地面判定。
            if (hit.transform.IsChildOf(_transform) || (_target != null && hit.transform.IsChildOf(_target)) || Vector3.Dot(hit.normal, Vector3.up) >= _settings.GroundNormalThreshold || hit.point.y - _transform.position.y < _settings.GroundHeightThreshold)
            {
                continue;
            }

            AccumulateRayHit(hit, ref forces);
        }
    }

    private void SampleNearbyColliders(ref Forces forces)
    {
        int count = Physics.OverlapSphereNonAlloc(_transform.position, _primaryRange, _nearbyColliders, _settings.ObstacleMask);
        for (int i = 0; i < count; i++)
        {
            Collider collider = _nearbyColliders[i];
            if (collider.transform == _transform || (_target != null && collider.transform.IsChildOf(_target)))
            {
                continue;
            }

            Vector3 obstaclePosition = ImpactPhysics.ClosestPoint(collider, _transform.position);
            if (obstaclePosition.y - _transform.position.y < _settings.GroundHeightThreshold)
            {
                continue;
            }

            float distance = Vector3.Distance(_transform.position, obstaclePosition);
            Vector3 direction = (_transform.position - obstaclePosition).normalized;
            if (distance < _emergencyRange)
            {
                forces.Emergency += direction * (1f - distance / _emergencyRange);
                forces.EmergencyCount++;
            }
            else if (distance < _primaryRange)
            {
                forces.Primary += direction * (1f - distance / _primaryRange);
                forces.PrimaryCount++;
            }
        }
    }

    private void SampleForwardPrediction(Vector3 velocity, Vector3 targetDirection, ref Forces forces)
    {
        if (velocity.magnitude <= 0.5f)
        {
            return;
        }

        Vector3 predictedPosition = _transform.position + velocity.normalized * _predictiveRange;
        Vector3 predictionDirection = (predictedPosition - _transform.position).normalized;
        if (!Physics.Raycast(
            _transform.position,
            predictionDirection,
            out RaycastHit hit,
            _predictiveRange,
            _settings.ObstacleMask) || Vector3.Dot(hit.normal, Vector3.up) >= _settings.GroundNormalThreshold || hit.point.y - _transform.position.y < _settings.GroundHeightThreshold)
        {
            return;
        }

        Vector3 direction = Vector3.Cross(hit.normal, Vector3.up).normalized;
        if (Vector3.Dot(direction, targetDirection) < 0f)
        {
            direction = -direction;
        }

        forces.Predictive += direction;
        forces.PredictiveCount++;
    }

    private void AccumulateRayHit(RaycastHit hit, ref Forces forces)
    {
        Vector3 direction = (_transform.position - hit.point).normalized;
        if (_target != null)
        {
            Vector3 toTarget = (_target.position - _transform.position).normalized;
            Vector3 sideDirection = Vector3.Cross(direction, Vector3.up);
            float sideAngle = Vector3.Dot(sideDirection, toTarget) < 0f ? 30f : -30f;
            direction = Quaternion.Euler(0f, sideAngle, 0f) * direction;
        }

        HazardLevel level;
        float strength;
        if (hit.distance < _emergencyRange)
        {
            level = HazardLevel.Emergency;
            strength = 1f - hit.distance / _emergencyRange;
            forces.Emergency += direction * strength;
            forces.EmergencyCount++;
        }
        else if (hit.distance < _primaryRange)
        {
            level = HazardLevel.Primary;
            strength = 1f - hit.distance / _primaryRange;
            forces.Primary += direction * strength;
            forces.PrimaryCount++;
        }
        else
        {
            level = HazardLevel.Predictive;
            strength = 1f - hit.distance / _predictiveRange;
            forces.Predictive += direction * strength;
            forces.PredictiveCount++;
        }

        _debugHits.Add(new DebugHit { HitPoint = hit.point, AvoidanceVector = direction * strength, Distance = hit.distance, Level = level });
    }

    private BotAvoidanceResult CombineForces(Forces forces)
    {
        Vector3 direction = Vector3.zero;
        float weight = 0f;
        if (forces.EmergencyCount > 0)
        {
            direction += forces.Emergency / forces.EmergencyCount * _settings.EmergencyForce;
            weight += _settings.EmergencyForce;
        }

        if (forces.PrimaryCount > 0)
        {
            direction += forces.Primary / forces.PrimaryCount * _settings.PrimaryForce;
            weight += _settings.PrimaryForce;
        }

        if (forces.PredictiveCount > 0)
        {
            direction += forces.Predictive / forces.PredictiveCount * _settings.PredictiveForce;
            weight += _settings.PredictiveForce;
        }

        float speedMultiplier = 1f;
        if (forces.EmergencyCount > 0)
        {
            speedMultiplier = _settings.SpeedDampingFactor * 0.3f;
        }
        else if (forces.PrimaryCount > 0)
        {
            speedMultiplier = Mathf.Lerp(1f, _settings.SpeedDampingFactor, forces.PrimaryCount / 5f);
        }
        else if (forces.PredictiveCount > 0)
        {
            speedMultiplier = Mathf.Lerp(1f, _settings.SpeedDampingFactor * 1.5f, forces.PredictiveCount / 8f);
        }

        return new BotAvoidanceResult
        {
            Direction = direction.normalized,
            Strength = Mathf.Clamp01(weight / 10f),
            SpeedMultiplier = speedMultiplier,
            ObstacleCount = forces.EmergencyCount + forces.PrimaryCount + forces.PredictiveCount
        };
    }
}

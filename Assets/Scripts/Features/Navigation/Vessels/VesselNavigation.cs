// 职责：组合整舰 A*、升降与动态预测，并复用 NavigationAvoidance 的绕行、脱困和速度安全约束。
// 入口：Evaluate() 降频选路；ConstrainVelocity() 逐物理步验证实际运动走廊；不写刚体。
// 生命周期：每艘友舰/敌舰独占实例；任务改变、禁用与回收均 Reset()。
using UnityEngine;

internal sealed class VesselNavigation
{
    internal readonly NavigationLocalPath Path = new NavigationLocalPath();
    internal readonly NavigationObstacleClimb Climb = new NavigationObstacleClimb();
    internal readonly NavigationPrediction Prediction = new NavigationPrediction();
    internal readonly NavigationAvoidance Local;
    internal bool urgentAvoidance;
    internal bool avoidingObstacle;
    internal float obstacleSpeedRatio = 1f;
    internal bool climbingObstacle;
    internal float obstacleHoverHeight;
    internal Bounds avoidanceBounds;
    internal float probeDistance;
    private NavigationAvoidanceSettings _localSettings;
    private bool _configured;
    internal VesselNavigation(Transform owner)
    {
        Local = new NavigationAvoidance(owner);
    }

    internal void Reset()
    {
        Path.Clear();
        Path.Perception = null;
        Path.PreviewRequest = null;
        Climb.Clear();
        Prediction.Clear();
        Prediction.TaskTarget = null;
        Local.Reset();
        _configured = false;
        climbingObstacle = false;
        avoidingObstacle = false;
        urgentAvoidance = false;
    }

    private void ConfigureLocal(VesselNavigationSettings settings)
    {
        _localSettings = new NavigationAvoidanceSettings
        {
            UseHull = true,
            HullBounds = avoidanceBounds,
            ObstacleMask = settings.ObstacleLayers,
            ClearanceRadius = 0.25f,
            PredictiveRange = probeDistance,
            BrakingAcceleration = settings.PredictionBrakingAcceleration,
            RayCount = 16,
            ProgressDistance = 0.5f,
            StuckTimeout = 2f,
            DetourHoldTime = settings.AvoidanceSideHoldTime,
            EmergencyRange = 3f
        };
        Local.Configure(_localSettings);
        _configured = true;
    }

    internal Vector3 ConstrainVelocity(ControlUnit unit, Vector3 desired, Vector3 velocity, float brakingAcceleration)
    {
        if (!_configured)
        {
            return desired;
        }

        Bounds bounds = Climb.GetHullBounds(unit);
        bounds.Expand(new Vector3(0.5f, 0f, 0.5f));
        Local.UpdateHull(bounds);
        return Local.ConstrainVelocity(desired, Time.fixedDeltaTime, velocity, brakingAcceleration);
    }

    internal Vector3 Evaluate(
        ControlUnit unit,
        Rigidbody movementBody,
        Vector3 desiredMove,
        Vector3 destination,
        bool navigationRequested,
        ControlUnit target,
        VesselNavigationSettings settings)
    {
        urgentAvoidance = false;
        avoidingObstacle = false;
        obstacleSpeedRatio = 1f;
        Vector3 velocity = movementBody != null ? movementBody.linearVelocity : Vector3.zero;
        climbingObstacle = false;
        avoidanceBounds = Climb.GetHullBounds(unit);
        Vector3 extents = avoidanceBounds.extents;
        extents.x += 0.25f;
        extents.z += 0.25f;
        avoidanceBounds.extents = extents;
        float horizon = Mathf.Max(settings.MovementInterval, settings.AvoidancePredictionTime);
        float speed = velocity.magnitude;
        // 速度越高，基础时间预判之外再加入刹停距离，提前量单调增加。
        float brakingDistance = speed * speed / (2f * Mathf.Max(0.1f, settings.PredictionBrakingAcceleration));
        probeDistance = Mathf.Max(0f, settings.ObstacleAvoidanceDistance) + speed * horizon + brakingDistance;
        ConfigureLocal(settings);
        Path.TurnPenalty = settings.PathTurnPenalty;
        Path.PredictionTime = settings.PathPredictionTime;
        Path.Perception = settings.UsePredictiveDecisions ? Prediction : null;
        Path.PreviewRequest = null;
        if (settings.UsePredictiveDecisions)
        {
            Prediction.MinimumDistance = settings.FarLookAheadDistance;
            Prediction.BodyLengths = settings.FarLookAheadBodyLengths;
            Prediction.LookAheadSeconds = settings.FarLookAheadSeconds;
            Prediction.TaskTarget = target;
            // 同一轮先感知再规划，A*与近场动作验证共享快照，避免各自重复查询且看到不同路况。
            Prediction.Observe(
                unit,
                avoidanceBounds,
                velocity,
                settings.ObstacleLayers,
                settings.CruiseSpeed,
                settings.DecisionHorizon,
                navigationRequested ? destination : unit.transform.position + desiredMove * settings.FarLookAheadDistance,
                Path.Active ? Path.Points : null,
                Path.Waypoint,
                settings.BaseHoverHeight);
            if (Prediction.FarDecision == "RouteBlocked" || Prediction.FarDecision == "CrossingWatch")
            {
                Path.PreviewRequest = Prediction.FarDecision == "RouteBlocked" ? "FarRouteBlocked" : "FarCrossing";
            }
        }

        // 越障看请求航向，实际速度用于计算预判距离和逐步制动。远处移动物体由预测单独处理。
        if (!Prediction.Saturated && TryClimbObstacle(unit, desiredMove, horizon, settings, out Vector3 climbMovement))
        {
            if (Path.Active)
            {
                Path.Clear();
            }

            return climbMovement;
        }

        // 垂直通道不可行后，再沿远场 A* 路线水平绕行。
        if (navigationRequested && unit.hoverFlightController != null && Path.TryFollow(
            unit,
            avoidanceBounds,
            destination,
            settings.ObstacleLayers,
            settings.PathCellSize,
            settings.PathSearchRadius,
            settings.PathNodeBudget,
            settings.PathReplanInterval,
            Mathf.Max(2f, settings.BaseHoverHeight - settings.MaxObstacleDescent),
            settings.BaseHoverHeight + settings.MaxObstacleClimb,
            out Vector3 waypoint))
        {
            climbingObstacle = true;
            avoidingObstacle = true;
            Vector3 alongPath = Vector3.ProjectOnPlane(waypoint - unit.transform.position, Vector3.up);
            obstacleHoverHeight = waypoint.y;
            float heightRatio = Climb.ForwardRatio(
                unit,
                alongPath,
                obstacleHoverHeight,
                settings.CruiseSpeed,
                settings.ObstacleClimbClearance,
                horizon,
                settings.ObstacleLayers);
            urgentAvoidance = heightRatio < 0.25f;
            obstacleSpeedRatio = heightRatio;
            // 高度与水平同时跟随；现高度走廊余量决定水平速度，航点距离本身不要求停车。
            return alongPath.normalized * heightRatio;
        }

        if (desiredMove.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        NavigationAvoidanceResult local = Local.Evaluate(
            target != null ? target.transform : null,
            velocity,
            desiredMove.normalized,
            1f,
            _localSettings,
            navigationRequested ? Vector3.Distance(unit.transform.position, destination) : float.PositiveInfinity);
        avoidingObstacle = local.OverrideDirection;
        obstacleSpeedRatio = local.SpeedMultiplier;
        urgentAvoidance = Local.Saturated || local.OverrideDirection && local.SpeedMultiplier <= 0.4f;
        return local.OverrideDirection ? local.Direction * local.SpeedMultiplier : desiredMove;
    }

    private bool TryClimbObstacle(
        ControlUnit unit,
        Vector3 desiredMove,
        float horizon,
        VesselNavigationSettings settings,
        out Vector3 movement)
    {
        movement = Vector3.zero;
        climbingObstacle = settings.PreferObstacleClimb && unit.hoverFlightController != null && Climb.TryGetHeight(
            unit,
            desiredMove,
            settings.BaseHoverHeight,
            Mathf.Max(probeDistance, settings.ClimbLookAhead),
            settings.MaxObstacleClimb,
            settings.ObstacleClimbClearance,
            settings.ObstacleLayers,
            out obstacleHoverHeight,
            settings.MaxObstacleDescent);
        if (climbingObstacle)
        {
            avoidingObstacle = true;
            float ratio = Climb.ForwardRatio(
                unit,
                desiredMove,
                obstacleHoverHeight,
                settings.CruiseSpeed,
                settings.ObstacleClimbClearance,
                horizon,
                settings.ObstacleLayers);
            obstacleSpeedRatio = ratio;
            urgentAvoidance = ratio < 0.25f;
            movement = desiredMove * ratio;
            return true;
        }

        return false;
    }
}

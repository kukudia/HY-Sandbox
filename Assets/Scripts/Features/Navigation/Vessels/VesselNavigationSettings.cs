// 职责：传递舰船 Inspector 导航参数的单次快照；不持有单位或运行状态。
using UnityEngine;

internal struct VesselNavigationSettings
{
    internal float ObstacleAvoidanceDistance;
    internal float AvoidancePredictionTime;
    internal float PredictionBrakingAcceleration;
    internal float PathTurnPenalty;
    internal float PathPredictionTime;
    internal bool UsePredictiveDecisions;
    internal float FarLookAheadDistance;
    internal float FarLookAheadBodyLengths;
    internal float FarLookAheadSeconds;
    internal float CruiseSpeed;
    internal float DecisionHorizon;
    internal float PathCellSize;
    internal float PathSearchRadius;
    internal int PathNodeBudget;
    internal float PathReplanInterval;
    internal float MaxObstacleDescent;
    internal float MaxObstacleClimb;
    internal bool PreferObstacleClimb;
    internal float ClimbLookAhead;
    internal float ObstacleClimbClearance;
    internal LayerMask ObstacleLayers;
    internal float AvoidanceSideHoldTime;
    internal float MovementInterval;
    internal float BaseHoverHeight;
}

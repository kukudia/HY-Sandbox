// 职责：描述导航控制器当前真实决策，供测试场记录与可视化，不参与驾驶。
// 入口：EnemyController.CaptureNavigation()；路线仅在版本变化时复制。
using System;
using UnityEngine;

[Serializable]
public struct EnemyNavigationSnapshot
{
    public Vector3 position;
    public Vector3 velocity;
    public Quaternion rotation;
    public Vector3 angularVelocity;
    public Vector3 desiredInput;
    public Vector3 referenceVelocity;
    public Vector3 appliedInput;
    public Vector3 steering;
    public float targetHeight;
    public float probeDistance;
    public float throttleRatio;
    public Bounds hull;
    public bool avoiding;
    public bool changingHeight;
    public bool urgent;
    public bool followingPath;
    public int turns;
    public int routeRevision;
    public int expandedNodes;
    public int waypoint;
    public string pathStatus;
    public bool routeChanged;
    public string replanReason;
    public float planMilliseconds;
    public string predictionDecision;
    public int predictionRevision;
    public string predictedThreat;
    public Vector3 predictedThreatVelocity;
    public float sensorRadius;
    public float lookAheadDistance;
    public float farObstacleDistance;
    public float farArrivalTime;
    public string farThreat;
    public string farDecision;
    public int observedObstacles;
    public int movingObstacles;
    public int predictionCandidates;
    public int predictionSegmentChecks;
    public float predictedCollisionTime;
    public float selectedCollisionTime;
    public float predictionMilliseconds;
    public bool predictionSaturated;
    public bool predictiveBraking;
    public Vector3 predictedVelocity;
    public Vector3[] route;
}

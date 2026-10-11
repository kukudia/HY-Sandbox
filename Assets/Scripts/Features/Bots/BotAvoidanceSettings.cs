// 职责：携带单次障碍采样的距离、权重、过滤条件；值来自 Bot Inspector。
// 单位：Range/Height 为世界单位，Force 为原有方向混合权重。
using UnityEngine;

public struct BotAvoidanceSettings
{
    public LayerMask ObstacleMask;
    public float EmergencyRange;
    public float PrimaryRange;
    public float PredictiveRange;
    public float EmergencyForce;
    public float PrimaryForce;
    public float PredictiveForce;
    public int RayCount;
    public float GroundNormalThreshold;
    public float GroundHeightThreshold;
    public float SpeedDampingFactor;
}

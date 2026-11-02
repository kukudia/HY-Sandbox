// 职责：携带单次障碍采样的距离、权重、过滤条件；值来自 Bot Inspector。
// 单位：Range/Radius/ProgressDistance 为米，Timeout/HoldTime 为秒，BrakingAcceleration 为米/秒²。
using UnityEngine;

public struct BotAvoidanceSettings
{
    public LayerMask ObstacleMask;
    public float ClearanceRadius;
    public float StuckTimeout;
    public float ProgressDistance;
    public float DetourHoldTime;
    public float EmergencyRange;
    public float PredictiveRange;
    public float BrakingAcceleration;
    public int RayCount;
}

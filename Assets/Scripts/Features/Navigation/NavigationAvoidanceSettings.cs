// 职责：携带单次障碍采样的距离、权重、过滤条件；由 Bot 策略或舰船 Inspector 参数构造。
// 单位：Range/Radius/ProgressDistance 为米，Timeout/HoldTime 为秒，BrakingAcceleration 为米/秒²。
using UnityEngine;

public struct NavigationAvoidanceSettings
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
    // Bot 使用球形净空并允许接近任务目标；舰船使用完整包围盒且目标仍参与防撞。
    public bool UseHull;
    public Bounds HullBounds;
}

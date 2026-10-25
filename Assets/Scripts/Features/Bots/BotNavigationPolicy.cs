// 职责：定义维修/回收 Bot 共用且不可由 Inspector/CSV 覆盖的导航规则。
// 契约：速度没有固定上限；只限制加速度、可制动距离及转弯净空，单位为米和秒。
using UnityEngine;

public static class BotNavigationPolicy
{
    public const float Acceleration = 48f;
    public const float SampleInterval = 0.1f;
    public const float DirectionResponseRate = 5f;
    public const float RotationSpeed = 6f;
    public const float PredictiveRange = 15f;
    public const float ClearanceRadius = 0.4f;
    public const float DockingApproachHeight = 1f;
    public const float DockingApproachTolerance = 0.2f;
    public const float DockingPositionSpeed = 3f;
    public const float DockingPositionTolerance = 0.08f;
    public const float DockingAlignmentAngle = 8f;
    public const float EffectFullSpeed = 30f;
    public static BotAvoidanceSettings CreateAvoidanceSettings()
    {
        return new BotAvoidanceSettings
        {
            ObstacleMask = LayerMask.GetMask("Default", "Block"),
            ClearanceRadius = ClearanceRadius,
            StuckTimeout = 1.25f,
            ProgressDistance = 0.25f,
            DetourHoldTime = 0.6f,
            EmergencyRange = 3f,
            PredictiveRange = PredictiveRange,
            BrakingAcceleration = Acceleration,
            RayCount = 16
        };
    }

    public static void ConfigureBody(Rigidbody body)
    {
        // 阻力会形成 a / drag 的隐式终端速度；控制器自己负责制动。
        body.useGravity = false;
        body.linearDamping = 0f;
        body.maxLinearVelocity = float.MaxValue;
        body.collisionDetectionMode = body.isKinematic ? CollisionDetectionMode.Discrete : CollisionDetectionMode.ContinuousDynamic;
    }
}

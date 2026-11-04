// 职责：传递重组刚体的阻尼、解穿透及继承速度限制，不拥有 Inspector 数据。
// 入口：由 PlayManager 从现有序列化字段构造，载具与建筑共用 ApplyTo() 和运动继承限制。
using UnityEngine;

public readonly struct GroupPhysicsSettings
{
    public readonly float LinearDamping;
    public readonly float AngularDamping;
    public readonly float MaxDepenetrationVelocity;
    public readonly float MaximumTangentialSpeed;
    public readonly float MaximumAngularSpeed;
    internal void ApplyTo(Rigidbody body)
    {
        body.linearDamping = Mathf.Max(0f, LinearDamping);
        body.angularDamping = Mathf.Max(0f, AngularDamping);
        body.maxDepenetrationVelocity = Mathf.Max(0f, MaxDepenetrationVelocity);
    }

    internal static GroupPhysicsSettings FromBody(Rigidbody body)
    {
        return new GroupPhysicsSettings(
            body.linearDamping,
            body.angularDamping,
            body.maxDepenetrationVelocity,
            body.maxLinearVelocity,
            body.maxAngularVelocity);
    }

    public GroupPhysicsSettings(
        float linearDamping,
        float angularDamping,
        float maxDepenetrationVelocity,
        float maximumTangentialSpeed,
        float maximumAngularSpeed)
    {
        LinearDamping = linearDamping;
        AngularDamping = angularDamping;
        MaxDepenetrationVelocity = maxDepenetrationVelocity;
        MaximumTangentialSpeed = maximumTangentialSpeed;
        MaximumAngularSpeed = maximumAngularSpeed;
    }
}

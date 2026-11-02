// 职责：传递重组刚体的阻尼、解穿透及继承速度限制，不拥有 Inspector 数据。
// 入口：由 PlayManager 从现有序列化字段构造，供 RuntimeGroupBuilder 使用。
public readonly struct GroupPhysicsSettings
{
    public readonly float LinearDamping;
    public readonly float AngularDamping;
    public readonly float MaxDepenetrationVelocity;
    public readonly float MaximumTangentialSpeed;
    public readonly float MaximumAngularSpeed;
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

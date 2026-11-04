// 职责：在拆分层级前捕获刚体运动，供载具分组和建筑残骸共同继承质心速度与角速度。
// 入口：构造时捕获；Restore() 在迁移和质量更新后调用，避免源质心变化污染后续碎片。
using UnityEngine;

internal readonly struct RigidbodyMotion
{
    private readonly Vector3 linearVelocity;
    private readonly Vector3 angularVelocity;
    private readonly Vector3 centerOfMass;
    internal RigidbodyMotion(Rigidbody body)
    {
        linearVelocity = body.isKinematic ? Vector3.zero : body.linearVelocity;
        angularVelocity = body.isKinematic ? Vector3.zero : body.angularVelocity;
        centerOfMass = body.worldCenterOfMass;
    }

    internal void Restore(Rigidbody target, GroupPhysicsSettings settings)
    {
        Vector3 tangential = Vector3.Cross(angularVelocity, target.worldCenterOfMass - centerOfMass);
        target.linearVelocity = linearVelocity + Vector3.ClampMagnitude(tangential, settings.MaximumTangentialSpeed);
        target.angularVelocity = Vector3.ClampMagnitude(angularVelocity, settings.MaximumAngularSpeed);
        target.WakeUp();
    }
}

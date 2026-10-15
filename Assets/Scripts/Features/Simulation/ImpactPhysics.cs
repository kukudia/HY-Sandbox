// 职责：集中计算碰撞/爆炸的力学与伤害反馈规则。
// 入口：ReducedMass() / DamageEnergy() / ClosestPoint()。
using System;
using UnityEngine;

/// <summary>Physical impact math shared by collision damage and diagnostics (Unity units: m, kg, s).</summary>
public static class ImpactPhysics
{
    public static event Action<float, float> PlayerImpact;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents()
    {
        PlayerImpact = null;
    }

    public static float ReducedMass(float mass, float otherMass, bool otherIsStatic)
    {
        mass = Mathf.Max(0f, mass);
        otherMass = Mathf.Max(0f, otherMass);
        return otherIsStatic ? mass : mass * otherMass / Mathf.Max(0.001f, mass + otherMass);
    }

    public static float DamageEnergy(float reducedMass, float normalSpeed, float safeSpeed)
    {
        return 0.5f * Mathf.Max(0f, reducedMass) * Mathf.Max(0f, normalSpeed * normalSpeed - safeSpeed * safeSpeed);
    }

    public static float BodyEnergyShare(float mass, float otherMass, bool otherIsStatic)
    {
        // Centre-of-mass kinetic energy: the lighter body undergoes the larger velocity change.
        return otherIsStatic ? 1f : Mathf.Max(0f, otherMass) / Mathf.Max(0.001f, mass + otherMass);
    }

    public static float Restitution(Collider first, Collider second, float normalSpeed)
    {
        if (normalSpeed < Physics.bounceThreshold)
        {
            return 0f;
        }

        PhysicsMaterial a = first.sharedMaterial;
        PhysicsMaterial b = second.sharedMaterial;
        float av = a != null ? a.bounciness : 0f;
        float bv = b != null ? b.bounciness : 0f;
        PhysicsMaterialCombine ac = a != null ? a.bounceCombine : PhysicsMaterialCombine.Average;
        PhysicsMaterialCombine bc = b != null ? b.bounceCombine : PhysicsMaterialCombine.Average;
        // PhysX priority is Average < Minimum < Multiply < Maximum, not enum numeric order.
        PhysicsMaterialCombine mode = CombinePriority(ac) >= CombinePriority(bc) ? ac : bc;
        switch (mode)
        {
            case PhysicsMaterialCombine.Minimum:
                return Mathf.Min(av, bv);
            case PhysicsMaterialCombine.Multiply:
                return av * bv;
            case PhysicsMaterialCombine.Maximum:
                return Mathf.Max(av, bv);
            default:
                return (av + bv) * 0.5f;
        }
    }

    private static int CombinePriority(PhysicsMaterialCombine mode)
    {
        switch (mode)
        {
            case PhysicsMaterialCombine.Minimum:
                return 1;
            case PhysicsMaterialCombine.Multiply:
                return 2;
            case PhysicsMaterialCombine.Maximum:
                return 3;
            default:
                return 0;
        }
    }

    public static Vector3 ClosestPoint(Collider collider, Vector3 position)
    {
        // Unity does not support Collider.ClosestPoint on non-convex meshes or terrains.
        if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider || (collider is MeshCollider mesh && mesh.convex))
        {
            return collider.ClosestPoint(position);
        }

        return collider.bounds.ClosestPoint(position);
    }

    public static void ReportPlayerImpact(float energy, float impulse)
    {
        PlayerImpact?.Invoke(energy, impulse);
    }
}

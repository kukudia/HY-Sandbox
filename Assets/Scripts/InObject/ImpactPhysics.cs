using System;
using UnityEngine;

/// <summary>Physical impact math shared by collision damage and diagnostics (Unity units: m, kg, s).</summary>
public static class ImpactPhysics
{
    public static event Action<float, float> PlayerImpact;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetEvents() { PlayerImpact = null; }

    public static float ReducedMass(float mass, float otherMass, bool otherIsStatic)
    {
        mass = Mathf.Max(0f, mass);
        otherMass = Mathf.Max(0f, otherMass);
        return otherIsStatic ? mass : mass * otherMass / Mathf.Max(0.001f, mass + otherMass);
    }

    public static float DamageEnergy(float reducedMass, float normalSpeed, float safeSpeed)
    {
        return 0.5f * Mathf.Max(0f, reducedMass)
            * Mathf.Max(0f, normalSpeed * normalSpeed - safeSpeed * safeSpeed);
    }

    public static Vector3 ClosestPoint(Collider collider, Vector3 position)
    {
        // Unity does not support Collider.ClosestPoint on non-convex meshes or terrains.
        if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider
            || (collider is MeshCollider mesh && mesh.convex)) return collider.ClosestPoint(position);
        return collider.bounds.ClosestPoint(position);
    }

    public static void ReportPlayerImpact(float energy, float impulse)
    {
        PlayerImpact?.Invoke(energy, impulse);
    }
}

// 职责：共享激光与实体炮弹的命中反馈、敌对伤害和可选炮弹冲量。
// 入口：Apply()；先施加炮弹冲量，确保拆组后的刚体继承命中速度。
using UnityEngine;

internal static class TurretHitResolver
{
    private static readonly Unity.Profiling.ProfilerMarker ImpactMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.ImpactFeedback");
    private static readonly Unity.Profiling.ProfilerMarker DamageMarker = new Unity.Profiling.ProfilerMarker("HY.Turret.ApplyDamage");
    internal static void Apply(
        Collider collider,
        Vector3 point,
        Vector3 normal,
        Vector3 direction,
        ControlUnit owner,
        UnitFaction faction,
        float damage,
        float impulse)
    {
        Rigidbody body = collider.attachedRigidbody;
        if (impulse > 0f && body != null && !body.isKinematic)
        {
            body.AddForceAtPosition(direction * impulse, point, ForceMode.Impulse);
        }

        Vector3 impactNormal = normal.sqrMagnitude > 0.001f ? normal : -direction;
        using (ImpactMarker.Auto())
        {
            BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Impact, point, Quaternion.LookRotation(impactNormal), 0.6f);
        }

        using var damageSample = DamageMarker.Auto();
        if (collider.TryGetComponent(out VoxelWorldChunk terrainChunk))
        {
            ProceduralVoxelWorld terrain = terrainChunk.GetComponentInParent<ProceduralVoxelWorld>();
            if (terrain != null)
            {
                terrain.DamageSphere(point - impactNormal * terrain.CellSize * 0.1f, terrain.CellSize * 0.85f, damage);
            }
        }

        ControlUnit unit = collider.GetComponentInParent<ControlUnit>();
        if (unit != null && unit.IsOperational && unit.faction == faction)
        {
            Durability durability = collider.GetComponentInParent<Durability>();
            if (durability != null)
            {
                durability.ApplyDamage(damage, owner);
            }
        }
    }
}

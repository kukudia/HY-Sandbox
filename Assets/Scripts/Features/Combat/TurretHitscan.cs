// 职责：按射线命中距离跳过本单位，先施加冲量，再结算有效敌方模块伤害。
// 入口：Fire()；由 TurretWeapon 的固定步射击流程调用并返回光束终点。
using System.Collections.Generic;
using UnityEngine;

internal sealed class TurretHitscan
{
    private const int MaxRaycastHits = 16;
    private readonly RaycastHit[] _hits = new RaycastHit[MaxRaycastHits];
    internal Vector3 Fire(
        Vector3 origin,
        Vector3 direction,
        ControlUnit owner,
        UnitFaction targetFaction,
        float range,
        LayerMask hitLayers,
        float damage,
        float impactImpulse)
    {
        Vector3 end = origin + direction * range;
        int hitCount = Physics.RaycastNonAlloc(origin, direction, _hits, range, hitLayers);
        System.Array.Sort(_hits, 0, hitCount, RaycastHitDistanceComparer.Instance);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hits[i];
            ControlUnit hitUnit = hit.collider.GetComponentInParent<ControlUnit>();
            if (hitUnit == owner)
            {
                continue;
            }

            end = hit.point;
            // RefreshGroup must inherit the shot impulse if damage detaches target blocks.
            Rigidbody hitBody = hit.rigidbody;
            if (hitBody != null && !hitBody.isKinematic)
            {
                hitBody.AddForceAtPosition(direction * impactImpulse, hit.point, ForceMode.Impulse);
            }

            BlockVfxLibrary.Play(BlockVfxLibrary.Effect.Impact, hit.point, Quaternion.LookRotation(hit.normal), 0.6f);
            Durability durability = hit.collider.GetComponentInParent<Durability>();
            if (hitUnit != null && hitUnit.HasValidCockpit && hitUnit.faction == targetFaction && durability != null)
            {
                durability.ApplyDamage(damage, owner);
            }

            break;
        }

        return end;
    }

    private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
    {
        internal static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();
        public int Compare(RaycastHit a, RaycastHit b)
        {
            return a.distance.CompareTo(b.distance);
        }
    }
}

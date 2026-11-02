// 职责：扫描 Home 范围内的友方耐久对象，并选择距离机器人最近的受损目标。
// 入口：Scan()/FindClosestDamaged()；IsValidTarget() 用于执行维修前重新核对归属与范围。
// 生命周期：非分配碰撞缓冲按需扩展至 1024；多个 Collider 归属的耐久对象只记录一次。
using System.Collections.Generic;
using UnityEngine;

public sealed class RepairTargetSearch
{
    private const int MaxColliderCapacity = 1024;
    private readonly List<Durability> _targets = new List<Durability>();
    private readonly HashSet<Durability> _uniqueTargets = new HashSet<Durability>();
    private readonly int _layerMask = LayerMask.GetMask("Block");
    private Collider[] _colliders = new Collider[128];
    private ControlUnit _owner;
    public void Scan(Transform home, int targetRange)
    {
        _targets.Clear();
        _uniqueTargets.Clear();
        _owner = home != null ? home.GetComponentInParent<ControlUnit>() : null;
        if (home == null || _owner == null || targetRange <= 0 || _layerMask == 0)
        {
            return;
        }

        int colliderCount;
        while (true)
        {
            colliderCount = Physics.OverlapSphereNonAlloc(home.position, targetRange, _colliders, _layerMask, QueryTriggerInteraction.Ignore);
            if (colliderCount < _colliders.Length || _colliders.Length >= MaxColliderCapacity)
            {
                break;
            }

            int newCapacity = Mathf.Min(_colliders.Length * 2, MaxColliderCapacity);
            _colliders = new Collider[newCapacity];
        }

        float rangeSqr = targetRange * targetRange;
        for (int i = 0; i < colliderCount; i++)
        {
            Collider candidateCollider = _colliders[i];
            _colliders[i] = null;
            if (candidateCollider == null)
            {
                continue;
            }

            Durability durability = candidateCollider.GetComponentInParent<Durability>();
            if (durability == null || durability.GetComponentInParent<ControlUnit>() != _owner || (durability.transform.position - home.position).sqrMagnitude > rangeSqr || !_uniqueTargets.Add(durability))
            {
                continue;
            }

            _targets.Add(durability);
        }
    }

    public Durability FindClosestDamaged(Vector3 position)
    {
        float closestDistanceSqr = Mathf.Infinity;
        Durability closest = null;
        foreach (Durability target in _targets)
        {
            if (target == null || !target.needToRepair)
            {
                continue;
            }

            float distanceSqr = (position - target.transform.position).sqrMagnitude;
            if (distanceSqr < closestDistanceSqr)
            {
                closestDistanceSqr = distanceSqr;
                closest = target;
            }
        }

        return closest;
    }

    public static bool IsValidTarget(Durability target, Transform home, int targetRange)
    {
        if (target == null || !target.needToRepair || home == null || targetRange <= 0)
        {
            return false;
        }

        ControlUnit currentOwner = home.GetComponentInParent<ControlUnit>();
        if (currentOwner == null || target.GetComponentInParent<ControlUnit>() != currentOwner)
        {
            return false;
        }

        float rangeSqr = targetRange * targetRange;
        return (target.transform.position - home.position).sqrMagnitude <= rangeSqr;
    }
}

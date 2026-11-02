// 职责：在源单位内概率断链，并对受影响刚体施加有限冲量及双向碰撞保护。
// 入口：DisconnectBlocksInExplosionRange() / ApplyExplosionForce()；由 DestroyManager 分别在重组前后调用。
// 约束：爆炸不直接造成伤害；原始 ownerUnitId 用于查回重组后的同源碎片。
using System.Collections.Generic;
using UnityEngine;

internal static class ExplosionPropagation
{
    internal static void DisconnectBlocksInExplosionRange(ControlUnit unit, Block block, Vector3 explosionPosition)
    {
        if (unit == null || block == null)
        {
            return;
        }

        float radiusSqr = block.explosionRadius * block.explosionRadius;
        float probability = Mathf.Clamp01(block.explosionDisconnectProbability);
        Block[] blocks = unit.GetComponentsInChildren<Block>(true);
        HashSet<Block> neighborsToRefresh = new HashSet<Block>();
        foreach (Block candidate in blocks)
        {
            if (candidate == null || candidate == block)
            {
                continue;
            }

            Vector3 offset = candidate.transform.position - explosionPosition;
            if (offset.sqrMagnitude > radiusSqr || Random.value > probability)
            {
                continue;
            }

            foreach (Block neighbor in candidate.neighbors)
            {
                if (neighbor != null && neighbor != block)
                {
                    neighborsToRefresh.Add(neighbor);
                }
            }

            candidate.DisConnectAllConnectors(false);
        }

        foreach (Block neighbor in neighborsToRefresh)
        {
            if (neighbor != null)
            {
                neighbor.CheckConnection();
            }
        }
    }

    internal static void ApplyExplosionForce(
        Block block,
        string ownerUnitId,
        UnitFaction ownerFaction,
        Vector3 explosionPosition,
        float upwardsModifier,
        float impulseScale,
        float maximumSpeedChange,
        float collisionProtection)
    {
        if (block == null || block.explosionForce <= 0f || block.explosionRadius <= 0f)
        {
            return;
        }

        float radius = block.explosionRadius;
        HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();
        Collider[] colliders = Physics.OverlapSphere(explosionPosition, radius, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        foreach (Collider collider in colliders)
        {
            if (collider == null)
            {
                continue;
            }

            Rigidbody body = collider.attachedRigidbody;
            if (body != null)
            {
                bodies.Add(body);
            }
        }

        // A grouped Rigidbody can be centered outside the radius while one of its child blocks is inside it.
        if (!string.IsNullOrEmpty(ownerUnitId))
        {
            RuntimeUnitMember[] members = Object.FindObjectsByType<RuntimeUnitMember>(FindObjectsSortMode.None);
            foreach (RuntimeUnitMember member in members)
            {
                if (member == null || member.ownerUnitId != ownerUnitId)
                {
                    continue;
                }

                Block memberBlock = member.GetComponent<Block>();
                ControlUnit group = member.GetComponentInParent<ControlUnit>();
                if (memberBlock == null || group == null)
                {
                    continue;
                }

                if ((memberBlock.transform.position - explosionPosition).sqrMagnitude <= radius * radius)
                {
                    Rigidbody body = group.GetComponent<Rigidbody>();
                    if (body != null)
                    {
                        bodies.Add(body);
                        group.faction = ownerFaction;
                    }
                }
            }
        }

        foreach (Rigidbody rb in bodies)
        {
            if (rb == null || rb.isKinematic)
            {
                continue;
            }

            Vector3 forcePoint = rb.worldCenterOfMass;
            float nearestDistance = Vector3.Distance(forcePoint, explosionPosition);
            Collider[] bodyColliders = rb.GetComponentsInChildren<Collider>();
            foreach (Collider bodyCollider in bodyColliders)
            {
                if (bodyCollider == null)
                {
                    continue;
                }

                Vector3 candidatePoint = ImpactPhysics.ClosestPoint(bodyCollider, explosionPosition);
                float candidateDistance = Vector3.Distance(candidatePoint, explosionPosition);
                if (candidateDistance < nearestDistance)
                {
                    forcePoint = candidatePoint;
                    nearestDistance = candidateDistance;
                }
            }

            float falloff = 1f - Mathf.Clamp01(nearestDistance / radius);
            if (falloff <= 0f)
            {
                continue;
            }

            Vector3 direction = forcePoint - explosionPosition;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = rb.worldCenterOfMass - explosionPosition;
            }

            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.up;
            Vector3 impulse = (direction + Vector3.up * upwardsModifier).normalized * Mathf.Min(block.explosionForce * falloff * impulseScale, rb.mass * maximumSpeedChange);
            // Keep explosion impulse purely physical. Its immediate collisions cannot cascade into destruction.
            foreach (Durability durability in rb.GetComponentsInChildren<Durability>())
            {
                durability.ProtectFromExplosionCollisions(collisionProtection);
            }

            rb.GetComponent<ControlUnit>()?.ProtectExplosionContacts(collisionProtection);
            rb.AddForceAtPosition(impulse, forcePoint, ForceMode.Impulse);
            ControlUnit detachedGroup = rb.GetComponent<ControlUnit>();
            if (detachedGroup != null && !detachedGroup.HasAnyCockpit)
            {
                // Only disposable fragments receive smoke; intact cockpit groups keep a clean combat silhouette.
                VisualEffectsManager.TryAttachDetachedPartSmoke(rb, forcePoint, falloff);
            }
        }
    }
}

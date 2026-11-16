// 职责：在源单位内概率断链，并在源PhysicsScene对受影响刚体施加有限冲量及双向碰撞保护。
// 入口：DisconnectBlocksInExplosionRange() / ApplyExplosionForce()；由 DestroyManager 分别在重组前后调用。
// 约束：本类仅负责断链与冲量；直接伤害由ExplosionDamageSession统一结算。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class ExplosionPropagation
{
    internal const float DefaultUpwardsModifier = 1.2f;
    internal const float DefaultImpulseScale = 0.15f;
    internal const float DefaultMaximumSpeedChange = 3f;
    internal const float DefaultCollisionProtection = 3f;
    internal static void ApplyDefaultExplosionForce(Block block)
    {
        ControlUnit unit = block != null ? block.GetComponentInParent<ControlUnit>() : null;
        ApplyExplosionForce(
            block,
            unit != null ? unit.runtimeUnitId : string.Empty,
            unit != null ? unit.faction : UnitFaction.Enemy,
            block != null ? block.transform.position : Vector3.zero,
            DefaultUpwardsModifier,
            DefaultImpulseScale,
            DefaultMaximumSpeedChange,
            DefaultCollisionProtection);
    }

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
        PhysicsScene physics = block.gameObject.scene.GetPhysicsScene();
        Collider[] colliders = new Collider[128];
        int count;
        while ((count = physics.OverlapSphere(explosionPosition, radius, colliders, Physics.AllLayers, QueryTriggerInteraction.Ignore)) == colliders.Length)
        {
            System.Array.Resize(ref colliders, checked(colliders.Length * 2));
        }

        for (int index = 0; index < count; index++)
        {
            Collider collider = colliders[index];
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

        // The source PhysicsScene query already includes child shapes whose body centre is
        // outside the radius; no global ownership scan is necessary after regrouping.
        foreach (Rigidbody rb in bodies)
        {
            if (rb == null || rb.isKinematic)
            {
                continue;
            }

            ControlUnit owner = rb.GetComponent<ControlUnit>();
            if (owner != null && !string.IsNullOrEmpty(ownerUnitId) && owner.runtimeUnitId == ownerUnitId)
            {
                owner.faction = ownerFaction;
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

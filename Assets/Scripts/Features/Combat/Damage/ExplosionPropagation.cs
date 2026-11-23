// 职责：重组前按爆炸源概率断开源单位及附近建筑的方块；重组后按 PhysicsScene 施加有限冲量。
// 入口：DisconnectBlocksInExplosionRange() / ApplyExplosionForce()。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class ExplosionPropagation
{
    internal const float DefaultUpwardsModifier = 1.2f;
    internal const float DefaultImpulseScale = 0.15f;
    internal const float DefaultMaximumSpeedChange = 3f;
    internal const float DefaultCollisionProtection = 3f;
    private static int FindOverlaps(Block source, Vector3 center, float radius, out Collider[] colliders)
    {
        PhysicsScene physics = source.gameObject.scene.GetPhysicsScene();
        colliders = new Collider[128];
        int count;
        while ((count = physics.OverlapSphere(center, radius, colliders, Physics.AllLayers, QueryTriggerInteraction.Ignore)) == colliders.Length)
        {
            System.Array.Resize(ref colliders, checked(colliders.Length * 2));
        }

        return count;
    }

    internal static HashSet<ControlUnit> DisconnectBlocksInExplosionRange(ControlUnit unit, Block source, Vector3 center)
    {
        if (source == null || !(source.explosionDisconnectProbability > 0f) || !(source.explosionRadius > 0f) || !float.IsFinite(source.explosionRadius))
        {
            return null;
        }

        float probability = Mathf.Clamp01(source.explosionDisconnectProbability);
        float radiusSquared = source.explosionRadius * source.explosionRadius;
        var candidates = new HashSet<Block>();
        HashSet<ControlUnit> affectedOwners = null;
        if (unit != null)
        {
            foreach (Block block in unit.Building != null ? unit.Building.Blocks : unit.GetComponentsInChildren<Block>())
            {
                if (block != null && block.GetComponentInParent<ControlUnit>() == unit)
                {
                    candidates.Add(block);
                }
            }
        }

        Physics.SyncTransforms();
        int count = FindOverlaps(source, center, source.explosionRadius, out Collider[] colliders);
        for (int index = 0; index < count; index++)
        {
            Collider hit = colliders[index];
            if (hit != null && hit.gameObject.scene == source.gameObject.scene && hit.attachedRigidbody != null && hit.attachedRigidbody.TryGetComponent(out ControlUnit owner) && owner != unit && owner.Building != null)
            {
                Block block = hit.GetComponentInParent<Block>();
                if (block != null)
                {
                    candidates.Add(block);
                }
            }
        }

        foreach (Block candidate in candidates)
        {
            if (candidate == source || !candidate.isActiveAndEnabled || (candidate.transform.position - center).sqrMagnitude > radiusSquared)
            {
                continue;
            }

            if (probability >= 1f || Random.value < probability)
            {
                // Disconnect clears both ends. The ensuing structure refresh rebuilds the surviving graph once.
                candidate.DisConnectAllConnectors(false);
                ControlUnit owner = candidate.GetComponentInParent<ControlUnit>();
                if (owner != null && owner != unit)
                {
                    affectedOwners ??= new HashSet<ControlUnit>();
                    affectedOwners.Add(owner);
                }
            }
        }

        return affectedOwners;
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
        int count = FindOverlaps(block, explosionPosition, radius, out Collider[] colliders);
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

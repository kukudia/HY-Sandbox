// 职责：将单次碰撞的法向求解器能量分摊给本控制单元实际接触的模块。
// 入口：ApplyContactDamage()；仅由 ControlUnit.OnCollisionEnter() 调用。
// 约束：爆炸保护检查双方；伤害前记录玩家冲击，避免重组丢失原事件信息。
using System.Collections.Generic;
using UnityEngine;

internal sealed class CollisionDamageResolver
{
    private readonly Dictionary<Durability, int> _impactTargets = new Dictionary<Durability, int>();
    internal void ApplyContactDamage(ControlUnit unit, Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("IgnoreCollision"))
        {
            return;
        }

        Rigidbody body = unit.GetComponent<Rigidbody>();
        if (body == null || body.isKinematic)
        {
            return;
        }

        Rigidbody otherBody = collision.rigidbody;
        ControlUnit otherUnit = otherBody != null ? otherBody.GetComponent<ControlUnit>() : null;
        // A blast-driven fragment must not damage an unprotected neighbour either.
        if (Time.time < unit.ExplosionCollisionProtectedUntil || (otherUnit != null && Time.time < otherUnit.ExplosionCollisionProtectedUntil))
        {
            return;
        }

        float reducedMass = ImpactPhysics.ReducedMass(body.mass, otherBody != null ? otherBody.mass : 0f, otherBody == null || otherBody.isKinematic);
        float normalSpeed = 0f;
        float normalImpulse = 0f;
        int contacts = 0;
        _impactTargets.Clear();
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);
            normalSpeed = Mathf.Max(normalSpeed, Mathf.Abs(Vector3.Dot(collision.relativeVelocity, contact.normal)));
            normalImpulse = Mathf.Max(normalImpulse, Mathf.Abs(Vector3.Dot(collision.impulse, contact.normal)));
            Durability target = contact.thisCollider.GetComponentInParent<Durability>();
            if (target == null || !target.enabled || target.GetComponentInParent<ControlUnit>() != unit)
            {
                continue;
            }

            _impactTargets.TryGetValue(target, out int count);
            _impactTargets[target] = count + 1;
            contacts++;
        }

        // Compound-collider callbacks partition solver impulses; using full construct mass
        // per callback would repeatedly count the same kinetic energy.
        float effectiveMass = normalSpeed > 0.001f ? Mathf.Min(reducedMass, normalImpulse / normalSpeed) : 0f;
        if (unit.faction == UnitFaction.Player && normalSpeed > 1f)
        {
            ImpactPhysics.ReportPlayerImpact(0.5f * effectiveMass * normalSpeed * normalSpeed, normalImpulse);
        }

        // Extra manifold points do not create energy. Dynamic pairs split the fixed budget equally.
        float sideShare = otherBody == null || otherBody.isKinematic ? 1f : 0.5f;
        foreach (KeyValuePair<Durability, int> target in _impactTargets)
        {
            if (target.Key != null)
            {
                target.Key.ApplyCollisionEnergy(effectiveMass, normalSpeed, sideShare * target.Value / Mathf.Max(1, contacts));
            }
        }
    }
}

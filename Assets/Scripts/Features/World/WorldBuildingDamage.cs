// 职责：顺序结算建筑范围伤害；爆炸连锁排队，避免递归销毁耗尽调用栈。
using System.Collections.Generic;
using UnityEngine;

internal sealed class WorldBuildingDamage
{
    private readonly Queue<Blast> pending = new Queue<Blast>();
    private readonly List<Durability> targets = new List<Durability>();
    private bool applying;
    private struct Blast
    {
        public Vector3 center;
        public float radius;
        public float damage;
    }

    public void Apply(IReadOnlyList<BuildingRuntime> buildings, Vector3 center, float radius, float damage)
    {
        if (!WorldBuildingValidation.Finite(center) || !WorldBuildingValidation.Finite(radius) || !WorldBuildingValidation.Finite(damage) || radius <= 0f || damage <= 0f)
        {
            return;
        }

        pending.Enqueue(new Blast { center = center, radius = radius, damage = damage });
        if (applying)
        {
            return;
        }

        applying = true;
        try
        {
            while (pending.Count > 0)
            {
                Blast blast = pending.Dequeue();
                targets.Clear();
                foreach (BuildingRuntime building in buildings)
                {
                    if (building == null)
                    {
                        continue;
                    }

                    foreach (Block block in building.Blocks)
                    {
                        if (block != null && Vector3.Distance(block.transform.position, blast.center) <= blast.radius && block.TryGetComponent(out Durability health))
                        {
                            targets.Add(health);
                        }
                    }
                }

                foreach (Durability health in targets)
                {
                    if (health != null && health.isActiveAndEnabled)
                    {
                        float falloff = Mathf.Clamp01(1f - Vector3.Distance(health.transform.position, blast.center) / blast.radius);
                        health.ApplyDamage(blast.damage * falloff, null);
                    }
                }
            }
        }
        finally
        {
            applying = false;
            pending.Clear();
            targets.Clear();
        }
    }
}

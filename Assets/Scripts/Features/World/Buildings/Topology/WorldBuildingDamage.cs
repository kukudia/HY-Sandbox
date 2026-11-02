// 职责：顺序结算建筑范围伤害；爆炸连锁排队，避免递归销毁耗尽调用栈。
using System.Collections.Generic;
using UnityEngine;

internal sealed class WorldBuildingDamage
{
    private readonly Queue<Blast> pending = new Queue<Blast>();
    private readonly List<Durability> targets = new List<Durability>();
    private readonly List<BuildingRuntime> batched = new List<BuildingRuntime>();
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
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingDamage);
        try
        {
            // 同一轮连锁先完成命中与销毁，再按建筑各遍历一次支撑。
            // 不让先脱落的模块提前禁用，从而跳过本轮已命中的其他模块。
            foreach (BuildingRuntime building in buildings)
            {
                if (building != null)
                {
                    building.BeginDamageBatch();
                    batched.Add(building);
                }
            }

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
            System.Exception refreshFailure = null;
            try
            {
                foreach (BuildingRuntime building in batched)
                {
                    if (building != null)
                    {
                        try
                        {
                            building.EndDamageBatch();
                        }
                        catch (System.Exception error)
                        {
                            // 一栋建筑刷新失败也必须释放其他建筑的批次深度。
                            refreshFailure ??= error;
                        }
                    }
                }
            }
            finally
            {
                applying = false;
                pending.Clear();
                targets.Clear();
                batched.Clear();
            }

            if (refreshFailure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(refreshFailure).Throw();
            }
        }
    }
}

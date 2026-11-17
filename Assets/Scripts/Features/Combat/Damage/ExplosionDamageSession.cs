// 职责：爆炸只破坏体素地形；显式建筑伤害工具独立结算，批末刷新拓扑与冲量。
// 入口：ApplyExplosion() / ApplyBuildings() / RefreshUnit()；实例由显式销毁服务或独立建筑工具拥有。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class ExplosionDamageSession
{
    private readonly Queue<Blast> pending = new Queue<Blast>();
    private readonly List<Durability> targets = new List<Durability>();
    private readonly HashSet<BuildingRuntime> batched = new HashSet<BuildingRuntime>();
    private readonly HashSet<ControlUnit> dirtyUnits = new HashSet<ControlUnit>();
    private readonly List<Action> impulses = new List<Action>();
    private bool applying;
    private struct Blast
    {
        public Vector3 center;
        public float radius;
        public float damage;
        public PhysicsScene physics;
        public ControlUnit attacker;
        public IReadOnlyList<BuildingRuntime> buildings;
        public bool terrain;
    }

    public void ApplyBuildings(IReadOnlyList<BuildingRuntime> buildings, Vector3 center, float radius, float damage)
    {
        Enqueue(new Blast { center = center, radius = radius, damage = damage, buildings = buildings });
    }

    public void ApplyBuildingLoss(BuildingRuntime building, Action destroy)
    {
        Execute(() =>
        {
            if (building != null && batched.Add(building))
            {
                building.BeginDamageBatch();
            }

            destroy();
        });
    }

    public void ApplyExplosion(Block source, ControlUnit attacker, ControlUnit dirtyUnit, Action impulse)
    {
        if (source == null)
        {
            return;
        }

        if (dirtyUnit != null)
        {
            dirtyUnits.Add(dirtyUnit);
        }

        if (impulse != null)
        {
            impulses.Add(impulse);
        }

        Enqueue(new Blast { center = source.transform.position, radius = source.explosionRadius, damage = source.explosionDamage, physics = source.gameObject.scene.GetPhysicsScene(), attacker = attacker, terrain = true });
    }

    public void RefreshUnit(ControlUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        if (applying)
        {
            dirtyUnits.Add(unit);
        }
        else
        {
            PlayManager.instance?.RefreshGroup(unit);
        }
    }

    private void Enqueue(Blast blast)
    {
        if (!WorldBuildingValidation.Finite(blast.center) || !WorldBuildingValidation.Finite(blast.radius) || !WorldBuildingValidation.Finite(blast.damage) || blast.radius <= 0f || blast.damage < 0f)
        {
            if (!applying)
            {
                FinishBatch();
            }

            return;
        }

        pending.Enqueue(blast);
        if (applying)
        {
            return;
        }

        Execute(() =>
        {
        });
    }

    private void Execute(Action initial)
    {
        if (applying)
        {
            initial();
            return;
        }

        applying = true;
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingDamage);
        try
        {
            initial();
            while (pending.Count > 0)
            {
                Apply(pending.Dequeue());
            }
        }
        finally
        {
            FinishBatch();
        }
    }

    private void Apply(Blast blast)
    {
        targets.Clear();
        if (blast.buildings != null)
        {
            foreach (BuildingRuntime building in blast.buildings)
            {
                if (building == null)
                {
                    continue;
                }

                foreach (Block block in building.Blocks)
                {
                    if (block != null && block.TryGetComponent(out Durability health))
                    {
                        AddTarget(health, blast);
                    }
                }
            }
        }

        // A block explosion has no module targets. Projectile hits and the explicit building
        // damage brush keep their own damage contracts; nearby modules never receive blast damage.
        foreach (Durability health in targets)
        {
            if (health != null && health.isActiveAndEnabled && health.currentDurability > 0f)
            {
                float falloff = Mathf.Clamp01(1f - Vector3.Distance(health.transform.position, blast.center) / blast.radius);
                health.ApplyDamage(blast.damage * falloff, blast.attacker);
            }
        }

        if (blast.terrain && blast.damage > 0f)
        {
            foreach (ProceduralVoxelWorld world in UnityEngine.Object.FindObjectsByType<ProceduralVoxelWorld>())
            {
                if (world.gameObject.scene.GetPhysicsScene() == blast.physics)
                {
                    world.DamageSphere(blast.center, blast.radius, blast.damage);
                }
            }
        }
    }

    private void AddTarget(Durability health, Blast blast)
    {
        if (!health.isActiveAndEnabled || health.currentDurability <= 0f || Vector3.Distance(health.transform.position, blast.center) >= blast.radius)
        {
            return;
        }

        BuildingRuntime building = health.GetComponentInParent<BuildingRuntime>();
        if (building != null && batched.Add(building))
        {
            building.BeginDamageBatch();
        }

        targets.Add(health);
    }

    private void FinishBatch()
    {
        Exception failure = null;
        try
        {
            foreach (BuildingRuntime building in batched)
            {
                if (building != null)
                {
                    Attempt(building.EndDamageBatch, ref failure);
                }
            }

            foreach (ControlUnit unit in dirtyUnits)
            {
                if (unit != null && unit.Building == null)
                {
                    Attempt(() => PlayManager.instance?.RefreshGroup(unit), ref failure);
                }
            }

            // Detached groups receive impulses only after both kinds of topology settle.
            if (impulses.Count > 0)
            {
                Physics.SyncTransforms();
            }

            foreach (Action impulse in impulses)
            {
                Attempt(impulse, ref failure);
            }
        }
        finally
        {
            applying = false;
            pending.Clear();
            targets.Clear();
            batched.Clear();
            dirtyUnits.Clear();
            impulses.Clear();
        }

        if (failure != null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    private static void Attempt(Action action, ref Exception failure)
    {
        try
        {
            action();
        }
        catch (Exception error)
        {
            failure ??= error;
        }
    }
}

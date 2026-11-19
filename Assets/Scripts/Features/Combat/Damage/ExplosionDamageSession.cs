// 职责：统一方块损毁批次；先处理目标耐久与体素，再重组失效的连接图，最后施加冲量。
// 入口：ApplyTargets() / ApplyExplosion() / RefreshUnit()；场景服务和独立模拟宿主持有会话。
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class ExplosionDamageSession
{
    private readonly HashSet<ControlUnit> dirtyUnits = new HashSet<ControlUnit>();
    private readonly List<Action> impulses = new List<Action>();
    private bool applying;
    internal void ApplyTargets(List<Durability> targets, Vector3 center, float radius, float damage)
    {
        if (!ValidSphere(center, radius, damage))
        {
            return;
        }

        Execute(() =>
        {
            foreach (Durability health in targets)
            {
                if (health != null && health.isActiveAndEnabled && health.currentDurability > 0f)
                {
                    float falloff = Mathf.Clamp01(1f - Vector3.Distance(health.transform.position, center) / radius);
                    health.ApplyDamage(damage * falloff, null);
                }
            }
        });
    }

    internal void ApplyExplosion(Block source, ControlUnit unit, Action impulse)
    {
        Execute(() =>
        {
            RefreshUnit(unit);
            impulses.Add(impulse);
            Vector3 center = source.transform.position;
            if (!ValidSphere(center, source.explosionRadius, source.explosionDamage))
            {
                return;
            }

            PhysicsScene physics = source.gameObject.scene.GetPhysicsScene();
            foreach (ProceduralVoxelWorld world in UnityEngine.Object.FindObjectsByType<ProceduralVoxelWorld>())
            {
                if (world.gameObject.scene.GetPhysicsScene() == physics)
                {
                    world.DamageSphere(center, source.explosionRadius, source.explosionDamage);
                }
            }
        });
    }

    internal void RefreshUnit(ControlUnit unit)
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
            RuntimeGroupBuilder.RefreshUnit(unit);
        }
    }

    private static bool ValidSphere(Vector3 center, float radius, float damage)
    {
        return WorldBuildingValidation.Finite(center) && WorldBuildingValidation.Finite(radius) && radius > 0f && WorldBuildingValidation.Finite(damage) && damage > 0f;
    }

    private void Execute(Action action)
    {
        if (applying)
        {
            action();
            return;
        }

        applying = true;
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingDamage);
        try
        {
            action();
        }
        finally
        {
            FinishBatch();
        }
    }

    private void FinishBatch()
    {
        Exception failure = null;
        try
        {
            foreach (ControlUnit unit in dirtyUnits)
            {
                if (unit != null)
                {
                    Attempt(() => RuntimeGroupBuilder.RefreshUnit(unit), ref failure);
                }
            }

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

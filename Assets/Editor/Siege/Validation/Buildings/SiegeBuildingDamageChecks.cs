// 职责：验证建筑批量损毁的命中顺序独立、一次支撑刷新、链式爆炸及单块即时断裂契约。
// 入口：Run()；仅使用远离场景的自有夹具，恢复阵地战伤害门控，不改资产。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeBuildingDamageChecks
{
    public static string Run()
    {
        if (!EditorApplication.isPlaying || PlayManager.instance == null || PlayManager.instance.playMode)
        {
            throw new InvalidOperationException("Requires Siege deployment Play.");
        }

        SiegeSession siege = PlayManager.instance.SiegeSession;
        bool metrics = SimulationWorkMetrics.Enabled;
        var checks = new List<string>();
        try
        {
            PlayManager.instance.BindSiege(null);
            foreach (bool reverse in new[]
            {
                false,
                true
            }

            )
            {
                CheckPartialBlast(checks, reverse);
            }

            CheckChain(checks, false);
            CheckChain(checks, true);
            CheckImmediate(checks);
            string json = JsonUtility.ToJson(new Report { checks = checks }, true);
            Directory.CreateDirectory("Temp/Siege");
            File.WriteAllText("Temp/Siege/building-damage-checks.json", json);
            return json;
        }
        finally
        {
            SimulationWorkMetrics.Enabled = metrics;
            PlayManager.instance.BindSiege(siege);
        }
    }

    private static void CheckPartialBlast(List<string> checks, bool reverse)
    {
        GameObject owner = CreateTower(reverse, out WorldBuildingManager manager, out BuildingRuntime tower);
        try
        {
            Block bottom = tower.Blocks.Single(block => block.transform.localPosition.y == 0.5f);
            Block top = tower.Blocks.Single(block => block.transform.localPosition.y == 1.5f);
            int destroyed = 0;
            tower.BlockDestroyed += _ => destroyed++;
            SimulationWorkMetrics.Reset();
            SimulationWorkMetrics.Enabled = true;
            manager.DamageSphere(bottom.transform.position, 2f, 150f);
            Check(
                checks,
                "partial blast damages future debris before detachment, reverse=" + reverse,
                Mathf.Approximately(top.GetComponent<Durability>().currentDurability, 25f));
            Check(
                checks,
                "partial blast removes stem and detaches top, reverse=" + reverse,
                destroyed == 1 && tower.Blocks.Count == 0 && top.transform.parent != tower.transform && !top.enabled);
            Check(
                checks,
                "one support traversal per dirty building, reverse=" + reverse,
                SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingSupport).calls == 1);
            Check(
                checks,
                "snapshot omits removed and detached modules, reverse=" + reverse,
                tower.Capture().blueprint.blocks.Count == 0);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    private static void CheckChain(List<string> checks, bool projectile)
    {
        GameObject owner = CreateTower(false, out WorldBuildingManager manager, out BuildingRuntime tower);
        try
        {
            foreach (Block block in tower.Blocks)
            {
                block.canExplode = true;
                block.explosionRadius = 2f;
                block.explosionForce = 1000f;
            }

            Vector3 center = tower.Blocks[0].transform.position;
            int destroyed = 0;
            tower.BlockDestroyed += _ => destroyed++;
            SimulationWorkMetrics.Reset();
            SimulationWorkMetrics.Enabled = true;
            if (projectile)
            {
                tower.Blocks[0].GetComponent<Durability>().ApplyDamage(150f, null);
            }
            else
            {
                manager.DamageSphere(center, 0.25f, 150f);
            }

            Check(
                checks,
                "queued chain destroys each explosive module once, projectile=" + projectile,
                destroyed == 2 && tower.Blocks.Count == 0);
            Check(
                checks,
                "whole chain performs one support traversal, projectile=" + projectile,
                SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingSupport).calls == 1);
            Check(
                checks,
                "chain has one outer damage scope",
                SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingDamage).calls == 1);
            SimulationWorkMetrics.Reset();
            manager.DamageSphere(center, 2f, 100f);
            Check(
                checks,
                "empty or unchanged blast does not refresh support",
                SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingSupport).calls == 0);
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    private static void CheckImmediate(List<string> checks)
    {
        GameObject owner = CreateTower(false, out _, out BuildingRuntime tower);
        try
        {
            Block bottom = tower.Blocks[0];
            Block top = tower.Blocks[1];
            SimulationWorkMetrics.Reset();
            SimulationWorkMetrics.Enabled = true;
            bottom.GetComponent<Durability>().ApplyDamage(150f, null);
            Check(
                checks,
                "single projectile loss still resolves support immediately",
                tower.Blocks.Count == 0 && top.transform.parent != tower.transform && SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingSupport).calls == 1);
            Check(
                checks,
                "standalone loss updates remaining body mass immediately",
                Mathf.Approximately(tower.GetComponent<Rigidbody>().mass, 1f));
        }
        finally
        {
            Object.DestroyImmediate(owner);
        }
    }

    private static GameObject CreateTower(bool reverse, out WorldBuildingManager manager, out BuildingRuntime tower)
    {
        var owner = new GameObject("Owned building damage checks");
        owner.SetActive(false);
        manager = owner.AddComponent<WorldBuildingManager>();
        var config = new SerializedObject(manager);
        config.FindProperty("initializeOnStart").boolValue = false;
        config.ApplyModifiedPropertiesWithoutUndo();
        owner.SetActive(true);
        var design = new BlockDataList
        {
            foundation = new BuildingFoundationData()
        };
        Block prefab = Resources.Load<GameObject>("Blocks/1x1x1").GetComponent<Block>();
        foreach (int y in reverse ? new[]
        {
            1,
            0
        }

        : new[]
        {
            0,
            1
        }

        )
        {
            design.blocks.Add(new BlockData(prefab) { id = "level" + y, resourcePath = "Blocks/1x1x1", x = 1, y = 1, z = 1, posX = -0.5f, posY = y + 0.5f, posZ = -0.5f, rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f, cargo = null });
        }

        tower = manager.RestoreBuilding(new WorldBuildingState { instanceId = Guid.NewGuid().ToString("N"), blueprintName = "Damage checks", faction = UnitFaction.Player, position = new Vector3(10000f, 1000f, 0f), rotation = Quaternion.identity, anchored = true, blueprint = design, durability = new[] { 100f, 100f } });
        foreach (Block block in tower.Blocks)
        {
            block.canExplode = false;
            Durability health = block.GetComponent<Durability>();
            health.maxDurability = health.currentDurability = 100f;
        }

        Physics.SyncTransforms();
        return owner;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Building damage check failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

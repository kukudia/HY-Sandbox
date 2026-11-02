// 职责：验证真实大型建筑蓝图基地的支撑/净空、原位修缮、独立核心以及舰船生命周期隔离。
// 入口：Run()；全新Siege Play，显式损坏夹具不代表自然波次或性能结论。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeBuildingChecks
{
    public static string Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0 || session.Fleet.Ships.Any(entry => entry != null))
        {
            throw new InvalidOperationException("Requires fresh Siege Play without deployed ships.");
        }

        var checks = new List<string>();
        SiegeBase baseEntry = session.Fleet.Base;
        BuildingRuntime building = baseEntry.Building;
        WorldBuildingValidation.ValidateBlueprint(baseEntry.Blueprint);
        Check(
            checks,
            "base uses valid 32x32 building blueprint with hundreds of modules",
            building.FootprintSize == new Vector2Int(32, 32) && building.Blocks.Count >= 350);
        Check(
            checks,
            "base has no cockpit and owns independent core module",
            building.GetComponentsInChildren<Cockpit>(true).Length == 0 && baseEntry.Core.uniqueId == baseEntry.Id + ":" + session.Settings.BaseCoreBlockId);
        Check(
            checks,
            "building unit is operational and anchored outside combat",
            baseEntry.Unit.IsOperational && building.Anchored && !baseEntry.Unit.IsSimulationActive);
        Check(
            checks,
            "all authored building modules form one component",
            BlockGroupManager.GroupBlocks(building.Blocks.ToList()).Count == 1);
        Physics.SyncTransforms();
        bool overlaps = false;
        for (int first = 0; first < building.Blocks.Count; first++)
        {
            for (int second = first + 1; second < building.Blocks.Count; second++)
            {
                // 炮塔/中继规范Collider带安装座向下偏移；蓝图占格使用模块尺寸，不把安装座视为重复模块。
                Block a = building.Blocks[first];
                Block b = building.Blocks[second];
                var aBounds = new Bounds(a.transform.position, new Vector3(a.x, a.y, a.z) - Vector3.one * 0.1f);
                var bBounds = new Bounds(b.transform.position, new Vector3(b.x, b.y, b.z) - Vector3.one * 0.1f);
                overlaps |= aBounds.Intersects(bBounds);
            }
        }

        Check(checks, "blueprint module occupancy has no overlapping authored volumes", !overlaps);
        int count = building.Blocks.Count;
        ControlUnit unit = baseEntry.Unit;
        Rigidbody body = unit.GetComponent<Rigidbody>();
        Block core = baseEntry.Core;
        PlayManager.instance.RefreshGroup(unit);
        Check(
            checks,
            "ship regroup entry delegates to building support and retains complete base",
            baseEntry.Unit == unit && unit.GetComponent<Rigidbody>() == body && building.Blocks.Count == count && building.Anchored);
        Check(
            checks,
            "all floor/wall/tower modules are supported by the virtual foundation",
            building.GetComponentsInChildren<Block>().Length == count);
        Durability health = core.GetComponent<Durability>();
        float beforeDamage = health.currentDurability;
        health.ApplyDamage(100f, null);
        Check(
            checks,
            "intermission rejects projectile damage to building core",
            Mathf.Approximately(health.currentDurability, beforeDamage));
        health.currentDurability -= 25f;
        long quote = baseEntry.Quote();
        long coins = session.Match.Coins;
        Check(
            checks,
            "building health refit retains root and core with exact quote",
            quote > 0 && session.TryRepair(true) && session.Match.Coins == coins - quote && baseEntry.Core == core && baseEntry.Unit == unit && Mathf.Approximately(health.currentDurability, health.maxDurability));
        // 压力蓝图细分了部分装甲，改用实际存在的非核心装甲；保留正常基地的原检查位置。
        Block removed = building.Blocks.FirstOrDefault(block => block.transform.localPosition == new Vector3(1f, 1f, 1f)) ?? building.Blocks.First(block => block.resourcePath == "Blocks/1x1x1");
        string removedId = removed.uniqueId;
        Vector3 missingPosition = removed.transform.position;
        building.DestroyBlock(removed);
        Check(
            checks,
            "non-core building destruction keeps match and core alive",
            session.Match.Phase == SiegePhase.Deployment && baseEntry.IsAlive && building.Blocks.Count == count - 1);
        quote = baseEntry.Quote();
        coins = session.Match.Coins;
        GameObject obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstruction.name = "Owned base repair obstruction";
        obstruction.transform.position = missingPosition;
        try
        {
            Check(
                checks,
                "occupied building replacement is rejected without spending",
                !session.TryRepair(true) && session.Match.Coins == coins && building.Blocks.Count == count - 1);
            Check(
                checks,
                "building repair names the obstruction and retry action",
                session.Status.Contains(obstruction.name) && session.Status.Contains("Clear the area"));
        }
        finally
        {
            Object.DestroyImmediate(obstruction);
        }

        Check(
            checks,
            "missing building module is rebuilt for exact cost",
            quote > 0 && session.TryRepair(true) && session.Match.Coins == coins - quote && building.Blocks.Count == count);
        Check(
            checks,
            "building replacement preserves blueprint module identity exactly once",
            building.Blocks.Count(block => block != null && block.uniqueId == removedId) == 1 && building.Blocks.Select(block => block.uniqueId).Distinct().Count() == count);
        Check(
            checks,
            "refitted building remains one supported anchored structure",
            BlockGroupManager.GroupBlocks(building.Blocks.ToList()).Count == 1 && building.Anchored && baseEntry.Core == core);
        Check(
            checks,
            "a ship can deploy independently beside the large building",
            session.TryDeploy(0, out _) && session.Fleet.Driven != null && session.Fleet.Driven.Unit.Building == null);
        Check(
            checks,
            "combat activates building systems while preserving ground anchor",
            session.BeginNextWave() && unit.IsSimulationActive && body.isKinematic);
        Check(checks, "combat rejects building repair", !session.TryRepair(true));
        core.canExplode = false;
        health.ApplyDamage(100000f, null);
        Check(
            checks,
            "real building core destruction ends match immediately",
            session.Match.Phase == SiegePhase.Defeat && !PlayManager.instance.playMode && session.Match.PendingAttackers == 0);
        Check(
            checks,
            "defeated building cannot be rebuilt or launch another wave",
            !session.TryRepair(true) && !session.BeginNextWave());
        string json = JsonUtility.ToJson(new Report { checks = checks }, true);
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/building-checks.json", json);
        return json;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Siege building check failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

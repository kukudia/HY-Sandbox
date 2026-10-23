// 职责：验证独立混合压力场的实际建筑规模、预算、模块身份和卫星损毁/清理边界。
// 入口：Start() / Status；真实Play加载完成后执行，仅作用于该测试会话。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeMixedStressChecks
{
    private static float deadline;
    public static string Status { get; private set; } = "Not run";

    public static void Start()
    {
        if (!EditorApplication.isPlaying || Object.FindFirstObjectByType<SiegeStressPopulation>() == null)
        {
            throw new InvalidOperationException("Requires fresh mixed stress Play.");
        }

        deadline = Time.realtimeSinceStartup + 90f;
        Status = "Loading population";
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/mixed-checks.json", "{\"status\":\"running\"}");
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        var report = new Report();
        try
        {
            SiegeStressPopulation population = Object.FindFirstObjectByType<SiegeStressPopulation>();
            if (EditorApplication.isPlaying && population != null && !population.IsReady && string.IsNullOrEmpty(population.Error) && Time.realtimeSinceStartup < deadline)
            {
                return;
            }

            EditorApplication.update -= Tick;
            Check(
                report,
                "population loaded without errors",
                population != null && population.IsReady && string.IsNullOrEmpty(population.Error));
            SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
            BuildingRuntime[] buildings = session.Buildings.Buildings.ToArray();
            Check(
                report,
                "four real2048 buildings reach8192 budget",
                buildings.Length == 4 && buildings.All(building => building.Blocks.Count == 2048));
            Check(
                report,
                "satellites account for6144 blocks",
                population.Buildings.Count == 3 && population.ConfiguredBlocks == 6144);
            Check(
                report,
                "all building modules have unique runtime identities",
                buildings.SelectMany(building => building.Blocks).Select(block => block.uniqueId).Distinct().Count() == 8192);
            Check(
                report,
                "buildings remain anchored and have no cockpit",
                buildings.All(building => building.Anchored && !building.GetComponent<ControlUnit>().HasAnyCockpit));
            Check(
                report,
                "satellites preserve player faction",
                population.Buildings.All(building => building.GetComponent<ControlUnit>().faction == UnitFaction.Player));
            Check(report, "existing world budget rejects another module", !session.Buildings.HasCapacity(1));
            Check(report, "HUD restored after loading", Object.FindFirstObjectByType<SiegeHud>().enabled);
            foreach (BuildingRuntime building in buildings)
            {
                PlayManager.instance.RefreshGroup(building.GetComponent<ControlUnit>());
            }

            Check(
                report,
                "real support refresh preserves all intact buildings",
                buildings.All(building => building.Blocks.Count == 2048 && building.Anchored));
            Check(report, "wave starts after population is ready", session.BeginNextWave());
            BuildingRuntime satellite = population.Buildings[0];
            Block cap = satellite.Blocks.Single(block => block.uniqueId.EndsWith(":stress-supported-cap", StringComparison.Ordinal));
            cap.canExplode = false;
            cap.GetComponent<Durability>().ApplyDamage(1000000f, null);
            Check(
                report,
                "satellite damage uses independent building topology",
                satellite.Blocks.Count == 2047 && session.Fleet.Base.Building.Blocks.Count == 2048);
            Check(report, "damage releases existing building capacity", session.Buildings.HasCapacity(1));
            Block extraCore = satellite.Blocks.Single(block => block.uniqueId.EndsWith(":bastion-core", StringComparison.Ordinal));
            extraCore.canExplode = false;
            extraCore.GetComponent<Durability>().ApplyDamage(1000000f, null);
            Check(
                report,
                "satellite core loss does not replace primary base defeat rule",
                session.Fleet.Base.IsAlive && session.Match.Phase == SiegePhase.Battle);
            Object.DestroyImmediate(population);
            Check(
                report,
                "removing stress component releases only its satellites",
                session.Buildings.Buildings.Count == 1 && session.Buildings.Buildings[0] == session.Fleet.Base.Building);
            session.End();
            report.status = "passed";
        }
        catch (Exception error)
        {
            EditorApplication.update -= Tick;
            report.status = "failed";
            report.error = error.ToString();
            Debug.LogException(error);
        }

        Status = "Temp/Siege/mixed-checks.json";
        File.WriteAllText(Status, JsonUtility.ToJson(report, true));
    }

    private static void Check(Report report, string label, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(label);
        }

        report.checks.Add(label);
    }

    [Serializable]
    private sealed class Report
    {
        public string status;
        public string error;
        public List<string> checks = new List<string>();
    }
}

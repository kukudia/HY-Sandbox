// 职责：逐个恢复官方/个人基地并使用生产连接图检查地面支撑，定位JSON合法但不能部署的设计。
// 入口：Start() / Status；Main Play远端隔离逐帧检查，只报告，不删除或重写个人创作。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlueprintSupportAudit
{
    [Serializable]
    private sealed class Result
    {
        public string path;
        public string status;
        public string reason;
        public int modules;
        public List<string> unsupportedModules = new List<string>();
    }

    [Serializable]
    private sealed class Report
    {
        public string status = "running";
        public List<Result> entries = new List<Result>();
        public string error;
    }

    private static Report report;
    private static IEnumerator work;
    private static GameObject root;
    private static float deadline;
    public static string Status { get; private set; } = "Not run";

    public static void Start()
    {
        if (!Application.isPlaying || PlayManager.instance == null || work != null)
        {
            throw new InvalidOperationException("Requires Main Play Mode and no other support audit.");
        }

        report = new Report();
        deadline = Time.realtimeSinceStartup + 600f;
        work = Run();
        EditorApplication.update += Tick;
        Status = "Running";
        Save();
    }

    private static void Tick()
    {
        try
        {
            if (!Application.isPlaying || Time.realtimeSinceStartup > deadline)
            {
                throw new TimeoutException("Support audit stopped or exceeded600 seconds.");
            }

            if (work.MoveNext())
            {
                return;
            }

            report.status = "complete";
        }
        catch (Exception error)
        {
            report.error = error.ToString();
            report.status = "failed";
        }

        EditorApplication.update -= Tick;
        (work as IDisposable)?.Dispose();
        work = null;
        if (root != null)
        {
            root.SetActive(false);
            Object.Destroy(root);
            root = null;
        }

        Save();
        Status = report.status;
    }

    private static IEnumerator Run()
    {
        root = new GameObject("Owned blueprint support audit");
        root.SetActive(false);
        WorldBuildingManager manager = root.AddComponent<WorldBuildingManager>();
        var settings = new SerializedObject(manager);
        settings.FindProperty("initializeOnStart").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        typeof(WorldBuildingManager).GetProperty("SimulationEnabled").SetValue(manager, false);
        IEnumerable<BlueprintIntegrityAudit.Entry> entries = BlueprintIntegrityAudit.InspectAll().entries.Where(entry => entry.category == "BaseBlueprints" || entry.path.Replace('\\', '/').Contains("/Assets/Siege/Construction/"));
        foreach (BlueprintIntegrityAudit.Entry entry in entries)
        {
            var result = new Result
            {
                path = entry.path,
                status = "skipped",
                reason = entry.reason
            };
            report.entries.Add(result);
            if (entry.status == "invalid" || entry.status == "draft")
            {
                Save();
                continue;
            }

            BlockDataList design = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(entry.path));
            design.foundation = BuildingFootprint.Calculate(design.blocks, 1);
            BuildingRuntime restored = null;
            var state = new WorldBuildingState
            {
                instanceId = "support-" + Guid.NewGuid().ToString("N"),
                blueprintName = Path.GetFileNameWithoutExtension(entry.path),
                faction = UnitFaction.Player,
                position = new Vector3(17000, 4000, 17000),
                rotation = Quaternion.identity,
                blueprint = design,
                durability = Enumerable.Repeat(float.MaxValue, design.blocks.Count).ToArray(),
                anchored = true
            };
            IEnumerator loading = (IEnumerator)typeof(WorldBuildingManager).GetMethod("RestoreBuildingRoutine", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, new object[] { state, new Action<BuildingRuntime>(building => restored = building) });
            try
            {
                while (loading.MoveNext())
                {
                    yield return null;
                }
            }
            finally
            {
                (loading as IDisposable)?.Dispose();
            }

            root.SetActive(true);
            IEnumerator activation = (IEnumerator)typeof(BuildingRuntime).GetMethod("ActivateRoutine", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(restored, null);
            try
            {
                while (activation.MoveNext())
                {
                    yield return null;
                }
            }
            finally
            {
                (activation as IDisposable)?.Dispose();
            }

            result.modules = restored.Blocks.Count;
            bool supported = restored.HasFullFoundationSupport();
            if (!supported)
            {
                var ground = typeof(BuildingRuntime).GetMethod("HasGroundConnector", BindingFlags.Instance | BindingFlags.NonPublic);
                var prepared = typeof(BlockGroupManager).GetMethod("GroupPreparedBlocks", BindingFlags.Static | BindingFlags.NonPublic);
                var components = (List<List<Block>>)prepared.Invoke(null, new object[] { new List<Block>(restored.Blocks) });
                foreach (List<Block> component in components)
                {
                    if (!component.Any(block => (bool)ground.Invoke(restored, new object[] { block })))
                    {
                        result.unsupportedModules.AddRange(component.Select(block => block.uniqueId));
                    }
                }
            }

            result.status = supported ? "supported" : "unsupported";
            result.reason = supported ? "Every real connector component reaches its foundation." : "At least one connected component has no downward foundation connector; retained for repair.";
            manager.RemoveBuilding(restored);
            root.SetActive(false);
            Save();
            yield return null;
        }
    }

    private static void Save()
    {
        Directory.CreateDirectory("Temp/BlueprintAuditChecks");
        File.WriteAllText("Temp/BlueprintAuditChecks/support-audit.json", JsonUtility.ToJson(report, true));
    }
}

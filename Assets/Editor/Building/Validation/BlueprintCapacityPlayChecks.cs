// 职责：通过生产分帧恢复/修缮链验证大型建筑不再受2048/8192模块硬限制，并验证取消回滚。
// 入口：Start() / Status；仅Main Play，远端隔离夹具，串行逐帧执行，结束销毁本工具拥有的对象。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlueprintCapacityPlayChecks
{
    [Serializable]
    private sealed class Report
    {
        public string status = "running";
        public List<string> checks = new List<string>();
        public string error;
    }

    private static readonly Report report = new Report();
    private static IEnumerator work;
    private static GameObject root;
    private static float deadline;
    public static string Status { get; private set; } = "Not run";

    public static void Start()
    {
        if (!Application.isPlaying || PlayManager.instance == null || work != null)
        {
            throw new InvalidOperationException("Requires Main Play Mode and no other capacity check.");
        }

        report.status = "running";
        report.checks.Clear();
        report.error = null;
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
                throw new TimeoutException("Capacity Play check stopped or exceeded600 seconds.");
            }

            if (work.MoveNext())
            {
                return;
            }

            report.status = "passed";
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
        root = new GameObject("Owned large building capacity validation");
        root.SetActive(false);
        WorldBuildingManager manager = root.AddComponent<WorldBuildingManager>();
        var settings = new SerializedObject(manager);
        settings.FindProperty("initializeOnStart").boolValue = false;
        settings.ApplyModifiedPropertiesWithoutUndo();
        typeof(WorldBuildingManager).GetProperty("LoadingFrameBudgetMilliseconds", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, 2f);
        BlockDataList design = CreateDesign();
        WorldBuildingState state = State(design, "cancelled", 0);
        bool published = false;
        IEnumerator loading = Restore(manager, state, building => published = true);
        try
        {
            Require(loading.MoveNext(), "large restore yields before publishing the building");
        }
        finally
        {
            (loading as IDisposable)?.Dispose();
        }

        yield return null;
        Require(
            !published && manager.Buildings.Count == 0 && root.transform.childCount == 0,
            "disposing staged restore rolls back all partial owned objects");
        var buildings = new List<BuildingRuntime>();
        for (int i = 0; i < 4; i++)
        {
            BuildingRuntime restored = null;
            loading = Restore(manager, State(design, "building-" + i, i), building => restored = building);
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

            Require(
                restored != null && restored.Blocks.Count == 2100,
                "production staged restore creates2100 modules for building" + i);
            buildings.Add(restored);
        }

        Require(
            manager.Buildings.Sum(building => building.Blocks.Count) == 8400 && manager.CanAddBlocks(1),
            "production manager owns8400 modules and still accepts additions");
        root.SetActive(true);
        foreach (BuildingRuntime building in buildings)
        {
            IEnumerator activation = (IEnumerator)typeof(BuildingRuntime).GetMethod("ActivateRoutine", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(building, null);
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
        }

        Require(
            buildings.All(building => building.IsOperational && building.Anchored),
            "all four actual large buildings activate anchored");
        BuildingRuntime first = buildings[0];
        Block removed = first.Blocks.Last();
        string missing = removed.uniqueId;
        removed.canExplode = false;
        DestroyManager.DestroyTarget(removed.gameObject);
        Require(first.Blocks.Count == 2099, "large live building loses one real module");
        object[] arguments =
        {
            design,
            null
        };
        bool repaired = (bool)typeof(BuildingRuntime).GetMethod("TryRefit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(first, arguments);
        Require(
            repaired && first.Blocks.Count == 2100 && first.Blocks.Any(block => block.uniqueId == missing),
            "production refit restores a missing identity beyond8192 aggregate modules");
        Require(
            manager.Buildings.Sum(building => building.Blocks.Count) == 8400,
            "refit preserves total module count exactly");
    }

    private static IEnumerator Restore(WorldBuildingManager manager, WorldBuildingState state, Action<BuildingRuntime> complete)
    {
        return (IEnumerator)typeof(WorldBuildingManager).GetMethod("RestoreBuildingRoutine", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(manager, new object[] { state, complete });
    }

    private static BlockDataList CreateDesign()
    {
        var design = new BlockDataList();
        for (int i = 0; i < 2100; i++)
        {
            BlockData block = JsonUtility.FromJson<BlockData>("{}");
            block.id = "module-" + i;
            block.resourcePath = "Blocks/2x2x1";
            block.materialId = "stone";
            block.x = 2;
            block.y = 2;
            block.z = 1;
            block.posX = (i % 50) * 2;
            block.posY = 1;
            block.posZ = i / 50 + .5f;
            block.rotW = 1;
            design.blocks.Add(block);
        }

        design.foundation = BuildingFootprint.Calculate(design.blocks, 1);
        return design;
    }

    private static WorldBuildingState State(BlockDataList design, string id, int slot)
    {
        return new WorldBuildingState
        {
            instanceId = "capacity-" + id,
            blueprintName = "Owned capacity fixture",
            faction = UnitFaction.Player,
            position = new Vector3(12000 + slot * 150, 3000, 12000),
            rotation = Quaternion.identity,
            blueprint = design,
            durability = Enumerable.Repeat(float.MaxValue, design.blocks.Count).ToArray(),
            anchored = true
        };
    }

    private static void Require(bool value, string name)
    {
        if (!value)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
        Save();
    }

    private static void Save()
    {
        Directory.CreateDirectory("Temp/BlueprintAuditChecks");
        File.WriteAllText("Temp/BlueprintAuditChecks/capacity-play.json", JsonUtility.ToJson(report, true));
    }
}

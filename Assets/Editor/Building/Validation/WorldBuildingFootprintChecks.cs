// 职责：验证自动占地偏移的世界地形平台、虚拟锚点和建筑保存/修复契约，不改写玩家蓝图。
// 入口：RunEdit() 检查确定性地形；RunRuntime() 在Play内检查真实建筑支撑和快照。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WorldBuildingFootprintChecks
{
    public static string RunEdit()
    {
        var checks = new List<string>();
        BlockDataList original = Sample(0, 0);
        BlockDataList shifted = Sample(-40, 27);
        WorldBuildingValidation.ValidateBlueprint(shifted);
        WorldBuildingValidation.ValidateWorldTemplate(shifted);
        Check(
            checks,
            "old zero-offset ground anchor remains unchanged",
            WorldBuildingPlacement.LocalAnchor(original.foundation, 0, 0) == WorldBuildingPlacement.LocalAnchor(0, 0));
        Check(
            checks,
            "cropped negative/positive origin adjusts local ground anchors",
            WorldBuildingPlacement.LocalAnchor(shifted.foundation, 0, 0) == new Vector3(-41.5f, 0f, 25.5f));
        BlockDataList restored = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(shifted));
        Check(
            checks,
            "JSON roundtrip preserves offset without moving modules",
            restored.foundation.offsetX == -40 && restored.foundation.offsetZ == 27 && restored.blocks[0].posX == shifted.blocks[0].posX);
        var footprints = new WorldBuildingFootprints(new[] { shifted });
        Vector3 footprintCenter = footprints.Get(14723, Vector2Int.zero).center;
        Check(
            checks,
            "world footprint carries cropped centre",
            Mathf.Abs(footprintCenter.x - shifted.foundation.Center.x) < .001f && Mathf.Abs(footprintCenter.z - shifted.foundation.Center.z) < .001f);
        WorldGenerationSettings settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>("Assets/World/WorldGeneration.asset"));
        try
        {
            JsonUtility.FromJsonOverwrite("{\"buildingChance\":1}", settings);
            var before = new StreamingWorldTerrain(settings, 14723, new[] { original });
            var after = new StreamingWorldTerrain(settings, 14723, new[] { shifted });
            var rotations = new HashSet<int>();
            int sites = 0;
            for (int z = -8; z <= 8; z++)
            {
                for (int x = -8; x <= 8; x++)
                {
                    var column = new Vector2Int(x, z);
                    if (!before.TryGetBuildingSite(column, out Vector3 a, out Quaternion rotation))
                    {
                        continue;
                    }

                    CheckCondition(
                        after.TryGetBuildingSite(column, out Vector3 b, out Quaternion shiftedRotation),
                        "offset template retains candidate site");
                    CheckCondition(Quaternion.Angle(rotation, shiftedRotation) < .01f, "offset template retains seeded yaw");
                    Vector3 expected = a - rotation * new Vector3(-40, 0, 27);
                    CheckCondition(
                        (b - expected).sqrMagnitude < .0001f,
                        "world site compensates offset to retain actual building position");
                    for (int iz = 0; iz < shifted.foundation.length; iz++)
                    {
                        for (int ix = 0; ix < shifted.foundation.width; ix++)
                        {
                            Vector3 point = b + rotation * WorldBuildingPlacement.LocalAnchor(shifted.foundation, ix, iz);
                            float ground = after.SurfaceHeight(Mathf.FloorToInt(point.x / settings.CellSize), Mathf.FloorToInt(point.z / settings.CellSize));
                            CheckCondition(Mathf.Abs(ground - b.y) < .01f, "shifted footprint remains fully supported on generated terrain");
                        }
                    }

                    rotations.Add(Mathf.RoundToInt(rotation.eulerAngles.y / 90f));
                    sites++;
                }
            }

            Check(
                checks,
                "offset templates preserve real sites, four rotations and full terrain support: " + sites,
                sites > 0 && rotations.Count == 4);
        }
        finally
        {
            Object.DestroyImmediate(settings);
        }

        return Save("edit", checks);
    }

    public static string RunRuntime()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("RunRuntime requires Play Mode.");
        }

        var checks = new List<string>();
        var root = new GameObject("Automatic footprint isolated runtime checks");
        try
        {
            WorldBuildingManager manager = root.AddComponent<WorldBuildingManager>();
            manager.enabled = false;
            BlockDataList design = Sample(-40, 27);
            var state = new WorldBuildingState
            {
                instanceId = "auto-footprint-check",
                blueprintName = "isolated offset footprint",
                faction = UnitFaction.Player,
                position = new Vector3(2500, 200, 2500),
                rotation = Quaternion.Euler(0f, 90f, 0f),
                blueprint = design,
                durability = new[]
                {
                    100f,
                    100f
                }
            };
            BuildingRuntime building = manager.RestoreBuilding(state);
            building.Activate();
            Check(
                checks,
                "restored runtime retains cropped origin",
                building.Footprint.offsetX == -40 && building.Footprint.offsetZ == 27);
            Check(
                checks,
                "disconnected groups each anchor directly to offset ground",
                building.HasFullFoundationSupport());
            Bounds actual = WorldBuildingPlacement.Footprint(building);
            Vector3 expected = building.transform.TransformPoint(design.foundation.Center);
            Check(
                checks,
                "terrain-damage footprint follows rotated cropped origin",
                (actual.center - expected).sqrMagnitude < .0001f);
            WorldBuildingState captured = building.Capture();
            Check(
                checks,
                "live capture retains offset and original relative poses",
                captured.blueprint.foundation.offsetX == -40 && captured.blueprint.foundation.offsetZ == 27 && captured.blueprint.blocks[0].posX == design.blocks[0].posX);
            BlockDataList changed = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(design));
            changed.foundation.offsetX++;
            var arguments = new object[]
            {
                changed,
                null
            };
            bool repaired = (bool)typeof(BuildingRuntime).GetMethod("TryRefit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(building, arguments);
            Check(checks, "repair rejects moving an existing footprint origin", !repaired);
            arguments = new object[]
            {
                design,
                null
            };
            repaired = (bool)typeof(BuildingRuntime).GetMethod("TryRefit", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(building, arguments);
            Check(
                checks,
                "repair accepts the unchanged cropped design",
                repaired && building.HasFullFoundationSupport());
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        return Save("runtime", checks);
    }

    private static BlockDataList Sample(int x, int z)
    {
        Block prefab = Resources.Load<GameObject>("Blocks/1x1x1").GetComponent<Block>();
        var design = new BlockDataList
        {
            foundation = new BuildingFoundationData
            {
                width = 4,
                length = 4,
                offsetX = x,
                offsetZ = z
            }
        };
        for (int i = 0; i < 2; i++)
        {
            design.blocks.Add(new BlockData(prefab) { id = "grounded-" + i, resourcePath = "Assets/Resources/Blocks/1x1x1.prefab", x = 1, y = 1, z = 1, posX = x - 1.5f + i * 2, posY = .5f, posZ = z - 1.5f + i * 2, rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f });
        }

        return design;
    }

    private static void Check(List<string> checks, string label, bool condition)
    {
        CheckCondition(condition, label);
        checks.Add("PASS " + label);
    }

    private static void CheckCondition(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
    }

    private static string Save(string phase, List<string> checks)
    {
        Directory.CreateDirectory("Temp/FreeBuilding");
        string report = string.Join("\n", checks);
        File.WriteAllText("Temp/FreeBuilding/world-footprint-" + phase + ".txt", report);
        return report;
    }
}

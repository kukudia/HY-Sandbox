// 职责：检查官方基地的实际目录、材质、无重叠、地面支撑、电网及机器人依赖。
// 入口：RunData() / RunPlay()；Play只创建远端临时实例，不读写个人蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeBaseExamplesChecks
{
    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public List<string> checks = new List<string>();
        public string error;
    }

    public static string RunData()
    {
        var report = new Report();
        try
        {
            SiegeConstructionSettings settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>("Assets/Siege/Construction/Settings.asset");
            var serialized = new SerializedObject(settings);
            SerializedProperty examples = serialized.FindProperty("examples");
            Check(
                report,
                examples != null && examples.arraySize == SiegeBaseExamplesAuthoring.Paths.Length,
                "Settings contains the official base examples");
            for (int index = 0; index < SiegeBaseExamplesAuthoring.Paths.Length; index++)
            {
                TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(SiegeBaseExamplesAuthoring.Paths[index]);
                Check(report, asset != null, "Official asset exists: " + SiegeBaseExamplesAuthoring.Names[index]);
                BlockDataList design = JsonUtility.FromJson<BlockDataList>(asset.text);
                Check(
                    report,
                    settings.Validate(design, out string reason),
                    "Production design validation: " + index + " / " + reason);
                Check(
                    report,
                    design.blocks.Select(block => block.materialId).Where(id => !string.IsNullOrEmpty(id)).Distinct().Count() == 4,
                    "All four structural materials used: " + index);
                Check(
                    report,
                    new[] { "Blocks/CargoHold", "Blocks/CoinHold", "Blocks/TechnologyHold", "Blocks/RepairBotContianer", "Blocks/CollectionBotContainer" }.All(resource => design.blocks.Any(block => block.resourcePath == resource)),
                    "Real hold and both bot modules used: " + index);
                int expectedHangars = index == 3 ? 4 : index == 2 ? 2 : 1;
                Check(
                    report,
                    design.blocks.Count(block => block.resourcePath == "Hangars/Hangar") == expectedHangars,
                    "Distinct intended hangar layout: " + index);
                Check(report, NoOverlaps(design), "Logical modules do not overlap: " + index);
                SerializedProperty item = examples.GetArrayElementAtIndex(index);
                Check(
                    report,
                    item.FindPropertyRelative("blueprint").objectReferenceValue == asset && item.FindPropertyRelative("name").stringValue == SiegeBaseExamplesAuthoring.Names[index],
                    "Catalog references correct named asset: " + index);
                Check(
                    report,
                    design.blocks.All(block => block.cargo == null || block.cargo.Count == 0),
                    "Examples grant no free cargo: " + index);
            }

            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
        }

        return Save(report, "data");
    }

    public static string RunPlay()
    {
        if (!EditorApplication.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Requires Main Play Mode with authored services.");
        }

        var report = new Report();
        GameObject owner = null;
        try
        {
            for (int index = 0; index < SiegeBaseExamplesAuthoring.Paths.Length; index++)
            {
                owner = new GameObject("Isolated official example " + index);
                owner.SetActive(false);
                BlueprintStructureRegistry manager = owner.AddComponent<BlueprintStructureRegistry>();
                var config = new SerializedObject(manager);
                config.FindProperty("useIndividualRenderers").boolValue = false;
                config.ApplyModifiedPropertiesWithoutUndo();
                BlockDataList design = JsonUtility.FromJson<BlockDataList>(AssetDatabase.LoadAssetAtPath<TextAsset>(SiegeBaseExamplesAuthoring.Paths[index]).text);
                BlueprintStructureRuntime building = manager.RestoreBuilding(new BlueprintStructureState { instanceId = "example-validation-" + index, blueprintName = SiegeBaseExamplesAuthoring.Names[index], faction = UnitFaction.Player, position = new Vector3(18000, 3000, 18000), rotation = Quaternion.identity, blueprint = design, durability = Enumerable.Repeat(float.MaxValue, design.blocks.Count).ToArray(), anchored = true });
                owner.SetActive(true);
                building.Activate();
                Physics.SyncTransforms();
                Check(
                    report,
                    building.Blocks.Count == HangarShellBlueprint.Expand(design).blocks.Count,
                    "All expanded blocks restored: " + index);
                Check(
                    report,
                    building.HasFullFoundationSupport(),
                    "Every connected building group has real downward foundation support: " + index);
                Check(
                    report,
                    building.GetComponentsInChildren<RepairBot>(true).Length >= 2 && building.GetComponentsInChildren<CollectionBot>(true).Length >= 2,
                    "Both real authored bot types present: " + index);
                Check(
                    report,
                    building.GetComponentsInChildren<CargoHold>(true).Length >= 6,
                    "Three hold types on both service pads: " + index);
                Power[] loads = building.GetComponentsInChildren<Power>();
                foreach (Power power in loads)
                {
                    power.SetWorkRequested(owner, true);
                }

                new PowerNetwork().Rebuild(
                    building.GetComponentsInChildren<PowerTransmissionDevice>(),
                    building.GetComponentsInChildren<PowerGeneratingUnit>(),
                    loads);
                Check(
                    report,
                    loads.Length > 0 && loads.All(load => load.isWorking && load.efficiency >= .99f),
                    "Every requested functional load receives real full-rate power: " + index + " / underpowered: " + string.Join(
                        ", ",
                        loads.Where(load => !load.isWorking || load.efficiency < .99f).Select(load => load.GetComponent<Block>()?.uniqueId ?? load.name)));
                foreach (Power power in loads)
                {
                    power.SetWorkRequested(owner, false);
                }

                Check(
                    report,
                    building.Blocks.Where(block => block.GetComponent<BlockMaterial>() != null).Select(block => block.GetComponent<BlockMaterial>().MaterialId).Where(id => !string.IsNullOrEmpty(id)).Distinct().Count() == 4,
                    "Real restored surfaces retain all material identities: " + index);
                owner.SetActive(false);
                Object.DestroyImmediate(owner);
                owner = null;
            }

            report.passed = true;
        }
        catch (Exception error)
        {
            report.error = error.ToString();
        }
        finally
        {
            if (owner != null)
            {
                owner.SetActive(false);
                Object.DestroyImmediate(owner);
            }
        }

        return Save(report, "play");
    }

    private static bool NoOverlaps(BlockDataList design)
    {
        for (int first = 0; first < design.blocks.Count; first++)
        {
            BlockData a = design.blocks[first];
            // A logical hangar reserves an empty interior; service modules are intentionally outside it.
            Bounds left = new Bounds(new Vector3(a.posX, a.posY, a.posZ), new Vector3(a.x, a.y, a.z) - Vector3.one * .01f);
            for (int second = first + 1; second < design.blocks.Count; second++)
            {
                BlockData b = design.blocks[second];
                Bounds right = new Bounds(new Vector3(b.posX, b.posY, b.posZ), new Vector3(b.x, b.y, b.z) - Vector3.one * .01f);
                if (left.Intersects(right))
                {
                    throw new InvalidOperationException("Example overlap: " + a.id + " / " + b.id);
                }
            }
        }

        return true;
    }

    private static void Check(Report report, bool passed, string name)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
    }

    private static string Save(Report report, string label)
    {
        Directory.CreateDirectory("Temp/SiegeBaseExamples");
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Temp/SiegeBaseExamples/" + label + ".json", json);
        return json;
    }
}

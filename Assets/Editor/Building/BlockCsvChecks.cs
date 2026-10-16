// 职责：隔离验证实际 CSV→Prefab 同步、幂等/错误拒绝及真实 Awake/OnEnable 顺序。
// 入口：RunEditMode() / RunPlayMode()；Edit Mode 最终恢复 CSV 与 Prefab 原始字节，不保存场景。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlockCsvChecks
{
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
    }

    public static string RunEditMode()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires Edit Mode.");
        }

        var report = new Report();
        string csvPath = BlockCsvDocument.AssetPath;
        byte[] originalCsv = File.ReadAllBytes(csvPath);
        var originals = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Blocks" }).Select(AssetDatabase.GUIDToAssetPath).ToDictionary(path => path, File.ReadAllBytes);
        byte[] scene = File.ReadAllBytes("Assets/Scenes/Main.unity");
        bool dirty = UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty;
        try
        {
            var records = BlockCsvDocument.ParseRecords(Encoding.UTF8.GetString(originalCsv).TrimStart('\uFEFF'));
            BlockCsvDocument.Parse(BlockCsvDocument.WriteRecords(records));
            Check(
                report,
                records.Count == 26 && records[0].Count == 83,
                "CSV has 25 Block rows, 79 parameter columns and 4 identity columns");
            Set(records, "MainThruster", "Block.mass", "123.5");
            Set(records, "MainThruster", "Block.cost", "789");
            Set(records, "MainThruster", "Durability.maxDurability", "321");
            Set(records, "MainThruster", "Thruster.maxThrust", "456");
            Set(records, "MainThruster", "Durability.damageMultiplier", "0.123");
            Set(records, "CargoHold", "CargoHold._capacity", "37");
            Set(records, "CargoHold", "CargoHold._kind", "Coins");
            Set(records, "CargoHold", "requiresPower", "false");
            Set(records, "CollectionBotContainer", "requiresPower", "false");
            Set(records, "RepairBotContianer", "requiresPower", "false");
            Set(records, "RepairBotContianer", "RepairBot.repairAmount", "17");
            Set(records, "Turret", "Turret.targetFaction", "Neutral");
            Set(records, "1x1x1", "Block.canEditInfo", "true");
            string description = "中文, \"说明\"\n第二行";
            Set(records, "1x1x1", "Block.info", description);
            Write(records);
            BlockCsvAuthoring.Bake();
            var thruster = Load("MainThruster");
            Check(
                report,
                thruster.GetComponent<Block>().mass == 123.5f && thruster.GetComponent<Block>().UseCustomMass && thruster.GetComponent<Block>().cost == 789,
                "Direct CSV edit persists manual mass and cost");
            Check(
                report,
                thruster.GetComponent<Durability>().maxDurability == 321f && thruster.GetComponent<MainThruster>().maxThrust == 456f,
                "Multiple components and inherited fields persisted");
            using (var data = new SerializedObject(thruster.GetComponent<Durability>()))
            {
                Check(
                    report,
                    Mathf.Approximately(data.FindProperty("damageMultiplier").floatValue, 0.123f),
                    "Private serialized damage field persisted");
            }

            Check(
                report,
                Load("CargoHold").GetComponent<CargoHold>().Capacity == 37 && Load("CargoHold").GetComponent<CargoHold>().Kind == CargoKind.Coins,
                "Private cargo fields and enum name persisted");
            foreach (Component component in new Component[]
            {
                Load("CargoHold").GetComponent<CargoHold>(),
                Load("CollectionBotContainer").GetComponentInChildren<CollectionBot>(true),
                Load("RepairBotContianer").GetComponentInChildren<RepairBot>(true)
            }

            )
            {
                using var serialized = new SerializedObject(component);
                Check(
                    report,
                    !serialized.FindProperty("_requiresPower").boolValue,
                    "Shared requiresPower column applied to " + component.GetType().Name);
            }

            GameObject combined = PrefabUtility.LoadPrefabContents("Assets/Resources/Blocks/CargoHold.prefab");
            try
            {
                combined.AddComponent<CollectionBot>();
                BlockCsvDocument.Parse(BlockCsvDocument.WriteRecords(records)).Apply(combined, AssetDatabase.AssetPathToGUID("Assets/Resources/Blocks/CargoHold.prefab"));
                using var cargoData = new SerializedObject(combined.GetComponent<CargoHold>());
                using var botData = new SerializedObject(combined.GetComponent<CollectionBot>());
                Check(
                    report,
                    !cargoData.FindProperty("_requiresPower").boolValue && !botData.FindProperty("_requiresPower").boolValue,
                    "Shared requiresPower applies to both matching components on one Prefab");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(combined);
            }

            GameObject repairBay = PrefabUtility.LoadPrefabContents("Assets/Resources/Blocks/RepairBotContianer.prefab");
            try
            {
                Object.DestroyImmediate(repairBay.GetComponent<Power>());
                var repairBot = repairBay.GetComponentInChildren<RepairBot>(true);
                JsonUtility.FromJsonOverwrite("{\"_requiresPower\":true}", repairBot);
                bool missingPowerRejected = false;
                try
                {
                    BlockCsvAuthoring.ValidatePowerDependencies(repairBay);
                }
                catch (InvalidDataException)
                {
                    missingPowerRejected = true;
                }

                Check(report, missingPowerRejected, "Required power rejects a repair bay without Power");
                JsonUtility.FromJsonOverwrite("{\"_requiresPower\":false}", repairBot);
                BlockCsvAuthoring.ValidatePowerDependencies(repairBay);
                Check(report, true, "Optional power permits a repair bay without Power");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(repairBay);
            }

            Check(
                report,
                Load("RepairBotContianer").GetComponentInChildren<RepairBot>(true).repairAmount == 17f,
                "Nested bot configuration persisted");
            Check(
                report,
                Load("Turret").GetComponent<LaserTurret>().targetFaction == UnitFaction.Neutral,
                "Inherited turret enum persisted");
            Check(
                report,
                Load("1x1x1").GetComponent<Block>().info == description,
                "Chinese, comma, quotes and multiline description roundtrip");
            var saved = originals.Keys.ToDictionary(path => path, File.ReadAllBytes);
            BlockCsvAuthoring.Bake();
            Check(
                report,
                saved.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)),
                "Repeat synchronization produces zero file changes");
            Set(records, "MainThruster", "Block.mass", "");
            Set(records, "MainThruster", "Block.x", "3");
            Set(records, "MainThruster", "Block.y", "2");
            Set(records, "MainThruster", "Block.z", "4");
            Write(records);
            BlockCsvAuthoring.Bake();
            var block = Load("MainThruster").GetComponent<Block>();
            Check(
                report,
                block.mass == 24f && !block.UseCustomMass && block.raycastCollider.size == new Vector3(2.9f, 1.9f, 3.9f),
                "Blank mass recomputes volume and collider from changed dimensions");
            saved = originals.Keys.ToDictionary(path => path, File.ReadAllBytes);
            Set(records, "MainThruster", "Block.mass", "-1");
            Write(records);
            bool rejected = false;
            try
            {
                BlockCsvAuthoring.Bake();
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check(
                report,
                rejected && saved.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)),
                "Invalid CSV rejects whole batch without writing any Prefab");
            Check(
                report,
                scene.SequenceEqual(File.ReadAllBytes("Assets/Scenes/Main.unity")) && UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty == dirty,
                "Scene bytes and Editor scene dirty state unchanged");
            report.status = "passed";
        }
        finally
        {
            File.WriteAllBytes(csvPath, originalCsv);
            foreach (var pair in originals)
            {
                File.WriteAllBytes(pair.Key, pair.Value);
                AssetDatabase.ImportAsset(pair.Key, ImportAssetOptions.ForceSynchronousImport);
            }

            AssetDatabase.ImportAsset(csvPath, ImportAssetOptions.ForceSynchronousImport);
            BlockCsvBinding.ResetCache();
        }

        return SaveReport(report, "edit-mode");
    }

    public static string RunPlayMode()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode.");
        }

        var report = new Report();
        var parent = new GameObject("CSV isolated Awake probe");
        parent.SetActive(false);
        parent.transform.position = Vector3.one * 5000f;
        try
        {
            var blockObject = Object.Instantiate(Load("1x1x1"), parent.transform);
            var block = blockObject.GetComponent<Block>();
            var durability = blockObject.GetComponent<Durability>();
            block.cost = -999;
            block.mass = -99f;
            durability.maxDurability = -123f;
            var cargoObject = Object.Instantiate(Load("CargoHold"), parent.transform);
            JsonUtility.FromJsonOverwrite("{\"_capacity\":999}", cargoObject.GetComponent<CargoHold>());
            parent.SetActive(true);
            var source = Load("1x1x1").GetComponent<Block>();
            Check(
                report,
                block.cost == source.cost && block.mass == source.mass,
                "Real binding Awake overrides corrupt instance values from Resources CSV");
            Check(
                report,
                durability.maxDurability == Load("1x1x1").GetComponent<Durability>().maxDurability && durability.currentDurability == durability.maxDurability,
                "CSV max durability applied before Durability.OnEnable initializes current health");
            Check(
                report,
                cargoObject.GetComponent<CargoHold>().Capacity == Load("CargoHold").GetComponent<CargoHold>().Capacity,
                "Runtime Awake applies private serialized cargo field");
            Check(
                report,
                blockObject.GetComponent<BlockCsvBinding>().PrefabGuid == AssetDatabase.AssetPathToGUID("Assets/Resources/Blocks/1x1x1.prefab"),
                "Prefab GUID binding preserved on runtime instance");
            report.status = "passed";
        }
        finally
        {
            parent.SetActive(false);
            Object.Destroy(parent);
        }

        return SaveReport(report, "play-mode");
    }

    private static GameObject Load(string name)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Blocks/" + name + ".prefab");
    }

    private static void Set(List<List<string>> records, string name, string field, string value)
    {
        int column = records[0].FindIndex(header => header.StartsWith(field + " | ", StringComparison.Ordinal));
        records.Single(record => record[0] == name)[column] = value;
    }

    private static void Write(List<List<string>> records)
    {
        File.WriteAllText(BlockCsvDocument.AssetPath, BlockCsvDocument.WriteRecords(records), new UTF8Encoding(true));
        AssetDatabase.ImportAsset(BlockCsvDocument.AssetPath, ImportAssetOptions.ForceSynchronousImport);
    }

    private static void Check(Report report, bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException("FAILED: " + description);
        }

        report.checks.Add(description);
    }

    private static string SaveReport(Report report, string name)
    {
        string json = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Temp/BlockCsvChecks");
        File.WriteAllText("Temp/BlockCsvChecks/" + name + ".json", json, new UTF8Encoding(false));
        return json;
    }
}

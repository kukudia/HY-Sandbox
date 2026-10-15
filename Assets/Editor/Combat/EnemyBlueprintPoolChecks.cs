// 职责：隔离临时蓝图库，验证真实敌机池初始化遇损坏文件仍保留有效蓝图。
// 入口：Run()；同步检查后恢复SaveManager私有文件存储和名称列表，不写用户目录。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EnemyBlueprintPoolChecks
{
    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static string Run()
    {
        if (!EditorApplication.isPlaying || SaveManager.instance == null || BuildManager.instance == null)
        {
            throw new InvalidOperationException("Requires Main Play Mode.");
        }

        var report = new Report();
        SaveManager saves = SaveManager.instance;
        FieldInfo filesField = typeof(SaveManager).GetField("_enemyFiles", Private);
        object previousFiles = filesField.GetValue(saves);
        List<string> previousNames = saves.enemyBlueprints;
        string directory = Path.GetFullPath(Path.Combine("Temp", "EnemyBlueprintPool", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        GameObject root = null;
        try
        {
            string valid = ValidBlueprint();
            var sources = new Dictionary<string, string>
            {
                {
                    "A-valid",
                    valid
                },
                {
                    "B-truncated",
                    "{\"blocks\":["
                },
                {
                    "C-locked",
                    valid
                },
                {
                    "D-empty",
                    "{}"
                },
                {
                    "E-null",
                    "{\"blocks\":[null]}"
                },
                {
                    "Z-valid",
                    valid
                }
            };
            foreach (var source in sources)
            {
                File.WriteAllText(Path.Combine(directory, source.Key + ".json"), source.Value);
            }

            Type storeType = typeof(SaveManager).Assembly.GetType("BlueprintFileStore", true);
            filesField.SetValue(saves, Activator.CreateInstance(storeType, new object[] { directory }));
            saves.enemyBlueprints = new List<string>();
            root = new GameObject("Isolated enemy blueprint pool checks");
            EnemySpawner spawner = root.AddComponent<EnemySpawner>();
            spawner.enabled = false;
            MethodInfo initialize = typeof(EnemySpawner).GetMethod("InitializeEnemyBlueprintPool", Private);
            using (var locked = new FileStream(Path.Combine(directory, "C-locked.json"), FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                initialize.Invoke(spawner, null);
            }

            Check(report, "malformed JSON and locked file do not abort initialization", true);
            IList pool = (IList)typeof(EnemySpawner).GetField("enemyBlueprintPool", Private).GetValue(spawner);
            var names = new List<string>();
            foreach (object item in pool)
            {
                names.Add((string)item.GetType().GetField("name").GetValue(item));
            }

            Check(
                report,
                "both valid siblings survive corrupt and unreadable entries",
                names.OrderBy(n => n).SequenceEqual(new[] { "A-valid", "Z-valid" }));
            Check(
                report,
                "pool reaches initialized state after skipped entries",
                (bool)typeof(EnemySpawner).GetField("isBlueprintPoolInitialized", Private).GetValue(spawner));
            initialize.Invoke(spawner, null);
            Check(report, "repeated initialization does not duplicate valid enemies", pool.Count == 2);
            Check(
                report,
                "invalid files are preserved byte-for-byte for manual repair",
                sources.All(source => File.ReadAllText(Path.Combine(directory, source.Key + ".json")) == source.Value));
        }
        catch (Exception exception)
        {
            report.errors.Add((exception is TargetInvocationException && exception.InnerException != null ? exception.InnerException : exception).ToString());
        }
        finally
        {
            filesField.SetValue(saves, previousFiles);
            saves.enemyBlueprints = previousNames;
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        Check(
            report,
            "original SaveManager storage and list restored",
            ReferenceEquals(filesField.GetValue(saves), previousFiles) && ReferenceEquals(saves.enemyBlueprints, previousNames));
        string json = JsonUtility.ToJson(report, true);
        File.WriteAllText("Temp/EnemyBlueprintPool/result.json", json);
        return json;
    }

    private static string ValidBlueprint()
    {
        Block block = Resources.Load<GameObject>("Blocks/Cockpit").GetComponent<Block>();
        var data = new BlockDataList();
        data.blocks.Add(new BlockData(block) { resourcePath = "Blocks/Cockpit", rotW = 1f });
        return JsonUtility.ToJson(data);
    }

    private static void Check(Report report, string label, bool passed)
    {
        (passed ? report.checks : report.errors).Add(label);
    }
}

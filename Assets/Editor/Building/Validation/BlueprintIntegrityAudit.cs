// 职责：审查官方/个人蓝图完整性，将确认损坏的个人文件移入可恢复隔离区，保留哈希和原路径。
// 入口：AuditAll() / QuarantineInvalidPersonal()；不把未完成设计当损坏，不自动移动被资产引用的官方文件。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BlueprintIntegrityAudit
{
    [Serializable]
    public sealed class Entry
    {
        public string path;
        public string category;
        public string sha256;
        public string status;
        public string reason;
        public int modules;
        public string quarantine;
    }

    [Serializable]
    public sealed class Report
    {
        public string createdUtc;
        public string persistentRoot;
        public List<Entry> entries = new List<Entry>();
    }

    [MenuItem("HY-Sandbox/Blueprints/Audit Official and Personal Blueprints")]
    public static string AuditAll()
    {
        Report report = InspectAll();
        string directory = NewReportDirectory();
        string path = Path.Combine(directory, "audit.json");
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    [MenuItem("HY-Sandbox/Blueprints/Quarantine Invalid Personal Blueprints")]
    public static string QuarantineInvalidPersonal()
    {
        if (Application.isPlaying)
        {
            throw new InvalidOperationException("Leave Play Mode before moving personal blueprint files.");
        }

        Report report = InspectAll();
        string directory = NewReportDirectory();
        string manifest = Path.Combine(directory, "manifest.json");
        // Write intent before touching source files. Each move has an independently verified copy.
        File.WriteAllText(manifest, JsonUtility.ToJson(report, true));
        foreach (Entry entry in report.entries.Where(item => item.status == "invalid" && !item.category.StartsWith("official", StringComparison.Ordinal)))
        {
            string source = Path.GetFullPath(entry.path);
            string expectedDirectory = Path.GetFullPath(Path.Combine(Application.persistentDataPath, entry.category));
            if (!string.Equals(Path.GetDirectoryName(source), expectedDirectory, StringComparison.OrdinalIgnoreCase) || Hash(source) != entry.sha256)
            {
                throw new IOException("Blueprint changed after audit or escaped its declared directory: " + source);
            }

            string targetDirectory = Path.Combine(directory, "quarantine", entry.category);
            Directory.CreateDirectory(targetDirectory);
            string target = Path.Combine(targetDirectory, Path.GetFileName(source));
            string backup = Path.Combine(directory, "backup", entry.category, Path.GetFileName(source));
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            File.Copy(source, backup, false);
            if (Hash(backup) != entry.sha256)
            {
                throw new IOException("Blueprint backup did not match the source: " + source);
            }

            entry.quarantine = target;
            File.WriteAllText(manifest, JsonUtility.ToJson(report, true));
            File.Move(source, target);
            entry.status = "quarantined";
            File.WriteAllText(manifest, JsonUtility.ToJson(report, true));
        }

        return manifest;
    }

    public static Report InspectAll()
    {
        var report = new Report
        {
            createdUtc = DateTime.UtcNow.ToString("O"),
            persistentRoot = Application.persistentDataPath
        };
        foreach (string category in new[]
        {
            "Saves",
            "EnemyBlueprints",
            "BuildingBlueprints",
            "BaseBlueprints"
        }

        )
        {
            string directory = Path.Combine(Application.persistentDataPath, category);
            if (Directory.Exists(directory))
            {
                foreach (string path in Directory.GetFiles(directory, "*.json", SearchOption.TopDirectoryOnly).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                {
                    report.entries.Add(Inspect(path, category));
                }
            }
        }

        foreach (string directory in new[]
        {
            "Assets/Siege",
            "Assets/Resources/WorldBlueprints",
        }

        )
        {
            if (Directory.Exists(directory))
            {
                foreach (string path in Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
                {
                    report.entries.Add(Inspect(path, "official/" + directory));
                }
            }
        }

        return report;
    }

    public static Entry Inspect(string path, string category)
    {
        var entry = new Entry
        {
            path = Path.GetFullPath(path),
            category = category,
            sha256 = Hash(path),
            status = "valid"
        };
        try
        {
            string json = File.ReadAllText(path);
            if (!(JToken.Parse(json) is JObject document) || !(document["blocks"] is JArray))
            {
                throw new InvalidDataException("Blueprint must contain a blocks array.");
            }

            BlockDataList data = JsonUtility.FromJson<BlockDataList>(json);
            ValidateModules(data);
            entry.modules = data.blocks.Count;
            string normalizedPath = entry.path.Replace('\\', '/');
            bool building = category == "BuildingBlueprints" || category == "BaseBlueprints" || (category.StartsWith("official/", StringComparison.Ordinal) && (normalizedPath.IndexOf("/Buildings/", StringComparison.OrdinalIgnoreCase) >= 0 || normalizedPath.IndexOf("/Assets/Siege/", StringComparison.OrdinalIgnoreCase) >= 0));
            if (building && data.blocks.Count > 0)
            {
                data.foundation = BuildingFootprint.Calculate(data.blocks, 1);
                BlueprintStructureValidation.ValidateBlueprint(HangarShellBlueprint.Expand(data));
                if (!HangarRules.Validate(data, out string reason))
                {
                    throw new InvalidDataException(reason);
                }
            }

            if (data.blocks.Count == 0 || (building && category == "BaseBlueprints" && !data.blocks.Exists(block => block.id == data.siegeCoreId)))
            {
                entry.status = "draft";
                entry.reason = "Incomplete but structurally readable design; retained for editing.";
            }
        }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException || error is IOException || error is InvalidDataException || error is Newtonsoft.Json.JsonException)
        {
            entry.status = "invalid";
            entry.reason = error.Message;
        }

        return entry;
    }

    private static void ValidateModules(BlockDataList data)
    {
        if (data == null || data.blocks == null)
        {
            throw new InvalidDataException("Blueprint has no module list.");
        }

        var identities = new HashSet<string>();
        foreach (BlockData block in data.blocks)
        {
            if (block == null || string.IsNullOrWhiteSpace(block.id) || !identities.Add(block.id) || block.x < 1 || block.y < 1 || block.z < 1 || block.x > 64 || block.y > 64 || block.z > 64 || !BlueprintStructureValidation.Finite(new Vector3(block.posX, block.posY, block.posZ)) || !BlueprintStructureValidation.ValidRotation(new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW)))
            {
                throw new InvalidDataException("Invalid or duplicate module identity, dimensions or pose.");
            }

            if (string.IsNullOrWhiteSpace(block.resourcePath))
            {
                throw new InvalidDataException("Missing module resource path.");
            }

            string resource = block.resourcePath.Replace("Assets/Resources/", "");
            if (resource.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                resource = resource.Substring(0, resource.Length - 7);
            }

            GameObject prefab = Resources.Load<GameObject>(resource);
            if (prefab == null || prefab.GetComponent<Block>() == null)
            {
                throw new InvalidDataException("Missing module prefab: " + block.resourcePath);
            }

            BlockMaterial.Validate(block.materialId, prefab);
            if (prefab.GetComponent<HangarModule>() != null && block.hangarEntrance != HangarEntrance.Front && block.hangarEntrance != HangarEntrance.Roof)
            {
                throw new InvalidDataException("Unknown hangar entrance direction.");
            }
        }
    }

    private static string NewReportDirectory()
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            "BlueprintQuarantine",
            DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string Hash(string path)
    {
        using (SHA256 sha = SHA256.Create())
        {
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            }
        }
    }
}

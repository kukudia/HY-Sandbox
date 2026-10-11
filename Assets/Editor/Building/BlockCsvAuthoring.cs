// 职责：初始化 CSV 与 Prefab 绑定，CSV 导入后自动备份/同步真实资产；校验失败不写入。
// 入口：CreateCsv() / Bake() / QueueBake()；只处理 Resources/Blocks，不保存当前场景。
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BlockCsvAuthoring
{
    private const string Folder = "Assets/Resources/Blocks";
    private static readonly ConcurrentQueue<BlockCsvBinding> Validations = new ConcurrentQueue<BlockCsvBinding>();
    private static bool _queued;
    private static bool _baking;
    public static string LastReport
    {
        get; private set;
    }

    static BlockCsvAuthoring()
    {
        BlockCsvBinding.ValidationRequested = RequestValidation;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/HY-Sandbox/CSV/打开方块 CSV")]
    public static void OpenCsv()
    {
        if (!File.Exists(BlockCsvDocument.AssetPath))
        {
            CreateCsv();
        }

        EditorUtility.OpenWithDefaultApp(Path.GetFullPath(BlockCsvDocument.AssetPath));
    }

    [MenuItem("Tools/HY-Sandbox/CSV/从现有 Prefab 创建 CSV（不覆盖）")]
    public static void CreateCsv()
    {
        if (File.Exists(BlockCsvDocument.AssetPath))
        {
            Debug.Log("CSV 已存在，保留当前编辑内容：" + BlockCsvDocument.AssetPath);
            return;
        }

        var columns = BlockDataTableSchema.CreateColumns();
        var records = new List<IEnumerable<string>>
        {
            BlockCsvDocument.Identity.Concat(columns.Select(column => column.Id + " | " + column.Label))
        };
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path, StringComparer.Ordinal))
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root.GetComponent<Block>() == null)
            {
                continue;
            }

            string components = string.Join(
                ", ",
                root.GetComponentsInChildren<MonoBehaviour>(true).Where(component => component != null && !(component is BlockCsvBinding)).Select(component => component.GetType().Name).Distinct().OrderBy(name => name));
            var values = new List<string>
            {
                root.name,
                path,
                AssetDatabase.AssetPathToGUID(path),
                components
            };
            foreach (var column in columns)
            {
                Component[] targets = column.FindTargets(root);
                if (targets.Length == 0)
                {
                    values.Add("");
                    continue;
                }

                if (targets.Length != 1 && column.Id != "requiresPower")
                {
                    throw new InvalidDataException(path + "：无法唯一定位 " + column.ComponentType.Name);
                }

                using var serialized = new SerializedObject(targets[0]);
                SerializedProperty property = serialized.FindProperty(column.Property);
                if (property == null)
                {
                    throw new InvalidDataException(path + "：字段不存在 " + column.Id);
                }

                foreach (Component other in targets.Skip(1))
                {
                    using var otherSerialized = new SerializedObject(other);
                    if (ReadValue(otherSerialized.FindProperty(column.Property)) != ReadValue(property))
                    {
                        throw new InvalidDataException(path + "：共享 requiresPower 初值不一致，不能自动合并。");
                    }
                }

                if ((column.Id == "Block.mass" && targets[0] is Block block && !block.UseCustomMass) || (column.Id == "Block.info" && targets[0] is Block infoBlock && !infoBlock.canEditInfo))
                {
                    values.Add("");
                }
                else
                {
                    values.Add(ReadValue(property));
                }
            }

            records.Add(values);
        }

        string text = BlockCsvDocument.WriteRecords(records);
        BlockCsvDocument.Parse(text);
        Directory.CreateDirectory(Path.GetDirectoryName(BlockCsvDocument.AssetPath));
        File.WriteAllText(BlockCsvDocument.AssetPath, text, new UTF8Encoding(true));
        AssetDatabase.Refresh();
        QueueBake();
    }

    [MenuItem("Tools/HY-Sandbox/CSV/立即校验并同步 Prefab")]
    public static void Bake()
    {
        if (_baking || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            throw new InvalidOperationException("请退出 Play Mode 并等待编译完成。");
        }

        // 在打开任何 Prefab 前校验完整 CSV；无效数字/重复 GUID 不会产生部分同步。
        string text = File.ReadAllText(BlockCsvDocument.AssetPath, new UTF8Encoding(false, true));
        BlockCsvDocument document = BlockCsvDocument.Parse(text);
        string[] paths = document.Guids.Select(guid =>
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path != document.PathFor(guid))
            {
                throw new InvalidDataException("CSV 路径/GUID 不匹配：" + document.PathFor(guid));
            }

            return path;
        }).ToArray();
        var stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && paths.Contains(stage.assetPath))
        {
            throw new InvalidOperationException("请先关闭 Blocks Prefab 编辑模式，再同步 CSV。");
        }

        string backup = "Library/BlockDataTable/Backups/" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N");
        var originals = paths.ToDictionary(path => path, File.ReadAllBytes);
        var hashes = originals.ToDictionary(pair => pair.Key, pair => Hash(pair.Value));
        string csvHash = Hash(File.ReadAllBytes(BlockCsvDocument.AssetPath));
        var written = new Dictionary<string, string>();
        int saved = 0;
        _baking = true;
        try
        {
            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    string before = Snapshot(root);
                    var binding = root.GetComponent<BlockCsvBinding>();
                    if (binding == null)
                    {
                        binding = root.AddComponent<BlockCsvBinding>();
                    }

                    binding.SetPrefabGuid(AssetDatabase.AssetPathToGUID(path));
                    document.Apply(root, binding.PrefabGuid);
                    if (Snapshot(root) == before)
                    {
                        continue;
                    }

                    if (Hash(File.ReadAllBytes(path)) != hashes[path] || Hash(File.ReadAllBytes(BlockCsvDocument.AssetPath)) != csvHash)
                    {
                        throw new IOException("同步期间文件被外部修改，停止写入：" + path);
                    }

                    // 首次实际写入时为全批保存原始字节、GUID 和清单，便于失败恢复与人工审查。
                    if (saved == 0)
                    {
                        SaveBackup(backup, originals, hashes, text);
                    }

                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                    written[path] = Hash(File.ReadAllBytes(path));
                    if (!success)
                    {
                        throw new IOException("保存失败：" + path);
                    }

                    saved++;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            BlockCsvBinding.ResetCache();
            LastReport = $"CSV 校验通过，{document.RowCount} 条记录，同步 {saved} 个 Prefab。" + (saved > 0 ? "备份：" + backup : "没有数值差异。");
            Debug.Log(LastReport);
        }
        catch
        {
            // 只恢复被本批实际写入的文件；不覆盖另一个编辑器/用户的并发变更。
            if (Directory.Exists(backup))
            {
                foreach (var pair in written)
                {
                    string path = pair.Key;
                    string backedUp = Path.Combine(backup, path);
                    if (File.Exists(backedUp) && Hash(File.ReadAllBytes(path)) == pair.Value)
                    {
                        File.WriteAllBytes(path, originals[path]);
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                    }
                }
            }

            throw;
        }
        finally
        {
            _baking = false;
        }
    }

    public static void QueueBake()
    {
        _queued = true;
        BlockCsvBinding.ResetCache();
    }

    private static void RequestValidation(BlockCsvBinding binding)
    {
        if (!_baking)
        {
            Validations.Enqueue(binding);
        }
    }

    private static void Update()
    {
        if (_baking || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        while (Validations.TryDequeue(out var binding))
        {
            if (binding != null && !EditorUtility.IsPersistent(binding))
            {
                binding.Apply();
            }
            else if (binding != null)
            {
                QueueBake();
            }
        }

        if (_queued && File.Exists(BlockCsvDocument.AssetPath))
        {
            _queued = false;
            try
            {
                Bake();
            }
            catch (Exception exception)
            {
                LastReport = "CSV 未同步：" + exception.Message;
                Debug.LogError(LastReport);
            }
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode && File.Exists(BlockCsvDocument.AssetPath))
        {
            QueueBake();
        }
    }

    private static string ReadValue(SerializedProperty property)
    {
        switch (property.propertyType)
        {
            case SerializedPropertyType.Boolean:
                return property.boolValue ? "true" : "false";
            case SerializedPropertyType.Integer:
                return property.intValue.ToString(CultureInfo.InvariantCulture);
            case SerializedPropertyType.Float:
                return property.floatValue.ToString("R", CultureInfo.InvariantCulture);
            case SerializedPropertyType.String:
                return property.stringValue;
            case SerializedPropertyType.Enum:
                return property.enumNames[property.enumValueIndex];
            default:
                throw new InvalidDataException("不支持导出字段类型：" + property.propertyType);
        }
    }

    private static string Snapshot(GameObject root)
    {
        return string.Join(
            "\n",
            root.GetComponentsInChildren<Component>(true).Where(component => component != null).Select(component => component.GetType().Name + ":" + EditorJsonUtility.ToJson(component)));
    }

    private static string Hash(byte[] bytes)
    {
        using var algorithm = SHA256.Create();
        return Convert.ToBase64String(algorithm.ComputeHash(bytes));
    }

    private static void SaveBackup(string backup, Dictionary<string, byte[]> originals, Dictionary<string, string> hashes, string csv)
    {
        Directory.CreateDirectory(backup);
        foreach (var pair in originals)
        {
            string target = Path.Combine(backup, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            File.WriteAllBytes(target, pair.Value);
            File.Copy(pair.Key + ".meta", target + ".meta", false);
        }

        File.WriteAllLines(
            backup + "/manifest.txt",
            hashes.Select(pair => pair.Key + " " + pair.Value),
            new UTF8Encoding(false));
        File.WriteAllText(backup + "/Blocks.csv", csv, new UTF8Encoding(true));
    }
}

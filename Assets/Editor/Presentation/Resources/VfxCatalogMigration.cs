// 职责：将项目 Prefab 中的内嵌效果迁移为集中库编号，保留挂载变换和现有组件身份。
// 入口：Run()；通过 Unity API 保存，重复执行跳过已迁移节点，差异外观保存为独立模板。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

public static class VfxCatalogMigration
{
    private const string Variants = BlockVfxBaker.Root + "/Variants";
    private const string LibraryPath = "Assets/Resources/VFX/BlockVfxLibrary.asset";
    [MenuItem("Tools/HY Sandbox/资源管理/迁移内嵌特效到集中库")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Exit Play Mode before migration.");
        }

        BlockVfxLibrary library = AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>(LibraryPath);
        var templates = new Dictionary<string, VfxEffect>();
        foreach (string name in BlockVfxBaker.Names)
        {
            templates.Add(name, BlockVfxBaker.Load(name).GetComponent<VfxEffect>());
        }

        templates.Add("LootBurst", templates["BuildBurst"]);
        using (var data = new SerializedObject(library))
        {
            SerializedProperty entries = data.FindProperty("_attached");
            for (int i = 0; i < entries.arraySize; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                templates[entry.FindPropertyRelative("id").stringValue] = (VfxEffect)entry.FindPropertyRelative("prefab").objectReferenceValue;
            }
        }

        var signatures = templates.ToDictionary(pair => pair.Key, pair => Signature(pair.Value));
        int changed = 0;
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art", "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (path.StartsWith(BlockVfxBaker.Root + "/", StringComparison.Ordinal))
            {
                continue;
            }

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset.GetComponentsInChildren<VfxEffect>(true).Any(effect => effect.IsTemplate && effect.Graphs.Length > 0))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                VfxEffect[] effects = root.GetComponentsInChildren<VfxEffect>(true);
                // 先解除组合控制器对内部 Graph 的引用，改为保存子挂载点。
                foreach (VfxEffect effect in effects.Where(effect => effect.GetComponentsInChildren<VfxEffect>(true).Length > 1))
                {
                    if (!string.IsNullOrEmpty(effect.EffectId))
                    {
                        continue;
                    }

                    BlockVfxBaker.SetObjects(
                        effect,
                        "_members",
                        effect.GetComponentsInChildren<VfxEffect>(true).Where(child => child != effect).ToArray());
                    BlockVfxBaker.SetObjects(effect, "_graphs", Array.Empty<Object>());
                }

                foreach (VfxEffect effect in effects.Where(effect => effect.IsTemplate && effect.Graphs.Length > 0))
                {
                    string signature = Signature(effect);
                    string semanticName = effect.name == "Coin burst" ? "LootBurst" : effect.name;
                    string key = signatures.ContainsKey(semanticName) && signatures[semanticName] == signature ? semanticName : signatures.FirstOrDefault(pair => pair.Value == signature).Key;
                    if (key == null)
                    {
                        using var sha = SHA256.Create();
                        string hash = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(signature))).Replace("-", "").Substring(0, 12);
                        key = "Authored_" + hash;
                        VfxEffect template = CreateTemplate(effect, key);
                        templates.Add(key, template);
                        signatures.Add(key, signature);
                    }

                    VisualEffect[] graphs = effect.Graphs.ToArray();
                    using (var data = new SerializedObject(effect))
                    {
                        data.FindProperty("_effectId").stringValue = key;
                        data.FindProperty("_graphs").arraySize = 0;
                        data.ApplyModifiedPropertiesWithoutUndo();
                    }

                    foreach (VisualEffect graph in graphs)
                    {
                        Object.DestroyImmediate(graph.gameObject);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        using (var data = new SerializedObject(library))
        {
            var entries = data.FindProperty("_attached");
            var attached = templates.Where(pair => !IsBuiltIn(pair.Key)).OrderBy(pair => pair.Key).ToArray();
            entries.arraySize = attached.Length;
            for (int i = 0; i < attached.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = attached[i].Key;
                entry.FindPropertyRelative("prefab").objectReferenceValue = attached[i].Value;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssetIfDirty(library);
        Debug.Log($"VFX catalog migration: {changed} prefabs; {templates.Count} templates.");
    }

    private static bool IsBuiltIn(string key)
    {
        return new[]
        {
            "BuildBurst",
            "BreakBurst",
            "Explosion",
            "SmokeBurst",
            "RepairPulse",
            "MuzzleFlash",
            "ImpactBurst",
            "DetachedSmoke",
            "EnergyBeam"
        }.Contains(key);
    }

    private static VfxEffect CreateTemplate(VfxEffect source, string key)
    {
        BlockArtDependencies.EnsureFolder(Variants);
        var root = new GameObject(key);
        try
        {
            var effect = root.AddComponent<VfxEffect>();
            var graphs = new List<VisualEffect>();
            foreach (VisualEffect graph in source.Graphs)
            {
                graphs.Add(Object.Instantiate(graph.gameObject, root.transform, false).GetComponent<VisualEffect>());
            }

            BlockVfxBaker.SetObjects(effect, "_graphs", graphs.ToArray());
            using var from = new SerializedObject(source);
            using var to = new SerializedObject(effect);
            to.FindProperty("_releaseAfter").floatValue = from.FindProperty("_releaseAfter").floatValue;
            to.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, Variants + "/" + key + ".prefab").GetComponent<VfxEffect>();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    public static string Signature(VfxEffect effect)
    {
        var graphs = new JArray();
        foreach (VisualEffect graph in effect.Graphs)
        {
            if (graph == null || graph.transform.parent != effect.transform || graph.transform.childCount != 0 || graph.GetComponents<Component>().Any(component => !(component is Transform) && !(component is VisualEffect) && !(component is VFXRenderer)))
            {
                throw new InvalidOperationException("Effect contains a non-leaf graph; cannot safely migrate: " + effect.name);
            }

            graphs.Add(new JObject { ["position"] = graph.transform.localPosition.ToString("R"), ["rotation"] = graph.transform.localRotation.ToString("R"), ["scale"] = graph.transform.localScale.ToString("R"), ["active"] = graph.gameObject.activeSelf, ["graph"] = ComponentData(graph), ["renderer"] = ComponentData(graph.GetComponent<VFXRenderer>()) });
        }

        using var data = new SerializedObject(effect);
        return new JObject
        {
            ["graphs"] = graphs,
            ["release"] = data.FindProperty("_releaseAfter").floatValue
        }.ToString(Newtonsoft.Json.Formatting.None);
    }

    private static JObject ComponentData(Component component)
    {
        JObject json = JObject.Parse(EditorJsonUtility.ToJson(component));
        foreach (string property in new[]
        {
            "m_GameObject",
            "m_ObjectHideFlags",
            "m_CorrespondingSourceObject",
            "m_PrefabInstance",
            "m_PrefabAsset"
        }

        )
        {
            json.Remove(property);
        }

        return json;
    }
}

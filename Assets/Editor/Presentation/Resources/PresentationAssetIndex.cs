// 职责：为特效和音频管理共用资源发现、搜索及引用查询，不持有运行时资源。
// 入口：Scan() / Matches() / FindReferencers()；仅在显式刷新或项目资源变更时扫描。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public static class PresentationAssetIndex
{
    public enum Kind
    {
        Effects,
        Audio
    }

    public sealed class Entry
    {
        public string Guid
        {
            get;
        }
        public string Path => AssetDatabase.GUIDToAssetPath(Guid);
        public string Category
        {
            get;
        }

        public Entry(string guid, string category)
        {
            Guid = guid;
            Category = category;
        }
    }

    public static List<Entry> Scan(Kind kind, bool includeDependencies)
    {
        var entries = new Dictionary<string, Entry>(StringComparer.Ordinal);
        void Add(string guid, string category)
        {
            entries[guid] = new Entry(guid, category);
        }

        string filter = kind == Kind.Audio ? "t:AudioClip" : "t:VisualEffectAsset";
        foreach (string guid in AssetDatabase.FindAssets(filter, new[] { "Assets" }))
        {
            Add(guid, kind == Kind.Audio ? "AudioClip" : "VFX Graph");
        }

        if (kind == Kind.Effects)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null && (prefab.GetComponentInChildren<VfxEffect>(true) != null || prefab.GetComponentInChildren<VisualEffect>(true) != null || prefab.GetComponentInChildren<ParticleSystem>(true) != null || prefab.GetComponentInChildren<TrailRenderer>(true) != null || prefab.GetComponentInChildren<LineRenderer>(true) != null))
                {
                    Add(guid, "Prefab");
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:BlockVfxLibrary", new[] { "Assets" }))
            {
                Add(guid, "播放库");
            }
        }

        if (includeDependencies && entries.Count > 0)
        {
            foreach (string path in AssetDatabase.GetDependencies(entries.Values.Select(entry => entry.Path).ToArray(), true))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (path.StartsWith("Assets/", StringComparison.Ordinal) && !entries.ContainsKey(guid))
                {
                    Add(guid, "依赖");
                }
            }
        }

        return entries.Values.OrderBy(entry => entry.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static bool Matches(Entry entry, string search, string category)
    {
        return (string.IsNullOrEmpty(category) || entry.Category == category) && (string.IsNullOrWhiteSpace(search) || entry.Path.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0);
    }

    public static string[] FindReferencers(string path)
    {
        // 按需检查序列化资产引用；字符串动态加载和运行时生成的引用不在 AssetDatabase 依赖图内。
        return AssetDatabase.GetAllAssetPaths().Where(candidate => candidate.StartsWith("Assets/", StringComparison.Ordinal) && candidate != path && !AssetDatabase.IsValidFolder(candidate)).Where(candidate => AssetDatabase.GetDependencies(candidate, false).Contains(path)).OrderBy(candidate => candidate, StringComparer.OrdinalIgnoreCase).ToArray();
    }
}

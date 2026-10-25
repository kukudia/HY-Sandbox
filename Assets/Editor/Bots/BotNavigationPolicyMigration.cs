// 职责：通过 Editor 重存 Bot Prefab，清理已固化的旧导航字段并核对剩余创作数据。
// 入口：Run() / Tools/Bots/Migrate fixed navigation policy；不保存场景、不重建 GUID。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BotNavigationPolicyMigration
{
    [MenuItem("Tools/Bots/Migrate fixed navigation policy")]
    public static string Run()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before migrating Bot assets.");
        }

        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Blocks" }).Select(AssetDatabase.GUIDToAssetPath).Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<Bot>(true) != null).ToList();
        var snapshots = new Dictionary<string, string>();
        foreach (string path in paths)
        {
            snapshots.Add(path, Snapshot(path));
        }

        foreach (string path in paths)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (Bot bot in contents.GetComponentsInChildren<Bot>(true))
                {
                    EditorUtility.SetDirty(bot);
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        foreach (string path in paths)
        {
            if (snapshots[path] != Snapshot(path))
            {
                throw new InvalidOperationException("Bot mission/socket data changed during migration: " + path);
            }

            using var serialized = new SerializedObject(AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentInChildren<Bot>(true));
            if (serialized.FindProperty("movementSpeed") != null || serialized.FindProperty("_navigationClearanceRadius") != null || File.ReadAllText(path).Contains("  movementSpeed:"))
            {
                throw new InvalidOperationException("Bot still exposes serialized navigation parameters: " + path);
            }
        }

        return "Migrated " + paths.Count + " Bot Prefabs; mission/socket/effect data and GUIDs preserved.";
    }

    private static string Snapshot(string path)
    {
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return AssetDatabase.AssetPathToGUID(path) + "\n" + string.Join("\n", root.GetComponentsInChildren<Bot>(true).Select(EditorJsonUtility.ToJson));
    }
}

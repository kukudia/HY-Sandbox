using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>Closes copied Temp/Block references over Art, retaining existing curated GUIDs.</summary>
public static class BlockArtDependencies
{
    [Serializable] private sealed class Record
    {
        public string source;
        public string destination;
        public string sourceSha256;
        public string sha256;
        public string guid;
    }
    [Serializable] private sealed class Catalog { public Record[] assets; }
    private static bool IsVendor(string path) => path.StartsWith("Assets/PolygonSciFiSpace/") || path.StartsWith("Assets/CUBE - Spaceships Pack 01/");

    [MenuItem("Tools/HY Sandbox/Block Art/Resolve Temp Dependencies")]
    public static void Resolve()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        string[] prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art/Temp", "Assets/Resources/Blocks" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var records = JsonUtility.FromJson<Catalog>(File.ReadAllText("Assets/Art/SpaceKit/Catalog.json")).assets.ToDictionary(r => r.source, r => r.destination);
        string extraCatalog = "Assets/Art/BlockVisuals/Dependencies.json";
        if (File.Exists(extraCatalog))
            foreach (var record in JsonUtility.FromJson<Catalog>(File.ReadAllText(extraCatalog)).assets) records[record.source] = record.destination;
        var sources = AssetDatabase.GetDependencies(prefabs, true).Where(IsVendor).Where(p => !p.EndsWith(".prefab") && !p.EndsWith(".shader")).ToArray();
        if (sources.Length == 0) return;
        var additions = new List<Record>();
        foreach (var source in sources)
        {
            if (records.ContainsKey(source)) continue;
            string destination = "Assets/Art/BlockVisuals/Dependencies/" + source.Substring("Assets/".Length);
            EnsureFolder(Path.GetDirectoryName(destination).Replace('\\', '/'));
            if (!File.Exists(destination) && !AssetDatabase.CopyAsset(source, destination)) throw new IOException(source);
            records[source] = destination;
            additions.Add(new Record { source = source, destination = destination });
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var map = new Dictionary<Object, Object>();
        foreach (var pair in records)
        {
            var copies = AssetDatabase.LoadAllAssetsAtPath(pair.Value).Where(o => o != null).ToArray();
            foreach (var original in AssetDatabase.LoadAllAssetsAtPath(pair.Key).Where(o => o != null))
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string guid, out long id);
                var copy = copies.FirstOrDefault(o =>
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string unused, out long copyId);
                    return copyId == id && o.GetType() == original.GetType();
                });
                if (copy != null) map[original] = copy;
            }
        }
        foreach (var item in additions)
        {
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(item.destination))
                if (asset is Material) { Remap(asset, map); EditorUtility.SetDirty(asset); }
            var importer = AssetImporter.GetAtPath(item.destination);
            if (!(importer is ModelImporter)) continue;
            foreach (var pair in importer.GetExternalObjectMap())
                if (pair.Value != null && map.TryGetValue(pair.Value, out Object replacement)) importer.AddRemap(pair.Key, replacement);
            importer.SaveAndReimport();
        }
        foreach (var path in prefabs)
        {
            // Unpack first: source correspondence is intentionally removed, meshes/materials remain mapped assets.
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Unpack(root);
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    if (component != null) Remap(component, map);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        EnsureFolder("Assets/Art/BlockVisuals");
        var extra = records.Where(p => p.Value.StartsWith("Assets/Art/BlockVisuals/Dependencies/")).Select(p => new Record
        {
            source = p.Key, destination = p.Value, sourceSha256 = Hash(p.Key), sha256 = Hash(p.Value), guid = AssetDatabase.AssetPathToGUID(p.Value)
        }).ToArray();
        File.WriteAllText(extraCatalog, JsonUtility.ToJson(new Catalog { assets = extra }, true));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    public static void Unpack(GameObject root)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child != null && PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject))
                PrefabUtility.UnpackPrefabInstance(child.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void Remap(Object target, Dictionary<Object, Object> map)
    {
        using (var serialized = new SerializedObject(target))
        {
            var property = serialized.GetIterator();
            bool changed = false;
            while (property.Next(true))
                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null && map.TryGetValue(property.objectReferenceValue, out Object replacement))
                { property.objectReferenceValue = replacement; changed = true; }
            if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}

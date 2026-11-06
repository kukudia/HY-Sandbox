// 职责：在真实 AssetDatabase 中验证特效/音频共享发现、过滤、引用与窗口生命周期。
// 入口：Run()；只读既有资源，临时验证资产在 finally 删除。
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PresentationResourceChecks
{
    public static string Run()
    {
        var effects = PresentationAssetIndex.Scan(PresentationAssetIndex.Kind.Effects, false);
        Require(effects.Select(entry => entry.Guid).Distinct().Count() == effects.Count, "Duplicate effect GUIDs");
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { BlockVfxBaker.Root }))
        {
            Require(
                effects.Any(entry => entry.Guid == guid),
                "Missing project effect: " + AssetDatabase.GUIDToAssetPath(guid));
        }

        var explosion = effects.Single(entry => entry.Path == BlockVfxBaker.Root + "/Explosion.prefab");
        Require(PresentationAssetIndex.Matches(explosion, "EXPLOSION", "Prefab"), "Case-insensitive search");
        Require(!PresentationAssetIndex.Matches(explosion, "Explosion", "VFX Graph"), "Category filter");
        Require(
            PresentationAssetIndex.FindReferencers(explosion.Path).Contains("Assets/Resources/VFX/BlockVfxLibrary.asset"),
            "Library reverse reference");
        var expanded = PresentationAssetIndex.Scan(PresentationAssetIndex.Kind.Effects, true);
        Require(
            expanded.Count >= effects.Count && expanded.Any(entry => entry.Category == "依赖"),
            "Dependency coverage");
        string folder = "Assets/__ResourceChecks_" + Guid.NewGuid().ToString("N");
        AssetDatabase.CreateFolder("Assets", folder.Substring("Assets/".Length));
        try
        {
            CheckLibraryEditing(folder);
            AudioClip clip = AudioClip.Create("ResourceCheck", 64, 1, 44100, false);
            string path = folder + "/Audio.asset";
            AssetDatabase.CreateAsset(clip, path);
            string guid = AssetDatabase.AssetPathToGUID(path);
            Require(
                PresentationAssetIndex.Scan(PresentationAssetIndex.Kind.Audio, false).Any(entry => entry.Guid == guid),
                "Audio discovery");
            string moved = folder + "/Renamed.asset";
            Require(string.IsNullOrEmpty(AssetDatabase.MoveAsset(path, moved)), "Move fixture");
            var entry = new PresentationAssetIndex.Entry(guid, "AudioClip");
            Require(
                entry.Path == moved && PresentationAssetIndex.Matches(entry, "Renamed", "AudioClip"),
                "GUID survives move");
            Require(
                !PresentationAssetIndex.Scan(PresentationAssetIndex.Kind.Effects, false).Any(item => item.Guid == guid),
                "Audio/effect isolation");
        }
        finally
        {
            AssetDatabase.DeleteAsset(folder);
        }

        BlockVfxLibrary library = AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>("Assets/Resources/VFX/BlockVfxLibrary.asset");
        Editor inspector = Editor.CreateEditor(library);
        try
        {
            Require(inspector is BlockVfxLibraryEditor, "Custom library inspector");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(inspector);
        }

        PresentationResourceWindow.Open();
        return $"PASS: {effects.Count} effect assets; {expanded.Count} with dependencies; audio discovery, GUID move, filters, reverse references, library edit/undo/persistence, library inspector, window opened.";
    }

    private static void CheckLibraryEditing(string folder)
    {
        string path = folder + "/Library.asset";
        Require(
            AssetDatabase.CopyAsset("Assets/Resources/VFX/BlockVfxLibrary.asset", path),
            "Copy library fixture");
        BlockVfxLibrary copy = AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>(path);
        using (var data = new SerializedObject(copy))
        {
            SerializedProperty slot = data.FindProperty("_bursts").GetArrayElementAtIndex(0);
            UnityEngine.Object original = slot.objectReferenceValue;
            Require(original != null, "Build slot assigned");
            Undo.IncrementCurrentGroup();
            slot.objectReferenceValue = null;
            data.ApplyModifiedProperties();
            Undo.FlushUndoRecordObjects();
            Undo.PerformUndo();
            data.Update();
            Require(slot.objectReferenceValue == original, "Library edit supports Undo");
            slot.objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(copy);
        }

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        using (var reloaded = new SerializedObject(AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>(path)))
        {
            Require(
                reloaded.FindProperty("_bursts").GetArrayElementAtIndex(0).objectReferenceValue == null,
                "Library edit persists");
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

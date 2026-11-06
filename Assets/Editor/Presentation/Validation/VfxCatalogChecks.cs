// 职责：验证集中配置的覆盖、持久引用和真实 Play 挂载/替换/停止生命周期。
// 入口：CheckAssets() / CheckPlay()；Play 检查只创建隔离夹具，finally 还原内存配置并清理。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

public static class VfxCatalogChecks
{
    public static string CheckPreservation(string baselineRoot)
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { baselineRoot }))
        {
            string baselinePath = AssetDatabase.GUIDToAssetPath(guid);
            string currentPath = "Assets" + baselinePath.Substring(baselineRoot.Length);
            GameObject before = AssetDatabase.LoadAssetAtPath<GameObject>(baselinePath);
            GameObject after = AssetDatabase.LoadAssetAtPath<GameObject>(currentPath);
            var current = after.GetComponentsInChildren<VfxEffect>(true).ToDictionary(effect => GlobalObjectId.GetGlobalObjectIdSlow(effect).targetObjectId);
            foreach (VfxEffect old in before.GetComponentsInChildren<VfxEffect>(true))
            {
                Require(
                    current.TryGetValue(GlobalObjectId.GetGlobalObjectIdSlow(old).targetObjectId, out VfxEffect migrated),
                    "Component identity: " + currentPath);
                Require(
                    old.transform.localPosition == migrated.transform.localPosition && old.transform.localRotation == migrated.transform.localRotation && old.transform.localScale == migrated.transform.localScale,
                    "Anchor transform: " + currentPath);
                if (!migrated.IsGroup)
                {
                    Require(
                        VfxCatalogMigration.Signature(old) == VfxCatalogMigration.Signature(BlockVfxLibrary.Instance.Resolve(migrated.EffectId)),
                        "Appearance changed: " + currentPath + "/" + old.name);
                }

                count++;
            }
        }

        return $"PASS: {count} component identities, transforms and leaf graph signatures preserved.";
    }

    public static string CheckAssets()
    {
        Require(!Application.isPlaying, "Requires Edit Mode");
        int anchors = 0, groups = 0;
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art", "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (path.StartsWith(BlockVfxBaker.Root + "/", StringComparison.Ordinal))
            {
                continue;
            }

            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (VfxEffect effect in root.GetComponentsInChildren<VfxEffect>(true))
            {
                Require(effect.Graphs.Length == 0, "Distributed Graph reference: " + path);
                if (effect.IsGroup)
                {
                    groups++;
                    continue;
                }

                VfxEffect template = BlockVfxLibrary.Instance.Resolve(effect.EffectId);
                Require(
                    template != null && template.IsTemplate && template.Graphs.Length > 0,
                    "Missing catalog entry: " + path);
                anchors++;
            }
        }

        Require(anchors > 40 && groups >= 3, "Coverage unexpectedly reduced");
        return $"PASS: {anchors} centralized anchors, {groups} groups; no distributed Graph references in migrated controllers.";
    }

    public static string CheckPlay()
    {
        Require(Application.isPlaying, "Requires Play Mode");
        var fixtures = new List<GameObject>();
        BlockVfxLibrary library = BlockVfxLibrary.Instance;
        using var data = new SerializedObject(library);
        SerializedProperty entries = data.FindProperty("_attached");
        SerializedProperty hover = null;
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            if (entry.FindPropertyRelative("id").stringValue == "HoverJet")
            {
                hover = entry.FindPropertyRelative("prefab");
            }
        }

        Require(hover != null, "Missing HoverJet slot");
        Object original = hover.objectReferenceValue;
        try
        {
            VfxEffect nozzle = LoadAnchor("Assets/Resources/Blocks/HoverThruster.prefab", "HoverJet", fixtures);
            nozzle.SetIntensity(1f);
            Require(nozzle.Graphs.Length == 1 && nozzle.IsEmitting, "Thruster resolves catalog");
            int childCount = nozzle.GetComponentsInChildren<VisualEffect>(true).Length;
            nozzle.gameObject.SetActive(false);
            Require(!nozzle.IsEmitting, "Disable clears playback");
            nozzle.gameObject.SetActive(true);
            nozzle.SetIntensity(1f);
            Require(
                nozzle.GetComponentsInChildren<VisualEffect>(true).Length == childCount,
                "Re-enable does not duplicate content");
            VfxEffect bot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Blocks/Bot.prefab").GetComponentsInChildren<VfxEffect>(true).Single(effect => effect.IsGroup);
            VfxEffect flight = Object.Instantiate(bot);
            fixtures.Add(flight.gameObject);
            flight.SetIntensity(1f);
            Require(flight.Graphs.Length == 2 && flight.IsEmitting, "Bot group resolves both exhausts");
            flight.Clear();
            Require(!flight.IsEmitting, "Bot group clears");
            VfxEffect muzzle = LoadAnchor("Assets/Resources/Blocks/Turret.prefab", "MuzzleFlash", fixtures);
            muzzle.PlayOnce();
            Require(muzzle.Graphs.Length == 1 && muzzle.IsEmitting, "Muzzle resolves and plays");
            VfxEffect trail = LoadAnchor("Assets/Resources/Salvage/LootDrop.prefab", "EnergyTrail", fixtures);
            Require(trail.IsEmitting, "Loot trail automatic playback preserved");
            hover.objectReferenceValue = library.Resolve("RepairContact");
            data.ApplyModifiedPropertiesWithoutUndo();
            VfxEffect replaced = LoadAnchor("Assets/Resources/Blocks/HoverThruster.prefab", "HoverJet", fixtures);
            Require(
                replaced.Graphs[0].visualEffectAsset == library.Resolve("RepairContact").Graphs[0].visualEffectAsset,
                "One catalog replacement reaches a new gameplay anchor");
            Require(
                nozzle.Graphs[0].visualEffectAsset != replaced.Graphs[0].visualEffectAsset,
                "Existing instance retains its content");
            return "PASS: thruster, bot dual exhaust, muzzle and loot trail; disable/re-enable; catalog replacement reaches new instances.";
        }
        finally
        {
            hover.objectReferenceValue = original;
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (GameObject fixture in fixtures)
            {
                Object.DestroyImmediate(fixture);
            }
        }
    }

    private static VfxEffect LoadAnchor(string path, string id, List<GameObject> fixtures)
    {
        VfxEffect source = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<VfxEffect>(true).First(effect => effect.EffectId == id);
        VfxEffect instance = Object.Instantiate(source);
        fixtures.Add(instance.gameObject);
        return instance;
    }

    private static void Require(bool value, string message)
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }
}

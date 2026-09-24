using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

/// <summary>One-way migration of project-owned prefab components using Editor serialization.</summary>
public static class NativeVfxMigration
{
    [MenuItem("Tools/HY Sandbox/VFX Graph/4 Migrate Project Prefabs")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art", "Assets/Resources" })
            .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p.StartsWith(BlockVfxBaker.Root) ? 0 : 1))
        {
            string text = File.ReadAllText(path);
            if (!text.Contains("ParticleSystem:") && !text.Contains("TrailRenderer:")) continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try { Convert(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        BlockVfxBaker.BakeMissing();
        AssetDatabase.SaveAssets();
    }
    public static void Convert(GameObject root)
    {
        BlockArtDependencies.Unpack(root);
        foreach (var effect in root.GetComponentsInChildren<VfxEffect>(true).Reverse())
        {
            if (effect.name == "Flight Effects") continue;
            if (effect.GetComponentsInChildren<ParticleSystem>(true).Length == 0) continue;
            RemoveParticles(effect.gameObject);
            BlockVfxBaker.Populate(effect.gameObject, effect.name);
        }
        // Reserve imported effects have no controller. Keep the original attachment transforms.
        foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (system == null) continue;
            GameObject owner = system.gameObject;
            bool loop = system.main.loop;
            string name = owner.name.ToLowerInvariant();
            string kind = name.Contains("steam") ? "SteamLeak"
                : name.Contains("fire") ? "DetachedSmoke"
                : name.Contains("pebble") ? "BreakBurst"
                : name.Contains("laser") ? "ImpactBurst"
                : name.Contains("smoke") ? (loop ? "DetachedSmoke" : "SmokeBurst")
                : name.Contains("explosion") ? "Explosion"
                : name.Contains("flame") || name.Contains("exhaust") || name.Contains("thruster") || name.Contains("boost") ? "ThrusterJet"
                : loop ? "RepairContact" : "BuildBurst";
            RemoveParticles(owner);
            BlockVfxBaker.Populate(owner, kind, true);
        }
        foreach (var trail in root.GetComponentsInChildren<TrailRenderer>(true))
        {
            GameObject owner = trail.gameObject;
            Object.DestroyImmediate(trail);
            // A trail on a gameplay root gets a separate child controller.
            if (owner == root) { owner = new GameObject("Loot trail"); owner.transform.SetParent(root.transform, false); }
            BlockVfxBaker.Populate(owner, "EnergyTrail", root.GetComponent<LootDrop>() != null);
        }
        foreach (var group in root.GetComponentsInChildren<VfxEffect>(true).Where(e => e.name == "Flight Effects"))
            BlockVfxBaker.SetObjects(group, "_graphs", group.GetComponentsInChildren<VisualEffect>(true));
        foreach (var bot in root.GetComponentsInChildren<Bot>(true))
        {
            var effects = bot.GetComponentsInChildren<VfxEffect>(true);
            BlockVfxBaker.SetObject(bot, "_flightTrail", effects.FirstOrDefault(e => e.name == "FlightTrail"));
            BlockVfxBaker.SetObject(bot, "_flightCoreTrail", effects.FirstOrDefault(e => e.name == "FlightTrailCore"));
        }
        var drop = root.GetComponent<LootDrop>();
        if (drop != null)
        {
            var burst = root.GetComponentsInChildren<VfxEffect>(true).First(e => e.name == "Coin burst");
            using (var data = new SerializedObject(burst)) { data.FindProperty("_playOnEnable").boolValue = false; data.ApplyModifiedPropertiesWithoutUndo(); }
            BlockVfxBaker.SetObject(drop, "_burst", burst);
        }
    }
    private static void RemoveParticles(GameObject root)
    {
        foreach (var renderer in root.GetComponentsInChildren<ParticleSystemRenderer>(true)) Object.DestroyImmediate(renderer);
        foreach (var system in root.GetComponentsInChildren<ParticleSystem>(true).Reverse()) Object.DestroyImmediate(system);
    }
    public static string Validate()
    {
        var errors = new List<string>(); int graphs = 0, prefabs = 0;
        foreach (string path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art", "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var node in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) > 0) errors.Add("Missing script: " + path);
            if (root.GetComponentsInChildren<ParticleSystem>(true).Length > 0 || root.GetComponentsInChildren<TrailRenderer>(true).Length > 0) errors.Add("Legacy renderer: " + path);
            foreach (var effect in root.GetComponentsInChildren<VfxEffect>(true))
                if (effect.Graphs.Length == 0 || effect.Graphs.Any(g => g == null)) errors.Add("Unbound controller: " + path + "/" + effect.name);
            var effects = root.GetComponentsInChildren<VisualEffect>(true); graphs += effects.Length;
            if (effects.Length > 0) prefabs++;
            foreach (var effect in effects)
                if (effect.visualEffectAsset == null || !effect.HasFloat("Intensity") || !effect.GetComponent<VFXRenderer>().enabled) errors.Add("Invalid graph: " + path);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:VisualEffectAsset", new[] { NativeVfxLibraryBuilder.Root }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (string dependency in AssetDatabase.GetDependencies(path))
                if (dependency.StartsWith("Assets/") && !dependency.StartsWith("Assets/Art/")) errors.Add("External graph dependency: " + path + " -> " + dependency);
        }
        return Newtonsoft.Json.JsonConvert.SerializeObject(new { prefabs, graphs, errors }, Newtonsoft.Json.Formatting.Indented);
    }
}

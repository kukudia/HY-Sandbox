using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BlockArtValidation
{
    [MenuItem("Tools/HY Sandbox/Block Art/Validate Assets")]
    public static void Validate()
        => ValidateAssets(new[] { "Assets/Resources/Blocks", "Assets/Art/Temp", BlockVfxBaker.Root });

    public static void ValidateAssets(string[] roots)
    {
        var issues = new List<string>();
        var rows = new List<object>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", roots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var dependency in AssetDatabase.GetDependencies(path, true))
                    if (dependency.StartsWith("Assets/PolygonSciFiSpace/") || dependency.StartsWith("Assets/CUBE - Spaceships Pack 01/")) issues.Add(path + ": vendor dependency " + dependency);
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject)) issues.Add(path + ": nested prefab " + t.name);
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0) issues.Add(path + ": missing script " + t.name);
                }
                foreach (var component in root.GetComponentsInChildren<Component>(true).Where(c => c != null))
                {
                    using (var serialized = new SerializedObject(component))
                    {
                        var property = serialized.GetIterator();
                        while (property.NextVisible(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                issues.Add(path + ": broken " + component.GetType().Name + "." + property.propertyPath);
                    }
                }
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer is UnityEngine.VFX.VFXRenderer) continue;
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported) issues.Add(path + ": invalid material " + renderer.name);
                }
                var weapon = root.GetComponent<TurretWeapon>();
                if (weapon != null && (weapon.muzzle == null || !weapon.muzzle.IsChildOf(weapon.verticalAxis) || !weapon.verticalAxis.IsChildOf(weapon.horizontalAxis))) issues.Add(path + ": invalid aiming chain");
                var thruster = root.GetComponent<Thruster>();
                if (thruster != null)
                {
                    var nozzles = thruster.model.GetComponentsInChildren<VfxEffect>();
                    if (nozzles.Length == 0) issues.Add(path + ": no nozzle effects");
                    foreach (var nozzle in nozzles)
                    {
                        Vector3 force = thruster is HoverThruster ? root.transform.up : thruster.model.forward;
                        if (Vector3.Dot(nozzle.transform.forward, -force) < 0.999f) issues.Add(path + ": nozzle direction " + nozzle.name);
                    }
                }
                var block = root.GetComponent<Block>();
                foreach (var bot in root.GetComponentsInChildren<RepairBot>(true))
                {
                    using (var serialized = new SerializedObject(bot))
                        foreach (string field in new[] { "_repairOrigin", "_flightEffect", "_repairImpact", "_flightTrail", "_flightCoreTrail" })
                            if (serialized.FindProperty(field).objectReferenceValue == null) issues.Add(path + ": unbound Bot " + field);
                    if (root.name == "RepairBotContianer" && (bot.home == null || bot.outside == null || bot.home == bot.outside)) issues.Add(path + ": invalid Bot home/outside");
                }
                rows.Add(new { path, connectors = block != null ? block.connectors.Count : 0,
                    enabledConnectors = block != null ? block.connectors.Count(c => c.canConnect) : 0,
                    colliders = root.GetComponentsInChildren<Collider>(true).Length,
                    lights = root.GetComponentsInChildren<Light>(true).Length,
                    graphs = root.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true).Length,
                    triangles = root.GetComponentsInChildren<MeshFilter>(true).Where(m => m.sharedMesh != null).Sum(m => (long)m.sharedMesh.triangles.Length / 3) });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText("Assets/Art/BlockVisuals/Validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { unity = Application.unityVersion, roots, issues, prefabs = rows }, Newtonsoft.Json.Formatting.Indented));
        AssetDatabase.Refresh();
        if (issues.Count > 0) throw new InvalidOperationException(string.Join("\n", issues));
        Debug.Log("Block art validation passed: " + rows.Count + " prefabs.");
    }
}

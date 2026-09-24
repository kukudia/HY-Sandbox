using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

/// <summary>Editable native Graph prefab factory. Existing GUIDs are retained.</summary>
public static class BlockVfxBaker
{
    public const string Root = "Assets/Art/BlockVisuals/VFX";
    public static readonly string[] Names = { "ThrusterJet", "HoverJet", "BotFlight", "RepairContact", "RepairPulse", "BuildBurst", "MuzzleFlash", "ImpactBurst", "DetachedSmoke", "SmokeBurst", "Explosion", "BreakBurst", "EnergyTrail", "EnergyBeam" };
    public static string GraphName(string name)
    {
        switch (name)
        {
            case "Explosion": return "UNI_Aerial_Explosion";
            case "BreakBurst": return "UNI_Small_Explosion";
            case "ImpactBurst": case "MuzzleFlash": return "UNI_Impact_Explosion";
            case "SmokeBurst": return "UNI_Small_Smoke_Impact";
            case "DetachedSmoke": return "UNI_Device_Fire";
            case "SteamLeak": return "UNI_Steam_Leak";
            case "ThrusterJet": case "HoverJet": case "BotFlight": return "UNI_Gas_Fire_Thruster";
            case "RepairContact": return "EnergyContact";
            case "EnergyTrail": case "EnergyBeam": return name;
            default: return "EnergyBurst";
        }
    }
    public static bool Continuous(string name) => name == "ThrusterJet" || name == "HoverJet" || name == "BotFlight" || name == "RepairContact" || name == "DetachedSmoke" || name == "EnergyTrail";
    public static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + name + ".prefab");
    public static VfxEffect Populate(GameObject root, string name, bool automatic = false)
    {
        var controller = root.GetComponent<VfxEffect>() ?? root.AddComponent<VfxEffect>();
        var child = new GameObject("Graph - " + GraphName(name)); child.transform.SetParent(root.transform, false);
        var graph = child.AddComponent<VisualEffect>();
        graph.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(NativeVfxLibraryBuilder.GraphPath(GraphName(name)));
        if (graph.visualEffectAsset == null) throw new System.InvalidOperationException("Missing graph: " + name);
        graph.initialEventName = "ControlledStart";
        graph.GetComponent<VFXRenderer>().enabled = true;
        // UNI Gas Fire's jet flows along -X; project sockets point along +Z.
        if (GraphName(name) == "UNI_Gas_Fire_Thruster")
        {
            child.transform.localRotation = Quaternion.FromToRotation(Vector3.left, Vector3.forward);
            child.transform.localScale = Vector3.one * (name == "BotFlight" ? 0.2f : name == "HoverJet" ? 0.6f : 0.45f);
            graph.SetFloat("EmberSpawnRate", 4f);
        }
        else if (name == "MuzzleFlash") child.transform.localScale = Vector3.one * 0.15f;
        else if (name == "ImpactBurst") child.transform.localScale = Vector3.one * 0.3f;
        else if (name == "DetachedSmoke") child.transform.localScale = Vector3.one * 0.35f;
        SetObjects(controller, "_graphs", new Object[] { graph });
        using (var data = new SerializedObject(controller))
        {
            data.FindProperty("_releaseAfter").floatValue = 12f;
            data.FindProperty("_playOnEnable").boolValue = automatic;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        return controller;
    }
    public static void BakeMissing()
    {
        BlockArtDependencies.EnsureFolder(Root);
        foreach (string name in Names)
        {
            if (Load(name) != null) continue;
            var root = new GameObject(name);
            try { Populate(root, name); PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab"); }
            finally { Object.DestroyImmediate(root); }
        }
        var library = AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>("Assets/Resources/VFX/BlockVfxLibrary.asset");
        SetObjects(library, "_bursts", new[] { "BuildBurst", "BreakBurst", "Explosion", "SmokeBurst", "RepairPulse", "MuzzleFlash", "ImpactBurst" }.Select(n => (Object)Load(n).GetComponent<VfxEffect>()).ToArray());
        SetObject(library, "_detachedSmoke", Load("DetachedSmoke").GetComponent<VfxEffect>());
        SetObject(library, "_beam", Load("EnergyBeam").GetComponent<VfxEffect>());
        AssetDatabase.SaveAssets();
    }
    public static void SetObjects(Object target, string field, Object[] values)
    {
        using (var data = new SerializedObject(target))
        {
            var property = data.FindProperty(field); property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
    public static void SetObject(Object target, string field, Object value)
    {
        using (var data = new SerializedObject(target))
        { data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    }
}

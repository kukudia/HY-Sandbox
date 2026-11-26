// 职责：建立原生 VFX Graph 资源库并配置效果参数。
// 入口：GraphPath() / RebuildPositionSafeEffects()。
// 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

/// <summary>Maintains the curated native and utility graphs in Art.</summary>
public static class NativeVfxLibraryBuilder
{
    public const string Root = "Assets/Art/VFX";
    private const string NativeGraphs = Root + "/UNI/Realistic Explosions, Fire & Smoke/Visual Effects/";
    public static readonly string[] NativeNames =
    {
        "UNI_Aerial_Explosion",
        "UNI_Small_Explosion",
        "UNI_Impact_Explosion",
        "UNI_Small_Smoke_Impact",
        "UNI_Device_Fire",
        "UNI_Gas_Fire",
        "UNI_Steam_Leak"
    };
    public static string GraphPath(string name) => NativeNames.Contains(name) ? NativeGraphs + name + ".vfx" : Root + "/Graphs/" + name + ".vfx";
    public static string ThrusterGraphPath => GraphPath("UNI_Gas_Fire_Thruster");

    [MenuItem("Tools/HY Sandbox/VFX Graph/6 Rebuild Position-Safe Effects")]
    public static void RebuildPositionSafeEffects()
    {
        string source = GraphPath("UNI_Gas_Fire");
        BlockArtDependencies.EnsureFolder(Root + "/Graphs");
        if (!File.Exists(ThrusterGraphPath) && !AssetDatabase.CopyAsset(source, ThrusterGraphPath))
        {
            throw new IOException("Could not copy native UNI Gas Fire graph.");
        }

        object jet = VfxGraphAuthoring.Graph(ThrusterGraphPath);
        object smoke = VfxGraphAuthoring.Items(jet, "children").FirstOrDefault(c => c.GetType().Name.Contains("Output") && VfxGraphAuthoring.Items(c, "inputSlots").Any(s => (string)VfxGraphAuthoring.Get(s, "name") == "mainTexture" && (VfxGraphAuthoring.Get(s, "value") as Object)?.name == "uni_smoke_big"));
        if (smoke != null)
        {
            var chain = new List<object>();
            void Collect(object context)
            {
                if (chain.Contains(context))
                {
                    return;
                }

                chain.Add(context);
                foreach (object input in VfxGraphAuthoring.Items(context, "inputContexts"))
                {
                    Collect(input);
                }
            }

            Collect(smoke);
            foreach (object context in chain)
            {
                VfxGraphAuthoring.Call(jet, "RemoveChild", context, true);
            }
        }

        // Also repairs a graph from an interrupted earlier migration.
        foreach (object orphan in VfxGraphAuthoring.Items(jet, "children").Where(c => c.GetType().Name == "VFXBasicSpawner" && VfxGraphAuthoring.Items(c, "outputContexts").Length == 0).ToArray())
        {
            VfxGraphAuthoring.Call(jet, "RemoveChild", orphan, true);
        }

        SetSimulationSpace(jet, "Local");
        VfxGraphAuthoring.Save(ThrusterGraphPath);
        object[] jetContexts = VfxGraphAuthoring.Items(VfxGraphAuthoring.Graph(ThrusterGraphPath), "children");
        foreach (string type in new[]
        {
            "VFXBasicSpawner",
            "VFXBasicInitialize",
            "VFXBasicUpdate",
            "VFXPlanarPrimitiveOutput"
        }

        )
        {
            if (jetContexts.Count(c => c.GetType().Name == type) != 3)
            {
                throw new InvalidOperationException("Thruster graph must have three complete flame systems: " + type);
            }
        }

        if (jetContexts.Where(c => c.GetType().Name.Contains("Output")).SelectMany(c => VfxGraphAuthoring.Items(c, "inputSlots")).Any(s => (VfxGraphAuthoring.Get(s, "value") as Object)?.name == "uni_smoke_big"))
        {
            throw new InvalidOperationException("Thruster graph still contains the smoke output.");
        }

        foreach (string name in new[]
        {
            "UNI_Device_Fire",
            "UNI_Steam_Leak"
        }

        )
        {
            string path = GraphPath(name);
            SetSimulationSpace(VfxGraphAuthoring.Graph(path), "Local");
            VfxGraphAuthoring.Save(path);
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art", "Assets/Resources" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || !asset.GetComponentsInChildren<VisualEffect>(true).Any(v => v.visualEffectAsset != null && v.visualEffectAsset.name == "UNI_Gas_Fire"))
            {
                continue;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (VisualEffect graph in root.GetComponentsInChildren<VisualEffect>(true).Where(v => v.visualEffectAsset != null && v.visualEffectAsset.name == "UNI_Gas_Fire"))
                {
                    graph.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(ThrusterGraphPath);
                    graph.gameObject.name = "Graph - UNI Gas Fire (no smoke)";
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static void SetSimulationSpace(object graph, string space)
    {
        foreach (object initialize in VfxGraphAuthoring.Items(graph, "children").Where(c => c.GetType().Name == "VFXBasicInitialize"))
        {
            object data = VfxGraphAuthoring.Call(initialize, "GetData");
            VfxGraphAuthoring.Set(data, "space", Enum.Parse(VfxGraphAuthoring.Get(data, "space").GetType(), space));
        }
    }

    [MenuItem("Tools/HY Sandbox/VFX Graph/3 Create Utility Graphs")]
    public static void CreateUtilityGraphs()
    {
        BuildUtility("EnergyBurst", false, false, false);
        BuildUtility("EnergyContact", true, false, false);
        BuildUtility("EnergyTrail", true, true, false);
        BuildUtility("EnergyBeam", false, false, true);
        AssetDatabase.SaveAssets();
    }

    private static void BuildUtility(string name, bool loop, bool world, bool beam)
    {
        string path = GraphPath(name);
        BlockArtDependencies.EnsureFolder(Root + "/Graphs");
        if (!File.Exists(path))
        {
            AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/01_Minimal_System.vfx", path);
        }

        object graph = VfxGraphAuthoring.Graph(path);
        if (VfxGraphAuthoring.Items(graph, "children").Any(c => c.GetType().Name == "VFXParameter"))
        {
            return;
        }

        object[] contexts = VfxGraphAuthoring.Items(graph, "children");
        object spawn = contexts.First(c => c.GetType().Name == "VFXBasicSpawner");
        object initialize = contexts.First(c => c.GetType().Name == "VFXBasicInitialize");
        object update = contexts.First(c => c.GetType().Name == "VFXBasicUpdate");
        object output = contexts.First(c => c.GetType().Name == "VFXPlanarPrimitiveOutput");
        object data = VfxGraphAuthoring.Call(initialize, "GetData");
        VfxGraphAuthoring.Set(
            data,
            "space",
            Enum.Parse(VfxGraphAuthoring.Get(data, "space").GetType(), world ? "World" : "Local"));
        VfxGraphAuthoring.Setting(data, "capacity", (uint)(beam ? 1 : 64));
        VfxGraphAuthoring.Setting(data, "boundsMode", "Manual");
        object bounds = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(initialize, "bounds"), "value");
        bounds.GetType().GetField("size").SetValue(bounds, Vector3.one * (world ? 30 : 8));
        VfxGraphAuthoring.Value(initialize, "bounds", bounds);
        object emitter = VfxGraphAuthoring.Create(loop ? "VFXSpawnerConstantRate" : "VFXSpawnerBurst");
        VfxGraphAuthoring.Add(spawn, emitter);
        string emissionSlot = loop ? "Rate" : "Count";
        VfxGraphAuthoring.Value(emitter, emissionSlot, beam ? 1f : loop ? 80f : 16f);
        if (!beam)
        {
            VfxGraphAuthoring.Link(
                VfxGraphAuthoring.Parameter(graph, loop ? "SpawnRate" : "SpawnCount", loop ? 80f : 16f),
                VfxGraphAuthoring.Slot(emitter, emissionSlot));
        }

        VfxGraphAuthoring.Attribute(initialize, "lifetime", beam ? 100000f : world ? 0.35f : loop ? 0.3f : 0.65f);
        var size = VfxGraphAuthoring.Attribute(initialize, "size", beam ? 1f : 0.07f);
        if (!beam)
        {
            VfxGraphAuthoring.Link(VfxGraphAuthoring.Parameter(graph, "Size", 0.07f), VfxGraphAuthoring.Items(size, "inputSlots")[0]);
        }

        if (beam)
        {
            VfxGraphAuthoring.Call(update, "UnlinkTo", output, 0, 0);
            VfxGraphAuthoring.Call(graph, "RemoveChild", output, true);
            output = VfxGraphAuthoring.Create("VFXMeshOutput");
            VfxGraphAuthoring.Add(graph, output);
            VfxGraphAuthoring.Call(update, "LinkTo", output, 0, 0);
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            VfxGraphAuthoring.Value(output, "mesh", primitive.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(primitive);
            VfxGraphAuthoring.Attribute(initialize, "position", new Vector3(0, 0, 0.5f));
            VfxGraphAuthoring.Attribute(initialize, "angleX", 90f);
            VfxGraphAuthoring.Attribute(initialize, "scaleY", 0.5f);
        }
        else
        {
            object position = VfxGraphAuthoring.Attribute(initialize, "position", Vector3.zero);
            object positionSlot = VfxGraphAuthoring.Items(position, "inputSlots")[0];
            VfxGraphAuthoring.Set(positionSlot, "space", Enum.Parse(VfxGraphAuthoring.Get(positionSlot, "space").GetType(), "Local"));
            var velocity = VfxGraphAuthoring.Attribute(initialize, "velocity", Vector3.zero);
            VfxGraphAuthoring.Setting(velocity, "Random", "PerComponent");
            object[] slots = VfxGraphAuthoring.Items(velocity, "inputSlots");
            VfxGraphAuthoring.Set(slots[0], "value", world ? Vector3.zero : Vector3.one * -1.8f);
            VfxGraphAuthoring.Set(slots[1], "value", world ? Vector3.zero : Vector3.one * 1.8f);
            object orient = VfxGraphAuthoring.Create("Block.Orient");
            VfxGraphAuthoring.Add(output, orient);
            object fade = VfxGraphAuthoring.Create("Block.AttributeFromCurve");
            VfxGraphAuthoring.Setting(fade, "attribute", "alpha");
            VfxGraphAuthoring.Add(output, fade);
            VfxGraphAuthoring.Set(
                VfxGraphAuthoring.Items(fade, "inputSlots")[0],
                "value",
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0)));
        }

        VfxGraphAuthoring.Setting(output, "blendMode", "Additive");
        VfxGraphAuthoring.Value(
            output,
            "mainTexture",
            beam ? Texture2D.whiteTexture : AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/UNI/Common/Textures/uni_glow.png"));
        object color = VfxGraphAuthoring.Attribute(output, "color", new Vector3(0.25f, 2f, 3f));
        VfxGraphAuthoring.Link(
            VfxGraphAuthoring.Parameter(graph, "Tint", new Vector3(0.25f, 2f, 3f)),
            VfxGraphAuthoring.Items(color, "inputSlots")[0]);
        VfxGraphAuthoring.Save(path);
        VfxGraphAuthoring.AddIntensity(path);
    }
}

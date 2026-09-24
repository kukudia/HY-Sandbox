using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

/// <summary>Copies native UNI graphs and authors small utility graphs through Unity's graph model.</summary>
public static class NativeVfxLibraryBuilder
{
    public const string Root = "Assets/Art/VFX";
    private const string Vendor = "Assets/UNI VFX/";
    private const string Pack = Vendor + "Realistic Explosions, Fire & Smoke/";
    public static readonly string[] NativeNames = { "UNI_Aerial_Explosion", "UNI_Small_Explosion", "UNI_Impact_Explosion",
        "UNI_Small_Smoke_Impact", "UNI_Device_Fire", "UNI_Gas_Fire", "UNI_Steam_Leak" };

    public static string Destination(string source)
    {
        string path = Root + "/UNI/" + source.Substring(Vendor.Length);
        return path.EndsWith(".tga", StringComparison.OrdinalIgnoreCase) ? Path.ChangeExtension(path, ".png").Replace('\\', '/') : path;
    }
    public static string GraphPath(string name) => NativeNames.Contains(name)
        ? Destination(Pack + "Visual Effects/" + name + ".vfx") : Root + "/Graphs/" + name + ".vfx";

    [MenuItem("Tools/HY Sandbox/VFX Graph/1 Prepare UNI Dependency Manifest")]
    public static void PrepareSources()
    {
        BlockArtDependencies.EnsureFolder(Root);
        string shader = Vendor + "Common/Shaders/UNI-Masked.shadergraph";
        AssetDatabase.ImportAsset(shader, ImportAssetOptions.ForceUpdate);
        string[] graphs = NativeNames.Select(n => Pack + "Visual Effects/" + n + ".vfx").ToArray();
        foreach (string path in graphs) AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        string[] sources = AssetDatabase.GetDependencies(graphs.Append(shader).ToArray(), true)
            .Where(p => p.StartsWith(Vendor)).Append(shader).Distinct().OrderBy(p => p).ToArray();
        File.WriteAllText(Root + "/Sources.json", Newtonsoft.Json.JsonConvert.SerializeObject(sources.Select(p => new
        {
            source = p, destination = Destination(p), sourceSha256 = Hash(p)
        }), Newtonsoft.Json.Formatting.Indented));
    }

    [MenuItem("Tools/HY Sandbox/VFX Graph/2 Import Native Graphs")]
    public static void ImportSources()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var records = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(Root + "/Sources.json"));
        var paths = records.ToDictionary(r => (string)r["source"], r => (string)r["destination"]);
        foreach (var pair in paths.OrderBy(p => p.Key.EndsWith(".vfx") ? 1 : 0))
        {
            BlockArtDependencies.EnsureFolder(Path.GetDirectoryName(pair.Value).Replace('\\', '/'));
            if (!File.Exists(pair.Value))
            {
                if (pair.Key.EndsWith(".tga")) throw new FileNotFoundException("Run ArtSource/VfxGraph/prepare_textures.py first", pair.Value);
                if (!AssetDatabase.CopyAsset(pair.Key, pair.Value)) throw new IOException(pair.Key);
            }
            AssetDatabase.ImportAsset(pair.Value, ImportAssetOptions.ForceSynchronousImport);
            if (AssetImporter.GetAtPath(pair.Key) is TextureImporter original && AssetImporter.GetAtPath(pair.Value) is TextureImporter copy)
            {
                var settings = new TextureImporterSettings(); original.ReadTextureSettings(settings); copy.SetTextureSettings(settings);
                copy.SetPlatformTextureSettings(original.GetDefaultPlatformTextureSettings());
                copy.SaveAndReimport();
            }
        }
        var guids = paths.ToDictionary(p => AssetDatabase.AssetPathToGUID(p.Key), p => AssetDatabase.AssetPathToGUID(p.Value));
        var objects = new Dictionary<Object, Object>();
        foreach (var pair in paths)
        {
            Object[] copies = AssetDatabase.LoadAllAssetsAtPath(pair.Value);
            foreach (Object original in AssetDatabase.LoadAllAssetsAtPath(pair.Key).Where(o => o != null))
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string unused, out long id);
                Object copy = copies.FirstOrDefault(o =>
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string guid, out long localId);
                    return localId == id && o.GetType() == original.GetType();
                });
                if (copy != null) objects[original] = copy;
            }
        }
        foreach (var pair in paths.Where(p => p.Key.EndsWith(".vfx")))
        {
            VfxGraphAuthoring.Graph(pair.Value);
            foreach (Object content in VfxGraphAuthoring.Contents(pair.Value).Where(o => o != null))
            {
                using (var data = new SerializedObject(content))
                {
                    var property = data.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue != null
                            && objects.TryGetValue(property.objectReferenceValue, out Object replacement)) property.objectReferenceValue = replacement;
                        if (property.propertyType == SerializedPropertyType.String)
                        {
                            string value = property.stringValue;
                            foreach (var guid in guids) value = value.Replace(guid.Key, guid.Value);
                            property.stringValue = value;
                        }
                    }
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            VfxGraphAuthoring.Save(pair.Value);
            // A final output alpha multiplier leaves UNI's simulation and authored gradients intact at 1.
            VfxGraphAuthoring.AddIntensity(pair.Value);
        }
        foreach (var record in records)
        {
            record["destinationSha256"] = Hash((string)record["destination"]);
            record["guid"] = AssetDatabase.AssetPathToGUID((string)record["destination"]);
        }
        File.WriteAllText(Root + "/Sources.json", records.ToString());
        AssetDatabase.SaveAssets();
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
        if (!File.Exists(path)) AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/01_Minimal_System.vfx", path);
        object graph = VfxGraphAuthoring.Graph(path);
        if (VfxGraphAuthoring.Items(graph, "children").Any(c => c.GetType().Name == "VFXParameter")) return;
        object[] contexts = VfxGraphAuthoring.Items(graph, "children");
        object spawn = contexts.First(c => c.GetType().Name == "VFXBasicSpawner");
        object initialize = contexts.First(c => c.GetType().Name == "VFXBasicInitialize");
        object update = contexts.First(c => c.GetType().Name == "VFXBasicUpdate");
        object output = contexts.First(c => c.GetType().Name == "VFXPlanarPrimitiveOutput");
        object data = VfxGraphAuthoring.Call(initialize, "GetData");
        VfxGraphAuthoring.Set(data, "space", Enum.Parse(VfxGraphAuthoring.Get(data, "space").GetType(), world ? "World" : "Local"));
        VfxGraphAuthoring.Setting(data, "capacity", (uint)(beam ? 1 : 64));
        VfxGraphAuthoring.Setting(data, "boundsMode", "Manual");
        object bounds = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(initialize, "bounds"), "value");
        bounds.GetType().GetField("size").SetValue(bounds, Vector3.one * (world ? 30 : 8));
        VfxGraphAuthoring.Value(initialize, "bounds", bounds);
        object emitter = VfxGraphAuthoring.Create(loop ? "VFXSpawnerConstantRate" : "VFXSpawnerBurst");
        VfxGraphAuthoring.Add(spawn, emitter);
        string emissionSlot = loop ? "Rate" : "Count";
        VfxGraphAuthoring.Value(emitter, emissionSlot, beam ? 1f : loop ? 80f : 16f);
        if (!beam) VfxGraphAuthoring.Link(VfxGraphAuthoring.Parameter(graph, loop ? "SpawnRate" : "SpawnCount", loop ? 80f : 16f), VfxGraphAuthoring.Slot(emitter, emissionSlot));
        VfxGraphAuthoring.Attribute(initialize, "lifetime", beam ? 100000f : world ? 0.35f : loop ? 0.3f : 0.65f);
        var size = VfxGraphAuthoring.Attribute(initialize, "size", beam ? 1f : 0.07f);
        if (!beam) VfxGraphAuthoring.Link(VfxGraphAuthoring.Parameter(graph, "Size", 0.07f), VfxGraphAuthoring.Items(size, "inputSlots")[0]);
        if (beam)
        {
            VfxGraphAuthoring.Call(update, "UnlinkTo", output, 0, 0);
            VfxGraphAuthoring.Call(graph, "RemoveChild", output, true);
            output = VfxGraphAuthoring.Create("VFXMeshOutput");
            VfxGraphAuthoring.Add(graph, output); VfxGraphAuthoring.Call(update, "LinkTo", output, 0, 0);
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
            object orient = VfxGraphAuthoring.Create("Block.Orient"); VfxGraphAuthoring.Add(output, orient);
            object fade = VfxGraphAuthoring.Create("Block.AttributeFromCurve");
            VfxGraphAuthoring.Setting(fade, "attribute", "alpha"); VfxGraphAuthoring.Add(output, fade);
            VfxGraphAuthoring.Set(VfxGraphAuthoring.Items(fade, "inputSlots")[0], "value", new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 1), new Keyframe(1, 0)));
        }
        VfxGraphAuthoring.Setting(output, "blendMode", "Additive");
        VfxGraphAuthoring.Value(output, "mainTexture", beam ? Texture2D.whiteTexture : AssetDatabase.LoadAssetAtPath<Texture2D>(Destination(Vendor + "Common/Textures/uni_glow.png")));
        object color = VfxGraphAuthoring.Attribute(output, "color", new Vector3(0.25f, 2f, 3f));
        VfxGraphAuthoring.Link(VfxGraphAuthoring.Parameter(graph, "Tint", new Vector3(0.25f, 2f, 3f)), VfxGraphAuthoring.Items(color, "inputSlots")[0]);
        VfxGraphAuthoring.Save(path);
        VfxGraphAuthoring.AddIntensity(path);
    }

    private static string Hash(string path)
    {
        using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
    }
}

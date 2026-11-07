// 职责：制作敌机出生传送门的程序化纹理、原生 GPU Graph 和可编辑 Prefab，接入集中播放库。
// 入口：BakeMissing()；只补缺失资源，保留已有 Graph、Prefab 和库选择。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

public static class EnemySpawnPortalBaker
{
    public const string Id = "EnemySpawnPortal";
    public const string GraphPath = "Assets/Art/VFX/Graphs/EnemySpawnPortal.vfx";
    public const string PrefabPath = "Assets/Art/BlockVisuals/VFX/EnemySpawnPortal.prefab";
    private const string TexturePath = "Assets/Art/BlockVisuals/VFX/EnemySpawnPortal.png";
    [MenuItem("Tools/HY Sandbox/资源管理/制作敌机传送门")]
    public static void BakeMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Exit Play Mode before baking the portal.");
        }

        CreateTexture();
        CreateGraph();
        CreatePrefab();
        var library = AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>("Assets/Resources/VFX/BlockVfxLibrary.asset");
        using var data = new SerializedObject(library);
        SerializedProperty entries = data.FindProperty("_attached");
        SerializedProperty entry = null;
        for (int i = 0; i < entries.arraySize; i++)
        {
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == Id)
            {
                entry = entries.GetArrayElementAtIndex(i);
                break;
            }
        }

        if (entry == null)
        {
            int index = entries.arraySize++;
            entry = entries.GetArrayElementAtIndex(index);
            entry.FindPropertyRelative("id").stringValue = Id;
            entry.FindPropertyRelative("prefab").objectReferenceValue = null;
        }

        if (entry.FindPropertyRelative("prefab").objectReferenceValue == null)
        {
            entry.FindPropertyRelative("prefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<VfxEffect>();
        }

        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(library);
    }

    [MenuItem("Tools/HY Sandbox/资源管理/重制敌机传送门纹理")]
    public static void RebuildTexture()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Exit Play Mode before rebuilding the portal texture.");
        }

        CreateTexture(true);
    }

    private static void CreateTexture(bool overwrite = false)
    {
        if (!overwrite && File.Exists(TexturePath))
        {
            return;
        }

        // 自有确定性数学纹理：金橙边缘、紫色螺旋和断续符文，不依赖第三方图片。
        const int resolution = 256;
        var texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true);
        try
        {
            var pixels = new Color[resolution * resolution];
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float u = (x + 0.5f) / resolution * 2f - 1f;
                    float v = (y + 0.5f) / resolution * 2f - 1f;
                    float radius = Mathf.Sqrt(u * u + v * v);
                    float angle = Mathf.Atan2(v, u);
                    float ring = Mathf.Exp(-Mathf.Pow((radius - 0.82f) / 0.025f, 2f));
                    float halo = 0.24f * Mathf.Exp(-Mathf.Pow((radius - 0.82f) / 0.09f, 2f));
                    float runes = Mathf.Exp(-Mathf.Pow((radius - 0.94f) / 0.013f, 2f)) * Mathf.Pow(Mathf.Max(0f, Mathf.Cos(angle * 16f)), 2f);
                    float spiral = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(angle * 5f - radius * 24f), 5f);
                    float interior = Mathf.SmoothStep(0f, 1f, radius / 0.2f) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 0.82f, radius)));
                    Color color = new Color(1f, 0.32f, 0.045f) * (ring + halo + runes * 0.65f) + new Color(0.3f, 0.12f, 0.8f) * (spiral * 0.52f + 0.04f) * interior;
                    color.a = Mathf.Clamp01(ring + halo + runes + interior * (spiral * 0.65f + 0.08f));
                    pixels[y * resolution + x] = color;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
        }
        finally
        {
            Object.DestroyImmediate(texture);
        }

        AssetDatabase.ImportAsset(TexturePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
        importer.sRGBTexture = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.mipmapEnabled = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void CreateGraph()
    {
        if (File.Exists(GraphPath))
        {
            return;
        }

        if (!AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/Minimal_System.vfx", GraphPath))
        {
            throw new IOException("Cannot create portal graph.");
        }

        object graph = VfxGraphAuthoring.Graph(GraphPath);
        object[] contexts = VfxGraphAuthoring.Items(graph, "children");
        object spawn = contexts.First(c => c.GetType().Name == "VFXBasicSpawner");
        object init = contexts.First(c => c.GetType().Name == "VFXBasicInitialize");
        object output = contexts.First(c => c.GetType().Name == "VFXPlanarPrimitiveOutput");
        object data = VfxGraphAuthoring.Call(init, "GetData");
        VfxGraphAuthoring.Set(data, "space", Enum.Parse(VfxGraphAuthoring.Get(data, "space").GetType(), "Local"));
        VfxGraphAuthoring.Setting(data, "capacity", (uint)1);
        object burst = VfxGraphAuthoring.Create("VFXSpawnerBurst");
        VfxGraphAuthoring.Add(spawn, burst);
        VfxGraphAuthoring.Value(burst, "Count", 1f);
        object life = VfxGraphAuthoring.Attribute(init, "lifetime", 2.4f);
        VfxGraphAuthoring.Link(
            VfxGraphAuthoring.Parameter(graph, "Lifetime", 2.4f),
            VfxGraphAuthoring.Items(life, "inputSlots")[0]);
        VfxGraphAuthoring.Attribute(init, "position", Vector3.zero);
        VfxGraphAuthoring.Attribute(init, "size", 1f);
        VfxGraphAuthoring.Setting(output, "blendMode", "Additive");
        VfxGraphAuthoring.Setting(output, "cullMode", "Off");
        VfxGraphAuthoring.Value(output, "mainTexture", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
        object color = VfxGraphAuthoring.Attribute(output, "color", Vector3.one * 3f);
        VfxGraphAuthoring.Link(
            VfxGraphAuthoring.Parameter(graph, "Tint", Vector3.one * 3f),
            VfxGraphAuthoring.Items(color, "inputSlots")[0]);
        Curve(
            output,
            "size",
            new AnimationCurve(new Keyframe(0f, 0.08f), new Keyframe(0.08f, 1f), new Keyframe(0.62f, 1f), new Keyframe(1f, 0f)));
        Curve(
            output,
            "alpha",
            new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.06f, 1f), new Keyframe(0.58f, 0.9f), new Keyframe(1f, 0f)));
        Curve(output, "angleZ", AnimationCurve.Linear(0f, 0f, 1f, 160f));
        VfxGraphAuthoring.Setting(data, "boundsMode", "Manual");
        object bounds = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(init, "bounds"), "value");
        bounds.GetType().GetField("size").SetValue(bounds, new Vector3(2f, 2f, 1f));
        VfxGraphAuthoring.Value(init, "bounds", bounds);
        VfxGraphAuthoring.Save(GraphPath);
        VfxGraphAuthoring.AddIntensity(GraphPath);
    }

    private static void Curve(object output, string attribute, AnimationCurve curve)
    {
        object block = VfxGraphAuthoring.Create("Block.AttributeFromCurve");
        VfxGraphAuthoring.Setting(block, "attribute", attribute);
        VfxGraphAuthoring.Add(output, block);
        VfxGraphAuthoring.Set(VfxGraphAuthoring.Items(block, "inputSlots")[0], "value", curve);
    }

    private static void CreatePrefab()
    {
        if (File.Exists(PrefabPath))
        {
            return;
        }

        var root = new GameObject(Id);
        try
        {
            VfxEffect controller = root.AddComponent<VfxEffect>();
            var child = new GameObject("Graph - Enemy Spawn Portal");
            child.transform.SetParent(root.transform, false);
            VisualEffect graph = child.AddComponent<VisualEffect>();
            graph.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphPath);
            graph.initialEventName = "ControlledStart";
            BlockVfxBaker.SetObjects(controller, "_graphs", new Object[] { graph });
            using var data = new SerializedObject(controller);
            data.FindProperty("_releaseAfter").floatValue = 3f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}

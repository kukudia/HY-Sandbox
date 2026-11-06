// 职责：制作随残骸模型移动的局部空间包裹火焰，组合进集中库烟迹模板。
// 入口：BakeMissing()；已有图和 Prefab 子节点保留，可在 Graph 中继续调参。
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public static class DebrisFireBaker
{
    private const string Path = "Assets/Art/VFX/Graphs/DebrisEnvelope.vfx";
    public static void BakeMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Path) == null)
        {
            AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/Minimal_System.vfx", Path);
            object graph = VfxGraphAuthoring.Graph(Path);
            object[] contexts = VfxGraphAuthoring.Items(graph, "children");
            object spawn = contexts.First(c => c.GetType().Name == "VFXBasicSpawner");
            object init = contexts.First(c => c.GetType().Name == "VFXBasicInitialize");
            object output = contexts.First(c => c.GetType().Name == "VFXPlanarPrimitiveOutput");
            object rate = VfxGraphAuthoring.Create("VFXSpawnerConstantRate");
            VfxGraphAuthoring.Add(spawn, rate);
            VfxGraphAuthoring.Value(rate, "Rate", 60f);
            object data = VfxGraphAuthoring.Call(init, "GetData");
            VfxGraphAuthoring.Set(data, "space", System.Enum.Parse(VfxGraphAuthoring.Get(data, "space").GetType(), "Local"));
            VfxGraphAuthoring.Setting(data, "capacity", (uint)64);
            VfxGraphAuthoring.Attribute(init, "lifetime", 0.45f);
            object position = VfxGraphAuthoring.Attribute(init, "position", Vector3.zero);
            VfxGraphAuthoring.Setting(position, "Random", "PerComponent");
            object[] slots = VfxGraphAuthoring.Items(position, "inputSlots");
            VfxGraphAuthoring.Link(VfxGraphAuthoring.Parameter(graph, "FlameMin", Vector3.one * -0.3f), slots[0]);
            VfxGraphAuthoring.Link(VfxGraphAuthoring.Parameter(graph, "FlameMax", Vector3.one * 0.3f), slots[1]);
            VfxGraphAuthoring.Attribute(init, "velocity", new Vector3(0, 0.15f, 0));
            object angle = VfxGraphAuthoring.Attribute(init, "angleZ", 0f);
            VfxGraphAuthoring.Setting(angle, "Random", "Uniform");
            VfxGraphAuthoring.Set(VfxGraphAuthoring.Items(angle, "inputSlots")[0], "value", -180f);
            VfxGraphAuthoring.Set(VfxGraphAuthoring.Items(angle, "inputSlots")[1], "value", 180f);
            object size = VfxGraphAuthoring.Attribute(init, "size", 1f);
            VfxGraphAuthoring.Link(
                VfxGraphAuthoring.Parameter(graph, "FlameSize", 1f),
                VfxGraphAuthoring.Items(size, "inputSlots")[0]);
            VfxGraphAuthoring.Setting(output, "blendMode", "Additive");
            VfxGraphAuthoring.Setting(output, "uvMode", "Flipbook");
            object flipbook = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(output, "flipBookSize"), "value");
            flipbook.GetType().GetField("x").SetValue(flipbook, 16);
            flipbook.GetType().GetField("y").SetValue(flipbook, 8);
            VfxGraphAuthoring.Value(output, "flipBookSize", flipbook);
            VfxGraphAuthoring.Value(
                output,
                "mainTexture",
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/VFX/UNI/Realistic Explosions, Fire & Smoke/Textures/uni_fire.png"));
            VfxGraphAuthoring.Add(output, VfxGraphAuthoring.Create("Block.Orient"));
            Curve(output, "texIndex", AnimationCurve.Linear(0, 0, 1, 25));
            Curve(
                output,
                "alpha",
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.15f, 0.65f), new Keyframe(0.6f, 0.4f), new Keyframe(1, 0)));
            VfxGraphAuthoring.Attribute(output, "color", new Vector3(1.1f, 0.8f, 0.45f));
            BuildSmokeVeil(graph);
            VfxGraphAuthoring.Setting(data, "boundsMode", "Manual");
            object bounds = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(init, "bounds"), "value");
            bounds.GetType().GetField("size").SetValue(bounds, Vector3.one * 100f);
            VfxGraphAuthoring.Value(init, "bounds", bounds);
            VfxGraphAuthoring.Save(Path);
            VfxGraphAuthoring.AddIntensity(Path);
        }

        string prefabPath = CombatVfxBaker.Root + "/DebrisSmoke.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.transform.Find("Graph - DebrisEnvelope") != null)
            {
                return;
            }

            var child = new GameObject("Graph - DebrisEnvelope");
            child.transform.SetParent(root.transform, false);
            var effect = child.AddComponent<VisualEffect>();
            effect.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(Path);
            effect.initialEventName = "ControlledStart";
            BlockVfxBaker.SetObjects(
                root.GetComponent<VfxEffect>(),
                "_graphs",
                root.GetComponentsInChildren<VisualEffect>().Cast<Object>().ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void BuildSmokeVeil(object graph)
    {
        // 与头部火焰共用粒子，少量深烟遮叠亮焰，避免形成均匀发亮的外圈。
        object update = VfxGraphAuthoring.Items(graph, "children").First(c => c.GetType().Name == "VFXBasicUpdate");
        object output = VfxGraphAuthoring.Create("VFXPlanarPrimitiveOutput");
        VfxGraphAuthoring.Add(graph, output);
        VfxGraphAuthoring.Call(update, "LinkTo", output, 0, 0);
        VfxGraphAuthoring.Setting(output, "blendMode", "Alpha");
        VfxGraphAuthoring.Setting(output, "uvMode", "Flipbook");
        object flipbook = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(output, "flipBookSize"), "value");
        flipbook.GetType().GetField("x").SetValue(flipbook, 8);
        flipbook.GetType().GetField("y").SetValue(flipbook, 8);
        VfxGraphAuthoring.Value(output, "flipBookSize", flipbook);
        VfxGraphAuthoring.Value(
            output,
            "mainTexture",
            AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/VFX/UNI/Realistic Explosions, Fire & Smoke/Textures/uni_smoke_cloud.png"));
        VfxGraphAuthoring.Add(output, VfxGraphAuthoring.Create("Block.Orient"));
        Curve(output, "texIndex", AnimationCurve.Linear(0, 8, 1, 32));
        Curve(
            output,
            "alpha",
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.25f, 0.32f), new Keyframe(0.6f, 0.22f), new Keyframe(1, 0)));
        VfxGraphAuthoring.Attribute(output, "color", new Vector3(0.8f, 0.82f, 0.85f));
    }

    private static void Curve(object output, string name, AnimationCurve curve)
    {
        object block = VfxGraphAuthoring.Create("Block.AttributeFromCurve");
        VfxGraphAuthoring.Setting(block, "attribute", name);
        VfxGraphAuthoring.Add(output, block);
        VfxGraphAuthoring.Set(VfxGraphAuthoring.Items(block, "inputSlots")[0], "value", curve);
    }
}

// 职责：隔离验证工业动画共享材质、独立属性、高亮恢复、重配及启停生命周期。
// 入口：Run()，仅Play Mode；输出Temp/IndustrialMotion/checks.txt。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class IndustrialMotionChecks
{
    public static string Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode.");
        }

        var checks = new List<string>();
        var root = new GameObject("Industrial motion owned checks");
        var source = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        var highlight = new Material(source);
        float previousScale = Time.timeScale;
        try
        {
            Renderer first = Visual(root.transform, "First", source);
            Renderer second = Visual(root.transform, "Second", source);
            var a = first.gameObject.AddComponent<IndustrialPartMotion>();
            var b = second.gameObject.AddComponent<IndustrialPartMotion>();
            Configure(a, first, Color.cyan);
            Configure(b, second, Color.cyan);
            Material shared = first.sharedMaterial;
            Check(
                shared != source && shared == second.sharedMaterial,
                "Identical configuration shares one runtime material",
                checks);
            Check(
                !first.HasPropertyBlock() && !second.HasPropertyBlock(),
                "Shared renderers preserve batching without property blocks",
                checks);
            Color authored = source.GetColor("_EmissionColor");
            Time.timeScale = 0f;
            CallBinding("Tick", 0.25f);
            Check(
                first.sharedMaterial.GetColor("_EmissionColor").Equals(Color.cyan * 1.5f),
                "Pulse uses unscaled time while simulation is paused",
                checks);
            Check(
                source.GetColor("_EmissionColor").Equals(authored),
                "Authored source material is never mutated",
                checks);
            Material[] saved = (Material[])CallBinding("CaptureSelection", first);
            first.sharedMaterial = highlight;
            Configure(a, first, Color.red);
            Check(first.sharedMaterial != null, "Reconfiguration during selection keeps a valid material", checks);
            CallBinding("RestoreSelection", first, saved);
            Check(
                first.sharedMaterial != source && first.sharedMaterial.GetColor("_EmissionColor").r > 0f,
                "Selection restores new pulse after configuration changes",
                checks);
            a.enabled = false;
            Configure(b, second, Color.cyan);
            saved = (Material[])CallBinding("CaptureSelection", second);
            second.sharedMaterial = highlight;
            b.enabled = false;
            CallBinding("RestoreSelection", second, saved);
            Check(
                second.sharedMaterial == source,
                "Disabling selected motion cannot restore a released material",
                checks);
            b.enabled = true;
            Check(second.sharedMaterial != source, "Reenabled motion reacquires shared material", checks);
            saved = (Material[])CallBinding("CaptureSelection", second);
            second.sharedMaterial = highlight;
            CallBinding("RestoreSelection", second, saved);
            Check(
                second.sharedMaterial != source && second.sharedMaterial != highlight,
                "Selection resumes current pulse binding",
                checks);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", Color.green);
            second.SetPropertyBlock(block);
            typeof(IndustrialPartMotion).GetMethod("AnimateAll", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            second.GetPropertyBlock(block);
            Check(
                block.GetColor("_BaseColor").Equals(Color.green),
                "Late property block keeps unrelated values",
                checks);
            Check(
                block.HasColor(Shader.PropertyToID("_EmissionColor")),
                "Late property block still receives pulse",
                checks);
            Renderer third = Visual(root.transform, "Independent", source);
            block.Clear();
            block.SetColor("_BaseColor", Color.yellow);
            third.SetPropertyBlock(block);
            var c = third.gameObject.AddComponent<IndustrialPartMotion>();
            Configure(c, third, Color.magenta);
            typeof(IndustrialPartMotion).GetMethod("AnimateAll", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            third.GetPropertyBlock(block);
            Check(
                third.sharedMaterial == source && block.GetColor("_BaseColor").Equals(Color.yellow),
                "Existing property block uses compatible fallback",
                checks);
            b.enabled = false;
            Check(second.sharedMaterial == source, "Disabling restores authored material", checks);
            int count = ActiveCount();
            Object.DestroyImmediate(root);
            Check(ActiveCount() == count - 1, "Destroyed components leave scheduler immediately", checks);
            Directory.CreateDirectory("Temp/IndustrialMotion");
            string path = "Temp/IndustrialMotion/checks.txt";
            File.WriteAllLines(path, checks);
            return checks.Count + " checks passed: " + path;
        }
        finally
        {
            Time.timeScale = previousScale;
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }

            Object.DestroyImmediate(source);
            Object.DestroyImmediate(highlight);
        }
    }

    public static string RunConventional()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires conventional-rendering Play Mode.");
        }

        var checks = new List<string>();
        var root = new GameObject("Conventional motion owned checks");
        var source = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        try
        {
            Renderer renderer = Visual(root.transform, "Fallback", source);
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", Color.green);
            renderer.SetPropertyBlock(properties);
            var motion = renderer.gameObject.AddComponent<IndustrialPartMotion>();
            Configure(motion, renderer, Color.cyan);
            typeof(IndustrialPartMotion).GetMethod("AnimateAll", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
            renderer.GetPropertyBlock(properties);
            Check(
                renderer.sharedMaterial == source,
                "Conventional path retains original material and instancing",
                checks);
            Check(
                properties.GetColor("_BaseColor").Equals(Color.green),
                "Conventional path preserves external properties",
                checks);
            Check(
                properties.HasColor(Shader.PropertyToID("_EmissionColor")),
                "Conventional path updates pulse through property block",
                checks);
            motion.enabled = false;
            Check(renderer.sharedMaterial == source, "Conventional disable never replaces source", checks);
            Directory.CreateDirectory("Temp/IndustrialMotion");
            File.WriteAllLines("Temp/IndustrialMotion/conventional-checks.txt", checks);
            return checks.Count + " conventional checks passed";
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(source);
        }
    }

    private static object CallBinding(string method, params object[] arguments)
    {
        return typeof(IndustrialPartMotion).Assembly.GetType("IndustrialEmissionBinding").GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, arguments);
    }

    private static Renderer Visual(Transform parent, string name, Material source)
    {
        var owner = new GameObject(name);
        owner.transform.SetParent(parent, false);
        Renderer renderer = owner.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = source;
        return renderer;
    }

    private static void Configure(IndustrialPartMotion motion, Renderer renderer, Color color)
    {
        motion.Configure(
            Array.Empty<Transform>(),
            Vector3.up,
            0f,
            null,
            0f,
            1f,
            new[] { renderer },
            color,
            0.5f);
    }

    private static int ActiveCount()
    {
        return ((System.Collections.ICollection)typeof(IndustrialPartMotion).GetField("Active", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null)).Count;
    }

    private static void Check(bool condition, string description, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }

        checks.Add(description);
    }
}

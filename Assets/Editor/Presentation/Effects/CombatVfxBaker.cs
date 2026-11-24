// 职责：制作可编辑残骸烟迹、分层激光和炮弹曳光 Graph/Prefab，并接入集中库。
// 入口：BakeMissing()；已有图和模板不覆盖，调参在 VFX Graph / Prefab Inspector。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

public static class CombatVfxBaker
{
    public const string Root = "Assets/Art/BlockVisuals/VFX";
    private const string GraphRoot = "Assets/Art/VFX/Graphs";
    [MenuItem("Tools/HY Sandbox/资源管理/制作战斗特效")]
    public static void BakeMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        BuildGraph("DebrisSmoke", true);
        BuildGraph("CombatBeam", false);
        CreatePrefab("DebrisSmoke", "DebrisSmoke", 1f);
        DebrisFireBaker.BakeMissing();
        CreatePrefab("LaserBeam", "CombatBeam", 1f);
        BakeLaserEndpoints();
        CreatePrefab("ProjectileTracer", "CombatBeam", 0.7f);
        var library = AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>("Assets/Resources/VFX/BlockVfxLibrary.asset");
        if (library.DetachedSmoke == null || library.DetachedSmoke == BlockVfxBaker.Load("DetachedSmoke").GetComponent<VfxEffect>())
        {
            BlockVfxBaker.SetObject(library, "_detachedSmoke", BlockVfxBaker.Load("DebrisSmoke").GetComponent<VfxEffect>());
        }

        Register(library, "LaserBeam");
        Register(library, "LaserContact");
        Register(library, "ProjectileTracer");
        AssetDatabase.SaveAssetIfDirty(library);
        const string shellPath = "Assets/Art/Combat/TurretShell.prefab";
        GameObject shell = PrefabUtility.LoadPrefabContents(shellPath);
        try
        {
            if (shell.GetComponent<ProjectileTracer>() == null)
            {
                shell.AddComponent<ProjectileTracer>();
                PrefabUtility.SaveAsPrefabAsset(shell, shellPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(shell);
        }
    }

    private static void BakeLaserEndpoints()
    {
        const string laserPath = GraphRoot + "/LaserBeam.vfx";
        if (!File.Exists(laserPath))
        {
            if (!AssetDatabase.CopyAsset(GraphRoot + "/CombatBeam.vfx", laserPath))
            {
                throw new IOException("Cannot create laser beam graph.");
            }

            var mesh = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/CombatBeamMesh.asset"));
            mesh.name = "Laser beam with axial UVs";
            Vector3[] vertices = mesh.vertices;
            var uv = new Vector2[vertices.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                // VFX uses a repeat sampler; stay within the first and last texel centers.
                float alongBeam = vertices[i].y * 0.5f + 0.5f;
                uv[i] = new Vector2(0.5f, (alongBeam * 63f + 0.5f) / 64f);
            }

            mesh.uv = uv;
            AssetDatabase.CreateAsset(mesh, Root + "/LaserBeamMesh.asset");
            object graph = VfxGraphAuthoring.Graph(laserPath);
            object gradient = VfxGraphAuthoring.Parameter(
                graph,
                "TipGradient",
                SaveTexture(
                    "LaserTipGradient",
                    2,
                    64,
                    (_, y) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.87f, 1f, y))));
            foreach (object output in VfxGraphAuthoring.Items(graph, "children").Where(c => c.GetType().Name == "VFXMeshOutput"))
            {
                VfxGraphAuthoring.Value(output, "mesh", mesh);
                VfxGraphAuthoring.Link(gradient, VfxGraphAuthoring.Slot(output, "mainTexture"));
            }

            VfxGraphAuthoring.Save(laserPath);
        }

        const string prefabPath = Root + "/LaserBeam.prefab";
        GameObject laser = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            VisualEffect visual = laser.GetComponentInChildren<VisualEffect>();
            VisualEffectAsset laserGraph = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(laserPath);
            if (visual.visualEffectAsset == AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphRoot + "/CombatBeam.vfx"))
            {
                // Migrate only the stock graph reference; retain authored prefab overrides.
                visual.visualEffectAsset = laserGraph;
                PrefabUtility.SaveAsPrefabAsset(laser, prefabPath);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(laser);
        }

        Texture2D softDisc = SaveTexture(
            "LaserContactDisc",
            64,
            64,
            (x, y) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 1f, new Vector2(x * 2f - 1f, y * 2f - 1f).magnitude)));
        BuildContactGraph("LaserContact", softDisc, false);
        BuildContactGraph("LaserSparks", softDisc, true);
        const string contactPath = Root + "/LaserContact.prefab";
        if (File.Exists(contactPath))
        {
            return;
        }

        var root = new GameObject("LaserContact");
        try
        {
            VfxEffect controller = root.AddComponent<VfxEffect>();
            VisualEffect flash = AddContactGraph(root.transform, "LaserContact");
            VisualEffect sparks = AddContactGraph(root.transform, "LaserSparks");
            BlockVfxBaker.SetObjects(controller, "_graphs", new Object[] { flash, sparks });
            PrefabUtility.SaveAsPrefabAsset(root, contactPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static VisualEffect AddContactGraph(Transform parent, string name)
    {
        var node = new GameObject(name);
        node.transform.SetParent(parent, false);
        VisualEffect effect = node.AddComponent<VisualEffect>();
        effect.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphRoot + "/" + name + ".vfx");
        effect.initialEventName = "ControlledStart";
        return effect;
    }

    private static Texture2D SaveTexture(string name, int width, int height, Func<float, float, float> opacity)
    {
        string path = GraphRoot + "/" + name + ".asset";
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;
        }

        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float value = opacity(x / (width - 1f), y / (height - 1f));
                pixels[y * width + x] = new Color(value, value, value, 1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, path);
        return texture;
    }

    private static void BuildContactGraph(string name, Texture2D texture, bool sparks)
    {
        string path = GraphRoot + "/" + name + ".vfx";
        if (File.Exists(path))
        {
            return;
        }

        if (!AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/Minimal_System.vfx", path))
        {
            throw new IOException("Cannot create graph: " + path);
        }

        object graph = VfxGraphAuthoring.Graph(path);
        object[] contexts = VfxGraphAuthoring.Items(graph, "children");
        object spawn = contexts.First(c => c.GetType().Name == "VFXBasicSpawner");
        object init = contexts.First(c => c.GetType().Name == "VFXBasicInitialize");
        object output = contexts.First(c => c.GetType().Name == "VFXPlanarPrimitiveOutput");
        object data = VfxGraphAuthoring.Call(init, "GetData");
        VfxGraphAuthoring.Setting(data, "capacity", sparks ? 8u : 1u);
        VfxGraphAuthoring.Setting(data, "boundsMode", "Manual");
        object bounds = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(init, "bounds"), "value");
        bounds.GetType().GetField("size").SetValue(bounds, new Vector3(6f, 6f, 6f));
        VfxGraphAuthoring.Value(init, "bounds", bounds);
        object burst = VfxGraphAuthoring.Create("VFXSpawnerBurst");
        VfxGraphAuthoring.Add(spawn, burst);
        VfxGraphAuthoring.Value(burst, "Count", sparks ? 8f : 1f);
        VfxGraphAuthoring.Attribute(init, "position", Vector3.zero);
        VfxGraphAuthoring.Attribute(init, "lifetime", sparks ? 0.16f : 0.14f);
        VfxGraphAuthoring.Setting(output, "blendMode", "Additive");
        VfxGraphAuthoring.Value(output, "mainTexture", texture);
        object orient = VfxGraphAuthoring.Create("Block.Orient");
        VfxGraphAuthoring.Add(output, orient);
        VfxGraphAuthoring.Setting(orient, "mode", sparks ? "AlongVelocity" : "FaceCameraPlane");
        if (sparks)
        {
            object velocity = VfxGraphAuthoring.Attribute(init, "velocity", Vector3.zero);
            VfxGraphAuthoring.Setting(velocity, "Random", "PerComponent");
            object[] slots = VfxGraphAuthoring.Items(velocity, "inputSlots");
            VfxGraphAuthoring.Set(slots[0], "value", new Vector3(-13f, -17f, 3f));
            VfxGraphAuthoring.Set(slots[1], "value", new Vector3(13f, 17f, 6f));
            VfxGraphAuthoring.Attribute(output, "scaleX", 0.14f);
            VfxGraphAuthoring.Attribute(output, "scaleY", 0.75f);
            VfxGraphAuthoring.Attribute(output, "color", new Vector3(8f, 2.8f, 0.45f));
            Curve(output, "alpha", AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));
        }
        else
        {
            VfxGraphAuthoring.Attribute(output, "color", new Vector3(8f, 3.6f, 1.35f));
            Curve(
                output,
                "size",
                new AnimationCurve(new Keyframe(0f, 0.85f), new Keyframe(0.2f, 1f), new Keyframe(1f, 0.65f)));
            Curve(
                output,
                "alpha",
                new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.36f, 0.8f), new Keyframe(1f, 0f)));
        }

        VfxGraphAuthoring.Save(path);
        VfxGraphAuthoring.AddIntensity(path);
    }

    private static void Register(BlockVfxLibrary library, string id)
    {
        using var data = new SerializedObject(library);
        var entries = data.FindProperty("_attached");
        for (int i = 0; i < entries.arraySize; i++)
        {
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == id)
            {
                var reference = entries.GetArrayElementAtIndex(i).FindPropertyRelative("prefab");
                if (reference.objectReferenceValue == null)
                {
                    reference.objectReferenceValue = BlockVfxBaker.Load(id).GetComponent<VfxEffect>();
                    data.ApplyModifiedPropertiesWithoutUndo();
                }

                return;
            }
        }

        int index = entries.arraySize++;
        var entry = entries.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("id").stringValue = id;
        entry.FindPropertyRelative("prefab").objectReferenceValue = BlockVfxBaker.Load(id).GetComponent<VfxEffect>();
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePrefab(string name, string graphName, float glow)
    {
        string path = Root + "/" + name + ".prefab";
        if (File.Exists(path))
        {
            return;
        }

        var root = new GameObject(name);
        try
        {
            VfxEffect controller = root.AddComponent<VfxEffect>();
            var child = new GameObject("Graph - " + graphName);
            child.transform.SetParent(root.transform, false);
            VisualEffect effect = child.AddComponent<VisualEffect>();
            effect.visualEffectAsset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(GraphRoot + "/" + graphName + ".vfx");
            effect.initialEventName = "ControlledStart";
            if (effect.HasFloat("Glow"))
            {
                effect.SetFloat("Glow", glow);
            }

            BlockVfxBaker.SetObjects(controller, "_graphs", new Object[] { effect });
            using var data = new SerializedObject(controller);
            data.FindProperty("_releaseAfter").floatValue = graphName == "DebrisSmoke" ? 4f : 1f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildGraph(string name, bool smoke)
    {
        string path = GraphRoot + "/" + name + ".vfx";
        if (File.Exists(path))
        {
            return;
        }

        if (!AssetDatabase.CopyAsset("Packages/com.unity.visualeffectgraph/Editor/Templates/Minimal_System.vfx", path))
        {
            throw new IOException("Cannot create graph: " + path);
        }

        object graph = VfxGraphAuthoring.Graph(path);
        object[] contexts = VfxGraphAuthoring.Items(graph, "children");
        object spawn = contexts.First(c => c.GetType().Name == "VFXBasicSpawner");
        object init = contexts.First(c => c.GetType().Name == "VFXBasicInitialize");
        object update = contexts.First(c => c.GetType().Name == "VFXBasicUpdate");
        object output = contexts.First(c => c.GetType().Name == "VFXPlanarPrimitiveOutput");
        object data = VfxGraphAuthoring.Call(init, "GetData");
        VfxGraphAuthoring.Set(
            data,
            "space",
            Enum.Parse(VfxGraphAuthoring.Get(data, "space").GetType(), smoke ? "World" : "Local"));
        VfxGraphAuthoring.Setting(data, "capacity", (uint)(smoke ? 192 : 1));
        object emitter = VfxGraphAuthoring.Create(smoke ? "VFXSpawnerConstantRate" : "VFXSpawnerBurst");
        VfxGraphAuthoring.Add(spawn, emitter);
        VfxGraphAuthoring.Value(emitter, smoke ? "Rate" : "Count", smoke ? 38f : 1f);
        if (smoke)
        {
            VfxGraphAuthoring.Link(VfxGraphAuthoring.Parameter(graph, "SpawnRate", 38f), VfxGraphAuthoring.Slot(emitter, "Rate"));
            BuildSmoke(graph, init, output);
            BuildResidualFire(graph, update);
        }
        else
        {
            VfxGraphAuthoring.Call(update, "UnlinkTo", output, 0, 0);
            VfxGraphAuthoring.Call(graph, "RemoveChild", output, true);
            BuildBeam(graph, init, update);
        }

        VfxGraphAuthoring.Setting(data, "boundsMode", "Manual");
        object bounds = VfxGraphAuthoring.Get(VfxGraphAuthoring.Slot(init, "bounds"), "value");
        bounds.GetType().GetField("size").SetValue(bounds, smoke ? new Vector3(160, 80, 160) : new Vector3(8, 8, 4));
        VfxGraphAuthoring.Value(init, "bounds", bounds);
        VfxGraphAuthoring.Save(path);
        VfxGraphAuthoring.AddIntensity(path);
    }

    private static void BuildSmoke(object graph, object init, object output)
    {
        VfxGraphAuthoring.Attribute(init, "lifetime", 2.8f);
        object position = VfxGraphAuthoring.Attribute(init, "position", Vector3.zero);
        object slot = VfxGraphAuthoring.Items(position, "inputSlots")[0];
        VfxGraphAuthoring.Set(slot, "space", Enum.Parse(VfxGraphAuthoring.Get(slot, "space").GetType(), "Local"));
        object velocity = VfxGraphAuthoring.Attribute(init, "velocity", Vector3.zero);
        VfxGraphAuthoring.Setting(velocity, "Random", "PerComponent");
        object[] velocities = VfxGraphAuthoring.Items(velocity, "inputSlots");
        VfxGraphAuthoring.Set(velocities[0], "value", new Vector3(-0.25f, 0.35f, -0.25f));
        VfxGraphAuthoring.Set(velocities[1], "value", new Vector3(0.25f, 0.8f, 0.25f));
        VfxGraphAuthoring.Attribute(init, "size", 1f);
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
        Curve(
            output,
            "size",
            new AnimationCurve(new Keyframe(0, 0.65f), new Keyframe(0.3f, 1.35f), new Keyframe(1, 2.4f)));
        Curve(
            output,
            "alpha",
            new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.06f, 0.75f), new Keyframe(0.55f, 0.52f), new Keyframe(1, 0)));
        Curve(output, "texIndex", AnimationCurve.Linear(0, 8, 1, 52));
        VfxGraphAuthoring.Attribute(output, "color", new Vector3(1.5f, 1.53f, 1.55f));
    }

    private static void BuildResidualFire(object graph, object update)
    {
        // 共用烟粒的世界空间轨迹，只在出生后约 0.5 秒绘制火光；不增加粒子或动态灯。
        object output = VfxGraphAuthoring.Create("VFXPlanarPrimitiveOutput");
        VfxGraphAuthoring.Add(graph, output);
        VfxGraphAuthoring.Call(update, "LinkTo", output, 0, 0);
        VfxGraphAuthoring.Setting(output, "blendMode", "Additive");
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
        Curve(
            output,
            "size",
            new AnimationCurve(
                new Keyframe(0, 0.28f),
                new Keyframe(0.08f, 0.52f),
                new Keyframe(0.18f, 0.2f),
                new Keyframe(1, 0.2f)));
        Curve(
            output,
            "alpha",
            new AnimationCurve(
                new Keyframe(0, 0),
                new Keyframe(0.015f, 1f),
                new Keyframe(0.08f, 0.65f),
                new Keyframe(0.18f, 0),
                new Keyframe(1, 0)));
        Curve(output, "texIndex", AnimationCurve.Linear(0, 8, 1, 52));
        VfxGraphAuthoring.Attribute(output, "color", new Vector3(0.5f, 0.12f, 0.012f));
    }

    private static void Curve(object output, string attribute, AnimationCurve curve)
    {
        object block = VfxGraphAuthoring.Create("Block.AttributeFromCurve");
        VfxGraphAuthoring.Setting(block, "attribute", attribute);
        VfxGraphAuthoring.Add(output, block);
        VfxGraphAuthoring.Set(VfxGraphAuthoring.Items(block, "inputSlots")[0], "value", curve);
    }

    private static void BuildBeam(object graph, object init, object update)
    {
        VfxGraphAuthoring.Attribute(init, "lifetime", 100000f);
        VfxGraphAuthoring.Attribute(init, "size", 1f);
        VfxGraphAuthoring.Attribute(init, "position", new Vector3(0, 0, 0.5f));
        VfxGraphAuthoring.Attribute(init, "angleX", 90f);
        VfxGraphAuthoring.Attribute(init, "scaleY", 0.5f);
        Mesh mesh = CreateBeamMesh();
        object tint = VfxGraphAuthoring.Parameter(graph, "Tint", new Vector3(6f, 1.1f, 0.15f));
        object glow = VfxGraphAuthoring.Parameter(graph, "Glow", 1f);
        for (int i = 0; i < 3; i++)
        {
            object output = VfxGraphAuthoring.Create("VFXMeshOutput");
            VfxGraphAuthoring.Add(graph, output);
            VfxGraphAuthoring.Call(update, "LinkTo", output, 0, 0);
            VfxGraphAuthoring.Setting(output, "blendMode", "Additive");
            VfxGraphAuthoring.Value(output, "mesh", mesh);
            VfxGraphAuthoring.Value(output, "mainTexture", Texture2D.whiteTexture);
            VfxGraphAuthoring.Attribute(output, "scaleX", i == 0 ? 0.7f : i == 1 ? 1.8f : 3.5f);
            VfxGraphAuthoring.Attribute(output, "scaleZ", i == 0 ? 0.7f : i == 1 ? 1.8f : 3.5f);
            object color = VfxGraphAuthoring.Attribute(output, "color", i == 0 ? new Vector3(5f, 4.3f, 3.4f) : Vector3.one);
            if (i > 0)
            {
                VfxGraphAuthoring.Link(tint, VfxGraphAuthoring.Items(color, "inputSlots")[0]);
                object alpha = VfxGraphAuthoring.Attribute(output, "alpha", 1f);
                VfxGraphAuthoring.Link(glow, VfxGraphAuthoring.Items(alpha, "inputSlots")[0]);
                VfxGraphAuthoring.Attribute(output, "alpha", i == 1 ? 0.6f : 0.14f, true);
            }
        }
    }

    private static Mesh CreateBeamMesh()
    {
        string path = Root + "/CombatBeamMesh.asset";
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing != null)
        {
            return existing;
        }

        const int sides = 12;
        float[] heights =
        {
            -1f,
            -0.97f,
            0.97f,
            1f
        };
        float[] radii =
        {
            0f,
            0.5f,
            0.5f,
            0f
        };
        var vertices = new Vector3[sides * heights.Length];
        var triangles = new int[sides * 3 * 6];
        for (int ring = 0; ring < heights.Length; ring++)
        {
            for (int side = 0; side < sides; side++)
            {
                float angle = side * Mathf.PI * 2 / sides;
                vertices[ring * sides + side] = new Vector3(Mathf.Cos(angle) * radii[ring], heights[ring], Mathf.Sin(angle) * radii[ring]);
                if (ring == heights.Length - 1)
                {
                    continue;
                }

                int a = ring * sides + side;
                int b = ring * sides + (side + 1) % sides;
                int index = (ring * sides + side) * 6;
                triangles[index] = a;
                triangles[index + 1] = a + sides;
                triangles[index + 2] = b;
                triangles[index + 3] = b;
                triangles[index + 4] = a + sides;
                triangles[index + 5] = b + sides;
            }
        }

        var mesh = new Mesh
        {
            name = "Tapered combat beam",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }
}

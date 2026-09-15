using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class IndustrialArtGenerator
{
    private const string ArtRoot = "Assets/Art/Industrial";
    private const string MaterialRoot = ArtRoot + "/Materials";
    private const string MeshRoot = ArtRoot + "/Meshes";
    private const string PreviewRoot = ArtRoot + "/Preview";
    private const string BlockRoot = "Assets/Resources/Blocks";
    private const string LegacyConnectorPrefabPath = "Assets/Connector.prefab";
    private const string ResourcesConnectorPrefabPath = BlockRoot + "/Connector.prefab";
    private const string GeneratedRootName = "IndustrialVisual";
    private const bool EnableLod = false;

    private static readonly Color CyanEmission = new Color(0.03f, 1.65f, 2.7f, 1f);
    private static readonly Color AmberEmission = new Color(2.8f, 0.72f, 0.06f, 1f);
    private static readonly Color RedEmission = new Color(2.6f, 0.12f, 0.035f, 1f);

    private static Material _graphite;
    private static Material _armor;
    private static Material _edge;
    private static Material _cyan;
    private static Material _amber;
    private static Material _red;
    private static Material _glass;
    private static Material _powerPaint;
    private static Material _thrusterPaint;
    private static Material _weaponPaint;
    private static Material _cockpitPaint;
    private static Material _utilityPaint;
    private static Mesh _roundedCube;
    private static Mesh _cylinder;
    private static Mesh _sphere;
    private static Mesh _torus;
    private static Mesh _cone;
    private static Mesh _wedge;

    [MenuItem("Tools/HY Sandbox/Rebuild Industrial Art")]
    public static void RebuildAll()
    {
        EnsureFolders();
        CreateSharedMeshes();
        CreateSharedMaterials();
        ConfigureRenderStyle();
        RebuildConnectorPrefab();

        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { BlockRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => Path.GetFileNameWithoutExtension(path) == "Bot" ? 0 : 1)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();

        int updated = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string prefabPath in prefabPaths)
            {
                if (RebuildPrefab(prefabPath))
                {
                    updated++;
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        CreatePreviewScene(prefabPaths);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Industrial art rebuilt: {updated} prefabs, shared materials/meshes, render profile and preview scene.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Art");
        EnsureFolder("Assets/Art", "Industrial");
        EnsureFolder(ArtRoot, "Materials");
        EnsureFolder(ArtRoot, "Meshes");
        EnsureFolder(ArtRoot, "Preview");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void CreateSharedMeshes()
    {
        _roundedCube = SaveOrUpdateMesh(MeshRoot + "/M_RoundedCube.asset", CreateRoundedCube(4, 0.12f));
        _cylinder = SaveOrUpdateMesh(MeshRoot + "/M_LowCylinder.asset", ClonePrimitiveMesh(PrimitiveType.Cylinder));
        _sphere = SaveOrUpdateMesh(MeshRoot + "/M_LowSphere.asset", ClonePrimitiveMesh(PrimitiveType.Sphere));
        _torus = SaveOrUpdateMesh(MeshRoot + "/M_Torus.asset", CreateTorus(18, 6, 0.5f, 0.09f));
        _cone = SaveOrUpdateMesh(MeshRoot + "/M_Cone.asset", CreateCone(14));
        _wedge = SaveOrUpdateMesh(MeshRoot + "/M_Wedge.asset", CreateWedge());
    }

    private static Mesh SaveOrUpdateMesh(string path, Mesh generated)
    {
        generated.name = Path.GetFileNameWithoutExtension(path);
        Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(generated, path);
            return generated;
        }

        EditorUtility.CopySerialized(generated, existing);
        UnityEngine.Object.DestroyImmediate(generated);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    private static Mesh ClonePrimitiveMesh(PrimitiveType type)
    {
        GameObject temporary = GameObject.CreatePrimitive(type);
        Mesh mesh = UnityEngine.Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
        UnityEngine.Object.DestroyImmediate(temporary);
        return mesh;
    }

    private static Mesh CreateRoundedCube(int segments, float radius)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        float inner = 0.5f - radius;

        for (int face = 0; face < 6; face++)
        {
            int start = vertices.Count;
            for (int y = 0; y <= segments; y++)
            {
                float v = Mathf.Lerp(-0.5f, 0.5f, y / (float)segments);
                for (int x = 0; x <= segments; x++)
                {
                    float u = Mathf.Lerp(-0.5f, 0.5f, x / (float)segments);
                    Vector3 point = FacePoint(face, u, v);
                    Vector3 nearest = new Vector3(
                        Mathf.Clamp(point.x, -inner, inner),
                        Mathf.Clamp(point.y, -inner, inner),
                        Mathf.Clamp(point.z, -inner, inner));
                    Vector3 normal = (point - nearest).normalized;
                    vertices.Add(nearest + normal * radius);
                    normals.Add(normal);
                    uvs.Add(new Vector2(x / (float)segments, y / (float)segments));
                }
            }

            for (int y = 0; y < segments; y++)
            {
                for (int x = 0; x < segments; x++)
                {
                    int a = start + y * (segments + 1) + x;
                    int b = a + 1;
                    int c = a + segments + 1;
                    int d = c + 1;
                    AddFacingQuad(vertices, normals, triangles, a, b, d, c);
                }
            }
        }

        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            normals = normals.ToArray(),
            uv = uvs.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 FacePoint(int face, float u, float v)
    {
        return face switch
        {
            0 => new Vector3(0.5f, v, -u),
            1 => new Vector3(-0.5f, v, u),
            2 => new Vector3(u, 0.5f, -v),
            3 => new Vector3(u, -0.5f, v),
            4 => new Vector3(u, v, 0.5f),
            _ => new Vector3(-u, v, -0.5f)
        };
    }

    private static void AddFacingQuad(
        IReadOnlyList<Vector3> vertices,
        IReadOnlyList<Vector3> normals,
        ICollection<int> triangles,
        int a,
        int b,
        int c,
        int d)
    {
        Vector3 cross = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
        Vector3 normal = (normals[a] + normals[b] + normals[c] + normals[d]).normalized;
        if (Vector3.Dot(cross, normal) >= 0f)
        {
            triangles.Add(a);
            triangles.Add(b);
            triangles.Add(c);
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);
        }
        else
        {
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(b);
            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(c);
        }
    }

    private static Mesh CreateTorus(int ringSegments, int tubeSegments, float radius, float tubeRadius)
    {
        var vertices = new Vector3[ringSegments * tubeSegments];
        var normals = new Vector3[vertices.Length];
        var uvs = new Vector2[vertices.Length];
        var triangles = new int[ringSegments * tubeSegments * 6];

        for (int ring = 0; ring < ringSegments; ring++)
        {
            float ringAngle = ring * Mathf.PI * 2f / ringSegments;
            Vector3 center = new Vector3(Mathf.Cos(ringAngle) * radius, 0f, Mathf.Sin(ringAngle) * radius);
            Vector3 radial = center.normalized;
            for (int tube = 0; tube < tubeSegments; tube++)
            {
                float tubeAngle = tube * Mathf.PI * 2f / tubeSegments;
                Vector3 normal = radial * Mathf.Cos(tubeAngle) + Vector3.up * Mathf.Sin(tubeAngle);
                int index = ring * tubeSegments + tube;
                vertices[index] = center + normal * tubeRadius;
                normals[index] = normal;
                uvs[index] = new Vector2(ring / (float)ringSegments, tube / (float)tubeSegments);
            }
        }

        int triangleIndex = 0;
        for (int ring = 0; ring < ringSegments; ring++)
        {
            int nextRing = (ring + 1) % ringSegments;
            for (int tube = 0; tube < tubeSegments; tube++)
            {
                int nextTube = (tube + 1) % tubeSegments;
                int a = ring * tubeSegments + tube;
                int b = nextRing * tubeSegments + tube;
                int c = nextRing * tubeSegments + nextTube;
                int d = ring * tubeSegments + nextTube;
                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = d;
                triangles[triangleIndex++] = c;
            }
        }

        var mesh = new Mesh
        {
            vertices = vertices,
            normals = normals,
            uv = uvs,
            triangles = triangles
        };
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateCone(int segments)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        vertices.Add(new Vector3(0f, 0.5f, 0f));
        vertices.Add(new Vector3(0f, -0.5f, 0f));
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices.Add(new Vector3(Mathf.Cos(angle) * 0.5f, -0.5f, Mathf.Sin(angle) * 0.5f));
        }

        for (int i = 0; i < segments; i++)
        {
            int current = 2 + i;
            int next = 2 + (i + 1) % segments;
            triangles.Add(0);
            triangles.Add(next);
            triangles.Add(current);
            triangles.Add(1);
            triangles.Add(current);
            triangles.Add(next);
        }

        var mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Mesh CreateWedge()
    {
        Vector3[] vertices =
        {
            new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f), new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.36f, 0.5f, -0.32f), new Vector3(0.36f, 0.5f, -0.32f),
            new Vector3(-0.22f, 0.5f, 0.38f), new Vector3(0.22f, 0.5f, 0.38f)
        };
        int[] triangles =
        {
            0, 1, 3, 0, 3, 2,
            4, 6, 7, 4, 7, 5,
            0, 4, 5, 0, 5, 1,
            2, 3, 7, 2, 7, 6,
            0, 2, 6, 0, 6, 4,
            1, 5, 7, 1, 7, 3
        };
        var mesh = new Mesh { vertices = vertices, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void CreateSharedMaterials()
    {
        // Flat, high-contrast cartoon colors still borrow from real materials: painted steel, safety
        // yellow, copper, aluminum, glass and warning red. Keeping these as shared materials preserves
        // batching while allowing each functional category to read at a glance.
        _graphite = CreateOrUpdateMaterial("MAT_Industrial_Graphite", new Color(0.12f, 0.19f, 0.24f), 0.68f, 0.38f, Color.black);
        _armor = CreateOrUpdateMaterial("MAT_Industrial_Armor", new Color(0.78f, 0.48f, 0.08f), 0.34f, 0.44f, Color.black);
        _edge = CreateOrUpdateMaterial("MAT_Industrial_Edge", new Color(0.62f, 0.68f, 0.7f), 0.76f, 0.58f, Color.black);
        _cyan = CreateOrUpdateMaterial("MAT_Industrial_Cyan", new Color(0.02f, 0.42f, 0.26f), 0.24f, 0.62f, new Color(0.03f, 1.4f, 0.62f, 1f));
        _amber = CreateOrUpdateMaterial("MAT_Industrial_Amber", new Color(0.68f, 0.24f, 0.035f), 0.28f, 0.5f, new Color(2.7f, 0.46f, 0.04f, 1f));
        _red = CreateOrUpdateMaterial("MAT_Industrial_Red", new Color(0.58f, 0.045f, 0.025f), 0.25f, 0.46f, new Color(2.5f, 0.1f, 0.03f, 1f));
        _glass = CreateOrUpdateMaterial("MAT_Industrial_Glass", new Color(0.06f, 0.35f, 0.5f, 0.42f), 0.05f, 0.86f, new Color(0.02f, 0.44f, 0.72f, 1f), true);
        _powerPaint = CreateOrUpdateMaterial("MAT_Industrial_PowerPaint", new Color(0.1f, 0.42f, 0.25f), 0.36f, 0.44f, Color.black);
        _thrusterPaint = CreateOrUpdateMaterial("MAT_Industrial_ThrusterPaint", new Color(0.68f, 0.23f, 0.045f), 0.42f, 0.42f, Color.black);
        _weaponPaint = CreateOrUpdateMaterial("MAT_Industrial_WeaponPaint", new Color(0.5f, 0.06f, 0.045f), 0.38f, 0.4f, Color.black);
        _cockpitPaint = CreateOrUpdateMaterial("MAT_Industrial_CockpitPaint", new Color(0.07f, 0.24f, 0.48f), 0.46f, 0.48f, Color.black);
        _utilityPaint = CreateOrUpdateMaterial("MAT_Industrial_UtilityPaint", new Color(0.62f, 0.5f, 0.2f), 0.3f, 0.42f, Color.black);
    }

    private static Material CreateOrUpdateMaterial(
        string name,
        Color baseColor,
        float metallic,
        float smoothness,
        Color emission,
        bool transparent = false)
    {
        string path = MaterialRoot + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            throw new InvalidOperationException("Universal Render Pipeline/Lit shader is unavailable.");
        }

        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetColor("_BaseColor", baseColor);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.enableInstancing = true;
        if (emission.maxColorComponent > 0.001f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else
        {
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }

        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }
        else
        {
            material.SetFloat("_Surface", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            material.SetFloat("_ZWrite", 1f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = -1;
        }

        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureRenderStyle()
    {
        ConfigureVolume("Assets/Settings/SampleSceneProfile.asset");

        var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/PC_RPAsset.asset");
        if (pipelineAsset != null)
        {
            pipelineAsset.msaaSampleCount = 2;
            pipelineAsset.shadowDistance = 55f;
            EditorUtility.SetDirty(pipelineAsset);
        }
    }

    private static void ConfigureVolume(string path)
    {
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (profile == null)
        {
            return;
        }

        Bloom bloom = GetOrAdd<Bloom>(profile);
        bloom.active = true;
        Set(bloom.threshold, 0.9f);
        Set(bloom.intensity, 0.62f);
        Set(bloom.scatter, 0.66f);
        Set(bloom.highQualityFiltering, false);

        Tonemapping tonemapping = GetOrAdd<Tonemapping>(profile);
        tonemapping.active = true;
        Set(tonemapping.mode, TonemappingMode.ACES);

        ColorAdjustments color = GetOrAdd<ColorAdjustments>(profile);
        color.active = true;
        Set(color.postExposure, 0.12f);
        Set(color.contrast, 11f);
        Set(color.saturation, -4f);
        Set(color.colorFilter, new Color(0.96f, 0.985f, 1f, 1f));

        Vignette vignette = GetOrAdd<Vignette>(profile);
        vignette.active = true;
        Set(vignette.intensity, 0.18f);
        Set(vignette.smoothness, 0.34f);

        MotionBlur motionBlur = GetOrAdd<MotionBlur>(profile);
        motionBlur.active = true;
        Set(motionBlur.intensity, 0.08f);
        Set(motionBlur.clamp, 0.03f);
        EditorUtility.SetDirty(profile);
    }

    private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
    {
        return profile.TryGet(out T component) ? component : profile.Add<T>(true);
    }

    private static void Set<T>(VolumeParameter<T> parameter, T value)
    {
        parameter.overrideState = true;
        parameter.value = value;
    }

    private static void RebuildConnectorPrefab()
    {
        string prefabPath = File.Exists(LegacyConnectorPrefabPath)
            ? LegacyConnectorPrefabPath
            : ResourcesConnectorPrefabPath;
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        if (root == null)
        {
            Debug.LogWarning($"Connector prefab not found at {LegacyConnectorPrefabPath} or {ResourcesConnectorPrefabPath}.");
            return;
        }

        try
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            }

            Transform visual = CreateGroup(root.transform, "IndustrialConnectorVisual");
            // The connector is spawned directly on a face and has no normal-aware rotation in Block.cs,
            // so the visual is intentionally rotationally symmetric and reads correctly on every face.
            AddPart(visual, "Connector Hub", _cylinder, Vector3.zero,
                new Vector3(0.17f, 0.075f, 0.17f), Quaternion.identity, _graphite);
            AddPart(visual, "Connector Collar", _cylinder, Vector3.zero,
                new Vector3(0.14f, 0.035f, 0.14f), Quaternion.identity, _edge);

            Transform signalRing = CreateGroup(visual, "Connector Signal Ring");
            Renderer ring = AddPart(signalRing, "Signal Ring", _torus, Vector3.zero,
                Vector3.one * 0.28f, Quaternion.identity, _cyan, false);
            Renderer contact = AddPart(visual, "Contact Core", _sphere, Vector3.zero,
                Vector3.one * 0.09f, Quaternion.identity, _cyan, false);
            AddPart(visual, "Contact Cap", _cylinder, Vector3.zero,
                new Vector3(0.065f, 0.025f, 0.065f), Quaternion.identity, _amber, false);

            var motion = visual.gameObject.AddComponent<IndustrialPartMotion>();
            motion.Configure(new[] { signalRing }, Vector3.up, 110f, null, 0f, 1f,
                new Renderer[] { ring, contact }, CyanEmission, 0.2f);
            SetLayerRecursively(visual.gameObject, root.layer);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static bool RebuildPrefab(string prefabPath)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.name == "Bot")
            {
                RebuildBot(root);
            }
            else
            {
                Block block = root.GetComponent<Block>();
                Transform model = root.transform.Find("Model");
                if (block == null || model == null)
                {
                    return false;
                }

                RemovePlaceholderRenderers(model);
                BuildBlockVisual(root.name, block, model);
            }

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void RemovePlaceholderRenderers(Transform model)
    {
        for (int i = model.childCount - 1; i >= 0; i--)
        {
            Transform child = model.GetChild(i);
            if (child.name == GeneratedRootName)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
                continue;
            }

            bool isPlainVisual = child.GetComponentInChildren<Renderer>(true) != null
                && child.GetComponentInChildren<Block>(true) == null
                && child.GetComponentInChildren<RepairBot>(true) == null;
            if (isPlainVisual)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void BuildBlockVisual(string name, Block block, Transform model)
    {
        var visual = new GameObject(GeneratedRootName);
        visual.transform.SetParent(model, false);
        SetLayerRecursively(visual, model.gameObject.layer);

        // LOD is intentionally disabled while the cartoon palette and silhouettes are being tuned.
        // Keeping one active visual hierarchy also makes close-range material authoring deterministic.
        bool useLod = ShouldUseLod(name, block);
        Transform lod0 = CreateGroup(visual.transform, "LOD0");
        var spinTargets = new List<Transform>();
        var glowRenderers = new List<Renderer>();
        List<Renderer> lod0Renderers = BuildDetailedModel(name, block, lod0, spinTargets, glowRenderers);
        ApplyCategoryPalette(name, lod0);

        if (useLod)
        {
            Transform lod1 = CreateGroup(visual.transform, "LOD1");
            List<Renderer> lod1Renderers = BuildSimpleModel(name, block, lod1);
            var lodGroup = visual.AddComponent<LODGroup>();
            lodGroup.fadeMode = LODFadeMode.CrossFade;
            lodGroup.animateCrossFading = false;
            lodGroup.SetLODs(new[]
            {
                // Screen-relative heights are intentionally conservative: keep the detailed silhouette
                // visible longer so the low-poly swap is not noticeable at normal build distances.
                new LOD(0.10f, lod0Renderers.ToArray()) { fadeTransitionWidth = 0.08f },
                new LOD(0.025f, lod1Renderers.ToArray()) { fadeTransitionWidth = 0.06f },
                new LOD(0.006f, Array.Empty<Renderer>())
            });
            lodGroup.RecalculateBounds();
        }

        if (spinTargets.Count > 0)
        {
            var motion = visual.AddComponent<IndustrialPartMotion>();
            Transform bobTarget = name is "PowerGeneratingUnit" or "PowerTransmissionDevice" or "HoverFlightController"
                ? lod0
                : null;
            Color emission = name.Contains("Thruster", StringComparison.Ordinal) ? AmberEmission : CyanEmission;
            motion.Configure(
                spinTargets.ToArray(),
                Vector3.up,
                name.Contains("Thruster", StringComparison.Ordinal) ? 64f : 24f,
                bobTarget,
                bobTarget != null ? 0.025f : 0f,
                1.1f,
                glowRenderers.ToArray(),
                emission,
                0.16f);
        }
    }

    private static bool ShouldUseLod(string name, Block block)
    {
        return EnableLod && (block.x * block.y * block.z >= 8
            || name is "PowerGeneratingUnit"
            or "PowerTransmissionDevice"
            or "HoverFlightController"
            or "Turret"
            or "RepairBotContianer"
            || name.Contains("Thruster", StringComparison.Ordinal));
    }

    private static void ApplyCategoryPalette(string name, Transform visualRoot)
    {
        Material categoryMaterial = name switch
        {
            "Cockpit" => _cockpitPaint,
            "PowerGeneratingUnit" or "PowerTransmissionDevice" or "HoverFlightController" => _powerPaint,
            "Turret" => _weaponPaint,
            "Door" or "Stairs" or "Rack" or "RepairBotContianer" => _utilityPaint,
            _ when name.Contains("Thruster", StringComparison.Ordinal) => _thrusterPaint,
            _ => _graphite
        };

        foreach (MeshRenderer renderer in visualRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (renderer.sharedMaterial == _graphite)
            {
                renderer.sharedMaterial = categoryMaterial;
            }
        }
    }

    private static List<Renderer> BuildDetailedModel(
        string name,
        Block block,
        Transform parent,
        ICollection<Transform> spinTargets,
        ICollection<Renderer> glowRenderers)
    {
        var renderers = new List<Renderer>();
        switch (name)
        {
            case "Door":
                BuildDoor(parent, block, renderers, glowRenderers);
                break;
            case "Stairs":
                BuildStairs(parent, renderers, glowRenderers);
                break;
            case "Rack":
                BuildRack(parent, renderers, glowRenderers);
                break;
            case "Cockpit":
                BuildCockpit(parent, block, renderers, glowRenderers);
                break;
            case "PowerGeneratingUnit":
                BuildGenerator(parent, block, renderers, spinTargets, glowRenderers);
                break;
            case "PowerTransmissionDevice":
                BuildRelay(parent, block, renderers, spinTargets, glowRenderers);
                break;
            case "HoverFlightController":
                BuildFlightController(parent, renderers, spinTargets, glowRenderers);
                break;
            case "Turret":
                BuildTurret(parent, renderers, spinTargets, glowRenderers);
                break;
            case "RepairBotContianer":
                BuildRepairBay(parent, renderers, glowRenderers);
                break;
            default:
                if (name.Contains("Thruster", StringComparison.Ordinal))
                {
                    BuildThruster(parent, block, name, renderers, spinTargets, glowRenderers);
                }
                else
                {
                    BuildStructural(parent, block, renderers, glowRenderers);
                }
                break;
        }

        return renderers;
    }

    private static List<Renderer> BuildSimpleModel(string name, Block block, Transform parent)
    {
        var renderers = new List<Renderer>();
        Vector3 size = new Vector3(block.x, block.y, block.z) * 0.88f;
        renderers.Add(AddPart(parent, "Silhouette", _roundedCube, Vector3.zero, size, Quaternion.identity, _graphite));
        // Structural blocks deliberately have one material and one uninterrupted shell on all six faces.
        // Functional parts keep their detailed LOD in BuildDetailedModel instead of adding a generic marker.
        return renderers;
    }

    private static void BuildStructural(
        Transform parent,
        Block block,
        ICollection<Renderer> renderers,
        ICollection<Renderer> glowRenderers)
    {
        Vector3 size = new Vector3(block.x, block.y, block.z);
        // A plain block is a single rounded shell. This keeps every face interchangeable when building
        // and avoids decorative top/front assumptions that become visually noisy in stacked structures.
        renderers.Add(AddPart(parent, "Uniform Chassis", _roundedCube,
            Vector3.zero, size - Vector3.one * 0.12f, Quaternion.identity, _graphite));
    }

    private static void BuildDoor(Transform parent, Block block, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)
    {
        Vector3 size = new Vector3(block.x, block.y, block.z);
        renderers.Add(AddPart(parent, "Door Slab", _roundedCube, Vector3.zero,
            new Vector3(0.72f, size.y * 0.86f, 0.28f), Quaternion.identity, _armor));
        for (int x = -1; x <= 1; x += 2)
        {
            renderers.Add(AddPart(parent, "Frame Side", _roundedCube,
                new Vector3(x * 0.41f, 0f, 0f), new Vector3(0.13f, size.y * 0.94f, 0.48f), Quaternion.identity, _graphite));
        }
        renderers.Add(AddPart(parent, "Frame Top", _roundedCube,
            new Vector3(0f, size.y * 0.45f, 0f), new Vector3(0.94f, 0.12f, 0.48f), Quaternion.identity, _edge));
        Renderer strip = AddPart(parent, "Access Strip", _roundedCube,
            new Vector3(0.27f, 0f, -0.155f), new Vector3(0.055f, size.y * 0.62f, 0.025f), Quaternion.identity, _cyan, false);
        renderers.Add(strip);
        glowRenderers.Add(strip);
        renderers.Add(AddPart(parent, "Warning Mark", _roundedCube,
            new Vector3(-0.2f, -0.15f, -0.16f), new Vector3(0.08f, 0.55f, 0.025f), Quaternion.Euler(0f, 0f, -28f), _amber, false));
    }

    private static void BuildStairs(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)
    {
        const int StepCount = 5;
        for (int i = 0; i < StepCount; i++)
        {
            float height = (i + 1f) / StepCount;
            float depth = 1f / StepCount;
            float z = -0.5f + depth * (i + 0.5f);
            renderers.Add(AddPart(parent, $"Step {i + 1}", _roundedCube,
                new Vector3(0f, -0.5f + height * 0.5f, z),
                new Vector3(0.88f, height - 0.025f, depth - 0.025f),
                Quaternion.identity,
                i == StepCount - 1 ? _edge : _armor));
        }
        for (int x = -1; x <= 1; x += 2)
        {
            renderers.Add(AddPart(parent, "Side Rail", _roundedCube,
                new Vector3(x * 0.46f, 0f, 0f), new Vector3(0.06f, 0.08f, 1.02f), Quaternion.Euler(-42f, 0f, 0f), _graphite));
        }
        Renderer marker = AddPart(parent, "Step Marker", _roundedCube,
            new Vector3(0f, 0.485f, 0.39f), new Vector3(0.52f, 0.025f, 0.08f), Quaternion.identity, _cyan, false);
        renderers.Add(marker);
        glowRenderers.Add(marker);
    }

    private static void BuildRack(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)
    {
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                renderers.Add(AddPart(parent, "Rack Post", _roundedCube,
                    new Vector3(x * 0.4f, 0f, z * 0.4f), new Vector3(0.1f, 0.92f, 0.1f), Quaternion.identity, _graphite));
            }
        }
        for (int y = -1; y <= 1; y += 2)
        {
            renderers.Add(AddPart(parent, "Rack Frame", _roundedCube,
                new Vector3(0f, y * 0.43f, 0f), new Vector3(0.88f, 0.09f, 0.88f), Quaternion.identity, _edge));
        }
        renderers.Add(AddPart(parent, "Rack Shelf", _roundedCube, Vector3.zero,
            new Vector3(0.78f, 0.055f, 0.78f), Quaternion.identity, _armor));
        Renderer status = AddPart(parent, "Rack Status", _roundedCube,
            new Vector3(0f, 0.43f, -0.445f), new Vector3(0.42f, 0.035f, 0.025f), Quaternion.identity, _cyan, false);
        renderers.Add(status);
        glowRenderers.Add(status);
    }

    private static void BuildCockpit(
        Transform parent,
        Block block,
        ICollection<Renderer> renderers,
        ICollection<Renderer> glowRenderers)
    {
        Vector3 size = new Vector3(block.x, block.y, block.z);
        renderers.Add(AddPart(parent, "Armored Hull", _roundedCube,
            new Vector3(0f, -0.48f, 0f), new Vector3(size.x * 0.9f, 0.86f, size.z * 0.9f), Quaternion.identity, _graphite));
        renderers.Add(AddPart(parent, "Canopy", _wedge,
            new Vector3(0f, 0.35f, 0.12f), new Vector3(1.52f, 0.78f, 1.35f), Quaternion.identity, _glass, false));
        renderers.Add(AddPart(parent, "Nose Armor", _wedge,
            new Vector3(0f, -0.02f, 0.72f), new Vector3(1.7f, 0.45f, 0.52f), Quaternion.Euler(10f, 0f, 0f), _armor));
        for (int x = -1; x <= 1; x += 2)
        {
            renderers.Add(AddPart(parent, "Side Pod", _roundedCube,
                new Vector3(x * 0.82f, -0.2f, -0.05f), new Vector3(0.22f, 0.72f, 1.48f), Quaternion.identity, _edge));
        }
        Renderer spine = AddPart(parent, "Cockpit Spine", _roundedCube,
            new Vector3(0f, 0.77f, -0.02f), new Vector3(0.12f, 0.045f, 0.92f), Quaternion.identity, _cyan, false);
        renderers.Add(spine);
        glowRenderers.Add(spine);
    }

    private static void BuildGenerator(
        Transform parent,
        Block block,
        ICollection<Renderer> renderers,
        ICollection<Transform> spinTargets,
        ICollection<Renderer> glowRenderers)
    {
        Vector3 size = new Vector3(block.x, block.y, block.z);
        renderers.Add(AddPart(parent, "Generator Base", _roundedCube,
            new Vector3(0f, -0.72f, 0f), new Vector3(1.72f, 0.42f, 1.72f), Quaternion.identity, _graphite));
        for (int x = -1; x <= 1; x += 2)
        {
            for (int z = -1; z <= 1; z += 2)
            {
                renderers.Add(AddPart(parent, "Generator Pylon", _roundedCube,
                    new Vector3(x * 0.68f, 0f, z * 0.68f), new Vector3(0.22f, 1.35f, 0.22f), Quaternion.Euler(x * 5f, 0f, z * -5f), _armor));
            }
        }

        Renderer core = AddPart(parent, "Energy Core", _sphere, Vector3.zero,
            Vector3.one * 0.78f, Quaternion.identity, _cyan, false);
        renderers.Add(core);
        glowRenderers.Add(core);

        Transform rings = CreateGroup(parent, "Generator Rings");
        spinTargets.Add(rings);
        Renderer ringA = AddPart(rings, "Horizontal Ring", _torus, Vector3.zero,
            Vector3.one * 1.65f, Quaternion.identity, _edge);
        Renderer ringB = AddPart(rings, "Vertical Ring", _torus, Vector3.zero,
            Vector3.one * 1.35f, Quaternion.Euler(90f, 0f, 0f), _cyan, false);
        renderers.Add(ringA);
        renderers.Add(ringB);
        glowRenderers.Add(ringB);
        renderers.Add(AddPart(parent, "Generator Crown", _roundedCube,
            new Vector3(0f, size.y * 0.5f - 0.12f, 0f), new Vector3(1.2f, 0.16f, 1.2f), Quaternion.identity, _edge));
    }

    private static void BuildRelay(
        Transform parent,
        Block block,
        ICollection<Renderer> renderers,
        ICollection<Transform> spinTargets,
        ICollection<Renderer> glowRenderers)
    {
        renderers.Add(AddPart(parent, "Relay Base", _roundedCube,
            new Vector3(0f, -0.34f, 0f), new Vector3(0.86f, 0.28f, 0.86f), Quaternion.identity, _graphite));
        renderers.Add(AddPart(parent, "Relay Mast", _cylinder,
            new Vector3(0f, 0.02f, 0f), new Vector3(0.2f, 0.42f, 0.2f), Quaternion.identity, _edge));
        Renderer core = AddPart(parent, "Relay Core", _sphere,
            new Vector3(0f, 0.19f, 0f), Vector3.one * 0.34f, Quaternion.identity, _cyan, false);
        renderers.Add(core);
        glowRenderers.Add(core);

        Transform crown = CreateGroup(parent, "Relay Crown");
        crown.localPosition = new Vector3(0f, 0.23f, 0f);
        spinTargets.Add(crown);
        Renderer ring = AddPart(crown, "Signal Ring", _torus, Vector3.zero,
            Vector3.one * 0.82f, Quaternion.identity, _cyan, false);
        renderers.Add(ring);
        glowRenderers.Add(ring);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f;
            Vector3 position = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.37f);
            renderers.Add(AddPart(crown, "Signal Fin", _wedge, position,
                new Vector3(0.16f, 0.34f, 0.18f), Quaternion.Euler(0f, angle, 0f), _armor));
        }
    }

    private static void BuildFlightController(
        Transform parent,
        ICollection<Renderer> renderers,
        ICollection<Transform> spinTargets,
        ICollection<Renderer> glowRenderers)
    {
        renderers.Add(AddPart(parent, "Controller Cradle", _roundedCube,
            new Vector3(0f, -0.31f, 0f), new Vector3(0.88f, 0.28f, 0.88f), Quaternion.identity, _graphite));
        Renderer core = AddPart(parent, "Gyro Core", _sphere, Vector3.zero,
            Vector3.one * 0.42f, Quaternion.identity, _cyan, false);
        renderers.Add(core);
        glowRenderers.Add(core);

        Transform gyro = CreateGroup(parent, "Gyro Rings");
        spinTargets.Add(gyro);
        renderers.Add(AddPart(gyro, "Gyro Ring A", _torus, Vector3.zero,
            Vector3.one * 0.88f, Quaternion.Euler(90f, 0f, 0f), _edge));
        Renderer ring = AddPart(gyro, "Gyro Ring B", _torus, Vector3.zero,
            Vector3.one * 0.72f, Quaternion.Euler(0f, 0f, 90f), _cyan, false);
        renderers.Add(ring);
        glowRenderers.Add(ring);
    }

    private static void BuildThruster(
        Transform parent,
        Block block,
        string name,
        ICollection<Renderer> renderers,
        ICollection<Transform> spinTargets,
        ICollection<Renderer> glowRenderers)
    {
        bool isHover = name.StartsWith("Hover", StringComparison.Ordinal);
        bool isUniversal = name.StartsWith("Universal", StringComparison.Ordinal);
        float size = Mathf.Max(block.x, Mathf.Max(block.y, block.z));
        float scale = size >= 2f ? 1.75f : 0.86f;

        // Keep the visual's local forward/up semantics intact: Main/Universal thrust along Model.forward,
        // while Hover thrusts along the block's up axis. The nozzle is therefore placed on the opposite
        // side of the force vector and all rings share that axis rotation.
        renderers.Add(AddPart(parent, "Thruster Housing", _roundedCube, Vector3.zero,
            new Vector3(scale, scale * 0.78f, scale), Quaternion.identity, _graphite));
        for (int side = -1; side <= 1; side += 2)
        {
            renderers.Add(AddPart(parent, "Thruster Side Rail", _wedge,
                new Vector3(side * scale * 0.39f, 0f, 0f),
                new Vector3(scale * 0.18f, scale * 0.58f, scale * 0.72f),
                Quaternion.Euler(0f, side * 8f, 0f), _armor));
        }

        Quaternion nozzleRotation = isHover ? Quaternion.identity : Quaternion.Euler(90f, 0f, 0f);
        Vector3 nozzleAxis = isHover ? Vector3.down : Vector3.back;
        Vector3 nozzlePosition = nozzleAxis * (scale * 0.39f);
        renderers.Add(AddPart(parent, "Nozzle Collar", _cylinder, nozzlePosition,
            new Vector3(scale * 0.58f, scale * 0.16f, scale * 0.58f), nozzleRotation, _edge));
        renderers.Add(AddPart(parent, "Nozzle Ring", _torus, nozzlePosition,
            Vector3.one * scale * 0.60f, nozzleRotation, _armor));

        Transform turbine = CreateGroup(parent, "Turbine");
        turbine.localPosition = nozzlePosition + nozzleAxis * (scale * 0.10f);
        turbine.localRotation = nozzleRotation;
        spinTargets.Add(turbine);
        Renderer heat = AddPart(turbine, "Heat Core", _cylinder, Vector3.zero,
            new Vector3(scale * 0.34f, scale * 0.055f, scale * 0.34f), Quaternion.identity, _amber, false);
        renderers.Add(heat);
        glowRenderers.Add(heat);
        renderers.Add(AddPart(turbine, "Combustion Cone", _cone,
            Vector3.down * (scale * 0.045f), new Vector3(scale * 0.38f, scale * 0.11f, scale * 0.38f),
            Quaternion.identity, _graphite, false));
        if (isUniversal)
        {
            Renderer vectorRing = AddPart(parent, "Vector Ring", _torus, Vector3.zero,
                Vector3.one * scale * 0.72f, Quaternion.Euler(90f, 0f, 0f), _cyan, false);
            renderers.Add(vectorRing);
            glowRenderers.Add(vectorRing);
        }
    }

    private static void BuildTurret(
        Transform parent,
        ICollection<Renderer> renderers,
        ICollection<Transform> spinTargets,
        ICollection<Renderer> glowRenderers)
    {
        renderers.Add(AddPart(parent, "Turret Base", _cylinder,
            new Vector3(0f, -0.34f, 0f), new Vector3(0.78f, 0.16f, 0.78f), Quaternion.identity, _graphite));
        renderers.Add(AddPart(parent, "Turret Yoke", _roundedCube,
            new Vector3(0f, -0.05f, 0f), new Vector3(0.64f, 0.45f, 0.6f), Quaternion.identity, _armor));
        renderers.Add(AddPart(parent, "Barrel", _cylinder,
            new Vector3(-0.16f, 0.13f, 0.34f), new Vector3(0.1f, 0.42f, 0.1f), Quaternion.Euler(90f, 0f, 0f), _edge));
        renderers.Add(AddPart(parent, "Barrel", _cylinder,
            new Vector3(0.16f, 0.13f, 0.34f), new Vector3(0.1f, 0.42f, 0.1f), Quaternion.Euler(90f, 0f, 0f), _edge));
        Renderer muzzle = AddPart(parent, "Muzzle Energy", _roundedCube,
            new Vector3(0f, 0.13f, 0.76f), new Vector3(0.42f, 0.08f, 0.08f), Quaternion.identity, _red, false);
        renderers.Add(muzzle);
        glowRenderers.Add(muzzle);

        Transform bearing = CreateGroup(parent, "Turret Bearing");
        spinTargets.Add(bearing);
        Renderer bearingRing = AddPart(bearing, "Bearing Ring", _torus,
            new Vector3(0f, -0.23f, 0f), Vector3.one * 0.68f, Quaternion.identity, _cyan, false);
        renderers.Add(bearingRing);
        glowRenderers.Add(bearingRing);
    }

    private static void BuildRepairBay(Transform parent, ICollection<Renderer> renderers, ICollection<Renderer> glowRenderers)
    {
        renderers.Add(AddPart(parent, "Repair Bay Chassis", _roundedCube, Vector3.zero,
            new Vector3(0.92f, 0.88f, 0.92f), Quaternion.identity, _graphite));
        renderers.Add(AddPart(parent, "Repair Bay Door", _wedge,
            new Vector3(0f, 0f, 0.45f), new Vector3(0.72f, 0.58f, 0.08f), Quaternion.Euler(0f, 0f, 180f), _armor));
        for (int x = -1; x <= 1; x += 2)
        {
            renderers.Add(AddPart(parent, "Repair Clamp", _roundedCube,
                new Vector3(x * 0.38f, 0f, 0.38f), new Vector3(0.1f, 0.62f, 0.22f), Quaternion.identity, _edge));
        }
        Renderer medical = AddPart(parent, "Repair Status", _roundedCube,
            new Vector3(0f, 0.33f, 0.465f), new Vector3(0.38f, 0.06f, 0.025f), Quaternion.identity, _cyan, false);
        renderers.Add(medical);
        glowRenderers.Add(medical);
    }

    private static void RebuildBot(GameObject root)
    {
        Transform oldGenerated = root.transform.Find(GeneratedRootName);
        if (oldGenerated != null)
        {
            UnityEngine.Object.DestroyImmediate(oldGenerated.gameObject);
        }

        MeshRenderer oldRenderer = root.GetComponent<MeshRenderer>();
        if (oldRenderer != null)
        {
            oldRenderer.enabled = false;
        }

        Transform visual = CreateGroup(root.transform, GeneratedRootName);
        var glowRenderers = new List<Renderer>();
        AddPart(visual, "Drone Body", _roundedCube, Vector3.zero, new Vector3(0.7f, 0.32f, 0.6f), Quaternion.identity, _graphite);
        for (int x = -1; x <= 1; x += 2)
        {
            AddPart(visual, "Drone Wing", _wedge, new Vector3(x * 0.46f, 0f, 0f),
                new Vector3(0.42f, 0.1f, 0.48f), Quaternion.Euler(0f, x * 8f, x * -8f), _armor);
            Renderer light = AddPart(visual, "Drone Light", _sphere, new Vector3(x * 0.55f, 0f, 0.08f),
                Vector3.one * 0.11f, Quaternion.identity, _cyan, false);
            glowRenderers.Add(light);
        }
        Renderer eye = AddPart(visual, "Repair Eye", _roundedCube, new Vector3(0f, 0.03f, 0.31f),
            new Vector3(0.3f, 0.08f, 0.04f), Quaternion.identity, _cyan, false);
        glowRenderers.Add(eye);
        Transform rotor = CreateGroup(visual, "Drone Rotor");
        AddPart(rotor, "Rotor Ring", _torus, new Vector3(0f, -0.14f, 0f), Vector3.one * 0.48f, Quaternion.identity, _edge);

        var motion = visual.gameObject.AddComponent<IndustrialPartMotion>();
        motion.Configure(new[] { rotor }, Vector3.up, 72f, null, 0f, 1f,
            glowRenderers.ToArray(), CyanEmission, 0.2f);
        SetLayerRecursively(visual.gameObject, root.layer);
    }

    private static Transform CreateGroup(Transform parent, string name)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        group.layer = parent.gameObject.layer;
        return group.transform;
    }

    private static MeshRenderer AddPart(
        Transform parent,
        string name,
        Mesh mesh,
        Vector3 localPosition,
        Vector3 localScale,
        Quaternion localRotation,
        Material material,
        bool castShadows = true)
    {
        var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = localRotation;
        part.transform.localScale = localScale;
        part.layer = parent.gameObject.layer;
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        renderer.receiveShadows = castShadows;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Object;
        renderer.allowOcclusionWhenDynamic = true;
        return renderer;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    private static void CreatePreviewScene(IReadOnlyList<string> prefabPaths)
    {
        string mainScenePath = SceneManager.GetActiveScene().path;
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        preview.name = "IndustrialArtPreview";

        Material floorMaterial = CreateOrUpdateMaterial(
            "MAT_Industrial_PreviewFloor",
            new Color(0.018f, 0.026f, 0.04f),
            0.3f,
            0.34f,
            Color.black);
        AddPartToScene("Preview Floor", _roundedCube, new Vector3(0f, -0.4f, 4f),
            new Vector3(18f, 0.5f, 18f), Quaternion.identity, floorMaterial);

        var keyLight = new GameObject("Key Light", typeof(Light));
        keyLight.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
        Light key = keyLight.GetComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(0.82f, 0.9f, 1f);
        key.intensity = 2.1f;
        key.shadows = LightShadows.Soft;

        var rimLight = new GameObject("Cyan Rim", typeof(Light));
        rimLight.transform.position = new Vector3(-6f, 5f, 6f);
        Light rim = rimLight.GetComponent<Light>();
        rim.type = LightType.Point;
        rim.color = new Color(0.03f, 0.72f, 1f);
        rim.range = 14f;
        rim.intensity = 850f;
        rim.shadows = LightShadows.None;

        var volumeObject = new GameObject("Industrial Global Volume", typeof(Volume));
        Volume volume = volumeObject.GetComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/Settings/SampleSceneProfile.asset");

        var cameraObject = new GameObject("Preview Camera", typeof(Camera), typeof(UniversalAdditionalCameraData));
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(15f, 12f, -18f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 1.25f, 6f) - camera.transform.position, Vector3.up);
        camera.fieldOfView = 47f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.008f, 0.014f, 0.026f);
        camera.allowHDR = true;
        camera.allowMSAA = true;
        UniversalAdditionalCameraData cameraData = cameraObject.GetComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;

        string[] showcaseNames =
        {
            "Cockpit", "PowerGeneratingUnit", "PowerTransmissionDevice", "HoverFlightController",
            "MainThruster", "UniversalThruster", "HoverThrusterBig", "Turret",
            "Door", "Stairs", "Rack", "RepairBotContianer",
            "1x1x1", "2x1x1", "2x2x1", "2x2x2"
        };
        for (int i = 0; i < showcaseNames.Length; i++)
        {
            string path = prefabPaths.FirstOrDefault(candidate => Path.GetFileNameWithoutExtension(candidate) == showcaseNames[i]);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            int column = i % 4;
            int row = i / 4;
            instance.transform.position = new Vector3((column - 1.5f) * 4f, 1.05f, row * 4f);
            instance.transform.rotation = Quaternion.Euler(0f, -18f + column * 8f, 0f);
            instance.name = $"Showcase {showcaseNames[i]}";

            Transform debugRoot = instance.transform.Find("Debug");
            if (debugRoot != null)
            {
                debugRoot.gameObject.SetActive(false);
            }

            foreach (LineRenderer lineRenderer in instance.GetComponentsInChildren<LineRenderer>(true))
            {
                lineRenderer.enabled = false;
            }

            foreach (LODGroup lodGroup in instance.GetComponentsInChildren<LODGroup>(true))
            {
                lodGroup.enabled = false;
            }

            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is not IndustrialPartMotion)
                {
                    behaviour.enabled = false;
                }
            }

            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == "LOD1")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.17f, 0.23f, 0.34f);
        RenderSettings.ambientEquatorColor = new Color(0.055f, 0.075f, 0.11f);
        RenderSettings.ambientGroundColor = new Color(0.012f, 0.018f, 0.03f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.008f, 0.018f, 0.035f);
        RenderSettings.fogDensity = 0.012f;

        string previewPath = PreviewRoot + "/IndustrialArtPreview.unity";
        EditorSceneManager.SaveScene(preview, previewPath);
        if (!string.IsNullOrEmpty(mainScenePath))
        {
            EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
        }
    }

    private static void AddPartToScene(
        string name,
        Mesh mesh,
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Material material)
    {
        var part = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        part.transform.position = position;
        part.transform.rotation = rotation;
        part.transform.localScale = scale;
        part.GetComponent<MeshFilter>().sharedMesh = mesh;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
    }
}

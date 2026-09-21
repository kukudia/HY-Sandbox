using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Copies the reviewed reserve library without changing source packs or gameplay Prefabs.</summary>
public static class SpaceKitCurator
{
    private const string Root = "Assets/Art/SpaceKit";
    private const string Cube = "Assets/CUBE - Spaceships Pack 01/";
    private const string Polygon = "Assets/PolygonSciFiSpace/";

    [Serializable] public sealed class Selection { public Entry[] entries; }
    [Serializable] public sealed class Entry
    {
        public string source;
        public string category;
        public string purpose;
        public string destination;
    }
    [Serializable] public sealed class Catalog { public Record[] assets; }
    [Serializable] public sealed class Record
    {
        public string source;
        public string destination;
        public string sourceGuid;
        public string guid;
        public string sourceSha256;
        public string sourceMetaSha256;
    }
    [Serializable] public sealed class Audit
    {
        public string unityVersion;
        public int assetCount;
        public string[] errors;
        public Sample[] prefabs;
        public string[] materialShaders;
    }
    [Serializable] public sealed class Sample
    {
        public string path;
        public Vector3 size;
        public long triangles;
        public int renderers;
        public int materialSlots;
        public int particleSystems;
        public int maxParticles;
        public int colliders;
        public int nonConvexMeshColliders;
    }

    private static bool IsSource(string path) => path.StartsWith(Cube, StringComparison.Ordinal) || path.StartsWith(Polygon, StringComparison.Ordinal);

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
        AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
    }

    private static string Destination(string path, Dictionary<string, string> selected)
    {
        if (selected.TryGetValue(path, out string destination)) return destination;
        string pack = path.StartsWith(Cube, StringComparison.Ordinal) ? "Cube" : "Polygon";
        string relative = path.Substring(pack == "Cube" ? Cube.Length : Polygon.Length);
        string extension = Path.GetExtension(path).ToLowerInvariant();
        string kind = extension == ".prefab" ? "Prefabs/Supporting" :
            extension == ".mat" ? "Materials" :
            extension == ".fbx" ? "Models" :
            extension == ".asset" && relative.Contains("Collision/") ? "Collision" :
            extension == ".anim" || extension == ".controller" ? "Animations" : "Textures";
        // Keep source subdirectories to avoid collisions between equal basenames.
        return Root + "/" + kind + "/" + pack + "/" + relative;
    }

    private static string Hash(string path)
    {
        using (var stream = File.OpenRead(path))
        using (var sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
    }

    [MenuItem("Tools/HY Sandbox/Space Kit/Create Missing Library")]
    public static void CreateLibrary()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before curating assets.");
        if (File.Exists(Root + "/Catalog.json"))
            throw new InvalidOperationException("Library already exists. Validate it; do not overwrite hand-edited assets.");

        var selection = JsonUtility.FromJson<Selection>(File.ReadAllText(Root + "/Selection.json"));
        Directory.CreateDirectory(".utmp/art-curation");
        var selected = selection.entries.ToDictionary(e => e.source, e => e.destination);
        var sources = AssetDatabase.GetDependencies(selected.Keys.ToArray(), true)
            .Where(IsSource).Where(p => !p.EndsWith(".shader", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p).ToArray();
        var records = sources.Select(p => new Record
        {
            source = p, destination = Destination(p, selected), sourceGuid = AssetDatabase.AssetPathToGUID(p),
            sourceSha256 = Hash(p), sourceMetaSha256 = Hash(p + ".meta")
        }).ToArray();
        foreach (var item in records)
        {
            if (File.Exists(item.destination)) throw new IOException("Destination exists: " + item.destination);
            EnsureFolder(Path.GetDirectoryName(item.destination).Replace('\\', '/'));
        }
        File.WriteAllText(".utmp/art-curation/copy-plan.json", JsonUtility.ToJson(new Catalog { assets = records }, true));

        // CopyAsset generates fresh GUIDs and preserves importer settings and FBX subasset local IDs.
        AssetDatabase.StartAssetEditing();
        try
        {
            foreach (var item in records)
                if (!AssetDatabase.CopyAsset(item.source, item.destination)) throw new IOException("Copy failed: " + item.source);
        }
        finally { AssetDatabase.StopAssetEditing(); }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var objects = new Dictionary<Object, Object>();
        foreach (var item in records)
        {
            item.guid = AssetDatabase.AssetPathToGUID(item.destination);
            var copies = AssetDatabase.LoadAllAssetsAtPath(item.destination).Where(o => o != null).ToArray();
            foreach (var original in AssetDatabase.LoadAllAssetsAtPath(item.source).Where(o => o != null))
            {
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(original, out string unused, out long id);
                var copy = copies.FirstOrDefault(o =>
                {
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(o, out string other, out long copyId);
                    return id == copyId && o.GetType() == original.GetType();
                });
                if (copy != null) objects[original] = copy;
            }
        }

        foreach (var item in records.Where(r => r.destination.EndsWith(".mat")))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(item.destination);
            ConvertMaterial(material);
            Remap(material, objects);
            EditorUtility.SetDirty(material);
        }
        foreach (var item in records.Where(r => r.destination.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)))
        {
            var importer = AssetImporter.GetAtPath(item.destination);
            foreach (var pair in importer.GetExternalObjectMap())
                if (pair.Value != null && objects.TryGetValue(pair.Value, out Object replacement)) importer.AddRemap(pair.Key, replacement);
            EditorUtility.SetDirty(importer);
            AssetDatabase.WriteImportSettingsIfDirty(item.destination);
            AssetDatabase.ImportAsset(item.destination, ImportAssetOptions.ForceSynchronousImport);
        }
        foreach (var item in records.Where(r => !r.destination.EndsWith(".prefab") && !r.destination.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase)))
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(item.destination))
                if (asset != null && !(asset is Shader)) Remap(asset, objects);

        // Unpack copies only: nested source Prefabs otherwise keep an ignored-pack dependency.
        foreach (var item in records.Where(r => r.destination.EndsWith(".prefab")))
        {
            var instance = PrefabUtility.LoadPrefabContents(item.destination);
            try
            {
                foreach (var transform in instance.GetComponentsInChildren<Transform>(true))
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject))
                        PrefabUtility.UnpackPrefabInstance(transform.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (var component in instance.GetComponentsInChildren<Component>(true))
                    if (component != null) Remap(component, objects);
                CleanLegacyArtifacts(instance);
                PrefabUtility.SaveAsPrefabAsset(instance, item.destination);
            }
            finally { PrefabUtility.UnloadPrefabContents(instance); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(Root + "/Catalog.json", JsonUtility.ToJson(new Catalog { assets = records }, true) + "\n");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ValidateLibrary();
    }

    private static void Remap(Object target, Dictionary<Object, Object> objects)
    {
        using (var serialized = new SerializedObject(target))
        {
            var property = serialized.GetIterator();
            bool changed = false;
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null) continue;
                if (objects.TryGetValue(property.objectReferenceValue, out Object replacement))
                {
                    property.objectReferenceValue = replacement;
                    changed = true;
                }
            }
            if (changed) serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CleanLegacyArtifacts(GameObject instance)
    {
        // Two static vendor props contain an Animator with a dangling avatar and no controller.
        foreach (var animator in instance.GetComponentsInChildren<Animator>(true))
            if (animator.runtimeAnimatorController == null && animator.avatar == null &&
                animator.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 0)
                Object.DestroyImmediate(animator);
        foreach (var renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
            if (renderer.renderMode != ParticleSystemRenderMode.Mesh)
                renderer.mesh = null; // Old mesh references are unused by billboard/stretch renderers.
    }

    public static void RepairLegacyImportArtifacts()
    {
        var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Root + "/Catalog.json"));
        foreach (var item in catalog.assets.Where(r => r.destination.EndsWith(".prefab")))
        {
            var instance = PrefabUtility.LoadPrefabContents(item.destination);
            try { CleanLegacyArtifacts(instance); PrefabUtility.SaveAsPrefabAsset(instance, item.destination); }
            finally { PrefabUtility.UnloadPrefabContents(instance); }
        }
        ValidateLibrary();
    }

    private static void ConvertMaterial(Material material)
    {
        string shader = material.shader.name;
        if (shader.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal)) return;
        bool particle = shader.Contains("Particles/");
        bool additive = shader.Contains("Additive");
        bool ship = shader == "SyntyStudios/SpaceShip_Rim";
        bool rock = shader == "SyntyStudios/PlanetsLines";
        if (!particle && !ship && !rock && shader != "Standard")
            throw new InvalidOperationException("Unreviewed shader: " + shader);
        string textureProperty = ship ? "_Texture" : "_MainTex";
        Texture texture = material.HasProperty(textureProperty) ? material.GetTexture(textureProperty) : null;
        Vector2 scale = material.HasProperty(textureProperty) ? material.GetTextureScale(textureProperty) : Vector2.one;
        Vector2 offset = material.HasProperty(textureProperty) ? material.GetTextureOffset(textureProperty) : Vector2.zero;
        bool hasParticleTint = particle && material.HasProperty("_TintColor");
        Color color = particle ? (hasParticleTint ? material.GetColor("_TintColor") : Color.white) :
            rock ? material.GetColor("_BaseColor") : material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
        if (rock) { color.a = 1; texture = null; }
        Texture emission = material.HasProperty(ship ? "_Emissive" : "_EmissionMap") ? material.GetTexture(ship ? "_Emissive" : "_EmissionMap") : null;
        Color emissionColor = material.HasProperty(ship ? "_EmissiveColor" : "_EmissionColor") ? material.GetColor(ship ? "_EmissiveColor" : "_EmissionColor") : Color.black;
        float smoothness = material.HasProperty(ship ? "_Smoothness" : "_Glossiness") ? material.GetFloat(ship ? "_Smoothness" : "_Glossiness") : 0.25f;
        float metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0;
        int mode = material.HasProperty("_Mode") ? (int)material.GetFloat("_Mode") : 0;
        Texture normal = material.HasProperty("_BumpMap") ? material.GetTexture("_BumpMap") : null;
        var clean = new Material(Shader.Find(particle ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Lit"));
        try
        {
            clean.SetTexture("_BaseMap", texture);
            clean.SetTextureScale("_BaseMap", scale);
            clean.SetTextureOffset("_BaseMap", offset);
            // Legacy particle shaders multiply tint by two; preserve that intensity.
            clean.SetColor("_BaseColor", hasParticleTint ? color * 2 : color);
            if (!particle)
            {
                clean.SetFloat("_Metallic", metallic);
                clean.SetFloat("_Smoothness", rock ? 0.15f : smoothness);
                clean.SetTexture("_BumpMap", normal);
                if (normal != null) clean.EnableKeyword("_NORMALMAP");
                clean.SetTexture("_EmissionMap", emission);
                clean.SetColor("_EmissionColor", emissionColor);
                if (emission != null || emissionColor.maxColorComponent > 0)
                {
                    clean.EnableKeyword("_EMISSION");
                    clean.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                }
            }
            if (particle || mode >= 2)
            {
                clean.SetFloat("_Surface", 1);
                clean.SetFloat("_Blend", additive ? 2 : 0);
                clean.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                clean.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                clean.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                clean.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                clean.SetFloat("_ZWrite", 0);
                clean.SetFloat("_Cull", particle ? 0 : 2);
                clean.SetOverrideTag("RenderType", "Transparent");
                clean.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                clean.SetShaderPassEnabled("ShadowCaster", false);
                clean.renderQueue = (int)RenderQueue.Transparent;
            }
            else if (mode == 1)
            {
                clean.SetFloat("_AlphaClip", 1);
                clean.EnableKeyword("_ALPHATEST_ON");
                clean.SetOverrideTag("RenderType", "TransparentCutout");
                clean.renderQueue = (int)RenderQueue.AlphaTest;
            }
            // Remove stale legacy texture slots, so unused source shader properties cannot leak dependencies.
            EditorUtility.CopySerialized(clean, material);
            material.name = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(material));
        }
        finally { Object.DestroyImmediate(clean); }
    }

    [MenuItem("Tools/HY Sandbox/Space Kit/Validate Library")]
    public static void ValidateLibrary()
    {
        var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(Root + "/Catalog.json"));
        var errors = new List<string>();
        var samples = new List<Sample>();
        var shaders = new HashSet<string>();
        foreach (var item in catalog.assets)
        {
            string path = item.destination;
            if (AssetDatabase.AssetPathToGUID(path) != item.guid) errors.Add("Changed GUID: " + path);
            if (item.guid == item.sourceGuid) errors.Add("Duplicated source GUID: " + path);
            if (AssetDatabase.LoadMainAssetAtPath(path) == null) errors.Add("Cannot load: " + path);
            foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                if (dependency.StartsWith("Assets/", StringComparison.Ordinal) && !dependency.StartsWith(Root + "/", StringComparison.Ordinal))
                    errors.Add("External dependency: " + path + " -> " + dependency);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                shaders.Add(material.shader.name);
                if (!material.shader.name.StartsWith("Universal Render Pipeline/", StringComparison.Ordinal) || !material.shader.isSupported || ShaderUtil.ShaderHasError(material.shader))
                    errors.Add("Incompatible shader: " + path);
            }
            if (!path.EndsWith(".prefab")) continue;
            var instance = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var transform in instance.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0) errors.Add("Missing script: " + path);
                foreach (var component in instance.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) continue;
                    using (var serialized = new SerializedObject(component))
                    {
                        var property = serialized.GetIterator();
                        while (property.Next(true))
                            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                errors.Add("Broken reference: " + path + ":" + property.propertyPath);
                    }
                }
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var meshes = instance.GetComponentsInChildren<MeshFilter>(true).Select(m => m.sharedMesh)
                    .Concat(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(m => m.sharedMesh)).ToArray();
                if (meshes.Any(m => m == null)) errors.Add("Missing mesh: " + path);
                foreach (var renderer in renderers)
                {
                    if (renderer is ParticleSystemRenderer particleRenderer)
                    {
                        // A null optional trail material is valid while trails are disabled.
                        var system = particleRenderer.GetComponent<ParticleSystem>();
                        if (particleRenderer.sharedMaterial == null || (system.trails.enabled && particleRenderer.trailMaterial == null))
                            errors.Add("Missing particle material: " + path);
                    }
                    else if (renderer.sharedMaterials.Any(m => m == null)) errors.Add("Missing material: " + path);
                }
                Bounds bounds = new Bounds(instance.transform.position, Vector3.zero);
                bool first = true;
                foreach (var renderer in renderers.Where(r => !(r is ParticleSystemRenderer)))
                {
                    if (first) { bounds = renderer.bounds; first = false; }
                    else bounds.Encapsulate(renderer.bounds);
                }
                var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
                samples.Add(new Sample
                {
                    path = path, size = bounds.size, renderers = renderers.Length,
                    triangles = meshes.Where(m => m != null).Sum(m => Enumerable.Range(0, m.subMeshCount).Sum(i => (long)m.GetIndexCount(i) / 3)),
                    materialSlots = renderers.Sum(r => r.sharedMaterials.Length), particleSystems = particles.Length,
                    maxParticles = particles.Sum(p => p.main.maxParticles), colliders = instance.GetComponentsInChildren<Collider>(true).Length,
                    nonConvexMeshColliders = instance.GetComponentsInChildren<MeshCollider>(true).Count(c => !c.convex)
                });
                foreach (var particle in particles)
                {
                    particle.useAutoRandomSeed = false;
                    particle.randomSeed = 12345;
                }
                foreach (var particle in particles.Where(p => p.transform.parent == null || p.transform.parent.GetComponentInParent<ParticleSystem>() == null))
                    particle.Simulate(1.0f, true, true, true);
            }
            finally { PrefabUtility.UnloadPrefabContents(instance); }
        }
        var report = new Audit { unityVersion = Application.unityVersion, assetCount = catalog.assets.Length,
            errors = errors.Distinct().ToArray(), prefabs = samples.ToArray(), materialShaders = shaders.OrderBy(s => s).ToArray() };
        File.WriteAllText(Root + "/Validation.json", JsonUtility.ToJson(report, true) + "\n");
        AssetDatabase.ImportAsset(Root + "/Validation.json");
        if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", report.errors.Take(20)));
        Debug.Log("SpaceKit validation: " + report.assetCount + " assets, " + samples.Count + " Prefabs, no external Assets dependencies or missing references.");
    }

    [MenuItem("Tools/HY Sandbox/Space Kit/Render Preview Images")]
    public static void RenderAllPreviews() => RenderPreviews(0, int.MaxValue);

    public static void RenderPreviews(int start, int count)
    {
        var selection = JsonUtility.FromJson<Selection>(File.ReadAllText(Root + "/Selection.json"));
        Directory.CreateDirectory(".utmp/art-curation/previews");
        var entries = selection.entries.Where(e => e.destination.EndsWith(".prefab")).ToArray();
        for (int index = start; index < Math.Min(entries.Length, start + count); index++)
        {
            var scene = EditorSceneManager.NewPreviewScene();
            var target = new RenderTexture(320, 260, 24);
            try
            {
                var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(entries[index].destination));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance, scene);
                var cameraObject = new GameObject("SpaceKit Preview Camera");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject, scene);
                var camera = cameraObject.AddComponent<Camera>();
                camera.scene = scene;
                camera.targetTexture = target;
                foreach (var light in instance.GetComponentsInChildren<Light>(true)) light.enabled = false;
                foreach (var lod in instance.GetComponentsInChildren<LODGroup>(true)) lod.ForceLOD(0);
                var particles = instance.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var particle in particles) { particle.useAutoRandomSeed = false; particle.randomSeed = 12345; }
                float sampleTime = entries[index].destination.Contains("FX_Explosion") ? 0.18f :
                    entries[index].destination.Contains("FX_Laser") ? 0.15f :
                    entries[index].destination.Contains("FX Ground Smoke") ? 1.5f : 0.65f;
                foreach (var particle in particles.Where(p => p.transform.parent == null || p.transform.parent.GetComponentInParent<ParticleSystem>() == null))
                    particle.Simulate(sampleTime, true, true, true);
                var renderers = instance.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
                var solid = renderers.Where(r => !(r is ParticleSystemRenderer)).ToArray();
                var visible = solid.Length > 0 ? solid : renderers;
                Bounds bounds = visible.Length > 0 ? visible[0].bounds : new Bounds(Vector3.zero, Vector3.one * 3);
                foreach (var renderer in visible.Skip(1)) bounds.Encapsulate(renderer.bounds);
                // Particle renderer bounds can contain the full lifetime trajectory, making short bursts tiny.
                if (solid.Length == 0)
                {
                    bool firstParticle = true;
                    foreach (var system in particles)
                    {
                        var live = new ParticleSystem.Particle[system.particleCount];
                        int liveCount = system.GetParticles(live);
                        for (int particleIndex = 0; particleIndex < liveCount; particleIndex++)
                        {
                            Vector3 position = live[particleIndex].position;
                            var main = system.main;
                            if (main.simulationSpace != ParticleSystemSimulationSpace.World)
                            {
                                var space = main.simulationSpace == ParticleSystemSimulationSpace.Custom ? main.customSimulationSpace : system.transform;
                                if (space != null) position = space.TransformPoint(position);
                            }
                            float size = Mathf.Max(live[particleIndex].GetCurrentSize(system), 0.05f);
                            var particleBounds = new Bounds(position, Vector3.one * size);
                            if (firstParticle) { bounds = particleBounds; firstParticle = false; }
                            else bounds.Encapsulate(particleBounds);
                        }
                    }
                }
                float radius = Mathf.Max(bounds.extents.magnitude, 0.2f);
                camera.transform.position = bounds.center + new Vector3(1, 0.7f, -1).normalized * radius * 3.5f;
                camera.transform.LookAt(bounds.center);
                camera.nearClipPlane = Mathf.Max(0.001f, radius * 0.01f);
                camera.farClipPlane = radius * 12 + 100;
                camera.fieldOfView = 38;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.075f, 0.09f, 0.12f, 1);
                var keyObject = new GameObject("Key Light");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(keyObject, scene);
                var key = keyObject.AddComponent<Light>();
                key.type = LightType.Directional;
                key.intensity = 3;
                key.transform.rotation = Quaternion.Euler(35, -35, 0);
                var fillObject = new GameObject("Fill Light");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject, scene);
                var fill = fillObject.AddComponent<Light>();
                fill.type = LightType.Directional;
                fill.intensity = 1.5f;
                fill.transform.rotation = Quaternion.Euler(330, 145, 0);
                camera.Render();
                var old = RenderTexture.active;
                var texture = new Texture2D(320, 260, TextureFormat.RGB24, false);
                try
                {
                    RenderTexture.active = target;
                    texture.ReadPixels(new Rect(0, 0, 320, 260), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(".utmp/art-curation/previews/" + index.ToString("D3") + ".png", texture.EncodeToPNG());
                }
                finally { RenderTexture.active = old; Object.DestroyImmediate(texture); }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(target); }
        }
    }
}

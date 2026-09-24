using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

/// <summary>Bakes UNI flipbooks into editable URP particles without a GPU Graph dependency.</summary>
public static class UniVfxIntegration
{
    public const string Root = "Assets/Art/BlockVisuals/UNI";

    [MenuItem("Tools/HY Sandbox/UNI VFX/Bake Combat Effects")]
    public static void Integrate()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        BlockArtDependencies.EnsureFolder(Root + "/Materials");
        foreach (string texture in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            AssetDatabase.ImportAsset(texture);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texture);
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        Material fireball = Material("Fireball", "uni_aerial_explosion", false);
        Material fire = Material("Flame", "uni_fire", false);
        Material smoke = Material("Smoke", "uni_smoke_misty", false);
        Material spiky = Material("PressureSmoke", "uni_smoke_spiky", false);
        Material glow = Material("Ember", "uni_glow", true);
        Material ring = Material("Shockwave", "Shockwave", true);
        foreach (string name in new[] { "Explosion", "BreakBurst", "ImpactBurst", "SmokeBurst", "DetachedSmoke" })
        {
            string path = BlockVfxBaker.Root + "/" + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
                foreach (Component c in root.GetComponents<Component>())
                    if (!(c is Transform) && !(c is AssetParticleEffect)) Object.DestroyImmediate(c);
                bool trail = name == "DetachedSmoke";
                bool smokeOnly = name == "SmokeBurst";
                float size = name == "Explosion" ? 1f : name == "BreakBurst" ? 0.5f : name == "ImpactBurst" ? 0.2f : 0.55f;
                if (trail)
                {
                    Layer(root, "Rolling smoke trail", smoke, true, 0.65f, 1.5f, 0.3f, 14, 0, true, new Color(0.62f, 0.69f, 0.78f, 0.65f));
                    Layer(root, "Licking flame trail", fire, true, 0.42f, 0.55f, 0.15f, 18, 0, true, new Color(1.65f, 1.3f, 1.05f, 0.75f));
                    Layer(root, "Drifting embers", glow, true, 0.035f, 0.7f, 1.1f, 8, 0, false, new Color(2.5f, 0.6f, 0.08f, 1f));
                }
                else
                {
                    if (!smokeOnly)
                    {
                        Layer(root, "Core fireball", fireball, false, 3.6f * size, 1.05f, 0.1f, 0, 1, true, new Color(1.8f, 1.4f, 1.1f));
                        Layer(root, "Satellite fireballs", fireball, false, 1.8f * size, 0.9f, 1.4f * size, 0, 5, true, new Color(1.5f, 1.15f, 0.9f));
                        Layer(root, "Hot debris", glow, false, 0.07f * size, 0.85f, 5f * size, 0, (short)(name == "Explosion" ? 26 : 10), false, new Color(3f, 1f, 0.15f));
                        var flash = Layer(root, "Ignition flash", glow, false, 3f * size, 0.14f, 0, 0, 1, false, new Color(3f, 1.6f, 0.5f));
                        var flashMain = flash.main; flashMain.startRotation = 0;
                        var wave = Layer(root, "Spherical shock front", ring, false, 1.2f * size, 0.55f, 0, 0, 1, false, new Color(1.4f, 0.65f, 0.2f, 0.45f));
                        var growth = wave.sizeOverLifetime; growth.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.4f, 1, 5f));
                    }
                    var pressure = Layer(root, "Pressure smoke", spiky, false, 2.8f * size, 1.4f, 0.7f * size, 0, 3, true, new Color(0.75f, 0.78f, 0.85f, 0.7f));
                    var pmain = pressure.main; pmain.startDelay = smokeOnly ? 0 : 0.08f;
                    var cloud = Layer(root, "Lingering rolling smoke", smoke, false, 2.4f * size, 2.1f, 0.4f * size, 0, 5, true, new Color(0.55f, 0.62f, 0.72f, 0.65f));
                    var cmain = cloud.main; cmain.startDelay = smokeOnly ? 0 : 0.18f;
                }
                var controller = root.GetComponent<AssetParticleEffect>();
                BlockVfxBaker.SetObjects(controller, "_particles", root.GetComponentsInChildren<ParticleSystem>(true));
                BlockVfxBaker.SetObjects(controller, "_lights", Array.Empty<Object>());
                using (var data = new SerializedObject(controller))
                {
                    data.FindProperty("_releaseAfter").floatValue = 3.2f;
                    data.FindProperty("_continuousEmission").boolValue = false;
                    data.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Validate();
    }

    private static Material Material(string name, string texture, bool additive)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + texture + ".png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", additive ? 2 : 0);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0);
        material.SetFloat("_Cull", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static ParticleSystem Layer(GameObject root, string name, Material material, bool loop, float size,
        float life, float speed, float rate, short count, bool flipbook, Color tint)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        var p = go.AddComponent<ParticleSystem>();
        p.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = p.main;
        main.loop = loop; main.duration = loop ? 1f : 0.25f; main.playOnAwake = false;
        main.startLifetime = loop ? new ParticleSystem.MinMaxCurve(life * 0.8f, life) : new ParticleSystem.MinMaxCurve(life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.65f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.8f, size * 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
        main.startColor = tint;
        main.maxParticles = loop ? Mathf.CeilToInt(rate * life) + 4 : Math.Max(2, (int)count);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
        var emission = p.emission; emission.rateOverTime = rate;
        emission.SetBursts(count > 0 ? new[] { new ParticleSystem.Burst(0f, count) } : Array.Empty<ParticleSystem.Burst>());
        var shape = p.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = loop ? 0.045f : 0.05f * size;
        var color = p.colorOverLifetime; color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0.8f, 0.55f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        var growth = p.sizeOverLifetime; growth.enabled = true;
        growth.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.65f, 1, name.Contains("smoke") ? 1.6f : 1.1f));
        if (name.Contains("smoke"))
        {
            var velocity = p.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = loop ? 0.35f : 0.2f;
            var noise = p.noise; noise.enabled = true; noise.strength = 0.12f; noise.frequency = 0.65f; noise.quality = ParticleSystemNoiseQuality.Low;
        }
        if (flipbook)
        {
            var animation = p.textureSheetAnimation; animation.enabled = true;
            animation.numTilesX = 8; animation.numTilesY = 8;
            animation.animation = ParticleSystemAnimationType.WholeSheet;
            animation.frameOverTime = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0, 1, 0.999f));
            animation.cycleCount = 1;
        }
        var renderer = p.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.maxParticleSize = 2f;
        p.useAutoRandomSeed = false; p.randomSeed = (uint)(root.transform.childCount * 73 + 11);
        return p;
    }

    [MenuItem("Tools/HY Sandbox/UNI VFX/Validate Dependencies")]
    public static void Validate()
    {
        string[] paths = new[] { "Explosion", "BreakBurst", "ImpactBurst", "SmokeBurst", "DetachedSmoke" }
            .Select(n => BlockVfxBaker.Root + "/" + n + ".prefab").ToArray();
        string[] leaked = AssetDatabase.GetDependencies(paths, true).Where(p => p.StartsWith("Assets/UNI VFX/")).ToArray();
        if (leaked.Length > 0) throw new InvalidOperationException(string.Join(", ", leaked));
        foreach (string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var renderer = p.GetComponent<ParticleSystemRenderer>();
                if (renderer.sharedMaterial == null || !renderer.sharedMaterial.shader.isSupported) throw new InvalidOperationException(path);
                if (renderer.sharedMaterial.mainTexture == null) throw new InvalidOperationException("Missing flipbook: " + path);
            }
        }
    }
}

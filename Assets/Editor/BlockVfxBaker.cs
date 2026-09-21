using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>One-time derivation of editable VFX from the curated vendor assets.</summary>
public static class BlockVfxBaker
{
    public const string Root = "Assets/Art/BlockVisuals/VFX";
    private const string Source = "Assets/Art/SpaceKit/Prefabs/VFX/";

    public static void BakeMissing()
    {
        BlockArtDependencies.EnsureFolder(Root);
        Jet("ThrusterJet", 0.09f, 2.8f, 0.24f);
        Jet("HoverJet", 0.22f, 1.5f, 0.3f);
        Jet("BotFlight", 0.035f, 0.8f, 0.18f);
        Single("RepairContact", "Energy/Polygon/FX_Electricity.prefab", true, 0.16f, 0.18f, 0f, 12f, 0);
        Single("RepairPulse", "Energy/Polygon/FX_Electricity.prefab", false, 0.22f, 0.22f, 0f, 0f, 3);
        Single("BuildBurst", "Energy/Polygon/FX_Electricity.prefab", false, 0.3f, 0.3f, 1.1f, 0f, 12);
        Single("MuzzleFlash", "Combat/Polygon/FX_Laser_Shot.prefab", false, 0.17f, 0.065f, 0f, 0f, 1);
        Single("ImpactBurst", "Combat/Polygon/FX_Laser_Shot.prefab", false, 0.24f, 0.12f, 0.8f, 0f, 5);
        Smoke("DetachedSmoke", true);
        Smoke("SmokeBurst", false);
        Explosion("Explosion", 0.32f);
        Explosion("BreakBurst", 0.16f);
        CreateLibrary();
        AssetDatabase.SaveAssets();
    }

    public static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/" + name + ".prefab");

    private static GameObject Clone(string path, Transform parent = null)
    {
        var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Source + path));
        BlockArtDependencies.Unpack(root);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        foreach (var particles in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.maxParticles = Mathf.Min(main.maxParticles, 128);
            var collision = particles.collision; collision.enabled = false;
            var sub = particles.subEmitters; sub.enabled = false;
            var lights = particles.lights; lights.enabled = false;
        }
        return root;
    }

    private static void Jet(string name, float radius, float speed, float life)
    {
        if (Load(name) != null) return;
        var root = new GameObject(name);
        try
        {
            var trail = Clone("Propulsion/Polygon/FX_Exhaust_Trail.prefab", root.transform);
            trail.name = "Exhaust stream";
            var p = trail.GetComponent<ParticleSystem>();
            var main = p.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = life; main.startSpeed = speed; main.startSize = radius * 1.5f; main.maxParticles = 64;
            var emission = p.emission; emission.rateOverTime = 65f;
            var shape = p.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone; shape.radius = radius; shape.angle = 4f;
            var core = Clone("Propulsion/Polygon/FX_Flame_Booster_Round.prefab", root.transform);
            core.name = "Nozzle core";
            // The imported booster mesh points along Y. Its source prefab rotates it to local Z.
            core.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            p = core.GetComponent<ParticleSystem>();
            main = p.main; main.loop = true; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.09f; main.startSize = radius * 2.2f; main.maxParticles = 8;
            emission = p.emission; emission.rateOverTime = 24f;
            PolishJet(root, radius, speed, life);
            Save(root, name);
        }
        finally { Object.DestroyImmediate(root); }
    }

    public static void PolishJet(GameObject root, float radius, float speed, float life)
    {
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(new Color(0.5f, 1.5f, 2f), 0f), new GradientColorKey(new Color(0.04f, 0.45f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.65f, 0f), new GradientAlphaKey(0.25f, 0.6f), new GradientAlphaKey(0f, 1f) });
        foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
        {
            p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            bool core = p.name == "Nozzle core";
            var main = p.main; main.startSize3D = false; main.startRotation3D = false; main.startRotation = 0f;
            main.startSize = core ? radius / 0.08f : radius * 0.75f;
            main.startLifetime = core ? 0.075f : life;
            main.startSpeed = core ? 0f : speed;
            main.startColor = Color.white;
            var rotation = p.rotationOverLifetime; rotation.enabled = false;
            var color = p.colorOverLifetime; color.enabled = true; color.color = gradient;
            var size = p.sizeOverLifetime; size.enabled = true; size.separateAxes = false;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));
            var emission = p.emission; emission.rateOverTime = core ? 18f : 50f;
            if (core) p.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            else
            {
                var renderer = p.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Stretch;
                renderer.lengthScale = 2f; renderer.velocityScale = 0.12f;
                renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/SpaceKit/Materials/Polygon/Materials/FX/FX_SphereGlow.mat");
            }
        }
    }

    private static void Single(string name, string source, bool loop, float size, float life, float speed, float rate, short burst)
    {
        if (Load(name) != null) return;
        var root = Clone(source);
        try
        {
            // Laser templates also contain moving projectiles; hitscan uses just the authored flash.
            foreach (Transform child in root.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(child.gameObject);
            var p = root.GetComponent<ParticleSystem>();
            var main = p.main; main.loop = loop; main.duration = loop ? 1f : 0.15f;
            main.startLifetime = life; main.startSize3D = false; main.startSize = size; main.startSpeed = speed;
            main.simulationSpace = loop ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
            main.maxParticles = 48;
            var velocity = p.velocityOverLifetime; velocity.enabled = false;
            var emission = p.emission; emission.rateOverTime = rate;
            emission.SetBursts(burst > 0 ? new[] { new ParticleSystem.Burst(0f, burst) } : Array.Empty<ParticleSystem.Burst>());
            var shape = p.shape; shape.enabled = speed > 0f; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = 0.03f;
            if (source.Contains("Electricity"))
            {
                var noise = p.noise; noise.strength = 0.18f; noise.frequency = 2f;
                var trails = p.trails; trails.lifetime = 0.15f; trails.widthOverTrail = new ParticleSystem.MinMaxCurve(0.4f);
            }
            Save(root, name);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Smoke(string name, bool loop)
    {
        if (Load(name) != null) return;
        var root = Clone("DamageSmoke/Polygon/FX_Steam.prefab");
        try
        {
            var p = root.GetComponent<ParticleSystem>();
            var main = p.main; main.loop = loop; main.duration = 0.2f; main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.4f); main.startSpeed = 0.15f;
            main.startColor = new Color(0.32f, 0.38f, 0.45f, 0.48f); main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            var e = p.emission; e.rateOverTime = loop ? 24f : 0f;
            e.SetBursts(loop ? Array.Empty<ParticleSystem.Burst>() : new[] { new ParticleSystem.Burst(0f, 15) });
            var shape = p.shape; shape.radius = 0.07f;
            Save(root, name);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Explosion(string name, float scale)
    {
        if (Load(name) != null) return;
        var root = Clone("Combat/Polygon/FX_Explosion.prefab");
        try
        {
            foreach (var p in root.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = p.main; main.loop = false; main.duration = 0.2f;
                main.startLifetimeMultiplier = Mathf.Min(main.startLifetimeMultiplier, 1.5f);
                main.startSizeMultiplier *= scale; main.startSpeedMultiplier *= scale;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = 80;
                var e = p.emission; e.rateOverTime = 0f;
                short count = (short)(p == root.GetComponent<ParticleSystem>() ? 1 : p.name.Contains("Smoke") ? 24 : 12);
                e.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });
                var shape = p.shape; shape.radius *= scale;
            }
            Save(root, name);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static void Save(GameObject root, string name)
    {
        root.name = name;
        var controller = root.AddComponent<AssetParticleEffect>();
        SetObjects(controller, "_particles", root.GetComponentsInChildren<ParticleSystem>(true));
        SetObjects(controller, "_lights", root.GetComponentsInChildren<Light>(true));
        PrefabUtility.SaveAsPrefabAsset(root, Root + "/" + name + ".prefab");
    }

    public static void SetObjects(Object target, string field, Object[] values)
    {
        using (var data = new SerializedObject(target))
        {
            var property = data.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    public static void SetObject(Object target, string field, Object value)
    {
        using (var data = new SerializedObject(target))
        { data.FindProperty(field).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    }

    private static void CreateLibrary()
    {
        const string path = "Assets/Resources/VFX/BlockVfxLibrary.asset";
        if (AssetDatabase.LoadAssetAtPath<BlockVfxLibrary>(path) != null) return;
        BlockArtDependencies.EnsureFolder("Assets/Resources/VFX");
        BlockArtDependencies.EnsureFolder("Assets/Art/BlockVisuals/Materials");
        var line = new Material(Shader.Find("Sprites/Default")) { name = "Beam and Trail" };
        AssetDatabase.CreateAsset(line, "Assets/Art/BlockVisuals/Materials/Beam and Trail.mat");
        var library = ScriptableObject.CreateInstance<BlockVfxLibrary>();
        AssetDatabase.CreateAsset(library, path);
        SetObjects(library, "_bursts", new[] { "BuildBurst", "BreakBurst", "Explosion", "SmokeBurst", "RepairPulse", "MuzzleFlash", "ImpactBurst" }.Select(n => (Object)Load(n).GetComponent<AssetParticleEffect>()).ToArray());
        SetObject(library, "_detachedSmoke", Load("DetachedSmoke").GetComponent<AssetParticleEffect>());
        SetObject(library, "_lineMaterial", line);
    }
}

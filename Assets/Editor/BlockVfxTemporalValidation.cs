using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Deterministic time sampling and URP frame sequences in an isolated preview scene.</summary>
public static class BlockVfxTemporalValidation
{
    [MenuItem("Tools/HY Sandbox/Block Art/Validate Particle Continuity")]
    public static void Validate()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var rows = new List<object>();
        var errors = new List<string>();
        try
        {
            foreach (string name in new[] { "ThrusterJet", "HoverJet", "BotFlight", "RepairContact" })
            foreach (int fps in new[] { 30, 60, 120 })
            foreach (float intensity in new[] { 0.02f, 0.05f, 0.1f, 0.25f, 1f })
            {
                var root = Spawn(name, scene);
                try
                {
                    var effect = root.GetComponent<AssetParticleEffect>();
                    effect.SetIntensity(intensity);
                    foreach (var p in root.GetComponentsInChildren<ParticleSystem>())
                    {
                        p.Simulate(1f, false, true, false);
                        var buffer = new ParticleSystem.Particle[p.main.maxParticles];
                        var properties = new MaterialPropertyBlock();
                        p.GetComponent<ParticleSystemRenderer>().GetPropertyBlock(properties);
                        float materialAlpha = properties.GetColor("_BaseColor").a;
                        int empty = 0, samples = fps * 3;
                        double sum = 0, square = 0;
                        for (int frame = 0; frame < samples; frame++)
                        {
                            p.Simulate(1f / fps, false, false, false);
                            int count = p.GetParticles(buffer);
                            float alpha = 0f;
                            for (int i = 0; i < count; i++) alpha += buffer[i].GetCurrentColor(p).a / 255f * materialAlpha;
                            if (alpha <= 0f) empty++;
                            sum += alpha; square += alpha * alpha;
                        }
                        double mean = sum / samples;
                        double variation = Math.Sqrt(Math.Max(0, square / samples - mean * mean)) / Math.Max(mean, 0.000001);
                        rows.Add(new { name, emitter = p.name, fps, intensity, samples, emptyFrames = empty, alphaMean = mean, alphaVariation = variation });
                        if (empty != 0 || variation > 0.12) errors.Add($"Unstable {name}/{p.name}: {fps} fps, {intensity}, empty={empty}, CV={variation}");
                        if (Math.Abs(materialAlpha - intensity) > 0.0001f) errors.Add($"Intensity not applied to {name}/{p.name}");
                    }
                    effect.SetIntensity(0f);
                    var particles = root.GetComponentsInChildren<ParticleSystem>();
                    if (particles.Any(p => p.isEmitting)) errors.Add(name + " emits after shutdown");
                    foreach (var p in particles) p.Simulate(0.05f, false, false, false);
                    effect.SetIntensity(intensity);
                    if (particles.Any(p => !p.isEmitting)) errors.Add(name + " failed rapid restart with live particles");
                    root.SetActive(false);
                    if (particles.Any(p => p.particleCount != 0)) errors.Add(name + " retains particles after disable");
                }
                finally { Object.DestroyImmediate(root); }
            }
            foreach (string name in new[] { "MuzzleFlash", "ImpactBurst", "RepairPulse", "BuildBurst", "Explosion", "BreakBurst", "SmokeBurst" })
            {
                var root = Spawn(name, scene);
                try
                {
                    var effect = root.GetComponent<AssetParticleEffect>();
                    var particles = root.GetComponentsInChildren<ParticleSystem>();
                    effect.PlayOnce();
                    foreach (var p in particles) p.Simulate(0.03f, false, true, false);
                    bool visible = particles.Any(p => p.particleCount > 0);
                    foreach (var p in particles) p.Simulate(4f, false, false, false);
                    // Simulate leaves systems paused; IsAlive can remain true after their
                    // non-looping timeline ends. Runtime completion is checked by the Play probe.
                    bool finished = particles.All(p => !p.main.loop && p.particleCount == 0);
                    effect.PlayOnce();
                    foreach (var p in particles) p.Simulate(0.03f, false, true, false);
                    bool replay = particles.Any(p => p.particleCount > 0);
                    rows.Add(new { name, visible, finished, replay });
                    if (!visible || !finished || !replay) errors.Add(name + " burst lifetime/replay failure");
                }
                finally { Object.DestroyImmediate(root); }
            }
            File.WriteAllText("Assets/Art/BlockVisuals/TemporalValidation.json", Newtonsoft.Json.JsonConvert.SerializeObject(
                new { unity = Application.unityVersion, method = "Isolated Unity ParticleSystem simulation; 1 s warm-up + 3 s samples. Alpha includes renderer intensity; variation is stddev/mean, not pixel brightness.", rows, errors }, Newtonsoft.Json.Formatting.Indented));
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
        if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
    }

    private static GameObject Spawn(string name, Scene scene)
    {
        var root = Object.Instantiate(BlockVfxBaker.Load(name));
        SceneManager.MoveGameObjectToScene(root, scene);
        foreach (var p in root.GetComponentsInChildren<ParticleSystem>())
        {
            p.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            p.useAutoRandomSeed = false; p.randomSeed = 12345;
        }
        return root;
    }

    public static void RenderSequence(string name, float intensity, string folder, int frames = 60, float warmup = 1f)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(384, 256, 24);
        var texture = new Texture2D(384, 256, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            var root = Spawn(name, scene);
            var particles = root.GetComponentsInChildren<ParticleSystem>();
            var effect = root.GetComponent<AssetParticleEffect>();
            if (particles.Any(p => p.main.loop)) effect.SetIntensity(intensity);
            else effect.PlayOnce();
            foreach (var p in particles) p.Simulate(warmup, false, true, false);
            var cameraObject = new GameObject("VFX temporal camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene; camera.targetTexture = target;
            camera.transform.position = new Vector3(1.1f, 0.55f, -1.1f);
            camera.transform.LookAt(new Vector3(0f, 0f, 0.2f));
            camera.orthographic = true; camera.orthographicSize = name == "BotFlight" ? 0.18f : 0.55f;
            if (name == "Explosion" || name == "BreakBurst") camera.orthographicSize = 2.5f;
            camera.nearClipPlane = 0.01f; camera.farClipPlane = 20f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.02f, 0.025f, 0.035f);
            Directory.CreateDirectory(folder);
            for (int frame = 0; frame < frames; frame++)
            {
                foreach (var p in particles) p.Simulate(1f / 60f, false, false, false);
                camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 384, 256), 0, 0); texture.Apply();
                File.WriteAllBytes(folder + "/" + frame.ToString("D3") + ".png", texture.EncodeToPNG());
            }
            camera.targetTexture = null;
        }
        finally
        {
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(target); Object.DestroyImmediate(texture);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class UniVfxPreview
{
    [MenuItem("Tools/HY Sandbox/UNI VFX/Render and Validate Playback")]
    public static void Validate()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(640, 480, 24);
        var image = new Texture2D(640, 480, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        var rows = new List<object>();
        var errors = new List<string>();
        string folder = UniVfxIntegration.Root + "/Preview";
        BlockArtDependencies.EnsureFolder(folder);
        try
        {
            var cameraObject = new GameObject("UNI Preview Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene; camera.targetTexture = target; camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.14f, 0.19f);
            camera.orthographic = true; camera.nearClipPlane = 0.01f; camera.farClipPlane = 100f;
            camera.transform.position = new Vector3(5, 3, -6); camera.transform.LookAt(Vector3.zero);
            foreach (string name in new[] { "Explosion", "BreakBurst", "ImpactBurst", "SmokeBurst", "DetachedSmoke" })
            {
                GameObject root = Object.Instantiate(BlockVfxBaker.Load(name));
                SceneManager.MoveGameObjectToScene(root, scene);
                var particles = root.GetComponentsInChildren<ParticleSystem>();
                var effect = root.GetComponent<AssetParticleEffect>();
                bool trail = name == "DetachedSmoke";
                camera.orthographicSize = name == "Explosion" ? 3.8f : name == "BreakBurst" ? 2f : name == "ImpactBurst" ? 0.8f : 2f;
                if (trail) effect.SetIntensity(1f); else effect.PlayOnce();
                float last = 0;
                foreach (float time in new[] { 0.12f, 0.4f, 0.9f, 1.8f, 3.2f })
                {
                    int steps = Mathf.CeilToInt((time - last) * 60);
                    for (int i = 0; i < steps; i++)
                    {
                        float now = last + (i + 1) * (time - last) / steps;
                        if (trail)
                        {
                            root.transform.position = new Vector3(Mathf.Min(now, 1.8f) - 0.9f, 0, 0);
                            if (now >= 1.8f) effect.SetIntensity(0);
                        }
                        foreach (var p in particles) p.Simulate((time - last) / steps, false, false, false);
                    }
                    last = time;
                    camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 640, 480), 0, 0); image.Apply();
                    File.WriteAllBytes(folder + "/" + name + "-" + time.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + ".png", image.EncodeToPNG());
                    int alive = particles.Sum(p => p.particleCount);
                    rows.Add(new { name, time, alive, capacity = particles.Sum(p => p.main.maxParticles) });
                    if (time == 0.4f && alive == 0) errors.Add(name + " did not emit");
                    if (!trail && time == 3.2f && alive != 0) errors.Add(name + " exceeded release lifetime");
                }
                effect.SetIntensity(0);
                foreach (var p in particles) p.Simulate(2.4f, false, false, false);
                if (particles.Any(p => p.particleCount != 0)) errors.Add(name + " did not dissipate");
                if (trail) effect.SetIntensity(0.1f); else effect.PlayOnce();
                foreach (var p in particles) p.Simulate(0.5f, false, false, false);
                if (particles.All(p => p.particleCount == 0)) errors.Add(name + " restart failed");
                root.SetActive(false);
                if (particles.Any(p => p.particleCount != 0)) errors.Add(name + " disable did not clear");
                Object.DestroyImmediate(root);
            }
            File.WriteAllText(folder + "/Validation.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { unity = Application.unityVersion, rows, errors }, Newtonsoft.Json.Formatting.Indented));
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("; ", errors));
        }
        finally
        {
            RenderTexture.active = previous;
            EditorSceneManager.ClosePreviewScene(scene);
            Object.DestroyImmediate(target); Object.DestroyImmediate(image);
        }
        AssetDatabase.Refresh();
    }
}

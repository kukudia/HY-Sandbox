using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Isolated URP renders; never changes the user's open scene.</summary>
public static class BlockArtPreview
{
    public static void Render(string path, string output, bool effects = false, bool front = false, Vector3? view = null, Action<GameObject> configure = null, bool transparent = false)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(640, 520, 24);
        try
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            SceneManager.MoveGameObjectToScene(root, scene);
            root.transform.position = Vector3.zero;
            configure?.Invoke(root);
            foreach (var lod in root.GetComponentsInChildren<LODGroup>(true)) lod.ForceLOD(0);
            // GPU graphs need the actual player loop; this renderer is for static models only.
            foreach (var graph in root.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true)) graph.enabled = false;
            var solid = root.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is UnityEngine.VFX.VFXRenderer) && !(r is LineRenderer)).ToArray();
            Bounds bounds = solid.Length > 0 ? solid[0].bounds : new Bounds(Vector3.zero, Vector3.one * 2f);
            foreach (var renderer in solid.Skip(1)) bounds.Encapsulate(renderer.bounds);
            var cameraObject = new GameObject("Block Preview Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.scene = scene;
            camera.targetTexture = target;
            float radius = Mathf.Max(bounds.extents.magnitude, 0.2f);
            camera.transform.position = bounds.center + (view ?? new Vector3(1f, 0.65f, front ? 1f : -1f)).normalized * radius * (effects ? 4.4f : 3.5f);
            camera.transform.LookAt(bounds.center);
            camera.nearClipPlane = 0.005f;
            camera.farClipPlane = radius * 12f + 100f;
            camera.fieldOfView = 38f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = transparent ? Color.clear : new Color(0.065f, 0.08f, 0.11f);
            for (int i = 0; i < 2; i++)
            {
                var lightObject = new GameObject("Preview Light");
                SceneManager.MoveGameObjectToScene(lightObject, scene);
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = i == 0 ? 3f : 1.5f;
                light.transform.rotation = i == 0 ? Quaternion.Euler(35f, -35f, 0f) : Quaternion.Euler(330f, 145f, 0f);
            }
            camera.Render();
            var old = RenderTexture.active;
            var texture = new Texture2D(640, 520, transparent ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
            try
            {
                RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 640, 520), 0, 0);
                texture.Apply();
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllBytes(output, texture.EncodeToPNG());
            }
            finally { RenderTexture.active = old; Object.DestroyImmediate(texture); }
            camera.targetTexture = null;
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); Object.DestroyImmediate(target); }
    }

    public static void RenderBlocks(string folder, bool effects = false)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Blocks" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Render(path, folder + "/" + Path.GetFileNameWithoutExtension(path) + ".png", effects);
        }
    }
}

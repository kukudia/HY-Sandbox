using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Renders the saved health hierarchy using real uGUI layout at several output resolutions.</summary>
public static class HealthHudPreview
{
    [MenuItem("Tools/Build Palette/Validate health HUD resolutions")]
    public static string Render()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before rendering HUD previews.");
        var main = SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
        var source = main.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainUIPanels>(true)).Single().playPanel.transform.Find("CockpitHealthBar");
        Directory.CreateDirectory("Temp/BuildRange");
        foreach (Vector2Int size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(2560, 1440), new Vector2Int(3440, 1440) })
            RenderOne(source, size);
        return "Health HUD: 1280x720, 1920x1080, 2560x1440 and 3440x1440 layouts and renders passed.";
    }

    private static void RenderOne(Transform source, Vector2Int size)
    {
        var scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(size.x, size.y, 24);
        Texture2D texture = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            var cameraObject = new GameObject("HUD preview camera"); SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.scene = scene; camera.targetTexture = target;
            camera.orthographic = true; camera.nearClipPlane = 0.01f; camera.farClipPlane = 10;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.07f, 0.1f, 0.14f);
            var canvasObject = new GameObject("HUD preview", typeof(RectTransform), typeof(Canvas)); SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            RectTransform health = (RectTransform)Object.Instantiate(source, canvas.transform, false); health.gameObject.SetActive(true);
            Image fill = health.Find("HealthBar/Fill").GetComponent<Image>(); fill.fillAmount = 0.5f;
            fill.color = source.GetComponentInParent<MainUIPanels>().healthBarColor.Evaluate(0.5f);
            health.Find("CockpitHealthValue").GetComponent<Text>().text = "50 / 100";
            Canvas.ForceUpdateCanvases(); camera.Render(); Canvas.ForceUpdateCanvases();
            Vector3[] corners = new Vector3[4]; health.GetWorldCorners(corners);
            Vector3 lower = camera.WorldToScreenPoint(corners[0]); Vector3 upper = camera.WorldToScreenPoint(corners[2]);
            if (Mathf.Abs(lower.x - 28) > 1 || Mathf.Abs(lower.y - 28) > 1 || upper.x > size.x || upper.y > size.y)
                throw new InvalidOperationException("Health HUD is outside its expected screen margin at " + size + ": " + lower + "/" + upper);
            foreach (RectTransform child in health.GetComponentsInChildren<RectTransform>(true))
            {
                child.GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 point = camera.WorldToScreenPoint(corner);
                    if (point.x < lower.x - 0.1f || point.y < lower.y - 0.1f || point.x > upper.x + 0.1f || point.y > upper.y + 0.1f)
                        throw new InvalidOperationException(child.name + " overflows the health HUD at " + size);
                }
            }
            camera.Render(); RenderTexture.active = target;
            texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0); texture.Apply();
            File.WriteAllBytes("Temp/BuildRange/Health-" + size.x + "x" + size.y + ".png", texture.EncodeToPNG());
            camera.targetTexture = null;
        }
        finally
        {
            RenderTexture.active = previous;
            if (texture != null) Object.DestroyImmediate(texture);
            Object.DestroyImmediate(target); EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}

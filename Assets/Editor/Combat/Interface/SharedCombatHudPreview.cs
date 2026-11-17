// 职责：用真实Prefab和uGUI相机渲染共享战斗HUD，检查多分辨率边界及字形。
// 入口：Render()；创建并清理独立Preview Scene，不修改生产场景。
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SharedCombatHudPreview
{
    [MenuItem("Tools/HY-Sandbox/Combat/Render shared HUD resolutions")]
    public static string Render()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before preview rendering.");
        }

        Directory.CreateDirectory("Temp/SharedCombatHud");
        var graphicsErrors = new List<string>();
        Application.LogCallback capture = (message, trace, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                graphicsErrors.Add(message);
            }
        };
        Application.logMessageReceived += capture;
        try
        {
            foreach (Vector2Int size in new[]
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1440),
                new Vector2Int(3440, 1440)
            }

            )
            {
                RenderOne(size);
                if (graphicsErrors.Count > 0)
                {
                    throw new InvalidOperationException("HUD preview produced engine errors: " + string.Join("; ", graphicsErrors));
                }
            }
        }
        finally
        {
            Application.logMessageReceived -= capture;
        }

        return "Four shared HUD resolutions rendered and checked";
    }

    private static void RenderOne(Vector2Int size)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = new RenderTexture(size.x, size.y, 24);
        RenderTexture previous = RenderTexture.active;
        Texture2D texture = null;
        try
        {
            if (!target.Create())
            {
                throw new InvalidOperationException("Could not allocate HUD preview target at " + size);
            }

            var cameraObject = new GameObject("Preview Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.scene = scene;
            camera.targetTexture = target;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.06f, .09f, .13f);
            var canvasObject = new GameObject("Preview Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            GameObject root = Object.Instantiate(
                AssetDatabase.LoadAssetAtPath<GameObject>(SharedCombatHudAuthoring.PrefabPath),
                canvas.transform,
                false);
            foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(component is UnityEngine.EventSystems.UIBehaviour))
                {
                    component.enabled = false;
                }
            }

            foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true))
            {
                group.alpha = 1f;
            }

            RectTransform plate = root.transform.Find("World Markers/EnemyNameplateTemplate") as RectTransform;
            plate.gameObject.SetActive(true);
            plate.anchoredPosition = new Vector2(-220, 40);
            CombatNameplate view = plate.GetComponent<CombatNameplate>();
            CombatHudTheme theme = AssetDatabase.LoadAssetAtPath<CombatHudTheme>(SharedCombatHudAuthoring.Folder + "/Theme.asset");
            view.Bind(theme, "HOSTILE / ENEMY RAIDER", theme.hostile);
            view.Sample(680, 1000, 80, 100, true);
            view.Present(plate.anchoredPosition, true, .3f);
            root.transform.Find("Battle Tools/Debug Panel").GetComponent<CanvasGroup>().alpha = 0f;
            Transform threats = root.transform.Find("World Markers/Threat Markers");
            for (int i = 0; i < threats.childCount; i++)
            {
                threats.GetChild(i).GetComponent<CanvasGroup>().alpha = i == 0 ? 1f : 0f;
            }

            ((RectTransform)threats.GetChild(0)).anchoredPosition = new Vector2(-700, 80);
            Transform window = root.transform.Find("Battle Tools/ThrusterInfoPanel/Window");
            window.Find("Owner").GetComponent<TMP_Text>().text = "STARTER FINCH / GUIDANCE";
            window.Find("Empty").gameObject.SetActive(false);
            GameObject row = window.Find("Viewport/Content/Row Template").gameObject;
            row.SetActive(true);
            row.transform.Find("Name").GetComponent<TMP_Text>().text = "Main Thruster";
            row.transform.Find("Value").GetComponent<TMP_Text>().text = "50 / 100 N | PWR 100%";
            ((RectTransform)row.transform.Find("Track/Fill")).anchorMax = new Vector2(.5f, 1f);
            RectTransform feed = (RectTransform)root.transform.Find("Notifications/Kill Feed");
            feed.anchoredPosition = new Vector2(-420, -155);
            for (int i = 0; i < 5; i++)
            {
                Transform item = feed.GetChild(i);
                item.Find("Title").GetComponent<TMP_Text>().text = "BASTION > HOSTILE RAIDER " + (i + 1);
                item.Find("Detail").GetComponent<TMP_Text>().text = "WAVE 3 / +120 PENDING";
            }

            root.transform.Find("Notifications/KillNotice/EnemyName").GetComponent<TMP_Text>().text = "HOSTILE RAIDER";
            root.transform.Find("Notifications/KillNotice/Count").GetComponent<TMP_Text>().text = "WAVE 3 / +120 PENDING";
            Canvas.ForceUpdateCanvases();
            camera.Render();
            Canvas.ForceUpdateCanvases();
            string[] names =
            {
                "Battle Tools/ThrusterInfoPanel/Window",
                "Battle Tools/DEBUG  F2",
                "Battle Tools/THRUST  F1",
                "Notifications/Kill Feed",
                "Notifications/KillNotice"
            };
            foreach (string path in names)
            {
                Vector3[] corners = new Vector3[4];
                ((RectTransform)root.transform.Find(path)).GetWorldCorners(corners);
                foreach (Vector3 corner in corners)
                {
                    Vector3 point = camera.WorldToScreenPoint(corner);
                    if (point.x < -1 || point.x > size.x + 1 || point.y < -1 || point.y > size.y + 1)
                    {
                        throw new InvalidOperationException(path + " overflows at " + size + " : " + point);
                    }
                }
            }

            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>())
            {
                label.ForceMeshUpdate();
                if (!string.IsNullOrEmpty(label.text) && label.textInfo.characterCount == 0)
                {
                    throw new InvalidOperationException("Missing text geometry: " + label.name);
                }
            }

            camera.Render();
            RenderTexture.active = target;
            texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            texture.Apply();
            File.WriteAllBytes("Temp/SharedCombatHud/Layout-" + size.x + "x" + size.y + ".png", texture.EncodeToPNG());
            camera.targetTexture = null;
        }
        finally
        {
            RenderTexture.active = previous;
            if (texture != null)
            {
                Object.DestroyImmediate(texture);
            }

            target.Release();
            Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }
}

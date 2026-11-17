// 职责：以保存的真实Canvas制作工坊、暂停及设置页多分辨率预览，检查可见交互区域。
// 入口：Render()/RenderSiege()；独立PreviewScene，禁止运行态写回，不初始化个人存档。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class WorkshopLayoutPreview
{
    public static string RenderSiege()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before rendering saved layouts.");
        }

        Scene source = SceneManager.GetSceneByPath("Assets/Scenes/SiegeGameplay.unity");
        SiegeCombatHud entry = source.isLoaded ? SceneObjectQuery.FindInScene<SiegeCombatHud>(source) : null;
        if (entry == null)
        {
            throw new InvalidOperationException("Load saved SiegeGameplay before previewing.");
        }

        Directory.CreateDirectory("Temp/WorkshopLayout");
        var errors = new System.Collections.Generic.List<string>();
        Application.LogCallback capture = (message, trace, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors.Add(message);
            }
        };
        Application.logMessageReceived += capture;
        try
        {
            foreach (Vector2Int size in new[]
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1280, 800),
                new Vector2Int(1024, 768),
                new Vector2Int(3440, 1440)
            }

            )
            {
                foreach (string page in new[]
                {
                    "SiegeBattle",
                    "SiegeDebug",
                    "SiegeRecovery",
                    "SiegeFailure"
                }

                )
                {
                    RenderOne(entry.GetComponentInParent<Canvas>(), page, size);
                    if (errors.Count > 0)
                    {
                        throw new InvalidOperationException(string.Join("; ", errors));
                    }
                }
            }
        }
        finally
        {
            Application.logMessageReceived -= capture;
        }

        return "16 saved Siege layouts rendered; interactive bounds and nameplate exclusions checked.";
    }

    public static string Render()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before rendering saved layouts.");
        }

        Scene build = SceneManager.GetSceneByPath(MainMenuController.MainPath);
        Scene menu = SceneManager.GetSceneByPath(WorkshopNavigation.MenuPath);
        if (!build.isLoaded || !menu.isLoaded)
        {
            throw new InvalidOperationException("Load saved Build and MainMenu scenes before previewing.");
        }

        Canvas workshop = build.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Single(c => c.name == "MainCanvas");
        Canvas settings = menu.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Single();
        Directory.CreateDirectory("Temp/WorkshopLayout");
        var errors = new System.Collections.Generic.List<string>();
        Application.LogCallback capture = (message, trace, type) =>
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errors.Add(message);
            }
        };
        Application.logMessageReceived += capture;
        try
        {
            foreach (Vector2Int size in new[]
            {
                new Vector2Int(1280, 720),
                new Vector2Int(1280, 800),
                new Vector2Int(1024, 768),
                new Vector2Int(1920, 1080),
                new Vector2Int(2560, 1440),
                new Vector2Int(3440, 1440)
            }

            )
            {
                foreach (string page in new[]
                {
                    "Workshop",
                    "Pause",
                    "General",
                    "Preference"
                }

                )
                {
                    RenderOne(page == "General" || page == "Preference" ? settings : workshop, page, size);
                    if (errors.Count > 0)
                    {
                        throw new InvalidOperationException(string.Join("; ", errors));
                    }
                }
            }
        }
        finally
        {
            Application.logMessageReceived -= capture;
        }

        return "24 saved UI layouts rendered; interactive bounds and button separation checked.";
    }

    private static void RenderOne(Canvas source, string page, Vector2Int size)
    {
        Scene scene = EditorSceneManager.NewPreviewScene();
        var target = new RenderTexture(size.x, size.y, 24);
        RenderTexture previous = RenderTexture.active;
        Texture2D image = null;
        Camera camera = null;
        try
        {
            if (!target.Create())
            {
                throw new InvalidOperationException("Texture allocation failed: " + size);
            }

            var cameraRoot = new GameObject("Layout Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraRoot, scene);
            camera = cameraRoot.GetComponent<Camera>();
            camera.scene = scene;
            camera.targetTexture = target;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.06f, .09f, .13f);
            GameObject root = Object.Instantiate(source.gameObject);
            SceneManager.MoveGameObjectToScene(root, scene);
            foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (!(script is UnityEngine.EventSystems.UIBehaviour))
                {
                    script.enabled = false;
                }
            }

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            if (page == "Workshop" || page == "Pause")
            {
                foreach (Transform child in root.transform)
                {
                    child.gameObject.SetActive(child.name == "BuildPanel" || child.name == "Main Menu" || (page == "Pause" && child.name == "Pause Overlay"));
                }
            }
            else if (page.StartsWith("Siege", StringComparison.Ordinal))
            {
                PrepareSiege(root, page);
            }
            else
            {
                GameObject CopyOf(SerializedObject owner, string name)
                {
                    Object value = owner.FindProperty(name).objectReferenceValue;
                    Transform sourceTransform = value is GameObject gameObject ? gameObject.transform : ((Component)value).transform;
                    return root.transform.Find(AnimationUtility.CalculateTransformPath(sourceTransform, source.transform)).gameObject;
                }

                MainMenuView view = SceneObjectQuery.FindInScene<MainMenuView>(source.gameObject.scene);
                var viewData = new SerializedObject(view);
                CopyOf(viewData, "home").SetActive(false);
                CopyOf(viewData, "picker").SetActive(false);
                CopyOf(viewData, "settingsPanel").SetActive(true);
                MenuSettings settings = SceneObjectQuery.FindInScene<MenuSettings>(source.gameObject.scene);
                var data = new SerializedObject(settings);
                CopyOf(data, "generalPage").SetActive(page == "General");
                CopyOf(data, "preferencePage").SetActive(page == "Preference");
            }

            Canvas.ForceUpdateCanvases();
            camera.Render();
            Selectable[] controls = root.GetComponentsInChildren<Selectable>().Where(s => s.isActiveAndEnabled && s.GetComponentsInParent<CanvasGroup>().All(g => g.alpha > .01f)).ToArray();
            Rect Bounds(Selectable control)
            {
                var corners = new Vector3[4];
                control.GetComponent<RectTransform>().GetWorldCorners(corners);
                Vector3 low = camera.WorldToScreenPoint(corners[0]);
                Vector3 high = camera.WorldToScreenPoint(corners[2]);
                return Rect.MinMaxRect(low.x, low.y, high.x, high.y);
            }

            foreach (Selectable control in controls)
            {
                Rect bounds = Bounds(control);
                // Scroll contents may extend beyond a clipped viewport by design.
                if (control.GetComponentInParent<ScrollRect>() == null && (bounds.xMin < -1 || bounds.yMin < -1 || bounds.xMax > size.x + 1 || bounds.yMax > size.y + 1))
                {
                    throw new InvalidOperationException(page + "/" + control.name + " leaves screen at " + size + ": " + bounds);
                }
            }

            Button[] buttons = controls.OfType<Button>().ToArray();
            for (int i = 0; i < buttons.Length; i++)
            {
                for (int j = i + 1; j < buttons.Length; j++)
                {
                    if (buttons[i].transform.parent == buttons[j].transform.parent && Bounds(buttons[i]).Overlaps(Bounds(buttons[j])))
                    {
                        throw new InvalidOperationException(page + " overlapping buttons: " + buttons[i].name + " / " + buttons[j].name + " at " + size);
                    }
                }
            }

            RenderTexture.active = target;
            image = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            image.Apply();
            File.WriteAllBytes("Temp/WorkshopLayout/" + page + "-" + size.x + "x" + size.y + ".png", image.EncodeToPNG());
            camera.targetTexture = null;
        }
        finally
        {
            if (camera != null)
            {
                camera.targetTexture = null;
            }

            RenderTexture.active = previous;
            if (image != null)
            {
                Object.DestroyImmediate(image);
            }

            target.Release();
            Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void PrepareSiege(GameObject root, string page)
    {
        SiegeCombatHud entry = root.GetComponentInChildren<SiegeCombatHud>(true);
        var data = new SerializedObject(entry);
        void SetText(string field, string value)
        {
            ((TMPro.TMP_Text)data.FindProperty(field).objectReferenceValue).text = value;
        }

        SetText("summary", "BASE CORE 1200 / 1200\nAirKing / AI\nHULL 61700 / 61900");
        SetText("phaseLabel", page == "SiegeRecovery" ? "RECOVERY / RETURN TO BASE" : "WAVE LIMIT");
        SetText("countdown", "01:34");
        SetText(
            "battleStatus",
            "CREDITS 5,188  +1,200 PENDING\nHOSTILES 1  /  KILLS 23" + (page == "SiegeRecovery" ? "\nMOVEMENT CHECK  15s" : string.Empty));
        SetText(
            "failureDetail",
            "CORE LOST\nYour original blueprint is preserved.\nRestart the same base or continue to the menu.");
        foreach (string field in new[]
        {
            "nextWave",
            "repairBase"
        }

        )
        {
            ((Component)data.FindProperty(field).objectReferenceValue).gameObject.SetActive(false);
        }

        CanvasGroup failure = (CanvasGroup)data.FindProperty("failure").objectReferenceValue;
        failure.gameObject.SetActive(page == "SiegeFailure");
        failure.alpha = page == "SiegeFailure" ? 1f : 0f;
        CombatHud hud = entry.Hud;
        hud.ResetHud();
        foreach (CanvasGroup group in hud.GetComponentsInChildren<CanvasGroup>(true))
        {
            if (group.name == "Debug Panel" || group.name == "Window")
            {
                group.alpha = group.name == "Debug Panel" && page == "SiegeDebug" ? 1f : 0f;
                group.blocksRaycasts = group.interactable = group.alpha > .01f;
            }
        }

        var hudData = new SerializedObject(hud);
        RectTransform overlay = (RectTransform)hudData.FindProperty("_overlay").objectReferenceValue;
        SerializedProperty regions = hudData.FindProperty("nameplateExclusions");
        if (regions.arraySize < 16)
        {
            throw new InvalidOperationException("Siege must reserve its own summary, actions, clock, status, failure and camera help.");
        }

        Canvas.ForceUpdateCanvases();
        var corners = new Vector3[4];
        for (int index = 0; index < regions.arraySize; index++)
        {
            CanvasGroup group = (CanvasGroup)regions.GetArrayElementAtIndex(index).objectReferenceValue;
            if (group == null || !group.gameObject.activeInHierarchy || group.alpha <= .01f)
            {
                continue;
            }

            ((RectTransform)group.transform).GetWorldCorners(corners);
            Vector3 center = overlay.InverseTransformPoint((corners[0] + corners[2]) * .5f);
            Rect probe = new Rect((Vector2)center - Vector2.one, Vector2.one * 2);
            if (hud.IsNameplateAreaAvailable(probe))
            {
                throw new InvalidOperationException("Visible Siege UI does not block nameplates: " + group.name);
            }
        }
    }
}

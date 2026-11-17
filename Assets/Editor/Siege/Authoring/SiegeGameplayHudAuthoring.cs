// 职责：原位迁移Siege战斗界面与共享Prefab，保留已有实例覆盖和用户调整。
// 入口：Upgrade() / ConfigureStage()；显式保存真实资产，修改前备份四场景与Prefab。
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeGameplayHudAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Siege/Upgrade combat flow and HUD")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save scenes and exit Play before migration.");
        }

        string backup = "Temp/SiegeHudMigration-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(backup);
        string[] paths =
        {
            MainMenuController.MainPath,
            "Assets/Scenes/Siege.unity",
            "Assets/Scenes/Random5v5Arena.unity",
            "Assets/Scenes/SiegeGameplay.unity"
        };
        foreach (string path in paths)
        {
            File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
        }

        File.Copy(SharedCombatHudAuthoring.PrefabPath, Path.Combine(backup, "SharedCombatHud.prefab"));
        GameObject prefab = PrefabUtility.LoadPrefabContents(SharedCombatHudAuthoring.PrefabPath);
        try
        {
            Restyle(prefab.transform);
            PrefabUtility.SaveAsPrefabAsset(prefab, SharedCombatHudAuthoring.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }

        CombatHudTheme theme = AssetDatabase.LoadAssetAtPath<CombatHudTheme>(SharedCombatHudAuthoring.Folder + "/Theme.asset");
        theme.background = new Color(.025f, .055f, .085f, .62f);
        theme.friendly = new Color(.35f, .85f, .94f, .95f);
        theme.hostile = new Color(1f, .43f, .33f, .95f);
        theme.warning = new Color(1f, .76f, .37f, .95f);
        EditorUtility.SetDirty(theme);
        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in paths)
            {
                Scene scene = EditorSceneManager.OpenScene(path);
                SiegeGameplayStage stage = SceneObjectQuery.FindInScene<SiegeGameplayStage>(scene);
                if (stage != null)
                {
                    ConfigureStage(stage);
                }

                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    {
                        // Style only battle-owned surfaces, never rewrite Main's unrelated construction/menu UI.
                        if (stage != null)
                        {
                            Soften(canvas.transform);
                        }
                    }
                }

                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        AssetDatabase.SaveAssets();
    }

    public static void ConfigureStage(SiegeGameplayStage stage)
    {
        Transform canvas = stage.transform.Find("Siege Gameplay UI");
        Remove(canvas.Find("Construction Controls"));
        Remove(canvas.Find("Return"));
        Remove(canvas.Find("Heading"));
        SiegeCombatHud entry = stage.GetComponentInChildren<SiegeCombatHud>(true);
        if (entry == null)
        {
            return;
        }

        Transform root = entry.transform;
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SharedCombatHudAuthoring.Folder + "/ChakraPetch SDF.asset");
        // Keep pre-existing summary, ally selection and authored shared HUD instance overrides.
        var data = new SerializedObject(entry);
        RectTransform phase = Box(root, "Wave Clock", new Vector2(.5f, 1), new Vector2(0, -62), new Vector2(240, 84));
        Set(
            data,
            "phaseLabel",
            Label(
                phase,
                "Phase",
                font,
                new Vector2(0, 22),
                new Vector2(220, 22),
                14));
        Set(
            data,
            "countdown",
            Label(
                phase,
                "Time",
                font,
                new Vector2(0, -8),
                new Vector2(220, 38),
                30));
        Set(
            data,
            "battleStatus",
            Label(
                root,
                "Battle Status",
                font,
                new Vector2(20, -170),
                new Vector2(330, 80),
                14,
                new Vector2(0, 1)));
        Set(
            data,
            "nextWave",
            Button(
                root,
                "StartNextWave",
                "START NEXT WAVE",
                font,
                new Vector2(.5f, 0),
                new Vector2(0, 54),
                new Vector2(270, 44)));
        Set(
            data,
            "repairBase",
            Button(
                root,
                "RepairBase",
                "REPAIR BASE",
                font,
                new Vector2(.5f, 0),
                new Vector2(0, 106),
                new Vector2(270, 36)));
        RectTransform failure = Box(root, "Siege Failure", new Vector2(.5f, .5f), Vector2.zero, new Vector2(580, 270));
        CanvasGroup group = Ensure<CanvasGroup>(failure);
        group.alpha = 0f;
        group.blocksRaycasts = group.interactable = false;
        Label(
            failure,
            "Title",
            font,
            new Vector2(0, 88),
            new Vector2(530, 42),
            32).text = "CORE LOST  /  SIEGE FAILED";
        TMP_Text detail = Label(
            failure,
            "Detail",
            font,
            Vector2.zero,
            new Vector2(530, 108),
            16);
        detail.textWrappingMode = TextWrappingModes.Normal;
        Set(data, "failure", group);
        Set(data, "failureDetail", detail);
        Set(
            data,
            "dismissFailure",
            Button(
                failure,
                "Continue",
                "CONTINUE",
                font,
                new Vector2(.5f, .5f),
                new Vector2(0, -94),
                new Vector2(220, 40)));
        Button continueButton = (Button)data.FindProperty("dismissFailure").objectReferenceValue;
        continueButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(140, -94);
        Set(
            data,
            "restartFailure",
            Button(
                failure,
                "Restart",
                "RESTART",
                font,
                new Vector2(.5f, .5f),
                new Vector2(-140, -94),
                new Vector2(220, 40)));
        data.ApplyModifiedPropertiesWithoutUndo();
        failure.gameObject.SetActive(false);
        foreach (Text text in canvas.GetComponentsInChildren<Text>(true))
        {
            text.color = new Color(.78f, .87f, .94f, .92f);
        }

        Transform title = canvas.Find("Title");
        if (title != null)
        {
            title.gameObject.SetActive(false);
        }

        Transform help = canvas.Find("Camera Help");
        if (help != null)
        {
            RectTransform rect = (RectTransform)help;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 12);
            rect.sizeDelta = new Vector2(740, 22);
            help.GetComponent<Text>().fontSize = 12;
        }

        Soften(canvas);
        EditorSceneManager.MarkSceneDirty(stage.gameObject.scene);
    }

    public static void Restyle(Transform root)
    {
        Soften(root);
        foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
        {
            toggle.targetGraphic.raycastTarget = true;
        }

        foreach (CombatThreatView view in root.GetComponentsInChildren<CombatThreatView>(true))
        {
            Transform arrow = view.transform.Find("Arrow");
            if (arrow != null)
            {
                TMP_Text old = arrow.GetComponent<TMP_Text>();
                if (old != null)
                {
                    Object.DestroyImmediate(old);
                }

                CombatThreatIcon icon = Ensure<CombatThreatIcon>(arrow);
                icon.color = new Color(1f, .43f, .33f, .9f);
                icon.raycastTarget = false;
                ((RectTransform)arrow).sizeDelta = new Vector2(36, 36);
            }
        }
    }

    private static void Soften(Transform root)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            Color color = image.color;
            color.a = Mathf.Min(color.a, image.name.Contains("Fill") || image.name == "Check" ? .9f : .62f);
            image.color = color;
        }
    }

    private static RectTransform Box(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null ? (RectTransform)existing : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = Ensure<Image>(rect);
        image.color = new Color(.035f, .075f, .11f, .62f);
        image.raycastTarget = false;
        return rect;
    }

    private static TMP_Text Label(
        Transform parent,
        string name,
        TMP_FontAsset font,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Vector2? anchor = null)
    {
        Transform existing = parent.Find(name);
        RectTransform rect = existing != null ? (RectTransform)existing : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor ?? new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text label = Ensure<TextMeshProUGUI>(rect);
        label.font = font;
        label.fontSize = fontSize;
        label.color = new Color(.74f, .88f, .95f, .95f);
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(
        Transform parent,
        string name,
        string caption,
        TMP_FontAsset font,
        Vector2 anchor,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = Box(parent, name, anchor, position, size);
        rect.GetComponent<Image>().raycastTarget = true;
        Button button = Ensure<Button>(rect);
        button.targetGraphic = rect.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(.55f, .9f, 1f);
        colors.pressedColor = new Color(.3f, .65f, .8f);
        button.colors = colors;
        Label(
            rect,
            "Label",
            font,
            Vector2.zero,
            size - new Vector2(16, 4),
            16).text = caption;
        return button;
    }

    private static void Set(SerializedObject data, string field, Object value)
    {
        data.FindProperty(field).objectReferenceValue = value;
    }

    private static T Ensure<T>(Transform owner)
        where T : Component
    {
        T component = owner.GetComponent<T>();
        return component != null ? component : owner.gameObject.AddComponent<T>();
    }

    private static void Remove(Transform owned)
    {
        if (owned != null)
        {
            Object.DestroyImmediate(owned.gameObject);
        }
    }
}

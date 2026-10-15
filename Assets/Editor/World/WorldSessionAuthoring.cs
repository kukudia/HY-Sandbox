// 职责：创建可编辑世界配置与会话 Prefab，并将后台预热及加载界面接入 Main。
// 入口：Install()；重复执行保留配置、Prefab 参数及已存在的界面布局。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class WorldSessionAuthoring
{
    public const string MainScenePath = "Assets/Scenes/Main.unity";
    public const string SettingsPath = "Assets/World/WorldGeneration.asset";
    public const string PrefabPath = "Assets/World/WorldSession.prefab";
    [MenuItem("Tools/HY-Sandbox/World/Install Main world session")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before installing the world session.");
        }

        Scene current = SceneManager.GetActiveScene();
        if (current.isDirty)
        {
            throw new InvalidOperationException("Save the active scene before installing the world session.");
        }

        if (!AssetDatabase.IsValidFolder("Assets/World"))
        {
            AssetDatabase.CreateFolder("Assets", "World");
        }

        WorldGenerationSettings configuration = AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>(SettingsPath);
        if (configuration == null)
        {
            configuration = ScriptableObject.CreateInstance<WorldGenerationSettings>();
            AssetDatabase.CreateAsset(configuration, SettingsPath);
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            prefab = CreatePrefab(configuration);
        }

        Scene scene = current.path == MainScenePath ? current : EditorSceneManager.OpenScene(MainScenePath);
        WorldSessionManager session = FindInScene<WorldSessionManager>(scene);
        if (session == null)
        {
            session = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, scene)).GetComponent<WorldSessionManager>();
            Undo.RegisterCreatedObjectUndo(session.gameObject, "Install world session");
        }

        PlayManager play = FindInScene<PlayManager>(scene);
        if (play == null)
        {
            throw new InvalidOperationException("Main has no PlayManager.");
        }

        var playData = new SerializedObject(play);
        playData.FindProperty("worldSession").objectReferenceValue = session;
        playData.ApplyModifiedProperties();
        WorldLoadingPanel panel = FindInScene<WorldLoadingPanel>(scene);
        if (panel == null)
        {
            panel = CreateLoadingPanel(scene);
        }

        Image backdrop = panel.GetComponent<Image>();
        Color backdropColor = backdrop.color;
        backdropColor.a = 1f;
        backdrop.color = backdropColor;
        EditorUtility.SetDirty(backdrop);
        var sessionData = new SerializedObject(session);
        sessionData.FindProperty("loadingPanel").objectReferenceValue = panel;
        SerializedProperty environment = sessionData.FindProperty("buildEnvironment");
        if (environment.arraySize == 0)
        {
            GameObject[] grounds = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).Where(item => item.name.StartsWith("Plane", StringComparison.Ordinal) && item.GetComponent<Renderer>() != null && IsInEnvironment(item)).Select(item => item.gameObject).ToArray();
            environment.arraySize = grounds.Length;
            for (int i = 0; i < grounds.Length; i++)
            {
                environment.GetArrayElementAtIndex(i).objectReferenceValue = grounds[i];
            }

            if (grounds.Length == 0)
            {
                throw new InvalidOperationException("No build grounds found under Environment; inspect Main before configuring buildEnvironment.");
            }
        }

        sessionData.ApplyModifiedProperties();
        PrefabUtility.RecordPrefabInstancePropertyModifications(session);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = session.gameObject;
        Debug.Log("Main world session installed. Configuration: " + SettingsPath + "; prefab: " + PrefabPath);
    }

    private static bool IsInEnvironment(Transform item)
    {
        for (Transform parent = item.parent; parent != null; parent = parent.parent)
        {
            if (parent.name == "Environment")
            {
                return true;
            }
        }

        return false;
    }

    private static GameObject CreatePrefab(WorldGenerationSettings configuration)
    {
        var root = new GameObject("WorldSession");
        try
        {
            WorldSessionManager session = root.AddComponent<WorldSessionManager>();
            var content = new GameObject("WorldRoot");
            content.transform.SetParent(root.transform, false);
            content.transform.localPosition = new Vector3(0f, -14f, 0f);
            content.SetActive(false);
            var terrainObject = new GameObject("Streaming Terrain");
            terrainObject.transform.SetParent(content.transform, false);
            ProceduralVoxelWorld world = terrainObject.AddComponent<ProceduralVoxelWorld>();
            var worldData = new SerializedObject(world);
            worldData.FindProperty("streaming").boolValue = true;
            SerializedProperty materials = worldData.FindProperty("materials");
            string[] names =
            {
                "Ochre",
                "Basalt",
                "Alloy"
            };
            materials.arraySize = names.Length;
            for (int i = 0; i < names.Length; i++)
            {
                Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/WorldLab/" + names[i] + ".mat");
                if (material == null)
                {
                    throw new InvalidOperationException("Missing WorldLab terrain material: " + names[i]);
                }

                materials.GetArrayElementAtIndex(i).objectReferenceValue = material;
            }

            worldData.ApplyModifiedPropertiesWithoutUndo();
            var buildingsObject = new GameObject("Streaming Buildings");
            buildingsObject.transform.SetParent(content.transform, false);
            WorldBuildingManager buildings = buildingsObject.AddComponent<WorldBuildingManager>();
            var buildingsData = new SerializedObject(buildings);
            buildingsData.FindProperty("world").objectReferenceValue = world;
            buildingsData.FindProperty("catalog").objectReferenceValue = AssetDatabase.LoadAssetAtPath<WorldBuildingCatalog>("Assets/WorldLab/Buildings/Catalog.asset");
            buildingsData.FindProperty("initializeOnStart").boolValue = false;
            buildingsData.ApplyModifiedPropertiesWithoutUndo();
            var sessionData = new SerializedObject(session);
            sessionData.FindProperty("settings").objectReferenceValue = configuration;
            sessionData.FindProperty("world").objectReferenceValue = world;
            sessionData.FindProperty("buildings").objectReferenceValue = buildings;
            sessionData.FindProperty("worldRoot").objectReferenceValue = content;
            sessionData.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static WorldLoadingPanel CreateLoadingPanel(Scene scene)
    {
        Canvas canvas = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>(true)).FirstOrDefault(item => item.name == "MainCanvas");
        if (canvas == null)
        {
            throw new InvalidOperationException("MainCanvas is missing.");
        }

        Font font = canvas.GetComponentsInChildren<Text>(true).Select(text => text.font).FirstOrDefault(value => value != null);
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        RectTransform panelRoot = Rect(
            "World Preparation",
            canvas.transform,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        panelRoot.gameObject.AddComponent<Canvas>().overrideSorting = true;
        panelRoot.GetComponent<Canvas>().sortingOrder = 100;
        panelRoot.gameObject.AddComponent<GraphicRaycaster>();
        Image backdrop = panelRoot.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.015f, 0.025f, 0.035f, 1f);
        CanvasGroup group = panelRoot.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        WorldLoadingPanel panel = panelRoot.gameObject.AddComponent<WorldLoadingPanel>();
        RectTransform card = Rect(
            "Content",
            panelRoot,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(560f, 270f));
        Label(
            "Title",
            card,
            "WORLD PREPARATION",
            font,
            new Vector2(0f, 85f),
            new Vector2(540f, 45f),
            28f,
            new Color(0.84f, 0.95f, 0.96f));
        Text message = Label(
            "Status",
            card,
            "Preparing terrain and buildings...",
            font,
            new Vector2(0f, 30f),
            new Vector2(540f, 42f),
            18f,
            new Color(0.6f, 0.76f, 0.78f));
        RectTransform track = Rect(
            "Progress Track",
            card,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -12f),
            new Vector2(500f, 8f));
        track.gameObject.AddComponent<Image>().color = new Color(0.15f, 0.23f, 0.25f);
        RectTransform fillRect = Rect(
            "Progress",
            track,
            Vector2.zero,
            Vector2.one,
            Vector2.zero,
            Vector2.zero);
        Image fill = fillRect.gameObject.AddComponent<Image>();
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        fill.color = new Color(0.25f, 0.83f, 0.78f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 0f;
        RectTransform buttonRect = Rect(
            "Cancel",
            card,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -85f),
            new Vector2(170f, 42f));
        Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
        buttonImage.color = new Color(0.12f, 0.2f, 0.22f);
        Button button = buttonRect.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        Label(
            "Label",
            buttonRect,
            "CANCEL",
            font,
            Vector2.zero,
            new Vector2(165f, 40f),
            16f,
            Color.white);
        var panelData = new SerializedObject(panel);
        panelData.FindProperty("group").objectReferenceValue = group;
        panelData.FindProperty("message").objectReferenceValue = message;
        panelData.FindProperty("progress").objectReferenceValue = fill;
        panelData.FindProperty("cancelButton").objectReferenceValue = button;
        panelData.ApplyModifiedPropertiesWithoutUndo();
        Undo.RegisterCreatedObjectUndo(panelRoot.gameObject, "Create world loading panel");
        return panel;
    }

    private static RectTransform Rect(
        string name,
        Transform parent,
        Vector2 minimum,
        Vector2 maximum,
        Vector2 position,
        Vector2 size)
    {
        var value = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        value.SetParent(parent, false);
        value.anchorMin = minimum;
        value.anchorMax = maximum;
        value.anchoredPosition = position;
        value.sizeDelta = size;
        return value;
    }

    private static Text Label(
        string name,
        Transform parent,
        string value,
        Font font,
        Vector2 position,
        Vector2 size,
        float fontSize,
        Color color)
    {
        RectTransform rect = Rect(
            name,
            parent,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            position,
            size);
        var label = rect.gameObject.AddComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = Mathf.RoundToInt(fontSize);
        label.alignment = TextAnchor.MiddleCenter;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static T FindInScene<T>(Scene scene)
        where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }

    [MenuItem("Tools/HY-Sandbox/World/Select world configuration")]
    public static void SelectConfiguration()
    {
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<WorldGenerationSettings>(SettingsPath);
    }

    [MenuItem("Tools/HY-Sandbox/World/Preview or regenerate session in Play Mode")]
    public static void RegeneratePreview()
    {
        if (!EditorApplication.isPlaying || (PlayManager.instance != null && PlayManager.instance.playMode))
        {
            throw new InvalidOperationException("Enter Main Play Mode and stay in build mode before regenerating the world preview.");
        }

        WorldSessionManager session = Object.FindFirstObjectByType<WorldSessionManager>();
        if (session == null)
        {
            throw new InvalidOperationException("Install the Main world session first.");
        }

        session.EndSession();
        Selection.activeGameObject = session.gameObject;
    }
}

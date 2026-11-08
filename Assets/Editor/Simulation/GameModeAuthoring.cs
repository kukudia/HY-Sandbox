// 职责：将Main中的世界会话迁移为探索副场景，保存模式入口与独立阵地战制作骨架。
// 入口：Install()；保留已有世界Prefab覆盖、Main界面布局和旧Siege场景，重复执行不重建资产。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class GameModeAuthoring
{
    public const string MainPath = "Assets/Scenes/Main.unity";
    public const string ExplorationPath = "Assets/Scenes/Exploration.unity";
    public const string SiegePath = "Assets/Scenes/SiegeGameplay.unity";
    [MenuItem("Tools/HY-Sandbox/Modes/Install gameplay subscenes")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Exit Play Mode and save open scenes before migrating gameplay scenes.");
        }

        Scene main = SceneManager.GetSceneByPath(MainPath);
        if (!main.isLoaded)
        {
            main = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(main);
        EnsureValidationTemplate();
        PlayManager play = SceneObjectQuery.FindInScene<PlayManager>(main);
        WorldLoadingPanel loading = SceneObjectQuery.FindInScene<WorldLoadingPanel>(main, FindObjectsInactive.Include);
        OutpostHud hud = SceneObjectQuery.FindInScene<OutpostHud>(main, FindObjectsInactive.Include);
        WorldSessionManager session = SceneObjectQuery.FindInScene<WorldSessionManager>(main, FindObjectsInactive.Include);
        GameModeController modes = SceneObjectQuery.FindInScene<GameModeController>(main);
        if (modes == null)
        {
            if (session == null || loading == null)
            {
                throw new InvalidOperationException("Main requires its existing world session and loading panel for first migration.");
            }

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ExplorationPath) != null)
            {
                throw new InvalidOperationException("Exploration already exists; inspect it before migrating another world session.");
            }

            var source = new SerializedObject(session);
            var environment = source.FindProperty("buildEnvironment");
            GameObject[] grounds = Enumerable.Range(0, environment.arraySize).Select(i => (GameObject)environment.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            modes = play.gameObject.AddComponent<GameModeController>();
            var modeData = new SerializedObject(modes);
            Set(modeData, "worldLoading", loading);
            Set(modeData, "outpostHud", hud);
            SerializedProperty targets = modeData.FindProperty("buildEnvironment");
            targets.arraySize = grounds.Length;
            for (int i = 0; i < grounds.Length; i++)
            {
                targets.GetArrayElementAtIndex(i).objectReferenceValue = grounds[i];
            }

            Set(modeData, "panel", CreateModePanel(main));
            modeData.ApplyModifiedPropertiesWithoutUndo();
            source.FindProperty("buildEnvironment").arraySize = 0;
            Set(source, "loadingPanel", null);
            source.FindProperty("prepareOnStart").boolValue = false;
            source.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(session);
            var playData = new SerializedObject(play);
            Set(playData, "worldSession", null);
            Set(playData, "modes", modes);
            playData.ApplyModifiedPropertiesWithoutUndo();
            if (hud != null)
            {
                var hudData = new SerializedObject(hud);
                Set(hudData, "_session", null);
                hudData.ApplyModifiedPropertiesWithoutUndo();
            }

            Scene exploration = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.MoveGameObjectToScene(session.transform.root.gameObject, exploration);
            EditorSceneManager.SaveScene(exploration, ExplorationPath);
            EditorSceneManager.CloseScene(exploration, true);
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SiegePath) == null)
        {
            CreateSiegeScene(main);
        }

        EnsureCombatPools(main);
        SceneManager.SetActiveScene(main);
        // Preserve all existing entries (including the prototype) while ensuring Main is the player entrypoint.
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(MainPath, true)
        };
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != MainPath && s.path != ExplorationPath && s.path != SiegePath));
        scenes.Add(new EditorBuildSettingsScene(ExplorationPath, true));
        scenes.Add(new EditorBuildSettingsScene(SiegePath, true));
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(WorkshopNavigation.MenuPath) != null)
        {
            scenes.RemoveAll(s => s.path == WorkshopNavigation.MenuPath);
            scenes.Insert(0, new EditorBuildSettingsScene(WorkshopNavigation.MenuPath, true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
        EditorSceneManager.MarkSceneDirty(main);
        EditorSceneManager.SaveScene(main);
        AssetDatabase.SaveAssets();
        Debug.Log("Gameplay subscenes installed: Main -> Exploration / SiegeGameplay; legacy Siege preserved.");
    }

    private static GameModePanel CreateModePanel(Scene main)
    {
        Canvas canvas = main.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).First(c => c.name == "MainCanvas");
        RectTransform root = Rect("Play Mode Selection", canvas.transform, Vector2.zero, Vector2.zero);
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        Image backdrop = root.gameObject.AddComponent<Image>();
        backdrop.color = new Color(0.025f, 0.035f, 0.06f, 1f);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        TextAt(
            "Title",
            root,
            "SIMULATE",
            new Vector2(0, 190),
            new Vector2(760, 55),
            34);
        Text status = TextAt(
            "Status",
            root,
            "Choose a play mode",
            new Vector2(0, 120),
            new Vector2(760, 80),
            19);
        Button exploration = ButtonAt("Exploration", root, "EXPLORATION", new Vector2(-190, 0), new Vector2(340, 62));
        Button siege = ButtonAt("Siege", root, "SIEGE  /  IN DEVELOPMENT", new Vector2(190, 0), new Vector2(340, 62));
        TextAt(
            "Details",
            root,
            "Explore a generated world, or open the new Siege stage.\nYour construction is preserved when you return.",
            new Vector2(0, -85),
            new Vector2(760, 70),
            17);
        Button retry = ButtonAt("Retry", root, "RETRY", new Vector2(0, 0), new Vector2(340, 62));
        retry.gameObject.SetActive(false);
        Button cancel = ButtonAt("Cancel", root, "BACK TO BUILD", new Vector2(0, -175), new Vector2(270, 48));
        GameModePanel panel = root.gameObject.AddComponent<GameModePanel>();
        var data = new SerializedObject(panel);
        Set(data, "group", group);
        Set(data, "status", status);
        Set(data, "exploration", exploration);
        Set(data, "siege", siege);
        Set(data, "retry", retry);
        Set(data, "cancel", cancel);
        data.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    private static void EnsureValidationTemplate()
    {
        const string path = "Assets/Resources/Validation/WorldSession.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Resources/Validation"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Validation");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WorldSessionAuthoring.PrefabPath));
        try
        {
            // A variant retains the original settings/materials; no second production world implementation.
            PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    private static void EnsureCombatPools(Scene main)
    {
        Scene exploration = SceneManager.GetSceneByPath(ExplorationPath);
        bool opened = !exploration.isLoaded;
        if (opened)
        {
            exploration = EditorSceneManager.OpenScene(ExplorationPath, OpenSceneMode.Additive);
        }

        // Projectiles and detached smoke explicitly request their owning scene; copy only authored pool roots.
        CopyPool<VfxEffectPool>(main, exploration);
        CopyPool<TurretProjectilePool>(main, exploration);
        EditorSceneManager.SaveScene(exploration);
        if (opened)
        {
            EditorSceneManager.CloseScene(exploration, true);
        }
    }

    private static void CopyPool<T>(Scene main, Scene target)
        where T : Component
    {
        if (SceneObjectQuery.FindInScene<T>(target) != null)
        {
            return;
        }

        T source = SceneObjectQuery.FindInScene<T>(main);
        GameObject copy = Object.Instantiate(source.gameObject);
        copy.name = source.name;
        copy.transform.SetParent(null, false);
        SceneManager.MoveGameObjectToScene(copy, target);
    }

    private static void CreateSiegeScene(Scene main)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var root = new GameObject("Siege Gameplay");
        SiegeGameplayStage stage = root.AddComponent<SiegeGameplayStage>();
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Arena Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.position = new Vector3(0f, -4f, 0f);
        ground.transform.localScale = new Vector3(200f, 2f, 200f);
        Renderer template = main.GetRootGameObjects().Where(r => r.name == "Environment").SelectMany(r => r.GetComponentsInChildren<Renderer>(true)).FirstOrDefault();
        if (template != null)
        {
            ground.GetComponent<Renderer>().sharedMaterial = template.sharedMaterial;
        }

        var overview = new GameObject("Overview");
        overview.transform.SetParent(root.transform);
        overview.transform.SetPositionAndRotation(new Vector3(0, 70, -90), Quaternion.Euler(36, 0, 0));
        var baseSpawn = new GameObject("Base Spawn");
        baseSpawn.transform.SetParent(root.transform);
        baseSpawn.transform.position = new Vector3(0, -3, 25);
        var playerSpawn = new GameObject("Player Deployment");
        playerSpawn.transform.SetParent(root.transform);
        playerSpawn.transform.position = new Vector3(0, 0, -25);
        var canvasRoot = new GameObject(
            "Siege Gameplay UI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(root.transform, false);
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        TextAt(
            "Heading",
            canvas.transform,
            "SIEGE",
            new Vector2(0, 270),
            new Vector2(700, 60),
            36);
        TextAt(
            "Stage Status",
            canvas.transform,
            "NEW GAMEPLAY STAGE\nCombat and deployment are under development.",
            new Vector2(0, 205),
            new Vector2(800, 70),
            20);
        Button back = ButtonAt("Return", canvas.transform, "BACK TO BUILD", new Vector2(0, -280), new Vector2(280, 50));
        var data = new SerializedObject(stage);
        Set(data, "overview", overview.transform);
        Set(data, "returnButton", back);
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene, SiegePath);
        EditorSceneManager.CloseScene(scene, true);
        SceneManager.SetActiveScene(main);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text TextAt(
        string name,
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        Text text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.color = new Color(.86f, .92f, 1f);
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static Button ButtonAt(string name, Transform parent, string title, Vector2 position, Vector2 size)
    {
        RectTransform root = Rect(name, parent, position, size);
        Image image = root.gameObject.AddComponent<Image>();
        image.color = new Color(.1f, .22f, .31f, 1f);
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TextAt(
            "Label",
            root,
            title,
            Vector2.zero,
            size - new Vector2(12, 8),
            19);
        return button;
    }

    private static void Set(SerializedObject data, string name, Object value)
    {
        data.FindProperty(name).objectReferenceValue = value;
    }
}

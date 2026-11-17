// 职责：从独立战斗服务场景制作可编辑的待机空战舞台、出生点、烟火点和镜头轨道。
// 入口：Create()；已有场景拒绝覆盖，源5v5和用户蓝图均保持原样。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BattleAttractAuthoring
{
    public const string ScenePath = "Assets/Scenes/BattleAttract.unity";
    [MenuItem("Tools/HY-Sandbox/Presentation/Create battle attract scene")]
    public static void Create()
    {
        if (EditorApplication.isPlaying || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save open scenes and exit Play; an existing attract scene is preserved.");
        }

        if (!AssetDatabase.CopyAsset(SkirmishArenaAuthoring.ScenePath, ScenePath))
        {
            throw new InvalidOperationException("Could not copy authored combat services.");
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        foreach (string name in new[]
        {
            "Battlefield",
            "Automatic 5v5 Match",
            "Arena HUD",
            "Arena Navigation Debug",
            "Status Overlay"
        }

        )
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == name);
            if (root != null)
            {
                Object.DestroyImmediate(root);
            }
        }

        foreach (EnemyLabCamera old in Object.FindObjectsByType<EnemyLabCamera>())
        {
            Object.DestroyImmediate(old);
        }

        Camera camera = SceneObjectQuery.FindInScene<Camera>(scene);
        camera.name = "Cinematic Camera";
        camera.farClipPlane = 1400f;
        PlayManager play = SceneObjectQuery.FindInScene<PlayManager>(scene);
        play.playMode = true;
        play.blocksParent = null;
        play.allControlUnits.Clear();
        var playData = new SerializedObject(play);
        playData.FindProperty("skirmish").objectReferenceValue = null;
        playData.ApplyModifiedPropertiesWithoutUndo();
        VisualEffectsManager look = SceneObjectQuery.FindInScene<VisualEffectsManager>(scene);
        look.enableSceneLook = false;
        look.cameraShakeStrength = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = new Color(.12f, .19f, .26f);
        RenderSettings.fogDensity = .0018f;
        RenderSettings.ambientLight = new Color(.33f, .39f, .48f);
        Light sun = SceneObjectQuery.FindInScene<Light>(scene);
        sun.transform.rotation = Quaternion.Euler(16, -40, 0);
        sun.color = new Color(1f, .72f, .48f);
        sun.intensity = 1.7f;
        var rootStage = new GameObject("Battle Attract Stage");
        WorldBuildingManager buildings = rootStage.AddComponent<WorldBuildingManager>();
        var buildingData = new SerializedObject(buildings);
        buildingData.FindProperty("initializeOnStart").boolValue = false;
        buildingData.FindProperty("collapseUnsupported").boolValue = false;
        buildingData.ApplyModifiedPropertiesWithoutUndo();
        BattleAttractStage stage = rootStage.AddComponent<BattleAttractStage>();
        Transform anchor = Point(rootStage.transform, "Bastion anchor", new Vector3(0, 1, 0));
        Transform focus = Point(rootStage.transform, "Battle focus", new Vector3(0, 30, 0));
        var stageData = new SerializedObject(stage);
        Ref(
            stageData,
            "baseSettings",
            AssetDatabase.LoadAssetAtPath<SiegeSettings>("Assets/Siege/DefaultSiege.asset"));
        Ref(stageData, "buildings", buildings);
        Ref(stageData, "baseAnchor", anchor);
        Ref(stageData, "effects", SceneObjectQuery.FindInScene<VfxEffectPool>(scene));
        string[] names =
        {
            "CODEX_E01_Finch",
            "CODEX_E02_Kestrel",
            "CODEX_E03_Buzzard"
        };
        TextAsset[] candidates = Resources.LoadAll<TextAsset>("WorldBlueprints/Enemies").OrderBy(asset => asset.name).ToArray();
        TextAsset[] chosen = candidates.Where(asset => names.Contains(asset.name)).ToArray();
        if (chosen.Length < 2)
        {
            chosen = candidates.Take(3).ToArray();
        }

        SerializedProperty library = stageData.FindProperty("blueprints");
        library.arraySize = chosen.Length;
        for (int i = 0; i < chosen.Length; i++)
        {
            library.GetArrayElementAtIndex(i).objectReferenceValue = chosen[i];
        }

        SerializedProperty slots = stageData.FindProperty("slots");
        slots.arraySize = 8;
        for (int i = 0; i < slots.arraySize; i++)
        {
            bool friendly = i < 4;
            Transform point = Point(
                rootStage.transform,
                (friendly ? "Defender " : "Raider ") + (i % 4 + 1),
                new Vector3(friendly ? -105 : 125, 32 + i % 3 * 12, (i % 4 - 1.5f) * 42));
            point.rotation = Quaternion.LookRotation(friendly ? Vector3.right : Vector3.left);
            slots.GetArrayElementAtIndex(i).objectReferenceValue = point;
        }

        SerializedProperty bursts = stageData.FindProperty("explosionPoints");
        bursts.arraySize = 6;
        for (int i = 0; i < bursts.arraySize; i++)
        {
            float angle = i * Mathf.PI / 3f;
            bursts.GetArrayElementAtIndex(i).objectReferenceValue = Point(
                rootStage.transform,
                "Atmosphere burst " + (i + 1),
                new Vector3(Mathf.Cos(angle) * 65, 28 + i % 3 * 16, Mathf.Sin(angle) * 65));
        }

        stageData.ApplyModifiedPropertiesWithoutUndo();
        var canvasRoot = new GameObject("Cinematic Letterbox", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasRoot.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Bar(canvasRoot.transform, "Top frame", true);
        Bar(canvasRoot.transform, "Bottom frame", false);
        var captionRoot = new GameObject("Scene caption", typeof(RectTransform), typeof(Text));
        captionRoot.transform.SetParent(canvasRoot.transform, false);
        Text caption = captionRoot.GetComponent<Text>();
        caption.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        caption.fontSize = 19;
        caption.color = new Color(.57f, .83f, .94f);
        caption.raycastTarget = false;
        caption.rectTransform.anchorMin = caption.rectTransform.anchorMax = caption.rectTransform.pivot = new Vector2(0, 0);
        caption.rectTransform.anchoredPosition = new Vector2(36, 12);
        caption.rectTransform.sizeDelta = new Vector2(700, 55);
        BattleAttractCamera director = camera.gameObject.AddComponent<BattleAttractCamera>();
        var cameraData = new SerializedObject(director);
        Ref(cameraData, "view", camera);
        Ref(cameraData, "stage", stage);
        Ref(cameraData, "focalPoint", focus);
        Ref(cameraData, "caption", caption);
        Vector3[] starts =
        {
            new Vector3(28, 5, -34),
            new Vector3(-150, 48, -60),
            new Vector3(105, 95, -150)
        };
        Vector3[] ends =
        {
            new Vector3(-22, 7, -42),
            new Vector3(110, 62, 40),
            new Vector3(-90, 80, -130)
        };
        string[] shots =
        {
            "Bastion low angle",
            "Airspace flythrough",
            "Fleet panorama"
        };
        for (int i = 0; i < starts.Length; i++)
        {
            foreach (string field in new[]
            {
                "starts",
                "ends"
            }

            )
            {
                SerializedProperty array = cameraData.FindProperty(field);
                array.arraySize = starts.Length;
                array.GetArrayElementAtIndex(i).objectReferenceValue = Point(
                    rootStage.transform,
                    field == "starts" ? shots[i] : shots[i] + " end",
                    field == "starts" ? starts[i] : ends[i]);
            }
        }

        cameraData.ApplyModifiedPropertiesWithoutUndo();
        camera.transform.SetPositionAndRotation(starts[0], Quaternion.LookRotation(focus.position - starts[0]));
        Polish(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/HY-Sandbox/Presentation/Apply attract lighting and effect budget")]
    public static void PolishSavedScene()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before saving lighting.");
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath);
        }

        Polish(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void Polish(Scene scene)
    {
        SceneManager.SetActiveScene(scene);
        Camera camera = SceneObjectQuery.FindInScene<Camera>(scene);
        camera.clearFlags = CameraClearFlags.Skybox;
        var cameraData = camera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (cameraData == null)
        {
            cameraData = camera.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        }

        cameraData.renderPostProcessing = true;
        const string folder = "Assets/Art/Attract";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            AssetDatabase.CreateFolder("Assets/Art", "Attract");
        }

        const string skyPath = folder + "/EveningSky.mat";
        Material sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
        if (sky == null)
        {
            sky = new Material(Shader.Find("Skybox/Procedural"));
            sky.SetColor("_SkyTint", new Color(.5f, .5f, .5f));
            sky.SetColor("_GroundColor", new Color(.16f, .18f, .22f));
            sky.SetFloat("_AtmosphereThickness", 1f);
            sky.SetFloat("_Exposure", 1.1f);
            AssetDatabase.CreateAsset(sky, skyPath);
        }

        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.45f, .52f, .65f);
        RenderSettings.ambientEquatorColor = new Color(.28f, .34f, .42f);
        RenderSettings.ambientGroundColor = new Color(.12f, .14f, .19f);
        Light sun = Object.FindObjectsByType<Light>().First(light => light.type == LightType.Directional && light.gameObject.scene == scene);
        sun.transform.rotation = Quaternion.Euler(28, -40, 0);
        GameObject ground = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Ground");
        if (ground != null)
        {
            ground.transform.localScale = new Vector3(4800, 1, 4800);
        }

        var pool = new SerializedObject(SceneObjectQuery.FindInScene<VfxEffectPool>(scene));
        pool.FindProperty("maximumActiveEffects").intValue = 160;
        pool.FindProperty("maximumActivePerTemplate").intValue = 24;
        pool.ApplyModifiedPropertiesWithoutUndo();
        var stage = new SerializedObject(SceneObjectQuery.FindInScene<BattleAttractStage>(scene));
        stage.FindProperty("explosionInterval").floatValue = 2.2f;
        stage.FindProperty("maximumAtmosphereEffects").intValue = 128;
        stage.ApplyModifiedPropertiesWithoutUndo();
        if (SceneObjectQuery.FindInScene<UnityEngine.Rendering.Volume>(scene) == null)
        {
            Scene source = EditorSceneManager.OpenScene(MainMenuController.MainPath, OpenSceneMode.Additive);
            UnityEngine.Rendering.Volume volume = SceneObjectQuery.FindInScene<UnityEngine.Rendering.Volume>(source);
            if (volume != null)
            {
                GameObject copy = Object.Instantiate(volume.gameObject);
                copy.name = "Cinematic Grade";
                SceneManager.MoveGameObjectToScene(copy, scene);
            }

            EditorSceneManager.CloseScene(source, true);
        }
    }

    private static Transform Point(Transform parent, string name, Vector3 position)
    {
        var point = new GameObject(name);
        point.transform.SetParent(parent, false);
        point.transform.position = position;
        return point.transform;
    }

    private static void Ref(SerializedObject target, string name, Object value)
    {
        target.FindProperty(name).objectReferenceValue = value;
    }

    private static void Bar(Transform parent, string name, bool top)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, top ? 1 : 0);
        rect.anchorMax = new Vector2(1, top ? 1 : 0);
        rect.pivot = new Vector2(.5f, top ? 1 : 0);
        rect.sizeDelta = new Vector2(0, 78);
        root.GetComponent<Image>().color = new Color(.012f, .021f, .035f, .96f);
        root.GetComponent<Image>().raycastTarget = false;
    }
}

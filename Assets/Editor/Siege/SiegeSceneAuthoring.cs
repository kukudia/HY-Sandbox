// 职责：通过Unity API创建独立、可编辑的阵地战场景和默认配置，不重写Main场景或用户存档。
// 入口：Create() / Open()；已有场景不覆盖，泊位、UI、材质和波次配置均保存为真实资产。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeSceneAuthoring
{
    public const string ScenePath = "Assets/Scenes/Siege.unity";
    private const string AssetFolder = "Assets/Siege";
    [MenuItem("HY-Sandbox/Siege/Create Editable Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before creating the siege scene.");
        }

        if (File.Exists(ScenePath))
        {
            Debug.Log("Siege scene already exists. Open it to edit; existing authoring is preserved.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Siege");
        }

        TextAsset baseBlueprint = CreateBaseBlueprint();
        SiegeSettings settings = CreateSettings(baseBlueprint);
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        try
        {
            CreateEnvironment();
            CreateManagers();
            var sessionObject = new GameObject("Siege Session");
            SiegeSession session = sessionObject.AddComponent<SiegeSession>();
            Transform coreDock = new GameObject("Base Dock").transform;
            coreDock.SetParent(sessionObject.transform);
            coreDock.position = new Vector3(-14f, 0f, -14f);
            Transform[] docks = CreateDocks(sessionObject.transform);
            var serialized = new SerializedObject(session);
            var buildingOwner = new GameObject("Base Building Manager");
            buildingOwner.transform.SetParent(sessionObject.transform, false);
            WorldBuildingManager buildings = buildingOwner.AddComponent<WorldBuildingManager>();
            SiegeBaseBlueprintAuthoring.ConfigureManager(buildings);
            serialized.FindProperty("buildings").objectReferenceValue = buildings;
            serialized.FindProperty("settings").objectReferenceValue = settings;
            serialized.FindProperty("baseDock").objectReferenceValue = coreDock;
            SetArray(serialized.FindProperty("shipDocks"), docks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            CreateCamera();
            CreateHud(session, settings, docks.Length);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            if (!buildScenes.Any(item => item.path == ScenePath))
            {
                EditorBuildSettings.scenes = buildScenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Created editable Siege scene, settings, base blueprint, materials and HUD.");
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
            SceneManager.SetActiveScene(previous);
        }
    }

    [MenuItem("HY-Sandbox/Siege/Open Scene")]
    public static void Open()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("HY-Sandbox/Siege/Regenerate Default Base Blueprint")]
    public static void RegenerateDefaultBase()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before regenerating the base blueprint.");
        }

        string path = AssetFolder + "/BastionBase.json";
        Directory.CreateDirectory("Temp/Siege");
        if (File.Exists(path))
        {
            File.Copy(path, "Temp/Siege/BastionBase-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + ".json");
        }

        CreateBaseBlueprint(true);
    }

    private static TextAsset CreateBaseBlueprint(bool replace = false)
    {
        return SiegeBaseBlueprintAuthoring.Generate(replace);
    }

    private static SiegeSettings CreateSettings(TextAsset baseBlueprint)
    {
        string path = AssetFolder + "/DefaultSiege.asset";
        SiegeSettings settings = AssetDatabase.LoadAssetAtPath<SiegeSettings>(path);
        if (settings != null)
        {
            return settings;
        }

        settings = ScriptableObject.CreateInstance<SiegeSettings>();
        var serialized = new SerializedObject(settings);
        serialized.FindProperty("baseBlueprint").objectReferenceValue = baseBlueprint;
        SerializedProperty ships = serialized.FindProperty("ships");
        ships.arraySize = 3;
        string[] files =
        {
            "CODEX_E01_Finch",
            "CODEX_E02_Pike",
            "CODEX_E03_Manta"
        };
        string[] names =
        {
            "Finch",
            "Pike",
            "Manta"
        };
        int[] prices =
        {
            500,
            850,
            650
        };
        for (int index = 0; index < files.Length; index++)
        {
            SerializedProperty option = ships.GetArrayElementAtIndex(index);
            option.FindPropertyRelative("displayName").stringValue = names[index];
            option.FindPropertyRelative("price").intValue = prices[index];
            option.FindPropertyRelative("blueprint").objectReferenceValue = Resources.Load<TextAsset>("WorldBlueprints/Enemies/" + files[index]);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(settings, path);
        return settings;
    }

    private static void CreateManagers()
    {
        var root = new GameObject("Managers");
        root.AddComponent<GameManager>().enabled = false;
        root.AddComponent<BuildManager>().enabled = false;
        PlayManager play = root.AddComponent<PlayManager>();
        play.groupCleanupDistance = 500f;
        root.AddComponent<DestroyManager>();
        root.AddComponent<SaveManager>();
        root.AddComponent<InputManager>().enabled = false;
        BuildManager build = root.GetComponent<BuildManager>();
        build.blockLayer = LayerMask.GetMask("Block");
        var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    private static void CreateEnvironment()
    {
        Material ground = MaterialAsset("ArenaGround", new Color(0.12f, 0.16f, 0.19f));
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Arena Ground";
        floor.transform.position = new Vector3(0f, -0.5f, 0f);
        floor.transform.localScale = new Vector3(400f, 1f, 400f);
        floor.GetComponent<Renderer>().sharedMaterial = ground;
        floor.isStatic = true;
        var lightObject = new GameObject("Directional Light", typeof(Light));
        lightObject.transform.rotation = Quaternion.Euler(45f, -25f, 0f);
        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.8f;
        light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.55f, 0.65f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.12f, 0.17f, 0.23f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.002f;
    }

    private static Transform[] CreateDocks(Transform parent)
    {
        Material padMaterial = MaterialAsset("DeploymentPad", new Color(0.08f, 0.32f, 0.36f));
        var result = new Transform[6];
        for (int index = 0; index < result.Length; index++)
        {
            Transform dock = new GameObject("Ship Bay " + (index + 1)).transform;
            dock.SetParent(parent);
            dock.position = new Vector3((index % 3 - 1) * 30f, 8f, 30f + index / 3 * 30f);
            result[index] = dock;
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Bay " + (index + 1) + " Platform";
            pad.transform.position = new Vector3(dock.position.x, 0.05f, dock.position.z);
            pad.transform.localScale = new Vector3(24f, 0.1f, 22f);
            pad.GetComponent<Renderer>().sharedMaterial = padMaterial;
            pad.isStatic = true;
        }

        return result;
    }

    private static Material MaterialAsset(string name, Color color)
    {
        string path = AssetFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"))
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
        }

        return material;
    }

    private static void CreateCamera()
    {
        var root = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraController));
        root.tag = "MainCamera";
        Camera camera = root.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.13f, 0.2f);
        camera.farClipPlane = 600f;
        root.transform.SetPositionAndRotation(new Vector3(0f, 35f, -65f), Quaternion.Euler(25f, 0f, 0f));
        CameraController controller = root.GetComponent<CameraController>();
        controller.currentMode = CameraMode.ThirdPersonLock;
        controller.thirdPersonOffset = new Vector3(0f, 8f, -38f);
        controller.minZoom = -15f;
        controller.maxZoom = -90f;
        Object.FindFirstObjectByType<PlayManager>().mainCamera = camera;
    }

    private static void CreateHud(SiegeSession session, SiegeSettings settings, int bays)
    {
        var root = new GameObject(
            "Siege HUD",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(SiegeHud));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform header = Panel(
            root.transform,
            "Command Header",
            32,
            32,
            1856,
            116);
        Text heading = Label(
            header,
            "BASTION / DEPLOYMENT",
            24,
            10,
            700,
            52,
            30);
        Text economy = Label(
            header,
            "CREDITS / WAVE / RAIDERS",
            800,
            20,
            1020,
            32,
            22);
        Text status = Label(
            header,
            "Deploy a fleet, then begin the first wave.",
            24,
            70,
            1780,
            30,
            20);
        RectTransform commands = Panel(
            root.transform,
            "Fleet Commands",
            32,
            172,
            330,
            860);
        Label(
            commands,
            "DEPLOY FLEET",
            20,
            18,
            290,
            32,
            24);
        var deploy = new Button[settings.Ships.Length];
        for (int index = 0; index < deploy.Length; index++)
        {
            deploy[index] = ActionButton(
                commands,
                "Deploy " + index,
                settings.Ships[index].DisplayName,
                20,
                70 + index * 60,
                290,
                48);
        }

        Label(
            commands,
            "TAKE CONTROL",
            20,
            275,
            290,
            32,
            24);
        var control = new Button[bays];
        for (int index = 0; index < bays; index++)
        {
            control[index] = ActionButton(
                commands,
                "Pilot Bay " + (index + 1),
                "BAY " + (index + 1),
                20,
                325 + index * 52,
                290,
                42);
        }

        Button repairCore = ActionButton(
            commands,
            "Repair Core",
            "REFIT BASE CORE",
            20,
            680,
            290,
            50);
        Button repairShip = ActionButton(
            commands,
            "Repair Ship",
            "REFIT PILOT SHIP",
            20,
            744,
            290,
            50);
        RectTransform corePanel = Panel(
            root.transform,
            "Core Health",
            1460,
            172,
            428,
            92);
        Text core = Label(
            corePanel,
            "CORE ONLINE",
            20,
            24,
            390,
            36,
            25);
        Button wave = ActionButton(
            root.transform,
            "Begin Wave",
            "BEGIN NEXT WAVE",
            760,
            948,
            430,
            64);
        Button restart = ActionButton(
            root.transform,
            "Restart",
            "RESTART SIEGE",
            1660,
            970,
            228,
            52);
        Label(
            root.transform,
            "WASD / drive     Q E / altitude     SPACE / hover\nHold right mouse / orbit     Scroll / zoom",
            1030,
            860,
            850,
            70,
            20);
        var serialized = new SerializedObject(root.GetComponent<SiegeHud>());
        serialized.FindProperty("session").objectReferenceValue = session;
        serialized.FindProperty("heading").objectReferenceValue = heading;
        serialized.FindProperty("economy").objectReferenceValue = economy;
        serialized.FindProperty("status").objectReferenceValue = status;
        serialized.FindProperty("coreHealth").objectReferenceValue = core;
        serialized.FindProperty("wave").objectReferenceValue = wave;
        serialized.FindProperty("repairCore").objectReferenceValue = repairCore;
        serialized.FindProperty("repairShip").objectReferenceValue = repairShip;
        serialized.FindProperty("restart").objectReferenceValue = restart;
        SetArray(serialized.FindProperty("deploy"), deploy);
        SetArray(serialized.FindProperty("takeControl"), control);
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Panel(
        Transform parent,
        string name,
        float x,
        float y,
        float width,
        float height)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), x, y, width, height);
        root.GetComponent<Image>().color = new Color(0.025f, 0.055f, 0.085f, 0.94f);
        return root.GetComponent<RectTransform>();
    }

    private static Text Label(
        Transform parent,
        string text,
        float x,
        float y,
        float width,
        float height,
        int size)
    {
        var root = new GameObject(text.Split('\n')[0], typeof(RectTransform), typeof(Text));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), x, y, width, height);
        Text label = root.GetComponent<Text>();
        label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSans-SemiBold.ttf");
        label.text = text;
        label.fontSize = size;
        label.color = new Color(0.83f, 0.94f, 0.98f);
        label.raycastTarget = false;
        return label;
    }

    private static Button ActionButton(
        Transform parent,
        string name,
        string text,
        float x,
        float y,
        float width,
        float height)
    {
        RectTransform root = Panel(
            parent,
            name,
            x,
            y,
            width,
            height);
        root.GetComponent<Image>().color = new Color(0.08f, 0.28f, 0.33f, 1f);
        Button button = root.gameObject.AddComponent<Button>();
        Text label = Label(
            root,
            text,
            10,
            4,
            width - 20,
            height - 8,
            19);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void SetArray(SerializedProperty property, Object[] values)
    {
        property.arraySize = values.Length;
        for (int index = 0; index < values.Length; index++)
        {
            property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }
    }
}

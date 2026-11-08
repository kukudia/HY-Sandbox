// 职责：保存新版基地战配置、Main编辑入口及副场景uGUI/建筑服务；保留旧Siege和Main手工布局。
// 入口：Install()；只重建明确拥有的 Construction Controls / Base Workshop 两个UI根。
using System;
using System.Linq;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeConstructionAuthoring
{
    public const string SettingsPath = "Assets/Siege/Construction/Settings.asset";
    [MenuItem("Tools/HY-Sandbox/Siege/Install base construction")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save open scenes and exit Play Mode before installing base construction.");
        }

        EnsureSettings();
        AssetDatabase.SaveAssets();
        Scene main = EditorSceneManager.OpenScene(GameModeAuthoring.MainPath, OpenSceneMode.Single);
        SiegeConstructionSettings settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>(SettingsPath);
        Transform parent = SceneObjectQuery.FindInScene<MainUIPanels>(main).buildPanel.transform;
        RemoveOwned(parent, "Base Workshop");
        RectTransform workshop = Rect("Base Workshop", parent, new Vector2(640, 120));
        workshop.anchorMin = workshop.anchorMax = workshop.pivot = Vector2.zero;
        workshop.anchoredPosition = new Vector2(20, 20);
        Image background = workshop.gameObject.AddComponent<Image>();
        background.color = new Color(.025f, .055f, .08f, .95f);
        SiegeBaseWorkshop entry = workshop.gameObject.AddComponent<SiegeBaseWorkshop>();
        var data = new SerializedObject(entry);
        Set(data, "settings", settings);
        Set(data, "toggle", Button(
            "Editor",
            workshop,
            "BASE EDITOR",
            110,
            90,
            195));
        Set(data, "starter", Button(
            "Starter",
            workshop,
            "COPY STARTER",
            325,
            90,
            195));
        Set(data, "core", Button(
            "Core",
            workshop,
            "MARK SELECTED CORE",
            535,
            90,
            195));
        Set(data, "status", Label(
            "Status",
            workshop,
            "",
            320,
            34,
            610,
            62,
            14));
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(main);
        Scene siege = EditorSceneManager.OpenScene(GameModeAuthoring.SiegePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(siege);
        SiegeGameplayStage stage = SceneObjectQuery.FindInScene<SiegeGameplayStage>(siege);
        Transform root = stage.transform;
        Transform canvas = root.Find("Siege Gameplay UI");
        Transform oldStatus = canvas.Find("Stage Status");
        if (oldStatus != null)
        {
            Object.DestroyImmediate(oldStatus.gameObject);
        }

        WorldBuildingManager buildings = stage.GetComponent<WorldBuildingManager>();
        if (buildings == null)
        {
            buildings = stage.gameObject.AddComponent<WorldBuildingManager>();
        }

        var buildingData = new SerializedObject(buildings);
        buildingData.FindProperty("initializeOnStart").boolValue = false;
        buildingData.ApplyModifiedPropertiesWithoutUndo();
        BoxCollider arena = root.Find("Arena Ground").GetComponent<BoxCollider>();
        if (root.Find("Arena Light") == null)
        {
            var lighting = new GameObject("Arena Light", typeof(Light));
            lighting.transform.SetParent(root, false);
            lighting.transform.rotation = Quaternion.Euler(48, -35, 0);
            Light light = lighting.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2f;
            light.shadows = LightShadows.Soft;
        }

        root.Find("Overview").SetPositionAndRotation(new Vector3(24, 65, -72), Quaternion.Euler(36, -12, 0));
        RemoveOwned(canvas, "Construction Controls");
        RectTransform controls = Rect("Construction Controls", canvas, new Vector2(325, 550));
        controls.anchorMin = controls.anchorMax = controls.pivot = new Vector2(0, .5f);
        controls.anchoredPosition = new Vector2(15, 0);
        controls.gameObject.AddComponent<Image>().color = new Color(.025f, .055f, .08f, .96f);
        SiegeConstructionPanel panel = controls.gameObject.AddComponent<SiegeConstructionPanel>();
        var panelData = new SerializedObject(panel);
        Label(
            "Title",
            controls,
            "BASE DEPLOYMENT",
            162,
            510,
            300,
            35,
            23);
        Dropdown dropdown = DropdownAt(controls, 162, 460);
        Set(panelData, "designs", dropdown);
        Label(
            "Coordinates",
            controls,
            "GROUND POSITION   X / Z",
            162,
            420,
            300,
            24,
            15);
        Set(panelData, "x", Input(controls, "X", 85, 384));
        Set(panelData, "z", Input(controls, "Z", 240, 384));
        Set(panelData, "rotate", Button(
            "Rotate",
            controls,
            "ROTATE 0",
            162,
            335,
            290));
        Set(panelData, "deploy", Button(
            "Deploy",
            controls,
            "CONFIRM DEPLOYMENT",
            162,
            285,
            290));
        Set(panelData, "details", Label(
            "Design",
            controls,
            "",
            162,
            220,
            295,
            90,
            15));
        Set(panelData, "wave", Button(
            "Wave",
            controls,
            "START NEXT WAVE",
            162,
            145,
            290));
        Set(panelData, "repair", Button(
            "Repair",
            controls,
            "REPAIR + REBUILD",
            162,
            95,
            290));
        Set(panelData, "summary", Label(
            "Summary",
            controls,
            "",
            162,
            35,
            295,
            65,
            16));
        Text feedback = Label(
            "Feedback",
            controls,
            "",
            780,
            50,
            820,
            90,
            18);
        Set(panelData, "feedback", feedback);
        Button back = canvas.Find("Return").GetComponent<Button>();
        Set(panelData, "returnLabel", back.GetComponentInChildren<Text>());
        panelData.ApplyModifiedPropertiesWithoutUndo();
        var stageData = new SerializedObject(stage);
        Set(stageData, "settings", settings);
        Set(stageData, "buildings", buildings);
        Set(stageData, "arena", arena);
        Set(stageData, "panel", panel);
        stageData.ApplyModifiedPropertiesWithoutUndo();
        CopyPool<VfxEffectPool>(main, siege);
        CopyPool<TurretProjectilePool>(main, siege);
        EditorSceneManager.SaveScene(siege);
        EditorSceneManager.CloseScene(siege, true);
        SceneManager.SetActiveScene(main);
        AssetDatabase.SaveAssets();
        Debug.Log("Base construction installed: Main workshop and SiegeGameplay deployment / waves / refit.");
    }

    private static SiegeConstructionSettings EnsureSettings()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Siege/Construction"))
        {
            AssetDatabase.CreateFolder("Assets/Siege", "Construction");
        }

        SiegeConstructionSettings settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>(SettingsPath);
        if (settings != null)
        {
            return settings;
        }

        BlockDataList starter = JsonUtility.FromJson<BlockDataList>(AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Siege/BastionBase.json").text);
        starter.siegeCoreId = "bastion-core";
        File.WriteAllText("Assets/Siege/Construction/StarterBase.json", JsonUtility.ToJson(starter, true));
        AssetDatabase.ImportAsset("Assets/Siege/Construction/StarterBase.json");
        SiegeSettings combat = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SiegeSettings>("Assets/Siege/DefaultSiege.asset"));
        var combatData = new SerializedObject(combat);
        combatData.FindProperty("autoRepairBase").boolValue = false;
        combatData.FindProperty("autoRepairFleet").boolValue = false;
        combatData.FindProperty("startingCoins").intValue = 6000;
        combatData.FindProperty("attackRadius").floatValue = 85f;
        combatData.FindProperty("maximumActiveEnemies").intValue = 4;
        combatData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(combat, "Assets/Siege/Construction/Combat.asset");
        settings = ScriptableObject.CreateInstance<SiegeConstructionSettings>();
        var data = new SerializedObject(settings);
        Set(data, "combat", combat);
        Set(
            data,
            "starter",
            AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Siege/Construction/StarterBase.json"));
        data.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(settings, SettingsPath);
        return settings;
    }

    private static void CopyPool<T>(Scene main, Scene target)
        where T : Component
    {
        if (SceneObjectQuery.FindInScene<T>(target) == null)
        {
            GameObject copy = Object.Instantiate(SceneObjectQuery.FindInScene<T>(main).gameObject);
            copy.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(copy, target);
        }
    }

    private static void RemoveOwned(Transform parent, string name)
    {
        Transform old = parent.Find(name);
        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
        rect.sizeDelta = size;
        return rect;
    }

    private static void At(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static Text Label(
        string name,
        Transform parent,
        string text,
        float x,
        float y,
        float width,
        float height,
        int size)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero);
        At(rect, x, y, width, height);
        Text label = rect.gameObject.AddComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = new Color(.86f, .93f, 1f);
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(
        string name,
        Transform parent,
        string title,
        float x,
        float y,
        float width)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero);
        At(rect, x, y, width, 38);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.1f, .26f, .35f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text label = Label(
            "Label",
            rect,
            title,
            width / 2f,
            19,
            width - 8,
            34,
            16);
        return button;
    }

    private static InputField Input(Transform parent, string name, float x, float y)
    {
        GameObject obj = DefaultControls.CreateInputField(new DefaultControls.Resources());
        obj.name = name;
        obj.transform.SetParent(parent, false);
        At((RectTransform)obj.transform, x, y, 130, 36);
        InputField input = obj.GetComponent<InputField>();
        input.contentType = InputField.ContentType.IntegerNumber;
        input.text = "-14";
        foreach (Text label in obj.GetComponentsInChildren<Text>(true))
        {
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
        }

        return input;
    }

    private static Dropdown DropdownAt(Transform parent, float x, float y)
    {
        GameObject obj = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        obj.transform.SetParent(parent, false);
        At((RectTransform)obj.transform, x, y, 295, 38);
        foreach (Text label in obj.GetComponentsInChildren<Text>(true))
        {
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
        }

        return obj.GetComponent<Dropdown>();
    }

    private static void Set(SerializedObject data, string field, Object value)
    {
        data.FindProperty(field).objectReferenceValue = value;
    }
}

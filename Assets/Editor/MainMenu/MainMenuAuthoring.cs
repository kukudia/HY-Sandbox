// 职责：制作可编辑主菜单场景、预览相机和Main返回入口；重复执行保留已制作的菜单布局。
// 入口：Install()；通过Editor API保存真实资产，不在运行时拼装菜单UI。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MainMenuAuthoring
{
    private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    private static readonly Color Ink = new Color(.035f, .05f, .085f);
    private static readonly Color Card = new Color(.07f, .105f, .16f);
    private static readonly Color Accent = new Color(.2f, .85f, .8f);
    [MenuItem("Tools/HY-Sandbox/Main Menu/Install editable menu")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save open scenes and exit Play Mode before menu authoring.");
        }

        Scene main = SceneManager.GetSceneByPath(MainMenuController.MainPath);
        if (!main.isLoaded)
        {
            main = EditorSceneManager.OpenScene(MainMenuController.MainPath, OpenSceneMode.Additive);
        }

        SceneManager.SetActiveScene(main);
        WorkshopNavigation navigation = SceneObjectQuery.FindInScene<WorkshopNavigation>(main);
        if (navigation == null)
        {
            navigation = SceneObjectQuery.FindInScene<GameManager>(main).gameObject.AddComponent<WorkshopNavigation>();
            Canvas canvas = main.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).First(c => c.name == "MainCanvas");
            Button menu = Button("Main Menu", canvas.transform, "MENU  [ESC]", new Vector2(0f, 0f), new Vector2(158f, 36f));
            RectTransform rect = menu.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -25f);
            Bind(navigation, "workshopEvents", SceneObjectQuery.FindInScene<EventSystem>(main));
            Bind(navigation, "menuButton", menu);
        }

        Bind(SceneObjectQuery.FindInScene<GameManager>(main), "navigation", navigation);
        var navigationData = new SerializedObject(navigation);
        if (navigationData.FindProperty("notice").objectReferenceValue == null)
        {
            Canvas canvas = main.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).First(c => c.name == "MainCanvas");
            Text notice = Text(
                "Menu Notice",
                canvas.transform,
                string.Empty,
                Vector2.zero,
                new Vector2(900, 64),
                17,
                new Color(1f, .7f, .35f));
            RectTransform rect = notice.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -85f);
            notice.alignment = TextAnchor.MiddleCenter;
            notice.gameObject.SetActive(false);
            Bind(navigation, "notice", notice);
        }

        EditorSceneManager.MarkSceneDirty(main);
        EditorSceneManager.SaveScene(main);
        Scene menuScene = SceneManager.GetSceneByPath(WorkshopNavigation.MenuPath);
        if (!menuScene.isLoaded)
        {
            menuScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(WorkshopNavigation.MenuPath) != null ? EditorSceneManager.OpenScene(WorkshopNavigation.MenuPath, OpenSceneMode.Additive) : CreateMenu();
        }

        var scenes = EditorBuildSettings.scenes.Where(s => s.path != WorkshopNavigation.MenuPath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(WorkshopNavigation.MenuPath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        MainMenuView menuView = SceneObjectQuery.FindInScene<MainMenuView>(menuScene);
        var viewData = new SerializedObject(menuView);
        if (viewData.FindProperty("cancelNavigation").objectReferenceValue == null)
        {
            Canvas canvas = menuScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).First();
            Button cancel = Button(
                "Cancel Navigation",
                canvas.transform,
                "CANCEL & RETURN",
                new Vector2(600, -343),
                new Vector2(260, 54));
            cancel.gameObject.SetActive(false);
            Bind(menuView, "cancelNavigation", cancel);
            Text status = (Text)viewData.FindProperty("status").objectReferenceValue;
            status.rectTransform.anchoredPosition = new Vector2(-190, -343);
            status.rectTransform.sizeDelta = new Vector2(1060, 64);
        }

        foreach (Dropdown dropdown in menuScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Dropdown>(true)))
        {
            Image arrow = dropdown.transform.Find("Arrow")?.GetComponent<Image>();
            if (arrow != null && arrow.sprite == null)
            {
                arrow.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd");
            }
        }

        foreach (Toggle toggle in menuScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Toggle>(true)))
        {
            Transform backgroundRoot = toggle.transform.Find("Background");
            if (backgroundRoot == null)
            {
                continue;
            }

            Image background = backgroundRoot.GetComponent<Image>();
            Image checkmark = toggle.graphic as Image;
            if (background.sprite == null)
            {
                background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                background.color = new Color(.25f, .32f, .4f);
            }

            if (checkmark != null && checkmark.sprite == null)
            {
                checkmark.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd");
                checkmark.color = Accent;
            }
        }

        foreach (Slider slider in menuScene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Slider>(true)))
        {
            Image background = slider.transform.Find("Background").GetComponent<Image>();
            if (background.sprite == null && background.color == Color.white)
            {
                background.color = new Color(.16f, .22f, .3f);
            }
        }

        EditorSceneManager.MarkSceneDirty(menuScene);
        EditorSceneManager.SaveScene(menuScene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.CloseScene(main, true);
        SceneManager.SetActiveScene(menuScene);
        Debug.Log("MainMenu installed as startup. Saved UI and cameras remain editable; Main loads on demand.");
    }

    private static Scene CreateMenu()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        var services = new GameObject("Menu Services");
        MainMenuController controller = services.AddComponent<MainMenuController>();
        MainMenuView view = services.AddComponent<MainMenuView>();
        MenuSettings settings = services.AddComponent<MenuSettings>();
        MenuBlueprintPreview preview = services.AddComponent<MenuBlueprintPreview>();
        var presentation = new GameObject("Menu Presentation");
        var cameraRoot = new GameObject("Menu Camera", typeof(Camera));
        cameraRoot.transform.SetParent(presentation.transform);
        Camera backgroundCamera = cameraRoot.GetComponent<Camera>();
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;
        backgroundCamera.backgroundColor = Ink;
        backgroundCamera.cullingMask = 0;
        backgroundCamera.depth = 5;
        var canvasRoot = new GameObject(
            "Menu Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvasRoot.transform.SetParent(presentation.transform);
        Canvas canvas = canvasRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600, 900);
        scaler.matchWidthOrHeight = .5f;
        RectTransform backdrop = Rect("Backdrop", canvasRoot.transform, Vector2.zero, new Vector2(1600, 900));
        backdrop.anchorMin = Vector2.zero;
        backdrop.anchorMax = Vector2.one;
        backdrop.sizeDelta = Vector2.zero;
        backdrop.gameObject.AddComponent<Image>().color = Ink;
        Text(
            "Brand",
            canvasRoot.transform,
            "HY / SANDBOX",
            new Vector2(-520, 356),
            new Vector2(440, 60),
            40,
            Accent);
        Text(
            "Tagline",
            canvasRoot.transform,
            "DESIGN.  EXPLORE.  HOLD THE LINE.",
            new Vector2(-448, 302),
            new Vector2(584, 36),
            17,
            Color.gray);
        Text(
            "Build",
            canvasRoot.transform,
            "CREATIVE SYSTEMS  /  SIEGE PROTOTYPE",
            new Vector2(430, 355),
            new Vector2(600, 36),
            15,
            Color.gray);
        Text(
            "Footer",
            canvasRoot.transform,
            "WORKSHOP DESIGNS ARE KEPT SEPARATE FROM BATTLE SESSIONS",
            new Vector2(0, -404),
            new Vector2(1440, 32),
            14,
            Color.gray);
        RectTransform controls = Rect("Controls", canvasRoot.transform, Vector2.zero, new Vector2(1600, 900));
        CanvasGroup controlGroup = controls.gameObject.AddComponent<CanvasGroup>();
        RectTransform home = Rect("Home", controls, Vector2.zero, new Vector2(1500, 620));
        Text(
            "Play heading",
            home,
            "START A SESSION",
            new Vector2(-530, 207),
            new Vector2(380, 32),
            18,
            Accent);
        Bind(
            view,
            "exploration",
            Button(
                "Exploration",
                home,
                "EXPLORATION     /     OPEN WORLD",
                new Vector2(-425, 130),
                new Vector2(590, 90)));
        Bind(
            view,
            "siege",
            Button("Siege", home, "SIEGE     /     DEFEND YOUR BASE", new Vector2(-425, 20), new Vector2(590, 90)));
        Text(
            "Workshop heading",
            home,
            "CREATIVE WORKSHOP",
            new Vector2(-530, -75),
            new Vector2(380, 32),
            18,
            Accent);
        Bind(
            view,
            "vehicleWorkshop",
            Button("Vehicle Workshop", home, "VEHICLE", new Vector2(-578, -145), new Vector2(282, 66)));
        Bind(
            view,
            "baseWorkshop",
            Button("Base Workshop", home, "BASE", new Vector2(-272, -145), new Vector2(282, 66)));
        RectTransform info = Panel("Resume card", home, new Vector2(350, 20), new Vector2(660, 460));
        Text(
            "Card Title",
            info,
            "YOUR NEXT IDEA\nSTARTS HERE.",
            new Vector2(0, 120),
            new Vector2(540, 130),
            38,
            Color.white);
        Text recentText = Text(
            "Recent",
            info,
            "No recent workshop",
            new Vector2(0, 12),
            new Vector2(540, 42),
            18,
            Accent);
        Bind(view, "recentLabel", recentText);
        Bind(
            view,
            "resume",
            Button("Resume", info, "RESUME CURRENT WORKSHOP", new Vector2(0, -62), new Vector2(540, 58)));
        Bind(
            view,
            "recent",
            Button("Recent", info, "OPEN RECENT DESIGN", new Vector2(0, -136), new Vector2(540, 58)));
        Bind(
            view,
            "settingsButton",
            Button("Settings", home, "SETTINGS", new Vector2(-578, -249), new Vector2(282, 46)));
        Bind(view, "quit", Button("Quit", home, "QUIT", new Vector2(-272, -249), new Vector2(282, 46)));
        RectTransform picker = Rect("Blueprint Picker", controls, Vector2.zero, new Vector2(1500, 620));
        Bind(
            view,
            "heading",
            Text(
                "Heading",
                picker,
                "SELECT BLUEPRINT",
                new Vector2(-425, 225),
                new Vector2(590, 52),
                26,
                Accent));
        Dropdown dropdown = Dropdown("Blueprints", picker, new Vector2(-425, 153), new Vector2(590, 54));
        Bind(view, "blueprints", dropdown);
        Bind(
            view,
            "details",
            Text(
                "Details",
                picker,
                "Select a saved design",
                new Vector2(-425, -4),
                new Vector2(590, 225),
                20,
                Color.white));
        Bind(view, "launch", Button("Launch", picker, "OPEN", new Vector2(-425, -174), new Vector2(590, 62)));
        Bind(
            view,
            "create",
            Button("Create", picker, "NEW EMPTY DESIGN", new Vector2(-425, -245), new Vector2(282, 48)));
        Bind(
            view,
            "refresh",
            Button("Refresh", picker, "REFRESH LIBRARY", new Vector2(-119, -245), new Vector2(282, 48)));
        Bind(view, "back", Button("Back", picker, "BACK", new Vector2(527, -245), new Vector2(220, 48)));
        RawImage previewImage = Rect("Preview", picker, new Vector2(355, 56), new Vector2(660, 432)).gameObject.AddComponent<RawImage>();
        previewImage.raycastTarget = false;
        Text(
            "Preview caption",
            picker,
            "BLUEPRINT PREVIEW  /  STATIC MODEL",
            new Vector2(355, -184),
            new Vector2(660, 30),
            14,
            Color.gray);
        RectTransform settingsPanel = Rect("Settings", controls, Vector2.zero, new Vector2(1500, 620));
        Text(
            "Title",
            settingsPanel,
            "SETTINGS",
            new Vector2(-425, 225),
            new Vector2(590, 52),
            28,
            Accent);
        Bind(
            settings,
            "volume",
            Slider(
                "Master volume",
                settingsPanel,
                "MASTER VOLUME",
                new Vector2(-425, 120),
                0f,
                1f));
        Bind(
            settings,
            "sensitivity",
            Slider(
                "Mouse sensitivity",
                settingsPanel,
                "MOUSE SENSITIVITY",
                new Vector2(-425, 10),
                .1f,
                8f));
        Text(
            "Quality label",
            settingsPanel,
            "QUALITY",
            new Vector2(-425, -88),
            new Vector2(590, 30),
            18,
            Color.white);
        Bind(
            settings,
            "quality",
            Dropdown("Quality", settingsPanel, new Vector2(-425, -137), new Vector2(590, 48)));
        Toggle fullscreen = DefaultControls.CreateToggle(new DefaultControls.Resources()).GetComponent<Toggle>();
        fullscreen.name = "Fullscreen";
        Place(
            fullscreen.GetComponent<RectTransform>(),
            settingsPanel,
            new Vector2(-425, -215),
            new Vector2(590, 44));
        fullscreen.GetComponentInChildren<Text>().text = "FULLSCREEN";
        StyleFonts(fullscreen.gameObject, 18);
        Bind(settings, "fullscreen", fullscreen);
        RectTransform help = Panel("Controls help", settingsPanel, new Vector2(350, 30), new Vector2(660, 460));
        Text(
            "Help title",
            help,
            "CONTROLS",
            new Vector2(0, 160),
            new Vector2(540, 44),
            25,
            Accent);
        Text(
            "Help",
            help,
            "ESC    Return to main menu\n\nWorkshop\nUse the existing build toolbar for editing.\n\nExploration\nUse the configured vehicle controls.\nHold Alt to release the cursor.\n\nSiege\nUse deployment and wave controls.\n\nKey remapping is not included in this version.",
            new Vector2(0, -25),
            new Vector2(540, 300),
            18,
            Color.white);
        Bind(
            view,
            "settingsBack",
            Button("Back", settingsPanel, "BACK", new Vector2(527, -245), new Vector2(220, 48)));
        Bind(
            view,
            "status",
            Text(
                "Status",
                canvasRoot.transform,
                "Ready",
                new Vector2(0, -343),
                new Vector2(1440, 64),
                17,
                new Color(.62f, .73f, .8f)));
        Bind(view, "home", home.gameObject);
        Bind(view, "picker", picker.gameObject);
        Bind(view, "settingsPanel", settingsPanel.gameObject);
        Bind(view, "controls", controlGroup);
        Bind(controller, "view", view);
        Bind(controller, "settings", settings);
        Bind(controller, "preview", preview);
        Bind(controller, "presentation", presentation);
        Bind(
            controller,
            "siegeSettings",
            AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>("Assets/Siege/Construction/Settings.asset"));
        Bind(
            controller,
            "vehicleExample",
            AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/WorldBlueprints/Enemies/CODEX_E01_Finch.json"));
        BuildPreview(presentation.transform, previewImage, preview);
        var events = new GameObject("Menu EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        home.gameObject.SetActive(true);
        picker.gameObject.SetActive(false);
        settingsPanel.gameObject.SetActive(false);
        EditorSceneManager.SaveScene(scene, WorkshopNavigation.MenuPath);
        return scene;
    }

    private static void BuildPreview(Transform parent, RawImage image, MenuBlueprintPreview preview)
    {
        const string path = "Assets/Scenes/MenuPreview.renderTexture";
        RenderTexture texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (texture == null)
        {
            texture = new RenderTexture(768, 512, 24)
            {
                name = "Menu Blueprint Preview"
            };
            AssetDatabase.CreateAsset(texture, path);
        }

        image.texture = texture;
        var anchor = new GameObject("Blueprint Preview Anchor");
        anchor.transform.SetParent(parent);
        var cameraRoot = new GameObject("Blueprint Camera", typeof(Camera));
        cameraRoot.transform.SetParent(parent);
        Camera camera = cameraRoot.GetComponent<Camera>();
        camera.orthographic = true;
        camera.cullingMask = 1 << 30;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Card;
        camera.targetTexture = texture;
        var lightRoot = new GameObject("Blueprint Light", typeof(Light));
        lightRoot.transform.SetParent(parent);
        Light light = lightRoot.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 2f;
        light.cullingMask = 1 << 30;
        lightRoot.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
        Bind(preview, "anchor", anchor.transform);
        Bind(preview, "previewCamera", camera);
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(name, parent, position, size);
        rect.gameObject.AddComponent<Image>().color = Card;
        return rect;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        RectTransform rect = obj.GetComponent<RectTransform>();
        Place(rect, parent, position, size);
        return rect;
    }

    private static void Place(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static Text Text(
        string name,
        Transform parent,
        string content,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color)
    {
        Text label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        label.font = Font;
        label.fontSize = fontSize;
        label.color = color;
        label.text = content;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        return label;
    }

    private static Button Button(string name, Transform parent, string content, Vector2 position, Vector2 size)
    {
        RectTransform rect = Panel(name, parent, position, size);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.4f, 1.6f, 1.7f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(.4f, .4f, .4f, .6f);
        button.colors = colors;
        Text label = Text(
            "Label",
            rect,
            content,
            Vector2.zero,
            size - new Vector2(30f, 4f),
            18,
            Color.white);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static Dropdown Dropdown(string name, Transform parent, Vector2 position, Vector2 size)
    {
        Dropdown control = DefaultControls.CreateDropdown(new DefaultControls.Resources()).GetComponent<Dropdown>();
        control.name = name;
        Place(control.GetComponent<RectTransform>(), parent, position, size);
        control.GetComponent<Image>().color = new Color(.16f, .22f, .3f);
        control.template.GetComponent<Image>().color = new Color(.1f, .15f, .22f);
        StyleFonts(control.gameObject, 19);
        DropdownTemplateAuthoring.Apply(control);
        return control;
    }

    private static Slider Slider(
        string name,
        Transform parent,
        string label,
        Vector2 position,
        float minimum,
        float maximum)
    {
        Text(
            name + " label",
            parent,
            label,
            position + Vector2.up * 36f,
            new Vector2(590, 32),
            18,
            Color.white);
        Slider control = DefaultControls.CreateSlider(new DefaultControls.Resources()).GetComponent<Slider>();
        control.name = name;
        Place(control.GetComponent<RectTransform>(), parent, position - Vector2.up * 8f, new Vector2(590, 30));
        control.minValue = minimum;
        control.maxValue = maximum;
        control.fillRect.GetComponent<Image>().color = Accent;
        return control;
    }

    private static void StyleFonts(GameObject root, int size)
    {
        foreach (Text label in root.GetComponentsInChildren<Text>(true))
        {
            label.font = Font;
            label.fontSize = size;
            label.color = Color.white;
        }
    }

    private static void Bind(Object owner, string field, Object value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}

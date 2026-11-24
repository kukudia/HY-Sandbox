// 职责：为现有可编辑菜单增补设置导航和建造偏好，保留已有页面内容。
// 入口：InstallPreferences()；幂等制作并保存主菜单场景。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class NightMenuAuthoring
{
    public static void ConfigurePauseOverlay(PauseMenu pause)
    {
        using var data = new SerializedObject(pause);
        var panel = (GameObject)data.FindProperty("panel").objectReferenceValue;
        // 子 Canvas 必须保存为场景组件，才能在叠加关卡的 HUD 上方接收输入。
        if (!panel.TryGetComponent(out Canvas overlay))
        {
            overlay = Undo.AddComponent<Canvas>(panel);
        }

        using var canvasData = new SerializedObject(overlay);
        canvasData.FindProperty("m_OverrideSorting").boolValue = true;
        canvasData.FindProperty("m_SortingOrder").intValue = short.MaxValue;
        canvasData.ApplyModifiedProperties();
        if (!panel.TryGetComponent(out GraphicRaycaster _))
        {
            Undo.AddComponent<GraphicRaycaster>(panel);
        }

        Image backdrop = panel.GetComponent<Image>();
        Color background = backdrop.color;
        background.a = 1f;
        backdrop.color = background;
    }

    [MenuItem("Tools/HY-Sandbox/Main Menu/Polish pause card")]
    public static void PolishPauseCard()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before saving pause layout.");
        }

        Scene scene = SceneManager.GetSceneByPath(MainMenuController.MainPath);
        PauseMenu pause = SceneObjectQuery.FindInScene<PauseMenu>(scene, FindObjectsInactive.Include);
        var data = new SerializedObject(pause);
        Transform panel = ((GameObject)data.FindProperty("panel").objectReferenceValue).transform;
        Transform existing = panel.Find("Pause Card");
        RectTransform card;
        if (existing == null)
        {
            card = new GameObject("Pause Card", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            Place(card, panel, Vector2.zero, new Vector2(620, 540));
            card.GetComponent<Image>().color = new Color(.035f, .065f, .09f, .98f);
        }
        else
        {
            card = (RectTransform)existing;
        }

        RectTransform content = card.Find("Actions") as RectTransform;
        if (content == null)
        {
            content = new GameObject("Actions", typeof(RectTransform), typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            Place(content, card, new Vector2(0, -25), new Vector2(420, 260));
            VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = false;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            layout.spacing = 18;
        }

        foreach (string field in new[]
        {
            "resume",
            "restart",
            "mainMenu"
        }

        )
        {
            Button button = (Button)data.FindProperty(field).objectReferenceValue;
            button.transform.SetParent(content, false);
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 62);
            Text label = button.GetComponentInChildren<Text>();
            label.fontSize = 23;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(16, 0);
            label.rectTransform.offsetMax = new Vector2(-16, 0);
        }

        PauseLabel(
            card,
            "Heading",
            "PAUSED",
            205,
            38,
            new Color(.2f, .88f, .83f));
        PauseLabel(
            card,
            "Subtitle",
            "Take a moment. Your current session is preserved.",
            145,
            19,
            new Color(.65f, .78f, .84f));
        PauseLabel(
            card,
            "Hint",
            "ESC  /  RESUME",
            -220,
            17,
            new Color(.55f, .7f, .77f));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PauseLabel(
        Transform parent,
        string name,
        string value,
        float y,
        int size,
        Color color)
    {
        Transform child = parent.Find(name);
        Text label = child == null ? new GameObject(name, typeof(RectTransform), typeof(Text)).GetComponent<Text>() : child.GetComponent<Text>();
        Place(label.rectTransform, parent, new Vector2(0, y), new Vector2(570, 52));
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = value;
        label.color = color;
        label.raycastTarget = false;
    }

    [MenuItem("Tools/HY-Sandbox/Main Menu/Fit menu to narrow screens")]
    public static void FitMenuCanvas()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before saving menu layout.");
        }

        Scene scene = SceneManager.GetSceneByPath(WorkshopNavigation.MenuPath);
        if (!scene.isLoaded)
        {
            throw new InvalidOperationException("Load the saved MainMenu scene first.");
        }

        CanvasScaler scaler = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<CanvasScaler>(true)).Single();
        Undo.RecordObject(scaler, "Fit menu canvas");
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/HY-Sandbox/Main Menu/Install battle attract entry")]
    public static void InstallAttractEntry()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before authoring.");
        }

        Scene scene = SceneManager.GetSceneByPath(WorkshopNavigation.MenuPath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(WorkshopNavigation.MenuPath, OpenSceneMode.Additive);
        }

        var view = new SerializedObject(SceneObjectQuery.FindInScene<MainMenuView>(scene));
        Button button = (Button)view.FindProperty("battleAttract").objectReferenceValue;
        if (button == null)
        {
            Button template = (Button)view.FindProperty("recent").objectReferenceValue;
            button = Tab(template, template.transform.parent, "Watch live defence", new Vector2(0, -205));
            button.name = "Watch live defence";
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(540, 46);
            Bind(view, "battleAttract", button);
            view.ApplyModifiedPropertiesWithoutUndo();
        }

        button.interactable = true;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/HY-Sandbox/Main Menu/Install pause and restart")]
    public static void InstallPause()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before authoring.");
        }

        Scene scene = SceneManager.GetSceneByPath(MainMenuController.MainPath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(MainMenuController.MainPath, OpenSceneMode.Additive);
        }

        WorkshopNavigation navigation = SceneObjectQuery.FindInScene<WorkshopNavigation>(scene);
        if (navigation.GetComponent<PauseMenu>() == null)
        {
            var navData = new SerializedObject(navigation);
            Button template = (Button)navData.FindProperty("menuButton").objectReferenceValue;
            template.GetComponentInChildren<Text>().text = "PAUSE  [ESC]";
            PauseMenu pause = navigation.gameObject.AddComponent<PauseMenu>();
            var root = new GameObject("Pause Overlay", typeof(RectTransform), typeof(Image));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(template.transform.parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(.015f, .025f, .045f, 1f);
            Button resume = Tab(template, rect, "Resume", new Vector2(0, 90));
            Button restart = Tab(template, rect, "Restart", Vector2.zero);
            Button menu = Tab(template, rect, "Main Menu", new Vector2(0, -90));
            foreach (Button button in new[]
            {
                resume,
                restart,
                menu
            }

            )
            {
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 62);
            }

            var data = new SerializedObject(pause);
            Bind(data, "panel", root);
            Bind(data, "resume", resume);
            Bind(data, "restart", restart);
            Bind(data, "mainMenu", menu);
            Bind(data, "navigation", navigation);
            data.ApplyModifiedPropertiesWithoutUndo();
            Bind(navData, "pauseMenu", pause);
            navData.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
        }

        ConfigurePauseOverlay(SceneObjectQuery.FindInScene<PauseMenu>(scene, FindObjectsInactive.Include));
        BuildManager build = SceneObjectQuery.FindInScene<BuildManager>(scene);
        var buildData = new SerializedObject(build);
        buildData.FindProperty("_structureBuildRange").floatValue = 30f;
        buildData.ApplyModifiedPropertiesWithoutUndo();
        MainUIPanels panels = SceneObjectQuery.FindInScene<MainUIPanels>(scene);
        if (panels.deathPanel.transform.Find("Restart") == null)
        {
            Button template = panels.deathPanel.GetComponentInChildren<Button>(true);
            if (template != null)
            {
                Button restart = Tab(template, panels.deathPanel.transform, "Restart", new Vector2(0, -100));
                restart.name = "Restart";
                UnityEditor.Events.UnityEventTools.AddPersistentListener(restart.onClick, SceneObjectQuery.FindInScene<GameModeController>(scene).Restart);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        InstallSiegeRestart();
    }

    private static void InstallSiegeRestart()
    {
        const string path = "Assets/Scenes/SiegeGameplay.unity";
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }

        SiegeCombatHud hud = SceneObjectQuery.FindInScene<SiegeCombatHud>(scene);
        var data = new SerializedObject(hud);
        if (data.FindProperty("restartFailure").objectReferenceValue == null)
        {
            Button dismiss = (Button)data.FindProperty("dismissFailure").objectReferenceValue;
            Button restart = UnityEngine.Object.Instantiate(dismiss, dismiss.transform.parent);
            restart.name = "Restart";
            restart.onClick = new Button.ButtonClickedEvent();
            restart.GetComponentInChildren<TMPro.TMP_Text>().text = "RESTART";
            RectTransform rect = restart.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, rect.sizeDelta.y);
            rect.anchoredPosition += Vector2.left * 140;
            RectTransform dismissRect = dismiss.GetComponent<RectTransform>();
            dismissRect.sizeDelta = rect.sizeDelta;
            dismissRect.anchoredPosition += Vector2.right * 140;
            Bind(data, "restartFailure", restart);
            data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        if (opened)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    [MenuItem("Tools/HY-Sandbox/Main Menu/Install preferences")]
    public static void InstallPreferences()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before authoring.");
        }

        Scene scene = SceneManager.GetSceneByPath(WorkshopNavigation.MenuPath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(WorkshopNavigation.MenuPath, OpenSceneMode.Additive);
        }

        MenuSettings settings = SceneObjectQuery.FindInScene<MenuSettings>(scene);
        var data = new SerializedObject(settings);
        MainMenuView view = SceneObjectQuery.FindInScene<MainMenuView>(scene);
        var viewData = new SerializedObject(view);
        Transform panel = ((GameObject)viewData.FindProperty("settingsPanel").objectReferenceValue).transform;
        RectTransform title = panel.Find("Title").GetComponent<RectTransform>();
        title.anchoredPosition = new Vector2(-600, 225);
        title.sizeDelta = new Vector2(220, 52);
        if (data.FindProperty("autoRotate").objectReferenceValue != null)
        {
            StylePreference((Toggle)data.FindProperty("autoRotate").objectReferenceValue);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return;
        }

        Transform[] original = panel.Cast<Transform>().ToArray();
        RectTransform general = Page("General page", panel);
        foreach (Transform child in original)
        {
            if (child.name != "Title" && child.name != "Back")
            {
                child.SetParent(general, false);
            }
        }

        RectTransform preference = Page("Preference page", panel);
        Toggle toggle = DefaultControls.CreateToggle(new DefaultControls.Resources()).GetComponent<Toggle>();
        toggle.name = "Auto rotate restricted connectors";
        Place(toggle.GetComponent<RectTransform>(), preference, new Vector2(-330, 100), new Vector2(750, 58));
        Text label = toggle.GetComponentInChildren<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 21;
        label.color = Color.white;
        label.text = "AUTO ROTATE TO SURFACE";
        StylePreference(toggle);
        var help = new GameObject("Auto rotate description", typeof(RectTransform), typeof(Text));
        Place(help.GetComponent<RectTransform>(), preference, new Vector2(-330, -10), new Vector2(750, 110));
        Text description = help.GetComponent<Text>();
        description.font = label.font;
        description.fontSize = 19;
        description.color = new Color(.65f, .8f, .88f);
        description.text = "Align modules with restricted attachment faces (such as turrets) to the selected surface.\n\nHold RMB to navigate. Release RMB to resume editing.";
        description.raycastTarget = false;
        Button template = panel.GetComponentsInChildren<Button>(true).First(button => button.name == "Back");
        Button generalTab = Tab(template, panel, "General", new Vector2(-200, 225));
        Button preferenceTab = Tab(template, panel, "Preference", new Vector2(50, 225));
        Bind(data, "autoRotate", toggle);
        Bind(data, "generalTab", generalTab);
        Bind(data, "preferenceTab", preferenceTab);
        Bind(data, "generalPage", general.gameObject);
        Bind(data, "preferencePage", preference.gameObject);
        data.ApplyModifiedPropertiesWithoutUndo();
        preference.gameObject.SetActive(false);
        foreach (Text text in panel.GetComponentsInChildren<Text>(true))
        {
            text.text = text.text.Replace("ESC    Return to main menu", "ESC    Pause / Resume").Replace(
                "Use the existing build toolbar for editing.",
                "Hold RMB + WASD / arrows to navigate.\nRelease RMB to edit.");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void StylePreference(Toggle toggle)
    {
        Image row = toggle.GetComponent<Image>();
        if (row == null)
        {
            row = toggle.gameObject.AddComponent<Image>();
        }

        row.color = new Color(.045f, .075f, .10f, .9f);
        row.raycastTarget = true;
        RectTransform box = toggle.targetGraphic.rectTransform;
        box.anchorMin = box.anchorMax = new Vector2(0, .5f);
        box.pivot = new Vector2(.5f, .5f);
        box.anchoredPosition = new Vector2(24, 0);
        box.sizeDelta = new Vector2(28, 28);
        ((Image)toggle.targetGraphic).sprite = null;
        toggle.targetGraphic.color = new Color(.22f, .32f, .4f);
        RectTransform mark = toggle.graphic.rectTransform;
        mark.anchorMin = mark.anchorMax = mark.pivot = new Vector2(.5f, .5f);
        mark.anchoredPosition = Vector2.zero;
        mark.sizeDelta = new Vector2(16, 16);
        ((Image)toggle.graphic).sprite = null;
        toggle.graphic.color = new Color(.15f, .85f, .8f);
        Text label = toggle.GetComponentInChildren<Text>();
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = new Vector2(50, 0);
        label.rectTransform.offsetMax = new Vector2(-12, 0);
        label.alignment = TextAnchor.MiddleLeft;
    }

    private static RectTransform Page(string name, Transform parent)
    {
        var root = new GameObject(name, typeof(RectTransform));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Button Tab(Button template, Transform parent, string name, Vector2 position)
    {
        Button button = UnityEngine.Object.Instantiate(template, parent);
        button.name = name + " tab";
        button.onClick = new Button.ButtonClickedEvent();
        button.GetComponentInChildren<Text>().text = name.ToUpperInvariant();
        Place(button.GetComponent<RectTransform>(), parent, position, new Vector2(230, 46));
        return button;
    }

    private static void Place(RectTransform rect, Transform parent, Vector2 position, Vector2 size)
    {
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Bind(SerializedObject data, string name, UnityEngine.Object value)
    {
        data.FindProperty(name).objectReferenceValue = value;
    }
}

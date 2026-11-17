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
            root.GetComponent<Image>().color = new Color(.015f, .025f, .045f, .96f);
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

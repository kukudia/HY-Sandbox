// 职责：为新版基地战保存共享机库部署UI；只重建工具拥有的Hangar Deployment Interface。
// 入口：Install()；与旧原型使用同一部署面板组件和筛选规则。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SiegeHangarInterfaceAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Siege/Install hangar deployment interface")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save scenes and exit Play Mode first.");
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/SiegeGameplay.unity", OpenSceneMode.Single);
            SiegeGameplayStage stage = SceneObjectQuery.FindInScene<SiegeGameplayStage>(scene);
            Transform previous = stage.transform.Find("Hangar Deployment Interface");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject root = new GameObject(
                "Hangar Deployment Interface",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(SiegeDeploymentView));
            root.transform.SetParent(stage.transform, false);
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 15;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            SiegeDeploymentView view = root.GetComponent<SiegeDeploymentView>();
            SerializedObject data = new SerializedObject(view);
            Bind(data, "stage", stage);
            RectTransform card = Panel("Ship Actions", root.transform, new Vector2(1430, 230), new Vector2(460, 460));
            Bind(data, "card", card.gameObject);
            Bind(
                data,
                "title",
                Label(
                    "Title",
                    card,
                    "HANGAR",
                    new Vector2(20, 15),
                    new Vector2(420, 44),
                    25));
            GameObject dropdownObject = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            dropdownObject.name = "Ship Blueprints";
            dropdownObject.transform.SetParent(card, false);
            Place((RectTransform)dropdownObject.transform, new Vector2(20, 75), new Vector2(420, 46));
            Dropdown dropdown = dropdownObject.GetComponent<Dropdown>();
            foreach (Text text in dropdownObject.GetComponentsInChildren<Text>(true))
            {
                text.font = Font;
                text.fontSize = 18;
            }

            DropdownTemplateAuthoring.Apply(dropdown);
            Bind(data, "defenderBlueprint", dropdown);
            Bind(
                data,
                "details",
                Label(
                    "Details",
                    card,
                    "",
                    new Vector2(20, 140),
                    new Vector2(420, 130),
                    19));
            Bind(data, "confirm", Button("Deploy", card, "DEPLOY SHIP", new Vector2(20, 290), new Vector2(420, 48)));
            Bind(data, "pilot", Button("Pilot", card, "TAKE CONTROL", new Vector2(20, 290), new Vector2(420, 48)));
            Bind(data, "repair", Button("Repair", card, "REPAIR SHIP", new Vector2(20, 360), new Vector2(270, 48)));
            Bind(data, "close", Button("Close", card, "CLOSE", new Vector2(310, 360), new Vector2(130, 48)));
            card.gameObject.SetActive(false);
            RectTransform debug = Panel("Blueprint Controls", root.transform, new Vector2(1430, 810), new Vector2(460, 180));
            Label(
                "Title",
                debug,
                "BLUEPRINTS [F3]",
                new Vector2(20, 10),
                new Vector2(420, 30),
                20);
            GameObject attackers = UnityEngine.Object.Instantiate(dropdownObject, debug);
            attackers.name = "Attacker Blueprints";
            Place((RectTransform)attackers.transform, new Vector2(20, 50), new Vector2(420, 46));
            Bind(data, "attackerBlueprint", attackers.GetComponent<Dropdown>());
            Bind(
                data,
                "refresh",
                Button("Refresh", debug, "REFRESH BLUEPRINTS", new Vector2(20, 112), new Vector2(420, 48)));
            Bind(data, "debugPanel", debug.gameObject);
            debug.gameObject.SetActive(false);
            SerializedProperty markers = data.FindProperty("markers");
            markers.arraySize = 6;
            for (int i = 0; i < markers.arraySize; i++)
            {
                Button marker = Button("Hangar " + (i + 1), root.transform, "DEPLOY", Vector2.zero, new Vector2(110, 42));
                RectTransform rect = (RectTransform)marker.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                marker.gameObject.SetActive(false);
                markers.GetArrayElementAtIndex(i).objectReferenceValue = marker;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            UpgradeActions(view);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    [MenuItem("Tools/HY-Sandbox/Siege/Upgrade deployment replacement actions")]
    public static void UpgradeScenes()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save scenes and exit Play Mode first.");
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string path in new[]
            {
                "Assets/Scenes/Siege.unity",
                "Assets/Scenes/SiegeGameplay.unity",
                "Assets/Scenes/SiegeStress.unity",
                "Assets/Scenes/SiegeMixedStress.unity",
                "Assets/Scenes/SiegeLargeStress.unity"
            }

            )
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                UpgradeActions(SceneObjectQuery.FindInScene<SiegeDeploymentView>(scene));
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }

    /// <summary>保留已有面板、目录和引用，只补齐换舰/取消按钮及其可编辑布局。</summary>
    public static void UpgradeActions(SiegeDeploymentView view)
    {
        Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Add deployment replacement actions");
        SerializedObject data = new SerializedObject(view);
        RectTransform card = (RectTransform)((GameObject)data.FindProperty("card").objectReferenceValue).transform;
        card.sizeDelta = new Vector2(460, 555);
        if (data.FindProperty("stage").objectReferenceValue != null)
        {
            GameObject debug = (GameObject)data.FindProperty("debugPanel").objectReferenceValue;
            Place((RectTransform)debug.transform, new Vector2(1430, 810), new Vector2(460, 180));
        }

        foreach (string field in new[]
        {
            "replace",
            "undeploy"
        }

        )
        {
            if (data.FindProperty(field).objectReferenceValue == null)
            {
                Bind(
                    data,
                    field,
                    Button(
                        field == "replace" ? "Replace Ship" : "Undeploy Ship",
                        card,
                        field == "replace" ? "REPLACE SHIP" : "UNDEPLOY / REFUND",
                        Vector2.zero,
                        new Vector2(420, 48)));
            }
        }

        data.ApplyModifiedPropertiesWithoutUndo();
        data.Update();
        PlaceAction(data, "title", new Vector2(20, 15), new Vector2(420, 44));
        PlaceAction(data, "defenderBlueprint", new Vector2(20, 75), new Vector2(420, 48));
        PlaceAction(data, "details", new Vector2(20, 140), new Vector2(420, 130));
        PlaceAction(data, "confirm", new Vector2(20, 290), new Vector2(420, 48));
        PlaceAction(data, "pilot", new Vector2(20, 290), new Vector2(420, 48));
        PlaceAction(data, "repair", new Vector2(20, 350), new Vector2(200, 48));
        PlaceAction(data, "replace", new Vector2(240, 350), new Vector2(200, 48));
        PlaceAction(data, "undeploy", new Vector2(20, 410), new Vector2(420, 48));
        PlaceAction(data, "close", new Vector2(310, 480), new Vector2(130, 48));
        foreach (string field in new[]
        {
            "confirm",
            "pilot",
            "repair",
            "replace",
            "undeploy",
            "close"
        }

        )
        {
            Button button = (Button)data.FindProperty(field).objectReferenceValue;
            Text text = button.GetComponentInChildren<Text>(true);
            RectTransform rect = text.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6, 0);
            rect.offsetMax = new Vector2(-6, 0);
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
        }

        SerializedProperty markers = data.FindProperty("markers");
        for (int i = 0; i < markers.arraySize; i++)
        {
            Button marker = (Button)markers.GetArrayElementAtIndex(i).objectReferenceValue;
            ((RectTransform)marker.transform).sizeDelta = new Vector2(110, 42);
            Text text = marker.GetComponentInChildren<Text>(true);
            text.fontSize = 18;
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.alignment = TextAnchor.MiddleCenter;
        }

        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
    }

    private static void PlaceAction(SerializedObject data, string field, Vector2 position, Vector2 size)
    {
        Component target = (Component)data.FindProperty(field).objectReferenceValue;
        Place((RectTransform)target.transform, position, size);
    }

    private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    private static void Bind(SerializedObject data, string field, UnityEngine.Object value)
    {
        data.FindProperty(field).objectReferenceValue = value;
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
        item.transform.SetParent(parent, false);
        item.GetComponent<Image>().color = new Color(.025f, .055f, .08f, .96f);
        Place((RectTransform)item.transform, position, size);
        return (RectTransform)item.transform;
    }

    private static Text Label(
        string name,
        Transform parent,
        string value,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        GameObject item = new GameObject(name, typeof(RectTransform), typeof(Text));
        item.transform.SetParent(parent, false);
        Text text = item.GetComponent<Text>();
        text.font = Font;
        text.fontSize = fontSize;
        text.color = new Color(.9f, .96f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.text = value;
        text.raycastTarget = false;
        Place(text.rectTransform, position, size);
        return text;
    }

    private static Button Button(string name, Transform parent, string caption, Vector2 position, Vector2 size)
    {
        RectTransform rect = Panel(name, parent, position, size);
        rect.GetComponent<Image>().color = new Color(.08f, .23f, .3f, .97f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        Text label = Label(
            "Label",
            rect,
            caption,
            Vector2.zero,
            size,
            19);
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
    }
}

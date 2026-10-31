// 职责：将当前Siege场景迁移为场景泊位交互、按需操作卡片和独立阶段HUD，保存可编辑UGUI资产。
// 入口：Apply()；只重建拥有的Siege HUD，保留竞技场布置、部署点和蓝图数据。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeInterfaceAuthoring
{
    private static Font font;
    private static readonly Color Background = new Color(0.025f, 0.055f, 0.075f, 0.96f);
    private static readonly Color Accent = new Color(0.25f, 0.85f, 0.9f);
    [MenuItem("HY-Sandbox/Siege/Apply Scene Deployment Interface")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || (!scene.path.StartsWith("Assets/Scenes/Siege", StringComparison.Ordinal) && !string.IsNullOrEmpty(scene.path)))
        {
            throw new InvalidOperationException("Open Siege in Edit Mode first.");
        }

        SiegeHud hud = Object.FindFirstObjectByType<SiegeHud>();
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        font = hud.GetComponentsInChildren<Text>(true).Select(text => text.font).FirstOrDefault(item => item != null) ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Dropdown source = hud.GetComponentsInChildren<Dropdown>(true).FirstOrDefault();
        GameObject template = source != null ? Object.Instantiate(source.gameObject) : DefaultControls.CreateDropdown(new DefaultControls.Resources());
        template.SetActive(false);
        template.name = "Temporary dropdown template";
        Undo.RegisterFullObjectHierarchyUndo(hud.gameObject, "Migrate Siege interface");
        foreach (Transform child in hud.transform.Cast<Transform>().ToArray())
        {
            Object.DestroyImmediate(child.gameObject);
        }

        CanvasScaler scaler = hud.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        var header = Panel(
            hud.transform,
            "Command Header",
            24f,
            20f,
            1872f,
            130f);
        Bind(hud, "session", session);
        Bind(hud, "heading", Label(
            header,
            "Heading",
            "BASTION",
            20f,
            12f,
            500f,
            42f,
            28));
        Bind(hud, "economy", Label(
            header,
            "Economy",
            "CREDITS",
            560f,
            12f,
            1250f,
            42f,
            24));
        Bind(hud, "coreHealth", Label(
            header,
            "Integrity",
            "CORE",
            20f,
            66f,
            1800f,
            40f,
            22));
        var status = Label(
            hud.transform,
            "Status",
            "Click a deployment pad to choose a ship.",
            32f,
            160f,
            1200f,
            52f,
            18);
        Bind(hud, "status", status);
        var preparation = Panel(
            hud.transform,
            "Preparation Actions",
            520f,
            970f,
            880f,
            86f);
        Button workshop = Button(
            preparation,
            "Open Blueprint Workshop",
            "BLUEPRINTS  [B]",
            16f,
            16f,
            260f);
        Bind(hud, "preparation", preparation.gameObject);
        Bind(hud, "repairCore", Button(
            preparation,
            "Repair Base",
            "REPAIR BASE",
            300f,
            16f,
            260f));
        Bind(hud, "wave", Button(
            preparation,
            "Begin Wave",
            "NEXT WAVE  [P]",
            584f,
            16f,
            280f));
        var defeat = Panel(
            hud.transform,
            "Defeat Actions",
            690f,
            600f,
            540f,
            160f);
        Label(
            defeat,
            "Result",
            "THE CORE HAS FALLEN",
            30f,
            20f,
            480f,
            45f,
            26);
        Bind(hud, "restart", Button(
            defeat,
            "Restart Siege",
            "RESTART SIEGE",
            130f,
            90f,
            280f));
        Bind(hud, "defeat", defeat.gameObject);
        defeat.gameObject.SetActive(false);
        var help = Panel(
            hud.transform,
            "Controls Help",
            560f,
            260f,
            800f,
            220f);
        Label(
            help,
            "Controls",
            "PREPARE: click a pad / preview / confirm\nCOMBAT: WASD / QE / Space    ALT: pointer\nTAB: next friendly ship    F: focus\nB: blueprints    P: next wave    F3: debug\nESC: cancel selection    F2: close help",
            24f,
            20f,
            750f,
            180f,
            22);
        Bind(hud, "help", help.gameObject);
        help.gameObject.SetActive(false);
        var hint = Label(
            hud.transform,
            "Controls Hint",
            "F2 HELP   /   ALT + CLICK FRIENDLY SHIP   /   TAB SWITCH PILOT",
            520f,
            925f,
            1200f,
            32f,
            17);
        hint.color = new Color(0.6f, 0.72f, 0.76f);
        SiegeDeploymentView view = hud.GetComponent<SiegeDeploymentView>() ?? hud.gameObject.AddComponent<SiegeDeploymentView>();
        Bind(view, "session", session);
        var card = Panel(
            hud.transform,
            "Ship Actions",
            28f,
            240f,
            440f,
            440f);
        Bind(view, "card", card.gameObject);
        Bind(view, "title", Label(
            card,
            "Selection",
            "DEPLOY",
            20f,
            16f,
            400f,
            48f,
            25));
        Bind(
            view,
            "defenderBlueprint",
            Dropdown(
                template,
                card,
                "Defender Blueprint",
                20f,
                80f,
                400f));
        Bind(view, "details", Label(
            card,
            "Details",
            "",
            20f,
            146f,
            400f,
            130f,
            20));
        Bind(
            view,
            "confirm",
            Button(
                card,
                "Confirm Deployment",
                "CONFIRM DEPLOYMENT",
                20f,
                285f,
                400f));
        Bind(view, "pilot", Button(
            card,
            "Pilot Ship",
            "TAKE CONTROL",
            20f,
            285f,
            400f));
        Bind(view, "repair", Button(
            card,
            "Repair Ship",
            "REPAIR SHIP",
            20f,
            352f,
            270f));
        Bind(view, "close", Button(
            card,
            "Cancel Selection",
            "CLOSE",
            300f,
            352f,
            120f));
        card.gameObject.SetActive(false);
        var debug = Panel(
            hud.transform,
            "Debug Controls",
            1380f,
            170f,
            510f,
            190f);
        Label(
            debug,
            "Debug Title",
            "DEBUG / ATTACKER SELECTION",
            18f,
            12f,
            470f,
            35f,
            20);
        Bind(view, "debugPanel", debug.gameObject);
        Bind(
            view,
            "attackerBlueprint",
            Dropdown(
                template,
                debug,
                "Attacker Blueprint",
                18f,
                56f,
                474f));
        Bind(
            view,
            "refresh",
            Button(
                debug,
                "Refresh Blueprints",
                "REFRESH BLUEPRINTS",
                18f,
                120f,
                474f));
        debug.gameObject.SetActive(false);
        Object.DestroyImmediate(template);
        var data = new SerializedObject(session);
        SerializedProperty docks = data.FindProperty("shipDocks");
        var viewData = new SerializedObject(view);
        SerializedProperty markers = viewData.FindProperty("markers");
        markers.arraySize = docks.arraySize;
        for (int index = 0; index < docks.arraySize; index++)
        {
            GameObject platform = scene.GetRootGameObjects().First(root => root.name == "Bay " + (index + 1) + " Platform");
            SiegeDeploymentPad pad = platform.GetComponent<SiegeDeploymentPad>() ?? platform.AddComponent<SiegeDeploymentPad>();
            var padData = new SerializedObject(pad);
            padData.FindProperty("index").intValue = index;
            padData.ApplyModifiedPropertiesWithoutUndo();
            Button marker = Button(
                hud.transform,
                "Deployment Marker " + (index + 1),
                "+",
                0f,
                0f,
                52f);
            RectTransform rect = (RectTransform)marker.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(52f, 52f);
            Text markerLabel = marker.GetComponentInChildren<Text>();
            markerLabel.rectTransform.sizeDelta = new Vector2(36f, 52f);
            markerLabel.fontSize = 28;
            marker.gameObject.SetActive(false);
            markers.GetArrayElementAtIndex(index).objectReferenceValue = marker;
        }

        viewData.ApplyModifiedPropertiesWithoutUndo();
        SiegeBattleFeedback feedback = hud.GetComponent<SiegeBattleFeedback>() ?? hud.gameObject.AddComponent<SiegeBattleFeedback>();
        Bind(feedback, "session", session);
        Bind(
            feedback,
            "banner",
            Label(
                hud.transform,
                "Wave Banner",
                "",
                560f,
                240f,
                1000f,
                60f,
                32));
        Bind(
            feedback,
            "notice",
            Label(
                hud.transform,
                "Kill Feed",
                "",
                560f,
                310f,
                1100f,
                45f,
                22));
        var alarm = Panel(
            hud.transform,
            "Core Alarm",
            0f,
            0f,
            1920f,
            8f);
        alarm.GetComponent<Image>().color = new Color(1f, 0.1f, 0.05f);
        alarm.GetComponent<Image>().raycastTarget = false;
        CanvasGroup damage = alarm.gameObject.AddComponent<CanvasGroup>();
        damage.blocksRaycasts = false;
        Bind(feedback, "damage", damage);
        SiegeWorkshop editor = Object.FindFirstObjectByType<SiegeWorkshop>();
        if (editor != null)
        {
            Bind(editor, "buildButton", workshop);
        }
        else
        {
            workshop.gameObject.SetActive(false);
        }

        MainUIPanels panels = Object.FindFirstObjectByType<MainUIPanels>();
        if (panels != null)
        {
            panels.playPanel.SetActive(false);
            panels.buildPanel.SetActive(false);
        }

        // 保留编辑器实际需要的选块、存档、变换、撤销控件，调试显示不占据正常编辑界面。
        Transform debugSettings = panels != null ? panels.buildPanel.transform.Find("DebugSettingsPanel") : null;
        if (debugSettings != null)
        {
            debugSettings.gameObject.SetActive(false);
        }

        MainUIButtons buttons = Object.FindFirstObjectByType<MainUIButtons>();
        foreach (Button button in buttons != null ? new[]
        {
            buttons.showPowerRangeButton,
            buttons.showPowerConnectionsButton,
            buttons.showConnectionStatusButton,
            buttons.showDurabilityStatusButton,
            buttons.showPowerStatusButton
        }

        : new Button[0])
        {
            if (button != null)
            {
                button.gameObject.SetActive(false);
            }
        }

        preparation.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!string.IsNullOrEmpty(scene.path))
        {
            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
    }

    private static RectTransform Panel(
        Transform parent,
        string name,
        float x,
        float y,
        float width,
        float height)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.GetComponent<Image>().color = Background;
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        Place(rect, x, y, width, height);
        return rect;
    }

    private static Text Label(
        Transform parent,
        string name,
        string content,
        float x,
        float y,
        float width,
        float height,
        int size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text label = obj.GetComponent<Text>();
        label.font = font;
        label.text = content;
        label.fontSize = size;
        label.color = new Color(0.86f, 0.95f, 0.97f);
        label.raycastTarget = false;
        label.alignment = TextAnchor.MiddleLeft;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        Place(label.rectTransform, x, y, width, height);
        return label;
    }

    private static Button Button(
        Transform parent,
        string name,
        string content,
        float x,
        float y,
        float width)
    {
        RectTransform rect = Panel(
            parent,
            name,
            x,
            y,
            width,
            54f);
        rect.GetComponent<Image>().color = new Color(0.055f, 0.24f, 0.29f, 1f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();
        Text label = Label(
            rect,
            "Label",
            content,
            8f,
            0f,
            width - 16f,
            54f,
            20);
        label.color = Accent;
        label.alignment = TextAnchor.MiddleCenter;
        return button;
    }

    private static Dropdown Dropdown(
        GameObject template,
        Transform parent,
        string name,
        float x,
        float y,
        float width)
    {
        GameObject obj = Object.Instantiate(template, parent);
        obj.name = name;
        obj.SetActive(true);
        Dropdown dropdown = obj.GetComponent<Dropdown>();
        dropdown.onValueChanged = new Dropdown.DropdownEvent();
        foreach (Text label in dropdown.GetComponentsInChildren<Text>(true))
        {
            label.font = font;
        }

        Place((RectTransform)obj.transform, x, y, width, 54f);
        return dropdown;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Bind(Object target, string field, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}

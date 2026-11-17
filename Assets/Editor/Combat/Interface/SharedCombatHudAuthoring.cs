// 职责：制作共享战斗HUD Prefab、TMP字体和主题，并幂等迁移真实场景的相关界面。
// 入口：Install() / InstallScene()；只重建自身HUD和旧战斗面板，保存前拒绝未保存场景。
using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SharedCombatHudAuthoring
{
    public static void InstallNameplateExclusions()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play before saving HUD references.");
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ConfigureNameplateExclusions(root);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConfigureNameplateExclusions(GameObject root)
    {
        CanvasGroup[] regions = root.GetComponentsInChildren<CanvasGroup>(true).Where(group => group.name == "Debug Panel" || group.name == "Window" || group.name == "KillNotice" || group.GetComponent<CombatFeedItem>() != null).ToArray();
        var data = new SerializedObject(root.GetComponent<CombatHud>());
        SerializedProperty array = data.FindProperty("nameplateExclusions");
        array.arraySize = regions.Length;
        for (int i = 0; i < regions.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = regions[i];
        }

        data.ApplyModifiedPropertiesWithoutUndo();
    }

    public const string Folder = "Assets/Art/CombatHud";
    public const string PrefabPath = Folder + "/SharedCombatHud.prefab";
    private static TMP_FontAsset font;
    private static CombatHudTheme theme;
    private static Color Ink => theme.background;
    private static Color Cyan => theme.friendly;

    [MenuItem("Tools/HY-Sandbox/Combat/Install shared battle HUD")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Exit Play Mode and save scenes before HUD migration.");
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        string backup = "Temp/CombatHudMigration-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff");
        Directory.CreateDirectory(backup);
        string[] paths =
        {
            "Assets/Scenes/Build.unity",
            "Assets/Scenes/Siege.unity",
            "Assets/Scenes/Random5v5Arena.unity",
            "Assets/Scenes/SiegeGameplay.unity"
        };
        foreach (string path in paths)
        {
            File.Copy(path, Path.Combine(backup, Path.GetFileName(path)));
        }

        EnsureAssets();
        try
        {
            foreach (string path in paths)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                InstallScene(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        finally
        {
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }

        AssetDatabase.SaveAssets();
    }

    public static void InstallScene(Scene scene)
    {
        EnsureAssets();
        MainUIPanels main = Find<MainUIPanels>(scene);
        SiegeGameplayStage stage = Find<SiegeGameplayStage>(scene);
        CombatHud old = Find<CombatHud>(scene);
        if (Find<SiegeSession>(scene) != null)
        {
            foreach (Text legacy in scene.GetRootGameObjects().SelectMany(item => item.GetComponentsInChildren<Text>(true)).Where(item => item.name == "Kill Feed").ToArray())
            {
                Object.DestroyImmediate(legacy.gameObject);
            }
        }

        Transform parent = old != null ? old.transform.parent : main != null ? main.playPanel.transform : stage.transform.Find("Siege Gameplay UI");
        // Legacy Siege hides PlayPanel while observing AI ships; its HUD must outlive that driving-only panel.
        if (Find<SiegeSession>(scene) != null && main != null)
        {
            parent = main.playPanel.transform.parent;
        }

        if (old != null)
        {
            Object.DestroyImmediate(old.gameObject);
        }

        foreach (ThrusterInfoPanel legacy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ThrusterInfoPanel>(true)).ToArray())
        {
            Object.DestroyImmediate(legacy.gameObject);
        }

        foreach (MonoBehaviour legacy in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Where(item => item != null && item.GetType().Name == "SiegeThreatMarkers").ToArray())
        {
            foreach (Transform marker in legacy.transform.Cast<Transform>().Where(child => child.name.StartsWith("Threat Marker ")).ToArray())
            {
                Object.DestroyImmediate(marker.gameObject);
            }

            Object.DestroyImmediate(legacy);
        }

        GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
        root.name = "CombatHud";
        root.transform.SetParent(parent, false);
        Stretch((RectTransform)root.transform);
        CombatHud hud = root.GetComponent<CombatHud>();
        ThrusterInfoPanel thrusters = root.GetComponentInChildren<ThrusterInfoPanel>(true);
        if (main != null)
        {
            Set(main, "_combatHud", hud);
            Set(main, "_thrusterInfoPanel", thrusters);
        }

        SkirmishHud arena = Find<SkirmishHud>(scene);
        if (arena != null)
        {
            var hudSettings = new SerializedObject(hud);
            hudSettings.FindProperty("nameplateDistanceOverride").floatValue = 850f;
            hudSettings.ApplyModifiedPropertiesWithoutUndo();
            Set(arena, "combatHud", hud);
            Transform feed = arena.transform.Find("Kill Feed");
            if (feed != null)
            {
                Object.DestroyImmediate(feed.gameObject);
            }
        }

        if (stage != null)
        {
            SiegeCombatHud entry = root.AddComponent<SiegeCombatHud>();
            Set(entry, "hud", hud);
            Set(entry, "panels", root.GetComponentInChildren<CombatHudPanels>(true));
            Set(entry, "thrusters", thrusters);
            TMP_Text summary = Label(
                root.transform,
                "Siege Summary",
                "BASE CORE",
                new Vector2(20, -20),
                new Vector2(320, 85),
                15,
                Cyan);
            Set(entry, "summary", summary);
            Set(
                entry,
                "nextShip",
                Button(root.transform, "Inspect next ally", new Vector2(20, -112), new Vector2(190, 34)));
            Set(stage, "combatHud", entry);
            Transform help = stage.transform.Find("Siege Gameplay UI/Camera Help");
            if (help != null)
            {
                RectTransform rect = (RectTransform)help;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -110f);
                rect.sizeDelta = new Vector2(740f, 30f);
                Text label = help.GetComponent<Text>();
                label.text = "RMB + WASD/QE  |  Shift: fast  |  Wheel: speed  |  F: overview";
                label.fontSize = 13;
            }

            SiegeGameplayHudAuthoring.ConfigureStage(stage);
        }

        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void EnsureAssets()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Art", "CombatHud");
        }

        theme = AssetDatabase.LoadAssetAtPath<CombatHudTheme>(Folder + "/Theme.asset");
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<CombatHudTheme>();
            AssetDatabase.CreateAsset(theme, Folder + "/Theme.asset");
        }

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Folder + "/ChakraPetch SDF.asset");
        if (font == null)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
            font = TMP_FontAsset.CreateFontAsset(
                source,
                48,
                5,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic);
            font.name = "ChakraPetch SDF";
            AssetDatabase.CreateAsset(font, Folder + "/ChakraPetch SDF.asset");
            font.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
            AssetDatabase.AddObjectToAsset(font.material, font);
            foreach (Texture2D atlas in font.atlasTextures)
            {
                AssetDatabase.AddObjectToAsset(atlas, font);
            }

            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            BakePrefab();
        }
    }

    [MenuItem("Tools/HY-Sandbox/Combat/Rebuild shared HUD prefab")]
    public static void RebuildPrefab()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        EnsureAssets();
        BakePrefab();
    }

    private static void BakePrefab()
    {
        RectTransform root = Rect("SharedCombatHud", null, Vector2.zero, new Vector2(1920, 1080));
        try
        {
            Stretch(root);
            CombatHud hud = root.gameObject.AddComponent<CombatHud>();
            RectTransform overlay = Rect("World Markers", root, Vector2.zero, Vector2.zero);
            Stretch(overlay);
            CanvasGroup overlayGroup = overlay.gameObject.AddComponent<CanvasGroup>();
            overlayGroup.blocksRaycasts = overlayGroup.interactable = false;
            RectTransform plate = BuildNameplate(overlay);
            Set(hud, "_overlay", overlay);
            Set(hud, "_enemyTemplate", plate);
            Set(hud, "theme", theme);
            Set(hud, "threats", BuildThreats(overlay, hud));
            Set(hud, "notifications", BuildNotifications(root));
            BuildPanels(root);
            Set(hud, "panels", root.GetComponentInChildren<CombatHudPanels>(true));
            Set(
                root.GetComponentInChildren<CombatHudPanels>(true),
                "notifications",
                root.GetComponentInChildren<CombatNotifications>(true));
            SiegeGameplayHudAuthoring.Restyle(root);
            ConfigureNameplateExclusions(root.gameObject);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root.gameObject);
        }
    }

    private static RectTransform BuildNameplate(Transform parent)
    {
        RectTransform root = Rect("EnemyNameplateTemplate", parent, Vector2.zero, new Vector2(240, 68));
        root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
        root.pivot = new Vector2(.5f, 0);
        Back(root, Ink);
        CombatNameplate view = root.gameObject.AddComponent<CombatNameplate>();
        Set(view, "group", Group(root));
        Set(
            view,
            "nameLabel",
            Label(
                root,
                "Name",
                "HOSTILE / RAIDER",
                new Vector2(10, -4),
                new Vector2(220, 20),
                14,
                Color.white));
        Set(
            view,
            "valueLabel",
            Label(
                root,
                "Value",
                "100 / 100",
                new Vector2(10, -23),
                new Vector2(220, 15),
                11,
                Color.white));
        Set(
            view,
            "cockpitLabel",
            Label(
                root,
                "CockpitValue",
                "CORE 100 / 100",
                new Vector2(10, -48),
                new Vector2(220, 15),
                10,
                Cyan));
        RectTransform bar = Rect("Bar", root, new Vector2(10, -40), new Vector2(220, 6));
        Back(bar, new Color(.15f, .2f, .25f));
        Set(view, "damageFill", Fill(bar, "Damage", theme.warning));
        Set(view, "healthFill", Fill(bar, "Fill", theme.hostile));
        RectTransform core = Rect("CockpitBar", root, new Vector2(10, -63), new Vector2(220, 3));
        Back(core, new Color(.15f, .2f, .25f));
        Set(view, "cockpitFill", Fill(core, "Fill", Cyan));
        Set(view, "accent", Back(Rect("Accent", root, Vector2.zero, new Vector2(3, 68)), theme.hostile));
        root.gameObject.SetActive(false);
        return root;
    }

    private static CombatThreatMarkers BuildThreats(Transform parent, CombatHud hud)
    {
        RectTransform root = Rect("Threat Markers", parent, Vector2.zero, Vector2.zero);
        Stretch(root);
        CombatThreatMarkers service = root.gameObject.AddComponent<CombatThreatMarkers>();
        Set(service, "hud", hud);
        var markers = new CombatThreatView[theme.maximumMarkers];
        for (int i = 0; i < markers.Length; i++)
        {
            RectTransform marker = Rect("Threat " + (i + 1), root, Vector2.zero, new Vector2(95, 58));
            marker.anchorMin = marker.anchorMax = marker.pivot = new Vector2(.5f, .5f);
            CombatThreatView view = marker.gameObject.AddComponent<CombatThreatView>();
            Set(view, "group", Group(marker));
            TMP_Text arrow = Label(
                marker,
                "Arrow",
                "^",
                new Vector2(32, 0),
                new Vector2(32, 32),
                32,
                theme.hostile);
            arrow.alignment = TextAlignmentOptions.Center;
            arrow.rectTransform.pivot = new Vector2(.5f, .5f);
            arrow.rectTransform.anchoredPosition = Vector2.zero;
            Set(view, "arrow", arrow.rectTransform);
            TMP_Text bracket = Label(
                marker,
                "Bracket",
                "[  ]",
                new Vector2(12, -2),
                new Vector2(72, 30),
                24,
                theme.hostile);
            Set(view, "bracket", bracket.gameObject);
            Back(Rect("Distance Background", marker, new Vector2(-10, -32), new Vector2(115, 22)), Ink);
            TMP_Text label = Label(
                marker,
                "Distance",
                "100m",
                new Vector2(-10, -32),
                new Vector2(115, 22),
                13,
                Color.white);
            label.alignment = TextAlignmentOptions.Center;
            Set(view, "label", label);
            markers[i] = view;
        }

        SetArray(service, "markers", markers);
        return service;
    }

    private static CombatNotifications BuildNotifications(RectTransform parent)
    {
        RectTransform root = Rect("Notifications", parent, Vector2.zero, Vector2.zero);
        Stretch(root);
        Group(root);
        CombatNotifications notifications = root.gameObject.AddComponent<CombatNotifications>();
        Set(notifications, "theme", theme);
        RectTransform feed = Rect("Kill Feed", root, new Vector2(-20, -65), new Vector2(400, 310));
        Set(notifications, "feedRoot", feed);
        feed.anchorMin = feed.anchorMax = feed.pivot = Vector2.one;
        var items = new CombatFeedItem[5];
        for (int i = 0; i < items.Length; i++)
        {
            RectTransform row = Rect("Report " + (i + 1), feed, new Vector2(0, -i * 62), new Vector2(400, 56));
            Back(row, Ink);
            CombatFeedItem item = row.gameObject.AddComponent<CombatFeedItem>();
            Set(item, "group", Group(row));
            Set(
                item,
                "title",
                Label(
                    row,
                    "Title",
                    "",
                    new Vector2(14, -6),
                    new Vector2(372, 25),
                    15,
                    Color.white));
            Set(
                item,
                "detail",
                Label(
                    row,
                    "Detail",
                    "",
                    new Vector2(14, -33),
                    new Vector2(372, 17),
                    11,
                    Cyan));
            Back(Rect("Accent", row, Vector2.zero, new Vector2(3, 56)), theme.hostile);
            items[i] = item;
        }

        SetArray(notifications, "feed", items);
        RectTransform notice = Rect("KillNotice", root, new Vector2(0, 0), new Vector2(500, 94));
        notice.anchorMin = notice.anchorMax = notice.pivot = new Vector2(.5f, .27f);
        Back(notice, Ink);
        Set(notifications, "notice", Group(notice));
        Label(
            notice,
            "Title",
            "TARGET DESTROYED",
            new Vector2(18, -8),
            new Vector2(464, 18),
            12,
            theme.hostile);
        Set(
            notifications,
            "noticeName",
            Label(
                notice,
                "EnemyName",
                "",
                new Vector2(18, -30),
                new Vector2(464, 30),
                24,
                Color.white));
        Set(
            notifications,
            "noticeDetail",
            Label(
                notice,
                "Count",
                "",
                new Vector2(18, -66),
                new Vector2(464, 20),
                13,
                Cyan));
        return notifications;
    }

    private static void BuildPanels(RectTransform root)
    {
        RectTransform owner = Rect("Battle Tools", root, Vector2.zero, Vector2.zero);
        Stretch(owner);
        CombatHudPanels panels = owner.gameObject.AddComponent<CombatHudPanels>();
        Button debug = Button(owner, "DEBUG  F2", new Vector2(-190, -20), new Vector2(170, 34));
        debug.GetComponent<RectTransform>().anchorMin = debug.GetComponent<RectTransform>().anchorMax = debug.GetComponent<RectTransform>().pivot = Vector2.one;
        Button thrust = Button(owner, "THRUST  F1", new Vector2(-20, -20), new Vector2(160, 34));
        thrust.GetComponent<RectTransform>().anchorMin = thrust.GetComponent<RectTransform>().anchorMax = thrust.GetComponent<RectTransform>().pivot = Vector2.one;
        Set(panels, "debugButton", debug);
        Set(panels, "thrusterButton", thrust);
        RectTransform window = Drawer("Debug Panel", owner, new Vector2(380, 420));
        Set(panels, "debugWindow", Group(window, true));
        Label(
            window,
            "Title",
            "DIAGNOSTICS",
            new Vector2(18, -14),
            new Vector2(345, 28),
            22,
            Cyan);
        string[] names =
        {
            "Power coverage",
            "Power connections",
            "Connection status",
            "Module durability",
            "Power status",
            "Friendly AI gizmos",
            "Enemy AI gizmos"
        };
        var toggles = new Toggle[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            RectTransform row = Rect(names[i], window, new Vector2(18, -60 - i * 40), new Vector2(342, 32));
            Image track = Back(row, new Color(.06f, .1f, .13f));
            track.raycastTarget = true;
            Toggle toggle = row.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = track;
            toggle.graphic = Back(Rect("Check", row, new Vector2(8, -8), new Vector2(16, 16)), Cyan);
            Label(
                row,
                "Label",
                names[i],
                new Vector2(35, -3),
                new Vector2(300, 26),
                15,
                Color.white);
            toggles[i] = toggle;
        }

        SetArray(panels, "debugToggles", toggles);
        Button clear = Button(window, "ALL OFF", new Vector2(18, -355), new Vector2(342, 35));
        UnityEventTools.AddPersistentListener(clear.onClick, panels.ClearDebug);
        Label(
            window,
            "Legend",
            "AI lines require Game / Scene Gizmos",
            new Vector2(18, -395),
            new Vector2(345, 18),
            11,
            Color.gray);
        Set(panels, "thrusters", BuildThrusters(owner));
        CombatDiagnosticsAuthoring.UpgradePanel(panels);
    }

    private static ThrusterInfoPanel BuildThrusters(Transform parent)
    {
        RectTransform owner = Rect("ThrusterInfoPanel", parent, Vector2.zero, Vector2.zero);
        Stretch(owner);
        ThrusterInfoPanel panel = owner.gameObject.AddComponent<ThrusterInfoPanel>();
        RectTransform window = Drawer("Window", owner, new Vector2(380, 540));
        Group(window, true);
        Set(panel, "_window", window.gameObject);
        Label(
            window,
            "Title",
            "PROPULSION",
            new Vector2(18, -14),
            new Vector2(340, 26),
            22,
            Cyan);
        Set(
            panel,
            "_ownerLabel",
            Label(
                window,
                "Owner",
                "NO ACTIVE SHIP",
                new Vector2(18, -48),
                new Vector2(342, 44),
                13,
                Color.white));
        RectTransform tabs = Rect("Tabs", window, new Vector2(18, -98), new Vector2(342, 36));
        string[] names =
        {
            "MAIN",
            "VECTOR",
            "HOVER"
        };
        var buttons = new Button[3];
        var labels = new TMP_Text[3];
        for (int i = 0; i < 3; i++)
        {
            buttons[i] = Button(tabs, names[i], new Vector2(i * 114, 0), new Vector2(110, 32));
            labels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
        }

        SetArray(panel, "_tabs", buttons);
        SetArray(panel, "_tabLabels", labels);
        RectTransform indicator = Rect("Indicator", tabs, Vector2.zero, Vector2.zero);
        indicator.anchorMin = Vector2.zero;
        indicator.anchorMax = new Vector2(1f / 3f, 0);
        indicator.offsetMin = Vector2.zero;
        indicator.offsetMax = new Vector2(0, 3);
        Set(panel, "_tabIndicator", Back(indicator, Cyan));
        RectTransform viewport = Rect("Viewport", window, new Vector2(18, -150), new Vector2(344, 366));
        viewport.gameObject.AddComponent<RectMask2D>();
        Back(viewport, new Color(.02f, .04f, .06f));
        RectTransform content = Rect("Content", viewport, Vector2.zero, new Vector2(344, 0));
        Set(panel, "_content", content);
        ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.scrollSensitivity = 28;
        RectTransform row = Rect("Row Template", content, Vector2.zero, new Vector2(342, 60));
        Back(row, new Color(.05f, .09f, .12f));
        Label(
            row,
            "Name",
            "Thruster",
            new Vector2(10, -4),
            new Vector2(322, 22),
            15,
            Color.white);
        Label(
            row,
            "Value",
            "0 / 0 N",
            new Vector2(10, -27),
            new Vector2(322, 18),
            12,
            Cyan);
        RectTransform track = Rect("Track", row, new Vector2(10, -51), new Vector2(322, 4));
        Back(track, Color.gray);
        Fill(track, "Fill", Cyan);
        row.gameObject.SetActive(false);
        Set(panel, "_rowTemplate", row);
        Set(
            panel,
            "_emptyState",
            Label(
                window,
                "Empty",
                "NO THRUSTERS IN THIS CATEGORY",
                new Vector2(18, -155),
                new Vector2(340, 55),
                14,
                Color.gray));
        return panel;
    }

    private static RectTransform Drawer(string name, Transform parent, Vector2 size)
    {
        RectTransform rect = Rect(name, parent, new Vector2(-20, -65), size);
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        Back(rect, Ink).raycastTarget = true;
        return rect;
    }

    public static T Find<T>(Scene scene)
        where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).FirstOrDefault();
    }

    public static void Set(Object owner, string field, Object value)
    {
        var data = new SerializedObject(owner);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(Object owner, string field, Object[] values)
    {
        var data = new SerializedObject(owner);
        SerializedProperty array = data.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static Image Back(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static RectTransform Fill(RectTransform parent, string name, Color color)
    {
        RectTransform rect = Rect(name, parent, Vector2.zero, Vector2.zero);
        Stretch(rect);
        rect.pivot = Vector2.zero;
        Back(rect, color);
        return rect;
    }

    private static CanvasGroup Group(RectTransform rect, bool interactive = false)
    {
        CanvasGroup group = rect.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = group.interactable = interactive;
        return group;
    }

    private static TMP_Text Label(
        Transform parent,
        string name,
        string text,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color)
    {
        RectTransform rect = Rect(name, parent, position, size);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * .7f;
        label.fontSizeMax = fontSize;
        label.color = color;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        return label;
    }

    private static Button Button(Transform parent, string title, Vector2 position, Vector2 size)
    {
        RectTransform rect = Rect(title, parent, position, size);
        Image image = Back(rect, new Color(.06f, .13f, .17f));
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        TMP_Text label = Label(
            rect,
            "Label",
            title,
            Vector2.zero,
            size,
            14,
            Cyan);
        label.alignment = TextAlignmentOptions.Center;
        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
        return button;
    }
}

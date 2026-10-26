// 职责：把阵地战HUD重排为单条指挥栏、左侧舰队坞与底部主操作，并停用Main遗留的准备/调试遮罩。
// 入口：Apply()；幂等重排，克隆HUD已有文本以保证字体/阴影材质可渲染，不改Main场景与用户蓝图。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeCommandHudAuthoring
{
    private const float ReferenceWidth = 1920f;
    private static readonly Color PanelColor = new Color(0.024f, 0.055f, 0.082f, 0.94f);
    private static readonly Color AccentColor = new Color(0.31f, 0.88f, 0.95f, 1f);
    private static readonly Color TextColor = new Color(0.83f, 0.94f, 0.98f, 1f);
    private static readonly Color DimColor = new Color(0.55f, 0.68f, 0.74f, 1f);
    private static readonly Color ButtonFill = new Color(0.06f, 0.20f, 0.24f, 1f);
    private static readonly Color PrimaryFill = new Color(0.07f, 0.38f, 0.44f, 1f);
    [MenuItem("HY-Sandbox/Siege/Apply Command HUD")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != SiegeSceneAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Open the saved Siege scene in Edit Mode before restyling its HUD.");
        }

        SiegeHud hud = Object.FindFirstObjectByType<SiegeHud>();
        if (hud == null)
        {
            throw new InvalidOperationException("The Siege HUD component is missing from the active scene.");
        }

        // 生成物按名字重建，避免旧版布局残留；这是制作工具自己的对象，不承载用户手工编辑。
        RescueLegacyHelp(hud);
        DestroyGenerated(
            hud,
            "Core Integrity",
            "Controls Hint",
            "Controls Help",
            "Probe Label",
            "Dock Hint",
            "Ship Status",
            "Damage Vignette",
            "Wave Status",
            "Wave Banner",
            "Kill Feed");
        DisableLegacyOverlays();
        Text template = RequireText(Require(hud.transform, "Command Header"), "CREDITS / WAVE / RAIDERS");
        CommandBar(hud, template);
        FleetDock(hud);
        ActionBar(hud);
        CombatHud(hud, template);
        ThreatMarkers(hud);
        ControlsHelp(hud, template);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Siege command HUD applied: single bar, fleet dock, primary action row, collapsible controls.");
    }

    /// <summary>帮助文本可能已被移入旧帮助面板；重建前先移回HUD根，避免随面板一起销毁。</summary>
    private static void RescueLegacyHelp(SiegeHud hud)
    {
        Text legacy = FindLegacyHelp(hud);
        if (legacy != null && legacy.transform.parent != hud.transform)
        {
            legacy.transform.SetParent(hud.transform, false);
        }
    }

    private static Text FindLegacyHelp(SiegeHud hud)
    {
        foreach (Text text in hud.GetComponentsInChildren<Text>(true))
        {
            if (text.name.StartsWith("WASD /") || (text.text != null && text.text.StartsWith("WASD /")))
            {
                return text;
            }
        }

        return null;
    }

    private static void DestroyGenerated(SiegeHud hud, params string[] names)
    {
        Transform[] all = hud.GetComponentsInChildren<Transform>(true);
        foreach (Transform target in all)
        {
            if (target != null && names.Contains(target.name))
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }
    }

    /// <summary>停用Main世界准备、推进器窗口、物理遥测与返回提示；只隐藏对象，保留序列化引用可逆恢复。</summary>
    private static void DisableLegacyOverlays()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "World Preparation")
            {
                root.SetActive(false);
            }
        }

        foreach (string path in new[]
        {
            "Main Interface/MainCanvas/World Preparation",
            "Main Interface/MainCanvas/PlayPanel/ThrusterInfoPanel",
            "Main Interface/MainCanvas/PlayPanel/PhysicsTelemetry",
            "Main Interface/MainCanvas/Return Status",
            "Main Interface/MainCanvas/PlayPanel/HealthBar/ThemeRule"
        }

        )
        {
            Transform target = FindByPath(path);
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }
    }

    private static void CommandBar(SiegeHud hud, Text template)
    {
        RectTransform header = Require(hud.transform, "Command Header");
        Place(header, 0f, 0f, ReferenceWidth, 96f);
        header.GetComponent<Image>().color = PanelColor;
        EnsureLine(
            header,
            "Header Accent",
            0f,
            93f,
            ReferenceWidth,
            3f,
            AccentColor);
        Text heading = RequireText(header, "BASTION / DEPLOYMENT");
        Place(heading.rectTransform, 28f, 10f, 640f, 44f);
        heading.fontSize = 30;
        heading.color = TextColor;
        Text economy = RequireText(header, "CREDITS / WAVE / RAIDERS");
        Place(economy.rectTransform, 700f, 16f, 960f, 34f);
        economy.fontSize = 22;
        economy.color = TextColor;
        Text status = FindText(header, "Deploy a fleet, then begin the first wave.");
        if (status != null)
        {
            Place(status.rectTransform, 28f, 58f, 1240f, 26f);
            status.fontSize = 18;
            status.color = DimColor;
        }

        // 顶部栏自带核心耐久条，左侧原“Core Health”面板退场，避免同一信息出现两次。
        Transform legacyCore = hud.transform.Find("Core Health");
        if (legacyCore != null)
        {
            legacyCore.gameObject.SetActive(false);
        }

        Text coreLabel = CloneLabel(
            header,
            template,
            "Core Integrity",
            "CORE  500 / 500",
            1420f,
            4f,
            472f,
            32f,
            20);
        coreLabel.alignment = TextAnchor.MiddleRight;
        RectTransform track = header.Find("Core Track") as RectTransform;
        if (track == null)
        {
            track = Panel(
                header,
                "Core Track",
                1450f,
                40f,
                442f,
                10f);
            track.GetComponent<Image>().color = new Color(0.05f, 0.11f, 0.14f, 1f);
            RectTransform created = Panel(
                track,
                "Core Fill",
                0f,
                0f,
                442f,
                10f);
            created.GetComponent<Image>().color = AccentColor;
        }

        Place(track, 1450f, 40f, 442f, 10f);
        RectTransform fill = track.Find("Core Fill") as RectTransform;
        fill.GetComponent<Image>().color = AccentColor;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("coreHealth").objectReferenceValue = coreLabel;
        serialized.FindProperty("coreFill").objectReferenceValue = fill.GetComponent<Image>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void FleetDock(SiegeHud hud)
    {
        RectTransform commands = Require(hud.transform, "Fleet Commands");
        Place(commands, 24f, 118f, 356f, 726f);
        commands.GetComponent<Image>().color = PanelColor;
        EnsureLine(
            commands,
            "Dock Accent",
            0f,
            0f,
            356f,
            3f,
            AccentColor);
        Text section = RequireText(commands, "DEPLOY FLEET");
        section.text = "FLEET DOCK";
        Place(section.rectTransform, 18f, 14f, 320f, 30f);
        section.fontSize = 20;
        section.color = AccentColor;
        PlaceOptional(
            commands,
            "Saves Blueprints",
            18f,
            50f,
            320f,
            46f);
        PlaceOptional(
            commands,
            "Deploy 0",
            18f,
            104f,
            320f,
            54f);
        PlaceOptional(
            commands,
            "Enemy Blueprints",
            18f,
            168f,
            320f,
            46f);
        PlaceOptional(
            commands,
            "Refresh Blueprints",
            18f,
            222f,
            320f,
            30f);
        Transform refresh = commands.Find("Refresh Blueprints");
        if (refresh != null)
        {
            StyleButton(refresh.GetComponent<Button>(), 14, false);
        }

        Text bays = RequireText(commands, "TAKE CONTROL");
        bays.text = "BAYS  /  CLICK TO PILOT";
        Place(bays.rectTransform, 18f, 266f, 320f, 26f);
        bays.fontSize = 16;
        bays.color = DimColor;
        for (int index = 0; index < 6; index++)
        {
            PlaceOptional(
                commands,
                "Pilot Bay " + (index + 1),
                18f,
                296f + index * 48f,
                320f,
                44f);
            Transform bay = commands.Find("Pilot Bay " + (index + 1));
            if (bay != null)
            {
                StyleButton(bay.GetComponent<Button>(), 17, false);
            }
        }

        PlaceOptional(
            commands,
            "Repair Core",
            18f,
            600f,
            320f,
            52f);
        PlaceOptional(
            commands,
            "Repair Ship",
            18f,
            660f,
            320f,
            52f);
        foreach (string name in new[]
        {
            "Deploy 0",
            "Repair Core",
            "Repair Ship"
        }

        )
        {
            Transform target = FindDeep(commands, name);
            if (target != null)
            {
                StyleButton(target.GetComponent<Button>(), 19, false);
            }
        }

        foreach (string name in new[]
        {
            "Saves Blueprints",
            "Enemy Blueprints"
        }

        )
        {
            Transform dropdown = FindDeep(commands, name);
            if (dropdown == null)
            {
                continue;
            }

            foreach (Text text in dropdown.GetComponentsInChildren<Text>(true))
            {
                text.fontSize = 17;
                text.color = TextColor;
            }

            foreach (Image image in dropdown.GetComponentsInChildren<Image>(true))
            {
                if (image.transform.name == "Arrow")
                {
                    // 默认按钮没有箭头贴图，实心方块反而像缺口；隐藏它，靠文字与悬停反馈表达可点。
                    image.gameObject.SetActive(false);
                }
            }
        }

        // 部署控件与修缮按钮各自成组：战斗阶段隐藏部署组，修缮保持原位置但由交互状态门控。
        Text hintTemplate = RequireText(commands, "TAKE CONTROL");
        RectTransform baysGroup = Group(
            commands,
            "Dock Bays Group",
            0f,
            266f,
            356f,
            340f);
        hintTemplate.transform.SetParent(baysGroup, false);
        Place(hintTemplate.rectTransform, 18f, 0f, 320f, 26f);
        for (int index = 0; index < 6; index++)
        {
            Transform bay = FindDeep(commands, "Pilot Bay " + (index + 1));
            if (bay != null)
            {
                bay.SetParent(baysGroup, false);
                Place((RectTransform)bay, 18f, 30f + index * 48f, 320f, 44f);
            }
        }

        RectTransform deployGroup = Group(
            commands,
            "Dock Deploy Group",
            0f,
            0f,
            356f,
            280f);
        foreach (string name in new[]
        {
            "Saves Blueprints",
            "Deploy 0",
            "Enemy Blueprints",
            "Refresh Blueprints"
        }

        )
        {
            Transform child = FindDeep(commands, name);
            if (child != null)
            {
                child.SetParent(deployGroup, false);
            }
        }

        RectTransform refitGroup = Group(
            commands,
            "Dock Refit Group",
            0f,
            0f,
            356f,
            726f);
        foreach (string name in new[]
        {
            "Repair Core",
            "Repair Ship"
        }

        )
        {
            Transform child = FindDeep(commands, name);
            if (child != null)
            {
                child.SetParent(refitGroup, false);
            }
        }
    }

    private static void ActionBar(SiegeHud hud)
    {
        RectTransform wave = Require(hud.transform, "Begin Wave");
        Place(wave, 660f, 964f, 420f, 64f);
        StyleButton(wave.GetComponent<Button>(), 24, true);
        RectTransform workshop = Require(hud.transform, "Open Blueprint Workshop");
        Place(workshop, 1100f, 964f, 340f, 64f);
        StyleButton(workshop.GetComponent<Button>(), 21, false);
        RectTransform restart = Require(hud.transform, "Restart");
        Place(restart, 1700f, 1032f, 196f, 32f);
        StyleButton(restart.GetComponent<Button>(), 15, false);
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("wave").objectReferenceValue = wave.GetComponent<Button>();
        serialized.FindProperty("restart").objectReferenceValue = restart.GetComponent<Button>();
        Transform repairCore = hud.transform.Find("Fleet Commands/Repair Core");
        Transform repairShip = hud.transform.Find("Fleet Commands/Repair Ship");
        if (repairCore != null)
        {
            serialized.FindProperty("repairCore").objectReferenceValue = repairCore.GetComponent<Button>();
        }

        if (repairShip != null)
        {
            serialized.FindProperty("repairShip").objectReferenceValue = repairShip.GetComponent<Button>();
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>战斗阶段专用：坐舰状态、底部波次进度、居中入场横幅和击杀提示；默认隐藏，由SiegeHud按阶段切换。</summary>
    private static void CombatHud(SiegeHud hud, Text template)
    {
        RectTransform ship = Panel(
            hud.transform,
            "Ship Status",
            1480f,
            112f,
            416f,
            152f);
        EnsureLine(
            ship,
            "Ship Accent",
            0f,
            0f,
            416f,
            3f,
            AccentColor);
        Text shipTitle = CloneLabel(
            ship,
            template,
            "Ship Title",
            "PILOT   ·   NO SHIP",
            18f,
            10f,
            380f,
            30f,
            17);
        shipTitle.color = AccentColor;
        Text integrityLabel = CloneLabel(
            ship,
            template,
            "Integrity Label",
            "INTEGRITY",
            18f,
            46f,
            180f,
            22f,
            13);
        integrityLabel.color = DimColor;
        Text integrityValue = CloneLabel(
            ship,
            template,
            "Integrity Value",
            "0 / 0",
            196f,
            46f,
            200f,
            22f,
            14);
        integrityValue.alignment = TextAnchor.MiddleRight;
        RectTransform integrityTrack = Panel(
            ship,
            "Integrity Track",
            18f,
            72f,
            380f,
            8f);
        integrityTrack.GetComponent<Image>().color = new Color(0.05f, 0.11f, 0.14f, 1f);
        RectTransform integrityFill = Panel(
            integrityTrack,
            "Integrity Fill",
            0f,
            0f,
            380f,
            8f);
        integrityFill.GetComponent<Image>().color = AccentColor;
        Text cockpitLabel = CloneLabel(
            ship,
            template,
            "Cockpit Label",
            "COCKPIT",
            18f,
            90f,
            180f,
            22f,
            13);
        cockpitLabel.color = DimColor;
        Text cockpitValue = CloneLabel(
            ship,
            template,
            "Cockpit Value",
            "0 / 0",
            196f,
            90f,
            200f,
            22f,
            14);
        cockpitValue.alignment = TextAnchor.MiddleRight;
        RectTransform cockpitTrack = Panel(
            ship,
            "Cockpit Track",
            18f,
            116f,
            380f,
            8f);
        cockpitTrack.GetComponent<Image>().color = new Color(0.05f, 0.11f, 0.14f, 1f);
        RectTransform cockpitFill = Panel(
            cockpitTrack,
            "Cockpit Fill",
            0f,
            0f,
            380f,
            8f);
        cockpitFill.GetComponent<Image>().color = AccentColor;
        ship.gameObject.SetActive(false);
        RectTransform vignette = Group(
            hud.transform,
            "Damage Vignette",
            0f,
            0f,
            ReferenceWidth,
            1080f);
        vignette.anchorMin = Vector2.zero;
        vignette.anchorMax = Vector2.one;
        vignette.offsetMin = Vector2.zero;
        vignette.offsetMax = Vector2.zero;
        var damageGroup = vignette.gameObject.AddComponent<CanvasGroup>();
        damageGroup.alpha = 0f;
        damageGroup.blocksRaycasts = false;
        damageGroup.interactable = false;
        // 只压四条边，保持画面中心可读；单一CanvasGroup透明度的成本与整屏色块相同。
        Edge(
            vignette,
            "Alarm Top",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, -96f),
            Vector2.zero);
        Edge(
            vignette,
            "Alarm Bottom",
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            Vector2.zero,
            new Vector2(0f, 96f));
        Edge(
            vignette,
            "Alarm Left",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(96f, 0f));
        Edge(
            vignette,
            "Alarm Right",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(-96f, 0f),
            Vector2.zero);
        vignette.SetAsFirstSibling();
        RectTransform status = Panel(
            hud.transform,
            "Wave Status",
            620f,
            964f,
            900f,
            64f);
        status.GetComponent<Image>().color = new Color(0.024f, 0.055f, 0.082f, 0.82f);
        EnsureLine(
            status,
            "Status Accent",
            0f,
            61f,
            900f,
            3f,
            AccentColor);
        Text label = CloneLabel(
            status,
            template,
            "Wave Status Label",
            "WAVE 1   ·   RAIDERS 2 / 2",
            20f,
            14f,
            560f,
            36f,
            22);
        label.color = TextColor;
        RectTransform track = Panel(
            status,
            "Wave Progress Track",
            600f,
            27f,
            280f,
            10f);
        track.GetComponent<Image>().color = new Color(0.05f, 0.11f, 0.14f, 1f);
        RectTransform fill = Panel(
            track,
            "Wave Progress Fill",
            0f,
            0f,
            280f,
            10f);
        fill.GetComponent<Image>().color = AccentColor;
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        status.gameObject.SetActive(false);
        RectTransform banner = Panel(
            hud.transform,
            "Wave Banner",
            560f,
            392f,
            800f,
            190f);
        banner.GetComponent<Image>().color = new Color(0.01f, 0.03f, 0.05f, 0.82f);
        var bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.blocksRaycasts = false;
        bannerGroup.interactable = false;
        Text title = CloneLabel(
            banner,
            template,
            "Banner Title",
            "WAVE 1",
            20f,
            34f,
            760f,
            84f,
            64);
        title.alignment = TextAnchor.MiddleCenter;
        title.color = AccentColor;
        Text subtitle = CloneLabel(
            banner,
            template,
            "Banner Subtitle",
            "2 RAIDERS INBOUND",
            20f,
            124f,
            760f,
            40f,
            22);
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.color = TextColor;
        banner.gameObject.SetActive(false);
        Text killFeed = CloneLabel(
            hud.transform,
            template,
            "Kill Feed",
            string.Empty,
            720f,
            118f,
            700f,
            36f,
            20);
        killFeed.alignment = TextAnchor.MiddleCenter;
        killFeed.color = AccentColor;
        var killGroup = killFeed.gameObject.AddComponent<CanvasGroup>();
        killGroup.alpha = 0f;
        killGroup.blocksRaycasts = false;
        killGroup.interactable = false;
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("deployGroup").objectReferenceValue = hud.transform.Find("Fleet Commands/Dock Deploy Group").gameObject;
        serialized.FindProperty("combatGroup").objectReferenceValue = status.gameObject;
        serialized.FindProperty("combatLabel").objectReferenceValue = label;
        serialized.FindProperty("combatFill").objectReferenceValue = fill.GetComponent<Image>();
        serialized.FindProperty("bannerPanel").objectReferenceValue = banner.gameObject;
        serialized.FindProperty("bannerTitle").objectReferenceValue = title;
        serialized.FindProperty("bannerSubtitle").objectReferenceValue = subtitle;
        serialized.FindProperty("bannerGroup").objectReferenceValue = bannerGroup;
        serialized.FindProperty("killFeed").objectReferenceValue = killFeed;
        serialized.FindProperty("killGroup").objectReferenceValue = killGroup;
        Transform shipStatus = FindByPath("Main Interface/MainCanvas/PlayPanel/HealthBar");
        if (shipStatus != null)
        {
            shipStatus.gameObject.SetActive(false);
            Transform exitButton = FindByPath("Main Interface/MainCanvas/PlayPanel/ExitPlayButton");
            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(false);
            }
        }

        serialized.FindProperty("shipStatus").objectReferenceValue = ship.gameObject;
        serialized.FindProperty("damageGroup").objectReferenceValue = damageGroup;
        serialized.FindProperty("shipTitle").objectReferenceValue = shipTitle;
        serialized.FindProperty("shipIntegrityFill").objectReferenceValue = integrityFill.GetComponent<Image>();
        serialized.FindProperty("shipIntegrityValue").objectReferenceValue = integrityValue;
        serialized.FindProperty("shipCockpitFill").objectReferenceValue = cockpitFill.GetComponent<Image>();
        serialized.FindProperty("shipCockpitValue").objectReferenceValue = cockpitValue;
        serialized.FindProperty("dockPanel").objectReferenceValue = hud.transform.Find("Fleet Commands").gameObject;
        serialized.FindProperty("dockTitle").objectReferenceValue = hud.transform.Find("Fleet Commands/DEPLOY FLEET").gameObject;
        serialized.FindProperty("baysGroup").objectReferenceValue = hud.transform.Find("Fleet Commands/Dock Bays Group").gameObject;
        serialized.FindProperty("refitGroup").objectReferenceValue = hud.transform.Find("Fleet Commands/Dock Refit Group").gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>创建一条贴边的告警色块；只用锚点+偏移定位，屏幕比例变化时仍贴合边缘。</summary>
    private static void Edge(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        Transform existing = FindChild(parent, name);
        RectTransform bar;
        if (existing == null)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            bar = root.GetComponent<RectTransform>();
            root.GetComponent<Image>().raycastTarget = false;
        }
        else
        {
            bar = (RectTransform)existing;
        }

        bar.anchorMin = anchorMin;
        bar.anchorMax = anchorMax;
        bar.offsetMin = offsetMin;
        bar.offsetMax = offsetMax;
        bar.GetComponent<Image>().color = new Color(0.62f, 0.05f, 0.04f, 1f);
    }

    private static RectTransform Group(
        Transform parent,
        string name,
        float x,
        float y,
        float width,
        float height)
    {
        Transform existing = FindDeep(parent, name);
        if (existing != null)
        {
            Place((RectTransform)existing, x, y, width, height);
            return (RectTransform)existing;
        }

        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        Place(root.GetComponent<RectTransform>(), x, y, width, height);
        return root.GetComponent<RectTransform>();
    }

    /// <summary>挂上有界威胁标记组件；标记池在运行时建立，场景里只保留可编辑参数。</summary>
    private static void ThreatMarkers(SiegeHud hud)
    {
        SiegeThreatMarkers markers = hud.GetComponent<SiegeThreatMarkers>();
        if (markers == null)
        {
            markers = hud.gameObject.AddComponent<SiegeThreatMarkers>();
        }

        var serialized = new SerializedObject(markers);
        serialized.FindProperty("session").objectReferenceValue = Object.FindFirstObjectByType<SiegeSession>();
        serialized.FindProperty("worldCamera").objectReferenceValue = Camera.main;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ControlsHelp(SiegeHud hud, Text template)
    {
        RectTransform panel = Panel(
            hud.transform,
            "Controls Help",
            660f,
            676f,
            780f,
            260f);
        panel.GetComponent<Image>().color = new Color(0.02f, 0.045f, 0.07f, 0.97f);
        EnsureLine(
            panel,
            "Help Accent",
            0f,
            0f,
            780f,
            3f,
            AccentColor);
        Text title = CloneLabel(
            panel,
            template,
            "Help Title",
            "CONTROLS",
            20f,
            12f,
            400f,
            28f,
            18);
        title.color = AccentColor;
        Text close = CloneLabel(
            panel,
            template,
            "Help Close",
            "F2  CLOSE",
            560f,
            12f,
            200f,
            28f,
            15);
        close.alignment = TextAnchor.MiddleRight;
        close.color = DimColor;
        Text legacy = FindLegacyHelp(hud);
        if (legacy == null)
        {
            legacy = CloneLabel(
                panel,
                template,
                "Controls Body",
                string.Empty,
                20f,
                54f,
                740f,
                190f,
                19);
        }
        else
        {
            legacy.transform.SetParent(panel, false);
            Place(legacy.rectTransform, 20f, 54f, 740f, 190f);
        }

        legacy.name = "Controls Body";
        legacy.text = "WASD  drive       Q / E  altitude       SPACE  hover\n" + "RIGHT MOUSE  orbit       SCROLL  zoom       F  focus camera       F1  thruster status\n" + "P  next wave       B  blueprint workshop between waves       F2  hide this panel";
        legacy.fontSize = 19;
        legacy.color = TextColor;
        legacy.alignment = TextAnchor.UpperLeft;
        legacy.lineSpacing = 1.3f;
        legacy.verticalOverflow = VerticalWrapMode.Overflow;
        panel.gameObject.SetActive(false);
        Text hint = CloneLabel(
            hud.transform,
            template,
            "Controls Hint",
            "F2  CONTROLS",
            1400f,
            1032f,
            180f,
            28f,
            15);
        hint.color = DimColor;
        hint.alignment = TextAnchor.MiddleRight;
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("helpPanel").objectReferenceValue = panel.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>克隆HUD内已有文本，继承真实字体、阴影和材质；直接新建的Text在保存场景后不出图。</summary>
    private static Text CloneLabel(
        Transform parent,
        Text template,
        string name,
        string text,
        float x,
        float y,
        float width,
        float height,
        int size)
    {
        Text label = Object.Instantiate(template, parent, false);
        label.name = name;
        label.text = text;
        label.fontSize = size;
        label.color = TextColor;
        label.alignment = TextAnchor.MiddleLeft;
        label.supportRichText = true;
        label.resizeTextForBestFit = false;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        // 截断模式会在行高略大于矩形时把整行丢弃；标签一律溢出绘制，避免文字静默消失。
        label.verticalOverflow = VerticalWrapMode.Overflow;
        Place(label.rectTransform, x, y, width, height);
        return label;
    }

    private static void StyleButton(Button button, int fontSize, bool primary)
    {
        if (button == null)
        {
            return;
        }

        button.GetComponent<Image>().color = primary ? PrimaryFill : ButtonFill;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = primary ? new Color(1.18f, 1.18f, 1.18f, 1f) : new Color(1.35f, 1.35f, 1.35f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.selectedColor = Color.white;
        colors.disabledColor = new Color(0.72f, 0.76f, 0.78f, 0.42f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.06f;
        button.colors = colors;
        foreach (Text text in button.GetComponentsInChildren<Text>(true))
        {
            text.fontSize = fontSize;
            text.color = primary ? new Color(0.92f, 1f, 1f, 1f) : TextColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = false;
            text.rectTransform.offsetMin = new Vector2(10f, 2f);
            text.rectTransform.offsetMax = new Vector2(-10f, -2f);
        }
    }

    private static void PlaceOptional(
        Transform parent,
        string name,
        float x,
        float y,
        float width,
        float height)
    {
        Transform target = FindDeep(parent, name);
        if (target != null)
        {
            Place((RectTransform)target, x, y, width, height);
        }
    }

    private static Text RequireText(Transform parent, string name)
    {
        Transform target = FindDeep(parent, name);
        Text text = target != null ? target.GetComponent<Text>() : null;
        if (text == null)
        {
            throw new InvalidOperationException("Missing HUD text '" + name + "' under " + parent.name + ".");
        }

        return text;
    }

    private static RectTransform Require(Transform parent, string path)
    {
        Transform target = path.Contains("/") ? FindUnder(parent, path) : FindDeep(parent, path);
        if (target == null)
        {
            throw new InvalidOperationException("Missing HUD object '" + path + "' under " + parent.name + ".");
        }

        return (RectTransform)target;
    }

    private static Text FindText(Transform parent, string name)
    {
        Transform target = FindDeep(parent, name);
        return target != null ? target.GetComponent<Text>() : null;
    }

    /// <summary>按完整子对象名查找；名称本身可能含“/”，不能用Transform.Find当作路径。</summary>
    private static Transform FindChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    /// <summary>跨层级查找：控件被移入分组后，重复执行的制作工具仍能找到并复用同一个对象。</summary>
    private static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child != parent && child.name == name)
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindUnder(Transform root, string path)
    {
        Transform current = root;
        foreach (string step in path.Split('/'))
        {
            current = current.Find(step);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static Transform FindByPath(string path)
    {
        string[] steps = path.Split('/');
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(item => item.name == steps[0]);
        Transform current = root != null ? root.transform : null;
        for (int index = 1; index < steps.Length && current != null; index++)
        {
            current = current.Find(steps[index]);
        }

        return current;
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
        root.GetComponent<Image>().color = PanelColor;
        return root.GetComponent<RectTransform>();
    }

    private static void EnsureLine(
        Transform parent,
        string name,
        float x,
        float y,
        float width,
        float height,
        Color color)
    {
        Transform existing = parent.Find(name);
        if (existing == null)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);
            existing = root.transform;
            root.GetComponent<Image>().raycastTarget = false;
        }

        Place((RectTransform)existing, x, y, width, height);
        existing.GetComponent<Image>().color = color;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}

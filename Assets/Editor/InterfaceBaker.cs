using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Styles Main and the dynamic save template without replacing gameplay bindings.</summary>
public static class InterfaceBaker
{
    private static readonly Color Surface = new Color(0.025f, 0.043f, 0.065f, 0.62f);
    private static readonly Color Raised = new Color(0.07f, 0.12f, 0.16f, 0.78f);
    private static readonly Color Accent = new Color(0.27f, 0.91f, 0.85f, 1f);
    private static readonly Color Ink = new Color(0.9f, 0.95f, 0.98f, 1f);
    private static readonly Color Muted = new Color(0.53f, 0.65f, 0.73f, 1f);
    private static Font _font;

    [MenuItem("Tools/HY-Sandbox/Polish Interface and Physics HUD")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before polishing the interface.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Main.unity") throw new System.InvalidOperationException("Open Main before polishing the interface.");
        _font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
        if (_font == null) throw new MissingReferenceException("Chakra Petch font is missing.");
        MainUIPanels panels = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainUIPanels>(true)).Single();
        Canvas canvas = panels.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true)).Single();
        Undo.RegisterFullObjectHierarchyUndo(canvas.gameObject, "Polish interface");
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Style(canvas.gameObject);
        // Preserve bar semantics and the existing 3D icon/thumbnail sprites.
        foreach (Image fill in canvas.GetComponentsInChildren<Image>(true).Where(i => i.name == "Fill"))
            fill.color = fill.transform.parent.name == "CockpitBar" ? (fill.transform.IsChildOf(panels.CombatHud.transform) ? new Color(1f, 0.34f, 0.34f) : Accent) : Color.white;
        foreach (string path in new[] { "BuildPanel/ButtonContent", "BuildPanel/BlueprintInfoPanel", "BuildPanel/SaveDisplayPanel", "BuildPanel/DebugSettingsPanel", "PlayPanel/HealthBar", "PlayPanel/ThrusterInfoPanel/Window", "CreateNewObjectPanel", "DeleteSavePanel", "DeathPanel" })
        {
            Transform node = canvas.transform.Find(path);
            if (node == null) continue;
            Image background = node.GetComponent<Image>();
            if (background != null) { background.sprite = null; background.color = Surface; background.raycastTarget = true; }
            if (path.EndsWith("Panel") && (path.StartsWith("Create") || path.StartsWith("Delete") || path.StartsWith("Death")))
            { if (background != null) background.color = new Color(0.015f, 0.025f, 0.04f, 0.55f); }
            else AddRule(node);
        }
        foreach (InputField field in canvas.GetComponentsInChildren<InputField>(true))
        {
            if (field.image != null) { field.image.color = Raised; field.image.raycastTarget = true; }
            if (field.textComponent != null) field.textComponent.color = Ink;
            if (field.placeholder is Text placeholder) placeholder.color = Muted;
        }
        // Navigation and scroll backgrounds must remain raycastable: construction ignores pointer input over UI.
        foreach (ScrollRect scroll in canvas.GetComponentsInChildren<ScrollRect>(true))
            if (scroll.viewport != null && scroll.viewport.TryGetComponent(out Image viewport)) { viewport.raycastTarget = true; viewport.color = Color.white; }
        foreach (Scrollbar scrollbar in canvas.GetComponentsInChildren<Scrollbar>(true))
            if (scrollbar.targetGraphic != null) { scrollbar.targetGraphic.raycastTarget = true; scrollbar.targetGraphic.color = Accent; }
        MainUIButtons buttons = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainUIButtons>(true)).Single();
        Primary(buttons.playButton, "SIMULATE  /  P");
        Primary(buttons.respawnButton, "RETURN TO WORKSHOP");
        Label(buttons.exitButton, "RETURN / SAVE CARGO");
        RectTransform exit = (RectTransform)buttons.exitButton.transform;
        exit.anchorMin = exit.anchorMax = Vector2.one; exit.pivot = Vector2.one;
        exit.anchoredPosition = new Vector2(-28f, -28f); exit.sizeDelta = new Vector2(250f, 44f);
        buttons.exitButton.GetComponentInChildren<Text>(true).fontSize = 14;
        Transform debugTitle = canvas.transform.Find("BuildPanel/DebugSettingsPanel/CurrentSaveName");
        if (debugTitle != null) debugTitle.GetComponent<Text>().text = "SYSTEM OVERLAYS";
        Transform save = canvas.transform.Find("BuildPanel/SaveDisplayPanel");
        if (save != null) foreach (Image image in save.GetComponentsInChildren<Image>(true).Where(i => i.GetComponent<Button>() == null && i.name != "ThemeRule"))
            image.color = image.GetComponent<Mask>() != null ? Color.white : Surface;
        foreach (Image panel in canvas.GetComponentsInChildren<Image>(true))
        {
            if (panel.GetComponent<Mask>() != null || panel.GetComponent<Selectable>() != null
                || panel.name == "Fill" || panel.name == "Preview" || panel.name == "ThemeRule"
                || panel.transform.parent?.name == "CategoryNavigation") continue;
            string name = panel.name.ToLowerInvariant();
            if (name.Contains("panel") || name.Contains("window") || name.Contains("template")
                || name.Contains("notice") || name == "healthbar" || name == "buttoncontent" || name == "scroll view")
            { Color color = panel.color; color.a = 0.62f; panel.color = color; }
        }
        BakeTelemetry(panels);
        const string prefabPath = "Assets/Resources/UI/SavePrefab.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try { Style(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void Style(GameObject root)
    {
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
        {
            text.font = _font; text.color = text.name == "Size" ? Muted : Ink;
            text.raycastTarget = false;
            Outline outline = text.GetComponent<Outline>(); if (outline != null) outline.enabled = false;
            Shadow shadow = text.GetComponents<Shadow>().FirstOrDefault(s => !(s is Outline));
            if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
            shadow.enabled = true; shadow.effectColor = new Color(0f, 0f, 0f, 0.45f); shadow.effectDistance = new Vector2(0f, -1f);
        }
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            bool icon = button.GetComponentInChildren<Text>(true) == null && button.GetComponent<BuildPaletteItem>() == null;
            if (button.image != null) { button.image.color = Color.white; button.image.raycastTarget = true; }
            ColorBlock colors = button.colors;
            colors.normalColor = icon ? Muted : Raised;
            colors.highlightedColor = icon ? Accent : new Color(0.14f, 0.34f, 0.37f);
            colors.pressedColor = Accent; colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.18f, 0.22f, 0.25f, 0.55f); colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (button.GetComponent<UIInteractionFeedback>() == null) button.gameObject.AddComponent<UIInteractionFeedback>();
        }
    }

    private static void Primary(Button button, string label)
    {
        if (button == null) return;
        ColorBlock colors = button.colors; colors.normalColor = Accent; colors.highlightedColor = new Color(0.55f, 1f, 0.92f); button.colors = colors;
        Label(button, label);
        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null) { text.color = Surface; text.fontSize = 18; text.resizeTextForBestFit = true; text.resizeTextMinSize = 11; text.resizeTextMaxSize = 18; }
    }

    private static void Label(Button button, string label)
    {
        if (button == null) return;
        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null) text.text = label;
    }

    private static RectTransform Child(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null) return (RectTransform)child;
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false); return (RectTransform)obj.transform;
    }

    private static void AddRule(Transform parent)
    {
        RectTransform rect = Child(parent, "ThemeRule");
        rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -2f); rect.offsetMax = Vector2.zero;
        Image image = rect.GetComponent<Image>() ?? rect.gameObject.AddComponent<Image>(); image.color = Accent; image.raycastTarget = false;
        LayoutElement layout = rect.GetComponent<LayoutElement>() ?? rect.gameObject.AddComponent<LayoutElement>(); layout.ignoreLayout = true;
    }

    private static void BakeTelemetry(MainUIPanels panels)
    {
        RectTransform root = Child(panels.playPanel.transform, "PhysicsTelemetry");
        root.anchorMin = root.anchorMax = new Vector2(1f, 0f); root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-28f, 28f); root.sizeDelta = new Vector2(292f, 228f);
        Image image = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>(); image.color = Surface; image.raycastTarget = false;
        AddRule(root);
        Text title = TextAt(root, "Title", "PHYSICS / LIVE", 18, 192, 274, 220, 14); title.color = Accent;
        Text motion = TextAt(root, "Motion", "SPEED      0.0 m/s\nMASS       0.0 kg\nKINETIC    0.0 kJ\nROTATION   0.0 deg/s", 18, 96, 274, 184, 13);
        Text impact = TextAt(root, "Impact", "LAST IMPACT   0.0 kJ\nIMPULSE       0.0 N s\nPEAK IMPACT   0.0 kJ", 18, 16, 274, 88, 12); impact.color = Muted;
        PhysicsTelemetryHud hud = panels.playPanel.GetComponent<PhysicsTelemetryHud>();
        if (hud == null) hud = panels.playPanel.AddComponent<PhysicsTelemetryHud>();
        SerializedObject serialized = new SerializedObject(hud);
        serialized.FindProperty("_motion").objectReferenceValue = motion;
        serialized.FindProperty("_impact").objectReferenceValue = impact;
        serialized.FindProperty("_impactPulse").objectReferenceValue = root.Find("ThemeRule").GetComponent<Image>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text TextAt(Transform parent, string name, string value, float x0, float y0, float x1, float y1, int size)
    {
        RectTransform rect = Child(parent, name); rect.anchorMin = rect.anchorMax = Vector2.zero;
        rect.offsetMin = new Vector2(x0, y0); rect.offsetMax = new Vector2(x1, y1);
        Text text = rect.GetComponent<Text>() ?? rect.gameObject.AddComponent<Text>();
        text.font = _font; text.text = value; text.fontSize = size; text.color = Ink; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
        return text;
    }
}

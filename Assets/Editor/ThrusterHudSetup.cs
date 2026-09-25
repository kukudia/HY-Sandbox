using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ThrusterHudSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string HoverPrefabPath = "Assets/Resources/Blocks/HoverFlightController.prefab";
    private const string LampMaterialPath = "Assets/Art/HoverControllerLamp.mat";
    private static readonly Color Ink = new Color(0.025f, 0.055f, 0.07f, 0.97f);
    private static readonly Color Cyan = new Color(0.31f, 0.88f, 0.95f);

    [MenuItem("Tools/HY-Sandbox/Setup Thruster HUD")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing Main.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        MainUIPanels panels = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MainUIPanels>(true)).Single();
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
        if (font == null) throw new MissingReferenceException("Chakra Petch font is missing.");

        RectTransform health = panels.healthValue.transform.parent.GetComponent<RectTransform>();
        health.sizeDelta = new Vector2(320f, 186f);
        Text healthTitle = health.Find("CockpitLabel").GetComponent<Text>();
        healthTitle.text = "UNIT INTEGRITY";
        Text telemetry = Label(health, "FlightTelemetry", font, 13, Color.white, TextAnchor.UpperLeft);
        telemetry.rectTransform.anchorMin = new Vector2(0f, 0f);
        telemetry.rectTransform.anchorMax = new Vector2(1f, 1f);
        telemetry.rectTransform.offsetMin = new Vector2(10f, 38f);
        telemetry.rectTransform.offsetMax = new Vector2(-10f, -31f);
        telemetry.lineSpacing = 1.08f;
        telemetry.text = "HOVER CONTROL   OFFLINE";
        Set(panels, "_flightTelemetry", telemetry);

        RectTransform root = Child(panels.playPanel.transform, "ThrusterInfoPanel");
        root.anchorMin = root.anchorMax = new Vector2(1f, 1f);
        root.pivot = Vector2.one;
        root.anchoredPosition = new Vector2(-28f, -28f);
        root.sizeDelta = new Vector2(480f, 540f);
        ThrusterInfoPanel panel = root.GetComponent<ThrusterInfoPanel>();
        if (panel == null) panel = root.gameObject.AddComponent<ThrusterInfoPanel>();
        root.SetAsLastSibling();
        RectTransform window = Child(root, "Window");
        Stretch(window);
        Paint(window, Ink, true);
        Text title = Label(window, "Title", font, 22, Color.white, TextAnchor.MiddleLeft);
        Box(title.rectTransform, new Vector2(18f, -16f), new Vector2(456f, -56f));
        title.text = "THRUSTER STATUS";
        Text subtitle = Label(window, "Subtitle", font, 12, Cyan, TextAnchor.MiddleRight);
        Box(subtitle.rectTransform, new Vector2(338f, -17f), new Vector2(462f, -55f));
        subtitle.text = "F1  CLOSE";

        RectTransform tabs = Child(window, "Tabs");
        tabs.anchorMin = new Vector2(0f, 1f); tabs.anchorMax = new Vector2(1f, 1f);
        tabs.offsetMin = new Vector2(16f, -113f); tabs.offsetMax = new Vector2(-16f, -67f);
        string[] categories = { "MAIN THRUSTER", "UNIVERSAL", "HOVER" };
        Button[] buttons = new Button[3];
        Text[] labels = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            RectTransform tab = Child(tabs, "Tab" + i);
            tab.anchorMin = new Vector2(i / 3f, 0f);
            tab.anchorMax = new Vector2((i + 1f) / 3f, 1f);
            tab.offsetMin = Vector2.zero; tab.offsetMax = Vector2.zero;
            Image image = Paint(tab, new Color(0.045f, 0.08f, 0.1f, 1f), true);
            Button button = tab.GetComponent<Button>();
            if (button == null) button = tab.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            buttons[i] = button;
            Text label = Label(tab, "Label", font, 13, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            label.text = categories[i];
            labels[i] = label;
        }
        RectTransform indicator = Child(tabs, "SelectionLine");
        Paint(indicator, Cyan, false);
        indicator.SetAsLastSibling();

        RectTransform viewport = Child(window, "Viewport");
        viewport.anchorMin = new Vector2(0f, 0f); viewport.anchorMax = new Vector2(1f, 1f);
        viewport.offsetMin = new Vector2(16f, 18f); viewport.offsetMax = new Vector2(-16f, -128f);
        Paint(viewport, new Color(0.035f, 0.075f, 0.09f, 1f), true);
        Mask mask = viewport.GetComponent<Mask>();
        if (mask == null) mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = true;
        ScrollRect scroll = viewport.GetComponent<ScrollRect>();
        if (scroll == null) scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
        scroll.viewport = viewport;
        RectTransform content = Child(viewport, "Content");
        content.anchorMin = new Vector2(0f, 1f); content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        scroll.content = content;
        Text empty = Label(viewport, "EmptyState", font, 15, new Color(0.6f, 0.76f, 0.78f), TextAnchor.MiddleCenter);
        Stretch(empty.rectTransform);
        empty.text = "NO CONTROLLED THRUSTERS";
        empty.raycastTarget = false;

        RectTransform row = Child(content, "RowTemplate");
        row.anchorMin = new Vector2(0f, 1f); row.anchorMax = Vector2.one;
        row.pivot = new Vector2(0.5f, 1f);
        row.sizeDelta = new Vector2(0f, 58f);
        row.anchoredPosition = Vector2.zero;
        Paint(row, new Color(0.06f, 0.11f, 0.13f, 1f), false);
        Text rowName = Label(row, "Name", font, 15, Color.white, TextAnchor.MiddleLeft);
        Box(rowName.rectTransform, new Vector2(14f, -6f), new Vector2(285f, -29f));
        Text rowValue = Label(row, "Value", font, 13, Color.white, TextAnchor.MiddleRight);
        Box(rowValue.rectTransform, new Vector2(280f, -6f), new Vector2(430f, -29f));
        RectTransform track = Child(row, "Track");
        Box(track, new Vector2(14f, -39f), new Vector2(430f, -46f));
        Paint(track, new Color(0.2f, 0.28f, 0.3f), false);
        RectTransform fill = Child(track, "Fill");
        Stretch(fill);
        Image fillImage = Paint(fill, Cyan, false);
        fillImage.type = Image.Type.Simple;
        row.gameObject.SetActive(false);

        Set(panel, "_window", window.gameObject);
        SetArray(panel, "_tabs", buttons);
        SetArray(panel, "_tabLabels", labels);
        Set(panel, "_content", content);
        Set(panel, "_rowTemplate", row);
        Set(panel, "_emptyState", empty);
        Set(panel, "_tabIndicator", indicator.GetComponent<Image>());
        Set(panels, "_thrusterInfoPanel", panel);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        SetupLamp();
        Debug.Log("Thruster HUD and hover controller status lamp saved.");
    }

    private static void SetupLamp()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(LampMaterialPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new MissingReferenceException("URP Lit shader is missing.");
            material = new Material(shader) { name = "Hover Controller Lamp" };
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.white);
            AssetDatabase.CreateAsset(material, LampMaterialPath);
        }
        GameObject prefab = PrefabUtility.LoadPrefabContents(HoverPrefabPath);
        try
        {
            HoverFlightController controller = prefab.GetComponent<HoverFlightController>();
            Transform model = prefab.transform.Find("Model");
            Transform lamp = model.Find("ControllerStatusLamp");
            if (lamp == null)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "ControllerStatusLamp";
                sphere.transform.SetParent(model, false);
                sphere.transform.localPosition = new Vector3(0f, 0.35f, 0.32f);
                sphere.transform.localScale = Vector3.one * 0.13f;
                UnityEngine.Object.DestroyImmediate(sphere.GetComponent<Collider>());
                lamp = sphere.transform;
            }
            MeshRenderer renderer = lamp.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            Light light = lamp.GetComponent<Light>();
            if (light == null) light = lamp.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 1.8f;
            light.shadows = LightShadows.None;
            HoverControllerStatusLight status = prefab.GetComponent<HoverControllerStatusLight>();
            if (status == null) status = prefab.AddComponent<HoverControllerStatusLight>();
            Set(status, "_controller", controller);
            Set(status, "_lens", renderer);
            Set(status, "_glow", light);
            PrefabUtility.SaveAsPrefabAsset(prefab, HoverPrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
    }

    private static RectTransform Child(Transform parent, string name)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect != null) return rect;
        rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
    }

    private static void Box(RectTransform rect, Vector2 topLeft, Vector2 bottomRight)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeft;
        rect.sizeDelta = new Vector2(bottomRight.x - topLeft.x, topLeft.y - bottomRight.y);
    }

    private static Image Paint(RectTransform rect, Color color, bool raycast)
    {
        Image image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static Text Label(Transform parent, string name, Font font, int size, Color color, TextAnchor alignment)
    {
        RectTransform rect = Child(parent, name);
        Text text = rect.GetComponent<Text>();
        if (text == null) text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
    {
        SerializedObject serialized = new SerializedObject(target);
        serialized.FindProperty(name).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray<T>(UnityEngine.Object target, string name, T[] values) where T : UnityEngine.Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(name);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

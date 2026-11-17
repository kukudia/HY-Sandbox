// 职责：配置主场景战斗 HUD 与界面反馈组件。
// 入口：UpgradeDualHealthHud() / Apply()。
// 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CombatHudSetup
{
    private const string ScenePath = "Assets/Scenes/Build.unity";
    private static readonly Color Ink = new Color(0.025f, 0.055f, 0.07f, 0.88f);
    private static readonly Color Coral = new Color(1f, 0.32f, 0.28f);
    private static readonly Color Cyan = new Color(0.31f, 0.88f, 0.95f);
    [MenuItem("Tools/HY-Sandbox/Upgrade Dual Health HUD")]
    public static void UpgradeDualHealthHud()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before editing Main.");
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        if (scene.isDirty)
        {
            throw new InvalidOperationException("Save the Main scene before upgrading the health HUD.");
        }

        MainUIPanels panels = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainUIPanels>(true)).Single();
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
        if (font == null)
        {
            throw new MissingReferenceException("Chakra Petch font is missing.");
        }

        RectTransform player = panels.healthValue.transform.parent as RectTransform;
        player.sizeDelta = new Vector2(320f, 226f);
        RectTransform playerBar = player.Find("HealthBar") as RectTransform;
        BottomBox(playerBar, 10f, 49f, 310f, 57f);
        Image playerFill = playerBar.Find("Fill").GetComponent<Image>();
        playerFill.color = Color.white;
        Text unitLabel = Text(
            player,
            "UnitLabel",
            font,
            11,
            Color.white,
            TextAnchor.MiddleLeft);
        unitLabel.text = "UNIT";
        BottomBox(unitLabel.rectTransform, 10f, 59f, 125f, 76f);
        Text unitValue = panels.healthValue;
        BottomBox(unitValue.rectTransform, 130f, 59f, 310f, 76f);
        unitValue.alignment = TextAnchor.MiddleRight;
        RectTransform cockpitBar = Child(player, "CockpitBar");
        BottomBox(cockpitBar, 10f, 12f, 310f, 20f);
        Image(cockpitBar, new Color(0.15f, 0.23f, 0.24f, 1f));
        RectTransform cockpitFill = Child(cockpitBar, "Fill");
        Stretch(cockpitFill);
        Image cockpitFillImage = Image(cockpitFill, new Color(0.25f, 0.9f, 0.48f));
        cockpitFillImage.type = UnityEngine.UI.Image.Type.Filled;
        cockpitFillImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        cockpitFillImage.fillOrigin = 0;
        Text cockpitLabel = Text(
            player,
            "CockpitBarLabel",
            font,
            11,
            new Color(0.5f, 1f, 0.68f),
            TextAnchor.MiddleLeft);
        cockpitLabel.text = "COCKPIT";
        BottomBox(cockpitLabel.rectTransform, 10f, 22f, 125f, 40f);
        Text cockpitValue = Text(
            player,
            "CockpitBarValue",
            font,
            11,
            Color.white,
            TextAnchor.MiddleRight);
        BottomBox(cockpitValue.rectTransform, 130f, 22f, 310f, 40f);
        Text telemetry = player.Find("FlightTelemetry")?.GetComponent<Text>();
        if (telemetry != null)
        {
            telemetry.rectTransform.offsetMin = new Vector2(10f, 80f);
            telemetry.rectTransform.offsetMax = new Vector2(-10f, -31f);
        }

        Set(panels, "_cockpitHealthFill", cockpitFillImage);
        Set(panels, "_cockpitHealthValue", cockpitValue);
        SharedCombatHudAuthoring.InstallScene(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/HY-Sandbox/Setup Combat HUD")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before editing Main.");
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        }

        if (scene.isDirty)
        {
            Debug.Log("Completing the loaded Main scene's combat HUD setup.");
        }

        SharedCombatHudAuthoring.InstallScene(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static RectTransform Child(Transform parent, string name)
    {
        RectTransform existing = parent.Find(name) as RectTransform;
        if (existing != null)
        {
            return existing;
        }

        RectTransform child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        child.SetParent(parent, false);
        return child;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Position(RectTransform rect, float left, float bottom, float right, float top)
    {
        Stretch(rect);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(right, top);
    }

    private static void BottomBox(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(left, bottom);
        rect.sizeDelta = new Vector2(right - left, top - bottom);
    }

    private static Image Image(RectTransform rect, Color color)
    {
        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Text(
        Transform parent,
        string name,
        Font font,
        int size,
        Color color,
        TextAnchor alignment)
    {
        RectTransform rect = Child(parent, name);
        Text text = rect.GetComponent<Text>();
        if (text == null)
        {
            text = rect.gameObject.AddComponent<Text>();
        }

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
}

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CombatHudSetup
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private static readonly Color Ink = new Color(0.025f, 0.055f, 0.07f, 0.88f);
    private static readonly Color Coral = new Color(1f, 0.32f, 0.28f);
    private static readonly Color Cyan = new Color(0.31f, 0.88f, 0.95f);

    [MenuItem("Tools/HY-Sandbox/Setup Combat HUD")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before editing Main.");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        if (scene.isDirty) Debug.Log("Completing the loaded Main scene's combat HUD setup.");

        MainUIPanels panels = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<MainUIPanels>(true)).Single();
        RectTransform play = panels.playPanel.GetComponent<RectTransform>();
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
        if (font == null) throw new MissingReferenceException("Chakra Petch font is missing.");

        RectTransform root = Child(play, "CombatHud");
        Stretch(root);
        root.SetAsLastSibling();
        CombatHud hud = root.GetComponent<CombatHud>();
        if (hud == null) hud = root.gameObject.AddComponent<CombatHud>();
        RectTransform template = Child(root, "EnemyNameplateTemplate");
        template.anchorMin = template.anchorMax = new Vector2(0.5f, 0.5f);
        template.pivot = new Vector2(0.5f, 0f);
        template.sizeDelta = new Vector2(194f, 48f);
        Image(template, Ink);
        Text name = Text(template, "Name", font, 13, Color.white, TextAnchor.MiddleLeft);
        Position(name.rectTransform, 14f, 24f, -75f, -4f);
        Text value = Text(template, "Value", font, 10, new Color(0.8f, 0.92f, 0.94f), TextAnchor.MiddleRight);
        Position(value.rectTransform, 120f, 24f, -10f, -4f);
        RectTransform bar = Child(template, "Bar");
        bar.anchorMin = new Vector2(0f, 0f); bar.anchorMax = new Vector2(1f, 0f);
        bar.offsetMin = new Vector2(14f, 9f); bar.offsetMax = new Vector2(-14f, 14f);
        Image(bar, new Color(0.24f, 0.3f, 0.33f));
        RectTransform fill = Child(bar, "Fill");
        Stretch(fill);
        fill.pivot = Vector2.zero;
        Image(fill, Coral);
        template.gameObject.SetActive(false);

        RectTransform notice = Child(root, "KillNotice");
        notice.anchorMin = notice.anchorMax = new Vector2(0.5f, 0.36f);
        notice.sizeDelta = new Vector2(460f, 96f);
        Image(notice, new Color(0.025f, 0.055f, 0.07f, 0.97f));
        notice.SetAsLastSibling();
        CanvasGroup group = notice.GetComponent<CanvasGroup>();
        if (group == null) group = notice.gameObject.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;
        RectTransform stripe = Child(notice, "Accent");
        stripe.anchorMin = new Vector2(0f, 0f); stripe.anchorMax = new Vector2(0f, 1f);
        stripe.offsetMin = Vector2.zero; stripe.offsetMax = new Vector2(6f, 0f);
        Image(stripe, Coral);
        Text title = Text(notice, "Title", font, 14, Cyan, TextAnchor.MiddleLeft);
        title.text = "TARGET DESTROYED";
        Position(title.rectTransform, 22f, 58f, -18f, -12f);
        Text killName = Text(notice, "EnemyName", font, 26, Color.white, TextAnchor.MiddleLeft);
        Position(killName.rectTransform, 22f, 14f, -130f, -39f);
        killName.resizeTextForBestFit = true;
        killName.resizeTextMinSize = 16;
        killName.resizeTextMaxSize = 26;
        Text count = Text(notice, "Count", font, 17, Cyan, TextAnchor.MiddleRight);
        Position(count.rectTransform, 330f, 14f, -18f, -39f);
        notice.gameObject.SetActive(false);

        Set(hud, "_overlay", root);
        Set(hud, "_enemyTemplate", template);
        Set(hud, "_killGroup", group);
        Set(hud, "_killName", killName);
        Set(hud, "_killCount", count);
        Set(panels, "_combatHud", hud);
        foreach (Button button in panels.GetComponentsInChildren<Button>(true))
            if (button.GetComponent<UIInteractionFeedback>() == null)
                button.gameObject.AddComponent<UIInteractionFeedback>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Combat HUD saved to Main/PlayPanel.");
    }

    private static RectTransform Child(Transform parent, string name)
    {
        RectTransform existing = parent.Find(name) as RectTransform;
        if (existing != null) return existing;
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

    private static Image Image(RectTransform rect, Color color)
    {
        Image image = rect.GetComponent<Image>();
        if (image == null) image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text Text(Transform parent, string name, Font font, int size, Color color, TextAnchor alignment)
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
}

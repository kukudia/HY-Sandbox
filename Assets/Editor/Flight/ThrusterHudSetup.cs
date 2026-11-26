// 职责：配置推进器参数 HUD 与交互引用。
// 入口：Apply()。
// 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ThrusterHudSetup
{
    private const string ScenePath = "Assets/Scenes/Build.unity";
    private const string HoverPrefabPath = "Assets/Resources/Blocks/HoverFlightController.prefab";
    private const string LampMaterialPath = "Assets/Art/HoverControllerLamp.mat";
    private static readonly Color Ink = new Color(0.025f, 0.055f, 0.07f, 0.97f);
    private static readonly Color Cyan = new Color(0.31f, 0.88f, 0.95f);
    [MenuItem("Tools/HY-Sandbox/Setup Thruster HUD")]
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

        MainUIPanels panels = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainUIPanels>(true)).Single();
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ChakraPetch-Medium.ttf");
        if (font == null)
        {
            throw new MissingReferenceException("Chakra Petch font is missing.");
        }

        RectTransform health = panels.healthValue.transform.parent.GetComponent<RectTransform>();
        health.sizeDelta = new Vector2(320f, 186f);
        Text healthTitle = health.Find("CockpitLabel").GetComponent<Text>();
        healthTitle.text = "UNIT INTEGRITY";
        Text telemetry = Label(
            health,
            "FlightTelemetry",
            font,
            13,
            Color.white,
            TextAnchor.UpperLeft);
        telemetry.rectTransform.anchorMin = new Vector2(0f, 0f);
        telemetry.rectTransform.anchorMax = new Vector2(1f, 1f);
        telemetry.rectTransform.offsetMin = new Vector2(10f, 38f);
        telemetry.rectTransform.offsetMax = new Vector2(-10f, -31f);
        telemetry.lineSpacing = 1.08f;
        telemetry.text = "HOVER CONTROL   OFFLINE";
        Set(panels, "_flightTelemetry", telemetry);
        SharedCombatHudAuthoring.InstallScene(scene);
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
            if (shader == null)
            {
                throw new MissingReferenceException("URP Lit shader is missing.");
            }

            material = new Material(shader)
            {
                name = "Hover Controller Lamp"
            };
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
            if (light == null)
            {
                light = lamp.gameObject.AddComponent<Light>();
            }

            light.type = LightType.Point;
            light.range = 1.8f;
            light.shadows = LightShadows.None;
            HoverControllerStatusLight status = prefab.GetComponent<HoverControllerStatusLight>();
            if (status == null)
            {
                status = prefab.AddComponent<HoverControllerStatusLight>();
            }

            Set(status, "_controller", controller);
            Set(status, "_lens", renderer);
            Set(status, "_glow", light);
            PrefabUtility.SaveAsPrefabAsset(prefab, HoverPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    private static RectTransform Child(Transform parent, string name)
    {
        RectTransform rect = parent.Find(name) as RectTransform;
        if (rect != null)
        {
            return rect;
        }

        rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static Text Label(
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

    private static void SetArray<T>(UnityEngine.Object target, string name, T[] values)
        where T : UnityEngine.Object
    {
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty property = serialized.FindProperty(name);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}

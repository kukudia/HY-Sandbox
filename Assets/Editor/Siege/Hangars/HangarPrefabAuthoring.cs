// 职责：制作可编辑机库 Prefab、材质和数量配置；尺寸控件、门和碰撞体均保存为真实子节点。
// 入口：Generate()；只重建本工具拥有的 Hangars 资产，不修改场景或玩家蓝图。
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class HangarPrefabAuthoring
{
    public const string PrefabPath = "Assets/Resources/Hangars/Hangar.prefab";
    private const string Folder = "Assets/Resources/Hangars";
    [MenuItem("HY-Sandbox/Siege/Hangars/Generate Editable Hangar")]
    public static GameObject Generate()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before making the hangar asset.");
        }

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Hangars");
        }

        Material floor = Material("Floor", new Color(0.035f, 0.055f, 0.07f, 1f));
        Material roof = Material("Roof", new Color(0.17f, 0.34f, 0.42f, 0.045f), true);
        roof.SetShaderPassEnabled("ShadowCaster", false);
        Material shell = Material("Shell", new Color(0.17f, 0.24f, 0.29f, 1f));
        Material door = Material("Door", new Color(0.14f, 0.42f, 0.47f, 1f));
        Material volume = Material("Capacity", new Color(0.12f, 0.75f, 0.95f, 0.13f), true);
        var root = new GameObject("Hangar");
        try
        {
            root.layer = LayerMask.NameToLayer("Block");
            Block block = root.AddComponent<Block>();
            block.x = 20;
            block.y = 9;
            block.z = 24;
            block.canSpawnConnector = false;
            block.canEditInfo = true;
            block.info = "HANGAR\nOne assigned ship per hangar.\nSelect to resize its interior; rotate around the vertical axis.\nThe front doors face +Z.";
            block.resourcePath = PrefabPath;
            block.showConnectors = false;
            block.showLabel = false;
            block.raycastCollider = root.GetComponent<BoxCollider>();
            block.SetMassSettings(true, 800f);
            Durability health = root.GetComponent<Durability>();
            health.maxDurability = 1200f;
            health.currentDurability = 1200f;
            health.debugLog = false;
            HangarModule module = root.AddComponent<HangarModule>();
            var data = new SerializedObject(module);
            data.FindProperty("floor").objectReferenceValue = Cube(root.transform, "Dark Floor", floor, true);
            data.FindProperty("roof").objectReferenceValue = Cube(root.transform, "Roof", roof, true);
            data.FindProperty("leftWall").objectReferenceValue = Cube(root.transform, "Left Wall", shell, true);
            data.FindProperty("rightWall").objectReferenceValue = Cube(root.transform, "Right Wall", shell, true);
            data.FindProperty("backWall").objectReferenceValue = Cube(root.transform, "Back Wall", shell, true);
            data.FindProperty("leftDoor").objectReferenceValue = Cube(root.transform, "Sliding Door Left", door, true);
            data.FindProperty("rightDoor").objectReferenceValue = Cube(root.transform, "Sliding Door Right", door, true);
            data.FindProperty("capacity").objectReferenceValue = Cube(root.transform, "Capacity Volume (Editor)", volume, false);
            Canvas label = Canvas(root.transform, "Capacity Label", new Vector2(400f, 120f));
            label.transform.localScale = Vector3.one * 0.024f;
            Text title = Text(
                label.transform,
                "Dimensions",
                "HANGAR",
                30,
                new Vector2(400f, 120f),
                Vector2.zero);
            title.raycastTarget = false;
            data.FindProperty("sizeLabel").objectReferenceValue = label.transform;
            data.FindProperty("sizeText").objectReferenceValue = title;
            Vector3[] normals =
            {
                Vector3.forward,
                Vector3.back,
                Vector3.left,
                Vector3.right
            };
            SerializedProperty panels = data.FindProperty("faces");
            panels.arraySize = normals.Length;
            for (int i = 0; i < normals.Length; i++)
            {
                panels.GetArrayElementAtIndex(i).objectReferenceValue = Face(root.transform, module, normals[i], i);
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            block.RefreshAuthoringData();
            block.resourcePath = PrefabPath;
            module.SetCombat(false);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            EnsureLimits();
            AssetDatabase.SaveAssets();
            return prefab;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    [MenuItem("HY-Sandbox/Siege/Hangars/Update Transparent Roof")]
    public static void UpdateTransparentRoof()
    {
        Material material = Material("Roof", new Color(0.17f, 0.34f, 0.42f, 0.045f), true);
        material.SetShaderPassEnabled("ShadowCaster", false);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Renderer renderer = root.transform.Find("Roof").GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void EnsureLimits()
    {
        string path = Folder + "/Limits.asset";
        if (AssetDatabase.LoadAssetAtPath<HangarLimits>(path) == null)
        {
            HangarLimits limits = ScriptableObject.CreateInstance<HangarLimits>();
            var settings = new SerializedObject(limits);
            settings.FindProperty("maximumCount").intValue = 6;
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(limits, path);
        }
    }

    private static Transform Cube(Transform parent, string name, Material material, bool collider)
    {
        GameObject child = GameObject.CreatePrimitive(PrimitiveType.Cube);
        child.name = name;
        child.layer = parent.gameObject.layer;
        child.transform.SetParent(parent, false);
        child.GetComponent<Renderer>().sharedMaterial = material;
        if (!collider)
        {
            UnityEngine.Object.DestroyImmediate(child.GetComponent<Collider>());
        }

        return child.transform;
    }

    private static Canvas Canvas(Transform parent, string name, Vector2 size)
    {
        var owner = new GameObject(name, typeof(RectTransform), typeof(Canvas));
        owner.layer = 5;
        owner.transform.SetParent(parent, false);
        owner.GetComponent<RectTransform>().sizeDelta = size;
        Canvas canvas = owner.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;
        return canvas;
    }

    private static HangarFacePanel Face(Transform parent, HangarModule module, Vector3 normal, int index)
    {
        Canvas canvas = Canvas(parent, "Resize Face " + index, new Vector2(280f, 160f));
        canvas.gameObject.AddComponent<GraphicRaycaster>();
        CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
        HangarFacePanel face = canvas.gameObject.AddComponent<HangarFacePanel>();
        var settings = new SerializedObject(face);
        settings.FindProperty("hangar").objectReferenceValue = module;
        settings.FindProperty("outwardNormal").vector3Value = normal;
        settings.ApplyModifiedPropertiesWithoutUndo();
        var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(canvas.transform, false);
        background.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 160f);
        background.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.06f, 0.93f);
        background.GetComponent<Image>().raycastTarget = false;
        string[] labels =
        {
            "WIDTH",
            "HEIGHT",
            "DEPTH"
        };
        for (int axis = 0; axis < 3; axis++)
        {
            float y = 50f - axis * 50f;
            Text(
                canvas.transform,
                labels[axis],
                labels[axis],
                20,
                new Vector2(130f, 40f),
                new Vector2(-65f, y));
            ResizeButton(canvas.transform, module, axis, -1, new Vector2(35f, y));
            ResizeButton(canvas.transform, module, axis, 1, new Vector2(100f, y));
        }

        return face;
    }

    private static void ResizeButton(Transform parent, HangarModule module, int axis, int delta, Vector2 position)
    {
        var owner = new GameObject(
            (delta > 0 ? "Increase " : "Decrease ") + axis,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(HangarResizeButton));
        owner.layer = 5;
        owner.transform.SetParent(parent, false);
        var rect = owner.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(54f, 40f);
        rect.anchoredPosition = position;
        owner.GetComponent<Image>().color = new Color(0.13f, 0.4f, 0.48f, 1f);
        Button button = owner.GetComponent<Button>();
        button.targetGraphic = owner.GetComponent<Image>();
        Text(
            owner.transform,
            "Label",
            delta > 0 ? "+" : "−",
            30,
            new Vector2(54f, 40f),
            Vector2.zero);
        var command = new SerializedObject(owner.GetComponent<HangarResizeButton>());
        command.FindProperty("hangar").objectReferenceValue = module;
        command.FindProperty("axis").intValue = axis;
        command.FindProperty("delta").intValue = delta;
        command.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text Text(
        Transform parent,
        string name,
        string value,
        int size,
        Vector2 dimensions,
        Vector2 position)
    {
        var owner = new GameObject(name, typeof(RectTransform), typeof(Text));
        owner.layer = 5;
        owner.transform.SetParent(parent, false);
        var rect = owner.GetComponent<RectTransform>();
        rect.sizeDelta = dimensions;
        rect.anchoredPosition = position;
        Text text = owner.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Material Material(string name, Color color, bool transparent = false)
    {
        string path = Folder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", 0.25f);
        if (transparent)
        {
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        EditorUtility.SetDirty(material);
        return material;
    }
}

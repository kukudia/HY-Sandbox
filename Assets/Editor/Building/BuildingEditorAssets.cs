// 职责：保存建筑底座、网格材质及尺寸编辑 uGUI 为真实可编辑资产，不写入 Main 场景。
// 入口：Bake() 菜单；已存在的 Prefab/材质优先保留，删除指定资产后可重新生成。
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildingEditorAssets
{
    private const string Folder = "Assets/Resources/BuildingEditor";
    [MenuItem("Tools/Building Editor/Create missing editable assets")]
    public static void Bake()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before creating assets.");
        }

        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();
        Material material = CreateGridMaterial();
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Foundation.prefab") == null)
        {
            CreateFoundation(material);
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/EditorPanel.prefab") == null)
        {
            CreatePanel();
        }

        AssetDatabase.SaveAssets();
    }

    private static Material CreateGridMaterial()
    {
        string path = Folder + "/FoundationGrid.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        string texturePath = Folder + "/FoundationGrid.png";
        if (!File.Exists(texturePath))
        {
            var texture = new Texture2D(64, 64, TextureFormat.RGB24, false);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    bool line = x < 2 || y < 2;
                    texture.SetPixel(x, y, line ? new Color(0.4f, 0.7f, 0.8f) : new Color(0.12f, 0.22f, 0.28f));
                }
            }

            texture.Apply();
            File.WriteAllBytes(texturePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(texturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.name = "FoundationGrid";
        material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        material.mainTextureScale = new Vector2(4f, 4f);
        material.SetFloat("_Smoothness", 0.15f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void CreateFoundation(Material material)
    {
        var root = new GameObject("Building Foundation");
        try
        {
            root.layer = LayerMask.NameToLayer("Block");
            root.AddComponent<BoxCollider>();
            Block block = root.AddComponent<Block>();
            block.canSpawnConnector = false;
            block.canEditInfo = true;
            block.info = "Building foundation. Resize with the Building Editor panel.";
            block.connectorPrefab = Resources.Load<GameObject>("Blocks/Cockpit").GetComponent<Block>().connectorPrefab;
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            Object.DestroyImmediate(model.GetComponent<Collider>());
            model.GetComponent<Renderer>().sharedMaterial = material;
            BuildingFoundation foundation = root.AddComponent<BuildingFoundation>();
            var serialized = new SerializedObject(foundation);
            serialized.FindProperty("_model").objectReferenceValue = model.transform;
            serialized.FindProperty("_surface").objectReferenceValue = model.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foundation.Configure(new BuildingFoundationData());
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/Foundation.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void CreatePanel()
    {
        GameObject root = Rect("Building Editor", null, new Vector2(430f, 146f), Vector2.zero);
        try
        {
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-22f, -110f);
            root.AddComponent<Image>().color = new Color(0.04f, 0.08f, 0.12f, 0.96f);
            Label(
                "Title",
                root.transform,
                "BUILDING  |  Ctrl+Alt+F to exit",
                new Vector2(402f, 24f),
                new Vector2(14f, -12f),
                17);
            Label(
                "WidthLabel",
                root.transform,
                "Width",
                new Vector2(50f, 32f),
                new Vector2(14f, -46f),
                15);
            InputField width = Input("Width", root.transform, new Vector2(66f, -46f));
            Label(
                "LengthLabel",
                root.transform,
                "Length",
                new Vector2(55f, 32f),
                new Vector2(146f, -46f),
                15);
            InputField length = Input("Length", root.transform, new Vector2(202f, -46f));
            GameObject apply = Rect("Apply", root.transform, new Vector2(118f, 32f), new Vector2(294f, -46f));
            apply.AddComponent<Image>().color = new Color(0.12f, 0.42f, 0.55f);
            Button button = apply.AddComponent<Button>();
            Text caption = Label(
                "Text",
                apply.transform,
                "Apply size",
                new Vector2(118f, 32f),
                Vector2.zero,
                15);
            caption.alignment = TextAnchor.MiddleCenter;
            Text status = Label(
                "Status",
                root.transform,
                "1-64 cells per side. Changes are saved automatically.",
                new Vector2(400f, 52f),
                new Vector2(14f, -86f),
                14);
            BuildingEditorPanel panel = root.AddComponent<BuildingEditorPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_width").objectReferenceValue = width;
            serialized.FindProperty("_length").objectReferenceValue = length;
            serialized.FindProperty("_status").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddPersistentListener(button.onClick, panel.ApplySize);
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "/EditorPanel.prefab");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return obj;
    }

    private static Text Label(
        string name,
        Transform parent,
        string value,
        Vector2 size,
        Vector2 position,
        int fontSize)
    {
        Text text = Rect(name, parent, size, position).AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.text = value;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private static InputField Input(string name, Transform parent, Vector2 position)
    {
        GameObject root = Rect(name, parent, new Vector2(64f, 32f), position);
        root.AddComponent<Image>().color = new Color(0.16f, 0.21f, 0.26f);
        Text text = Label(
            "Text",
            root.transform,
            "4",
            new Vector2(52f, 32f),
            new Vector2(6f, 0f),
            17);
        InputField field = root.AddComponent<InputField>();
        field.textComponent = text;
        field.contentType = InputField.ContentType.IntegerNumber;
        field.characterLimit = 2;
        field.text = "4";
        return field;
    }
}

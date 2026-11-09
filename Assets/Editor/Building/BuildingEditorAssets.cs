// 职责：保存建筑底座、网格材质及尺寸编辑 uGUI 为真实可编辑资产，不写入 Main 场景。
// 入口：Bake() 菜单；已存在的 Prefab/材质优先保留，删除指定资产后可重新生成。
using System;
using System.IO;
using UnityEditor;
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

    [MenuItem("Tools/Building Editor/Migrate free building panel")]
    public static void MigrateFreeBuildingPanel()
    {
        string path = Folder + "/EditorPanel.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (string name in new[]
            {
                "Width",
                "Length",
                "WidthLabel",
                "LengthLabel",
                "Apply"
            }

            )
            {
                Transform old = root.transform.Find(name);
                if (old != null)
                {
                    Object.DestroyImmediate(old.gameObject);
                }
            }

            Text title = root.transform.Find("Title").GetComponent<Text>();
            title.text = "BUILDING  |  Free ground building";
            Text status = root.transform.Find("Status").GetComponent<Text>();
            status.rectTransform.anchoredPosition = new Vector2(14f, -42f);
            status.rectTransform.sizeDelta = new Vector2(400f, 90f);
            status.text = "Build anywhere on the ground. Footprint crops automatically.";
            status.verticalOverflow = VerticalWrapMode.Overflow;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
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
            Text status = Label(
                "Status",
                root.transform,
                "Build anywhere on the ground. Footprint crops automatically.",
                new Vector2(400f, 90f),
                new Vector2(14f, -42f),
                14);
            BuildingEditorPanel panel = root.AddComponent<BuildingEditorPanel>();
            var serialized = new SerializedObject(panel);
            serialized.FindProperty("_status").objectReferenceValue = status;
            serialized.FindProperty("_title").objectReferenceValue = root.transform.Find("Title").GetComponent<Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
}

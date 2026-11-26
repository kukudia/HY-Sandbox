// 职责：将可编辑快捷栏、目录图标和默认槽位保存为真实配置资产及Main场景层级。
// 入口：Install()；只创建自己拥有的QuickBuild层级，保留已有目录和个人快捷栏配置。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BuildHotbarAuthoring
{
    private const string CatalogPath = "Assets/Resources/Building/BuildHotbar.asset";
    [MenuItem("Tools/HY-Sandbox/Building/Install quick build bar")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before authoring the build bar.");
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Build.unity");
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene("Assets/Scenes/Build.unity", OpenSceneMode.Additive);
        }

        if (scene.isDirty)
        {
            throw new InvalidOperationException("Save Main before installing the build bar.");
        }

        string backup = "Library/BuildHotbar/" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fffffff");
        Directory.CreateDirectory(backup);
        File.Copy(scene.path, backup + "/Main.unity");
        BuildHotbarCatalog catalog = AssetDatabase.LoadAssetAtPath<BuildHotbarCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BuildHotbarCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            using var data = new SerializedObject(catalog);
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Blocks").Where(p => p.GetComponent<Block>() != null).OrderBy(p => p.name, StringComparer.Ordinal).ToArray();
            var entries = data.FindProperty("entries");
            entries.arraySize = prefabs.Length + 1;
            for (int i = 0; i < prefabs.Length; i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("resourcePath").stringValue = "Blocks/" + prefabs[i].name;
                entry.FindPropertyRelative("icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/BuildPalette/Icons/" + prefabs[i].name + ".png");
            }

            entries.GetArrayElementAtIndex(prefabs.Length).FindPropertyRelative("resourcePath").stringValue = "Hangars/Hangar";
            string[] defaults =
            {
                "2x2x1",
                "2x2x2",
                "1x1x1",
                "2x1x1",
                "Cockpit",
                "UniversalThruster",
                "PowerGeneratingUnit",
                "Turret"
            };
            var slots = data.FindProperty("defaults");
            slots.arraySize = BuildHotbar.Capacity;
            for (int i = 0; i < defaults.Length; i++)
            {
                GameObject prefab = prefabs.FirstOrDefault(p => p.name == defaults[i]);
                var slot = slots.GetArrayElementAtIndex(i);
                slot.FindPropertyRelative("resourcePath").stringValue = prefab != null ? "Blocks/" + prefab.name : string.Empty;
                slot.FindPropertyRelative("materialId").stringValue = prefab != null && prefab.GetComponent<BlockMaterial>() != null ? "metal" : string.Empty;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        MainUIPanels panels = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainUIPanels>(true)).Single();
        Transform parent = panels.buildPanel.transform;
        RectTransform root = Child(parent, "QuickBuild");
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Configure quick build bar");
        root.anchorMin = root.anchorMax = new Vector2(.5f, 0f);
        root.pivot = new Vector2(.5f, 0f);
        root.anchoredPosition = new Vector2(85f, 12f);
        root.sizeDelta = new Vector2(760f, 132f);
        Image background = Component<Image>(root);
        background.color = new Color(.015f, .035f, .055f, .94f);
        BuildHotbar bar = Component<BuildHotbar>(root);
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/Fonts/ChakraPetch-Medium.ttf");
        Text details = Text(root, "Details", font, 13);
        Place((RectTransform)details.transform, new Vector2(8, 110), new Vector2(744, 20));
        details.alignment = TextAnchor.MiddleLeft;
        details.text = "QUICK BUILD · 1–8 SELECT / SHIFT + 1–8 PIN / DRAG TO SWAP / RIGHT CLICK CLEAR";
        Text hint = Text(root, "Hint", font, 11);
        hint.text = "1–8 SELECT / SHIFT + 1–8 PIN CURRENT / DRAG FROM CATALOG OR SWAP / RIGHT CLICK CLEAR";
        hint.alignment = TextAnchor.MiddleLeft;
        hint.color = new Color(.5f, .7f, .75f);
        Place((RectTransform)hint.transform, new Vector2(8, 88), new Vector2(744, 18));
        BlueprintUIPanel statistics = panels.GetComponentInChildren<BlueprintUIPanel>(true);
        if (statistics != null)
        {
            RectTransform rect = statistics.GetComponent<RectTransform>();
            // Keep the original statistics card above the new bottom bar without moving other panels.
            if (rect.anchorMin == new Vector2(.5f, 0f) && rect.anchorMax == rect.anchorMin && rect.anchoredPosition.y < 251.5f)
            {
                Undo.RecordObject(rect, "Make space for quick build bar");
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, 251.5f);
            }
        }

        BuildHotbarSlot[] slotsView = new BuildHotbarSlot[BuildHotbar.Capacity];
        for (int i = 0; i < slotsView.Length; i++)
        {
            RectTransform tile = Child(root, "Slot" + (i + 1));
            Place(tile, new Vector2(8 + i * 93, 6), new Vector2(88, 78));
            Image border = Component<Image>(tile);
            border.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/BuildPalette/RoundedOutline.png");
            border.type = Image.Type.Sliced;
            BuildHotbarSlot view = Component<BuildHotbarSlot>(tile);
            RectTransform icon = Child(tile, "Icon");
            Place(icon, new Vector2(23, 24), new Vector2(44, 46));
            Image image = Component<Image>(icon);
            image.preserveAspect = true;
            image.raycastTarget = false;
            Text label = Text(tile, "Label", font, 10);
            Place((RectTransform)label.transform, new Vector2(2, 0), new Vector2(84, 26));
            label.alignment = TextAnchor.MiddleCenter;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 10;
            Text number = Text(tile, "Shortcut", font, 14);
            Place((RectTransform)number.transform, new Vector2(5, 55), new Vector2(18, 20));
            using var data = new SerializedObject(view);
            data.FindProperty("icon").objectReferenceValue = image;
            data.FindProperty("border").objectReferenceValue = border;
            data.FindProperty("label").objectReferenceValue = label;
            data.FindProperty("shortcut").objectReferenceValue = number;
            data.ApplyModifiedPropertiesWithoutUndo();
            slotsView[i] = view;
        }

        RectTransform drag = Child(root, "DragPreview");
        drag.anchorMin = drag.anchorMax = new Vector2(.5f, .5f);
        drag.pivot = new Vector2(.5f, .5f);
        drag.sizeDelta = new Vector2(56, 56);
        Image dragImage = Component<Image>(drag);
        dragImage.preserveAspect = true;
        dragImage.raycastTarget = false;
        dragImage.color = new Color(1, 1, 1, .8f);
        drag.gameObject.SetActive(false);
        BuildPalette palette = panels.GetComponentInChildren<BuildPalette>(true);
        if (palette != null)
        {
            using var data = new SerializedObject(palette);
            data.FindProperty("hotbar").objectReferenceValue = bar;
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        using (var data = new SerializedObject(bar))
        {
            data.FindProperty("catalog").objectReferenceValue = catalog;
            data.FindProperty("details").objectReferenceValue = details;
            data.FindProperty("dragPreview").objectReferenceValue = dragImage;
            var views = data.FindProperty("views");
            views.arraySize = slotsView.Length;
            for (int i = 0; i < slotsView.Length; i++)
            {
                views.GetArrayElementAtIndex(i).objectReferenceValue = slotsView[i];
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssetIfDirty(catalog);
        Debug.Log("Quick build bar saved to Main; backup=" + backup);
    }

    private static RectTransform Child(Transform parent, string name)
    {
        RectTransform child = parent.Find(name) as RectTransform;
        if (child == null)
        {
            child = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            child.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(child.gameObject, "Create quick build element");
        }

        return child;
    }

    private static T Component<T>(Component owner)
        where T : Component
    {
        return owner.GetComponent<T>() ?? owner.gameObject.AddComponent<T>();
    }

    private static Text Text(Transform parent, string name, Font font, int size)
    {
        Text text = Component<Text>(Child(parent, name));
        text.font = font;
        text.fontSize = size;
        text.color = new Color(.8f, .94f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private static void Place(RectTransform target, Vector2 position, Vector2 size)
    {
        target.anchorMin = target.anchorMax = Vector2.zero;
        target.pivot = Vector2.zero;
        target.anchoredPosition = position;
        target.sizeDelta = size;
    }
}

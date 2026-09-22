using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Refreshes the saved Main catalog and renders real Prefabs into alpha thumbnails.</summary>
public static class BuildPaletteBaker
{
    private const string Folder = "Assets/Art/BuildPalette";
    private static readonly string[] Categories = { "ALL", "STRUCTURE", "FLIGHT", "POWER", "COMBAT", "SALVAGE" };

    [MenuItem("Tools/Build Palette/Bake thumbnails and refresh Main")]
    public static void Bake()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before baking.");
        Directory.CreateDirectory(Folder + "/Icons");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        if (scene.isDirty) throw new InvalidOperationException("Save Main before baking so unrelated edits are not saved implicitly.");
        Directory.CreateDirectory("Temp/BuildPalette");
        File.Copy(scene.path, "Temp/BuildPalette/Main-before-bake.unity", true);
        var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainUIButtons>(true)).Single();
        RectTransform root = (RectTransform)ui.transform.Find("BuildPanel/ButtonContent");
        var palette = root.GetComponent<BuildPalette>() ?? root.gameObject.AddComponent<BuildPalette>();
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
        var prefabs = Resources.LoadAll<GameObject>("Blocks").Where(p => p.GetComponent<Block>() != null && p.name != "Connector").OrderBy(p => Category(p.name)).ThenBy(p => p.name).ToArray();
        foreach (GameObject prefab in prefabs)
            BlockArtPreview.Render(AssetDatabase.GetAssetPath(prefab), Folder + "/Icons/" + prefab.name + ".png", configure: ConfigureThumbnail, transparent: true);
        CreateBorder();
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(Folder, "*.png", SearchOption.AllDirectories)) ImportSprite(path.Replace('\\', '/'));
        Sprite border = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/RoundedOutline.png");

        var oldGrid = root.GetComponent<GridLayoutGroup>();
        if (oldGrid != null) Object.DestroyImmediate(oldGrid);
        root.anchorMin = new Vector2(0, 0); root.anchorMax = new Vector2(0, 1);
        root.pivot = new Vector2(0, 1); root.offsetMin = new Vector2(60, 42); root.offsetMax = new Vector2(444, -320);
        Image background = root.GetComponent<Image>() ?? root.gameObject.AddComponent<Image>();
        background.color = new Color(0.015f, 0.035f, 0.055f, 0.76f);
        RectTransform nav = Child(root, "CategoryNavigation");
        Rect(nav, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -68), Vector2.zero);
        var navGrid = nav.GetComponent<GridLayoutGroup>() ?? nav.gameObject.AddComponent<GridLayoutGroup>();
        navGrid.cellSize = new Vector2(120, 28); navGrid.spacing = new Vector2(6, 6); navGrid.padding = new RectOffset(6, 6, 4, 4);
        Button[] tabs = new Button[Categories.Length];
        for (int i = 0; i < tabs.Length; i++)
        {
            RectTransform t = Child(nav, Categories[i]);
            Image outline = t.GetComponent<Image>() ?? t.gameObject.AddComponent<Image>(); outline.sprite = border; outline.type = Image.Type.Sliced;
            Button button = t.GetComponent<Button>() ?? t.gameObject.AddComponent<Button>(); button.targetGraphic = outline;
            button.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddIntPersistentListener(button.onClick, palette.SelectCategory, i);
            Text label = Label(t, "Label", font, 13); label.text = Categories[i];
            tabs[i] = button;
        }
        RectTransform viewport = Child(root, "Viewport");
        Rect(viewport, Vector2.zero, Vector2.one, new Vector2(6, 38), new Vector2(-14, -76));
        if (viewport.GetComponent<RectMask2D>() == null) viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = Child(viewport, "Blocks");
        Rect(content, new Vector2(0, 1), Vector2.one, Vector2.zero, Vector2.zero); content.pivot = new Vector2(0.5f, 1);
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>() ?? content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(84, 84); grid.spacing = new Vector2(8, 8); grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 4;
        var fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>(); fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ScrollRect scroll = root.GetComponent<ScrollRect>() ?? root.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 28;
        RectTransform rail = Child(root, "Scrollbar");
        Rect(rail, new Vector2(1, 0), Vector2.one, new Vector2(-7, 38), new Vector2(-3, -76));
        Image railImage = rail.GetComponent<Image>() ?? rail.gameObject.AddComponent<Image>(); railImage.color = new Color(1, 1, 1, 0.12f);
        RectTransform handle = Child(rail, "Handle"); Rect(handle, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image handleImage = handle.GetComponent<Image>() ?? handle.gameObject.AddComponent<Image>(); handleImage.color = new Color(0.35f, 0.85f, 1f, 0.7f);
        Scrollbar scrollbar = rail.GetComponent<Scrollbar>() ?? rail.gameObject.AddComponent<Scrollbar>(); scrollbar.handleRect = handle; scrollbar.targetGraphic = handleImage; scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = scrollbar;
        Text tooltip = Label(root, "HoveredBlockName", font, 17); Rect(tooltip.rectTransform, Vector2.zero, new Vector2(1, 0), Vector2.zero, new Vector2(0, 34)); tooltip.gameObject.SetActive(false);

        Set(palette, "_tooltip", tooltip); Set(palette, "_scroll", scroll);
        var serialized = new SerializedObject(palette); var tabProperty = serialized.FindProperty("_tabs"); tabProperty.arraySize = tabs.Length;
        for (int i = 0; i < tabs.Length; i++) tabProperty.GetArrayElementAtIndex(i).objectReferenceValue = tabs[i]; serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (GameObject prefab in prefabs)
        {
            BlockButton entry = ui.blockButtons.FirstOrDefault(b => b.block == prefab);
            Button button = entry?.button;
            if (button == null)
            {
                var t = Child(content, prefab.name + "Button"); button = t.gameObject.AddComponent<Button>();
                entry = new BlockButton { block = prefab, button = button, name = prefab.name }; ui.blockButtons.Add(entry);
            }
            button.transform.SetParent(content, false);
            // Migrate only this catalog's legacy label; unrelated tool buttons retain their layout.
            foreach (Text text in button.GetComponentsInChildren<Text>(true)) Object.DestroyImmediate(text.gameObject);
            Image outline = button.GetComponent<Image>() ?? button.gameObject.AddComponent<Image>(); outline.sprite = border; outline.type = Image.Type.Sliced; outline.color = Color.white;
            button.targetGraphic = outline; button.onClick = new Button.ButtonClickedEvent();
            var colors = button.colors; colors.normalColor = Color.white; colors.highlightedColor = new Color(0.4f, 0.9f, 1f); colors.pressedColor = new Color(0.2f, 0.65f, 0.85f); button.colors = colors;
            RectTransform icon = Child(button.transform, "Preview"); Rect(icon, Vector2.zero, Vector2.one, new Vector2(3, 3), new Vector2(-3, -3));
            Image image = icon.GetComponent<Image>() ?? icon.gameObject.AddComponent<Image>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/Icons/" + prefab.name + ".png"); image.preserveAspect = true; image.raycastTarget = false;
            BuildPaletteItem item = button.GetComponent<BuildPaletteItem>() ?? button.gameObject.AddComponent<BuildPaletteItem>();
            Set(item, "_palette", palette); Set(item, "_outline", outline);
            serialized = new SerializedObject(item); serialized.FindProperty("_blockName").stringValue = prefab.name; serialized.FindProperty("_category").intValue = Category(prefab.name); serialized.ApplyModifiedPropertiesWithoutUndo();
            button.gameObject.SetActive(true);
        }
        ui.blockButtons.RemoveAll(b => b.block == null || !prefabs.Contains(b.block));
        var manager = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BuildManager>(true)).Single();
        var hints = manager.GetComponent<ConnectorPlacementHints>() ?? manager.gameObject.AddComponent<ConnectorPlacementHints>();
        CreateHintAssets(out Mesh mesh, out Material material);
        Set(hints, "_outlineMesh", mesh); Set(hints, "_outlineMaterial", material); Set(manager, "_connectorHints", hints);
        EditorUtility.SetDirty(ui); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssetIfDirty(mesh); AssetDatabase.SaveAssetIfDirty(material);
        Debug.Log("Build palette baked: " + prefabs.Length + " blocks, 6 tabs, saved to Main.");
    }

    private static void ConfigureThumbnail(GameObject root)
    {
        CargoHold hold = root.GetComponent<CargoHold>();
        if (hold == null || hold.Kind == CargoKind.SpecialPart) return;
        // Preview contents exist only in the isolated rendering clone, never in the gameplay Prefab.
        hold.SendMessage("Awake");
        root.GetComponent<Power>().currentPower = 1000f;
        hold.RestoreContents(new System.Collections.Generic.List<CargoItem> { new CargoItem { kind = hold.Kind, amount = 65 } });
        CargoHoldView display = root.GetComponent<CargoHoldView>();
        display.SendMessage("Awake"); display.SendMessage("LateUpdate");
    }

    private static int Category(string name)
    {
        if (name.Contains("Hold") || name.Contains("BotCont")) return 5;
        if (name.Contains("Thruster") || name.Contains("Cockpit") || name.Contains("Flight")) return 2;
        if (name.Contains("Power")) return 3;
        if (name.Contains("Turret")) return 4;
        return 1;
    }
    private static RectTransform Child(Transform parent, string name)
    {
        var found = parent.Find(name) as RectTransform;
        if (found != null) return found;
        var result = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); result.SetParent(parent, false); return result;
    }
    private static void Rect(RectTransform t, Vector2 min, Vector2 max, Vector2 low, Vector2 high) { t.anchorMin = min; t.anchorMax = max; t.offsetMin = low; t.offsetMax = high; }
    private static Text Label(Transform parent, string name, Font font, int size)
    {
        RectTransform t = Child(parent, name); Rect(t, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text text = t.GetComponent<Text>() ?? t.gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white; text.raycastTarget = false; return text;
    }
    private static void Set(Object target, string field, Object value) { var s = new SerializedObject(target); s.FindProperty(field).objectReferenceValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
    private static void ImportSprite(string path)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single; importer.maxTextureSize = 256; importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (path.EndsWith("RoundedOutline.png")) importer.spriteBorder = new Vector4(18, 18, 18, 18);
        importer.SaveAndReimport();
    }
    private static void CreateBorder()
    {
        var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        for (int y = 0; y < 128; y++) for (int x = 0; x < 128; x++)
        {
            Vector2 q = new Vector2(Mathf.Abs(x - 63.5f), Mathf.Abs(y - 63.5f)) - new Vector2(48, 48);
            float d = new Vector2(Mathf.Max(q.x, 0), Mathf.Max(q.y, 0)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0) - 13;
            texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1.5f - Mathf.Abs(d))));
        }
        texture.Apply(); File.WriteAllBytes(Folder + "/RoundedOutline.png", texture.EncodeToPNG()); Object.DestroyImmediate(texture);
    }
    private static void CreateHintAssets(out Mesh mesh, out Material material)
    {
        string meshPath = Folder + "/ConnectorOutline.asset";
        mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (mesh == null) { mesh = new Mesh { name = "Unit rounded connector outline" }; AssetDatabase.CreateAsset(mesh, meshPath); }
        const int steps = 9; const float radius = 0.1f; const float width = 0.02f;
        int count = steps * 4; Vector3[] vertices = new Vector3[count * 2]; int[] triangles = new int[count * 6];
        for (int corner = 0; corner < 4; corner++) for (int step = 0; step < steps; step++)
        {
            int i = corner * steps + step; float angle = (corner * 90 + step * 90f / (steps - 1)) * Mathf.Deg2Rad;
            Vector3 center = new Vector3(corner == 0 || corner == 3 ? 0.4f : -0.4f, corner < 2 ? 0.4f : -0.4f, 0);
            Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            vertices[i * 2] = center + direction * radius; vertices[i * 2 + 1] = center + direction * (radius - width);
            int next = ((i + 1) % count) * 2; int j = i * 6;
            triangles[j] = i * 2; triangles[j + 1] = next; triangles[j + 2] = i * 2 + 1; triangles[j + 3] = next; triangles[j + 4] = next + 1; triangles[j + 5] = i * 2 + 1;
        }
        mesh.Clear(); mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
        string materialPath = Folder + "/ConnectorOutline.mat"; material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Unlit")); AssetDatabase.CreateAsset(material, materialPath); }
        material.SetColor("_BaseColor", Color.white); material.SetFloat("_Cull", 0); EditorUtility.SetDirty(material);
    }
}

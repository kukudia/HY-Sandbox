// 职责：复制双层避障地图为独立3v3观战场景，保存六槽位、自由相机与复用战斗HUD。
// 入口：Create()菜单；已存在场景拒绝覆盖，不修改原避障场景。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SkirmishArenaAuthoring
{
    public const string ScenePath = "Assets/Scenes/Random3v3Arena.unity";
    [MenuItem("Tools/HY-Sandbox/Combat/Create Random 3v3 Arena")]
    public static void Create()
    {
        if (EditorApplication.isPlaying || AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            throw new InvalidOperationException("Exit Play Mode; existing arena is preserved.");
        }

        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save open scenes before creating the arena copy.");
        }

        if (!AssetDatabase.CopyAsset(EnemyAvoidanceLabAuthoring.ScenePath, ScenePath))
        {
            throw new InvalidOperationException("Cannot copy the avoidance map.");
        }

        Scene arena = EditorSceneManager.OpenScene(ScenePath);
        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        Transform district = lab.transform.Find("Complex obstacle district");
        district.SetParent(null, true);
        district.name = "Battlefield";
        district.gameObject.SetActive(true);
        UnityEngine.Object.DestroyImmediate(district.GetComponent<EnemyAvoidanceCourse>());
        UnityEngine.Object.DestroyImmediate(lab.gameObject);
        foreach (ControlUnit unit in arena.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ControlUnit>(true)).ToArray())
        {
            UnityEngine.Object.DestroyImmediate(unit.gameObject);
        }

        PlayManager play = Find<PlayManager>(arena);
        play.playMode = true;
        play.blocksParent = null;
        play.allControlUnits.Clear();
        play.showUI = false;
        Camera camera = Find<EnemyLabCamera>(arena).GetComponent<Camera>();
        var cameraSettings = new SerializedObject(camera.GetComponent<EnemyLabCamera>());
        cameraSettings.FindProperty("lab").objectReferenceValue = null;
        cameraSettings.FindProperty("reservedLeftWidth").floatValue = 0f;
        cameraSettings.ApplyModifiedPropertiesWithoutUndo();
        var owner = new GameObject("Automatic 3v3 Match");
        SkirmishMatch match = owner.AddComponent<SkirmishMatch>();
        var matchSettings = new SerializedObject(match);
        TextAsset[] blueprints = Resources.LoadAll<TextAsset>("WorldBlueprints/Enemies").OrderBy(asset => asset.name).ToArray();
        SerializedProperty pool = matchSettings.FindProperty("blueprints");
        pool.arraySize = blueprints.Length;
        for (int i = 0; i < blueprints.Length; i++)
        {
            pool.GetArrayElementAtIndex(i).objectReferenceValue = blueprints[i];
        }

        SerializedProperty slots = matchSettings.FindProperty("slots");
        slots.arraySize = 6;
        for (int i = 0; i < 6; i++)
        {
            var slot = new GameObject((i < 3 ? "Blue" : "Red") + " Slot " + (i % 3 + 1));
            slot.transform.SetParent(owner.transform, false);
            slot.transform.SetPositionAndRotation(
                new Vector3(i < 3 ? -124f : 124f, 12f, (i % 3 - 1) * 96f),
                Quaternion.LookRotation(i < 3 ? Vector3.right : Vector3.left));
            slots.GetArrayElementAtIndex(i).objectReferenceValue = slot.transform;
        }

        matchSettings.ApplyModifiedPropertiesWithoutUndo();
        Scene main = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        CombatHud source = Find<CombatHud>(main);
        SceneManager.SetActiveScene(arena);
        var canvasObject = new GameObject("Arena HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;
        GameObject copy = UnityEngine.Object.Instantiate(source.gameObject, canvas.transform, false);
        copy.name = "CombatHud";
        copy.SetActive(true);
        CombatHud combatHud = copy.GetComponent<CombatHud>();
        var combatSettings = new SerializedObject(combatHud);
        combatSettings.FindProperty("_maxLabelDistance").floatValue = 500f;
        combatSettings.ApplyModifiedPropertiesWithoutUndo();
        ConfigureCombatHud(combatHud);
        EditorSceneManager.CloseScene(main, true);
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
        Text title = Label(
            canvas.transform,
            "Round Status",
            font,
            new Vector2(20f, -18f),
            new Vector2(720f, 90f),
            24);
        Text roster = Label(
            canvas.transform,
            "Team Roster",
            font,
            new Vector2(20f, -115f),
            new Vector2(520f, 180f),
            17);
        Text feed = Label(
            canvas.transform,
            "Kill Feed",
            font,
            new Vector2(1000f, -18f),
            new Vector2(580f, 220f),
            17);
        Text controls = Label(
            canvas.transform,
            "Camera Controls",
            font,
            new Vector2(20f, -850f),
            new Vector2(1400f, 35f),
            16);
        controls.text = "SPECTATOR   RMB + WASD / Q E   |   SHIFT: FAST   |   MMB: PAN   |   WHEEL: ZOOM   |   AUTO RESTART: 5s";
        SkirmishHud hud = canvasObject.AddComponent<SkirmishHud>();
        Set(hud, "combatHud", combatHud);
        Set(hud, "status", title);
        Set(hud, "roster", roster);
        Set(hud, "feed", feed);
        Set(match, "hud", hud);
        Set(match, "play", play);
        Set(match, "destruction", Find<DestroyManager>(arena));
        Set(match, "projectiles", Find<TurretProjectilePool>(arena));
        Set(match, "effects", Find<VfxEffectPool>(arena));
        Set(play, "skirmish", match);
        EditorSceneManager.MarkSceneDirty(arena);
        EditorSceneManager.SaveScene(arena);
        AssetDatabase.SaveAssets();
    }

    private static T Find<T>(Scene scene)
        where T : Component
    {
        return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
    }

    public static void UpgradeHud()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath)
        {
            throw new InvalidOperationException("Open the arena in Edit Mode.");
        }

        ConfigureCombatHud(Find<CombatHud>(scene));
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureCombatHud(CombatHud hud)
    {
        // 观战提示同时显示双方长蓝图名；扩大本副本，保持探索原场景的布局。
        RectTransform notice = (RectTransform)hud.transform.Find("KillNotice");
        notice.sizeDelta = new Vector2(900f, 110f);
        RectTransform victim = (RectTransform)notice.Find("EnemyName");
        victim.offsetMin = new Vector2(22f, 14f);
        victim.offsetMax = new Vector2(-18f, -39f);
        victim.GetComponent<Text>().resizeTextMinSize = 12;
        RectTransform score = (RectTransform)notice.Find("Count");
        score.offsetMin = new Vector2(550f, 65f);
        score.offsetMax = new Vector2(-18f, -12f);
        RectTransform template = (RectTransform)hud.transform.Find("EnemyNameplateTemplate");
        template.sizeDelta = new Vector2(320f, 74f);
        RectTransform name = (RectTransform)template.Find("Name");
        name.offsetMax = new Vector2(210f, name.offsetMax.y);
        RectTransform value = (RectTransform)template.Find("Value");
        value.offsetMin = new Vector2(218f, value.offsetMin.y);
        value.offsetMax = new Vector2(310f, value.offsetMax.y);
    }

    private static void Set(UnityEngine.Object owner, string field, UnityEngine.Object value)
    {
        var settings = new SerializedObject(owner);
        settings.FindProperty(field).objectReferenceValue = value;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Text Label(
        Transform parent,
        string name,
        Font font,
        Vector2 position,
        Vector2 size,
        int fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Shadow));
        go.transform.SetParent(parent, false);
        Text label = go.GetComponent<Text>();
        label.font = font;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAnchor.UpperLeft;
        label.raycastTarget = false;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return label;
    }
}

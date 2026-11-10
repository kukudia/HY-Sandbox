// 职责：迁移预研泊位为建筑蓝图机库，并保存Main基地编辑入口；保留旧基地原始资产。
// 入口：Install()；仅在保存后的Edit Mode执行，可重复更新工具拥有的引用。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HangarSceneIntegration
{
    public const string PrototypeBlueprint = "Assets/Siege/BastionHangars.json";
    [MenuItem("HY-Sandbox/Siege/Hangars/Install Integration")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save scenes and exit Play before installing hangars.");
        }

        HangarPrefabAuthoring.Generate();
        Scene previous = SceneManager.GetActiveScene();
        InstallPrototype();
        InstallStarter();
        InstallWorkshop();
        if (previous.IsValid() && previous.isLoaded)
        {
            SceneManager.SetActiveScene(previous);
        }

        AssetDatabase.SaveAssets();
    }

    private static void InstallPrototype()
    {
        Scene scene = EditorSceneManager.OpenScene(SiegeSceneAuthoring.ScenePath, OpenSceneMode.Additive);
        try
        {
            SiegeSession session = SceneObjectQuery.FindInScene<SiegeSession>(scene);
            var config = new SerializedObject(session);
            SiegeSettings settings = (SiegeSettings)config.FindProperty("settings").objectReferenceValue;
            var source = JsonUtility.FromJson<BlockDataList>(File.ReadAllText("Assets/Siege/BastionBase.json"));
            source.foundation.width = 64;
            source.foundation.length = 64;
            Quaternion facing = Quaternion.Euler(0f, 90f, 0f);
            Block template = AssetDatabase.LoadAssetAtPath<GameObject>(HangarPrefabAuthoring.PrefabPath).GetComponent<Block>();
            for (int i = 0; i < 3; i++)
            {
                var data = new BlockData(template)
                {
                    id = "hangar-" + i,
                    posX = i < 2 ? 45f : 10f,
                    posY = template.y * .5f,
                    posZ = i < 2 ? 8f + i * 24f : 50f,
                    rotX = facing.x,
                    rotY = i < 2 ? facing.y : 0f,
                    rotZ = facing.z,
                    rotW = i < 2 ? facing.w : 1f,
                    resourcePath = "Hangars/Hangar"
                };
                source.blocks.Add(data);
            }

            File.WriteAllText(PrototypeBlueprint, JsonUtility.ToJson(source, true));
            AssetDatabase.ImportAsset(PrototypeBlueprint);
            var values = new SerializedObject(settings);
            values.FindProperty("baseBlueprint").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(PrototypeBlueprint);
            values.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("Bay ", StringComparison.Ordinal) && root.name.EndsWith(" Platform", StringComparison.Ordinal))
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }

            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void InstallStarter()
    {
        const string original = "Assets/Siege/Construction/StarterBase.json";
        const string target = "Assets/Siege/Construction/StarterHangar.json";
        BlockDataList design = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(original));
        design.foundation.width = 64;
        design.foundation.length = 32;
        Block template = AssetDatabase.LoadAssetAtPath<GameObject>(HangarPrefabAuthoring.PrefabPath).GetComponent<Block>();
        Quaternion facing = Quaternion.Euler(0f, 90f, 0f);
        var data = new BlockData(template)
        {
            id = "starter-hangar",
            posX = 45f,
            posY = template.y * .5f,
            posZ = 10f,
            rotX = facing.x,
            rotY = facing.y,
            rotZ = facing.z,
            rotW = facing.w,
            resourcePath = "Hangars/Hangar"
        };
        design.blocks.Add(data);
        File.WriteAllText(target, JsonUtility.ToJson(design, true));
        AssetDatabase.ImportAsset(target);
        SiegeConstructionSettings settings = AssetDatabase.LoadAssetAtPath<SiegeConstructionSettings>(SiegeConstructionAuthoring.SettingsPath);
        var values = new SerializedObject(settings);
        values.FindProperty("starter").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(target);
        values.ApplyModifiedPropertiesWithoutUndo();
        if (!settings.Validate(design, out string reason))
        {
            throw new InvalidOperationException("Hangar starter must satisfy the actual base rules: " + reason);
        }

        EditorUtility.SetDirty(settings);
        var combat = new SerializedObject(settings.Combat);
        combat.FindProperty("autoRepairBase").boolValue = true;
        combat.FindProperty("autoRepairFleet").boolValue = true;
        combat.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(settings.Combat);
    }

    private static void InstallWorkshop()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Additive);
        try
        {
            SiegeBaseWorkshop workshop = SceneObjectQuery.FindInScene<SiegeBaseWorkshop>(scene, FindObjectsInactive.Include);
            RectTransform root = (RectTransform)workshop.transform;
            root.sizeDelta = new Vector2(850f, root.sizeDelta.y);
            Transform previous = root.Find("Hangar");
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            var values = new SerializedObject(workshop);
            Button source = (Button)values.FindProperty("core").objectReferenceValue;
            Button button = UnityEngine.Object.Instantiate(source, root);
            button.name = "Hangar";
            button.onClick = new Button.ButtonClickedEvent();
            ((RectTransform)button.transform).anchoredPosition = new Vector2(740f, 90f);
            button.GetComponentInChildren<Text>().text = "PLACE HANGAR";
            values.FindProperty("hangar").objectReferenceValue = button;
            Text status = (Text)values.FindProperty("status").objectReferenceValue;
            status.rectTransform.anchoredPosition = new Vector2(425f, 34f);
            status.rectTransform.sizeDelta = new Vector2(820f, 62f);
            values.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}

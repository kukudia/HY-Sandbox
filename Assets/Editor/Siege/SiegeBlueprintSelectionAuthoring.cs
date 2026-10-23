// 职责：通过Editor API为正常Siege安装真实可编辑的双方蓝图选择器，保留压力场夹具。
// 入口：Install()；重复执行复用现有控件，不覆盖Main或用户蓝图。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeBlueprintSelectionAuthoring
{
    [MenuItem("HY-Sandbox/Siege/Install Blueprint Selection")]
    public static void Install()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != SiegeSceneAuthoring.ScenePath || scene.isDirty)
        {
            throw new InvalidOperationException("Open clean normal Siege in Edit Mode before migrating its UI.");
        }

        SiegeHud hud = Object.FindFirstObjectByType<SiegeHud>();
        Transform commands = hud.transform.Find("Fleet Commands");
        var serialized = new SerializedObject(hud);
        Dropdown defenders = CreateDropdown(commands, "Saves Blueprints", 70);
        Dropdown attackers = CreateDropdown(commands, "Enemy Blueprints", 188);
        SerializedProperty buttons = serialized.FindProperty("deploy");
        Button deploy = (Button)buttons.GetArrayElementAtIndex(0).objectReferenceValue;
        for (int index = 1; index < buttons.arraySize; index++)
        {
            ((Button)buttons.GetArrayElementAtIndex(index).objectReferenceValue).gameObject.SetActive(false);
        }

        buttons.arraySize = 1;
        Place((RectTransform)deploy.transform, 20, 124, 290, 48);
        Button refresh = commands.Find("Refresh Blueprints")?.GetComponent<Button>();
        if (refresh == null)
        {
            refresh = Object.Instantiate(deploy, commands);
            refresh.name = "Refresh Blueprints";
        }

        refresh.GetComponentInChildren<Text>().text = "REFRESH SAVES / ENEMY";
        Place((RectTransform)refresh.transform, 20, 240, 290, 32);
        serialized.FindProperty("defenderBlueprint").objectReferenceValue = defenders;
        serialized.FindProperty("attackerBlueprint").objectReferenceValue = attackers;
        serialized.FindProperty("refreshBlueprints").objectReferenceValue = refresh;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        var settings = new SerializedObject(AssetDatabase.LoadAssetAtPath<SiegeSettings>("Assets/Siege/DefaultSiege.asset"));
        settings.FindProperty("usePlayerBlueprintFolders").boolValue = true;
        settings.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static Dropdown CreateDropdown(Transform parent, string name, float y)
    {
        Dropdown dropdown = parent.Find(name)?.GetComponent<Dropdown>();
        if (dropdown == null)
        {
            GameObject root = DefaultControls.CreateDropdown(new DefaultControls.Resources());
            root.name = name;
            root.transform.SetParent(parent, false);
            dropdown = root.GetComponent<Dropdown>();
        }

        Place((RectTransform)dropdown.transform, 20, y, 290, 44);
        foreach (Text text in dropdown.GetComponentsInChildren<Text>(true))
        {
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/NotoSans-SemiBold.ttf");
            text.fontSize = 16;
            text.color = new Color(0.83f, 0.94f, 0.98f);
        }

        foreach (Image image in dropdown.GetComponentsInChildren<Image>(true))
        {
            image.color = new Color(0.08f, 0.28f, 0.33f);
        }

        return dropdown;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}

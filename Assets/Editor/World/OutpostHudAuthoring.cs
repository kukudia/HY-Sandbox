// 职责：为Main保存据点状态HUD，并在场景中禁用旧EnemySpawner。
// 入口：Apply()；只新增所属HUD，保留既有界面布局。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class OutpostHudAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Outposts/Install Main HUD")]
    public static void Apply()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Main.unity" || scene.isDirty)
        {
            throw new InvalidOperationException("Open clean Main in Edit Mode first.");
        }

        MainUIPanels panels = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MainUIPanels>(true)).Single();
        Transform parent = panels.playPanel.transform;
        if (parent.Find("OutpostStatus") == null)
        {
            var root = new GameObject("OutpostStatus", typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(root, "Add outpost HUD");
            root.layer = LayerMask.NameToLayer("UI");
            root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -82f);
            rect.sizeDelta = new Vector2(520f, 52f);
            Text label = root.AddComponent<Text>();
            label.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
            label.fontSize = 18;
            label.color = new Color(1f, 0.75f, 0.4f);
            label.alignment = TextAnchor.UpperCenter;
            label.raycastTarget = false;
            OutpostHud hud = root.AddComponent<OutpostHud>();
            var serialized = new SerializedObject(hud);
            serialized.FindProperty("_label").objectReferenceValue = label;
            serialized.FindProperty("_session").objectReferenceValue = UnityEngine.Object.FindFirstObjectByType<WorldSessionManager>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (EnemySpawner spawner in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemySpawner>(true)))
        {
            Undo.RecordObject(spawner, "Disable legacy enemy spawner");
            spawner.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

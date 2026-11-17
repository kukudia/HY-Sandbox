// 职责：迁移共享诊断抽屉并保存有界轨迹组件，保留现有HUD与各场景手工排版。
// 入口：Install()；Prefab改动向场景传播，轨迹池只制作在工坊服务场景。
using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CombatDiagnosticsAuthoring
{
    [MenuItem("Tools/HY-Sandbox/Combat/Install faction diagnostics and traces")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        GameObject root = PrefabUtility.LoadPrefabContents(SharedCombatHudAuthoring.PrefabPath);
        try
        {
            UpgradePanel(root.GetComponentInChildren<CombatHudPanels>(true));
            PrefabUtility.SaveAsPrefabAsset(root, SharedCombatHudAuthoring.PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        Scene scene = SceneManager.GetSceneByPath(MainMenuController.MainPath);
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(MainMenuController.MainPath, OpenSceneMode.Additive);
        }

        DebugManager manager = SceneObjectQuery.FindInScene<DebugManager>(scene);
        if (manager.GetComponent<CombatFlightTracks>() == null)
        {
            CombatFlightTracks pool = manager.gameObject.AddComponent<CombatFlightTracks>();
            var data = new SerializedObject(pool);
            SerializedProperty slots = data.FindProperty("tracks");
            slots.arraySize = 32;
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/NavigationDebugTrace.mat");
            for (int i = 0; i < slots.arraySize; i++)
            {
                var owner = new GameObject("Combat flight track " + (i + 1));
                owner.transform.SetParent(manager.transform, false);
                NavigationTrack track = owner.AddComponent<NavigationTrack>();
                var trackData = new SerializedObject(track);
                trackData.FindProperty("maximumPoints").intValue = 1024;
                trackData.FindProperty("traceLine").objectReferenceValue = Line(owner.transform, "Flight history", material, Color.white, .3f);
                trackData.FindProperty("routeLine").objectReferenceValue = Line(owner.transform, "Planned route", material, new Color(1f, .6f, 0f), .2f);
                trackData.ApplyModifiedPropertiesWithoutUndo();
                slots.GetArrayElementAtIndex(i).objectReferenceValue = track;
            }

            data.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void UpgradePanel(CombatHudPanels panel)
    {
        var data = new SerializedObject(panel);
        SerializedProperty toggles = data.FindProperty("debugToggles");
        Transform window = ((CanvasGroup)data.FindProperty("debugWindow").objectReferenceValue).transform;
        if (data.FindProperty("factionFilter").objectReferenceValue != null)
        {
            return;
        }

        Toggle trace = (Toggle)toggles.GetArrayElementAtIndex(5).objectReferenceValue;
        trace.name = "Flight trajectories";
        trace.GetComponentInChildren<TMP_Text>().text = "Flight trajectories + routes";
        if (toggles.arraySize > 6)
        {
            UnityEngine.Object.DestroyImmediate(((Toggle)toggles.GetArrayElementAtIndex(6).objectReferenceValue).gameObject);
        }

        toggles.arraySize = 6;
        Dropdown dropdown = DefaultControls.CreateDropdown(new DefaultControls.Resources()).GetComponent<Dropdown>();
        dropdown.name = "Diagnostic faction";
        RectTransform rect = dropdown.GetComponent<RectTransform>();
        rect.SetParent(window, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(18, -305);
        rect.sizeDelta = new Vector2(342, 36);
        dropdown.ClearOptions();
        dropdown.AddOptions(new System.Collections.Generic.List<string> { "ALL TEAMS", "FRIENDLY ONLY", "ENEMY ONLY" });
        foreach (Text text in dropdown.GetComponentsInChildren<Text>(true))
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.color = new Color(.08f, .14f, .2f);
        }

        data.FindProperty("factionFilter").objectReferenceValue = dropdown;
        data.ApplyModifiedPropertiesWithoutUndo();
        TMP_Text legend = window.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text.name == "Legend");
        if (legend != null)
        {
            legend.text = "History: red slow / cyan fast. Orange: planned route.";
        }
    }

    private static LineRenderer Line(Transform parent, string name, Material material, Color color, float width)
    {
        var owner = new GameObject(name, typeof(LineRenderer));
        owner.transform.SetParent(parent, false);
        LineRenderer line = owner.GetComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.widthMultiplier = width;
        line.startColor = line.endColor = color;
        line.positionCount = 0;
        line.enabled = false;
        return line;
    }
}

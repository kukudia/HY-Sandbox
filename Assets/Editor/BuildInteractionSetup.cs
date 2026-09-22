using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class BuildInteractionSetup
{
    [MenuItem("Tools/Build Palette/Apply range and health HUD update")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/Main.unity");
        if (EditorApplication.isPlaying || !scene.isLoaded || scene.isDirty)
            throw new InvalidOperationException("Open and save Main in Edit Mode before migrating the health HUD.");
        Directory.CreateDirectory("Temp/BuildRange");
        File.Copy(scene.path, "Temp/BuildRange/Main-before-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".unity");
        var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MainUIPanels>(true)).Single();
        var panel = (RectTransform)ui.transform.Find("PlayPanel/CockpitHealthBar");
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
        panel.anchoredPosition = new Vector2(28, 28); panel.sizeDelta = new Vector2(280, 58);
        Transform legacy = ui.transform.Find("PlayPanel/HeathBar");
        RectTransform bar = legacy != null ? (RectTransform)legacy : (RectTransform)panel.Find("HealthBar");
        if (bar == null) throw new InvalidOperationException("Expected the saved health bar or migrated HealthBar.");
        bar.SetParent(panel, false); bar.name = "HealthBar";
        bar.anchorMin = Vector2.zero; bar.anchorMax = new Vector2(1, 0); bar.pivot = Vector2.zero;
        bar.offsetMin = new Vector2(10, 10); bar.offsetMax = new Vector2(-110, 28);
        var oldScrollbar = bar.GetComponent<Scrollbar>();
        var mask = bar.GetComponent<RectMask2D>();
        if (oldScrollbar != null) Object.DestroyImmediate(oldScrollbar);
        if (mask != null) Object.DestroyImmediate(mask);
        Image fill = bar.Find("Fill")?.GetComponent<Image>() ?? bar.Find("Sliding Area/Handle")?.GetComponent<Image>();
        if (fill == null) throw new InvalidOperationException("Expected the authored health fill image.");
        fill.transform.SetParent(bar, false); fill.name = "Fill";
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = new Vector2(2, 2); fill.rectTransform.offsetMax = new Vector2(-2, -2);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0;
        // The legacy gradient was green at zero and red at one; migrate it to remaining-health semantics.
        if (ui.healthBarColor.Evaluate(0f).g > ui.healthBarColor.Evaluate(0f).r
            && ui.healthBarColor.Evaluate(1f).r > ui.healthBarColor.Evaluate(1f).g)
        {
            GradientColorKey[] colors = ui.healthBarColor.colorKeys;
            for (int i = 0; i < colors.Length; i++) colors[i].time = 1f - colors[i].time;
            Array.Reverse(colors);
            ui.healthBarColor.SetKeys(colors, ui.healthBarColor.alphaKeys);
        }
        fill.fillAmount = 1f; fill.color = ui.healthBarColor.Evaluate(1f); fill.raycastTarget = false;
        bar.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.055f, 0.9f);
        foreach (Graphic graphic in panel.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        if (bar.Find("Sliding Area") != null) Object.DestroyImmediate(bar.Find("Sliding Area").gameObject);
        var serialized = new SerializedObject(ui); serialized.FindProperty("_healthFill").objectReferenceValue = fill; serialized.ApplyModifiedPropertiesWithoutUndo();
        var manager = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BuildManager>(true)).Single();
        serialized = new SerializedObject(manager); serialized.FindProperty("_buildRange").floatValue = 15f; serialized.ApplyModifiedPropertiesWithoutUndo();
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/BuildPalette/ConnectorOutline.mat");
        BuildPaletteBaker.ConfigureHintMaterial(material); AssetDatabase.SaveAssetIfDirty(material);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("Saved 15-unit build range, fading hint material and anchored health HUD.");
    }
}

// 职责：统一自有uGUI下拉项的行高、文字留白和对比度，保存可继续编辑的模板。
// 入口：Apply() / UpgradeAll()；制作工具重用相同规则，避免重新生成后退回默认20px行高。
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DropdownTemplateAuthoring
{
    public static void Apply(Dropdown dropdown)
    {
        if (dropdown.template == null || dropdown.itemText == null)
        {
            return;
        }

        Text text = dropdown.itemText;
        text.fontSize = Mathf.Clamp(text.fontSize, 14, 22);
        text.color = new Color(.93f, .96f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = false;
        RectTransform row = (RectTransform)text.GetComponentInParent<Toggle>(true).transform;
        float height = Mathf.Max(36f, text.fontSize + 16f);
        row.sizeDelta = new Vector2(row.sizeDelta.x, height);
        RectTransform label = text.rectTransform;
        label.anchorMin = Vector2.zero;
        label.anchorMax = Vector2.one;
        label.offsetMin = new Vector2(28f, 3f);
        label.offsetMax = new Vector2(-8f, -3f);
        RectTransform content = (RectTransform)row.parent;
        content.sizeDelta = new Vector2(content.sizeDelta.x, height + 8f);
        dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, height * 6f + 8f);
        Image background = dropdown.template.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(.07f, .1f, .15f);
        }

        Toggle toggle = row.GetComponent<Toggle>();
        if (toggle.targetGraphic != null)
        {
            toggle.targetGraphic.color = Color.white;
        }

        ColorBlock colors = toggle.colors;
        colors.normalColor = new Color(.1f, .15f, .22f);
        colors.highlightedColor = new Color(.18f, .3f, .42f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.1f, .23f, .33f);
        colors.disabledColor = colors.normalColor;
        toggle.colors = colors;
        if (dropdown.captionText != null)
        {
            dropdown.captionText.resizeTextForBestFit = true;
            dropdown.captionText.resizeTextMinSize = 12;
            dropdown.captionText.resizeTextMaxSize = Mathf.Clamp(dropdown.captionText.fontSize, 14, 22);
        }
    }

    [MenuItem("Tools/HY-Sandbox/UI/Repair dropdown templates")]
    public static void UpgradeAll()
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Save scenes and exit Play Mode first.");
        }

        SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Dropdown[] controls = root.GetComponentsInChildren<Dropdown>(true);
                foreach (Dropdown dropdown in controls)
                {
                    Apply(dropdown);
                }

                if (controls.Length > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            Scene scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid), OpenSceneMode.Single);
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Dropdown dropdown in root.GetComponentsInChildren<Dropdown>(true))
                {
                    Apply(dropdown);
                    count++;
                }
            }

            if (count > 0)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        EditorSceneManager.RestoreSceneManagerSetup(setup);
    }
}

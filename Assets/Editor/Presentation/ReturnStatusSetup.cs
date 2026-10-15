// 职责：只在Main创建/绑定可编辑返航失败状态栏，不重排既有面板。
// 入口：Apply()；保存真实场景，保留已有状态栏的布局与样式。
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ReturnStatusSetup
{
    [MenuItem("Tools/HY-Sandbox/Setup Return Status")]
    public static void Apply()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/Main.unity" || scene.isDirty)
        {
            throw new InvalidOperationException("Requires clean Main in Edit Mode.");
        }

        MainUIPanels panels = UnityEngine.Object.FindFirstObjectByType<MainUIPanels>();
        Transform canvas = panels.GetComponentInParent<Canvas>().transform;
        Transform existing = canvas.Find("Return Status");
        ReturnStatusPanel status;
        if (existing == null)
        {
            var root = new GameObject(
                "Return Status",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(ReturnStatusPanel));
            Undo.RegisterCreatedObjectUndo(root, "Create return status");
            root.transform.SetParent(canvas, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -96f);
            rect.sizeDelta = new Vector2(760f, 104f);
            Image image = root.GetComponent<Image>();
            image.color = new Color(.12f, .045f, .025f, .97f);
            image.raycastTarget = false;
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = group.blocksRaycasts = false;
            var label = new GameObject("Message", typeof(RectTransform), typeof(Text));
            label.transform.SetParent(root.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(18f, 10f);
            labelRect.offsetMax = new Vector2(-18f, -10f);
            Text text = label.GetComponent<Text>();
            text.font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ChakraPetch-Medium.ttf");
            if (text.font == null)
            {
                throw new MissingReferenceException("Main interface font missing.");
            }

            text.fontSize = 20;
            text.color = new Color(1f, .87f, .73f);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            status = root.GetComponent<ReturnStatusPanel>();
            var statusData = new SerializedObject(status);
            statusData.FindProperty("_group").objectReferenceValue = group;
            statusData.FindProperty("_message").objectReferenceValue = text;
            statusData.ApplyModifiedProperties();
        }
        else
        {
            status = existing.GetComponent<ReturnStatusPanel>();
            if (status == null)
            {
                throw new InvalidOperationException("Return Status name is already in use by another object.");
            }
        }

        Undo.RecordObject(panels, "Bind return status");
        var data = new SerializedObject(panels);
        data.FindProperty("_returnStatus").objectReferenceValue = status;
        data.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

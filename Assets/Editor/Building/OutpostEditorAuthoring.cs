// 职责：为现有建筑编辑Prefab添加可编辑据点表单，保留底座尺寸控件与既有GUID。
// 入口：Upgrade()；通过PrefabUtility保存真实资产，重复执行不重复添加。
using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class OutpostEditorAuthoring
{
    [MenuItem("Tools/Building Editor/Add outpost editor controls")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode first.");
        }

        const string path = "Assets/Resources/BuildingEditor/EditorPanel.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform existingToggle = root.transform.Find("EnemyOutpost");
            if (existingToggle != null)
            {
                existingToggle.GetComponentInChildren<Text>().color = Color.white;
            }

            if (root.GetComponent<BuildingOutpostEditor>() != null)
            {
                AddTemplateCopy(root, root.GetComponent<BuildingOutpostEditor>());
                AddMatchingBase(root, root.GetComponent<BuildingOutpostEditor>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(root, "Add outpost editor");
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 450f);
            BuildingOutpostEditor editor = root.AddComponent<BuildingOutpostEditor>();
            GameObject toggleObject = DefaultControls.CreateToggle(new DefaultControls.Resources());
            Place(toggleObject, root.transform, "EnemyOutpost", new Vector2(400f, 30f), new Vector2(14f, -146f));
            Toggle toggle = toggleObject.GetComponent<Toggle>();
            toggleObject.GetComponentInChildren<Text>().text = "Enemy outpost (finite garrison)";
            toggleObject.GetComponentInChildren<Text>().color = Color.white;
            Dropdown blueprint = Dropdown(root.transform, "EnemyBlueprint", "Enemy blueprint", -180f);
            Dropdown defender = Dropdown(root.transform, "Defender", "Deployment slot", -216f);
            string[] labels =
            {
                "X",
                "Height",
                "Z",
                "Yaw"
            };
            string[] fields =
            {
                "_x",
                "_y",
                "_z",
                "_yaw"
            };
            var serialized = new SerializedObject(editor);
            for (int i = 0; i < labels.Length; i++)
            {
                float x = 14f + i * 100f;
                Label(root.transform, labels[i] + "Label", labels[i], new Vector2(92f, 22f), new Vector2(x, -254f));
                GameObject obj = DefaultControls.CreateInputField(new DefaultControls.Resources());
                Place(obj, root.transform, labels[i], new Vector2(92f, 30f), new Vector2(x, -278f));
                InputField input = obj.GetComponent<InputField>();
                input.contentType = InputField.ContentType.DecimalNumber;
                input.characterLimit = 8;
                serialized.FindProperty(fields[i]).objectReferenceValue = input;
            }

            Button add = Button(root.transform, "Add", "Add defender", 14f);
            Button save = Button(root.transform, "Pose", "Save pose", 150f);
            Button remove = Button(root.transform, "Remove", "Remove", 286f);
            Text status = Label(root.transform, "OutpostStatus", "", new Vector2(398f, 76f), new Vector2(14f, -356f));
            status.fontSize = 13;
            serialized.FindProperty("_isOutpost").objectReferenceValue = toggle;
            serialized.FindProperty("_blueprint").objectReferenceValue = blueprint;
            serialized.FindProperty("_defender").objectReferenceValue = defender;
            serialized.FindProperty("_status").objectReferenceValue = status;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            UnityEventTools.AddVoidPersistentListener(toggle.onValueChanged, editor.SetOutpost);
            UnityEventTools.AddVoidPersistentListener(defender.onValueChanged, editor.SelectDefender);
            UnityEventTools.AddPersistentListener(add.onClick, editor.AddDefender);
            UnityEventTools.AddPersistentListener(save.onClick, editor.SavePose);
            UnityEventTools.AddPersistentListener(remove.onClick, editor.RemoveDefender);
            AddTemplateCopy(root, editor);
            AddMatchingBase(root, editor);
            foreach (Text text in root.GetComponentsInChildren<Text>(true))
            {
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void AddTemplateCopy(GameObject root, BuildingOutpostEditor editor)
    {
        if (root.transform.Find("BuildingTemplate") != null)
        {
            return;
        }

        foreach (RectTransform child in root.transform)
        {
            if (child.anchoredPosition.y <= -146f)
            {
                child.anchoredPosition -= Vector2.up * 36f;
            }
        }

        ((RectTransform)root.transform).sizeDelta += Vector2.up * 36f;
        GameObject obj = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        Place(obj, root.transform, "BuildingTemplate", new Vector2(266f, 30f), new Vector2(14f, -146f));
        GameObject buttonObject = DefaultControls.CreateButton(new DefaultControls.Resources());
        Place(buttonObject, root.transform, "CopyTemplate", new Vector2(126f, 30f), new Vector2(286f, -146f));
        buttonObject.GetComponentInChildren<Text>().text = "Copy template";
        UnityEventTools.AddPersistentListener(buttonObject.GetComponent<Button>().onClick, editor.CopyBuildingTemplate);
        foreach (Text text in obj.GetComponentsInChildren<Text>(true))
        {
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        buttonObject.GetComponentInChildren<Text>().font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var serialized = new SerializedObject(editor);
        serialized.FindProperty("_buildingTemplate").objectReferenceValue = obj.GetComponent<Dropdown>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddMatchingBase(GameObject root, BuildingOutpostEditor editor)
    {
        if (root.transform.Find("MatchingBase") != null)
        {
            return;
        }

        float height = ((RectTransform)root.transform).sizeDelta.y;
        GameObject button = DefaultControls.CreateButton(new DefaultControls.Resources());
        Place(button, root.transform, "MatchingBase", new Vector2(398f, 30f), new Vector2(14f, -height + 4f));
        Text label = button.GetComponentInChildren<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "Create matching base for selected enemy";
        UnityEventTools.AddPersistentListener(button.GetComponent<Button>().onClick, editor.CreateMatchingBase);
        ((RectTransform)root.transform).sizeDelta += Vector2.up * 38f;
    }

    private static Dropdown Dropdown(Transform parent, string name, string caption, float y)
    {
        Label(parent, name + "Label", caption, new Vector2(128f, 30f), new Vector2(14f, y));
        GameObject obj = DefaultControls.CreateDropdown(new DefaultControls.Resources());
        Place(obj, parent, name, new Vector2(266f, 30f), new Vector2(146f, y));
        return obj.GetComponent<Dropdown>();
    }

    private static Button Button(Transform parent, string name, string caption, float x)
    {
        GameObject obj = DefaultControls.CreateButton(new DefaultControls.Resources());
        Place(obj, parent, name, new Vector2(126f, 32f), new Vector2(x, -318f));
        obj.GetComponentInChildren<Text>().text = caption;
        return obj.GetComponent<Button>();
    }

    private static Text Label(Transform parent, string name, string caption, Vector2 size, Vector2 position)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        Place(obj, parent, name, size, position);
        Text text = obj.AddComponent<Text>();
        text.text = caption;
        text.color = Color.white;
        text.fontSize = 14;
        text.raycastTarget = false;
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }

    private static void Place(GameObject obj, Transform parent, string name, Vector2 size, Vector2 position)
    {
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
}

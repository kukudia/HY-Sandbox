using System.Collections.Generic;

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BlockStatusSetup
{
    private const string BlocksFolder = "Assets/Resources/Blocks";
    private const string MainScenePath = "Assets/Scenes/Main.unity";

    private static readonly (string statusName, string iconPath)[] StatusIcons =
    {
        (nameof(ConnectionStatus.NoConnection), "Assets/Art/Icons/Status/broken-link.png"),
        (nameof(DurabilityStatus.Damaged), "Assets/Art/Icons/Status/shield.png"),
        (nameof(DurabilityStatus.Broken), "Assets/Art/Icons/Status/mark.png"),
        (nameof(PowerStatus.UnderPower), "Assets/Art/Icons/Status/warning.png"),
        (nameof(PowerStatus.NoPower), "Assets/Art/Icons/Status/energy.png"),
    };

    [MenuItem("Tools/HY-Sandbox/Setup Block Status UI")]
    public static void Apply()
    {
        int prefabCount = ConfigureBlockPrefabs();
        ConfigureMainScene();
        AssetDatabase.SaveAssets();
        Debug.Log($"Block status setup complete: {prefabCount} Block prefabs configured.");
    }

    private static int ConfigureBlockPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { BlocksFolder });
        int configuredCount = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Block block = root.GetComponent<Block>();
                if (block == null) continue;

                Info info = root.GetComponent<Info>();
                if (info == null)
                {
                    info = root.AddComponent<Info>();
                }

                info.blockName = root.name;
                info.statuses = BuildStatusList();
                EditorUtility.SetDirty(info);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                configuredCount++;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        return configuredCount;
    }

    private static List<Status> BuildStatusList()
    {
        var result = new List<Status>(StatusIcons.Length);
        for (int i = 0; i < StatusIcons.Length; i++)
        {
            (string statusName, string iconPath) = StatusIcons[i];
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (icon == null)
            {
                throw new MissingReferenceException($"Status icon could not be loaded as Sprite: {iconPath}");
            }

            result.Add(new Status
            {
                name = statusName,
                icon = icon
            });
        }

        return result;
    }

    private static void ConfigureMainScene()
    {
        Scene scene = SceneManager.GetSceneByPath(MainScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
        {
            scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Additive);
        }

        try
        {
            BlueprintUIPanel blueprintPanel = FindComponentInScene<BlueprintUIPanel>(scene);
            MainUIButtons mainButtons = FindComponentInScene<MainUIButtons>(scene);
            if (blueprintPanel == null || mainButtons == null)
            {
                throw new MissingReferenceException("Main scene is missing BlueprintUIPanel or MainUIButtons.");
            }

            ConfigureBlueprintPanel(blueprintPanel);
            ConfigureDebugPanel(mainButtons);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally
        {
            if (openedForSetup && scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    private static void ConfigureBlueprintPanel(BlueprintUIPanel panel)
    {
        panel.totalRequiredPower = GetOrCreateText(
            panel.transform,
            "TotalRequiredPower",
            panel.totalMass,
            "Required power: 0");
        panel.totalGeneratorOutput = GetOrCreateText(
            panel.transform,
            "TotalGeneratorOutput",
            panel.totalMass,
            "Generator output: 0");

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 190f);
        panelRect.anchoredPosition = new Vector2(
            panelRect.anchoredPosition.x,
            151.5f);
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(panelRect);
    }

    private static Text GetOrCreateText(Transform parent, string objectName, Text template, string initialText)
    {
        Transform existing = parent.Find(objectName);
        Text text = existing != null ? existing.GetComponent<Text>() : null;
        if (text == null)
        {
            GameObject clone = Object.Instantiate(template.gameObject, parent);
            clone.name = objectName;
            text = clone.GetComponent<Text>();
        }

        text.text = initialText;
        text.transform.SetAsLastSibling();
        EditorUtility.SetDirty(text);
        return text;
    }

    private static void ConfigureDebugPanel(MainUIButtons mainButtons)
    {
        Button template = mainButtons.showPowerConnectionsButton;
        if (template == null)
        {
            throw new MissingReferenceException("MainUIButtons.showPowerConnectionsButton is not assigned.");
        }

        Transform rowTemplate = template.transform;
        while (rowTemplate.parent != null && rowTemplate.parent.GetComponent<VerticalLayoutGroup>() == null)
        {
            rowTemplate = rowTemplate.parent;
        }

        if (rowTemplate.parent == null)
        {
            throw new MissingReferenceException("DebugSettingsPanel could not be resolved from its existing buttons.");
        }

        Transform debugPanel = rowTemplate.parent;
        mainButtons.showConnectionStatusButton = GetOrCreateButton(
            debugPanel,
            rowTemplate,
            "ConnectionStatusRow",
            "Show Connection Status");
        mainButtons.showDurabilityStatusButton = GetOrCreateButton(
            debugPanel,
            rowTemplate,
            "DurabilityStatusRow",
            "Show Durability Status");
        mainButtons.showPowerStatusButton = GetOrCreateButton(
            debugPanel,
            rowTemplate,
            "PowerStatusRow",
            "Show Power Status");

        VerticalLayoutGroup layout = debugPanel.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 3f;
        for (int i = 0; i < debugPanel.childCount; i++)
        {
            RectTransform rowRect = debugPanel.GetChild(i).GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, i == 0 ? 35f : 25f);
            EditorUtility.SetDirty(rowRect);
        }

        RectTransform panelRect = debugPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 190f);
        panelRect.anchoredPosition = new Vector2(
            panelRect.anchoredPosition.x,
            -235.03662f);
        EditorUtility.SetDirty(mainButtons);
        EditorUtility.SetDirty(layout);
        EditorUtility.SetDirty(panelRect);
    }

    private static Button GetOrCreateButton(Transform panel, Transform rowTemplate, string rowName, string label)
    {
        Transform row = panel.Find(rowName);
        if (row == null)
        {
            row = Object.Instantiate(rowTemplate.gameObject, panel).transform;
            row.name = rowName;
        }

        row.SetAsLastSibling();
        Button button = row.GetComponentInChildren<Button>(true);
        Text buttonText = row.GetComponentInChildren<Text>(true);
        if (button == null || buttonText == null)
        {
            throw new MissingReferenceException($"Debug button template is missing Button or Text: {rowTemplate.name}");
        }

        button.gameObject.name = rowName.Replace("Row", "Button");
        buttonText.text = label;
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(buttonText);
        return button;
    }

    private static T FindComponentInScene<T>(Scene scene) where T : Component
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null) return component;
        }

        return null;
    }
}

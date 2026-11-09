// 职责：在Main暴露建筑编辑、示例复制和唯一核心指定；战斗永不回写这些设计文件。
// 入口：ToggleEditor() / CopyStarter() / DesignateCore()；编辑尺寸和方块继续使用原建筑工具。
using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeBaseWorkshop : MonoBehaviour
{
    [SerializeField]
    private SiegeConstructionSettings settings;
    [SerializeField]
    private Button toggle;
    [SerializeField]
    private Button starter;
    [SerializeField]
    private Button core;
    [SerializeField]
    private Text status;
    [SerializeField]
    private Button hangar;
    private float nextRefresh;
    private string message;
    private void Start()
    {
        toggle.onClick.AddListener(ToggleEditor);
        starter.onClick.AddListener(CopyStarter);
        core.onClick.AddListener(DesignateCore);
        hangar?.onClick.AddListener(SelectHangar);
    }

    private bool Ready => BuildManager.instance != null && !BuildManager.instance.IsLoadingBlocks && PlayManager.instance != null && !PlayManager.instance.playMode && (PlayManager.instance.Modes == null || PlayManager.instance.Modes.State == GameModeState.Building);

    public void ToggleEditor()
    {
        if (Ready)
        {
            BuildManager.instance.ToggleBaseBlueprintBuildMode();
            message = null;
        }
    }

    public void SelectHangar()
    {
        if (Ready && BuildManager.instance.IsEditingBaseBlueprint)
        {
            BuildManager.instance.SetCurrentBlockResource("Hangars/Hangar");
            message = $"Place a hangar anywhere on the ground grid. Select it to resize; A/D or Q/E turn it. Maximum {HangarRules.MaximumCount}.";
        }
    }

    public void CopyStarter()
    {
        if (!Ready || !BuildManager.instance.IsEditingBaseBlueprint)
        {
            return;
        }

        TrySave(() =>
        {
            BlockDataList design = JsonUtility.FromJson<BlockDataList>(settings.Starter.text);
            string name = SaveManager.instance.SaveNewBuilding("Siege Base", design);
            BuildManager.instance.EnterBaseBlueprintBuildMode(name);
            message = "Starter copied to your own blueprint. Changes save automatically.";
        });
    }

    public void DesignateCore()
    {
        if (!Ready || !BuildManager.instance.IsEditingBaseBlueprint || !BuildManager.instance.HasBuildingBlueprint)
        {
            return;
        }

        Block selected = BuildManager.instance.selectedBlock;
        BlockDataList data = SaveManager.instance.cachedData;
        if (selected == null || selected.GetComponent<BuildingFoundation>() != null || selected.GetComponent<HangarModule>() != null || selected.GetComponent<Durability>() == null || !data.blocks.Exists(block => block.id == selected.uniqueId))
        {
            message = "Select a destructible building module, then mark it as the core.";
            return;
        }

        TrySave(() =>
        {
            // Write a new snapshot before updating the live cache so a failed write preserves its metadata.
            BlockDataList copy = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(data));
            copy.siegeCoreId = selected.uniqueId;
            string path = SaveManager.instance.GetBaseBlueprintPath(BuildManager.instance.CurrentBuildingBlueprintName);
            string pending = path + ".pending";
            File.WriteAllText(pending, JsonUtility.ToJson(copy, true));
            File.Replace(pending, path, null);
            data.siegeCoreId = copy.siegeCoreId;
            message = "Core set: " + selected.name + ". Destroying it ends the siege.";
        });
    }

    private void TrySave(Action action)
    {
        try
        {
            action();
        }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is InvalidOperationException || error is ArgumentException)
        {
            message = "Could not save: " + error.Message;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh || BuildManager.instance == null)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + 1f;
        bool editing = BuildManager.instance.IsEditingBaseBlueprint;
        GetComponent<Image>().enabled = editing;
        toggle.interactable = Ready;
        toggle.GetComponentInChildren<Text>().text = editing ? "BACK TO VEHICLE" : "BASE EDITOR";
        starter.gameObject.SetActive(editing);
        core.gameObject.SetActive(editing);
        if (hangar != null)
        {
            hangar.gameObject.SetActive(editing);
            hangar.interactable = Ready && BuildManager.instance.HasBuildingBlueprint;
        }

        starter.interactable = Ready;
        core.interactable = Ready && BuildManager.instance.HasBuildingBlueprint;
        string validation = string.Empty;
        if (editing && BuildManager.instance.HasBuildingBlueprint)
        {
            settings.Validate(SaveManager.instance.cachedData, out validation);
        }

        status.text = editing ? (message ?? "Select a module to designate its core. Exit to Simulate.") + "\n" + validation : string.Empty;
    }
}

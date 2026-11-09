// 职责：展示玩家/敌方/建筑蓝图存档列表并绑定选择动作。
// 入口：RefreshList()。
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveUIPanel : MonoBehaviour
{
    public static SaveUIPanel instance;
    public Transform listParent; // 存档按钮生成的父物体（比如 ScrollView Content）
    public GameObject savePrefab; // 存档按钮预制体
    private void Awake()
    {
        instance = this;
    }

    public void RefreshList()
    {
        // 清空旧按钮
        foreach (Transform child in listParent)
        {
            Destroy(child.gameObject);
        }

        if (BuildManager.instance.IsEditingBuildingBlueprint)
        {
            SaveManager.instance.GetAllStructureBlueprintNames();
            ShowEntries(SaveManager.instance.CurrentStructureNames, SaveManager.instance.LoadEditedStructure);
        }
        else if (BuildManager.instance.IsEditingEnemyBlueprint)
        {
            Debug.Log("Refreshing enemy blueprint list...");
            SaveManager.instance.GetAllEnemyBlueprintNames();
            ShowEntries(SaveManager.instance.enemyBlueprints, OnEnemyBlueprintClicked);
        }
        else
        {
            Debug.Log("Refreshing save list...");
            SaveManager.instance.GetAllSaveNames();
            ShowEntries(SaveManager.instance.saves, OnSaveClicked);
        }
    }

    private void ShowEntries(IEnumerable<string> names, System.Action<string> open)
    {
        foreach (string name in names)
        {
            GameObject item = Instantiate(savePrefab, listParent);
            ConfigureSaveItem(item, name, () => open(name));
        }
    }

    private void ConfigureSaveItem(GameObject obj, string saveName, UnityEngine.Events.UnityAction onOpen)
    {
        Button savePrefabButton = obj.transform.Find("SavePrefabButton").GetComponent<Button>();
        savePrefabButton.GetComponentInChildren<Text>().text = "\t" + saveName;
        savePrefabButton.onClick.AddListener(onOpen);
        Button renameButton = obj.transform.Find("RenameSaveButton").GetComponent<Button>();
        Button duplicateButton = obj.transform.Find("DuplicateSaveButton").GetComponent<Button>();
        Button deleteButton = obj.transform.Find("DeleteSaveButton").GetComponent<Button>();
        renameButton.onClick.RemoveAllListeners();
        renameButton.onClick.AddListener(() => MainUIPanels.instance.ShowRenamePanel(saveName));
        duplicateButton.onClick.RemoveAllListeners();
        duplicateButton.onClick.AddListener(() => OnDuplicateClicked(saveName));
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => MainUIPanels.instance.ShowDeletePanel(saveName));
        Text saveSizeText = obj.transform.Find("Size").GetComponent<Text>();
        saveSizeText.text = SaveManager.instance.GetSaveFileSize(saveName);
    }

    private void OnSaveClicked(string saveName)
    {
        SaveManager.instance.LoadSave(saveName);
    }

    private void OnEnemyBlueprintClicked(string blueprintName)
    {
        SaveManager.instance.LoadEnemyBlueprint(blueprintName);
    }

    private void OnDuplicateClicked(string saveName)
    {
        SaveManager.instance.DuplicateSave(saveName);
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveUIPanel : MonoBehaviour
{
    public static SaveUIPanel instance;
    public Transform listParent;   // 存档按钮生成的父物体（比如 ScrollView Content）
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

        if (BuildManager.instance.enemyBlueprintBuildMode)
        {
            Debug.Log("Refreshing enemy blueprint list...");
            SaveManager.instance.GetAllEnemyBlueprintNames();

            if (SaveManager.instance.enemyBlueprints.Count > 0)
            {
                foreach (string blueprint in SaveManager.instance.enemyBlueprints)
                {
                    GameObject obj = Instantiate(savePrefab, listParent);

                    ConfigureSaveItem(obj, blueprint, () => OnEnemyBlueprintClicked(blueprint));
                }
            }
        }
        else
        {
            Debug.Log("Refreshing save list...");
            SaveManager.instance.GetAllSaveNames();

            if (SaveManager.instance.saves.Count > 0)
            {
                foreach (string save in SaveManager.instance.saves)
                {
                    GameObject obj = Instantiate(savePrefab, listParent);

                    ConfigureSaveItem(obj, save, () => OnSaveClicked(save));
                }
            }
        }
    }

    private void ConfigureSaveItem(GameObject obj, string saveName, UnityEngine.Events.UnityAction onOpen)
    {
        Button savePrefabButton = obj.transform.Find("SavePrefabButton").GetComponent<Button>();
        savePrefabButton.GetComponentInChildren<Text>().text = "\t" + saveName;
        savePrefabButton.onClick.AddListener(onOpen);

        Button deleteButton = obj.transform.Find("DeleteSaveButton").GetComponent<Button>();
        Transform renameTransform = obj.transform.Find("RenameSaveButton");
        Button renameButton = renameTransform != null
            ? renameTransform.GetComponent<Button>()
            : CreateRenameButton(deleteButton, obj.transform);

        renameButton.onClick.RemoveAllListeners();
        renameButton.onClick.AddListener(() => MainUIPanels.instance.ShowRenamePanel(saveName));
        deleteButton.onClick.RemoveAllListeners();
        deleteButton.onClick.AddListener(() => MainUIPanels.instance.ShowDeletePanel(saveName));

        Text saveSizeText = obj.transform.Find("Size").GetComponent<Text>();
        saveSizeText.text = SaveManager.instance.GetSaveFileSize(saveName);
    }

    private Button CreateRenameButton(Button deleteButton, Transform parent)
    {
        Button renameButton = Instantiate(deleteButton, parent);
        renameButton.name = "RenameSaveButton";
        renameButton.onClick.RemoveAllListeners();

        RectTransform renameRect = renameButton.GetComponent<RectTransform>();
        RectTransform deleteRect = deleteButton.GetComponent<RectTransform>();
        renameRect.anchoredPosition = deleteRect.anchoredPosition + new Vector2(-55f, 0f);

        Text text = renameButton.GetComponentInChildren<Text>();
        if (text != null)
        {
            text.text = "R";
        }

        return renameButton;
    }

    private void OnSaveClicked(string saveName)
    {
        SaveManager.instance.LoadSave(saveName);
    }

    private void OnEnemyBlueprintClicked(string blueprintName)
    {
        BuildManager.instance.EnterEnemyBlueprintBuildMode(blueprintName);
    }

    //private void OnDeleteClicked(string saveName)
    //{
    //    SaveManager.instance.DeleteSave(saveName);
    //}
}

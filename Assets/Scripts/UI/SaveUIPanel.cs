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
            SaveManager.instance.GetAllEnemyBlueprintNames();

            if (SaveManager.instance.enemyBlueprints.Count > 0)
            {
                foreach (string blueprint in SaveManager.instance.enemyBlueprints)
                {
                    GameObject obj = Instantiate(savePrefab, listParent);

                    Button savePrefabButton = obj.transform.Find("SavePrefabButton").GetComponent<Button>();
                    savePrefabButton.GetComponentInChildren<Text>().text = "\t" + blueprint;
                    savePrefabButton.onClick.AddListener(() => OnEnemyBlueprintClicked(blueprint));

                    Button deleteButton = obj.transform.Find("DeleteSaveButton").GetComponent<Button>();
                    deleteButton.onClick.AddListener(() => MainUIPanels.instance.ShowDeletePanel(blueprint));

                    Text saveSizeText = obj.transform.Find("Size").GetComponent<Text>();
                    saveSizeText.text = SaveManager.instance.GetSaveFileSize(blueprint);
                }
            }
        }
        else
        {
            SaveManager.instance.GetAllSaveNames();

            if (SaveManager.instance.saves.Count > 0)
            {
                foreach (string save in SaveManager.instance.saves)
                {
                    GameObject obj = Instantiate(savePrefab, listParent);

                    Button savePrefabButton = obj.transform.Find("SavePrefabButton").GetComponent<Button>();
                    savePrefabButton.GetComponentInChildren<Text>().text = "\t" + save;
                    savePrefabButton.onClick.AddListener(() => OnSaveClicked(save));

                    Button deleteButton = obj.transform.Find("DeleteSaveButton").GetComponent<Button>();
                    deleteButton.onClick.AddListener(() => MainUIPanels.instance.ShowDeletePanel(save));

                    Text saveSizeText = obj.transform.Find("Size").GetComponent<Text>();
                    saveSizeText.text = SaveManager.instance.GetSaveFileSize(save);
                }
            }
        }
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

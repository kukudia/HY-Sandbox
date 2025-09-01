using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SaveUIPanel : MonoBehaviour
{
    public static SaveUIPanel instance;
    public Transform listParent;   // 存档按钮生成的父物体（比如 ScrollView Content）
    public GameObject savePrefab; // 存档按钮预制体
    private MainUIPanels mainUIPanels;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        mainUIPanels = transform.parent.GetComponentInParent<MainUIPanels>();
        RefreshList();
    }

    public void RefreshList()
    {
        // 清空旧按钮
        foreach (Transform child in listParent)
        {
            Destroy(child.gameObject);
        }

        SaveManager.instance.GetAllSaveNames();
        foreach (string save in SaveManager.instance.saves)
        {
            GameObject obj = Instantiate(savePrefab, listParent);

            Button savePrefabButton = obj.transform.Find("SavePrefabButton").GetComponent<Button>();
            savePrefabButton.GetComponentInChildren<Text>().text = "\t" + save;
            savePrefabButton.onClick.AddListener(() => OnSaveClicked(save));

            Button deleteButton = obj.transform.Find("DeleteSaveButton").GetComponent<Button>();
            deleteButton.onClick.AddListener(() => OnDeleteClicked(save));

            Text saveSizeText = obj.transform.Find("Size").GetComponent<Text>();
            saveSizeText.text = SaveManager.instance.GetSaveFileSize(save);
        }
    }

    private void OnSaveClicked(string saveName)
    {
        SaveManager.instance.LoadSave(saveName);
    }

    private void OnDeleteClicked(string saveName)
    {
        SaveManager.instance.DeleteSave(saveName);
    }
}

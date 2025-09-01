using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 存档管理器：负责创建、加载、删除存档，并通知 BuildManager 切换
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    private string saveDirectory => Path.Combine(Application.persistentDataPath, "Saves");

    public string currentSaveName = "default"; // 当前存档槽

    public List<string> saves = new List<string>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (!Directory.Exists(saveDirectory))
            Directory.CreateDirectory(saveDirectory);
    }

    /// <summary>
    /// 获取所有存档名字
    /// </summary>
    public void GetAllSaveNames()
    {
        saves.Clear();

        if (!Directory.Exists(saveDirectory))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(saveDirectory, "*.json"))
        {
            saves.Add(Path.GetFileNameWithoutExtension(file));
        }
    }

    /// <summary>
    /// 新建存档（如果已存在则覆盖）
    /// </summary>
    public void CreateNewSave(string saveName)
    {
        currentSaveName = saveName;
        string path = GetSavePath(saveName);

        // 清空 BuildManager 内的缓存和方块
        BuildManager.instance.cachedData = new BlockDataList();
        foreach (Transform child in BuildManager.instance.blocksParent)
            Destroy(child.gameObject);

        // 保存一个空存档文件
        File.WriteAllText(path, JsonUtility.ToJson(BuildManager.instance.cachedData, true));

        Debug.Log($"新建存档 {saveName}");

        SaveUIPanel.instance.RefreshList();
    }

    /// <summary>
    /// 切换存档并加载
    /// </summary>
    public void LoadSave(string saveName)
    {
        currentSaveName = saveName;

        Destroy(BuildManager.instance.blocksParent.gameObject);

        BuildManager.instance.cachedData = new BlockDataList();
        BuildManager.instance.currentSaveName = saveName;

        BuildManager.instance.LoadAllBlocks();

        Debug.Log($"切换到存档 {saveName}");
    }

    /// <summary>
    /// 删除指定存档
    /// </summary>
    public void DeleteSave(string saveName)
    {
        string path = GetSavePath(saveName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"删除存档 {saveName}");
        }
        SaveUIPanel.instance.RefreshList();
    }

    /// <summary>
    /// 获取存档文件路径
    /// </summary>
    public string GetSavePath(string saveName)
    {
        return Path.Combine(saveDirectory, saveName + ".json");
    }

    public string GetSaveFileSize(string saveName)
    {
        string path = SaveManager.instance.GetSavePath(saveName);
        if (File.Exists(path))
        {
            long bytes = new FileInfo(path).Length;
            if (bytes < 1024) return bytes + "b";
            else if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("F1") + "kb";
            else return (bytes / (1024f * 1024f)).ToString("F1") + "mb";
        }
        return "0 B";
    }
}

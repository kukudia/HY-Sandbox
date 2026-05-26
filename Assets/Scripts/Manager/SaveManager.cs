using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager instance;

    private string saveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
    private string enemyBlueprintDirectory => Path.Combine(Application.persistentDataPath, "EnemyBlueprints");

    public string currentSaveName;
    public string currentEnemyBlueprintName;

    public List<string> saves = new List<string>();
    public List<string> enemyBlueprints = new List<string>();

    public BlockDataList cachedData = new BlockDataList();

    public List<Block> blocks = new List<Block>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        EnsureSaveDirectories();
    }

    public void EnsureSaveDirectories()
    {
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        if (!Directory.Exists(enemyBlueprintDirectory))
        {
            Directory.CreateDirectory(enemyBlueprintDirectory);
        }
    }

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

        if (saves.Count == 0)
        {
            Debug.Log("saves.Count == 0");
            MainUIPanels.instance.ShowCreatePanel();
        }
    }

    public void GetAllEnemyBlueprintNames()
    {
        enemyBlueprints.Clear();
        //EnsureSaveDirectories();

        foreach (var file in Directory.GetFiles(enemyBlueprintDirectory, "*.json"))
        {
            enemyBlueprints.Add(Path.GetFileNameWithoutExtension(file));
        }
    }

    public void CreateNewSave(string saveName)
    {
        currentSaveName = saveName;
        string path = GetSavePath(saveName);

        cachedData = new BlockDataList();
        if (BuildManager.instance.blocksParent != null)
        {
            foreach (Transform child in BuildManager.instance.blocksParent)
            {
                Destroy(child.gameObject);
            }
        }

        File.WriteAllText(path, JsonUtility.ToJson(cachedData, true));

        Debug.Log($"New save created: {saveName}");

        SaveUIPanel.instance.RefreshList();
    }

    public void CreateNewEnemyBlueprint(string blueprintName)
    {
        currentEnemyBlueprintName = blueprintName;
        string path = GetEnemyBlueprintPath(blueprintName);

        if (!File.Exists(path))
        {
            File.WriteAllText(path, JsonUtility.ToJson(new BlockDataList(), true));
        }

        GetAllEnemyBlueprintNames();
        Debug.Log($"Enemy blueprint ready: {blueprintName}");
    }

    public void LoadSave(string saveName)
    {
        currentSaveName = saveName;

        ControlUnit[] deleteObjs = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None);

        foreach (ControlUnit obj in deleteObjs)
        {
            Destroy(obj.gameObject);
        }

        if (BuildManager.instance.blocksParent != null)
        {
            Destroy(BuildManager.instance.blocksParent.gameObject);
        }

        cachedData = new BlockDataList();
        BuildManager.instance.currentSaveName = saveName;
        BuildManager.instance.ExitEnemyBlueprintBuildMode(false);

        BuildManager.instance.LoadAllBlocks();

        Debug.Log($"Loaded save: {saveName}");
    }

    public void LoadEnemyBlueprint(string blueprintName)
    {
        
        Debug.Log($"Loaded enemy blueprint: {blueprintName}");
    }

    public void DeleteSave(string saveName)
    {
        string path = GetSavePath(saveName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Deleted save: {saveName}");
        }
        SaveUIPanel.instance.RefreshList();
    }

    public void DeleteEnemyBlueprint(string blueprintName)
    {
        string path = GetEnemyBlueprintPath(blueprintName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Deleted enemy blueprint: {blueprintName}");
        }
        SaveUIPanel.instance.RefreshList();
    }

    public string GetSavePath(string saveName)
    {
        EnsureSaveDirectories();
        return Path.Combine(saveDirectory, saveName + ".json");
    }

    public string GetEnemyBlueprintPath(string blueprintName)
    {
        EnsureSaveDirectories();
        return Path.Combine(enemyBlueprintDirectory, blueprintName + ".json");
    }

    public string GetSaveFileSize(string saveName)
    {
        string path;
        if (BuildManager.instance != null && BuildManager.instance.enemyBlueprintBuildMode)
        {
            path = GetEnemyBlueprintPath(saveName);
        }
        else
        {
            path = GetSavePath(saveName);
        }

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

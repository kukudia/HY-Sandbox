// 职责：管理玩家/敌方蓝图目录、名称和缓存，提供加载/复制/重命名入口。
// 入口：EnsureSaveDirectories() / GetAllSaveNames() / GetAllEnemyBlueprintNames()。
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    // Persists player saves and enemy blueprints as JSON under separate persistent-data directories.
    public static SaveManager instance;
    private string saveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
    private string enemyBlueprintDirectory => Path.Combine(Application.persistentDataPath, "EnemyBlueprints");

    private BlueprintFileStore _playerFiles;
    private BlueprintFileStore _enemyFiles;
    private BlueprintFileStore PlayerFiles => _playerFiles ?? (_playerFiles = new BlueprintFileStore(saveDirectory));
    private BlueprintFileStore EnemyFiles => _enemyFiles ?? (_enemyFiles = new BlueprintFileStore(enemyBlueprintDirectory));

    public string currentSaveName;
    public string currentEnemyBlueprintName;
    public List<string> saves = new List<string>();
    public List<string> enemyBlueprints = new List<string>();
    public BlockDataList cachedData = new BlockDataList();
    public List<Block> blocks = new List<Block>();
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        EnsureSaveDirectories();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
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
        saves.AddRange(PlayerFiles.GetNames());
        if (saves.Count == 0)
        {
            MainUIPanels.instance.ShowCreatePanel();
        }
    }

    public void GetAllEnemyBlueprintNames()
    {
        enemyBlueprints.Clear();
        enemyBlueprints.AddRange(EnemyFiles.GetNames());
        if (BuildManager.instance.IsEditingEnemyBlueprint && enemyBlueprints.Count == 0)
        {
            MainUIPanels.instance.ShowCreatePanel();
        }
    }

    public void CreateNewSave(string saveName)
    {
        currentSaveName = saveName;
        string path = GetSavePath(saveName);
        cachedData = new BlockDataList();
        if (GameManager.instance.blocksParent != null)
        {
            foreach (Transform child in GameManager.instance.blocksParent)
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
        if (BuildManager.instance != null && BuildManager.instance.IsLoadingBuildTarget(BuildTargetKind.PlayerSave, saveName, BuildManager.instance.currentEnemyBlueprintName))
        {
            Debug.Log($"Save {saveName} is already loading.");
            return;
        }

        currentSaveName = saveName;
        ControlUnit[] deleteObjs = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None);
        foreach (ControlUnit obj in deleteObjs)
        {
            if (GameManager.instance.blocksParent == null || obj.gameObject != GameManager.instance.blocksParent.gameObject)
            {
                Destroy(obj.gameObject);
            }
        }

        BuildManager.instance.SetCurrentSaveName(saveName);
        BuildManager.instance.ExitEnemyBlueprintBuildMode(false);
        BuildManager.instance.LoadAllBlocks();
        Debug.Log($"Loaded save: {saveName}");
    }

    public void LoadEnemyBlueprint(string blueprintName)
    {
        if (BuildManager.instance != null && BuildManager.instance.IsLoadingBuildTarget(BuildTargetKind.EnemyBlueprint, BuildManager.instance.currentSaveName, blueprintName))
        {
            Debug.Log($"Enemy blueprint {blueprintName} is already loading.");
            return;
        }

        currentEnemyBlueprintName = blueprintName;
        ControlUnit[] deleteObjs = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None);
        foreach (ControlUnit obj in deleteObjs)
        {
            if (GameManager.instance.blocksParent == null || obj.gameObject != GameManager.instance.blocksParent.gameObject)
            {
                Destroy(obj.gameObject);
            }
        }

        BuildManager.instance.EnterEnemyBlueprintBuildMode(blueprintName);
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

    public bool RenameSave(string oldSaveName, string newSaveName)
    {
        return RenameBlueprint(oldSaveName, newSaveName, false);
    }

    public bool RenameEnemyBlueprint(string oldBlueprintName, string newBlueprintName)
    {
        return RenameBlueprint(oldBlueprintName, newBlueprintName, true);
    }

    private bool RenameBlueprint(string oldName, string newName, bool enemy)
    {
        oldName = (oldName ?? string.Empty).Trim();
        newName = (newName ?? string.Empty).Trim();
        BlueprintFileStore files = enemy ? EnemyFiles : PlayerFiles;
        if (!files.TryRename(oldName, newName, out string reason))
        {
            Debug.LogWarning(reason);
            return false;
        }

        BuildManager build = BuildManager.instance;
        bool active = enemy ? currentEnemyBlueprintName == oldName || (build != null && build.currentEnemyBlueprintName == oldName) : currentSaveName == oldName || (build != null && build.currentSaveName == oldName);
        if (enemy)
        {
            if (currentEnemyBlueprintName == oldName)
            {
                currentEnemyBlueprintName = newName;
            }

            if (build != null && build.currentEnemyBlueprintName == oldName)
            {
                build.SetCurrentEnemyBlueprintName(newName);
            }
        }
        else
        {
            if (currentSaveName == oldName)
            {
                currentSaveName = newName;
            }

            if (build != null && build.currentSaveName == oldName)
            {
                build.SetCurrentSaveName(newName);
            }
        }

        if (active && GameManager.instance != null && GameManager.instance.blocksParent != null)
        {
            GameManager.instance.blocksParent.name = newName;
        }

        SaveUIPanel.instance.RefreshList();
        return true;
    }

    public void DuplicateSave(string saveName)
    {
        bool enemy = BuildManager.instance != null && BuildManager.instance.IsEditingEnemyBlueprint;
        BlueprintFileStore files = enemy ? EnemyFiles : PlayerFiles;
        if (!files.TryDuplicate((saveName ?? string.Empty).Trim(), out string copyName, out string reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        Debug.Log($"Duplicated blueprint: {saveName} -> {copyName}");
        SaveUIPanel.instance.RefreshList();
    }

    public string GetSavePath(string saveName)
    {
        return PlayerFiles.GetPath(saveName);
    }

    public string GetEnemyBlueprintPath(string blueprintName)
    {
        return EnemyFiles.GetPath(blueprintName);
    }

    public string GetSaveFileSize(string saveName)
    {
        string path;
        if (BuildManager.instance != null && BuildManager.instance.IsEditingEnemyBlueprint)
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
            if (bytes < 1024)
            {
                return bytes + "b";
            }
            else if (bytes < 1024 * 1024)
            {
                return (bytes / 1024f).ToString("F1") + "kb";
            }
            else
            {
                return (bytes / (1024f * 1024f)).ToString("F1") + "mb";
            }
        }

        return "0 B";
    }
}

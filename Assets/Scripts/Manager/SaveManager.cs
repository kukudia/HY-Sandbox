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
        EnsureSaveDirectories();

        foreach (var file in Directory.GetFiles(enemyBlueprintDirectory, "*.json"))
        {
            enemyBlueprints.Add(Path.GetFileNameWithoutExtension(file));
        }

        if (BuildManager.instance.IsEditingEnemyBlueprint && enemyBlueprints.Count == 0)
        {
            Debug.Log("enemyBlueprints.Count == 0");
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
        if (BuildManager.instance != null
            && BuildManager.instance.IsLoadingBuildTarget(BuildTargetKind.PlayerSave, saveName, BuildManager.instance.currentEnemyBlueprintName))
        {
            Debug.Log($"Save {saveName} is already loading.");
            return;
        }

        currentSaveName = saveName;

        ControlUnit[] deleteObjs = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None);

        foreach (ControlUnit obj in deleteObjs)
        {
            Destroy(obj.gameObject);
        }

        if (GameManager.instance.blocksParent != null)
        {
            Destroy(GameManager.instance.blocksParent.gameObject);
        }

        cachedData = new BlockDataList();
        BuildManager.instance.SetCurrentSaveName(saveName);
        BuildManager.instance.ExitEnemyBlueprintBuildMode(false);

        BuildManager.instance.LoadAllBlocks();

        Debug.Log($"Loaded save: {saveName}");
    }

    public void LoadEnemyBlueprint(string blueprintName)
    {
        if (BuildManager.instance != null
            && BuildManager.instance.IsLoadingBuildTarget(BuildTargetKind.EnemyBlueprint, BuildManager.instance.currentSaveName, blueprintName))
        {
            Debug.Log($"Enemy blueprint {blueprintName} is already loading.");
            return;
        }

        currentEnemyBlueprintName = blueprintName;

        ControlUnit[] deleteObjs = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None);

        foreach (ControlUnit obj in deleteObjs)
        {
            Destroy(obj.gameObject);
        }

        if (GameManager.instance.blocksParent != null)
        {
            Destroy(GameManager.instance.blocksParent.gameObject);
        }

        cachedData = new BlockDataList();
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
        oldSaveName = (oldSaveName ?? string.Empty).Trim();
        newSaveName = (newSaveName ?? string.Empty).Trim();

        if (!CanUseFileName(newSaveName, out string reason))
        {
            Debug.LogWarning(reason);
            return false;
        }

        if (string.Equals(oldSaveName, newSaveName, System.StringComparison.Ordinal))
        {
            SaveUIPanel.instance.RefreshList();
            return true;
        }

        string oldPath = GetSavePath(oldSaveName);
        string newPath = GetSavePath(newSaveName);

        if (!File.Exists(oldPath))
        {
            Debug.LogWarning($"Save not found: {oldSaveName}");
            return false;
        }

        if (File.Exists(newPath))
        {
            Debug.LogWarning($"Save already exists: {newSaveName}");
            return false;
        }

        bool renamedCurrentSave = currentSaveName == oldSaveName
            || (BuildManager.instance != null && BuildManager.instance.currentSaveName == oldSaveName);

        File.Move(oldPath, newPath);

        if (currentSaveName == oldSaveName)
        {
            currentSaveName = newSaveName;
        }

        if (BuildManager.instance != null && BuildManager.instance.currentSaveName == oldSaveName)
        {
            BuildManager.instance.SetCurrentSaveName(newSaveName);
        }

        if (renamedCurrentSave && GameManager.instance != null && GameManager.instance.blocksParent != null)
        {
            GameManager.instance.blocksParent.name = newSaveName;
        }

        Debug.Log($"Renamed save: {oldSaveName} -> {newSaveName}");
        SaveUIPanel.instance.RefreshList();
        return true;
    }

    public bool RenameEnemyBlueprint(string oldBlueprintName, string newBlueprintName)
    {
        oldBlueprintName = (oldBlueprintName ?? string.Empty).Trim();
        newBlueprintName = (newBlueprintName ?? string.Empty).Trim();

        if (!CanUseFileName(newBlueprintName, out string reason))
        {
            Debug.LogWarning(reason);
            return false;
        }

        if (string.Equals(oldBlueprintName, newBlueprintName, System.StringComparison.Ordinal))
        {
            SaveUIPanel.instance.RefreshList();
            return true;
        }

        string oldPath = GetEnemyBlueprintPath(oldBlueprintName);
        string newPath = GetEnemyBlueprintPath(newBlueprintName);

        if (!File.Exists(oldPath))
        {
            Debug.LogWarning($"Enemy blueprint not found: {oldBlueprintName}");
            return false;
        }

        if (File.Exists(newPath))
        {
            Debug.LogWarning($"Enemy blueprint already exists: {newBlueprintName}");
            return false;
        }

        bool renamedCurrentBlueprint = currentEnemyBlueprintName == oldBlueprintName
            || (BuildManager.instance != null && BuildManager.instance.currentEnemyBlueprintName == oldBlueprintName);

        File.Move(oldPath, newPath);

        if (currentEnemyBlueprintName == oldBlueprintName)
        {
            currentEnemyBlueprintName = newBlueprintName;
        }

        if (BuildManager.instance != null && BuildManager.instance.currentEnemyBlueprintName == oldBlueprintName)
        {
            BuildManager.instance.SetCurrentEnemyBlueprintName(newBlueprintName);
        }

        if (renamedCurrentBlueprint && GameManager.instance != null && GameManager.instance.blocksParent != null)
        {
            GameManager.instance.blocksParent.name = newBlueprintName;
        }

        Debug.Log($"Renamed enemy blueprint: {oldBlueprintName} -> {newBlueprintName}");
        SaveUIPanel.instance.RefreshList();
        return true;
    }

    public void DuplicateSave(string saveName)
    {

    }

    private bool CanUseFileName(string fileName, out string reason)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            reason = "Name cannot be empty.";
            return false;
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || fileName.EndsWith(" ")
            || fileName.EndsWith("."))
        {
            reason = $"Invalid file name: {fileName}";
            return false;
        }

        reason = string.Empty;
        return true;
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
            if (bytes < 1024) return bytes + "b";
            else if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("F1") + "kb";
            else return (bytes / (1024f * 1024f)).ToString("F1") + "mb";
        }
        return "0 B";
    }
}

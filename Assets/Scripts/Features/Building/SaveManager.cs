// 职责：管理玩家/敌方/建筑蓝图目录、名称和缓存，提供加载/复制/重命名入口。
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
    private string buildingBlueprintDirectory => Path.Combine(Application.persistentDataPath, "BuildingBlueprints");

    private BlueprintFileStore _buildingFiles;
    private BlueprintFileStore BuildingFiles => _buildingFiles ?? (_buildingFiles = new BlueprintFileStore(buildingBlueprintDirectory));

    public string currentBuildingBlueprintName;
    public List<string> buildingBlueprints = new List<string>();
    private BlueprintFileStore FilesFor(BuildTargetKind kind)
    {
        return kind == BuildTargetKind.BuildingBlueprint ? BuildingFiles : kind == BuildTargetKind.EnemyBlueprint ? EnemyFiles : PlayerFiles;
    }

    private BlueprintFileStore CurrentFiles => FilesFor(BuildManager.instance != null ? BuildManager.instance.CurrentBuildTarget : BuildTargetKind.PlayerSave);

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
        Directory.CreateDirectory(buildingBlueprintDirectory);
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        if (!Directory.Exists(enemyBlueprintDirectory))
        {
            Directory.CreateDirectory(enemyBlueprintDirectory);
        }
    }

    public void GetAllBuildingBlueprintNames()
    {
        buildingBlueprints.Clear();
        buildingBlueprints.AddRange(BuildingFiles.GetNames());
    }

    public void CreateNewBuildingBlueprint(string blueprintName)
    {
        currentBuildingBlueprintName = blueprintName;
        string path = GetBuildingBlueprintPath(blueprintName);
        if (!File.Exists(path))
        {
            var data = new BlockDataList
            {
                foundation = new BuildingFoundationData()
            };
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }

        GetAllBuildingBlueprintNames();
    }

    public void LoadBuildingBlueprint(string blueprintName)
    {
        BuildManager.instance.EnterBuildingBlueprintBuildMode(blueprintName);
    }

    public void DeleteBuildingBlueprint(string blueprintName)
    {
        string path = GetBuildingBlueprintPath(blueprintName);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        SaveUIPanel.instance.RefreshList();
    }

    public bool RenameBuildingBlueprint(string oldName, string newName)
    {
        return RenameBlueprint(oldName, newName, BuildTargetKind.BuildingBlueprint);
    }

    public string GetBuildingBlueprintPath(string blueprintName)
    {
        return BuildingFiles.GetPath(blueprintName);
    }

    public void SaveBuildingFoundation(BuildingFoundationData foundation)
    {
        // 新包装对象共享只读方块列表，写入成功后才更新缓存元数据。
        var data = new BlockDataList
        {
            blocks = cachedData.blocks,
            foundation = foundation,
            outpost = cachedData.outpost
        };
        File.WriteAllText(
            GetBuildingBlueprintPath(BuildManager.instance.CurrentBuildingBlueprintName),
            JsonUtility.ToJson(data, true));
        cachedData.foundation = foundation;
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

    public void SaveBuildingOutpost(EnemyOutpostData outpost)
    {
        if (BuildManager.instance == null || !BuildManager.instance.IsEditingBuildingBlueprint || BuildManager.instance.IsLoadingBlocks)
        {
            throw new System.InvalidOperationException("Wait until the building editor is ready.");
        }

        outpost?.Validate();
        BlockDataList copy = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(cachedData));
        copy.outpost = outpost;
        string path = GetBuildingBlueprintPath(BuildManager.instance.CurrentBuildingBlueprintName);
        string pending = path + ".pending";
        File.WriteAllText(pending, JsonUtility.ToJson(copy, true));
        if (File.Exists(path))
        {
            File.Replace(pending, path, null);
        }
        else
        {
            File.Move(pending, path);
        }

        cachedData.outpost = outpost;
    }

    public string CopyBundledBuilding(string template)
    {
        if (BuildManager.instance == null || !BuildManager.instance.IsEditingBuildingBlueprint || BuildManager.instance.IsLoadingBlocks)
        {
            throw new System.InvalidOperationException("Wait until the building editor is ready.");
        }

        TextAsset asset = Resources.Load<TextAsset>("WorldBlueprints/Buildings/" + template);
        if (asset == null || template.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new System.InvalidOperationException("Building template not found.");
        }

        BlockDataList data = JsonUtility.FromJson<BlockDataList>(asset.text);
        return SaveNewBuilding(template, data);
    }

    public string SaveNewBuilding(string template, BlockDataList data)
    {
        if (BuildManager.instance == null || !BuildManager.instance.IsEditingBuildingBlueprint || BuildManager.instance.IsLoadingBlocks)
        {
            throw new System.InvalidOperationException("Wait until the building editor is ready.");
        }

        WorldBuildingValidation.ValidateBlueprint(data);
        string name = template;
        for (int i = 1; File.Exists(GetBuildingBlueprintPath(name)); i++)
        {
            name = template + " (" + i + ")";
        }

        using (var stream = new FileStream(GetBuildingBlueprintPath(name), FileMode.CreateNew, FileAccess.Write))
        {
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(data, true));
            }
        }

        GetAllBuildingBlueprintNames();
        return name;
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

        BuildManager.instance.ExitDeveloperBlueprintBuildMode(false);
        BuildManager.instance.SetCurrentSaveName(saveName);
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
        return RenameBlueprint(oldSaveName, newSaveName, BuildTargetKind.PlayerSave);
    }

    public bool RenameEnemyBlueprint(string oldBlueprintName, string newBlueprintName)
    {
        return RenameBlueprint(oldBlueprintName, newBlueprintName, BuildTargetKind.EnemyBlueprint);
    }

    private bool RenameBlueprint(string oldName, string newName, BuildTargetKind kind)
    {
        oldName = (oldName ?? string.Empty).Trim();
        newName = (newName ?? string.Empty).Trim();
        if (!FilesFor(kind).TryRename(oldName, newName, out string reason))
        {
            Debug.LogWarning(reason);
            return false;
        }

        BuildManager build = BuildManager.instance;
        bool active = build != null && build.CurrentBuildTarget == kind && build.CurrentBuildName == oldName;
        if (kind == BuildTargetKind.BuildingBlueprint)
        {
            if (currentBuildingBlueprintName == oldName)
            {
                currentBuildingBlueprintName = newName;
            }

            if (build != null && build.CurrentBuildingBlueprintName == oldName)
            {
                build.SetCurrentBuildingBlueprintName(newName);
            }
        }
        else if (kind == BuildTargetKind.EnemyBlueprint)
        {
            if (currentEnemyBlueprintName == oldName)
            {
                currentEnemyBlueprintName = newName;
            }

            if (active)
            {
                build.SetCurrentEnemyBlueprintName(newName);
            }
            else if (build != null && build.currentEnemyBlueprintName == oldName)
            {
                build.currentEnemyBlueprintName = newName;
            }
        }
        else
        {
            if (currentSaveName == oldName)
            {
                currentSaveName = newName;
            }

            if (active)
            {
                build.SetCurrentSaveName(newName);
            }
            else if (build != null && build.currentSaveName == oldName)
            {
                build.currentSaveName = newName;
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
        BlueprintFileStore files = CurrentFiles;
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
        string path = CurrentFiles.GetPath(saveName);
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

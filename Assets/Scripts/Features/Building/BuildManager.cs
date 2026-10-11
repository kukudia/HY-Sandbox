// 职责：协调建造输入、选择与放置；蓝图加载委托 Building 功能，保留现有 Inspector 契约。
// 入口：SetBuildMode() / ToggleEnemyBlueprintBuildMode() / EnterEnemyBlueprintBuildMode()。
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    // Coordinates selection, grid-snapped edits, and asynchronous reconstruction of saved blocks.
    public static BuildManager instance;
    public Camera mainCamera;
    public LayerMask axisLayer;
    public LayerMask blockLayer; // 方块所在的层
    public GameObject moveAxis;
    public GameObject rotateAxis;
    public GameObject blocksParentPrefab;
    // 网格参数
    public float gridSize = 1f;
    [SerializeField]
    private ConnectorPlacementHints _connectorHints;
    [SerializeField, Min(0.1f)]
    private float _buildRange = 15f;
    public float BuildRange => Mathf.Max(0.1f, _buildRange);

    public Material highlightMaterial;
    [Header("Current")]
    [UnityEngine.Serialization.FormerlySerializedAs("currentSelectType")]
    public BuildType currentBuildType;
    public Block selectedBlock;
    public Block lastSaveBlock;
    public GameObject currentGhost; // 当前的 ghost 实例
    public string currentBlockResourcePath;
    public string currentSaveName = "default";
    public string currentEnemyBlueprintName = "default_enemy";
    public bool enemyBlueprintBuildMode;
    public Connector hoveredConnector; // 鼠标当前指向的连接点
    private BuildTargetContext CurrentBuildContext => enemyBlueprintBuildMode ? BuildTargetContext.EnemyBlueprint(currentSaveName, currentEnemyBlueprintName) : BuildTargetContext.PlayerSave(currentSaveName, currentEnemyBlueprintName);
    private string savePath => CurrentBuildContext.GetSavePath(SaveManager.instance);
    public bool IsEditingEnemyBlueprint => CurrentBuildContext.IsEnemyBlueprint;
    public BuildTargetKind CurrentBuildTarget => CurrentBuildContext.Kind;
    public UnitFaction CurrentBuildFaction => CurrentBuildContext.Faction;
    public string CurrentBuildName => CurrentBuildContext.Name;

    public bool penetrationMode;
    [Header("Blueprint Loading")]
    [Min(0f), Tooltip("Initial delay between blocks. The delay decreases as this load progresses.")]
    public float BlockLoadIntervalSeconds = 0.25f;
    [SerializeField, Min(0f), Tooltip("Lower limit for the delay between blocks, in seconds.")]
    private float _minimumBlockLoadIntervalSeconds = 0.005f;
    [SerializeField, Min(0.01f), Tooltip("Seconds of loading time required to halve the initial delay.")]
    private float _blockLoadIntervalHalfLifeSeconds = 2f;
    [SerializeField, Min(0.001f), Tooltip("Camera orbit reference duration per block, independent of the accelerating load delay.")]
    private float _loadCameraReferenceSecondsPerBlock = 0.1f;
    public bool moveCameraDuringBlockLoad = true;
    public int minBlocksForLoadCameraOrbit = 5;
    public float blockLoadCameraMoveDuration = 0.35f;
    public float blockLoadCameraOrbitPitch = 25f;
    public float blockLoadCameraOrbitRadiusVariation = 0.25f;
    public float blockLoadCameraOrbitRadiusWaveDegrees = 120f;
    private BlueprintLoader _blueprintLoader;
    public bool IsLoadingBlocks => _blueprintLoader != null && _blueprintLoader.IsLoading;

    private void Awake()
    {
        instance = this;
    }

    private void OnDisable()
    {
        ClearCurrentGhost();
    }

    private void OnDestroy()
    {
        // Do not assemble new dependencies while scene objects are being destroyed.
        _preview?.Clear();
        _selection?.DeselectBlock();
        if (_blueprintLoader != null)
        {
            _blueprintLoader.StatisticsChanged -= DisplayBlueprintStatistics;
            _blueprintLoader.Cancel();
        }

        if (instance == this)
        {
            instance = null;
        }
    }

    private void Start()
    {
        SetBuildMode(true);
        SetBuildType(BuildType.Select);
    }

    private void Update()
    {
        EnsureBuildInteraction();
        if (IsLoadingBlocks)
        {
            return;
        }

        if (InputManager.instance.lockView)
        {
            if (currentBuildType != BuildType.Build)
            {
                if (currentGhost != null)
                {
                    ClearCurrentGhost();
                }

                _selection.Poll();
                selectedBlock = _selection.selectedBlock;
            }
            else
            {
                HandleBuildingPreview();
            }
        }

        if (InputManager.instance.lockView && selectedBlock != null)
        {
            _manipulation.Poll(selectedBlock, currentBuildType);
            selectedBlock = _selection.selectedBlock;
        }

        // 删除
        if (selectedBlock != null)
        {
            MainUIButtons.instance.deleteButton.gameObject.SetActive(true);
            if (Keyboard.current.deleteKey.wasPressedThisFrame)
            {
                DeleteBlock();
            }
        }
        else
        {
            MainUIButtons.instance.deleteButton.gameObject.SetActive(false);
        }

        // 撤销重做
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame)
        {
            ActionManager.instance.Undo();
        }

        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.yKey.wasPressedThisFrame)
        {
            ActionManager.instance.Redo();
        }
    }

    public void SetBuildMode(bool lockView)
    {
        EnsureBuildInteraction();
        _selection.SetGizmosEnabled(lockView);
        if (!lockView)
        {
            // 切换视角只取消拖拽/预览，保留选中方块、类型和待搭建资源。
            _manipulation.CancelDrag();
            ClearCurrentGhost();
        }
    }

    public void ToggleEnemyBlueprintBuildMode()
    {
        if (IsEditingEnemyBlueprint)
        {
            ExitEnemyBlueprintBuildMode(true);
        }
        else
        {
            EnterEnemyBlueprintBuildMode(currentEnemyBlueprintName);
        }
    }

    public void EnterEnemyBlueprintBuildMode(string blueprintName)
    {
        if (!InputManager.instance.DeveloperToolsAvailable)
        {
            Debug.LogWarning("Enemy blueprint build mode is developer-only.");
            return;
        }

        if (PlayManager.instance != null && PlayManager.instance.playMode)
        {
            Debug.LogWarning("Cannot enter enemy blueprint build mode during play mode.");
            return;
        }

        BuildTargetContext targetContext = BuildTargetContext.EnemyBlueprint(currentSaveName, blueprintName);
        if (IsLoadingBuildTarget(targetContext))
        {
            Debug.Log($"Enemy blueprint {targetContext.EnemyBlueprintName} is already loading.");
            return;
        }

        SetBuildTarget(targetContext);
        currentBlockResourcePath = string.Empty;
        ResetBuildState();
        MainUIPanels.instance.EnterEnemyBlueprintBuildMode();
        SaveManager.instance.CreateNewEnemyBlueprint(currentEnemyBlueprintName);
        LoadAllBlocks();
        Debug.Log($"Entered enemy blueprint build mode: {currentEnemyBlueprintName}");
    }

    public void ExitEnemyBlueprintBuildMode(bool reloadPlayerSave)
    {
        if (!IsEditingEnemyBlueprint)
        {
            return;
        }

        SetBuildTarget(BuildTargetContext.PlayerSave(currentSaveName, currentEnemyBlueprintName));
        currentBlockResourcePath = string.Empty;
        ResetBuildState();
        if (reloadPlayerSave && SaveManager.instance != null && !string.IsNullOrEmpty(currentSaveName))
        {
            SaveManager.instance.LoadSave(currentSaveName);
        }

        MainUIPanels.instance.ExitEnemyBlueprintBuildMode();
        Debug.Log("Exited enemy blueprint build mode.");
    }

    public void SetCurrentBlockResource(string resourcePath)
    {
        currentBlockResourcePath = resourcePath ?? string.Empty;
        if (!string.IsNullOrEmpty(currentBlockResourcePath))
        {
            SetBuildType(BuildType.Build);
        }
        else if (currentBuildType == BuildType.Build)
        {
            SetBuildType(BuildType.Select);
        }

        ClearCurrentGhost();
    }

    public void SetCurrentSaveName(string saveName)
    {
        SetBuildTarget(BuildTargetContext.PlayerSave(saveName, currentEnemyBlueprintName));
    }

    public void SetCurrentEnemyBlueprintName(string blueprintName)
    {
        SetBuildTarget(BuildTargetContext.EnemyBlueprint(currentSaveName, blueprintName));
    }

    public void SetBuildType(BuildType buildType)
    {
        currentBuildType = buildType;
        if (buildType != BuildType.Build)
        {
            currentBlockResourcePath = string.Empty;
            ClearCurrentGhost();
        }
        else
        {
            DeselectBlock();
        }

        EnsureBuildInteraction();
        _manipulation.CancelDrag();
        _selection.currentBuildType = buildType;
        _selection.RefreshBuildGizmos();
        if (MainUIButtons.instance != null)
        {
            MainUIButtons.instance.RefreshBuildModeButtons(buildType);
        }
    }

    private BuildPlacementGeometry _geometry;
    private BuildPreview _preview;
    private BuildManipulation _manipulation;
    private BuildSelection _selection;
    private void EnsureBuildInteraction()
    {
        if (_geometry == null)
        {
            _geometry = new BuildPlacementGeometry();
            _preview = new BuildPreview(_geometry, CreateBlock);
            _manipulation = new BuildManipulation(_geometry, SaveBlock, CreateDuplicateBlock);
            _selection = new BuildSelection(_manipulation);
            _manipulation.SelectionChanged += _selection.SelectBlock;
        }

        _geometry.gridSize = gridSize;
        _geometry.blockLayer = blockLayer;
        _preview.Configure(mainCamera, BuildRange, highlightMaterial, _connectorHints);
        _preview.ResourcePath = currentBlockResourcePath;
        _preview.PenetrationMode = penetrationMode;
        _manipulation.Configure(mainCamera, moveAxis, rotateAxis);
        _selection.Configure(
            mainCamera,
            axisLayer,
            blockLayer,
            highlightMaterial,
            moveAxis,
            rotateAxis);
        _selection.currentBuildType = currentBuildType;
    }

    private void HandleBuildingPreview()
    {
        EnsureBuildInteraction();
        _preview.Poll();
        currentGhost = _preview.currentGhost;
        hoveredConnector = _preview.hoveredConnector;
    }

    private void ClearCurrentGhost(bool hideHints = true)
    {
        EnsureBuildInteraction();
        _preview.Clear(hideHints);
        currentGhost = null;
        hoveredConnector = null;
    }

    public void DeselectBlock()
    {
        EnsureBuildInteraction();
        _selection.DeselectBlock();
        selectedBlock = null;
    }

    public Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)
    {
        EnsureBuildInteraction();
        return _geometry.SnapCenterByMinCorner(targetCenter, targetRotation, b);
    }

    public bool IsWithinBuildRange(Vector3 position)
    {
        return mainCamera != null && (position - mainCamera.transform.position).sqrMagnitude <= BuildRange * BuildRange;
    }

    public void CreateBlock(GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot)
    {
        if (!CanCreateBlock(prefab, out string reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, pos, rot);
            obj.transform.parent = GameManager.instance.blocksParent;
            Block block = obj.GetComponent<Block>();
            block.resourcePath = resourcePath;
            ApplyBlockBuildDefaults(block);
            SaveBlock(block);
            VisualEffectsManager.TryPlayBlockPlaced(block);
            // 记录到 Undo 栈
            var action = new CreateBlockAction(block);
            ActionManager.instance.Push(action);
        }
        else
        {
            Debug.LogWarning($"Prefab not found at Resources/{resourcePath}");
        }
    }

    public void DeleteBlock()
    {
        if (selectedBlock == null)
        {
            return;
        }

        if (selectedBlock.GetComponent<Cockpit>() != null && CountCockpitsInCurrentConstruct() <= 1)
        {
            Debug.LogWarning("Cannot delete the last Cockpit. A modular unit must have exactly one Cockpit.");
            return;
        }

        var action = new DeleteBlockAction(selectedBlock);
        ActionManager.instance.Push(action);
        action.Redo(); // 执行删除
        // 4. 如果删除的是当前选中的方块，清空选中状态
        DeselectBlock();
    }

    private BuildBlueprintEditor _editor;
    private void EnsureBlueprintEditor()
    {
        EnsureBuildInteraction();
        if (_editor == null)
        {
            _editor = new BuildBlueprintEditor(SaveManager.instance, _geometry);
        }
    }

    private Block CreateDuplicateBlock(GameObject prefab, string path, Vector3 position, Quaternion rotation)
    {
        Block previous = lastSaveBlock;
        CreateBlock(prefab, path, position, rotation);
        return lastSaveBlock != previous ? lastSaveBlock : null;
    }

    public void SaveBlock(Block block)
    {
        if (block == null || SaveManager.instance == null)
        {
            return;
        }

        EnsureBlueprintEditor();
        lastSaveBlock = block;
        _editor.Save(block, savePath, CurrentBuildFaction);
        RefreshBlueprintUI();
    }

    public void RemoveBlock(Block block)
    {
        EnsureBlueprintEditor();
        _editor.Remove(block, savePath);
        RefreshBlueprintUI();
    }

    /// <summary>蓝图加载入口；文件、逐帧恢复和统计在 BlueprintLoader 中审查。</summary>
    public void LoadAllBlocks()
    {
        if (!IsEditingEnemyBlueprint && currentSaveName == String.Empty && SaveManager.instance.saves.Count > 0)
        {
            currentSaveName = SaveManager.instance.saves[0];
        }

        EnsureBlueprintLoader();
        if (_blueprintLoader.IsLoadingTarget(CurrentBuildContext))
        {
            return;
        }

        DeselectBlock();
        ClearCurrentGhost();
        _blueprintLoader.Load(CurrentBuildContext, blocksParentPrefab, CreateLoadSettings(), mainCamera, InitialBlock);
    }

    public BlueprintStatistics GetBlueprintStatistics()
    {
        return BlueprintStatistics.Calculate(
            SaveManager.instance?.cachedData?.blocks,
            GameManager.instance != null ? GameManager.instance.blocksParent : null);
    }

    private void EnsureBlueprintLoader()
    {
        if (_blueprintLoader == null)
        {
            _blueprintLoader = new BlueprintLoader(this, SaveManager.instance, GameManager.instance, PlayManager.instance);
            _blueprintLoader.StatisticsChanged += DisplayBlueprintStatistics;
        }
    }

    private void DisplayBlueprintStatistics(BlueprintStatistics statistics)
    {
        if (BlueprintUIPanel.instance != null)
        {
            BlueprintUIPanel.instance.DisplayStatistics(statistics);
        }
    }

    private BlueprintLoadSettings CreateLoadSettings()
    {
        return new BlueprintLoadSettings
        {
            InitialInterval = BlockLoadIntervalSeconds,
            MinimumInterval = _minimumBlockLoadIntervalSeconds,
            IntervalHalfLife = _blockLoadIntervalHalfLifeSeconds,
            GridSize = gridSize,
            MoveCamera = moveCameraDuringBlockLoad,
            MinimumOrbitBlocks = minBlocksForLoadCameraOrbit,
            CameraReferenceSecondsPerBlock = _loadCameraReferenceSecondsPerBlock,
            CameraMoveDuration = blockLoadCameraMoveDuration,
            CameraPitch = blockLoadCameraOrbitPitch,
            CameraRadiusVariation = blockLoadCameraOrbitRadiusVariation,
            CameraRadiusWaveDegrees = blockLoadCameraOrbitRadiusWaveDegrees
        };
    }

    public void ClearUnloadableData(string id)
    {
        ClearUnloadableData(id, savePath);
    }

    public bool IsLoadingBuildTarget(BuildTargetKind kind, string saveName, string enemyBlueprintName)
    {
        BuildTargetContext context = kind == BuildTargetKind.EnemyBlueprint ? BuildTargetContext.EnemyBlueprint(saveName, enemyBlueprintName) : BuildTargetContext.PlayerSave(saveName, enemyBlueprintName);
        return IsLoadingBuildTarget(context);
    }

    private bool IsLoadingBuildTarget(BuildTargetContext context)
    {
        return _blueprintLoader != null && _blueprintLoader.IsLoadingTarget(context);
    }

    private void ClearUnloadableData(string id, string targetSavePath)
    {
        EnsureBlueprintEditor();
        if (_editor.RemoveData(id, targetSavePath))
        {
            Debug.Log($"Removed unload data {id}");
        }
    }

    private void InitialBlock()
    {
        string cockpitResourcePath = ConvertToResourcesPath("Assets/Resources/Blocks/Cockpit.prefab");
        GameObject prefab = Resources.Load<GameObject>(cockpitResourcePath);
        CreateBlock(prefab, cockpitResourcePath, Vector3.zero, Quaternion.identity);
    }

    // 轴对齐方块的精确吸附：先对齐最小角，再还原中心
    private bool CanCreateBlock(GameObject prefab, out string reason)
    {
        reason = string.Empty;
        if (IsEditingEnemyBlueprint && !InputManager.instance.DeveloperToolsAvailable)
        {
            reason = "Enemy blueprint build mode is developer-only.";
            return false;
        }

        if (prefab == null)
        {
            reason = "Prefab is missing.";
            return false;
        }

        if (prefab.GetComponent<Cockpit>() != null && CountCockpitsInCurrentConstruct() >= 1)
        {
            reason = "Cannot place another Cockpit. A modular unit must have exactly one Cockpit.";
            return false;
        }

        return true;
    }

    public void ApplyBlockBuildDefaults(Block block)
    {
        BlueprintBlockFactory.ApplyFaction(block, CurrentBuildFaction);
    }

    private int CountCockpitsInCurrentConstruct()
    {
        if (GameManager.instance.blocksParent == null)
        {
            return 0;
        }

        return GameManager.instance.blocksParent.GetComponentsInChildren<Cockpit>(true).Length;
    }

    private void ResetBuildState()
    {
        DeselectBlock();
        ClearCurrentGhost();
        if (ActionManager.instance != null)
        {
            ActionManager.instance.Clear();
        }
    }

    private void SetBuildTarget(BuildTargetContext context)
    {
        StopActiveBlockLoad();
        enemyBlueprintBuildMode = context.IsEnemyBlueprint;
        currentSaveName = context.SaveName;
        currentEnemyBlueprintName = context.EnemyBlueprintName;
        if (BlueprintUIPanel.instance != null)
        {
            BlueprintUIPanel.instance.UpdateCurrentSaveName(CurrentBuildName);
        }
    }

    private void RefreshBlueprintUI()
    {
        if (BlueprintUIPanel.instance != null)
        {
            BlueprintUIPanel.instance.Refresh();
        }
    }

    private void StopActiveBlockLoad()
    {
        _blueprintLoader?.Cancel();
    }

    public static string ConvertToResourcesPath(string fullPath)
    {
        return BlueprintBlockFactory.ToResourcePath(fullPath);
    }
}

public enum BuildType
{
    Select = 0,
    Move = 1,
    Rotate = 2,
    Build = 3
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    // Coordinates selection, grid-snapped edits, and asynchronous reconstruction of saved blocks.
    public static BuildManager instance;
    public Camera mainCamera;
    public LayerMask axisLayer;
    public LayerMask blockLayer;   // 方块所在的层

    public GameObject moveAxis;
    public GameObject rotateAxis;
    public GameObject blocksParentPrefab;

    private readonly Dictionary<Renderer, Material[]> _selectedMaterials = new Dictionary<Renderer, Material[]>();

    private MoveAxisHandle activeHandle = null;
    private Vector3 dragStartPos;
    private Vector3 blockStartPos;

    private RotateAxisHandle activeRotateHandle = null;
    private Vector3 rotateDragStart;
    private Quaternion blockStartRot;


    // 网格参数
    public float gridSize = 1f;
    private Vector3 gridOrigin = Vector3.zero;

    [SerializeField] private ConnectorPlacementHints _connectorHints;
    [SerializeField, Min(0.1f)] private float _buildRange = 15f;
    public float BuildRange => Mathf.Max(0.1f, _buildRange);
    public Material highlightMaterial;
    private float moveStep = 1f;    // 移动步长

    [Header("Current")]
    public SelectType currentSelectType;
    public Block selectedBlock;
    public Block lastSaveBlock;
    public GameObject currentGhost;        // 当前的 ghost 实例
    public string currentBlockResourcePath;
    public string currentSaveName = "default";
    public string currentEnemyBlueprintName = "default_enemy";
    public bool enemyBlueprintBuildMode;
    public Connector hoveredConnector;     // 鼠标当前指向的连接点

    private Vector3 axisForward;
    private Vector3 axisRight;
    private Vector3 axisUp;

    private Vector3[] dirs = new Vector3[]
    {
        Vector3.right,
        -Vector3.right,
        Vector3.up,
        -Vector3.up,
        Vector3.forward,
        -Vector3.forward
    };

    private BuildTargetContext CurrentBuildContext => enemyBlueprintBuildMode
        ? BuildTargetContext.EnemyBlueprint(currentSaveName, currentEnemyBlueprintName)
        : BuildTargetContext.PlayerSave(currentSaveName, currentEnemyBlueprintName);

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
    private Coroutine loadAllBlocksCoroutine;
    private int loadAllBlocksVersion;
    private BuildTargetContext loadingBuildTarget;
    private string loadingBuildSavePath = string.Empty;
    private CameraController loadingCameraController;
    private float loadingCameraOrbitAngle;
    private Vector3 loadingCameraOrbitCenter;
    private Bounds loadingCameraOrbitBounds;
    private bool hasLoadingCameraOrbitBounds;

    public bool IsLoadingBlocks { get; private set; }

    private void Awake()
    {
        instance = this;
    }

    private void OnDisable() { ClearCurrentGhost(); }

    private void Start()
    {
        SetBuildMode(true);
    }

    private void Update()
    {
        if (IsLoadingBlocks)
        {
            return;
        }

        if (InputManager.instance.lockView)
        {
            if (currentBlockResourcePath == string.Empty)
            {
                if (currentGhost != null)
                {
                    ClearCurrentGhost();
                }

                HandleSelection();
            }
            else
            {
                HandleBuildingPreview();
            }
        }

        if (InputManager.instance.lockView && selectedBlock != null)
        {
            AlignAxisToNearestWorldDir();

            if (currentSelectType == SelectType.Move)
            {
                HandleMovement();
                HandleMoveAxisDrag();
            }
            else if (currentSelectType == SelectType.Rotate)
            {
                if (selectedBlock.canRotate)
                {
                    HandleRotation();
                    HandleRotateAxisDrag();
                }
            }
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
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame) ActionManager.instance.Undo();
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.yKey.wasPressedThisFrame) ActionManager.instance.Redo();
    }

    public void SetBuildMode(bool lockView)
    {
        if (!lockView)
        {
            ClearCurrentGhost();
            if (selectedBlock != null)
            {
                DeselectBlock();
            }

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
        if (!IsEditingEnemyBlueprint) return;

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
        currentBlockResourcePath = resourcePath;
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

    private void AlignAxisToNearestWorldDir()
    {
        Vector3 camForward = mainCamera.transform.forward.normalized;
        Vector3 camUp = mainCamera.transform.up.normalized;

        // 找到与相机 forward 最接近的方向
        Vector3 nearestForward = dirs[0];
        Vector3 nearestUp = dirs[0];

        float maxDotForward = Vector3.Dot(camForward, nearestForward);
        float maxDotUp = Vector3.Dot(camForward, nearestUp);

        for (int i = 1; i < dirs.Length; i++)
        {
            float dotForward = Vector3.Dot(camForward, dirs[i]);
            float dotUp = Vector3.Dot(camUp, dirs[i]);

            if (dotForward > maxDotForward)
            {
                maxDotForward = dotForward;
                nearestForward = dirs[i];
            }

            if (dotUp > maxDotUp)
            {
                maxDotUp = dotUp;
                nearestUp = dirs[i];
            }
        }

        // 保存坐标系三方向，供移动用
        axisForward = nearestForward;
        axisRight = Vector3.Cross(nearestUp, axisForward).normalized;
        axisUp = Vector3.Cross(axisForward, axisRight).normalized;

        if (moveAxis != null)
        {
            moveAxis.transform.rotation = Quaternion.LookRotation(nearestForward, nearestUp);
        }

        if (rotateAxis != null)
        {
            rotateAxis.transform.rotation = Quaternion.LookRotation(nearestForward, nearestUp);
        }
    }

    private void HandleSelection()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, axisLayer))
            {
                MoveAxisHandle moveHandle = hit.collider.GetComponent<MoveAxisHandle>();
                if (moveHandle != null)
                {
                    activeHandle = moveHandle;
                    dragStartPos = hit.point;
                    blockStartPos = selectedBlock.transform.position;
                    return;
                }

                RotateAxisHandle rotateHandle = hit.collider.GetComponent<RotateAxisHandle>();
                if (rotateHandle != null)
                {
                    activeRotateHandle = rotateHandle;
                    rotateDragStart = hit.point;
                    blockStartRot = selectedBlock.transform.rotation;
                    return;
                }
            }

            if (Physics.Raycast(ray, out hit, 100f, blockLayer))
            {
                Block block = hit.collider.GetComponentInParent<Block>();
                if (block != null)
                {
                    if (selectedBlock == block)
                    {
                        DeselectBlock();
                    }
                    else
                    {
                        SelectBlock(block);
                    }
                }
                else
                {
                    //DeselectBlock();
                }

                MainUIButtons mainUIButtons = FindFirstObjectByType<MainUIButtons>();
                mainUIButtons.deleteButton.gameObject.SetActive(selectedBlock != null);
            }
        }
    }

    private void SelectBlock(Block block)
    {
        if (selectedBlock == block) return;

        DeselectBlock();

        selectedBlock = block;

        if (highlightMaterial != null)
        {
            Transform modelRoot = block.transform.Find("Model");
            Renderer[] renderers = modelRoot != null
                ? modelRoot.GetComponentsInChildren<Renderer>(true)
                : block.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                // Socket effects keep their authored materials while the solid model is selected.
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                Material[] originalMaterials = renderer.sharedMaterials;
                _selectedMaterials[renderer] = originalMaterials;
                var highlightedMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < highlightedMaterials.Length; i++)
                {
                    highlightedMaterials[i] = highlightMaterial;
                }
                renderer.sharedMaterials = highlightedMaterials;
            }
        }

        // 生成移动轴 Gizmo
        if (moveAxis != null)
        {
            moveAxis.SetActive(true);
            moveAxis.transform.position = selectedBlock.transform.position;
            moveAxis.transform.SetParent(selectedBlock.transform); // 绑定在方块上
        }

        VisualEffectsManager.TryShowBlockSelection(selectedBlock);
    }

    public void DeselectBlock()
    {
        VisualEffectsManager.TryClearBlockSelection(selectedBlock);

        foreach (KeyValuePair<Renderer, Material[]> entry in _selectedMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.sharedMaterials = entry.Value;
            }
        }
        _selectedMaterials.Clear();

        if (moveAxis != null)
        {
            moveAxis.SetActive(false);
            moveAxis.transform.SetParent(null);
        }

        selectedBlock = null;
    }

    private void HandleMovement()
    {
        if (selectedBlock == null) return;

        Vector3 moveDir = Vector3.zero;

        if (Keyboard.current.wKey.wasPressedThisFrame) moveDir += axisForward;
        if (Keyboard.current.sKey.wasPressedThisFrame) moveDir -= axisForward;
        if (Keyboard.current.dKey.wasPressedThisFrame) moveDir += axisRight;
        if (Keyboard.current.aKey.wasPressedThisFrame) moveDir -= axisRight;
        if (Keyboard.current.qKey.wasPressedThisFrame) moveDir += axisUp;
        if (Keyboard.current.eKey.wasPressedThisFrame) moveDir -= axisUp;

        if (moveDir != Vector3.zero)
        {
            Debug.Log($"Move direction: {moveDir}");

            //if (Keyboard.current.shiftKey.isPressed)
            //{
            //    moveStep 
            //}

            Vector3 oldPos = selectedBlock.transform.position;

            Vector3 newPos = SnapCenterByMinCorner(
                oldPos + moveDir * moveStep,
                selectedBlock.transform.rotation,
                selectedBlock
            );

            // 检查是否被阻挡
            if (!IsBlocked(newPos, selectedBlock.transform.rotation, selectedBlock))
            {
                if (Keyboard.current.shiftKey.isPressed)
                {
                    HandleDuplicate(newPos, selectedBlock.transform.rotation);
                }
                else
                {
                    selectedBlock.transform.position = newPos;
                    SaveBlock(selectedBlock);

                    // 记录操作到 Undo 栈
                    var action = new MoveBlockAction(selectedBlock, oldPos, newPos);
                    ActionManager.instance.Push(action);
                    VisualEffectsManager.TryPlayBlockMoved(selectedBlock, oldPos, newPos);
                }
            }
            else
            {
                
            }
        }
    }

    private void HandleRotation()
    {
        if (selectedBlock == null) return;

        Vector3 moveEuler = Vector3.zero;

        if (Keyboard.current.wKey.wasPressedThisFrame) moveEuler += axisRight * 90;
        if (Keyboard.current.sKey.wasPressedThisFrame) moveEuler -= axisRight * 90;
        if (Keyboard.current.dKey.wasPressedThisFrame) moveEuler += axisUp * 90;
        if (Keyboard.current.aKey.wasPressedThisFrame) moveEuler -= axisUp * 90;
        if (Keyboard.current.eKey.wasPressedThisFrame) moveEuler += axisForward * 90;
        if (Keyboard.current.qKey.wasPressedThisFrame) moveEuler -= axisForward * 90;

        if (moveEuler != Vector3.zero)
        {
            Quaternion oldRot = selectedBlock.transform.rotation;

            Quaternion newRot = oldRot * Quaternion.Euler(moveEuler);

            Vector3 oldPos = selectedBlock.transform.position;

            Vector3 newPos = SnapCenterByMinCorner(
                oldPos,
                newRot,
                selectedBlock
            );

            // 检查是否被阻挡
            if (!IsBlocked(newPos, newRot, selectedBlock))
            {
                selectedBlock.transform.position = newPos;
                selectedBlock.transform.rotation = newRot;
                SaveBlock(selectedBlock);

                // 记录操作到 Undo 栈
                var action = new RotateBlockAction(selectedBlock, oldPos, newPos, oldRot, newRot);
                ActionManager.instance.Push(action);
                VisualEffectsManager.TryPlayBlockRotated(selectedBlock);
            }
            else
            {
                
            }
        }
    }

    private void HandleMoveAxisDrag()
    {
        if (Mouse.current.leftButton.isPressed && activeHandle != null)
        {
            // 根据 handle 名称选择方向
            Vector3 dir = Vector3.zero;
            if (activeHandle.name.Contains("Forward")) dir = axisForward;
            if (activeHandle.name.Contains("Right")) dir = axisRight;
            if (activeHandle.name.Contains("Up")) dir = axisUp;

            // 构建一个拖拽平面：法线 = 相机方向 × 拖拽方向
            Vector3 planeNormal = Vector3.Cross(dir, mainCamera.transform.up);
            if (planeNormal == Vector3.zero)
                planeNormal = Vector3.Cross(dir, mainCamera.transform.right);

            Plane dragPlane = new Plane(planeNormal, blockStartPos);

            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                Vector3 delta = hitPoint - dragStartPos;

                // 投影到拖拽方向
                float moveAmount = Vector3.Dot(delta, dir.normalized);

                Vector3 oldPos = selectedBlock.transform.position;

                // 步进对齐
                Vector3 newPos = blockStartPos + dir.normalized * Mathf.Round(moveAmount / moveStep) * moveStep;

                if (newPos != oldPos)
                {
                    // 检查是否被阻挡
                    if (!IsBlocked(newPos, selectedBlock.transform.rotation, selectedBlock))
                    {
                        if (Keyboard.current.shiftKey.isPressed)
                        {
                            HandleDuplicate(newPos, selectedBlock.transform.rotation);
                        }
                        else
                        {
                            selectedBlock.transform.position = newPos;
                            SaveBlock(selectedBlock);

                            // 记录操作到 Undo 栈
                            var action = new MoveBlockAction(selectedBlock, oldPos, newPos);
                            ActionManager.instance.Push(action);
                            VisualEffectsManager.TryPlayBlockMoved(selectedBlock, oldPos, newPos);
                        }
                    }
                    else
                    {
                        
                    }
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && activeHandle != null)
        {
            SaveBlock(selectedBlock);
            activeHandle = null;
        }
    }

    private void HandleRotateAxisDrag()
    {
        if (Mouse.current.leftButton.isPressed && activeRotateHandle != null)
        {
            // 旋转轴（世界空间）
            Vector3 axis = activeRotateHandle.axis;

            // 从相机发射射线，与一个垂直于旋转轴的平面相交
            Plane dragPlane = new Plane(axis, selectedBlock.transform.position);

            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);

                // 起点和当前点在平面上的向量
                Vector3 from = (rotateDragStart - selectedBlock.transform.position).normalized;
                Vector3 to = (hitPoint - selectedBlock.transform.position).normalized;

                // 计算旋转角度
                float angle = Vector3.SignedAngle(from, to, axis);

                // 步进（比如 15°/45°）
                float step = 15f;
                float snappedAngle = Mathf.Round(angle / step) * step;

                Quaternion newRot = blockStartRot * Quaternion.AngleAxis(snappedAngle, axis);

                // 检查是否阻挡
                if (!IsBlocked(selectedBlock.transform.position, newRot, selectedBlock))
                {
                    selectedBlock.transform.rotation = newRot;
                    rotateAxis.transform.rotation = newRot; // gizmo 跟随
                }
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && activeRotateHandle != null)
        {
            SaveBlock(selectedBlock);
            activeRotateHandle = null;
        }
    }

    private void HandleDuplicate(Vector3 newPos, Quaternion newRot)
    {
        string resourcePath = selectedBlock.resourcePath;
        Debug.Log(resourcePath);
        GameObject prefab = Resources.Load<GameObject>(resourcePath);

        Vector3 moveDir = (newPos - selectedBlock.transform.position).normalized;
        float step = GetMoveStep(selectedBlock, moveDir);

        // 复制时对齐到步长
        newPos = selectedBlock.transform.position + moveDir * step;

        DeselectBlock();

        CreateBlock(prefab, resourcePath, newPos, newRot);
        SelectBlock(lastSaveBlock);
    }


    private void HandleBuildingPreview()
    {
        GameObject prefab = Resources.Load<GameObject>(currentBlockResourcePath);
        Block prefabBlock = prefab != null ? prefab.GetComponent<Block>() : null;
        if (mainCamera == null || prefabBlock == null) { ClearCurrentGhost(); return; }
        if (_connectorHints != null) _connectorHints.Show(mainCamera, BuildRange, blockLayer, currentGhost);
        // Keep the range visible while aiming at empty space or browsing UI, but never place through UI.
        if (Mouse.current == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            ClearCurrentGhost(false);
            return;
        }
        hoveredConnector = null;

        Vector3 rawPos = Vector3.zero;
        Vector3 snappedPos = Vector3.zero;
        Vector3 nearestWorldPos = Vector3.zero;
        Vector3 nearestNormal = Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, BuildRange + Vector3.Distance(ray.origin, mainCamera.transform.position), blockLayer))
        {
            Block block = hit.collider.GetComponentInParent<Block>();
            if (block != null)
            {
                // 找最近的 connector
                float minDist = float.MaxValue;
                Connector nearest = null;

                foreach (var c in block.connectors)
                {
                    if (!block.IsConnectorAvailableForPlacement(c)) continue;

                    Vector3 worldPos = block.GetConnectorWorldPosition(c);
                    if (!IsWithinBuildRange(worldPos)) continue;
                    float dist = Vector3.Distance(hit.point, worldPos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = c;
                        nearestWorldPos = worldPos;
                        nearestNormal = block.GetConnectorWorldNormal(c);
                    }
                }

                if (nearest != null)
                {
                    hoveredConnector = nearest;

                    // 生成或更新 Ghost
                    if (currentGhost == null)
                    {
                        currentGhost = Instantiate(prefab);
                        Collider[] colliders = currentGhost.GetComponentsInChildren<Collider>();
                        foreach (var collider in colliders)
                        {
                            collider.gameObject.layer = 0;
                        }
                    }

                    // Put the preview's nearest face on the target connector. The old fixed 0.5 offset
                    // only worked for 1x1x1 blocks and left larger prefabs floating or intersecting.
                    // Match the target's quarter-turn orientation so connector faces stay coplanar
                    // when the target has been rotated in the build grid.
                    currentGhost.transform.rotation = block.transform.rotation;
                    Quaternion previewRotation = currentGhost.transform.rotation;
                    if (!TryCalculateConnectorAlignedPosition(
                        prefabBlock,
                        previewRotation,
                        nearestWorldPos,
                        nearestNormal,
                        out snappedPos))
                    {
                        float previewHalfExtent = GetHalfExtentAlongDirection(prefabBlock, previewRotation, nearestNormal);
                        rawPos = nearestWorldPos + nearestNormal * previewHalfExtent;
                        snappedPos = SnapCenterByMinCorner(rawPos, previewRotation, prefabBlock);
                    }

                    currentGhost.transform.position = snappedPos;
                }
            }
        }
        else
        {
            if (currentGhost != null)
            {
                ClearCurrentGhost(false);
            }
        }

        if (hoveredConnector == null) { ClearCurrentGhost(false); return; }

        if (currentGhost != null)
        {
            Block ghostBlock = currentGhost.GetComponent<Block>();
            bool isBlocked = ghostBlock == null
                || IsBlocked(currentGhost.transform.position, currentGhost.transform.rotation, prefabBlock)
                || (ghostBlock != null && ghostBlock.IsBlockedGhost());

            if (penetrationMode)
            {
                // Bound the search when a target is enclosed or its normal is invalid.
                for (int step = 0; isBlocked && step < 64 && nearestNormal.sqrMagnitude > 0.5f; step++)
                {
                    Vector3 nextPosition = currentGhost.transform.position + nearestNormal;
                    if (!IsWithinBuildRange(nextPosition)) break;
                    currentGhost.transform.position = nextPosition;
                    isBlocked = IsBlocked(currentGhost.transform.position, currentGhost.transform.rotation, prefabBlock)
                        || (ghostBlock != null && ghostBlock.IsBlockedGhost());
                }
            }

            isBlocked |= !IsWithinBuildRange(currentGhost.transform.position);

            Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
                renderer.material = highlightMaterial;
                if (renderer.material.HasProperty("_Color"))
                    renderer.material.color = isBlocked ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.5f);
            }
            VisualEffectsManager.TryUpdateGhostPreview(currentGhost, isBlocked);

            // 鼠标左键点击 → 真正生成方块
            if (Mouse.current.leftButton.wasPressedThisFrame && !isBlocked)
            {
                CreateBlock(prefab, currentBlockResourcePath, currentGhost.transform.position, currentGhost.transform.rotation);
            }
        }
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
        if (selectedBlock == null) return;

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

    public void SaveBlock(Block block)
    {
        if (block == null || SaveManager.instance == null)
        {
            return;
        }

        lastSaveBlock = block;
        ApplyBlockBuildDefaults(block);
        BlockData data = new BlockData(block);

        int index = SaveManager.instance.cachedData.blocks.FindIndex(b => b.id == data.id);
        if (index >= 0)
        {
            SaveManager.instance.cachedData.blocks[index] = data;
        }
        else
        {
            SaveManager.instance.cachedData.blocks.Add(data);
        }

        WriteCachedData();
        Debug.Log($"Saved block {block.name} at {block.transform.position}, {block.transform.rotation.eulerAngles}");

        RefreshConnectionsAround(block);
        RefreshBlueprintUI();
    }

    private void RefreshConnectionsAround(Block block)
    {
        if (block == null || BuildManager.instance == null) return;

        Physics.SyncTransforms();
        HashSet<Block> affectedBlocks = new HashSet<Block> { block };
        Vector3 halfExtents = GetBlockHalfExtents(block) + Vector3.one * 0.35f;
        Collider[] hits = Physics.OverlapBox(
            block.transform.position,
            halfExtents,
            block.transform.rotation,
            blockLayer,
            QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            Block hitBlock = hit.GetComponentInParent<Block>();
            if (hitBlock != null && hitBlock != block)
            {
                affectedBlocks.Add(hitBlock);
            }
        }

        // Rebuild every nearby block after transforms are synced. This also updates the
        // opposite Info component when a connector is moved, removed, or replaced.
        foreach (Block affectedBlock in affectedBlocks)
        {
            if (affectedBlock != null && affectedBlock.isActiveAndEnabled)
            {
                affectedBlock.CheckConnection();
            }
        }

        foreach (Block affectedBlock in affectedBlocks)
        {
            if (affectedBlock != null && affectedBlock.isActiveAndEnabled)
            {
                affectedBlock.neighbors = affectedBlock.Neighbors();
            }
        }
    }

    public void RemoveBlock(Block block)
    {
        block.neighbors = block.Neighbors();
        List<Block> blockNeighbors = new List<Block>(block.neighbors);

        if (RemoveCachedBlockData(block.uniqueId))
        {
            Debug.Log($"Removed block {block.name}");
        }

        block.DisConnectAllConnectors();

        if (blockNeighbors.Count > 0)
        {
            foreach (Block blockNeighbor in blockNeighbors)
            {
                if (blockNeighbor != null)
                {
                    blockNeighbor.CheckConnection();
                }
            }
        }

        RefreshBlueprintUI();
    }

    public void LoadAllBlocks()
    {
        // Loading is versioned so a newer save request can invalidate an older coroutine safely.
        double time0 = Time.timeAsDouble;

        if (!IsEditingEnemyBlueprint && currentSaveName == String.Empty && SaveManager.instance.saves.Count > 0)
        {
            currentSaveName = SaveManager.instance.saves[0];
        }

        BuildTargetContext loadTarget = CurrentBuildContext;
        string loadSavePath = loadTarget.GetSavePath(SaveManager.instance);
        if (IsLoadingBuildTarget(loadTarget))
        {
            Debug.Log($"Already loading {loadSavePath}, skip duplicate load request.");
            return;
        }

        StopActiveBlockLoad();
        DeselectBlock();
        ClearCurrentGhost();

        if (GameManager.instance.blocksParent != null)
        {
            Destroy(GameManager.instance.blocksParent.gameObject);
        }

        SaveManager.instance.blocks.Clear();

        GameObject gameObj = Instantiate(blocksParentPrefab);
        gameObj.name = CurrentBuildName;
        GameManager.instance.blocksParent = gameObj.transform;
        PlayManager.instance.blocksParent = GameManager.instance.blocksParent;

        GameManager.instance.blocksParent.GetComponent<Rigidbody>().isKinematic = true;

        if (!File.Exists(loadSavePath))
        {
            SaveManager.instance.cachedData = new BlockDataList();
            WriteCachedData();
        }

        string json = File.ReadAllText(loadSavePath);
        SaveManager.instance.cachedData = JsonUtility.FromJson<BlockDataList>(json);

        if (SaveManager.instance.cachedData == null || SaveManager.instance.cachedData.blocks == null)
        {
            Debug.Log("保存文件为空或损坏。");
            return;
        }

        List<BlockData> blocksToLoad = new List<BlockData>(SaveManager.instance.cachedData.blocks);
        int loadVersion = ++loadAllBlocksVersion;
        loadingBuildTarget = loadTarget;
        loadingBuildSavePath = loadSavePath;
        loadingCameraController = GetMainCameraController();
        hasLoadingCameraOrbitBounds = TryCalculateLoadingCameraOrbitBounds(blocksToLoad, gameObj.transform.position, out loadingCameraOrbitBounds);
        loadingCameraOrbitCenter = hasLoadingCameraOrbitBounds ? loadingCameraOrbitBounds.center : gameObj.transform.position;
        loadingCameraOrbitAngle = GetCameraOrbitAngle(loadingCameraOrbitCenter);
        StartLoadingCameraOrbit(gameObj, blocksToLoad.Count);
        IsLoadingBlocks = true;
        if (BlueprintUIPanel.instance != null)
        {
            BlueprintUIPanel.instance.UpdateStatistics(0, 0f, 0f, 0f);
        }
        loadAllBlocksCoroutine = StartCoroutine(LoadAllBlocksRoutine(loadVersion, loadSavePath, gameObj.transform, blocksToLoad, time0));
    }

    private IEnumerator LoadAllBlocksRoutine(int loadVersion, string loadSavePath, Transform loadParent, List<BlockData> blocksToLoad, double time0)
    {
        List<string> unloadIds = new List<string>();
        int failCount = 0;
        int sucessCount = 0;
        float loadedMass = 0f;
        float loadedRequiredPower = 0f;
        float loadedGeneratorOutput = 0f;
        // A new load starts its own decay clock; camera motion retains its independent speed.
        double intervalStartTime = Time.timeAsDouble;

        for (int i = 0; i < blocksToLoad.Count; i++)
        {
            if (!IsCurrentBlockLoad(loadVersion, loadSavePath, loadParent))
            {
                AbortBlockLoadIfCurrent(loadVersion, loadParent);
                yield break;
            }

            BlockData data = blocksToLoad[i];
            // 从 Resources 目录加载 prefab
            GameObject prefab = Resources.Load<GameObject>(ConvertToResourcesPath(data.resourcePath));
            if (prefab == null)
            {
                Debug.LogWarning($"第{i + 1}个方块 找不到资源路径: {data.resourcePath}");
                unloadIds.Add(data.id);
                failCount++;
            }
            else
            {
                GameObject obj = Instantiate(prefab, new Vector3(data.posX, data.posY, data.posZ), new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW));
                obj.transform.SetParent(loadParent);
                CargoHold restoredHold = obj.GetComponent<CargoHold>();
                if (restoredHold != null && data.cargo != null) restoredHold.RestoreContents(data.cargo);
                Block block = obj.GetComponent<Block>();
                if (block != null)
                {
                    block.x = data.x;
                    block.y = data.y;
                    block.z = data.z;
                    block.resourcePath = data.resourcePath;
                    block.uniqueId = data.id; // 保持唯一 ID 一致
                    ApplyBlockBuildDefaults(block);
                    SaveManager.instance.blocks.Add(block);
                    sucessCount++;
                    loadedMass += block.mass;

                    Power power = block.GetComponent<Power>();
                    if (power != null)
                    {
                        loadedRequiredPower += Mathf.Max(0f, power.standardWorkingPower);
                    }

                    PowerGeneratingUnit generator = block.GetComponent<PowerGeneratingUnit>();
                    if (generator != null)
                    {
                        loadedGeneratorOutput += Mathf.Max(0f, generator.outputPower);
                    }

                    if (BlueprintUIPanel.instance != null)
                    {
                        BlueprintUIPanel.instance.UpdateStatistics(
                            sucessCount,
                            loadedMass,
                            loadedRequiredPower,
                            loadedGeneratorOutput);
                    }
                }
                Durability durability = obj.GetComponent<Durability>();
                if (durability != null)
                {
                    durability.currentDurability = durability.maxDurability;
                }
            }

            if (i < blocksToLoad.Count - 1)
            {
                float interval = CalculateBlockLoadInterval(Time.timeAsDouble - intervalStartTime);
                // Even a zero-delay load yields a frame so progress and cancellation remain responsive.
                yield return interval > 0f ? new WaitForSeconds(interval) : null;
            }
        }

        if (!IsCurrentBlockLoad(loadVersion, loadSavePath, loadParent))
        {
            AbortBlockLoadIfCurrent(loadVersion, loadParent);
            yield break;
        }

        if (blocksToLoad.Count == 0)
        {
            InitialBlock();
        }

        if (unloadIds.Count > 0 )
        {
            foreach (string id in unloadIds)
            {
                ClearUnloadableData(id, loadSavePath);
            }
        }

        foreach (Block block in SaveManager.instance.blocks)
        {
            block.neighbors = block.Neighbors();
            block.CheckConnection();
        }

        double time1 = Time.timeAsDouble;

        IsLoadingBlocks = false;
        loadAllBlocksCoroutine = null;
        ClearLoadingBuildTarget();

        Debug.Log($"加载{loadSavePath}完成，耗时{time1 - time0}s，共{blocksToLoad.Count}个方块, 恢复成功{sucessCount}个方块，恢复失败{failCount}个方块");

        StopLoadingCameraOrbit();
        loadingCameraController = null;
    }

    public void ClearUnloadableData(string id)
    {
        ClearUnloadableData(id, savePath);
    }

    public bool IsLoadingBuildTarget(BuildTargetKind kind, string saveName, string enemyBlueprintName)
    {
        BuildTargetContext context = kind == BuildTargetKind.EnemyBlueprint
            ? BuildTargetContext.EnemyBlueprint(saveName, enemyBlueprintName)
            : BuildTargetContext.PlayerSave(saveName, enemyBlueprintName);

        return IsLoadingBuildTarget(context);
    }

    private bool IsLoadingBuildTarget(BuildTargetContext context)
    {
        if (!IsLoadingBlocks || SaveManager.instance == null) return false;
        if (loadAllBlocksCoroutine == null || GameManager.instance == null || GameManager.instance.blocksParent == null) return false;

        return loadingBuildTarget.Kind == context.Kind
            && string.Equals(loadingBuildTarget.SaveName, context.SaveName, StringComparison.Ordinal)
            && string.Equals(loadingBuildTarget.EnemyBlueprintName, context.EnemyBlueprintName, StringComparison.Ordinal)
            && string.Equals(loadingBuildSavePath, context.GetSavePath(SaveManager.instance), StringComparison.OrdinalIgnoreCase);
    }

    private void ClearUnloadableData(string id, string targetSavePath)
    {
        if (RemoveCachedBlockData(id, targetSavePath))
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

    private Vector3 GetBlockHalfExtents(Block block)
    {
        if (block == null) return Vector3.zero;
        float safeGridSize = Mathf.Max(0.0001f, gridSize);
        return new Vector3(block.x, block.y, block.z) * safeGridSize * 0.5f;
    }

    private float GetHalfExtentAlongDirection(Block block, Quaternion rotation, Vector3 worldDirection)
    {
        Vector3 direction = worldDirection.normalized;
        Vector3 halfExtents = GetBlockHalfExtents(block);
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        Vector3 forward = rotation * Vector3.forward;
        return Mathf.Abs(Vector3.Dot(direction, right)) * halfExtents.x
            + Mathf.Abs(Vector3.Dot(direction, up)) * halfExtents.y
            + Mathf.Abs(Vector3.Dot(direction, forward)) * halfExtents.z;
    }

    private bool TryCalculateConnectorAlignedPosition(
        Block prefabBlock,
        Quaternion previewRotation,
        Vector3 targetConnectorPosition,
        Vector3 targetConnectorNormal,
        out Vector3 position)
    {
        position = Vector3.zero;
        if (prefabBlock == null || prefabBlock.connectors == null) return false;

        Transform connectorRoot = prefabBlock.connectorParent != null ? prefabBlock.connectorParent : prefabBlock.transform;
        Connector bestConnector = null;
        float bestNormalMatch = 0.95f;
        for (int i = 0; i < prefabBlock.connectors.Count; i++)
        {
            Connector connector = prefabBlock.connectors[i];
            if (connector == null || !connector.canConnect) continue;

            Vector3 localNormal = prefabBlock.transform.InverseTransformDirection(connectorRoot.TransformDirection(connector.normal));
            Vector3 connectorNormal = previewRotation * localNormal;
            float normalMatch = Vector3.Dot(connectorNormal.normalized, -targetConnectorNormal.normalized);
            if (normalMatch > bestNormalMatch)
            {
                bestNormalMatch = normalMatch;
                bestConnector = connector;
            }
        }

        if (bestConnector == null) return false;

        Vector3 connectorLocalPosition = prefabBlock.transform.InverseTransformPoint(connectorRoot.TransformPoint(bestConnector.localPos));
        connectorLocalPosition = Vector3.Scale(connectorLocalPosition, prefabBlock.transform.localScale);
        position = targetConnectorPosition - previewRotation * connectorLocalPosition;
        return true;
    }

    // 轴对齐方块的精确吸附：先对齐最小角，再还原中心
    public Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)
    {
        if (b == null) return targetCenter;

        // 方块的局部半尺寸（不含旋转）
        Vector3 halfSize = GetBlockHalfExtents(b);
        float safeGridSize = Mathf.Max(0.0001f, gridSize);
        float snapStep = safeGridSize * 0.5f;

        // 计算旋转后的 8 个顶点
        Vector3[] corners = new Vector3[8];
        int i = 0;
        for (int xi = -1; xi <= 1; xi += 2)
        {
            for (int yi = -1; yi <= 1; yi += 2)
            {
                for (int zi = -1; zi <= 1; zi += 2)
                {
                    Vector3 localCorner = new Vector3(xi * halfSize.x, yi * halfSize.y, zi * halfSize.z);
                    corners[i++] = targetCenter + targetRotation * localCorner;
                }
            }
        }

        // 得到 AABB 的 min/max
        Vector3 min = corners[0];
        Vector3 max = corners[0];
        foreach (var c in corners)
        {
            min = Vector3.Min(min, c);
            max = Vector3.Max(max, c);
        }

        // Block centers are allowed on half-grid coordinates (Block.Awake preserves 0.5 steps),
        // so align bounds to half-grid steps instead of moving odd-sized blocks by half a cell.
        Vector3 snappedMin = new Vector3(
            Mathf.Round((min.x - gridOrigin.x) / snapStep) * snapStep + gridOrigin.x,
            Mathf.Round((min.y - gridOrigin.y) / snapStep) * snapStep + gridOrigin.y,
            Mathf.Round((min.z - gridOrigin.z) / snapStep) * snapStep + gridOrigin.z
        );

        Vector3 snappedCenter = snappedMin + (max - min) * 0.5f;

        return snappedCenter;
    }


    private bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)
    {
        if (block == null) return true;

        // 方块的半尺寸
        Vector3 halfExtents = GetBlockHalfExtents(block);

        // 检测范围（目标位置 + 半尺寸）
        Physics.SyncTransforms();
        Collider[] hits = Physics.OverlapBox(
            targetCenter,
            halfExtents,    // 稍微缩小，避免边界浮点误差
            targetRotation,
            blockLayer              // 只检测方块层
        );

        foreach (var hit in hits)
        {
            Block other = hit.GetComponentInParent<Block>();
            if (other != null && other != block)
            {
                Debug.Log($"被{other}阻挡");
                return true; // 有别的方块 → 阻挡
            }
        }
        return false;
    }

    private float GetMoveStep(Block block, Vector3 moveDir)
    {
        // 方块的局部半尺寸（不考虑旋转）
        Vector3 halfSize = new Vector3(block.x, block.y, block.z) * gridSize * 0.5f;

        // 方块旋转
        Quaternion rot = block.transform.rotation;

        // 取旋转后局部坐标轴
        Vector3 right = rot * Vector3.right;
        Vector3 up = rot * Vector3.up;
        Vector3 forward = rot * Vector3.forward;

        // 移动方向（归一化）
        Vector3 dir = moveDir.normalized;

        // 在这个方向上的“投影厚度” = 各轴厚度在 dir 上的分量绝对值
        float step =
            Mathf.Abs(Vector3.Dot(dir, right)) * (halfSize.x * 2) +
            Mathf.Abs(Vector3.Dot(dir, up)) * (halfSize.y * 2) +
            Mathf.Abs(Vector3.Dot(dir, forward)) * (halfSize.z * 2);

        return step;
    }


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
        if (block == null) return;

        Cockpit cockpit = block.GetComponent<Cockpit>();
        if (cockpit != null)
        {
            cockpit.faction = CurrentBuildFaction;
        }
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
        loadAllBlocksVersion++;

        if (loadAllBlocksCoroutine != null)
        {
            StopCoroutine(loadAllBlocksCoroutine);
            loadAllBlocksCoroutine = null;
        }

        IsLoadingBlocks = false;
        ClearLoadingBuildTarget();
        StopLoadingCameraOrbit();
        loadingCameraController = null;
    }

    private bool IsCurrentBlockLoad(int loadVersion, string loadSavePath, Transform loadParent)
    {
        return loadVersion == loadAllBlocksVersion
            && loadParent != null
            && GameManager.instance != null
            && GameManager.instance.blocksParent == loadParent
            && string.Equals(savePath, loadSavePath, StringComparison.OrdinalIgnoreCase);
    }

    private void AbortBlockLoadIfCurrent(int loadVersion, Transform loadParent)
    {
        if (loadVersion != loadAllBlocksVersion) return;

        loadAllBlocksCoroutine = null;
        IsLoadingBlocks = false;
        ClearLoadingBuildTarget();
        StopLoadingCameraOrbit();
        loadingCameraController = null;

        bool ownsLoadParent = GameManager.instance != null && GameManager.instance.blocksParent == loadParent;
        if (ownsLoadParent)
        {
            Destroy(loadParent.gameObject);
            GameManager.instance.blocksParent = null;

            if (PlayManager.instance != null && PlayManager.instance.blocksParent == loadParent)
            {
                PlayManager.instance.blocksParent = null;
            }

            SaveManager.instance.blocks.Clear();
        }
    }

    private void ClearLoadingBuildTarget()
    {
        loadingBuildTarget = default;
        loadingBuildSavePath = string.Empty;
        hasLoadingCameraOrbitBounds = false;
        loadingCameraOrbitBounds = default;
    }

    private void StartLoadingCameraOrbit(GameObject frameObject, int blockCount)
    {
        if (!moveCameraDuringBlockLoad || frameObject == null) return;
        if (blockCount < minBlocksForLoadCameraOrbit) return;

        CameraController cameraController = loadingCameraController != null
            ? loadingCameraController
            : GetMainCameraController();

        if (cameraController == null) return;

        float orbitDegreesPerSecond = CalculateLoadingCameraOrbitDegreesPerSecond(blockCount);
        if (hasLoadingCameraOrbitBounds)
        {
            // Use the complete saved layout from the first frame so loading progress cannot
            // continuously expand the renderer bounds and push the camera farther away.
            cameraController.StartContinuousOrbitCameraAroundBounds(
                loadingCameraOrbitBounds,
                loadingCameraOrbitCenter,
                loadingCameraOrbitAngle,
                orbitDegreesPerSecond,
                blockLoadCameraOrbitPitch,
                blockLoadCameraOrbitRadiusVariation,
                blockLoadCameraOrbitRadiusWaveDegrees,
                Mathf.Max(0.01f, blockLoadCameraMoveDuration)
            );
        }
        else
        {
            cameraController.StartContinuousOrbitCameraAroundBlock(
                frameObject,
                loadingCameraOrbitCenter,
                loadingCameraOrbitAngle,
                orbitDegreesPerSecond,
                blockLoadCameraOrbitPitch,
                blockLoadCameraOrbitRadiusVariation,
                blockLoadCameraOrbitRadiusWaveDegrees,
                Mathf.Max(0.01f, blockLoadCameraMoveDuration)
            );
        }
    }

    private float CalculateBlockLoadInterval(double elapsedSeconds)
    {
        float initialInterval = Mathf.Max(0f, BlockLoadIntervalSeconds);
        float minimumInterval = Mathf.Clamp(_minimumBlockLoadIntervalSeconds, 0f, initialInterval);
        float halfLife = Mathf.Max(0.01f, _blockLoadIntervalHalfLifeSeconds);
        float decay = Mathf.Pow(0.5f, (float)Math.Max(0d, elapsedSeconds) / halfLife);
        return Mathf.Max(minimumInterval, initialInterval * decay);
    }

    private float CalculateLoadingCameraOrbitDegreesPerSecond(int blockCount)
    {
        float expectedLoadDuration = Mathf.Max(_loadCameraReferenceSecondsPerBlock * Mathf.Max(blockCount - 1, 1), 0.01f);
        return 360f / expectedLoadDuration;
    }

    private void StopLoadingCameraOrbit()
    {
        CameraController cameraController = loadingCameraController != null
            ? loadingCameraController
            : GetMainCameraController();

        if (cameraController == null) return;

        cameraController.StopCameraMotion();
    }

    private CameraController GetMainCameraController()
    {
        Camera camera = mainCamera != null ? mainCamera : Camera.main;
        return camera != null ? camera.GetComponent<CameraController>() : null;
    }

    private float GetCameraOrbitAngle(Vector3 orbitCenter)
    {
        if (loadingCameraController == null) return 135f;

        Vector3 cameraOffset = loadingCameraController.transform.position - orbitCenter;
        cameraOffset.y = 0f;
        if (cameraOffset.sqrMagnitude < 0.001f) return 135f;

        return Mathf.Atan2(cameraOffset.x, cameraOffset.z) * Mathf.Rad2Deg;
    }

    private bool TryCalculateLoadingCameraOrbitBounds(List<BlockData> blocksToLoad, Vector3 fallbackCenter, out Bounds bounds)
    {
        bounds = new Bounds(fallbackCenter, Vector3.zero);
        if (blocksToLoad == null || blocksToLoad.Count == 0) return false;

        bool hasBounds = false;
        foreach (BlockData data in blocksToLoad)
        {
            if (data == null) continue;

            Bounds blockBounds = CalculateBlockDataBounds(data);
            if (!hasBounds)
            {
                bounds = blockBounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(blockBounds);
            }
        }

        return hasBounds;
    }

    private Bounds CalculateBlockDataBounds(BlockData data)
    {
        Vector3 center = new Vector3(data.posX, data.posY, data.posZ);
        Vector3 halfSize = new Vector3(
            Mathf.Max(data.x * gridSize, gridSize),
            Mathf.Max(data.y * gridSize, gridSize),
            Mathf.Max(data.z * gridSize, gridSize)
        ) * 0.5f;

        Quaternion rotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);
        float rotationMagnitude = Mathf.Sqrt(
            rotation.x * rotation.x +
            rotation.y * rotation.y +
            rotation.z * rotation.z +
            rotation.w * rotation.w
        );
        if (rotationMagnitude > 0.0001f)
        {
            rotation = new Quaternion(
                rotation.x / rotationMagnitude,
                rotation.y / rotationMagnitude,
                rotation.z / rotationMagnitude,
                rotation.w / rotationMagnitude
            );
        }
        else
        {
            rotation = Quaternion.identity;
        }

        Bounds bounds = new Bounds(center, Vector3.zero);
        bool hasCorner = false;
        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localCorner = Vector3.Scale(halfSize, new Vector3(x, y, z));
                    Vector3 worldCorner = center + rotation * localCorner;
                    if (!hasCorner)
                    {
                        bounds = new Bounds(worldCorner, Vector3.zero);
                        hasCorner = true;
                    }
                    else
                    {
                        bounds.Encapsulate(worldCorner);
                    }
                }
            }
        }

        return bounds;
    }

    private void ClearCurrentGhost(bool hideHints = true)
    {
        if (hideHints && _connectorHints != null) _connectorHints.Hide();
        hoveredConnector = null;
        if (currentGhost == null) return;

        VisualEffectsManager.TryClearGhostPreview(currentGhost);
        Destroy(currentGhost);
        currentGhost = null;
        hoveredConnector = null;
    }

    private bool RemoveCachedBlockData(string id)
    {
        return RemoveCachedBlockData(id, savePath);
    }

    private bool RemoveCachedBlockData(string id, string targetSavePath)
    {
        int index = SaveManager.instance.cachedData.blocks.FindIndex(b => b.id == id);
        if (index < 0) return false;

        SaveManager.instance.cachedData.blocks.RemoveAt(index);
        WriteCachedData(targetSavePath);
        return true;
    }

    private void WriteCachedData()
    {
        WriteCachedData(savePath);
    }

    private void WriteCachedData(string targetSavePath)
    {
        string json = JsonUtility.ToJson(SaveManager.instance.cachedData, true);
        File.WriteAllText(targetSavePath, json);
    }

    public static string ConvertToResourcesPath(string fullPath)
    {
        if (fullPath.StartsWith("Assets/Resources/"))
        {
            fullPath = fullPath.Substring("Assets/Resources/".Length);
        }

        if (fullPath.EndsWith(".prefab"))
        {
            fullPath = fullPath.Substring(0, fullPath.Length - ".prefab".Length);
        }

        return fullPath;
    }
}

public enum SelectType
{
    Move,
    Rotate
}

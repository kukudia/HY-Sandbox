using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;
    public Camera mainCamera;
    public Transform blocksParent;
    public LayerMask axisLayer;
    public LayerMask blockLayer;   // 方块所在的层

    public GameObject moveAxis;
    public GameObject rotateAxis;
    public GameObject blocksParentPrefab;

    private Material originalMaterial;
    private Renderer selectedRenderer;

    private MoveAxisHandle activeHandle = null;
    private Vector3 dragStartPos;
    private Vector3 blockStartPos;

    private RotateAxisHandle activeRotateHandle = null;
    private Vector3 rotateDragStart;
    private Quaternion blockStartRot;


    // 网格参数
    public float gridSize = 1f;
    private Vector3 gridOrigin = Vector3.zero;

    public Material highlightMaterial;
    private float moveStep = 1f;    // 移动步长

    [Header("Current")]
    public SelectType currentSelectType;
    public Block selectedBlock;
    public Block lastSaveBlock;
    public GameObject currentGhost;        // 当前的 ghost 实例
    public string currentBlockResourcePath;
    public string currentSaveName = "default";
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

    private string savePath => SaveManager.instance.GetSavePath(currentSaveName);


    public bool lockView;
    public bool penetrationMode;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        lockView = true;
        SetBuildMode();
    }

    void Update()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            lockView = !lockView;
            SetBuildMode();
        }

        if (lockView)
        {
            if (currentBlockResourcePath == string.Empty)
            {
                if (currentGhost != null)
                {
                    Destroy(currentGhost);
                    hoveredConnector = null;
                }

                HandleSelection();
            }
            else
            {
                HandleBuildingPreview();
            }
        }

        if (lockView && selectedBlock != null)
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

        // 撤销重做
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame) ActionManager.instance.Undo();
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.yKey.wasPressedThisFrame) ActionManager.instance.Redo();

        // 删除
        if (selectedBlock != null && Keyboard.current.deleteKey.wasPressedThisFrame) DeleteBlock();
    }

    void SetBuildMode()
    {
        Cursor.lockState = lockView ? CursorLockMode.Confined : CursorLockMode.Locked;

        if (!lockView)
        {
            if (selectedBlock != null)
            {
                DeselectBlock();
            }

            if (currentGhost != null)
            {
                Destroy(currentGhost);
                hoveredConnector = null;
            }
        }
    }

    void AlignAxisToNearestWorldDir()
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

    void HandleSelection()
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

    void SelectBlock(Block block)
    {
        if (selectedBlock == block) return;

        DeselectBlock();

        selectedBlock = block;
        selectedRenderer = block.GetComponentInChildren<Renderer>();

        if (selectedRenderer != null && highlightMaterial != null)
        {
            originalMaterial = selectedRenderer.sharedMaterial;
            selectedRenderer.sharedMaterial = highlightMaterial;
        }

        // 生成移动轴 Gizmo
        if (moveAxis != null)
        {
            moveAxis.SetActive(true);
            moveAxis.transform.position = selectedBlock.transform.position;
            moveAxis.transform.SetParent(selectedBlock.transform); // 绑定在方块上
        }
    }

    public void DeselectBlock()
    {
        if (selectedRenderer != null && originalMaterial != null)
        {
            selectedRenderer.sharedMaterial = originalMaterial;
        }

        if (moveAxis != null)
        {
            moveAxis.SetActive(false);
            moveAxis.transform.SetParent(null);
        }

        selectedBlock = null;
        selectedRenderer = null;
    }

    void HandleMovement()
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
                }
            }
            else
            {
                
            }
        }
    }

    void HandleRotation()
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
            }
            else
            {
                
            }
        }
    }

    void HandleMoveAxisDrag()
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

    void HandleRotateAxisDrag()
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

    void HandleDuplicate(Vector3 newPos, Quaternion newRot)
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


    void HandleBuildingPreview()
    {
        // 目标预制体
        GameObject prefab = Resources.Load<GameObject>(currentBlockResourcePath);
        Block prefabBlock = prefab.GetComponent<Block>();

        Vector3 rawPos = Vector3.zero;
        Vector3 snappedPos = Vector3.zero;
        Vector3 nearestWorldPos = Vector3.zero;
        Vector3 nearestNormal = Vector3.zero;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, blockLayer))
        {
            Block block = hit.collider.GetComponentInParent<Block>();
            if (block != null)
            {
                // 找最近的 connector
                float minDist = float.MaxValue;
                Connector nearest = null;

                foreach (var c in block.connectors)
                {
                    if (!c.canConnect || c.isConnected) continue;

                    Vector3 worldPos = block.transform.TransformPoint(c.localPos);
                    float dist = Vector3.Distance(hit.point, worldPos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = c;
                        nearestWorldPos = worldPos;
                        nearestNormal = block.transform.TransformDirection(c.normal);
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

                    // 计算 Snap 后的位置
                    rawPos = nearestWorldPos + nearestNormal * 0.5f;
                    snappedPos = SnapCenterByMinCorner(rawPos, block.transform.rotation, prefabBlock);

                    currentGhost.transform.position = snappedPos;
                    //currentGhost.transform.rotation = Quaternion.LookRotation(nearestNormal);
                }
            }
        }
        else
        {
            if (currentGhost != null)
            {
                Destroy(currentGhost);
                hoveredConnector = null;
            }
        }

        if (currentGhost != null)
        {
            bool isBlocked = currentGhost.GetComponent<Block>().IsBlockedGhost();

            if (penetrationMode)
            {
                while (isBlocked)
                {
                    currentGhost.transform.position += nearestNormal;
                    isBlocked = currentGhost.GetComponent<Block>().IsBlockedGhost();
                }
            }

            Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material = highlightMaterial;
                if (renderer.material.HasProperty("_Color"))
                    renderer.material.color = isBlocked ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.5f);
            }

            // 鼠标左键点击 → 真正生成方块
            if (Mouse.current.leftButton.wasPressedThisFrame && !isBlocked)
            {
                CreateBlock(prefab, currentBlockResourcePath, currentGhost.transform.position, currentGhost.transform.rotation);
            }
        }
    }


    public void CreateBlock(GameObject prefab, string resourcePath, Vector3 pos, Quaternion rot)
    {
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, pos, rot);
            obj.transform.parent = blocksParent;
            Block block = obj.GetComponent<Block>();
            block.resourcePath = resourcePath;
            SaveBlock(block);

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

        string id = selectedBlock.GetInstanceID().ToString();

        RemoveBlock(selectedBlock);
        var action = new DeleteBlockAction(selectedBlock);
        ActionManager.instance.Push(action);

        action.Redo(); // 执行删除

        // 4. 如果删除的是当前选中的方块，清空选中状态
        DeselectBlock();
    }

    public void SaveBlock(Block block)
    {
        lastSaveBlock = block;
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

        string json = JsonUtility.ToJson(SaveManager.instance.cachedData, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Saved block {block.name} at {block.transform.position}, {block.transform.rotation.eulerAngles}");

        block.CheckConnection();
        Collider[] hits = Physics.OverlapBox(
            transform.position,
            new Vector3(block.x * 2, block.y * 2, block.z * 2),
            transform.rotation,
            blockLayer              // 只检测方块层
        );

        foreach (var hit in hits)
        {
            hit.GetComponent<Block>().CheckConnection();
        }

        block.neighbors = block.Neighbors();
        //if (block.neighbors.Count > 0)
        //{
        //    foreach (Block blockNeighbor in block.neighbors)
        //    {
        //        blockNeighbor.CheckConnection();
        //    }
        //}
    }

    public void RemoveBlock(Block block)
    {
        block.neighbors = block.Neighbors();

        int index = SaveManager.instance.cachedData.blocks.FindIndex(b => b.id == block.uniqueId);
        if (index >= 0)
        {
            SaveManager.instance.cachedData.blocks.RemoveAt(index);
            string json = JsonUtility.ToJson(SaveManager.instance.cachedData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Removed block {block.name}");
        }

        if (block.neighbors.Count > 0)
        {
            foreach (Block blockNeighbor in block.neighbors)
            {
                blockNeighbor.CheckConnection();
            }
        }
    }

    public void LoadAllBlocks()
    {
        double time0 = Time.timeAsDouble;

        if (blocksParent != null)
        {
            Destroy(blocksParent);
        }

        SaveManager.instance.blocks.Clear();

        if (currentSaveName == String.Empty && SaveManager.instance.saves.Count > 0)
        {
            currentSaveName = SaveManager.instance.saves[0];
        }

        GameObject gameObj = Instantiate(blocksParentPrefab);
        gameObj.name = currentSaveName;
        blocksParent = gameObj.transform;
        PlayManager.instance.blocksParent = blocksParent;

        blocksParent.GetComponent<Rigidbody>().isKinematic = true;

        //if (!File.Exists(savePath))
        //{
        //    Debug.Log($"没有在{savePath}找到保存文件，跳过加载。");
        //    return;
        //}

        string json = File.ReadAllText(savePath);
        SaveManager.instance.cachedData = JsonUtility.FromJson<BlockDataList>(json);

        if (SaveManager.instance.cachedData == null || SaveManager.instance.cachedData.blocks == null)
        {
            Debug.Log("保存文件为空或损坏。");
            return;
        }

        List<string> unloadIds = new List<string>();
        int failCount = 0;
        int sucessCount = 0;
        int i = 0;
        foreach (var data in SaveManager.instance.cachedData.blocks)
        {
            i++;
            // 从 Resources 目录加载 prefab
            GameObject prefab = Resources.Load<GameObject>(ConvertToResourcesPath(data.resourcePath));
            if (prefab == null)
            {
                Debug.LogWarning($"第{i}个方块 找不到资源路径: {data.resourcePath}");
                unloadIds.Add(data.id);
                failCount++;
                continue;
            }

            GameObject obj = Instantiate(prefab, new Vector3(data.posX, data.posY, data.posZ), new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW));
            obj.transform.SetParent(blocksParent);
            Block block = obj.GetComponent<Block>();
            if (block != null)
            {
                block.x = data.x;
                block.y = data.y;
                block.z = data.z;
                block.resourcePath = data.resourcePath;
                block.uniqueId = data.id; // 保持唯一 ID 一致
                SaveManager.instance.blocks.Add(block);
                sucessCount++;
            }
            Durability durability = obj.GetComponent<Durability>();
            if (durability != null)
            {
                durability.currentDurability = durability.maxDurability;
            }
        }

        if (SaveManager.instance.cachedData.blocks.Count == 0)
        {
            InitialBlock();
        }

        if (unloadIds.Count > 0 )
        {
            foreach (string id in unloadIds)
            {
                ClearUnloadableData(id);
            }
        }

        foreach (Block block in SaveManager.instance.blocks)
        {
            block.neighbors = block.Neighbors();
            block.CheckConnection();
        }

        double time1 = Time.timeAsDouble;

        Debug.Log($"加载{savePath}完成，耗时{time1 - time0}s，共{SaveManager.instance.cachedData.blocks.Count}个方块, 恢复成功{sucessCount}个方块，恢复失败{failCount}个方块");

        Camera.main.GetComponent<CameraController>().FocusCameraOnBlock(blocksParent.gameObject);
    }

    public void ClearUnloadableData(string id)
    {
        int index = SaveManager.instance.cachedData.blocks.FindIndex(b => b.id == id);
        if (index >= 0)
        {
            SaveManager.instance.cachedData.blocks.RemoveAt(index);
            string json = JsonUtility.ToJson(SaveManager.instance.cachedData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Removed unload data {id}");
        }
    }

    void InitialBlock()
    {
        string cockpitResourcePath = ConvertToResourcesPath("Assets/Resources/Blocks/Cockpit.prefab");
        GameObject prefab = Resources.Load<GameObject>(cockpitResourcePath);
        Block prefabBlock = prefab.GetComponent<Block>();
        CreateBlock(prefab, cockpitResourcePath, Vector3.zero, Quaternion.identity);
    }

    // 轴对齐方块的精确吸附：先对齐最小角，再还原中心
    public Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)
    {
        // 方块的局部半尺寸（不含旋转）
        Vector3 halfSize = new Vector3(b.x * gridSize, b.y * gridSize, b.z * gridSize) * 0.5f;

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

        // 将 min 对齐到网格
        Vector3 snappedMin = new Vector3(
            Mathf.Round((min.x - gridOrigin.x) / gridSize) * gridSize + gridOrigin.x,
            Mathf.Round((min.y - gridOrigin.y) / gridSize) * gridSize + gridOrigin.y,
            Mathf.Round((min.z - gridOrigin.z) / gridSize) * gridSize + gridOrigin.z
        );

        // 新中心 = snappedMin + 半尺寸 (要在旋转空间里算)
        Vector3 offset = targetRotation * halfSize; // 半尺寸在旋转后的偏移
        Vector3 snappedCenter = snappedMin + (max - min) * 0.5f;

        return snappedCenter;
    }


    bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)
    {
        // 方块的半尺寸
        Vector3 halfExtents = new Vector3(block.x, block.y, block.z) * 0.5f;

        // 检测范围（目标位置 + 半尺寸）
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

    float GetMoveStep(Block block, Vector3 moveDir)
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

[System.Serializable]
public class BlockData
{
    public string id;
    public int x, y, z;
    public float posX, posY, posZ;
    public float rotX, rotY, rotZ, rotW;
    public string resourcePath;

    public BlockData(Block block)
    {
        id = block.uniqueId;
        x = block.x;
        y = block.y;
        z = block.z;

        var t = block.transform;
        // 强制对齐到0.5的倍数
        posX = Mathf.Round(t.position.x * 2) / 2f;
        posY = Mathf.Round(t.position.y * 2) / 2f;
        posZ = Mathf.Round(t.position.z * 2) / 2f;

        // 将旋转对齐到90度的倍数
        Vector3 euler = t.rotation.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90) * 90;
        euler.y = Mathf.Round(euler.y / 90) * 90;
        euler.z = Mathf.Round(euler.z / 90) * 90;

        Quaternion snappedRot = Quaternion.Euler(euler);
        rotX = snappedRot.x;
        rotY = snappedRot.y;
        rotZ = snappedRot.z;
        rotW = snappedRot.w;

        resourcePath = block.resourcePath;
    }
}

[System.Serializable]
public class BlockDataList
{
    public List<BlockData> blocks = new List<BlockData>();
}
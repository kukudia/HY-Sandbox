using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BuildManager : MonoBehaviour
{
    public static BuildManager instance;
    public Camera mainCamera;
    public Transform blocksParent;
    public LayerMask blockLayer;   // 方块所在的层

    public Button undoButton;
    public Button redoButton;
    public Button deleteButton;
    public GameObject axis;

    private Material originalMaterial;
    private Renderer selectedRenderer;

    // 网格参数
    private float gridSize = 1f;
    private Vector3 gridOrigin = Vector3.zero;

    public Material highlightMaterial;
    private float moveStep = 1f;    // 移动步长

    public BlockDataList cachedData = new BlockDataList();

    [Header("Current")]
    public Block selectedBlock;
    public GameObject currentGhost;        // 当前的 ghost 实例
    public string currentBlockResourcePath;
    public string currentSaveName;
    public Connector hoveredConnector;     // 鼠标当前指向的连接点

    private Dictionary<string, int> actionCounter = new Dictionary<string, int>();
    private string savePath => Path.Combine(Application.persistentDataPath, "blocks.json");

    public bool buildMode;
    public bool penetrationMode;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        undoButton.onClick.AddListener(() => ActionManager.instance.Undo());
        redoButton.onClick.AddListener(() => ActionManager.instance.Redo());
        deleteButton.onClick.AddListener(DeleteBlock);
        LoadAllBlocks();
    }

    void Update()
    {
        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            buildMode = !buildMode;
            Cursor.lockState = buildMode ? CursorLockMode.Confined : CursorLockMode.Locked;

            if (!buildMode && currentGhost != null)
            {
                Destroy(currentGhost);
                hoveredConnector = null;
            }
        }

        if (buildMode)
        {
            if (currentBlockResourcePath == string.Empty)
            {
                if (currentGhost != null)
                {
                    Destroy(currentGhost);
                    hoveredConnector = null;
                }
                HandleSelection();
                HandleMovement();
            }
            else
            {
                HandleBuildingPreview();
            }
        }

        // 撤销重做
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame) ActionManager.instance.Undo();
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.yKey.wasPressedThisFrame) ActionManager.instance.Redo();

        // 删除
        if (selectedBlock != null && Keyboard.current.deleteKey.wasPressedThisFrame) DeleteBlock();
    }


    void HandleSelection()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, blockLayer))
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
            }
            else
            {
                //DeselectBlock();
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
    }

    public void DeselectBlock()
    {
        if (selectedRenderer != null && originalMaterial != null)
        {
            selectedRenderer.sharedMaterial = originalMaterial;
        }

        selectedBlock = null;
        selectedRenderer = null;
    }

    void HandleMovement()
    {
        if (selectedBlock == null) return;

        Vector3 moveDir = Vector3.zero;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;
        Vector3 camUp = mainCamera.transform.up;

        camForward.Normalize();
        camRight.Normalize();
        camUp.Normalize();

        if (Keyboard.current.wKey.wasPressedThisFrame) moveDir += camForward;
        if (Keyboard.current.sKey.wasPressedThisFrame) moveDir -= camForward;
        if (Keyboard.current.dKey.wasPressedThisFrame) moveDir += camRight;
        if (Keyboard.current.aKey.wasPressedThisFrame) moveDir -= camRight;
        if (Keyboard.current.eKey.wasPressedThisFrame) moveDir += camUp;
        if (Keyboard.current.qKey.wasPressedThisFrame) moveDir -= camUp;

        //Debug.Log($"{camForward} {camRight} {camUp}");

        if (moveDir != Vector3.zero)
        {
            Vector3 oldPos = selectedBlock.transform.position;

            Vector3 newPos = SnapCenterByMinCorner(
                oldPos + moveDir * moveStep,
                selectedBlock
            );

            // 检查是否被阻挡
            if (!IsMovementBlocked(newPos, selectedBlock))
            {
                selectedBlock.transform.position = newPos;
                SaveBlock(selectedBlock);

                // 记录操作到 Undo 栈
                var action = new MoveBlockAction(selectedBlock, oldPos, newPos);
                ActionManager.instance.Push(action);
            }
            else
            {
                Debug.Log("移动失败，被其他方块阻挡");
            }
        }
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
                    snappedPos = SnapCenterByMinCorner(rawPos, prefabBlock);

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

        //// 1. 在缓存列表中移除
        //int index = cachedData.blocks.FindIndex(b => b.id == id);
        //if (index >= 0)
        //{
        //    cachedData.blocks.RemoveAt(index);
        //}

        //// 2. 写回到文件
        //string json = JsonUtility.ToJson(cachedData, true);
        //File.WriteAllText(savePath, json);
        //Debug.Log($"Deleted block {selectedBlock.name}");

        // 3. 销毁场景中的方块对象
        // 记录操作

        RemoveBlock(selectedBlock);
        var action = new DeleteBlockAction(selectedBlock);
        ActionManager.instance.Push(action);

        action.Redo(); // 执行删除

        // 4. 如果删除的是当前选中的方块，清空选中状态
        DeselectBlock();
    }

    public void SaveBlock(Block block)
    {
        BlockData data = new BlockData(block);

        int index = cachedData.blocks.FindIndex(b => b.id == data.id);
        if (index >= 0)
        {
            cachedData.blocks[index] = data;
        }
        else
        {
            cachedData.blocks.Add(data);
        }

        string json = JsonUtility.ToJson(cachedData, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Saved block {block.name} at {block.transform.position}, {block.transform.rotation.eulerAngles}");

        block.CheckConnection();
        List<Block> blockNeighbors = block.Neighbors();
        if (blockNeighbors.Count > 0)
        {
            foreach (Block blockNeighbor in blockNeighbors)
            {
                blockNeighbor.CheckConnection();
            }
        }
    }

    public void RemoveBlock(Block block)
    {
        List<Block> blockNeighbors = block.Neighbors();

        int index = cachedData.blocks.FindIndex(b => b.id == block.uniqueId);
        if (index >= 0)
        {
            cachedData.blocks.RemoveAt(index);
            string json = JsonUtility.ToJson(cachedData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Removed block {block.name}");
        }

        if (blockNeighbors.Count > 0)
        {
            foreach (Block blockNeighbor in blockNeighbors)
            {
                blockNeighbor.CheckConnection();
            }
        }
    }

    public void LoadAllBlocks()
    {
        if (!File.Exists(savePath))
        {
            Debug.Log("没有找到保存文件，跳过加载。");
            return;
        }

        string json = File.ReadAllText(savePath);
        cachedData = JsonUtility.FromJson<BlockDataList>(json);

        if (cachedData == null || cachedData.blocks == null)
        {
            Debug.Log("保存文件为空或损坏。");
            return;
        }

        List<string> unloadIds = new List<string>();
        int failCount = 0;
        int sucessCount = 0;
        int i = 0;
        foreach (var data in cachedData.blocks)
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
                block.CheckConnection();
                sucessCount++;
            }
        }

        Debug.Log($"加载完成，耗时{Time.timeSinceLevelLoadAsDouble}s，共{cachedData.blocks.Count}个方块, 恢复成功{sucessCount}个方块，恢复失败{failCount}个方块");

        if ( unloadIds.Count > 0 )
        {
            foreach (string id in unloadIds)
            {
                ClearUnloadableData(id);
            }
        }
    }

    public void ClearUnloadableData(string id)
    {
        int index = cachedData.blocks.FindIndex(b => b.id == id);
        if (index >= 0)
        {
            cachedData.blocks.RemoveAt(index);
            string json = JsonUtility.ToJson(cachedData, true);
            File.WriteAllText(savePath, json);
            Debug.Log($"Removed unload data {id}");
        }
    }

    // 轴对齐方块的精确吸附：先对齐最小角，再还原中心
    public Vector3 SnapCenterByMinCorner(Vector3 center, Block b)
    {
        // 方块在世界中的“占用尺寸”（单位：格子）
        Vector3 worldSize = new Vector3(b.x * gridSize, b.y * gridSize, b.z * gridSize);

        // 计算当前最小角（min）在世界空间的位置
        Vector3 min = center - 0.5f * worldSize;

        // 将最小角对齐到网格（考虑 gridOrigin）
        Vector3 snappedMin = new Vector3(
            Mathf.Round((min.x - gridOrigin.x) / gridSize) * gridSize + gridOrigin.x,
            Mathf.Round((min.y - gridOrigin.y) / gridSize) * gridSize + gridOrigin.y,
            Mathf.Round((min.z - gridOrigin.z) / gridSize) * gridSize + gridOrigin.z
        );

        // 用对齐后的最小角 + 半尺寸 = 新的中心
        return snappedMin + 0.5f * worldSize;
    }

    bool IsMovementBlocked(Vector3 targetCenter, Block block)
    {
        // 方块的半尺寸
        Vector3 halfExtents = new Vector3(block.x, block.y, block.z) * 0.5f;

        // 检测范围（目标位置 + 半尺寸）
        Collider[] hits = Physics.OverlapBox(
            targetCenter,
            halfExtents,    // 稍微缩小，避免边界浮点误差
            Quaternion.identity,
            blockLayer              // 只检测方块层
        );

        foreach (var hit in hits)
        {
            Block other = hit.GetComponentInParent<Block>();
            if (other != null && other != block)
            {
                return true; // 有别的方块 → 阻挡
            }
        }
        return false;
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
        posX = t.position.x;
        posY = t.position.y;
        posZ = t.position.z;

        rotX = t.rotation.x;
        rotY = t.rotation.y;
        rotZ = t.rotation.z;
        rotW = t.rotation.w;

        resourcePath = block.resourcePath;
    }
}

[System.Serializable]
public class BlockDataList
{
    public List<BlockData> blocks = new List<BlockData>();
}
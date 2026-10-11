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
    public Block selectedBlock;
    public Button undoButton;
    public Button redoButton;
    private Material originalMaterial;
    private Renderer selectedRenderer;

    // 网格参数
    private float gridSize = 1f;
    private Vector3 gridOrigin = Vector3.zero;

    public Material highlightMaterial;
    private float moveStep = 1f;    // 移动步长
    // 历史记录
    private Stack<IBlockAction> undoStack = new Stack<IBlockAction>();
    private Stack<IBlockAction> redoStack = new Stack<IBlockAction>();


    private string savePath => Path.Combine(Application.persistentDataPath, "blocks.json");

    private BlockDataList cachedData = new BlockDataList();

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        HandleSelection();
        HandleMovement();

        // 撤销 (Ctrl+Z)
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.zKey.wasPressedThisFrame)
        {
            UndoAction();
        }

        // 重做 (Ctrl+Y)
        if (Keyboard.current.leftCtrlKey.isPressed && Keyboard.current.yKey.wasPressedThisFrame)
        {
            RedoAction();
        }

        // 删除选中的方块
        if (selectedBlock != null && Keyboard.current.deleteKey.wasPressedThisFrame)
        {
            DeleteBlock(selectedBlock);
        }
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
                    DeselectBlock();
                }
            }
            else
            {
                DeselectBlock();
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

        if (moveDir != Vector3.zero)
        {
            Vector3 oldPos = selectedBlock.transform.position;

            Vector3 newPos = SnapCenterByMinCorner(
                oldPos + moveDir * moveStep,
                selectedBlock
            );

            // 检查是否被阻挡
            if (!IsBlocked(newPos, selectedBlock))
            {
                selectedBlock.transform.position = newPos;

                // 记录操作到 Undo 栈
                var action = new MoveBlockAction(selectedBlock, oldPos, newPos);
                undoStack.Push(action);
                redoStack.Clear();
            }
            else
            {
                Debug.Log("移动失败，被其他方块阻挡");
            }
        }
    }

    public void DeleteBlock(Block block)
    {
        if (block == null) return;

        string id = block.GetInstanceID().ToString();

        // 1. 在缓存列表中移除
        int index = cachedData.blocks.FindIndex(b => b.id == id);
        if (index >= 0)
        {
            cachedData.blocks.RemoveAt(index);
        }

        // 2. 写回到文件
        string json = JsonUtility.ToJson(cachedData, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Deleted block {block.name}");

        // 3. 销毁场景中的方块对象
        // 记录操作
        var action = new DeleteBlockAction(block);
        undoStack.Push(action);
        redoStack.Clear();

        action.Redo(); // 执行删除

        // 4. 如果删除的是当前选中的方块，清空选中状态
        if (selectedBlock == block)
        {
            DeselectBlock();
        }
    }

    public void SaveBlock(Block block)
    {
        BlockData data = new BlockData(block);

        // 检查缓存里是否已有这个方块，更新而不是重复添加
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
        Debug.Log($"Saved block {block.name} at {block.transform.position}");
    }

    public void LoadBlocks(GameObject blockPrefab)
    {
        if (!File.Exists(savePath)) return;

        string json = File.ReadAllText(savePath);
        cachedData = JsonUtility.FromJson<BlockDataList>(json);

        foreach (var data in cachedData.blocks)
        {
            GameObject obj = Instantiate(blockPrefab);
            Block block = obj.GetComponent<Block>();
            block.x = data.x;
            block.y = data.y;
            block.z = data.z;

            obj.transform.position = data.position;
            obj.transform.rotation = data.rotation;
        }
    }

    void UndoAction()
    {
        if (undoStack.Count > 0)
        {
            var action = undoStack.Pop();
            action.Undo();
            redoStack.Push(action);
        }
    }

    void RedoAction()
    {
        if (redoStack.Count > 0)
        {
            var action = redoStack.Pop();
            action.Redo();
            undoStack.Push(action);
        }
    }

    // 轴对齐方块的精确吸附：先对齐最小角，再还原中心
    Vector3 SnapCenterByMinCorner(Vector3 center, Block b)
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

    bool IsBlocked(Vector3 targetCenter, Block block)
    {
        // 方块的半尺寸
        Vector3 halfExtents = new Vector3(block.x, block.y, block.z) * 0.5f;

        // 检测范围（目标位置 + 半尺寸）
        Collider[] hits = Physics.OverlapBox(
            targetCenter,
            halfExtents * 0.95f,    // 稍微缩小，避免边界浮点误差
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

}

[System.Serializable]
public class BlockData
{
    public string id;   // 每个方块唯一标识，可以用 GetInstanceID 或自定义
    public int x, y, z;
    public Vector3 position;
    public Quaternion rotation;

    public BlockData(Block block)
    {
        id = block.GetInstanceID().ToString();
        x = block.x;
        y = block.y;
        z = block.z;
        position = block.transform.position;
        rotation = block.transform.rotation;
    }
}

[System.Serializable]
public class BlockDataList
{
    public List<BlockData> blocks = new List<BlockData>();
}
using System.Collections.Generic;
using System.Linq;
using Unity.IO.Archive;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayManager : MonoBehaviour
{
    public static PlayManager instance;
    public bool playMode = false;
    public Transform blocksParent;

    public Camera mainCamera;
    public LayerMask blockLayer;   // 方块所在的层

    private Material originalMaterial;
    private Renderer selectedRenderer;
    public Material highlightMaterial;

    public Block selectedBlock;
    public List<ControlUnit> allControlUnits = new List<ControlUnit>(); // 新增列表

    public float lastHeight;
    public float currentHeight;
    public float verticalVelocity;
    public float horizontalVelocity;

    public bool lockView = false;
    public bool showConnectors = true;
    public bool showLabel = true;

    [Tooltip("是否在游戏运行时显示调试UI")]
    public bool showUI = true;

    private GUIStyle headerStyle; // GUI标题样式
    private GUIStyle labelStyle;  // GUI标签样式

    private void Awake()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        if (playMode)
        {
            if (blocksParent == null)
            {
                PlayManager.instance.PlayEnd();
            }

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                lockView = !lockView;
                SetPlayMode();
            }

            HandleSelection();
            CalculateVelocity();
        }
    }

    public void PlayStart()
    {
        ControlUnit controlUnit = BuildManager.instance.blocksParent.GetComponent<ControlUnit>();
        RefreshGroup(controlUnit);

        lastHeight = blocksParent.position.y;

        BuildManager.instance.DeselectBlock();
        BuildManager.instance.enabled = false;

        playMode = true;
    }

    public void RefreshGroup(ControlUnit unit)
    {
        List<Block> blocks = unit.GetComponentsInChildren<Block>().ToList();
        //blocks.RemoveAll(item => item == null);
        if (blocks.Count > 1)
        {
            AssignBlocksToParentGroups(blocks);
            //unit.RefreshChildren();
        }

        foreach (ControlUnit controlUnit in allControlUnits)
        {
            controlUnit.RefreshChildren();
        }

        Debug.Log($"Find {allControlUnits.Count} controls");
    }

    void SetPlayMode()
    {
        Cursor.lockState = lockView ? CursorLockMode.Confined : CursorLockMode.Locked;

        if (!lockView)
        {
            if (selectedBlock != null)
            {
                DeselectBlock();
            }
        }
    }

    void CalculateVelocity()
    {
        currentHeight = blocksParent.position.y;
        // 计算垂直速度
        verticalVelocity = (blocksParent.position.y - lastHeight) / Time.fixedDeltaTime;

        lastHeight = currentHeight;

        Rigidbody rb = blocksParent.GetComponent<Rigidbody>();
        horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
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

        Rack rack = selectedBlock.GetComponent<Rack>();
        if (rack != null)
        {
            ControlUnit unit = rack.GetComponentInParent<ControlUnit>();
            selectedBlock.DisConnectAllConnectors();
            RefreshGroup(unit);
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

    public void PlayEnd()
    {
        List<ControlUnit> controlUnits = Object.FindObjectsByType<ControlUnit>(FindObjectsSortMode.None).ToList();
        foreach (ControlUnit controlUnit in controlUnits)
        {
            controlUnit.PlayEnd();
        }

        playMode = false;
        BuildManager.instance.enabled = true;
        GameManager.Init();
    }

    // 为每个分组创建父物体并分配Block
    public void AssignBlocksToParentGroups(List<Block> blocks)
    {
        // 获取所有Block的分组
        List<List<Block>> groups = BlockGroupManager.GroupBlocks(blocks);
        GameObject parentPrefab = Resources.Load<GameObject>("Prefabs/BlocksParent");
        int groupIndex = 1;
        foreach (List<Block> group in groups)
        {
            // 为每个组创建父物体
            GameObject groupParent = Instantiate(parentPrefab);
            groupParent.name = $"Group_{groupIndex++}";
            groupParent.transform.position = BlockGroupManager.CalculateGroupCenter(group);

            float mass = 0;
            // 将所有Block移动到该父物体下
            foreach (Block block in group)
            {
                if (block.GetComponent<Cockpit>())
                {
                    groupParent.name = SaveManager.instance.currentSaveName;
                    blocksParent = groupParent.transform;
                    BuildManager.instance.blocksParent = groupParent.transform;
                    Debug.Log($"Change new block parent {blocksParent}.");
                }

                block.transform.SetParent(groupParent.transform);

                mass += block.mass;
                block.showConnectors = PlayManager.instance.showConnectors;
                block.showLabel = PlayManager.instance.showLabel;
            }

            Debug.Log($"{groupParent.name} mass: {mass}");

            Rigidbody rb = groupParent.GetComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.5f;      // 增加空气阻力，减缓水平漂移
            rb.angularDamping = 2f; // 增加角阻力，抑制小幅旋转

            //rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.isKinematic = false;
        }

        Camera.main.GetComponent<CameraController>().playerBody = blocksParent;
    }

    public void RegisterControlUnit(ControlUnit unit)
    {
        if (!allControlUnits.Contains(unit))
            allControlUnits.Add(unit);
    }

    public void UnregisterControlUnit(ControlUnit unit)
    {
        if (allControlUnits.Contains(unit))
            allControlUnits.Remove(unit);
    }
}

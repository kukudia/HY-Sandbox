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

    public List<ControlUnit> controlUnits = new List<ControlUnit>();
    public List<Block> blocks = new List<Block>();

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
        RefreshGroups();

        lastHeight = blocksParent.position.y;

        BuildManager.instance.DeselectBlock();
        BuildManager.instance.enabled = false;

        playMode = true;
    }

    public void RefreshGroups()
    {
        AssignBlocksToParentGroups();

        controlUnits = FindObjectsOfType<ControlUnit>().ToList();
        
        foreach (ControlUnit controlUnit in controlUnits)
        {
            controlUnit.RefreshChildren();
        }
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
            rack.DisConnectAllConnectors();
            RefreshGroups();
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
        controlUnits = FindObjectsOfType<ControlUnit>().ToList();
        foreach (ControlUnit controlUnit in controlUnits)
        {
            controlUnit.PlayEnd();
        }

        playMode = false;
        BuildManager.instance.enabled = true;
        GameManager.Init();
    }

    // 为每个分组创建父物体并分配Block
    public void AssignBlocksToParentGroups()
    {
        blocks = FindObjectsOfType<Block>().ToList();

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

    //void OnGUI()
    //{
    //    if (!showUI && !playMode) return;

    //    headerStyle = new GUIStyle(GUI.skin.label);
    //    headerStyle.fontSize = 16;
    //    headerStyle.fontStyle = FontStyle.Bold;
    //    headerStyle.normal.textColor = Color.cyan;

    //    labelStyle = new GUIStyle(GUI.skin.label);
    //    labelStyle.fontSize = 13;
    //    labelStyle.normal.textColor = Color.white;

    //    GUILayout.BeginArea(new Rect(20, 20, 320, 600), GUI.skin.window);

    //    if (hoverFlightController.enabled)
    //    {
    //        GUILayout.Label("Hover Flight Controll System", headerStyle);

    //        GUILayout.Space(8);
    //        GUILayout.Label($"Target Height: {hoverFlightController.targetHoverHeight:F2}", labelStyle);
    //        GUILayout.Label($"Current Height: {transform.position.y:F2}", labelStyle);
    //        GUILayout.Label($"Dynamic Height P: {hoverFlightController.currentHeightP:F2}", labelStyle);
    //        GUILayout.Label($"Vertical Velocity: {hoverFlightController.verticalVelocity:F2} m/s", labelStyle);

    //        GUILayout.Space(10);
    //        GUILayout.Label("Hover Thrusters:", headerStyle);

    //        Thruster[] thrusters = hoverThrusters;

    //        for (int i = 0; i < thrusters.Length; i++)
    //        {
    //            if (thrusters[i] == null) continue;

    //            float norm = thrusters[i].maxThrust > 1e-5f ? thrusters[i].thrust / thrusters[i].maxThrust : 0f;
    //            Color barColor = Color.Lerp(Color.red, Color.green, norm);

    //            GUILayout.BeginHorizontal();
    //            GUILayout.Label($"#{i} {thrusters[i].thrust:F1}/{thrusters[i].maxThrust}", labelStyle);

    //            if (thrusters[i].thrust > 0)
    //            {
    //                // 画进度条背景
    //                Rect r = GUILayoutUtility.GetRect(100, 18);
    //                GUI.color = Color.gray;
    //                GUI.Box(r, GUIContent.none);

    //                // 画推力值条
    //                Rect filled = new Rect(r.x, r.y, r.width * norm, r.height);
    //                GUI.color = barColor;
    //                GUI.Box(filled, GUIContent.none);
    //            }
    //        }

    //        // 恢复颜色
    //        GUI.color = Color.white;

    //        GUILayout.EndHorizontal();
    //    }

    //    GUILayout.EndArea();
    //}
}

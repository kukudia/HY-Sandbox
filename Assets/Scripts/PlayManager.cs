using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayManager : MonoBehaviour
{
    public static PlayManager instance;
    public bool playMode = false;
    public Transform blocksParent;
    public List<ControlUnit> controlUnits = new List<ControlUnit>();
    public List<Block> blocks = new List<Block>();

    public float lastHeight;
    public float currentHeight;
    public float verticalVelocity;
    public float horizontalVelocity;

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
            currentHeight = blocksParent.position.y;
            // 计算垂直速度
            verticalVelocity = (blocksParent.position.y - lastHeight) / Time.fixedDeltaTime;

            lastHeight = currentHeight;

            //horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        }
    }

    public void PlayStart()
    {
        GameObject previousParent = blocksParent.gameObject;
        blocks = blocksParent.GetComponentsInChildren<Block>().ToList();
        AssignBlocksToParentGroups(blocks);
        Destroy(previousParent);

        controlUnits = FindObjectsOfType<ControlUnit>().ToList();
        
        foreach (ControlUnit controlUnit in controlUnits)
        {
            controlUnit.PlayStart();
        }

        lastHeight = blocksParent.position.y;

        Camera.main.GetComponent<CameraController>().playerBody = blocksParent;

        BuildManager.instance.DeselectBlock();
        BuildManager.instance.enabled = false;
        playMode = true;
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
    public static void AssignBlocksToParentGroups(List<Block> allBlocks)
    {
        // 获取所有Block的分组
        List<List<Block>> groups = BlockGroupManager.GroupBlocks(allBlocks);
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
                    BuildManager.instance.blocksParent = groupParent.transform;
                    PlayManager.instance.blocksParent = groupParent.transform;
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

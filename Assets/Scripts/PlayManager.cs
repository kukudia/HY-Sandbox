using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayManager : MonoBehaviour
{
    public static PlayManager instance;
    public bool playMode = false;
    public Transform blocksParent;
    public Block[] blocks;
    public Cockpit[] cockpits;
    public MainThruster[] mainThrusters;
    public HoverThruster[] hoverThrusters;
    public float lastHeight;
    public float currentHeight;
    public float verticalVelocity;
    public float horizontalVelocity;

    public bool showCube = true;
    public bool showConnectors = true;
    public bool showLabel = true;

    [Tooltip("是否在游戏运行时显示调试UI")]
    public bool showUI = true;

    private GUIStyle headerStyle; // GUI标题样式
    private GUIStyle labelStyle;  // GUI标签样式

    private Rigidbody rb;
    private HoverFlightController hoverFlightController;

    private void Awake()
    {
        instance = this;
    }

    private void Update()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            foreach (HoverThruster thruster in hoverThrusters)
            {
                thruster.isHovered = !thruster.isHovered;
            }

            if (!hoverFlightController.setHeight)
            {
                hoverFlightController.targetHoverHeight = (int)blocksParent.position.y + 10;
                hoverFlightController.setHeight = true;
            }
            else
            {
                hoverFlightController.targetHoverHeight = 0;
                hoverFlightController.setHeight = false;
            }
        }
    }

    private void FixedUpdate()
    {
        if (playMode)
        {
            currentHeight = blocksParent.position.y;
            // 计算垂直速度
            verticalVelocity = (blocksParent.position.y - lastHeight) / Time.fixedDeltaTime;

            lastHeight = currentHeight;

            horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude;
        }
    }

    public void PlayStart()
    {
        //blocksParent = BuildManager.instance.blocksParent;

        rb = blocksParent.GetComponent<Rigidbody>();

        SetRigibody();
        GetBlocks();
        GetCockpits();
        GetThrusters();

        hoverFlightController = blocksParent.GetComponent<HoverFlightController>();
        if (hoverThrusters.Length > 0)
        {
            hoverFlightController.thrusters = hoverThrusters;
            hoverFlightController.enabled = true;
            hoverFlightController.Init();
        }

        lastHeight = blocksParent.position.y;

        Camera.main.GetComponent<CameraController>().playerBody = blocksParent;

        BuildManager.instance.DeselectBlock();
        BuildManager.instance.enabled = false;
        playMode = true;
    }

    void SetRigibody()
    {
        rb.linearDamping = 0.5f;      // 增加空气阻力，减缓水平漂移
        rb.angularDamping = 2f; // 增加角阻力，抑制小幅旋转

        //rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.isKinematic = false;
    }

    public void GetBlocks()
    {
        blocks = blocksParent.GetComponentsInChildren<Block>();
        float mass = 0;
        foreach (Block block in blocks)
        {
            mass += block.mass;
            block.showCube = showCube;
            block.showConnectors = showConnectors;
            block.showLabel = showLabel;
        }
        rb.mass = mass;
        Debug.Log($"{blocksParent.name} mass: {mass}");
    }

    void GetCockpits()
    {
        cockpits = blocksParent.GetComponentsInChildren<Cockpit>();
    }

    void GetThrusters()
    {
        mainThrusters = blocksParent.GetComponentsInChildren<MainThruster>();
        hoverThrusters = blocksParent.GetComponentsInChildren<HoverThruster>();
    }

    public void PlayEnd()
    {
        playMode = false;
        hoverFlightController = blocksParent.GetComponent<HoverFlightController>();
        hoverFlightController.enabled = false;
        BuildManager.instance.enabled = true;
        GameManager.Init();
    }

    public bool hasCockpit()
    {
        if (cockpits.Length> 0)
        {
            return true;
        }
        return false;
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

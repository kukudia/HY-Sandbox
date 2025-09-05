using Unity.VisualScripting;
using UnityEngine;

public class PlayManager : MonoBehaviour
{
    public static PlayManager instance;
    public bool playMode = false;
    public Transform blocksParent;
    public Block[] blocks;
    public Cockpit[] cockpits;
    public MainThruster[] mainThrusters;
    public HoverThruster[] hoverThrusters;

    public bool showCube = true;
    public bool showConnectors = true;
    public bool showLabel = true;

    private Rigidbody rb;
    private HoverFlightController hoverFlightController;

    private void Awake()
    {
        instance = this;
    }

    public void PlayStart()
    {
        blocksParent = BuildManager.instance.blocksParent;

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
        else
        {
            hoverFlightController.enabled = false;
        }

        Camera.main.GetComponent<CameraController>().playerBody = blocksParent;
        BuildManager.instance.enabled = false;
        playMode = true;
    }

    void SetRigibody()
    {
        rb.linearDamping = 0.5f;      // 增加空气阻力，减缓水平漂移
        rb.angularDamping = 2f; // 增加角阻力，抑制小幅旋转

        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
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
}

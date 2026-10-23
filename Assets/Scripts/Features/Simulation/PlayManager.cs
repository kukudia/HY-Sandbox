// 职责：协调建造/游玩会话、组注册、选中表现及遥测；重组和清理规则由 Simulation 功能实现。
// 入口：PlayStart() / CanStartPlay() / RefreshGroup()。
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayManager : MonoBehaviour
{
    // Owns the build-to-play transition and keeps runtime unit membership synchronized after topology changes.
    public static PlayManager instance;
    [SerializeField]
    private WorldSessionManager worldSession;
    public WorldSessionManager WorldSession => worldSession;

    [SerializeField]
    private SiegeSession siegeSession;
    public SiegeSession SiegeSession => siegeSession;

    public void BindSiege(SiegeSession session)
    {
        siegeSession = session;
    }

    public bool playMode = false;
    public Transform blocksParent;
    public Camera mainCamera;
    public LayerMask blockLayer;
    public Material highlightMaterial;
    public Block selectedBlock;
    public List<ControlUnit> allControlUnits = new List<ControlUnit>();
    public float maxHealth = 0;
    public float lastHeight;
    public float currentHeight;
    public float verticalVelocity;
    public float horizontalVelocity;
    public bool showConnectors = true;
    public bool showLabel = true;
    private float _lastGroupCleanupCheckTime;
    private RuntimeGroupBuilder _groupBuilder;
    private readonly PlayBlockSelection _selection = new PlayBlockSelection();
    [Header("运行时分组限制")]
    [Min(1)]
    public int maxUselessControlUnitGroups = 32;
    [Min(1f)]
    public float groupCleanupDistance = 200f;
    [Min(0.25f)]
    public float groupCleanupCheckInterval = 1f;
    [Header("运行时刚体阻尼")]
    [SerializeField, Min(0f), Tooltip("所有重组后的控制单元刚体的线性阻尼；提高可减缓惯性漂移。")]
    private float _groupLinearDamping = 1.5f;
    [SerializeField, Min(0f), Tooltip("所有重组后的控制单元刚体的角阻尼。")]
    private float _groupAngularDamping = 2f;
    [SerializeField, Min(0f), Tooltip("重组时由旋转传给新刚体质心的最大附加速度（m/s）。")]
    private float _maximumInheritedTangentialSpeed = 6f;
    [SerializeField, Min(0f), Tooltip("重组后刚体继承的最大角速度（rad/s）。")]
    private float _maximumInheritedAngularSpeed = 2f;
    [SerializeField, Min(0f), Tooltip("重组后复合碰撞体重叠时的最大解穿透速度（m/s）。")]
    private float _groupMaxDepenetrationVelocity = 4f;
    [Tooltip("Show runtime debug UI")]
    public bool showUI = true;
    private void Awake()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        if (!playMode)
        {
            return;
        }

        if (blocksParent == null)
        {
            //MainUIPanels.instance.PlayEnd();
            return;
        }

        if (Time.time - _lastGroupCleanupCheckTime >= Mathf.Max(0.25f, groupCleanupCheckInterval))
        {
            _lastGroupCleanupCheckTime = Time.time;
            RuntimeGroupCleanup.EnforceDistance(allControlUnits, playMode, blocksParent, groupCleanupDistance, DestroyManager.instance);
        }

        //if (Keyboard.current.bKey.wasPressedThisFrame)
        //{
        //    lockView = !lockView;
        //    SetPlayMode();
        //}
        if (siegeSession == null)
        {
            HandleSelection();
        }

        CalculateVelocity();
    }

    public void PlayStart()
    {
        if (playMode)
        {
            return;
        }

        if (!CanStartPlay(out string reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        ControlUnit controlUnit = GameManager.instance.blocksParent.GetComponent<ControlUnit>();
        if (worldSession != null)
        {
            if (!worldSession.CanEnterPlay(GameManager.instance.blocksParent, out reason))
            {
                Debug.LogWarning(reason);
                return;
            }

            worldSession.EnterPlay(GameManager.instance.blocksParent);
        }

        if (controlUnit != null)
        {
            controlUnit.AssignRuntimeOwnershipToBlocks(true);
        }

        maxHealth = 0;
        Durability[] durabilities = blocksParent.GetComponentsInChildren<Durability>();
        foreach (Durability durability in durabilities)
        {
            maxHealth += durability.maxDurability;
        }

        RefreshGroup(controlUnit);
        lastHeight = blocksParent.position.y;
        BuildManager.instance.DeselectBlock();
        BuildManager.instance.enabled = false;
        playMode = true;
        if (worldSession == null)
        {
            FindFirstObjectByType<EnemySpawner>()?.BeginPlayMode(blocksParent);
        }
    }

    public bool CanStartPlay(out string reason)
    {
        reason = string.Empty;
        if (BuildManager.instance == null || GameManager.instance.blocksParent == null)
        {
            reason = "Cannot start play mode without a loaded construct.";
            return false;
        }

        if (BuildManager.instance.IsLoadingBlocks)
        {
            reason = "Cannot start play mode while blocks are still loading.";
            return false;
        }

        if (BuildManager.instance.IsEditingDeveloperBlueprint)
        {
            reason = "Cannot start player play mode while editing a developer blueprint.";
            return false;
        }

        if (!ModularUnitValidator.TryGetSingleCockpit(GameManager.instance.blocksParent, out Cockpit cockpit, out reason))
        {
            return false;
        }

        if (cockpit.faction != UnitFaction.Player)
        {
            reason = "The player construct must contain exactly one Player Cockpit.";
            return false;
        }

        return true;
    }

    public void RefreshGroup(ControlUnit unit)
    {
        // Recalculate connected components, then restore each component's Rigidbody and thruster references.
        if (unit == null)
        {
            return;
        }

        List<Block> blocks = unit.GetComponentsInChildren<Block>(true).Where(block => block != null && block.isActiveAndEnabled).ToList();
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.Refresh, blocks.Count);
        if (blocks.Count > 0)
        {
            AssignBlocksToParentGroups(blocks);
        }
        else
        {
            // 最后一个模块已被移出；清除旧驾驶舱引用，不等待下一帧Destroy空根。
            unit.RefreshChildren();
        }

        RuntimeGroupCleanup.EnforceLimit(allControlUnits, maxUselessControlUnitGroups, DestroyManager.instance);
        RuntimeGroupCleanup.EnforceDistance(allControlUnits, playMode, blocksParent, groupCleanupDistance, DestroyManager.instance);
    }

    public void SetPlayMode(bool lockView)
    {
        if (!lockView && selectedBlock != null)
        {
            DeselectBlock();
        }
    }

    private void CalculateVelocity()
    {
        currentHeight = blocksParent.position.y;
        verticalVelocity = (blocksParent.position.y - lastHeight) / Time.fixedDeltaTime;
        lastHeight = currentHeight;
        Rigidbody rb = blocksParent.GetComponent<Rigidbody>();
        horizontalVelocity = rb != null ? new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z).magnitude : 0f;
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
            }
        }
    }

    private void SelectBlock(Block block)
    {
        if (selectedBlock == block)
        {
            return;
        }

        DeselectBlock();
        selectedBlock = block;
        _selection.Highlight(selectedBlock, highlightMaterial);
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
        _selection.Clear(selectedBlock);
        selectedBlock = null;
    }

    public void PlayEnd()
    {
        maxHealth = 0;
        DestroyManager.instance.EndSalvageSession();
        LootDrop.ClearSession();
        List<ControlUnit> controlUnits = allControlUnits.ToList();
        foreach (ControlUnit controlUnit in controlUnits)
        {
            if (controlUnit != null)
            {
                controlUnit.PlayEnd();
                controlUnit.gameObject.SetActive(false);
                Destroy(controlUnit.gameObject);
            }
        }

        allControlUnits.Clear();
        blocksParent = null;
        GameManager.instance.blocksParent = null;
        playMode = false;
        worldSession?.EndSession();
        if (InputManager.instance != null)
        {
            InputManager.instance.EnterBuildMode();
        }

        BuildManager.instance.enabled = true;
        GameManager.Init();
        FindFirstObjectByType<EnemySpawner>()?.EndPlayMode();
    }

    public void AssignBlocksToParentGroups(List<Block> blocks)
    {
        if (_groupBuilder == null)
        {
            _groupBuilder = new RuntimeGroupBuilder(this);
        }

        GroupPhysicsSettings settings = new GroupPhysicsSettings(
            _groupLinearDamping,
            _groupAngularDamping,
            _groupMaxDepenetrationVelocity,
            _maximumInheritedTangentialSpeed,
            _maximumInheritedAngularSpeed);
        _groupBuilder.Rebuild(blocks, settings);
    }

    public void RegisterControlUnit(ControlUnit unit)
    {
        RegisterControlUnitWithoutCleanup(unit);
        if (playMode)
        {
            RuntimeGroupCleanup.EnforceLimit(allControlUnits, maxUselessControlUnitGroups, DestroyManager.instance);
            RuntimeGroupCleanup.EnforceDistance(allControlUnits, playMode, blocksParent, groupCleanupDistance, DestroyManager.instance);
        }
    }

    internal void RegisterControlUnitWithoutCleanup(ControlUnit unit)
    {
        if (unit != null && !allControlUnits.Contains(unit))
        {
            allControlUnits.Add(unit);
        }
    }

    public void UnregisterControlUnit(ControlUnit unit)
    {
        if (allControlUnits.Contains(unit))
        {
            allControlUnits.Remove(unit);
        }
    }

    public IReadOnlyList<ControlUnit> GetControlUnits()
    {
        for (int i = allControlUnits.Count - 1; i >= 0; i--)
        {
            if (allControlUnits[i] == null)
            {
                allControlUnits.RemoveAt(i);
            }
        }

        return allControlUnits;
    }
}

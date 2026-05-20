using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayManager : MonoBehaviour
{
    public static PlayManager instance;
    public bool playMode = false;
    public Transform blocksParent;

    public Camera mainCamera;
    public LayerMask blockLayer;

    private Material originalMaterial;
    private Renderer selectedRenderer;
    public Material highlightMaterial;

    public Block selectedBlock;
    public List<ControlUnit> allControlUnits = new List<ControlUnit>();

    public float lastHeight;
    public float currentHeight;
    public float verticalVelocity;
    public float horizontalVelocity;

    public bool lockView = false;
    public bool showConnectors = true;
    public bool showLabel = true;

    [Tooltip("Show runtime debug UI")]
    public bool showUI = true;

    private void Awake()
    {
        instance = this;
    }

    private void FixedUpdate()
    {
        if (!playMode) return;

        if (blocksParent == null)
        {
            MainUIPanels.instance.PlayEnd();
            return;
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            lockView = !lockView;
            SetPlayMode();
        }

        HandleSelection();
        CalculateVelocity();
    }

    public void PlayStart()
    {
        if (!CanStartPlay(out string reason))
        {
            Debug.LogWarning(reason);
            return;
        }

        ControlUnit controlUnit = BuildManager.instance.blocksParent.GetComponent<ControlUnit>();
        RefreshGroup(controlUnit);

        lastHeight = blocksParent.position.y;

        BuildManager.instance.DeselectBlock();
        BuildManager.instance.enabled = false;

        playMode = true;
    }

    public bool CanStartPlay(out string reason)
    {
        reason = string.Empty;

        if (BuildManager.instance == null || BuildManager.instance.blocksParent == null)
        {
            reason = "Cannot start play mode without a loaded construct.";
            return false;
        }

        if (BuildManager.instance.enemyBlueprintBuildMode)
        {
            reason = "Cannot start player play mode while editing an enemy blueprint.";
            return false;
        }

        if (!ModularUnitValidator.TryGetSingleCockpit(BuildManager.instance.blocksParent, out Cockpit cockpit, out reason))
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
        if (unit == null) return;

        List<Block> blocks = unit.GetComponentsInChildren<Block>().ToList();
        if (blocks.Count > 1)
        {
            AssignBlocksToParentGroups(blocks);
        }

        foreach (ControlUnit controlUnit in allControlUnits.ToArray())
        {
            if (controlUnit != null)
            {
                controlUnit.RefreshChildren();
            }
        }

        Debug.Log($"Find {allControlUnits.Count} controls");
    }

    private void SetPlayMode()
    {
        Cursor.lockState = lockView ? CursorLockMode.Confined : CursorLockMode.Locked;

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

    public void AssignBlocksToParentGroups(List<Block> blocks)
    {
        List<List<Block>> groups = BlockGroupManager.GroupBlocks(blocks);
        GameObject parentPrefab = Resources.Load<GameObject>("Prefabs/BlocksParent");
        int groupIndex = 1;

        foreach (List<Block> group in groups)
        {
            GameObject groupParent = Instantiate(parentPrefab);
            groupParent.name = $"Group_{groupIndex++}";
            groupParent.transform.position = BlockGroupManager.CalculateGroupCenter(group);

            float mass = 0f;
            ControlUnit groupControl = groupParent.GetComponent<ControlUnit>();

            foreach (Block block in group)
            {
                Cockpit cockpit = block.GetComponent<Cockpit>();
                if (cockpit != null)
                {
                    groupControl.faction = cockpit.faction;

                    if (cockpit.faction == UnitFaction.Player)
                    {
                        groupParent.name = SaveManager.instance.currentSaveName;
                        blocksParent = groupParent.transform;
                        BuildManager.instance.blocksParent = groupParent.transform;
                        Debug.Log($"Change new block parent {blocksParent}.");
                    }
                }

                block.transform.SetParent(groupParent.transform);
                mass += block.mass;
                block.showConnectors = showConnectors;
                block.showLabel = showLabel;
            }

            Debug.Log($"{groupParent.name} mass: {mass}");

            Rigidbody rb = groupParent.GetComponent<Rigidbody>();
            rb.mass = mass;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 2f;
            rb.isKinematic = false;

            groupControl.RefreshChildren();
        }

        if (blocksParent != null)
        {
            Camera.main.GetComponent<CameraController>().playerBody = blocksParent;
        }
    }

    public void RegisterControlUnit(ControlUnit unit)
    {
        if (!allControlUnits.Contains(unit))
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
}

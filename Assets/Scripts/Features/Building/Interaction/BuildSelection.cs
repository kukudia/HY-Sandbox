// 职责：选择方块、恢复高亮材质和管理移动/旋转 Gizmo 的可见性。
// 入口：Poll() / SelectBlock() / DeselectBlock()；轴拖拽由 BuildManipulation 管理。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

internal sealed class BuildSelection
{
    private readonly BuildManipulation _manipulation;
    private readonly Dictionary<Renderer, Material[]> _selectedMaterials = new Dictionary<Renderer, Material[]>();
    private Camera mainCamera;
    private LayerMask axisLayer;
    private LayerMask blockLayer;
    private Material highlightMaterial;
    private GameObject moveAxis;
    private GameObject rotateAxis;
    private bool _gizmosEnabled = true;
    public Block selectedBlock;
    public BuildType currentBuildType;
    public BuildSelection(BuildManipulation manipulation)
    {
        _manipulation = manipulation;
    }

    public void Configure(
        Camera camera,
        LayerMask axes,
        LayerMask blocks,
        Material highlight,
        GameObject move,
        GameObject rotate)
    {
        mainCamera = camera;
        axisLayer = axes;
        blockLayer = blocks;
        highlightMaterial = highlight;
        moveAxis = move;
        rotateAxis = rotate;
    }

    public void Poll()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, axisLayer) && _manipulation.TryBeginDrag(hit, selectedBlock, currentBuildType))
            {
                return;
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
            }
        }
    }

    public void SelectBlock(Block block)
    {
        if (block == null)
        {
            DeselectBlock();
            return;
        }

        if (selectedBlock == block)
        {
            return;
        }

        DeselectBlock();
        selectedBlock = block;
        if (highlightMaterial != null)
        {
            Transform modelRoot = block.transform.Find("Model");
            Renderer[] renderers = modelRoot != null ? modelRoot.GetComponentsInChildren<Renderer>(true) : block.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                // Socket effects keep their authored materials while the solid model is selected.
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                Material[] originalMaterials = renderer.sharedMaterials;
                _selectedMaterials[renderer] = originalMaterials;
                var highlightedMaterials = new Material[originalMaterials.Length];
                for (int i = 0; i < highlightedMaterials.Length; i++)
                {
                    highlightedMaterials[i] = highlightMaterial;
                }

                renderer.sharedMaterials = highlightedMaterials;
            }
        }

        RefreshBuildGizmos();
        VisualEffectsManager.TryShowBlockSelection(selectedBlock);
    }

    public void DeselectBlock()
    {
        _manipulation.CancelDrag();
        VisualEffectsManager.TryClearBlockSelection(selectedBlock);
        foreach (KeyValuePair<Renderer, Material[]> entry in _selectedMaterials)
        {
            if (entry.Key != null)
            {
                entry.Key.sharedMaterials = entry.Value;
            }
        }

        _selectedMaterials.Clear();
        HideBuildGizmos();
        selectedBlock = null;
    }

    public void SetGizmosEnabled(bool enabled)
    {
        _gizmosEnabled = enabled;
        RefreshBuildGizmos();
    }

    public void RefreshBuildGizmos()
    {
        HideBuildGizmos();
        if (!_gizmosEnabled || selectedBlock == null || (currentBuildType != BuildType.Move && currentBuildType != BuildType.Rotate))
        {
            return;
        }

        GameObject gizmo = currentBuildType == BuildType.Move ? moveAxis : rotateAxis;
        if (gizmo == null)
        {
            return;
        }

        gizmo.SetActive(true);
        gizmo.transform.SetParent(selectedBlock.transform, true);
        gizmo.transform.position = selectedBlock.transform.position;
        gizmo.transform.rotation = Quaternion.LookRotation(_manipulation.AxisForward, _manipulation.AxisUp);
    }

    private void HideBuildGizmos()
    {
        if (moveAxis != null)
        {
            moveAxis.SetActive(false);
            moveAxis.transform.SetParent(null, true);
        }

        if (rotateAxis != null)
        {
            rotateAxis.SetActive(false);
            rotateAxis.transform.SetParent(null, true);
        }
    }
}

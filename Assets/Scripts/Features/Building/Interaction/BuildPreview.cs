// 职责：管理建造 Ghost、连接点提示、有效位置判定和确认放置。
// 入口：Configure() / Poll() / Clear()；不负责选择、加载、JSON 或 Undo。
// 生命周期：退出建造或销毁时 Clear() 释放 Ghost 和提示。
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

internal sealed class BuildPreview
{
    private readonly BuildPlacementGeometry _geometry;
    private readonly Action<GameObject, string, Vector3, Quaternion> _create;
    private Camera _camera;
    private Material _highlight;
    private ConnectorPlacementHints _hints;
    private float _range;
    public string ResourcePath;
    public bool PenetrationMode;
    public GameObject currentGhost;
    public Connector hoveredConnector;
    public BuildPreview(BuildPlacementGeometry geometry, Action<GameObject, string, Vector3, Quaternion> create)
    {
        _geometry = geometry;
        _create = create;
    }

    public void Configure(Camera camera, float range, Material highlight, ConnectorPlacementHints hints)
    {
        _camera = camera;
        _range = range;
        _highlight = highlight;
        _hints = hints;
    }

    public bool IsWithinBuildRange(Vector3 position)
    {
        return _camera != null && (position - _camera.transform.position).sqrMagnitude <= _range * _range;
    }

    public void Poll()
    {
        GameObject prefab = Resources.Load<GameObject>(ResourcePath);
        Block prefabBlock = prefab != null ? prefab.GetComponent<Block>() : null;
        if (_camera == null || prefabBlock == null)
        {
            Clear();
            return;
        }

        if (_hints != null)
        {
            _hints.Show(_camera, _range, _geometry.blockLayer, currentGhost);
        }

        // Keep the range visible while aiming at empty space or browsing UI, but never place through UI.
        if (Mouse.current == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            Clear(false);
            return;
        }

        hoveredConnector = null;
        Vector3 rawPos = Vector3.zero;
        Vector3 snappedPos = Vector3.zero;
        Vector3 nearestWorldPos = Vector3.zero;
        Vector3 nearestNormal = Vector3.zero;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _camera.ScreenPointToRay(mousePos);
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            _range + Vector3.Distance(ray.origin, _camera.transform.position),
            _geometry.blockLayer))
        {
            Block block = hit.collider.GetComponentInParent<Block>();
            if (block != null)
            {
                // 找最近的 connector
                float minDist = float.MaxValue;
                Connector nearest = null;
                foreach (var c in block.connectors)
                {
                    if (!block.IsConnectorAvailableForPlacement(c))
                    {
                        continue;
                    }

                    Vector3 worldPos = block.GetConnectorWorldPosition(c);
                    if (!IsWithinBuildRange(worldPos))
                    {
                        continue;
                    }

                    float dist = Vector3.Distance(hit.point, worldPos);
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearest = c;
                        nearestWorldPos = worldPos;
                        nearestNormal = block.GetConnectorWorldNormal(c);
                    }
                }

                if (nearest != null)
                {
                    hoveredConnector = nearest;
                    // 生成或更新 Ghost
                    if (currentGhost == null)
                    {
                        currentGhost = Object.Instantiate(prefab);
                        Collider[] colliders = currentGhost.GetComponentsInChildren<Collider>();
                        foreach (var collider in colliders)
                        {
                            collider.gameObject.layer = 0;
                        }
                    }

                    // Put the preview's nearest face on the target connector. The old fixed 0.5 offset
                    // only worked for 1x1x1 blocks and left larger prefabs floating or intersecting.
                    // Match the target's quarter-turn orientation so connector faces stay coplanar
                    // when the target has been rotated in the build grid.
                    currentGhost.transform.rotation = block.transform.rotation;
                    Quaternion previewRotation = currentGhost.transform.rotation;
                    if (!_geometry.TryCalculateConnectorAlignedPosition(prefabBlock, previewRotation, nearestWorldPos, nearestNormal, out snappedPos))
                    {
                        float previewHalfExtent = _geometry.GetHalfExtentAlongDirection(prefabBlock, previewRotation, nearestNormal);
                        rawPos = nearestWorldPos + nearestNormal * previewHalfExtent;
                        snappedPos = _geometry.SnapCenterByMinCorner(rawPos, previewRotation, prefabBlock);
                    }

                    currentGhost.transform.position = snappedPos;
                }
            }
        }
        else
        {
            if (currentGhost != null)
            {
                Clear(false);
            }
        }

        if (hoveredConnector == null)
        {
            Clear(false);
            return;
        }

        if (currentGhost != null)
        {
            Block ghostBlock = currentGhost.GetComponent<Block>();
            bool isBlocked = ghostBlock == null || _geometry.IsBlocked(currentGhost.transform.position, currentGhost.transform.rotation, prefabBlock) || (ghostBlock != null && ghostBlock.IsBlockedGhost());
            if (PenetrationMode)
            {
                // Bound the search when a target is enclosed or its normal is invalid.
                for (int step = 0; isBlocked && step < 64 && nearestNormal.sqrMagnitude > 0.5f; step++)
                {
                    Vector3 nextPosition = currentGhost.transform.position + nearestNormal;
                    if (!IsWithinBuildRange(nextPosition))
                    {
                        break;
                    }

                    currentGhost.transform.position = nextPosition;
                    isBlocked = _geometry.IsBlocked(currentGhost.transform.position, currentGhost.transform.rotation, prefabBlock) || (ghostBlock != null && ghostBlock.IsBlockedGhost());
                }
            }

            isBlocked |= !IsWithinBuildRange(currentGhost.transform.position);
            Renderer[] renderers = currentGhost.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                {
                    continue;
                }

                renderer.material = _highlight;
                if (renderer.material.HasProperty("_Color"))
                {
                    renderer.material.color = isBlocked ? new Color(1, 0, 0, 0.5f) : new Color(0, 1, 0, 0.5f);
                }
            }

            VisualEffectsManager.TryUpdateGhostPreview(currentGhost, isBlocked);
            // 鼠标左键点击 → 真正生成方块
            if (Mouse.current.leftButton.wasPressedThisFrame && !isBlocked)
            {
                _create(prefab, ResourcePath, currentGhost.transform.position, currentGhost.transform.rotation);
            }
        }
    }

    public void Clear(bool hideHints = true)
    {
        if (hideHints && _hints != null)
        {
            _hints.Hide();
        }

        hoveredConnector = null;
        if (currentGhost == null)
        {
            return;
        }

        VisualEffectsManager.TryClearGhostPreview(currentGhost);
        Object.Destroy(currentGhost);
        currentGhost = null;
        hoveredConnector = null;
    }
}

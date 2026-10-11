// 职责：集中建造尺寸、连接点对齐、半网格吸附、阻挡与步长几何。
// 入口：SnapCenterByMinCorner() / TryCalculateConnectorAlignedPosition() / IsBlocked()。
// 依赖：显式网格尺寸和方块层；无管理器单例、UI 或存档依赖。
using UnityEngine;

internal sealed class BuildPlacementGeometry
{
    public float gridSize;
    public LayerMask blockLayer;
    public Vector3 GetBlockHalfExtents(Block block)
    {
        if (block == null)
        {
            return Vector3.zero;
        }

        float safeGridSize = Mathf.Max(0.0001f, gridSize);
        return new Vector3(block.x, block.y, block.z) * safeGridSize * 0.5f;
    }

    public float GetHalfExtentAlongDirection(Block block, Quaternion rotation, Vector3 worldDirection)
    {
        Vector3 direction = worldDirection.normalized;
        Vector3 halfExtents = GetBlockHalfExtents(block);
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        Vector3 forward = rotation * Vector3.forward;
        return Mathf.Abs(Vector3.Dot(direction, right)) * halfExtents.x + Mathf.Abs(Vector3.Dot(direction, up)) * halfExtents.y + Mathf.Abs(Vector3.Dot(direction, forward)) * halfExtents.z;
    }

    public bool TryCalculateConnectorAlignedPosition(
        Block prefabBlock,
        Quaternion previewRotation,
        Vector3 targetConnectorPosition,
        Vector3 targetConnectorNormal,
        out Vector3 position)
    {
        position = Vector3.zero;
        if (prefabBlock == null || prefabBlock.connectors == null)
        {
            return false;
        }

        Transform connectorRoot = prefabBlock.connectorParent != null ? prefabBlock.connectorParent : prefabBlock.transform;
        Connector bestConnector = null;
        float bestNormalMatch = 0.95f;
        for (int i = 0; i < prefabBlock.connectors.Count; i++)
        {
            Connector connector = prefabBlock.connectors[i];
            if (connector == null || !connector.canConnect)
            {
                continue;
            }

            Vector3 localNormal = prefabBlock.transform.InverseTransformDirection(connectorRoot.TransformDirection(connector.normal));
            Vector3 connectorNormal = previewRotation * localNormal;
            float normalMatch = Vector3.Dot(connectorNormal.normalized, -targetConnectorNormal.normalized);
            if (normalMatch > bestNormalMatch)
            {
                bestNormalMatch = normalMatch;
                bestConnector = connector;
            }
        }

        if (bestConnector == null)
        {
            return false;
        }

        Vector3 connectorLocalPosition = prefabBlock.transform.InverseTransformPoint(connectorRoot.TransformPoint(bestConnector.localPos));
        connectorLocalPosition = Vector3.Scale(connectorLocalPosition, prefabBlock.transform.localScale);
        position = targetConnectorPosition - previewRotation * connectorLocalPosition;
        return true;
    }

    public Vector3 SnapCenterByMinCorner(Vector3 targetCenter, Quaternion targetRotation, Block b)
    {
        if (b == null)
        {
            return targetCenter;
        }

        // 方块的局部半尺寸（不含旋转）
        Vector3 halfSize = GetBlockHalfExtents(b);
        float safeGridSize = Mathf.Max(0.0001f, gridSize);
        float snapStep = safeGridSize * 0.5f;
        // 计算旋转后的 8 个顶点
        Vector3[] corners = new Vector3[8];
        int i = 0;
        for (int xi = -1; xi <= 1; xi += 2)
        {
            for (int yi = -1; yi <= 1; yi += 2)
            {
                for (int zi = -1; zi <= 1; zi += 2)
                {
                    Vector3 localCorner = new Vector3(xi * halfSize.x, yi * halfSize.y, zi * halfSize.z);
                    corners[i++] = targetCenter + targetRotation * localCorner;
                }
            }
        }

        // 得到 AABB 的 min/max
        Vector3 min = corners[0];
        Vector3 max = corners[0];
        foreach (var c in corners)
        {
            min = Vector3.Min(min, c);
            max = Vector3.Max(max, c);
        }

        // Block centers are allowed on half-grid coordinates (Block.Awake preserves 0.5 steps),
        // so align bounds to half-grid steps instead of moving odd-sized blocks by half a cell.
        Vector3 snappedMin = new Vector3(
            Mathf.Round((min.x - Vector3.zero.x) / snapStep) * snapStep + Vector3.zero.x,
            Mathf.Round((min.y - Vector3.zero.y) / snapStep) * snapStep + Vector3.zero.y,
            Mathf.Round((min.z - Vector3.zero.z) / snapStep) * snapStep + Vector3.zero.z);
        Vector3 snappedCenter = snappedMin + (max - min) * 0.5f;
        return snappedCenter;
    }

    public bool IsBlocked(Vector3 targetCenter, Quaternion targetRotation, Block block)
    {
        if (block == null)
        {
            return true;
        }

        // 方块的半尺寸
        Vector3 halfExtents = GetBlockHalfExtents(block);
        // 检测范围（目标位置 + 半尺寸）
        Physics.SyncTransforms();
        Collider[] hits = Physics.OverlapBox(targetCenter, halfExtents, // 稍微缩小，避免边界浮点误差
            targetRotation, blockLayer // 只检测方块层
        );
        foreach (var hit in hits)
        {
            Block other = hit.GetComponentInParent<Block>();
            if (other != null && other != block)
            {
                Debug.Log($"被{other}阻挡");
                return true; // 有别的方块 → 阻挡
            }
        }

        return false;
    }

    public float GetMoveStep(Block block, Vector3 moveDir)
    {
        // 方块的局部半尺寸（不考虑旋转）
        Vector3 halfSize = new Vector3(block.x, block.y, block.z) * gridSize * 0.5f;
        // 方块旋转
        Quaternion rot = block.transform.rotation;
        // 取旋转后局部坐标轴
        Vector3 right = rot * Vector3.right;
        Vector3 up = rot * Vector3.up;
        Vector3 forward = rot * Vector3.forward;
        // 移动方向（归一化）
        Vector3 dir = moveDir.normalized;
        // 在这个方向上的“投影厚度” = 各轴厚度在 dir 上的分量绝对值
        float step = Mathf.Abs(Vector3.Dot(dir, right)) * (halfSize.x * 2) + Mathf.Abs(Vector3.Dot(dir, up)) * (halfSize.y * 2) + Mathf.Abs(Vector3.Dot(dir, forward)) * (halfSize.z * 2);
        return step;
    }
}

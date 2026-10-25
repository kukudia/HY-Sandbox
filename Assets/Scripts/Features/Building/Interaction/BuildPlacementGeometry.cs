// 职责：集中建造尺寸、连接点对齐、旋转网格锚点、半网格吸附与阻挡几何。
// 入口：CalculateRotationPosition() / SnapRotationToGrid() / TryCalculateConnectorAlignedPosition() / IsBlocked()。
// 依赖：显式网格尺寸和方块层；无管理器单例、UI 或存档依赖。
using UnityEngine;

internal sealed class BuildPlacementGeometry
{
    public float gridSize;
    public LayerMask blockLayer;
    public Quaternion SnapRotationToGrid(Quaternion rotation)
    {
        Vector3 euler = rotation.eulerAngles;
        return Quaternion.Euler(
            (Mathf.RoundToInt(euler.x / 90f) % 4) * 90f,
            (Mathf.RoundToInt(euler.y / 90f) % 4) * 90f,
            (Mathf.RoundToInt(euler.z / 90f) % 4) * 90f);
    }

    public Vector3 CalculateRotationPosition(Vector3 startCenter, Quaternion startRotation, Quaternion targetRotation, Block block)
    {
        if (block == null)
        {
            return startCenter;
        }

        // 锚点是中心所在整数格的基准。按世界轴尺寸奇偶取正半格偏移，反向/180°不额外平移一格。
        // 在整数半格坐标中运算，不能把旋转后的浮点 AABB 尺寸直接加回中心。
        Vector3 localOffset = new Vector3(block.x % 2 == 0 ? 1 : 0, block.y % 2 == 0 ? 1 : 0, block.z % 2 == 0 ? 1 : 0);
        Vector3 oldOffset = SnapRotationToGrid(startRotation) * localOffset;
        Vector3 newOffset = SnapRotationToGrid(targetRotation) * localOffset;
        float safeGridSize = Mathf.Max(0.0001f, gridSize);
        Vector3 anchor = new Vector3(
            Mathf.Round(startCenter.x / safeGridSize - Mathf.Abs(Mathf.Round(oldOffset.x)) * 0.5f),
            Mathf.Round(startCenter.y / safeGridSize - Mathf.Abs(Mathf.Round(oldOffset.y)) * 0.5f),
            Mathf.Round(startCenter.z / safeGridSize - Mathf.Abs(Mathf.Round(oldOffset.z)) * 0.5f));
        return new Vector3(
            (anchor.x * 2f + Mathf.Abs(Mathf.Round(newOffset.x))) * (safeGridSize * 0.5f),
            (anchor.y * 2f + Mathf.Abs(Mathf.Round(newOffset.y))) * (safeGridSize * 0.5f),
            (anchor.z * 2f + Mathf.Abs(Mathf.Round(newOffset.z))) * (safeGridSize * 0.5f));
    }

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
        position = RemoveGridNoise(targetConnectorPosition - previewRotation * connectorLocalPosition);
        return true;
    }

    private Vector3 RemoveGridNoise(Vector3 position)
    {
        // 只清理由正交旋转带来的近网格尾差，不强制改变特殊底座/自定义连接点的有效位置。
        float step = Mathf.Max(0.0001f, gridSize) * 0.5f;
        float tolerance = step * 0.0002f;
        for (int axis = 0; axis < 3; axis++)
        {
            float snapped = Mathf.Round(position[axis] / step) * step;
            if (Mathf.Abs(position[axis] - snapped) <= tolerance)
            {
                position[axis] = snapped;
            }
        }

        return position;
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
        return RemoveGridNoise(snappedCenter);
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

using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    // 单例（方便调用）
    public static GridManager instance;

    // 记录每个占用格子 -> 对应的 Block
    private Dictionary<Vector3Int, Block> occupiedGrid = new Dictionary<Vector3Int, Block>();

    private void Awake()
    {
        instance = this;
    }

    /// <summary>
    /// 计算方块占用的所有格子
    /// </summary>
    public List<Vector3Int> GetOccupiedCells(Vector3 pos, Block block)
    {
        List<Vector3Int> cells = new List<Vector3Int>();

        // 对齐中心位置（以最小角为基准）
        Vector3 basePos = BuildManager.instance.SnapCenterByMinCorner(pos, block);

        // 找到最小角
        Vector3 minCorner = basePos - new Vector3(block.x, block.y, block.z) * 0.5f;

        for (int i = 0; i < block.x; i++)
        {
            for (int j = 0; j < block.y; j++)
            {
                for (int k = 0; k < block.z; k++)
                {
                    Vector3 cell = minCorner + new Vector3(i + 0.5f, j + 0.5f, k + 0.5f);
                    cells.Add(Vector3Int.RoundToInt(cell));
                }
            }
        }

        return cells;
    }

    /// <summary>
    /// 检查是否阻挡
    /// </summary>
    public bool IsBlocked(Vector3 pos, Block block)
    {
        foreach (var cell in GetOccupiedCells(pos, block))
        {
            if (occupiedGrid.ContainsKey(cell))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 注册方块到网格
    /// </summary>
    public void AddBlock(Block block)
    {
        Vector3 pos = block.transform.position;
        foreach (var cell in GetOccupiedCells(pos, block))
        {
            occupiedGrid[cell] = block;
        }
    }

    /// <summary>
    /// 移除方块占用
    /// </summary>
    public void RemoveBlock(Block block)
    {
        Vector3 pos = block.transform.position;
        foreach (var cell in GetOccupiedCells(pos, block))
        {
            if (occupiedGrid.ContainsKey(cell) && occupiedGrid[cell] == block)
            {
                occupiedGrid.Remove(cell);
            }
        }
    }

    /// <summary>
    /// Gizmos 可视化
    /// </summary>
    private void OnDrawGizmos()
    {
        if (occupiedGrid == null) return;

        Gizmos.color = new Color(0, 1, 0, 0.3f);
        foreach (var kvp in occupiedGrid)
        {
            Vector3 pos = kvp.Key;
            Gizmos.DrawCube(pos, Vector3.one * 0.9f);
        }
    }
}

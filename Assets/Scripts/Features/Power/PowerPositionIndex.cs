// 职责：按世界坐标网格索引供电参与者，复用桶与候选列表以减少全体范围扫描。
// 入口：Clear() / Add() / Query()；候选仍须由调用者执行精确盒边界检查。
using System.Collections.Generic;
using UnityEngine;

internal sealed class PowerPositionIndex
{
    private const float CellSize = 10f;
    private readonly Dictionary<Vector3Int, List<int>> _cells = new Dictionary<Vector3Int, List<int>>();
    private readonly Stack<List<int>> _freeBuckets = new Stack<List<int>>();
    private int _count;
    internal void Clear()
    {
        foreach (List<int> bucket in _cells.Values)
        {
            bucket.Clear();
            _freeBuckets.Push(bucket);
        }

        _cells.Clear();
        _count = 0;
    }

    internal void Add(Vector3 position, int index)
    {
        Vector3Int cell = GetCell(position);
        if (!_cells.TryGetValue(cell, out List<int> bucket))
        {
            bucket = _freeBuckets.Count > 0 ? _freeBuckets.Pop() : new List<int>();
            _cells.Add(cell, bucket);
        }

        bucket.Add(index);
        _count++;
    }

    internal void Query(Vector3 position, float range, List<int> result)
    {
        result.Clear();
        range = Mathf.Max(0f, range);
        Vector3Int min = GetCell(position - Vector3.one * range);
        Vector3Int max = GetCell(position + Vector3.one * range);
        double cells = ((double)max.x - min.x + 1) * ((double)max.y - min.y + 1) * ((double)max.z - min.z + 1);
        // 巨大盒范围直接扫已占用的桶，避免遍历大量空单元或坐标溢出。
        if (cells > Mathf.Max(64, _count * 2) || max.x == int.MaxValue || max.y == int.MaxValue || max.z == int.MaxValue)
        {
            foreach (List<int> bucket in _cells.Values)
            {
                result.AddRange(bucket);
            }

            return;
        }

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    if (_cells.TryGetValue(new Vector3Int(x, y, z), out List<int> bucket))
                    {
                        result.AddRange(bucket);
                    }
                }
            }
        }
    }

    private static Vector3Int GetCell(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / CellSize),
            Mathf.FloorToInt(position.y / CellSize),
            Mathf.FloorToInt(position.z / CellSize));
    }
}

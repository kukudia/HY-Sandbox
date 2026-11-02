// 职责：按六邻接找金属连通结构，接触土/岩视为有支撑；只返回有限尺寸的无支撑片段。
// 入口：FindDetached()；只读取体素，不将土/岩地形转成动态刚体。
using System.Collections.Generic;
using UnityEngine;

public static class VoxelStructureSupport
{
    private static readonly Vector3Int[] Neighbors =
    {
        Vector3Int.right,
        Vector3Int.left,
        Vector3Int.up,
        Vector3Int.down,
        Vector3Int.forward,
        Vector3Int.back
    };
    public static List<List<Vector3Int>> FindDetached(VoxelWorldData data, int maximumCells, int maximumGroups)
    {
        var detached = new List<List<Vector3Int>>();
        if (maximumCells < 1 || maximumGroups < 1)
        {
            return detached;
        }

        var visited = new HashSet<Vector3Int>();
        var queue = new Queue<Vector3Int>();
        for (int z = 0; z < data.Size.z; z++)
        {
            for (int y = 0; y < data.Size.y; y++)
            {
                for (int x = 0; x < data.Size.x; x++)
                {
                    var start = new Vector3Int(x, y, z);
                    if (data.Get(start) != 3 || !visited.Add(start))
                    {
                        continue;
                    }

                    var component = new List<Vector3Int>();
                    bool supported = false;
                    queue.Enqueue(start);
                    while (queue.Count > 0)
                    {
                        Vector3Int cell = queue.Dequeue();
                        supported |= cell.y == 0;
                        component.Add(cell);
                        foreach (Vector3Int direction in Neighbors)
                        {
                            Vector3Int neighbor = cell + direction;
                            byte material = data.Get(neighbor);
                            if (material == 1 || material == 2)
                            {
                                supported = true;
                            }
                            else if (material == 3 && visited.Add(neighbor))
                            {
                                queue.Enqueue(neighbor);
                            }
                        }
                    }

                    if (!supported && component.Count <= maximumCells)
                    {
                        detached.Add(component);
                        if (detached.Count >= maximumGroups)
                        {
                            return detached;
                        }
                    }
                }
            }
        }

        return detached;
    }
}

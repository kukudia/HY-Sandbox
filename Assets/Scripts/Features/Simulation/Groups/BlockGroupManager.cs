// 职责：按方块连接关系分组并计算组中心，供游玩物理单元建立使用。
// 入口：GroupBlocks() / CalculateGroupCenter()。
using System.Collections.Generic;
using UnityEngine;

public class BlockGroupManager : MonoBehaviour
{
    // Synchronize once per traversal; reuse queue and neighbor storage across groups.
    public static List<List<Block>> GroupBlocks(List<Block> allBlocks)
    {
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.Connectivity, allBlocks?.Count ?? 0);
        return Traverse(allBlocks, false);
    }

    internal static List<List<Block>> GroupRuntimeBlocks(List<Block> allBlocks)
    {
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.Connectivity, allBlocks?.Count ?? 0);
        if (allBlocks == null || allBlocks.Count == 0)
        {
            return new List<List<Block>>();
        }

        foreach (Block block in allBlocks)
        {
            if (block != null && block.isActiveAndEnabled && !block.HasValidRuntimeConnections)
            {
                // 蓝图/制作变更重新探测一次；刚性运动和显式断链复用双侧邻接，不做逐面PhysX查询。
                new BatchConnectionBuilder(allBlocks).RebuildBatchImmediate();
                break;
            }
        }

        return Traverse(allBlocks, true);
    }

    private static List<List<Block>> Traverse(List<Block> allBlocks, bool runtimeLinks)
    {
        List<List<Block>> groups = new List<List<Block>>();
        if (allBlocks == null || allBlocks.Count == 0)
        {
            return groups;
        }

        HashSet<Block> remaining = new HashSet<Block>();
        foreach (Block block in allBlocks)
        {
            if (block != null && block.isActiveAndEnabled)
            {
                remaining.Add(block);
            }
        }

        if (remaining.Count == 0)
        {
            return groups;
        }

        if (!runtimeLinks)
        {
            Physics.SyncTransforms();
        }

        Queue<Block> queue = new Queue<Block>();
        List<Block> neighbors = new List<Block>();
        foreach (Block block in allBlocks)
        {
            if (block == null || !remaining.Remove(block))
            {
                continue;
            }

            List<Block> group = new List<Block>();
            queue.Enqueue(block);
            while (queue.Count > 0)
            {
                Block current = queue.Dequeue();
                group.Add(current);
                if (runtimeLinks)
                {
                    current.CollectRuntimeNeighbors(neighbors);
                }
                else
                {
                    current.CollectNeighbors(neighbors);
                }

                foreach (Block neighbor in neighbors)
                {
                    // Removal both bounds traversal to the input and marks visited.
                    if (remaining.Remove(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            groups.Add(group);
        }

        return groups;
    }

    // 计算组的中心点（可选）- 优化版本
    public static Vector3 CalculateGroupCenter(List<Block> group)
    {
        if (group == null || group.Count == 0)
        {
            return Vector3.zero;
        }

        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Block block in group)
        {
            if (block != null)
            {
                center += block.transform.position;
                count++;
            }
        }

        return count > 0 ? center / count : Vector3.zero;
    }
}

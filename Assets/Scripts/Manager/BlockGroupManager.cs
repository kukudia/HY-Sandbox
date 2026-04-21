using System.Collections.Generic;
using UnityEngine;

public class BlockGroupManager : MonoBehaviour
{
    // 优化的分组算法：使用更高效的邻接表构建和 BFS 遍历
    public static List<List<Block>> GroupBlocks(List<Block> allBlocks)
    {
        if (allBlocks == null || allBlocks.Count == 0) return new List<List<Block>>();
        
        // 预先分配容量，减少扩容开销
        int blockCount = allBlocks.Count;
        Dictionary<Block, List<Block>> adjacencyList = new Dictionary<Block, List<Block>>(blockCount);
        
        // 构建邻接表 - 避免重复计算
        foreach (Block block in allBlocks)
        {
            if (block == null) continue;
            List<Block> neighbors = block.Neighbors();
            adjacencyList[block] = neighbors;
        }

        // BFS 遍历分组 - 使用预分配的 HashSet
        List<List<Block>> groups = new List<List<Block>>();
        HashSet<Block> visited = new HashSet<Block>(blockCount);

        foreach (Block block in allBlocks)
        {
            if (block == null || visited.Contains(block)) continue;

            List<Block> currentGroup = new List<Block>();
            Queue<Block> queue = new Queue<Block>(blockCount);
            queue.Enqueue(block);
            visited.Add(block);

            while (queue.Count > 0)
            {
                Block current = queue.Dequeue();

                if (current == null) continue;

                if (!adjacencyList.TryGetValue(current, out List<Block> neighbors) || neighbors == null) continue;

                currentGroup.Add(current);

                foreach (Block neighbor in neighbors)
                {
                    if (neighbor != null && !visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            groups.Add(currentGroup);
        }

        return groups;
    }

    // 计算组的中心点（可选）- 优化版本
    public static Vector3 CalculateGroupCenter(List<Block> group)
    {
        if (group == null || group.Count == 0) return Vector3.zero;

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

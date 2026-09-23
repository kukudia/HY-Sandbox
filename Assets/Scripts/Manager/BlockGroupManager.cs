using System.Collections.Generic;
using UnityEngine;

public class BlockGroupManager : MonoBehaviour
{
    // Synchronize once per traversal; reuse queue and neighbor storage across groups.
    public static List<List<Block>> GroupBlocks(List<Block> allBlocks)
    {
        List<List<Block>> groups = new List<List<Block>>();
        if (allBlocks == null || allBlocks.Count == 0) return groups;
        HashSet<Block> remaining = new HashSet<Block>();
        foreach (Block block in allBlocks)
        {
            if (block != null && block.isActiveAndEnabled) remaining.Add(block);
        }
        if (remaining.Count == 0) return groups;

        Physics.SyncTransforms();
        Queue<Block> queue = new Queue<Block>();
        List<Block> neighbors = new List<Block>();
        foreach (Block block in allBlocks)
        {
            if (block == null || !remaining.Remove(block)) continue;
            List<Block> group = new List<Block>();
            queue.Enqueue(block);
            while (queue.Count > 0)
            {
                Block current = queue.Dequeue();
                group.Add(current);
                current.CollectNeighbors(neighbors);
                foreach (Block neighbor in neighbors)
                {
                    // Removal both bounds traversal to the input and marks visited.
                    if (remaining.Remove(neighbor)) queue.Enqueue(neighbor);
                }
            }
            groups.Add(group);
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

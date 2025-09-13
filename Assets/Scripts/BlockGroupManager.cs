using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class BlockGroupManager : MonoBehaviour
{
    // 分组算法（与之前相同）
    public static List<List<Block>> GroupBlocks(List<Block> allBlocks)
    {
        // 更新所有Block的连接状态
        //foreach (Block block in allBlocks)
        //{
        //    block.CheckConnection();
        //}

        // 构建邻接表
        Dictionary<Block, List<Block>> adjacencyList = new Dictionary<Block, List<Block>>();
        foreach (Block block in allBlocks)
        {
            List<Block> neighbors = block.Neighbors();
            adjacencyList[block] = neighbors;
        }

        // BFS遍历分组
        List<List<Block>> groups = new List<List<Block>>();
        HashSet<Block> visited = new HashSet<Block>();

        foreach (Block block in allBlocks)
        {
            if (visited.Contains(block)) continue;

            List<Block> currentGroup = new List<Block>();
            Queue<Block> queue = new Queue<Block>();
            queue.Enqueue(block);
            visited.Add(block);

            while (queue.Count > 0)
            {
                Block current = queue.Dequeue();
                currentGroup.Add(current);

                if (adjacencyList[current] == null) continue;

                foreach (Block neighbor in adjacencyList[current])
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

    // 计算组的中心点（可选）
    public static Vector3 CalculateGroupCenter(List<Block> group)
    {
        if (group == null || group.Count == 0) return Vector3.zero;

        Vector3 center = Vector3.zero;
        foreach (Block block in group)
        {
            center += block.transform.position;
        }
        return center / group.Count;
    }
}
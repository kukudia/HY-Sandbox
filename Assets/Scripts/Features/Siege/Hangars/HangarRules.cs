// 职责：共享基地编辑和加载时的机库数量、直立姿态规则。
// 入口：MaximumCount / Validate()；不修改蓝图或玩家存档。
using UnityEngine;

public static class HangarRules
{
    private static HangarLimits limits;
    public static int MaximumCount
    {
        get
        {
            if (limits == null)
            {
                limits = Resources.Load<HangarLimits>("Hangars/Limits");
            }

            return limits != null ? limits.MaximumCount : 6;
        }
    }

    public static long ShellCost(BlockData block)
    {
        _ = MaximumCount; // Resolve the shared configuration once before either editing or deployment.
        long area = (long)block.x * block.z * 2 + (long)block.y * (block.x + block.z) * 2;
        return area * (limits != null ? limits.ShellCoinsPerSquareMetre : 1);
    }

    public static bool IsHangar(BlockData block)
    {
        GameObject prefab = BlockPrefabCache.GetPrefab(BlueprintBlockFactory.ToResourcePath(block.resourcePath));
        return prefab != null && prefab.GetComponent<HangarModule>() != null;
    }

    public static bool Validate(BlockDataList data, out string reason)
    {
        int count = 0;
        foreach (BlockData block in data.blocks)
        {
            if (!IsHangar(block))
            {
                continue;
            }

            count++;
            Quaternion rotation = new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
            if (Vector3.Dot(rotation * Vector3.up, Vector3.up) < .999f || block.x < 3 || block.y < 2 || block.z < 3)
            {
                reason = "Hangars must remain upright and contain positive clear space.";
                return false;
            }
        }

        reason = count > MaximumCount ? $"A base supports at most {MaximumCount} hangars." : string.Empty;
        return count <= MaximumCount;
    }
}

// 职责：统一蓝图完整刷新与增量恢复的质量、耗电和发电统计。
// 入口：Accumulate() / Calculate()；不读取管理器单例，不更新 UI。
using System.Collections.Generic;
using UnityEngine;

public struct BlueprintStatistics
{
    public int BlockCount
    {
        get; private set;
    }
    public float Mass
    {
        get; private set;
    }
    public float RequiredPower
    {
        get; private set;
    }
    public float GeneratorOutput
    {
        get; private set;
    }

    /// <summary>仅累计实际恢复成功的 Block；负功率不计入统计。</summary>
    public void Accumulate(Block block)
    {
        if (block == null)
        {
            return;
        }

        BlockCount++;
        Mass += block.mass;
        if (block.TryGetComponent(out Power power))
        {
            RequiredPower += Mathf.Max(0f, power.standardWorkingPower);
        }

        if (block.TryGetComponent(out PowerGeneratingUnit generator))
        {
            GeneratorOutput += Mathf.Max(0f, generator.outputPower);
        }
    }

    /// <summary>完整刷新保持原语义：数量来自存档，物理/功率来自匹配 ID 的场景实体。</summary>
    public static BlueprintStatistics Calculate(List<BlockData> savedBlocks, Transform parent)
    {
        var statistics = new BlueprintStatistics();
        if (savedBlocks == null)
        {
            return statistics;
        }

        var ids = new HashSet<string>();
        foreach (BlockData data in savedBlocks)
        {
            if (data != null && !string.IsNullOrEmpty(data.id))
            {
                ids.Add(data.id);
            }
        }

        if (parent != null)
        {
            foreach (Block block in parent.GetComponentsInChildren<Block>(true))
            {
                if (block != null && ids.Contains(block.uniqueId))
                {
                    statistics.Accumulate(block);
                }
            }
        }

        statistics.BlockCount = savedBlocks.Count;
        return statistics;
    }
}

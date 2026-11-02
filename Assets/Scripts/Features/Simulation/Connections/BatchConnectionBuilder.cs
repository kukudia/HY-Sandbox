// 职责：批量重建方块邻接与双侧连接；每个执行片段统一同步物理。
// 入口：RebuildBatchRoutine() / RebuildBatchImmediate()；分帧版本用于加载中的静止蓝图。
// 约束：预算在完整单块操作后检查，不保证单个昂贵操作低于预算；恢复后重新同步物理。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BatchConnectionBuilder
{
    private readonly List<Block> _blocks;
    private readonly double _frameBudgetMs;
    public BatchConnectionBuilder(List<Block> blocks, float frameBudgetMs = 2f)
    {
        _blocks = blocks ?? new List<Block>();
        _frameBudgetMs = Mathf.Max(0.1f, frameBudgetMs);
    }

    public IEnumerator RebuildBatchRoutine()
    {
        if (_blocks.Count == 0)
        {
            yield break;
        }

        Physics.SyncTransforms();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int index = 0; index < _blocks.Count; index++)
        {
            RefreshBlock(_blocks[index]);
            if (index < _blocks.Count - 1 && stopwatch.Elapsed.TotalMilliseconds >= _frameBudgetMs)
            {
                yield return null;
                // Other systems may move colliders while this iterator is suspended.
                Physics.SyncTransforms();
                stopwatch.Restart();
            }
        }
    }

    public void RebuildBatchImmediate()
    {
        if (_blocks.Count == 0)
        {
            return;
        }

        Physics.SyncTransforms();
        foreach (Block block in _blocks)
        {
            RefreshBlock(block);
        }
    }

    private static void RefreshBlock(Block block)
    {
        if (block == null)
        {
            return;
        }

        if (block.neighbors == null)
        {
            block.neighbors = new List<Block>();
        }

        block.CollectNeighbors(block.neighbors);
        // Connection probing still queries physics; only the redundant sync is skipped.
        block.CheckConnectionAfterPhysicsSync();
    }
}

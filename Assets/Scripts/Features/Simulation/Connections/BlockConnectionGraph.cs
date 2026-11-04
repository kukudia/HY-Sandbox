// 职责：为建筑与载具准备同一份双侧连接图；几何失效重建，删除成员仅重探附近幸存模块。
// 入口：Prepare() / PrepareBlocks() / CollectNeighbors()；支撑和驾驶舱策略由消费者决定。
using System.Collections.Generic;
using UnityEngine;

internal sealed class BlockConnectionGraph
{
    private readonly Dictionary<Block, Bounds> members = new Dictionary<Block, Bounds>();
    private readonly HashSet<Block> current = new HashSet<Block>();
    private readonly List<Block> removed = new List<Block>();
    private readonly List<Block> affected = new List<Block>();
    private Transform boundsRoot;
    private Vector3 rootScale;
    internal int Revision
    {
        get; private set;
    }

    internal void Prepare(List<Block> blocks, Transform root)
    {
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingGraph, blocks.Count);
        current.Clear();
        bool rebuild = boundsRoot != root || !root.lossyScale.Equals(rootScale);
        foreach (Block block in blocks)
        {
            if (block != null && block.isActiveAndEnabled)
            {
                current.Add(block);
                rebuild |= !members.ContainsKey(block) || !block.HasValidRuntimeConnections;
            }
        }

        if (rebuild)
        {
            PrepareBlocks(blocks, true);
            members.Clear();
            Matrix4x4 worldToRoot = root.worldToLocalMatrix;
            foreach (Block block in current)
            {
                members.Add(block, ProbeBounds(block, worldToRoot));
            }

            boundsRoot = root;
            rootScale = root.lossyScale;
            Revision++;
            return;
        }

        removed.Clear();
        foreach (Block block in members.Keys)
        {
            if (!current.Contains(block))
            {
                removed.Add(block);
            }
        }

        if (removed.Count == 0)
        {
            return;
        }

        affected.Clear();
        foreach (Block block in current)
        {
            foreach (Block deleted in removed)
            {
                if (members[block].Intersects(members[deleted]))
                {
                    affected.Add(block);
                    break;
                }
            }
        }

        // 删除碰撞体可能曾遮住另一候选，不能只重建旧邻居。
        // 共用批量入口同步物理并维护双侧引用；未接触的远处连接与快照保持有效。
        new BatchConnectionBuilder(affected).RebuildBatchImmediate();
        foreach (Block block in removed)
        {
            members.Remove(block);
        }

        Revision++;
    }

    private static Bounds ProbeBounds(Block block, Matrix4x4 worldToRoot)
    {
        BoxCollider collider = block.raycastCollider;
        Bounds bounds = collider != null ? BlockBounds.Transform(new Bounds(collider.center, collider.size), worldToRoot * collider.transform.localToWorldMatrix) : new Bounds(worldToRoot.MultiplyPoint3x4(block.transform.position), Vector3.zero);
        foreach (Connector connector in block.connectors)
        {
            if (connector != null)
            {
                bounds.Encapsulate(worldToRoot.MultiplyPoint3x4(block.GetConnectorWorldPosition(connector)));
            }
        }

        // 连接探针向法线外移0.05m，球半径0.08m；按世界球在根局部空间的轴向范围保守扩张。
        const float reach = 0.13f;
        Vector3 padding = new Vector3(
            new Vector3(worldToRoot.m00, worldToRoot.m01, worldToRoot.m02).magnitude,
            new Vector3(worldToRoot.m10, worldToRoot.m11, worldToRoot.m12).magnitude,
            new Vector3(worldToRoot.m20, worldToRoot.m21, worldToRoot.m22).magnitude) * reach;
        bounds.Expand(padding * 2f);
        return bounds;
    }

    internal static bool PrepareBlocks(List<Block> blocks, bool force = false)
    {
        if (blocks == null || blocks.Count == 0)
        {
            return false;
        }

        bool rebuild = force;
        foreach (Block block in blocks)
        {
            rebuild |= block != null && block.isActiveAndEnabled && !block.HasValidRuntimeConnections;
        }

        if (rebuild)
        {
            // 同一轮先探测全部几何再维护双侧引用；后续遍历仅读取连接，不再次查询物理。
            new BatchConnectionBuilder(blocks).RebuildBatchImmediate();
        }

        return rebuild;
    }

    internal void CollectNeighbors(Block block, List<Block> result)
    {
        block.CollectRuntimeNeighbors(result);
        result.RemoveAll(neighbor => !members.ContainsKey(neighbor));
    }
}

// 职责：缓存单栋建筑的实际几何邻接；模块删除只更新受影响邻居，姿态/连接面/碰撞配置改变则重建。
// 入口：Prepare() / CollectNeighbors()；不创建连接视觉或跨建筑连边，不替代虚拟地面支撑规则。
using System.Collections.Generic;
using UnityEngine;

internal sealed class BuildingConnectionGraph
{
    private readonly Dictionary<Block, Node> nodes = new Dictionary<Block, Node>();
    private readonly HashSet<Block> current = new HashSet<Block>();
    private readonly List<Block> removed = new List<Block>();
    private readonly List<Collider> colliderScratch = new List<Collider>();
    private Vector3 rootScale;
    private bool queryTriggers;
    private bool complexLayout;
    internal void Prepare(List<Block> blocks, Transform root)
    {
        using SimulationWorkMetrics.Scope timing = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.BuildingGraph, blocks.Count);
        current.Clear();
        bool rebuild = complexLayout || !root.lossyScale.Equals(rootScale) || queryTriggers != Physics.queriesHitTriggers;
        bool nextComplexLayout = false;
        foreach (Block block in blocks)
        {
            if (block == null || !block.isActiveAndEnabled)
            {
                continue;
            }

            current.Add(block);
            // 只有规范单碰撞体布局具备完整快照；附加碰撞体的形状与子层级可能动态变化。
            // 每轮检查布局，复杂模块继续使用实时几何，不能悄悄复用不完整的缓存。
            block.GetComponentsInChildren(true, colliderScratch);
            if (colliderScratch.Count != 1 || colliderScratch[0] != block.raycastCollider || (block.connectorParent != null && block.connectorParent != block.transform && block.connectorParent.parent != block.transform))
            {
                rebuild = true;
                nextComplexLayout = true;
            }

            if (!nodes.TryGetValue(block, out Node node) || !node.IsValid(block) || node.Participates != block.CanParticipateInConnections || node.Trigger != (block.raycastCollider != null && block.raycastCollider.isTrigger))
            {
                rebuild = true;
            }
        }

        if (rebuild)
        {
            nodes.Clear();
            Physics.SyncTransforms();
            foreach (Block block in current)
            {
                var node = new Node(block, root.lossyScale);
                block.CollectNeighbors(node.Neighbors);
                node.Snapshot.Capture();
                nodes.Add(block, node);
            }

            rootScale = root.lossyScale;
            queryTriggers = Physics.queriesHitTriggers;
            complexLayout = nextComplexLayout;
            return;
        }

        removed.Clear();
        foreach (KeyValuePair<Block, Node> pair in nodes)
        {
            if (!current.Contains(pair.Key))
            {
                removed.Add(pair.Key);
            }
        }

        if (removed.Count == 0)
        {
            return;
        }

        Physics.SyncTransforms();
        // 删除的碰撞体可能曾遮住有效候选，即使没有形成连接边也需刷新附近探测。
        foreach (KeyValuePair<Block, Node> pair in nodes)
        {
            if (!current.Contains(pair.Key))
            {
                continue;
            }

            bool affected = false;
            foreach (Block deleted in removed)
            {
                Node old = nodes[deleted];
                float radius = pair.Value.Radius + old.Radius;
                if ((pair.Value.Position - old.Position).sqrMagnitude <= radius * radius)
                {
                    affected = true;
                    break;
                }
            }

            if (affected)
            {
                pair.Key.CollectNeighbors(pair.Value.Neighbors);
            }
        }

        foreach (Block block in removed)
        {
            nodes.Remove(block);
        }
    }

    internal void CollectNeighbors(Block block, List<Block> result)
    {
        result.Clear();
        if (!nodes.TryGetValue(block, out Node node))
        {
            return;
        }

        foreach (Block neighbor in node.Neighbors)
        {
            if (neighbor != null && current.Contains(neighbor))
            {
                result.Add(neighbor);
            }
        }
    }

    private sealed class Node
    {
        internal readonly BlockConnectionSnapshot Snapshot;
        internal readonly List<Block> Neighbors = new List<Block>();
        internal readonly bool Participates;
        internal readonly bool Trigger;
        internal readonly Vector3 Position;
        internal readonly float Radius;
        private readonly Transform connectorRoot;
        private readonly int connectorRootId;
        private readonly Vector3 connectorPosition;
        private readonly Quaternion connectorRotation;
        private readonly Vector3 connectorScale;
        internal bool IsValid(Block block)
        {
            Transform currentRoot = block.connectorParent;
            int currentRootId = currentRoot != null ? currentRoot.GetInstanceID() : 0;
            return Snapshot.IsValid && currentRootId == connectorRootId && (currentRoot == null || currentRoot == block.transform || (currentRoot.parent == block.transform && currentRoot.localPosition.Equals(connectorPosition) && currentRoot.localRotation.Equals(connectorRotation) && currentRoot.localScale.Equals(connectorScale)));
        }

        internal Node(Block block, Vector3 rootScale)
        {
            Snapshot = new BlockConnectionSnapshot(block);
            Participates = block.CanParticipateInConnections;
            Trigger = block.raycastCollider != null && block.raycastCollider.isTrigger;
            Position = block.transform.localPosition;
            connectorRoot = block.connectorParent;
            connectorRootId = connectorRoot != null ? connectorRoot.GetInstanceID() : 0;
            if (connectorRoot != null)
            {
                connectorPosition = connectorRoot.localPosition;
                connectorRotation = connectorRoot.localRotation;
                connectorScale = connectorRoot.localScale;
            }

            float radius = 0f;
            foreach (Connector connector in block.connectors)
            {
                if (connector != null)
                {
                    Vector3 position = connector.localPos;
                    if (connectorRoot != null && connectorRoot != block.transform)
                    {
                        position = connectorRoot.localPosition + connectorRoot.localRotation * Vector3.Scale(position, connectorRoot.localScale);
                    }

                    radius = Mathf.Max(radius, Vector3.Scale(position, block.transform.localScale).magnitude);
                }
            }

            if (block.raycastCollider != null)
            {
                BoxCollider box = block.raycastCollider;
                radius = Mathf.Max(
                    radius,
                    Vector3.Scale(box.center, block.transform.localScale).magnitude + Vector3.Scale(box.size, block.transform.localScale).magnitude * 0.5f);
            }

            float minimumScale = Mathf.Max(0.00001f, Mathf.Min(Mathf.Abs(rootScale.x), Mathf.Abs(rootScale.y), Mathf.Abs(rootScale.z)));
            Radius = radius + 0.13f / minimumScale;
        }
    }
}

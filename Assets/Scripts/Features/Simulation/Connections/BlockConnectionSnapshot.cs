// 职责：精确校验运行连接缓存的模块局部姿态、连接面身份/几何、规范碰撞体与查询层配置。
// 入口：IsValid / Capture() / Invalidate()；刚体整体运动不失效，复杂层级回退物理探测。
using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class BlockConnectionSnapshot
{
    private readonly Block block;
    private bool ready;
    private Transform parent;
    private Vector3 position;
    private Quaternion rotation;
    private Vector3 scale;
    private Face[] faces = Array.Empty<Face>();
    private BoxCollider collider;
    private Vector3 colliderCenter;
    private Vector3 colliderSize;
    private int layer;
    private int mask;
    private bool colliderEnabled;
    private bool trigger;
    private bool queryTriggers;
    private Vector3 rootScale;
    private Transform connectorRoot;
    private int connectorRootId;
    private Vector3 connectorPosition;
    private Quaternion connectorRotation;
    private Vector3 connectorScale;
    private readonly List<Collider> colliders = new List<Collider>();
    internal BlockConnectionSnapshot(Block block)
    {
        this.block = block;
    }

    internal bool IsValid
    {
        get
        {
            if (!ready || block == null || block.transform.parent != parent || !block.transform.localPosition.Equals(position) || !block.transform.localRotation.Equals(rotation) || !block.transform.localScale.Equals(scale))
            {
                return false;
            }

            if (block.raycastCollider != collider || collider == null || !collider.center.Equals(colliderCenter) || !collider.size.Equals(colliderSize) || collider.enabled != colliderEnabled || collider.gameObject.layer != layer || ConnectionMask() != mask || block.connectors.Count != faces.Length)
            {
                return false;
            }

            Transform currentRoot = block.connectorParent;
            int currentRootId = currentRoot != null ? currentRoot.GetInstanceID() : 0;
            if (!HasCanonicalColliderLayout() || collider.isTrigger != trigger || Physics.queriesHitTriggers != queryTriggers || !parent.lossyScale.Equals(rootScale) || currentRootId != connectorRootId || (currentRoot != null && currentRoot != block.transform && (currentRoot.parent != block.transform || !currentRoot.localPosition.Equals(connectorPosition) || !currentRoot.localRotation.Equals(connectorRotation) || !currentRoot.localScale.Equals(connectorScale))))
            {
                return false;
            }

            for (int index = 0; index < faces.Length; index++)
            {
                Connector connector = block.connectors[index];
                Face saved = faces[index];
                if (connector != saved.Connector || (connector != null && (connector.canConnect != saved.CanConnect || !connector.localPos.Equals(saved.Position) || !connector.normal.Equals(saved.Normal))))
                {
                    return false;
                }
            }

            return true;
        }
    }

    internal void Capture()
    {
        parent = block.transform.parent;
        position = block.transform.localPosition;
        rotation = block.transform.localRotation;
        scale = block.transform.localScale;
        if (faces.Length != block.connectors.Count)
        {
            faces = new Face[block.connectors.Count];
        }

        for (int index = 0; index < faces.Length; index++)
        {
            Connector connector = block.connectors[index];
            faces[index] = new Face
            {
                Connector = connector,
                CanConnect = connector != null && connector.canConnect,
                Position = connector?.localPos ?? Vector3.zero,
                Normal = connector?.normal ?? Vector3.zero
            };
        }

        collider = block.raycastCollider;
        if (collider != null)
        {
            colliderCenter = collider.center;
            colliderSize = collider.size;
            colliderEnabled = collider.enabled;
            layer = collider.gameObject.layer;
        }

        trigger = collider != null && collider.isTrigger;
        queryTriggers = Physics.queriesHitTriggers;
        rootScale = parent != null ? parent.lossyScale : Vector3.one;
        connectorRoot = block.connectorParent;
        connectorRootId = connectorRoot != null ? connectorRoot.GetInstanceID() : 0;
        if (connectorRoot != null)
        {
            connectorPosition = connectorRoot.localPosition;
            connectorRotation = connectorRoot.localRotation;
            connectorScale = connectorRoot.localScale;
        }

        mask = ConnectionMask();
        // 复杂布局不建立可复用快照，回到规范布局时也必须重新探测一次。
        ready = parent != null && parent.GetComponent<ControlUnit>() != null && collider != null && collider.transform == block.transform && HasCanonicalColliderLayout() && (connectorRoot == null || connectorRoot == block.transform || connectorRoot.parent == block.transform);
    }

    private bool HasCanonicalColliderLayout()
    {
        block.GetComponentsInChildren(true, colliders);
        Rigidbody ownerBody = collider.attachedRigidbody;
        foreach (Collider candidate in colliders)
        {
            if (candidate == collider)
            {
                continue;
            }

            // 停靠机器人仍在仓位层级中，但具有独立刚体，不是该模块的连接几何。
            // 同一刚体内的额外形状继续回退实时探测，不能把真正复杂的模块误当规范布局。
            if (candidate.attachedRigidbody == ownerBody && candidate.GetComponentInParent<Block>() == block)
            {
                return false;
            }
        }

        return true;
    }

    internal void RefreshIfReady()
    {
        if (ready)
        {
            Capture();
        }
    }

    internal void Invalidate()
    {
        ready = false;
    }

    private static int ConnectionMask()
    {
        return BuildManager.instance != null ? BuildManager.instance.blockLayer.value : LayerMask.GetMask("Block");
    }

    private struct Face
    {
        internal Connector Connector;
        internal bool CanConnect;
        internal Vector3 Position;
        internal Vector3 Normal;
    }
}

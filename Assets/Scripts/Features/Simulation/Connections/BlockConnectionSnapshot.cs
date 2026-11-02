// 职责：精确校验运行连接缓存的模块局部姿态、连接面身份/几何、规范碰撞体与查询层配置。
// 入口：IsValid / Capture() / Invalidate()；刚体整体运动不失效，复杂层级回退物理探测。
using System;
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

        mask = ConnectionMask();
        ready = parent != null && parent.GetComponent<ControlUnit>() != null && collider != null && collider.transform == block.transform;
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

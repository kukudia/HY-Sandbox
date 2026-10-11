// 职责：探测匹配连接面、维护双侧连接引用和邻接查询；与方块序列化配置分离。
// 入口：CheckConnection() / CollectNeighbors() / DisConnectAllConnectors()。
// 约束：批量图遍历自行统一同步物理；探测缓冲满时扩容，连接视觉不决定拓扑。
using System.Collections.Generic;
using UnityEngine;

internal sealed class BlockConnections
{
    private const float connectionProbeOffset = 0.05f;
    private const float connectionProbeRadius = 0.08f;
    private const float connectorMatchDistance = 0.25f;
    private const float oppositeNormalDotThreshold = 0.75f;
    private readonly Block _block;
    private Collider[] _connectionHits = new Collider[8];
    internal BlockConnections(Block block)
    {
        _block = block;
    }

    internal bool IsConnectorAvailableForPlacement(Connector connector)
    {
        if (connector == null || !connector.canConnect || connector.isConnected)
        {
            return false;
        }

        // An occupied face is unavailable even when the opposite Block exists but its
        // matching Connector is disabled. Placement and connection probing must share
        // the same two-sided validation rules.
        Block otherBlock = FindBlockAcrossConnector(connector);
        return otherBlock == null || FindMatchingConnector(otherBlock, connector) == null;
    }

    internal void CheckConnection()
    {
        // Rebuild connector links from physics probes so edits, loading, and explosions converge on one state.
        if (_block == null)
        {
            return;
        }

        if (BuildManager.instance == null)
        {
            return;
        }

        // Transform changes made by the build UI are not guaranteed to be visible to physics immediately.
        // Sync before probing so a move and its connection refresh observe the same frame.
        Physics.SyncTransforms();
        ReleaseExistingConnections();
        foreach (Connector c in _block.connectors)
        {
            if (c.isConnected && c.connector == null)
            {
                ReleaseConnectionPairWithoutVisual(c);
            }

            c.isConnected = false;
            if (!c.canConnect)
            {
                ClearConnector(c);
                continue;
            }

            Block otherBlock = FindBlockAcrossConnector(c);
            Connector otherConnector = otherBlock != null ? FindMatchingConnector(otherBlock, c) : null;
            // A ray hit alone is never enough to connect. Both sides must explicitly
            // allow connections, be free, and match by position and opposite normal.
            if (c.canConnect && otherBlock != null && otherConnector != null && otherConnector.canConnect && !c.isConnected && !otherConnector.isConnected)
            {
                ConnectTo(c, otherBlock, otherConnector);
            }

            if (c.connector != null && !c.isConnected)
            {
                ClearConnector(c);
            }
        }

        _block.RefreshConnectionStatus();
    }

    private void ReleaseExistingConnections()
    {
        HashSet<GameObject> releasedObjects = new HashSet<GameObject>();
        for (int i = 0; i < _block.connectors.Count; i++)
        {
            GameObject connectorObject = _block.connectors[i].connector;
            if (connectorObject == null || !releasedObjects.Add(connectorObject))
            {
                continue;
            }

            ReleaseConnectionReferences(connectorObject);
            Object.Destroy(connectorObject);
        }
    }

    private void ReleaseConnectionPairWithoutVisual(Connector connector)
    {
        Block otherBlock = FindBlockAcrossConnector(connector);
        Connector otherConnector = otherBlock != null ? FindMatchingConnector(otherBlock, connector, false) : null;
        if (otherConnector == null)
        {
            return;
        }

        otherConnector.connector = null;
        otherConnector.isConnected = false;
        otherBlock.RefreshConnectionStatus();
    }

    private void ReleaseConnectionReferences(GameObject connectorObject)
    {
        Block[] blocks = Object.FindObjectsByType<Block>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Block block in blocks)
        {
            if (block == null || block.connectors == null)
            {
                continue;
            }

            bool statusChanged = false;
            foreach (Connector connector in block.connectors)
            {
                if (connector == null || connector.connector != connectorObject)
                {
                    continue;
                }

                connector.connector = null;
                connector.isConnected = false;
                statusChanged = true;
            }

            if (statusChanged)
            {
                block.RefreshConnectionStatus();
            }
        }
    }

    private Block FindBlockAcrossConnector(Connector connector)
    {
        Vector3 worldPos = _block.GetConnectorWorldPosition(connector);
        Vector3 worldNormal = _block.GetConnectorWorldNormal(connector);
        Vector3 probeCenter = worldPos + worldNormal * connectionProbeOffset;
        int hitCount;
        // Retry full buffers so dense collider arrangements cannot truncate the query.
        while ((hitCount = Physics.OverlapSphereNonAlloc(probeCenter, connectionProbeRadius, _connectionHits, BuildManager.instance.blockLayer)) == _connectionHits.Length)
        {
            System.Array.Resize(ref _connectionHits, _connectionHits.Length * 2);
        }

        Block result = null;
        for (int i = 0; i < hitCount; i++)
        {
            Block otherBlock = _connectionHits[i].GetComponentInParent<Block>();
            if (otherBlock != null && otherBlock != _block)
            {
                result = otherBlock;
                break;
            }
        }

        System.Array.Clear(_connectionHits, 0, hitCount);
        return result;
    }

    private Connector FindMatchingConnector(Block otherBlock, Connector connector, bool requireAvailable = true)
    {
        Vector3 worldPos = _block.GetConnectorWorldPosition(connector);
        Vector3 worldNormal = _block.GetConnectorWorldNormal(connector);
        foreach (Connector otherConnector in otherBlock.connectors)
        {
            if (otherConnector == null || !otherConnector.canConnect || (requireAvailable && otherConnector.isConnected))
            {
                continue;
            }

            Vector3 otherWorldPos = otherBlock.GetConnectorWorldPosition(otherConnector);
            Vector3 otherWorldNormal = otherBlock.GetConnectorWorldNormal(otherConnector);
            if ((otherWorldPos - worldPos).sqrMagnitude < connectorMatchDistance * connectorMatchDistance && Vector3.Dot(otherWorldNormal, -worldNormal) > oppositeNormalDotThreshold)
            {
                return otherConnector;
            }
        }

        return null;
    }

    private void ConnectTo(Connector connector, Block otherBlock, Connector otherConnector)
    {
        connector.isConnected = true;
        otherConnector.isConnected = true;
        GameObject connectorObject = connector.connector != null ? connector.connector : otherConnector.connector;
        if (connectorObject == null && _block.connectorPrefab != null)
        {
            connectorObject = Object.Instantiate(_block.connectorPrefab, _block.connectorParent != null ? _block.connectorParent : _block.transform);
            connectorObject.transform.localPosition = connector.localPos;
            connectorObject.transform.localRotation = Quaternion.identity;
        }

        connector.connector = connectorObject;
        otherConnector.connector = connectorObject;
        _block.RefreshConnectionStatus();
        if (otherBlock != null)
        {
            otherBlock.RefreshConnectionStatus();
        }
    }

    private void ClearConnector(Connector connector)
    {
        if (connector == null)
        {
            return;
        }

        GameObject connectorObject = connector.connector;
        connector.connector = null;
        connector.isConnected = false;
        if (connectorObject != null)
        {
            ReleaseConnectionReferences(connectorObject);
            Object.Destroy(connectorObject);
        }
    }

    // Batch callers synchronize once and reuse their result buffer.
    // Topology depends on enabled matching faces, not optional visuals or stale flags.
    internal void CollectNeighbors(List<Block> result)
    {
        result.Clear();
        if (_block.connectors == null || BuildManager.instance == null)
        {
            return;
        }

        foreach (Connector connector in _block.connectors)
        {
            if (connector == null || !connector.canConnect)
            {
                continue;
            }

            Block otherBlock = FindBlockAcrossConnector(connector);
            if (otherBlock == null || !otherBlock.isActiveAndEnabled || result.Contains(otherBlock))
            {
                continue;
            }

            if (FindMatchingConnector(otherBlock, connector, false) != null)
            {
                result.Add(otherBlock);
            }
        }
    }

    internal void DisConnectAllConnectors(bool refreshNeighbors = true)
    {
        if (_block == null)
        {
            return;
        }

        LayerMask rackLayer = 0;
        Collider[] colliders = _block.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            collider.gameObject.layer = rackLayer;
        }

        ReleaseExistingConnections();
        foreach (Connector connector in _block.connectors)
        {
            connector.isConnected = false;
            connector.connector = null;
        }

        if (!refreshNeighbors)
        {
            _block.neighbors.Clear();
            return;
        }

        foreach (Block neighbor in _block.neighbors)
        {
            if (neighbor != null)
            {
                neighbor.CheckConnection();
            }
        }

        _block.neighbors.Clear();
    }
}

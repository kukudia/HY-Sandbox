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
    private Dictionary<GameObject, int> disconnectedLayers;
    private readonly BlockConnectionSnapshot runtimeSnapshot;
    internal bool RuntimeLinksValid => runtimeSnapshot.IsValid;

    internal bool CanParticipate
    {
        get
        {
            if (_block == null || !_block.isActiveAndEnabled || _block.raycastCollider == null || !_block.raycastCollider.enabled || !_block.raycastCollider.gameObject.activeInHierarchy)
            {
                return false;
            }

            int mask = ConnectionMask();
            return (mask & (1 << _block.raycastCollider.gameObject.layer)) != 0;
        }
    }

    internal BlockConnections(Block block)
    {
        _block = block;
        runtimeSnapshot = new BlockConnectionSnapshot(block);
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

    internal void CheckConnection(bool synchronizePhysics = true)
    {
        // Rebuild connector links from physics probes so edits, loading, and explosions converge on one state.
        if (_block == null)
        {
            return;
        }

        // Transform changes made by the build UI are not guaranteed to be visible to physics immediately.
        // Batch callers synchronize once before probing all blocks.
        if (synchronizePhysics)
        {
            Physics.SyncTransforms();
        }

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
        CaptureRuntimePose();
    }

    private void ReleaseExistingConnections()
    {
        HashSet<GameObject> releasedObjects = new HashSet<GameObject>();
        for (int i = 0; i < _block.connectors.Count; i++)
        {
            Connector connector = _block.connectors[i];
            GameObject connectorObject = connector.connector;
            if (connector.runtimePeer != null)
            {
                ReleaseRuntimePair(connector);
                if (connectorObject != null && releasedObjects.Add(connectorObject))
                {
                    Object.Destroy(connectorObject);
                }

                continue;
            }

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
        if (connector.runtimePeer != null)
        {
            ReleaseRuntimePair(connector);
            return;
        }

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
        // 只兼容域重载后尚无运行时双侧引用的旧视觉；正常连接使用ReleaseRuntimePair。
        SimulationWorkMetrics.RecordLegacyReferenceScan();
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
                if (connector.runtimePeer != null)
                {
                    ReleaseRuntimePair(connector);
                }

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
        // 断开的方块既不能被查询到，也不能向外连接；否则BFS输入顺序会改变分组。
        if (!CanParticipate)
        {
            return null;
        }

        Vector3 worldPos = _block.GetConnectorWorldPosition(connector);
        Vector3 worldNormal = _block.GetConnectorWorldNormal(connector);
        Vector3 probeCenter = worldPos + worldNormal * connectionProbeOffset;
        int hitCount;
        // Retry full buffers so dense collider arrangements cannot truncate the query.
        int blockLayer = BuildManager.instance != null ? BuildManager.instance.blockLayer.value : LayerMask.GetMask("Block");
        while (true)
        {
            SimulationWorkMetrics.RecordPhysicsNeighborQuery();
            hitCount = Physics.OverlapSphereNonAlloc(probeCenter, connectionProbeRadius, _connectionHits, blockLayer);
            if (hitCount < _connectionHits.Length)
            {
                break;
            }

            System.Array.Resize(ref _connectionHits, _connectionHits.Length * 2);
        }

        Block result = null;
        for (int i = 0; i < hitCount; i++)
        {
            Block otherBlock = _connectionHits[i].GetComponentInParent<Block>();
            if (otherBlock != null && otherBlock != _block && otherBlock.CanParticipateInConnections && SharesBuildingScope(otherBlock))
            {
                result = otherBlock;
                break;
            }
        }

        System.Array.Clear(_connectionHits, 0, hitCount);
        return result;
    }

    private bool SharesBuildingScope(Block other)
    {
        BuildingRuntime building = _block.GetComponentInParent<BuildingRuntime>();
        BuildingRuntime otherBuilding = other.GetComponentInParent<BuildingRuntime>();
        return building == otherBuilding && _block.GetComponentInParent<ControlUnit>() == other.GetComponentInParent<ControlUnit>();
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
        connector.runtimePeer = otherConnector;
        connector.runtimePeerBlock = otherBlock;
        otherConnector.runtimePeer = connector;
        otherConnector.runtimePeerBlock = _block;
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
        bool knownPair = connector.runtimePeer != null;
        if (knownPair)
        {
            ReleaseRuntimePair(connector);
        }

        connector.connector = null;
        connector.isConnected = false;
        if (connectorObject != null)
        {
            if (!knownPair)
            {
                ReleaseConnectionReferences(connectorObject);
            }

            Object.Destroy(connectorObject);
        }
    }

    // Batch callers synchronize once and reuse their result buffer.
    // Topology depends on enabled matching faces, not optional visuals or stale flags.
    internal void CollectNeighbors(List<Block> result)
    {
        result.Clear();
        if (_block.connectors == null)
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

        disconnectedLayers ??= new Dictionary<GameObject, int>();
        Collider[] colliders = _block.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            if (!disconnectedLayers.ContainsKey(collider.gameObject))
            {
                disconnectedLayers.Add(collider.gameObject, collider.gameObject.layer);
            }

            collider.gameObject.layer = 0;
        }

        ReleaseExistingConnections();
        foreach (Connector connector in _block.connectors)
        {
            connector.isConnected = false;
            connector.connector = null;
            connector.runtimePeer = null;
            connector.runtimePeerBlock = null;
        }

        CaptureRuntimePose();
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

    internal void RestoreParticipation()
    {
        runtimeSnapshot.Invalidate();
        if (disconnectedLayers == null)
        {
            return;
        }

        foreach (KeyValuePair<GameObject, int> saved in disconnectedLayers)
        {
            if (saved.Key != null)
            {
                saved.Key.layer = saved.Value;
            }
        }

        disconnectedLayers.Clear();
    }

    private void ReleaseRuntimePair(Connector connector)
    {
        Connector peer = connector.runtimePeer;
        Block peerBlock = connector.runtimePeerBlock;
        connector.runtimePeer = null;
        connector.runtimePeerBlock = null;
        connector.connector = null;
        connector.isConnected = false;
        if (peer != null && peer.runtimePeer == connector)
        {
            peer.runtimePeer = null;
            peer.runtimePeerBlock = null;
            peer.connector = null;
            peer.isConnected = false;
            if (peerBlock != null)
            {
                if (!peerBlock.CanParticipateInConnections)
                {
                    peerBlock.InvalidateRuntimeConnections();
                }

                peerBlock.RefreshConnectionStatus();
            }
        }
    }

    internal void CollectRuntimeNeighbors(List<Block> result)
    {
        result.Clear();
        if (!CanParticipate)
        {
            return;
        }

        ControlUnit unit = _block.GetComponentInParent<ControlUnit>();
        foreach (Connector connector in _block.connectors)
        {
            Connector peer = connector.runtimePeer;
            Block other = connector.runtimePeerBlock;
            if (!connector.canConnect || peer == null || peer.runtimePeer != connector || !peer.canConnect || other == null || !other.CanParticipateInConnections || other.GetComponentInParent<ControlUnit>() != unit || result.Contains(other))
            {
                continue;
            }

            result.Add(other);
        }
    }

    internal void RefreshRuntimePoseIfReady()
    {
        runtimeSnapshot.RefreshIfReady();
    }

    internal void InvalidateRuntimeLinks()
    {
        runtimeSnapshot.Invalidate();
    }

    private void CaptureRuntimePose()
    {
        runtimeSnapshot.Capture();
    }

    private static int ConnectionMask()
    {
        return BuildManager.instance != null ? BuildManager.instance.blockLayer.value : LayerMask.GetMask("Block");
    }
}

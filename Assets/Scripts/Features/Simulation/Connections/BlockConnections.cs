// 职责：探测匹配连接面、维护双侧连接引用和邻接查询；与方块序列化配置分离。
// 入口：CheckConnection() / CollectNeighbors() / DisConnectAllConnectors()。
// 约束：批量图遍历自行统一同步物理；探测缓冲满时扩容，连接视觉不决定拓扑。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            if (_block == null || !_block.isActiveAndEnabled || _block.raycastCollider == null || !_block.raycastCollider.gameObject.activeInHierarchy)
            {
                return false;
            }

            // 战斗机库关闭用于编辑选取的整盒，实体底板仍可承重；否则垫高机库永远被判为孤立组。
            if (!_block.raycastCollider.enabled && !(_block.TryGetComponent(out HangarModule hangar) && hangar.IsCombat))
            {
                return false;
            }

            int mask = ConnectionMask();
            return (mask & (1 << _block.ConnectionLayer(_block.raycastCollider.gameObject))) != 0;
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
        Vector3 worldPos = _block.GetConnectorWorldPosition(connector);
        Vector3 worldNormal = _block.GetConnectorWorldNormal(connector);
        Block otherBlock = FindBlockAcrossConnector(connector, worldPos, worldNormal, out _);
        return otherBlock == null || FindMatchingConnector(otherBlock, worldPos, worldNormal, true) == null;
    }

    internal void CheckConnection(bool synchronizePhysics = true, List<Block> neighborsFound = null)
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

        neighborsFound?.Clear();
        foreach (Connector connector in _block.connectors)
        {
            if (connector == null || !connector.canConnect)
            {
                ClearConnector(connector);
                continue;
            }

            Block otherBlock = FindBlockAcrossConnector(
                connector,
                _block.GetConnectorWorldPosition(connector),
                _block.GetConnectorWorldNormal(connector),
                out Connector other);
            // Geometry determines adjacency even when another face already owns the reciprocal link.
            if (neighborsFound != null && otherBlock != null && !neighborsFound.Contains(otherBlock))
            {
                neighborsFound.Add(otherBlock);
            }

            // 重新探测几何，但未改变的双侧连接保留原视觉，避免伤害刷新整舰时成批销毁/创建。
            bool retained = connector.isConnected && other != null && other.isConnected && connector.runtimePeer == other && other.runtimePeer == connector && connector.runtimePeerBlock == otherBlock && other.runtimePeerBlock == _block;
            if (retained)
            {
                ConnectTo(connector, otherBlock, other);
                continue;
            }

            ClearConnector(connector);
            if (other != null && !other.isConnected)
            {
                ConnectTo(connector, otherBlock, other);
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
                    DestroyConnectorVisual(connectorObject);
                }

                continue;
            }

            if (connectorObject == null || !releasedObjects.Add(connectorObject))
            {
                continue;
            }

            ReleaseConnectionReferences(connectorObject);
            DestroyConnectorVisual(connectorObject);
        }
    }

    private void ReleaseConnectionReferences(GameObject connectorObject)
    {
        // 只兼容域重载后尚无运行时双侧引用的旧视觉；正常连接使用ReleaseRuntimePair。
        SimulationWorkMetrics.RecordLegacyReferenceScan();
        Block[] blocks = Object.FindObjectsByType<Block>(FindObjectsInactive.Include);
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

    private Block FindBlockAcrossConnector(Connector connector, Vector3 worldPos, Vector3 worldNormal, out Connector matchedFace)
    {
        matchedFace = null;
        // 断开的方块既不能被查询到，也不能向外连接；否则BFS输入顺序会改变分组。
        if (!CanParticipate)
        {
            return null;
        }

        Vector3 probeCenter = worldPos + worldNormal * connectionProbeOffset;
        int hitCount;
        // Retry full buffers so dense collider arrangements cannot truncate the query.
        int blockLayer = ConnectionMask() | ControlUnit.DebrisMask;
        while (true)
        {
            SimulationWorkMetrics.RecordPhysicsNeighborQuery();
            hitCount = _block.gameObject.scene.GetPhysicsScene().OverlapSphere(probeCenter, connectionProbeRadius, _connectionHits, blockLayer, QueryTriggerInteraction.UseGlobal);
            if (hitCount < _connectionHits.Length)
            {
                break;
            }

            System.Array.Resize(ref _connectionHits, _connectionHits.Length * 2);
        }

        Block result = null;
        float closest = float.PositiveInfinity;
        // Expanded bays disable their editing box so the reserved interior stays empty.
        // Their own downward probe establishes a real floor pair first; the floor's
        // reciprocal refresh cannot rediscover that invisible controller via physics.
        Block existing = connector.runtimePeerBlock;
        if (existing != null && existing != _block && existing.TryGetComponent(out HangarModule bay) && bay.IsCombat && existing.raycastCollider != null && !existing.raycastCollider.enabled && existing.CanParticipateInConnections && SharesUnit(existing))
        {
            Connector matching = FindMatchingConnector(existing, worldPos, worldNormal, false);
            if (matching != null && connector.runtimePeer == matching && matching.runtimePeer == connector && matching.runtimePeerBlock == _block)
            {
                result = existing;
                matchedFace = matching;
                closest = (existing.GetConnectorWorldPosition(matching) - worldPos).sqrMagnitude;
            }
        }

        for (int i = 0; i < hitCount; i++)
        {
            Block otherBlock = _connectionHits[i].GetComponentInParent<Block>();
            if (otherBlock != null && otherBlock != _block && otherBlock.CanParticipateInConnections && SharesUnit(otherBlock))
            {
                // Dense shell corners can overlap several colliders. A collider without
                // a matching face must not hide a valid connector later in the buffer.
                Connector matching = FindMatchingConnector(otherBlock, worldPos, worldNormal, false);
                if (matching == null)
                {
                    continue;
                }

                float distance = (otherBlock.GetConnectorWorldPosition(matching) - worldPos).sqrMagnitude;
                if (distance < closest || (Mathf.Approximately(distance, closest) && string.CompareOrdinal(otherBlock.uniqueId, result != null ? result.uniqueId : null) < 0))
                {
                    result = otherBlock;
                    matchedFace = matching;
                    closest = distance;
                }
            }
        }

        System.Array.Clear(_connectionHits, 0, hitCount);
        return result;
    }

    private bool SharesUnit(Block other)
    {
        return _block.GetComponentInParent<ControlUnit>() == other.GetComponentInParent<ControlUnit>();
    }

    private static Connector FindMatchingConnector(Block otherBlock, Vector3 worldPos, Vector3 worldNormal, bool requireAvailable)
    {
        foreach (Connector otherConnector in otherBlock.connectors)
        {
            if (otherConnector == null || !otherConnector.canConnect || (requireAvailable && otherConnector.isConnected))
            {
                continue;
            }

            Vector3 otherWorldPos = otherBlock.GetConnectorWorldPosition(otherConnector);
            if ((otherWorldPos - worldPos).sqrMagnitude < connectorMatchDistance * connectorMatchDistance && Vector3.Dot(otherBlock.GetConnectorWorldNormal(otherConnector), -worldNormal) > oppositeNormalDotThreshold)
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

            DestroyConnectorVisual(connectorObject);
        }
    }

    private static void DestroyConnectorVisual(GameObject connectorObject)
    {
        // 可编辑舰体制作同样会重建连接；Edit Mode 必须立即释放被替代的视觉对象。
        if (Application.isPlaying)
        {
            Object.Destroy(connectorObject);
        }
        else
        {
            Object.DestroyImmediate(connectorObject);
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

            Block otherBlock = FindBlockAcrossConnector(
                connector,
                _block.GetConnectorWorldPosition(connector),
                _block.GetConnectorWorldNormal(connector),
                out _);
            if (otherBlock == null || !otherBlock.isActiveAndEnabled || result.Contains(otherBlock))
            {
                continue;
            }

            result.Add(otherBlock);
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

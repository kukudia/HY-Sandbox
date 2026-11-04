// 职责：对照实时几何验证建筑邻接缓存的热路径、根运动、局部修改、断链与删除失效。
// 入口：Run()；隔离真实方块夹具，不运行物理步，不修改场景资产。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeBuildingGraphChecks
{
    public static string Run()
    {
        if (!EditorApplication.isPlaying || SimulationWorkMetrics.Enabled)
        {
            throw new InvalidOperationException("Requires Play without metrics capture.");
        }

        var checks = new List<string>();
        Type graphType = typeof(BuildingRuntime).Assembly.GetType("BlockConnectionGraph");
        object graph = Activator.CreateInstance(graphType, true);
        MethodInfo prepare = graphType.GetMethod("Prepare", BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo collect = graphType.GetMethod("CollectNeighbors", BindingFlags.Instance | BindingFlags.NonPublic);
        var root = new GameObject("Owned building graph checks", typeof(ControlUnit), typeof(Rigidbody));
        root.transform.position = new Vector3(10000, 2000, 0);
        root.GetComponent<Rigidbody>().isKinematic = true;
        var blocks = new List<Block>();
        bool queriesHitTriggers = Physics.queriesHitTriggers;
        try
        {
            GameObject prefab = Resources.Load<GameObject>("Blocks/1x1x1");
            for (int index = 0; index < 4; index++)
            {
                Block block = Object.Instantiate(prefab, root.transform).GetComponent<Block>();
                block.transform.localPosition = Vector3.right * index;
                block.RefreshRuntimeData(true);
                blocks.Add(block);
            }

            SimulationWorkMetrics.Enabled = true;
            Action<string, bool> verify = (name, expectQueries) =>
            {
                SimulationWorkMetrics.Reset();
                prepare.Invoke(graph, new object[] { blocks, root.transform });
                long queries = SimulationWorkMetrics.PhysicsNeighborQueries;
                Physics.SyncTransforms();
                foreach (Block block in blocks.Where(block => block != null && block.isActiveAndEnabled))
                {
                    var actual = new List<Block>();
                    var expected = block.Neighbors();
                    collect.Invoke(graph, new object[] { block, actual });
                    expected.RemoveAll(neighbor => !blocks.Contains(neighbor));
                    if (!new HashSet<Block>(actual).SetEquals(expected))
                    {
                        throw new InvalidOperationException(name + " geometry differs for " + block.transform.localPosition);
                    }
                }

                if ((queries > 0) != expectQueries)
                {
                    throw new InvalidOperationException(name + " query contract violated: " + queries);
                }

                checks.Add("PASS " + name);
            };
            verify("cold graph matches real geometry", true);
            verify("unchanged graph performs zero physics queries", false);
            root.transform.SetPositionAndRotation(root.transform.position + Vector3.up * 7f, Quaternion.Euler(0, 37, 0));
            verify("rigid root motion retains graph", false);
            blocks[2].transform.localPosition += Vector3.up;
            verify("local position mutation rebuilds graph", true);
            blocks[2].transform.localPosition -= Vector3.up;
            verify("restoring local pose rebuilds graph", true);
            Connector face = blocks[1].connectors.First();
            face.canConnect = !face.canConnect;
            verify("connector enabled mutation rebuilds graph", true);
            face.canConnect = !face.canConnect;
            verify("connector restoration rebuilds graph", true);
            blocks[1].raycastCollider.size *= 0.9f;
            verify("canonical collider size mutation rebuilds graph", true);
            blocks[1].raycastCollider.enabled = false;
            verify("disabled canonical collider is removed symmetrically", true);
            blocks[1].raycastCollider.enabled = true;
            verify("collider reenable rebuilds graph", true);
            Physics.queriesHitTriggers = !queriesHitTriggers;
            verify("global query trigger policy invalidates graph", true);
            Physics.queriesHitTriggers = queriesHitTriggers;
            verify("query policy restoration rebuilds graph", true);
            blocks[1].DisConnectAllConnectors(false);
            verify("explicit disconnect updates shared reciprocal graph without queries", false);
            blocks[1].RestoreConnectionParticipation();
            verify("repair participation restoration rebuilds graph", true);
            Block removed = blocks[1];
            removed.gameObject.SetActive(false);
            blocks.Remove(removed);
            verify("node removal refreshes nearby surviving probes", true);
            verify("post-removal graph becomes query-free again", false);
            Block inactive = blocks[1];
            inactive.gameObject.SetActive(false);
            verify("inactive retained-list module leaves graph", true);
            inactive.gameObject.SetActive(true);
            verify("reactivated module rebuilds graph", true);
            root.transform.localScale = Vector3.one * 1.1f;
            verify("root scaling invalidates graph", true);
            root.transform.localScale = Vector3.one;
            verify("root scale restoration invalidates graph", true);
            blocks[0].connectors[0].localPos += Vector3.up * 0.1f;
            verify("connector geometry mutation invalidates graph", true);
            blocks[0].connectors[0].localPos -= Vector3.up * 0.1f;
            verify("connector geometry restoration invalidates graph", true);
            Block added = Object.Instantiate(prefab, root.transform).GetComponent<Block>();
            added.transform.localPosition = Vector3.right * 4f;
            added.RefreshRuntimeData(true);
            blocks.Add(added);
            verify("new module rebuilds graph", true);
            verify("new module graph becomes query-free", false);
            SphereCollider extra = added.gameObject.AddComponent<SphereCollider>();
            extra.radius = 0.2f;
            verify("additional collider enters geometry fallback", true);
            extra.center = Vector3.left * 0.5f;
            verify("complex collider mutation is probed live", true);
            Object.DestroyImmediate(extra);
            verify("canonical layout restoration rebuilds once", true);
            verify("canonical layout can resume caching", false);
            var connectorRoot = new GameObject("Owned movable connector root");
            connectorRoot.transform.SetParent(added.transform, false);
            Transform originalConnectorRoot = added.connectorParent;
            added.connectorParent = connectorRoot.transform;
            verify("separate connector transform invalidates graph", true);
            connectorRoot.transform.localPosition = Vector3.up * 0.2f;
            verify("connector transform motion invalidates graph", true);
            verify("unchanged direct connector transform is cached", false);
            var pivot = new GameObject("Owned connector ancestor");
            pivot.transform.SetParent(added.transform, false);
            connectorRoot.transform.SetParent(pivot.transform, false);
            verify("nested connector hierarchy uses live fallback", true);
            pivot.transform.localPosition = Vector3.left * 0.2f;
            verify("connector ancestor motion is probed live", true);
            added.connectorParent = originalConnectorRoot;
            Object.DestroyImmediate(pivot);
            verify("connector root restoration rebuilds once", true);
            verify("restored connector root resumes caching", false);
            int layer = added.gameObject.layer;
            added.gameObject.layer = 0;
            verify("canonical collider layer mutation invalidates graph", true);
            added.gameObject.layer = layer;
            verify("collider layer restoration invalidates graph", true);
            added.raycastCollider.isTrigger = true;
            verify("canonical trigger mutation invalidates graph", true);
            added.raycastCollider.isTrigger = false;
            verify("canonical trigger restoration invalidates graph", true);
            var deletedRoot = new GameObject("Owned deleted connector transform");
            deletedRoot.transform.SetParent(added.transform, false);
            deletedRoot.transform.localPosition = Vector3.up * 0.3f;
            added.connectorParent = deletedRoot.transform;
            verify("offset connector root rebuilds graph", true);
            Object.DestroyImmediate(deletedRoot);
            verify("destroyed connector reference restores block-space probes", true);
            verify("destroyed connector reference can be cached as null", false);
            added.connectorParent = originalConnectorRoot;
            verify("original connector reference restoration rebuilds graph", true);
            SparseRemovalChecks(checks, prefab, graphType, prepare, collect);
            ConnectionVisualPolicyChecks(checks, prefab, graphType, prepare, collect);
            string json = JsonUtility.ToJson(new Report { checks = checks }, true);
            Directory.CreateDirectory("Temp/Siege");
            File.WriteAllText("Temp/Siege/building-graph-checks.json", json);
            return json;
        }
        finally
        {
            SimulationWorkMetrics.Enabled = false;
            Physics.queriesHitTriggers = queriesHitTriggers;
            Object.DestroyImmediate(root);
        }
    }

    private static void ConnectionVisualPolicyChecks(List<string> checks, GameObject prefab, Type graphType, MethodInfo prepare, MethodInfo collect)
    {
        foreach (bool building in new[]
        {
            true,
            false
        }

        )
        {
            var root = new GameObject("Owned connection visual policy checks", typeof(ControlUnit), typeof(Rigidbody));
            root.transform.position = new Vector3(14000f, 2000f, 0f);
            root.GetComponent<Rigidbody>().isKinematic = true;
            if (building)
            {
                root.AddComponent<BuildingRuntime>();
            }

            try
            {
                var blocks = new List<Block>();
                for (int index = 0; index < 2; index++)
                {
                    Block block = Object.Instantiate(prefab, root.transform).GetComponent<Block>();
                    block.transform.SetLocalPositionAndRotation(Vector3.right * index, Quaternion.identity);
                    block.RefreshRuntimeData(true);
                    // Gizmo开关不应控制游戏内接缝装饰；两个领域故意采用相反值。
                    block.showConnectors = building;
                    blocks.Add(block);
                }

                object graph = Activator.CreateInstance(graphType, true);
                prepare.Invoke(graph, new object[] { blocks, root.transform });
                var neighbors = new List<Block>();
                collect.Invoke(graph, new object[] { blocks[0], neighbors });
                List<Connector> linked = blocks.SelectMany(block => block.connectors).Where(connector => connector.isConnected).ToList();
                if (neighbors.Count != 1 || neighbors[0] != blocks[1] || linked.Count != 2)
                {
                    throw new InvalidOperationException("Connection visual policy changed reciprocal topology.");
                }

                bool expectedVisuals = building ? linked.All(connector => connector.connector == null) : linked[0].connector != null && linked[0].connector == linked[1].connector;
                if (!expectedVisuals)
                {
                    throw new InvalidOperationException(building ? "Building connection unexpectedly created a visual decoration." : "Vehicle connection lost its shared visual decoration.");
                }

                SimulationWorkMetrics.Reset();
                prepare.Invoke(graph, new object[] { blocks, root.transform });
                if (SimulationWorkMetrics.PhysicsNeighborQueries != 0)
                {
                    throw new InvalidOperationException("Connection visual policy invalidated otherwise unchanged geometry.");
                }

                checks.Add(building ? "PASS real BuildingRuntime keeps reciprocal cached topology without connection decorations regardless of Gizmo flag" : "PASS vehicle keeps its shared connection decoration and cached topology regardless of Gizmo flag");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }

    private static void SparseRemovalChecks(List<string> checks, GameObject prefab, Type graphType, MethodInfo prepare, MethodInfo collect)
    {
        var root = new GameObject("Owned sparse graph removal checks", typeof(ControlUnit), typeof(Rigidbody));
        root.transform.SetPositionAndRotation(new Vector3(16000f, 2200f, 0f), Quaternion.Euler(0f, 37f, 0f));
        root.transform.localScale = new Vector3(0.75f, 1.25f, 0.9f);
        root.GetComponent<Rigidbody>().isKinematic = true;
        var blocks = new List<Block>();
        object graph = Activator.CreateInstance(graphType, true);
        FieldInfo peerField = typeof(Connector).GetField("runtimePeer", BindingFlags.Instance | BindingFlags.NonPublic);
        PropertyInfo revision = graphType.GetProperty("Revision", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            Func<float, Block> add = x =>
            {
                Block block = Object.Instantiate(prefab, root.transform).GetComponent<Block>();
                block.transform.SetLocalPositionAndRotation(Vector3.right * x, Quaternion.identity);
                block.RefreshRuntimeData(true);
                block.connectorPrefab = null;
                blocks.Add(block);
                return block;
            };
            for (int index = 0; index < 3; index++)
            {
                add(index);
                add(10f + index);
            }

            Block firstMiddle = blocks[2];
            Block secondMiddle = blocks[3];
            Block far = null;
            for (int pair = 0; pair < 8; pair++)
            {
                Block endpoint = add(100f + pair * 4f);
                far ??= endpoint;
                add(101f + pair * 4f);
            }

            prepare.Invoke(graph, new object[] { blocks, root.transform });
            Func<Block, long> removeAndMeasure = deleted =>
            {
                object[] peers = far.connectors.Select(connector => peerField.GetValue(connector)).ToArray();
                var before = new List<Block>();
                collect.Invoke(graph, new object[] { far, before });
                int oldRevision = (int)revision.GetValue(graph);
                deleted.gameObject.SetActive(false);
                blocks.Remove(deleted);
                SimulationWorkMetrics.Reset();
                prepare.Invoke(graph, new object[] { blocks, root.transform });
                long queries = SimulationWorkMetrics.PhysicsNeighborQueries;
                var after = new List<Block>();
                collect.Invoke(graph, new object[] { far, after });
                bool samePeers = far.connectors.Select(connector => peerField.GetValue(connector)).SequenceEqual(peers);
                bool reciprocal = far.connectors.All(connector => peerField.GetValue(connector) is not Connector peer || ReferenceEquals(peerField.GetValue(peer), connector));
                if (before.Count != 1 || !new HashSet<Block>(before).SetEquals(after) || !samePeers || !reciprocal || (int)revision.GetValue(graph) != oldRevision + 1)
                {
                    throw new InvalidOperationException("Local removal changed distant reciprocal links or did not advance graph revision.");
                }

                foreach (Block block in blocks)
                {
                    var actual = new List<Block>();
                    collect.Invoke(graph, new object[] { block, actual });
                    var expected = block.Neighbors();
                    expected.RemoveAll(neighbor => !blocks.Contains(neighbor));
                    if (!new HashSet<Block>(actual).SetEquals(expected))
                    {
                        throw new InvalidOperationException("Sparse incremental removal differs from live geometry at " + block.transform.localPosition);
                    }
                }

                SimulationWorkMetrics.Reset();
                prepare.Invoke(graph, new object[] { blocks, root.transform });
                if (SimulationWorkMetrics.PhysicsNeighborQueries != 0 || (int)revision.GetValue(graph) != oldRevision + 1)
                {
                    throw new InvalidOperationException("Sparse graph removal left an invalid cache or advanced the revision without changes.");
                }

                return queries;
            };
            long smallQueries = removeAndMeasure(firstMiddle);
            for (int pair = 8; pair < 128; pair++)
            {
                add(100f + pair * 4f);
                add(101f + pair * 4f);
            }

            prepare.Invoke(graph, new object[] { blocks, root.transform });
            long largeQueries = removeAndMeasure(secondMiddle);
            if (smallQueries <= 0 || largeQueries != smallQueries)
            {
                throw new InvalidOperationException("Local deletion queries grew with remote population: " + smallQueries + " -> " + largeQueries);
            }

            checks.Add("PASS sparse removal preserves distant reciprocal links and matches live geometry under scaled rotated root");
            checks.Add("PASS growing the same construct from 8 to 128 distant pairs keeps local deletion queries constant: " + largeQueries);
            checks.Add("PASS deletion advances graph revision once and the resulting graph resumes query-free caching");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

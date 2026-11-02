// 职责：验证缓存连接的无查询热路径、断链、制作变更失效和无视觉/双侧视觉生命周期。
// 入口：Run()；调用真实内部运行分组入口，物理探测/旧视觉扫描计数由生产代码按需记录。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeRuntimeConnectionChecks
{
    private static readonly MethodInfo RuntimeGrouping = typeof(BlockGroupManager).GetMethod("GroupRuntimeBlocks", BindingFlags.Static | BindingFlags.NonPublic);
    public static string Run()
    {
        if (!EditorApplication.isPlaying || SimulationWorkMetrics.Enabled)
        {
            throw new InvalidOperationException("Requires Play Mode without another metrics capture.");
        }

        var checks = new List<string>();
        GameObject root = Object.Instantiate(
            Resources.Load<GameObject>("BlocksParent/BlocksParent"),
            new Vector3(7000, 1500, 0),
            Quaternion.identity);
        var blocks = new List<Block>();
        GameObject otherRoot = null;
        try
        {
            GameObject prefab = Resources.Load<GameObject>("Blocks/1x1x1");
            GameObject visual = prefab.GetComponent<Block>().connectorPrefab;
            for (int index = 0; index < 3; index++)
            {
                Block block = Object.Instantiate(prefab, root.transform.position + Vector3.right * index, Quaternion.identity, root.transform).GetComponent<Block>();
                block.RefreshRuntimeData(true);
                block.connectorPrefab = null;
                blocks.Add(block);
            }

            foreach (Block block in blocks)
            {
                block.CheckConnection();
            }

            SimulationWorkMetrics.Enabled = true;
            SimulationWorkMetrics.Reset();
            Check(
                checks,
                "initialized invisible links group without physics probes",
                Groups(blocks).Count == 1 && SimulationWorkMetrics.PhysicsNeighborQueries == 0);
            root.transform.SetPositionAndRotation(root.transform.position + Vector3.up * 10f, Quaternion.Euler(0f, 37f, 0f));
            Check(
                checks,
                "rigid root motion preserves cache without physics probes",
                Groups(blocks).Count == 1 && SimulationWorkMetrics.PhysicsNeighborQueries == 0);
            Block middle = blocks[1];
            middle.DisConnectAllConnectors(false);
            Check(
                checks,
                "explicit detach cuts invisible reciprocal links without probing or scene scans",
                Groups(blocks).Count == 3 && SimulationWorkMetrics.PhysicsNeighborQueries == 0 && SimulationWorkMetrics.LegacyReferenceScans == 0);
            blocks.Reverse();
            Check(
                checks,
                "cached detached graph is independent of input order",
                Groups(blocks).TrueForAll(group => group.Count == 1));
            middle.RestoreConnectionParticipation();
            Check(
                checks,
                "restoring participation invalidates cache and reconnects",
                Groups(blocks).Count == 1 && SimulationWorkMetrics.PhysicsNeighborQueries > 0);
            middle.transform.localPosition += Vector3.up * 2f;
            SimulationWorkMetrics.Reset();
            Check(
                checks,
                "relative same-frame movement rebuilds physical links",
                Groups(blocks).Count == 3 && SimulationWorkMetrics.PhysicsNeighborQueries > 0);
            middle.transform.localPosition -= Vector3.up * 2f;
            Check(checks, "moving back restores links", Groups(blocks).Count == 1);
            foreach (Connector connector in middle.connectors)
            {
                connector.canConnect = false;
            }

            Check(checks, "face disabling invalidates cached adjacency", Groups(blocks).Count == 3);
            foreach (Connector connector in middle.connectors)
            {
                connector.canConnect = true;
            }

            Check(checks, "face enabling discovers newly valid adjacency", Groups(blocks).Count == 1);
            middle.raycastCollider.enabled = false;
            Check(checks, "canonical collider disable rebuilds to isolated nodes", Groups(blocks).Count == 3);
            middle.raycastCollider.enabled = true;
            Check(checks, "canonical collider reenable reconnects", Groups(blocks).Count == 1);
            middle.enabled = false;
            Check(checks, "disabled block is excluded and cannot bridge components", Groups(blocks).Count == 2);
            middle.enabled = true;
            Check(checks, "reenabling block invalidates and reconnects", Groups(blocks).Count == 1);
            var replacement = middle.connectors.Select(connector => new Connector { canConnect = connector.canConnect, localPos = connector.localPos, normal = connector.normal, connectType = connector.connectType }).ToList();
            middle.connectors = replacement;
            Check(
                checks,
                "replacing connector objects with identical geometry invalidates cached references",
                Groups(blocks).Count == 1);
            SimulationWorkMetrics.Reset();
            foreach (Block block in blocks)
            {
                block.connectorPrefab = visual;
                block.CheckConnection();
            }

            for (int repeat = 0; repeat < 3; repeat++)
            {
                middle.CheckConnection();
            }

            bool pairedVisuals = blocks.SelectMany(block => block.connectors).Where(connector => connector.isConnected).All(connector => connector.connector != null && blocks.Sum(block => block.connectors.Count(other => other.connector == connector.connector)) == 2);
            Check(checks, "visual refresh retains exactly two references per live connection", pairedVisuals);
            Check(
                checks,
                "known reciprocal connections never scan all scene blocks",
                SimulationWorkMetrics.LegacyReferenceScans == 0);
            SimulationWorkMetrics.Reset();
            Check(
                checks,
                "refreshed visual graph remains a no-query hot path",
                Groups(blocks).Count == 1 && SimulationWorkMetrics.PhysicsNeighborQueries == 0);
            Block endpoint = blocks[0];
            otherRoot = Object.Instantiate(
                Resources.Load<GameObject>("BlocksParent/BlocksParent"),
                endpoint.transform.position + root.transform.right,
                root.transform.rotation);
            Block other = Object.Instantiate(prefab, otherRoot.transform).GetComponent<Block>();
            other.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            other.RefreshRuntimeData(true);
            other.connectorPrefab = null;
            other.CheckConnection();
            endpoint.CheckConnection();
            var combined = new List<Block>(blocks)
            {
                other
            };
            Check(
                checks,
                "touching independent control units never weld into one component",
                BlockGroupManager.GroupBlocks(combined).Count == 2 && Groups(combined).Count == 2 && other.Neighbors().Count == 0);
        }
        finally
        {
            SimulationWorkMetrics.Enabled = false;
            SimulationWorkMetrics.Reset();
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(otherRoot);
        }

        Directory.CreateDirectory("Temp/Siege");
        string json = JsonUtility.ToJson(new Report { checks = checks }, true);
        File.WriteAllText("Temp/Siege/runtime-connections.json", json);
        return json;
    }

    private static List<List<Block>> Groups(List<Block> blocks)
    {
        return (List<List<Block>>)RuntimeGrouping.Invoke(null, new object[] { blocks });
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Runtime connection check failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

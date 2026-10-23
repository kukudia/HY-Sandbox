// 职责：用真实方块验证断链对称性、碰撞层恢复以及移动/禁用连接面等分组契约。
// 入口：Run()；临时夹具与现有单位隔离，失败抛出并在finally清理。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeTopologyChecks
{
    public static string Run()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Play Mode required.");
        }

        var checks = new List<string>();
        var root = new GameObject("Owned topology contract checks");
        try
        {
            GameObject prefab = Resources.Load<GameObject>("Blocks/1x1x1");
            var blocks = new List<Block>();
            for (int index = 0; index < 3; index++)
            {
                Block block = Object.Instantiate(prefab, new Vector3(5000 + index, 1500, 0), Quaternion.identity, root.transform).GetComponent<Block>();
                block.RefreshRuntimeData(true);
                blocks.Add(block);
            }

            Check(checks, "intact chain is one component", BlockGroupManager.GroupBlocks(blocks).Count == 1);
            Block middle = blocks[1];
            int originalLayer = middle.gameObject.layer;
            middle.gameObject.AddComponent<BoxCollider>();
            GameObject child = new GameObject("Original ignored-query child");
            child.transform.SetParent(middle.transform, false);
            child.layer = 2;
            child.AddComponent<BoxCollider>().isTrigger = true;
            middle.DisConnectAllConnectors(false);
            List<List<Block>> forward = BlockGroupManager.GroupBlocks(blocks);
            blocks.Reverse();
            List<List<Block>> reverse = BlockGroupManager.GroupBlocks(blocks);
            Check(
                checks,
                "detached middle yields three singleton components in either order",
                IsAllSingleton(forward) && IsAllSingleton(reverse));
            middle.CheckConnection();
            Check(
                checks,
                "refreshing visuals cannot reconnect a detached block",
                BlockGroupManager.GroupBlocks(blocks).Count == 3);
            middle.DisConnectAllConnectors(false);
            middle.RestoreConnectionParticipation();
            Check(
                checks,
                "repeated detach restores exact layers even with multiple colliders per node",
                middle.gameObject.layer == originalLayer && child.layer == 2);
            middle.CheckConnection();
            Check(
                checks,
                "explicit participation restore reconnects geometry",
                BlockGroupManager.GroupBlocks(blocks).Count == 1);
            middle.transform.position += Vector3.up * 4f;
            Check(
                checks,
                "same-frame transform movement invalidates geometric neighbors",
                BlockGroupManager.GroupBlocks(blocks).Count == 3);
            middle.transform.position -= Vector3.up * 4f;
            foreach (Connector connector in middle.connectors)
            {
                connector.canConnect = false;
            }

            Check(
                checks,
                "disabled faces reject both traversal directions with stale flags",
                BlockGroupManager.GroupBlocks(blocks).Count == 3);
            foreach (Connector connector in middle.connectors)
            {
                connector.canConnect = true;
            }

            middle.raycastCollider.enabled = false;
            Check(
                checks,
                "disabled canonical collider cannot emit one-sided links",
                BlockGroupManager.GroupBlocks(blocks).Count == 3);
            middle.raycastCollider.enabled = true;
            middle.connectorPrefab = null;
            middle.CheckConnection();
            Check(
                checks,
                "topology does not require connection visuals",
                BlockGroupManager.GroupBlocks(blocks).Count == 1);
            Check(
                checks,
                "null duplicate and external blocks do not leak into grouping",
                BlockGroupManager.GroupBlocks(new List<Block> { middle, null, middle })[0].Count == 1);
            BuildingFoundation foundation = Object.Instantiate(
                Resources.Load<GameObject>("BuildingEditor/Foundation"),
                new Vector3(8000, 1600, 0),
                Quaternion.identity,
                root.transform).GetComponent<BuildingFoundation>();
            foundation.Configure(new BuildingFoundationData { width = 4, length = 4 });
            Block foundationBlock = foundation.GetComponent<Block>();
            Vector3 supportedPosition = foundationBlock.GetConnectorWorldPosition(foundationBlock.connectors[0]) + Vector3.up * 0.5f;
            Block supported = Object.Instantiate(prefab, supportedPosition, Quaternion.identity, root.transform).GetComponent<Block>();
            supported.RefreshRuntimeData(true);
            var foundationPair = new List<Block>
            {
                foundationBlock,
                supported
            };
            Check(
                checks,
                "configured building foundation participates in two-sided graph",
                BlockGroupManager.GroupBlocks(foundationPair).Count == 1);
            foundation.Configure(new BuildingFoundationData { width = 5, length = 4 });
            Check(
                checks,
                "foundation resize restores participation without moving supported grid",
                BlockGroupManager.GroupBlocks(foundationPair).Count == 1);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }

        Directory.CreateDirectory("Temp/Siege");
        string json = JsonUtility.ToJson(new Report { checks = checks }, true);
        File.WriteAllText("Temp/Siege/topology-checks.json", json);
        return json;
    }

    private static bool IsAllSingleton(List<List<Block>> groups)
    {
        return groups.Count == 3 && groups.TrueForAll(group => group.Count == 1);
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Topology check failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

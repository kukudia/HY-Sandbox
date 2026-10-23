// 职责：在真实基地上对照缓存与实时底座判定，验证根运动和局部连接几何失效。
// 入口：Run()；同步隔离变化后恢复，不推进物理步或保存场景。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeFoundationCacheChecks
{
    public static string Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0)
        {
            throw new InvalidOperationException("Requires fresh Siege deployment Play.");
        }

        BuildingRuntime building = session.Fleet.Base.Building;
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        object graph = typeof(BuildingRuntime).GetField("connections", flags).GetValue(building);
        MethodInfo prepare = graph.GetType().GetMethod("Prepare", flags);
        MethodInfo cached = typeof(BuildingRuntime).GetMethod("HasPreparedGroundConnector", flags);
        MethodInfo direct = typeof(BuildingRuntime).GetMethod("HasGroundConnector", flags);
        BuildingFoundationData foundation = (BuildingFoundationData)typeof(BuildingRuntime).GetField("foundationData", flags).GetValue(building);
        int width = foundation.width;
        int length = foundation.length;
        var checks = new List<string>();
        List<Block> blocks = building.Blocks.ToList();
        Block sample = blocks.First(block => (bool)direct.Invoke(building, new object[] { block }));
        Vector3 rootPosition = building.transform.position;
        Quaternion rootRotation = building.transform.rotation;
        Vector3 rootScale = building.transform.localScale;
        Vector3 blockPosition = sample.transform.localPosition;
        Quaternion blockRotation = sample.transform.localRotation;
        Transform connectorRoot = sample.connectorParent;
        Connector face = sample.connectors.First(c => Vector3.Dot(sample.GetConnectorWorldNormal(c), -building.transform.up) > 0.75f);
        bool faceEnabled = face.canConnect;
        Vector3 facePosition = face.localPos;
        Vector3 faceNormal = face.normal;
        GameObject pivot = null;
        GameObject moving = null;
        Action<string> verify = label =>
        {
            prepare.Invoke(graph, new object[] { blocks, building.transform });
            foreach (Block block in blocks)
            {
                bool expected = (bool)direct.Invoke(building, new object[] { block });
                bool actual = (bool)cached.Invoke(building, new object[] { block });
                bool hot = (bool)cached.Invoke(building, new object[] { block });
                if (expected != actual || actual != hot)
                {
                    throw new InvalidOperationException(label + " mismatch at " + block.uniqueId);
                }
            }

            checks.Add(label);
        };
        try
        {
            verify("cold and hot support match direct geometry");
            foundation.width = 1;
            foundation.length = 1;
            verify("foundation footprint change invalidates cached result");
            foundation.width = width;
            foundation.length = length;
            verify("foundation footprint restoration matches direct geometry");
            building.transform.SetPositionAndRotation(rootPosition + new Vector3(31, 12, 17), Quaternion.Euler(19, 37, 11));
            verify("rigid root translation and tilt preserve local foundation semantics");
            building.transform.localScale = Vector3.one * 1.1f;
            verify("uniform root scaling invalidates prepared support");
            building.transform.localScale = new Vector3(1.2f, 0.9f, 1.1f);
            verify("nonuniform root scaling matches live normal test");
            building.transform.SetPositionAndRotation(rootPosition, rootRotation);
            building.transform.localScale = rootScale;
            foundation.width = width;
            foundation.length = length;
            sample.transform.localPosition += Vector3.up;
            verify("module movement invalidates support");
            sample.transform.localPosition = blockPosition;
            sample.transform.localRotation = Quaternion.Euler(0, 0, 90);
            verify("module rotation invalidates support");
            sample.transform.localRotation = blockRotation;
            face.canConnect = !faceEnabled;
            verify("connection face enabled state invalidates support");
            face.canConnect = faceEnabled;
            face.localPos += Vector3.up;
            verify("connection face position invalidates support");
            face.localPos = facePosition;
            face.normal = -faceNormal;
            verify("connection face normal invalidates support");
            face.normal = faceNormal;
            pivot = new GameObject("Owned foundation connector pivot");
            pivot.transform.SetParent(sample.transform, false);
            moving = new GameObject("Owned foundation connectors");
            moving.transform.SetParent(pivot.transform, false);
            sample.connectorParent = moving.transform;
            verify("nested connector hierarchy uses live support");
            pivot.transform.localPosition = Vector3.up;
            verify("nested connector ancestor motion remains live");
            moving.transform.SetParent(sample.transform, false);
            verify("direct connector root restores cache eligibility");
            moving.transform.localPosition = Vector3.up;
            verify("direct connector motion invalidates support");
            sample.connectorParent = connectorRoot;
            verify("restored original connector root matches live support");
            string path = "Temp/Siege/foundation-cache-checks.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, JsonUtility.ToJson(new Report { checks = checks }, true));
            return path;
        }
        finally
        {
            foundation.width = width;
            foundation.length = length;
            building.transform.SetPositionAndRotation(rootPosition, rootRotation);
            building.transform.localScale = rootScale;
            sample.transform.SetLocalPositionAndRotation(blockPosition, blockRotation);
            face.canConnect = faceEnabled;
            face.localPos = facePosition;
            face.normal = faceNormal;
            sample.connectorParent = connectorRoot;
            if (moving != null)
            {
                Object.DestroyImmediate(moving);
            }

            if (pivot != null)
            {
                Object.DestroyImmediate(pivot);
            }

            prepare.Invoke(graph, new object[] { blocks, building.transform });
        }
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

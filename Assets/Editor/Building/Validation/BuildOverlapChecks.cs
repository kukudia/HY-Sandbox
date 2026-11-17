// 职责：验证建造碰撞查询复用缓冲区后与原OverlapBox结果一致，覆盖饱和及触发器。
// 入口：Run()；仅Edit Mode、隔离位置、finally清理，不修改场景与个人存档。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BuildOverlapChecks
{
    public static string Run()
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires Edit Mode.");
        }

        var made = new List<GameObject>();
        var checks = new List<string>();
        bool originalTriggers = Physics.queriesHitTriggers;
        Type type = typeof(BuildManager).Assembly.GetType("BuildPlacementGeometry");
        object geometry = Activator.CreateInstance(type);
        type.GetField("gridSize").SetValue(geometry, 1f);
        type.GetField("blockLayer").SetValue(geometry, (LayerMask)(1 << 3));
        var query = (Func<Vector3, Quaternion, Block, bool>)Delegate.CreateDelegate(typeof(Func<Vector3, Quaternion, Block, bool>), geometry, type.GetMethod("IsBlocked"));
        try
        {
            Vector3 origin = new Vector3(5000.5f, 5000.5f, 5000.5f);
            GameObject prefab = Resources.Load<GameObject>("Blocks/1x1x1");
            GameObject self = Object.Instantiate(prefab, origin, Quaternion.identity);
            made.Add(self);
            Block block = self.GetComponent<Block>();
            Compare(
                origin,
                Quaternion.identity,
                block,
                false,
                "own collider is ignored",
                query,
                checks);
            Compare(
                origin + Vector3.right * 3,
                Quaternion.identity,
                block,
                false,
                "empty location remains clear",
                query,
                checks);
            GameObject other = Object.Instantiate(prefab, origin + Vector3.right * .25f, Quaternion.identity);
            made.Add(other);
            Compare(
                origin,
                Quaternion.identity,
                block,
                true,
                "overlapping authored block rejects placement",
                query,
                checks);
            other.transform.position = origin + Vector3.right * 2;
            Compare(
                origin,
                Quaternion.identity,
                block,
                false,
                "moving blocker away clears reusable results",
                query,
                checks);
            other.transform.position = origin;
            Collider trigger = other.GetComponentInChildren<Collider>();
            trigger.isTrigger = true;
            Physics.queriesHitTriggers = false;
            Compare(
                origin,
                Quaternion.identity,
                block,
                false,
                "global trigger disabled contract",
                query,
                checks);
            Physics.queriesHitTriggers = true;
            Compare(
                origin,
                Quaternion.identity,
                block,
                true,
                "global trigger enabled contract",
                query,
                checks);
            other.SetActive(false);
            for (int i = 0; i < 100; i++)
            {
                GameObject sensor = new GameObject("Overlap saturation fixture");
                sensor.layer = 3;
                sensor.transform.position = origin;
                sensor.AddComponent<BoxCollider>().size = Vector3.one * .2f;
                made.Add(sensor);
            }

            Compare(
                origin,
                Quaternion.identity,
                block,
                false,
                "non-block collider saturation stays clear",
                query,
                checks);
            Collider[] buffer = (Collider[])type.GetField("overlapBuffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(geometry);
            Require(buffer.Length >= 128, "buffer grew beyond saturated 32 and 64 hits", checks);
            Require(Array.TrueForAll(buffer, item => item == null), "clear query releases collider references", checks);
            other.SetActive(true);
            Compare(
                origin,
                Quaternion.identity,
                block,
                true,
                "blocking neighbour detected beyond initial buffer capacity",
                query,
                checks);
            Require(
                Array.TrueForAll(buffer, item => item == null),
                "blocked query releases collider references",
                checks);
            var random = new System.Random(1805);
            for (int i = 0; i < 160; i++)
            {
                Vector3 offset = new Vector3(
                    (float)random.NextDouble() * 4 - 2,
                    (float)random.NextDouble() * 4 - 2,
                    (float)random.NextDouble() * 4 - 2);
                Quaternion rotation = Quaternion.Euler(random.Next(4) * 90, random.Next(4) * 90, random.Next(4) * 90);
                Vector3 point = origin + offset;
                bool expected = Original(point, rotation, block);
                Compare(
                    point,
                    rotation,
                    block,
                    expected,
                    "original query parity " + i,
                    query,
                    checks);
            }

            Require(
                ReferenceEquals(
                    buffer,
                    type.GetField("overlapBuffer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(geometry)),
                "warmed query keeps the same result buffer",
                checks);
            Directory.CreateDirectory("Temp/NightDevelopment");
            string report = "passed " + checks.Count + " checks\n" + string.Join("\n", checks);
            File.WriteAllText("Temp/NightDevelopment/build-overlap.txt", report);
            return report;
        }
        finally
        {
            Physics.queriesHitTriggers = originalTriggers;
            foreach (GameObject item in made)
            {
                if (item != null)
                {
                    Object.DestroyImmediate(item);
                }
            }
        }
    }

    private static bool Original(Vector3 point, Quaternion rotation, Block block)
    {
        Physics.SyncTransforms();
        foreach (Collider collider in Physics.OverlapBox(point, Vector3.one * .5f, rotation, 1 << 3))
        {
            Block other = collider.GetComponentInParent<Block>();
            if (other != null && other != block)
            {
                return true;
            }
        }

        return false;
    }

    private static void Compare(
        Vector3 point,
        Quaternion rotation,
        Block block,
        bool expected,
        string name,
        Func<Vector3, Quaternion, Block, bool> query,
        List<string> checks)
    {
        Require(
            Original(point, rotation, block) == expected && query(point, rotation, block) == expected,
            name,
            checks);
    }

    private static void Require(bool valid, string name, List<string> checks)
    {
        if (!valid)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}

// 职责：在远离战场的真实方块夹具上测量分组，并复现断链后输入顺序影响，保留优化前证据。
// 入口：Run()；不运行物理步，不将同步图查询耗时称为大规模战斗帧率。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeTopologyBaseline
{
    public static string Run()
    {
        if (!EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Play Mode required.");
        }

        GameObject prefab = Resources.Load<GameObject>("Blocks/1x1x1");
        var report = new Report
        {
            unity = Application.unityVersion,
            utc = DateTime.UtcNow.ToString("O")
        };
        long calibrationStart = GC.GetAllocatedBytesForCurrentThread();
        byte[] calibration = new byte[4096];
        report.allocationCounterAvailable = GC.GetAllocatedBytesForCurrentThread() - calibrationStart >= calibration.Length;
        GC.KeepAlive(calibration);
        foreach (int count in new[]
        {
            100,
            256,
            1024
        }

        )
        {
            var root = new GameObject("Owned topology baseline fixture");
            try
            {
                var blocks = new List<Block>(count);
                for (int index = 0; index < count; index++)
                {
                    GameObject instance = Object.Instantiate(prefab, new Vector3(10000 + index, 2000, 0), Quaternion.identity, root.transform);
                    Block block = instance.GetComponent<Block>();
                    block.RefreshRuntimeData(true);
                    blocks.Add(block);
                }

                Physics.SyncTransforms();
                var sample = new Sample
                {
                    blocks = count
                };
                for (int repeat = 0; repeat < 8; repeat++)
                {
                    long beforeBytes = GC.GetAllocatedBytesForCurrentThread();
                    Stopwatch timer = Stopwatch.StartNew();
                    List<List<Block>> groups = BlockGroupManager.GroupBlocks(blocks);
                    timer.Stop();
                    long bytes = GC.GetAllocatedBytesForCurrentThread() - beforeBytes;
                    if (groups.Count != 1 || groups[0].Count != count)
                    {
                        throw new InvalidOperationException("Intact chain topology mismatch at " + count);
                    }

                    if (repeat > 0)
                    {
                        sample.milliseconds.Add(timer.Elapsed.TotalMilliseconds);
                        sample.bytes.Add(report.allocationCounterAvailable ? bytes : -1L);
                    }
                }

                if (count == 100)
                {
                    Block detached = blocks[count / 2];
                    detached.DisConnectAllConnectors(false);
                    List<List<Block>> forward = BlockGroupManager.GroupBlocks(blocks);
                    blocks.Reverse();
                    List<List<Block>> reverse = BlockGroupManager.GroupBlocks(blocks);
                    report.detachedForwardGroups = forward.Count;
                    report.detachedReverseGroups = reverse.Count;
                    report.detachOrderIndependent = Partition(forward) == Partition(reverse);
                    report.detachedIsSingleton = forward.Exists(group => group.Count == 1 && group[0] == detached) && reverse.Exists(group => group.Count == 1 && group[0] == detached);
                }

                report.samples.Add(sample);
            }
            finally
            {
                // 仅清理本探针的临时根，释放默认场景物理形状；不改变现有战斗单位。
                Object.DestroyImmediate(root);
            }
        }

        Directory.CreateDirectory("Temp/Siege/Performance");
        string path = "Temp/Siege/Performance/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-topology-baseline.json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path + " / orderIndependent=" + report.detachOrderIndependent + " / singleton=" + report.detachedIsSingleton;
    }

    private static string Partition(List<List<Block>> groups)
    {
        var partitions = new List<string>();
        foreach (List<Block> group in groups)
        {
            var identities = new List<int>();
            foreach (Block block in group)
            {
                identities.Add(block.GetInstanceID());
            }

            identities.Sort();
            partitions.Add(string.Join(",", identities));
        }

        partitions.Sort(StringComparer.Ordinal);
        return string.Join(";", partitions);
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "Unity Editor Play, real 1x1x1 prefab chain at remote coordinates in default physics scene. One warmup and seven timed synchronous GroupBlocks calls per size; creation, sync before first sample, partition validation and cleanup excluded. GroupBlocks internal sync included. Allocation counter is calibrated with a known allocation; unavailable results are -1, never zero-allocation proof. If supported, bytes include Stopwatch and are current-thread only, not whole-frame GC. No connection visual initialization or physics steps. Detach fixture records a known baseline defect rather than suppressing it. Not large-scale battle FPS.";
        public string unity;
        public string utc;
        public bool allocationCounterAvailable;
        public bool detachOrderIndependent;
        public bool detachedIsSingleton;
        public int detachedForwardGroups;
        public int detachedReverseGroups;
        public List<Sample> samples = new List<Sample>();
    }

    [Serializable]
    private sealed class Sample
    {
        public int blocks;
        public List<double> milliseconds = new List<double>();
        public List<long> bytes = new List<long>();
    }
}

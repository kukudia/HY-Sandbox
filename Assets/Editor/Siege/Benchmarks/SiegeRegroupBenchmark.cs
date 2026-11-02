// 职责：在相同真实方块链上交替比较冻结整组迁移与主体保留，分开记录创建/验证和同步刷新耗时。
// 入口：Run()；临时夹具每次立即清理，不将同步CPU对照称为物理帧率。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeRegroupBenchmark
{
    private static bool running;
    public static string Status { get; private set; } = "Idle";

    public static string Start()
    {
        if (running)
        {
            throw new InvalidOperationException("A regroup benchmark is already running.");
        }

        running = true;
        Status = "Scheduled";
        // CLI主线程请求有独立5秒上限；延后执行避免误将请求超时视为实验未执行。
        EditorApplication.delayCall += ExecuteScheduled;
        return Status;
    }

    private static void ExecuteScheduled()
    {
        try
        {
            Status = "Running";
            Status = Run();
        }
        catch (Exception exception)
        {
            Status = "Failed: " + exception.Message;
            UnityEngine.Debug.LogException(exception);
        }
        finally
        {
            running = false;
        }
    }

    public static string Run()
    {
        if (!EditorApplication.isPlaying || PlayManager.instance == null || PlayManager.instance.playMode)
        {
            throw new InvalidOperationException("Requires Play Mode with simulation inactive (Siege deployment).");
        }

        var report = new Report
        {
            unity = Application.unityVersion,
            utc = DateTime.UtcNow.ToString("O")
        };
        var serialized = new SerializedObject(PlayManager.instance);
        var settings = new GroupPhysicsSettings(
            serialized.FindProperty("_groupLinearDamping").floatValue,
            serialized.FindProperty("_groupAngularDamping").floatValue,
            serialized.FindProperty("_groupMaxDepenetrationVelocity").floatValue,
            serialized.FindProperty("_maximumInheritedTangentialSpeed").floatValue,
            serialized.FindProperty("_maximumInheritedAngularSpeed").floatValue);
        var baseline = new SiegeRegroupBaseline(PlayManager.instance);
        foreach (int count in new[]
        {
            100,
            256,
            1024
        }

        )
        {
            foreach (bool split in new[]
            {
                false,
                true
            }

            )
            {
                var sample = new Sample
                {
                    blocks = count,
                    split = split
                };
                // 两次预热后ABBAABBA，分散顺序、JIT和垃圾回收影响；每次使用新建相同夹具。
                bool[] order =
                {
                    true,
                    false,
                    true,
                    false,
                    false,
                    true,
                    true,
                    false,
                    false,
                    true
                };
                for (int index = 0; index < order.Length; index++)
                {
                    bool useBaseline = order[index];
                    double milliseconds = Measure(count, split, useBaseline, baseline, settings);
                    if (index >= 2)
                    {
                        (useBaseline ? sample.baselineMs : sample.retainedMs).Add(milliseconds);
                    }
                }

                sample.baselineMedianMs = Median(sample.baselineMs);
                sample.retainedMedianMs = Median(sample.retainedMs);
                report.samples.Add(sample);
            }
        }

        Directory.CreateDirectory("Temp/Siege/Performance");
        string path = "Temp/Siege/Performance/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-regroup-comparison.json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    private static double Measure(int count, bool split, bool baseline, SiegeRegroupBaseline previous, GroupPhysicsSettings settings)
    {
        GameObject root = Object.Instantiate(
            Resources.Load<GameObject>("BlocksParent/BlocksParent"),
            new Vector3(10000, 2000, 0),
            Quaternion.identity);
        var blocks = new List<Block>(count);
        try
        {
            ControlUnit source = root.GetComponent<ControlUnit>();
            GameObject prefab = Resources.Load<GameObject>("Blocks/1x1x1");
            for (int index = 0; index < count; index++)
            {
                Block block = Object.Instantiate(prefab, root.transform.position + Vector3.right * index, Quaternion.identity, root.transform).GetComponent<Block>();
                block.RefreshRuntimeData(true);
                block.connectorPrefab = null;
                blocks.Add(block);
            }

            source.RefreshChildren();
            source.AssignRuntimeOwnershipToBlocks(true);
            PlayManager.instance.RegisterControlUnit(source);
            Type builder = typeof(Block).Assembly.GetType("BatchConnectionBuilder", true);
            object batch = Activator.CreateInstance(builder, new object[] { blocks, 2f });
            builder.GetMethod("RebuildBatchImmediate", BindingFlags.Public | BindingFlags.Instance).Invoke(batch, null);
            if (split)
            {
                blocks[count - 4].DisConnectAllConnectors(false);
            }

            Physics.SyncTransforms();
            Stopwatch timer = Stopwatch.StartNew();
            if (baseline)
            {
                List<Block> input = source.GetComponentsInChildren<Block>(true).Where(block => block != null && block.isActiveAndEnabled).ToList();
                previous.Rebuild(input, settings);
                foreach (ControlUnit unit in PlayManager.instance.GetControlUnits().ToArray())
                {
                    if (unit != null)
                    {
                        unit.RefreshChildren();
                    }
                }
            }
            else
            {
                PlayManager.instance.RefreshGroup(source);
            }

            timer.Stop();
            ControlUnit[] groups = blocks.Select(block => block.GetComponentInParent<ControlUnit>()).Distinct().ToArray();
            if (groups.Length != (split ? 3 : 1) || groups.Any(unit => Mathf.Abs(unit.GetComponent<Rigidbody>().mass - unit.GetComponentsInChildren<Block>().Sum(block => block.mass)) > 0.001f))
            {
                throw new InvalidOperationException("Benchmark grouping/mass mismatch.");
            }

            if (!baseline && root.GetComponentsInChildren<Block>().Length != (split ? count - 4 : count))
            {
                throw new InvalidOperationException("Largest component was not retained.");
            }

            return timer.Elapsed.TotalMilliseconds;
        }
        finally
        {
            var owned = new HashSet<GameObject>
            {
                root
            };
            foreach (Block block in blocks)
            {
                if (block != null && block.GetComponentInParent<ControlUnit>() != null)
                {
                    owned.Add(block.GetComponentInParent<ControlUnit>().gameObject);
                }
            }

            foreach (GameObject unit in owned)
            {
                Object.DestroyImmediate(unit);
            }
        }
    }

    private static double Median(List<double> values)
    {
        var ordered = values.OrderBy(value => value).ToArray();
        return (ordered[ordered.Length / 2 - 1] + ordered[ordered.Length / 2]) * 0.5;
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "Unity Editor Siege deployment, synchronous real 1x1x1 prefab chains, no physics steps/connection visuals. Frozen 3b1e13d body migration plus original global RefreshChildren versus current full RefreshGroup. Both initialize invisible reciprocal links before timing; baseline traverses current symmetric geometry, current traverses valid reciprocal adjacency. Baseline excludes cleanup, current includes cleanup/registry changes. Creation, link setup/sync, validation and immediate cleanup excluded; internal query sync and original baseline Debug.Log included. One warmup per variant then ABBAABBA (four measurements each). Split at count-4 creates three components, retaining count-4 on the original body in current implementation. No cockpit in chain; gameplay contracts tested separately. Editor CPU comparison, not Player frame rate or independent per-block physics.";
        public string unity;
        public string utc;
        public List<Sample> samples = new List<Sample>();
    }

    [Serializable]
    private sealed class Sample
    {
        public int blocks;
        public bool split;
        public List<double> baselineMs = new List<double>();
        public List<double> retainedMs = new List<double>();
        public double baselineMedianMs;
        public double retainedMedianMs;
    }
}

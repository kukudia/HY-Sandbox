// 职责：在真实 Unity 组件上交替测量冻结基线与当前供电实现，并验证行为等价。
// 入口：Run()；Play Mode 同步执行，生成原始样本，不将子系统耗时转换成游戏 FPS。
// 生命周期：使用两套相同布局防止连接列表污染缓存，finally 销毁所有临时对象。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class PowerPerformanceChecks
{
    private static Rig _frameRig;
    private static readonly List<double> FrameTimes = new List<double>();
    private static readonly List<double> RebuildTimes = new List<double>();
    private static readonly List<FrameSample> FrameResults = new List<FrameSample>();
    private static int _frameStage;
    private static int _previousFrame;
    private static double _previousTime;
    private static bool _previousBackground;
    [Serializable]
    private sealed class FrameSample
    {
        public string implementation;
        public double medianFrameMs;
        public double p95FrameMs;
        public double medianRebuildMs;
        public double[] rawFrameMs;
        public double[] rawRebuildMs;
    }

    [Serializable]
    private sealed class FrameReport
    {
        public string methodology = "Unity Editor Main Play Mode, 100 relays/500 loads, ABBA four windows; 20 frame warmup + 100 frame samples each. Benchmark rig removed from global registries to prevent duplicate network processing. Frame intervals include surrounding scene/editor/vsync; not standalone Player FPS or GPU measurements.";
        public List<FrameSample> samples;
    }

    public static void StartFrames()
    {
        if (!Application.isPlaying || _frameRig != null)
        {
            throw new InvalidOperationException("Requires Play Mode and no running frame probe.");
        }

        _previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        FrameResults.Clear();
        _frameStage = 0;
        BeginWindow();
        EditorApplication.update += FrameTick;
    }

    private static void BeginWindow()
    {
        _frameRig = new Rig(100, 500);
        // 保持参与者 enabled，但退出自动装配注册表，由探针每真实帧仅执行选定算法。
        Unregister("ActiveDevices", typeof(PowerTransmissionDevice), _frameRig.devices);
        Unregister("ActiveGenerators", typeof(PowerGeneratingUnit), _frameRig.generators);
        Unregister("ActivePowerBlocks", typeof(Power), _frameRig.loads);
        FrameTimes.Clear();
        RebuildTimes.Clear();
        _previousFrame = Time.frameCount;
        _previousTime = EditorApplication.timeSinceStartup;
    }

    private static void Unregister<T>(string field, Type type, List<T> participants)
    {
        HashSet<T> registry = (HashSet<T>)type.GetField(field, BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
        foreach (T participant in participants)
        {
            registry.Remove(participant);
        }
    }

    private static void FrameTick()
    {
        if (!Application.isPlaying)
        {
            FinishFrames();
            return;
        }

        if (Time.frameCount == _previousFrame)
        {
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        FrameTimes.Add((now - _previousTime) * 1000);
        _previousTime = now;
        _previousFrame = Time.frameCount;
        Stopwatch timer = Stopwatch.StartNew();
        _frameRig.Rebuild(_frameStage == 0 || _frameStage == 3);
        timer.Stop();
        RebuildTimes.Add(timer.Elapsed.TotalMilliseconds);
        if (FrameTimes.Count < 120)
        {
            return;
        }

        double[] frames = FrameTimes.GetRange(20, 100).ToArray();
        double[] costs = RebuildTimes.GetRange(20, 100).ToArray();
        FrameResults.Add(new FrameSample { implementation = _frameStage == 0 || _frameStage == 3 ? "baseline" : "optimized", medianFrameMs = Percentile(frames, 0.5), p95FrameMs = Percentile(frames, 0.95), medianRebuildMs = Percentile(costs, 0.5), rawFrameMs = frames, rawRebuildMs = costs });
        _frameRig.Dispose();
        _frameRig = null;
        _frameStage++;
        if (_frameStage == 4)
        {
            File.WriteAllText(
                "Docs/Validation/2026-10-14-power-frames.json",
                JsonUtility.ToJson(new FrameReport { samples = FrameResults }, true));
            FinishFrames();
        }
        else
        {
            BeginWindow();
        }
    }

    private static void FinishFrames()
    {
        EditorApplication.update -= FrameTick;
        _frameRig?.Dispose();
        _frameRig = null;
        Application.runInBackground = _previousBackground;
    }

    [Serializable]
    private sealed class Samples
    {
        public string scenario;
        public int relays;
        public int generators;
        public int loads;
        public double[] baselineMs;
        public double[] optimizedMs;
        public long[] baselineBytes;
        public long[] optimizedBytes;
        public double baselineMedianMs;
        public double optimizedMedianMs;
        public double baselineP95Ms;
        public double optimizedP95Ms;
    }

    [Serializable]
    private sealed class Report
    {
        public string baselineCommit = "14496aa";
        public string unityVersion;
        public string device;
        public string methodology = "Play Mode; identical separate rigs; 20 warmups, 40 alternating AB/BA samples, 3 rebuilds per sample; Stopwatch + current-thread allocated bytes. Setup, movement and validation excluded. Subsystem CPU cost, not FPS or GPU time.";
        public string allocationCounter = "INVALID: calibration allocated 4096 bytes but GC.GetAllocatedBytesForCurrentThread returned zero in this Unity runtime. Ignore byte samples; no zero-GC claim.";
        public int equivalenceChecks;
        public List<Samples> samples = new List<Samples>();
    }

    private sealed class Rig : IDisposable
    {
        public readonly GameObject root = new GameObject("Power benchmark temporary rig");
        public readonly List<PowerTransmissionDevice> devices = new List<PowerTransmissionDevice>();
        public readonly List<PowerGeneratingUnit> generators = new List<PowerGeneratingUnit>();
        public readonly List<Power> loads = new List<Power>();
        public readonly PowerNetwork optimized = new PowerNetwork();
        public readonly PowerNetworkBaseline baseline = new PowerNetworkBaseline();
        public Rig(int count, int loadCount)
        {
            root.hideFlags = HideFlags.DontSave;
            for (int i = 0; i < count; i++)
            {
                Vector3 position = new Vector3((i % 10) * 12f, (i / 100) * 12f, (i / 10 % 10) * 12f) + Vector3.one * 4000f;
                PowerTransmissionDevice relay = Child<PowerTransmissionDevice>(position);
                relay.maxConnectionDistance = 14;
                relay.powerRange = 8;
                devices.Add(relay);
                if (i % 10 == 0)
                {
                    PowerGeneratingUnit generator = Child<PowerGeneratingUnit>(position);
                    generator.outputPower = 350f + i;
                    generators.Add(generator);
                }
            }

            for (int i = 0; i < loadCount; i++)
            {
                Vector3 position = devices[i % count].transform.position + new Vector3(i % 5 - 2, 0, i % 3 - 1);
                Power load = Child<Power>(position);
                load.minWorkingPower = 5f;
                load.standardWorkingPower = 80f + i % 7 * 13;
                load.SetWorkRequested(root, true);
                loads.Add(load);
            }
        }

        private T Child<T>(Vector3 position)
            where T : Component
        {
            GameObject child = new GameObject(typeof(T).Name);
            child.transform.SetParent(root.transform, false);
            child.transform.position = position;
            return child.AddComponent<T>();
        }

        public void Rebuild(bool useBaseline)
        {
            if (useBaseline)
            {
                baseline.Rebuild(devices, generators, loads);
            }
            else
            {
                optimized.Rebuild(devices, generators, loads);
            }
        }

        public void Change(int step, string scenario)
        {
            if (scenario == "moving")
            {
                // 每次调用都使拓扑失效，不能让批次内后两次命中静态缓存。
                root.transform.position = Vector3.right * ((step % 2) * 0.125f);
            }
            else if (scenario == "requests")
            {
                for (int i = 0; i < loads.Count; i += 3)
                {
                    loads[i].SetWorkRequested(root, step % 2 == 0);
                }
            }
        }

        public void Dispose()
        {
            optimized.Clear(loads);
            Object.DestroyImmediate(root);
        }
    }

    public static string Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Power benchmarks require Play Mode.");
        }

        Report report = new Report
        {
            unityVersion = Application.unityVersion,
            device = SystemInfo.processorType
        };
        CheckEquivalence(report);
        foreach (int count in new[]
        {
            20,
            100,
            300
        }

        )
        {
            foreach (string scenario in new[]
            {
                "static",
                "moving",
                "requests"
            }

            )
            {
                report.samples.Add(Measure(count, count * 5, scenario));
            }
        }

        string json = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Docs/Validation");
        File.WriteAllText("Docs/Validation/2026-10-14-power-performance.json", json);
        return json;
    }

    private static Samples Measure(int count, int loadCount, string scenario)
    {
        using (Rig before = new Rig(count, loadCount))
        {
            using (Rig after = new Rig(count, loadCount))
            {
                Samples result = new Samples
                {
                    scenario = scenario,
                    relays = count,
                    generators = before.generators.Count,
                    loads = loadCount,
                    baselineMs = new double[40],
                    optimizedMs = new double[40],
                    baselineBytes = new long[40],
                    optimizedBytes = new long[40]
                };
                for (int i = 0; i < 20; i++)
                {
                    before.Change(i, scenario);
                    after.Change(i, scenario);
                    before.Rebuild(true);
                    after.Rebuild(false);
                }

                Stopwatch timer = new Stopwatch();
                for (int i = 0; i < 40; i++)
                {
                    for (int order = 0; order < 2; order++)
                    {
                        bool isBaseline = (i + order) % 2 == 0;
                        Rig rig = isBaseline ? before : after;
                        double totalMs = 0;
                        long totalBytes = 0;
                        for (int repeat = 0; repeat < 3; repeat++)
                        {
                            rig.Change(i * 3 + repeat, scenario);
                            long bytes = GC.GetAllocatedBytesForCurrentThread();
                            timer.Restart();
                            rig.Rebuild(isBaseline);
                            timer.Stop();
                            totalBytes += GC.GetAllocatedBytesForCurrentThread() - bytes;
                            totalMs += timer.Elapsed.TotalMilliseconds;
                        }

                        (isBaseline ? result.baselineMs : result.optimizedMs)[i] = totalMs / 3;
                        (isBaseline ? result.baselineBytes : result.optimizedBytes)[i] = totalBytes / 3;
                    }
                }

                result.baselineMedianMs = Percentile(result.baselineMs, 0.5);
                result.optimizedMedianMs = Percentile(result.optimizedMs, 0.5);
                result.baselineP95Ms = Percentile(result.baselineMs, 0.95);
                result.optimizedP95Ms = Percentile(result.optimizedMs, 0.95);
                return result;
            }
        }
    }

    private static double Percentile(double[] values, double percentile)
    {
        double[] sorted = (double[])values.Clone();
        Array.Sort(sorted);
        return sorted[(int)Math.Ceiling(percentile * sorted.Length) - 1];
    }

    private static void CheckEquivalence(Report report)
    {
        using (Rig before = new Rig(24, 120))
        {
            using (Rig after = new Rig(24, 120))
            {
                System.Random random = new System.Random(20261014);
                for (int step = 0; step < 150; step++)
                {
                    for (int i = 0; i < before.devices.Count; i++)
                    {
                        Vector3 position = Vector3.one * 4000f + new Vector3(random.Next(-20, 21), random.Next(-8, 9), random.Next(-20, 21));
                        int connection = random.Next(0, 17);
                        int range = random.Next(0, 14);
                        foreach (Rig rig in new[]
                        {
                            before,
                            after
                        }

                        )
                        {
                            rig.devices[i].transform.position = position;
                            rig.devices[i].maxConnectionDistance = connection;
                            rig.devices[i].powerRange = range;
                            rig.devices[i].enabled = (i + step) % 11 != 0;
                        }
                    }

                    for (int i = 0; i < before.generators.Count; i++)
                    {
                        Vector3 position = before.devices[i * 10].transform.position;
                        float output = random.Next(0, 2000);
                        foreach (Rig rig in new[]
                        {
                            before,
                            after
                        }

                        )
                        {
                            rig.generators[i].transform.position = position;
                            rig.generators[i].outputPower = output;
                            rig.generators[i].enabled = (i + step) % 7 != 0;
                        }
                    }

                    for (int i = 0; i < before.loads.Count; i++)
                    {
                        Vector3 position = before.devices[i % 24].transform.position + new Vector3(random.Next(-14, 15), 0, 0);
                        foreach (Rig rig in new[]
                        {
                            before,
                            after
                        }

                        )
                        {
                            rig.loads[i].transform.position = position;
                            rig.loads[i].enabled = (i + step) % 13 != 0;
                            rig.loads[i].SetWorkRequested(rig.root, (i + step) % 3 != 0);
                        }
                    }

                    // 同一拓扑再跑一次，覆盖负载位置/需求/发电量变化且缓存命中的路径。
                    for (int pass = 0; pass < 2; pass++)
                    {
                        if (pass == 1)
                        {
                            before.generators[0].outputPower += 19;
                            after.generators[0].outputPower += 19;
                            before.loads[1].SetWorkRequested(before.root, true);
                            after.loads[1].SetWorkRequested(after.root, true);
                            before.loads[2].transform.position += Vector3.right * 9;
                            after.loads[2].transform.position += Vector3.right * 9;
                        }

                        before.Rebuild(true);
                        after.Rebuild(false);
                        Compare(before, after, step);
                        report.equivalenceChecks++;
                    }
                }
            }
        }
    }

    private static void Compare(Rig before, Rig after, int step)
    {
        for (int i = 0; i < before.loads.Count; i++)
        {
            Power first = before.loads[i];
            Power second = after.loads[i];
            if (Mathf.Abs(first.currentPower - second.currentPower) > 0.001f || first.isWorking != second.isWorking || Mathf.Abs(first.SupplyEfficiency - second.SupplyEfficiency) > 0.0001f)
            {
                throw new InvalidOperationException($"Power mismatch at step {step}, load {i}: {first.currentPower} / {second.currentPower}");
            }
        }

        for (int i = 0; i < before.devices.Count; i++)
        {
            PowerTransmissionDevice first = before.devices[i];
            PowerTransmissionDevice second = after.devices[i];
            if (Mathf.Abs(first.availableNetworkPower - second.availableNetworkPower) > 0.001f || first.connectedDevices.Count != second.connectedDevices.Count || first.connectedGenerators.Count != second.connectedGenerators.Count)
            {
                throw new InvalidOperationException($"Topology mismatch at step {step}, relay {i}");
            }

            foreach (PowerTransmissionDevice peer in first.connectedDevices)
            {
                if (!second.connectedDevices.Contains(after.devices[before.devices.IndexOf(peer)]))
                {
                    throw new InvalidOperationException("Different relay edge");
                }
            }

            foreach (PowerGeneratingUnit generator in first.connectedGenerators)
            {
                if (!second.connectedGenerators.Contains(after.generators[before.generators.IndexOf(generator)]))
                {
                    throw new InvalidOperationException("Different generator edge");
                }
            }
        }
    }
}

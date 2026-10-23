// 职责：采集阵地战真实Play帧观察间隔、分阶段CPU计时和低频场景规模，保存可审查原始样本。
// 入口：StartCapture() / Finish() / Status；Editor观察间隔含编辑器调度，不代表Player或GPU耗时。
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SiegePerformanceCapture
{
    private static Report report;
    private static SiegeSession session;
    private static ProfilerRecorder gcRecorder;
    private static double started;
    private static double previous;
    private static double nextCensus;
    private static double duration;
    private static int previousFrame;
    private static bool previousMetrics;
    private static string outputPath;
    public static string Status => report == null ? "Idle" : $"Capturing {report.samples.Count} observations / {EditorApplication.timeSinceStartup - started:F1}s";

    static SiegePerformanceCapture()
    {
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
    }

    public static string StartCapture(string label = "natural-baseline", float seconds = 90f)
    {
        if (report != null)
        {
            throw new InvalidOperationException("Finish the current capture first.");
        }

        session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null)
        {
            throw new InvalidOperationException("Requires a running Siege session.");
        }

        string safeLabel = string.Join("_", label.Split(Path.GetInvalidFileNameChars()));
        Directory.CreateDirectory("Temp/Siege/Performance");
        outputPath = "Temp/Siege/Performance/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + safeLabel + ".json";
        report = new Report
        {
            label = label,
            utc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            processor = SystemInfo.processorType,
            processorCount = SystemInfo.processorCount,
            seed = session.Settings.SpawnSeed,
            maxActiveAttackers = session.Settings.MaximumActiveEnemies,
            initialWave = session.Match.Wave,
            initialCoins = session.Match.Coins
        };
        previousMetrics = SimulationWorkMetrics.Enabled;
        SimulationWorkMetrics.Reset();
        SimulationWorkMetrics.Enabled = true;
        gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
        report.gcCounterAvailable = gcRecorder.Valid;
        started = previous = EditorApplication.timeSinceStartup;
        nextCensus = started;
        duration = Mathf.Max(1f, seconds);
        previousFrame = Time.frameCount;
        EditorApplication.update += Tick;
        return outputPath;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || session == null)
        {
            Finish("Play stopped or session removed");
            return;
        }

        if (Time.frameCount == previousFrame)
        {
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        report.samples.Add(new FrameSample { seconds = now - started, frame = Time.frameCount, advancedFrames = Time.frameCount - previousFrame, editorObservationMs = (now - previous) * 1000.0, gameDeltaMs = Time.unscaledDeltaTime * 1000.0, gcBytes = gcRecorder.Valid ? gcRecorder.LastValue : -1, connectivity = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.Connectivity), rebuild = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BodyRebuild), refresh = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.Refresh), buildingSupport = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingSupport), buildingDamage = SimulationWorkMetrics.Read(SimulationWorkMetrics.Stage.BuildingDamage) });
        previousFrame = Time.frameCount;
        previous = now;
        if (now >= nextCensus)
        {
            nextCensus = now + 1.0;
            TakeCensus(now);
        }

        if (now - started >= duration || session.Match.Phase == SiegePhase.Defeat)
        {
            Finish(session.Match.Phase == SiegePhase.Defeat ? "Base defeated" : "Duration reached");
        }
    }

    private static void TakeCensus(double now)
    {
        int units = 0;
        int fragments = 0;
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (unit != null && unit.gameObject.scene == session.gameObject.scene)
            {
                units++;
                if (!unit.HasAnyCockpit && unit.Building == null)
                {
                    fragments++;
                }
            }
        }

        // 全场查询仅每秒一次；该观察本身会产生开销，报告明确保留此限制。
        Block[] blocks = Object.FindObjectsByType<Block>(FindObjectsSortMode.None);
        Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        Rigidbody[] bodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        int awake = 0;
        foreach (Rigidbody body in bodies)
        {
            if (!body.isKinematic && !body.IsSleeping())
            {
                awake++;
            }
        }

        report.census.Add(new Census { seconds = now - started, wave = session.Match.Wave, phase = session.Match.Phase.ToString(), coins = session.Match.Coins, attackers = session.Match.ActiveAttackers, pending = session.Match.PendingAttackers, units = units, fragments = fragments, blocks = blocks.Length, colliders = colliders.Length, rigidbodies = bodies.Length, awakeDynamicBodies = awake, baseBlocks = session.Fleet.Base.Building.Blocks.Count, baseAlive = session.Fleet.Base.IsAlive });
    }

    public static string Finish(string reason = "Manual stop")
    {
        if (report == null)
        {
            return "No active capture";
        }

        EditorApplication.update -= Tick;
        SimulationWorkMetrics.Enabled = previousMetrics;
        gcRecorder.Dispose();
        report.reason = reason;
        report.elapsedSeconds = EditorApplication.timeSinceStartup - started;
        report.physicsNeighborQueries = SimulationWorkMetrics.PhysicsNeighborQueries;
        report.legacyReferenceScans = SimulationWorkMetrics.LegacyReferenceScans;
        var intervals = new List<double>();
        foreach (FrameSample sample in report.samples)
        {
            if (sample.advancedFrames == 1)
            {
                intervals.Add(sample.editorObservationMs);
            }
            else
            {
                report.skippedObservations++;
            }
        }

        intervals.Sort();
        report.observationMedianMs = Percentile(intervals, 0.5);
        report.observationP95Ms = Percentile(intervals, 0.95);
        report.observationP99Ms = Percentile(intervals, 0.99);
        string path = outputPath;
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(report, true));
        }
        finally
        {
            report = null;
            session = null;
        }

        Debug.Log("Siege performance capture saved: " + path + " / " + reason);
        return path;
    }

    private static double Percentile(List<double> values, double fraction)
    {
        return values.Count == 0 ? 0.0 : values[Mathf.Clamp((int)Math.Ceiling(values.Count * fraction) - 1, 0, values.Count - 1)];
    }

    private static void BeforeReload()
    {
        Finish("Assembly reload interrupted capture");
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "Unity Editor Siege Play. Editor update observes advancing game frames; single-frame observation intervals are summarized, gaps retained and counted. Game delta is also retained. CPU counters are cumulative and nested: refresh includes rebuild, rebuild includes connectivity; never sum them. Full refresh timing begins after input block enumeration. GC counter is observed last profiler value, not a complete allocation trace. Once-per-second scene census allocates and perturbs measurements. Includes editor, render and scheduling effects; not Player FPS, GPU time, deterministic combat or an isolated subsystem benchmark.";
        public string label;
        public string utc;
        public string unityVersion;
        public string processor;
        public int processorCount;
        public int seed;
        public int maxActiveAttackers;
        public int initialWave;
        public long initialCoins;
        public bool gcCounterAvailable;
        public long physicsNeighborQueries;
        public long legacyReferenceScans;
        public string reason;
        public double elapsedSeconds;
        public int skippedObservations;
        public double observationMedianMs;
        public double observationP95Ms;
        public double observationP99Ms;
        public List<FrameSample> samples = new List<FrameSample>(16384);
        public List<Census> census = new List<Census>(256);
    }

    [Serializable]
    private struct FrameSample
    {
        public double seconds;
        public int frame;
        public int advancedFrames;
        public double editorObservationMs;
        public double gameDeltaMs;
        public long gcBytes;
        public SimulationWorkMetrics.Counters connectivity;
        public SimulationWorkMetrics.Counters rebuild;
        public SimulationWorkMetrics.Counters refresh;
        public SimulationWorkMetrics.Counters buildingSupport;
        public SimulationWorkMetrics.Counters buildingDamage;
    }

    [Serializable]
    private struct Census
    {
        public double seconds;
        public int wave;
        public string phase;
        public long coins;
        public int attackers;
        public int pending;
        public int units;
        public int fragments;
        public int blocks;
        public int colliders;
        public int rigidbodies;
        public int awakeDynamicBodies;
        public int baseBlocks;
        public bool baseAlive;
    }
}

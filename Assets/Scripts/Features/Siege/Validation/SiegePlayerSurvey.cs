// 职责：显式Player检查的低频规模普查、有限慢帧记录和可选嵌套CPU计时；不参与正常游戏。
// 入口：Observe() / Dispose()；帧间隔对应前一观察窗口，普查自身开销单独记录，不能据相关性推断瓶颈。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

public sealed class SiegePlayerSurvey : IDisposable
{
    private const int SlowFrameLimit = 32;
    private readonly bool previousMetrics;
    private readonly SiegeEngineCounters engine;
    private readonly float started;
    private readonly Work baseline;
    private Work previousWork;
    private Work previousFrameWork;
    private bool observed;
    private bool disposed;
    private int previousSurveyIndex = -1;
    private float nextCensus;
    public readonly List<Census> CensusSamples = new List<Census>();
    public readonly Summary Result = new Summary();
    public SiegePlayerSurvey(float started, bool profileWork)
    {
        this.started = started;
        previousMetrics = SimulationWorkMetrics.Enabled;
        Result.workMetricsEnabled = profileWork;
        if (profileWork)
        {
            SimulationWorkMetrics.Enabled = true;
            engine = new SiegeEngineCounters();
            Result.engineCounters = engine.Counters;
        }

        baseline = previousWork = ReadWork();
    }

    public void Observe(SiegeSession session, float milliseconds)
    {
        if (disposed)
        {
            return;
        }

        Work now = ReadWork();
        SiegeEngineCounters.Sample engineSample = engine != null ? engine.Read() : default;
        float seconds = Time.realtimeSinceStartup - started;
        if (observed)
        {
            Result.maximumMs = Mathf.Max(Result.maximumMs, milliseconds);
            Result.over33Ms += milliseconds > 1000f / 30f ? 1 : 0;
            Result.over50Ms += milliseconds > 50f ? 1 : 0;
            Result.over100Ms += milliseconds > 100f ? 1 : 0;
            Result.over250Ms += milliseconds > 250f ? 1 : 0;
            if (milliseconds > 1000f / 30f)
            {
                KeepSlowFrame(new SlowFrame { seconds = seconds, frame = Time.frameCount, milliseconds = milliseconds, precedingCensus = CensusSamples.Count - 1, precedingFrameSurvey = previousSurveyIndex, precedingFrameWork = previousFrameWork, engine = engineSample });
            }
        }

        // 当前Update的累计量留给下一帧delta关联；仅近似观察窗口，不是PlayerLoop/GPU分析器。
        previousFrameWork = Work.Subtract(now, previousWork);
        previousWork = now;
        observed = true;
        previousSurveyIndex = -1;
        if (session != null && Time.realtimeSinceStartup >= nextCensus)
        {
            nextCensus = Time.realtimeSinceStartup + 1f;
            previousSurveyIndex = CensusSamples.Count;
            CensusSamples.Add(TakeCensus(session, seconds, Work.Subtract(now, baseline)));
        }
    }

    public static BodyCounts Classify(Rigidbody[] bodies)
    {
        var counts = new BodyCounts();
        foreach (Rigidbody body in bodies)
        {
            if (body == null || !body.gameObject.activeInHierarchy)
            {
                continue;
            }

            counts.total++;
            if (body.isKinematic)
            {
                counts.kinematic++;
            }
            else if (body.IsSleeping())
            {
                counts.sleepingDynamic++;
            }
            else
            {
                counts.awakeDynamic++;
            }

            // 只按刚体根上可核验的组件分类；无标记建筑残骸归other，不靠名称猜测。
            if (body.TryGetComponent(out ControlUnit unit))
            {
                if (unit.Building != null)
                {
                    counts.buildings++;
                }
                else if (unit.HasValidCockpit)
                {
                    counts.ships++;
                }
                else
                {
                    counts.fragments++;
                }
            }
            else if (body.TryGetComponent<TurretProjectile>(out _))
            {
                counts.projectiles++;
            }
            else
            {
                counts.other++;
            }
        }

        return counts;
    }

    private static Census TakeCensus(SiegeSession session, float seconds, Work work)
    {
        long begin = Stopwatch.GetTimestamp();
        Block[] blocks = Object.FindObjectsByType<Block>(FindObjectsSortMode.None);
        Rigidbody[] bodies = Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
        Collider[] colliders = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
        var sample = new Census
        {
            seconds = seconds,
            frame = Time.frameCount,
            wave = session.Match.Wave,
            phase = session.Match.Phase.ToString(),
            attackers = session.Match.ActiveAttackers,
            pending = session.Match.PendingAttackers,
            coins = session.Match.Coins,
            baseBlocks = session.Fleet.Base.Building.Blocks.Count,
            baseAlive = session.Fleet.Base.IsAlive,
            blocks = blocks.Length,
            bodies = bodies.Length,
            colliders = colliders.Length,
            bodyKinds = Classify(bodies),
            memoryBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
            work = work
        };
        sample.surveyMs = (Stopwatch.GetTimestamp() - begin) * 1000.0 / Stopwatch.Frequency;
        return sample;
    }

    private void KeepSlowFrame(SlowFrame frame)
    {
        List<SlowFrame> slow = Result.slowestFrames;
        int index = 0;
        while (index < slow.Count && slow[index].milliseconds >= frame.milliseconds)
        {
            index++;
        }

        if (index < SlowFrameLimit)
        {
            slow.Insert(index, frame);
            if (slow.Count > SlowFrameLimit)
            {
                slow.RemoveAt(SlowFrameLimit);
            }
        }
    }

    private Work ReadWork()
    {
        if (!Result.workMetricsEnabled)
        {
            return default;
        }

        var sample = new Work
        {
            connectivity = ReadStage(SimulationWorkMetrics.Stage.Connectivity),
            bodyRebuild = ReadStage(SimulationWorkMetrics.Stage.BodyRebuild),
            refresh = ReadStage(SimulationWorkMetrics.Stage.Refresh),
            buildingSupport = ReadStage(SimulationWorkMetrics.Stage.BuildingSupport),
            buildingDamage = ReadStage(SimulationWorkMetrics.Stage.BuildingDamage),
            buildingGraph = ReadStage(SimulationWorkMetrics.Stage.BuildingGraph),
            buildingGround = ReadStage(SimulationWorkMetrics.Stage.BuildingGround),
            buildingTraversal = ReadStage(SimulationWorkMetrics.Stage.BuildingTraversal),
            buildingDebris = ReadStage(SimulationWorkMetrics.Stage.BuildingDebris),
            siegeSimulation = ReadStage(SimulationWorkMetrics.Stage.SiegeSimulation),
            siegeFleetPhysics = ReadStage(SimulationWorkMetrics.Stage.SiegeFleetPhysics),
            siegeRefit = ReadStage(SimulationWorkMetrics.Stage.SiegeRefit),
            siegeWorkshop = ReadStage(SimulationWorkMetrics.Stage.SiegeWorkshop)
        };
        sample.physicsNeighborQueries = SimulationWorkMetrics.PhysicsNeighborQueries;
        sample.legacyReferenceScans = SimulationWorkMetrics.LegacyReferenceScans;
        return sample;
    }

    private static StageWork ReadStage(SimulationWorkMetrics.Stage stage)
    {
        SimulationWorkMetrics.Counters counters = SimulationWorkMetrics.Read(stage);
        return new StageWork
        {
            calls = counters.calls,
            inputBlocks = counters.inputBlocks,
            milliseconds = counters.milliseconds
        };
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Result.work = Work.Subtract(ReadWork(), baseline);
        engine?.Dispose();
        SimulationWorkMetrics.Enabled = previousMetrics;
        disposed = true;
    }

    [Serializable]
    public sealed class Summary
    {
        public string methodology = "Active scene objects only; inactive pooled projectiles excluded. Root component categories are disjoint, unmarked building debris belongs to other. One census/second allocates query arrays; surveyMs measures its CPU cost. Delta is start-to-start; precedingFrameWork is previous LateUpdate-to-LateUpdate cumulative difference, approximate association only. Nested work stages cannot be added. No GPU/PhysX attribution. First observation omitted from slow-frame summary. Engine counters are optional engine published samples across recorded threads; -1 means unavailable or no new sample. Counter units and availability recorded; nested markers cannot be added. Publication lag means engine sample alignment is approximate. Top 32 frames over 33.33ms retained; indices refer to census array, -1 means none.";
        public bool workMetricsEnabled;
        public float maximumMs;
        public int over33Ms;
        public int over50Ms;
        public int over100Ms;
        public int over250Ms;
        public Work work;
        public List<SiegeEngineCounters.Counter> engineCounters = new List<SiegeEngineCounters.Counter>();
        public List<SlowFrame> slowestFrames = new List<SlowFrame>();
    }

    [Serializable]
    public struct Census
    {
        public float seconds;
        public int frame;
        public int wave;
        public string phase;
        public int attackers;
        public int pending;
        public long coins;
        public int baseBlocks;
        public bool baseAlive;
        public int blocks;
        public int bodies;
        public int colliders;
        public BodyCounts bodyKinds;
        public long memoryBytes;
        public double surveyMs;
        public Work work;
    }

    [Serializable]
    public struct BodyCounts
    {
        public int total;
        public int kinematic;
        public int awakeDynamic;
        public int sleepingDynamic;
        public int buildings;
        public int ships;
        public int fragments;
        public int projectiles;
        public int other;
    }

    [Serializable]
    public struct SlowFrame
    {
        public float seconds;
        public int frame;
        public float milliseconds;
        public int precedingCensus;
        public int precedingFrameSurvey;
        public Work precedingFrameWork;
        public SiegeEngineCounters.Sample engine;
    }

    [Serializable]
    public struct StageWork
    {
        public long calls;
        public long inputBlocks;
        public double milliseconds;
    }

    [Serializable]
    public struct Work
    {
        public StageWork connectivity;
        public StageWork bodyRebuild;
        public StageWork refresh;
        public StageWork buildingSupport;
        public StageWork buildingDamage;
        public StageWork buildingGraph;
        public StageWork buildingGround;
        public StageWork buildingTraversal;
        public StageWork buildingDebris;
        public StageWork siegeSimulation;
        public StageWork siegeFleetPhysics;
        public StageWork siegeRefit;
        public StageWork siegeWorkshop;
        public long physicsNeighborQueries;
        public long legacyReferenceScans;
        public static Work Subtract(Work current, Work previous)
        {
            return new Work
            {
                connectivity = Difference(current.connectivity, previous.connectivity),
                bodyRebuild = Difference(current.bodyRebuild, previous.bodyRebuild),
                refresh = Difference(current.refresh, previous.refresh),
                buildingSupport = Difference(current.buildingSupport, previous.buildingSupport),
                buildingDamage = Difference(current.buildingDamage, previous.buildingDamage),
                buildingGraph = Difference(current.buildingGraph, previous.buildingGraph),
                buildingGround = Difference(current.buildingGround, previous.buildingGround),
                buildingTraversal = Difference(current.buildingTraversal, previous.buildingTraversal),
                buildingDebris = Difference(current.buildingDebris, previous.buildingDebris),
                siegeSimulation = Difference(current.siegeSimulation, previous.siegeSimulation),
                siegeFleetPhysics = Difference(current.siegeFleetPhysics, previous.siegeFleetPhysics),
                siegeRefit = Difference(current.siegeRefit, previous.siegeRefit),
                siegeWorkshop = Difference(current.siegeWorkshop, previous.siegeWorkshop),
                physicsNeighborQueries = current.physicsNeighborQueries - previous.physicsNeighborQueries,
                legacyReferenceScans = current.legacyReferenceScans - previous.legacyReferenceScans
            };
        }

        private static StageWork Difference(StageWork current, StageWork previous)
        {
            return new StageWork
            {
                calls = current.calls - previous.calls,
                inputBlocks = current.inputBlocks - previous.inputBlocks,
                milliseconds = current.milliseconds - previous.milliseconds
            };
        }
    }
}

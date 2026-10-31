// 职责：按需累计连接遍历、刚体重建与完整刷新的CPU工作量，供可重复Play采样读取。
// 入口：Measure() / Read() / Reset()；默认关闭，嵌套阶段分别计时，不能相加为整帧耗时。
using System;
using System.Diagnostics;

public static class SimulationWorkMetrics
{
    public enum Stage
    {
        Connectivity,
        BodyRebuild,
        Refresh,
        BuildingSupport,
        BuildingDamage,
        BuildingGraph,
        BuildingGround,
        BuildingTraversal,
        BuildingDebris,
        SiegeSimulation,
        SiegeFleetPhysics,
        SiegeRefit,
        SiegeWorkshop
    }

    [Serializable]
    public struct Counters
    {
        public long calls;
        public long inputBlocks;
        public double milliseconds;
        public double maximumMilliseconds;
    }

    private static readonly Counters[] totals = new Counters[Enum.GetValues(typeof(Stage)).Length];
    public static bool Enabled
    {
        get; set;
    }
    public static long PhysicsNeighborQueries
    {
        get; private set;
    }
    public static long LegacyReferenceScans
    {
        get; private set;
    }

    internal static void RecordPhysicsNeighborQuery()
    {
        if (Enabled)
        {
            PhysicsNeighborQueries++;
        }
    }

    internal static void RecordLegacyReferenceScan()
    {
        if (Enabled)
        {
            LegacyReferenceScans++;
        }
    }

    public static Scope Measure(Stage stage, int inputBlocks = 0)
    {
        return new Scope(stage, inputBlocks, Enabled);
    }

    public static Counters Read(Stage stage)
    {
        return totals[(int)stage];
    }

    public static void Reset()
    {
        Array.Clear(totals, 0, totals.Length);
        PhysicsNeighborQueries = 0;
        LegacyReferenceScans = 0;
    }

    public readonly struct Scope : IDisposable
    {
        private readonly Stage stage;
        private readonly int blocks;
        private readonly long started;
        internal Scope(Stage stage, int blocks, bool enabled)
        {
            this.stage = stage;
            this.blocks = blocks;
            started = enabled ? Stopwatch.GetTimestamp() : 0L;
        }

        public void Dispose()
        {
            if (started == 0L)
            {
                return;
            }

            double elapsed = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            ref Counters value = ref totals[(int)stage];
            value.calls++;
            value.inputBlocks += blocks;
            value.milliseconds += elapsed;
            value.maximumMilliseconds = Math.Max(value.maximumMilliseconds, elapsed);
        }
    }
}

// 职责：显式性能诊断时读取可用的引擎计时/GC计数，记录缺失和缓冲溢出，避免旧值冒充新帧。
// 入口：Read() / Dispose()；只读已完成样本并清空缓冲，跨帧发布延迟不保证与delta严格对齐。
using System;
using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;

public sealed class SiegeEngineCounters : IDisposable
{
    private static readonly string[] Names =
    {
        "Main Thread",
        "Physics.Simulate",
        "Physics.FetchResults",
        "Physics.SyncTransforms",
        "VFX.Update",
        "VFX.ProcessCamera",
        "RenderLoop.Draw",
        "GC.Collect",
        "GC Allocated In Frame"
    };
    private readonly ProfilerRecorder[] recorders = new ProfilerRecorder[Names.Length];
    private readonly List<ProfilerRecorderSample> scratch = new List<ProfilerRecorderSample>(4);
    private bool disposed;
    public readonly List<Counter> Counters = new List<Counter>();
    public SiegeEngineCounters()
    {
        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        for (int index = 0; index < Names.Length; index++)
        {
            Counters.Add(new Counter { name = Names[index] });
        }

        foreach (ProfilerRecorderHandle handle in handles)
        {
            ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
            int index = Array.IndexOf(Names, description.Name);
            if (index < 0)
            {
                continue;
            }

            Counter counter = Counters[index];
            counter.category = description.Category.Name;
            counter.unit = description.UnitType.ToString();
            string expected = index == Names.Length - 1 ? "Bytes" : "TimeNanoseconds";
            if (counter.unit == expected)
            {
                // 保留引擎默认线程覆盖；worker合计不能冒充主线程墙钟或与父阶段混加。
                recorders[index] = new ProfilerRecorder(
                    handle,
                    4,
                    ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.WrapAroundWhenCapacityReached | ProfilerRecorderOptions.SumAllSamplesInFrame);
                counter.available = recorders[index].Valid;
            }
        }
    }

    public Sample Read()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(SiegeEngineCounters));
        }

        return new Sample
        {
            mainThreadNs = ReadCounter(0),
            simulateNs = ReadCounter(1),
            fetchNs = ReadCounter(2),
            syncNs = ReadCounter(3),
            vfxUpdateNs = ReadCounter(4),
            vfxCameraNs = ReadCounter(5),
            renderDrawNs = ReadCounter(6),
            gcCollectNs = ReadCounter(7),
            gcAllocatedBytes = ReadCounter(8)
        };
    }

    private long ReadCounter(int index)
    {
        ref ProfilerRecorder recorder = ref recorders[index];
        if (!recorder.Valid)
        {
            return -1;
        }

        Counter counter = Counters[index];
        if (recorder.WrappedAround)
        {
            counter.wrappedWindows++;
        }

        scratch.Clear();
        recorder.CopyTo(scratch, true);
        if (scratch.Count == 0)
        {
            return -1;
        }

        long value = 0;
        long samples = 0;
        foreach (ProfilerRecorderSample sample in scratch)
        {
            value += sample.Value;
            samples += sample.Count;
        }

        if (samples == 0 && value == 0)
        {
            return -1;
        }

        counter.samples += samples;
        counter.observations++;
        counter.total += value;
        counter.maximum = Math.Max(counter.maximum, value);
        return value;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        foreach (ProfilerRecorder recorder in recorders)
        {
            recorder.Dispose();
        }

        disposed = true;
    }

    [Serializable]
    public sealed class Counter
    {
        public string name;
        public string category;
        public string unit;
        public bool available;
        public long observations;
        public long samples;
        public long total;
        public long maximum;
        public long wrappedWindows;
    }

    [Serializable]
    public struct Sample
    {
        public long mainThreadNs;
        public long simulateNs;
        public long fetchNs;
        public long syncNs;
        public long vfxUpdateNs;
        public long vfxCameraNs;
        public long renderDrawNs;
        public long gcCollectNs;
        public long gcAllocatedBytes;
    }
}

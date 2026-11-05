// 职责：解析 Profiler 捕获数据并输出性能分析结果。
// 入口：AnalyzeLatest() / Analyze() / Add()。
// 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using Unity.Profiling.LowLevel;

public static class ProfilerCaptureAnalysis
{
    private const string CaptureDirectoryName = "ProfilerCaptures";
    private const string ReportRelativePath = "Temp/profiler_analysis_report.txt";
    [MenuItem("Tools/Profiler/Analyze Latest Capture")]
    public static void AnalyzeLatest()
    {
        string capturePath = GetLatestCapturePath();
        string reportPath = Path.Combine(Directory.GetCurrentDirectory(), ReportRelativePath);
        Analyze(capturePath, reportPath);
    }

    public static void Analyze(string capturePath, string reportPath)
    {
        var report = new StringBuilder();
        report.AppendLine("Capture: " + capturePath);
        report.AppendLine("Source: recorded capture; Editor/Player provenance must be supplied by the capture operator.");
        if (string.IsNullOrEmpty(capturePath) || !File.Exists(capturePath))
        {
            report.AppendLine("Loaded: False");
            report.AppendLine("No profiler capture found.");
            WriteReport(reportPath, report);
            Finish("Profiler analysis failed: no capture found.");
            return;
        }

        ProfilerDriver.ClearAllFrames();
        bool loaded = ProfilerDriver.LoadProfile(capturePath, false);
        report.AppendLine("Loaded: " + loaded);
        report.AppendLine("Frame range: " + ProfilerDriver.firstFrameIndex + " .. " + ProfilerDriver.lastFrameIndex);
        if (!loaded || ProfilerDriver.firstFrameIndex > ProfilerDriver.lastFrameIndex)
        {
            WriteReport(reportPath, report);
            Finish("Profiler analysis failed or empty capture.");
            return;
        }

        var frames = new List<FrameSummary>();
        var counters = new Dictionary<string, CounterAggregate>(StringComparer.Ordinal);
        var aggregate = new Dictionary<string, MarkerAggregate>(StringComparer.Ordinal);
        var gcMarkers = new Dictionary<string, MarkerAggregate>(StringComparer.Ordinal);
        var scriptMarkers = new Dictionary<string, MarkerAggregate>(StringComparer.Ordinal);
        var mainThreadNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Main Thread",
            "Unity Main"
        };
        for (int frame = ProfilerDriver.firstFrameIndex; frame <= ProfilerDriver.lastFrameIndex; frame++)
        {
            FrameSummary summary = null;
            int invalidThreadStreak = 0;
            for (int thread = 0; thread < 64; thread++)
            {
                using (var raw = ProfilerDriver.GetRawFrameDataView(frame, thread))
                {
                    if (!raw.valid)
                    {
                        invalidThreadStreak++;
                        if (invalidThreadStreak > 8)
                        {
                            break;
                        }

                        continue;
                    }

                    invalidThreadStreak = 0;
                    bool isMainThread = thread == 0 || mainThreadNames.Contains(raw.threadName);
                    if (summary == null)
                    {
                        summary = new FrameSummary
                        {
                            Frame = frame,
                            Fps = raw.frameFps,
                            CpuMs = raw.frameTimeMs,
                            GpuMs = (ProfilerDriver.GetGpuStatisticsAvailabilityState(frame) & GpuProfilingStatisticsAvailabilityStates.Gathered) != 0 ? raw.frameGpuTimeMs : float.NaN
                        };
                    }

                    if (!isMainThread)
                    {
                        continue;
                    }

                    summary.MainThreadName = raw.threadName;
                    summary.MainSamples = raw.sampleCount;
                    AddRawMarkers(raw, aggregate, gcMarkers, scriptMarkers, summary);
                    AddCounters(raw, counters);
                    break;
                }
            }

            if (summary != null)
            {
                frames.Add(summary);
            }
        }

        report.AppendLine();
        WriteFrameStats(report, frames);
        WriteCounters(report, counters, frames.Count);
        WriteMarkers(report, "Top main-thread markers by inclusive time", aggregate.Values, 25);
        WriteMarkers(report, "GC / allocation related markers", gcMarkers.Values, 25);
        WriteMarkers(report, "Script / behaviour markers", scriptMarkers.Values, 25);
        WriteWorstFrames(report, frames, 12);
        WriteHierarchyDrilldowns(report, frames.OrderByDescending(f => f.CpuMs).Take(6).Select(f => f.Frame));
        WriteReport(reportPath, report);
        Finish("Profiler analysis report written to " + reportPath);
    }

    private static string GetLatestCapturePath()
    {
        string captureDirectory = Path.Combine(Directory.GetCurrentDirectory(), CaptureDirectoryName);
        if (!Directory.Exists(captureDirectory))
        {
            return null;
        }

        return Directory.GetFiles(captureDirectory, "*.data", SearchOption.TopDirectoryOnly).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
    }

    private static void AddRawMarkers(
        RawFrameDataView raw,
        Dictionary<string, MarkerAggregate> aggregate,
        Dictionary<string, MarkerAggregate> gcMarkers,
        Dictionary<string, MarkerAggregate> scriptMarkers,
        FrameSummary summary)
    {
        for (int i = 0; i < raw.sampleCount; i++)
        {
            string name = raw.GetSampleName(i);
            float ms = raw.GetSampleTimeMs(i);
            var target = GetOrCreate(aggregate, name);
            target.Add(ms, raw.frameIndex);
            if (name.IndexOf("GC", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Alloc", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Garbage", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                GetOrCreate(gcMarkers, name).Add(ms, raw.frameIndex);
                summary.GcRelatedMs += ms;
            }

            if (name.IndexOf("Behaviour", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Script", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Update", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("OnGUI", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("PerformanceMonitor", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                GetOrCreate(scriptMarkers, name).Add(ms, raw.frameIndex);
            }

            if (ms > summary.TopMarkerMs)
            {
                summary.TopMarkerMs = ms;
                summary.TopMarker = name;
            }
        }
    }

    private static MarkerAggregate GetOrCreate(Dictionary<string, MarkerAggregate> map, string name)
    {
        if (!map.TryGetValue(name, out var marker))
        {
            marker = new MarkerAggregate
            {
                Name = name
            };
            map.Add(name, marker);
        }

        return marker;
    }

    private static void WriteFrameStats(StringBuilder report, List<FrameSummary> frames)
    {
        report.AppendLine("Frames analyzed: " + frames.Count);
        if (frames.Count == 0)
        {
            return;
        }

        var cpu = frames.Select(f => f.CpuMs).OrderBy(v => v).ToArray();
        var gpu = frames.Select(f => f.GpuMs).Where(v => v > 0).OrderBy(v => v).ToArray();
        report.AppendLine("CPU frame ms avg/p50/p95/max: " + F(cpu.Average()) + " / " + F(Percentile(cpu, 50)) + " / " + F(Percentile(cpu, 95)) + " / " + F(cpu.Last()));
        report.AppendLine("FPS avg/min: " + F(frames.Average(f => f.Fps)) + " / " + F(frames.Min(f => f.Fps)));
        if (gpu.Length > 0)
        {
            report.AppendLine("GPU frame ms avg/p50/p95/max: " + F(gpu.Average()) + " / " + F(Percentile(gpu, 50)) + " / " + F(Percentile(gpu, 95)) + " / " + F(gpu.Last()));
        }
        else
        {
            report.AppendLine("GPU hierarchy frame time: unavailable in this capture (not zero). Frame-timing counters, when present below, are a separate measurement.");
        }
    }

    private static void AddCounters(RawFrameDataView raw, Dictionary<string, CounterAggregate> counters)
    {
        var markers = new List<FrameDataView.MarkerInfo>();
        raw.GetMarkers(markers);
        foreach (var marker in markers)
        {
            if (!raw.HasCounterValue(marker.id) || marker.metadataInfo == null || marker.metadataInfo.Length == 0)
            {
                continue;
            }

            var metadata = marker.metadataInfo[0];
            double value;
            // Counter storage is typed; reinterpret unsigned values rather than reading them as doubles.
            switch (metadata.type)
            {
                case ProfilerMarkerDataType.Int32:
                    value = raw.GetCounterValueAsInt(marker.id);
                    break;
                case ProfilerMarkerDataType.UInt32:
                    value = unchecked((uint)raw.GetCounterValueAsInt(marker.id));
                    break;
                case ProfilerMarkerDataType.Int64:
                    value = raw.GetCounterValueAsLong(marker.id);
                    break;
                case ProfilerMarkerDataType.UInt64:
                    value = unchecked((ulong)raw.GetCounterValueAsLong(marker.id));
                    break;
                case ProfilerMarkerDataType.Float:
                    value = raw.GetCounterValueAsFloat(marker.id);
                    break;
                case ProfilerMarkerDataType.Double:
                    value = raw.GetCounterValueAsDouble(marker.id);
                    break;
                default:
                    continue;
            }

            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                continue;
            }

            string key = marker.category + "/" + marker.name + " [" + metadata.unit + ", " + metadata.type + "]";
            if (!counters.TryGetValue(key, out var counter))
            {
                counter = new CounterAggregate();
                counters.Add(key, counter);
            }

            counter.Count++;
            counter.Sum += value;
            counter.Max = Math.Max(counter.Max, value);
        }
    }

    private static void WriteCounters(StringBuilder report, Dictionary<string, CounterAggregate> counters, int frameCount)
    {
        report.AppendLine();
        report.AppendLine("Captured numeric counters (raw units; averages include zero; missing frames excluded):");
        foreach (var pair in counters.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            CounterAggregate counter = pair.Value;
            report.AppendLine("  " + pair.Key + ": max=" + F(counter.Max) + ", avg=" + F(counter.Sum / counter.Count) + ", available=" + counter.Count + "/" + frameCount);
        }

        if (counters.Count == 0)
        {
            report.AppendLine("  unavailable: no supported numeric counters in the captured main-thread frames.");
        }
    }

    private static void WriteMarkers(StringBuilder report, string title, IEnumerable<MarkerAggregate> markers, int count)
    {
        report.AppendLine();
        report.AppendLine(title + ":");
        foreach (var marker in markers.OrderByDescending(m => m.TotalMs).Take(count))
        {
            report.AppendLine("  " + Pad(F(marker.TotalMs), 10) + " total ms | " + Pad(F(marker.MaxMs), 8) + " max | " + Pad(F(marker.AvgMs), 8) + " avg | " + Pad(marker.Count.ToString(), 7) + " calls | frame " + marker.MaxFrame + " | " + marker.Name);
        }
    }

    private static void WriteWorstFrames(StringBuilder report, List<FrameSummary> frames, int count)
    {
        report.AppendLine();
        report.AppendLine("Worst frames:");
        foreach (var frame in frames.OrderByDescending(f => f.CpuMs).Take(count))
        {
            report.AppendLine("  frame " + frame.Frame + " CPU " + F(frame.CpuMs) + " ms" + " GPU " + (float.IsNaN(frame.GpuMs) ? "unavailable" : F(frame.GpuMs) + " ms") + " FPS " + F(frame.Fps) + " top " + F(frame.TopMarkerMs) + " ms " + frame.TopMarker + " thread " + frame.MainThreadName);
        }
    }

    private static void WriteHierarchyDrilldowns(StringBuilder report, IEnumerable<int> frames)
    {
        report.AppendLine();
        report.AppendLine("Worst-frame hierarchy drilldown:");
        foreach (int frame in frames)
        {
            using (var view = ProfilerDriver.GetHierarchyFrameDataView(frame, 0, HierarchyFrameDataView.ViewModes.Default, HierarchyFrameDataView.columnTotalTime, false))
            {
                report.AppendLine("Frame " + frame + " valid=" + view.valid + " thread=" + view.threadName + " frameMs=" + F(view.frameTimeMs));
                if (!view.valid)
                {
                    continue;
                }

                var children = new List<int>();
                view.GetItemChildren(view.GetRootItemID(), children);
                foreach (int child in children.OrderByDescending(id => view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnTotalTime)).Take(8))
                {
                    WriteHierarchyItem(report, view, child, 1, 3);
                }
            }
        }
    }

    private static void WriteHierarchyItem(StringBuilder report, HierarchyFrameDataView view, int id, int depth, int maxDepth)
    {
        double total = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnTotalTime);
        double self = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnSelfTime);
        double gc = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnGcMemory);
        double calls = view.GetItemColumnDataAsDouble(id, HierarchyFrameDataView.columnCalls);
        report.AppendLine("  " + new string(' ', depth * 2) + Pad(F(total), 9) + " total | " + Pad(F(self), 8) + " self | " + Pad(F(gc), 8) + " GC | " + Pad(F(calls), 6) + " calls | " + view.GetItemName(id));
        if (depth >= maxDepth)
        {
            return;
        }

        var children = new List<int>();
        view.GetItemChildren(id, children);
        foreach (int child in children.OrderByDescending(childId => view.GetItemColumnDataAsDouble(childId, HierarchyFrameDataView.columnTotalTime)).Take(5))
        {
            WriteHierarchyItem(report, view, child, depth + 1, maxDepth);
        }
    }

    private static float Percentile(float[] sorted, int pct)
    {
        if (sorted.Length == 0)
        {
            return 0;
        }

        int index = Mathf.Clamp(Mathf.CeilToInt((pct / 100f) * sorted.Length) - 1, 0, sorted.Length - 1);
        return sorted[index];
    }

    private static void WriteReport(string reportPath, StringBuilder report)
    {
        string directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(reportPath, report.ToString());
    }

    private static void Finish(string message)
    {
        Debug.Log(message);
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static string F(float value)
    {
        return value.ToString("0.###");
    }

    private static string F(double value)
    {
        return value.ToString("0.###");
    }

    private static string Pad(string value, int width)
    {
        return value.PadLeft(width);
    }

    private sealed class MarkerAggregate
    {
        public string Name;
        public int Count;
        public float TotalMs;
        public float MaxMs;
        public int MaxFrame;
        public float AvgMs => Count == 0 ? 0 : TotalMs / Count;

        public void Add(float ms, int frame)
        {
            Count++;
            TotalMs += ms;
            if (ms > MaxMs)
            {
                MaxMs = ms;
                MaxFrame = frame;
            }
        }
    }

    private sealed class FrameSummary
    {
        public int Frame;
        public float Fps;
        public float CpuMs;
        public float GpuMs;
        public string MainThreadName = "";
        public int MainSamples;
        public string TopMarker = "";
        public float TopMarkerMs;
        public float GcRelatedMs;
    }

    private sealed class CounterAggregate
    {
        public int Count;
        public double Sum;
        public double Max = double.NegativeInfinity;
    }
}

// 职责：在同一批舰船、镜头与冻结模拟下对照属性块染色和共享材质绘制成本。
// 入口：Begin()启动有限帧Profiler捕获；Status显示进度，退出/重载时恢复全部临时状态。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[InitializeOnLoad]
public static class SkirmishPerformanceChecks
{
    private static SkirmishMatch match;
    private static MeshRenderer[] renderers;
    private static MaterialPropertyBlock[] saved;
    private static bool[] hadBlock;
    private static IndustrialPartMotion[] motions;
    private static bool recording;
    private static bool matchEnabled;
    private static float scale;
    private static int lastFrame;
    private static int stage;
    private static int frames;
    private static double deadline;
    private static readonly List<float> frameTimes = new List<float>();
    private static readonly List<float> renderTimes = new List<float>();
    private static readonly List<string> results = new List<string>();
    public static string Status { get; private set; } = "Idle";

    static SkirmishPerformanceChecks()
    {
        AssemblyReloadEvents.beforeAssemblyReload += Restore;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Restore();
            }
        };
    }

    public static void Begin()
    {
        if (!Application.isPlaying || match != null)
        {
            throw new InvalidOperationException("Requires arena Play Mode and no active comparison.");
        }

        match = UnityEngine.Object.FindAnyObjectByType<SkirmishMatch>();
        if (match == null)
        {
            throw new InvalidOperationException("Open the 5v5 arena first.");
        }

        scale = Time.timeScale;
        matchEnabled = match.enabled;
        recording = ProfilerDriver.enabled;
        match.enabled = false;
        Time.timeScale = 0f;
        motions = ControlUnit.RuntimeUnits.SelectMany(u => u.GetComponentsInChildren<IndustrialPartMotion>()).Where(m => m.enabled).ToArray();
        foreach (IndustrialPartMotion motion in motions)
        {
            motion.enabled = false;
        }

        renderers = ControlUnit.RuntimeUnits.SelectMany(u => u.GetComponentsInChildren<MeshRenderer>()).ToArray();
        saved = new MaterialPropertyBlock[renderers.Length];
        hadBlock = new bool[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            saved[i] = new MaterialPropertyBlock();
            hadBlock[i] = renderers[i].HasPropertyBlock();
            renderers[i].GetPropertyBlock(saved[i]);
            var properties = new MaterialPropertyBlock();
            renderers[i].GetPropertyBlock(properties);
            Color color = match.TeamColor(renderers[i].GetComponentInParent<ControlUnit>().faction == UnitFaction.Player);
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
            renderers[i].SetPropertyBlock(properties);
        }

        results.Clear();
        results.Add("Same fleet/camera; frozen simulation and industrial motion. Editor CPU frames, not Player FPS.");
        results.Add("MeshRenderers=" + renderers.Length);
        stage = 0;
        deadline = EditorApplication.timeSinceStartup + 90f;
        StartWindow();
        EditorApplication.update += Tick;
    }

    private static void StartWindow()
    {
        frames = 0;
        frameTimes.Clear();
        renderTimes.Clear();
        ProfilerDriver.enabled = false;
        ProfilerDriver.ClearAllFrames();
        lastFrame = -1;
        ProfilerDriver.enabled = true;
        Status = stage == 0 ? "Capturing property blocks" : "Capturing shared materials";
    }

    private static void Tick()
    {
        try
        {
            if (!Application.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Profiler comparison interrupted or timed out.");
            }

            int frame = ProfilerDriver.lastFrameIndex;
            if (frame <= lastFrame)
            {
                return;
            }

            lastFrame = frame;
            using (var raw = ProfilerDriver.GetRawFrameDataView(frame, 0))
            {
                if (!raw.valid)
                {
                    return;
                }

                frames++;
                if (frames > 5)
                {
                    frameTimes.Add(raw.frameTimeMs);
                    float render = 0f;
                    for (int i = 0; i < raw.sampleCount; i++)
                    {
                        if (raw.GetSampleName(i) == "Inl_RenderCameraStack")
                        {
                            render += raw.GetSampleTimeMs(i);
                        }
                    }

                    renderTimes.Add(render);
                }
            }

            if (frames < 20)
            {
                return;
            }

            frameTimes.Sort();
            results.Add((stage == 0 ? "PropertyBlock" : "SharedMaterial") + ": samples=" + frameTimes.Count + ", medianFrameMs=" + frameTimes[frameTimes.Count / 2].ToString("F3") + ", meanRenderMs=" + renderTimes.Average().ToString("F3"));
            if (stage++ == 0)
            {
                RestoreProperties();
                StartWindow();
                return;
            }

            Status = string.Join("\n", results);
            Directory.CreateDirectory("Temp/Random5v5Arena");
            File.WriteAllText("Temp/Random5v5Arena/same-fleet-performance.txt", Status);
            Restore();
        }
        catch (Exception exception)
        {
            Status = exception.ToString();
            Restore();
        }
    }

    private static void RestoreProperties()
    {
        for (int i = 0; renderers != null && i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].SetPropertyBlock(hadBlock[i] ? saved[i] : null);
            }
        }
    }

    private static void Restore()
    {
        EditorApplication.update -= Tick;
        if (renderers == null)
        {
            return;
        }

        ProfilerDriver.enabled = recording;
        RestoreProperties();
        foreach (IndustrialPartMotion motion in motions)
        {
            if (motion != null)
            {
                motion.enabled = true;
            }
        }

        if (match != null)
        {
            match.enabled = matchEnabled;
        }

        Time.timeScale = scale;
        match = null;
        renderers = null;
        saved = null;
        hadBlock = null;
        motions = null;
    }
}

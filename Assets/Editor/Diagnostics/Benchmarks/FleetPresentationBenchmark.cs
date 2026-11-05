// 职责：采集固定舰队的CPU整帧原始样本；记录当前绘制配置，不在运行中切换渲染系统。
// 入口：StartCapture() / Status / Finish()；Play部署阶段，结果写Temp/FleetPresentation。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class FleetPresentationBenchmark
{
    private static bool originalRecording;
    private static int previousFrame;
    private static int observed;
    private static double started;
    private static string output;
    private static Report report;
    public static string Status => report == null ? "Idle" : $"Observations {observed}";

    static FleetPresentationBenchmark()
    {
        AssemblyReloadEvents.beforeAssemblyReload += OnReload;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    public static string CreateFixedFleet()
    {
        if (!Application.isPlaying || PlayManager.instance.playMode || UnityEngine.Object.FindObjectsByType<ControlUnit>().Length != 1)
        {
            throw new InvalidOperationException("Requires fresh Siege deployment with only its base.");
        }

        EditorApplication.delayCall += SpawnFleet;
        return "Queued eight fixed ships; exit Play to remove fixture.";
    }

    private static void SpawnFleet()
    {
        var session = UnityEngine.Object.FindAnyObjectByType<SiegeSession>();
        var option = System.Linq.Enumerable.First(session.DefenderOptions, value => value.DisplayName == "CODEX_01_Bastion");
        for (int i = 0; i < 8; i++)
        {
            ControlUnit unit = ModularUnitFactory.Spawn(
                JsonUtility.FromJson<BlockDataList>(option.BlueprintJson),
                "Performance owned fleet " + i,
                new Vector3((i % 4 - 1.5f) * 28f, 8f, 40f + (i / 4) * 40f),
                Quaternion.identity,
                UnitFaction.Player);
            unit.GetComponent<Rigidbody>().isKinematic = true;
        }

        Camera camera = Camera.main;
        camera.GetComponent<CameraController>().enabled = false;
        camera.transform.SetPositionAndRotation(new Vector3(0, 95, -90), Quaternion.Euler(32, 0, 0));
    }

    public static string StartCapture(string label = "fixed-fleet")
    {
        if (!Application.isPlaying || EditorApplication.isPaused || PlayManager.instance == null || PlayManager.instance.playMode)
        {
            throw new InvalidOperationException("Requires unpaused Play deployment with a fixed fleet/camera.");
        }

        if (report != null)
        {
            throw new InvalidOperationException("Comparison is already active.");
        }

        originalRecording = ProfilerDriver.enabled;
        report = new Report
        {
            utc = DateTime.UtcNow.ToString("O"),
            unity = Application.unityVersion,
            gpu = SystemInfo.graphicsDeviceName,
            units = UnityEngine.Object.FindObjectsByType<ControlUnit>().Length,
            motions = UnityEngine.Object.FindObjectsByType<IndustrialPartMotion>().Length,
            gpuDrawing = (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset)?.gpuResidentDrawerMode == GPUResidentDrawerMode.InstancedDrawing,
            initialized = IsInitialized()
        };
        string safe = string.Join("_", label.Split(Path.GetInvalidFileNameChars()));
        output = "Temp/FleetPresentation/" + safe + ".json";
        observed = 0;
        started = EditorApplication.timeSinceStartup;
        ProfilerDriver.ClearAllFrames();
        ProfilerDriver.enabled = true;
        previousFrame = ProfilerDriver.lastFrameIndex;
        EditorApplication.update += Tick;
        return Status;
    }

    private static void Tick()
    {
        try
        {
            if (!Application.isPlaying || EditorApplication.timeSinceStartup - started > 150)
            {
                Finish("Interrupted or timed out");
                return;
            }

            int current = ProfilerDriver.lastFrameIndex;
            if (current <= previousFrame)
            {
                return;
            }

            previousFrame = current;
            using var raw = ProfilerDriver.GetRawFrameDataView(current, 0);
            if (!raw.valid)
            {
                return;
            }

            observed++;
            if (observed > 40)
            {
                report.cpuMs.Add(raw.frameTimeMs);
            }

            if (observed >= 340)
            {
                Finish("Completed");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Finish("Exception: " + exception.Message);
        }
    }

    public static string Finish(string reason = "Manual stop")
    {
        EditorApplication.update -= Tick;
        if (report == null)
        {
            return "Idle";
        }

        report.reason = reason;
        Directory.CreateDirectory("Temp/FleetPresentation");
        File.WriteAllText(output, JsonUtility.ToJson(report, true));
        report = null;
        ProfilerDriver.enabled = originalRecording;
        return output;
    }

    private static void OnReload()
    {
        Finish("Assembly reload");
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Finish("Play stopped");
        }
    }

    private static bool IsInitialized()
    {
        return (bool)typeof(GPUResidentDrawer).GetMethod("IsInitialized", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
    }

    [Serializable]
    private sealed class Report
    {
        public string utc;
        public string unity;
        public string gpu;
        public int units;
        public int motions;
        public bool gpuDrawing;
        public bool initialized;
        public string reason;
        public List<double> cpuMs = new List<double>();
    }
}

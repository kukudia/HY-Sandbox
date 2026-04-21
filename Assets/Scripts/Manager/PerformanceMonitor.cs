using UnityEngine;
using System.Diagnostics;
using Unity.Profiling;

public class PerformanceMonitor : MonoBehaviour
{
    // GUI 样式 - 缓存避免重复创建
    private GUIStyle style;
    private Rect guiBoxRect;
    private Rect[] guiLabelRects;
    
    // 帧率计算变量
    private int frameCount = 0;
    private float elapsedTime = 0f;
    private float currentFPS = 0f;
    
    // CPU 使用率变量
    private Process currentProcess;
    private double lastCpuTime;
    private float currentCpuUsage = 0f;
    private int processorCount;
    
    // GPU 相关变量
    private float currentGpuFrameTime = 0f;
    private float gpuUsageEstimate = 0f;
    
    // 刷新间隔
    public float updateInterval = 0.5f;
    public float targetFrameRate = 60f;
    
    // FrameTimingManager 相关
    private FrameTiming[] frameTimings = new FrameTiming[1];
    
    // 字符串缓存，减少 GC
    private string fpsText = "";
    private string cpuText = "";
    private string gpuTimeText = "";
    private string gpuUseText = "";

    void Start()
    {
        // 初始化 GUI 样式（只创建一次）
        style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        style.padding = new RectOffset(10, 10, 10, 10);
        
        // 预计算 GUI 矩形区域
        guiBoxRect = new Rect(10, 10, 300, 130);
        guiLabelRects = new Rect[4];
        guiLabelRects[0] = new Rect(20, 20, 280, 30);
        guiLabelRects[1] = new Rect(20, 50, 280, 30);
        guiLabelRects[2] = new Rect(20, 80, 280, 30);
        guiLabelRects[3] = new Rect(20, 110, 280, 30);

        // 初始化进程对象
        currentProcess = Process.GetCurrentProcess();
        lastCpuTime = currentProcess.TotalProcessorTime.TotalMilliseconds;
        processorCount = SystemInfo.processorCount;

        if (targetFrameRate <= 0) targetFrameRate = 60f;
    }

    void Update()
    {
        frameCount++;
        elapsedTime += Time.unscaledDeltaTime;
        
        if (elapsedTime >= updateInterval)
        {
            currentFPS = frameCount / elapsedTime;
            frameCount = 0;
            elapsedTime = 0f;

            UpdateCpuUsage();
            UpdateGpuFrameTime();

            float frameTimeBudgetMs = 1000f / targetFrameRate;
            gpuUsageEstimate = Mathf.Clamp01(currentGpuFrameTime / frameTimeBudgetMs) * 100f;
            
            // 更新缓存的文本（使用 StringBuilder 或直接格式化）
            fpsText = $"FPS: {currentFPS:0.0}";
            cpuText = $"CPU: {currentCpuUsage:0.0}%";
            gpuTimeText = $"GPU Time: {currentGpuFrameTime:0.00} ms";
            gpuUseText = $"GPU Use (Est.): {gpuUsageEstimate:0.0}%";
        }
    }

    void UpdateCpuUsage()
    {
        double newCpuTime = currentProcess.TotalProcessorTime.TotalMilliseconds;
        double cpuTimeDelta = newCpuTime - lastCpuTime;
        float intervalMs = updateInterval * 1000f;

        currentCpuUsage = (float)((cpuTimeDelta / intervalMs) * 100f / processorCount);
        lastCpuTime = newCpuTime;
    }

    void UpdateGpuFrameTime()
    {
        FrameTimingManager.CaptureFrameTimings();
        uint framesRetrieved = FrameTimingManager.GetLatestTimings(1, frameTimings);

        if (framesRetrieved > 0)
        {
            currentGpuFrameTime = (float)frameTimings[0].gpuFrameTime;
        }
        else
        {
            currentGpuFrameTime = 0f;
        }
    }

    void OnGUI()
    {
        GUI.Box(guiBoxRect, "");
        GUI.Label(guiLabelRects[0], fpsText, style);
        GUI.Label(guiLabelRects[1], cpuText, style);
        GUI.Label(guiLabelRects[2], gpuTimeText, style);
        GUI.Label(guiLabelRects[3], gpuUseText, style);
    }
}

// 职责：在冷主菜单持续观察真实帧、内存和错误，报告仅写Temp，不加载工坊或存档。
// 入口：Run(seconds) / Stop()；静态Editor回调保留观察状态，正常停止不冒充达到时长。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class MainMenuIdleChecks
{
    private const string Output = "Temp/MainMenu/idle-checks.txt";
    private static readonly List<string> samples = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static MainMenuController menu;
    private static double started;
    private static double nextSample;
    private static double duration;
    private static int initialFrame;
    private static int previousFrame;
    public static string Status { get; private set; } = "idle";

    public static void Run(float seconds)
    {
        if (Status == "running" || !EditorApplication.isPlaying || seconds < 10f)
        {
            throw new InvalidOperationException("Requires a new menu Play observation of at least ten seconds.");
        }

        menu = Object.FindAnyObjectByType<MainMenuController>();
        if (menu == null || menu.Busy || !menu.MenuVisible || SceneManager.sceneCount != 1)
        {
            throw new InvalidOperationException("Requires the cold idle main menu with one scene.");
        }

        samples.Clear();
        errors.Clear();
        duration = seconds;
        started = EditorApplication.timeSinceStartup;
        nextSample = started;
        initialFrame = previousFrame = Time.frameCount;
        Status = "running";
        Application.logMessageReceived += Capture;
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += Stop;
    }

    public static void Stop()
    {
        if (Status == "running")
        {
            Finish("stopped");
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            Finish("stopped");
            return;
        }

        if (errors.Count > 0)
        {
            Finish("failed");
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        if (now < nextSample)
        {
            return;
        }

        nextSample = now + 5;
        if (menu == null || menu.Busy || !menu.MenuVisible || SceneManager.sceneCount != 1 || GameManager.instance != null)
        {
            errors.Add("Idle menu ownership changed during observation.");
        }

        if (now - started > 5 && Time.frameCount == previousFrame)
        {
            errors.Add("No game frames advanced between samples.");
        }

        previousFrame = Time.frameCount;
        samples.Add(string.Format(
            CultureInfo.InvariantCulture,
            "{0:o} elapsed={1:F2} frames={2} allocated={3} reserved={4} errors={5}",
            DateTimeOffset.Now,
            now - started,
            Time.frameCount - initialFrame,
            UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),
            UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong(),
            errors.Count));
        if (errors.Count > 0 || now - started >= duration)
        {
            Finish(errors.Count > 0 ? "failed" : "passed");
        }
        else
        {
            Write();
        }
    }

    private static void Capture(string message, string trace, LogType type)
    {
        if (errors.Count < 16 && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
        {
            errors.Add(message + "\n" + trace);
        }
    }

    private static void Finish(string result)
    {
        Application.logMessageReceived -= Capture;
        EditorApplication.update -= Tick;
        AssemblyReloadEvents.beforeAssemblyReload -= Stop;
        Status = result;
        Write();
        menu = null;
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static void Write()
    {
        Directory.CreateDirectory("Temp/MainMenu");
        using (var writer = new StreamWriter(Output))
        {
            writer.WriteLine(Status);
            writer.WriteLine("plannedSeconds=" + duration.ToString("F2", CultureInfo.InvariantCulture));
            foreach (string sample in samples)
            {
                writer.WriteLine(sample);
            }

            foreach (string error in errors)
            {
                writer.WriteLine(error);
            }
        }
    }
}

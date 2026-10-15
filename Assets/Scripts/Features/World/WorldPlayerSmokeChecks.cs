// 职责：仅在显式-world-smoke-report参数下自动验证真实Player的加载、游玩、区块移动和退出。
// 入口：命令行初始化；只替换临时运行时载具，不保存游戏数据，报告后正常退出进程。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class WorldPlayerSmokeChecks : MonoBehaviour
{
    [Serializable]
    private sealed class Phase
    {
        public string name;
        public int frames;
        public float medianMs;
        public float p95Ms;
        public float maxMs;
        public float totalMs;
    }

    [Serializable]
    private sealed class Report
    {
        public string status = "running";
        public string graphics;
        public int seed;
        public int buildings;
        public int blocks;
        public Vector3 travelStart;
        public Vector3 travelEnd;
        public long memoryBytes;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
        public List<Phase> phases = new List<Phase>();
    }

    private readonly Report report = new Report();
    private readonly List<float> samples = new List<float>();
    private string reportPath;
    private string phase = "startup";
    private IEnumerator scenario;
    private float previous;
    private float deadline;
    private bool finished;
    private bool captureImages;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-world-smoke-report");
        if (index < 0 || index + 1 >= args.Length)
        {
            return;
        }

        var root = new GameObject("Explicit Player smoke checks");
        DontDestroyOnLoad(root);
        WorldPlayerSmokeChecks checks = root.AddComponent<WorldPlayerSmokeChecks>();
        checks.reportPath = args[index + 1];
        checks.captureImages = Array.IndexOf(args, "-world-smoke-capture") >= 0;
    }

    private void Start()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
        report.graphics = SystemInfo.graphicsDeviceType.ToString();
        previous = Time.realtimeSinceStartup;
        deadline = previous + 240f;
        Application.logMessageReceived += CaptureError;
        scenario = Scenario();
        Save();
    }

    private void Update()
    {
        if (finished)
        {
            return;
        }

        float now = Time.realtimeSinceStartup;
        samples.Add((now - previous) * 1000f);
        previous = now;
        try
        {
            if (now > deadline)
            {
                throw new TimeoutException("Player timed out in " + phase);
            }

            if (!scenario.MoveNext())
            {
                Finish();
            }
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
            Finish();
        }
    }

    private IEnumerator Scenario()
    {
        yield return null;
        WorldSessionManager session = Object.FindFirstObjectByType<WorldSessionManager>();
        Check(session != null, "Main starts with world session");
        bool capturedStartup = false;
        while (BuildManager.instance.IsLoadingBlocks)
        {
            WorldLoadingPanel startupPanel = Object.FindFirstObjectByType<WorldLoadingPanel>();
            Check(
                startupPanel.GetComponent<CanvasGroup>().alpha >= .99f,
                "startup mask stays opaque during construct restoration");
            Check(
                BuildManager.instance.BlueprintLoadProgress >= 0f && BuildManager.instance.BlueprintLoadProgress <= 1f,
                "construct load exposes bounded progress");
            if (captureImages && !capturedStartup && BuildManager.instance.BlueprintLoadProgress >= .1f)
            {
                capturedStartup = true;
                ScreenCapture.CaptureScreenshot(Path.ChangeExtension(reportPath, "startup.png"));
            }

            yield return null;
        }

        report.seed = session.SessionSeed;
        GameManager.instance.blocksParent.gameObject.SetActive(false);
        var player = new GameObject("Player smoke construct");
        Rigidbody body = player.AddComponent<Rigidbody>();
        body.isKinematic = true;
        player.AddComponent<ControlUnit>();
        Object.Instantiate(Resources.Load<GameObject>("Blocks/Cockpit"), player.transform);
        foreach (Durability health in player.GetComponentsInChildren<Durability>())
        {
            health.maxDurability = health.currentDurability = 100000000f;
        }

        GameManager.instance.blocksParent = player.transform;
        PlayManager.instance.blocksParent = player.transform;
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        spawner.spawnOnPlayStart = false;
        spawner.spawnInterval = float.MaxValue;
        Next("loading-and-entry");
        MainUIPanels.instance.PlayStart();
        bool sawMask = false;
        float minimumProgress = session.Progress;
        while (session.State != WorldSessionState.Playing || session.IsEntering)
        {
            Check(session.State != WorldSessionState.Failed, "world generation remains valid");
            WorldLoadingPanel panel = Object.FindFirstObjectByType<WorldLoadingPanel>();
            if (!sawMask && panel.GetComponent<CanvasGroup>().alpha >= .99f)
            {
                sawMask = true;
                if (captureImages)
                {
                    ScreenCapture.CaptureScreenshot(Path.ChangeExtension(reportPath, "loading.png"));
                }
            }

            yield return null;
        }

        body.isKinematic = true;
        Check(sawMask, "opaque loading mask renders while waiting or enabling");
        Check(session.Progress >= minimumProgress, "loading reaches completion");
        Check(session.World.LoadedColumns.Count >= 81, "initial 81 terrain columns loaded");
        Check(session.Buildings.Buildings.Count > 0, "Player creates buildings from blueprint library");
        Check(session.BuildingTemplates.Count > 0, "Player loads external blueprint library");
        report.buildings = session.Buildings.Buildings.Count;
        report.blocks = session.Buildings.Buildings.Sum(building => building.Blocks.Count);
        Check(
            Physics.Raycast(player.transform.position + Vector3.up * 2f, Vector3.down, 100f),
            "spawn has real terrain collision");
        Next("idle");
        for (int i = 0; i < 360; i++)
        {
            Check(session.State == WorldSessionState.Playing, "session stays playing");
            yield return null;
        }

        Next("travel");
        if (captureImages)
        {
            ScreenCapture.CaptureScreenshot(Path.ChangeExtension(reportPath, "world.png"));
        }

        report.travelStart = player.transform.position;
        Vector3 destination = player.transform.position + Vector3.right * 200f;
        for (int i = 0; i < 420; i++)
        {
            Vector3 position = player.transform.position;
            player.transform.position = new Vector3(Mathf.Min(destination.x, position.x + .6f), position.y, position.z);
            yield return null;
        }

        report.travelEnd = player.transform.position;
        Check(
            session.World.ColumnAt(player.transform.position).x >= 4,
            "Player crosses multiple chunk boundaries");
        Check(
            session.World.HasColumn(session.World.ColumnAt(player.transform.position)),
            "travel retains terrain under player");
        Check(session.World.LoadedColumns.Count <= 121, "loaded terrain remains bounded");
        report.memoryBytes = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        Next("exit");
        // 使用正式模式退出，跳过返航货仓保存入口，避免测试写入玩家存档。
        MainUIPanels.instance.CompletePlayEnd();
        yield return null;
        Check(session.State == WorldSessionState.Preparing, "exit discards world and prepares fresh session");
        Next("reprepare");
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        Check(session.State == WorldSessionState.Ready, "second preparation completes");
        Next("done");
    }

    private void Check(bool valid, string name)
    {
        if (!valid)
        {
            throw new InvalidOperationException(name);
        }

        if (!report.checks.Contains(name))
        {
            report.checks.Add(name);
        }
    }

    private void Next(string name)
    {
        if (samples.Count > 0)
        {
            float[] sorted = samples.OrderBy(value => value).ToArray();
            report.phases.Add(new Phase { name = phase, frames = sorted.Length, medianMs = sorted[sorted.Length / 2], p95Ms = sorted[(int)((sorted.Length - 1) * .95f)], maxMs = sorted[sorted.Length - 1], totalMs = samples.Sum() });
        }

        samples.Clear();
        phase = name;
        Save();
    }

    private void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            report.errors.Add(message + "\n" + stack);
        }
    }

    private void Save()
    {
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
    }

    private void Finish()
    {
        finished = true;
        Next("finished");
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Save();
        Application.logMessageReceived -= CaptureError;
        Application.Quit(report.errors.Count == 0 ? 0 : 1);
    }
}

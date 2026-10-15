// 职责：在真实 Main Play Mode 采样固定种子的准备、切换、静止和跨区块帧耗时。
// 入口：Run(label)；只使用运行时配置副本和测试载具，不保存玩家数据。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WorldSessionPerformanceChecks
{
    private const string Pending = "HY.WorldPerformance";
    private static IEnumerator scenario;
    private static readonly Stopwatch clock = new Stopwatch();
    private static readonly List<double> samples = new List<double>();
    private static readonly List<string> results = new List<string>();
    private static int frame;
    private static double previous;
    private static double deadline;
    private static string phase;
    static WorldSessionPerformanceChecks()
    {
        EditorApplication.playModeStateChanged += OnState;
    }

    public static void Run(string label)
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Run from saved Main in Edit Mode.");
        }

        SessionState.SetString(Pending, label);
        EditorApplication.isPlaying = true;
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode || string.IsNullOrEmpty(SessionState.GetString(Pending, "")))
        {
            return;
        }

        results.Clear();
        results.Add($"Unity={Application.unityVersion}, graphics={SystemInfo.graphicsDeviceType}, scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}, seed=14721; Editor frame intervals, no Deep Profile.");
        if (SessionState.GetString(Pending, "").Contains("profile"))
        {
            UnityEditorInternal.ProfilerDriver.profileEditor = false;
            UnityEditorInternal.ProfilerDriver.SetAreaEnabled(UnityEngine.Profiling.ProfilerArea.CPU, true);
            UnityEditorInternal.ProfilerDriver.enabled = true;
        }

        samples.Clear();
        clock.Restart();
        previous = 0;
        frame = -1;
        phase = "startup";
        deadline = EditorApplication.timeSinceStartup + 300;
        scenario = Scenario();
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        EditorApplication.QueuePlayerLoopUpdate();
        if (frame == Time.frameCount || !EditorApplication.isPlaying)
        {
            return;
        }

        frame = Time.frameCount;
        double now = clock.Elapsed.TotalMilliseconds;
        samples.Add(now - previous);
        previous = now;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Performance probe timed out in " + phase);
            }

            if (scenario.MoveNext())
            {
                return;
            }
        }
        catch (Exception error)
        {
            results.Add(error.ToString());
        }

        EditorApplication.update -= Tick;
        if (SessionState.GetString(Pending, "").Contains("profile"))
        {
            UnityEditorInternal.ProfilerDriver.enabled = false;
        }

        Directory.CreateDirectory("Temp/WorldSession");
        File.WriteAllLines("Temp/WorldSession/performance-" + SessionState.GetString(Pending, "run") + ".txt", results);
        SessionState.EraseString(Pending);
        scenario = null;
        EditorApplication.isPlaying = false;
    }

    private static void Next(string name)
    {
        if (samples.Count > 0)
        {
            double[] sorted = samples.OrderBy(value => value).ToArray();
            results.Add($"{phase}: frames={samples.Count}, totalMs={samples.Sum():F1}, medianMs={sorted[sorted.Length / 2]:F2}, p95Ms={sorted[(int)((sorted.Length - 1) * .95)]:F2}, maxMs={sorted[sorted.Length - 1]:F2}, over50ms={samples.Count(value => value > 50)}");
        }

        samples.Clear();
        if (phase == "idle" && SessionState.GetString(Pending, "").Contains("profile"))
        {
            WriteCpuSamples();
            previous = clock.Elapsed.TotalMilliseconds;
        }

        phase = name;
    }

    private static void WriteCpuSamples()
    {
        var totals = new Dictionary<string, double>();
        int last = UnityEditorInternal.ProfilerDriver.lastFrameIndex;
        int first = Math.Max(UnityEditorInternal.ProfilerDriver.firstFrameIndex, last - 99);
        for (int index = first; index <= last; index++)
        {
            using (var raw = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(index, 0))
            {
                if (!raw.valid)
                {
                    continue;
                }

                for (int i = 0; i < raw.sampleCount; i++)
                {
                    string name = raw.GetSampleName(i);
                    totals.TryGetValue(name, out double total);
                    totals[name] = total + raw.GetSampleTimeMs(i);
                }
            }
        }

        results.Add("Idle CPU inclusive marker average, last 100 frames:");
        results.AddRange(totals.OrderByDescending(entry => entry.Value).Take(35).Select(entry => $"{entry.Key}: {entry.Value / (last - first + 1):F3}ms"));
    }

    private static IEnumerator Scenario()
    {
        yield return null;
        WorldSessionManager session = Object.FindFirstObjectByType<WorldSessionManager>();
        var configuration = Object.Instantiate(session.Settings);
        var data = new SerializedObject(configuration);
        data.FindProperty("randomizeSeed").boolValue = false;
        data.FindProperty("seed").intValue = 14721;
        data.ApplyModifiedPropertiesWithoutUndo();
        var sessionData = new SerializedObject(session);
        sessionData.FindProperty("settings").objectReferenceValue = configuration;
        sessionData.ApplyModifiedPropertiesWithoutUndo();
        Next("prepare");
        session.BeginPreparation();
        while (session.State == WorldSessionState.Preparing || BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        results.Add($"ready: state={session.State}, buildings={session.Buildings.Buildings.Count}, blocks={session.Buildings.Buildings.Sum(building => building.Blocks.Count)}");
        Next("entry");
        GameManager.instance.blocksParent.gameObject.SetActive(false);
        var player = new GameObject("Performance probe construct");
        player.AddComponent<Rigidbody>().isKinematic = true;
        player.AddComponent<ControlUnit>();
        Object.Instantiate(Resources.Load<GameObject>("Blocks/Cockpit"), player.transform);
        foreach (Durability health in player.GetComponentsInChildren<Durability>())
        {
            health.maxDurability = health.currentDurability = 100000000f;
        }

        GameManager.instance.blocksParent = player.transform;
        PlayManager.instance.blocksParent = player.transform;
        double start = clock.Elapsed.TotalMilliseconds;
        bool production = SessionState.GetString(Pending, "").Contains("production");
        if (production)
        {
            EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
            spawner.spawnOnPlayStart = false;
            spawner.spawnInterval = float.MaxValue;
            MainUIPanels.instance.PlayStart();
        }
        else
        {
            session.EnterPlay(player.transform);
        }

        results.Add($"Play entry synchronousMs={clock.Elapsed.TotalMilliseconds - start:F2}");
        player.GetComponent<Rigidbody>().isKinematic = true;
        while (session.IsEntering)
        {
            yield return null;
        }

        results.Add($"rendering: batches={UnityStats.batches}, triangles={UnityStats.triangles}, shadowCasters={UnityStats.shadowCasters}, screen={UnityStats.screenRes}");
        for (int i = 0; i < 120; i++)
        {
            yield return null;
        }

        results.Add($"settled rendering: batches={UnityStats.batches}, triangles={UnityStats.triangles}, shadowCasters={UnityStats.shadowCasters}, screen={UnityStats.screenRes}");
        Next("idle");
        double idleEnd = clock.Elapsed.TotalSeconds + 3;
        for (int i = 0; i < 240 || clock.Elapsed.TotalSeconds < idleEnd; i++)
        {
            if (session.State != WorldSessionState.Playing)
            {
                throw new InvalidOperationException("The performance fixture left play unexpectedly.");
            }

            yield return null;
        }

        Next("travel");
        for (int i = 0; i < 300; i++)
        {
            if (session.State != WorldSessionState.Playing)
            {
                throw new InvalidOperationException("The travel fixture left play unexpectedly.");
            }

            player.transform.position += Vector3.right * .8f;
            yield return null;
        }

        Next("exit");
        session.EndSession();
        yield return null;
        Next("done");
    }
}

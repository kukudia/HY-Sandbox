// 职责：仅显式-siege-checks-report参数启动独立Player验证，记录真实帧、玩法契约与自然多波结果。
// 入口：Initialize()；smoke使用明确损伤刺激验证闭环，soak只自然战斗/整备，不把两种结果混用。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class SiegePlayerChecks : MonoBehaviour
{
    private string reportPath;
    private bool soak;
    private float duration = 300f;
    private int shipCount = 2;
    private bool capture;
    private IEnumerator work;
    private bool finished;
    private float started;
    private bool profileWork;
    private bool driving;
    private IEnumerator drivingWork;
    private SiegePlayerSurvey survey;
    private SiegeSession activeSession;
    private readonly Report report = new Report();
    private readonly List<float> frameTimes = new List<float>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-siege-checks-report");
        if (index < 0 || index + 1 >= args.Length || Object.FindFirstObjectByType<SiegePlayerChecks>() != null)
        {
            return;
        }

        var root = new GameObject("Explicit Siege Player checks");
        DontDestroyOnLoad(root);
        SiegePlayerChecks checks = root.AddComponent<SiegePlayerChecks>();
        checks.reportPath = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(Path.GetDirectoryName(checks.reportPath));
        checks.soak = Array.IndexOf(args, "-siege-checks-soak") >= 0;
        checks.capture = Array.IndexOf(args, "-siege-checks-capture") >= 0;
        checks.profileWork = Array.IndexOf(args, "-siege-checks-profile") >= 0;
        checks.driving = Array.IndexOf(args, "-siege-checks-driving") >= 0;
        int seconds = Array.IndexOf(args, "-siege-checks-seconds");
        if (seconds >= 0 && seconds + 1 < args.Length && float.TryParse(args[seconds + 1], out float parsedSeconds))
        {
            checks.duration = Mathf.Clamp(parsedSeconds, 30f, 1800f);
        }

        int ships = Array.IndexOf(args, "-siege-checks-ships");
        if (ships >= 0 && ships + 1 < args.Length && int.TryParse(args[ships + 1], out int parsedShips))
        {
            checks.shipCount = Mathf.Clamp(parsedShips, 2, 6);
        }

        checks.report.mode = checks.driving ? "scripted-driving" : checks.soak ? "natural-soak" : "scripted-smoke";
        checks.report.utc = DateTime.UtcNow.ToString("O");
        checks.report.development = Debug.isDebugBuild;
        checks.report.profilerEnabledAtStartup = UnityEngine.Profiling.Profiler.enabled;
        checks.report.graphics = SystemInfo.graphicsDeviceType.ToString();
        checks.report.unity = Application.unityVersion;
        checks.report.width = Screen.width;
        checks.report.height = Screen.height;
        checks.started = Time.realtimeSinceStartup;
        Application.logMessageReceived += checks.CaptureError;
        checks.work = checks.Run();
    }

    private void Update()
    {
        if (finished || work == null)
        {
            return;
        }

        try
        {
            if (!work.MoveNext())
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

    private void LateUpdate()
    {
        if (finished || report.phase != "natural-combat")
        {
            return;
        }

        frameTimes.Add(Time.unscaledDeltaTime * 1000f);
        if (survey == null)
        {
            survey = new SiegePlayerSurvey(started, profileWork);
            report.census = survey.CensusSamples;
            report.survey = survey.Result;
        }

        survey.Observe(activeSession, Time.unscaledDeltaTime * 1000f);
    }

    private void OnDestroy()
    {
        (drivingWork as IDisposable)?.Dispose();
        drivingWork = null;
        survey?.Dispose();
        Application.logMessageReceived -= CaptureError;
    }

    private IEnumerator Run()
    {
        report.phase = "startup";
        yield return null;
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        activeSession = session;
        Check(
            "Siege scene and session started in Player",
            session?.Match != null && session.Match.Phase == SiegePhase.Deployment);
        SiegeStressPopulation population = Object.FindFirstObjectByType<SiegeStressPopulation>();
        if (population != null)
        {
            report.phase = "population-loading";
            float loadingDeadline = Time.realtimeSinceStartup + 90f;
            while (!population.IsReady && string.IsNullOrEmpty(population.Error) && Time.realtimeSinceStartup < loadingDeadline)
            {
                yield return null;
            }

            Check(
                "mixed stress population restored within existing building budget",
                population.IsReady && string.IsNullOrEmpty(population.Error));
            report.additionalBuildings = population.Buildings.Count;
            report.additionalBuildingBlocks = population.ConfiguredBlocks;
            report.populationLoadSeconds = population.LoadSeconds;
        }

        report.profile = session.Settings.name;
        report.initialCoins = session.Match.Coins;
        report.requestedShips = shipCount;
        report.configuredBaseBlocks = session.Fleet.Base.Blueprint.blocks.Count;
        Check(
            "large building base has independent core and no cockpit",
            report.configuredBaseBlocks > 0 && session.Fleet.Base.Building.Blocks.Count == report.configuredBaseBlocks && session.Fleet.Base.Unit.Building != null && !session.Fleet.Base.Unit.HasAnyCockpit && session.Fleet.Base.IsAlive);
        float readyAt = Time.realtimeSinceStartup + 0.4f;
        while (Time.realtimeSinceStartup < readyAt)
        {
            yield return null;
        }

        Canvas.ForceUpdateCanvases();
        foreach (Graphic graphic in Object.FindFirstObjectByType<SiegeHud>().GetComponentsInChildren<Graphic>())
        {
            var corners = new Vector3[4];
            graphic.rectTransform.GetWorldCorners(corners);
            Check(
                "HUD bounds: " + graphic.name,
                corners.All(point => point.x >= -1f && point.x <= Screen.width + 1f && point.y >= -1f && point.y <= Screen.height + 1f));
            if (graphic is Text label && !string.IsNullOrWhiteSpace(label.text))
            {
                Check("HUD text geometry: " + label.name, label.cachedTextGenerator.vertexCount > 4);
            }
        }

        Durability core = session.Fleet.Base.Core.GetComponent<Durability>();
        core.currentDurability -= 25f;
        long price = session.Fleet.Base.Quote();
        long coins = session.Match.Coins;
        Check(
            "Player base repair restores health and quoted debit",
            price > 0 && session.TryRepair(true) && Mathf.Approximately(core.currentDurability, core.maxDurability) && session.Match.Coins == coins - price);
        for (int index = 0; index < shipCount; index++)
        {
            Check("deploy ship " + index, session.TryDeploy(index % session.Settings.Ships.Length, out _));
        }

        report.deployedShips = session.Fleet.Ships.Count(ship => ship != null && ship.IsAlive);
        Check("requested fleet deployed exactly", report.deployedShips == shipCount);
        Check("switch player to second ship", session.TryTakeControl(1));
        Check(
            "one manual driver and remaining ships autonomous",
            session.Fleet.Ships.Count(ship => ship != null && ship.IsAlive && ship.Unit.AcceptsPlayerInput) == 1 && session.Fleet.Ships.Where(ship => ship != null && ship.IsAlive && ship != session.Fleet.Driven).All(ship => ship.Cockpit.ControlMode == UnitControlMode.Autonomous));
        if (capture)
        {
            readyAt = Time.realtimeSinceStartup + 0.3f;
            while (Time.realtimeSinceStartup < readyAt)
            {
                yield return null;
            }

            ScreenCapture.CaptureScreenshot(ImagePath("deployment"));
            yield return null;
        }

        Check("begin first wave", session.BeginNextWave());
        int firstWaveAttackers = session.Match.PendingAttackers + session.Match.ActiveAttackers;
        Check(
            "combat keeps building anchored and rejects repair/deploy",
            session.Fleet.Base.Building.Anchored && !session.TryRepair(true) && !session.TryDeploy(0, out _));
        if (driving)
        {
            Check("driving probe is independent from natural soak", !soak);
            report.phase = "driving-input";
            report.driving = new SiegeDrivingPlayerChecks.Report();
            drivingWork = SiegeDrivingPlayerChecks.Run(session, Check, report.driving);
            while (drivingWork.MoveNext())
            {
                yield return drivingWork.Current;
            }

            (drivingWork as IDisposable)?.Dispose();
            drivingWork = null;
            Check("virtual input devices and background behavior restored", report.driving.devicesRestored);
            if (capture)
            {
                ScreenCapture.CaptureScreenshot(ImagePath("driving"));
                yield return null;
            }
        }

        if (soak)
        {
            report.phase = "natural-combat";
            float end = Time.realtimeSinceStartup + duration;
            int lastIntermission = 0;
            while (Time.realtimeSinceStartup < end && session.Match.Phase != SiegePhase.Defeat)
            {
                if (session.Match.Phase == SiegePhase.Intermission && session.Match.Wave != lastIntermission)
                {
                    lastIntermission = session.Match.Wave;
                    report.completedWaves++;
                    bool baseRepaired = session.TryRepair(true);
                    report.actions.Add("wave " + lastIntermission + " completed; base repair=" + baseRepaired + "; " + session.Status);
                    bool shipRepaired = session.TryRepair(false);
                    report.actions.Add("pilot ship repair=" + shipRepaired + "; " + session.Status);
                    if (capture)
                    {
                        readyAt = Time.realtimeSinceStartup + 0.3f;
                        while (Time.realtimeSinceStartup < readyAt)
                        {
                            yield return null;
                        }

                        ScreenCapture.CaptureScreenshot(ImagePath("intermission-" + lastIntermission));
                    }

                    yield return null;
                    Check("start next natural wave " + (lastIntermission + 1), session.BeginNextWave());
                }

                yield return null;
            }

            report.outcome = session.Match.Phase == SiegePhase.Defeat ? "base-defeated" : "duration-reached";
            report.finalWave = session.Match.Wave;
            report.finalBaseBlocks = session.Fleet.Base.Building.Blocks.Count;
            report.finalCoins = session.Match.Coins;
            if (capture)
            {
                ScreenCapture.CaptureScreenshot(ImagePath("final"));
            }

            yield return null;
            yield break;
        }

        report.phase = "scripted-combat";
        float deadline = Time.realtimeSinceStartup + 20f;
        while (session.Match.Phase == SiegePhase.Battle && Time.realtimeSinceStartup < deadline)
        {
            if (session.Match.PendingAttackers == 0)
            {
                // 明确刺激真实死亡链，只验证波次/奖励，不作为自然交火成绩。
                foreach (Cockpit enemy in Object.FindObjectsByType<Cockpit>(FindObjectsSortMode.None).Where(cockpit => cockpit.faction == UnitFaction.Enemy).ToArray())
                {
                    enemy.GetComponent<Durability>().ApplyDamage(1000000f, session.Fleet.Driven.Unit);
                }
            }

            yield return null;
        }

        Check(
            "real enemy deaths resolve wave and award coins",
            session.Match.Phase == SiegePhase.Intermission && session.Match.DefeatedAttackers == firstWaveAttackers);
        Check("second wave starts from intermission", session.BeginNextWave());
        Cockpit driven = session.Fleet.Driven.Cockpit;
        driven.GetComponent<Durability>().ApplyDamage(1000000f, null);
        Check(
            "pilot loss transfers authority without ending match",
            session.Match.Phase == SiegePhase.Battle && session.Fleet.Driven != null && session.Fleet.Driven.Cockpit != driven);
        if (driving)
        {
            Check(
                "pilot death rebinds camera to replacement ship",
                CameraController.instance.playerBody == session.Fleet.Driven.Unit.transform && session.Fleet.Driven.Unit.AcceptsPlayerInput);
        }

        session.Fleet.Base.Core.canExplode = false;
        core.ApplyDamage(1000000f, null);
        Check(
            "real core destruction ends Player match and reinforcements",
            session.Match.Phase == SiegePhase.Defeat && session.Match.PendingAttackers == 0 && !session.BeginNextWave() && !session.TryRepair(true));
        report.phase = "restart";
        if (capture)
        {
            readyAt = Time.realtimeSinceStartup + 0.3f;
            while (Time.realtimeSinceStartup < readyAt)
            {
                yield return null;
            }

            ScreenCapture.CaptureScreenshot(ImagePath("defeat"));
        }

        yield return null;
        SceneManager.LoadScene(session.gameObject.scene.name);
        yield return null;
        SiegeSession restarted = Object.FindFirstObjectByType<SiegeSession>();
        if (report.additionalBuildings > 0)
        {
            SiegeStressPopulation restoredPopulation = Object.FindFirstObjectByType<SiegeStressPopulation>();
            float populationDeadline = Time.realtimeSinceStartup + 90f;
            while (restoredPopulation != null && !restoredPopulation.IsReady && string.IsNullOrEmpty(restoredPopulation.Error) && Time.realtimeSinceStartup < populationDeadline)
            {
                yield return null;
            }

            Check(
                "mixed scene restart restores all satellite buildings without orphaned roots",
                restoredPopulation != null && restoredPopulation.IsReady && restoredPopulation.Buildings.Count == report.additionalBuildings && restarted.Buildings.Buildings.Sum(building => building.Blocks.Count) == report.configuredBaseBlocks + report.additionalBuildingBlocks && Object.FindObjectsByType<BuildingRuntime>(FindObjectsSortMode.None).Length == report.additionalBuildings + 1);
        }

        Check(
            "scene restart restores pristine building and empty fleet",
            restarted != null && restarted != session && restarted.Match.Phase == SiegePhase.Deployment && restarted.Fleet.Base.Building.Blocks.Count == report.configuredBaseBlocks && restarted.Fleet.Ships.All(ship => ship == null));
        report.outcome = "scripted-loop-passed";
    }

    private string ImagePath(string suffix)
    {
        return Path.Combine(
            Path.GetDirectoryName(reportPath),
            Path.GetFileNameWithoutExtension(reportPath) + "-" + suffix + ".png");
    }

    private void Check(string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Siege Player check failed: " + name);
        }

        report.checks.Add(name);
    }

    private void CaptureError(string condition, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            report.errors.Add(condition + "\n" + trace);
        }
    }

    private void Finish()
    {
        finished = true;
        (drivingWork as IDisposable)?.Dispose();
        drivingWork = null;
        survey?.Dispose();
        Application.logMessageReceived -= CaptureError;
        report.elapsedSeconds = Time.realtimeSinceStartup - started;
        frameTimes.Sort();
        report.frames = frameTimes.Count;
        report.medianMs = Percentile(0.5f);
        report.p95Ms = Percentile(0.95f);
        report.p99Ms = Percentile(0.99f);
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
        File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
        Application.Quit(report.errors.Count == 0 ? 0 : 1);
    }

    private float Percentile(float fraction)
    {
        return frameTimes.Count == 0 ? 0f : frameTimes[Mathf.Clamp(Mathf.CeilToInt(frameTimes.Count * fraction) - 1, 0, frameTimes.Count - 1)];
    }

    [Serializable]
    private sealed class Report
    {
        public int additionalBuildings;
        public int additionalBuildingBlocks;
        public float populationLoadSeconds;
        public SiegeDrivingPlayerChecks.Report driving;
        public string methodology = "Explicit opt-in standalone Player. Scripted smoke uses forced real damage; natural soak only automates intermission repair and next-wave buttons. Once-per-second scene census perturbs timing. Frame deltas include rendering/scheduling, not isolated CPU or GPU time. Soak includes intermission and maintenance spikes. No comparison across unlike battles.";
        public string status = "running";
        public string mode;
        public string profile;
        public long initialCoins;
        public int requestedShips;
        public int configuredBaseBlocks;
        public int deployedShips;
        public string phase;
        public string utc;
        public string unity;
        public string graphics;
        public bool development;
        public bool profilerEnabledAtStartup;
        public string outcome;
        public int width;
        public int height;
        public int frames;
        public float medianMs;
        public float p95Ms;
        public float p99Ms;
        public float elapsedSeconds;
        public int completedWaves;
        public int finalWave;
        public int finalBaseBlocks;
        public long finalCoins;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
        public List<string> actions = new List<string>();
        public SiegePlayerSurvey.Summary survey;
        public List<SiegePlayerSurvey.Census> census = new List<SiegePlayerSurvey.Census>();
    }
}

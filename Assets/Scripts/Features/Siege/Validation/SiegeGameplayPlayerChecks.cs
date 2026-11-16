// 职责：仅显式命令行参数启用新版菜单到Siege的独立Player闭环和自然战斗观察，不写玩家蓝图。
// 入口：Initialize()；smoke明确施加真实伤害，soak只观察自然战斗；两类结果分开报告。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class SiegeGameplayPlayerChecks : MonoBehaviour
{
    [Serializable]
    private sealed class Sample
    {
        public float seconds;
        public int wave;
        public string phase;
        public int enemyCockpits;
        public int units;
        public int blocks;
        public long memoryBytes;
        public long coins;
        public int recycled;
    }

    [Serializable]
    private sealed class Report
    {
        public string status = "running";
        public string mode;
        public string phase;
        public string outcome;
        public string unity;
        public string graphics;
        public int width;
        public int height;
        public bool roof;
        public int completedWaves;
        public float elapsed;
        public float medianMs;
        public float p95Ms;
        public float p99Ms;
        public int frames;
        public List<string> checks = new List<string>();
        public List<string> actions = new List<string>();
        public List<string> errors = new List<string>();
        public List<Sample> samples = new List<Sample>();
    }

    private readonly Report report = new Report();
    private readonly List<float> frameTimes = new List<float>();
    private string output;
    private bool soak;
    private bool roof;
    private int exampleIndex;
    private bool finished;
    private float seconds = 180f;
    private float started;
    private float nextSample;
    private float deadline;
    private IEnumerator scenario;
    private SiegeGameplayStage stage;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-siege-gameplay-report");
        if (index < 0 || index + 1 >= args.Length || Object.FindAnyObjectByType<SiegeGameplayPlayerChecks>() != null)
        {
            return;
        }

        var root = new GameObject("Explicit menu Siege Player checks");
        DontDestroyOnLoad(root);
        var checks = root.AddComponent<SiegeGameplayPlayerChecks>();
        checks.output = Path.GetFullPath(args[index + 1]);
        Directory.CreateDirectory(Path.GetDirectoryName(checks.output));
        checks.soak = Array.IndexOf(args, "-siege-gameplay-soak") >= 0;
        checks.roof = Array.IndexOf(args, "-siege-gameplay-roof") >= 0;
        int exampleArgument = Array.IndexOf(args, "-siege-gameplay-example");
        if (exampleArgument >= 0 && exampleArgument + 1 < args.Length && int.TryParse(args[exampleArgument + 1], out int exampleIndex))
        {
            checks.exampleIndex = Mathf.Clamp(exampleIndex, 0, 2);
        }

        int duration = Array.IndexOf(args, "-siege-gameplay-seconds");
        if (duration >= 0 && duration + 1 < args.Length && float.TryParse(args[duration + 1], out float parsed))
        {
            checks.seconds = Mathf.Clamp(parsed, 30f, 900f);
        }

        checks.started = Time.realtimeSinceStartup;
        checks.deadline = checks.started + checks.seconds + 300f;
        checks.report.mode = checks.soak ? "natural-observation" : "scripted-real-damage-smoke";
        checks.report.roof = checks.roof;
        checks.report.unity = Application.unityVersion;
        checks.report.graphics = SystemInfo.graphicsDeviceType.ToString();
        checks.report.width = Screen.width;
        checks.report.height = Screen.height;
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
        Application.logMessageReceived += checks.CaptureError;
        checks.scenario = checks.Run();
    }

    private void Update()
    {
        if (finished || scenario == null)
        {
            return;
        }

        try
        {
            if (Time.realtimeSinceStartup > deadline)
            {
                throw new TimeoutException("Menu Siege Player check exceeded its deadline in " + report.phase);
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

    private void LateUpdate()
    {
        if (finished || stage == null || stage.Match == null)
        {
            return;
        }

        if (report.phase == "combat")
        {
            frameTimes.Add(Time.unscaledDeltaTime * 1000f);
        }

        if (Time.realtimeSinceStartup < nextSample)
        {
            return;
        }

        nextSample = Time.realtimeSinceStartup + 1f;
        report.samples.Add(new Sample { seconds = Time.realtimeSinceStartup - started, wave = stage.Match.Wave, phase = stage.Match.Phase.ToString(), enemyCockpits = Enemies().Length, units = ControlUnit.RuntimeUnits.Count(unit => unit != null && unit.gameObject.scene == stage.gameObject.scene), blocks = stage.Base?.Building != null ? stage.Base.Building.Blocks.Count : 0, memoryBytes = Profiler.GetTotalAllocatedMemoryLong(), coins = stage.Match.Coins, recycled = stage.Cleanup != null ? stage.Cleanup.RetiredGroups : 0 });
        Save();
    }

    private IEnumerator Run()
    {
        report.phase = "menu";
        yield return null;
        MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
        Check(menu != null && menu.MenuVisible, "real standalone main menu is visible");
        if (roof)
        {
            SiegeConstructionSettings settings = Get<SiegeConstructionSettings>(menu, "siegeSettings");
            BlockDataList source = JsonUtility.FromJson<BlockDataList>(settings.Starter.text);
            foreach (BlockData block in source.blocks.Where(HangarRules.IsHangar))
            {
                block.hangarEntrance = HangarEntrance.Roof;
            }

            // An explicit validation process mutates only its loaded asset in memory.
            // No editor assets or personal base files are rewritten.
            TextAsset roofExample = new TextAsset(JsonUtility.ToJson(source));
            Set(settings, "starter", roofExample);
            if (settings.Examples.Count > 0)
            {
                settings.Examples[0].blueprint = roofExample;
            }
        }

        menu.Choose(MenuDestination.Siege);
        menu.SelectBlueprint(exampleIndex);
        Check(menu.Selected?.IsExample == true, "menu selects official example without creating personal files");
        menu.LaunchSelected();
        while (menu.Busy)
        {
            yield return null;
        }

        stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
        Check(
            stage != null && stage.Base?.IsAlive == true,
            "menu deploys the real additive Siege stage: " + menu.Status);
        Check(Object.FindAnyObjectByType<SiegeSession>() == null, "prototype session stays unloaded");
        Check(
            stage.Island != null && stage.Island.PlayableBounds.size.x > 0f && stage.Cleanup != null,
            "authored floating island and cleanup presentation are available");
        int expectedHangars = stage.Base.Blueprint.blocks.Count(HangarRules.IsHangar);
        Check(
            stage.Fleet.UsesHangars && stage.Fleet.DeploymentCount == expectedHangars,
            "selected official example exposes all its actual modular hangars");
        Check(
            stage.Base.Blueprint.siegeCoreId == JsonUtility.FromJson<BlockDataList>(menu.Selected.Example.text).siegeCoreId,
            "menu-selected example reaches the production stage without falling back to Starter");
        bool entrancesMatch = true;
        for (int slot = 0; slot < expectedHangars; slot++)
        {
            HangarModule actual = stage.Fleet.GetHangar(slot);
            string identity = actual.GetComponent<Block>().uniqueId;
            BlockData intended = stage.Base.Blueprint.blocks.Find(block => identity == block.id || identity.EndsWith(":" + block.id, StringComparison.Ordinal));
            entrancesMatch &= intended != null && actual.Entrance == intended.hangarEntrance;
        }

        Check(entrancesMatch, "every deployed hangar retains its identity-matched physical entrance direction");
        Check(
            stage.Base.Building.HasFullFoundationSupport(),
            "deployed modular base has actual foundation support");
        Check(
            stage.Base.Building.Blocks.Count == stage.Base.Blueprint.blocks.Count,
            "expanded shell count matches real restored building");
        Capture("deployment");
        yield return null;
        TextAsset finch = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
        BlockDataList ship = JsonUtility.FromJson<BlockDataList>(finch.text);
        var option = new SiegeShipOption(
            "Player fixture Finch",
            finch.text,
            ship.blocks.Count * stage.Settings.DeploymentCoinsPerBlock,
            ship.blocks.Count);
        // Isolate personal catalog contents while retaining production deployment, payment,
        // power, flight, enemy spawning and physical death consumers.
        Set(stage, "defenders", new[] { option });
        Set(stage, "attackers", new[] { option });
        Check(stage.Fleet.FitsHangar(0, ship), "real default hangar has Finch clearance");
        long coins = stage.Match.Coins;
        bool deployed = stage.TryDeployAt(0, 0, out string reason);
        Check(deployed, "production fleet purchases Finch: " + reason);
        SiegeFleetEntry entry = stage.Fleet.Ships[0];
        Check(
            stage.Match.Coins == coins - option.Price && entry.IsAlive,
            "deployment debits the actual quote exactly once");
        Check(stage.BeginWave(), "production stage starts wave and guidance");
        report.phase = "combat";
        Save();
        float end = Time.realtimeSinceStartup + seconds;
        int previousIntermission = 0;
        float intermissionAt = -1f;
        bool launchObserved = false;
        bool enemyObserved = false;
        while (Time.realtimeSinceStartup < end && stage.Match.Phase != SiegePhase.Defeat)
        {
            if (!launchObserved && stage.FlightGuidance.IsComplete && stage.Match.Phase == SiegePhase.Battle)
            {
                Check(
                    stage.FlightGuidance.CompletedCount == 1 && stage.FlightGuidance.FailedCount == 0 && entry.IsAlive,
                    "real powered ship completes launch in standalone Player");
                launchObserved = true;
                Capture("launched");
            }

            Cockpit[] enemies = Enemies();
            enemyObserved |= enemies.Length > 0;
            if (!soak && launchObserved && stage.Match.Phase == SiegePhase.Battle)
            {
                foreach (Cockpit enemy in enemies)
                {
                    report.actions.Add("scripted real cockpit damage at " + (Time.realtimeSinceStartup - started).ToString("F2"));
                    enemy.GetComponent<Durability>().ApplyDamage(1000000f, entry.Unit);
                }
            }

            if (stage.Match.Phase == SiegePhase.Intermission && stage.Match.Wave != previousIntermission)
            {
                previousIntermission = stage.Match.Wave;
                report.completedWaves++;
                intermissionAt = Time.realtimeSinceStartup;
                Check(
                    stage.Match.PendingCoins == 0,
                    "wave " + previousIntermission + " pending income settles after return");
                Capture("intermission-" + previousIntermission);
                if (!soak)
                {
                    Check(
                        entry.IsAlive && entry.Unit.GetComponent<Rigidbody>().isKinematic,
                        "scripted wave recovers its actual ship into parked hangar");
                    Check(
                        stage.Cleanup.IsActive || stage.Cleanup.RetiredGroups > 0,
                        "return or intermission owns the real cleanup sweep");
                }
            }

            if (intermissionAt >= 0f && Time.realtimeSinceStartup - intermissionAt >= 12f)
            {
                if (!soak)
                {
                    break;
                }

                Check(stage.BeginWave(), "natural observation starts the next wave after visible cleanup");
                intermissionAt = -1f;
            }

            yield return null;
        }

        Check(enemyObserved, "production wave spawns real enemy cockpits");
        if (!soak)
        {
            Check(
                launchObserved && report.completedWaves == 1 && stage.Match.Phase == SiegePhase.Intermission,
                "scripted damage closes one real spawn/death/return/settlement loop");
        }

        report.outcome = stage.Match.Phase == SiegePhase.Defeat ? "natural-base-defeated" : soak ? "natural-duration-reached" : "scripted-loop-passed";
        Capture("final");
        report.phase = "return";
        GameModeController modes = PlayManager.instance.Modes;
        modes.ReturnToBuild();
        while (modes.State == GameModeState.Returning || modes.State == GameModeState.Siege)
        {
            yield return null;
        }

        Check(
            modes.State == GameModeState.Building && Object.FindAnyObjectByType<SiegeGameplayStage>() == null,
            "return unloads additive stage and restores real workshop");
        BuildHotbar hotbar = Object.FindAnyObjectByType<BuildHotbar>();
        Check(
            hotbar != null && hotbar.Catalog != null && hotbar.GetComponentsInChildren<BuildHotbarSlot>(true).Length == 8,
            "returned standalone workshop retains eight authored hotbar slots");
        menu.ReturnToMenu();
        while (menu.Busy)
        {
            yield return null;
        }

        Check(menu.MenuVisible, "standalone flow returns to the actual main menu");
        Capture("menu-return");
        yield return null;
        yield return null;
        report.phase = "complete";
    }

    private Cockpit[] Enemies()
    {
        return stage == null ? Array.Empty<Cockpit>() : Object.FindObjectsByType<Cockpit>().Where(cockpit => cockpit.faction == UnitFaction.Enemy && cockpit.gameObject.scene == stage.gameObject.scene).ToArray();
    }

    private static T Get<T>(object owner, string field)
    {
        return (T)owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
    }

    private static void Set(object owner, string field, object value)
    {
        owner.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(owner, value);
    }

    private void Capture(string label)
    {
        ScreenCapture.CaptureScreenshot(Path.Combine(Path.GetDirectoryName(output), Path.GetFileNameWithoutExtension(output) + "-" + label + ".png"));
    }

    private void Check(bool success, string name)
    {
        if (!success)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
        Save();
    }

    private void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert || message.Contains("Physics.ClosestPoint"))
        {
            if (!report.errors.Contains(message + "\n" + stack))
            {
                report.errors.Add(message + "\n" + stack);
            }
        }
    }

    private void Save()
    {
        report.elapsed = Time.realtimeSinceStartup - started;
        File.WriteAllText(output, JsonUtility.ToJson(report, true));
    }

    private void Finish()
    {
        finished = true;
        Application.logMessageReceived -= CaptureError;
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        frameTimes.Sort();
        report.frames = frameTimes.Count;
        if (frameTimes.Count > 0)
        {
            report.medianMs = frameTimes[frameTimes.Count / 2];
            report.p95Ms = frameTimes[Mathf.Min(frameTimes.Count - 1, Mathf.FloorToInt(frameTimes.Count * .95f))];
            report.p99Ms = frameTimes[Mathf.Min(frameTimes.Count - 1, Mathf.FloorToInt(frameTimes.Count * .99f))];
        }

        Save();
        Application.Quit(report.errors.Count == 0 ? 0 : 1);
    }
}

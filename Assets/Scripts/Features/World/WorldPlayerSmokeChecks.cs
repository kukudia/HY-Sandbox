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
        public float p99Ms;
        public int over50Ms;
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
        public int requestedCycles = 1;
        public int completedCycles;
        public List<long> cycleMemoryBytes = new List<long>();
        public List<int> cycleSeeds = new List<int>();
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
    private bool validateEnemies;
    private bool validateCombat;
    private string flightBlueprint;
    private int cycleCount = 1;
    private int cycle;
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
        checks.validateEnemies = Array.IndexOf(args, "-world-smoke-enemies") >= 0;
        checks.validateCombat = Array.IndexOf(args, "-world-smoke-combat") >= 0;
        try
        {
            int seedIndex = Array.IndexOf(args, "-world-smoke-seed");
            if (seedIndex >= 0)
            {
                int seed = ReadInteger(args, seedIndex, "seed", int.MinValue, int.MaxValue);
                WorldSessionManager session = Object.FindFirstObjectByType<WorldSessionManager>();
                if (session == null)
                {
                    throw new InvalidOperationException("Fixed-seed validation requires Main world session.");
                }

                // AfterSceneLoad runs before scene Start; only this opt-in Player process mutates the loaded settings.
                JsonUtility.FromJsonOverwrite("{\"randomizeSeed\":false,\"seed\":" + seed + "}", session.Settings);
            }

            int flightIndex = Array.IndexOf(args, "-world-smoke-enemy-flight");
            if (flightIndex >= 0)
            {
                if (flightIndex + 1 >= args.Length || string.IsNullOrWhiteSpace(args[flightIndex + 1]) || args[flightIndex + 1].StartsWith("-"))
                {
                    throw new ArgumentException("Enemy flight requires an existing blueprint name.");
                }

                checks.flightBlueprint = args[flightIndex + 1];
            }

            int cyclesIndex = Array.IndexOf(args, "-world-smoke-cycles");
            if (cyclesIndex >= 0)
            {
                checks.cycleCount = ReadInteger(args, cyclesIndex, "cycles", 1, 10);
            }
        }
        catch (Exception error)
        {
            checks.report.errors.Add(error.Message);
        }

        checks.report.requestedCycles = checks.cycleCount;
    }

    private static int ReadInteger(string[] args, int index, string name, int minimum, int maximum)
    {
        if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out int value) || value < minimum || value > maximum)
        {
            throw new ArgumentException("Invalid world smoke " + name + " argument.");
        }

        return value;
    }

    private void Start()
    {
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
        report.graphics = SystemInfo.graphicsDeviceType.ToString();
        previous = Time.realtimeSinceStartup;
        deadline = previous + 240f * cycleCount;
        Application.logMessageReceived += CaptureError;
        scenario = Scenario();
        Save();
        if (report.errors.Count > 0)
        {
            Finish();
        }
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

        for (cycle = 0; cycle < cycleCount; cycle++)
        {
            report.seed = session.SessionSeed;
            report.cycleSeeds.Add(session.SessionSeed);
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
            if (validateEnemies)
            {
                Next("enemy-blueprint");
                IEnumerator enemies = ValidateEnemyBlueprints(spawner, player.transform.position);
                try
                {
                    while (enemies.MoveNext())
                    {
                        yield return enemies.Current;
                    }
                }
                finally
                {
                    (enemies as IDisposable)?.Dispose();
                }
            }

            if (validateCombat)
            {
                Next("combat-recovery");
                IEnumerator combat = WorldCombatPlayerChecks.Run(player.GetComponent<ControlUnit>(), spawner, Check);
                try
                {
                    while (combat.MoveNext())
                    {
                        yield return combat.Current;
                    }
                }
                finally
                {
                    (combat as IDisposable)?.Dispose();
                }

                // The general streaming route below uses a kinematic host again.
                body.isKinematic = true;
            }

            if (flightBlueprint != null)
            {
                Next("natural-enemy-flight");
                IEnumerator flight = EnemyFlightPlayerChecks.Run(player.GetComponent<ControlUnit>(), spawner, flightBlueprint, Check);
                try
                {
                    while (flight.MoveNext())
                    {
                        yield return flight.Current;
                    }
                }
                finally
                {
                    (flight as IDisposable)?.Dispose();
                }
            }

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
            report.cycleMemoryBytes.Add(report.memoryBytes);
            Next("exit");
            // 使用正式模式退出，跳过返航货仓保存入口，避免测试写入玩家存档。
            MainUIPanels.instance.CompletePlayEnd();
            yield return null;
            Check(session.State == WorldSessionState.Preparing, "exit discards world and prepares fresh session");
            Next("reprepare");
            while (session.State == WorldSessionState.Preparing || BuildManager.instance.IsLoadingBlocks)
            {
                yield return null;
            }

            Check(session.State == WorldSessionState.Ready, "second preparation completes");
            report.completedCycles++;
        }

        Next("done");
    }

    private static BlockDataList EnemyFixture()
    {
        var data = new BlockDataList();
        foreach (string resource in new[]
        {
            "Cockpit",
            "1x1x1"
        }

        )
        {
            var block = new BlockData(Resources.Load<GameObject>("Blocks/" + resource).GetComponent<Block>())
            {
                id = "enemy-probe-" + resource,
                resourcePath = "Blocks/" + resource,
                posX = resource == "Cockpit" ? 0f : 3.5f,
                posY = 0f,
                posZ = 0f,
                rotX = 0f,
                rotY = 0f,
                rotZ = 0f,
                rotW = 1f
            };
            if (resource != "Cockpit")
            {
                block.x = 3;
                block.y = 2;
                block.z = 1;
            }

            data.blocks.Add(block);
        }

        return data;
    }

    private IEnumerator ValidateEnemyBlueprints(EnemySpawner spawner, Vector3 origin)
    {
        BlockDataList fixture = EnemyFixture();
        ControlUnit enemy = spawner.SpawnBlockData(
            fixture,
            "Explicit enemy validation",
            origin + new Vector3(25f, 15f, 0f),
            Quaternion.Euler(0f, 90f, 0f));
        Check(enemy != null, "valid enemy blueprint spawns a complete unit");
        try
        {
            enemy.GetComponent<Rigidbody>().isKinematic = true;
            yield return null;
            Block resized = enemy.GetComponentsInChildren<Block>().First(block => block.resourcePath == "Blocks/1x1x1");
            Check(
                resized.x == 3 && resized.y == 2 && resized.z == 1 && Vector3.Distance(resized.GetComponent<BoxCollider>().size, new Vector3(2.9f, 1.9f, 0.9f)) < .001f,
                "enemy instance dimensions rebuild its real collider after Awake");
            Check(
                Mathf.Approximately(resized.mass, 6f) && Mathf.Approximately(
                    enemy.GetComponent<Rigidbody>().mass,
                    enemy.GetComponentsInChildren<Block>().Sum(block => block.mass)),
                "enemy instance volume updates block and aggregate rigidbody mass");
            Check(
                Vector3.Distance(resized.transform.localPosition, new Vector3(3.5f, 0f, 0f)) < .001f && enemy.faction == UnitFaction.Enemy && enemy.cockpit.faction == UnitFaction.Enemy,
                "enemy restore preserves local pose and enemy ownership");
        }
        finally
        {
            Object.Destroy(enemy.gameObject);
        }

        yield return null;
        int activeUnits = ControlUnit.RuntimeUnits.Count;
        fixture.blocks[1].resourcePath = "Blocks/MissingExplicitValidationModule";
        ControlUnit incomplete = spawner.SpawnBlockData(fixture, "Missing module validation", origin + Vector3.up * 15f, Quaternion.identity);
        if (incomplete != null)
        {
            Object.Destroy(incomplete.gameObject);
        }

        Check(
            incomplete == null && ControlUnit.RuntimeUnits.Count == activeUnits,
            "missing enemy resource rejects the entire blueprint without publishing a partial unit");
        fixture = EnemyFixture();
        fixture.blocks[1] = null;
        Check(
            spawner.SpawnBlockData(fixture, "Null module validation", origin, Quaternion.identity) == null,
            "null enemy module rejects blueprint without throwing");
        fixture = EnemyFixture();
        fixture.blocks.RemoveAt(0);
        Check(
            spawner.SpawnBlockData(fixture, "No cockpit validation", origin, Quaternion.identity) == null,
            "enemy blueprint without a cockpit rejects before instantiation");
        fixture = EnemyFixture();
        fixture.blocks.Add(fixture.blocks[0]);
        Check(
            spawner.SpawnBlockData(fixture, "Duplicate cockpit validation", origin, Quaternion.identity) == null && ControlUnit.RuntimeUnits.Count == activeUnits,
            "enemy blueprint with duplicate cockpits cannot publish a partial unit");
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
            report.phases.Add(new Phase { name = phase, frames = sorted.Length, medianMs = sorted[sorted.Length / 2], p95Ms = sorted[(int)((sorted.Length - 1) * .95f)], p99Ms = sorted[(int)((sorted.Length - 1) * .99f)], over50Ms = samples.Count(value => value > 50f), maxMs = sorted[sorted.Length - 1], totalMs = samples.Sum() });
        }

        samples.Clear();
        phase = cycleCount > 1 ? "cycle-" + (cycle + 1) + "/" + name : name;
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

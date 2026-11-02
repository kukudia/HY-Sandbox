// 职责：用运行时配置副本验证建筑预算延期、高速区块等待及脱锚建筑休眠恢复。
// 入口：Run()；只写Temp报告，不保存场景、配置资产或玩家存档，结束后退出Play Mode。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WorldSessionEdgeChecks
{
    private const string Pending = "HY.WorldSessionEdgeChecks";
    public const string ReportPath = "Temp/WorldSession/edge-checks.json";
    private static IEnumerator scenario;
    private static Report report;
    private static double deadline;
    private static WorldSessionManager session;
    private static WorldGenerationSettings originalSettings;
    private static WorldGenerationSettings profile;
    private static GameObject testPlayer;
    private static Transform originalGameParent;
    private static Transform originalPlayParent;
    private static bool originalParentActive;
    private static float originalTimeScale;
    private static SimulationMode originalSimulationMode;
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> skipped = new List<string>();
        public List<string> errors = new List<string>();
    }

    static WorldSessionEdgeChecks()
    {
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/HY-Sandbox/World/Validate world session edges")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != WorldSessionAuthoring.MainScenePath || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Open saved Main in Edit Mode before running world edge checks.");
        }

        Directory.CreateDirectory("Temp/WorldSession");
        File.WriteAllText(ReportPath, "{\"status\":\"starting\"}");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            report = new Report
            {
                status = "running"
            };
            originalTimeScale = Time.timeScale;
            originalSimulationMode = Physics.simulationMode;
            Application.logMessageReceived += CaptureError;
            scenario = Scenario();
            deadline = EditorApplication.timeSinceStartup + 360d;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && scenario != null)
        {
            report.errors.Add("Play Mode exited before the edge checks completed.");
            Finish(false);
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("World session edge checks exceeded 360 seconds.");
            }

            if (!scenario.MoveNext())
            {
                Finish(true);
            }
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
            Finish(true);
        }
    }

    private static void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            report.errors.Add(message + "\n" + stack);
        }
    }

    private static void Check(bool condition, string description)
    {
        if (!condition)
        {
            throw new InvalidOperationException(description);
        }

        report.checks.Add(description);
    }

    private static IEnumerator Scenario()
    {
        yield return null;
        session = Object.FindFirstObjectByType<WorldSessionManager>();
        Check(session != null, "Main contains the production world session");
        while (BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        originalSettings = (WorldGenerationSettings)new SerializedObject(session).FindProperty("settings").objectReferenceValue;
        profile = Object.Instantiate(originalSettings);
        profile.name = "World edge checks runtime configuration";
        BindProfile(profile);
        ConfigureProfile(4, 5, 1f);
        session.BeginPreparation();
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        Check(
            session.State == WorldSessionState.Ready,
            "maximum load radius and full building density reach Ready without a budget exception");
        Check(session.World.LoadedColumns.Count == 81, "load radius four prepares exactly 81 terrain columns");
        Check(
            session.Buildings.Buildings.Count > 0 && session.Buildings.Buildings.Count <= 64,
            "prepared building instances respect the 64-building active budget");
        Check(
            session.DormantBuildingCount > 0,
            "excess generated buildings remain deferred rather than lost or fatal");
        Check(!session.World.gameObject.activeInHierarchy, "budgeted preparation keeps world simulation hidden");
        ConfigureProfile(2, 3, 0f);
        session.BeginPreparation();
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        Check(
            session.State == WorldSessionState.Ready,
            "a separate clear radius-two session prepares for the high-speed probe");
        CreatePlayer();
        Rigidbody body = testPlayer.GetComponent<Rigidbody>();
        body.maxLinearVelocity = 10000f;
        session.EnterPlay(testPlayer.transform);
        while (session.IsEntering)
        {
            yield return null;
        }

        float span = session.Settings.ChunkSize * session.Settings.CellSize;
        // Offset from the exact 16+80=96 boundary: the project's rational timestep is slightly below 0.02.
        testPlayer.transform.position = session.World.SpawnPosition + Vector3.right * (span * 0.25f) + Vector3.up * 90f;
        Physics.SyncTransforms();
        InvokeSessionPhysics();
        body.isKinematic = false;
        body.linearVelocity = Vector3.right * 4000f;
        Time.timeScale = 0f;
        InvokeSessionPhysics();
        Check(body.isKinematic, "4000 m/s movement freezes before crossing into an unprepared predicted column");
        object guard = typeof(WorldSessionManager).GetField("playerGuard", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
        Vector3 heldTarget = ReadGuardVector(guard, "heldTarget");
        Vector3 heldVelocity = ReadGuardVector(guard, "heldVelocity");
        Vector2Int predictedColumn = session.World.ColumnAt(heldTarget + heldVelocity * Time.fixedDeltaTime);
        Check(
            !session.World.HasColumn(predictedColumn),
            "the high-speed fixture actually targets terrain outside the prepared radius");
        InvokeSessionPhysics();
        Check(
            body.isKinematic && !session.World.HasColumn(predictedColumn),
            "a second production guard tick keeps waiting while the saved velocity still reaches missing terrain");
        double speedDeadline = EditorApplication.timeSinceStartup + 90d;
        while (!session.World.HasColumn(predictedColumn))
        {
            CheckSessionHealthy(speedDeadline, "loading the predicted high-speed column");
            yield return null;
        }

        // Generation uses frame yields and continues at timeScale zero; explicitly tick the real guard.
        while (body.isKinematic)
        {
            InvokeSessionPhysics();
            CheckSessionHealthy(speedDeadline, "releasing the high-speed guard after its complete swept footprint is ready");
            yield return null;
        }

        Check(
            session.World.HasColumn(predictedColumn),
            "high-speed release occurs only after the predicted terrain column is loaded");
        Physics.simulationMode = SimulationMode.Script;
        Physics.SyncTransforms();
        Physics.Simulate(Time.fixedDeltaTime);
        Physics.simulationMode = originalSimulationMode;
        Check(
            !body.isKinematic && session.World.HasColumn(session.World.ColumnAt(testPlayer.transform.position)),
            "the resumed real physics step ends above a prepared terrain column");
        Check(
            Vector3.Distance(testPlayer.transform.position, heldTarget) > span,
            "the real resumed physics step travels beyond one terrain column rather than being silently clamped");
        Check(
            Physics.RaycastAll(testPlayer.transform.position, Vector3.down, 300f).Any(hit => hit.collider.transform.IsChildOf(session.World.transform)),
            "the resumed vehicle retains a real terrain collider below and does not fall into an unloaded gap");
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = true;
        Time.timeScale = originalTimeScale;
        ConfigureProfile(2, 3, 1f);
        session.EndSession();
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        Check(
            session.State == WorldSessionState.Ready,
            "the dynamic-building probe starts in an independent fresh session");
        session.EnterPlay(testPlayer.transform);
        while (session.IsEntering)
        {
            yield return null;
        }

        Time.timeScale = 0f;
        InvokeSessionPhysics();
        Vector2Int missingColumn = new Vector2Int(3, 0);
        Check(
            !session.World.HasColumn(missingColumn),
            "the dynamic-building destination is within unload radius but has no generated ground");
        BuildingRuntime building = session.Buildings.Buildings.First();
        string identity = building.InstanceId;
        Durability durability = building.GetComponentsInChildren<Durability>().First();
        durability.currentDurability = durability.maxDurability * 0.73f;
        string damagedBlock = durability.GetComponent<Block>().uniqueId;
        float expectedHealth = durability.currentDurability;
        Vector3 destination = session.World.transform.TransformPoint(new Vector3(3.5f * span, 55f, span * 0.5f));
        building.transform.position = destination;
        building.ReleaseAnchor();
        building.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        Physics.SyncTransforms();
        int dormantBefore = session.DormantBuildingCount;
        double buildingDeadline = EditorApplication.timeSinceStartup + 90d;
        while (session.Buildings.Buildings.Any(item => item.InstanceId == identity))
        {
            CheckSessionHealthy(buildingDeadline, "putting an unsupported dynamic building to sleep");
            yield return null;
        }

        Check(
            session.DormantBuildingCount > dormantBefore,
            "a released building with missing ground sleeps even inside the distance-based unload radius");
        Check(
            building == null || !building.gameObject.activeInHierarchy,
            "the unsupported building stops its live physics simulation while dormant");
        testPlayer.transform.position = destination + Vector3.up * 35f;
        InvokeSessionPhysics();
        while (!session.Buildings.Buildings.Any(item => item.InstanceId == identity))
        {
            CheckSessionHealthy(buildingDeadline, "restoring the sleeping building when its region loads");
            yield return null;
        }

        building = session.Buildings.Buildings.First(item => item.InstanceId == identity);
        Check(
            Vector3.Distance(building.transform.position, destination) < 0.01f,
            "restoring the dynamic building preserves its sleep position without unsupported free fall");
        Check(
            !building.GetComponent<Rigidbody>().isKinematic,
            "restoring the building preserves its released anchor state");
        durability = building.GetComponentsInChildren<Durability>().First(item => item.GetComponent<Block>().uniqueId == damagedBlock);
        Check(
            Mathf.Approximately(durability.currentDurability, expectedHealth),
            "the unsupported-building sleep and restore path preserves damage");
        Check(
            session.World.HasColumn(missingColumn),
            "restoring the dormant building waits for actual destination terrain");
        Time.timeScale = originalTimeScale;
        report.skipped.Add("Failure injection is excluded: it intentionally emits an Error log and runs the full player-group/load UI recovery; this isolated probe does not depend on the user's current saved construct.");
        Check(report.errors.Count == 0, "all edge probes complete without unexpected Unity errors or exceptions");
    }

    private static void BindProfile(WorldGenerationSettings value)
    {
        var data = new SerializedObject(session);
        data.FindProperty("settings").objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureProfile(int loadRadius, int unloadRadius, float buildingChance)
    {
        var data = new SerializedObject(profile);
        data.FindProperty("randomizeSeed").boolValue = false;
        data.FindProperty("seed").intValue = 14721;
        data.FindProperty("chunkSize").intValue = 16;
        data.FindProperty("cellSize").floatValue = 2f;
        data.FindProperty("spawnClearRadius").intValue = 1;
        data.FindProperty("loadRadius").intValue = loadRadius;
        data.FindProperty("unloadRadius").intValue = unloadRadius;
        data.FindProperty("buildingChance").floatValue = buildingChance;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreatePlayer()
    {
        originalGameParent = GameManager.instance.blocksParent;
        originalPlayParent = PlayManager.instance.blocksParent;
        if (originalGameParent != null)
        {
            originalParentActive = originalGameParent.gameObject.activeSelf;
            originalGameParent.gameObject.SetActive(false);
        }

        testPlayer = new GameObject("World edge checks isolated construct");
        testPlayer.AddComponent<Rigidbody>().isKinematic = true;
        ControlUnit unit = testPlayer.AddComponent<ControlUnit>();
        GameObject cockpit = Object.Instantiate(Resources.Load<GameObject>("Blocks/Cockpit"), testPlayer.transform);
        cockpit.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        cockpit.GetComponent<Cockpit>().faction = UnitFaction.Player;
        foreach (Durability health in testPlayer.GetComponentsInChildren<Durability>())
        {
            health.currentDurability = health.maxDurability = 1000000f;
        }

        unit.RefreshChildren();
        GameManager.instance.blocksParent = testPlayer.transform;
        PlayManager.instance.blocksParent = testPlayer.transform;
        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.spawnOnPlayStart = false;
            spawner.spawnInterval = float.MaxValue;
        }
    }

    private static Vector3 ReadGuardVector(object guard, string field)
    {
        return (Vector3)guard.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(guard);
    }

    private static void InvokeSessionPhysics()
    {
        typeof(WorldSessionManager).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(session, null);
    }

    private static void CheckSessionHealthy(double timeout, string operation)
    {
        if (session.State == WorldSessionState.Failed || EditorApplication.timeSinceStartup > timeout)
        {
            throw new InvalidOperationException("Failed while " + operation + ": " + session.Status);
        }
    }

    private static void Finish(bool exitPlayMode)
    {
        Application.logMessageReceived -= CaptureError;
        EditorApplication.update -= Tick;
        (scenario as IDisposable)?.Dispose();
        scenario = null;
        Time.timeScale = originalTimeScale;
        Physics.simulationMode = originalSimulationMode;
        if (session != null && originalSettings != null)
        {
            BindProfile(originalSettings);
            session.EndSession();
        }

        if (testPlayer != null)
        {
            GameManager.instance.blocksParent = originalGameParent;
            PlayManager.instance.blocksParent = originalPlayParent;
            Object.Destroy(testPlayer);
        }

        if (originalGameParent != null)
        {
            originalGameParent.gameObject.SetActive(originalParentActive);
        }

        if (profile != null)
        {
            Object.Destroy(profile);
        }

        SessionState.SetBool(Pending, false);
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/WorldSession");
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log("World session edge checks: " + report.status + " / " + report.checks.Count + " checks. " + ReportPath);
        if (exitPlayMode)
        {
            EditorApplication.isPlaying = false;
        }
    }
}

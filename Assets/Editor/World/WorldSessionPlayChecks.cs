// 职责：在真实 Main Play Mode 验证后台预热、流式卸载、会话内修改及退出清空。
// 入口：Run()；只操作运行时测试对象，报告写 Temp/WorldSession，不保存玩家数据或场景。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WorldSessionPlayChecks
{
    private const string Pending = "HY.WorldSessionPlayChecks";
    public const string ReportPath = "Temp/WorldSession/play-checks.json";
    private static IEnumerator scenario;
    private static Report report;
    private static double deadline;
    private static GameObject testPlayer;
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    static WorldSessionPlayChecks()
    {
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/HY-Sandbox/World/Validate Main world session")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != WorldSessionAuthoring.MainScenePath || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Open saved Main in Edit Mode before running world session checks.");
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
            Application.logMessageReceived += CaptureError;
            scenario = Scenario();
            deadline = EditorApplication.timeSinceStartup + 240d;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && scenario != null)
        {
            report.errors.Add("Play Mode exited before the checks completed.");
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
                throw new TimeoutException("World session checks exceeded 240 seconds.");
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
        WorldSessionManager session = Object.FindFirstObjectByType<WorldSessionManager>();
        Check(
            session != null && PlayManager.instance.WorldSession == session,
            "Main binds the authored world session to PlayManager");
        IEnumerator lowLevelChecks = StreamingChecks(session.Settings);
        try
        {
            while (lowLevelChecks.MoveNext())
            {
                yield return lowLevelChecks.Current;
            }
        }
        finally
        {
            (lowLevelChecks as IDisposable)?.Dispose();
        }

        ProceduralVoxelWorld world = session.World;
        WorldBuildingManager buildings = session.Buildings;
        GameObject worldRoot = world.transform.parent.gameObject;
        SerializedProperty environmentData = new SerializedObject(session).FindProperty("buildEnvironment");
        var grounds = new List<GameObject>();
        var groundStates = new List<bool>();
        for (int i = 0; i < environmentData.arraySize; i++)
        {
            var groundObject = (GameObject)environmentData.GetArrayElementAtIndex(i).objectReferenceValue;
            grounds.Add(groundObject);
            groundStates.Add(groundObject.activeSelf);
        }

        Check(
            grounds.Count > 0,
            "the scene binds the existing build grounds without replacing lighting or the Canvas");
        Check(!worldRoot.activeInHierarchy, "world content remains hidden while Main is in build mode");
        Check(
            !ControlUnit.RuntimeUnits.Any(unit => unit.Building != null && unit.IsSimulationActive),
            "background preparation runs no active building simulation");
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        Check(
            session.State == WorldSessionState.Ready && session.Progress >= 1f,
            "background preparation reaches Ready with full progress");
        string blueprintDirectory = Path.Combine(Application.persistentDataPath, "BuildingBlueprints");
        string[] userBlueprints = Directory.GetFiles(blueprintDirectory, "*.json").Select(Path.GetFileNameWithoutExtension).OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
        Check(
            session.BuildingTemplates.SequenceEqual(userBlueprints),
            "the generation pool includes every blueprint in the real user BuildingBlueprints directory");
        Check(
            buildings.Buildings.All(building => userBlueprints.Contains(building.Capture().blueprintName)),
            "spawned buildings use user templates rather than the bundled Outpost catalog");
        Transform horizon = world.transform.Find("Distant terrain - visual only");
        Mesh horizonMesh = horizon.GetComponent<MeshFilter>().sharedMesh;
        Check(
            horizonMesh != null && horizonMesh.bounds.size.x > session.Settings.ViewDistance * 2f && horizon.GetComponent<Collider>() == null,
            "the prepared visual horizon extends beyond the fog range without adding distant physics");
        Check(
            world.LoadedColumns.Count == (session.Settings.LoadRadius * 2 + 1) * (session.Settings.LoadRadius * 2 + 1),
            "only the initial load radius is generated");
        Check(
            buildings.Buildings.Count > 0 && !worldRoot.activeInHierarchy,
            "terrain and real Buildings are prepared together while hidden");
        Check(
            buildings.Buildings.All(building => !building.IsSimulationActive),
            "prepared buildings stay dormant before entering play");
        while (BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        // The temporary anchor exercises the production session entrypoint without touching player blueprint files.
        if (GameManager.instance.blocksParent != null)
        {
            GameManager.instance.blocksParent.gameObject.SetActive(false);
        }

        testPlayer = new GameObject("World session validation construct");
        Rigidbody playerBody = testPlayer.AddComponent<Rigidbody>();
        playerBody.isKinematic = true;
        ControlUnit playerUnit = testPlayer.AddComponent<ControlUnit>();
        GameObject cockpitObject = Object.Instantiate(Resources.Load<GameObject>("Blocks/Cockpit"), testPlayer.transform);
        cockpitObject.transform.localPosition = Vector3.zero;
        cockpitObject.transform.localRotation = Quaternion.identity;
        cockpitObject.GetComponent<Cockpit>().faction = UnitFaction.Player;
        foreach (Durability health in testPlayer.GetComponentsInChildren<Durability>())
        {
            health.currentDurability = health.maxDurability = 1000000f;
        }

        playerUnit.RefreshChildren();
        GameManager.instance.blocksParent = testPlayer.transform;
        PlayManager.instance.blocksParent = testPlayer.transform;
        FogMode previousFogMode = RenderSettings.fogMode;
        float previousFogEnd = RenderSettings.fogEndDistance;
        Color previousFogColor = RenderSettings.fogColor;
        CameraClearFlags previousClearFlags = Camera.main.clearFlags;
        session.EnterPlay(testPlayer.transform);
        WorldLoadingPanel entryPanel = Object.FindFirstObjectByType<WorldLoadingPanel>();
        Check(
            session.IsEntering && Time.timeScale == 0f,
            "world activation pauses physics until all building batches finish");
        Check(
            entryPanel.GetComponent<CanvasGroup>().blocksRaycasts && entryPanel.GetComponent<Image>().color.a == 1f,
            "activation uses an opaque input-blocking progress overlay");
        bool partialProgress = false;
        bool capturedLoading = false;
        while (session.IsEntering)
        {
            partialProgress |= session.Progress > 0f && session.Progress < 1f;
            if (!capturedLoading && session.Progress > 0.25f)
            {
                ScreenCapture.CaptureScreenshot("Temp/WorldSession/performance-loading.png");
                capturedLoading = true;
            }

            yield return null;
        }

        Check(
            partialProgress && Time.timeScale == 1f && entryPanel.GetComponent<CanvasGroup>().alpha == 0f,
            "activation updates intermediate progress and restores physics and view when complete");
        Check(
            session.State == WorldSessionState.Playing && worldRoot.activeInHierarchy,
            "EnterPlay displays and enables the prepared world");
        Check(grounds.All(item => !item.activeSelf), "entering play hides the old build grounds");
        Check(
            buildings.Buildings.All(building => building.IsSimulationActive),
            "entering play activates building simulation");
        MeshFilter[] combinedVisuals = buildings.GetComponentsInChildren<MeshFilter>().Where(filter => filter.name == "World combined visual").ToArray();
        Check(
            combinedVisuals.Length > 0 && combinedVisuals.GroupBy(filter => filter.sharedMesh).Any(group => group.Count() > 1),
            "world modules share combined visual meshes while retaining separate block entities");
        Transform view = Camera.main.transform;
        Vector3 originalCameraPosition = view.position;
        Quaternion originalCameraRotation = view.rotation;
        Vector3 buildingView = buildings.Buildings.First().transform.position + Vector3.up * 3f;
        view.position = buildingView + new Vector3(-17f, 12f, -17f);
        view.LookAt(buildingView);
        yield return null;
        ScreenCapture.CaptureScreenshot("Temp/WorldSession/performance-building.png");
        yield return null;
        view.SetPositionAndRotation(originalCameraPosition, originalCameraRotation);
        Check(
            Physics.RaycastAll(testPlayer.transform.position + Vector3.up * 4f, Vector3.down, 30f).Any(hit => hit.collider.transform.IsChildOf(world.transform)),
            "spawn has a loaded terrain collider underneath");
        Block cockpitBlock = cockpitObject.GetComponent<Block>();
        Bounds spawnBounds = new Bounds(cockpitObject.transform.position, new Vector3(cockpitBlock.x, cockpitBlock.y, cockpitBlock.z));
        Check(
            spawnBounds.min.y >= world.FindSpawnSurface(spawnBounds).y + session.Settings.SpawnClearance - 0.01f,
            "the player bottom is raised above the highest natural terrain under its entire footprint");
        Check(
            RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear && Mathf.Approximately(RenderSettings.fogEndDistance, session.Settings.ViewDistance) && Camera.main.backgroundColor == RenderSettings.fogColor,
            "the extended view fades into a matching fog background before the distant mesh boundary");
        Vector3Int spawnCell = world.WorldToCell(world.SpawnPosition) - Vector3Int.up;
        Check(world.SetCell(spawnCell, 0), "terrain at the prepared spawn can be modified");
        world.FlushDirty();
        Vector2Int spawnColumn = world.ColumnAt(world.SpawnPosition);
        BuildingRuntime first = buildings.Buildings.First(building => building.GetComponentsInChildren<CargoHold>(true).Length > 0);
        string buildingId = first.InstanceId;
        Vector2Int buildingColumn = world.ColumnAt(first.transform.position);
        CargoHold hold = first.GetComponentInChildren<CargoHold>(true);
        hold.RestoreContents(new List<CargoItem> { new CargoItem { kind = hold.Kind, resourcePath = "Assets/Resources/Blocks/1x1x1.prefab", amount = 3 } });
        Check(hold.Used == 3, "the cargo persistence fixture accepts three items matching its hold kind");
        Durability durability = first.GetComponentsInChildren<Durability>(true).First();
        durability.currentDurability = durability.maxDurability * 0.6f;
        string damagedId = durability.GetComponent<Block>().uniqueId;
        float remainingHealth = durability.currentDurability;
        Vector3 firstPosition = first.transform.position;
        testPlayer.transform.position = world.transform.TransformPoint(new Vector3(
            10.5f * session.Settings.ChunkSize * session.Settings.CellSize,
            50f,
            10.5f * session.Settings.ChunkSize * session.Settings.CellSize));
        while (!NeighborhoodReady(world, new Vector2Int(10, 10)) || world.ColumnAt(testPlayer.transform.position) != new Vector2Int(10, 10) || world.HasColumn(spawnColumn) || buildings.Buildings.Any(building => building.InstanceId == buildingId))
        {
            yield return null;
        }

        Check(session.DormantBuildingCount > 0, "leaving a region stores dormant building state");
        Check(
            world.LoadedColumns.Count <= (session.Settings.UnloadRadius * 2 + 1) * (session.Settings.UnloadRadius * 2 + 1),
            "positive world travel keeps the loaded terrain bounded");
        testPlayer.transform.position = world.transform.TransformPoint(new Vector3(
            -10.5f * session.Settings.ChunkSize * session.Settings.CellSize,
            50f,
            -10.5f * session.Settings.ChunkSize * session.Settings.CellSize));
        while (!NeighborhoodReady(world, new Vector2Int(-11, -11)) || world.ColumnAt(testPlayer.transform.position) != new Vector2Int(-11, -11) || world.HasColumn(new Vector2Int(10, 10)))
        {
            yield return null;
        }

        Check(
            world.LoadedColumns.Count <= (session.Settings.UnloadRadius * 2 + 1) * (session.Settings.UnloadRadius * 2 + 1),
            "negative coordinates stream and unload within the same bound");
        testPlayer.transform.position = firstPosition + Vector3.up * 35f;
        while (!NeighborhoodReady(world, buildingColumn) || world.ColumnAt(testPlayer.transform.position) != buildingColumn || !buildings.Buildings.Any(building => building.InstanceId == buildingId))
        {
            yield return null;
        }

        first = buildings.Buildings.First(building => building.InstanceId == buildingId);
        Check(
            first.GetComponentInChildren<CargoHold>(true).Used == 3,
            "building cargo survives unloading and reloading within a session");
        durability = first.GetComponentsInChildren<Durability>(true).First(item => item.GetComponent<Block>().uniqueId == damagedId);
        Check(
            Mathf.Approximately(durability.currentDurability, remainingHealth),
            "building damage survives unloading and reloading within a session");
        Check(world.Data.Get(spawnCell) == 0, "terrain edits survive unloading and reloading within a session");
        session.EndSession();
        Check(
            session.State == WorldSessionState.Preparing && !worldRoot.activeInHierarchy,
            "exit hides the world and starts a fresh preparation");
        Check(
            grounds.Select((item, index) => item.activeSelf == groundStates[index]).All(value => value),
            "exit restores each build ground's authored activation state");
        Check(
            RenderSettings.fogMode == previousFogMode && Mathf.Approximately(RenderSettings.fogEndDistance, previousFogEnd) && RenderSettings.fogColor == previousFogColor && Camera.main.clearFlags == previousClearFlags,
            "exit restores the authored build fog and camera background mode");
        Check(
            session.DormantBuildingCount == 0 && buildings.Buildings.Count == 0 && world.LoadedColumns.Count == 0,
            "exit clears terrain, buildings and dormant state immediately");
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        Check(
            session.State == WorldSessionState.Ready && world.Data.Get(spawnCell) == new StreamingWorldTerrain(session.Settings, session.SessionSeed).Sample(spawnCell),
            "a fresh session restores baseline terrain instead of retaining changes");
        Check(
            buildings.Buildings.SelectMany(building => building.GetComponentsInChildren<CargoHold>(true)).All(item => item.Used != 3),
            "a new session discards modified building cargo");
        session.BeginPreparation();
        session.RequestPlay();
        session.RequestPlay();
        session.CancelPlayRequest();
        Check(!session.IsPlayRequested, "repeated play requests can be cancelled without a stale transition");
        WorldLoadingPanel panel = Object.FindFirstObjectByType<WorldLoadingPanel>();
        Check(
            panel != null && panel.GetComponent<CanvasGroup>().alpha == 0f,
            "cancel dismisses the blocking loading interface");
        session.RequestPlay();
        Button cancel = panel.GetComponentsInChildren<Button>(true).Single();
        cancel.onClick.Invoke();
        Check(!session.IsPlayRequested, "the authored Cancel button invokes production cancellation");
        session.EndSession();
        yield return null;
        Check(
            session.State != WorldSessionState.Playing && !worldRoot.activeInHierarchy,
            "cancel and exit cannot reactivate obsolete prepared content");
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null)
        {
            spawner = new GameObject("World session validation enemy scheduler").AddComponent<EnemySpawner>();
        }

        // Keep the real scheduler active, but exclude unrelated random enemy encounters from this scenario.
        spawner.spawnOnPlayStart = false;
        spawner.spawnInterval = float.MaxValue;
        MainUIPanels.instance.PlayStart();
        yield return null;
        Check(
            PlayManager.instance.playMode && session.State == WorldSessionState.Playing,
            "the production Main Play button enables the prepared world and player simulation");
        MainUIPanels.instance.PlayerDeath();
        Check(
            !PlayManager.instance.playMode && session.State == WorldSessionState.Preparing && !worldRoot.activeInHierarchy && session.DormantBuildingCount == 0 && Time.timeScale == 1f,
            "the production death path discards the exploration world and restores build preparation");
        while (session.State == WorldSessionState.Preparing)
        {
            yield return null;
        }

        MainUIPanels.instance.PlayStart();
        Check(
            PlayManager.instance.playMode,
            "the next exploration starts after death without stale session state");
        // This production exit intentionally skips MainUIPanels.PlayEnd, whose cargo transfer writes a player save.
        PlayManager.instance.PlayEnd();
        Check(
            !PlayManager.instance.playMode && session.State == WorldSessionState.Preparing && !worldRoot.activeInHierarchy,
            "the production PlayManager exit discards the world and prepares the next session");
        Check(report.errors.Count == 0, "Main world session completes with no Unity error or exception logs");
    }

    private static IEnumerator StreamingChecks(WorldGenerationSettings settings)
    {
        var baseline = new StreamingWorldTerrain(settings, 14721);
        var repeated = new StreamingWorldTerrain(settings, 14721);
        var different = new StreamingWorldTerrain(settings, 14722);
        var spawnHeights = new HashSet<float>();
        for (int seed = 14721; seed < 14741; seed++)
        {
            spawnHeights.Add(new StreamingWorldTerrain(settings, seed).SpawnLocalPosition.y);
        }

        Check(
            spawnHeights.Count > 1,
            "spawn ground follows seeded natural terrain instead of a fixed depressed platform");
        bool positiveMatches = true;
        bool negativeMatches = true;
        bool seedChangesTerrain = false;
        bool buildingSitesMatch = true;
        for (int sign = -1; sign <= 1; sign += 2)
        {
            for (int x = 2; x <= 5; x++)
            {
                for (int z = 2; z <= 5; z++)
                {
                    var coordinate = new Vector2Int(sign * x, sign * z);
                    bool firstHasSite = baseline.TryGetBuildingSite(coordinate, out Vector3 firstSite, out Quaternion firstRotation);
                    bool repeatedHasSite = repeated.TryGetBuildingSite(coordinate, out Vector3 repeatedSite, out Quaternion repeatedRotation);
                    buildingSitesMatch &= firstHasSite == repeatedHasSite && (!firstHasSite || (firstSite == repeatedSite && firstRotation == repeatedRotation));
                    for (int y = 0; y < settings.WorldHeight; y++)
                    {
                        var cell = new Vector3Int(coordinate.x * settings.ChunkSize + 3, y, coordinate.y * settings.ChunkSize + 3);
                        byte value = baseline.Sample(cell);
                        bool matches = value == repeated.Sample(cell);
                        positiveMatches &= sign < 0 || matches;
                        negativeMatches &= sign > 0 || matches;
                        seedChangesTerrain |= value != different.Sample(cell);
                    }
                }
            }
        }

        Check(
            positiveMatches && negativeMatches,
            "a fixed seed deterministically reproduces positive and negative terrain coordinates");
        Check(buildingSitesMatch, "a fixed seed deterministically reproduces building sites and rotations");
        Check(seedChangesTerrain, "a different seed changes the sampled terrain baseline");
        var temporary = new GameObject("World session interrupted-column fixture");
        temporary.SetActive(false);
        ProceduralVoxelWorld world = temporary.AddComponent<ProceduralVoxelWorld>();
        try
        {
            world.InitializeStreaming(settings, 14721);
            bool rejected = false;
            try
            {
                world.CaptureEdits();
            }
            catch (InvalidOperationException)
            {
                rejected = true;
            }

            Check(rejected, "streamed terrain rejects incompatible WorldLab snapshot capture");
            var coordinate = new Vector2Int(-3, 3);
            IEnumerator load = world.LoadColumn(coordinate);
            Mesh ownedMesh = null;
            try
            {
                Check(
                    load.MoveNext() && !world.HasColumn(coordinate),
                    "a raw column loader yields before publishing a completed column");
                MeshFilter filter = temporary.GetComponentInChildren<MeshFilter>(true);
                while (filter == null)
                {
                    yield return null;
                    Check(load.MoveNext(), "asynchronous column continues before its first mesh upload");
                    filter = temporary.GetComponentInChildren<MeshFilter>(true);
                }

                ownedMesh = filter.sharedMesh;
                Check(ownedMesh != null, "the interrupted column owns a generated mesh before cancellation");
                world.UnloadColumn(coordinate);
            }
            finally
            {
                (load as IDisposable)?.Dispose();
            }

            Check(
                !world.HasColumn(coordinate) && temporary.GetComponentsInChildren<MeshFilter>(true).All(filter => filter.sharedMesh == null) && temporary.GetComponentsInChildren<MeshCollider>(true).All(collider => !collider.enabled),
                "unload clears the partial mesh presentation, disables collision and never publishes the column");
            int frame = Time.frameCount;
            while (Time.frameCount == frame)
            {
                yield return null;
            }

            Check(
                ownedMesh == null && temporary.GetComponentsInChildren<VoxelWorldChunk>(true).Length == 0,
                "the cancelled column releases its owned mesh and chunk objects after Unity destruction");
        }
        finally
        {
            world.ClearStreaming();
            Object.Destroy(temporary);
        }
    }

    private static bool NeighborhoodReady(ProceduralVoxelWorld world, Vector2Int center)
    {
        for (int z = -1; z <= 1; z++)
        {
            for (int x = -1; x <= 1; x++)
            {
                if (!world.HasColumn(center + new Vector2Int(x, z)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void Finish(bool exitPlayMode)
    {
        Application.logMessageReceived -= CaptureError;
        EditorApplication.update -= Tick;
        (scenario as IDisposable)?.Dispose();
        scenario = null;
        SessionState.SetBool(Pending, false);
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/WorldSession");
        File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        Debug.Log("Main world session checks: " + report.status + " / " + report.checks.Count + " checks. " + ReportPath);
        if (testPlayer != null)
        {
            Object.Destroy(testPlayer);
        }

        if (exitPlayMode)
        {
            EditorApplication.isPlaying = false;
        }
    }
}

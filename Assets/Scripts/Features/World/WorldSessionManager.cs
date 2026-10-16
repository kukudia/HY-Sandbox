// 职责：在Main建造期间分帧预热世界，进入游玩后启用并跟踪区块，退出即丢弃整个探索会话。
// 入口：BeginPreparation() / RequestPlay() / EnterPlay() / EndSession()；不读写玩家或WorldLab世界存档。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WorldSessionState
{
    Preparing,
    Ready,
    Playing,
    Failed
}

public sealed class WorldSessionManager : MonoBehaviour
{
    [SerializeField]
    private WorldGenerationSettings settings;
    [SerializeField]
    private ProceduralVoxelWorld world;
    [SerializeField]
    private WorldBuildingManager buildings;
    [SerializeField]
    private GameObject worldRoot;
    [SerializeField]
    private GameObject[] buildEnvironment = Array.Empty<GameObject>();
    [SerializeField]
    private WorldLoadingPanel loadingPanel;
    private readonly Dictionary<GameObject, bool> environmentStates = new Dictionary<GameObject, bool>();
    private WorldBuildingStreaming buildingStreaming;
    private OutpostEncounterDirector outposts;
    public OutpostEncounterDirector Outposts => outposts;

    private WorldRegionStreaming regionStreaming;
    private WorldGenerationSettings sessionSettings;
    private Coroutine routine;
    private Coroutine distanceRoutine;
    private Coroutine activationRoutine;
    private bool entering;
    private float previousTimeScale;
    private WorldDistanceView distanceView;
    private Transform player;
    private Vector3 streamingFocus;
    private bool playRequested;
    private int generation;
    private WorldPlayerStreamingGuard playerGuard;
    private bool failureNeedsExit;
    private bool started;
    public WorldSessionState State { get; private set; } = WorldSessionState.Preparing;
    public float Progress
    {
        get; private set;
    }
    public string Status { get; private set; } = "Preparing the exploration world...";
    public int SessionSeed
    {
        get; private set;
    }
    public bool IsPlayRequested => playRequested;
    public bool IsEntering => entering;
    public int DormantBuildingCount => buildingStreaming?.DormantCount ?? 0;
    public ProceduralVoxelWorld World => world;
    public WorldBuildingManager Buildings => buildings;
    public WorldGenerationSettings Settings => sessionSettings != null ? sessionSettings : settings;
    public IReadOnlyList<string> BuildingTemplates { get; private set; } = Array.Empty<string>();

    private void Start()
    {
        started = true;
        loadingPanel?.Bind(this);
        BeginPreparation();
        loadingPanel?.ShowStartup();
    }

    private void OnEnable()
    {
        if (started)
        {
            BeginPreparation();
        }
    }

    /// <summary>取消旧任务并丢弃全部变化；禁用根下的数据、网格和建筑逐帧准备，避免进入后台模拟。</summary>
    public void BeginPreparation()
    {
        StopPreparation();
        outposts?.Clear();
        outposts = null;
        SetBuildEnvironmentVisible(true);
        buildings?.PrepareForUnload();
        worldRoot?.SetActive(false);
        if (buildings != null)
        {
            buildings.SimulationEnabled = false;
        }

        buildings?.ClearSession();
        world?.ClearStreaming();
        buildingStreaming = null;
        player = null;
        playRequested = false;
        loadingPanel?.Show(false);
        Progress = 0f;
        State = WorldSessionState.Preparing;
        failureNeedsExit = false;
        Status = "Preparing the exploration world...";
        try
        {
            if (settings == null || world == null || buildings == null || worldRoot == null)
            {
                throw new InvalidOperationException("World session scene references are incomplete.");
            }

            SessionSeed = Settings.RandomizeSeed ? unchecked((int)DateTime.UtcNow.Ticks ^ Guid.NewGuid().GetHashCode()) : Settings.Seed;
            sessionSettings = Instantiate(settings);
            routine = StartCoroutine(RunGuarded(generation, PrepareWorld()));
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private IEnumerator PrepareWorld()
    {
        // 先让UI呈现一帧，再读取和预加载资源；Resources异步请求避免首次同步加载整套模型。
        yield return null;
        var library = new WorldBuildingLibrary();
        IEnumerator load = library.LoadRoutine(WorldBuildingLibrary.DirectoryPath);
        try
        {
            while (load.MoveNext())
            {
                Progress = library.Progress * 0.1f;
                Status = $"Loading building blueprints... {Progress:P0}";
                yield return load.Current;
            }
        }
        finally
        {
            (load as IDisposable)?.Dispose();
        }

        BuildingTemplates = library.Names.AsReadOnly();
        foreach (string error in library.Errors)
        {
            Debug.LogWarning("Skipping building blueprint: " + error);
        }

        buildings.LoadingFrameBudgetMilliseconds = Settings.LoadingFrameBudgetMilliseconds;
        world.InitializeStreaming(Settings, SessionSeed, library.Templates);
        buildingStreaming = new WorldBuildingStreaming(world, buildings, Settings, library);
        outposts = new OutpostEncounterDirector(buildings);
        regionStreaming = new WorldRegionStreaming(world, buildingStreaming, Settings);
        streamingFocus = world.SpawnPosition;
        distanceView = new WorldDistanceView(world, Settings);
        distanceRoutine = StartCoroutine(RunGuarded(generation, distanceView.Refresh()));
        IEnumerator stream = StreamWorld();
        try
        {
            while (stream.MoveNext())
            {
                yield return stream.Current;
            }
        }
        finally
        {
            (stream as IDisposable)?.Dispose();
        }
    }

    /// <summary>等待世界就绪后再切换UI和物理；重复点击只保留一个请求，取消不打断后台准备。</summary>
    public void RequestPlay()
    {
        if (State == WorldSessionState.Playing || playRequested)
        {
            return;
        }

        if (State == WorldSessionState.Failed)
        {
            BeginPreparation();
        }

        playRequested = true;
        loadingPanel?.Show(true);
        CompletePendingPlay();
    }

    public void CancelPlayRequest()
    {
        playRequested = false;
        loadingPanel?.Show(false);
    }

    private void CompletePendingPlay()
    {
        if (!playRequested || State != WorldSessionState.Ready)
        {
            return;
        }

        playRequested = false;
        loadingPanel?.Show(false);
        MainUIPanels.instance.CompletePlayStart();
    }

    public bool CanEnterPlay(Transform construct, out string reason)
    {
        reason = "World is still preparing.";
        if (State != WorldSessionState.Ready || construct == null)
        {
            return false;
        }

        Bounds bounds = ConstructBounds(construct);
        float span = Settings.ChunkSize * Settings.CellSize;
        float safeWidth = (Settings.SpawnClearRadius * 2 + 1) * span - Settings.CellSize * 4f;
        safeWidth = Mathf.Min(safeWidth, (Settings.LoadRadius * 2f * span - Settings.CellSize * 4f) / Mathf.Sqrt(2f));
        if (bounds.size.x > safeWidth || bounds.size.z > safeWidth)
        {
            reason = "Construct is too large for the safe spawn area. Increase the spawn area in World Generation Settings.";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public void EnterPlay(Transform construct)
    {
        if (!CanEnterPlay(construct, out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        player = construct;
        Bounds bounds = ConstructBounds(construct);
        Vector3 bottom = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 delta = world.FindSpawnSurface(bounds) + Vector3.up * Settings.SpawnClearance - bottom;
        construct.position += delta;
        if (CameraController.instance != null)
        {
            CameraController.instance.StopCameraMotion();
            CameraController.instance.transform.position += delta;
        }

        SetBuildEnvironmentVisible(false);
        State = WorldSessionState.Playing;
        distanceView.Enter(Camera.main);
        streamingFocus = construct.position;
        playerGuard = new WorldPlayerStreamingGuard(world, Settings);
        entering = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        Progress = 0f;
        Status = "Enabling world... 0%";
        loadingPanel?.Show(true);
        activationRoutine = StartCoroutine(RunGuarded(generation, EnableWorld()));
    }

    private IEnumerator EnableWorld()
    {
        yield return null;
        worldRoot.SetActive(true);
        int total = buildings.Buildings.Count;
        for (int i = 0; i < total; i++)
        {
            IEnumerator activate = buildings.Buildings[i].ActivateRoutine();
            try
            {
                while (activate.MoveNext())
                {
                    yield return activate.Current;
                }
            }
            finally
            {
                (activate as IDisposable)?.Dispose();
            }

            Progress = (i + 1f) / Mathf.Max(1, total);
            Status = $"Enabling buildings... {i + 1} / {total} ({Progress:P0})";
            yield return null;
        }

        Physics.SyncTransforms();
        buildings.SimulationEnabled = true;
        FinishEntering();
        Progress = 1f;
        Status = "Exploring";
        loadingPanel?.Show(false);
        activationRoutine = null;
    }

    private void FinishEntering()
    {
        if (entering)
        {
            Time.timeScale = previousTimeScale;
            entering = false;
        }
    }

    public void EndSession()
    {
        BeginPreparation();
    }

    private IEnumerator RunGuarded(int version, IEnumerator work)
    {
        try
        {
            while (version == generation && State != WorldSessionState.Failed)
            {
                object current;
                try
                {
                    if (!work.MoveNext())
                    {
                        yield break;
                    }

                    current = work.Current;
                }
                catch (Exception error)
                {
                    Fail(error);
                    yield break;
                }

                yield return current;
            }
        }
        finally
        {
            (work as IDisposable)?.Dispose();
        }
    }

    private IEnumerator StreamWorld()
    {
        while (true)
        {
            if (entering)
            {
                yield return null;
                continue;
            }

            Vector2Int center = State == WorldSessionState.Playing ? world.ColumnAt(streamingFocus) : Vector2Int.zero;
            IEnumerator region = regionStreaming.LoadNext(center);
            try
            {
                while (region.MoveNext())
                {
                    yield return region.Current;
                }
            }
            finally
            {
                (region as IDisposable)?.Dispose();
            }

            if (State == WorldSessionState.Preparing)
            {
                int total = (Settings.LoadRadius * 2 + 1) * (Settings.LoadRadius * 2 + 1);
                Progress = 0.1f + Mathf.Clamp01((float)world.LoadedColumns.Count / total) * 0.85f;
                Status = $"Preparing terrain and buildings... {world.LoadedColumns.Count} / {total}";
                distanceView.AllowRefresh = !regionStreaming.LoadedWork;
                if (!regionStreaming.LoadedWork)
                {
                    Status = "Preparing distant terrain... 95%";
                }

                if (!regionStreaming.LoadedWork && distanceView.IsReady)
                {
                    Progress = 1f;
                    State = WorldSessionState.Ready;
                    Status = "World ready";
                    CompletePendingPlay();
                }
            }

            yield return null;
        }
    }

    private void FixedUpdate()
    {
        if (State != WorldSessionState.Playing || entering)
        {
            return;
        }

        if (PlayManager.instance != null && PlayManager.instance.blocksParent != null)
        {
            player = PlayManager.instance.blocksParent;
        }

        if (player == null)
        {
            return;
        }

        playerGuard.Tick(player);
        streamingFocus = playerGuard.Focus;
        distanceView.Focus = streamingFocus;
        Status = playerGuard.IsWaiting ? "Loading nearby terrain..." : "Exploring";
        Progress = playerGuard.Progress;
        loadingPanel?.Show(playerGuard.IsWaiting);
    }

    private void Update()
    {
        if (State == WorldSessionState.Playing && !entering && player != null)
        {
            outposts?.Tick(player.position);
        }

        if (failureNeedsExit)
        {
            // 不在正在迭代的生成协程中重入StopCoroutine；下一帧走与正常退出相同的清理/UI链。
            failureNeedsExit = false;
            MainUIPanels.instance.CompletePlayEnd();
        }
    }

    private static Bounds ConstructBounds(Transform construct)
    {
        Bounds bounds = new Bounds(construct.position, Vector3.zero);
        bool found = false;
        foreach (Block block in construct.GetComponentsInChildren<Block>())
        {
            Vector3 half = new Vector3(block.x, block.y, block.z) * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                Vector3 point = block.transform.TransformPoint(Vector3.Scale(half, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f)));
                if (!found)
                {
                    bounds = new Bounds(point, Vector3.zero);
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(point);
                }
            }
        }

        return bounds;
    }

    private void SetBuildEnvironmentVisible(bool visible)
    {
        foreach (GameObject item in buildEnvironment)
        {
            if (item == null)
            {
                continue;
            }

            if (!visible)
            {
                if (!environmentStates.ContainsKey(item))
                {
                    environmentStates.Add(item, item.activeSelf);
                }

                item.SetActive(false);
            }
            else if (environmentStates.TryGetValue(item, out bool active))
            {
                item.SetActive(active);
            }
        }

        if (visible)
        {
            environmentStates.Clear();
        }
    }

    private void StopPreparation()
    {
        generation++;
        if (activationRoutine != null)
        {
            StopCoroutine(activationRoutine);
            activationRoutine = null;
        }

        FinishEntering();
        if (distanceRoutine != null)
        {
            StopCoroutine(distanceRoutine);
            distanceRoutine = null;
        }

        distanceView?.Dispose();
        distanceView = null;
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        playerGuard?.Release();
        if (sessionSettings != null)
        {
            Destroy(sessionSettings);
            sessionSettings = null;
        }
    }

    private void Fail(Exception error)
    {
        FinishEntering();
        outposts?.Clear();
        outposts = null;
        failureNeedsExit = State == WorldSessionState.Playing;
        playerGuard?.Release();
        State = WorldSessionState.Failed;
        Status = "World preparation failed: " + error.Message;
        buildings?.PrepareForUnload();
        worldRoot?.SetActive(false);
        SetBuildEnvironmentVisible(true);
        Debug.LogError(Status + "\n" + error);
    }

    private void OnDisable()
    {
        StopPreparation();
        outposts?.Clear();
        outposts = null;
        playRequested = false;
        loadingPanel?.Show(false);
        buildings?.PrepareForUnload();
        worldRoot?.SetActive(false);
        buildings?.ClearSession();
        world?.ClearStreaming();
        buildingStreaming = null;
        regionStreaming = null;
        State = WorldSessionState.Preparing;
        failureNeedsExit = false;
        SetBuildEnvironmentVisible(true);
    }
}

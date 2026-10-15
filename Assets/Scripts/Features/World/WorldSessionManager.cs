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
    private WorldRegionStreaming regionStreaming;
    private WorldGenerationSettings sessionSettings;
    private Coroutine routine;
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
    public int DormantBuildingCount => buildingStreaming?.DormantCount ?? 0;
    public ProceduralVoxelWorld World => world;
    public WorldBuildingManager Buildings => buildings;
    public WorldGenerationSettings Settings => sessionSettings != null ? sessionSettings : settings;

    private void Start()
    {
        started = true;
        loadingPanel?.Bind(this);
        BeginPreparation();
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
        SetBuildEnvironmentVisible(true);
        worldRoot?.SetActive(false);
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
            world.InitializeStreaming(Settings, SessionSeed);
            buildingStreaming = new WorldBuildingStreaming(world, buildings, Settings);
            regionStreaming = new WorldRegionStreaming(world, buildingStreaming, Settings);
            streamingFocus = world.SpawnPosition;
            routine = StartCoroutine(RunGuarded(generation));
        }
        catch (Exception error)
        {
            Fail(error);
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
        Vector3 delta = world.SpawnPosition + Vector3.up * 0.5f - bottom;
        construct.position += delta;
        if (CameraController.instance != null)
        {
            CameraController.instance.StopCameraMotion();
            CameraController.instance.transform.position += delta;
        }

        SetBuildEnvironmentVisible(false);
        State = WorldSessionState.Playing;
        worldRoot.SetActive(true);
        Physics.SyncTransforms();
        streamingFocus = construct.position;
        playerGuard = new WorldPlayerStreamingGuard(world, Settings);
        Status = "Exploring";
    }

    public void EndSession()
    {
        BeginPreparation();
    }

    private IEnumerator RunGuarded(int version)
    {
        IEnumerator work = StreamWorld();
        try
        {
            while (version == generation)
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
                Progress = Mathf.Clamp01((float)world.LoadedColumns.Count / total);
                Status = $"Preparing terrain and buildings... {world.LoadedColumns.Count} / {total}";
                if (!regionStreaming.LoadedWork)
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
        if (State != WorldSessionState.Playing)
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
        Status = playerGuard.IsWaiting ? "Loading nearby terrain..." : "Exploring";
        loadingPanel?.Show(playerGuard.IsWaiting);
    }

    private void Update()
    {
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
        failureNeedsExit = State == WorldSessionState.Playing;
        playerGuard?.Release();
        State = WorldSessionState.Failed;
        Status = "World preparation failed: " + error.Message;
        worldRoot?.SetActive(false);
        SetBuildEnvironmentVisible(true);
        Debug.LogError(Status + "\n" + error);
    }

    private void OnDisable()
    {
        StopPreparation();
        playRequested = false;
        loadingPanel?.Show(false);
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

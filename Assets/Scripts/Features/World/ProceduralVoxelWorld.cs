// 职责：拥有有限或流式体素数据、分块网格与受影响块的重建队列。
// 入口：Generate()保留WorldLab；InitializeStreaming/LoadColumn供游玩会话逐帧准备地形。
// 约束：根Transform保持单位缩放；运行修改属于本次会话，不写玩家蓝图存档。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ProceduralVoxelWorld : MonoBehaviour
{
    public event System.Action<Bounds> TerrainChanged;
    public event System.Action Regenerated;
    [SerializeField, Tooltip("由世界会话驱动分块加载；启用后 Awake 不生成有限 WorldLab。")]
    private bool streaming;
    [SerializeField]
    private int seed = 14721;
    [SerializeField]
    private Vector3Int dimensions = new Vector3Int(64, 24, 64);
    [SerializeField, Min(0.1f)]
    private float cellSize = 2f;
    [SerializeField, Range(4, 32)]
    private int chunkSize = 16;
    [SerializeField, Range(1, 16)]
    private int rebuildsPerFrame = 2;
    [SerializeField]
    private Material[] materials = new Material[3];
    [SerializeField]
    private VoxelWorldData data = new VoxelWorldData();
    private readonly Dictionary<Vector3Int, VoxelWorldChunk> chunks = new Dictionary<Vector3Int, VoxelWorldChunk>();
    private readonly HashSet<Vector3Int> dirty = new HashSet<Vector3Int>();
    private readonly Dictionary<Vector3Int, float> damage = new Dictionary<Vector3Int, float>();
    private readonly HashSet<Vector2Int> loadedColumns = new HashSet<Vector2Int>();
    private readonly Dictionary<Vector2Int, int> loadingColumns = new Dictionary<Vector2Int, int>();
    private StreamingWorldTerrain streamingTerrain;
    private int loadRevision;
    public IReadOnlyCollection<Vector2Int> LoadedColumns => loadedColumns;
    public bool IsStreaming => streaming;
    public Vector3 SpawnPosition => streamingTerrain == null ? transform.position : transform.TransformPoint(streamingTerrain.SpawnLocalPosition);
    public VoxelWorldData Data => data;
    public int Seed => seed;
    public float CellSize => cellSize;
    public Material[] Materials => materials;
    public int PendingChunks => dirty.Count;
    public int LastRebuiltChunks
    {
        get; private set;
    }
    public int RemovedCells
    {
        get; private set;
    }

    private void Awake()
    {
        CacheChunks();
        if (streaming)
        {
            return;
        }

        if (!data.IsValid)
        {
            Generate();
        }
        else
        {
            data.Recount();
        }
    }

    /// <summary>固定种子重建本组件直属的生成分块，保留其他子对象和用户资源。</summary>
    public void Generate(System.Action<GameObject> registerCreated = null, System.Action<GameObject> destroyObsolete = null)
    {
        if (streaming)
        {
            throw new InvalidOperationException("Streamed worlds must use InitializeStreaming and LoadColumn.");
        }

        if (Application.isPlaying && TryGetComponent(out VoxelStructureCollapse collapse))
        {
            collapse.ClearDebris();
        }

        dimensions = new Vector3Int(
            Mathf.Clamp(dimensions.x, 16, 128),
            Mathf.Clamp(dimensions.y, 16, 64),
            Mathf.Clamp(dimensions.z, 16, 128));
        cellSize = Mathf.Max(0.1f, cellSize);
        chunkSize = Mathf.Clamp(chunkSize, 4, 32);
        data.Generate(dimensions, seed);
        damage.Clear();
        dirty.Clear();
        RemovedCells = 0;
        CacheChunks();
        var required = new HashSet<Vector3Int>();
        for (int z = 0; z < dimensions.z; z += chunkSize)
        {
            for (int y = 0; y < dimensions.y; y += chunkSize)
            {
                for (int x = 0; x < dimensions.x; x += chunkSize)
                {
                    var start = new Vector3Int(x, y, z);
                    required.Add(start);
                    if (!chunks.TryGetValue(start, out VoxelWorldChunk chunk))
                    {
                        var obj = new GameObject("Chunk_" + start);
                        registerCreated?.Invoke(obj);
                        obj.transform.SetParent(transform, false);
                        chunk = obj.AddComponent<VoxelWorldChunk>();
                        chunk.Configure(start);
                        chunks.Add(start, chunk);
                    }

                    chunk.transform.localPosition = (Vector3)start * cellSize;
                    chunk.transform.localRotation = Quaternion.identity;
                    chunk.transform.localScale = Vector3.one;
                    chunk.Rebuild(data, chunkSize, cellSize, materials);
                }
            }
        }

        foreach (var entry in new List<KeyValuePair<Vector3Int, VoxelWorldChunk>>(chunks))
        {
            if (!required.Contains(entry.Key))
            {
                entry.Value.gameObject.SetActive(false);
                if (destroyObsolete != null)
                {
                    destroyObsolete(entry.Value.gameObject);
                }
                else if (Application.isPlaying)
                {
                    Destroy(entry.Value.gameObject);
                }
                else
                {
                    DestroyImmediate(entry.Value.gameObject);
                }

                chunks.Remove(entry.Key);
            }
        }

        if (Application.isPlaying)
        {
            Regenerated?.Invoke();
        }
    }

    /// <summary>开始全新会话；不生成全图，不保留上个会话的修改或未完成网格。</summary>
    public void InitializeStreaming(WorldGenerationSettings settings, int sessionSeed)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        ClearStreaming();
        streaming = true;
        seed = sessionSeed;
        chunkSize = settings.ChunkSize;
        cellSize = settings.CellSize;
        streamingTerrain = new StreamingWorldTerrain(settings, seed);
        data.InitializeStreaming(streamingTerrain);
        if (TryGetComponent(out VoxelStructureCollapse collapse))
        {
            collapse.ClearDebris();
            // WorldLab's full-volume support scan does not apply to streamed terrain.
            collapse.enabled = false;
        }
    }

    public bool HasColumn(Vector2Int coordinate)
    {
        return loadedColumns.Contains(coordinate);
    }

    public bool IsColumnInBounds(Vector2Int coordinate)
    {
        return streamingTerrain != null && streamingTerrain.IsColumnInBounds(coordinate);
    }

    public Vector2Int ColumnAt(Vector3 worldPosition)
    {
        Vector3Int cell = WorldToCell(worldPosition);
        return new Vector2Int(Mathf.FloorToInt((float)cell.x / chunkSize), Mathf.FloorToInt((float)cell.z / chunkSize));
    }

    public bool TryGetBuildingSite(Vector2Int coordinate, out Vector3 worldPosition, out Quaternion rotation)
    {
        worldPosition = Vector3.zero;
        rotation = transform.rotation;
        if (streamingTerrain == null || !streamingTerrain.TryGetBuildingSite(coordinate, out Vector3 localPosition, out Quaternion localRotation))
        {
            return false;
        }

        worldPosition = transform.TransformPoint(localPosition);
        rotation = transform.rotation * localRotation;
        return true;
    }

    /// <summary>每个立方网格后让出一帧；中断、卸载或新会话都移除未完成列的拥有网格。</summary>
    public IEnumerator LoadColumn(Vector2Int coordinate)
    {
        if (!IsColumnInBounds(coordinate) || HasColumn(coordinate) || loadingColumns.ContainsKey(coordinate))
        {
            yield break;
        }

        int revision = ++loadRevision;
        loadingColumns.Add(coordinate, revision);
        var created = new List<VoxelWorldChunk>();
        bool complete = false;
        try
        {
            for (int y = 0; y < data.Maximum.y; y += chunkSize)
            {
                if (!loadingColumns.TryGetValue(coordinate, out int current) || current != revision)
                {
                    yield break;
                }

                var start = new Vector3Int(coordinate.x * chunkSize, y, coordinate.y * chunkSize);
                var obj = new GameObject("StreamingChunk_" + start);
                obj.transform.SetParent(transform, false);
                obj.transform.localPosition = (Vector3)start * cellSize;
                VoxelWorldChunk chunk = obj.AddComponent<VoxelWorldChunk>();
                created.Add(chunk);
                chunk.Configure(start);
                chunks.Add(start, chunk);
                chunk.Rebuild(data, chunkSize, cellSize, materials);
                yield return null;
            }

            if (loadingColumns.TryGetValue(coordinate, out int active) && active == revision)
            {
                loadedColumns.Add(coordinate);
                complete = true;
            }
        }
        finally
        {
            if (loadingColumns.TryGetValue(coordinate, out int active) && active == revision)
            {
                loadingColumns.Remove(coordinate);
            }

            if (!complete)
            {
                foreach (VoxelWorldChunk chunk in created)
                {
                    if (chunk != null && chunks.TryGetValue(chunk.Start, out VoxelWorldChunk registered) && registered == chunk)
                    {
                        chunks.Remove(chunk.Start);
                        dirty.Remove(chunk.Start);
                        DestroyChunk(chunk);
                    }
                }
            }
        }
    }

    public void UnloadColumn(Vector2Int coordinate)
    {
        loadingColumns.Remove(coordinate);
        loadedColumns.Remove(coordinate);
        for (int y = 0; y < data.Maximum.y; y += chunkSize)
        {
            var start = new Vector3Int(coordinate.x * chunkSize, y, coordinate.y * chunkSize);
            dirty.Remove(start);
            if (chunks.TryGetValue(start, out VoxelWorldChunk chunk))
            {
                chunks.Remove(start);
                DestroyChunk(chunk);
            }
        }
    }

    public void ClearStreaming()
    {
        loadingColumns.Clear();
        loadedColumns.Clear();
        foreach (VoxelWorldChunk chunk in chunks.Values)
        {
            DestroyChunk(chunk);
        }

        chunks.Clear();
        dirty.Clear();
        damage.Clear();
        streamingTerrain = null;
        data = new VoxelWorldData();
        RemovedCells = 0;
        LastRebuiltChunks = 0;
    }

    private void DestroyChunk(VoxelWorldChunk chunk)
    {
        if (chunk == null)
        {
            return;
        }

        chunk.gameObject.SetActive(false);
        chunk.ClearOwnedMesh();
        if (Application.isPlaying)
        {
            Destroy(chunk.gameObject);
        }
        else
        {
            DestroyImmediate(chunk.gameObject);
        }
    }

    private void CacheChunks()
    {
        chunks.Clear();
        foreach (VoxelWorldChunk chunk in GetComponentsInChildren<VoxelWorldChunk>(true))
        {
            if (chunk.transform.parent == transform)
            {
                chunks[chunk.Start] = chunk;
            }
        }
    }

    public Vector3Int WorldToCell(Vector3 position)
    {
        return Vector3Int.FloorToInt(transform.InverseTransformPoint(position) / cellSize);
    }

    public Vector3 CellCenter(Vector3Int cell)
    {
        return transform.TransformPoint(((Vector3)cell + Vector3.one * 0.5f) * cellSize);
    }

    public bool SetCell(Vector3Int cell, byte material)
    {
        if (!data.Set(cell, material))
        {
            return false;
        }

        damage.Remove(cell);
        QueueCell(cell);
        TerrainChanged?.Invoke(new Bounds(CellCenter(cell), Vector3.one * (cellSize * transform.lossyScale.magnitude)));
        return true;
    }

    public VoxelWorldEdits CaptureEdits()
    {
        if (streaming)
        {
            throw new InvalidOperationException("Streamed session state cannot use WorldLab snapshots.");
        }

        return VoxelWorldEdits.Capture(data, seed);
    }

    public void RestoreEdits(VoxelWorldEdits edits)
    {
        if (streaming)
        {
            throw new InvalidOperationException("Streamed session state cannot use WorldLab snapshots.");
        }

        // Restore validates all cells into a new buffer before replacing live data.
        VoxelWorldData restored = edits.Restore(seed, data.Size);
        if (TryGetComponent(out VoxelStructureCollapse collapse))
        {
            collapse.ClearDebris();
        }

        data = restored;
        damage.Clear();
        RemovedCells = 0;
        foreach (Vector3Int start in chunks.Keys)
        {
            dirty.Add(start);
        }

        TerrainChanged?.Invoke(new Bounds(
            transform.TransformPoint((Vector3)data.Size * cellSize * 0.5f),
            Vector3.one * ((Vector3)data.Size * cellSize).magnitude));
    }

    /// <summary>半径内按距离衰减积累伤害，土/岩/金属分别需要20/60/100；不会创建刚体碎片。</summary>
    public int DamageSphere(Vector3 center, float radius, float energy)
    {
        if (!data.IsValid || radius <= 0f || energy <= 0f || float.IsNaN(radius) || float.IsInfinity(radius) || float.IsNaN(energy))
        {
            return 0;
        }

        Vector3 scale = transform.lossyScale;
        float minimumScale = Mathf.Min(Mathf.Abs(scale.x), Mathf.Min(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        if (minimumScale < 0.0001f)
        {
            return 0;
        }

        // A local-space conservative sphere bound remains valid for rotated/nonuniformly scaled roots.
        Vector3 localCenter = transform.InverseTransformPoint(center) / cellSize;
        Vector3 extent = Vector3.one * (radius / (minimumScale * cellSize));
        Vector3Int minimum = Vector3Int.Max(data.Minimum, Vector3Int.FloorToInt(localCenter - extent));
        Vector3Int maximum = Vector3Int.Min(data.Maximum - Vector3Int.one, Vector3Int.FloorToInt(localCenter + extent));
        int removed = 0;
        for (int z = minimum.z; z <= maximum.z; z++)
        {
            for (int y = minimum.y; y <= maximum.y; y++)
            {
                for (int x = minimum.x; x <= maximum.x; x++)
                {
                    var cell = new Vector3Int(x, y, z);
                    byte material = data.Get(cell);
                    float distance = Vector3.Distance(CellCenter(cell), center);
                    if (material == 0 || distance > radius)
                    {
                        continue;
                    }

                    damage.TryGetValue(cell, out float previous);
                    float value = previous + energy * Mathf.Max(0.15f, 1f - distance / radius);
                    float hardness = material == 1 ? 20f : material == 2 ? 60f : 100f;
                    if (value >= hardness)
                    {
                        SetCell(cell, 0);
                        removed++;
                    }
                    else
                    {
                        damage[cell] = value;
                    }
                }
            }
        }

        RemovedCells += removed;
        if (!streaming && removed > 0 && TryGetComponent(out VoxelStructureCollapse collapse))
        {
            collapse.RequestCheck();
        }

        return removed;
    }

    private void QueueCell(Vector3Int cell)
    {
        QueueChunk(cell);
        QueueChunk(cell + Vector3Int.right);
        QueueChunk(cell + Vector3Int.left);
        QueueChunk(cell + Vector3Int.up);
        QueueChunk(cell + Vector3Int.down);
        QueueChunk(cell + Vector3Int.forward);
        QueueChunk(cell + Vector3Int.back);
    }

    private void QueueChunk(Vector3Int cell)
    {
        if (!data.Contains(cell))
        {
            return;
        }

        var start = new Vector3Int(
            Mathf.FloorToInt((float)cell.x / chunkSize),
            Mathf.FloorToInt((float)cell.y / chunkSize),
            Mathf.FloorToInt((float)cell.z / chunkSize)) * chunkSize;
        if (chunks.ContainsKey(start))
        {
            dirty.Add(start);
        }
    }

    private void LateUpdate()
    {
        FlushDirty(rebuildsPerFrame);
    }

    public void FlushDirty(int budget = int.MaxValue)
    {
        LastRebuiltChunks = 0;
        while (dirty.Count > 0 && LastRebuiltChunks < budget)
        {
            var iterator = dirty.GetEnumerator();
            iterator.MoveNext();
            Vector3Int start = iterator.Current;
            iterator.Dispose();
            dirty.Remove(start);
            if (chunks.TryGetValue(start, out VoxelWorldChunk chunk) && chunk != null)
            {
                chunk.Rebuild(data, chunkSize, cellSize, materials);
                LastRebuiltChunks++;
            }
        }
    }
}

// 职责：拥有可编辑体素数据、分块网格与受影响块的重建队列。
// 入口：Generate() / DamageSphere() / SetCell()；LateUpdate分预算刷新碰撞和显示。
// 约束：根Transform保持单位缩放；运行修改属于本次会话，不写玩家蓝图存档。
using System.Collections.Generic;
using UnityEngine;

public sealed class ProceduralVoxelWorld : MonoBehaviour
{
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
    public VoxelWorldData Data => data;
    public int Seed => seed;
    public float CellSize => cellSize;
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
        return true;
    }

    public VoxelWorldEdits CaptureEdits()
    {
        return VoxelWorldEdits.Capture(data, seed);
    }

    public void RestoreEdits(VoxelWorldEdits edits)
    {
        // Restore validates all cells into a new buffer before replacing live data.
        VoxelWorldData restored = edits.Restore(seed, data.Size);
        data = restored;
        damage.Clear();
        RemovedCells = 0;
        foreach (Vector3Int start in chunks.Keys)
        {
            dirty.Add(start);
        }
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
        Vector3Int minimum = Vector3Int.Max(Vector3Int.zero, Vector3Int.FloorToInt(localCenter - extent));
        Vector3Int maximum = Vector3Int.Min(data.Size - Vector3Int.one, Vector3Int.FloorToInt(localCenter + extent));
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

        var start = new Vector3Int(cell.x / chunkSize, cell.y / chunkSize, cell.z / chunkSize) * chunkSize;
        dirty.Add(start);
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

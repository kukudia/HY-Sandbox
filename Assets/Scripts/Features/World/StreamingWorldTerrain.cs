// 职责：按全局单元坐标确定性采样大世界地形、出生平台和建筑平台，无场景对象或全图数组。
// 入口：Sample() / TryGetBuildingSite()；高度缓存有界，区块卸载不改变同一种子的基线。
using System.Collections.Generic;
using UnityEngine;

public sealed class StreamingWorldTerrain
{
    private const int MaximumCachedHeights = 32768;
    private readonly int chunkSize;
    private readonly float cellSize;
    private readonly int worldHeight;
    private readonly int worldRadiusInChunks;
    private readonly float buildingChance;
    private readonly int buildingPadHalfSize;
    private readonly int spawnClearRadius;
    private readonly int seed;
    private readonly Vector2 noiseOffset;
    private readonly Dictionary<Vector2Int, int> heights = new Dictionary<Vector2Int, int>();
    private readonly Queue<Vector2Int> heightOrder = new Queue<Vector2Int>();
    public Vector3Int Minimum => new Vector3Int(-worldRadiusInChunks * chunkSize, 0, -worldRadiusInChunks * chunkSize);
    public Vector3Int Maximum => new Vector3Int((worldRadiusInChunks + 1) * chunkSize, worldHeight, (worldRadiusInChunks + 1) * chunkSize);
    public Vector3 SpawnLocalPosition => new Vector3(chunkSize * 0.5f, Height(chunkSize / 2, chunkSize / 2) + 1f, chunkSize * 0.5f) * cellSize;

    public float SurfaceHeight(int x, int z) => (Height(x, z) + 1) * cellSize;
    public StreamingWorldTerrain(WorldGenerationSettings configuration, int sessionSeed)
    {
        // Keep a session's baseline stable when its authoring asset is edited in Play Mode.
        chunkSize = configuration.ChunkSize;
        cellSize = configuration.CellSize;
        worldHeight = configuration.WorldHeight;
        worldRadiusInChunks = configuration.WorldRadiusInChunks;
        buildingChance = configuration.BuildingChance;
        buildingPadHalfSize = configuration.BuildingPadHalfSize;
        spawnClearRadius = configuration.SpawnClearRadius;
        seed = sessionSeed;
        var random = new System.Random(seed);
        noiseOffset = new Vector2(random.Next(100, 10000), random.Next(100, 10000));
    }

    public bool IsColumnInBounds(Vector2Int coordinate)
    {
        int radius = worldRadiusInChunks;
        return coordinate.x >= -radius && coordinate.x <= radius && coordinate.y >= -radius && coordinate.y <= radius;
    }

    public byte Sample(Vector3Int cell)
    {
        int height = Height(cell.x, cell.z);
        if (cell.y > height)
        {
            return 0;
        }

        return cell.y >= height - 1 ? (byte)1 : (byte)2;
    }

    public bool TryGetBuildingSite(Vector2Int coordinate, out Vector3 localPosition, out Quaternion rotation)
    {
        localPosition = Vector3.zero;
        rotation = Quaternion.identity;
        if (!HasBuilding(coordinate))
        {
            return false;
        }

        int x = coordinate.x * chunkSize + chunkSize / 2;
        int z = coordinate.y * chunkSize + chunkSize / 2;
        localPosition = new Vector3(x, Height(x, z) + 1f, z) * cellSize;
        rotation = Quaternion.Euler(0f, (Hash(coordinate) % 4u) * 90f, 0f);
        return true;
    }

    private bool HasBuilding(Vector2Int coordinate)
    {
        int spacing = Mathf.Max(1, Mathf.CeilToInt((buildingPadHalfSize * 2f + 4f) / chunkSize));
        if (coordinate.x % spacing != 0 || coordinate.y % spacing != 0)
        {
            return false;
        }

        if (!IsColumnInBounds(coordinate) || Mathf.Max(Mathf.Abs(coordinate.x), Mathf.Abs(coordinate.y)) <= spawnClearRadius)
        {
            return false;
        }

        return (Hash(coordinate) & 0x00ffffffu) / 16777216f < buildingChance;
    }

    private int Height(int x, int z)
    {
        var key = new Vector2Int(x, z);
        if (heights.TryGetValue(key, out int height))
        {
            return height;
        }

        float value = RawHeight(x, z);
        int spacing = Mathf.Max(1, Mathf.CeilToInt((buildingPadHalfSize * 2f + 4f) / chunkSize));
        var column = new Vector2Int(
            Mathf.RoundToInt(((x + 0.5f) / chunkSize - 0.5f) / spacing) * spacing,
            Mathf.RoundToInt(((z + 0.5f) / chunkSize - 0.5f) / spacing) * spacing);
        if (HasBuilding(column))
        {
            int centerX = column.x * chunkSize + chunkSize / 2;
            int centerZ = column.y * chunkSize + chunkSize / 2;
            // Distances use cell centers, keeping either side of a platform edge symmetric.
            float distance = Mathf.Max(Mathf.Abs(x + 0.5f - centerX), Mathf.Abs(z + 0.5f - centerZ));
            int halfSize = buildingPadHalfSize;
            float blend = Mathf.Clamp01((distance - halfSize) / 2f);
            value = Mathf.Lerp(Mathf.RoundToInt(RawHeight(centerX, centerZ)), value, blend);
        }

        height = Mathf.Clamp(Mathf.RoundToInt(value), 2, worldHeight - 5);
        if (heights.Count >= MaximumCachedHeights)
        {
            heights.Remove(heightOrder.Dequeue());
        }

        heights.Add(key, height);
        heightOrder.Enqueue(key);
        return height;
    }

    private float RawHeight(int x, int z)
    {
        float broad = Mathf.PerlinNoise(noiseOffset.x + x * 0.025f, noiseOffset.y + z * 0.025f);
        float fine = Mathf.PerlinNoise(noiseOffset.x + x * 0.11f, noiseOffset.y + z * 0.11f);
        return 3f + broad * (worldHeight * 0.45f) + fine * 2f;
    }

    private uint Hash(Vector2Int coordinate)
    {
        unchecked
        {
            uint value = (uint)seed ^ ((uint)coordinate.x * 0x9e3779b9u) ^ ((uint)coordinate.y * 0x85ebca6bu);
            value ^= value >> 16;
            value *= 0x7feb352du;
            value ^= value >> 15;
            value *= 0x846ca68bu;
            return value ^ (value >> 16);
        }
    }
}

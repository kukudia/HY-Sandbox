// 职责：有限世界保存体素数组，大世界按需采样并仅保存本次会话的稀疏单元修改。
// 入口：Generate() / Set()；无场景或存档副作用，0为空气、1土、2岩、3金属。
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VoxelWorldData
{
    [SerializeField]
    private Vector3Int size;
    [SerializeField]
    private byte[] cells = Array.Empty<byte>();
    [NonSerialized]
    private StreamingWorldTerrain streamingTerrain;
    [NonSerialized]
    private Dictionary<Vector3Int, byte> streamingEdits;
    public Vector3Int Size => size;
    public Vector3Int Minimum => streamingTerrain != null ? streamingTerrain.Minimum : Vector3Int.zero;
    public Vector3Int Maximum => streamingTerrain != null ? streamingTerrain.Maximum : size;
    public bool IsStreaming => streamingTerrain != null;
    public int Revision
    {
        get; private set;
    }
    public int SolidCount
    {
        get; private set;
    }
    public bool IsValid => IsStreaming || (cells != null && cells.LongLength == (long)size.x * size.y * size.z && cells.Length > 0);

    public byte Get(Vector3Int cell)
    {
        if (!Contains(cell))
        {
            return 0;
        }

        if (IsStreaming)
        {
            return streamingEdits.TryGetValue(cell, out byte edited) ? edited : streamingTerrain.Sample(cell);
        }

        return cells[(cell.z * size.y + cell.y) * size.x + cell.x];
    }

    public bool Contains(Vector3Int cell)
    {
        Vector3Int minimum = Minimum;
        Vector3Int maximum = Maximum;
        return cell.x >= minimum.x && cell.y >= minimum.y && cell.z >= minimum.z && cell.x < maximum.x && cell.y < maximum.y && cell.z < maximum.z;
    }

    public bool Set(Vector3Int cell, byte material)
    {
        if (!Contains(cell) || material > 3)
        {
            return false;
        }

        byte previous = Get(cell);
        if (previous == material)
        {
            return false;
        }

        Revision++;
        if (IsStreaming)
        {
            if (material == streamingTerrain.Sample(cell))
            {
                streamingEdits.Remove(cell);
            }
            else
            {
                streamingEdits[cell] = material;
            }

            return true;
        }

        int index = (cell.z * size.y + cell.y) * size.x + cell.x;
        cells[index] = material;
        SolidCount += (material == 0 ? 0 : 1) - (previous == 0 ? 0 : 1);
        return true;
    }

    public void Recount()
    {
        if (IsStreaming)
        {
            return;
        }

        SolidCount = 0;
        foreach (byte cell in cells)
        {
            if (cell != 0)
            {
                SolidCount++;
            }
        }
    }

    public void InitializeEmpty(Vector3Int dimensions)
    {
        if (dimensions.x < 1 || dimensions.y < 1 || dimensions.z < 1 || dimensions.x > 128 || dimensions.y > 64 || dimensions.z > 128)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions));
        }

        streamingTerrain = null;
        streamingEdits = null;
        size = dimensions;
        cells = new byte[size.x * size.y * size.z];
        SolidCount = 0;
    }

    public void Generate(Vector3Int dimensions, int seed)
    {
        if (dimensions.x < 16 || dimensions.z < 16 || dimensions.y < 16 || dimensions.x > 128 || dimensions.z > 128 || dimensions.y > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Voxel world dimensions must be 16..128 by 16..64 by 16..128.");
        }

        streamingTerrain = null;
        streamingEdits = null;
        size = dimensions;
        cells = new byte[size.x * size.y * size.z];
        SolidCount = 0;
        var random = new System.Random(seed);
        float offsetX = random.Next(100, 10000);
        float offsetZ = random.Next(100, 10000);
        for (int z = 0; z < size.z; z++)
        {
            for (int x = 0; x < size.x; x++)
            {
                float broad = Mathf.PerlinNoise(offsetX + x * 0.045f, offsetZ + z * 0.045f);
                float fine = Mathf.PerlinNoise(offsetX + x * 0.16f, offsetZ + z * 0.16f);
                int height = Mathf.Clamp(3 + Mathf.RoundToInt(broad * 9f + fine * 2f), 2, size.y - 7);
                for (int y = 0; y <= height; y++)
                {
                    Set(new Vector3Int(x, y, z), y >= height - 1 ? (byte)1 : (byte)2);
                }
            }
        }

        // Sparse ruins create landmarks without one GameObject or collider per voxel.
        for (int i = 0; i < 6; i++)
        {
            int cx = random.Next(6, size.x - 6);
            int cz = random.Next(6, size.z - 6);
            int ground = size.y - 1;
            while (ground > 0 && Get(new Vector3Int(cx, ground, cz)) == 0)
            {
                ground--;
            }

            for (int y = ground + 1; y < Mathf.Min(size.y, ground + 7); y++)
            {
                for (int x = cx - 3; x <= cx + 3; x++)
                {
                    for (int z = cz - 3; z <= cz + 3; z++)
                    {
                        bool wall = x == cx - 3 || x == cx + 3 || z == cz - 3 || z == cz + 3;
                        bool doorway = z == cz - 3 && Mathf.Abs(x - cx) <= 1 && y < ground + 4;
                        if (wall && !doorway && (y < ground + 5 || random.NextDouble() > 0.4))
                        {
                            Set(new Vector3Int(x, y, z), 3);
                        }
                    }
                }
            }
        }
    }

    public void InitializeStreaming(StreamingWorldTerrain terrain)
    {
        streamingTerrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        streamingEdits = new Dictionary<Vector3Int, byte>();
        size = terrain.Maximum - terrain.Minimum;
        cells = Array.Empty<byte>();
        // An entire procedural world's solid count is deliberately not scanned.
        SolidCount = 0;
    }

    public uint Fingerprint()
    {
        if (IsStreaming)
        {
            throw new InvalidOperationException("A streamed session has no finite WorldLab fingerprint.");
        }

        uint hash = 2166136261;
        foreach (byte value in cells)
        {
            hash = unchecked((hash ^ value) * 16777619);
        }

        return hash;
    }
}

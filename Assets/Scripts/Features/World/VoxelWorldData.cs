// 职责：固定种子生成体素地形与遗迹，保存单元材质和采掘修改。
// 入口：Generate() / Set()；无场景或存档副作用，0为空气、1土、2岩、3金属。
using System;
using UnityEngine;

[Serializable]
public sealed class VoxelWorldData
{
    [SerializeField]
    private Vector3Int size;
    [SerializeField]
    private byte[] cells = Array.Empty<byte>();
    public Vector3Int Size => size;
    public int SolidCount
    {
        get; private set;
    }
    public bool IsValid => cells != null && cells.Length == size.x * size.y * size.z && cells.Length > 0;

    public byte Get(Vector3Int cell)
    {
        if (!Contains(cell))
        {
            return 0;
        }

        return cells[(cell.z * size.y + cell.y) * size.x + cell.x];
    }

    public bool Contains(Vector3Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.z >= 0 && cell.x < size.x && cell.y < size.y && cell.z < size.z;
    }

    public bool Set(Vector3Int cell, byte material)
    {
        if (!Contains(cell) || material > 3)
        {
            return false;
        }

        int index = (cell.z * size.y + cell.y) * size.x + cell.x;
        byte previous = cells[index];
        if (previous == material)
        {
            return false;
        }

        cells[index] = material;
        SolidCount += (material == 0 ? 0 : 1) - (previous == 0 ? 0 : 1);
        return true;
    }

    public void Recount()
    {
        SolidCount = 0;
        foreach (byte cell in cells)
        {
            if (cell != 0)
            {
                SolidCount++;
            }
        }
    }

    public void Generate(Vector3Int dimensions, int seed)
    {
        if (dimensions.x < 16 || dimensions.z < 16 || dimensions.y < 16 || dimensions.x > 128 || dimensions.z > 128 || dimensions.y > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(dimensions), "Voxel world dimensions must be 16..128 by 16..64 by 16..128.");
        }

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

    public uint Fingerprint()
    {
        uint hash = 2166136261;
        foreach (byte value in cells)
        {
            hash = unchecked((hash ^ value) * 16777619);
        }

        return hash;
    }
}

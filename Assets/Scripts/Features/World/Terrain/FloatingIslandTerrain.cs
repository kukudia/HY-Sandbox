// 职责：生成有空气边界、平整中心与收尖岩底的有限浮空岛体素数据。
// 入口：Generate()；共享World体素与材质编号，不创建场景对象或修改随机全局状态。
using System;
using UnityEngine;

public static class FloatingIslandTerrain
{
    public static VoxelWorldData Generate(Vector3Int size, int seed, int surface, Vector2 flatHalfSize, int depth)
    {
        if (surface < 4 || surface > size.y - 4 || depth < 2 || depth > surface - 2 || flatHalfSize.x > size.x * .5f - 4 || flatHalfSize.y > size.z * .5f - 4)
        {
            throw new ArgumentException("Island plateau and depth must fit within an air-bordered voxel volume.");
        }

        var data = new VoxelWorldData();
        data.InitializeEmpty(size);
        var random = new System.Random(seed);
        float ox = random.Next(100, 10000);
        float oz = random.Next(100, 10000);
        Vector2 half = new Vector2(size.x * .5f - 2, size.z * .5f - 2);
        for (int x = 1; x < size.x - 1; x++)
        {
            for (int z = 1; z < size.z - 1; z++)
            {
                float dx = Mathf.Abs(x + .5f - size.x * .5f);
                float dz = Mathf.Abs(z + .5f - size.z * .5f);
                float edge = Mathf.Max(
                    (dx - flatHalfSize.x) / (half.x - flatHalfSize.x),
                    (dz - flatHalfSize.y) / (half.y - flatHalfSize.y));
                float noise = Mathf.PerlinNoise(ox + x * .13f, oz + z * .13f);
                if (edge > .88f + noise * .12f)
                {
                    continue;
                }

                float radial = Mathf.Max(dx / half.x, dz / half.y);
                int thickness = Mathf.Max(2, Mathf.RoundToInt(depth * Mathf.Sqrt(Mathf.Max(0, 1f - radial * radial)) + noise * 2f - 1f));
                int top = surface - 1;
                if (edge > 0)
                {
                    top += Mathf.RoundToInt((noise - .5f) * Mathf.Clamp01(edge * 2) * 4);
                }

                int bottom = Mathf.Max(1, top - thickness + 1);
                for (int y = bottom; y <= top; y++)
                {
                    data.Set(new Vector3Int(x, y, z), y >= top - 1 ? (byte)1 : (byte)2);
                }
            }
        }

        return data;
    }
}

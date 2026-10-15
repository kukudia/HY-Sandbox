// 职责：按距离选择下一个地形列，先休眠建筑再卸载远处网格，并在列完成后装入建筑。
// 入口：LoadNext()；由WorldSessionManager的可取消协程逐帧驱动，无模式/UI副作用。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class WorldRegionStreaming
{
    private readonly ProceduralVoxelWorld world;
    private readonly WorldBuildingStreaming buildings;
    private readonly WorldGenerationSettings settings;
    private readonly List<Vector2Int> retiring = new List<Vector2Int>();
    internal bool LoadedWork
    {
        get; private set;
    }

    internal WorldRegionStreaming(
        ProceduralVoxelWorld terrain,
        WorldBuildingStreaming structures,
        WorldGenerationSettings configuration)
    {
        world = terrain;
        buildings = structures;
        settings = configuration;
    }

    internal IEnumerator LoadNext(Vector2Int center)
    {
        RetireDistant(center);
        LoadedWork = FindNextColumn(center, out Vector2Int coordinate);
        if (LoadedWork)
        {
            IEnumerator column = world.LoadColumn(coordinate);
            try
            {
                while (column.MoveNext())
                {
                    yield return column.Current;
                }
            }
            finally
            {
                (column as IDisposable)?.Dispose();
            }

            buildings.LoadColumn(coordinate);
        }

        IEnumerator structures = buildings.RestoreAvailable();
        try
        {
            while (structures.MoveNext())
            {
                yield return structures.Current;
            }
        }
        finally
        {
            (structures as IDisposable)?.Dispose();
        }
    }

    private bool FindNextColumn(Vector2Int center, out Vector2Int coordinate)
    {
        for (int ring = 0; ring <= settings.LoadRadius; ring++)
        {
            for (int z = -ring; z <= ring; z++)
            {
                for (int x = -ring; x <= ring; x++)
                {
                    if (Mathf.Max(Mathf.Abs(x), Mathf.Abs(z)) != ring)
                    {
                        continue;
                    }

                    coordinate = center + new Vector2Int(x, z);
                    if (world.IsColumnInBounds(coordinate) && !world.HasColumn(coordinate))
                    {
                        return true;
                    }
                }
            }
        }

        coordinate = default;
        return false;
    }

    private void RetireDistant(Vector2Int center)
    {
        buildings.UnloadDistant(center);
        retiring.Clear();
        foreach (Vector2Int column in world.LoadedColumns)
        {
            if (WorldBuildingStreaming.Distance(column, center) > settings.UnloadRadius)
            {
                retiring.Add(column);
            }
        }

        foreach (Vector2Int column in retiring)
        {
            world.UnloadColumn(column);
        }
    }
}

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
    private Vector2Int lastCenter;
    private int observedVersion = -1;
    private float nextMaintenanceTime;
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
        LoadedWork = false;
        // 静止且已满载时不每帧扫描全部建筑/休眠快照；跨列、地形加载变化立即唤醒。
        if (center == lastCenter && observedVersion == world.ColumnVersion && Time.realtimeSinceStartup < nextMaintenanceTime)
        {
            yield break;
        }

        lastCenter = center;
        IEnumerator retirement = RetireDistant(center);
        try
        {
            while (retirement.MoveNext())
            {
                yield return retirement.Current;
            }
        }
        finally
        {
            (retirement as IDisposable)?.Dispose();
        }

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

        // Terrain nearest the player wins over restoring far structures in the same ring.
        // Otherwise a large blueprint can delay collision-ready columns during travel.
        if (LoadedWork && WorldBuildingStreaming.Distance(coordinate, center) <= 2)
        {
            observedVersion = world.ColumnVersion;
            nextMaintenanceTime = 0f;
            yield break;
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

        observedVersion = world.ColumnVersion;
        nextMaintenanceTime = LoadedWork ? 0f : Time.realtimeSinceStartup + .2f;
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

    private IEnumerator RetireDistant(Vector2Int center)
    {
        IEnumerator structures = buildings.UnloadDistantRoutine(center);
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
            yield return null;
        }
    }
}

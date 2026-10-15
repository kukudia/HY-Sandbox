// 职责：按世界区块生成建筑，将远处建筑休眠为快照，并保留本次探索中的损坏和货仓。
// 入口：LoadColumn() / UnloadDistantRoutine()；实例归WorldBuildingManager，缓存随会话退出丢弃。
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

internal sealed class WorldBuildingStreaming
{
    private readonly ProceduralVoxelWorld world;
    private readonly WorldBuildingManager manager;
    private readonly WorldGenerationSettings settings;
    private readonly List<BlockDataList> templates = new List<BlockDataList>();
    private readonly List<string> names = new List<string>();
    private readonly HashSet<Vector2Int> generated = new HashSet<Vector2Int>();
    private readonly Dictionary<string, DormantBuilding> dormant = new Dictionary<string, DormantBuilding>();
    private readonly List<string> restored = new List<string>();
    private readonly List<BuildingRuntime> retiring = new List<BuildingRuntime>();
    public int DormantCount => dormant.Count;

    internal WorldBuildingStreaming(
        ProceduralVoxelWorld terrain,
        WorldBuildingManager buildings,
        WorldGenerationSettings configuration,
        WorldBuildingLibrary library)
    {
        world = terrain;
        manager = buildings;
        settings = configuration;
        templates.AddRange(library.Templates);
        names.AddRange(library.Names);
    }

    internal void LoadColumn(Vector2Int coordinate)
    {
        if (templates.Count > 0 && generated.Add(coordinate) && world.TryGetBuildingSite(coordinate, out Vector3 position, out Quaternion rotation))
        {
            uint hash = unchecked((uint)(world.Seed * 73856093) ^ (uint)(coordinate.x * 19349663) ^ (uint)(coordinate.y * 83492791));
            int index = (int)(hash % (uint)templates.Count);
            BlockDataList blueprint = templates[index];
            var health = new float[blueprint.blocks.Count];
            for (int i = 0; i < health.Length; i++)
            {
                health[i] = float.MaxValue;
            }

            // 平台与落点一起由相同种子生成；此时旧建造场景仍活动，不能用其碰撞体拒绝生成。
            var state = new WorldBuildingState
            {
                instanceId = $"world-{world.Seed}-{coordinate.x}-{coordinate.y}",
                blueprintName = names[index],
                faction = UnitFaction.Enemy,
                position = position,
                rotation = rotation,
                blueprint = blueprint,
                durability = health
            };
            dormant.Add(state.instanceId, new DormantBuilding { State = state });
        }
    }

    internal IEnumerator RestoreAvailable()
    {
        restored.Clear();
        foreach (KeyValuePair<string, DormantBuilding> entry in dormant)
        {
            WorldBuildingState state = entry.Value.State;
            var size = new Vector2Int(state.blueprint.foundation.width, state.blueprint.foundation.length);
            if (!HasLoadedGround(state.position, state.rotation, size) || !manager.HasCapacity(state.blueprint.blocks.Count))
            {
                continue;
            }

            DormantBuilding snapshot = entry.Value;
            IEnumerator work = manager.RestoreBuildingRoutine(
                snapshot.State,
                building =>
            {
                Rigidbody body = building.GetComponent<Rigidbody>();
                if (!body.isKinematic)
                {
                    body.linearVelocity = snapshot.Velocity;
                    body.angularVelocity = snapshot.AngularVelocity;
                }
            });
            try
            {
                while (work.MoveNext())
                {
                    yield return work.Current;
                }
            }
            finally
            {
                (work as IDisposable)?.Dispose();
            }

            restored.Add(entry.Key);
        }

        foreach (string id in restored)
        {
            dormant.Remove(id);
        }
    }

    internal IEnumerator UnloadDistantRoutine(Vector2Int center)
    {
        retiring.Clear();
        foreach (BuildingRuntime building in manager.Buildings)
        {
            if (building != null && (Distance(world.ColumnAt(building.transform.position), center) > settings.UnloadRadius || !HasLoadedGround(building.transform.position, building.transform.rotation, building.FootprintSize)))
            {
                retiring.Add(building);
            }
        }

        var timer = System.Diagnostics.Stopwatch.StartNew();
        foreach (BuildingRuntime building in retiring)
        {
            // 跨帧期间战斗可能已经摧毁候选；会话取消则由拥有该协程的管理器整体释放。
            if (building == null)
            {
                continue;
            }

            WorldBuildingState state = building.Capture();
            if (state.blueprint.blocks.Count > 0)
            {
                Rigidbody body = building.GetComponent<Rigidbody>();
                dormant[building.InstanceId] = new DormantBuilding
                {
                    State = state,
                    Velocity = body.isKinematic ? Vector3.zero : body.linearVelocity,
                    AngularVelocity = body.isKinematic ? Vector3.zero : body.angularVelocity
                };
            }

            manager.RemoveBuilding(building);
            if (timer.Elapsed.TotalMilliseconds >= settings.LoadingFrameBudgetMilliseconds)
            {
                yield return null;
                timer.Restart();
            }
        }
    }

    internal static int Distance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    private bool HasLoadedGround(Vector3 position, Quaternion rotation, Vector2Int size)
    {
        Vector2Int minimum = world.ColumnAt(position);
        Vector2Int maximum = minimum;
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = position + rotation * new Vector3((i & 1) == 0 ? -2.05f : size.x - 1.95f, 0f, (i & 2) == 0 ? -2.05f : size.y - 1.95f);
            Vector2Int column = world.ColumnAt(corner);
            minimum = Vector2Int.Min(minimum, column);
            maximum = Vector2Int.Max(maximum, column);
        }

        for (int z = minimum.y; z <= maximum.y; z++)
        {
            for (int x = minimum.x; x <= maximum.x; x++)
            {
                if (!world.HasColumn(new Vector2Int(x, z)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class DormantBuilding
    {
        internal WorldBuildingState State;
        internal Vector3 Velocity;
        internal Vector3 AngularVelocity;
    }
}

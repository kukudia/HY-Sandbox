// 职责：按世界区块生成建筑，将远处建筑休眠为快照，并保留本次探索中的损坏和货仓。
// 入口：LoadColumn() / UnloadDistant()；实例归WorldBuildingManager，缓存随WorldSessionManager退出丢弃。
using System;
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
        WorldGenerationSettings configuration)
    {
        world = terrain;
        manager = buildings;
        settings = configuration;
        LoadTemplates();
    }

    private void LoadTemplates()
    {
        if (manager.Catalog == null || manager.Catalog.blueprints == null)
        {
            throw new InvalidOperationException("World building catalog is missing.");
        }

        float padRadius = settings.BuildingPadHalfSize * settings.CellSize - settings.CellSize;
        foreach (TextAsset asset in manager.Catalog.blueprints)
        {
            if (asset == null)
            {
                throw new InvalidOperationException("World building catalog contains a missing blueprint.");
            }

            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(asset.text);
            WorldBuildingValidation.ValidateBlueprint(blueprint);
            if (blueprint.blocks.Count == 0 || blueprint.foundation.width > padRadius || blueprint.foundation.length > padRadius)
            {
                throw new InvalidOperationException("Building does not fit its generated platform: " + asset.name);
            }

            foreach (BlockData block in blueprint.blocks)
            {
                Vector3 half = new Vector3(block.x, block.y, block.z) * 0.5f;
                Quaternion rotation = new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = rotation * Vector3.Scale(half, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    corner += new Vector3(block.posX, block.posY, block.posZ);
                    if (Mathf.Abs(corner.x) > padRadius || Mathf.Abs(corner.z) > padRadius || corner.y < -0.01f)
                    {
                        throw new InvalidOperationException("Building extends outside its generated platform: " + asset.name);
                    }
                }
            }

            templates.Add(blueprint);
            names.Add(asset.name);
        }

        if (templates.Count == 0)
        {
            throw new InvalidOperationException("World generation needs at least one building blueprint.");
        }
    }

    internal void LoadColumn(Vector2Int coordinate)
    {
        if (generated.Add(coordinate) && world.TryGetBuildingSite(coordinate, out Vector3 position, out Quaternion rotation))
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

        RestoreAvailable();
    }

    internal void RestoreAvailable()
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

            BuildingRuntime building = manager.RestoreBuilding(entry.Value.State);
            Rigidbody body = building.GetComponent<Rigidbody>();
            if (!body.isKinematic)
            {
                body.linearVelocity = entry.Value.Velocity;
                body.angularVelocity = entry.Value.AngularVelocity;
            }

            restored.Add(entry.Key);
        }

        foreach (string id in restored)
        {
            dormant.Remove(id);
        }
    }

    internal void UnloadDistant(Vector2Int center)
    {
        retiring.Clear();
        foreach (BuildingRuntime building in manager.Buildings)
        {
            if (building != null && (Distance(world.ColumnAt(building.transform.position), center) > settings.UnloadRadius || !HasLoadedGround(building.transform.position, building.transform.rotation, building.FootprintSize)))
            {
                retiring.Add(building);
            }
        }

        foreach (BuildingRuntime building in retiring)
        {
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

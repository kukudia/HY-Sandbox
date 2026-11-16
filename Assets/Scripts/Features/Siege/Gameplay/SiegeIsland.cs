// 职责：将基地占地转换为固定有限浮空岛，提供地面、敌人入口和坠落/远离边界。
// 入口：GenerateFor() / SpawnPosition() / BeyondRecovery()；复用ProceduralVoxelWorld的破坏和网格。
using System;
using UnityEngine;

public sealed class SiegeIsland : MonoBehaviour
{
    [SerializeField]
    private ProceduralVoxelWorld world;
    [SerializeField]
    private SiegeIslandSettings settings;
    [SerializeField, HideInInspector]
    private Bounds playableBounds;
    public ProceduralVoxelWorld World => world;
    public Bounds PlayableBounds => playableBounds;
    public float SurfaceY => playableBounds.center.y;
    public bool IsGenerated => world != null && world.Data.IsValid && !world.IsStreaming;

    public void GenerateFor(BuildingFoundationData foundation, Vector3 position, Quaternion rotation)
    {
        Bounds bounds = new Bounds(position + rotation * foundation.Center, Vector3.zero);
        for (int i = 0; i < 4; i++)
        {
            bounds.Encapsulate(position + rotation * (foundation.Center + new Vector3(
                (i & 1) == 0 ? -foundation.width * .5f : foundation.width * .5f,
                0,
                (i & 2) == 0 ? -foundation.length * .5f : foundation.length * .5f)));
        }

        GenerateFor(bounds);
    }

    public bool RecoverFleet(SiegeFleet fleet, SiegeBase fortress)
    {
        if (fortress?.Core != null && BeyondRecovery(fortress.Core.transform.position))
        {
            return false;
        }

        if (fleet != null)
        {
            for (int i = 0; i < fleet.Ships.Length; i++)
            {
                SiegeFleetEntry ship = fleet.Ships[i];
                if (ship?.IsAlive == true && BeyondRecovery(ship.Cockpit.transform.position))
                {
                    fleet.Scrap(i);
                }
            }
        }

        return true;
    }

    public void GenerateFor(Bounds foundation)
    {
        if (world == null || settings == null || !WorldBuildingValidation.Finite(foundation.center) || !WorldBuildingValidation.Finite(foundation.size))
        {
            throw new InvalidOperationException("Siege island requires authored world/settings and a finite foundation.");
        }

        float width = Mathf.Max(settings.MinimumWidth, foundation.size.x + settings.EdgeMargin * 2);
        float length = Mathf.Max(settings.MinimumWidth, foundation.size.z + settings.EdgeMargin * 2);
        // Retain the finite World data ceiling for large designs by coarsening cells only
        // before deployment. A battle never expands/regenerates its island afterwards.
        float cell = Mathf.Max(settings.CellSize, Mathf.Ceil(Mathf.Max(width, length) / 128f));
        var size = new Vector3Int(
            Mathf.Clamp(Mathf.CeilToInt(width / cell), 16, 128),
            32,
            Mathf.Clamp(Mathf.CeilToInt(length / cell), 16, 128));
        const int surface = 28;
        int depth = Mathf.Clamp(Mathf.RoundToInt(settings.Depth / cell), 2, surface - 2);
        Vector2 plateau = new Vector2((foundation.size.x * .5f + 8) / cell, (foundation.size.z * .5f + 8) / cell);
        VoxelWorldData terrain = FloatingIslandTerrain.Generate(size, settings.Seed, surface, plateau, depth);
        Vector3 center = foundation.center;
        center.y = foundation.max.y;
        world.transform.SetPositionAndRotation(
            center - new Vector3(size.x * cell * .5f, surface * cell, size.z * cell * .5f),
            Quaternion.identity);
        world.transform.localScale = Vector3.one;
        world.InitializeFinite(terrain, cell, settings.Seed);
        playableBounds = new Bounds(center, new Vector3(size.x * cell, 0, size.z * cell));
        Physics.SyncTransforms();
    }

    public Vector3 SpawnPosition(float angle, float desiredRadius)
    {
        Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
        Vector3 half = playableBounds.extents;
        float x = (half.x - 12) / Mathf.Max(.001f, Mathf.Abs(direction.x));
        float z = (half.z - 12) / Mathf.Max(.001f, Mathf.Abs(direction.z));
        float radius = Mathf.Min(desiredRadius, x, z);
        return playableBounds.center + direction * Mathf.Max(12, radius) + Vector3.up * 14;
    }

    public bool BeyondRecovery(Vector3 position)
    {
        if (!IsGenerated)
        {
            return false;
        }

        Vector3 delta = position - playableBounds.center;
        return delta.y < -settings.VoidDistance || Mathf.Abs(delta.x) > playableBounds.extents.x + 200 || Mathf.Abs(delta.z) > playableBounds.extents.z + 200;
    }
}

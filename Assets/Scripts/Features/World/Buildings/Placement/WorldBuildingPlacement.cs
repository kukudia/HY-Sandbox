// 职责：在1m建筑网格与体素地形间转换位置，检测支撑、体素穿插和其他碰撞体。
using UnityEngine;

public static class WorldBuildingPlacement
{
    public static Vector3 Snap(ProceduralVoxelWorld world, Vector3 hit)
    {
        Vector3 local = world.transform.InverseTransformPoint(hit);
        local.x = Mathf.Round(local.x);
        local.z = Mathf.Round(local.z);
        local.y = Mathf.Round(local.y / world.CellSize) * world.CellSize;
        return world.transform.TransformPoint(local);
    }

    public static bool Check(
        ProceduralVoxelWorld world,
        BlockDataList blueprint,
        Vector3 position,
        Quaternion rotation,
        out string reason)
    {
        reason = "Requires flat, fully supported ground.";
        if (world == null || blueprint?.foundation == null || !blueprint.foundation.IsValid || blueprint.blocks == null || !WorldBuildingValidation.Finite(position))
        {
            return false;
        }

        Quaternion localRotation = Quaternion.Inverse(world.transform.rotation) * rotation;
        float yaw = Mathf.Round(localRotation.eulerAngles.y / 90f) * 90f;
        if (Quaternion.Angle(localRotation, Quaternion.Euler(0, yaw, 0)) > 0.1f)
        {
            reason = "Placement requires quarter-turn rotation on the world grid.";
            return false;
        }

        for (int x = 0; x < blueprint.foundation.width; x++)
        {
            for (int z = 0; z < blueprint.foundation.length; z++)
            {
                Vector3 sample = position + rotation * (LocalAnchor(x, z) - Vector3.up * 0.05f);
                Vector3Int cell = world.WorldToCell(sample);
                Vector3 above = sample + rotation * Vector3.up * 0.15f;
                if (!world.Data.Contains(cell) || world.Data.Get(cell) == 0 || world.Data.Get(world.WorldToCell(above)) != 0)
                {
                    return false;
                }
            }
        }

        Physics.SyncTransforms();
        foreach (BlockData block in blueprint.blocks)
        {
            Vector3 center = position + rotation * new Vector3(block.posX, block.posY, block.posZ);
            Quaternion blockRotation = rotation * new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
            Vector3 half = new Vector3(block.x, block.y, block.z) * 0.5f - Vector3.one * 0.06f;
            // 物理碰撞可能仍排队重建，体素穿插直接读取数据，其他实体用真实碰撞体。
            if (IntersectsTerrain(world, center, half, blockRotation))
            {
                reason = "Building intersects terrain.";
                return false;
            }

            foreach (Collider hit in Physics.OverlapBox(center, half, blockRotation, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.GetComponent<VoxelWorldChunk>() == null)
                {
                    reason = "Building overlaps another object.";
                    return false;
                }
            }
        }

        reason = "Ready to place";
        return true;
    }

    private static bool IntersectsTerrain(ProceduralVoxelWorld world, Vector3 center, Vector3 half, Quaternion rotation)
    {
        Bounds bounds = new Bounds(world.transform.InverseTransformPoint(center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3((i & 1) == 0 ? -half.x : half.x, (i & 2) == 0 ? -half.y : half.y, (i & 4) == 0 ? -half.z : half.z);
            bounds.Encapsulate(world.transform.InverseTransformPoint(center + rotation * corner));
        }

        Vector3Int min = Vector3Int.FloorToInt(bounds.min / world.CellSize);
        Vector3Int max = Vector3Int.FloorToInt(bounds.max / world.CellSize);
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int z = min.z; z <= max.z; z++)
                {
                    var cell = new Vector3Int(x, y, z);
                    if (!world.Data.Contains(cell) || world.Data.Get(cell) != 0)
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public static Vector3 LocalAnchor(int x, int z)
    {
        return new Vector3(x - 1.5f, 0f, z - 1.5f);
    }

    public static float Support(ProceduralVoxelWorld world, Vector2Int size, Transform pose)
    {
        int supported = 0;
        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector3 sample = pose.TransformPoint(LocalAnchor(x, z) - Vector3.up * 0.05f);
                if (world.Data.Get(world.WorldToCell(sample)) != 0)
                {
                    supported++;
                }
            }
        }

        return (float)supported / Mathf.Max(1, size.x * size.y);
    }

    public static Bounds Footprint(BuildingRuntime building)
    {
        Vector2Int size = building.FootprintSize;
        Vector3 center = new Vector3(size.x * 0.5f - 2f, 0f, size.y * 0.5f - 2f);
        Bounds bounds = new Bounds(building.transform.TransformPoint(center), Vector3.zero);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? -size.x * 0.5f : size.x * 0.5f,
                (i & 2) == 0 ? -0.1f : 0.1f,
                (i & 4) == 0 ? -size.y * 0.5f : size.y * 0.5f);
            bounds.Encapsulate(building.transform.TransformPoint(center + corner));
        }

        bounds.Expand(0.4f);
        return bounds;
    }
}

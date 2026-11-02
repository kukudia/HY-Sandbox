// 职责：创建前校验建筑模板、实例状态和资源，拒绝部分加载与非法尺寸/坐标。
// 入口：ValidateBlueprint() / ValidateState()；不会修改资源或活动世界。
using System;
using System.Collections.Generic;
using UnityEngine;

public static class WorldBuildingValidation
{
    public const int MaximumBlocks = 2048;
    public const int MaximumWorldBlocks = 8192;
    public static void ValidateBlueprint(BlockDataList blueprint)
    {
        if (blueprint == null || blueprint.foundation == null || !blueprint.foundation.IsValid || blueprint.blocks == null || blueprint.blocks.Count > MaximumBlocks)
        {
            throw new InvalidOperationException("Building requires a valid foundation and at most 2048 blocks.");
        }

        var ids = new HashSet<string>();
        if (blueprint.IsOutpost)
        {
            blueprint.outpost.Validate();
        }

        foreach (BlockData block in blueprint.blocks)
        {
            if (block == null || string.IsNullOrWhiteSpace(block.id) || !ids.Add(block.id) || block.x < 1 || block.y < 1 || block.z < 1 || block.x > 64 || block.y > 64 || block.z > 64)
            {
                throw new InvalidOperationException("Building contains invalid block dimensions or duplicate identities.");
            }

            Vector3 position = new Vector3(block.posX, block.posY, block.posZ);
            Quaternion rotation = new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
            if (!Finite(position) || position.sqrMagnitude > 1000000f || !ValidRotation(rotation))
            {
                throw new InvalidOperationException("Building contains an invalid local pose.");
            }

            GameObject prefab = Resources.Load<GameObject>(BlueprintBlockFactory.ToResourcePath(block.resourcePath));
            if (prefab == null || prefab.GetComponent<Block>() == null || prefab.GetComponent<Cockpit>() != null || prefab.GetComponent<BuildingFoundation>() != null)
            {
                throw new InvalidOperationException("Missing or unsupported building block: " + block.resourcePath);
            }

            if (block.cargo != null && block.cargo.Count > 4096)
            {
                throw new InvalidOperationException("Building cargo exceeds the item limit.");
            }

            long amount = 0;
            if (block.cargo != null)
            {
                foreach (CargoItem item in block.cargo)
                {
                    if (item == null || !item.IsValid || !Enum.IsDefined(typeof(CargoKind), item.kind))
                    {
                        throw new InvalidOperationException("Invalid building cargo item.");
                    }

                    amount += item.amount;
                }
            }

            CargoHold hold = prefab.GetComponent<CargoHold>();
            if (amount > (hold != null ? hold.Capacity : 0))
            {
                throw new InvalidOperationException("Building cargo exceeds its hold capacity.");
            }
        }
    }

    public static void ValidateState(WorldBuildingState state)
    {
        if (state == null || string.IsNullOrWhiteSpace(state.instanceId) || state.instanceId.Length > 128 || !Enum.IsDefined(typeof(UnitFaction), state.faction) || !Finite(state.position) || state.position.sqrMagnitude > 1e12f || !ValidRotation(state.rotation))
        {
            throw new InvalidOperationException("Invalid building instance identity or pose.");
        }

        ValidateBlueprint(state.blueprint);
        if (state.durability == null || state.durability.Length != state.blueprint.blocks.Count)
        {
            throw new InvalidOperationException("Building health does not match its blocks.");
        }

        foreach (float health in state.durability)
        {
            if (!Finite(health) || health <= 0f)
            {
                throw new InvalidOperationException("Invalid building block health.");
            }
        }
    }

    public static bool Finite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static bool Finite(Vector3 value)
    {
        return Finite(value.x) && Finite(value.y) && Finite(value.z);
    }

    public static bool ValidRotation(Quaternion value)
    {
        float norm = value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w;
        return Finite(norm) && Mathf.Abs(norm - 1f) < 0.01f;
    }
}

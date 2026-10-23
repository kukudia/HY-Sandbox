// 职责：先暂存建筑缺件，再按原蓝图恢复模块身份/姿态/耐久；存活模块保留原对象和货物。
// 入口：Apply()；调用者负责整备权限、空间净空和扣费，本类不替换建筑根或核心。
using System;
using System.Collections.Generic;
using UnityEngine;

internal static class BuildingRefit
{
    internal static bool Apply(
        BuildingRuntime building,
        List<Block> blocks,
        WorldBuildingManager manager,
        BlockDataList design,
        out string reason)
    {
        WorldBuildingValidation.ValidateBlueprint(design);
        reason = "Building refitted.";
        if (design.foundation.width != building.FootprintSize.x || design.foundation.length != building.FootprintSize.y)
        {
            reason = "Refit cannot resize an existing building footprint.";
            return false;
        }

        var owned = new Dictionary<string, Block>(StringComparer.Ordinal);
        foreach (Block block in blocks)
        {
            if (block != null && block.gameObject.activeSelf)
            {
                owned.Add(block.uniqueId, block);
            }
        }

        var created = new List<Block>();
        foreach (BlockData data in design.blocks)
        {
            string id = building.InstanceId + ":" + data.id;
            if (owned.ContainsKey(id))
            {
                continue;
            }

            if (!BlueprintBlockFactory.TryRestore(data, building.transform, building.GetComponent<ControlUnit>().faction, out Block restored))
            {
                foreach (Block staged in created)
                {
                    staged.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(staged.gameObject);
                }

                reason = "A building replacement could not be restored; no live modules changed.";
                return false;
            }

            restored.uniqueId = id;
            created.Add(restored);
            owned.Add(id, restored);
        }

        foreach (BlockData data in design.blocks)
        {
            Block block = owned[building.InstanceId + ":" + data.id];
            block.RestoreConnectionParticipation();
            block.transform.SetLocalPositionAndRotation(
                new Vector3(data.posX, data.posY, data.posZ),
                new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW));
            block.showConnectors = false;
            block.showLabel = false;
            RuntimeUnitMember.Ensure(block.gameObject, building.InstanceId, building.GetComponent<ControlUnit>().faction);
            Durability health = block.GetComponent<Durability>();
            if (health != null)
            {
                health.UpdateDurablility(Mathf.Max(0f, health.maxDurability - health.currentDurability));
            }
        }

        blocks.AddRange(created);
        foreach (Block restored in created)
        {
            manager.OptimizeVisuals(restored);
        }

        return true;
    }
}

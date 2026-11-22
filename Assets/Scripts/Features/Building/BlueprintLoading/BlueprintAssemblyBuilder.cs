// 职责：从蓝图恢复模块并完成共享连接、质量和单位归属初始化。
// 入口：Build(); 仅处理可复用的装配步骤，不读取存档、不决定建筑地基或载具驾驶权。
using System.Collections.Generic;
using UnityEngine;

internal static class BlueprintAssemblyBuilder
{
    internal static bool Build(BlockDataList blueprint, Transform root, UnitFaction faction, List<Block> blocks, out float mass)
    {
        mass = 0f;
        if (blueprint == null || blueprint.blocks == null || blocks == null)
        {
            return false;
        }

        for (int i = 0; i < blueprint.blocks.Count; i++)
        {
            BlockData data = blueprint.blocks[i];
            if (!TryRestoreLocal(data, root, faction, out Block block))
            {
                return false;
            }

            blocks.Add(block);
            mass += Mathf.Max(0.01f, block.mass);
        }

        new BatchConnectionBuilder(blocks).RebuildBatchImmediate();
        return true;
    }

    internal static bool TryRestoreLocal(BlockData data, Transform root, UnitFaction faction, out Block block)
    {
        if (!BlueprintBlockFactory.TryRestore(data, root, faction, out block))
        {
            return false;
        }

        block.transform.SetLocalPositionAndRotation(
            new Vector3(data.posX, data.posY, data.posZ),
            new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW));
        return true;
    }

    internal static void FinalizeUnit(ControlUnit unit, float mass, UnitFaction faction, bool assignOwnership = true)
    {
        if (unit == null)
        {
            return;
        }

        Rigidbody body = unit.GetComponent<Rigidbody>();
        if (body != null)
        {
            body.mass = Mathf.Max(1f, mass);
        }

        unit.faction = faction;
        unit.RefreshChildren();
        if (assignOwnership)
        {
            unit.AssignRuntimeOwnershipToBlocks(true);
        }
    }
}

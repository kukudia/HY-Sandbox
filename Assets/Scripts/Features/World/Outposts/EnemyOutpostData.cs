// 职责：保存建筑附带的有限驻军蓝图与本地部署点，独立于用户敌机文件后续改动。
// 入口：Validate()；建筑蓝图缺少outpost字段时仍是普通建筑。
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EnemyOutpostData
{
    public bool enabled;
    public List<OutpostGarrisonData> garrison = new List<OutpostGarrisonData>();
    public float activationDistance = 120f;
    public void Validate()
    {
        if (garrison == null || garrison.Count < 1 || garrison.Count > 8 || !WorldBuildingValidation.Finite(activationDistance) || activationDistance < 20f || activationDistance > 200f)
        {
            throw new InvalidOperationException("Outpost requires 1-8 defenders and activation range 20-200m.");
        }

        var ids = new HashSet<string>();
        foreach (OutpostGarrisonData guard in garrison)
        {
            if (guard == null || string.IsNullOrWhiteSpace(guard.id) || !ids.Add(guard.id) || !WorldBuildingValidation.Finite(guard.position) || guard.position.sqrMagnitude > 128f * 128f || guard.position.y < 1f || !WorldBuildingValidation.Finite(guard.yaw))
            {
                throw new InvalidOperationException("Invalid outpost defender identity or deployment point.");
            }

            if (guard.blueprint?.blocks == null || guard.blueprint.blocks.Count < 1 || guard.blueprint.blocks.Count > 256)
            {
                throw new InvalidOperationException("Defender requires 1-256 modules and cannot contain another outpost.");
            }

            int cockpits = 0;
            var moduleIds = new HashSet<string>();
            foreach (BlockData block in guard.blueprint.blocks)
            {
                GameObject prefab = block != null ? Resources.Load<GameObject>(BuildManager.ConvertToResourcesPath(block.resourcePath)) : null;
                if (prefab == null || prefab.GetComponent<Block>() == null || block.x < 1 || block.y < 1 || block.z < 1 || block.x > 64 || block.y > 64 || block.z > 64 || !WorldBuildingValidation.Finite(new Vector3(block.posX, block.posY, block.posZ)))
                {
                    throw new InvalidOperationException("Defender contains invalid or missing modules.");
                }

                if (string.IsNullOrWhiteSpace(block.id) || !moduleIds.Add(block.id) || !WorldBuildingValidation.ValidRotation(new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW)))
                {
                    throw new InvalidOperationException("Defender has invalid identity or rotation.");
                }

                if (prefab.GetComponent<Cockpit>() != null)
                {
                    cockpits++;
                }
            }

            if (cockpits != 1)
            {
                throw new InvalidOperationException("Each defender must have exactly one Cockpit.");
            }
        }
    }
}

[Serializable]
public sealed class OutpostGarrisonData
{
    public string id;
    public string name;
    public Vector3 position;
    public float yaw;
    public OutpostEnemyBlueprint blueprint;
    public BlockDataList CreateBlueprint()
    {
        return new BlockDataList
        {
            blocks = blueprint.blocks
        };
    }
}

// A separate non-recursive shape avoids Unity's depth limit when serializing nested blueprints.
[Serializable]
public sealed class OutpostEnemyBlueprint
{
    public List<BlockData> blocks = new List<BlockData>();
}

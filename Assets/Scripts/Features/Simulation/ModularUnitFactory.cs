// 职责：为任意阵营从蓝图建立模块飞船，统一尺寸、货物、身份、连接和物理初始化。
// 入口：IsValidBlueprint() / Spawn()；不写存档、不控制波次、不决定玩家驾驶权。
using System.Collections.Generic;
using UnityEngine;

public static class ModularUnitFactory
{
    public static bool IsValidBlueprint(BlockDataList dataList, string displayName)
    {
        if (dataList == null || dataList.blocks == null || dataList.blocks.Count == 0)
        {
            Debug.LogWarning($"Unit blueprint {displayName} is empty.");
            return false;
        }

        int cockpits = 0;
        foreach (BlockData data in dataList.blocks)
        {
            GameObject prefab = data != null && !string.IsNullOrWhiteSpace(data.resourcePath) ? BlockPrefabCache.GetPrefab(BlueprintBlockFactory.ToResourcePath(data.resourcePath)) : null;
            if (prefab == null || prefab.GetComponent<Block>() == null || data.x < 1 || data.y < 1 || data.z < 1)
            {
                Debug.LogWarning($"Unit blueprint {displayName} contains a missing or invalid module; spawn skipped.");
                return false;
            }

            if (prefab.GetComponent<Cockpit>() != null)
            {
                cockpits++;
            }
        }

        if (cockpits != 1)
        {
            Debug.LogWarning($"Unit blueprint {displayName} must contain exactly one Cockpit.");
            return false;
        }

        return true;
    }

    public static ControlUnit Spawn(
        BlockDataList blueprint,
        string displayName,
        Vector3 position,
        Quaternion rotation,
        UnitFaction faction)
    {
        if (!IsValidBlueprint(blueprint, displayName))
        {
            return null;
        }

        GameObject parentPrefab = Resources.Load<GameObject>("BlocksParent/BlocksParent");
        if (parentPrefab == null)
        {
            Debug.LogError("Cannot spawn modular unit: BlocksParent prefab is missing.");
            return null;
        }

        GameObject root = Object.Instantiate(parentPrefab, position, rotation);
        root.name = string.IsNullOrWhiteSpace(displayName) ? "ModularUnit" : displayName;
        Rigidbody body = root.GetComponent<Rigidbody>();
        // 组装碰撞形状和同步连接时保持运动学，避免插值历史与未完成的复合体进入物理模拟。
        body.isKinematic = true;
        var blocks = new List<Block>(blueprint.blocks.Count);
        float mass = 0f;
        float maximumHealth = 0f;
        foreach (BlockData data in blueprint.blocks)
        {
            if (!BlueprintBlockFactory.TryRestore(data, root.transform, faction, out Block block))
            {
                root.SetActive(false);
                Object.Destroy(root);
                return null;
            }

            block.transform.SetLocalPositionAndRotation(
                new Vector3(data.posX, data.posY, data.posZ),
                new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW));
            blocks.Add(block);
            mass += block.mass;
            foreach (Durability health in block.GetComponentsInChildren<Durability>())
            {
                if (health.enabled)
                {
                    maximumHealth += Mathf.Max(0f, health.maxDurability);
                }
            }
        }

        body.mass = Mathf.Max(1f, mass);
        new BatchConnectionBuilder(blocks).RebuildBatchImmediate();
        body.position = position;
        body.rotation = rotation;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        ControlUnit unit = root.GetComponent<ControlUnit>();
        unit.faction = faction;
        unit.RefreshChildren();
        unit.AssignRuntimeOwnershipToBlocks(true);
        if (faction == UnitFaction.Enemy)
        {
            EnemyIdentity identity = unit.cockpit.GetComponent<EnemyIdentity>();
            if (identity == null)
            {
                identity = unit.cockpit.gameObject.AddComponent<EnemyIdentity>();
            }

            identity.SetDisplayName(root.name);
            identity.SetSpawnMaxHealth(maximumHealth);
        }

        if (PlayManager.instance != null)
        {
            PlayManager.instance.RegisterControlUnit(unit);
        }

        return unit;
    }
}

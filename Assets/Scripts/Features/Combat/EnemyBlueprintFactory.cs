// 职责：由已校验的模块蓝图恢复一台敌机，供据点驻军和显式诊断共用。
// 入口：Spawn()；不负责计时、随机刷怪或持久化。
using System.Collections.Generic;
using UnityEngine;

public static class EnemyBlueprintFactory
{
    public static bool IsValidBlueprint(BlockDataList dataList, string enemyBlueprint)
    {
        if (dataList == null || dataList.blocks == null || dataList.blocks.Count == 0)
        {
            Debug.LogWarning($"Enemy blueprint {enemyBlueprint} is empty.");
            return false;
        }

        int cockpits = 0;
        foreach (BlockData data in dataList.blocks)
        {
            GameObject prefab = data != null && !string.IsNullOrWhiteSpace(data.resourcePath) ? Resources.Load<GameObject>(BuildManager.ConvertToResourcesPath(data.resourcePath)) : null;
            if (prefab == null || prefab.GetComponent<Block>() == null)
            {
                Debug.LogWarning($"Enemy blueprint {enemyBlueprint} contains a missing or invalid module; spawn skipped.");
                return false;
            }

            if (prefab.GetComponent<Cockpit>() != null)
            {
                cockpits++;
            }
        }

        if (cockpits != 1)
        {
            Debug.LogWarning($"Enemy blueprint {enemyBlueprint} must contain exactly one Cockpit.");
            return false;
        }

        return true;
    }

    public static ControlUnit Spawn(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (!IsValidBlueprint(dataList, enemyBlueprint))
        {
            return null;
        }

        GameObject parentPrefab = Resources.Load<GameObject>("BlocksParent/BlocksParent");
        GameObject unitObject = Object.Instantiate(parentPrefab, spawnPosition, spawnRotation);
        unitObject.name = string.IsNullOrWhiteSpace(enemyBlueprint) ? "ModularEnemy" : enemyBlueprint;
        float mass = 0f;
        float spawnMaxHealth = 0f;
        EnemyIdentity enemyIdentity = null;
        List<Block> spawnedBlocks = new List<Block>();
        foreach (BlockData data in dataList.blocks)
        {
            GameObject prefab = Resources.Load<GameObject>(BuildManager.ConvertToResourcesPath(data.resourcePath));
            if (prefab == null)
            {
                Debug.LogWarning($"Cannot spawn enemy block at Resources/{data.resourcePath}.");
                Object.Destroy(unitObject);
                return null;
            }

            Vector3 localPosition = new Vector3(data.posX, data.posY, data.posZ);
            Quaternion localRotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);
            GameObject blockObject = Object.Instantiate(prefab, unitObject.transform);
            ApplyBlueprintLocalTransform(blockObject.transform, localPosition, localRotation);
            CargoHold restoredHold = blockObject.GetComponent<CargoHold>();
            if (restoredHold != null && data.cargo != null)
            {
                restoredHold.RestoreContents(data.cargo);
            }

            Block block = blockObject.GetComponent<Block>();
            if (block != null)
            {
                bool shapeChanged = block.x != data.x || block.y != data.y || block.z != data.z;
                block.x = data.x;
                block.y = data.y;
                block.z = data.z;
                // CSV Awake 先应用模板；随后必须像玩家/建筑恢复一样重建实例几何和质量。
                block.RefreshRuntimeData(shapeChanged);
                block.resourcePath = data.resourcePath;
                if (!string.IsNullOrWhiteSpace(data.id))
                {
                    block.uniqueId = data.id;
                }

                mass += block.mass;
                spawnedBlocks.Add(block);
                foreach (Durability durability in blockObject.GetComponentsInChildren<Durability>())
                {
                    if (durability.enabled)
                    {
                        spawnMaxHealth += Mathf.Max(0f, durability.maxDurability);
                    }
                }
            }

            Cockpit cockpit = blockObject.GetComponent<Cockpit>();
            if (cockpit != null)
            {
                cockpit.faction = UnitFaction.Enemy;
                EnemyIdentity identity = cockpit.GetComponent<EnemyIdentity>();
                if (identity == null)
                {
                    identity = cockpit.gameObject.AddComponent<EnemyIdentity>();
                }

                identity.SetDisplayName(unitObject.name);
                enemyIdentity = identity;
            }
        }

        if (ModularUnitValidator.CountCockpits(spawnedBlocks) != 1)
        {
            Debug.LogWarning($"Enemy blueprint {enemyBlueprint} must contain exactly one Cockpit.");
            Object.Destroy(unitObject);
            return null;
        }

        enemyIdentity.SetSpawnMaxHealth(spawnMaxHealth);
        Rigidbody rb = unitObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = Mathf.Max(1f, mass);
            rb.isKinematic = false;
        }

        ControlUnit unit = unitObject.GetComponent<ControlUnit>();
        if (unit != null)
        {
            unit.faction = UnitFaction.Enemy;
            PrepareRuntimeEnemy(unit);
            unit.RefreshChildren();
            unit.AssignRuntimeOwnershipToBlocks(true);
            if (PlayManager.instance != null)
            {
                PlayManager.instance.RegisterControlUnit(unit);
            }
        }

        Debug.Log($"Spawned enemy [{unitObject.name}] at {unitObject.transform.position} with {spawnedBlocks.Count} blocks and total mass {mass}.");
        return unit;
    }

    private static void ApplyBlueprintLocalTransform(Transform blockTransform, Vector3 localPosition, Quaternion localRotation)
    {
        blockTransform.localPosition = CleanIntegerPosition(localPosition, blockTransform.name);
        blockTransform.localRotation = CleanRightAngleRotation(localRotation, blockTransform.name);
    }

    private static Vector3 CleanIntegerPosition(Vector3 position, string blockName)
    {
        Vector3 cleaned = new Vector3(SnapNearInteger(position.x), SnapNearInteger(position.y), SnapNearInteger(position.z));
        if (!IsIntegerVector(cleaned))
        {
            // Debug.LogWarning($"Enemy block [{blockName}] blueprint local position is not integer: {position}");
        }

        return cleaned;
    }

    private static Quaternion CleanRightAngleRotation(Quaternion rotation, string blockName)
    {
        Vector3 euler = rotation.eulerAngles;
        Vector3 cleanedEuler = new Vector3(SnapNearRightAngle(euler.x), SnapNearRightAngle(euler.y), SnapNearRightAngle(euler.z));
        if (!IsRightAngleVector(cleanedEuler))
        {
            //Debug.LogWarning($"Enemy block [{blockName}] blueprint local rotation is not a 90-degree increment: {euler}");
        }

        return Quaternion.Euler(cleanedEuler);
    }

    private static float SnapNearInteger(float value)
    {
        float rounded = Mathf.Round(value);
        return Mathf.Abs(value - rounded) < 0.001f ? rounded : value;
    }

    private static float SnapNearRightAngle(float angle)
    {
        float rounded = Mathf.Round(angle / 90f) * 90f;
        return Mathf.Abs(Mathf.DeltaAngle(angle, rounded)) < 0.001f ? rounded : angle;
    }

    private static bool IsIntegerVector(Vector3 value)
    {
        return IsNearlyInteger(value.x) && IsNearlyInteger(value.y) && IsNearlyInteger(value.z);
    }

    private static bool IsRightAngleVector(Vector3 euler)
    {
        return IsNearlyRightAngle(euler.x) && IsNearlyRightAngle(euler.y) && IsNearlyRightAngle(euler.z);
    }

    private static bool IsNearlyInteger(float value)
    {
        return Mathf.Abs(value - Mathf.Round(value)) < 0.001f;
    }

    private static bool IsNearlyRightAngle(float angle)
    {
        float rounded = Mathf.Round(angle / 90f) * 90f;
        return Mathf.Abs(Mathf.DeltaAngle(angle, rounded)) < 0.001f;
    }

    private static void PrepareRuntimeEnemy(ControlUnit enemy)
    {
        if (enemy == null)
        {
            return;
        }

        Rigidbody rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        enemy.faction = UnitFaction.Enemy;
        Cockpit[] cockpits = enemy.GetComponentsInChildren<Cockpit>(true);
        foreach (Cockpit cockpit in cockpits)
        {
            cockpit.faction = UnitFaction.Enemy;
        }
    }
}

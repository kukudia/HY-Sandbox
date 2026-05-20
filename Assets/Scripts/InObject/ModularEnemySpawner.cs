using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ModularEnemySpawner : MonoBehaviour
{
    public GameObject blocksParentPrefab;
    public bool spawnOnStart = true;
    public bool useSavedBlueprint = true;
    public string enemyBlueprintName = "default_enemy";
    public List<ModularBlockPlacement> blueprint = new List<ModularBlockPlacement>
    {
        new ModularBlockPlacement { blockResourcePath = "Blocks/Cockpit", localPosition = Vector3.zero },
        new ModularBlockPlacement { blockResourcePath = "Blocks/Turret", localPosition = new Vector3(0f, 1.5f, 0f) }
    };

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnEnemy();
        }
    }

    public ControlUnit SpawnEnemy()
    {
        if (useSavedBlueprint && TrySpawnSavedBlueprint(out ControlUnit savedUnit))
        {
            return savedUnit;
        }

        return SpawnPlacements(blueprint);
    }

    private bool TrySpawnSavedBlueprint(out ControlUnit unit)
    {
        unit = null;

        if (SaveManager.instance == null || string.IsNullOrWhiteSpace(enemyBlueprintName))
        {
            return false;
        }

        string path = SaveManager.instance.GetEnemyBlueprintPath(enemyBlueprintName);
        if (!File.Exists(path))
        {
            return false;
        }

        BlockDataList dataList = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
        if (dataList == null || dataList.blocks == null || dataList.blocks.Count == 0)
        {
            return false;
        }

        unit = SpawnBlockData(dataList);
        return unit != null;
    }

    private ControlUnit SpawnPlacements(List<ModularBlockPlacement> placements)
    {
        GameObject parentPrefab = blocksParentPrefab != null
            ? blocksParentPrefab
            : Resources.Load<GameObject>("Prefabs/BlocksParent");

        if (parentPrefab == null)
        {
            Debug.LogWarning("Cannot spawn modular enemy because BlocksParent prefab is missing.");
            return null;
        }

        GameObject unitObject = Instantiate(parentPrefab, transform.position, transform.rotation);
        unitObject.name = "ModularEnemy";

        float mass = 0f;
        List<Block> spawnedBlocks = new List<Block>();

        foreach (ModularBlockPlacement placement in placements)
        {
            if (placement == null || string.IsNullOrEmpty(placement.blockResourcePath)) continue;

            GameObject prefab = Resources.Load<GameObject>(placement.blockResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"Cannot spawn enemy block at Resources/{placement.blockResourcePath}.");
                continue;
            }

            GameObject blockObject = Instantiate(
                prefab,
                unitObject.transform.TransformPoint(placement.localPosition),
                unitObject.transform.rotation * Quaternion.Euler(placement.localEulerAngles),
                unitObject.transform
            );

            Block block = blockObject.GetComponent<Block>();
            if (block != null)
            {
                block.resourcePath = placement.blockResourcePath;
                mass += block.mass;
                spawnedBlocks.Add(block);
            }

            Cockpit cockpit = blockObject.GetComponent<Cockpit>();
            if (cockpit != null)
            {
                cockpit.faction = UnitFaction.Enemy;
            }
        }

        if (ModularUnitValidator.CountCockpits(spawnedBlocks) != 1)
        {
            Debug.LogWarning("Modular enemy blueprints must contain exactly one Cockpit.");
            Destroy(unitObject);
            return null;
        }

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
            unit.RefreshChildren();
        }

        return unit;
    }

    private ControlUnit SpawnBlockData(BlockDataList dataList)
    {
        GameObject parentPrefab = blocksParentPrefab != null
            ? blocksParentPrefab
            : Resources.Load<GameObject>("Prefabs/BlocksParent");

        if (parentPrefab == null)
        {
            Debug.LogWarning("Cannot spawn modular enemy because BlocksParent prefab is missing.");
            return null;
        }

        GameObject unitObject = Instantiate(parentPrefab, transform.position, transform.rotation);
        unitObject.name = string.IsNullOrWhiteSpace(enemyBlueprintName) ? "ModularEnemy" : enemyBlueprintName;

        float mass = 0f;
        List<Block> spawnedBlocks = new List<Block>();

        foreach (BlockData data in dataList.blocks)
        {
            GameObject prefab = Resources.Load<GameObject>(BuildManager.ConvertToResourcesPath(data.resourcePath));
            if (prefab == null)
            {
                Debug.LogWarning($"Cannot spawn enemy block at Resources/{data.resourcePath}.");
                continue;
            }

            Vector3 localPosition = new Vector3(data.posX, data.posY, data.posZ);
            Quaternion localRotation = new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW);
            GameObject blockObject = Instantiate(
                prefab,
                unitObject.transform.TransformPoint(localPosition),
                unitObject.transform.rotation * localRotation,
                unitObject.transform
            );

            Block block = blockObject.GetComponent<Block>();
            if (block != null)
            {
                block.x = data.x;
                block.y = data.y;
                block.z = data.z;
                block.resourcePath = data.resourcePath;
                mass += block.mass;
                spawnedBlocks.Add(block);
            }

            Cockpit cockpit = blockObject.GetComponent<Cockpit>();
            if (cockpit != null)
            {
                cockpit.faction = UnitFaction.Enemy;
            }
        }

        if (ModularUnitValidator.CountCockpits(spawnedBlocks) != 1)
        {
            Debug.LogWarning($"Enemy blueprint {enemyBlueprintName} must contain exactly one Cockpit.");
            Destroy(unitObject);
            return null;
        }

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
            unit.RefreshChildren();
        }

        return unit;
    }
}

[System.Serializable]
public class ModularBlockPlacement
{
    public string blockResourcePath;
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
}

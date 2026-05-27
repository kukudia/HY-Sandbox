using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    private void Start()
    {
        // Delay spawning until PlayManager is ready and playMode starts
        if (PlayManager.instance != null)
        {
            // Wait for playMode to start before spawning enemies
            StartCoroutine(SpawnWhenPlayModeStarts());
        }
        else
        {
            Debug.LogWarning("PlayManager instance not found. Enemy spawning disabled.");
        }
    }

    private System.Collections.IEnumerator SpawnWhenPlayModeStarts()
    {
        // Wait until playMode is active
        while (!PlayManager.instance.playMode)
        {
            yield return null;
        }

        // Small delay to ensure everything is initialized
        yield return new WaitForSeconds(0.1f);

        SpawnEnemy();
    }

    public void SpawnEnemy()
    {
        SaveManager.instance.GetAllEnemyBlueprintNames();

        if (SaveManager.instance.enemyBlueprints.Count == 0)
        {
            Debug.LogWarning("No enemy blueprints found in SaveManager.");
            return;
        }

        foreach (string enemyBlueprint in SaveManager.instance.enemyBlueprints)
        {
            string path = SaveManager.instance.GetEnemyBlueprintPath(enemyBlueprint);
            BlockDataList dataList = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
            SpawnBlockData(dataList, enemyBlueprint);
        }
    }

    private ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint)
    {
        if (SaveManager.instance == null || string.IsNullOrWhiteSpace(enemyBlueprint))
        {
            return null;
        }

        string path = SaveManager.instance.GetEnemyBlueprintPath(enemyBlueprint);
        if (!File.Exists(path))
        {
            return null;
        }

        if (dataList == null || dataList.blocks == null || dataList.blocks.Count == 0)
        {
            return null;
        }

        GameObject parentPrefab = Resources.Load<GameObject>("Prefabs/BlocksParent");

        if (parentPrefab == null)
        {
            Debug.LogWarning("Cannot spawn modular enemy because BlocksParent prefab is missing.");
            return null;
        }

        GameObject unitObject = Instantiate(parentPrefab, transform.position, transform.rotation);
        unitObject.name = string.IsNullOrWhiteSpace(enemyBlueprint) ? "ModularEnemy" : enemyBlueprint;

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
            Debug.LogWarning($"Enemy blueprint {enemyBlueprint} must contain exactly one Cockpit.");
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
            
            // Register the enemy control unit with PlayManager
            if (PlayManager.instance != null)
            {
                PlayManager.instance.RegisterControlUnit(unit);
            }
        }

        Debug.Log($"Spawned enemy {unitObject.name} at {unitObject.transform} with {spawnedBlocks.Count} blocks and total mass {mass}.");
        return unit;
    }
}
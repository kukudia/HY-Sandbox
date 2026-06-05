using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Time interval in seconds between spawning enemies from the blueprint list")]
    public float spawnInterval = 10f;

    [Tooltip("Spawn one enemy immediately when play mode starts")]
    public bool spawnOnPlayStart = true;

    [Tooltip("Distance from the player construct used when choosing a spawn point")]
    public float spawnDistance = 45f;

    [Tooltip("Vertical offset above the player construct used when spawning enemies")]
    public float spawnHeightOffset = 6f;
    
    private readonly List<EnemyBlueprintData> enemyBlueprintPool = new List<EnemyBlueprintData>();
    private bool isBlueprintPoolInitialized = false;
    
    private float spawnTimer = 0f;
    private Transform spawnAnchor;
    
    private void Start()
    {
        InitializeEnemyBlueprintPool();
    }
    
    private void Update()
    {
        // Only spawn when in playMode and blueprint pool is initialized
        if (!PlayManager.instance.playMode || !isBlueprintPoolInitialized || enemyBlueprintPool.Count == 0)
        {
            return;
        }
        
        // Accumulate time and spawn when interval is reached
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomEnemy();
        }
    }

    public void BeginPlayMode(Transform anchor)
    {
        spawnAnchor = anchor;
        InitializeEnemyBlueprintPool();
        spawnTimer = 0f;

        if (spawnOnPlayStart)
        {
            SpawnRandomEnemy();
        }
    }
    
    private void InitializeEnemyBlueprintPool()
    {
        if (isBlueprintPoolInitialized) return;
        
        SaveManager.instance.GetAllEnemyBlueprintNames();

        if (SaveManager.instance.enemyBlueprints.Count == 0)
        {
            Debug.LogWarning("No enemy blueprints found in SaveManager.");
            isBlueprintPoolInitialized = true;
            return;
        }

        foreach (string enemyBlueprint in SaveManager.instance.enemyBlueprints)
        {
            string path = SaveManager.instance.GetEnemyBlueprintPath(enemyBlueprint);
            if (File.Exists(path))
            {
                BlockDataList dataList = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
                if (IsValidBlueprint(dataList, enemyBlueprint))
                {
                    enemyBlueprintPool.Add(new EnemyBlueprintData(enemyBlueprint, dataList));
                }
            }
        }
        
        isBlueprintPoolInitialized = true;
        Debug.Log($"Enemy blueprint pool initialized with {enemyBlueprintPool.Count} enemies.");
    }
    
    /// <summary>
    /// Spawns a random enemy from cached blueprint data into the scene.
    /// </summary>
    public void SpawnRandomEnemy()
    {
        if (enemyBlueprintPool.Count == 0)
        {
            Debug.LogWarning("Enemy blueprint pool is empty.");
            return;
        }
        
        EnemyBlueprintData randomEnemy = enemyBlueprintPool[Random.Range(0, enemyBlueprintPool.Count)];
        if (randomEnemy == null || randomEnemy.dataList == null)
        {
            Debug.LogWarning("Selected enemy blueprint is null.");
            return;
        }
        
        Vector3 spawnPosition = GetSpawnPosition();
        Quaternion spawnRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        SpawnBlockData(randomEnemy.dataList, randomEnemy.name, spawnPosition, spawnRotation);
    }

    /// <summary>
    /// Legacy method for backward compatibility - spawns all enemies from cached blueprints.
    /// </summary>
    public void SpawnEnemy()
    {
        foreach (EnemyBlueprintData enemyBlueprint in enemyBlueprintPool)
        {
            if (enemyBlueprint != null)
            {
                SpawnBlockData(enemyBlueprint.dataList, enemyBlueprint.name, GetSpawnPosition(), transform.rotation);
            }
        }
    }

    private bool IsValidBlueprint(BlockDataList dataList, string enemyBlueprint)
    {
        if (dataList == null || dataList.blocks == null || dataList.blocks.Count == 0)
        {
            Debug.LogWarning($"Enemy blueprint {enemyBlueprint} is empty.");
            return false;
        }

        return true;
    }

    private ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        if (!IsValidBlueprint(dataList, enemyBlueprint))
        {
            return null;
        }

        GameObject parentPrefab = Resources.Load<GameObject>("Prefabs/BlocksParent");

        if (parentPrefab == null)
        {
            Debug.LogWarning("Cannot spawn modular enemy because BlocksParent prefab is missing.");
            return null;
        }

        GameObject unitObject = Instantiate(parentPrefab, spawnPosition, spawnRotation);
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
            PrepareRuntimeEnemy(unit);
            unit.RefreshChildren();
            
            if (PlayManager.instance != null)
            {
                PlayManager.instance.RegisterControlUnit(unit);
            }
        }

        Debug.Log($"Spawned enemy {unitObject.name} at {unitObject.transform.position} with {spawnedBlocks.Count} blocks and total mass {mass}.");
        
        return unit;
    }

    private Vector3 GetSpawnPosition()
    {
        Transform anchor = spawnAnchor != null ? spawnAnchor : PlayManager.instance?.blocksParent;
        if (anchor == null)
        {
            return transform.position;
        }

        Vector2 randomCircle = Random.insideUnitCircle.normalized;
        if (randomCircle.sqrMagnitude < 0.01f)
        {
            randomCircle = Vector2.right;
        }

        Vector3 offset = new Vector3(randomCircle.x, 0f, randomCircle.y) * spawnDistance;
        return anchor.position + offset + Vector3.up * spawnHeightOffset;
    }

    private void PrepareRuntimeEnemy(ControlUnit enemy)
    {
        if (enemy == null) return;

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

    private class EnemyBlueprintData
    {
        public readonly string name;
        public readonly BlockDataList dataList;

        public EnemyBlueprintData(string name, BlockDataList dataList)
        {
            this.name = name;
            this.dataList = dataList;
        }
    }
}

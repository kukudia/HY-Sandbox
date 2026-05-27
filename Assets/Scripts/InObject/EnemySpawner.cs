using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Tooltip("Time interval in seconds between spawning enemies from the pool")]
    public float spawnInterval = 10f;
    
    // Pool of pre-spawned enemy templates (hidden in edit mode)
    private List<ControlUnit> enemyPool = new List<ControlUnit>();
    private bool isPoolingInitialized = false;
    
    private float spawnTimer = 0f;
    
    private void Start()
    {
        // Initialize enemy pool
        InitializeEnemyPool();
    }
    
    private void Update()
    {
        // Only spawn when in playMode and pool is initialized
        if (!PlayManager.instance.playMode || !isPoolingInitialized || enemyPool.Count == 0)
        {
            return;
        }
        
        // Accumulate time and spawn when interval is reached
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnRandomEnemyClone();
        }
    }
    
    private void InitializeEnemyPool()
    {
        if (isPoolingInitialized) return;
        
        SaveManager.instance.GetAllEnemyBlueprintNames();

        if (SaveManager.instance.enemyBlueprints.Count == 0)
        {
            Debug.LogWarning("No enemy blueprints found in SaveManager.");
            isPoolingInitialized = true;
            return;
        }

        foreach (string enemyBlueprint in SaveManager.instance.enemyBlueprints)
        {
            string path = SaveManager.instance.GetEnemyBlueprintPath(enemyBlueprint);
            if (File.Exists(path))
            {
                BlockDataList dataList = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
                ControlUnit pooledEnemy = SpawnBlockData(dataList, enemyBlueprint, isForPool: true);
                if (pooledEnemy != null)
                {
                    enemyPool.Add(pooledEnemy);
                }
            }
        }
        
        isPoolingInitialized = true;
        Debug.Log($"Enemy pool initialized with {enemyPool.Count} enemies.");
    }
    
    /// <summary>
    /// Spawns a clone of a random enemy from the pool into the scene
    /// </summary>
    public void SpawnRandomEnemyClone()
    {
        if (enemyPool.Count == 0)
        {
            Debug.LogWarning("Enemy pool is empty.");
            return;
        }
        
        ControlUnit randomEnemy = enemyPool[Random.Range(0, enemyPool.Count)];
        if (randomEnemy == null)
        {
            Debug.LogWarning("Selected enemy from pool is null.");
            return;
        }
        
        // Instantiate a clone at the spawner's position
        GameObject cloneObject = Instantiate(randomEnemy.gameObject, transform.position, transform.rotation);
        cloneObject.SetActive(true);
        
        // Ensure the clone is properly registered
        ControlUnit cloneUnit = cloneObject.GetComponent<ControlUnit>();
        if (cloneUnit != null)
        {
            cloneUnit.RefreshChildren();
            if (PlayManager.instance != null)
            {
                PlayManager.instance.RegisterControlUnit(cloneUnit);
            }
            Debug.Log($"Spawned enemy clone {cloneObject.name} at {cloneObject.transform.position}.");
        }
    }

    /// <summary>
    /// Legacy method for backward compatibility - spawns all enemies as clones
    /// </summary>
    public void SpawnEnemy()
    {
        foreach (ControlUnit pooledEnemy in enemyPool)
        {
            if (pooledEnemy != null)
            {
                GameObject cloneObject = Instantiate(pooledEnemy.gameObject, transform.position, transform.rotation);
                cloneObject.SetActive(true);
                
                ControlUnit cloneUnit = cloneObject.GetComponent<ControlUnit>();
                if (cloneUnit != null)
                {
                    cloneUnit.RefreshChildren();
                    if (PlayManager.instance != null)
                    {
                        PlayManager.instance.RegisterControlUnit(cloneUnit);
                    }
                }
            }
        }
    }

    private ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint, bool isForPool = false)
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

        // If this is for the pool, hide it and make it static
        if (isForPool)
        {
            unitObject.SetActive(false);
        }

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
            
            // For pool objects, make them kinematic to prevent physics simulation while hidden
            if (isForPool)
            {
                rb.isKinematic = true;
            }
        }

        ControlUnit unit = unitObject.GetComponent<ControlUnit>();
        if (unit != null)
        {
            unit.faction = UnitFaction.Enemy;
            unit.RefreshChildren();
            
            // Only register with PlayManager if not in pool (pool objects are registered when cloned)
            if (!isForPool && PlayManager.instance != null)
            {
                PlayManager.instance.RegisterControlUnit(unit);
            }
        }

        if (!isForPool)
        {
            Debug.Log($"Spawned enemy {unitObject.name} at {unitObject.transform} with {spawnedBlocks.Count} blocks and total mass {mass}.");
        }
        else
        {
            Debug.Log($"Created pooled enemy template {unitObject.name} with {spawnedBlocks.Count} blocks and total mass {mass}.");
        }
        
        return unit;
    }
}
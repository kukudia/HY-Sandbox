// 职责：保留旧蓝图池与显式诊断兼容入口；自动计时刷怪已停用，据点使用EnemyBlueprintFactory。
// 入口：BeginPlayMode() / SpawnRandomEnemy() / SpawnEnemy()。
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
    private Transform spawnAnchor;
    private void Awake()
    {
        //enabled = false;
    }

    public void BeginPlayMode(Transform anchor)
    {
        spawnAnchor = anchor;
        InitializeEnemyBlueprintPool();
    }

    private void InitializeEnemyBlueprintPool()
    {
        if (isBlueprintPoolInitialized)
        {
            return;
        }

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
            try
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                BlockDataList dataList = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path));
                if (EnemyBlueprintFactory.IsValidBlueprint(dataList, enemyBlueprint))
                {
                    enemyBlueprintPool.Add(new EnemyBlueprintData(enemyBlueprint, dataList));
                }
            }
            catch (System.Exception exception) when (exception is IOException || exception is System.UnauthorizedAccessException || exception is System.ArgumentException)
            {
                // 单个外部文件损坏或暂被占用不能中断其他敌机，也不修改玩家原文件。
                Debug.LogWarning($"Enemy blueprint {enemyBlueprint} could not be loaded; skipped: {exception.Message}");
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

    internal ControlUnit SpawnBlockData(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        return EnemyBlueprintFactory.Spawn(dataList, enemyBlueprint, spawnPosition, spawnRotation);
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

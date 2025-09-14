using UnityEngine;
using System.Collections.Generic;

public class RepairBotManager : MonoBehaviour
{
    public static RepairBotManager Instance;

    public GameObject flyingBotPrefab;
    public int initialBots = 3;
    public Transform botSpawnArea;

    private List<RepairBot> allBots = new List<RepairBot>();

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        // 初始创建一些机器人
        for (int i = 0; i < initialBots; i++)
        {
            SpawnNewBot();
        }
    }

    public void SpawnNewBot()
    {
        Vector3 spawnPos;

        if (botSpawnArea != null)
        {
            // 在指定区域内随机生成
            Vector3 areaSize = botSpawnArea.localScale;
            Vector3 areaCenter = botSpawnArea.position;

            spawnPos = new Vector3(
                areaCenter.x + Random.Range(-areaSize.x / 2, areaSize.x / 2),
                areaCenter.y + Random.Range(0, areaSize.y),
                areaCenter.z + Random.Range(-areaSize.z / 2, areaSize.z / 2)
            );
        }
        else
        {
            // 默认生成位置
            spawnPos = new Vector3(
                Random.Range(-20f, 20f),
                Random.Range(5f, 15f),
                Random.Range(-20f, 20f)
            );
        }

        GameObject newBot = Instantiate(flyingBotPrefab, spawnPos, Quaternion.identity);
        allBots.Add(newBot.GetComponent<RepairBot>());
    }

    // 当方块被破坏时通知所有机器人
    public void NotifyBlockDestroyed(Durability destroyedBlock)
    {
        foreach (RepairBot bot in allBots)
        {
            // 如果机器人正在修复这个方块，清除它的目标
            if (bot.currentTarget == destroyedBlock)
            {
                bot.ClearTarget();
            }
        }
    }

    // 获取当前所有机器人
    public List<RepairBot> GetAllBots()
    {
        return new List<RepairBot>(allBots);
    }

    // 添加一个新机器人
    public void AddNewBot()
    {
        SpawnNewBot();
    }
}
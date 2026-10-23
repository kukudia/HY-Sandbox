// 职责：保存阵地战可编辑蓝图、经济、波次与部署预算；不持有运行中会话状态。
// 入口：CreateMatch()；由SiegeSession读取，场景制作工具创建默认配置资产。
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Siege Settings")]
public sealed class SiegeSettings : ScriptableObject
{
    [SerializeField]
    private TextAsset baseBlueprint;
    [SerializeField, Tooltip("建筑蓝图中承担基地核心的模块ID；不能指向驾驶舱。")]
    private string baseCoreBlockId = "bastion-core";
    [SerializeField, Min(1), Tooltip("基地缺件维修按模块占比计费的重建估值。")]
    private int baseReplacementValue = 6000;
    [SerializeField]
    private SiegeShipOption[] ships = Array.Empty<SiegeShipOption>();
    [SerializeField, Min(0)]
    private int startingCoins = 3000;
    [SerializeField, Min(1)]
    private int firstWaveSize = 2;
    [SerializeField, Min(0)]
    private int waveGrowth = 1;
    [SerializeField, Min(1)]
    private int maximumWaveSize = 48;
    [SerializeField, Min(1)]
    private int maximumActiveEnemies = 6;
    [SerializeField, Min(0)]
    private int killReward = 120;
    [SerializeField, Min(0)]
    private int waveReward = 350;
    [SerializeField, Min(0.1f)]
    private float spawnInterval = 3f;
    [SerializeField, Min(20f)]
    private float attackRadius = 130f;
    [SerializeField, Min(10f)]
    private float guardRadius = 100f;
    [SerializeField, Min(0.001f)]
    private float repairCoinsPerHealth = 0.03f;
    [SerializeField]
    private int spawnSeed = 14741;
    public TextAsset BaseBlueprint => baseBlueprint;
    public string BaseCoreBlockId => baseCoreBlockId;
    public int BaseReplacementValue => baseReplacementValue;
    public SiegeShipOption[] Ships => ships;
    public int MaximumActiveEnemies => maximumActiveEnemies;
    public float SpawnInterval => spawnInterval;
    public float AttackRadius => attackRadius;
    public float GuardRadius => guardRadius;
    public float RepairCoinsPerHealth => repairCoinsPerHealth;
    public int SpawnSeed => spawnSeed;

    public SiegeMatch CreateMatch()
    {
        return new SiegeMatch(
            startingCoins,
            firstWaveSize,
            waveGrowth,
            maximumWaveSize,
            killReward,
            waveReward);
    }
}

[Serializable]
public sealed class SiegeShipOption
{
    [SerializeField]
    private string displayName;
    [SerializeField]
    private TextAsset blueprint;
    [SerializeField, Min(1)]
    private int price = 500;
    public string DisplayName => displayName;
    public TextAsset Blueprint => blueprint;
    public int Price => price;
}

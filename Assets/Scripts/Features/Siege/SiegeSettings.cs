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
    [SerializeField, Tooltip("正常玩法从Saves与EnemyBlueprints读取；压力场关闭以保留可重复夹具。")]
    private bool usePlayerBlueprintFolders;
    [SerializeField, Min(1), Tooltip("用户舰船部署价格：模块数量乘以此单价。")]
    private int deploymentCoinsPerBlock = 5;
    public bool UsePlayerBlueprintFolders => usePlayerBlueprintFolders;
    public int DeploymentCoinsPerBlock => deploymentCoinsPerBlock;

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
    [SerializeField, Min(0), Tooltip("敌方无驾驶舱碎片的软数量预算；每0.5秒分批退场。")]
    private int maximumEnemyWreckGroups = 64;
    [SerializeField, Min(0), Tooltip("敌方碎片模块软预算，不包含建筑、存活驾驶舱与友舰维修件。")]
    private int maximumEnemyWreckBlocks = 512;
    [SerializeField, Min(0.5f)]
    private float enemyWreckLifetime = 15f;
    [SerializeField, Min(1), Tooltip("每次扫描最多退场组数，控制回收与掉落突发。")]
    private int enemyWreckRetirePerScan = 8;
    [SerializeField, Tooltip("每次波次结束后按报价自动修缮基地；金币不足或净空受阻时不扣款。")]
    private bool autoRepairBase = true;
    [SerializeField, Tooltip("基地自动修缮后，按驾驶舰优先顺序自动修复存活舰船；余额不足或泊位受阻时保留资产并记录结果。")]
    private bool autoRepairFleet = true;
    public bool AutoRepairBase => autoRepairBase;
    public bool AutoRepairFleet => autoRepairFleet;
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
    public int MaximumEnemyWreckGroups => maximumEnemyWreckGroups;
    public int MaximumEnemyWreckBlocks => maximumEnemyWreckBlocks;
    public float EnemyWreckLifetime => enemyWreckLifetime;
    public int EnemyWreckRetirePerScan => enemyWreckRetirePerScan;

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
    [NonSerialized]
    private string runtimeJson;
    public SiegeShipOption()
    {
    }

    public SiegeShipOption(string name, string json, int purchasePrice)
    {
        displayName = name;
        runtimeJson = json;
        price = purchasePrice;
    }

    [SerializeField]
    private string displayName;
    [SerializeField]
    private TextAsset blueprint;
    [SerializeField, Min(1)]
    private int price = 500;
    public string DisplayName => displayName;
    public TextAsset Blueprint => blueprint;
    public string BlueprintJson => runtimeJson ?? (blueprint != null ? blueprint.text : null);
    public int Price => price;
}

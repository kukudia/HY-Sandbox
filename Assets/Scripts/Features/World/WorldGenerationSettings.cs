// 职责：保存大世界种子、区块预算、建筑平台及安全出生区的可编辑生成参数。
// 入口：WorldSessionManager.BeginPreparation()读取；不会写入玩家或WorldLab存档。
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/World generation settings")]
public sealed class WorldGenerationSettings : ScriptableObject
{
    [SerializeField]
    private int seed = 14721;
    [SerializeField, Tooltip("每次退出后使用新种子；关闭后仍丢弃变化，并按固定种子重建。")]
    private bool randomizeSeed = true;
    [SerializeField, Range(1f, 4f)]
    private float cellSize = 2f;
    [SerializeField, Range(8, 32)]
    private int chunkSize = 16;
    [SerializeField, Range(16, 64)]
    private int worldHeight = 32;
    [SerializeField, Range(8, 128), Tooltip("从出生区向各方向延伸的区块数；默认世界宽约4.1km。")]
    private int worldRadiusInChunks = 64;
    [SerializeField, Range(2, 4)]
    private int loadRadius = 4;
    [SerializeField, Range(3, 5), Tooltip("比加载范围多一圈，减少来回穿越边界时重复创建。")]
    private int unloadRadius = 5;
    [SerializeField, Range(0f, 1f)]
    private float buildingChance = 0.1f;
    [SerializeField, Range(2, 64), Tooltip("建筑平台最小半边长（体素格）；会按本次蓝图自动扩大。")]
    private int buildingPadHalfSize = 4;
    [SerializeField, Range(1, 2), Tooltip("出生区周围不生成敌方建筑的区块半径；不修改自然地形。")]
    private int spawnClearRadius = 1;
    [SerializeField, Min(1f), Tooltip("载具底部高于其整个水平范围内最高地面的距离（米）。")]
    private float spawnClearance = 6f;
    [SerializeField, Range(300f, 1000f), Tooltip("远景可见距离（米）；外侧距离雾遮住流式边缘。")]
    private float viewDistance = 600f;
    [SerializeField, Tooltip("游玩时远处地形与背景共同渐隐到此颜色；退出恢复建造环境颜色。")]
    private Color distanceFogColor = new Color(0.48f, 0.57f, 0.64f, 1f);
    [SerializeField, Range(0.5f, 8f), Tooltip("建筑创建/启用每批的主线程时间预算（毫秒）；单模块操作仍不可中断。")]
    private float loadingFrameBudgetMilliseconds = 2f;
    public int Seed => seed;
    public bool RandomizeSeed => randomizeSeed;
    public float CellSize => Mathf.Clamp(cellSize, 1f, 4f);
    public int ChunkSize => Mathf.Clamp(chunkSize, 8, 32);
    public int WorldHeight => Mathf.Clamp(worldHeight, 16, 64);
    public int WorldRadiusInChunks => Mathf.Clamp(worldRadiusInChunks, 8, 128);
    public int LoadRadius => Mathf.Clamp(loadRadius, 2, 4);
    public int UnloadRadius => Mathf.Clamp(unloadRadius, LoadRadius + 1, 5);
    public float BuildingChance => Mathf.Clamp01(buildingChance);
    public int BuildingPadHalfSize => Mathf.Clamp(buildingPadHalfSize, 2, 132);
    public int SpawnClearRadius => Mathf.Clamp(spawnClearRadius, 1, LoadRadius - 1);
    public float SpawnClearance => Mathf.Max(1f, spawnClearance);
    public float ViewDistance => Mathf.Clamp(viewDistance, 300f, 1000f);
    public Color DistanceFogColor => distanceFogColor;
    public float LoadingFrameBudgetMilliseconds => Mathf.Clamp(loadingFrameBudgetMilliseconds, 0.5f, 8f);

    internal void FitBuildingPlatforms(float requiredRadius)
    {
        buildingPadHalfSize = Mathf.Max(BuildingPadHalfSize, Mathf.CeilToInt(requiredRadius / CellSize) + 1);
    }
}

// 职责：保存阵地战浮空岛可编辑生成规格与坠落边界；尺寸按基地占地只在部署时确定。
// 入口：Inspector / SiegeIsland.GenerateFor()；单位为游戏米。
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Siege Floating Island Settings")]
public sealed class SiegeIslandSettings : ScriptableObject
{
    [SerializeField]
    private int seed = 160611;
    [SerializeField, Min(96)]
    private float minimumWidth = 192;
    [SerializeField, Min(16)]
    private float edgeMargin = 32;
    [SerializeField, Min(1)]
    private float cellSize = 2;
    [SerializeField, Range(8, 100)]
    private float depth = 40;
    [SerializeField, Min(20)]
    private float voidDistance = 80;
    public int Seed => seed;
    public float MinimumWidth => minimumWidth;
    public float EdgeMargin => edgeMargin;
    public float CellSize => cellSize;
    public float Depth => depth;
    public float VoidDistance => voidDistance;
}

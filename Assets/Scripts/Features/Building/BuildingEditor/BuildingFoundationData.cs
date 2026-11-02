// 职责：保存建筑底座的网格尺寸；缺省为 4×4，不改变原有方块存档坐标。
using System;

[Serializable]
public sealed class BuildingFoundationData
{
    public const int DefaultSize = 4;
    public const int MaximumSize = 64;
    public int width = DefaultSize;
    public int length = DefaultSize;
    public bool IsValid => width >= 1 && width <= MaximumSize && length >= 1 && length <= MaximumSize;
}

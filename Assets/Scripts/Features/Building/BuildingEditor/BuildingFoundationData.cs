// 职责：保存自动占地的网格范围；偏移缺省为零，保持旧蓝图 (-2, 0, -2) 原点契约。
using System;
using UnityEngine;

[Serializable]
public sealed class BuildingFoundationData
{
    public const int DefaultSize = 4;
    public const int MaximumSize = 64;
    public int width = DefaultSize;
    public int length = DefaultSize;
    public int offsetX;
    public int offsetZ;
    public float MinX => offsetX - 2f;
    public float MinZ => offsetZ - 2f;
    public Vector3 Center => new Vector3(MinX + width * .5f, 0f, MinZ + length * .5f);
    public bool IsValid => width >= 1 && width <= MaximumSize && length >= 1 && length <= MaximumSize && Math.Abs((long)offsetX) <= 1002 && Math.Abs((long)offsetZ) <= 1002;

    public BuildingFoundationData Copy()
    {
        return new BuildingFoundationData
        {
            width = width,
            length = length,
            offsetX = offsetX,
            offsetZ = offsetZ
        };
    }
}

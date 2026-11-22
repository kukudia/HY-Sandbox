// 职责：保存世界建筑独立身份、根姿态、蓝图快照与逐块耐久；坐标相对建筑底座。
using System;
using UnityEngine;

[Serializable]
public sealed class BlueprintStructureState
{
    public string instanceId;
    public string blueprintName;
    public UnitFaction faction;
    public Vector3 position;
    public Quaternion rotation = Quaternion.identity;
    public bool anchored = true;
    public BlockDataList blueprint;
    public float[] durability;
}

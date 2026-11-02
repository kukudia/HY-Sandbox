// 职责：保存建筑制作参考与WorldLab初始布局；运行时内容只从BuildingBlueprints按名称解析。
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/World building catalog")]
public sealed class WorldBuildingCatalog : ScriptableObject
{
    [Tooltip("制作参考，不加入运行时建筑库。需通过建筑编辑器显式复制到BuildingBlueprints后使用。")]
    public TextAsset[] blueprints = Array.Empty<TextAsset>();
    public InitialBuilding[] initialBuildings = Array.Empty<InitialBuilding>();
    [Serializable]
    public sealed class InitialBuilding
    {
        [Tooltip("仅使用资源名称匹配BuildingBlueprints文件；不存在或无效则跳过，不读取此资源的内容。")]
        public TextAsset blueprint;
        public Vector3 worldLocalPosition;
        [Range(0, 3)]
        public int quarterTurns;
        public UnitFaction faction = UnitFaction.Enemy;
    }
}

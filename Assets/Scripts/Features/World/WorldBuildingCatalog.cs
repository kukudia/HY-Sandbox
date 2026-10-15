// 职责：可编辑的随项目建筑模板与初始布局；用户建筑文件由运行时面板另行读取。
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/World building catalog")]
public sealed class WorldBuildingCatalog : ScriptableObject
{
    public TextAsset[] blueprints = Array.Empty<TextAsset>();
    public InitialBuilding[] initialBuildings = Array.Empty<InitialBuilding>();
    [Serializable]
    public sealed class InitialBuilding
    {
        public TextAsset blueprint;
        public Vector3 worldLocalPosition;
        [Range(0, 3)]
        public int quarterTurns;
        public UnitFaction faction = UnitFaction.Enemy;
    }
}

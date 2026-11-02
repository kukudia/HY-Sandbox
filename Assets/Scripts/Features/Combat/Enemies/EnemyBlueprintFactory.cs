// 职责：保留敌机生成公开入口；所有阵营共用ModularUnitFactory恢复模块与物理归属。
// 入口：IsValidBlueprint() / Spawn()；现有据点和普通刷怪调用者保持兼容。
using UnityEngine;

public static class EnemyBlueprintFactory
{
    public static bool IsValidBlueprint(BlockDataList dataList, string enemyBlueprint)
    {
        return ModularUnitFactory.IsValidBlueprint(dataList, enemyBlueprint);
    }

    public static ControlUnit Spawn(BlockDataList dataList, string enemyBlueprint, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        return ModularUnitFactory.Spawn(dataList, enemyBlueprint, spawnPosition, spawnRotation, UnitFaction.Enemy);
    }
}

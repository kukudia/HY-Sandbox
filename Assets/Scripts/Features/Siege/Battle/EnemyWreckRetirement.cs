// 职责：共用敌方残骸筛选与幂等回收退场规则，保护建筑、友军、混合归属和存活驾驶舱。
// 入口：Eligible()/Retire()；预算与波次扫描共用，调用者负责保留期和分批节流。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyWreckRetirement
{
    public static bool Eligible(ControlUnit unit, Scene scene, List<Block> blocks)
    {
        blocks.Clear();
        if (unit == null || !unit.gameObject.activeInHierarchy || unit.gameObject.scene != scene || unit.Building != null || unit.faction != UnitFaction.Enemy || unit.HasAnyCockpit)
        {
            return false;
        }

        unit.GetComponentsInChildren(false, blocks);
        if (blocks.Count == 0)
        {
            return false;
        }

        foreach (Block block in blocks)
        {
            RuntimeUnitMember member = block.GetComponent<RuntimeUnitMember>();
            if (member == null || member.ownerFaction != UnitFaction.Enemy)
            {
                return false;
            }
        }

        return true;
    }

    public static bool Retire(ControlUnit unit, Scene scene, List<Block> blocks, bool allowPausedSimulation)
    {
        // Recheck immediately before conversion: ownership can change while a scan is pending.
        if (!Eligible(unit, scene, blocks))
        {
            return false;
        }

        WreckSalvage.ConvertGroup(unit, allowPausedSimulation);
        unit.gameObject.SetActive(false);
        Object.Destroy(unit.gameObject);
        return true;
    }
}

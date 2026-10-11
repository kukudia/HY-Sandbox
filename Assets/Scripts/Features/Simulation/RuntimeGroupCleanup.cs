// 职责：按无驾驶舱组上限和玩家距离安排残骸清理，不立即销毁或更改归属。
// 入口：EnforceLimit() / EnforceDistance()；由 PlayManager 在原有时序调用。
using System.Collections.Generic;
using UnityEngine;

internal static class RuntimeGroupCleanup
{
    internal static void EnforceLimit(List<ControlUnit> units, int maximumUselessGroups, DestroyManager destruction)
    {
        int groupLimit = Mathf.Max(1, maximumUselessGroups);
        for (int i = units.Count - 1; i >= 0; i--)
        {
            if (units[i] == null)
            {
                units.RemoveAt(i);
            }
        }

        int uselessGroupCount = 0;
        foreach (ControlUnit group in units)
        {
            if (group != null && !group.HasAnyCockpit)
            {
                uselessGroupCount++;
            }
        }

        int cleanupCount = uselessGroupCount - groupLimit;
        if (cleanupCount <= 0)
        {
            return;
        }

        foreach (ControlUnit group in units.ToArray())
        {
            if (cleanupCount <= 0)
            {
                break;
            }

            if (group == null || group.HasAnyCockpit)
            {
                continue;
            }

            destruction.ScheduleUnitCleanup(group);
            cleanupCount--;
        }
    }

    internal static void EnforceDistance(
        List<ControlUnit> units,
        bool playMode,
        Transform playerRoot,
        float cleanupDistance,
        DestroyManager destruction)
    {
        if (!playMode || playerRoot == null || cleanupDistance <= 0f)
        {
            return;
        }

        ControlUnit playerGroup = playerRoot.GetComponent<ControlUnit>();
        float distanceSqr = cleanupDistance * cleanupDistance;
        foreach (ControlUnit group in units.ToArray())
        {
            if (group == null || group == playerGroup)
            {
                continue;
            }

            if ((group.transform.position - playerRoot.position).sqrMagnitude > distanceSqr)
            {
                destruction.ScheduleDistantGroupCleanup(group, playerRoot, cleanupDistance);
            }
        }
    }
}

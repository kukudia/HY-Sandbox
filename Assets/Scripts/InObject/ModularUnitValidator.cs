using System.Collections.Generic;
using UnityEngine;

public static class ModularUnitValidator
{
    public static bool TryGetSingleCockpit(Component root, out Cockpit cockpit, out string reason)
    {
        cockpit = null;
        reason = string.Empty;

        if (root == null)
        {
            reason = "Unit root is missing.";
            return false;
        }

        Cockpit[] cockpits = root.GetComponentsInChildren<Cockpit>(true);
        if (cockpits.Length != 1)
        {
            reason = $"{root.name} must contain exactly one Cockpit, found {cockpits.Length}.";
            return false;
        }

        cockpit = cockpits[0];
        return true;
    }

    public static int CountCockpits(IEnumerable<Block> blocks)
    {
        int count = 0;
        if (blocks == null) return count;

        foreach (Block block in blocks)
        {
            if (block != null && block.GetComponent<Cockpit>() != null)
            {
                count++;
            }
        }

        return count;
    }

    public static int CountLoadedCockpits()
    {
        if (SaveManager.instance == null)
        {
            return 0;
        }

        return CountCockpits(SaveManager.instance.blocks);
    }
}

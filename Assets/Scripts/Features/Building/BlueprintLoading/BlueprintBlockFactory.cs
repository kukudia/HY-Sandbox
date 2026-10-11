// 职责：从存档恢复一个方块的身份、尺寸、阵营、货仓和耐久。
// 入口：TryRestore()；只创建实体，不负责加载会话、统计、UI 或 JSON 写入。
using UnityEngine;

internal static class BlueprintBlockFactory
{
    public static bool TryRestore(BlockData data, Transform parent, UnitFaction faction, out Block block)
    {
        block = null;
        if (data == null || string.IsNullOrWhiteSpace(data.resourcePath))
        {
            return false;
        }

        GameObject prefab = Resources.Load<GameObject>(ToResourcePath(data.resourcePath));
        if (prefab == null)
        {
            return false;
        }

        GameObject obj = Object.Instantiate(
            prefab,
            new Vector3(data.posX, data.posY, data.posZ),
            new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW),
            parent);
        block = obj.GetComponent<Block>();
        if (block == null)
        {
            Object.Destroy(obj);
            return false;
        }

        bool shapeChanged = block.x != data.x || block.y != data.y || block.z != data.z;
        block.x = data.x;
        block.y = data.y;
        block.z = data.z;
        // 存档保留实例尺寸；自动质量/碰撞体需按恢复后的尺寸重算，手动质量保持 CSV 值。
        block.RefreshRuntimeData(shapeChanged);
        block.resourcePath = data.resourcePath;
        block.uniqueId = data.id;
        ApplyFaction(block, faction);
        if (obj.TryGetComponent(out CargoHold cargo) && data.cargo != null)
        {
            cargo.RestoreContents(data.cargo);
        }

        if (obj.TryGetComponent(out Durability durability))
        {
            durability.currentDurability = durability.maxDurability;
        }

        return true;
    }

    public static void ApplyFaction(Block block, UnitFaction faction)
    {
        if (block != null && block.TryGetComponent(out Cockpit cockpit))
        {
            cockpit.faction = faction;
        }
    }

    public static string ToResourcePath(string fullPath)
    {
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            return string.Empty;
        }

        if (fullPath.StartsWith("Assets/Resources/"))
        {
            fullPath = fullPath.Substring("Assets/Resources/".Length);
        }

        if (fullPath.EndsWith(".prefab"))
        {
            fullPath = fullPath.Substring(0, fullPath.Length - ".prefab".Length);
        }

        return fullPath;
    }
}

// 职责：从真实旋转模块包围盒收紧建筑占地；只更新元数据，不平移模块或改变玩家铺设的平台。
// 入口：Calculate()；空设计保留默认小网格，安全上限由部署校验另行处理。
using System.Collections.Generic;
using UnityEngine;

public static class BuildingFootprint
{
    public static BuildingFoundationData Calculate(IReadOnlyList<BlockData> blocks, int padding = 1)
    {
        if (blocks == null || blocks.Count == 0)
        {
            return new BuildingFoundationData();
        }

        Bounds bounds = BlockBounds.FromBlueprint(blocks, Matrix4x4.identity);
        int margin = Mathf.Clamp(padding, 0, 8);
        int minX = Mathf.FloorToInt(bounds.min.x + .001f) - margin;
        int minZ = Mathf.FloorToInt(bounds.min.z + .001f) - margin;
        int maxX = Mathf.CeilToInt(bounds.max.x - .001f) + margin;
        int maxZ = Mathf.CeilToInt(bounds.max.z - .001f) + margin;
        return new BuildingFoundationData
        {
            width = Mathf.Max(1, maxX - minX),
            length = Mathf.Max(1, maxZ - minZ),
            offsetX = minX + 2,
            offsetZ = minZ + 2
        };
    }
}

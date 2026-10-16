// 职责：快照每套建筑真实支撑范围，并让地形和建筑生成使用同一确定性模板索引。
// 入口：构造后Get()读取；不包含驻军飞行范围，不访问场景或用户文件。
using System.Collections.Generic;
using UnityEngine;

public sealed class WorldBuildingFootprints
{
    private readonly Bounds[] footprints;
    public int Count => footprints.Length;
    public float MaximumWidth
    {
        get; private set;
    }

    public WorldBuildingFootprints(IReadOnlyList<BlockDataList> templates)
    {
        footprints = new Bounds[templates?.Count ?? 0];
        for (int i = 0; i < footprints.Length; i++)
        {
            BlockDataList template = templates[i];
            Bounds bounds = new Bounds(
                new Vector3(template.foundation.width * .5f - 2f, 0f, template.foundation.length * .5f - 2f),
                new Vector3(template.foundation.width, 0f, template.foundation.length));
            if (template.blocks.Count > 0)
            {
                bounds.Encapsulate(OutpostBaseDesigner.BoundsOf(template.blocks, Quaternion.identity));
            }

            footprints[i] = bounds;
            MaximumWidth = Mathf.Max(MaximumWidth, bounds.size.x, bounds.size.z);
        }
    }

    public Bounds Get(int seed, Vector2Int column)
    {
        return footprints[SelectTemplate(seed, column, Count)];
    }

    public static int SelectTemplate(int seed, Vector2Int column, int count)
    {
        uint hash = unchecked((uint)(seed * 73856093) ^ (uint)(column.x * 19349663) ^ (uint)(column.y * 83492791));
        // Mix high bits before modulo: an even site grid otherwise selects only odd templates.
        unchecked
        {
            hash ^= hash >> 16;
            hash *= 0x7feb352du;
            hash ^= hash >> 15;
            hash *= 0x846ca68bu;
            hash ^= hash >> 16;
        }

        return (int)(hash % (uint)count);
    }
}

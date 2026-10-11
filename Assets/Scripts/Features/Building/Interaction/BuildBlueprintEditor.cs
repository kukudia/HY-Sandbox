// 职责：持久化建造编辑并同步受影响连接；不负责输入、Ghost 或加载会话。
// 入口：Save() / Remove() / RemoveData()；SaveManager 和几何查询由组合入口注入。
using System.Collections.Generic;
using System.IO;
using UnityEngine;

internal sealed class BuildBlueprintEditor
{
    private readonly SaveManager _saves;
    private readonly BuildPlacementGeometry _geometry;
    public BuildBlueprintEditor(SaveManager saves, BuildPlacementGeometry geometry)
    {
        _saves = saves;
        _geometry = geometry;
    }

    public void Save(Block block, string path, UnitFaction faction)
    {
        BlueprintBlockFactory.ApplyFaction(block, faction);
        var data = new BlockData(block);
        int index = _saves.cachedData.blocks.FindIndex(item => item.id == data.id);
        if (index < 0)
        {
            _saves.cachedData.blocks.Add(data);
        }
        else
        {
            _saves.cachedData.blocks[index] = data;
        }

        Write(path);
        RefreshConnectionsAround(block);
    }

    public void Remove(Block block, string path)
    {
        block.neighbors = block.Neighbors();
        var neighbors = new List<Block>(block.neighbors);
        RemoveData(block.uniqueId, path);
        block.DisConnectAllConnectors();
        foreach (Block neighbor in neighbors)
        {
            if (neighbor != null)
            {
                neighbor.CheckConnection();
            }
        }
    }

    public bool RemoveData(string id, string path)
    {
        int index = _saves.cachedData.blocks.FindIndex(data => data.id == id);
        if (index < 0)
        {
            return false;
        }

        _saves.cachedData.blocks.RemoveAt(index);
        Write(path);
        return true;
    }

    private void Write(string path)
    {
        File.WriteAllText(path, JsonUtility.ToJson(_saves.cachedData, true));
    }

    private void RefreshConnectionsAround(Block block)
    {
        Physics.SyncTransforms();
        var affected = new HashSet<Block>
        {
            block
        };
        Vector3 halfExtents = _geometry.GetBlockHalfExtents(block) + Vector3.one * 0.35f;
        foreach (Collider hit in Physics.OverlapBox(
            block.transform.position,
            halfExtents,
            block.transform.rotation,
            _geometry.blockLayer,
            QueryTriggerInteraction.Ignore))
        {
            Block neighbor = hit.GetComponentInParent<Block>();
            if (neighbor != null)
            {
                affected.Add(neighbor);
            }
        }

        // Connections must be rebuilt before neighbors are read, after transforms synchronize.
        foreach (Block item in affected)
        {
            if (item != null && item.isActiveAndEnabled)
            {
                item.CheckConnection();
            }
        }

        foreach (Block item in affected)
        {
            if (item != null && item.isActiveAndEnabled)
            {
                item.neighbors = item.Neighbors();
            }
        }
    }
}

// 职责：将可选材质配置应用到基础结构方块；保留旧蓝图的Prefab参数和健康比例。
// 入口：SetMaterial()/Refresh()/Restore()；仅预挂在支持材质的Prefab，运行时不补组件。
using System;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Block))]
public sealed class BlockMaterial : MonoBehaviour
{
    [SerializeField]
    private string materialId;
    [SerializeField]
    private MeshRenderer[] surfaces = Array.Empty<MeshRenderer>();
    public string MaterialId => materialId;
    public BlockMaterialCatalog.Definition Definition => BlockMaterialCatalog.Resolve(materialId);

    public void SetMaterial(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }

        if (BlockMaterialCatalog.Resolve(id) == null)
        {
            throw new ArgumentException("Unknown structural material: " + id);
        }

        materialId = id;
        Refresh();
    }

    public void Refresh()
    {
        BlockMaterialCatalog.Definition definition = Definition;
        if (definition == null)
        {
            return;
        }

        Block block = GetComponent<Block>();
        float volume = (float)Mathf.Max(1, block.x) * Mathf.Max(1, block.y) * Mathf.Max(1, block.z);
        block.density = definition.density;
        block.SetMassSettings(true, volume * definition.density);
        block.cost = checked((int)(volume * definition.coinsPerCell));
        Durability health = GetComponent<Durability>();
        // Refreshing a resized/deserialized module must not grant repairs in combat.
        float fraction = health.maxDurability > 0f ? Mathf.Clamp01(health.currentDurability / health.maxDurability) : 1f;
        health.maxDurability = volume * definition.durabilityPerCell;
        health.currentDurability = health.maxDurability * fraction;
        foreach (MeshRenderer renderer in surfaces)
        {
            if (renderer != null && definition.surface != null)
            {
                Material[] slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    slots[i] = definition.surface;
                }

                renderer.sharedMaterials = slots;
            }
        }
    }

    public static void Validate(string id, GameObject prefab)
    {
        if (!string.IsNullOrEmpty(id) && (BlockMaterialCatalog.Resolve(id) == null || prefab == null || prefab.GetComponent<BlockMaterial>() == null))
        {
            throw new ArgumentException("Unknown or unsupported structural material: " + id);
        }
    }

    public static void Restore(Block block, string id)
    {
        Validate(id, block.gameObject);
        if (!string.IsNullOrEmpty(id))
        {
            block.GetComponent<BlockMaterial>().SetMaterial(id);
        }
    }

    public bool PlayBreak()
    {
        return Definition != null && MaterialBreakEffect.Play(Definition.breakEffect, GetComponent<Block>());
    }
}

// 职责：保存阵地战基地参考造价及共享战斗配置；出战不限制尺寸、预算和高度，供Inspector独立调参。
// 入口：Cost() / Validate()；不读写玩家蓝图。
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "HY-Sandbox/Siege Construction Settings")]
public sealed class SiegeConstructionSettings : ScriptableObject
{
    [SerializeField]
    private SiegeSettings combat;
    [SerializeField]
    private TextAsset starter;
    [SerializeField, Min(1)]
    private int coinsPerCell = 5;
    public SiegeSettings Combat => combat;
    public TextAsset Starter => starter;

    public long Cost(BlockDataList data)
    {
        long price = 0;
        foreach (BlockData block in data.blocks)
        {
            // The hangar interior is reserved empty space; price its shell using the shared design rate.
            price += HangarRules.IsHangar(block) ? HangarRules.ShellCost(block) : (long)block.x * block.y * block.z * Mathf.Max(1, coinsPerCell);
        }

        return price;
    }

    public bool Validate(BlockDataList data, out string reason)
    {
        try
        {
            WorldBuildingValidation.ValidateBlueprint(data);
            if (!HangarRules.Validate(data, out reason))
            {
                return false;
            }

            if (data.IsOutpost || data.blocks.Count == 0 || string.IsNullOrWhiteSpace(data.siegeCoreId) || !data.blocks.Exists(block => block.id == data.siegeCoreId))
            {
                reason = "Choose a base with one designated core (not an enemy outpost).";
                return false;
            }

            BlockData core = data.blocks.Find(block => block.id == data.siegeCoreId);
            GameObject prefab = Resources.Load<GameObject>(BlueprintBlockFactory.ToResourcePath(core.resourcePath));
            if (prefab.GetComponent<Durability>() == null)
            {
                reason = "The core must be a destructible building module.";
                return false;
            }

            reason = $"Design valid: {data.blocks.Count} modules, reference value {Cost(data)}. Support checked on deploy. " + Warnings(data);
            return true;
        }
        catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
        {
            reason = error.Message;
            return false;
        }
    }

    public string Warnings(BlockDataList data)
    {
        int generators = 0;
        int turrets = 0;
        foreach (BlockData block in data.blocks)
        {
            GameObject prefab = BlockPrefabCache.GetPrefab(BlueprintBlockFactory.ToResourcePath(block.resourcePath));
            generators += prefab != null && prefab.GetComponent<PowerGeneratingUnit>() != null ? 1 : 0;
            turrets += prefab != null && prefab.GetComponent<Turret>() != null ? 1 : 0;
        }

        return (generators == 0 ? "Warning: no generator. " : generators == 1 ? "Warning: single generator. " : string.Empty) + (turrets == 0 ? "Warning: no defensive turret. " : string.Empty);
    }
}

// 职责：保存新版阵地战基地的预算、尺寸边界和共享战斗配置，供Inspector独立调参。
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
    private int deploymentBudget = 20000;
    [SerializeField, Min(1)]
    private int coinsPerCell = 5;
    [SerializeField, Range(1, 64)]
    private int maximumWidth = 48;
    [SerializeField, Min(1)]
    private int maximumHeight = 32;
    public SiegeSettings Combat => combat;
    public TextAsset Starter => starter;
    public int DeploymentBudget => deploymentBudget;

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

            if (data.foundation.width > maximumWidth || data.foundation.length > maximumWidth || Cost(data) > deploymentBudget)
            {
                reason = $"Limit: {maximumWidth} x {maximumWidth} cells; budget {deploymentBudget}.";
                return false;
            }

            var occupied = new System.Collections.Generic.List<Bounds>();
            foreach (BlockData block in data.blocks)
            {
                Quaternion rotation = new Quaternion(block.rotX, block.rotY, block.rotZ, block.rotW);
                Vector3 euler = rotation.eulerAngles;
                Quaternion snapped = Quaternion.Euler(
                    Mathf.Round(euler.x / 90f) * 90f,
                    Mathf.Round(euler.y / 90f) * 90f,
                    Mathf.Round(euler.z / 90f) * 90f);
                Bounds bounds = BlockBounds.FromBlueprint(block, Matrix4x4.identity);
                if (Quaternion.Angle(rotation, snapped) > .1f || bounds.min.y < -.01f || bounds.max.y > maximumHeight || bounds.min.x < data.foundation.MinX - .01f || bounds.min.z < data.foundation.MinZ - .01f || bounds.max.x > data.foundation.MinX + data.foundation.width + .01f || bounds.max.z > data.foundation.MinZ + data.foundation.length + .01f)
                {
                    reason = $"Modules must fit the footprint, use quarter turns and stay below {maximumHeight}m.";
                    return false;
                }

                bounds.Expand(-.12f);
                if (occupied.Exists(other => other.Intersects(bounds)))
                {
                    reason = "Modules overlap inside the design.";
                    return false;
                }

                occupied.Add(bounds);
            }

            BlockData core = data.blocks.Find(block => block.id == data.siegeCoreId);
            GameObject prefab = Resources.Load<GameObject>(BlueprintBlockFactory.ToResourcePath(core.resourcePath));
            if (prefab.GetComponent<Durability>() == null)
            {
                reason = "The core must be a destructible building module.";
                return false;
            }

            reason = $"Design valid: {data.blocks.Count} modules, {Cost(data)} / {deploymentBudget}. Support checked on deploy. " + Warnings(data);
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

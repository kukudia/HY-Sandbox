// 职责：以大型建筑蓝图创建阵地战基地，独立绑定核心目标，并按建筑原蓝图报价/修缮。
// 入口：Create() / Quote() / Repair() / Dispose()；不复用舰船驾驶舱或舰船物理重组。
using System;
using System.Linq;
using UnityEngine;

public sealed class SiegeBase : IDisposable
{
    private readonly SiegeMatch match;
    private readonly float repairCoinsPerHealth;
    private readonly int replacementValue;
    private readonly Action coreLost;
    public BuildingRuntime Building
    {
        get;
    }
    public BlockDataList Blueprint
    {
        get;
    }
    public Block Core
    {
        get;
    }
    public ControlUnit Unit => Building != null ? Building.GetComponent<ControlUnit>() : null;
    public string Id => Building.InstanceId;
    public bool IsAlive => Building != null && Core != null && Core.isActiveAndEnabled && Core.GetComponentInParent<BuildingRuntime>() == Building;

    private SiegeBase(
        SiegeMatch match,
        float repairCoinsPerHealth,
        int replacementValue,
        Action coreLost,
        BuildingRuntime building,
        BlockDataList blueprint,
        Block core)
    {
        this.match = match;
        this.repairCoinsPerHealth = repairCoinsPerHealth;
        this.replacementValue = replacementValue;
        this.coreLost = coreLost;
        Building = building;
        Blueprint = blueprint;
        Core = core;
        Building.BlockDestroyed += OnBlockDestroyed;
    }

    public static SiegeBase Create(SiegeSession session, WorldBuildingManager buildings, Transform anchor)
    {
        if (buildings == null || session.Settings.BaseBlueprint == null)
        {
            throw new InvalidOperationException("Siege requires a building manager and a building blueprint.");
        }

        BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(session.Settings.BaseBlueprint.text);
        WorldBuildingValidation.ValidateBlueprint(blueprint);
        if (!blueprint.blocks.Any(block => block.id == session.Settings.BaseCoreBlockId))
        {
            throw new InvalidOperationException("The configured base core is missing from the building blueprint.");
        }

        return Create(
            session.Match,
            session.Settings,
            buildings,
            blueprint,
            session.Settings.BaseCoreBlockId,
            session.Settings.BaseBlueprint.name,
            anchor.position,
            anchor.rotation,
            session.Settings.BaseReplacementValue,
            session.LoseBase);
    }

    public static SiegeBase Create(
        SiegeMatch match,
        SiegeSettings settings,
        WorldBuildingManager buildings,
        BlockDataList source,
        string coreId,
        string name,
        Vector3 position,
        Quaternion rotation,
        int replacementValue,
        Action coreLost)
    {
        // Own an isolated snapshot; stripping cargo and later refits must never mutate the editor cache.
        BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(source));
        WorldBuildingValidation.ValidateBlueprint(blueprint);
        if (blueprint.blocks.Count(block => block.id == coreId) != 1)
        {
            throw new InvalidOperationException("Choose exactly one existing base core.");
        }

        foreach (BlockData block in blueprint.blocks)
        {
            block.cargo = null;
        }

        var state = new WorldBuildingState
        {
            instanceId = Guid.NewGuid().ToString("N"),
            blueprintName = name,
            faction = UnitFaction.Player,
            position = position,
            rotation = rotation,
            anchored = true,
            blueprint = blueprint,
            durability = Enumerable.Repeat(float.MaxValue, blueprint.blocks.Count).ToArray()
        };
        BuildingRuntime building = buildings.RestoreBuilding(state);
        Block core = building.Blocks.Single(block => block.uniqueId == state.instanceId + ":" + coreId);
        return new SiegeBase(
            match,
            settings.RepairCoinsPerHealth,
            replacementValue,
            coreLost,
            building,
            blueprint,
            core);
    }

    public long Quote()
    {
        if (!IsAlive)
        {
            return 0;
        }

        var owned = Building.Blocks.Where(block => block != null && block.gameObject.activeSelf).ToDictionary(block => block.uniqueId, StringComparer.Ordinal);
        int missing = 0;
        double lostHealth = 0;
        foreach (BlockData data in Blueprint.blocks)
        {
            if (!owned.TryGetValue(Id + ":" + data.id, out Block block))
            {
                missing++;
                continue;
            }

            Durability health = block.GetComponent<Durability>();
            if (health != null)
            {
                lostHealth += Mathf.Max(0f, health.maxDurability - health.currentDurability);
            }
        }

        return (long)Math.Ceiling(lostHealth * repairCoinsPerHealth + (double)replacementValue * missing / Blueprint.blocks.Count);
    }

    public bool Repair(out string reason)
    {
        reason = "Building repairs require a surviving core and an intermission.";
        if (!IsAlive || !match.CanRefit)
        {
            return false;
        }

        long price = Quote();
        if (!match.CanAfford(price))
        {
            reason = "Insufficient coins for base repair: " + price;
            return false;
        }

        if (!SiegeRefit.IsBuildingClear(Blueprint, Building.transform.position, Building.transform.rotation, Id, out reason) || !Building.TryRefit(Blueprint, out reason))
        {
            return false;
        }

        match.TrySpend(price);
        reason = "Base building refitted for " + price + " coins.";
        return true;
    }

    private void OnBlockDestroyed(Block block)
    {
        if (block == Core)
        {
            coreLost?.Invoke();
        }
    }

    public void Dispose()
    {
        if (Building != null)
        {
            Building.BlockDestroyed -= OnBlockDestroyed;
        }
    }
}

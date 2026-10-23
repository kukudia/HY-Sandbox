// 职责：以大型建筑蓝图创建阵地战基地，独立绑定核心目标，并按建筑原蓝图报价/修缮。
// 入口：Create() / Quote() / Repair() / Dispose()；不复用舰船驾驶舱或舰船物理重组。
using System;
using System.Linq;
using UnityEngine;

public sealed class SiegeBase : IDisposable
{
    private readonly SiegeSession session;
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

    private SiegeBase(SiegeSession session, BuildingRuntime building, BlockDataList blueprint, Block core)
    {
        this.session = session;
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

        foreach (BlockData block in blueprint.blocks)
        {
            block.cargo = null;
        }

        var state = new WorldBuildingState
        {
            instanceId = Guid.NewGuid().ToString("N"),
            blueprintName = session.Settings.BaseBlueprint.name,
            faction = UnitFaction.Player,
            position = anchor.position,
            rotation = anchor.rotation,
            anchored = true,
            blueprint = blueprint,
            durability = Enumerable.Repeat(float.MaxValue, blueprint.blocks.Count).ToArray()
        };
        BuildingRuntime building = buildings.RestoreBuilding(state);
        Block core = building.Blocks.Single(block => block.uniqueId == state.instanceId + ":" + session.Settings.BaseCoreBlockId);
        return new SiegeBase(session, building, blueprint, core);
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

        return (long)Math.Ceiling(lostHealth * session.Settings.RepairCoinsPerHealth + (double)session.Settings.BaseReplacementValue * missing / Blueprint.blocks.Count);
    }

    public bool Repair(out string reason)
    {
        reason = "Building repairs require a surviving core and an intermission.";
        if (!IsAlive || !session.Match.CanRefit)
        {
            return false;
        }

        long price = Quote();
        if (!session.Match.CanAfford(price))
        {
            reason = "Insufficient coins for base repair: " + price;
            return false;
        }

        if (!SiegeRefit.IsBuildingClear(Blueprint, Building.transform.position, Building.transform.rotation, Id, out reason) || !Building.TryRefit(Blueprint, out reason))
        {
            return false;
        }

        session.Match.TrySpend(price);
        reason = "Base building refitted for " + price + " coins.";
        return true;
    }

    private void OnBlockDestroyed(Block block)
    {
        if (block == Core)
        {
            session.LoseBase();
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

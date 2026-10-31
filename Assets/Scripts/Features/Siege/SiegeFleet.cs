// 职责：部署固定泊位舰队、保持唯一玩家驾驶权，并在波次边界切换舰队物理状态。
// 入口：CreateBase() / Deploy() / TakeControl() / SetCombatActive() / NotifyLost()。
using System;
using UnityEngine;

public sealed class SiegeFleet
{
    private readonly SiegeSession session;
    private readonly Transform[] docks;
    public SiegeFleetEntry[] Ships
    {
        get;
    }
    public SiegeBase Base
    {
        get; private set;
    }
    public SiegeFleetEntry Driven
    {
        get; private set;
    }

    public SiegeFleet(SiegeSession session, Transform[] docks)
    {
        this.session = session;
        this.docks = docks;
        Ships = new SiegeFleetEntry[docks.Length];
    }

    public int DeploymentCount => docks.Length;

    public Transform GetDeploymentPoint(int index)
    {
        return index >= 0 && index < docks.Length ? docks[index] : null;
    }

    public SiegeFleetEntry Find(string id)
    {
        return Array.Find(Ships, ship => ship != null && ship.Id == id && ship.IsAlive);
    }

    public bool CreateBase(Transform anchor)
    {
        Base = SiegeBase.Create(session, session.Buildings, anchor);
        SetBaseCamera();
        return true;
    }

    public bool Deploy(int optionIndex, out string reason)
    {
        return DeployAt(optionIndex, -1, out reason);
    }

    public bool DeployAt(int optionIndex, int requestedDock, out string reason)
    {
        reason = "Deployment is only available between waves.";
        if (!session.Match.CanRefit || optionIndex < 0 || optionIndex >= session.DefenderOptions.Length)
        {
            return false;
        }

        SiegeShipOption option = session.DefenderOptions[optionIndex];
        reason = "Insufficient coins.";
        if (!session.Match.CanAfford(option.Price))
        {
            return false;
        }

        BlockDataList blueprint = PrepareBlueprint(option.BlueprintJson, out string id);
        int slot = -1;
        bool foundFreeBay = false;
        if (requestedDock < -1 || requestedDock >= docks.Length)
        {
            reason = "Unknown deployment position.";
            return false;
        }

        string firstObstruction = null;
        for (int candidate = 0; candidate < Ships.Length; candidate++)
        {
            if (requestedDock >= 0 && requestedDock != candidate)
            {
                continue;
            }

            if (Ships[candidate] != null && Ships[candidate].IsAlive)
            {
                continue;
            }

            foundFreeBay = true;
            if (SiegeRefit.IsClear(blueprint, docks[candidate].position, docks[candidate].rotation, id, out string candidateReason))
            {
                slot = candidate;
                break;
            }

            firstObstruction ??= candidateReason;
        }

        if (slot < 0)
        {
            reason = foundFreeBay ? firstObstruction ?? "The selected deployment area is blocked." : "The deployment position is occupied.";
            return false;
        }

        SiegeFleetEntry entry = Spawn(
            blueprint,
            id,
            option.DisplayName,
            option.Price,
            docks[slot].position,
            docks[slot].rotation);
        if (entry == null)
        {
            reason = "Ship blueprint could not be restored.";
            return false;
        }

        session.Match.TrySpend(option.Price);
        Ships[slot] = entry;
        // 用户蓝图可能自带半格偏移的独立吊舱：如实提示，避免玩家以为船体无故散开。
        int parts = BlockGroupManager.GroupBlocks(new System.Collections.Generic.List<Block>(entry.Unit.GetComponentsInChildren<Block>())).Count;
        if (parts > 1)
        {
            Debug.LogWarning("Siege blueprint '" + option.DisplayName + "' deploys as " + parts + " disconnected hull parts; blocks are not connector-aligned.");
        }

        SiegeFleetPhysics.Park(entry.Unit);
        if (Driven == null || !Driven.IsAlive)
        {
            TakeControl(slot);
        }

        reason = "Deployed " + option.DisplayName + (parts > 1 ? "  ·  " + parts + " hull parts" : string.Empty);
        return true;
    }

    public bool TakeControl(int slot)
    {
        if (session.Match.Phase == SiegePhase.Defeat || slot < 0 || slot >= Ships.Length || Ships[slot] == null || !Ships[slot].IsAlive)
        {
            return false;
        }

        Driven = Ships[slot];
        foreach (SiegeFleetEntry entry in Ships)
        {
            if (entry == null || !entry.IsAlive)
            {
                continue;
            }

            bool manual = entry == Driven;
            entry.Cockpit.SetControlMode(manual ? UnitControlMode.Player : UnitControlMode.Autonomous);
            ControlUnit unit = entry.Unit;
            entry.LastBoundUnit = unit;
            unit.SetMovementInput(Vector3.zero);
            unit.RefreshChildren();
            EnemyController ai = unit.GetComponent<EnemyController>();
            if (ai != null)
            {
                ai.SetGuardAnchor(Base.Core.transform, session.Settings.GuardRadius);
                ai.enabled = session.Match.Phase == SiegePhase.Battle && !manual;
            }
        }

        SetCamera(Driven.Unit);
        return true;
    }

    public void SetCombatActive(bool active)
    {
        using var measurement = SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.SiegeFleetPhysics);
        foreach (SiegeFleetEntry entry in Ships)
        {
            ControlUnit unit = entry?.Unit;
            if (unit == null)
            {
                continue;
            }

            if (active)
            {
                SiegeFleetPhysics.Activate(unit);
            }
            else
            {
                SiegeFleetPhysics.Park(unit);
            }
        }
    }

    public void RepairBetweenWaves(out int repairedCount, out int attemptedCount, out long spent, out string firstFailure)
    {
        repairedCount = 0;
        attemptedCount = 0;
        spent = 0;
        firstFailure = null;
        if (!session.Settings.AutoRepairFleet)
        {
            return;
        }

        for (int offset = 0; offset < Ships.Length; offset++)
        {
            int slot = Driven != null ? (Array.IndexOf(Ships, Driven) + offset) % Ships.Length : offset;
            SiegeFleetEntry entry = Ships[slot];
            if (entry == null || !entry.IsAlive || SiegeRefit.Quote(entry, session.Settings) == 0)
            {
                continue;
            }

            attemptedCount++;
            long balanceBefore = session.Match.Coins;
            bool repaired = SiegeRefit.Repair(entry, session, out string reason);
            if (repaired)
            {
                repairedCount++;
                spent += balanceBefore - session.Match.Coins;
            }
            else if (firstFailure == null)
            {
                firstFailure = entry.DisplayName + ": " + reason;
            }

            if (repaired && entry == Driven)
            {
                SetCamera(entry.Unit);
            }
        }
    }

    public void RefreshBindings()
    {
        foreach (SiegeFleetEntry entry in Ships)
        {
            ControlUnit unit = entry?.Unit;
            if (unit == null || unit == entry.LastBoundUnit)
            {
                continue;
            }

            entry.LastBoundUnit = unit;
            unit.RefreshChildren();
            EnemyController ai = unit.GetComponent<EnemyController>();
            if (ai != null)
            {
                ai.SetGuardAnchor(Base.Core.transform, session.Settings.GuardRadius);
                ai.enabled = session.Match.Phase == SiegePhase.Battle && entry != Driven;
            }

            if (entry == Driven)
            {
                SetCamera(unit);
            }
        }
    }

    public void NotifyLost(Cockpit cockpit)
    {
        foreach (SiegeFleetEntry entry in Ships)
        {
            if (entry?.Cockpit == cockpit)
            {
                entry.MarkLost();
                if (entry == Driven)
                {
                    Driven = null;
                    int next = Array.FindIndex(Ships, ship => ship != null && ship.IsAlive);
                    if (next >= 0)
                    {
                        TakeControl(next);
                    }
                    else if (Base?.Unit != null)
                    {
                        SetBaseCamera();
                    }
                }

                break;
            }
        }
    }

    private SiegeFleetEntry Spawn(
        BlockDataList blueprint,
        string id,
        string displayName,
        int price,
        Vector3 position,
        Quaternion rotation)
    {
        ControlUnit unit = ModularUnitFactory.Spawn(blueprint, displayName, position, rotation, UnitFaction.Player);
        if (unit == null)
        {
            return null;
        }

        unit.runtimeUnitId = id;
        unit.AssignRuntimeOwnershipToBlocks(true);
        unit.cockpit.SetControlMode(UnitControlMode.Autonomous);
        unit.RefreshChildren();
        unit.GetComponent<EnemyController>().enabled = false;
        foreach (Block block in unit.GetComponentsInChildren<Block>())
        {
            block.showConnectors = false;
            block.showLabel = false;
        }

        return new SiegeFleetEntry(
            id,
            displayName,
            blueprint,
            unit.cockpit,
            price,
            position,
            rotation);
    }

    public static BlockDataList PrepareBlueprint(TextAsset asset, out string id)
    {
        return PrepareBlueprint(asset.text, out id);
    }

    public static BlockDataList PrepareBlueprint(string json, out string id)
    {
        id = Guid.NewGuid().ToString("N");
        BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(json);
        for (int index = 0; index < blueprint.blocks.Count; index++)
        {
            blueprint.blocks[index].id = id + ":" + index;
            // 部署购买的是空载机体，蓝图货仓快照不能产生免费的物资副本。
            blueprint.blocks[index].cargo = null;
        }

        return blueprint;
    }

    public void RestoreView()
    {
        if (Driven != null && Driven.IsAlive)
        {
            SetCamera(Driven.Unit);
        }
        else
        {
            SetBaseCamera();
        }
    }

    private void SetCamera(ControlUnit unit)
    {
        SiegeFleetView.Focus(unit, Driven != null && Driven.Unit == unit ? Driven.MaximumDurability : 0f);
    }

    private void SetBaseCamera()
    {
        SiegeFleetView.FocusBase(Base);
    }
}

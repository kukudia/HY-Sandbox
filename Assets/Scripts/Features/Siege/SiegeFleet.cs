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
    public SiegeFleetEntry Base
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

    public bool CreateBase(Transform anchor)
    {
        Base = Create(
            session.Settings.BaseBlueprint,
            "Bastion Core",
            1500,
            anchor.position,
            anchor.rotation,
            true);
        if (Base == null)
        {
            return false;
        }

        Base.Cockpit.SetAnchored(true);
        Base.Unit.GetComponent<Rigidbody>().isKinematic = true;
        Base.LastBoundUnit = Base.Unit;
        SetCamera(Base.Unit);
        return true;
    }

    public bool Deploy(int optionIndex, out string reason)
    {
        reason = "Deployment is only available between waves.";
        if (!session.Match.CanRefit || optionIndex < 0 || optionIndex >= session.Settings.Ships.Length)
        {
            return false;
        }

        SiegeShipOption option = session.Settings.Ships[optionIndex];
        int slot = Array.FindIndex(Ships, entry => entry == null || !entry.IsAlive);
        reason = slot < 0 ? "All deployment bays are occupied." : "Insufficient coins.";
        if (slot < 0 || !session.Match.CanAfford(option.Price))
        {
            return false;
        }

        BlockDataList blueprint = PrepareBlueprint(option.Blueprint, out string id);
        if (!SiegeRefit.IsClear(blueprint, docks[slot].position, docks[slot].rotation, id, out reason))
        {
            return false;
        }

        SiegeFleetEntry entry = Spawn(
            blueprint,
            id,
            option.DisplayName,
            option.Price,
            docks[slot].position,
            docks[slot].rotation,
            false);
        if (entry == null)
        {
            reason = "Ship blueprint could not be restored.";
            return false;
        }

        session.Match.TrySpend(option.Price);
        Ships[slot] = entry;
        Park(entry.Unit);
        if (Driven == null || !Driven.IsAlive)
        {
            TakeControl(slot);
        }

        reason = "Deployed " + option.DisplayName;
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
                ai.SetGuardAnchor(Base.Cockpit.transform, session.Settings.GuardRadius);
                ai.enabled = !manual;
            }
        }

        SetCamera(Driven.Unit);
        return true;
    }

    public void SetCombatActive(bool active)
    {
        foreach (SiegeFleetEntry entry in Ships)
        {
            ControlUnit unit = entry?.Unit;
            if (unit == null)
            {
                continue;
            }

            if (!active)
            {
                Park(unit);
                continue;
            }

            unit.GetComponent<Rigidbody>().isKinematic = false;
            foreach (HoverThruster thruster in unit.hoverThrusters)
            {
                thruster.isHovered = true;
            }

            if (unit.hoverFlightController != null)
            {
                unit.hoverFlightController.setHeight = true;
                unit.hoverFlightController.targetHoverHeight = Mathf.Max(8f, unit.transform.position.y);
            }

            EnemyController ai = unit.GetComponent<EnemyController>();
            if (ai != null)
            {
                ai.enabled = !unit.AcceptsPlayerInput;
            }
        }
    }

    public void RefreshBindings()
    {
        if (Base?.Unit != null && Base.LastBoundUnit != Base.Unit)
        {
            Base.LastBoundUnit = Base.Unit;
            if (Driven == null)
            {
                SetCamera(Base.Unit);
            }
        }

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
                ai.SetGuardAnchor(Base.Cockpit.transform, session.Settings.GuardRadius);
                ai.enabled = session.Match.Phase == SiegePhase.Battle && entry != Driven;
            }

            if (entry == Driven)
            {
                SetCamera(unit);
            }
        }
    }

    public bool NotifyLost(Cockpit cockpit)
    {
        if (Base?.Cockpit == cockpit)
        {
            Base.MarkLost();
            return true;
        }

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
                        SetCamera(Base.Unit);
                    }
                }

                break;
            }
        }

        return false;
    }

    private SiegeFleetEntry Create(
        TextAsset asset,
        string displayName,
        int price,
        Vector3 position,
        Quaternion rotation,
        bool isBase)
    {
        BlockDataList blueprint = PrepareBlueprint(asset, out string id);
        return Spawn(
            blueprint,
            id,
            displayName,
            price,
            position,
            rotation,
            isBase);
    }

    private SiegeFleetEntry Spawn(
        BlockDataList blueprint,
        string id,
        string displayName,
        int price,
        Vector3 position,
        Quaternion rotation,
        bool isBase)
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
            rotation,
            isBase);
    }

    public static BlockDataList PrepareBlueprint(TextAsset asset, out string id)
    {
        id = Guid.NewGuid().ToString("N");
        BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(asset.text);
        for (int index = 0; index < blueprint.blocks.Count; index++)
        {
            blueprint.blocks[index].id = id + ":" + index;
            // 部署购买的是空载机体，蓝图货仓快照不能产生免费的物资副本。
            blueprint.blocks[index].cargo = null;
        }

        return blueprint;
    }

    private static void Park(ControlUnit unit)
    {
        Rigidbody body = unit.GetComponent<Rigidbody>();
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        body.isKinematic = true;
        unit.SetMovementInput(Vector3.zero);
        EnemyController ai = unit.GetComponent<EnemyController>();
        if (ai != null)
        {
            ai.enabled = false;
        }
    }

    private static void SetCamera(ControlUnit unit)
    {
        PlayManager.instance.blocksParent = unit.transform;
        GameManager.instance.blocksParent = unit.transform;
        PlayManager.instance.lastHeight = unit.transform.position.y;
        if (CameraController.instance != null)
        {
            CameraController.instance.playerBody = unit.transform;
            CameraController.instance.currentMode = CameraMode.ThirdPersonLock;
        }
    }
}

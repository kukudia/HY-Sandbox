// 职责：部署固定泊位舰队、保持唯一玩家驾驶权，并在波次边界切换舰队物理状态。
// 入口：CreateBase() / DeployAt() / ReplaceAt() / CancelDeployment() / SetCombatActive()。
using System;
using UnityEngine;

public sealed class SiegeFleet
{
    private readonly ISiegeDeploymentHost session;
    private readonly bool manageCamera;
    private string[] hangarIds;
    private Transform[] docks;
    public SiegeFleetEntry[] Ships
    {
        get; private set;
    }
    public SiegeBase Base
    {
        get; private set;
    }
    public SiegeFleetEntry Driven
    {
        get; private set;
    }

    public SiegeFleet(
        ISiegeDeploymentHost session,
        Transform[] docks,
        SiegeBase existingBase = null,
        bool manageCamera = true)
    {
        this.session = session;
        this.docks = docks;
        this.manageCamera = manageCamera;
        Base = existingBase;
        Ships = new SiegeFleetEntry[docks.Length];
    }

    public int DeploymentCount => docks.Length;
    public bool UsesHangars => hangarIds != null;

    public Transform GetDeploymentPoint(int index)
    {
        HangarModule hangar = GetHangar(index);
        return hangar != null ? hangar.transform : index >= 0 && index < docks.Length && hangarIds == null ? docks[index] : null;
    }

    public void RefreshHangars(HangarModule[] hangars)
    {
        if (hangarIds != null)
        {
            return; // Slot identity survives module destruction and paid blueprint restoration.
        }

        int count = Mathf.Min(hangars.Length, HangarRules.MaximumCount);
        docks = new Transform[count];
        hangarIds = new string[count];
        Ships = new SiegeFleetEntry[count];
        for (int i = 0; i < count; i++)
        {
            docks[i] = hangars[i].transform;
            hangarIds[i] = hangars[i].GetComponent<Block>().uniqueId;
            hangars[i].SetCombat(true);
        }
    }

    public HangarModule GetHangar(int slot)
    {
        if (slot < 0 || slot >= docks.Length || hangarIds == null || Base?.Building == null)
        {
            return null;
        }

        foreach (Block block in Base.Building.Blocks)
        {
            if (block != null && block.uniqueId == hangarIds[slot] && block.isActiveAndEnabled)
            {
                HangarModule hangar = block.GetComponent<HangarModule>();
                if (hangar != null && !hangar.IsCombat)
                {
                    hangar.SetCombat(true);
                }

                return hangar;
            }
        }

        return null;
    }

    public bool FitsHangar(int slot, BlockDataList blueprint)
    {
        if (slot < 0 || slot >= docks.Length)
        {
            return false;
        }

        HangarModule hangar = GetHangar(slot);
        return hangarIds == null ? docks[slot] != null : hangar != null && hangar.IsOperational && hangar.Fits(blueprint);
    }

    public Pose GetDeploymentPose(int slot, BlockDataList blueprint)
    {
        HangarModule hangar = GetHangar(slot);
        if (hangar != null)
        {
            return hangar.GetDockPose(blueprint);
        }

        Transform point = GetDeploymentPoint(slot);
        return point != null ? new Pose(point.position, point.rotation) : default;
    }

    public void Scrap(int slot)
    {
        SiegeFleetEntry entry = slot >= 0 && slot < Ships.Length ? Ships[slot] : null;
        if (entry == null)
        {
            return;
        }

        foreach (ControlUnit unit in new System.Collections.Generic.List<ControlUnit>(ControlUnit.RuntimeUnits))
        {
            if (unit != null && (unit == entry.Unit || unit.runtimeUnitId == entry.Id || Array.Exists(unit.GetComponentsInChildren<RuntimeUnitMember>(), member => member.ownerUnitId == entry.Id)))
            {
                unit.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(unit.gameObject);
            }
        }

        entry.MarkLost();
        if (Driven == entry)
        {
            Driven = null;
            SetBaseCamera();
        }

        GetHangar(slot)?.SetDoorOpen(false);
    }

    public SiegeFleetEntry Find(string id)
    {
        return Array.Find(Ships, ship => ship != null && ship.Id == id && ship.IsAlive);
    }

    public bool CreateBase(Transform anchor)
    {
        SiegeSession prototype = (SiegeSession)session;
        Base = SiegeBase.Create(prototype, prototype.Buildings, anchor);
        HangarModule[] hangars = Base.Building.GetComponentsInChildren<HangarModule>();
        if (hangars.Length > 0)
        {
            RefreshHangars(hangars);
        }

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
            if (!FitsHangar(candidate, blueprint))
            {
                firstObstruction ??= "Blueprint exceeds the hangar clear size or the hangar is destroyed.";
                continue;
            }

            Pose pose = GetDeploymentPose(candidate, blueprint);
            if (SiegeRefit.IsClear(blueprint, pose.position, pose.rotation, id, out string candidateReason))
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
            GetDeploymentPose(slot, blueprint).position,
            GetDeploymentPose(slot, blueprint).rotation);
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
        if (manageCamera && (Driven == null || !Driven.IsAlive))
        {
            TakeControl(slot);
        }

        reason = "Deployed " + option.DisplayName + (parts > 1 ? "  ·  " + parts + " hull parts" : string.Empty);
        return true;
    }

    /// <summary>完整舰全额退回；战损按既有维修报价扣除，换购不能绕过维修费用。</summary>
    public long CancellationCredit(int slot)
    {
        SiegeFleetEntry entry = slot >= 0 && slot < Ships.Length ? Ships[slot] : null;
        return entry?.IsAlive == true ? Math.Max(0, entry.PurchasePrice - SiegeRefit.Quote(entry, session.Settings)) : 0;
    }

    public bool CancelDeployment(int slot, out string reason)
    {
        reason = "Undeploy is only available for a surviving ship between waves.";
        if (!session.Match.CanRefit || slot < 0 || slot >= Ships.Length || Ships[slot]?.IsAlive != true)
        {
            return false;
        }

        long credit = CancellationCredit(slot);
        if (!session.Match.TryExchange(0, credit))
        {
            return false;
        }

        Scrap(slot);
        Ships[slot] = null;
        reason = "Undeployed ship; refunded " + credit.ToString("N0") + " credits.";
        return true;
    }

    public bool ReplaceAt(int optionIndex, int slot, out string reason)
    {
        reason = "Replacement requires a surviving ship and an intermission.";
        if (!session.Match.CanRefit || slot < 0 || slot >= Ships.Length || Ships[slot]?.IsAlive != true || optionIndex < 0 || optionIndex >= session.DefenderOptions.Length)
        {
            return false;
        }

        SiegeFleetEntry previous = Ships[slot];
        SiegeShipOption option = session.DefenderOptions[optionIndex];
        BlockDataList blueprint = PrepareBlueprint(option.BlueprintJson, out string id);
        if (!FitsHangar(slot, blueprint))
        {
            reason = "Blueprint exceeds the hangar clear size or the hangar is destroyed.";
            return false;
        }

        long credit = CancellationCredit(slot);
        if (!session.Match.CanExchange(option.Price, credit))
        {
            reason = "Insufficient credits after the old ship trade-in.";
            return false;
        }

        Pose pose = GetDeploymentPose(slot, blueprint);
        if (!SiegeRefit.IsClear(blueprint, pose.position, pose.rotation, previous.Id, out reason))
        {
            return false;
        }

        // Validate and restore the replacement before touching the old ship or balance.
        // Both hulls coexist only within this synchronous transaction, before the next physics step.
        SiegeFleetEntry replacement = Spawn(
            blueprint,
            id,
            option.DisplayName,
            option.Price,
            pose.position,
            pose.rotation);
        if (replacement == null)
        {
            reason = "Replacement could not be restored; old ship and credits retained.";
            return false;
        }

        bool restorePilot = Driven == previous;
        session.Match.TryExchange(option.Price, credit);
        Scrap(slot);
        Ships[slot] = replacement;
        SiegeFleetPhysics.Park(replacement.Unit);
        if (manageCamera && (restorePilot || Driven == null || !Driven.IsAlive))
        {
            TakeControl(slot);
        }

        reason = "Replaced with " + option.DisplayName + "; trade-in " + credit.ToString("N0") + " / price " + option.Price.ToString("N0") + " credits.";
        return true;
    }

    public bool TakeControl(int slot)
    {
        if (session.Match.Phase == SiegePhase.Defeat || session.Match.Phase == SiegePhase.Returning || slot < 0 || slot >= Ships.Length || Ships[slot] == null || !Ships[slot].IsAlive || session.FlightGuidance?.IsGuiding(Ships[slot]) == true)
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
                GetHangar(Array.IndexOf(Ships, entry))?.SetDoorOpen(true);
                SiegeFleetPhysics.Activate(unit);
            }
            else
            {
                SiegeFleetPhysics.Park(unit);
                GetHangar(Array.IndexOf(Ships, entry))?.SetDoorOpen(false);
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
                ai.enabled = session.Match.Phase == SiegePhase.Battle && entry != Driven && session.FlightGuidance?.IsGuiding(entry) != true;
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

        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(unit.gameObject, Base.Building.gameObject.scene);
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
        if (!manageCamera)
        {
            return;
        }

        SiegeFleetView.Focus(unit, Driven != null && Driven.Unit == unit ? Driven.MaximumDurability : 0f);
    }

    private void SetBaseCamera()
    {
        if (manageCamera)
        {
            SiegeFleetView.FocusBase(Base);
        }
    }
}

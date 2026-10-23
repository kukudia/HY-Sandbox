// 职责：托管阵地战会话生命周期、波次入口和基地失败；不改变现有Main WorldSession流程。
// 入口：Begin() / BeginNextWave() / End()；场景通过Inspector引用设置和泊位。
using UnityEngine;
using System;
using System.IO;

public sealed class SiegeSession : MonoBehaviour
{
    [SerializeField]
    private SiegeSettings settings;
    [SerializeField]
    private Transform baseDock;
    [SerializeField]
    private Transform[] shipDocks = new Transform[4];
    [SerializeField, Tooltip("大型建筑蓝图基地的运行管理器，独立于舰船。")]
    private WorldBuildingManager buildings;
    public WorldBuildingManager Buildings => buildings;

    private SiegeWaveDirector waves;
    private SiegeWreckBudget wrecks;
    public SiegeWreckBudget Wrecks => wrecks;

    private bool previousBackground;
    private SiegePhase previousPhase;
    public string Status { get; private set; } = "Deploy a fleet, then begin the first wave.";
    public SiegeSettings Settings => settings;
    public SiegeShipOption[] DefenderOptions { get; private set; } = Array.Empty<SiegeShipOption>();
    public SiegeShipOption[] AttackerOptions { get; private set; } = Array.Empty<SiegeShipOption>();
    public string BlueprintStatus
    {
        get; private set;
    }
    public int SelectedAttacker { get; private set; } = -1;

    public void SelectAttacker(int index)
    {
        if (Match != null && Match.CanRefit && index >= -1 && index < AttackerOptions.Length)
        {
            SelectedAttacker = index;
        }
    }

    public bool RefreshBlueprints()
    {
        if (Match != null && !Match.CanRefit)
        {
            return false;
        }

        if (settings.UsePlayerBlueprintFolders)
        {
            DefenderOptions = SiegeBlueprintCatalog.Load(
                Path.Combine(Application.persistentDataPath, "Saves"),
                settings.DeploymentCoinsPerBlock,
                out string defenders);
            AttackerOptions = SiegeBlueprintCatalog.Load(
                Path.Combine(Application.persistentDataPath, "EnemyBlueprints"),
                settings.DeploymentCoinsPerBlock,
                out string attackers);
            BlueprintStatus = defenders + " " + attackers;
        }
        else
        {
            DefenderOptions = settings.Ships;
            AttackerOptions = settings.Ships;
            BlueprintStatus = "Fixed benchmark blueprints.";
        }

        SelectedAttacker = -1;
        Status = BlueprintStatus;
        return true;
    }

    public SiegeMatch Match
    {
        get; private set;
    }
    public SiegeFleet Fleet
    {
        get; private set;
    }
    public bool IsRunning => Match != null && Match.Phase != SiegePhase.Defeat;

    private void Awake()
    {
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        PlayManager.instance?.BindSiege(this);
        if (buildings != null)
        {
            buildings.SimulationEnabled = false;
        }
    }

    private void Start()
    {
        PlayManager.instance.BindSiege(this);
        if (!Begin())
        {
            Status = "Siege setup failed. Check the scene configuration.";
            Debug.LogError(Status);
        }
    }

    public bool Begin()
    {
        if (settings == null || buildings == null || baseDock == null || shipDocks == null || shipDocks.Length == 0 || System.Array.Exists(shipDocks, dock => dock == null) || Match != null)
        {
            return false;
        }

        Match = settings.CreateMatch();
        RefreshBlueprints();
        Fleet = new SiegeFleet(this, shipDocks);
        if (!Fleet.CreateBase(baseDock))
        {
            Match = null;
            return false;
        }

        waves = new SiegeWaveDirector(this);
        wrecks = new SiegeWreckBudget(gameObject.scene);
        previousPhase = Match.Phase;
        PlayManager.instance.playMode = false;
        return true;
    }

    public bool BeginNextWave()
    {
        if (AttackerOptions.Length == 0)
        {
            Status = "Select a valid Enemy blueprint before starting a wave.";
            return false;
        }

        if (!IsRunning || !Match.BeginNextWave())
        {
            return false;
        }

        Fleet.SetCombatActive(true);
        SetSimulationActive(true);
        previousPhase = SiegePhase.Battle;
        Status = "Wave " + Match.Wave + " incoming. Protect the base core.";
        return true;
    }

    public bool TryDeploy(int optionIndex, out string reason)
    {
        reason = "The siege is not ready.";
        bool deployed = Fleet != null && Fleet.Deploy(optionIndex, out reason);
        Status = reason;
        return deployed;
    }

    public bool TryTakeControl(int slot)
    {
        return Fleet != null && Fleet.TakeControl(slot);
    }

    public bool TryRepair(bool repairBase)
    {
        string reason = "The siege is not ready.";
        bool repaired = Fleet != null && (repairBase ? Fleet.Base.Repair(out reason) : SiegeRefit.Repair(Fleet.Driven, this, out reason));
        Status = reason;
        return repaired;
    }

    public void End()
    {
        waves?.Stop();
        Fleet?.SetCombatActive(false);
        Match?.LoseBase();
        if (PlayManager.instance != null)
        {
            SetSimulationActive(false);
        }
    }

    public void LoseBase()
    {
        if (Match == null)
        {
            return;
        }

        Match.LoseBase();
        waves?.Stop();
        Fleet?.SetCombatActive(false);
        SetSimulationActive(false);
        Status = "Base core lost. The siege is over.";
    }

    public void NotifyCockpitDestroyed(Cockpit cockpit)
    {
        Fleet?.NotifyLost(cockpit);
    }

    private void Update()
    {
        if (!IsRunning)
        {
            return;
        }

        if (!Fleet.Base.IsAlive)
        {
            LoseBase();
            return;
        }

        Fleet.RefreshBindings();
        wrecks.Tick(
            Time.time,
            settings.MaximumEnemyWreckGroups,
            settings.MaximumEnemyWreckBlocks,
            settings.EnemyWreckLifetime,
            settings.EnemyWreckRetirePerScan);
        waves.Tick(Time.time);
        if (previousPhase == SiegePhase.Battle && Match.Phase == SiegePhase.Intermission)
        {
            SetSimulationActive(false);
            Fleet.SetCombatActive(false);
            Status = "Wave cleared. Repair and reinforce before the next attack.";
            RepairBaseBetweenWaves();
        }

        previousPhase = Match.Phase;
    }

    /// <summary>波次边界调用一次，避免每帧重复报价、净空查询或失败重试。</summary>
    private void RepairBaseBetweenWaves()
    {
        if (!settings.AutoRepairBase || !Fleet.Base.IsAlive || Fleet.Base.Quote() == 0)
        {
            return;
        }

        Fleet.Base.Repair(out string reason);
        Status = "Wave cleared. Auto-refit: " + reason;
    }

    private void OnDestroy()
    {
        waves?.Stop();
        Fleet?.Base?.Dispose();
        Application.runInBackground = previousBackground;
    }

    private void SetSimulationActive(bool active)
    {
        PlayManager.instance.playMode = active;
        buildings.SimulationEnabled = active;
        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (unit == null || unit.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            Rigidbody body = unit.GetComponent<Rigidbody>();
            if (!active && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = !active || (unit.Building != null && unit.Building.Anchored) || (unit.cockpit != null && unit.cockpit.Anchored);
        }
    }
}

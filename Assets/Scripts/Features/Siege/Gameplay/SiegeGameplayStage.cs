// 职责：新版基地战的自动部署、真实守波和整备；共享波次/修缮机制，不依赖旧Session/HUD。
// 入口：Enter() / BeginWave() / Repair() / Retreat() / Exit()。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeGameplayStage : MonoBehaviour, ISiegeDeploymentHost
{
    [SerializeField]
    private Transform overview;
    [SerializeField]
    private Button returnButton;
    [SerializeField]
    private SiegeConstructionSettings settings;
    [SerializeField]
    private WorldBuildingManager buildings;
    [SerializeField]
    private BoxCollider arena;
    [SerializeField]
    private SiegeConstructionPanel panel;
    [SerializeField]
    private Transform baseSpawn;
    [SerializeField]
    private SiegeFlightSettings flightSettings = new SiegeFlightSettings();
    private SiegeReturnFlight flight;
    private bool returning;
    private SiegeShipOption[] defenders = Array.Empty<SiegeShipOption>();
    public SiegeSettings Settings => settings.Combat;
    public SiegeFleet Fleet
    {
        get; private set;
    }
    public SiegeReturnFlight FlightGuidance => flight;
    public SiegeShipOption[] DefenderOptions => defenders;
    public SiegeShipOption[] AttackerOptions => attackers;
    public int SelectedDefender { get; private set; } = -1;
    public int SelectedAttacker { get; private set; } = -1;
    public bool IsEditingBlueprint => false;
    public bool IsRunning => entered && !Settled && Match != null && Match.Phase != SiegePhase.Defeat;

    public event Action RefitChanged;
    private readonly Dictionary<GameObject, bool> environment = new Dictionary<GameObject, bool>();
    private readonly Dictionary<Rigidbody, bool> parkedBodies = new Dictionary<Rigidbody, bool>();
    private GameModeController owner;
    private BlockDataList design;
    private TextMesh coreMarker;
    private string designWarnings;
    private SiegeWaveDirector waves;
    private SiegeWreckBudget wrecks;
    private SiegeShipOption[] attackers = Array.Empty<SiegeShipOption>();
    private Vector3 position;
    private Quaternion rotation;
    private string selectedName;
    private bool entered;
    public SiegeMatch Match
    {
        get; private set;
    }
    public SiegeBase Base
    {
        get; private set;
    }
    public string Status
    {
        get; private set;
    }
    public string DesignStatus
    {
        get; private set;
    }

    private bool canDeploy;
    public bool Settled
    {
        get; private set;
    }

    public void Enter(GameModeController host, GameObject[] buildEnvironment, string blueprintName = null)
    {
        owner = host;
        entered = true;
        Match = settings.Combat.CreateMatch();
        Match.EnableReturnSettlement();
        buildings.SimulationEnabled = false;
        wrecks = new SiegeWreckBudget(gameObject.scene);
        foreach (GameObject item in buildEnvironment)
        {
            if (item != null && !environment.ContainsKey(item))
            {
                environment.Add(item, item.activeSelf);
                item.SetActive(false);
            }
        }

        CameraController camera = CameraController.instance;
        camera.StopCameraMotion();
        camera.currentMode = CameraMode.Lock;
        camera.transform.SetPositionAndRotation(overview.position, overview.rotation);
        returnButton.onClick.AddListener(ReturnOrRetreat);
        attackers = SiegeBlueprintCatalog.Load(Path.Combine(Application.persistentDataPath, "EnemyBlueprints"), 5, out _);
        if (attackers.Length == 0)
        {
            var options = new List<SiegeShipOption>();
            foreach (SiegeShipOption option in settings.Combat.Ships)
            {
                if (option == null || string.IsNullOrWhiteSpace(option.BlueprintJson))
                {
                    continue;
                }

                BlockDataList data = JsonUtility.FromJson<BlockDataList>(option.BlueprintJson);
                if (ModularUnitFactory.IsValidBlueprint(data, option.DisplayName))
                {
                    options.Add(new SiegeShipOption(option.DisplayName, option.BlueprintJson, option.Price, data.blocks.Count));
                }
            }

            attackers = options.ToArray();
        }

        RefreshDefenders();
        selectedName = string.IsNullOrEmpty(blueprintName) ? "Starter Bastion" : blueprintName;
        string json = string.IsNullOrEmpty(blueprintName) ? settings.Starter.text : File.ReadAllText(SaveManager.instance.GetBaseBlueprintPath(blueprintName));
        design = JsonUtility.FromJson<BlockDataList>(json);
        if (!settings.Validate(design, out string reason))
        {
            throw new InvalidOperationException(reason);
        }

        if (baseSpawn == null)
        {
            throw new InvalidOperationException("Siege scene requires an authored base spawn.");
        }

        rotation = baseSpawn.rotation;
        // The spawn denotes the foundation centre, independent of the saved footprint size.
        position = baseSpawn.position - rotation * design.foundation.Center;
        position.y = arena.bounds.max.y;
        designWarnings = settings.Warnings(design);
        if (!Deploy())
        {
            throw new InvalidOperationException(Status ?? DesignStatus);
        }

        panel.Bind(this);
    }

    private void ValidatePose()
    {
        canDeploy = false;
        if (design == null || Base != null || Settled)
        {
            return;
        }

        Bounds bounds = arena.bounds;
        for (int i = 0; i < 4; i++)
        {
            Vector3 corner = position + rotation * new Vector3(
                design.foundation.MinX + ((i & 1) == 0 ? 0f : design.foundation.width),
                0f,
                design.foundation.MinZ + ((i & 2) == 0 ? 0f : design.foundation.length));
            if (corner.x < bounds.min.x + .1f || corner.x > bounds.max.x - .1f || corner.z < bounds.min.z + .1f || corner.z > bounds.max.z - .1f)
            {
                DesignStatus = "The entire footprint must remain inside the arena.";
                return;
            }
        }

        if (!SiegeRefit.IsBuildingClear(design, position, rotation, null, out string obstruction))
        {
            DesignStatus = obstruction;
            return;
        }

        canDeploy = true;
        DesignStatus = $"{design.blocks.Count} modules | {settings.Cost(design)} / {settings.DeploymentBudget} budget\nSupport checked on confirm. Cargo excluded. {designWarnings}";
    }

    private bool Deploy()
    {
        ValidatePose();
        if (!canDeploy)
        {
            return false;
        }

        try
        {
            Base = SiegeBase.Create(
                Match,
                settings.Combat,
                buildings,
                design,
                design.siegeCoreId,
                selectedName,
                position,
                rotation,
                (int)settings.Cost(design),
                LoseBase);
            if (!Base.Building.HasFullFoundationSupport())
            {
                Base.Dispose();
                buildings.RemoveBuilding(Base.Building);
                Base = null;
                Status = "Deployment rejected: every connected group needs a downward connector on its foundation.";
                return false;
            }

            HangarModule[] hangars = Base.Building.GetComponentsInChildren<HangarModule>();
            Array.Sort(
                hangars,
                (a, b) => string.CompareOrdinal(a.GetComponent<Block>().uniqueId, b.GetComponent<Block>().uniqueId));
            var docks = new Transform[hangars.Length];
            for (int i = 0; i < hangars.Length; i++)
            {
                hangars[i].SetCombat(true);
                docks[i] = hangars[i].transform;
            }

            Fleet = new SiegeFleet(this, docks, Base, false);
            Fleet.RefreshHangars(hangars);
            flight = new SiegeReturnFlight(Fleet, flightSettings, this);
            ShowCoreMarker(Base.Core.transform, Vector3.up * (Base.Core.y * .5f + 1f));
            canDeploy = false;
            waves = new SiegeWaveDirector(
                Match,
                settings.Combat,
                () => Base,
                () => attackers,
                () => SelectedAttacker,
                gameObject.scene);
            Status = "Base deployed. Start the first wave. Repair coins are separate from the design budget.";
            DesignStatus = selectedName + "\n" + designWarnings;
            return true;
        }
        catch (Exception error) when (error is InvalidOperationException || error is ArgumentException)
        {
            Base?.Dispose();
            buildings.ClearSession();
            Base = null;
            Status = "Deployment failed: " + error.Message;
            return false;
        }
    }

    public bool BeginWave()
    {
        if (Settled || Base == null || !Base.IsAlive || attackers.Length == 0 || !Match.BeginNextWave())
        {
            Status = "A surviving base and valid attackers are required to start a wave.";
            return false;
        }

        SetSimulation(true);
        Fleet.SetCombatActive(true);
        flight.BeginLaunch();
        RefitChanged?.Invoke();
        Status = "Defend the core. Repairs unlock between waves.";
        return true;
    }

    public bool Repair()
    {
        if (Base == null || Settled)
        {
            return false;
        }

        bool repaired = Base.Repair(out string reason);
        Status = reason;
        RefitChanged?.Invoke();
        return repaired;
    }

    private void Update()
    {
        if (!entered || Settled || Base == null)
        {
            return;
        }

        if (!Base.IsAlive)
        {
            LoseBase();
            return;
        }

        Fleet?.RefreshBindings();
        if (Match.Phase == SiegePhase.Returning && !returning)
        {
            returning = true;
            flight.BeginReturn();
            Status = "Ships returning to assigned hangars. Wave income remains pending.";
            RefitChanged?.Invoke();
        }

        if (Match.Phase == SiegePhase.Battle)
        {
            SiegeSettings combat = settings.Combat;
            wrecks.Tick(
                Time.time,
                combat.MaximumEnemyWreckGroups,
                combat.MaximumEnemyWreckBlocks,
                combat.EnemyWreckLifetime,
                combat.EnemyWreckRetirePerScan);
            waves.Tick(Time.time);
            if (Match.Phase == SiegePhase.Returning)
            {
                returning = true;
                flight.BeginReturn();
                Status = "Wave cleared. Ships returning to assigned hangars; income settles after recovery.";
                RefitChanged?.Invoke();
            }
        }
    }

    public void SelectDefender(int index)
    {
        if (Match?.CanRefit == true && index >= 0 && index < defenders.Length)
        {
            SelectedDefender = index;
        }
    }

    public void SelectAttacker(int index)
    {
        if (Match?.CanRefit == true && index >= -1 && index < attackers.Length)
        {
            SelectedAttacker = index;
        }
    }

    public bool RefreshBlueprints()
    {
        if (Match?.CanRefit != true)
        {
            return false;
        }

        RefreshDefenders();
        return true;
    }

    private void RefreshDefenders()
    {
        defenders = SiegeBlueprintCatalog.Load(Path.Combine(Application.persistentDataPath, "Saves"), Settings.DeploymentCoinsPerBlock, out _);
        if (defenders.Length == 0)
        {
            defenders = Settings.Ships;
        }

        SelectedDefender = defenders.Length == 0 ? -1 : Mathf.Clamp(SelectedDefender, 0, defenders.Length - 1);
    }

    public bool TryDeployAt(int optionIndex, int dock, out string reason)
    {
        reason = "Deployment requires a surviving base and an intermission.";
        bool deployed = IsRunning && Fleet != null && Fleet.DeployAt(optionIndex, dock, out reason);
        Status = reason;
        RefitChanged?.Invoke();
        return deployed;
    }

    public bool TryReplaceAt(int optionIndex, int dock, out string reason)
    {
        reason = "Replacement is only available between waves.";
        bool replaced = IsRunning && Fleet != null && Fleet.ReplaceAt(optionIndex, dock, out reason);
        Status = reason;
        RefitChanged?.Invoke();
        return replaced;
    }

    public bool TryCancelDeployment(int dock, out string reason)
    {
        reason = "Undeploy is only available between waves.";
        bool removed = IsRunning && Fleet != null && Fleet.CancelDeployment(dock, out reason);
        Status = reason;
        RefitChanged?.Invoke();
        return removed;
    }

    public bool TryTakeControl(int slot)
    {
        // The new base scene retains its overview camera; deployed defenders are autonomous.
        return false;
    }

    public bool TryTakeControl(string id)
    {
        return false;
    }

    public bool TryRepairShip(string id, bool repairBase = false)
    {
        if (repairBase)
        {
            return Repair();
        }

        string reason = "Repairs require an intermission.";
        bool repaired = IsRunning && Fleet != null && SiegeRefit.Repair(Fleet.Find(id), this, out reason);
        Status = reason;
        RefitChanged?.Invoke();
        return repaired;
    }

    private void FixedUpdate()
    {
        if (!IsRunning || flight == null || (Match.Phase != SiegePhase.Battle && Match.Phase != SiegePhase.Returning))
        {
            return;
        }

        flight.Tick(Time.fixedDeltaTime);
        if (Match.Phase != SiegePhase.Returning || !returning || !flight.IsComplete)
        {
            return;
        }

        Match.CompleteReturn();
        returning = false;
        SetSimulation(false);
        Fleet.SetCombatActive(false);
        string repairStatus = string.Empty;
        if (Settings.AutoRepairBase && Base.Quote() > 0)
        {
            Base.Repair(out repairStatus);
        }

        Fleet.RepairBetweenWaves(out int repaired, out int attempted, out long spent, out string failure);
        Status = $"Recovered {flight.CompletedCount}; scrapped {flight.FailedCount}. Income settled. " + repairStatus;
        if (attempted > 0)
        {
            Status += $" Fleet repaired {repaired}/{attempted} for {spent}. " + failure;
        }

        RefitChanged?.Invoke();
    }

    private void SetSimulation(bool active)
    {
        if (PlayManager.instance != null)
        {
            PlayManager.instance.playMode = active;
        }

        buildings.SimulationEnabled = active;
        if (active)
        {
            foreach (KeyValuePair<Rigidbody, bool> entry in parkedBodies)
            {
                if (entry.Key != null)
                {
                    entry.Key.isKinematic = entry.Value;
                }
            }

            parkedBodies.Clear();
            return;
        }

        foreach (ControlUnit unit in ControlUnit.RuntimeUnits)
        {
            if (unit == null || unit.gameObject.scene != gameObject.scene)
            {
                continue;
            }

            Rigidbody body = unit.GetComponent<Rigidbody>();
            if (body != null && !parkedBodies.ContainsKey(body))
            {
                parkedBodies.Add(body, body.isKinematic);
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                    body.isKinematic = true;
                }
            }
        }
    }

    private void LoseBase()
    {
        Settle("Core destroyed");
    }

    public void Retreat()
    {
        Settle("Retreated");
    }

    private void Settle(string reason)
    {
        if (!entered || Settled)
        {
            return;
        }

        Settled = true;
        waves?.Stop();
        flight?.Stop();
        Fleet?.SetCombatActive(false);
        SetSimulation(false);
        Status = $"{reason}. Wave {Match.Wave} | destroyed {Match.DefeatedAttackers} | coins {Match.Coins}.\nYour original design is preserved. Return to build to improve it.";
        Match.LoseBase();
    }

    private void ReturnOrRetreat()
    {
        if (Base != null && !Settled)
        {
            Retreat();
        }
        else
        {
            owner?.ReturnToBuild();
        }
    }

    private void ShowCoreMarker(Transform parent, Vector3 localPosition)
    {
        coreMarker = Instantiate(RuntimeObjectTemplates.Instance.WorldLabel, parent);
        coreMarker.transform.localPosition = localPosition;
        coreMarker.text = "CORE";
        coreMarker.color = Color.cyan;
        coreMarker.characterSize = .3f;
    }

    private void LateUpdate()
    {
        if (coreMarker != null && CameraController.instance != null)
        {
            coreMarker.transform.rotation = CameraController.instance.transform.rotation;
        }
    }

    public void Exit()
    {
        if (!entered)
        {
            return;
        }

        entered = false;
        waves?.Stop();
        flight?.Stop();
        if (Fleet != null)
        {
            for (int i = 0; i < Fleet.DeploymentCount; i++)
            {
                Fleet.Scrap(i);
            }
        }

        Base?.Dispose();
        buildings?.PrepareForUnload();
        if (buildings != null)
        {
            SetSimulation(false);
        }

        foreach (KeyValuePair<GameObject, bool> item in environment)
        {
            if (item.Key != null)
            {
                item.Key.SetActive(item.Value);
            }
        }

        environment.Clear();
        parkedBodies.Clear();
        if (returnButton != null)
        {
            returnButton.onClick.RemoveListener(ReturnOrRetreat);
        }

        owner = null;
    }

    private void OnDestroy()
    {
        Exit();
    }
}

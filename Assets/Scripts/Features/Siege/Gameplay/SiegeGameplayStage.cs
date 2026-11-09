// 职责：新版基地战的自动部署、真实守波和整备；共享波次/修缮机制，不依赖旧Session/HUD。
// 入口：Enter() / BeginWave() / Repair() / Retreat() / Exit()。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class SiegeGameplayStage : MonoBehaviour
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
        position = baseSpawn.position - rotation * new Vector3(design.foundation.width * .5f - 2f, 0f, design.foundation.length * .5f - 2f);
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
                (i & 1) == 0 ? -2f : design.foundation.width - 2f,
                0f,
                (i & 2) == 0 ? -2f : design.foundation.length - 2f);
            if (corner.x < bounds.min.x + .1f || corner.x > bounds.max.x - .1f || corner.z < bounds.min.z + .1f || corner.z > bounds.max.z - .1f)
            {
                DesignStatus = "The entire foundation must remain inside the arena.";
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

            ShowCoreMarker(Base.Core.transform, Vector3.up * (Base.Core.y * .5f + 1f));
            canDeploy = false;
            waves = new SiegeWaveDirector(
                Match,
                settings.Combat,
                () => Base,
                () => attackers,
                () => -1,
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
            if (Match.Phase == SiegePhase.Intermission)
            {
                SetSimulation(false);
                Status = "Wave cleared. Repair durability and replace missing modules from the original design, then continue.";
            }
        }
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

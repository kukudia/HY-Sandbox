// 职责：在Main真实Play中验证机库容量、独占绑定、净空、毁坏重建和基地战共享舰队。
// 入口：Run()；夹具远离地图，不写玩家存档，报告写Temp/Hangars/play-checks.json。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SiegeHangarPlayChecks
{
    private const string Pending = "HY.HangarPlayChecks";
    private static IEnumerator scenario;
    private static Report report;
    private static double deadline;
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
        public List<FlightSample> flightSamples = new List<FlightSample>();
    }

    [Serializable]
    private sealed class FlightSample
    {
        public string phase;
        public float time;
        public bool alive;
        public Vector3 position;
        public Vector3 velocity;
        public float mass;
        public bool useGravity;
        public bool kinematic;
        public float doorFraction;
        public float elapsed;
        public float disabledFor;
        public int waypoint;
        public Vector3 target;
        public float totalDriveForce;
        public float totalLiftForce;
        public List<EngineSample> engines = new List<EngineSample>();
        public List<string> generators = new List<string>();
        public List<string> relays = new List<string>();
    }

    [Serializable]
    private sealed class EngineSample
    {
        public string name;
        public Vector3 position;
        public bool enabled;
        public bool ownerMatches;
        public bool hover;
        public float maxThrust;
        public float powerEfficiency;
        public float currentPower;
        public float requestedPower;
        public bool workRequested;
        public bool working;
    }

    private sealed class Host : ISiegeDeploymentHost
    {
        public SiegeMatch Match
        {
            get; set;
        }
        public SiegeSettings Settings
        {
            get; set;
        }
        public SiegeFleet Fleet
        {
            get; set;
        }
        public SiegeReturnFlight FlightGuidance
        {
            get; set;
        }
        public SiegeShipOption[] DefenderOptions
        {
            get; set;
        }
        public SiegeShipOption[] AttackerOptions => Array.Empty<SiegeShipOption>();
        public bool IsEditingBlueprint => false;
        public bool IsRunning => true;
        public int SelectedDefender => 0;
        public int SelectedAttacker => -1;

        public event Action RefitChanged
        {
            add
            {
            }

            remove
            {
            }
        }

        public void SelectDefender(int index)
        {
        }

        public void SelectAttacker(int index)
        {
        }

        public bool RefreshBlueprints() => false;
        public bool TryDeployAt(int optionIndex, int dock, out string reason) => Fleet.DeployAt(optionIndex, dock, out reason);
        public bool TryReplaceAt(int optionIndex, int dock, out string reason) => Fleet.ReplaceAt(optionIndex, dock, out reason);
        public bool TryCancelDeployment(int dock, out string reason) => Fleet.CancelDeployment(dock, out reason);
        public bool TryTakeControl(int slot) => false;
        public bool TryTakeControl(string id) => false;
        public bool TryRepairShip(string id, bool repairBase = false) => false;
    }

    static SiegeHangarPlayChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
    }

    [MenuItem("Tools/HY-Sandbox/Siege/Validate hangar fleet in Play")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity" || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Open only saved Main in Edit Mode first.");
        }

        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
        {
            report = new Report
            {
                status = "running"
            };
            scenario = Scenario();
            deadline = EditorApplication.timeSinceStartup + 240;
            Application.logMessageReceived += LogError;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && scenario != null)
        {
            report.errors.Add("Play exited before completion.");
            Finish();
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Hangar validation exceeded 240 seconds.");
            }

            if (!scenario.MoveNext())
            {
                Finish();
            }
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
            Finish();
        }
    }

    private static IEnumerator Scenario()
    {
        yield return null;
        while (BuildManager.instance == null || BuildManager.instance.IsLoadingBlocks)
        {
            yield return null;
        }

        GameObject managerRoot = new GameObject("Hangar validation owner");
        managerRoot.SetActive(false);
        WorldBuildingManager manager = managerRoot.AddComponent<WorldBuildingManager>();
        SerializedObject config = new SerializedObject(manager);
        config.FindProperty("initializeOnStart").boolValue = false;
        config.ApplyModifiedPropertiesWithoutUndo();
        managerRoot.SetActive(true);
        SiegeBase fortress = null;
        Host host = null;
        bool previousPlayMode = PlayManager.instance.playMode;
        float previousCleanupDistance = PlayManager.instance.groupCleanupDistance;
        // Keep this deliberately remote physics fixture outside ordinary world retirement policy.
        PlayManager.instance.groupCleanupDistance = 100000f;
        bool previousSimulation = manager.SimulationEnabled;
        try
        {
            SiegeSettings settings = AssetDatabase.LoadAssetAtPath<SiegeSettings>("Assets/Siege/DefaultSiege.asset");
            TextAsset shipAsset = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
            BlockDataList ship = JsonUtility.FromJson<BlockDataList>(shipAsset.text);
            Bounds bounds = BlockBounds.FromBlueprint(ship.blocks, Matrix4x4.identity);
            GameObject hangarPrefab = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources" }).Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<GameObject>).First(asset => asset != null && asset.GetComponent<HangarModule>() != null);
            BlockData hangarData = new BlockData(hangarPrefab.GetComponent<Block>());
            hangarData.id = "probe-hangar";
            hangarData.x = Mathf.CeilToInt(bounds.size.x) + 2;
            hangarData.y = Mathf.CeilToInt(bounds.size.y) + 1;
            hangarData.z = Mathf.CeilToInt(bounds.size.z) + 2;
            hangarData.posX = 24;
            hangarData.posZ = 24;
            hangarData.posY = hangarData.y * .5f;
            hangarData.rotX = hangarData.rotY = hangarData.rotZ = 0;
            hangarData.rotW = 1;
            BlockData core = JsonUtility.FromJson<BlockDataList>(settings.BaseBlueprint.text).blocks.First(block => block.id == settings.BaseCoreBlockId);
            core = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(core));
            core.id = "probe-core";
            core.posX = core.posZ = 4;
            core.posY = core.y * .5f;
            core.rotX = core.rotY = core.rotZ = 0;
            core.rotW = 1;
            BlockDataList design = new BlockDataList
            {
                foundation = new BuildingFoundationData
                {
                    width = 64,
                    length = 64
                },
                siegeCoreId = core.id,
                blocks = new List<BlockData>
                {
                    core,
                    hangarData
                }
            };
            host = new Host
            {
                Match = new SiegeMatch(
                    100000,
                    1,
                    0,
                    1,
                    5,
                    10),
                Settings = settings
            };
            fortress = SiegeBase.Create(
                host.Match,
                settings,
                manager,
                design,
                core.id,
                "Hangar validation base",
                new Vector3(3000, 500, 3000),
                Quaternion.identity,
                1000,
                null);
            HangarModule hangar = fortress.Building.GetComponentsInChildren<HangarModule>().Single();
            host.Fleet = new SiegeFleet(host, new[] { hangar.transform }, fortress, false);
            host.Fleet.RefreshHangars(new[] { hangar });
            string smallJson = JsonUtility.ToJson(ship);
            BlockDataList oversized = JsonUtility.FromJson<BlockDataList>(smallJson);
            oversized.blocks[0].x = hangarData.x + 10;
            host.DefenderOptions = new[]
            {
                new SiegeShipOption("Probe ship", smallJson, 1, ship.blocks.Count),
                new SiegeShipOption("Oversized ship", JsonUtility.ToJson(oversized), 1, oversized.blocks.Count)
            };
            Check(host.Fleet.UsesHangars && host.Fleet.DeploymentCount == 1, "Shared fleet binds a real base hangar");
            Check(host.Fleet.FitsHangar(0, ship), "Fitting ship accepted by internal clear-space bounds");
            Check(!host.Fleet.FitsHangar(0, oversized), "Oversized hull rejected before purchase");
            long initialCoins = host.Match.Coins;
            Check(!host.TryDeployAt(1, 0, out _), "Oversized blueprint cannot deploy into an empty hangar");
            Check(host.Match.Coins == initialCoins, "Rejected oversized purchase spends no coins");
            Check(
                host.TryDeployAt(0, 0, out string deployed),
                "Small blueprint deploys inside actual walls: " + deployed);
            SiegeFleetEntry entry = host.Fleet.Ships[0];
            Check(entry.IsAlive && !entry.Unit.AcceptsPlayerInput, "Overview fleet spawns autonomous friendly ship");
            Check(host.Match.Coins == initialCoins - 1, "Successful purchase spends exactly the quoted price");
            Check(
                entry.Unit.GetComponent<Rigidbody>().useGravity,
                "Flight fixture uses the real ship gravity setting");
            Check(
                entry.Unit.GetComponentsInChildren<PowerGeneratingUnit>().Length > 0,
                "Flight fixture carries real on-board generators");
            host.FlightGuidance = new SiegeReturnFlight(host.Fleet, new SiegeFlightSettings { timeout = 80f, disabledGrace = 5f }, manager);
            PlayManager.instance.playMode = true;
            SetSimulation(manager, true);
            host.Fleet.SetCombatActive(true);
            Vector3 startingPosition = entry.Cockpit.transform.position;
            host.FlightGuidance.BeginLaunch();
            bool navigationComplete = false;
            manager.StartCoroutine(GuideFlight(host.FlightGuidance, entry, "launch", () => navigationComplete = true));
            while (!navigationComplete)
            {
                yield return null;
            }

            Check(
                host.FlightGuidance.CompletedCount == 1 && host.FlightGuidance.FailedCount == 0 && entry.IsAlive,
                "Powered ship physically exits the animated hangar through its door: " + host.FlightGuidance.LastFailureReason);
            Check(
                Vector3.Distance(entry.Cockpit.transform.position, startingPosition) > hangarData.z * .5f,
                "Launch travels beyond the external hangar wall under real physics");
            EnemyController ai = entry.Unit.GetComponent<EnemyController>();
            if (ai != null)
            {
                ai.enabled = false;
            }

            Check(!host.TryDeployAt(0, 0, out _), "Assigned hangar remains occupied after its ship leaves the floor");
            Check(
                host.Fleet.Ships[0] == entry && host.Match.Coins == initialCoins - 1,
                "Occupied purchase preserves original binding and balance");
            host.FlightGuidance.BeginReturn();
            navigationComplete = false;
            manager.StartCoroutine(GuideFlight(host.FlightGuidance, entry, "return", () => navigationComplete = true));
            while (!navigationComplete)
            {
                yield return null;
            }

            Check(
                host.FlightGuidance.CompletedCount == 1 && host.FlightGuidance.FailedCount == 0 && entry.IsAlive,
                "Powered ship returns through the same door and docks without teleportation: " + host.FlightGuidance.LastFailureReason);
            Check(
                entry.Unit.GetComponent<Rigidbody>().isKinematic,
                "Returned ship is parked only after navigation completes");
            Check(
                Vector3.Distance(entry.Cockpit.transform.position, startingPosition) < .35f,
                "Returning ship restores its authored cockpit dock pose within tolerance");
            host.FlightGuidance.Stop();
            host.Fleet.SetCombatActive(false);
            SetSimulation(manager, previousSimulation);
            PlayManager.instance.playMode = previousPlayMode;
            Durability health = hangar.GetComponent<Durability>();
            health.currentDurability = health.maxDurability * .5f;
            long quote = fortress.Quote();
            long beforeRepair = host.Match.Coins;
            bool repairSucceeded = fortress.Repair(out string repaired);
            Check(
                quote > 0 && repairSucceeded,
                "Damaged hangar refits while its assigned ship remains docked: " + repaired);
            Check(host.Match.Coins == beforeRepair - quote, "Hangar repair deducts the actual base quote once");
            Check(
                Mathf.Approximately(health.currentDurability, health.maxDurability),
                "Paid refit restores hangar durability");
            Check(entry.IsAlive, "Base repair preserves its docked ship");
            host.Fleet.Scrap(0);
            Check(!entry.IsAlive, "Scrapping releases the assigned ship");
            fortress.Building.DestroyBlock(hangar.GetComponent<Block>());
            Check(host.Fleet.GetDeploymentPoint(0) == null, "Destroyed hangar cannot expose a deployment marker");
            Check(!host.TryDeployAt(0, 0, out _), "Destroyed hangar rejects deployment");
            yield return null;
            beforeRepair = host.Match.Coins;
            quote = fortress.Quote();
            repairSucceeded = fortress.Repair(out repaired);
            Check(quote > 0 && repairSucceeded, "Missing hangar is rebuilt from the original design: " + repaired);
            HangarModule restored = host.Fleet.GetHangar(0);
            Check(
                restored != null && restored.IsOperational && restored.IsCombat,
                "Fleet slot reconnects restored hangar by stable block identity");
            Check(host.Match.Coins == beforeRepair - quote, "Destroyed hangar reconstruction is paid once");
            Check(host.TryDeployAt(0, 0, out deployed), "Restored hangar accepts a new assigned ship: " + deployed);
            Check(host.Fleet.Ships[0] != entry, "Restored hangar creates a new ship identity");
        }
        finally
        {
            host?.FlightGuidance?.Stop();
            manager.StopAllCoroutines();
            SetSimulation(manager, previousSimulation);
            if (PlayManager.instance != null)
            {
                PlayManager.instance.playMode = previousPlayMode;
                PlayManager.instance.groupCleanupDistance = previousCleanupDistance;
            }

            host?.Fleet?.Scrap(0);
            fortress?.Dispose();
            managerRoot.SetActive(false);
            Object.Destroy(managerRoot);
        }
    }

    private static void SetSimulation(WorldBuildingManager manager, bool active)
    {
        // This runtime-owned setter is intentionally internal; the isolated Editor fixture opts in explicitly.
        typeof(WorldBuildingManager).GetProperty(nameof(WorldBuildingManager.SimulationEnabled)).GetSetMethod(true).Invoke(manager, new object[] { active });
    }

    private static IEnumerator GuideFlight(SiegeReturnFlight flight, SiegeFleetEntry entry, string phase, Action completed)
    {
        // The real engine advances gravity, collisions, doors and power between these fixed steps.
        // A coroutine on the existing fixture manager avoids treating Editor repaint frequency as physics time.
        float nextSample = Time.time;
        SampleFlight(flight, entry, phase);
        while (!flight.IsComplete)
        {
            yield return new WaitForFixedUpdate();
            if (Time.time >= nextSample)
            {
                SampleFlight(flight, entry, phase);
                nextSample = Time.time + 1f;
            }

            flight.Tick(Time.fixedDeltaTime);
        }

        SampleFlight(flight, entry, phase + " complete");
        completed();
    }

    private static void SampleFlight(SiegeReturnFlight flight, SiegeFleetEntry entry, string phase)
    {
        var sample = new FlightSample
        {
            phase = phase,
            time = Time.time,
            alive = entry.IsAlive
        };
        var flights = (IList)typeof(SiegeReturnFlight).GetField("flights", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(flight);
        if (flights.Count > 0)
        {
            object navigation = flights[0];
            Type type = navigation.GetType();
            sample.elapsed = (float)type.GetField("elapsed").GetValue(navigation);
            sample.disabledFor = (float)type.GetField("disabledFor").GetValue(navigation);
            sample.waypoint = (int)type.GetField("point").GetValue(navigation);
            var points = (Vector3[])type.GetField("points").GetValue(navigation);
            if (points != null && sample.waypoint < points.Length)
            {
                sample.target = points[sample.waypoint];
            }

            HangarModule hangar = (HangarModule)type.GetField("hangar").GetValue(navigation);
            sample.doorFraction = hangar != null ? hangar.DoorFraction : -1f;
        }

        ControlUnit unit = entry.Unit;
        if (unit != null)
        {
            Rigidbody body = unit.GetComponent<Rigidbody>();
            sample.position = body.position;
            sample.velocity = body.linearVelocity;
            sample.mass = body.mass;
            sample.useGravity = body.useGravity;
            sample.kinematic = body.isKinematic;
            foreach (Thruster engine in unit.GetComponentsInChildren<Thruster>())
            {
                Power power = engine.GetComponent<Power>();
                var engineSample = new EngineSample
                {
                    name = engine.name,
                    position = engine.transform.position,
                    enabled = engine.isActiveAndEnabled,
                    ownerMatches = engine.GetComponentInParent<ControlUnit>() == unit,
                    hover = engine is HoverThruster,
                    maxThrust = engine.maxThrust,
                    powerEfficiency = engine.PowerEfficiency,
                    currentPower = power != null ? power.currentPower : 0f,
                    requestedPower = power != null ? power.RequestedPower : 0f,
                    workRequested = power != null && power.IsWorkRequested,
                    working = power != null && power.isWorking
                };
                sample.engines.Add(engineSample);
                if (engineSample.enabled && engineSample.ownerMatches)
                {
                    if (engineSample.hover)
                    {
                        sample.totalLiftForce += engineSample.maxThrust * engineSample.powerEfficiency;
                    }
                    else
                    {
                        sample.totalDriveForce += engineSample.maxThrust * engineSample.powerEfficiency;
                    }
                }
            }

            foreach (PowerGeneratingUnit generator in unit.GetComponentsInChildren<PowerGeneratingUnit>())
            {
                sample.generators.Add($"{generator.name}: enabled={generator.isActiveAndEnabled}, output={generator.outputPower}, devices={generator.connectedDevices.Count}");
            }

            foreach (PowerTransmissionDevice relay in unit.GetComponentsInChildren<PowerTransmissionDevice>())
            {
                sample.relays.Add($"{relay.name}: enabled={relay.isActiveAndEnabled}, available={relay.availableNetworkPower}, generators={relay.connectedGenerators.Count}, relays={relay.connectedDevices.Count}, range={relay.powerRange}, linkRange={relay.maxConnectionDistance}");
            }
        }

        report.flightSamples.Add(sample);
        Directory.CreateDirectory("Temp/Hangars");
        File.WriteAllText("Temp/Hangars/flight-telemetry.json", JsonUtility.ToJson(report, true));
    }

    private static void Check(bool success, string description)
    {
        if (!success)
        {
            throw new InvalidOperationException(description);
        }

        report.checks.Add(description);
    }

    private static void LogError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            report.errors.Add(message + "\n" + stack);
        }
    }

    private static void Finish()
    {
        Application.logMessageReceived -= LogError;
        EditorApplication.update -= Tick;
        (scenario as IDisposable)?.Dispose();
        scenario = null;
        SessionState.SetBool(Pending, false);
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/Hangars");
        File.WriteAllText("Temp/Hangars/play-checks.json", JsonUtility.ToJson(report, true));
        Debug.Log("Hangar checks: " + report.status + " / " + report.checks.Count);
        EditorApplication.isPlaying = false;
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Bounded Play Mode probe in an empty scene, restoring the previously open scene afterwards.</summary>
[InitializeOnLoad]
public static class BlockArtPlayProbe
{
    private const string Pending = "HY.BlockArtProbe.Pending";
    private const string Previous = "HY.BlockArtProbe.Scene";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static double _started;
    private static float _gameStarted;
    private static int _phase;
    private static TurretWeapon _turret;
    private static Durability _target;
    private static RepairBot _bot;
    private static Durability _repairTarget;
    private static MainThruster _main;
    private static UniversalThruster _universal;
    private static HoverThruster _hover;
    private static Rigidbody _body;
    private static Transform _base;
    private static Quaternion _baseRotation;
    private static bool _sawRepair;
    private static bool _sawFlight;
    private static bool _sawMuzzleFlash;
    private static bool _sawRepairSocket;
    private static AssetParticleEffect[] _bursts;
    private static AssetParticleEffect[] _continuous;
    private static int _continuitySamples;
    private static int _continuityGaps;

    static BlockArtPlayProbe() { EditorApplication.playModeStateChanged += StateChanged; }

    [MenuItem("Tools/HY Sandbox/Block Art/Run Play Mode Probe")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the open scene and exit Play Mode first.");
        SessionState.SetString(Previous, SceneManager.GetActiveScene().path);
        SessionState.SetBool(Pending, true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear(); Errors.Clear(); _phase = 0; _sawRepair = _sawFlight = _sawMuzzleFlash = _sawRepairSocket = false;
            Application.logMessageReceived += Capture;
            try { Setup(); _started = EditorApplication.timeSinceStartup; _gameStarted = Time.time; EditorApplication.update += Tick; }
            catch (Exception exception) { Errors.Add(exception.ToString()); Finish(); }
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
            string path = SessionState.GetString(Previous, "");
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }

    private static void Setup()
    {
        SessionState.SetBool("HY.BlockArtProbe.Background", Application.runInBackground);
        Application.runInBackground = true;
        EditorApplication.isPaused = false;
        Time.timeScale = 1f;
        var pm = new GameObject("Probe PlayManager").AddComponent<PlayManager>();
        pm.enabled = false; pm.playMode = true; pm.showUI = false;
        new GameObject("Probe UI").AddComponent<MainUIPanels>().enabled = false;
        var camera = new GameObject("Probe Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; camera.transform.position = new Vector3(12, 8, -12); camera.transform.LookAt(Vector3.zero);
        pm.mainCamera = camera;
        var owner = Unit("Turret owner", Vector3.zero, UnitFaction.Player, true);
        _turret = Spawn("Turret", owner.transform, Vector3.zero).GetComponent<TurretWeapon>();
        Supply(owner.transform);
        var enemy = Unit("Enemy", new Vector3(0, 0, 10), UnitFaction.Enemy, true);
        _target = Spawn("1x1x1", enemy.transform, Vector3.zero).GetComponent<Durability>();
        _target.maxDurability = _target.currentDurability = 10000f;
        pm.RegisterControlUnit(owner); pm.RegisterControlUnit(enemy);

        var repairOwner = Unit("Repair owner", new Vector3(20, 0, 0), UnitFaction.Player, true);
        var bay = Spawn("RepairBotContianer", repairOwner.transform, Vector3.zero);
        _bot = bay.GetComponentInChildren<RepairBot>();
        _bot.findTargetInterval = 0.25f; _bot.showAvoidanceRays = _bot.showAvoidanceZones = _bot.showDirectionVectors = false;
        _repairTarget = Spawn("1x1x1", repairOwner.transform, new Vector3(2, 0, 0)).GetComponent<Durability>();
        _repairTarget.currentDurability = 70f;

        var propulsion = Unit("Propulsion", new Vector3(-20, 0, 0), UnitFaction.Player, false);
        propulsion.SetMovementInput(Vector3.right);
        _body = propulsion.GetComponent<Rigidbody>(); _body.mass = 100f;
        _main = Spawn("MainThrusterBig", propulsion.transform, Vector3.zero).GetComponent<MainThruster>();
        _main.transform.rotation = Quaternion.Euler(0, 90, 0); _main.SetRuntimeReferences(propulsion, _body);
        _universal = Spawn("UniversalThruster", propulsion.transform, new Vector3(0, 2, 0)).GetComponent<UniversalThruster>();
        _universal.SetRuntimeReferences(propulsion, _body);
        _base = _universal.model.parent; _baseRotation = _base.rotation;
        _hover = Spawn("HoverThruster", propulsion.transform, new Vector3(0, -2, 0)).GetComponent<HoverThruster>();
        _hover.isHovered = true; _hover.SetRuntimeReferences(propulsion, _body);
        Supply(propulsion.transform);
        _continuitySamples = _continuityGaps = 0;
        var continuousRoot = new GameObject("Continuity samples");
        _continuous = new[] { "ThrusterJet", "HoverJet", "BotFlight", "RepairContact" }
            .SelectMany(name => new[] { 0.05f, 0.1f, 1f }.Select(intensity =>
            {
                var effect = Object.Instantiate(BlockVfxBaker.Load(name)).GetComponent<AssetParticleEffect>();
                effect.transform.SetParent(continuousRoot.transform, false);
                effect.transform.position = new Vector3(100f, 0f, 0f);
                effect.SetIntensity(intensity);
                return effect;
            })).ToArray();
        Physics.SyncTransforms();
    }

    private static ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool kinematic)
    {
        var root = new GameObject(name); root.transform.position = position;
        var body = root.AddComponent<Rigidbody>(); body.useGravity = false; body.isKinematic = kinematic; body.constraints = RigidbodyConstraints.FreezeRotation;
        var unit = root.AddComponent<ControlUnit>(); unit.enabled = false; unit.faction = faction;
        var cockpit = new GameObject("Probe Cockpit"); cockpit.transform.SetParent(root.transform, false);
        unit.cockpit = cockpit.AddComponent<Cockpit>(); unit.cockpit.faction = UnitFaction.Enemy;
        unit.hasValidCockpit = true;
        return unit;
    }

    private static GameObject Spawn(string name, Transform parent, Vector3 local)
    {
        var root = Object.Instantiate(Resources.Load<GameObject>("Blocks/" + name), parent);
        root.transform.localPosition = local;
        return root;
    }

    private static void Supply(Transform parent)
    {
        Spawn("PowerGeneratingUnit", parent, new Vector3(0, -4, 0));
        var relay = Spawn("PowerTransmissionDevice", parent, new Vector3(0, -3, 0)).GetComponent<PowerTransmissionDevice>();
        relay.powerRange = 8;
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            double elapsed = Time.time - _gameStarted;
            if (EditorApplication.timeSinceStartup - _started > 60) throw new TimeoutException("Probe did not receive 24 seconds of game updates.");
            if (_bot == null) throw new InvalidOperationException("Bot was destroyed.");
            if (elapsed > 0.6 && elapsed < 2)
            {
                _continuitySamples++;
                foreach (var effect in _continuous)
                    if (effect.GetComponentsInChildren<ParticleSystem>().Any(p => p.particleCount == 0)) _continuityGaps++;
            }
            _sawFlight |= _bot.transform.parent == _bot.outside && !_bot.GetComponent<Rigidbody>().isKinematic;
            _sawRepair |= _repairTarget.currentDurability > 70f;
            _sawMuzzleFlash |= _turret.muzzle.GetComponentsInChildren<ParticleSystem>().Any(p => p.particleCount > 0);
            if (_bot.isRepairing)
            {
                var socket = new SerializedObject(_bot).FindProperty("_repairOrigin").objectReferenceValue as Transform;
                var beam = _bot.GetComponent<StylizedBeamEffect>();
                if (beam != null && socket != null)
                {
                    Vector3 start = (Vector3)typeof(StylizedBeamEffect).GetField("startPoint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(beam);
                    _sawRepairSocket |= Vector3.Distance(start, socket.position) < 0.03f;
                }
            }
            if (_phase == 0 && elapsed > 0.4)
            {
                Check("Bot starts docked, kinematic, collisions off", _bot.currentState == RepairBot.NavigationState.Idle && _bot.GetComponent<Rigidbody>().isKinematic && !_bot.GetComponent<Rigidbody>().detectCollisions);
                _phase++;
            }
            if (_phase == 1 && elapsed > 2)
            {
                Check("Continuous jets and repair contact have no gaps at 5, 10 and 100 percent", _continuitySamples > 10 && _continuityGaps == 0);
                foreach (var effect in _continuous) effect.SetIntensity(0f);
                Check("Turret raycast applies damage", _target.currentDurability < 10000f);
                Check("Turret muzzle follows active barrel", _turret.muzzle.IsChildOf(_turret.verticalAxis) && _turret.muzzle.gameObject.activeInHierarchy);
                Check("Authored muzzle flash plays on firing", _sawMuzzleFlash);
                Check("Generator and repair bay lights illuminate", Object.FindObjectsByType<BlockStatusLight>(FindObjectsSortMode.None).All(s => s.GetComponentsInChildren<Light>().Count(l => l.intensity > 0f) == 2));
                Check("Main thrust moves rotated unit along world X", _body.linearVelocity.x > 0.2f);
                Check("Universal head rotates while base stays fixed", Vector3.Dot(_universal.model.forward, Vector3.right) > 0.98f && Quaternion.Angle(_base.rotation, _baseRotation) < 0.1f);
                Check("All propulsion outlets emit", new Thruster[] { _main, _universal, _hover }.All(t => t.GetComponentsInChildren<ParticleSystem>().Any(p => p.particleCount > 0)));
                foreach (var t in new Thruster[] { _main, _universal, _hover })
                    Check(t.name + " nozzle axes oppose force", t.GetComponentsInChildren<AssetParticleEffect>().All(e => Vector3.Dot(e.transform.forward, -(t is HoverThruster ? t.transform.up : t.model.forward)) > 0.999f));
                _turret.enabled = false;
                _main.GetComponent<Power>().currentPower = 0f;
                _main.enabled = false;
                _phase++;
            }
            if (_phase == 2 && elapsed > 3)
            {
                Check("Continuous effects stop emission and expire", _continuous.All(e => e.GetComponentsInChildren<ParticleSystem>().All(p => !p.isEmitting && p.particleCount == 0)));
                foreach (var e in _continuous)
                    foreach (var p in e.GetComponentsInChildren<ParticleSystem>())
                        if (p.isEmitting || p.particleCount > 0) Errors.Add($"Continuous residual {e.name}/{p.name}: count={p.particleCount}, emitting={p.isEmitting}, culling={p.main.cullingMode}");
                foreach (var effect in _continuous) Object.Destroy(effect.gameObject);
                Check("Disabled thruster stops and clears plume", _main.GetComponentsInChildren<ParticleSystem>().All(p => !p.isEmitting && p.particleCount == 0));
                foreach (BlockVfxLibrary.Effect kind in Enum.GetValues(typeof(BlockVfxLibrary.Effect)))
                    BlockVfxLibrary.Play(kind, new Vector3(50f + (int)kind * 4f, 0f, 0f), Quaternion.identity);
                _bursts = Object.FindObjectsByType<AssetParticleEffect>(FindObjectsSortMode.None)
                    .Where(e => e.transform.parent == null && e.transform.position.x >= 50f && e.transform.position.x <= 74f).ToArray();
                Check("All seven event effects instantiate", _bursts.Length == 7);
                var debris = new GameObject("Probe debris").AddComponent<Rigidbody>();
                debris.useGravity = false; debris.position = new Vector3(70, 0, 0); debris.linearVelocity = Vector3.right * 5f;
                DetachedPartSmokeTrail.Attach(debris, debris.position, 1f);
                _phase++;
            }
            if (_phase == 3 && elapsed > 7)
            {
                Check("Event particles finish before release timeout", _bursts.All(e => e != null && e.GetComponentsInChildren<ParticleSystem>().All(p => !p.IsAlive(false))));
                foreach (var e in _bursts.Where(e => e != null))
                    foreach (var p in e.GetComponentsInChildren<ParticleSystem>())
                        if (p.IsAlive(false)) Errors.Add($"Burst residual {e.name}/{p.name}: count={p.particleCount}, emitting={p.isEmitting}, time={p.time}, culling={p.main.cullingMode}");
                _phase++;
            }
            if (elapsed > 24)
            {
                Check("Bot undocks with dynamic body", _sawFlight);
                Check("Bot repairs same-unit damaged block", _sawRepair && _repairTarget.currentDurability >= _repairTarget.maxDurability);
                Check("Repair beam starts at model tool socket", _sawRepairSocket);
                Check("Bot returns to authored home", _bot.transform.parent == _bot.home && _bot.currentState == RepairBot.NavigationState.Idle && Vector3.Distance(_bot.transform.localPosition, _bot.homeOffset) < 0.001f);
                Check("Docked bot stops effects", _bot.GetComponentsInChildren<ParticleSystem>().All(p => !p.isEmitting));
                Check("Event effects release after playback", _bursts != null && _bursts.All(e => e == null));
                Check("Detached smoke releases its controller", Object.FindObjectsByType<DetachedPartSmokeTrail>(FindObjectsSortMode.None).Length == 0);
                Finish();
            }
        }
        catch (Exception exception) { Errors.Add(exception.ToString()); Finish(); }
    }

    private static void Check(string name, bool pass) { if (pass) Checks.Add(name); else Errors.Add("FAILED: " + name); }
    private static void Capture(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Errors.Add(message + "\n" + stack);
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Capture;
        Application.runInBackground = SessionState.GetBool("HY.BlockArtProbe.Background", false);
        File.WriteAllText("Assets/Art/BlockVisuals/PlayModeValidation.json", Newtonsoft.Json.JsonConvert.SerializeObject(new { unity = Application.unityVersion, passed = Checks, errors = Errors }, Newtonsoft.Json.Formatting.Indented));
        EditorApplication.isPlaying = false;
    }
}

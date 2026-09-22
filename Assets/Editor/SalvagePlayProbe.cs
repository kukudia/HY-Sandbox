using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SalvagePlayProbe
{
    private const string Pending = "HY.SalvageProbe.Pending";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static IEnumerator _scenario;
    private static float _resume;
    private static double _deadline;
    private static bool _background;
    private static float _timeScale;
    public static string LastResult => File.Exists("Temp/SalvageWork/PlayValidation.json") ? File.ReadAllText("Temp/SalvageWork/PlayValidation.json") : "pending";

    static SalvagePlayProbe() { EditorApplication.playModeStateChanged += StateChanged; }

    [MenuItem("Tools/Salvage/Run Play Mode validation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before running the probe.");
        if (File.Exists("Temp/SalvageWork/PlayValidation.json")) File.Delete("Temp/SalvageWork/PlayValidation.json");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear(); Errors.Clear();
            _background = Application.runInBackground; _timeScale = Time.timeScale;
            Application.runInBackground = true; Time.timeScale = 1f;
            // These changes exist only in the Play Mode copy. Unsaved authoring scenes stay untouched.
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) root.SetActive(false);
            Application.logMessageReceived += Capture;
            _scenario = Scenario(); _resume = 0f; _deadline = EditorApplication.timeSinceStartup + 140;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Pending, false);
    }

    private static void Capture(string condition, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) Errors.Add(condition + "\n" + stack);
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("Salvage probe exceeded 140 seconds.");
            if (Time.time < _resume) return;
            if (!_scenario.MoveNext()) { Finish(); return; }
            _resume = Time.time + (_scenario.Current is float seconds ? seconds : 0.05f);
        }
        catch (Exception exception) { Errors.Add(exception.ToString()); Finish(); }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick; Application.logMessageReceived -= Capture;
        Application.runInBackground = _background; Time.timeScale = _timeScale;
        var report = new Report { checks = Checks.ToArray(), errors = Errors.ToArray(), passed = Errors.Count == 0, unity = Application.unityVersion };
        Directory.CreateDirectory("Temp/SalvageWork");
        File.WriteAllText("Temp/SalvageWork/PlayValidation.json", JsonUtility.ToJson(report, true));
        EditorApplication.isPlaying = false;
    }

    [Serializable] private class Report { public bool passed; public string unity; public string[] checks; public string[] errors; }
    private static void Check(string name, bool result) { if (result) Checks.Add(name); else throw new InvalidOperationException(name); }

    private static ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool cockpit = true)
    {
        var root = new GameObject(name); root.transform.position = position;
        var body = root.AddComponent<Rigidbody>(); body.useGravity = false; body.isKinematic = true;
        var unit = root.AddComponent<ControlUnit>(); unit.enabled = false; unit.faction = faction; unit.EnsureRuntimeUnitId();
        if (cockpit)
        {
            var pilot = new GameObject("Probe cockpit"); pilot.transform.SetParent(root.transform, false);
            unit.cockpit = pilot.AddComponent<Cockpit>(); unit.cockpit.faction = faction;
            unit.cockpits = new[] { unit.cockpit }; unit.hasValidCockpit = true;
        }
        return unit;
    }

    private static GameObject Spawn(string name, ControlUnit unit, Vector3 local)
    {
        var root = Object.Instantiate(Resources.Load<GameObject>("Blocks/" + name), unit.transform);
        root.transform.localPosition = local;
        Power power = root.GetComponent<Power>(); if (power != null) power.currentPower = 1000f;
        RuntimeUnitMember.Ensure(root, unit.runtimeUnitId, unit.faction);
        return root;
    }

    private static CargoItem Coins(int count) => new CargoItem { kind = CargoKind.Coins, amount = count };
    private static CargoItem Part() => new CargoItem { kind = CargoKind.SpecialPart, resourcePath = "Blocks/UniversalThruster", amount = 1 };

    private static IEnumerator Scenario()
    {
        var pm = new GameObject("Salvage test manager").AddComponent<PlayManager>(); pm.enabled = false; pm.playMode = true;
        var ui = new GameObject("Salvage test UI").AddComponent<MainUIPanels>(); ui.enabled = false;
        var camera = new GameObject("Salvage test camera").AddComponent<Camera>(); camera.tag = "MainCamera"; pm.mainCamera = camera;
        VisualEffectsManager.EnsureInstance().gameObject.SetActive(true);
        var build = new GameObject("Salvage test build").AddComponent<BuildManager>(); build.enabled = false; build.blockLayer = LayerMask.GetMask("Block");
        var owner = Unit("Salvage owner", Vector3.zero, UnitFaction.Player);
        var hold = Spawn("CargoHold", owner, new Vector3(3, 0, 0)).GetComponent<CargoHold>();
        var coins = Spawn("CoinHold", owner, new Vector3(0, 0, -3)).GetComponent<CargoHold>();
        var technology = Spawn("TechnologyHold", owner, new Vector3(-3, 0, -3)).GetComponent<CargoHold>();
        yield return 0.2f;
        Check("Authored capacities: 8 parts / 100 coins / 100 technology", hold.Capacity == 8 && coins.Capacity == 100 && technology.Capacity == 100);
        Check("Default explosion policy", hold.GetComponent<Block>().canExplode && !coins.GetComponent<Block>().canExplode && !technology.GetComponent<Block>().canExplode);
        coins.GetComponent<Power>().currentPower = 0;
        Check("Unpowered hold rejects deposit", coins.Store(Coins(10)) == 0);
        coins.GetComponent<Power>().currentPower = 1000;
        Check("Capacity clamps currency and rejects wrong kind", coins.Store(Coins(120)) == 100 && coins.Store(Coins(1)) == 0 && hold.Store(Coins(2)) == 0);
        coins.RestoreContents(new List<CargoItem> { Coins(97) });
        var coins2 = Spawn("CoinHold", owner, new Vector3(5, 0, -3)).GetComponent<CargoHold>();
        LootDrop.Spawn(Coins(10), new Vector3(0, 2, -3));
        yield return 2f;
        Check("Currency arrival fills first hold and routes overflow without loss", coins.Used == 100 && coins2.Used == 7);
        LootDrop.Spawn(new CargoItem { kind = CargoKind.Technology, amount = 25 }, new Vector3(-3, 2, -3));
        yield return 1f;
        Check("Technology routes only to technology hold", technology.Used == 25);
        Check("Resource fill uses capacity ratio", Mathf.Abs(technology.transform.Find("Model/Liquid fill").localScale.y - 0.81f * 0.25f) < 0.001f);

        var bay = Spawn("CollectionBotContainer", owner, Vector3.zero);
        var bot = bay.GetComponentInChildren<CollectionBot>();
        bot.movementSpeed = 8f;
        var drop = LootDrop.Spawn(Part(), new Vector3(0, 1, 5));
        yield return 0.4f;
        Check("Collection drone reserves a special item", drop.ClaimedBy == bot && drop.Destination == hold && hold.Used == 0);
        bool sawCargo = false;
        float deadline = Time.time + 35f;
        while (hold.Used == 0 && Time.time < deadline)
        {
            sawCargo |= bot.HasCargo;
            yield return 0.1f;
        }
        Check("Drone flies, carries and docks before crediting cargo", sawCargo && hold.Used == 1 && bot.transform.parent == bot.home);
        Check("Stored item display contains no gameplay components", hold.transform.Find("Model/Contents display").GetComponentsInChildren<Block>().Length == 0);

        // Reservation competition on the last slot and recovery when a bay loses power.
        hold.RestoreContents(Enumerable.Range(0, 7).Select(_ => Part()).ToList());
        var bay2 = Spawn("CollectionBotContainer", owner, new Vector3(-3, 0, 0));
        var bot2 = bay2.GetComponentInChildren<CollectionBot>(); bot2.obstacleMask = 0;
        var drop2 = LootDrop.Spawn(Part(), new Vector3(0, 2, 10));
        var drop3 = LootDrop.Spawn(Part(), new Vector3(0, 2, 11));
        yield return 0.8f;
        Check("Two drones cannot reserve beyond last free slot", new[] { drop2, drop3 }.Count(d => d.ClaimedBy != null) == 1);
        bay.GetComponent<Power>().currentPower = 0; bay2.GetComponent<Power>().currentPower = 0;
        yield return 0.2f;
        Check("Power loss releases claims and keeps dropped cargo", drop2.Available && drop3.Available);
        Object.Destroy(bay); Object.Destroy(bay2);
        yield return 0.2f;
        LootDrop.ClearSession();

        hold.RestoreContents(new List<CargoItem>());
        var doomedBay = Spawn("CollectionBotContainer", owner, Vector3.zero);
        var doomedBot = doomedBay.GetComponentInChildren<CollectionBot>(); doomedBot.obstacleMask = 0;
        var carriedDrop = LootDrop.Spawn(Part(), new Vector3(0, 1, 5));
        deadline = Time.time + 20f;
        while (!doomedBot.HasCargo && Time.time < deadline) yield return 0.05f;
        Check("Drone picked up cargo for bay destruction test", doomedBot.HasCargo);
        // Isolate destruction from unrelated player's group reconstruction in this probe.
        doomedBay.transform.SetParent(null, true);
        DestroyManager.Instance.DestroyGameObject(doomedBay);
        yield return 0.2f;
        Check("Destroying collection bay releases carried special part", carriedDrop != null && carriedDrop.Available && carriedDrop.transform.parent == null);
        LootDrop.ClearSession();

        var doomedOwner = Unit("Explosive hold owner", new Vector3(70, 0, 0), UnitFaction.Player, false);
        var doomedHold = Spawn("CargoHold", doomedOwner, Vector3.zero).GetComponent<CargoHold>();
        doomedHold.RestoreContents(new List<CargoItem> { Part(), Part() });
        DestroyManager.Instance.DestroyGameObject(doomedHold.gameObject);
        yield return 0.3f;
        Check("Actual explosive destruction emits cargo once", doomedHold == null && LootDrop.Active.Count(d => d.Item.kind == CargoKind.SpecialPart) == 2);
        LootDrop.ClearSession();
        var coinWreck = Unit("Non-explosive hold owner", new Vector3(80, 0, 0), UnitFaction.Player, false);
        var destroyedCoins = Spawn("CoinHold", coinWreck, Vector3.zero).GetComponent<CargoHold>();
        destroyedCoins.Store(Coins(12));
        DestroyManager.Instance.DestroyGameObject(destroyedCoins.gameObject);
        Check("Non-explosive destroyed hold leaves connectivity graph immediately", destroyedCoins.transform.parent == null);
        yield return 0.2f;
        Check("Actual non-explosive destruction spills currency", destroyedCoins == null && LootDrop.Active.Sum(d => d.Item.amount) == 12);
        LootDrop.ClearSession();

        hold.RestoreContents(new List<CargoItem> { Part(), Part() });
        string json = JsonUtility.ToJson(new BlockData(hold.GetComponent<Block>()));
        BlockData restored = JsonUtility.FromJson<BlockData>(json);
        Check("Blueprint JSON round-trips cargo", restored.cargo.Count == 2 && restored.cargo[0].resourcePath == "Blocks/UniversalThruster");
        var legacyCargo = JsonUtility.FromJson<BlockData>("{\"resourcePath\":\"Blocks/CargoHold\"}").cargo;
        Check("Old blueprint without cargo remains readable", legacyCargo == null || legacyCargo.Count == 0);
        // Only a unique, test-owned filename is touched; user saves are never loaded or overwritten.
        SaveManager previousSaveManager = SaveManager.instance;
        SaveManager.instance = null;
        var saveObject = new GameObject("Salvage persistence test");
        var saves = saveObject.AddComponent<SaveManager>();
        saves.currentSaveName = "SalvageProbe_" + Guid.NewGuid().ToString("N");
        string testPath = saves.GetSavePath(saves.currentSaveName);
        try
        {
            var blueprint = new BlockDataList();
            var savedBlock = new BlockData(hold.GetComponent<Block>());
            savedBlock.cargo = new List<CargoItem>();
            blueprint.blocks.Add(savedBlock);
            File.WriteAllText(testPath, JsonUtility.ToJson(blueprint));
            Check("Successful return writes cargo atomically", CargoPersistence.SaveReturnCargo(true));
            var returned = JsonUtility.FromJson<BlockDataList>(File.ReadAllText(testPath));
            Check("Return preserves blueprint geometry and two recovered parts", returned.blocks[0].cargo.Count == 2 && returned.blocks[0].posX == savedBlock.posX && File.Exists(testPath + ".cargo.bak"));
            Check("Death return persists empty cargo", CargoPersistence.SaveReturnCargo(false) && JsonUtility.FromJson<BlockDataList>(File.ReadAllText(testPath)).blocks[0].cargo.Count == 0);
        }
        finally
        {
            foreach (string suffix in new[] { "", ".cargo.bak", ".cargo.tmp" }) if (File.Exists(testPath + suffix)) File.Delete(testPath + suffix);
            Object.Destroy(saveObject);
            SaveManager.instance = previousSaveManager;
        }
        hold.ReleaseContents(UnitFaction.Player); hold.ReleaseContents(UnitFaction.Player);
        Check("Cargo destruction spills parts exactly once", LootDrop.Active.Count(d => d.Item.kind == CargoKind.SpecialPart) == 2 && hold.Used == 0);
        LootDrop.ClearSession();
        coins.ReleaseContents(UnitFaction.Player);
        Check("Player currency hold spills exact amount", LootDrop.Active.Sum(d => d.Item.amount) == 100);
        LootDrop.ClearSession();
        technology.ReleaseContents(UnitFaction.Enemy);
        Check("Enemy currency spill disabled by prefab default", LootDrop.Active.Count() == 0);

        var wreck = Unit("Enemy wreck", new Vector3(40, 0, 0), UnitFaction.Enemy, false);
        var ordinary = Spawn("1x1x1", wreck, Vector3.zero).GetComponent<Block>();
        WreckSalvage.Convert(ordinary); WreckSalvage.Convert(ordinary);
        Check("Ordinary enemy wreck converts to coins exactly once", LootDrop.Active.Count() == 1 && LootDrop.Active.First().Item.kind == CargoKind.Coins);
        LootDrop.ClearSession();
        var playerWreck = Unit("Player wreck", new Vector3(45, 0, 0), UnitFaction.Player, false);
        WreckSalvage.Convert(Spawn("1x1x1", playerWreck, Vector3.zero).GetComponent<Block>());
        Check("Player self-dismantling cannot farm coins", LootDrop.Active.Count() == 0);
        var special = LootDrop.Spawn(Part(), new Vector3(40, 0, 3));
        DestroyManager.Instance.ScheduleUnitCleanup(wreck);
        yield return WreckSalvage.Settings.cleanupDelay + 0.4f;
        Check("Cleanup removes ordinary wreck but preserves independent special loot", wreck == null && special != null);
        LootDrop.ClearSession();

        // Existing repair job still uses the extracted navigator and repair beam.
        var repairOwner = Unit("Repair regression owner", new Vector3(0, 0, 25), UnitFaction.Player);
        var repairBay = Spawn("RepairBotContianer", repairOwner, Vector3.zero);
        var repair = repairBay.GetComponentInChildren<RepairBot>(); repair.targetRange = 20; repair.obstacleMask = 0;
        var target = Spawn("1x1x1", repairOwner, new Vector3(0, 0, 4)).GetComponent<Durability>();
        target.currentDurability = 70f;
        yield return 7f;
        Check("RepairBot still finds and repairs its own unit after extraction", target.currentDurability > 70f);
        target.currentDurability = target.maxDurability;
        deadline = Time.time + 30f;
        while (repair.transform.parent != repair.home && Time.time < deadline)
        {
            repairOwner.transform.position += Vector3.right * 0.025f;
            yield return 0.1f;
        }
        Check("RepairBot returns to a moving authored socket", repair.transform.parent == repair.home && repair.currentState == Bot.NavigationState.Idle);
        Check("Authored scene changes remain confined to Play Mode", EditorApplication.isPlaying);
    }
}

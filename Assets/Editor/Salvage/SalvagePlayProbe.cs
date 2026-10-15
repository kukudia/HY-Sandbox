// 职责：在 Play Mode 检查货仓、回收、机器人和持久化行为。
// 入口：Run()。
// 环境：仅 Unity Editor；资产写入/运行验证以具体菜单入口为准。
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

    static SalvagePlayProbe()
    {
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/Salvage/Run Play Mode validation")]
    public static void Run()
    {
        Start(false);
    }

    [MenuItem("Tools/Salvage/Validate building debris in Play Mode")]
    public static void RunBuildings()
    {
        Start(true);
    }

    private static void Start(bool buildingsOnly)
    {
        if (EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Exit Play Mode before running the probe.");
        }

        if (File.Exists("Temp/SalvageWork/PlayValidation.json"))
        {
            File.Delete("Temp/SalvageWork/PlayValidation.json");
        }

        SessionState.SetBool("HY.SalvageProbe.BuildingsOnly", buildingsOnly);
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear();
            Errors.Clear();
            _background = Application.runInBackground;
            _timeScale = Time.timeScale;
            Application.runInBackground = true;
            Time.timeScale = 1f;
            // Coroutine-only destruction must retain an active host while world roots are isolated.
            // Reparent just these services in the disposable Play copy; the authored scene is untouched.
            DestroyManager.instance.transform.SetParent(null, true);
            VisualEffectsManager.EnsureInstance().transform.SetParent(null, true);
            // These changes exist only in the Play Mode copy. Unsaved authoring scenes stay untouched.
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root == DestroyManager.instance.gameObject || root == VisualEffectsManager.instance.gameObject)
                {
                    continue;
                }

                root.SetActive(false);
            }

            Application.logMessageReceived += Capture;
            _scenario = Scenario();
            _resume = 0f;
            _deadline = EditorApplication.timeSinceStartup + 140;
            EditorApplication.update += Tick;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
        }
    }

    private static void Capture(string condition, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
        {
            Errors.Add(condition + "\n" + stack);
        }
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > _deadline)
            {
                throw new TimeoutException("Salvage probe exceeded 140 seconds.");
            }

            if (Time.time < _resume)
            {
                return;
            }

            if (!_scenario.MoveNext())
            {
                Finish();
                return;
            }

            _resume = Time.time + (_scenario.Current is float seconds ? seconds : 0.05f);
        }
        catch (Exception exception)
        {
            Errors.Add(exception.ToString());
            Finish();
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Capture;
        Application.runInBackground = _background;
        Time.timeScale = _timeScale;
        var report = new Report
        {
            checks = Checks.ToArray(),
            errors = Errors.ToArray(),
            passed = Errors.Count == 0,
            unity = Application.unityVersion
        };
        Directory.CreateDirectory("Temp/SalvageWork");
        File.WriteAllText("Temp/SalvageWork/PlayValidation.json", JsonUtility.ToJson(report, true));
        EditorApplication.isPlaying = false;
    }

    [Serializable]
    private class Report
    {
        public bool passed;
        public string unity;
        public string[] checks;
        public string[] errors;
    }

    private static void Check(string name, bool result)
    {
        if (result)
        {
            Checks.Add(name);
        }
        else
        {
            throw new InvalidOperationException(name);
        }
    }

    private static ControlUnit Unit(string name, Vector3 position, UnitFaction faction, bool cockpit = true)
    {
        var root = new GameObject(name);
        root.transform.position = position;
        var body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        var unit = root.AddComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = faction;
        unit.EnsureRuntimeUnitId();
        if (cockpit)
        {
            var pilot = new GameObject("Probe cockpit");
            pilot.transform.SetParent(root.transform, false);
            unit.cockpit = pilot.AddComponent<Cockpit>();
            unit.cockpit.faction = faction;
            unit.cockpits = new[]
            {
                unit.cockpit
            };
            unit.hasValidCockpit = true;
        }

        return unit;
    }

    private static GameObject Spawn(string name, ControlUnit unit, Vector3 local)
    {
        var root = Object.Instantiate(Resources.Load<GameObject>("Blocks/" + name), unit.transform);
        root.transform.localPosition = local;
        Power power = root.GetComponent<Power>();
        if (power != null)
        {
            power.SetWorkRequested(root, true);
            power.currentPower = 1000f;
        }

        RuntimeUnitMember.Ensure(root, unit.runtimeUnitId, unit.faction);
        return root;
    }

    private static CargoItem Coins(int count) => new CargoItem
    {
        kind = CargoKind.Coins,
        amount = count
    };
    private static CargoItem Part() => new CargoItem
    {
        kind = CargoKind.SpecialPart,
        resourcePath = "Blocks/UniversalThruster",
        amount = 1
    };
    private static BuildingRuntime SalvageBuilding(WorldBuildingManager manager, UnitFaction faction, bool withBot = false)
    {
        var blueprint = new BlockDataList
        {
            foundation = new BuildingFoundationData()
        };
        foreach (string name in withBot ? new[]
        {
            "1x1x1",
            "CargoHold",
            "CollectionBotContainer"
        }

        : new[]
        {
            "1x1x1",
            "CargoHold"
        }

        )
        {
            Block prefab = Resources.Load<GameObject>("Blocks/" + name).GetComponent<Block>();
            var data = new BlockData(prefab)
            {
                id = name,
                resourcePath = "Blocks/" + name,
                posX = name == "CollectionBotContainer" ? 4f : -0.5f,
                posY = name == "1x1x1" ? 0.5f : 4f,
                posZ = 0.5f,
                rotW = 1f
            };
            data.cargo = name == "CargoHold" ? new List<CargoItem>
            {
                Part(),
                Part()
            }

            : null;
            blueprint.blocks.Add(data);
        }

        return manager.RestoreBuilding(new WorldBuildingState { instanceId = Guid.NewGuid().ToString("N"), blueprintName = "Salvage probe", faction = faction, position = new Vector3(200, 100, 200), blueprint = blueprint, durability = Enumerable.Repeat(100f, blueprint.blocks.Count).ToArray() });
    }

    private static IEnumerator BuildingSalvage()
    {
        var root = new GameObject("Building salvage probe");
        root.SetActive(false);
        var manager = root.AddComponent<WorldBuildingManager>();
        var serialized = new SerializedObject(manager);
        serialized.FindProperty("initializeOnStart").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        root.SetActive(true);
        SalvageSettings settings = WreckSalvage.Settings;
        float chance = settings.coinDropChance;
        float specialChance = settings.specialPartChance;
        try
        {
            settings.coinDropChance = 1f;
            settings.specialPartChance = 0f;
            foreach (bool exhausted in new[]
            {
                false,
                true
            }

            )
            {
                LootDrop.ClearSession();
                serialized.Update();
                serialized.FindProperty("maximumDebrisPieces").intValue = exhausted ? 0 : 12;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                BuildingRuntime building = SalvageBuilding(manager, UnitFaction.Enemy);
                Block target = building.Blocks[0];
                target.canExplode = false;
                Block detached = building.Blocks[1];
                target.GetComponent<Durability>().ApplyDamage(10000f, null);
                Check(
                    "Building damage removes detached cargo from snapshot " + exhausted,
                    building.Capture().blueprint.blocks.Count == 0);
                Check(
                    "Detached enemy building preserves cargo and rolls salvage before debris budget " + exhausted,
                    LootDrop.Active.Count(d => d.Item.kind == CargoKind.SpecialPart) == 2 && LootDrop.Active.Where(d => d.Item.kind == CargoKind.Coins).Sum(d => d.Item.amount) == settings.coinsPerBlock);
                WreckSalvage.Convert(detached);
                Check("Detached building salvage cannot convert twice " + exhausted, LootDrop.Active.Count() == 3);
                WorldBuildingState captured = building.Capture();
                manager.RemoveBuilding(building);
                yield return 0.15f;
                BuildingRuntime restored = manager.RestoreBuilding(captured);
                Check(
                    "Reloading destroyed building cannot respawn salvaged modules " + exhausted,
                    restored.Blocks.Count == 0 && LootDrop.Active.Count() == 3);
                manager.RemoveBuilding(restored);
                yield return 0.15f;
            }

            LootDrop.ClearSession();
            for (int unloadMode = 0; unloadMode < 3; unloadMode++)
            {
                bool exhausted = unloadMode > 0;
                serialized.Update();
                serialized.FindProperty("maximumDebrisPieces").intValue = exhausted ? 0 : 12;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                BuildingRuntime carrier = SalvageBuilding(manager, UnitFaction.Player, true);
                CollectionBot flying = carrier.GetComponentInChildren<CollectionBot>();
                flying.obstacleMask = 0;
                flying.targetRange = 30;
                flying.movementSpeed = 10f;
                foreach (Power power in carrier.GetComponentsInChildren<Power>())
                {
                    power.SetWorkRequested(carrier, true);
                    power.currentPower = 1000f;
                }

                LootDrop transported = LootDrop.Spawn(Part(), flying.transform.position + Vector3.forward * 5f);
                float limit = Time.time + 20f;
                while (!flying.HasCargo && Time.time < limit)
                {
                    yield return 0.05f;
                }

                Check(
                    "Building drone really leaves home and carries cargo " + exhausted,
                    flying.HasCargo && flying.transform.parent != flying.home);
                if (exhausted)
                {
                    WorldBuildingState sleep = carrier.Capture();
                    if (unloadMode == 2)
                    {
                        manager.PrepareForUnload();
                        root.SetActive(false);
                        manager.ClearSession();
                        root.SetActive(true);
                    }
                    else
                    {
                        manager.RemoveBuilding(carrier);
                    }

                    Check(
                        "Unload mode " + unloadMode + " releases carried cargo before hierarchy deactivation",
                        transported != null && transported.Available && transported.transform.parent == null);
                    BuildingRuntime wake = manager.RestoreBuilding(sleep);
                    Check(
                        "Waking building keeps only stored cargo, without duplicating in-flight delivery",
                        wake.Blocks[1].GetComponent<CargoHold>().Used == 2);
                    manager.RemoveBuilding(wake);
                }
                else
                {
                    carrier.Blocks[0].canExplode = false;
                    carrier.Blocks[0].GetComponent<Durability>().ApplyDamage(10000f, null);
                    Check(
                        "Building bay detachment immediately releases deployed drone cargo",
                        transported != null && transported.Available && transported.transform.parent == null);
                    Check(
                        "Detached building drone releases destination reservation",
                        transported.ClaimedBy == null && transported.Destination == null);
                    manager.RemoveBuilding(carrier);
                }

                yield return 0.15f;
                LootDrop.ClearSession();
            }

            BuildingRuntime player = SalvageBuilding(manager, UnitFaction.Player);
            player.Blocks[0].canExplode = false;
            player.Blocks[0].GetComponent<Durability>().ApplyDamage(10000f, null);
            Check(
                "Player building debris spills stored cargo without farming coins",
                LootDrop.Active.Count() == 2 && LootDrop.Active.All(d => d.Item.kind == CargoKind.SpecialPart));
            manager.RemoveBuilding(player);
            yield return 0.15f;
            LootDrop.ClearSession();
            BuildingRuntime sleeper = SalvageBuilding(manager, UnitFaction.Enemy);
            WorldBuildingState sleeping = sleeper.Capture();
            manager.RemoveBuilding(sleeper);
            yield return 0.15f;
            BuildingRuntime waking = manager.RestoreBuilding(sleeping);
            Check(
                "Streaming unload/reload preserves cargo without emitting salvage",
                !LootDrop.Active.Any() && waking.Blocks[1].GetComponent<CargoHold>().Used == 2);
            manager.ClearSession();
            yield return 0.15f;
            Check("Session teardown never rolls building salvage", !LootDrop.Active.Any());
        }
        finally
        {
            settings.coinDropChance = chance;
            settings.specialPartChance = specialChance;
            manager.ClearSession();
            Object.Destroy(root);
            LootDrop.ClearSession();
        }
    }

    private static IEnumerator Scenario()
    {
        var pm = new GameObject("Salvage test manager").AddComponent<PlayManager>();
        pm.enabled = false;
        pm.playMode = true;
        var ui = new GameObject("Salvage test UI").AddComponent<MainUIPanels>();
        ui.enabled = false;
        var camera = new GameObject("Salvage test camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        pm.mainCamera = camera;
        VisualEffectsManager.EnsureInstance().gameObject.SetActive(true);
        var build = new GameObject("Salvage test build").AddComponent<BuildManager>();
        build.enabled = false;
        build.blockLayer = LayerMask.GetMask("Block");
        IEnumerator buildings = BuildingSalvage();
        try
        {
            while (buildings.MoveNext())
            {
                yield return buildings.Current;
            }
        }
        finally
        {
            (buildings as IDisposable)?.Dispose();
        }

        if (SessionState.GetBool("HY.SalvageProbe.BuildingsOnly", false))
        {
            yield break;
        }

        // LootDrop.Update 以场景内已启用的 ControlUnit 为模拟门槛；隔离所有 Main 根后必须提供真实宿主。
        // 中立单位不读取玩家输入、不创建敌方AI；其他受测单位仍由探针控制，避免自动重分组。
        var simulationOwner = Unit("Salvage simulation host", new Vector3(500, 0, 0), UnitFaction.Neutral);
        simulationOwner.enabled = true;
        Check(
            "Scene simulation requires an enabled runtime unit",
            ControlUnit.IsSceneSimulationActive(camera.gameObject.scene));
        camera.transform.position = new Vector3(0f, 50f, 0f);
        foreach (CargoItem item in new[]
        {
            Coins(2),
            new CargoItem
            {
                kind = CargoKind.Technology,
                amount = 2
            },
            Part()
        }

        )
        {
            var visualDrop = LootDrop.Spawn(item, new Vector3(0f, 50f, 10f));
            Vector3 startPosition = visualDrop.transform.position;
            Transform display = visualDrop.transform.Find("Item display");
            Quaternion startRotation = display.localRotation;
            yield return 0.35f;
            TextMesh label = visualDrop.GetComponentInChildren<TextMesh>(true);
            Check(item.kind + " label visible without obstacle", label.gameObject.activeSelf);
            Check(
                item.kind + " scatters and rotates",
                (visualDrop.transform.position - startPosition).sqrMagnitude > 0.001f && Quaternion.Angle(startRotation, display.localRotation) > 1f);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = new Vector3(0f, 50f, 5f);
            wall.transform.localScale = new Vector3(10f, 10f, 0.5f);
            Physics.SyncTransforms();
            yield return 0.1f;
            Check(item.kind + " label shares icon occlusion", !label.gameObject.activeSelf);
            wall.SetActive(false);
            yield return 0.1f;
            Check(item.kind + " label reappears after obstacle removal", label.gameObject.activeSelf);
            Vector3 behind = camera.transform.position - Vector3.forward * 10f;
            visualDrop.transform.position = behind;
            yield return 0.1f;
            Check(item.kind + " label hidden behind camera", !label.gameObject.activeSelf);
            Object.Destroy(wall);
            LootDrop.ClearSession();
            yield return 0.1f;
        }

        camera.transform.position = Vector3.zero;
        var owner = Unit("Salvage owner", Vector3.zero, UnitFaction.Player);
        var hold = Spawn("CargoHold", owner, new Vector3(3, 0, 0)).GetComponent<CargoHold>();
        var coins = Spawn("CoinHold", owner, new Vector3(0, 0, -3)).GetComponent<CargoHold>();
        var technology = Spawn("TechnologyHold", owner, new Vector3(-3, 0, -3)).GetComponent<CargoHold>();
        yield return 0.2f;
        Check(
            "Authored capacities: 8 parts / 100 coins / 100 technology",
            hold.Capacity == 8 && coins.Capacity == 100 && technology.Capacity == 100);
        Check(
            "Default explosion policy",
            hold.GetComponent<Block>().canExplode && !coins.GetComponent<Block>().canExplode && !technology.GetComponent<Block>().canExplode);
        coins.GetComponent<Power>().currentPower = 0;
        Check("Unpowered hold rejects deposit", coins.Store(Coins(10)) == 0);
        coins.GetComponent<Power>().currentPower = 1000;
        Check(
            "Capacity clamps currency and rejects wrong kind",
            coins.Store(Coins(120)) == 100 && coins.Store(Coins(1)) == 0 && hold.Store(Coins(2)) == 0);
        coins.RestoreContents(new List<CargoItem> { Coins(97) });
        var coins2 = Spawn("CoinHold", owner, new Vector3(5, 0, -3)).GetComponent<CargoHold>();
        LootDrop.Spawn(Coins(10), new Vector3(0, 2, -3));
        yield return 2f;
        Check(
            "Currency arrival fills first hold and routes overflow without loss",
            coins.Used == 100 && coins2.Used == 7);
        LootDrop.Spawn(new CargoItem { kind = CargoKind.Technology, amount = 25 }, new Vector3(-3, 2, -3));
        yield return 1f;
        Check("Technology routes only to technology hold", technology.Used == 25);
        Check(
            "Resource fill uses capacity ratio",
            Mathf.Abs(technology.transform.Find("Model/Liquid fill").localScale.y - 0.81f * 0.25f) < 0.001f);
        var bay = Spawn("CollectionBotContainer", owner, Vector3.zero);
        var bot = bay.GetComponentInChildren<CollectionBot>();
        bot.movementSpeed = 8f;
        var drop = LootDrop.Spawn(Part(), new Vector3(0, 1, 5));
        yield return 0.4f;
        Check(
            "Collection drone reserves a special item",
            drop.ClaimedBy == bot && drop.Destination == hold && hold.Used == 0);
        bool sawCargo = false;
        float deadline = Time.time + 35f;
        while (hold.Used == 0 && Time.time < deadline)
        {
            sawCargo |= bot.HasCargo;
            yield return 0.1f;
        }

        Check(
            "Drone flies, carries and docks before crediting cargo",
            sawCargo && hold.Used == 1 && bot.transform.parent == bot.home);
        Check(
            "Stored item display contains no gameplay components",
            hold.transform.Find("Model/Contents display").GetComponentsInChildren<Block>().Length == 0);
        // 断电时保留任务等待恢复；显式禁用才撤销认领。
        hold.RestoreContents(Enumerable.Range(0, 7).Select(_ => Part()).ToList());
        var bay2 = Spawn("CollectionBotContainer", owner, new Vector3(-3, 0, 0));
        var bot2 = bay2.GetComponentInChildren<CollectionBot>();
        bot2.obstacleMask = 0;
        var drop2 = LootDrop.Spawn(Part(), new Vector3(0, 2, 10));
        var drop3 = LootDrop.Spawn(Part(), new Vector3(0, 2, 11));
        yield return 0.8f;
        Check(
            "Two drones cannot reserve beyond last free slot",
            new[] { drop2, drop3 }.Count(d => d.ClaimedBy != null) == 1);
        bay.GetComponent<Power>().currentPower = 0;
        bay2.GetComponent<Power>().currentPower = 0;
        yield return 0.2f;
        Check(
            "Power loss preserves task claim and keeps cargo",
            new[] { drop2, drop3 }.Count(d => d.ClaimedBy != null) == 1);
        Object.Destroy(bay);
        Object.Destroy(bay2);
        yield return 0.2f;
        LootDrop.ClearSession();
        hold.RestoreContents(new List<CargoItem>());
        var doomedBay = Spawn("CollectionBotContainer", owner, Vector3.zero);
        var doomedBot = doomedBay.GetComponentInChildren<CollectionBot>();
        doomedBot.obstacleMask = 0;
        var carriedDrop = LootDrop.Spawn(Part(), new Vector3(0, 1, 5));
        deadline = Time.time + 20f;
        while (!doomedBot.HasCargo && Time.time < deadline)
        {
            yield return 0.05f;
        }

        Check("Drone picked up cargo for bay destruction test", doomedBot.HasCargo);
        Transform carriedDisplay = carriedDrop.transform.Find("Item display");
        Check(
            "Carried display restores socket pose",
            carriedDisplay.localPosition.sqrMagnitude < 0.0001f && Quaternion.Angle(carriedDisplay.localRotation, Quaternion.identity) < 0.01f);
        // Isolate destruction from unrelated player's group reconstruction in this probe.
        doomedBay.transform.SetParent(null, true);
        DestroyManager.instance.DestroyGameObject(doomedBay);
        yield return 0.2f;
        Check(
            "Destroying collection bay releases carried special part",
            carriedDrop != null && carriedDrop.Available && carriedDrop.transform.parent == null);
        LootDrop.ClearSession();
        var doomedOwner = Unit("Explosive hold owner", new Vector3(70, 0, 0), UnitFaction.Player, false);
        var doomedHold = Spawn("CargoHold", doomedOwner, Vector3.zero).GetComponent<CargoHold>();
        doomedHold.RestoreContents(new List<CargoItem> { Part(), Part() });
        DestroyManager.instance.DestroyGameObject(doomedHold.gameObject);
        yield return 0.3f;
        Check(
            "Actual explosive destruction emits cargo once",
            doomedHold == null && LootDrop.Active.Count(d => d.Item.kind == CargoKind.SpecialPart) == 2);
        LootDrop.ClearSession();
        var coinWreck = Unit("Non-explosive hold owner", new Vector3(80, 0, 0), UnitFaction.Player, false);
        var destroyedCoins = Spawn("CoinHold", coinWreck, Vector3.zero).GetComponent<CargoHold>();
        destroyedCoins.Store(Coins(12));
        DestroyManager.instance.DestroyGameObject(destroyedCoins.gameObject);
        Check(
            "Non-explosive destroyed hold leaves connectivity graph immediately",
            destroyedCoins.transform.parent == null);
        yield return 0.2f;
        Check(
            "Actual non-explosive destruction spills currency",
            destroyedCoins == null && LootDrop.Active.Sum(d => d.Item.amount) == 12);
        LootDrop.ClearSession();
        hold.RestoreContents(new List<CargoItem> { Part(), Part() });
        string json = JsonUtility.ToJson(new BlockData(hold.GetComponent<Block>()));
        BlockData restored = JsonUtility.FromJson<BlockData>(json);
        Check(
            "Blueprint JSON round-trips cargo",
            restored.cargo.Count == 2 && restored.cargo[0].resourcePath == "Blocks/UniversalThruster");
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
            Check(
                "Return preserves blueprint geometry and two recovered parts",
                returned.blocks[0].cargo.Count == 2 && returned.blocks[0].posX == savedBlock.posX && File.Exists(testPath + ".cargo.bak"));
            Check(
                "Death return persists empty cargo",
                CargoPersistence.SaveReturnCargo(false) && JsonUtility.FromJson<BlockDataList>(File.ReadAllText(testPath)).blocks[0].cargo.Count == 0);
        }
        finally
        {
            foreach (string suffix in new[]
            {
                "",
                ".cargo.bak",
                ".cargo.tmp"
            }

            )
            {
                if (File.Exists(testPath + suffix))
                {
                    File.Delete(testPath + suffix);
                }
            }

            Object.Destroy(saveObject);
            SaveManager.instance = previousSaveManager;
        }

        hold.ReleaseContents(UnitFaction.Player);
        hold.ReleaseContents(UnitFaction.Player);
        Check(
            "Cargo destruction spills parts exactly once",
            LootDrop.Active.Count(d => d.Item.kind == CargoKind.SpecialPart) == 2 && hold.Used == 0);
        LootDrop.ClearSession();
        coins.ReleaseContents(UnitFaction.Player);
        Check("Player currency hold spills exact amount", LootDrop.Active.Sum(d => d.Item.amount) == 100);
        LootDrop.ClearSession();
        technology.ReleaseContents(UnitFaction.Enemy);
        Check("Enemy currency spill disabled by prefab default", LootDrop.Active.Count() == 0);
        var wreck = Unit("Enemy wreck", new Vector3(40, 0, 0), UnitFaction.Enemy, false);
        var ordinary = Spawn("1x1x1", wreck, Vector3.zero).GetComponent<Block>();
        SalvageSettings settings = WreckSalvage.Settings;
        Check(
            "Reduced authored drop rates",
            settings.coinDropChance == 0.3f && settings.specialPartChance == 0.08f);
        float coinChance = settings.coinDropChance;
        try
        {
            settings.coinDropChance = 1f;
            WreckSalvage.Convert(ordinary);
            WreckSalvage.Convert(ordinary);
            Check(
                "Guaranteed coin drop converts exactly once",
                LootDrop.Active.Count() == 1 && LootDrop.Active.First().Item.kind == CargoKind.Coins);
            LootDrop.ClearSession();
            settings.coinDropChance = 0f;
            var empty = Spawn("1x1x1", wreck, Vector3.right * 2f).GetComponent<Block>();
            WreckSalvage.Convert(empty);
            settings.coinDropChance = 1f;
            WreckSalvage.Convert(empty);
            Check("Zero coin chance stays empty on repeat conversion", !LootDrop.Active.Any());
        }
        finally
        {
            settings.coinDropChance = coinChance;
        }

        LootDrop.ClearSession();
        var playerWreck = Unit("Player wreck", new Vector3(45, 0, 0), UnitFaction.Player, false);
        WreckSalvage.Convert(Spawn("1x1x1", playerWreck, Vector3.zero).GetComponent<Block>());
        Check("Player self-dismantling cannot farm coins", LootDrop.Active.Count() == 0);
        var special = LootDrop.Spawn(Part(), new Vector3(40, 0, 3));
        DestroyManager.instance.ScheduleUnitCleanup(wreck);
        yield return WreckSalvage.Settings.cleanupDelay + 0.4f;
        Check(
            "Cleanup removes ordinary wreck but preserves independent special loot",
            wreck == null && special != null);
        LootDrop.ClearSession();
        // Existing repair job still uses the extracted navigator and repair beam.
        var repairOwner = Unit("Repair regression owner", new Vector3(0, 0, 25), UnitFaction.Player);
        var repairBay = Spawn("RepairBotContianer", repairOwner, Vector3.zero);
        var repair = repairBay.GetComponentInChildren<RepairBot>();
        repair.targetRange = 20;
        repair.obstacleMask = 0;
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

        Check(
            "RepairBot returns to a moving authored socket",
            repair.transform.parent == repair.home && repair.currentState == Bot.NavigationState.Idle);
        simulationOwner.enabled = false;
        Check(
            "Disabling the last runtime unit suspends scene simulation",
            !ControlUnit.IsSceneSimulationActive(camera.gameObject.scene));
        Check("Authored scene changes remain confined to Play Mode", EditorApplication.isPlaying);
    }
}

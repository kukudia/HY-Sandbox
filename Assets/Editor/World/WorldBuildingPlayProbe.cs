// 职责：在 WorldLab 真实物理帧验证建筑放置、战斗、事务恢复、支撑坍塌和输入预览。
// 入口：Run()；存档只写 Temp/WorldBuildings，结束恢复种子和初始建筑，不保存运行场景。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class WorldBuildingPlayProbe
{
    private const string Pending = "HY.WorldBuildingProbe";
    private static IEnumerator scenario;
    private static Report report;
    private static double nextTick;
    private static double deadline;
    private static Keyboard testKeyboard;
    private static Mouse testMouse;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    static WorldBuildingPlayProbe()
    {
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/HY-Sandbox/World/Validate building integration")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != WorldLabAuthoring.ScenePath)
        {
            throw new InvalidOperationException("Open WorldLab in Edit Mode first.");
        }

        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false) || state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        report = new Report
        {
            status = "running"
        };
        Application.logMessageReceived += CaptureError;
        scenario = Scenario();
        nextTick = 0;
        deadline = EditorApplication.timeSinceStartup + 120;
        EditorApplication.update += Tick;
    }

    private static void CaptureError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            report.errors.Add(message + "\n" + stack);
        }
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < nextTick)
        {
            return;
        }

        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Building probe timed out.");
            }

            if (!scenario.MoveNext())
            {
                Finish();
                return;
            }

            nextTick = EditorApplication.timeSinceStartup + 0.15;
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
            Finish();
        }
    }

    private static void Check(bool result, string description)
    {
        if (!result)
        {
            throw new InvalidOperationException(description);
        }

        report.checks.Add(description);
    }

    private static IEnumerator Scenario()
    {
        yield return null;
        WorldBuildingManager manager = Object.FindFirstObjectByType<WorldBuildingManager>();
        ProceduralVoxelWorld world = manager.World;
        WorldBuildingPalette palette = Object.FindFirstObjectByType<WorldBuildingPalette>();
        WorldLabController controller = Object.FindFirstObjectByType<WorldLabController>();
        controller.enabled = false;
        Check(manager.Buildings.Count == 1, "serialized initial layout spawns a real outpost");
        Check(
            PlayManager.instance == null && GameManager.instance == null,
            "WorldLab works without Main managers");
        palette.BeginPlacement();
        Check(palette.IsPlacing, "catalog opens placement mode");
        GameObject preview = GameObject.Find("Building preview");
        Check(
            preview != null && preview.GetComponentsInChildren<Collider>().Length == 0 && preview.GetComponentsInChildren<Power>().Length == 0 && preview.GetComponentsInChildren<MeshFilter>().All(filter => Vector3.Distance(filter.transform.localScale, new Vector3(4f, 0.1f, 4f)) > 0.01f),
            "preview contains module meshes without foundation geometry, gameplay components or physics");
        palette.Cancel();
        Check(!palette.BlocksWorldInput || palette.PointerOverPanel, "cancel releases placement input");
        BlockDataList sample = WorldBuildingAuthoring.CreateSample();
        for (int z = 17; z <= 23; z++)
        {
            for (int x = 17; x <= 33; x++)
            {
                world.SetCell(new Vector3Int(x, 18, z), 2);
                for (int y = 19; y < world.Data.Size.y; y++)
                {
                    world.SetCell(new Vector3Int(x, y, z), 0);
                }
            }
        }

        world.FlushDirty();
        var resized = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(sample));
        resized.blocks.Clear();
        BlockData enlarged = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(sample)).blocks[3];
        enlarged.x = 2;
        enlarged.posX = 0f;
        resized.blocks.Add(enlarged);
        BuildingRuntime custom = manager.Place(resized, "Resized", new Vector3(40, 38, 44), Quaternion.identity, UnitFaction.Player);
        Check(
            custom.Blocks[0].x == 2 && Mathf.Approximately(custom.Blocks[0].GetComponent<BoxCollider>().size.x, 1.9f),
            "CSV Awake preserves restored instance dimensions");
        manager.Restore(new WorldLabSnapshot { cellSize = world.CellSize, terrain = world.CaptureEdits(), buildings = new[] { manager.Buildings[0].Capture() } });
        yield return null;
        // Input System 注入真实设备状态，调用生产输入入口，确认放置不触发地形采掘。
        testKeyboard = InputSystem.AddDevice<Keyboard>();
        testMouse = InputSystem.AddDevice<Mouse>();
        Camera camera = palette.GetComponent<Camera>();
        Vector3 cameraPosition = camera.transform.position;
        Quaternion cameraRotation = camera.transform.rotation;
        Vector3 placementTarget = new Vector3(50, 38, 44);
        camera.transform.position = placementTarget + new Vector3(0, 20, -10);
        camera.transform.LookAt(placementTarget);
        InputSystem.QueueStateEvent(testMouse, new MouseState { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) });
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.B));
        InputSystem.Update();
        InvokeUpdate(palette);
        Check(palette.IsPlacing, "B key enters placement via production input");
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.T));
        InputSystem.Update();
        InvokeUpdate(palette);
        Check(
            (int)typeof(WorldBuildingPalette).GetField("quarterTurns", Private).GetValue(palette) == 1,
            "T key rotates placement by one quarter turn");
        uint unmined = world.Data.Fingerprint();
        InputSystem.QueueStateEvent(
            testMouse,
            new MouseState { position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), buttons = 1 });
        InputSystem.Update();
        InvokeUpdate(palette);
        InvokeUpdate(controller);
        Check(
            manager.Buildings.Count == 2 && world.Data.Fingerprint() == unmined,
            "LMB places building while world mining is suppressed");
        InputSystem.QueueStateEvent(testKeyboard, new KeyboardState(Key.Escape));
        InputSystem.Update();
        InvokeUpdate(palette);
        Check(!palette.IsPlacing, "Escape cancels via production input");
        InputSystem.RemoveDevice(testKeyboard);
        InputSystem.RemoveDevice(testMouse);
        testKeyboard = null;
        testMouse = null;
        camera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        manager.Restore(new WorldLabSnapshot { cellSize = world.CellSize, terrain = world.CaptureEdits(), buildings = new[] { manager.Buildings[0].Capture() } });
        yield return null;
        Vector3 a = new Vector3(40f, 38f, 40f);
        Vector3 b = new Vector3(50f, 38f, 40f);
        Vector3 c = new Vector3(60f, 38f, 40f);
        BuildingRuntime first = manager.Place(sample, "A", a, Quaternion.identity, UnitFaction.Player);
        BuildingRuntime second = manager.Place(sample, "B", b, Quaternion.Euler(0, 90, 0), UnitFaction.Player);
        BuildingRuntime third = manager.Place(sample, "C", c, Quaternion.Euler(0, 180, 0), UnitFaction.Player);
        Check(
            first.Blocks.Count == 5 && second.Blocks.Count == 5 && third.Blocks.Count == 5,
            "one blueprint creates three full instances");
        Check(
            (second.Blocks[0].transform.position - (b + second.transform.rotation * new Vector3(-1, 1, -1))).sqrMagnitude < 0.001f,
            "90 degree placement transforms template local coordinates");
        Check(first.Blocks[0].uniqueId != second.Blocks[0].uniqueId, "block identities are scoped per building");
        Check(
            first.GetComponent<ControlUnit>().IsOperational && !first.GetComponent<ControlUnit>().HasAnyCockpit,
            "building operates without any cockpit or foundation object");
        Check(
            first.FootprintSize == new Vector2Int(4, 4) && first.GetComponentsInChildren<BuildingFoundation>(true).Length == 0 && first.GetComponentsInChildren<Block>(true).Length == first.Blocks.Count,
            "runtime retains footprint data without spawning a foundation block, mesh or collider");
        Check(!manager.CanPlace(sample, a, Quaternion.identity, out _), "overlap rejected");
        Check(
            !manager.CanPlace(sample, a + Vector3.up * 2, Quaternion.identity, out _),
            "unsupported placement rejected");
        Check(
            !manager.CanPlace(sample, a - Vector3.up * 2, Quaternion.identity, out _),
            "buried placement rejected");
        first.Blocks[0].GetComponent<Durability>().ApplyDamage(7f, null);
        float damaged = first.Blocks[0].GetComponent<Durability>().currentDurability;
        CargoHold cargo = first.Blocks[1].GetComponent<CargoHold>();
        cargo.RestoreContents(new List<CargoItem> { new CargoItem { kind = cargo.Kind, resourcePath = "Blocks/1x1x1", amount = 2 } });
        WorldLabSnapshot snapshot = manager.Capture();
        Directory.CreateDirectory("Temp/WorldBuildings");
        string path = "Temp/WorldBuildings/integration.json";
        WorldLabPersistence.SaveSnapshot(path, snapshot);
        WorldLabPersistence.SaveSnapshot(path, snapshot);
        Check(File.Exists(path + ".previous"), "world snapshot atomic replace keeps previous backup");
        File.WriteAllText("Temp/WorldBuildings/corrupt.json", "{ invalid json");
        bool invalidJson = false;
        try
        {
            manager.Restore(WorldLabPersistence.LoadSnapshot("Temp/WorldBuildings/corrupt.json", world.CellSize));
        }
        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
        {
            invalidJson = true;
        }

        Check(
            invalidJson && manager.Buildings.Count == 4,
            "corrupt JSON rejected without replacing live buildings");
        manager.Restore(WorldLabPersistence.LoadSnapshot(path, world.CellSize));
        yield return null;
        first = manager.Buildings.First(building => building.InstanceId == snapshot.buildings[1].instanceId);
        Check(
            Mathf.Approximately(first.Blocks[0].GetComponent<Durability>().currentDurability, damaged),
            "damaged block health survives activation and JSON reload");
        Check(first.Blocks[1].GetComponent<CargoHold>().Used == 2, "cargo survives world reload");
        Check(manager.Buildings.Count == 4, "snapshot reload restores initial plus three placed buildings");
        second = manager.Buildings.First(building => building.InstanceId == snapshot.buildings[2].instanceId);
        Check(
            Quaternion.Angle(second.transform.rotation, Quaternion.Euler(0, 90, 0)) < 0.01f,
            "rotation survives JSON reload");
        uint terrainBefore = world.Data.Fingerprint();
        string identityBefore = manager.Buildings[0].InstanceId;
        var broken = JsonUtility.FromJson<WorldLabSnapshot>(JsonUtility.ToJson(snapshot));
        broken.buildings[1].blueprint.blocks[0].resourcePath = "Blocks/MissingBuildingPart";
        ExpectRejected(manager, broken, "missing resource rejected before mutation");
        broken = JsonUtility.FromJson<WorldLabSnapshot>(JsonUtility.ToJson(snapshot));
        broken.buildings[1].instanceId = broken.buildings[0].instanceId;
        ExpectRejected(manager, broken, "duplicate building identity rejected");
        broken = JsonUtility.FromJson<WorldLabSnapshot>(JsonUtility.ToJson(snapshot));
        broken.buildings[1].durability[0] = -1;
        ExpectRejected(manager, broken, "invalid durability rejected");
        broken = JsonUtility.FromJson<WorldLabSnapshot>(JsonUtility.ToJson(snapshot));
        broken.cellSize = 3f;
        ExpectRejected(manager, broken, "world cell size mismatch rejected");
        Check(
            world.Data.Fingerprint() == terrainBefore && manager.Buildings[0].InstanceId == identityBefore,
            "rejected restores preserve live terrain and building objects");
        // 真实生产炮塔循环：等待 Update 电网分配及 FixedUpdate 索敌、发射。
        foreach (BuildingRuntime building in manager.Buildings)
        {
            foreach (Turret turret in building.GetComponentsInChildren<Turret>())
            {
                turret.SetStarted(false);
            }
        }

        second.GetComponent<ControlUnit>().faction = UnitFaction.Enemy;
        Turret weapon = first.GetComponentInChildren<Turret>();
        weapon.range = 30f;
        weapon.damage = 0.5f;
        weapon.maxFireAngle = 180f;
        weapon.fireInterval = 0.03f;
        weapon.SetStarted(true);
        yield return null;
        yield return null;
        Check(
            weapon.GetComponent<Power>().IsWorkRequested && weapon.GetComponent<Power>().isWorking,
            "building turret independently acquires enemy and receives real network power");
        ControlUnit target = (ControlUnit)typeof(Turret).GetField("_target", Private).GetValue(weapon);
        Check(
            target == second.GetComponent<ControlUnit>(),
            "building turret targets hostile building without cockpit");
        Durability hitHealth = second.Blocks[0].GetComponent<Durability>();
        float before = hitHealth.currentDurability;
        Type resolver = typeof(Turret).Assembly.GetType("TurretHitResolver");
        resolver.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic).Invoke(
            null,
            new object[] { second.Blocks[0].GetComponent<Collider>(), hitHealth.transform.position, Vector3.up, Vector3.forward, first.GetComponent<ControlUnit>(), UnitFaction.Enemy, 2f, 0f });
        Check(
            hitHealth.currentDurability < before,
            "production projectile hit resolver damages hostile building");
        before = first.Blocks[0].GetComponent<Durability>().currentDurability;
        resolver.GetMethod("Apply", BindingFlags.Static | BindingFlags.NonPublic).Invoke(
            null,
            new object[] { first.Blocks[0].GetComponent<Collider>(), a, Vector3.up, Vector3.forward, first.GetComponent<ControlUnit>(), UnitFaction.Enemy, 2f, 0f });
        Check(
            Mathf.Approximately(first.Blocks[0].GetComponent<Durability>().currentDurability, before),
            "same faction building protected from hostile-only hit");
        TurretProjectile projectilePrefab = Resources.Load<GameObject>("Blocks/ProjectileTurretSingle").GetComponent<ProjectileTurret>().ProjectilePrefab;
        TurretProjectile shell = TurretProjectilePool.Rent(projectilePrefab, new Vector3(80, 44, 80), Quaternion.identity);
        shell.Launch(
            first.GetComponent<ControlUnit>(),
            UnitFaction.Enemy,
            1f,
            0f,
            Vector3.forward * 5f,
            2f,
            30f,
            ~0);
        float projectileStartZ = shell.transform.position.z;
        yield return null;
        yield return null;
        Check(
            shell != null && shell.gameObject.activeInHierarchy && shell.transform.position.z > projectileStartZ + 0.1f,
            "pooled physical projectile survives and flies without Main PlayManager");
        weapon.SetStarted(false);
        PowerTransmissionDevice relay = first.GetComponentInChildren<PowerTransmissionDevice>();
        weapon.SetStarted(true);
        relay.gameObject.SetActive(false);
        // 邻栋中继也可能覆盖本栋，移除测试区域其他网络后确认残留供电清理。
        foreach (BuildingRuntime building in manager.Buildings.Where(building => building != first))
        {
            foreach (PowerTransmissionDevice other in building.GetComponentsInChildren<PowerTransmissionDevice>())
            {
                other.enabled = false;
            }
        }

        yield return null;
        Check(!weapon.GetComponent<Power>().isWorking, "relay shutdown clears building turret power");
        relay.gameObject.SetActive(true);
        weapon.SetStarted(false);
        // 结构断裂：生成器支撑顶部炮塔，移除后炮塔成为独立刚体残骸。
        int originalCount = first.Blocks.Count;
        first.Blocks[0].canExplode = false;
        first.Blocks[0].GetComponent<Durability>().ApplyDamage(100000f, null);
        yield return null;
        Check(first.Blocks.Count < originalCount - 1, "destroyed support detaches disconnected blocks");
        Check(
            Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Any(body => body.name == "Building debris" && !body.isKinematic),
            "disconnected group becomes bounded physical debris");
        WorldLabSnapshot damagedSnapshot = manager.Capture();
        manager.Restore(damagedSnapshot);
        yield return null;
        first = manager.Buildings.First(building => building.InstanceId == snapshot.buildings[1].instanceId);
        Check(
            first.Blocks.Count < originalCount - 1,
            "reload preserves destroyed parts instead of respawning template");
        // 只去除第三栋脚下的地形；经过真实物理帧验证脱锚与下落。
        third = manager.Buildings.First(building => building.InstanceId == snapshot.buildings[3].instanceId);
        float initialY = third.transform.position.y;
        int initialFrame = Time.frameCount;
        for (int x = 0; x < third.FootprintSize.x; x++)
        {
            for (int z = 0; z < third.FootprintSize.y; z++)
            {
                Vector3 samplePoint = third.transform.TransformPoint(WorldBuildingPlacement.LocalAnchor(x, z) - Vector3.up * 0.05f);
                world.SetCell(world.WorldToCell(samplePoint), 0);
            }
        }

        for (int i = 0; i < 6; i++)
        {
            yield return null;
        }

        Check(
            !third.Anchored && third.SupportRatio < 0.5f,
            "terrain edits cause foundation support loss and detachment");
        Check(
            Time.frameCount > initialFrame + 3 && third.transform.position.y < initialY - 0.2f,
            "detached building falls during real simulation frames");
        Vector3 fallenPosition = third.transform.position;
        WorldLabSnapshot fallen = manager.Capture();
        manager.Restore(fallen);
        third = manager.Buildings.First(building => building.InstanceId == snapshot.buildings[3].instanceId);
        Check(
            !third.Anchored && Vector3.Distance(third.transform.position, fallenPosition) < 0.001f,
            "dynamic building pose reloads without editor grid snapping");
        yield return null;
        WorldLabPersistence.Save("Temp/WorldBuildings/legacy.json", world.CaptureEdits());
        manager.Restore(WorldLabPersistence.LoadSnapshot("Temp/WorldBuildings/legacy.json", world.CellSize));
        Check(manager.Buildings.Count == 0, "legacy voxel save migrates to empty building list");
        world.Generate();
        Check(
            manager.Buildings.Count == 1 && !Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Any(body => body.name == "Building debris" && body.gameObject.activeSelf),
            "regenerate restores configured layout and clears building debris");
        controller.enabled = true;
    }

    private static void InvokeUpdate(MonoBehaviour behaviour)
    {
        behaviour.GetType().GetMethod("Update", Private).Invoke(behaviour, null);
    }

    private static void ExpectRejected(WorldBuildingManager manager, WorldLabSnapshot snapshot, string name)
    {
        bool rejected = false;
        try
        {
            manager.Restore(snapshot);
        }
        catch (InvalidOperationException)
        {
            rejected = true;
        }

        Check(rejected, name);
    }

    private static void Finish()
    {
        Application.logMessageReceived -= CaptureError;
        if (testKeyboard != null)
        {
            InputSystem.RemoveDevice(testKeyboard);
        }

        if (testMouse != null)
        {
            InputSystem.RemoveDevice(testMouse);
        }

        EditorApplication.update -= Tick;
        SessionState.SetBool(Pending, false);
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Docs/Validation");
        File.WriteAllText("Docs/Validation/2026-10-15-world-buildings.json", JsonUtility.ToJson(report, true));
        Debug.Log("World building validation: " + report.status + " / " + report.checks.Count + " checks");
        EditorApplication.isPlaying = false;
    }
}

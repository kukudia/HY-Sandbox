// 职责：在隔离物理场景验证真实导航烘焙、绕墙驾驶意图与两种炮塔的遮挡锁定。
// 入口：Run()；仅 Play Mode，结果写入 Temp/NavigationCombat，不修改玩家蓝图和场景。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class NavigationCombatPlayChecks
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static Task<string> Pending
    {
        get; private set;
    }

    public static void Begin()
    {
        Pending = Run();
    }

    public static async Task<string> Run()
    {
        if (!Application.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Requires Main Play Mode.");
        }

        bool previousMode = PlayManager.instance.playMode;
        PlayManager.instance.playMode = true;
        Scene scene = SceneManager.CreateScene("Navigation combat checks", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var root = new GameObject("Navigation combat fixture");
        SceneManager.MoveGameObjectToScene(root, scene);
        root.transform.position = Vector3.right * 20000f;
        var checks = new List<string>();
        try
        {
            ControlUnit owner = Unit(root.transform, "Owner", UnitFaction.Player, new Vector3(-20f, 1f, 0f));
            ControlUnit hidden = Unit(root.transform, "Occluded enemy", UnitFaction.Enemy, new Vector3(20f, 1f, 0f));
            ControlUnit visible = Unit(root.transform, "Visible enemy", UnitFaction.Enemy, new Vector3(-20f, 1f, 50f));
            BoxCollider wall = Box(root.transform, "Solid wall", new Vector3(0f, 3.5f, 0f), new Vector3(2f, 8f, 40f));
            Box(root.transform, "Ground", Vector3.down * 0.5f, new Vector3(120f, 1f, 140f));
            Physics.SyncTransforms();
            CheckObstacleClimb(checks, root.transform, owner, wall);
            EnemyNavigationWorld world = root.AddComponent<EnemyNavigationWorld>();
            world.Rebuild();
            float timeout = Time.realtimeSinceStartup + 30f;
            while (!world.IsReady && Time.realtimeSinceStartup < timeout)
            {
                await Task.Delay(50);
            }

            Check(checks, "Runtime static colliders produce registered NavMesh", world.IsReady);
            EnemyController ai = owner.gameObject.AddComponent<EnemyController>();
            ai.enabled = false;
            typeof(EnemyController).GetField("currentTarget", Instance).SetValue(ai, hidden);
            MethodInfo move = typeof(EnemyController).GetMethod("CalculateDesiredMovement", Instance);
            Vector3 direction = (Vector3)move.Invoke(ai, new object[] { hidden.transform.position - owner.transform.position });
            Check(
                checks,
                "NavMesh bends pursuit around wall instead of direct pursuit",
                Mathf.Abs(direction.z) > 0.2f && direction.x > 0f);
            Check(
                checks,
                "Navigation produces intent without teleporting physics unit",
                owner.transform.localPosition == new Vector3(-20f, 1f, 0f));
            ai.desiredDistance = 80f;
            Vector3 nearWallDirection = (Vector3)move.Invoke(ai, new object[] { hidden.transform.position - owner.transform.position });
            Check(
                checks,
                "Wall detour takes priority over strafing within engagement distance",
                Mathf.Abs(nearWallDirection.z) > 0.2f && nearWallDirection.x > 0f);
            ai.desiredDistance = 18f;
            Type selectorType = typeof(Turret).Assembly.GetType("TurretTargetSelector");
            object selector = Activator.CreateInstance(selectorType, Instance, null, new object[] { owner.transform }, null);
            MethodInfo select = selectorType.GetMethod("FindNearestTarget", Instance);
            MethodInfo sight = selectorType.GetMethod("HasLineOfSight", Instance);
            var units = new List<ControlUnit>
            {
                hidden,
                visible
            };
            object[] arguments =
            {
                units,
                owner,
                UnitFaction.Enemy,
                100f,
                true,
                null
            };
            Check(
                checks,
                "Visible enemy wins over nearer cockpit behind wall",
                select.Invoke(selector, arguments) as ControlUnit == visible);
            Check(
                checks,
                "Solid local-physics wall blocks line of sight",
                !(bool)sight.Invoke(selector, new object[] { owner, hidden, hidden.cockpit.GetComponent<Durability>() }));
            wall.isTrigger = true;
            Physics.SyncTransforms();
            Check(
                checks,
                "Trigger does not occlude enemy cockpit",
                select.Invoke(selector, arguments) as ControlUnit == hidden);
            wall.isTrigger = false;
            Physics.SyncTransforms();
            object[] maskedOrigin =
            {
                owner.transform.position,
                (LayerMask)0
            };
            selectorType.GetMethod("ConfigureVisibility", Instance).Invoke(selector, maskedOrigin);
            Check(
                checks,
                "Visibility uses configured hit layer mask",
                select.Invoke(selector, arguments) as ControlUnit == hidden);
            selectorType.GetMethod("ConfigureVisibility", Instance).Invoke(selector, new object[] { owner.transform.position, (LayerMask)(~0) });
            foreach (Type turretType in new[]
            {
                typeof(LaserTurret),
                typeof(ProjectileTurret)
            }

            )
            {
                var weapon = new GameObject(turretType.Name + " sight check");
                weapon.transform.SetParent(owner.transform, false);
                Turret turret = (Turret)weapon.AddComponent(turretType);
                turret.range = 100f;
                MethodInfo refresh = typeof(Turret).GetMethod("RefreshTarget", Instance);
                typeof(Turret).GetProperty("Owner", Instance).SetValue(turret, owner);
                FieldInfo target = typeof(Turret).GetField("_target", Instance);
                wall.enabled = false;
                Physics.SyncTransforms();
                refresh.Invoke(turret, null);
                Check(
                    checks,
                    turretType.Name + " acquires unobstructed nearest enemy",
                    target.GetValue(turret) as ControlUnit == hidden);
                BoxCollider ownWall = Box(owner.transform, "Own hull wall", Vector3.right * 10f, new Vector3(1f, 8f, 10f));
                Physics.SyncTransforms();
                refresh.Invoke(turret, null);
                Check(
                    checks,
                    turretType.Name + " respects walls belonging to its own construct",
                    target.GetValue(turret) as ControlUnit == visible);
                ownWall.enabled = false;
                typeof(Turret).GetField("_nextSearchTime", Instance).SetValue(turret, 0f);
                Physics.SyncTransforms();
                refresh.Invoke(turret, null);
                wall.enabled = true;
                Physics.SyncTransforms();
                refresh.Invoke(turret, null);
                Check(
                    checks,
                    turretType.Name + " drops occluded lock before search interval",
                    target.GetValue(turret) as ControlUnit == visible);
                turret.enabled = false;
            }

            // 移除中间通路地板后重烘焙，确认覆盖内断路停止，而非回退直冲。
            wall.enabled = false;
            BoxCollider ground = root.transform.Find("Ground").GetComponent<BoxCollider>();
            ground.size = new Vector3(25f, 1f, 140f);
            ground.transform.localPosition = new Vector3(-25f, -0.5f, 0f);
            Box(root.transform, "Disconnected ground", new Vector3(25f, -0.5f, 0f), new Vector3(25f, 1f, 140f));
            world.enabled = false;
            world.enabled = true;
            Physics.SyncTransforms();
            world.Rebuild();
            timeout = Time.realtimeSinceStartup + 30f;
            while (!world.IsReady && Time.realtimeSinceStartup < timeout)
            {
                await Task.Delay(50);
            }

            owner.transform.localPosition = new Vector3(-16f, 1f, 0f);
            Vector3 partial = (Vector3)move.Invoke(ai, new object[] { hidden.transform.position - owner.transform.position });
            Check(
                checks,
                "Disconnected NavMesh stops at reachable edge instead of crossing gap",
                partial.sqrMagnitude < 0.001f);
            world.enabled = false;
            Check(checks, "Disabling navigation unregisters its NavMesh", !world.IsReady);
            Vector3 fallback = (Vector3)move.Invoke(ai, new object[] { hidden.transform.position - owner.transform.position });
            Check(checks, "Missing NavMesh preserves physics steering fallback", fallback.x > 0.5f);
            string report = string.Join("\n", checks);
            Directory.CreateDirectory("Temp/NavigationCombat");
            File.WriteAllText("Temp/NavigationCombat/checks.txt", report);
            return report;
        }
        finally
        {
            foreach (ControlUnit unit in root.GetComponentsInChildren<ControlUnit>(true))
            {
                PlayManager.instance.UnregisterControlUnit(unit);
            }

            Object.DestroyImmediate(root);
            _ = SceneManager.UnloadSceneAsync(scene);
            PlayManager.instance.playMode = previousMode;
        }
    }

    private static void CheckObstacleClimb(List<string> checks, Transform root, ControlUnit owner, BoxCollider wall)
    {
        Type type = typeof(EnemyController).Assembly.GetType("EnemyObstacleClimb");
        object climb = Activator.CreateInstance(type, true);
        MethodInfo query = type.GetMethod("TryGetHeight", Instance);
        MethodInfo clear = type.GetMethod("Clear", Instance);
        object[] arguments =
        {
            owner,
            Vector3.right,
            4f,
            50f,
            32f,
            3f,
            (LayerMask)(~0),
            0f
        };
        Check(
            checks,
            "Clear overhead prioritizes obstacle-top altitude with hull clearance",
            (bool)query.Invoke(climb, arguments) && (float)arguments[7] >= wall.bounds.max.y + 3.4f);
        owner.transform.localPosition = new Vector3(0f, 12f, 0f);
        Physics.SyncTransforms();
        Check(checks, "Climb altitude is held while hull crosses obstacle", (bool)query.Invoke(climb, arguments));
        owner.transform.localPosition = new Vector3(6f, 12f, 0f);
        Physics.SyncTransforms();
        Check(
            checks,
            "Climb altitude releases after the whole hull clears obstacle",
            !(bool)query.Invoke(climb, arguments));
        owner.transform.localPosition = new Vector3(-20f, 1f, 0f);
        Physics.SyncTransforms();
        clear.Invoke(climb, null);
        BoxCollider ceiling = Box(root, "Climb ceiling", new Vector3(-20f, 8f, 0f), new Vector3(10f, 1f, 10f));
        Physics.SyncTransforms();
        Check(
            checks,
            "Ceiling above hull rejects climb for horizontal navigation fallback",
            !(bool)query.Invoke(climb, arguments));
        ceiling.transform.localPosition = new Vector3(-5f, 11f, 0f);
        Physics.SyncTransforms();
        Check(checks, "Blocked elevated forward corridor rejects climb", !(bool)query.Invoke(climb, arguments));
        Object.DestroyImmediate(ceiling.gameObject);
        Physics.SyncTransforms();
        arguments[4] = 2f;
        Check(
            checks,
            "Obstacle above configured climb limit uses horizontal fallback",
            !(bool)query.Invoke(climb, arguments));
        arguments[4] = 32f;
        wall.isTrigger = true;
        Physics.SyncTransforms();
        Check(checks, "Trigger and own hull do not request climb", !(bool)query.Invoke(climb, arguments));
        wall.isTrigger = false;
        Physics.SyncTransforms();
    }

    private static ControlUnit Unit(Transform root, string name, UnitFaction faction, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        ControlUnit unit = go.AddComponent<ControlUnit>();
        unit.faction = faction;
        var cockpit = new GameObject("Cockpit");
        cockpit.transform.SetParent(go.transform, false);
        unit.cockpit = cockpit.AddComponent<Cockpit>();
        cockpit.AddComponent<Durability>().currentDurability = 100f;
        cockpit.AddComponent<BoxCollider>();
        unit.hasValidCockpit = true;
        PlayManager.instance.RegisterControlUnit(unit);
        return unit;
    }

    private static BoxCollider Box(Transform root, string name, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        BoxCollider box = go.AddComponent<BoxCollider>();
        box.size = size;
        return box;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        checks.Add((passed ? "PASS " : "FAIL ") + name);
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }
    }
}

// 职责：在隔离物理场景验证稳定局部驾驶、爬升净空与两种炮塔的遮挡锁定。
// 入口：Run()；仅 Play Mode，结果写入 Temp/NavigationCombat，不修改玩家蓝图和场景。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
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

    public static Task<string> Run()
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
            EnemyController ai = owner.gameObject.AddComponent<EnemyController>();
            ai.enabled = false;
            CheckStableSteering(checks, ai, owner);
            CheckContinuousCourse(checks, ai);
            CheckHullAvoidance(checks, root.transform, owner, ai);
            CheckLocalPath(checks, root.transform, owner);
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

            string report = string.Join("\n", checks);
            Directory.CreateDirectory("Temp/NavigationCombat");
            File.WriteAllText("Temp/NavigationCombat/checks.txt", report);
            return Task.FromResult(report);
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

    private static void CheckStableSteering(List<string> checks, EnemyController ai, ControlUnit owner)
    {
        MethodInfo steer = typeof(EnemyController).GetMethod("StabilizeDirection", Instance);
        FieldInfo next = typeof(EnemyController).GetField("nextDirectionChange", Instance);
        Vector3 initial = (Vector3)steer.Invoke(ai, new object[] { Vector3.right });
        Vector3 small = Quaternion.AngleAxis(5f, Vector3.up) * Vector3.right;
        Check(
            checks,
            "Small target-angle noise does not change committed heading",
            (Vector3)steer.Invoke(ai, new object[] { small }) == initial);
        Check(
            checks,
            "Large direction changes wait for heading hold time",
            (Vector3)steer.Invoke(ai, new object[] { Vector3.forward }) == initial);
        next.SetValue(ai, Time.time - 1f);
        Check(
            checks,
            "Meaningful turn is accepted after hold time",
            (Vector3)steer.Invoke(ai, new object[] { Vector3.forward }) == Vector3.forward);
        typeof(EnemyController).GetField("urgentAvoidance", Instance).SetValue(ai, true);
        Check(
            checks,
            "Imminent obstacle can override heading hold",
            (Vector3)steer.Invoke(ai, new object[] { Vector3.left }) == Vector3.left);
        Check(
            checks,
            "Normal movement decisions run no faster than twice per second",
            (float)typeof(EnemyController).GetMethod("GetMovementInterval", Instance).Invoke(ai, null) >= 0.5f);
        MethodInfo strafe = typeof(EnemyController).GetMethod("GetStrafeDirection", Instance);
        Vector3 side = (Vector3)strafe.Invoke(ai, new object[] { Vector3.right });
        Check(
            checks,
            "Strafe side stays fixed between scheduled changes",
            (Vector3)strafe.Invoke(ai, new object[] { Vector3.right }) == side);
        Check(
            checks,
            "Steering rules do not teleport the unit",
            owner.transform.localPosition == new Vector3(-20f, 1f, 0f));
    }

    private static void CheckContinuousCourse(List<string> checks, EnemyController ai)
    {
        FieldInfo course = typeof(EnemyController).GetField("courseVelocity", Instance);
        FieldInfo desired = typeof(EnemyController).GetField("desiredMovementInput", Instance);
        FieldInfo urgent = typeof(EnemyController).GetField("urgentAvoidance", Instance);
        MethodInfo input = typeof(EnemyController).GetMethod("GetVelocityInput", Instance);
        urgent.SetValue(ai, false);
        course.SetValue(ai, Vector3.right * 6f);
        desired.SetValue(ai, Vector3.left);
        input.Invoke(ai, null);
        Vector3 next = (Vector3)course.GetValue(ai);
        Check(checks, "Reverse course keeps reference speed instead of crossing zero", next.magnitude > 5.9f);
        Check(
            checks,
            "Accepted heading turns gradually each physics step",
            Vector3.Angle(Vector3.right, next) > 0.1f && Vector3.Angle(Vector3.right, next) <= 2f);
        for (int i = 0; i < 110; i++)
        {
            input.Invoke(ai, null);
        }

        Check(
            checks,
            "Continuous course converges to the new heading",
            Vector3.Angle(Vector3.left, (Vector3)course.GetValue(ai)) < 0.1f);
        desired.SetValue(ai, Vector3.zero);
        input.Invoke(ai, null);
        Check(checks, "Ending movement does not delay braking", (Vector3)course.GetValue(ai) == Vector3.zero);
        desired.SetValue(ai, Vector3.forward);
        urgent.SetValue(ai, true);
        input.Invoke(ai, null);
        Check(
            checks,
            "Urgent avoidance bypasses course smoothing",
            (Vector3)course.GetValue(ai) == Vector3.forward * 6f);
        urgent.SetValue(ai, false);
        course.SetValue(ai, Vector3.zero);
        desired.SetValue(ai, Vector3.zero);
    }

    private static void CheckHullAvoidance(List<string> checks, Transform root, ControlUnit owner, EnemyController ai)
    {
        Vector3 origin = owner.transform.localPosition;
        BoxCollider wing = Box(owner.transform, "Wide wing", Vector3.zero, new Vector3(2f, 1f, 10f));
        BoxCollider obstacle = Box(root, "Wing-only wall", origin + new Vector3(6f, 0f, 4.5f), new Vector3(1f, 3f, 1f));
        Physics.SyncTransforms();
        ai.obstacleLayers = 1 << obstacle.gameObject.layer;
        MethodInfo apply = typeof(EnemyController).GetMethod("ApplyObstacleAvoidance", Instance);
        FieldInfo bounds = typeof(EnemyController).GetField("avoidanceBounds", Instance);
        FieldInfo distance = typeof(EnemyController).GetField("probeDistance", Instance);
        FieldInfo next = typeof(EnemyController).GetField("avoidanceSideUntil", Instance);
        MethodInfo probe = typeof(EnemyController).GetMethod("ProbeObstacle", Instance);
        Rigidbody body = owner.GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.linearVelocity = Vector3.right * 4f;
        apply.Invoke(ai, new object[] { Vector3.forward, Vector3.forward });
        Check(
            checks,
            "Only actual velocity direction is swept",
            (int)typeof(EnemyController).GetField("debugProbeCount", Instance).GetValue(ai) == 1);
        Check(
            checks,
            "Hull sweep includes wing beyond center probe radius",
            ((Bounds)bounds.GetValue(ai)).extents.z >= 5f);
        next.SetValue(ai, 0f);
        Vector3 avoid = (Vector3)probe.Invoke(ai, new object[] { Vector3.right, 1f });
        Check(checks, "Wing-only wall generates avoidance", avoid.sqrMagnitude > 0.01f);
        Check(checks, "Wall on wing side selects opposite bypass side", avoid.z < 0f);
        obstacle.transform.localPosition = origin + new Vector3(0f, 0f, 5f);
        Physics.SyncTransforms();
        Check(
            checks,
            "Moving away from overlapping wing wall stays clear",
            ((Vector3)probe.Invoke(ai, new object[] { Vector3.back, 1f })).sqrMagnitude < 0.001f);
        obstacle.transform.localPosition = origin + new Vector3(3f, 0f, 0f);
        body.linearVelocity = Vector3.right * 8f;
        Physics.SyncTransforms();
        typeof(EnemyController).GetField("debugProbeCount", Instance).SetValue(ai, 0);
        Vector3 movement = (Vector3)apply.Invoke(ai, new object[] { Vector3.right, Vector3.right });
        typeof(EnemyController).GetField("desiredMovementInput", Instance).SetValue(ai, movement);
        Vector3 thrust = (Vector3)typeof(EnemyController).GetMethod("GetVelocityInput", Instance).Invoke(ai, null);
        Check(checks, "Obstacle reduces forward movement magnitude", movement.magnitude < 0.5f);
        Check(
            checks,
            "Obstacle slowdown never requests reverse thrust",
            Vector3.Dot(thrust, Vector3.right) >= -0.0001f);
        Vector3 smoothed = (Vector3)typeof(EnemyController).GetMethod("LimitObstacleInput", Instance).Invoke(ai, new object[] { Vector3.left + Vector3.forward });
        Check(
            checks,
            "Input smoothing cannot retain reverse thrust near obstacle",
            smoothed.x >= 0f && smoothed.z > 0f);
        obstacle.enabled = false;
        body.linearVelocity = Vector3.forward * 20f;
        typeof(EnemyController).GetField("debugProbeCount", Instance).SetValue(ai, 0);
        apply.Invoke(ai, new object[] { Vector3.right, Vector3.right });
        float fastDistance = (float)distance.GetValue(ai);
        Check(checks, "Prediction distance scales with actual velocity", fastDistance >= 30f);
        body.linearVelocity = Vector3.forward * 4f;
        typeof(EnemyController).GetField("debugProbeCount", Instance).SetValue(ai, 0);
        apply.Invoke(ai, new object[] { Vector3.forward, Vector3.forward });
        float slowDistance = (float)distance.GetValue(ai);
        Check(checks, "Higher speed gets a longer prediction lead", fastDistance > slowDistance);
        body.linearVelocity = Vector3.forward * 10f;
        typeof(EnemyController).GetField("debugProbeCount", Instance).SetValue(ai, 0);
        apply.Invoke(ai, new object[] { Vector3.forward, Vector3.forward });
        float mediumDistance = (float)distance.GetValue(ai);
        Check(
            checks,
            "Prediction lead increases across slow, cruise and fast speeds",
            slowDistance < mediumDistance && mediumDistance < fastDistance);
        Check(
            checks,
            "High speed gets extra braking-distance margin",
            fastDistance - mediumDistance > mediumDistance - slowDistance);
        Check(
            checks,
            "Sideways drift uses one actual-velocity sweep",
            (int)typeof(EnemyController).GetField("debugProbeCount", Instance).GetValue(ai) == 1);
        body.linearVelocity = Vector3.zero;
        body.isKinematic = true;
        wing.enabled = false;
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
            0f,
            0f
        };
        Check(
            checks,
            "Clear overhead prioritizes obstacle-top altitude with hull clearance",
            (bool)query.Invoke(climb, arguments) && (float)arguments[7] >= wall.bounds.max.y + 3.4f);
        owner.transform.localPosition = new Vector3(0f, 12f, 0f);
        Physics.SyncTransforms();
        Check(checks, "Climb altitude is held while hull crosses obstacle", (bool)query.Invoke(climb, arguments));
        arguments[1] = Vector3.zero;
        Check(checks, "Stopped hull retains committed climb altitude", (bool)query.Invoke(climb, arguments));
        arguments[1] = Vector3.up;
        Check(
            checks,
            "Vertical velocity cannot cancel horizontal climb commitment",
            (bool)query.Invoke(climb, arguments));
        arguments[1] = Vector3.right;
        owner.transform.localPosition = new Vector3(6f, 12f, 0f);
        Physics.SyncTransforms();
        BoxCollider nextWall = Box(root, "Next wall after descent", new Vector3(20f, 3.5f, 0f), new Vector3(2f, 8f, 40f));
        Physics.SyncTransforms();
        Check(
            checks,
            "Upcoming wall below current altitude keeps climb before descent",
            (bool)query.Invoke(climb, arguments));
        Object.DestroyImmediate(nextWall.gameObject);
        clear.Invoke(climb, null);
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
        owner.transform.localPosition = new Vector3(-20f, 12f, 0f);
        wall.transform.localPosition = new Vector3(0f, 20f, 0f);
        wall.size = new Vector3(2f, 24f, 40f);
        ceiling = Box(root, "Blocked climb permits underpass", new Vector3(-20f, 18f, 0f), new Vector3(10f, 1f, 10f));
        arguments[2] = 12f;
        arguments[8] = 12f;
        clear.Invoke(climb, null);
        Physics.SyncTransforms();
        Check(
            checks,
            "Blocked upward route prioritizes clear underpass",
            (bool)query.Invoke(climb, arguments) && (float)arguments[7] < 12f);
        BoxCollider floor = Box(root, "Underpass floor blockage", new Vector3(-20f, 4f, 0f), new Vector3(10f, 1f, 10f));
        clear.Invoke(climb, null);
        Physics.SyncTransforms();
        Check(
            checks,
            "Downward sweep rejects floor before horizontal fallback",
            !(bool)query.Invoke(climb, arguments));
        Object.DestroyImmediate(floor.gameObject);
        Object.DestroyImmediate(ceiling.gameObject);
        wall.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        wall.size = new Vector3(2f, 8f, 40f);
        owner.transform.localPosition = new Vector3(-20f, 1f, 0f);
        Physics.SyncTransforms();
    }

    private static void CheckLocalPath(List<string> checks, Transform root, ControlUnit owner)
    {
        Type type = typeof(EnemyController).Assembly.GetType("EnemyLocalPath");
        object planner = Activator.CreateInstance(type, true);
        MethodInfo follow = type.GetMethod("TryFollow", Instance);
        owner.transform.localPosition = new Vector3(-20f, 4f, -45f);
        BoxCollider wall = Box(root, "A star unjumpable wall", new Vector3(0f, 10f, -45f), new Vector3(3f, 25f, 16f));
        Physics.SyncTransforms();
        Vector3 destination = root.TransformPoint(new Vector3(20f, 4f, -45f));
        object[] args =
        {
            owner,
            new Bounds(owner.transform.position, Vector3.one * 2f),
            destination,
            (LayerMask)(~0),
            4f,
            64f,
            1024,
            2f,
            2f,
            6f,
            Vector3.zero
        };
        Check(checks, "A star routes around wall above climb limit", (bool)follow.Invoke(planner, args));
        var points = (IReadOnlyList<Vector3>)type.GetProperty("Points", Instance).GetValue(planner);
        Check(
            checks,
            "A star route contains lateral bypass",
            System.Linq.Enumerable.Any(points, p => Mathf.Abs(p.z - destination.z) > 8f));
        int expanded = (int)type.GetProperty("Expanded", Instance).GetValue(planner);
        Check(checks, "A star respects expansion budget", expanded > 0 && expanded <= 1024);
        Check(checks, "Cached path continues on next decision", (bool)follow.Invoke(planner, args));
        Vector3 blockedPoint = ((Vector3)args[10] + owner.transform.position) * 0.5f;
        BoxCollider blocker = Box(
            root,
            "New obstruction in cached route",
            root.InverseTransformPoint(blockedPoint),
            new Vector3(4f, 25f, 200f));
        Physics.SyncTransforms();
        Check(
            checks,
            "New geometry invalidates cached path before thrust",
            !(bool)follow.Invoke(planner, args) && !(bool)type.GetProperty("Active", Instance).GetValue(planner));
        Object.DestroyImmediate(blocker.gameObject);
        Physics.SyncTransforms();
        type.GetMethod("Clear", Instance).Invoke(planner, null);
        wall.size = new Vector3(3f, 25f, 200f);
        Physics.SyncTransforms();
        Check(checks, "Sealed local search returns no unsafe path", !(bool)follow.Invoke(planner, args));
        Check(checks, "Failed local search retries are throttled", !(bool)follow.Invoke(planner, args));
        type.GetMethod("Clear", Instance).Invoke(planner, null);
        wall.isTrigger = true;
        Physics.SyncTransforms();
        Check(checks, "Trigger does not require A star detour", !(bool)follow.Invoke(planner, args));
        Object.DestroyImmediate(wall.gameObject);
        owner.transform.localPosition = new Vector3(-20f, 1f, 0f);
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

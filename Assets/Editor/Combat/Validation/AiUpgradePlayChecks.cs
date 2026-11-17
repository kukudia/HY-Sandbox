// 职责：用真实运行组件检查目标稳定性、局部物理场景避障、修复重组索敌及炮弹相对运动提前量。
// 入口：Run()；仅当前 Play Mode，夹具隔离于本地物理场景，结果写入 Temp/AIUpgrade。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class AiUpgradePlayChecks
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    public static string Run()
    {
        if (!Application.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Requires Main or Siege Play Mode.");
        }

        var checks = new List<string>();
        bool previousMode = PlayManager.instance.playMode;
        PlayManager.instance.playMode = true;
        Scene scene = SceneManager.CreateScene("AI upgrade isolated checks", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        GameObject root = new GameObject("AI upgrade owned fixture");
        SceneManager.MoveGameObjectToScene(root, scene);
        try
        {
            ControlUnit owner = Unit(root.transform, "Raider", UnitFaction.Enemy, Vector3.zero);
            ControlUnit first = Unit(root.transform, "First", UnitFaction.Player, Vector3.forward * 20f);
            ControlUnit second = Unit(root.transform, "Second", UnitFaction.Player, Vector3.right * 19f);
            EnemyController ai = owner.gameObject.AddComponent<EnemyController>();
            FieldInfo current = typeof(EnemyController).GetField("currentTarget", Instance);
            MethodInfo find = typeof(EnemyController).GetMethod("FindNearestOpponent", Instance);
            current.SetValue(ai, first);
            Check(
                checks,
                "Small distance crossing retains current target",
                find.Invoke(ai, null) as ControlUnit == first);
            second.transform.position = Vector3.right * 10f;
            Check(
                checks,
                "Clearly nearer opponent replaces current target",
                find.Invoke(ai, null) as ControlUnit == second);
            ai.SetPriorityTarget(first);
            Check(
                checks,
                "Explicit mission overrides proximity hysteresis",
                find.Invoke(ai, null) as ControlUnit == first);
            ai.SetPriorityTarget(null);
            typeof(EnemyController).GetField("nextUpdateTime", Instance).SetValue(ai, Time.time + 100f);
            first.gameObject.SetActive(false);
            typeof(EnemyController).GetMethod("FixedUpdate", Instance).Invoke(ai, null);
            Check(
                checks,
                "Inactive target is replaced before scheduled search",
                current.GetValue(ai) as ControlUnit == second && owner.target == second.transform);
            Rigidbody probeBody = owner.GetComponent<Rigidbody>();
            probeBody.isKinematic = false;
            probeBody.linearVelocity = Vector3.forward * 4f;
            // 从真实决策入口准备本轮整舰范围、速度预测距离与绕行状态。
            typeof(EnemyController).GetMethod("ApplyObstacleAvoidance", Instance).Invoke(ai, new object[] { Vector3.forward, Vector3.forward });
            object vesselNavigation = typeof(EnemyController).GetField("navigation", Instance).GetValue(ai);
            var avoidance = (NavigationAvoidance)vesselNavigation.GetType().GetField("Local", Instance).GetValue(vesselNavigation);
            var settings = new NavigationAvoidanceSettings
            {
                UseHull = true,
                HullBounds = new Bounds(owner.transform.position, Vector3.one),
                ObstacleMask = ~0,
                ClearanceRadius = 0.25f,
                PredictiveRange = 15f,
                BrakingAcceleration = 6f,
                RayCount = 16,
                StuckTimeout = 2f,
                DetourHoldTime = 0.6f
            };
            var ground = new GameObject("Support plane");
            ground.transform.SetParent(root.transform, false);
            BoxCollider floor = ground.AddComponent<BoxCollider>();
            ground.transform.position = Vector3.down * 0.5f;
            floor.size = new Vector3(100f, 1f, 100f);
            Physics.SyncTransforms();
            Check(
                checks,
                "Initial ground overlap does not reverse horizontal flight",
                !avoidance.Evaluate(
                    null,
                    probeBody.linearVelocity,
                    Vector3.forward,
                    1f,
                    settings,
                    20f).OverrideDirection);
            GameObject wall = new GameObject("Trigger then solid local wall");
            wall.transform.SetParent(root.transform, false);
            wall.transform.position = Vector3.forward * 5f + Vector3.up * 0.5f;
            BoxCollider shape = wall.AddComponent<BoxCollider>();
            shape.size = new Vector3(4f, 4f, 0.2f);
            shape.isTrigger = true;
            Physics.SyncTransforms();
            Check(
                checks,
                "Trigger volumes do not redirect enemy flight",
                !avoidance.Evaluate(
                    null,
                    probeBody.linearVelocity,
                    Vector3.forward,
                    1f,
                    settings,
                    20f).OverrideDirection);
            shape.isTrigger = false;
            Physics.SyncTransforms();
            Check(
                checks,
                "Enemy avoidance queries its local PhysicsScene",
                avoidance.Evaluate(
                    null,
                    probeBody.linearVelocity,
                    Vector3.forward,
                    1f,
                    settings,
                    20f).OverrideDirection);
            for (int index = 0; index < 24; index++)
            {
                var dense = new GameObject("Dense hull " + index);
                dense.transform.SetParent(root.transform, false);
                dense.transform.position = wall.transform.position + Vector3.forward * (index * 0.02f);
                dense.AddComponent<BoxCollider>();
            }

            Physics.SyncTransforms();
            avoidance.Reset();
            NavigationAvoidanceResult denseAvoidance = avoidance.Evaluate(
                null,
                probeBody.linearVelocity,
                Vector3.forward,
                1f,
                settings,
                20f);
            Check(
                checks,
                "Dense hull query grows buffer and still avoids nearest obstacle",
                denseAvoidance.OverrideDirection && denseAvoidance.ObstacleCount > 0);
            ai.enabled = false;
            first.gameObject.SetActive(true);
            // 后续测试重组/修复，不应被前面的实体避障墙遮住目标。
            wall.SetActive(false);
            foreach (Transform child in root.transform)
            {
                if (child.name.StartsWith("Dense hull ", StringComparison.Ordinal))
                {
                    child.gameObject.SetActive(false);
                }
            }

            Physics.SyncTransforms();
            Type selectorType = typeof(Turret).Assembly.GetType("TurretTargetSelector");
            object selector = Activator.CreateInstance(selectorType, Instance, null, new object[] { owner.transform }, null);
            MethodInfo select = selectorType.GetMethod("FindNearestTarget", Instance);
            object[] args =
            {
                new List<ControlUnit>
                {
                    first
                },
                owner,
                UnitFaction.Player,
                100f,
                false,
                null
            };
            Durability original = Health(first.transform, "Original", Vector3.zero);
            select.Invoke(selector, args);
            Check(checks, "Initial scan acquires original damage module", args[5] as Durability == original);
            original.enabled = false;
            Durability repaired = Health(first.transform, "Repaired", Vector3.zero);
            select.Invoke(selector, args);
            Check(checks, "Existing selector discovers newly repaired module", args[5] as Durability == repaired);
            repaired.transform.SetParent(second.transform, true);
            select.Invoke(selector, args);
            Check(checks, "Reparented damage module is removed from old target", args[5] == null);
            repaired.transform.SetParent(first.transform, true);
            original.enabled = true;
            select.Invoke(selector, args);
            var inactive = new GameObject("Inactive nested module");
            inactive.SetActive(false);
            inactive.transform.SetParent(first.transform, false);
            Durability inactiveHealth = Health(inactive.transform, "Health", Vector3.back * 5f);
            select.Invoke(selector, args);
            inactive.SetActive(true);
            select.Invoke(selector, args);
            Check(
                checks,
                "Same-step activation invalidates shared hierarchy snapshot",
                args[5] as Durability == inactiveHealth);
            inactiveHealth.transform.localPosition = Vector3.forward * 50f;
            select.Invoke(selector, args);
            Check(
                checks,
                "Shared membership still reads live module positions",
                args[5] as Durability != inactiveHealth);
            inactive.transform.SetParent(second.transform, true);
            inactiveHealth.transform.localPosition = Vector3.zero;
            select.Invoke(selector, args);
            Check(
                checks,
                "Ancestor regroup cannot leave a module selectable by its old unit",
                args[5] as Durability != inactiveHealth);
            UnityEngine.Object.DestroyImmediate(inactive);
            CheckVisibility(root.transform, checks, selectorType, select);
            ProjectileTurret cannon = Object.Instantiate(Resources.Load<GameObject>("Blocks/ProjectileTurretSingle"), owner.transform).GetComponent<ProjectileTurret>();
            if (cannon == null)
            {
                throw new InvalidOperationException("Single barrel resource missing.");
            }

            typeof(Turret).GetProperty("Owner", Instance).SetValue(cannon, owner);
            cannon.range = 500f;
            MethodInfo aim = typeof(ProjectileTurret).GetMethod("GetAimPosition", Instance);
            owner.GetComponent<Rigidbody>().linearVelocity = Vector3.right * 5f;
            first.GetComponent<Rigidbody>().linearVelocity = Vector3.right * 20f;
            Vector3 origin = owner.transform.position;
            Vector3 aimPoint = (Vector3)aim.Invoke(cannon, new object[] { original, origin });
            float speed = (float)typeof(ProjectileTurret).GetField("_projectileSpeed", Instance).GetValue(cannon);
            Vector3 shotVelocity = (aimPoint - origin).normalized * speed + Vector3.right * 5f;
            float time = (original.transform.position.z - origin.z) / shotVelocity.z;
            Vector3 impact = origin + shotVelocity * time;
            Vector3 future = original.transform.position + Vector3.right * 20f * time;
            Check(
                checks,
                "Projectile lead matches crossing target with shooter velocity",
                time > 0f && Vector3.Distance(impact, future) < 0.01f);
            typeof(Turret).GetField("_target", Instance).SetValue(cannon, first);
            typeof(Turret).GetField("_targetDurability", Instance).SetValue(cannon, original);
            typeof(Turret).GetField("_nextSearchTime", Instance).SetValue(cannon, Time.time + 100f);
            first.faction = owner.faction;
            typeof(Turret).GetMethod("RefreshTarget", Instance).Invoke(cannon, null);
            Check(
                checks,
                "Faction change invalidates turret target before scheduled scan",
                typeof(Turret).GetField("_target", Instance).GetValue(cannon) == null);
            first.faction = UnitFaction.Player;
            original.transform.SetParent(second.transform, true);
            typeof(Turret).GetField("_target", Instance).SetValue(cannon, first);
            typeof(Turret).GetField("_targetDurability", Instance).SetValue(cannon, original);
            typeof(Turret).GetField("_nextSearchTime", Instance).SetValue(cannon, Time.time + 100f);
            typeof(Turret).GetMethod("RefreshTarget", Instance).Invoke(cannon, null);
            Check(
                checks,
                "Regrouped module immediately stops being the old turret aim target",
                typeof(Turret).GetField("_target", Instance).GetValue(cannon) as ControlUnit != first || typeof(Turret).GetField("_targetDurability", Instance).GetValue(cannon) as Durability != original);
            original.transform.SetParent(first.transform, true);
            cannon.range = 1f;
            Check(
                checks,
                "Out-of-range intercept falls back to direct aim",
                (Vector3)aim.Invoke(cannon, new object[] { original, origin }) == original.transform.position);
            Type solver = typeof(Turret).Assembly.GetType("ProjectileIntercept");
            MethodInfo solve = solver.GetMethod("TryGetTime", BindingFlags.Static | BindingFlags.NonPublic);
            object[] solveArgs =
            {
                Vector3.forward * 10f,
                Vector3.forward * 100f,
                85f,
                4f,
                0f
            };
            Check(checks, "Faster receding target has no intercept", !(bool)solve.Invoke(null, solveArgs));
            solveArgs[1] = Vector3.back * 85f;
            Check(
                checks,
                "Equal-speed approaching target solves linear case",
                (bool)solve.Invoke(null, solveArgs) && Mathf.Abs((float)solveArgs[4] - 10f / 170f) < 0.0001f);
            solveArgs[1] = Vector3.zero;
            solveArgs[3] = 0.01f;
            Check(checks, "Intercept beyond projectile lifetime is rejected", !(bool)solve.Invoke(null, solveArgs));
            string result = JsonUtility.ToJson(new Report { checks = checks }, true);
            Directory.CreateDirectory("Temp/AIUpgrade");
            File.WriteAllText("Temp/AIUpgrade/checks.json", result);
            return result;
        }
        finally
        {
            foreach (ControlUnit unit in root.GetComponentsInChildren<ControlUnit>(true))
            {
                PlayManager.instance.UnregisterControlUnit(unit);
            }

            Object.DestroyImmediate(root);
            SceneManager.UnloadSceneAsync(scene);
            PlayManager.instance.playMode = previousMode;
        }
    }

    private static void CheckVisibility(Transform root, List<string> checks, Type selectorType, MethodInfo select)
    {
        ControlUnit owner = Unit(root, "Visibility owner", UnitFaction.Enemy, new Vector3(1000, 0, 0));
        ControlUnit target = Unit(root, "Visibility target", UnitFaction.Player, new Vector3(1000, 0, 40));
        var weapon = new GameObject("Visibility weapon");
        weapon.transform.SetParent(owner.transform, false);
        weapon.AddComponent<BoxCollider>();
        object selector = Activator.CreateInstance(selectorType, Instance, null, new object[] { weapon.transform }, null);
        // 先加入较远模块，确保选择与层级顺序无关。
        Durability far = Health(target.transform, "Visible far module", new Vector3(12, 0, 0));
        far.gameObject.AddComponent<BoxCollider>();
        Durability near = Health(target.transform, "Near module", new Vector3(0, 0, -4));
        near.gameObject.AddComponent<BoxCollider>();
        var wall = new GameObject("Visibility wall");
        wall.transform.SetParent(owner.transform, false);
        wall.transform.localPosition = new Vector3(0, 0, 20);
        wall.AddComponent<BoxCollider>().size = new Vector3(4, 4, 1);
        object[] args =
        {
            new List<ControlUnit>
            {
                target
            },
            owner,
            UnitFaction.Player,
            100f,
            false,
            null
        };
        Physics.SyncTransforms();
        select.Invoke(selector, args);
        Check(
            checks,
            "Own hull blocks closest module but farther visible module remains selectable",
            args[5] as Durability == far);
        wall.SetActive(false);
        Physics.SyncTransforms();
        select.Invoke(selector, args);
        Check(checks, "Nearest visible module wins regardless of hierarchy order", args[5] as Durability == near);
        wall.SetActive(true);
        wall.GetComponent<BoxCollider>().size = new Vector3(40, 4, 1);
        Physics.SyncTransforms();
        select.Invoke(selector, args);
        Check(checks, "Fully occluded unit cannot be selected through own hull", args[5] == null);
        wall.transform.SetParent(weapon.transform, true);
        Physics.SyncTransforms();
        select.Invoke(selector, args);
        Check(checks, "Only the actual weapon hierarchy is ignored by visibility", args[5] as Durability == near);
        wall.SetActive(false);
        var shell = new GameObject("Target outer shell");
        shell.transform.SetParent(target.transform, false);
        shell.transform.localPosition = new Vector3(0, 0, -8);
        shell.AddComponent<BoxCollider>().size = new Vector3(4, 4, 1);
        Physics.SyncTransforms();
        select.Invoke(selector, args);
        Check(checks, "Target outer shell does not hide its own damage module", args[5] as Durability == near);
        for (int i = 0; i < 24; i++)
        {
            var dense = new GameObject("Dense target shell " + i);
            dense.transform.SetParent(target.transform, false);
            dense.transform.localPosition = new Vector3(0, 0, -5 - i * .03f);
            dense.AddComponent<BoxCollider>();
        }

        Physics.SyncTransforms();
        select.Invoke(selector, args);
        Check(
            checks,
            "Unordered saturated ray buffer grows and preserves target visibility",
            args[5] as Durability == near);
        owner.enabled = target.enabled = false;
    }

    private static ControlUnit Unit(Transform root, string name, UnitFaction faction, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        go.AddComponent<Rigidbody>().useGravity = false;
        ControlUnit unit = go.AddComponent<ControlUnit>();
        unit.faction = faction;
        var cockpit = new GameObject("Cockpit");
        cockpit.transform.SetParent(go.transform, false);
        unit.cockpit = cockpit.AddComponent<Cockpit>();
        unit.hasValidCockpit = true;
        PlayManager.instance.RegisterControlUnit(unit);
        return unit;
    }

    private static Durability Health(Transform root, string name, Vector3 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.localPosition = position;
        Durability health = go.AddComponent<Durability>();
        health.currentDurability = health.maxDurability = 100f;
        health.debugLog = false;
        return health;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

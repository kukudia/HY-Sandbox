// 职责：用隔离物理场景验证横向运动感知、相对碰撞时间和候选动作预算。
// 入口：Run()；仅Play Mode，真实PhysX采集，反射调用生产预测器，不修改试验场。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EnemyPredictionPlayChecks
{
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public static string Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode.");
        }

        Scene scene = SceneManager.CreateScene("Predictive navigation checks", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var root = new GameObject("Predictive navigation fixture");
        SceneManager.MoveGameObjectToScene(root, scene);
        var ownerObject = new GameObject("Sensor owner");
        ownerObject.transform.SetParent(root.transform);
        ownerObject.transform.position = Vector3.up * 20f;
        Rigidbody ownerBody = ownerObject.AddComponent<Rigidbody>();
        ownerBody.isKinematic = true;
        ControlUnit owner = ownerObject.AddComponent<ControlUnit>();
        owner.enabled = false;
        ownerObject.AddComponent<BoxCollider>();
        Type type = typeof(EnemyController).Assembly.GetType("EnemyNavigationPrediction");
        object predictor = Activator.CreateInstance(type, true);
        MethodInfo choose = type.GetMethod("Choose", Instance);
        var checks = new List<string>();
        var crossing = new GameObject("Crossing obstacle");
        crossing.transform.SetParent(root.transform);
        crossing.transform.position = new Vector3(8f, 20f, -8f);
        BoxCollider collider = crossing.AddComponent<BoxCollider>();
        collider.size = Vector3.one * 2f;
        Rigidbody moving = crossing.AddComponent<Rigidbody>();
        moving.useGravity = false;
        moving.linearVelocity = Vector3.forward * 4f;
        object[] arguments =
        {
            owner,
            new Bounds(owner.transform.position, Vector3.one * 2f),
            Vector3.right * 4f,
            Vector3.right,
            20f,
            (LayerMask)(~0),
            4f,
            3f,
            4f,
            90f,
            false,
            new Vector3(80f, 20f, 0f),
            null,
            0
        };
        try
        {
            Physics.SyncTransforms();
            Vector3 selected = (Vector3)choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Side-entering obstacle is gathered without forward ray hit",
                (int)type.GetProperty("Moving", Instance).GetValue(predictor) == 1);
            float contact = (float)type.GetProperty("CollisionTime", Instance).GetValue(predictor);
            Check(checks, "Relative trajectories predict crossing before contact", contact > 0f && contact < 3f);
            Check(
                checks,
                "Crossing changes speed or heading before contact",
                (selected - Vector3.right).sqrMagnitude > 0.01f);
            Check(
                checks,
                "Crossing has collision-free alternative in prediction window",
                (float)type.GetProperty("SelectedCollisionTime", Instance).GetValue(predictor) < 0f);
            Check(
                checks,
                "Candidate rollout stays within seven actions",
                (int)type.GetProperty("Candidates", Instance).GetValue(predictor) <= 7);
            Check(
                checks,
                "Own collider is excluded from observations",
                (int)type.GetProperty("Observed", Instance).GetValue(predictor) == 1);
            crossing.transform.position = new Vector3(8f, 20f, 0f);
            moving.linearVelocity = Vector3.right * 8f;
            Physics.SyncTransforms();
            selected = (Vector3)choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Receding obstacle does not seal its whole swept volume",
                selected == Vector3.right && (float)type.GetProperty("CollisionTime", Instance).GetValue(predictor) < 0f);
            Check(
                checks,
                "Clear course avoids full candidate search",
                (int)type.GetProperty("Candidates", Instance).GetValue(predictor) <= 2);
            moving.linearVelocity = Vector3.zero;
            crossing.transform.position = new Vector3(4f, 20f, 0f);
            collider.size = new Vector3(2f, 10f, 10f);
            arguments[2] = Vector3.right * 8f;
            arguments[3] = Vector3.forward;
            Physics.SyncTransforms();
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Turning rollout retains current momentum near wall",
                (float)type.GetProperty("CollisionTime", Instance).GetValue(predictor) >= 0f);
            arguments[10] = true;
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Committed static route compares slowdown without competing bypass",
                (int)type.GetProperty("Candidates", Instance).GetValue(predictor) == 3);
            Check(
                checks,
                "Prediction identifies the actual blocking collider",
                (string)type.GetProperty("ThreatName", Instance).GetValue(predictor) == crossing.name);
            collider.isTrigger = true;
            Physics.SyncTransforms();
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Trigger volumes do not become predicted walls",
                (int)type.GetProperty("Observed", Instance).GetValue(predictor) == 0);
            collider.isTrigger = false;
            arguments[5] = (LayerMask)0;
            Physics.SyncTransforms();
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Prediction respects obstacle layer mask",
                (int)type.GetProperty("Observed", Instance).GetValue(predictor) == 0);
            arguments[5] = (LayerMask)(~0);
            arguments[2] = Vector3.right * 80f;
            arguments[3] = Vector3.right;
            arguments[6] = 80f;
            collider.size = new Vector3(0.1f, 10f, 10f);
            Physics.SyncTransforms();
            choose.Invoke(predictor, arguments);
            contact = (float)type.GetProperty("CollisionTime", Instance).GetValue(predictor);
            Check(
                checks,
                "Continuous relative sweep catches thin wall between samples",
                contact >= 0f && contact < 0.25f);
            crossing.transform.position = new Vector3(1.8f, 20f, 0f);
            collider.size = Vector3.one * 2f;
            arguments[2] = Vector3.left * 4f;
            arguments[3] = Vector3.left;
            arguments[6] = 4f;
            Physics.SyncTransforms();
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Small initial envelope overlap permits monotonic escape",
                (float)type.GetProperty("CollisionTime", Instance).GetValue(predictor) < 0f);
            crossing.transform.position = new Vector3(55f, 20f, 0f);
            arguments[2] = Vector3.right * 4f;
            arguments[3] = Vector3.right;
            Physics.SyncTransforms();
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Far preview detects wall over fifty metres away before short rollout",
                (float)type.GetProperty("FarObstacleDistance", Instance).GetValue(predictor) > 50f && (float)type.GetProperty("CollisionTime", Instance).GetValue(predictor) < 0f);
            Check(
                checks,
                "Far preview reports arrival beyond ten seconds",
                (float)type.GetProperty("FarArrivalTime", Instance).GetValue(predictor) > 10f);
            Check(
                checks,
                "Default preview spans at least eighty metres",
                (float)type.GetProperty("LookAheadDistance", Instance).GetValue(predictor) >= 80f);
            moving.linearVelocity = Vector3.forward * 4f;
            crossing.transform.position = new Vector3(30f, 20f, -8f);
            Physics.SyncTransforms();
            selected = (Vector3)choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Distant crossing triggers early speed adjustment outside near horizon",
                (string)type.GetProperty("Decision", Instance).GetValue(predictor) == "EarlySlow" && selected.magnitude < 1f && (float)type.GetProperty("CollisionTime", Instance).GetValue(predictor) < 0f);
            arguments[1] = new Bounds(owner.transform.position, new Vector3(20f, 2f, 5f));
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Large ship preview scales to eight body lengths with cap",
                (float)type.GetProperty("LookAheadDistance", Instance).GetValue(predictor) == 160f);
            arguments[1] = new Bounds(owner.transform.position, Vector3.one * 2f);
            moving.linearVelocity = Vector3.zero;
            // 远场快照必须真正驱动规划，而非仅显示提示；规划时禁用碰撞体排除第二次物理查询的影响。
            crossing.transform.position = new Vector3(100f, 20f, 0f);
            collider.size = new Vector3(4f, 40f, 14f);
            type.GetProperty("MinimumDistance", Instance).SetValue(predictor, 120f);
            Physics.SyncTransforms();
            type.GetMethod("Observe", Instance).Invoke(
                predictor,
                new object[] { owner, arguments[1], Vector3.right * 4f, (LayerMask)(~0), 4f, 3f, new Vector3(140f, 20f, 0f), null, 0, 20f });
            Check(
                checks,
                "Far observation requests route before approaching a hundred metre wall",
                (string)type.GetProperty("FarDecision", Instance).GetValue(predictor) == "RouteBlocked" && (float)type.GetProperty("FarObstacleDistance", Instance).GetValue(predictor) > 90f);
            Type pathType = typeof(EnemyController).Assembly.GetType("EnemyLocalPath");
            object planner = Activator.CreateInstance(pathType, true);
            pathType.GetProperty("Perception", Instance).SetValue(planner, predictor);
            pathType.GetProperty("PreviewRequest", Instance).SetValue(planner, "FarRouteBlocked");
            collider.enabled = false;
            Physics.SyncTransforms();
            object[] follow =
            {
                owner,
                arguments[1],
                new Vector3(140f, 20f, 0f),
                (LayerMask)0,
                6f,
                96f,
                1024,
                2f,
                20f,
                20f,
                Vector3.zero
            };
            MethodInfo tryFollow = pathType.GetMethod("TryFollow", Instance);
            bool planned = (bool)tryFollow.Invoke(planner, follow);
            Check(checks, "A star uses shared observations beyond original search range", planned);
            Check(
                checks,
                "Far plan produces lateral bypass before near collision horizon",
                Mathf.Abs(((Vector3)follow[10]).z) > 5f);
            Check(
                checks,
                "Far planning preserves expansion budget and trigger",
                (int)pathType.GetProperty("Expanded", Instance).GetValue(planner) <= 1024 && (string)pathType.GetProperty("ReplanReason", Instance).GetValue(planner) == "FarRouteBlocked");
            int revision = (int)pathType.GetProperty("Revision", Instance).GetValue(planner);
            Check(
                checks,
                "Repeated far risk respects replan cooldown and retains route",
                (bool)tryFollow.Invoke(planner, follow) && revision == (int)pathType.GetProperty("Revision", Instance).GetValue(planner));
            choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Near decision reuses prepared snapshot without recapture",
                (int)type.GetProperty("Observed", Instance).GetValue(predictor) == 1);
            collider.enabled = true;
            for (int i = 0; i < 260; i++)
            {
                var clutter = new GameObject("Dense sensor fixture");
                clutter.transform.SetParent(root.transform);
                clutter.transform.position = new Vector3(15f, 20f, 15f);
                clutter.AddComponent<BoxCollider>();
            }

            Physics.SyncTransforms();
            selected = (Vector3)choose.Invoke(predictor, arguments);
            Check(
                checks,
                "Saturated sensor requests stop instead of assuming missing space clear",
                selected == Vector3.zero && (bool)type.GetProperty("Saturated", Instance).GetValue(predictor));
            Check(
                checks,
                "Sensor and rollout checks have fixed upper budgets",
                (int)type.GetProperty("Observed", Instance).GetValue(predictor) <= 256 && (int)type.GetProperty("SegmentChecks", Instance).GetValue(predictor) <= 256 * (12 * 7 + 8));
            type.GetMethod("Clear", Instance).Invoke(predictor, null);
            Check(
                checks,
                "Disable reset clears observations and decisions",
                (int)type.GetProperty("Observed", Instance).GetValue(predictor) == 0 && (string)type.GetProperty("Decision", Instance).GetValue(predictor) == "Idle");
            string result = string.Join("\n", checks);
            Directory.CreateDirectory("Temp/NavigationCombat");
            File.WriteAllText("Temp/NavigationCombat/prediction-checks.txt", result);
            return result;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            _ = SceneManager.UnloadSceneAsync(scene);
        }
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

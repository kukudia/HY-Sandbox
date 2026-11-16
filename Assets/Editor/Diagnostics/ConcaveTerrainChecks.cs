// 职责：验证真实非凸地形网格接触时避障与炮弹复用兼容最近点查询，避免Unity不支持API日志。
// 入口：Run()；Play模式隔离远端对象，恢复模拟开关，不写场景或玩家数据。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ConcaveTerrainChecks
{
    public static string Run()
    {
        if (!Application.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Requires Main Play Mode.");
        }

        var checks = new List<string>();
        var errors = new List<string>();
        Application.LogCallback capture = (message, stack, type) =>
        {
            if (message.Contains("Physics.ClosestPoint") || type == LogType.Error || type == LogType.Exception)
            {
                errors.Add(message);
            }
        };
        Application.logMessageReceived += capture;
        GameObject root = new GameObject("Isolated concave terrain query fixture");
        root.transform.position = new Vector3(14000, 3000, 14000);
        bool previous = PlayManager.instance.playMode;
        TurretProjectile shot = null;
        try
        {
            PlayManager.instance.playMode = true;
            GameObject terrain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            terrain.transform.SetParent(root.transform, false);
            terrain.transform.localScale = new Vector3(4f, 4f, 4f);
            Mesh mesh = terrain.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(terrain.GetComponent<BoxCollider>());
            MeshCollider concave = terrain.AddComponent<MeshCollider>();
            concave.sharedMesh = mesh;
            concave.convex = false;
            GameObject owner = new GameObject("Navigation owner", typeof(BoxCollider), typeof(Rigidbody));
            owner.transform.SetParent(root.transform, false);
            owner.GetComponent<Rigidbody>().isKinematic = true;
            Physics.SyncTransforms();
            Vector3 point = ImpactPhysics.ClosestPoint(concave, root.transform.position + Vector3.right * 3f);
            Require(
                Vector3.Distance(point, root.transform.position + Vector3.right * 2f) < .01f,
                "concave mesh uses bounded fallback position",
                checks);
            var settings = BotNavigationPolicy.CreateAvoidanceSettings();
            settings.UseHull = true;
            settings.HullBounds = new Bounds(owner.transform.position, Vector3.one);
            settings.ObstacleMask = ~0;
            var avoidance = new NavigationAvoidance(owner.transform);
            NavigationAvoidanceResult result = avoidance.Evaluate(
                null,
                Vector3.zero,
                Vector3.right,
                1f,
                settings,
                12f);
            Require(
                !float.IsNaN(result.Direction.x) && avoidance.QueryCount > 0,
                "hull overlap query handles a real nonconvex obstacle",
                checks);
            avoidance.ConstrainVelocity(Vector3.right, .02f, Vector3.zero, 10f);
            Require(errors.Count == 0, "navigation overlap and cast emit no unsupported closest-point message", checks);
            TurretProjectile prefab = Resources.Load<GameObject>("Blocks/ProjectileTurretDouble").GetComponent<ProjectileTurret>().ProjectilePrefab;
            shot = TurretProjectilePool.Rent(prefab, root.transform.position + Vector3.right * 2f, Quaternion.identity);
            shot.Launch(
                null,
                UnitFaction.Player,
                1f,
                0f,
                Vector3.right,
                3f,
                30f,
                ~0);
            Physics.SyncTransforms();
            bool resolved = (bool)typeof(TurretProjectile).GetMethod("ResolveOverlaps", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shot, new object[] { shot.transform.position, .25f });
            Require(
                resolved && !shot.gameObject.activeSelf,
                "real projectile overlap on concave mesh resolves and retires once",
                checks);
            Require(errors.Count == 0, "projectile contact emits no unsupported closest-point message", checks);
        }
        finally
        {
            Application.logMessageReceived -= capture;
            if (shot != null && shot.gameObject.activeSelf)
            {
                typeof(TurretProjectile).GetMethod("Retire", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shot, null);
            }

            PlayManager.instance.playMode = previous;
            root.SetActive(false);
            Object.Destroy(root);
        }

        Directory.CreateDirectory("Temp/GameplayPlayer");
        string report = "PASS\n" + string.Join("\n", checks);
        File.WriteAllText("Temp/GameplayPlayer/concave-checks.txt", report);
        return report;
    }

    private static void Require(bool value, string name, List<string> checks)
    {
        if (!value)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}

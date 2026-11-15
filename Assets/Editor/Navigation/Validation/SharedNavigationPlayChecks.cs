// 职责：在隔离 PhysX 场景验证 Bot/舰船共用避障的形状、过滤、查询预算和状态隔离。
// 入口：Run()；Play Mode 执行，结果写入 Temp/NavigationCombat/shared-checks.txt。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SharedNavigationPlayChecks
{
    public static string Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires Play Mode.");
        }

        Scene scene = SceneManager.CreateScene("Shared navigation checks", new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var root = new GameObject("Shared navigation fixture");
        SceneManager.MoveGameObjectToScene(root, scene);
        var checks = new List<string>();
        try
        {
            GameObject owner = Box(root.transform, "Owner", Vector3.zero, Vector3.one);
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.isKinematic = true;
            GameObject wing = Box(owner.transform, "Wing", Vector3.zero, new Vector3(2f, 1f, 10f));
            GameObject wall = Box(root.transform, "Wing-only wall", new Vector3(6f, 0f, 4.5f), new Vector3(1f, 3f, 1f));
            var botSettings = BotNavigationPolicy.CreateAvoidanceSettings();
            botSettings.ObstacleMask = ~0;
            var vesselSettings = botSettings;
            vesselSettings.UseHull = true;
            vesselSettings.HullBounds = new Bounds(Vector3.zero, new Vector3(2f, 1f, 10f));
            vesselSettings.RayCount = 16;
            var bot = new NavigationAvoidance(owner.transform);
            var vessel = new NavigationAvoidance(owner.transform);
            Physics.SyncTransforms();
            Check(
                checks,
                "Sphere profile ignores obstacle outside Bot clearance",
                !bot.Evaluate(
                    null,
                    Vector3.zero,
                    Vector3.right,
                    1f,
                    botSettings,
                    20f).OverrideDirection);
            Check(
                checks,
                "Hull profile detects the same wall at the wing",
                vessel.Evaluate(
                    null,
                    Vector3.zero,
                    Vector3.right,
                    1f,
                    vesselSettings,
                    20f).OverrideDirection);
            Check(checks, "Vessel local sampling has a bounded horizontal budget", vessel.QueryCount <= 18);
            wall.transform.localPosition = Vector3.right * 6f;
            vessel.Reset();
            bot.Reset();
            Physics.SyncTransforms();
            Check(
                checks,
                "Bot can approach its task target collider",
                !bot.Evaluate(
                    wall.transform,
                    Vector3.zero,
                    Vector3.right,
                    1f,
                    botSettings,
                    20f).OverrideDirection);
            Check(
                checks,
                "Vessel task target remains a collision obstacle",
                vessel.Evaluate(
                    wall.transform,
                    Vector3.zero,
                    Vector3.right,
                    1f,
                    vesselSettings,
                    20f).OverrideDirection);
            bot.Reset();
            Check(
                checks,
                "Bot blocked route samples at most 26 candidates plus direct and overlap",
                bot.Evaluate(
                    null,
                    Vector3.zero,
                    Vector3.right,
                    1f,
                    botSettings,
                    20f).OverrideDirection && bot.QueryCount <= 28);
            wall.SetActive(false);
            Physics.SyncTransforms();
            Check(
                checks,
                "Bot route memory does not leak into vessel instance",
                !new NavigationAvoidance(owner.transform).Evaluate(
                    null,
                    Vector3.zero,
                    Vector3.right,
                    1f,
                    vesselSettings,
                    20f).OverrideDirection);
            wall.SetActive(true);
            wall.transform.localPosition = Vector3.right * 12f;
            Physics.SyncTransforms();
            vessel.Reset();
            vessel.Evaluate(
                null,
                Vector3.zero,
                Vector3.right,
                1f,
                vesselSettings,
                20f);
            Vector3 safe = vessel.ConstrainVelocity(Vector3.right * 30f, 0.02f, Vector3.right * 30f, 6f);
            Check(checks, "Local PhysicsScene wall limits high-speed approach", safe.x < 15f);
            Check(
                checks,
                "Navigation leaves kinematic body pose unchanged",
                owner.transform.position == Vector3.zero && body.position == Vector3.zero);
            wall.SetActive(false);
            Box(root.transform, "Ground", Vector3.down, new Vector3(60f, 1f, 60f));
            Physics.SyncTransforms();
            vessel.Reset();
            Check(
                checks,
                "Support plane below hull does not block horizontal flight",
                !vessel.Evaluate(
                    null,
                    Vector3.zero,
                    Vector3.right,
                    1f,
                    vesselSettings,
                    20f).OverrideDirection);
            wing.SetActive(false);
            string report = string.Join("\n", checks);
            Directory.CreateDirectory("Temp/NavigationCombat");
            File.WriteAllText("Temp/NavigationCombat/shared-checks.txt", report);
            return report;
        }
        finally
        {
            Object.DestroyImmediate(root);
            SceneManager.UnloadSceneAsync(scene);
        }
    }

    private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size)
    {
        var item = new GameObject(name);
        item.transform.SetParent(parent, false);
        item.transform.localPosition = position;
        item.AddComponent<BoxCollider>().size = size;
        return item;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add("PASS " + name);
    }
}

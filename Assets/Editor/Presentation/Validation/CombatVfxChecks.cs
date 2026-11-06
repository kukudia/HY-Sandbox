// 职责：验证战斗特效资源与真实炮弹池复用的表现状态，不改变伤害/速度规则。
// 入口：RunPlay()；在隔离 Play 场景同步检查，finally 清理本次炮弹池。
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CombatVfxChecks
{
    public static string RunPlay()
    {
        if (!Application.isPlaying || Object.FindAnyObjectByType<TurretProjectilePool>() != null)
        {
            throw new InvalidOperationException("Requires isolated Play scene without an existing projectile pool.");
        }

        TurretProjectile prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Combat/TurretShell.prefab").GetComponent<TurretProjectile>();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        MethodInfo retire = typeof(TurretProjectile).GetMethod("Retire", flags);
        FieldInfo count = typeof(ProjectileTracer).GetField("count", flags);
        try
        {
            TurretProjectile first = TurretProjectilePool.Rent(prefab, Vector3.zero, Quaternion.identity);
            first.Launch(
                null,
                UnitFaction.Player,
                1,
                1,
                Vector3.forward * 42,
                2,
                100,
                ~0);
            Require(first.GetComponent<Rigidbody>().linearVelocity == Vector3.forward * 42, "Launch velocity changed");
            Require(
                (int)count.GetValue(first.GetComponent<ProjectileTracer>()) == 1,
                "Tracer did not start with one sample");
            retire.Invoke(first, null);
            Require(
                (int)count.GetValue(first.GetComponent<ProjectileTracer>()) == 0,
                "Tracer history leaked into pool");
            TurretProjectile reused = TurretProjectilePool.Rent(prefab, Vector3.one * 1000, Quaternion.identity);
            reused.Launch(
                null,
                UnitFaction.Player,
                1,
                1,
                Vector3.right * 85,
                2,
                100,
                ~0);
            Require(
                first == reused && (int)count.GetValue(reused.GetComponent<ProjectileTracer>()) == 1,
                "Pool reuse did not reset history");
            Vector3[] history = (Vector3[])typeof(ProjectileTracer).GetField("positions", flags).GetValue(reused.GetComponent<ProjectileTracer>());
            Require(history[0] == Vector3.one * 1000, "Old launch location retained");
            Require(reused.GetComponent<SphereCollider>().enabled, "Collider not re-enabled");
            retire.Invoke(reused, null);
            return "PASS: actual projectile pool reuses instance, clears tracer history, resets launch location and preserves velocity/collider.";
        }
        finally
        {
            TurretProjectilePool pool = Object.FindAnyObjectByType<TurretProjectilePool>();
            if (pool != null)
            {
                Object.DestroyImmediate(pool.gameObject);
            }
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

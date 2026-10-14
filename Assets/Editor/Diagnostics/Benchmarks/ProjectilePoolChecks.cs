// 职责：验证炮弹复用重置、自身碰撞过滤恢复与模拟退出清理，并测量创建/复用周期。
// 入口：Run()；Play Mode 隔离对象，生命周期微基准不代表完整交火帧率。
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ProjectilePoolChecks
{
    [Serializable]
    private sealed class Report
    {
        public string methodology = "100 alternating batches, 10 cycles/batch after 20 warmups. Before: Instantiate/Launch/DestroyImmediate; after: Rent/Launch/Retire. Null owner. Synchronous destruction bounds creation+cleanup CPU cost; not an exact deferred-Destroy frame or whole-game FPS comparison.";
        public string allocationCounter = "INVALID: calibration allocated 4096 bytes but GC.GetAllocatedBytesForCurrentThread returned zero in this Unity runtime. Ignore byte samples; no zero-GC claim.";
        public double beforeMedianMs;
        public double afterMedianMs;
        public long beforeMedianBytes;
        public long afterMedianBytes;
        public double[] beforeMs = new double[100];
        public double[] afterMs = new double[100];
        public long[] beforeBytes = new long[100];
        public long[] afterBytes = new long[100];
        public string[] checks;
    }

    public static string Run()
    {
        if (!Application.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Requires Play Mode.");
        }

        bool previousMode = PlayManager.instance.playMode;
        PlayManager.instance.playMode = true;
        GameObject root = new GameObject("Projectile pool isolated checks");
        TurretProjectile prefab = Resources.Load<GameObject>("Blocks/ProjectileTurretDouble").GetComponent<ProjectileTurret>().ProjectilePrefab;
        MethodInfo retire = typeof(TurretProjectile).GetMethod("Retire", BindingFlags.Instance | BindingFlags.NonPublic);
        try
        {
            GameObject ownerObject = new GameObject("First owner");
            ownerObject.transform.SetParent(root.transform);
            ControlUnit owner = ownerObject.AddComponent<ControlUnit>();
            owner.enabled = false;
            BoxCollider ownerCollider = ownerObject.AddComponent<BoxCollider>();
            TurretProjectile first = TurretProjectilePool.Rent(prefab, Vector3.one * 4000, Quaternion.identity);
            Launch(first, owner);
            SphereCollider collider = first.GetComponent<SphereCollider>();
            Require(Physics.GetIgnoreCollision(collider, ownerCollider), "Owner collision was not ignored");
            Action release = (Action)Delegate.CreateDelegate(typeof(Action), first, retire);
            release();
            Require(
                !first.gameObject.activeSelf && !Physics.GetIgnoreCollision(collider, ownerCollider),
                "Retirement did not restore ignored collision");
            TurretProjectile second = TurretProjectilePool.Rent(prefab, Vector3.one * 4100, Quaternion.Euler(0, 90, 0));
            Require(first == second, "Idle projectile was not reused");
            Launch(second, null);
            Require(
                collider.enabled && second.GetComponent<Rigidbody>().linearVelocity == Vector3.forward * 42 && second.transform.position == Vector3.one * 4100,
                "Reuse state was not reset");
            release();
            Report report = new Report
            {
                checks = new[]
                {
                    "Own colliders ignored on launch",
                    "Ignored collider state restored on retirement",
                    "Same instance reused",
                    "Collider, position and velocity reset for new launch"
                }
            };
            for (int i = 0; i < 20; i++)
            {
                Launch(TurretProjectilePool.Rent(prefab, Vector3.one * 4000, Quaternion.identity), null);
                release();
            }

            Stopwatch timer = new Stopwatch();
            for (int sample = 0; sample < 100; sample++)
            {
                for (int order = 0; order < 2; order++)
                {
                    bool before = (sample + order) % 2 == 0;
                    long bytes = GC.GetAllocatedBytesForCurrentThread();
                    timer.Restart();
                    for (int cycle = 0; cycle < 10; cycle++)
                    {
                        TurretProjectile shot = before ? Object.Instantiate(prefab, Vector3.one * 4000, Quaternion.identity) : TurretProjectilePool.Rent(prefab, Vector3.one * 4000, Quaternion.identity);
                        Launch(shot, null);
                        if (before)
                        {
                            Object.DestroyImmediate(shot.gameObject);
                        }
                        else
                        {
                            release();
                        }
                    }

                    timer.Stop();
                    long allocated = GC.GetAllocatedBytesForCurrentThread() - bytes;
                    (before ? report.beforeMs : report.afterMs)[sample] = timer.Elapsed.TotalMilliseconds / 10;
                    (before ? report.beforeBytes : report.afterBytes)[sample] = allocated / 10;
                }
            }

            report.beforeMedianMs = Median(report.beforeMs);
            report.afterMedianMs = Median(report.afterMs);
            report.beforeMedianBytes = Median(report.beforeBytes);
            report.afterMedianBytes = Median(report.afterBytes);
            string json = JsonUtility.ToJson(report, true);
            File.WriteAllText("Docs/Validation/2026-10-14-projectile-pool.json", json);
            return json;
        }
        finally
        {
            foreach (TurretProjectilePool pool in Object.FindObjectsByType<TurretProjectilePool>(FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(pool.gameObject);
            }

            Object.DestroyImmediate(root);
            PlayManager.instance.playMode = previousMode;
        }
    }

    private static void Launch(TurretProjectile shot, ControlUnit owner)
    {
        shot.Launch(
            owner,
            UnitFaction.Player,
            10,
            1,
            Vector3.forward * 42,
            2,
            45,
            ~0);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static T Median<T>(T[] values)
    {
        T[] copy = (T[])values.Clone();
        Array.Sort(copy);
        return copy[copy.Length / 2];
    }
}

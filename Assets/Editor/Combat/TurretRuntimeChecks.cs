// 职责：在当前 Play Mode 隔离检查真实炮塔资产、瞬时激光、炮管数量及炮弹物理扫掠。
// 入口：RunInCurrentPlayMode()；不保存场景、不切换模式，清理本次创建的检查对象。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TurretRuntimeChecks
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable]
    private sealed class Report
    {
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    /// <summary>返回 JSON 并写入 Temp/TurretRuntime；扫掠检查调用生产 FixedUpdate，不推进其他物体。</summary>
    public static string RunInCurrentPlayMode()
    {
        if (!EditorApplication.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Requires current Play Mode with PlayManager.");
        }

        Report report = new Report();
        GameObject root = new GameObject("Turret isolated checks");
        root.transform.position = new Vector3(4000f, 4000f, 4000f);
        bool previousMode = PlayManager.instance.playMode;
        PlayManager.instance.playMode = true;
        List<TurretProjectile> shots = new List<TurretProjectile>();
        try
        {
            GameObject laserPrefab = Resources.Load<GameObject>("Blocks/Turret");
            Check(
                report,
                laserPrefab != null && laserPrefab.GetComponent<LaserTurret>() != null,
                "Existing Turret resource migrated to LaserTurret");
            Check(
                report,
                AssetDatabase.GUIDToAssetPath("f5a03ef431944f29b88e05e1c8ee2594").EndsWith("LaserTurret.cs"),
                "Legacy turret script GUID preserved");
            ProjectileTurret[] cannonPrefabs = Resources.LoadAll<GameObject>("Blocks").Select(p => p.GetComponent<ProjectileTurret>()).Where(p => p != null).ToArray();
            Check(
                report,
                cannonPrefabs.Any(p => p.Muzzles.Length == 1) && cannonPrefabs.Any(p => p.Muzzles.Length == 2),
                "Single and double barrel editable assets exist");
            ControlUnit owner = Unit(root.transform, "Owner", UnitFaction.Player, Vector3.zero);
            ControlUnit enemy = Unit(root.transform, "Enemy", UnitFaction.Enemy, Vector3.forward * 7f);
            GameObject target = new GameObject("Target");
            target.transform.SetParent(enemy.transform, false);
            BoxCollider collider = target.AddComponent<BoxCollider>();
            collider.size = new Vector3(6f, 6f, 0.05f);
            Durability health = target.AddComponent<Durability>();
            health.maxDurability = health.currentDurability = 1000f;
            health.debugLog = false;
            Rigidbody targetBody = enemy.GetComponent<Rigidbody>();
            LaserTurret laser = Object.Instantiate(laserPrefab, owner.transform).GetComponent<LaserTurret>();
            laser.transform.localPosition = Vector3.zero;
            PrepareWeapon(laser, owner);
            Physics.SyncTransforms();
            float initialHealth = health.currentDurability;
            Vector3 initialVelocity = targetBody.linearVelocity;
            Vector3 initialForce = targetBody.GetAccumulatedForce();
            Vector3 initialTorque = targetBody.GetAccumulatedTorque();
            CallFire(laser, owner.transform.position, Vector3.forward);
            Check(report, health.currentDurability < initialHealth, "Laser damage occurs synchronously on fire");
            Check(report, targetBody.linearVelocity == initialVelocity, "Laser applies zero linear impulse");
            Check(report, targetBody.angularVelocity == Vector3.zero, "Laser applies zero angular impulse");
            Check(
                report,
                targetBody.GetAccumulatedForce() == initialForce && targetBody.GetAccumulatedTorque() == initialTorque,
                "Laser queues no deferred physics impulse or torque");
            laser.SetStarted(false);
            Check(report, !laser.GetComponent<Power>().IsWorkRequested, "Stopped turret releases power immediately");
            laser.gameObject.SetActive(false);
            foreach (int barrelCount in new[]
            {
                1,
                2
            }

            )
            {
                ProjectileTurret prefab = cannonPrefabs.First(p => p.Muzzles.Length == barrelCount);
                ProjectileTurret cannon = Object.Instantiate(prefab, owner.transform);
                cannon.transform.localPosition = Vector3.zero;
                PrepareWeapon(cannon, owner);
                Check(
                    report,
                    cannon.ProjectilePrefab != null && cannon.ProjectilePrefab.GetComponent<Rigidbody>() != null && cannon.ProjectilePrefab.GetComponent<Collider>() != null,
                    barrelCount + " barrel physical projectile prefab");
                HashSet<TurretProjectile> before = new HashSet<TurretProjectile>(Object.FindObjectsByType<TurretProjectile>(FindObjectsSortMode.None));
                Transform[] barrels = cannon.Muzzles.Select(m => m.parent).ToArray();
                Vector3[] rest = barrels.Select(b => b.localPosition).ToArray();
                Vector3[] muzzleRest = cannon.Muzzles.Select(m => m.position).ToArray();
                CallFire(cannon, cannon.Muzzles[0].position, cannon.Muzzles[0].forward);
                TurretProjectile[] spawned = Object.FindObjectsByType<TurretProjectile>(FindObjectsSortMode.None).Where(p => !before.Contains(p)).ToArray();
                shots.AddRange(spawned);
                Check(
                    report,
                    spawned.Length == barrelCount,
                    barrelCount + " barrel fires exactly " + barrelCount + " entities");
                Check(
                    report,
                    spawned.All(p => p.GetComponent<Rigidbody>().linearVelocity.sqrMagnitude > 0f),
                    barrelCount + " barrel shells have finite physical velocity");
                foreach (TurretProjectile shot in spawned)
                {
                    Object.DestroyImmediate(shot.gameObject);
                }

                MethodInfo stepRecoil = typeof(ProjectileTurret).GetMethod("UpdateRecoil", PrivateInstance);
                stepRecoil.Invoke(cannon, new object[] { 0.04f });
                Check(
                    report,
                    cannon.Muzzles.Select((m, i) => Vector3.Distance(m.position, muzzleRest[i])).All(d => Mathf.Abs(d - 0.12f) < 0.001f),
                    barrelCount + " barrels recoil 0.12 world meters together");
                Check(
                    report,
                    cannon.Muzzles.Select((m, i) => Vector3.Dot(m.position - muzzleRest[i], m.forward)).All(d => d < -0.119f),
                    barrelCount + " barrels recoil opposite firing direction");
                stepRecoil.Invoke(cannon, new object[] { 0.3f });
                Check(
                    report,
                    barrels.Select((b, i) => Vector3.Distance(b.localPosition, rest[i])).All(d => d < 0.00001f),
                    barrelCount + " barrels recover exact authored positions without drift");
                CallFire(cannon, cannon.Muzzles[0].position, cannon.Muzzles[0].forward);
                stepRecoil.Invoke(cannon, new object[] { 0.04f });
                shots.AddRange(Object.FindObjectsByType<TurretProjectile>(FindObjectsSortMode.None).Where(p => !before.Contains(p)));
                cannon.gameObject.SetActive(false);
                Check(
                    report,
                    barrels.Select((b, i) => Vector3.Distance(b.localPosition, rest[i])).All(d => d < 0.00001f),
                    barrelCount + " disabled turret immediately restores barrel pose");
            }

            TurretProjectile template = cannonPrefabs[0].ProjectilePrefab;
            TurretProjectile fastShot = Object.Instantiate(template, owner.transform.position, Quaternion.identity);
            shots.Add(fastShot);
            fastShot.Launch(
                owner,
                UnitFaction.Enemy,
                20f,
                5f,
                Vector3.forward * 1000f,
                2f,
                100f,
                ~0);
            Physics.SyncTransforms();
            float beforeShell = health.currentDurability;
            typeof(TurretProjectile).GetMethod("FixedUpdate", PrivateInstance).Invoke(fastShot, null);
            Check(
                report,
                Mathf.Approximately(beforeShell - health.currentDurability, 20f),
                "High speed physical shell sweeps thin wall and applies damage exactly once");
            Check(
                report,
                targetBody.GetAccumulatedForce().sqrMagnitude > initialForce.sqrMagnitude,
                "Physical shell queues configured impact impulse");
            Check(
                report,
                !fastShot.GetComponent<Collider>().enabled,
                "Resolved shell disables collision immediately");
            typeof(TurretProjectile).GetMethod("FixedUpdate", PrivateInstance).Invoke(fastShot, null);
            Check(
                report,
                Mathf.Approximately(beforeShell - health.currentDurability, 20f),
                "Resolved shell cannot damage twice");
            TurretProjectile expired = Object.Instantiate(template, owner.transform.position, Quaternion.identity);
            shots.Add(expired);
            expired.Launch(
                owner,
                UnitFaction.Enemy,
                20f,
                5f,
                Vector3.forward,
                2f,
                100f,
                ~0);
            typeof(TurretProjectile).GetField("_expiresAt", PrivateInstance).SetValue(expired, Time.time - 1f);
            typeof(TurretProjectile).GetMethod("FixedUpdate", PrivateInstance).Invoke(expired, null);
            Check(report, !expired.GetComponent<Collider>().enabled, "Lifetime expiration retires physical shell");
            BoxCollider ownCollider = owner.gameObject.AddComponent<BoxCollider>();
            TurretProjectile ownShot = Object.Instantiate(template, owner.transform.position, Quaternion.identity);
            shots.Add(ownShot);
            ownShot.Launch(
                owner,
                UnitFaction.Enemy,
                20f,
                5f,
                Vector3.forward,
                2f,
                100f,
                ~0);
            bool canHitOwner = (bool)typeof(TurretProjectile).GetMethod("CanHit", PrivateInstance).Invoke(ownShot, new object[] { ownCollider });
            Check(
                report,
                !canHitOwner && Physics.GetIgnoreCollision(ownShot.GetComponent<Collider>(), ownCollider),
                "Own construction ignored by physics and sweep filter");
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
        }
        finally
        {
            foreach (TurretProjectile shot in shots)
            {
                if (shot != null)
                {
                    Object.DestroyImmediate(shot.gameObject);
                }
            }

            Object.DestroyImmediate(root);
            PlayManager.instance.playMode = previousMode;
        }

        string json = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Temp/TurretRuntime");
        File.WriteAllText("Temp/TurretRuntime/PlayValidation.json", json);
        return json;
    }

    private static void PrepareWeapon(Turret turret, ControlUnit owner)
    {
        typeof(Turret).GetProperty("Owner", PrivateInstance).SetValue(turret, owner);
        Power power = turret.GetComponent<Power>();
        power.SetWorkRequested(turret, true);
        power.ReceivePower(power.RequestedPower);
    }

    private static void CallFire(Turret turret, Vector3 origin, Vector3 direction)
    {
        turret.GetType().GetMethod("Fire", PrivateInstance).Invoke(turret, new object[] { origin, direction, UnitFaction.Enemy });
    }

    private static ControlUnit Unit(Transform parent, string name, UnitFaction faction, Vector3 localPosition)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        ControlUnit unit = root.AddComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = faction;
        GameObject cockpit = new GameObject("Cockpit");
        cockpit.transform.SetParent(root.transform, false);
        unit.cockpit = cockpit.AddComponent<Cockpit>();
        unit.hasValidCockpit = true;
        return unit;
    }

    private static void Check(Report report, bool passed, string name)
    {
        if (passed)
        {
            report.checks.Add(name);
        }
        else
        {
            report.errors.Add(name);
        }
    }
}

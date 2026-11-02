// 职责：用基地中央的高耐久敌方固定靶验证四座建筑炮塔真实索敌、供电和弹道命中。
// 入口：Start() / Status；真实推进物理帧，固定靶是显式夹具，不代表自然波次完成。
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeBuildingPowerProbe
{
    private static SiegeSession session;
    private static ControlUnit target;
    private static double started;
    private static int initialFrame;
    public static string Status { get; private set; } = "Idle";

    public static string Start()
    {
        if (target != null || !EditorApplication.isPlaying)
        {
            throw new InvalidOperationException("Requires fresh Siege Play without another power probe.");
        }

        session = Object.FindFirstObjectByType<SiegeSession>();
        if (session?.Match == null || session.Match.Wave != 0)
        {
            throw new InvalidOperationException("Start a fresh Siege session.");
        }

        Block prefab = Resources.Load<GameObject>("Blocks/Cockpit").GetComponent<Block>();
        var blueprint = new BlockDataList();
        blueprint.blocks.Add(new BlockData(prefab) { id = "power-probe-target", posX = 0f, posY = 0f, posZ = 0f, rotX = 0f, rotY = 0f, rotZ = 0f, rotW = 1f, resourcePath = "Blocks/Cockpit", cargo = null });
        target = ModularUnitFactory.Spawn(
            blueprint,
            "Owned building power target",
            session.Fleet.Base.Core.transform.position + Vector3.up * 8f,
            Quaternion.identity,
            UnitFaction.Enemy);
        target.GetComponent<Rigidbody>().isKinematic = true;
        target.GetComponent<EnemyController>().enabled = false;
        Durability health = target.cockpit.GetComponent<Durability>();
        health.maxDurability = health.currentDurability = 100000f;
        target.cockpit.GetComponent<Block>().canExplode = false;
        if (!session.BeginNextWave())
        {
            throw new InvalidOperationException("Cannot start combat.");
        }

        target.GetComponent<Rigidbody>().isKinematic = true;
        started = EditorApplication.timeSinceStartup;
        initialFrame = Time.frameCount;
        Status = "Running";
        EditorApplication.update += Tick;
        return Status;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || session == null || target == null)
        {
            Finish("Interrupted before validation");
            return;
        }

        if (EditorApplication.timeSinceStartup - started < 4f)
        {
            return;
        }

        Turret[] turrets = session.Fleet.Base.Unit.GetComponentsInChildren<Turret>();
        var report = new Report
        {
            advancedFrames = Time.frameCount - initialFrame,
            seconds = EditorApplication.timeSinceStartup - started,
            turretCount = turrets.Length,
            requested = turrets.Count(turret => turret.GetComponent<Power>().IsWorkRequested),
            powered = turrets.Count(turret => turret.GetComponent<Power>().isWorking),
            damage = 100000f - target.cockpit.GetComponent<Durability>().currentDurability
        };
        report.passed = report.advancedFrames > 30 && report.turretCount == 4 && report.requested == 4 && report.powered == 4 && report.damage > 0f;
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/building-power.json", JsonUtility.ToJson(report, true));
        Finish(report.passed ? "Passed" : "Failed; see Temp/Siege/building-power.json");
    }

    private static void Finish(string result)
    {
        EditorApplication.update -= Tick;
        if (target != null)
        {
            Object.Destroy(target.gameObject);
        }

        target = null;
        session = null;
        Status = result;
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public int advancedFrames;
        public double seconds;
        public int turretCount;
        public int requested;
        public int powered;
        public float damage;
    }
}

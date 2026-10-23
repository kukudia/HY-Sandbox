// 职责：真实Play跨帧验证范围外任务目标的接近输入与刚体位移，保留生产飞行和供电参数。
// 入口：Start() / Status；仅创建并清理自己的敌舰，不代表自然整波性能或全部AI导航。
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegePriorityTargetFlight
{
    public static string Status { get; private set; } = "Idle";

    private static ControlUnit raider;
    private static ControlUnit target;
    private static Report report;
    private static double started;
    private static int lastFrame;
    public static string Start()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0 || raider != null)
        {
            throw new InvalidOperationException("Requires fresh Siege Play without a running flight check.");
        }

        target = session.Fleet.Base.Unit;
        BlockDataList blueprint = SiegeFleet.PrepareBlueprint(session.Settings.Ships[0].Blueprint, out _);
        raider = EnemyBlueprintFactory.Spawn(
            blueprint,
            "Owned far mission flight",
            session.Fleet.Base.Core.transform.position + Vector3.right * 300f + Vector3.up * 10f,
            Quaternion.LookRotation(Vector3.left));
        if (raider == null || !session.BeginNextWave())
        {
            if (raider != null)
            {
                Object.DestroyImmediate(raider.gameObject);
            }

            raider = null;
            throw new InvalidOperationException("Could not start the real flight fixture.");
        }

        raider.GetComponent<EnemyController>().SetPriorityTarget(target);
        report = new Report
        {
            initialDistance = Vector3.Distance(raider.transform.position, target.transform.position)
        };
        started = EditorApplication.timeSinceStartup;
        lastFrame = Time.frameCount;
        Status = "Running";
        EditorApplication.update += Tick;
        AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
        return Status;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || raider == null || target == null)
        {
            Finish("Fixture interrupted");
            return;
        }

        if (Time.frameCount != lastFrame)
        {
            lastFrame = Time.frameCount;
            report.frames++;
            Vector3 direction = target.transform.position - raider.transform.position;
            direction.y = 0f;
            if (Vector3.Dot(raider.MovementInput, direction.normalized) > 0.1f)
            {
                report.approachFrames++;
            }
        }

        if (EditorApplication.timeSinceStartup - started >= 12.0)
        {
            Finish("Duration reached");
        }
    }

    private static void BeforeReload()
    {
        Finish("Assembly reload");
    }

    private static void Finish(string reason)
    {
        EditorApplication.update -= Tick;
        AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
        report.reason = reason;
        report.elapsedSeconds = EditorApplication.timeSinceStartup - started;
        report.finalDistance = raider != null && target != null ? Vector3.Distance(raider.transform.position, target.transform.position) : report.initialDistance;
        report.passed = reason == "Duration reached" && report.frames >= 120 && report.approachFrames > report.frames / 2 && report.initialDistance - report.finalDistance > 5f;
        Directory.CreateDirectory("Temp/Siege");
        string path = "Temp/Siege/priority-target-flight.json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        if (raider != null)
        {
            PlayManager.instance?.UnregisterControlUnit(raider);
            Object.DestroyImmediate(raider.gameObject);
        }

        raider = null;
        target = null;
        Status = path;
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string reason;
        public double elapsedSeconds;
        public int frames;
        public int approachFrames;
        public float initialDistance;
        public float finalDistance;
    }
}

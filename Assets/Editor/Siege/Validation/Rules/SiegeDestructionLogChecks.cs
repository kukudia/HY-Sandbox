// 职责：隔离检查销毁/VFX诊断默认值、可恢复开关及ABBA日志路径开销，不刺激自然战斗。
// 入口：Run()；禁用效果的同方法对照只测诊断路径，不代表交火帧率或真实爆炸成本。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

public static class SiegeDestructionLogChecks
{
    public static string Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0)
        {
            throw new InvalidOperationException("Requires fresh Siege Play.");
        }

        var report = new Report();
        VisualEffectsManager effects = VisualEffectsManager.EnsureInstance();
        DestroyManager destruction = DestroyManager.instance;
        FieldInfo vfxFlag = typeof(VisualEffectsManager).GetField("_debugDestructionVfx", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo destroyFlag = typeof(DestroyManager).GetField("_debugDestruction", BindingFlags.Instance | BindingFlags.NonPublic);
        bool previousVfx = effects.enableRuntimeVfx;
        bool previousVfxLog = (bool)vfxFlag.GetValue(effects);
        bool previousDestroyLog = (bool)destroyFlag.GetValue(destruction);
        int logs = 0;
        Application.LogCallback capture = (message, trace, type) =>
        {
            if (type == LogType.Log && (message.Contains("[Destruction VFX]") || message.Contains("durability reached 0")))
            {
                logs++;
            }
        };
        Application.logMessageReceived += capture;
        try
        {
            Check(report, "Siege auto-created diagnostics default off", !previousVfxLog && !previousDestroyLog);
            effects.enableRuntimeVfx = false;
            Block block = session.Fleet.Base.Core;
            VisualEffectsManager.TryPlayBlockExplosion(block);
            Check(report, "disabled diagnostics emit no skipped-effect log", logs == 0);
            vfxFlag.SetValue(effects, true);
            VisualEffectsManager.TryPlayBlockExplosion(block);
            Check(report, "VFX diagnostics remain opt-in", logs == 1);
            vfxFlag.SetValue(effects, false);
            const int count = 64;
            bool[] order =
            {
                false,
                true,
                true,
                false,
                false,
                true,
                true,
                false
            };
            foreach (bool enabled in order)
            {
                vfxFlag.SetValue(effects, enabled);
                int before = logs;
                long start = Stopwatch.GetTimestamp();
                for (int index = 0; index < count; index++)
                {
                    VisualEffectsManager.TryPlayBlockExplosion(block);
                }

                double elapsed = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                report.samples.Add(new Sample { path = "VFX disabled branch", enabled = enabled, calls = count, milliseconds = elapsed });
                Check(report, "VFX ABBA exact log count", logs - before == (enabled ? count : 0));
            }

            vfxFlag.SetValue(effects, false);
            foreach (bool enabled in order)
            {
                var objects = new GameObject[count];
                for (int index = 0; index < count; index++)
                {
                    objects[index] = new GameObject("Destruction diagnostic fixture");
                    objects[index].transform.position = Vector3.one * 10000f;
                }

                destroyFlag.SetValue(destruction, enabled);
                int before = logs;
                long start = Stopwatch.GetTimestamp();
                foreach (GameObject target in objects)
                {
                    destruction.DestroyGameObject(target);
                }

                double elapsed = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                report.samples.Add(new Sample { path = "Bare object destruction; effects disabled", enabled = enabled, calls = count, milliseconds = elapsed });
                Check(report, "destruction ABBA exact log count", logs - before == (enabled ? count : 0));
            }

            Check(report, "fixture preserves real building", session.Fleet.Base.IsAlive && session.Match.Wave == 0);
            report.passed = true;
        }
        finally
        {
            effects.enableRuntimeVfx = previousVfx;
            vfxFlag.SetValue(effects, previousVfxLog);
            destroyFlag.SetValue(destruction, previousDestroyLog);
            Application.logMessageReceived -= capture;
        }

        const string path = "Temp/Siege/destruction-log-checks.json";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    private static void Check(Report report, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        report.checks.Add(name);
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "Synchronous Editor Play ABBA x2, 64 requests/batch, real production methods with effects disabled. Bare destruction excludes block health/topology and allocation of fixture objects. Logging callback and Editor stack traces included. Not Player, natural combat or end-to-end speedup.";
        public bool passed;
        public List<string> checks = new List<string>();
        public List<Sample> samples = new List<Sample>();
    }

    [Serializable]
    private struct Sample
    {
        public string path;
        public bool enabled;
        public int calls;
        public double milliseconds;
    }
}

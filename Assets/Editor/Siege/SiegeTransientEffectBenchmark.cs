// 职责：测量实际短时光束/点光源的创建路径；远处隔离夹具不代表自然战斗FPS。
// 入口：Run(label)；每批同步到期，清理在计时外，不推进物理或保存场景。
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeTransientEffectBenchmark
{
    public static string Run(string label)
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0)
        {
            throw new InvalidOperationException("Requires fresh Siege deployment Play.");
        }

        var report = new Report
        {
            label = label
        };
        Vector3 origin = new Vector3(10000f, 10000f, 10000f);
        Type transient = typeof(VisualEffectsManager).Assembly.GetType("TransientEffects");
        MethodInfo line = transient.GetMethod("CreateLineStreak");
        MethodInfo light = transient.GetMethod("CreateLightFlash");
        foreach (string kind in new[]
        {
            "beam",
            "light"
        }

        )
        {
            for (int batch = -1; batch < 6; batch++)
            {
                long start = Stopwatch.GetTimestamp();
                for (int index = 0; index < 16; index++)
                {
                    if (kind == "beam")
                    {
                        line.Invoke(null, new object[] { origin, origin + Vector3.forward * 3f, Color.cyan, 0.1f, 0.03f });
                    }
                    else
                    {
                        light.Invoke(null, new object[] { origin, Color.red, 3f, 4f, 0.1f });
                    }
                }

                double milliseconds = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
                if (batch >= 0)
                {
                    report.samples.Add(new Sample { kind = kind, batch = batch, calls = 16, milliseconds = milliseconds });
                }

                // 使用实际到期回调；仅触及本夹具坐标的活动对象，生产池的闲置对象保留供热复用。
                foreach (MonoBehaviour fade in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).Where(item => IsOwned(item, origin)))
                {
                    Type type = fade.GetType();
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    type.GetField("elapsed", flags).SetValue(fade, 1f);
                    type.GetMethod("Update", flags).Invoke(fade, null);
                    if (fade != null && fade.gameObject.activeSelf)
                    {
                        Object.DestroyImmediate(fade.gameObject);
                    }
                }
            }
        }

        string path = "Temp/Siege/transient-" + label + ".json";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    private static bool IsOwned(MonoBehaviour item, Vector3 origin)
    {
        if (item is LightFade)
        {
            return (item.transform.position - origin).sqrMagnitude < 100f;
        }

        if (item is BeamFade)
        {
            StylizedBeamEffect beam = item.GetComponent<StylizedBeamEffect>();
            Vector3 start = (Vector3)typeof(StylizedBeamEffect).GetField("_start", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(beam);
            return (start - origin).sqrMagnitude < 100f;
        }

        return false;
    }

    [Serializable]
    private sealed class Report
    {
        public string methodology = "Editor Play; actual GPU beam prefab and point light, remote coordinates, no physics steps. One warmup then six batches of16. Stopwatch includes reflection invocation. Expiration/query/cleanup excluded. Not FPS or GPU time.";
        public string label;
        public List<Sample> samples = new List<Sample>();
    }

    [Serializable]
    private sealed class Sample
    {
        public string kind;
        public int batch;
        public int calls;
        public double milliseconds;
    }
}

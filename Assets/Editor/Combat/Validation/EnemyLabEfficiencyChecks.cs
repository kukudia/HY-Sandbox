// 职责：用相同蓝图和路线顺序对比导航效率，保留逐轮记录供耗时、停顿和规划分析。
// 入口：Begin(label)；仅试验场Play Mode，固定物理步长，临时加速并在结束时恢复。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

public static class EnemyLabEfficiencyChecks
{
    public static Task<string> Pending
    {
        get; private set;
    }

    [Serializable]
    private sealed class Report
    {
        public string label;
        public List<Run> runs = new List<Run>();
    }

    [Serializable]
    private sealed class Run
    {
        public int layout;
        public bool north;
        public string directory;
        public string result;
    }

    public static void Begin(string label)
    {
        if (Pending != null && !Pending.IsCompleted)
        {
            throw new InvalidOperationException("Efficiency run already active.");
        }

        Pending = RunCourses(label);
    }

    private static async Task<string> RunCourses(string label)
    {
        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        if (!Application.isPlaying || lab == null)
        {
            throw new InvalidOperationException("Requires navigation lab Play Mode.");
        }

        float previousScale = Time.timeScale;
        bool previousBackground = Application.runInBackground;
        var report = new Report
        {
            label = label
        };
        string output = Path.Combine("Temp", "EnemyAvoidanceLab", "efficiency-" + label + ".json");
        int[] layouts =
        {
            0,
            4,
            6,
            7,
            11,
            11
        };
        try
        {
            Application.runInBackground = true;
            lab.SetPaused(false);
            Time.timeScale = 3f;
            lab.SelectBlueprint(0);
            if (!lab.DeploySelected())
            {
                throw new InvalidOperationException("Finch deployment failed.");
            }

            for (int i = 0; i < layouts.Length; i++)
            {
                lab.SelectLayout(layouts[i]);
                if (i == layouts.Length - 1)
                {
                    lab.SelectLocation(2, true);
                    lab.SelectLocation(3, false);
                }

                typeof(EnemyAvoidanceLab).GetField("timeout", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(lab, 150f);
                string directory = lab.RecordingDirectory;
                double deadline = Time.realtimeSinceStartupAsDouble + 180;
                while (Application.isPlaying && lab != null && lab.Running && Time.realtimeSinceStartupAsDouble < deadline)
                {
                    await Task.Delay(200);
                }

                if (!Application.isPlaying || lab == null)
                {
                    throw new InvalidOperationException("Play Mode ended before comparison finished.");
                }

                if (lab.Running)
                {
                    lab.StopTrial();
                    throw new TimeoutException("Simulation did not complete in wall-clock budget.");
                }

                report.runs.Add(new Run { layout = layouts[i], north = i == layouts.Length - 1, directory = directory, result = File.ReadAllText(Path.Combine(directory, "result.json")) });
                Directory.CreateDirectory(Path.GetDirectoryName(output));
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }

            return output;
        }
        finally
        {
            Time.timeScale = previousScale;
            Application.runInBackground = previousBackground;
        }
    }
}

// 职责：验证竞技场显式追踪配置、共享轨迹环形边界及真实战斗中的绑定/换轮清理。
// 入口：ValidateSaved()/Begin()；Play检查保留开火并采集导航，末段通过正式淘汰流程验证重开。
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SkirmishNavigationChecks
{
    public static Task<string> Pending
    {
        get; private set;
    }

    public static string ValidateSaved()
    {
        var checks = new List<string>();
        SkirmishNavigationDebug debug = Object.FindAnyObjectByType<SkirmishNavigationDebug>();
        Check(checks, "Five scene-authored tracking slots", debug != null && debug.TrackedCount == 5);
        for (int i = 0; i < 5; i++)
        {
            NavigationTrack track = debug.TrackAt(i);
            var data = new SerializedObject(track);
            Check(
                checks,
                "Track and both editable renderers saved " + i,
                track != null && data.FindProperty("traceLine").objectReferenceValue != null && data.FindProperty("routeLine").objectReferenceValue != null);
            Check(
                checks,
                "Track uses fixed eight-metre speed scale " + i,
                data.FindProperty("traceFastSpeed").floatValue == 8f);
        }

        var fixture = new GameObject("Navigation trace fixture");
        LineRenderer line = fixture.AddComponent<LineRenderer>();
        line.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Combat/NavigationDebugTrace.mat");
        Material original = line.sharedMaterial;
        var trace = new NavigationFlightTrace(line, 4);
        try
        {
            for (int i = 0; i < 7; i++)
            {
                trace.Sample(Vector3.right * i, i, 0.1f, 8f);
            }

            Check(
                checks,
                "Bounded ring keeps four most recent points in chronological order",
                trace.Count == 4 && line.positionCount == 4 && line.GetPosition(0).x == 3f && line.GetPosition(3).x == 6f);
            Check(
                checks,
                "Speed texture belongs to instance material",
                line.sharedMaterial != original && line.sharedMaterial.GetTexture("_BaseMap") != null);
            trace.Clear();
            Check(checks, "Round reset removes all historical vertices", trace.Count == 0 && line.positionCount == 0);
        }
        finally
        {
            trace.Dispose();
            Check(checks, "Disposal restores authored material", line.sharedMaterial == original);
            Object.DestroyImmediate(fixture);
        }

        return Save("saved-checks.txt", checks);
    }

    public static void Begin()
    {
        Pending = Run();
    }

    private static async Task<string> Run()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Requires arena Play Mode.");
        }

        var checks = new List<string>();
        SkirmishNavigationDebug debug = Object.FindAnyObjectByType<SkirmishNavigationDebug>();
        SkirmishMatch match = Object.FindAnyObjectByType<SkirmishMatch>();
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        try
        {
            await Task.Delay(8000);
            int sampled = 0;
            for (int i = 0; i < 5; i++)
            {
                NavigationTrack track = debug.TrackAt(i);
                if (track.TracePoints > 2)
                {
                    sampled++;
                }

                Check(checks, "Enemy slot bound to correct label " + i, track.Label == match.NameAt(i + 5));
                Check(
                    checks,
                    "Enemy ownership remains red " + i,
                    track.Owner == null || track.Owner.faction == UnitFaction.Enemy);
            }

            Check(checks, "Real battle creates multi-point enemy speed traces", sampled > 0);
            debug.SelectEnemy(2);
            await Task.Delay(350);
            Check(
                checks,
                "Selected enemy changes without losing other tracks",
                debug.Selected == debug.TrackAt(2) && debug.TrackAt(0).TracePoints > 0);
            Check(
                checks,
                "Debug text contains tactical target, path, prediction and efficiency data",
                debug.Report.Contains("Target:") && debug.Report.Contains("Stopped") && debug.Report.Contains("A*:") && debug.Report.Contains("TTC") && debug.Report.Contains("Travel:"));
            Save("live-report.txt", new List<string> { debug.Report });
            debug.Selected.ClearHistory();
            Check(
                checks,
                "Clear selected history does not clear other slots",
                debug.Selected.TracePoints == 0 && debug.TrackAt(0).TracePoints > 0);
            int oldRound = match.Round;
            for (int i = 5; i < 10; i++)
            {
                Cockpit cockpit = match.CockpitAt(i);
                if (cockpit != null)
                {
                    cockpit.GetComponent<Durability>().currentDurability = 0f;
                }
            }

            float deadline = Time.realtimeSinceStartup + 15f;
            while (match.Round == oldRound && Time.realtimeSinceStartup < deadline)
            {
                await Task.Delay(250);
            }

            await Task.Delay(350);
            Check(checks, "Match restarts through real settlement and cleanup", match.Round > oldRound);
            Check(
                checks,
                "Authored tracking service survives round cleanup",
                debug != null && debug.TrackedCount == 5);
            Check(
                checks,
                "New round rebinds and clears old lifetime metrics",
                debug.TrackAt(0).Owner != null && debug.TrackAt(0).Elapsed < 5f);
            Check(
                checks,
                "No per-round tracking object growth",
                Object.FindObjectsByType<NavigationTrack>().Length == 5);
            return Save("play-checks.txt", checks);
        }
        finally
        {
            Application.runInBackground = background;
        }
    }

    private static string Save(string name, List<string> checks)
    {
        Directory.CreateDirectory("Temp/ArenaNavigation");
        string report = string.Join("\n", checks);
        File.WriteAllText("Temp/ArenaNavigation/" + name, report);
        return report;
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

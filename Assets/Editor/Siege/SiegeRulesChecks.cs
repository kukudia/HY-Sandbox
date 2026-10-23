// 职责：验证阵地战阶段、波次预算和经济的边界；无需进入Play或读写玩家存档。
// 入口：Run() / 菜单 HY-Sandbox/Validation/Siege Rules。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SiegeRulesChecks
{
    [MenuItem("HY-Sandbox/Validation/Siege Rules")]
    public static void RunFromMenu()
    {
        Debug.Log(Run());
    }

    public static string Run()
    {
        var checks = new List<string>();
        var match = new SiegeMatch(
            100,
            2,
            2,
            5,
            10,
            30);
        Check(
            checks,
            "deployment permits affordable refit only",
            match.CanRefit && match.TrySpend(60) && match.Coins == 40 && !match.TrySpend(41) && !match.TrySpend(-1));
        Check(
            checks,
            "first wave starts exactly once",
            match.BeginNextWave() && !match.BeginNextWave() && match.PendingAttackers == 2 && !match.TrySpend(0));
        Check(
            checks,
            "active cap preserves unspawned tickets",
            match.RegisterAttacker("one", 1) && !match.RegisterAttacker("two", 1) && match.PendingAttackers == 1);
        Check(
            checks,
            "duplicate and invalid spawn ids cannot consume tickets",
            !match.RegisterAttacker("one", 2) && !match.RegisterAttacker(" ", 2) && match.PendingAttackers == 1);
        Check(
            checks,
            "one kill rewards once without early wave completion",
            match.ResolveAttacker("one", true) && !match.ResolveAttacker("one", true) && match.Phase == SiegePhase.Battle && match.Coins == 50);
        Check(
            checks,
            "failed spawn leaves ticket available for retry",
            match.CanSpawn(1) && match.PendingAttackers == 1);
        Check(
            checks,
            "disappeared unit clears wave without kill reward",
            match.RegisterAttacker("two", 1) && match.ResolveAttacker("two", false) && match.Phase == SiegePhase.Intermission && match.Coins == 80 && match.DefeatedAttackers == 1);
        Check(
            checks,
            "unknown deaths cannot manufacture income",
            !match.ResolveAttacker("unknown", true) && match.Coins == 80);
        Check(
            checks,
            "wave growth and repair gate",
            match.TrySpend(80) && match.Coins == 0 && match.BeginNextWave() && match.PendingAttackers == 4);
        for (int index = 0; index < 4; index++)
        {
            string id = "wave2-" + index;
            Check(checks, "wave2 ticket " + index, match.RegisterAttacker(id, 1) && match.ResolveAttacker(id, true));
        }

        Check(
            checks,
            "completion reward is paid exactly once",
            match.Coins == 70 && !match.ResolveAttacker("wave2-3", true) && match.Coins == 70);
        Check(checks, "unlimited waves respect size cap", match.BeginNextWave() && match.PendingAttackers == 5);
        match.LoseBase();
        Check(
            checks,
            "base loss cancels pending spawns and all purchases",
            match.Phase == SiegePhase.Defeat && !match.BeginNextWave() && !match.CanSpawn(100) && !match.TrySpend(0) && match.PendingAttackers == 0);
        Check(
            checks,
            "late deaths cannot change defeated match economy",
            !match.ResolveAttacker("wave3", true) && match.Coins == 70);
        var large = new SiegeMatch(
            0,
            int.MaxValue - 1,
            int.MaxValue,
            int.MaxValue,
            0,
            0);
        Check(
            checks,
            "large wave size remains positive",
            large.BeginNextWave() && large.PendingAttackers == int.MaxValue - 1);
        bool rejected = false;
        try
        {
            new SiegeMatch(
                -1,
                0,
                -1,
                0,
                -1,
                -1);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }

        Check(checks, "invalid economy configuration is rejected", rejected);
        string report = JsonUtility.ToJson(new Report { checks = checks }, true);
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/rules.json", report);
        return report;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Siege rule failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

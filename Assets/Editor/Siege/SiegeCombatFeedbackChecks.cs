// 职责：验证精英波次奖励倍数与战斗中连击加成的纯规则行为，不依赖场景、物理或敌舰生成。
// 入口：Run()；只构造SiegeMatch并登记/结算虚构敌机，不写资产、不改设置。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SiegeCombatFeedbackChecks
{
    private sealed class Report
    {
        public List<string> checks = new List<string>();
    }

    public static string Run()
    {
        var checks = new List<string>();
        // 非精英基线：第1波不加成，击毁与清波奖励保持原值。
        var normal = new SiegeMatch(
            0,
            2,
            0,
            8,
            100,
            200,
            2,
            2);
        Check(
            checks,
            "first wave is not elite",
            normal.BeginNextWave() && !normal.IsEliteWave && normal.KillReward == 100);
        normal.RegisterAttacker("n1", 8);
        normal.RegisterAttacker("n2", 8);
        Check(
            checks,
            "normal kill and clear rewards stay unchanged",
            normal.ResolveAttacker("n1", true) && normal.Coins == 100);
        Check(
            checks,
            "normal wave clear adds the base reward",
            normal.ResolveAttacker("n2", true) && normal.Phase == SiegePhase.Intermission && normal.Coins == 400);
        // 第二波为精英：击毁与清波都按倍数放大，且只放大一次。
        Check(
            checks,
            "every second wave is elite",
            normal.BeginNextWave() && normal.Wave == 2 && normal.IsEliteWave);
        Check(checks, "elite wave doubles the displayed kill reward", normal.KillReward == 200);
        normal.RegisterAttacker("e1", 8);
        normal.RegisterAttacker("e2", 8);
        Check(
            checks,
            "elite kill pays the doubled reward once",
            normal.ResolveAttacker("e1", true) && normal.Coins == 600);
        Check(
            checks,
            "elite wave clear pays the doubled reward once",
            normal.ResolveAttacker("e2", true) && normal.Coins == 1200 && normal.Phase == SiegePhase.Intermission);
        // 连击加成只在战斗与波次结算瞬间生效，部署阶段与失败后拒绝。
        Check(
            checks,
            "intermission accepts the wave-final combo bonus",
            normal.GrantBonus(60) && normal.Coins == 1260);
        normal.BeginNextWave();
        Check(checks, "battle accepts combo bonuses", normal.GrantBonus(120) && normal.Coins == 1380);
        Check(
            checks,
            "zero or negative bonuses are rejected",
            !normal.GrantBonus(0) && !normal.GrantBonus(-40) && normal.Coins == 1380);
        var deployOnly = new SiegeMatch(
            0,
            2,
            0,
            8,
            100,
            200,
            0,
            2);
        Check(checks, "deployment phase rejects bonuses", !deployOnly.GrantBonus(60) && deployOnly.Coins == 0);
        deployOnly.LoseBase();
        Check(checks, "defeat rejects bonuses", !deployOnly.GrantBonus(60) && deployOnly.Coins == 0);
        // 关闭精英间隔时保持纯线性波次。
        var linear = new SiegeMatch(
            0,
            2,
            0,
            8,
            100,
            200,
            0,
            2);
        linear.BeginNextWave();
        linear.RegisterAttacker("l1", 8);
        linear.RegisterAttacker("l2", 8);
        linear.ResolveAttacker("l1", true);
        linear.ResolveAttacker("l2", true);
        linear.BeginNextWave();
        Check(
            checks,
            "disabled elite interval keeps every wave normal",
            !linear.IsEliteWave && linear.KillReward == 100);
        // 蜂群波：数量翻倍、与精英波重叠时精英优先；关闭间隔时保持线性。
        var swarm = new SiegeMatch(
            0,
            2,
            1,
            40,
            100,
            200,
            4,
            2,
            2,
            2);
        swarm.BeginNextWave();
        Check(checks, "first wave is not a swarm", !swarm.IsSwarmWave && swarm.PendingAttackers == 2);
        swarm.RegisterAttacker("s1", 40);
        swarm.RegisterAttacker("s2", 40);
        swarm.ResolveAttacker("s1", false);
        swarm.ResolveAttacker("s2", false);
        Check(
            checks,
            "swarm wave doubles the incoming count",
            swarm.BeginNextWave() && swarm.IsSwarmWave && swarm.PendingAttackers == 6);
        var both = new SiegeMatch(
            0,
            2,
            1,
            40,
            100,
            200,
            2,
            2,
            2,
            2);
        both.BeginNextWave();
        both.RegisterAttacker("b1", 40);
        both.RegisterAttacker("b2", 40);
        both.ResolveAttacker("b1", false);
        both.ResolveAttacker("b2", false);
        Check(
            checks,
            "elite interval wins over swarm on the same wave",
            both.BeginNextWave() && both.Wave == 2 && both.IsEliteWave && !both.IsSwarmWave && both.PendingAttackers == 3);
        var noSwarm = new SiegeMatch(
            0,
            2,
            1,
            40,
            100,
            200,
            0,
            2,
            0,
            2);
        noSwarm.BeginNextWave();
        Check(
            checks,
            "disabled swarm interval keeps linear counts",
            !noSwarm.IsSwarmWave && noSwarm.PendingAttackers == 2);
        // 整备预览要提前把蜂群/精英的倍率算进去，否则玩家按低估的数量备舰。
        var preview = new SiegeMatch(
            0,
            2,
            1,
            40,
            100,
            200,
            4,
            2,
            2,
            2);
        preview.BeginNextWave();
        preview.RegisterAttacker("p1", 40);
        preview.RegisterAttacker("p2", 40);
        preview.ResolveAttacker("p1", false);
        preview.ResolveAttacker("p2", false);
        Check(
            checks,
            "refit preview scales and flags the upcoming swarm",
            preview.NextWaveIsSwarm && !preview.NextWaveIsElite && preview.NextWaveSize == 6);
        var elitePreview = new SiegeMatch(
            0,
            2,
            1,
            40,
            100,
            200,
            2,
            2,
            2,
            2);
        elitePreview.BeginNextWave();
        elitePreview.RegisterAttacker("q1", 40);
        elitePreview.RegisterAttacker("q2", 40);
        elitePreview.ResolveAttacker("q1", false);
        elitePreview.ResolveAttacker("q2", false);
        Check(
            checks,
            "refit preview flags the upcoming elite without swarm scaling",
            elitePreview.NextWaveIsElite && !elitePreview.NextWaveIsSwarm && elitePreview.NextWaveSize == 3);
        int previousBest = SiegeProgress.BestWave;
        int previousKills = SiegeProgress.TotalKills;
        int previousRuns = SiegeProgress.TotalRuns;
        bool raised = SiegeProgress.TryRecordResult(previousBest + 1, 3);
        Check(checks, "record run raises the best wave", raised && SiegeProgress.BestWave == previousBest + 1);
        Check(
            checks,
            "record run accumulates kills and runs",
            SiegeProgress.TotalKills == previousKills + 3 && SiegeProgress.TotalRuns == previousRuns + 1);
        bool lowered = SiegeProgress.TryRecordResult(previousBest, 1);
        Check(
            checks,
            "shorter run keeps the record and still counts kills",
            !lowered && SiegeProgress.BestWave == previousBest + 1 && SiegeProgress.TotalKills == previousKills + 4);
        SiegeProgress.RestoreForProbe(previousBest, previousKills, previousRuns);
        Check(
            checks,
            "probe restores the original record",
            SiegeProgress.BestWave == previousBest && SiegeProgress.TotalKills == previousKills && SiegeProgress.TotalRuns == previousRuns);
        var report = new Report
        {
            checks = checks
        };
        Directory.CreateDirectory("Temp/Siege");
        string path = "Temp/Siege/combat-feedback-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Siege combat feedback check failed: " + name);
        }

        checks.Add(name);
    }
}

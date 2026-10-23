// 职责：在真实Siege Play中校验刚体分类、闲置池过滤与显式采样的计时开关恢复，避免诊断污染玩法。
// 入口：Run()；分类用真实基地/舰船，慢帧边界用合成delta，不是性能或物理步证据。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegePlayerSurveyChecks
{
    public static string Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0 || session.Fleet.Driven != null)
        {
            throw new InvalidOperationException("Requires fresh Siege Play without deployed ships.");
        }

        var checks = new List<string>();
        var roots = new List<GameObject>();
        bool previousMetrics = SimulationWorkMetrics.Enabled;
        SiegePlayerSurvey survey = null;
        try
        {
            Check(checks, "deploy actual ship", session.TryDeploy(0, out _));
            Rigidbody building = session.Fleet.Base.Unit.GetComponent<Rigidbody>();
            Rigidbody ship = session.Fleet.Driven.Unit.GetComponent<Rigidbody>();
            Rigidbody fragment = CreateBody(roots, "fragment");
            fragment.gameObject.AddComponent<ControlUnit>();
            Rigidbody projectile = CreateBody(roots, "projectile");
            projectile.gameObject.AddComponent<TurretProjectile>();
            Rigidbody other = CreateBody(roots, "unmarked debris");
            other.isKinematic = true;
            Rigidbody sleeping = CreateBody(roots, "sleeping");
            sleeping.Sleep();
            Rigidbody idle = CreateBody(roots, "idle projectile");
            idle.gameObject.AddComponent<TurretProjectile>();
            idle.gameObject.SetActive(false);
            SiegePlayerSurvey.BodyCounts kinds = SiegePlayerSurvey.Classify(new[] { building, ship, fragment, projectile, other, sleeping, idle, null });
            Check(checks, "inactive pooled body and null excluded", kinds.total == 6);
            Check(checks, "real building and cockpit ship classified", kinds.buildings == 1 && kinds.ships == 1);
            Check(checks, "cockpitless unit is fragment", kinds.fragments == 1);
            Check(checks, "active projectile distinct from other bodies", kinds.projectiles == 1 && kinds.other == 2);
            Check(
                checks,
                "root categories partition all bodies",
                kinds.buildings + kinds.ships + kinds.fragments + kinds.projectiles + kinds.other == kinds.total);
            Check(
                checks,
                "physical states partition all bodies and sleeping is observed",
                kinds.kinematic + kinds.awakeDynamic + kinds.sleepingDynamic == kinds.total && kinds.sleepingDynamic >= 1);
            SimulationWorkMetrics.Enabled = false;
            survey = new SiegePlayerSurvey(Time.realtimeSinceStartup, true);
            Check(checks, "profiling explicitly enables metrics", SimulationWorkMetrics.Enabled);
            Check(
                checks,
                "engine capabilities reported without assuming availability",
                survey.Result.engineCounters.Count == 9);
            using (var counters = new SiegeEngineCounters())
            {
                counters.Read();
                SiegeEngineCounters.Sample empty = counters.Read();
                Check(
                    checks,
                    "managed GC buffers drained rather than reusing stale values",
                    empty.gcCollectNs == -1 && empty.gcAllocatedBytes == -1);
            }

            using (SimulationWorkMetrics.Measure(SimulationWorkMetrics.Stage.Connectivity, 7))
            {
                survey.Observe(session, 10f);
            }

            // 合成输入只检验固定上限、阈值与排序；不冒充自然战斗慢帧。
            for (int index = 0; index < 40; index++)
            {
                survey.Observe(session, 40f + index);
            }

            Check(
                checks,
                "slow frames bounded and ordered",
                survey.Result.slowestFrames.Count == 32 && survey.Result.slowestFrames[0].milliseconds == 79f && survey.Result.over33Ms == 40 && survey.Result.over50Ms == 29);
            Check(
                checks,
                "census captures real objects and own cost",
                survey.CensusSamples.Count == 1 && survey.CensusSamples[0].bodies == survey.CensusSamples[0].bodyKinds.total && survey.CensusSamples[0].surveyMs > 0.0);
            survey.Dispose();
            Check(
                checks,
                "dispose restores disabled state and preserves measured work",
                !SimulationWorkMetrics.Enabled && survey.Result.work.connectivity.calls == 1 && survey.Result.work.connectivity.inputBlocks == 7);
            survey.Dispose();
            Check(
                checks,
                "dispose is idempotent",
                !SimulationWorkMetrics.Enabled && survey.Result.work.connectivity.calls == 1);
            SimulationWorkMetrics.Enabled = true;
            using (var passive = new SiegePlayerSurvey(Time.realtimeSinceStartup, false))
            {
                Check(
                    checks,
                    "passive survey leaves external metrics enabled",
                    SimulationWorkMetrics.Enabled && !passive.Result.workMetricsEnabled);
            }

            Check(checks, "passive dispose preserves external metrics enabled", SimulationWorkMetrics.Enabled);
        }
        finally
        {
            survey?.Dispose();
            SimulationWorkMetrics.Enabled = previousMetrics;
            foreach (GameObject root in roots)
            {
                Object.DestroyImmediate(root);
            }

            session.End();
        }

        const string path = "Temp/Siege/player-survey-checks.json";
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(new Report { passed = true, checks = checks }, true));
        return path;
    }

    private static Rigidbody CreateBody(List<GameObject> roots, string name)
    {
        var root = new GameObject("Survey fixture " + name);
        roots.Add(root);
        root.transform.position = Vector3.one * 10000f;
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.useGravity = false;
        return body;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public List<string> checks;
    }
}

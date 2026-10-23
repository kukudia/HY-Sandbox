// 职责：用真实基地与敌舰验证任务目标远程追击、普通索敌边界及阵营/启用/守卫约束。
// 入口：Run()；全新Siege Play的同步隔离夹具，不推进物理步，不把该检查当作AI实飞验证。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegePriorityTargetChecks
{
    public static string Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0)
        {
            throw new InvalidOperationException("Requires fresh Siege Play before any wave.");
        }

        var report = new Report();
        ControlUnit raider = null;
        ControlUnit target = session.Fleet.Base.Unit;
        bool targetEnabled = target.enabled;
        bool targetWasRegistered = PlayManager.instance.GetControlUnits().Contains(target);
        try
        {
            if (!session.BeginNextWave())
            {
                throw new InvalidOperationException("Could not enable the actual building combat target.");
            }

            BlockDataList blueprint = SiegeFleet.PrepareBlueprint(session.Settings.Ships[0].Blueprint, out _);
            raider = EnemyBlueprintFactory.Spawn(
                blueprint,
                "Owned priority-target fixture",
                target.transform.position + Vector3.right * 300f,
                Quaternion.identity);
            if (raider == null)
            {
                throw new InvalidOperationException("Could not create the raider fixture.");
            }

            EnemyController ai = raider.GetComponent<EnemyController>();
            // 普通索敌也必须看到真实候选，避免空登记表造成无效通过。
            PlayManager.instance.RegisterControlUnit(target);
            MethodInfo find = typeof(EnemyController).GetMethod("FindNearestOpponent", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo move = typeof(EnemyController).GetMethod("CalculateDesiredMovement", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo current = typeof(EnemyController).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic);
            Physics.SyncTransforms();
            Check(
                report,
                "ordinary acquisition stays bounded without an assigned mission",
                find.Invoke(ai, null) == null);
            ai.SetPriorityTarget(target);
            Check(
                report,
                "assigned operational building is acquired outside detection range",
                find.Invoke(ai, null) as ControlUnit == target);
            current.SetValue(ai, target);
            Vector3 delta = target.transform.position - raider.transform.position;
            delta.y = 0f;
            Vector3 input = (Vector3)move.Invoke(ai, new object[] { delta });
            Check(
                report,
                "assigned far building produces approach intent",
                Vector3.Dot(input, delta.normalized) > 0.5f);
            // 与实际第1艘同种子的位置：围绕核心生成，距建筑根略超过150m。
            var random = new System.Random(session.Settings.SpawnSeed);
            random.Next(session.Settings.Ships.Length);
            double angle = random.NextDouble() * Math.PI * 2.0;
            Vector3 offset = new Vector3((float)Math.Cos(angle), 0f, (float)Math.Sin(angle)) * session.Settings.AttackRadius;
            raider.transform.position = session.Fleet.Base.Core.transform.position + offset + Vector3.up * 10f;
            report.generatedRootDistance = Vector3.Distance(raider.transform.position, target.transform.position);
            Check(
                report,
                "actual core-centered spawn retains its assigned building target",
                find.Invoke(ai, null) as ControlUnit == target);
            raider.transform.position = target.transform.position + Vector3.right * 40f;
            Check(
                report,
                "near assigned building retains normal acquisition",
                find.Invoke(ai, null) as ControlUnit == target);
            ai.SetPriorityTarget(null);
            Check(
                report,
                "ordinary near opponent is acquired from the populated registry",
                find.Invoke(ai, null) as ControlUnit == target);
            ai.SetPriorityTarget(target);
            target.enabled = false;
            Check(report, "disabled mission target is rejected", find.Invoke(ai, null) == null);
            target.enabled = targetEnabled;
            raider.faction = target.faction;
            Check(report, "friendly mission target is rejected", find.Invoke(ai, null) == null);
            raider.faction = UnitFaction.Enemy;
            ai.SetGuardAnchor(raider.transform, 10f);
            Check(report, "guard territory still constrains explicit priority", find.Invoke(ai, null) == null);
            ai.SetGuardAnchor(null, 10f);
            raider.transform.position = target.transform.position + Vector3.right * 300f;
            ai.SetPriorityTarget(null);
            Check(report, "clearing mission restores bounded ordinary acquisition", find.Invoke(ai, null) == null);
        }
        finally
        {
            target.enabled = targetEnabled;
            if (!targetWasRegistered)
            {
                PlayManager.instance.UnregisterControlUnit(target);
            }

            if (raider != null)
            {
                PlayManager.instance.UnregisterControlUnit(raider);
                Object.DestroyImmediate(raider.gameObject);
            }

            session.End();
        }

        report.passed = report.failures == 0;
        string json = JsonUtility.ToJson(report, true);
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/priority-target-checks.json", json);
        return json;
    }

    private static void Check(Report report, string name, bool passed)
    {
        report.checks.Add((passed ? "PASS " : "FAIL ") + name);
        if (!passed)
        {
            report.failures++;
        }
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public int failures;
        public float generatedRootDistance;
        public List<string> checks = new List<string>();
    }
}

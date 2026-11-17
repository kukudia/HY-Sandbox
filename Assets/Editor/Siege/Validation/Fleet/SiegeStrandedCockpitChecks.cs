// 职责：用真实受损舰和生产波次导演验证残舱退场、恢复边界及零击杀奖励。
// 入口：RunLive()；新鲜Build Play的同步规则夹具，时间参数推进不等同自然12秒实飞。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeStrandedCockpitChecks
{
    public static string RunLive()
    {
        if (!EditorApplication.isPlaying || PlayManager.instance == null)
        {
            throw new InvalidOperationException("Requires Build Play Mode.");
        }

        var checks = new List<string>();
        SiegeSettings settings = Object.Instantiate(AssetDatabase.LoadAssetAtPath<SiegeSettings>("Assets/Siege/DefaultSiege.asset"));
        ControlUnit unit = null;
        SiegeWaveDirector director = null;
        bool previousPlay = PlayManager.instance.playMode;
        try
        {
            var data = new SerializedObject(settings);
            data.FindProperty("waveSpawnDelay").floatValue = 0f;
            data.FindProperty("strandedCockpitSeconds").floatValue = 12f;
            data.ApplyModifiedPropertiesWithoutUndo();
            TextAsset source = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
            BlockDataList blueprint = JsonUtility.FromJson<BlockDataList>(source.text);
            unit = EnemyBlueprintFactory.Spawn(blueprint, "Stranded regression", new Vector3(50, 50, 50), Quaternion.identity);
            PlayManager.instance.playMode = true;
            Cockpit cockpit = unit.cockpit;
            var policy = new SiegeStrandedCockpit(true);
            Check(
                checks,
                "Intact hull never retired",
                !policy.ShouldRetire(cockpit, 0f, 12f) && !policy.ShouldRetire(cockpit, 100f, 12f));
            foreach (Block block in unit.GetComponentsInChildren<Block>())
            {
                if (block.gameObject != cockpit.gameObject)
                {
                    Object.DestroyImmediate(block.gameObject);
                }
            }

            unit.RefreshChildren();
            Check(
                checks,
                "Fixture has only physical cockpit and no engines",
                unit.GetComponentsInChildren<Block>().Length == 1 && unit.Thrusters.Count == 0 && !unit.HasCombatCapability);
            Check(
                checks,
                "Residual cockpit receives grace period",
                !policy.ShouldRetire(cockpit, 101f, 12f) && !policy.ShouldRetire(cockpit, 112f, 12f));
            Check(checks, "Immobile residual retires after timeout", policy.ShouldRetire(cockpit, 113f, 12f));
            cockpit.transform.position += Vector3.right * 3f;
            Check(checks, "Meaningful movement resets timeout", !policy.ShouldRetire(cockpit, 114f, 12f));
            Check(
                checks,
                "Fresh window must fully expire",
                !policy.ShouldRetire(cockpit, 125f, 12f) && policy.ShouldRetire(cockpit, 126f, 12f));
            var tinyBlueprint = new SiegeStrandedCockpit(false);
            Check(
                checks,
                "Authored cockpit-only designs are preserved",
                !tinyBlueprint.ShouldRetire(cockpit, 0, 12) && !tinyBlueprint.ShouldRetire(cockpit, 100, 12));
            Check(checks, "Zero disables automatic retirement", !policy.ShouldRetire(cockpit, 200, 0));
            var match = new SiegeMatch(
                100,
                1,
                0,
                1,
                120,
                0);
            match.BeginNextWave();
            match.RegisterAttacker("stranded", 1);
            var attackerType = typeof(SiegeWaveDirector).GetNestedType("Attacker", System.Reflection.BindingFlags.NonPublic);
            object attacker = Activator.CreateInstance(attackerType, new object[] { "stranded", cockpit, true });
            director = new SiegeWaveDirector(
                match,
                settings,
                () => null,
                () => Array.Empty<SiegeShipOption>(),
                () => -1,
                unit.gameObject.scene);
            var list = (System.Collections.IList)typeof(SiegeWaveDirector).GetField("attackers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(director);
            list.Add(attacker);
            // Prevent spawning and target refresh in this focused orchestration check; the actual policy performs its first scan here.
            var actualPolicy = (SiegeStrandedCockpit)attackerType.GetField("Stranded").GetValue(attacker);
            actualPolicy.ShouldRetire(cockpit, 0, 12);
            director.BeginWave(0);
            director.Tick(12);
            Check(
                checks,
                "Production director removes stranded slot without killing",
                match.ActiveAttackers == 0 && match.DefeatedAttackers == 0 && !unit.gameObject.activeSelf);
            Check(checks, "Retirement grants no kill coins", match.Coins == 100 && match.PendingCoins == 0);
            Directory.CreateDirectory("Temp/NightDevelopment");
            File.WriteAllLines("Temp/NightDevelopment/stranded-checks.txt", checks);
            return checks.Count + " stranded cockpit checks passed";
        }
        finally
        {
            director?.Stop();
            if (unit != null)
            {
                PlayManager.instance.UnregisterControlUnit(unit);
                Object.DestroyImmediate(unit.gameObject);
            }

            Object.DestroyImmediate(settings);
            PlayManager.instance.playMode = previousPlay;
        }
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}

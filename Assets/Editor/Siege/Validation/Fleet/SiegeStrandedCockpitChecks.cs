// 职责：用真实受损舰和生产波次导演验证残舱退场、恢复边界及零击杀奖励。
// 入口：RunLive()为同步规则；RunNaturalLive()跨真实帧损毁并自然等待，均在Build Play运行。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegeStrandedCockpitChecks
{
    private static IEnumerator natural;
    private static readonly List<string> naturalChecks = new List<string>();
    private static readonly List<string> naturalErrors = new List<string>();
    private static double deadline;
    public static string NaturalStatus { get; private set; } = "idle";

    public static void RunNaturalLive()
    {
        if (!EditorApplication.isPlaying || natural != null || PlayManager.instance?.Modes?.State != GameModeState.Building || BuildManager.instance.IsLoadingBlocks)
        {
            throw new InvalidOperationException("Requires a loaded Build Play session with no other active probe.");
        }

        naturalChecks.Clear();
        naturalErrors.Clear();
        deadline = EditorApplication.timeSinceStartup + 120;
        NaturalStatus = "running";
        natural = NaturalScenario();
        Application.logMessageReceived += CaptureNaturalError;
        EditorApplication.update += TickNatural;
    }

    private static void CaptureNaturalError(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            naturalErrors.Add(message + "\n" + trace);
        }
    }

    private static void TickNatural()
    {
        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Natural stranded scenario stopped or exceeded 120 seconds.");
            }

            if (natural.MoveNext())
            {
                return;
            }
        }
        catch (Exception error)
        {
            naturalErrors.Add(error.ToString());
        }

        EditorApplication.update -= TickNatural;
        Application.logMessageReceived -= CaptureNaturalError;
        (natural as IDisposable)?.Dispose();
        natural = null;
        NaturalStatus = naturalErrors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/NightDevelopment");
        File.WriteAllLines(
            "Temp/NightDevelopment/stranded-natural.txt",
            new[] { NaturalStatus }.Concat(naturalChecks).Concat(naturalErrors));
    }

    private static IEnumerator NaturalScenario()
    {
        GameModeController modes = PlayManager.instance.Modes;
        modes.LaunchSiege(null);
        while (modes.State == GameModeState.Loading)
        {
            yield return null;
        }

        SiegeGameplayStage stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
        Check(
            naturalChecks,
            "Real production base loads for residual fixture",
            stage != null && stage.Base.IsAlive);
        SiegeSettings settings = Object.Instantiate(stage.Settings);
        SiegeWaveDirector director = null;
        ControlUnit unit = null;
        try
        {
            foreach (Turret turret in stage.Base.Building.GetComponentsInChildren<Turret>())
            {
                turret.SetStarted(false);
            }

            var data = new SerializedObject(settings);
            data.FindProperty("waveSpawnDelay").floatValue = 0f;
            data.FindProperty("strandedCockpitSeconds").floatValue = 12f;
            data.ApplyModifiedPropertiesWithoutUndo();
            TextAsset source = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
            BlockDataList design = JsonUtility.FromJson<BlockDataList>(source.text);
            var option = new SiegeShipOption("Natural residual Finch", source.text, 1, design.blocks.Count);
            var match = new SiegeMatch(
                100,
                1,
                0,
                1,
                120,
                0);
            match.BeginNextWave();
            director = new SiegeWaveDirector(
                match,
                settings,
                () => stage.Base,
                () => new[] { option },
                () => 0,
                stage.gameObject.scene);
            director.BeginWave(Time.time);
            director.Tick(Time.time);
            yield return null;
            unit = ControlUnit.RuntimeUnits.Single(candidate => candidate != null && candidate.gameObject.scene == stage.gameObject.scene && candidate.faction == UnitFaction.Enemy);
            Cockpit cockpit = unit.cockpit;
            Check(
                naturalChecks,
                "Production director spawns and registers intact hull",
                match.ActiveAttackers == 1 && unit.GetComponentsInChildren<Block>().Length > 1);
            // Isolate the immobility condition from knockback and gravity; navigation and damage remain production code.
            Rigidbody body = unit.GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeAll;
            foreach (Block block in unit.GetComponentsInChildren<Block>())
            {
                if (block.gameObject != cockpit.gameObject)
                {
                    block.GetComponent<Durability>().ApplyDamage(999999, null);
                }
            }

            yield return null;
            yield return null;
            unit = cockpit.GetComponentInParent<ControlUnit>();
            unit.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
            unit.RefreshChildren();
            Check(
                naturalChecks,
                "Lethal module damage leaves a real zero-thruster cockpit",
                unit.GetComponentsInChildren<Block>().Length == 1 && unit.Thrusters.Count == 0 && !unit.HasCombatCapability);
            float started = Time.time;
            double wallStart = EditorApplication.timeSinceStartup;
            int frameStart = Time.frameCount;
            bool observedGrace = false;
            while (Time.time - started < 14f && match.ActiveAttackers > 0)
            {
                director.Tick(Time.time);
                if (Time.time - started < 11.9f)
                {
                    Check(naturalChecks, "grace:" + Time.time, match.ActiveAttackers == 1 && cockpit != null);
                    // Keep the report bounded while still checking every real frame.
                    naturalChecks.RemoveAt(naturalChecks.Count - 1);
                    observedGrace = true;
                }

                yield return null;
            }

            float elapsed = Time.time - started;
            Check(
                naturalChecks,
                "Natural grace window keeps cockpit until twelve simulation seconds",
                observedGrace && elapsed >= 12f && elapsed < 14f);
            Check(
                naturalChecks,
                "Production director frees residual slot without a kill or reward",
                match.ActiveAttackers == 0 && match.DefeatedAttackers == 0 && match.Coins == 100 && match.PendingCoins == 0);
            Check(
                naturalChecks,
                "Residual root is disabled or destroyed",
                unit == null || !unit.gameObject.activeSelf);
            naturalChecks.Add("Measured simulation seconds=" + elapsed + "; wall seconds=" + (EditorApplication.timeSinceStartup - wallStart) + "; frames=" + (Time.frameCount - frameStart));
        }
        finally
        {
            director?.Stop();
            if (unit != null)
            {
                Object.Destroy(unit.gameObject);
            }

            Object.Destroy(settings);
            modes.ReturnToBuild();
        }

        while (modes.State == GameModeState.Returning)
        {
            yield return null;
        }

        Check(
            naturalChecks,
            "Return from natural fixture restores workshop",
            modes.State == GameModeState.Building);
    }

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

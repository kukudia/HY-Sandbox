// 职责：在独立Siege真实Play中验证部署/接管、修缮身份与金币、驾驶舰死亡和基地失败。
// 入口：Run()；损伤/缺件/断裂为显式夹具，死亡走生产伤害链；不代表自然交火或性能测试。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SiegePlayChecks
{
    private static readonly List<string> failures = new List<string>();
    public static string Run()
    {
        failures.Clear();
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session == null || session.Match == null || session.Match.Wave != 0)
        {
            throw new InvalidOperationException("Start a fresh Siege Play session before running checks.");
        }

        var checks = new List<string>();
        Check(
            checks,
            "base is one real connected component",
            BlockGroupManager.GroupBlocks(session.Fleet.Base.Unit.GetComponentsInChildren<Block>().ToList()).Count == 1);
        Check(
            checks,
            "intermission previews next wave size and selected enemy",
            session.NextWaveSize == session.Settings.FirstWaveSize && session.AttackerPreview == "RANDOM ENEMY");
        long coinsBeforeDeployment = session.Match.Coins;
        int[] affordableOptions = session.DefenderOptions.Select((option, index) => new { option.Price, index }).OrderBy(entry => entry.Price).Take(2).Select(entry => entry.index).ToArray();
        int firstOptionIndex = affordableOptions.Length > 0 ? affordableOptions[0] : -1;
        int secondOptionIndex = affordableOptions.Length > 1 ? affordableOptions[1] : -1;
        int firstPrice = firstOptionIndex >= 0 ? session.DefenderOptions[firstOptionIndex].Price : 0;
        int secondPrice = secondOptionIndex >= 0 ? session.DefenderOptions[secondOptionIndex].Price : 0;
        Check(
            checks,
            "first two ships deploy through production economy",
            affordableOptions.Length == 2 && session.TryDeploy(firstOptionIndex, out _) && session.TryDeploy(secondOptionIndex, out _) && session.Match.Coins == coinsBeforeDeployment - firstPrice - secondPrice);
        Check(
            checks,
            "driving authority can switch to a second ship",
            session.TryTakeControl(1) && session.Fleet.Driven == session.Fleet.Ships[1]);
        Check(
            checks,
            "exactly one allied ship reads manual input",
            session.Fleet.Ships.Count(entry => entry?.Unit != null && entry.Unit.AcceptsPlayerInput) == 1);
        Check(
            checks,
            "non-driven allied ship has autonomous control",
            !session.Fleet.Ships[0].Unit.AcceptsPlayerInput && session.Fleet.Ships[0].Unit.GetComponent<EnemyController>() != null);
        SiegeFleetEntry ship = session.Fleet.Ships[1];
        // 用户蓝图可能自带半格偏移的独立吊舱（例如 Swordfish 的侧舱），因此“单连通壳体”的基线要按实物取，
        // 修缮只要求不把壳体拆得比出厂更碎，而不是假设所有存档都是单连通。
        int baselineHullGroups = BlockGroupManager.GroupBlocks(ship.Unit.GetComponentsInChildren<Block>().ToList()).Count;
        Durability health = ship.Cockpit.GetComponent<Durability>();
        float intactHealth = health.currentDurability;
        health.ApplyDamage(50f, null);
        Check(
            checks,
            "intermission rejects late projectile damage",
            Mathf.Approximately(health.currentDurability, intactHealth));
        health.currentDurability -= 25f;
        long quote = SiegeRefit.Quote(ship, session.Settings);
        long coins = session.Match.Coins;
        Check(
            checks,
            "surviving-module repair restores health and exact quoted debit",
            session.TryRepair(false) && session.Match.Coins == coins - quote && Mathf.Approximately(health.currentDurability, health.maxDurability));
        Block missing = ship.Unit.GetComponentsInChildren<Block>().First(block => block.GetComponent<Cockpit>() == null && block.GetComponent<CargoHold>() == null);
        string missingId = missing.uniqueId;
        Object.DestroyImmediate(missing.gameObject);
        coins = session.Match.Coins;
        quote = SiegeRefit.Quote(ship, session.Settings);
        Check(
            checks,
            "missing module is rebuilt for quoted cost",
            quote > 0 && session.TryRepair(false) && session.Match.Coins == coins - quote);
        Check(
            checks,
            "replacement preserves original blueprint identity without duplicates",
            ship.Unit.GetComponentsInChildren<Block>().Count(block => block.uniqueId == missingId) == 1 && ship.Unit.GetComponentsInChildren<Block>().Length == ship.Blueprint.blocks.Count);
        Block detached = ship.Unit.GetComponentsInChildren<Block>().First(block => block.GetComponent<Cockpit>() == null && block.GetComponent<CargoHold>() == null);
        int detachedOriginalLayer = detached.raycastCollider.gameObject.layer;
        detached.DisConnectAllConnectors(false);
        GameObject fragment = Object.Instantiate(
            Resources.Load<GameObject>("BlocksParent/BlocksParent"),
            ship.Unit.transform.position + Vector3.up * 30f,
            Quaternion.identity);
        fragment.name = "Explicit repair identity fixture";
        detached.transform.SetParent(fragment.transform, true);
        fragment.GetComponent<ControlUnit>().RefreshChildren();
        Check(
            checks,
            "detached live module is recovered as the same object",
            session.TryRepair(false) && detached != null && detached.GetComponentInParent<ControlUnit>() == ship.Unit && ship.Unit.GetComponentsInChildren<Block>().Length == ship.Blueprint.blocks.Count);
        Check(
            checks,
            "repair restores actual detached collider participation and baseline hull connectivity",
            detached.raycastCollider.gameObject.layer == detachedOriginalLayer && BlockGroupManager.GroupBlocks(ship.Unit.GetComponentsInChildren<Block>().ToList()).Count == baselineHullGroups);
        fragment.SetActive(false);
        Object.Destroy(fragment);
        GameObject obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstruction.name = "Owned ship repair obstruction";
        obstruction.transform.position = ship.Cockpit.transform.position;
        coins = session.Match.Coins;
        Check(
            checks,
            "occupied repair bay is rejected without spending",
            !session.TryRepair(false) && session.Match.Coins == coins);
        Check(
            checks,
            "ship repair names the blocking object",
            session.Status.Contains(obstruction.name) && session.Status.Contains("Clear the area"));
        Object.DestroyImmediate(obstruction);
        Physics.SyncTransforms();
        SiegeFleetEntry autonomous = session.Fleet.Ships[0];
        Durability autonomousHealth = autonomous.Cockpit.GetComponent<Durability>();
        autonomousHealth.currentDurability -= 25f;
        long autonomousQuote = SiegeRefit.Quote(autonomous, session.Settings);
        var automaticObstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        automaticObstruction.name = "Automatic fleet refit obstruction";
        automaticObstruction.transform.position = autonomous.Cockpit.transform.position;
        long unobstructedCoins = session.Match.Coins;
        session.Fleet.RepairBetweenWaves(out int blockedRepaired, out int blockedAttempted, out long blockedSpend, out string blockedReason);
        Check(
            checks,
            "automatic fleet refit preserves damage and coins when its bay is occupied",
            blockedRepaired == 0 && blockedAttempted == 1 && blockedSpend == 0 && session.Match.Coins == unobstructedCoins && autonomousHealth.currentDurability < autonomousHealth.maxDurability && blockedReason.Contains(automaticObstruction.name));
        Object.DestroyImmediate(automaticObstruction);
        Physics.SyncTransforms();
        session.Fleet.RepairBetweenWaves(out int clearRepaired, out int clearAttempted, out long clearSpend, out _);
        Check(
            checks,
            "automatic fleet refit restores the ally after clearing its bay",
            clearRepaired == 1 && clearAttempted == 1 && clearSpend == autonomousQuote && session.Match.Coins == unobstructedCoins - autonomousQuote && Mathf.Approximately(autonomousHealth.currentDurability, autonomousHealth.maxDurability));
        ControlUnit oldCoreGroup = session.Fleet.Base.Unit;
        PlayManager.instance.RefreshGroup(oldCoreGroup);
        Check(
            checks,
            "building support refresh keeps anchored building outside ship regroup",
            session.Fleet.Base.Unit != null && session.Fleet.Base.Unit.GetComponent<Rigidbody>().isKinematic && session.Fleet.Base.Building.Anchored);
        Check(
            checks,
            "base regroup does not steal player camera ownership",
            PlayManager.instance.blocksParent == ship.Unit.transform);
        Check(
            checks,
            "wave start activates simulation and closes repair",
            session.BeginNextWave() && PlayManager.instance.playMode && !session.TryRepair(false));
        ControlUnit drivenUnit = session.Fleet.Driven.Unit;
        Durability drivenHealth = drivenUnit.cockpit.GetComponent<Durability>();
        drivenHealth.currentDurability -= Mathf.Max(1f, drivenHealth.maxDurability * 0.1f);
        long automaticShipQuote = SiegeRefit.Quote(session.Fleet.Driven, session.Settings);
        Durability baseHealth = session.Fleet.Base.Core.GetComponent<Durability>();
        baseHealth.currentDurability -= 25f;
        long automaticRepairQuote = session.Fleet.Base.Quote();
        int waveAttackers = session.Match.PendingAttackers;
        for (int index = 0; index < waveAttackers; index++)
        {
            string attackerId = "automatic-refit-check-" + index;
            session.Match.RegisterAttacker(attackerId, session.Settings.MaximumActiveEnemies);
            session.Match.ResolveAttacker(attackerId, true);
        }

        long coinsBeforeAutomaticRepair = session.Match.Coins;
        // 连击奖励也在这帧发放；本段只核对自动修缮的精确扣费，所以先把连击加成临时归零，结束再恢复。
        int originalComboBonus = SetComboBonus(session, 0);
        System.Reflection.MethodInfo update = typeof(SiegeSession).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        update.Invoke(session, null);
        long coinsAfterAutomaticRepair = session.Match.Coins;
        update.Invoke(session, null);
        Check(
            checks,
            "wave intermission automatically repairs the base and spends its exact quote once",
            session.Match.Phase == SiegePhase.Intermission && Mathf.Approximately(baseHealth.currentDurability, baseHealth.maxDurability) && coinsAfterAutomaticRepair == coinsBeforeAutomaticRepair - automaticRepairQuote - automaticShipQuote && session.Match.Coins == coinsAfterAutomaticRepair && automaticRepairQuote > 0 && session.Status.Contains("Auto-refit"));
        Check(
            checks,
            "wave intermission repairs the driven ship from remaining coins",
            Mathf.Approximately(drivenHealth.currentDurability, drivenHealth.maxDurability) && automaticShipQuote > 0 && session.Match.Coins == coinsBeforeAutomaticRepair - automaticRepairQuote - automaticShipQuote);
        SetComboBonus(session, originalComboBonus);
        long afterAutomaticRepairs = session.Match.Coins;
        update.Invoke(session, null);
        Check(
            checks,
            "repeated intermission updates never charge the same base or ship repair twice",
            session.Match.Coins == afterAutomaticRepairs);
        Check(checks, "next wave starts after automatic refit", session.BeginNextWave());
        ControlUnit previousAllyGroup = session.Fleet.Ships[0].Unit;
        PlayManager.instance.RefreshGroup(previousAllyGroup);
        session.Fleet.RefreshBindings();
        Check(
            checks,
            "autonomous ally retains its body and active AI after physical regroup",
            session.Fleet.Ships[0].Unit == previousAllyGroup && !session.Fleet.Ships[0].Unit.AcceptsPlayerInput && session.Fleet.Ships[0].Unit.GetComponent<EnemyController>().enabled);
        Cockpit driven = session.Fleet.Driven.Cockpit;
        driven.GetComponent<Durability>().ApplyDamage(100000f, null);
        Check(
            checks,
            "pilot destruction transfers control instead of ending match",
            session.Match.Phase == SiegePhase.Battle && PlayManager.instance.playMode && session.Fleet.Driven == session.Fleet.Ships[0]);
        session.Fleet.Driven.Cockpit.GetComponent<Durability>().ApplyDamage(100000f, null);
        Check(
            checks,
            "no surviving ships falls back to base observation",
            session.Match.Phase == SiegePhase.Battle && session.Fleet.Driven == null && PlayManager.instance.blocksParent == session.Fleet.Base.Unit.transform);
        session.Fleet.Base.Core.GetComponent<Durability>().ApplyDamage(100000f, null);
        Check(
            checks,
            "real core destruction ends siege and cancels reinforcements",
            session.Match.Phase == SiegePhase.Defeat && !PlayManager.instance.playMode && session.Match.PendingAttackers == 0);
        Check(
            checks,
            "defeat rejects new waves, deployment and repairs",
            !session.BeginNextWave() && !session.TryDeploy(0, out _) && !session.TryRepair(true));
        string json = JsonUtility.ToJson(new Report { checks = checks, failures = new List<string>(failures) }, true);
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/play.json", json);
        if (failures.Count > 0)
        {
            throw new InvalidOperationException("Siege Play checks failed: " + string.Join(" | ", failures));
        }

        return json;
    }

    /// <summary>测试夹具有限地改设置：只改内存中的连击单价，调用方负责恢复并避免保存资产。</summary>
    private static int SetComboBonus(SiegeSession session, int value)
    {
        var data = new SerializedObject(session.Settings);
        SerializedProperty property = data.FindProperty("comboBonusCoins");
        int previous = property.intValue;
        property.intValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
        return previous;
    }

    // 逐项记录而不是首个失败就中断：同一次运行能看到全部失败项，避免“每次停在不同断点”。
    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            failures.Add(name);
            checks.Add("FAIL " + name);
            Debug.LogError("Siege Play check failed: " + name);
            return;
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
        public List<string> failures;
    }
}

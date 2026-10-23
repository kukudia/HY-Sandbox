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
    public static string Run()
    {
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
            "first two ships deploy through production economy",
            session.TryDeploy(0, out _) && session.TryDeploy(1, out _) && session.Match.Coins == 1650);
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
        fragment.SetActive(false);
        Object.Destroy(fragment);
        GameObject obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstruction.transform.position = ship.Cockpit.transform.position;
        coins = session.Match.Coins;
        Check(
            checks,
            "occupied repair bay is rejected without spending",
            !session.TryRepair(false) && session.Match.Coins == coins);
        Object.DestroyImmediate(obstruction);
        Physics.SyncTransforms();
        ControlUnit oldCoreGroup = session.Fleet.Base.Unit;
        PlayManager.instance.RefreshGroup(oldCoreGroup);
        Check(
            checks,
            "base anchor survives production regroup",
            session.Fleet.Base.Unit != null && session.Fleet.Base.Unit.GetComponent<Rigidbody>().isKinematic && session.Fleet.Base.Cockpit.Anchored);
        Check(
            checks,
            "base regroup does not steal player camera ownership",
            PlayManager.instance.blocksParent == ship.Unit.transform);
        Check(
            checks,
            "wave start activates simulation and closes repair",
            session.BeginNextWave() && PlayManager.instance.playMode && !session.TryRepair(false));
        ControlUnit previousAllyGroup = session.Fleet.Ships[0].Unit;
        PlayManager.instance.RefreshGroup(previousAllyGroup);
        session.Fleet.RefreshBindings();
        Check(
            checks,
            "autonomous ally resumes AI after physical regroup",
            session.Fleet.Ships[0].Unit != previousAllyGroup && !session.Fleet.Ships[0].Unit.AcceptsPlayerInput && session.Fleet.Ships[0].Unit.GetComponent<EnemyController>().enabled);
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
        session.Fleet.Base.Cockpit.GetComponent<Durability>().ApplyDamage(100000f, null);
        Check(
            checks,
            "real core destruction ends siege and cancels reinforcements",
            session.Match.Phase == SiegePhase.Defeat && !PlayManager.instance.playMode && session.Match.PendingAttackers == 0);
        Check(
            checks,
            "defeat rejects new waves, deployment and repairs",
            !session.BeginNextWave() && !session.TryDeploy(0, out _) && !session.TryRepair(true));
        string json = JsonUtility.ToJson(new Report { checks = checks }, true);
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText("Temp/Siege/play.json", json);
        return json;
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException("Siege Play check failed: " + name);
        }

        checks.Add("PASS " + name);
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
    }
}

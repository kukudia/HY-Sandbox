// 职责：验证敌方碎片预算、寿命、节流及阵营/驾驶舱/场景保护，不制造真实击杀或修改蓝图。
// 入口：Run()；普通Siege全新Play，夹具同步检查后清除。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class SiegeWreckBudgetChecks
{
    private static GameObject root;
    private static readonly List<string> checks = new List<string>();
    public static string Run()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0)
        {
            throw new InvalidOperationException("Requires fresh Siege Play.");
        }

        checks.Clear();
        var report = new Report();
        Scene fixture = SceneManager.CreateScene("Owned enemy wreck budget fixture");
        root = new GameObject("Owned budget fixture");
        SceneManager.MoveGameObjectToScene(root, fixture);
        GameObject foreign = null;
        bool previousPlayMode = PlayManager.instance.playMode;
        UnityEngine.Random.State previousRandom = UnityEngine.Random.state;
        var previousDrops = new HashSet<LootDrop>(Object.FindObjectsByType<LootDrop>(FindObjectsSortMode.None));
        try
        {
            PlayManager.instance.playMode = true;
            var budget = new SiegeWreckBudget(fixture);
            ControlUnit friend = Create("friendly repairable", UnitFaction.Player, 2);
            ControlUnit live = Create("live enemy", UnitFaction.Enemy, 2);
            live.GetComponentInChildren<Block>().gameObject.AddComponent<Cockpit>();
            ControlUnit adopted = Create("adopted enemy fragment", UnitFaction.Enemy, 2);
            adopted.GetComponentInChildren<RuntimeUnitMember>().ownerFaction = UnitFaction.Player;
            ControlUnit outsider = Create("foreign scene enemy", UnitFaction.Enemy, 1);
            outsider.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(outsider.gameObject, session.gameObject.scene);
            foreign = outsider.gameObject;
            ControlUnit first = Create("old fragment", UnitFaction.Enemy, 2);
            budget.Tick(100f, 10, 100, 10f, 1);
            Check(
                "only enemy cockpitless same-scene ownership is counted",
                budget.PendingGroups == 1 && budget.PendingBlocks == 2);
            ControlUnit second = Create("new fragment", UnitFaction.Enemy, 3);
            budget.Tick(101f, 1, 100, 10f, 1);
            Check(
                "group budget retires oldest fragment",
                !first.gameObject.activeSelf && second.gameObject.activeSelf && budget.RetiredGroups == 1);
            Check(
                "retirement converts once through salvage marker",
                first.GetComponentInChildren<WreckSalvage>(true) != null);
            budget.Tick(101.1f, 0, 0, 10f, 1);
            Check("half-second scan interval avoids repeated retirement", second.gameObject.activeSelf);
            budget.Tick(102f, 10, 2, 10f, 1);
            Check(
                "module budget retires oversized fragment",
                !second.gameObject.activeSelf && budget.PendingBlocks == 0);
            ControlUnit aged = Create("aged fragment", UnitFaction.Enemy, 1);
            budget.Tick(103f, 10, 100, 2f, 1);
            budget.Tick(104f, 10, 100, 2f, 1);
            Check("young fragments remain within budget", aged.gameObject.activeSelf);
            budget.Tick(105f, 10, 100, 2f, 1);
            Check("lifetime expires even below count budget", !aged.gameObject.activeSelf);
            ControlUnit a = Create("burst a", UnitFaction.Enemy, 1);
            ControlUnit b = Create("burst b", UnitFaction.Enemy, 1);
            budget.Tick(106f, 0, 0, 100f, 1);
            Check(
                "retirement batch preserves one queued burst fragment",
                budget.PendingGroups == 1 && (a.gameObject.activeSelf != b.gameObject.activeSelf));
            budget.Tick(107f, 0, 0, 100f, 1);
            Check("next scan converges to configured budget", budget.PendingGroups == 0 && budget.PendingBlocks == 0);
            Check(
                "friendly live cockpit adopted and foreign objects survive",
                friend.gameObject.activeSelf && live.gameObject.activeSelf && adopted.gameObject.activeSelf && outsider.gameObject.activeSelf);
            ControlUnit reclaimed = Create("reclaimed before expiration", UnitFaction.Enemy, 1);
            budget.Tick(108f, 10, 100, 1f, 1);
            reclaimed.GetComponentInChildren<RuntimeUnitMember>().ownerFaction = UnitFaction.Player;
            budget.Tick(110f, 0, 0, 1f, 1);
            Check(
                "ownership is rechecked before delayed expiration",
                reclaimed.gameObject.activeSelf && budget.PendingGroups == 0);
            Check(
                "primary building and deployment state unchanged",
                session.Fleet.Base.IsAlive && session.Match.Wave == 0 && session.Buildings.Buildings.Count == 1);
            report.status = "passed";
        }
        catch (Exception error)
        {
            report.status = "failed";
            report.error = error.ToString();
        }
        finally
        {
            PlayManager.instance.playMode = previousPlayMode;
            UnityEngine.Random.state = previousRandom;
            foreach (LootDrop drop in Object.FindObjectsByType<LootDrop>(FindObjectsSortMode.None))
            {
                if (!previousDrops.Contains(drop))
                {
                    Object.DestroyImmediate(drop.gameObject);
                }
            }

            Object.DestroyImmediate(root);
            if (foreign != null)
            {
                Object.DestroyImmediate(foreign);
            }

            SceneManager.UnloadSceneAsync(fixture);
        }

        report.checks = checks;
        const string path = "Temp/Siege/wreck-budget-checks.json";
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        return path;
    }

    private static ControlUnit Create(string label, UnitFaction faction, int count)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(root.transform, false);
        obj.transform.position = Vector3.one * 10000f;
        obj.AddComponent<Rigidbody>().isKinematic = true;
        ControlUnit unit = obj.AddComponent<ControlUnit>();
        unit.faction = faction;
        for (int index = 0; index < count; index++)
        {
            var module = new GameObject("module " + index);
            module.transform.SetParent(obj.transform, false);
            module.AddComponent<Block>();
            RuntimeUnitMember.Ensure(module, "owned-budget-fixture", faction);
        }

        return unit;
    }

    private static void Check(string label, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(label);
        }

        checks.Add(label);
    }

    [Serializable]
    private sealed class Report
    {
        public string status;
        public string error;
        public List<string> checks;
    }
}

// 职责：以真实部署净空和工作台往返检查阵地战泊位选择与蓝图选择保持，保护金币和用户存档。
// 入口：RunDeployment() / RunSelection()；分别在全新 Siege Play 中执行，不修改源蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SiegeUsabilityChecks
{
    public static string RunDeployment()
    {
        SiegeSession session = FreshSession();
        var checks = new List<string>();
        var blockers = new List<GameObject>();
        try
        {
            int option = session.DefenderOptions.Select((ship, index) => new { ship.Price, index }).OrderBy(ship => ship.Price).First().index;
            int price = session.DefenderOptions[option].Price;
            var docks = (Transform[])typeof(SiegeSession).GetField("shipDocks", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(session);
            BlockData data = JsonUtility.FromJson<BlockDataList>(session.DefenderOptions[option].BlueprintJson).blocks[0];
            Func<int, GameObject> obstruct = slot =>
            {
                var blocker = new GameObject("Owned blocked deployment bay " + slot);
                blocker.transform.SetPositionAndRotation(docks[slot].TransformPoint(new Vector3(data.posX, data.posY, data.posZ)), docks[slot].rotation);
                blocker.AddComponent<BoxCollider>().size = Vector3.one * 0.5f;
                blockers.Add(blocker);
                return blocker;
            };
            long balance = session.Match.Coins;
            obstruct(0);
            bool deployed = session.TryDeploy(option, out string reason);
            Check(checks, "blocked first bay falls through to a clear bay: " + reason, deployed);
            Check(
                checks,
                "clear second bay is used without occupying the blocked first bay",
                session.Fleet.Ships[0] == null && session.Fleet.Ships[1]?.IsAlive == true);
            Check(
                checks,
                "successful fallback deployment charges exactly once",
                session.Match.Coins == balance - price);
            SiegeFleetEntry driven = session.Fleet.Driven;
            for (int index = 1; index < docks.Length; index++)
            {
                obstruct(index);
            }

            balance = session.Match.Coins;
            Check(
                checks,
                "all remaining free bays obstructed rejects deployment",
                !session.TryDeploy(option, out reason));
            Check(
                checks,
                "obstruction names a blocking object",
                reason.Contains("Blocked by", StringComparison.Ordinal));
            Check(
                checks,
                "failed deployment preserves balance and existing pilot",
                session.Match.Coins == balance && session.Fleet.Driven == driven);
            Object.DestroyImmediate(blockers[0]);
            Check(
                checks,
                "clearing the first bay permits another purchase",
                session.TryDeploy(option, out reason) && session.Fleet.Ships[0]?.IsAlive == true);
            Check(
                checks,
                "new purchase preserves existing driving authority and exact debit",
                session.Fleet.Driven == driven && session.Match.Coins == balance - price);
            Check(
                checks,
                "invalid option does not debit",
                !session.TryDeploy(-1, out _) && session.Match.Coins == balance - price);
            return Write("usability-deployment", checks);
        }
        catch (Exception error)
        {
            return Write("usability-deployment", checks, error);
        }
        finally
        {
            foreach (GameObject blocker in blockers)
            {
                if (blocker != null)
                {
                    Object.DestroyImmediate(blocker);
                }
            }
        }
    }

    public static string RunSelection()
    {
        SiegeSession session = FreshSession();
        var checks = new List<string>();
        try
        {
            SiegeHud hud = Object.FindFirstObjectByType<SiegeHud>();
            Dropdown defender = (Dropdown)typeof(SiegeHud).GetField("defenderBlueprint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            Dropdown attacker = (Dropdown)typeof(SiegeHud).GetField("attackerBlueprint", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(hud);
            RefreshHud(hud);
            Check(
                checks,
                "test has multiple real player and enemy choices",
                session.DefenderOptions.Length > 1 && session.AttackerOptions.Length > 1);
            defender.value = session.DefenderOptions.Length - 1;
            attacker.value = session.AttackerOptions.Length;
            string defenderName = session.DefenderOptions[defender.value].DisplayName;
            string attackerName = session.AttackerOptions[session.SelectedAttacker].DisplayName;
            long balance = session.Match.Coins;
            session.RefreshBlueprints();
            RefreshHud(hud);
            Check(
                checks,
                "explicit refresh preserves selected attacker",
                session.AttackerPreview == attackerName && attacker.value == session.SelectedAttacker + 1);
            Check(
                checks,
                "explicit refresh preserves selected defender",
                session.DefenderOptions[defender.value].DisplayName == defenderName);
            Check(checks, "workshop opens through production entry", session.Workshop.Open());
            Check(checks, "workshop closes through production entry", session.Workshop.Close());
            RefreshHud(hud);
            Check(
                checks,
                "workshop roundtrip preserves both selected blueprints",
                session.AttackerPreview == attackerName && session.DefenderOptions[defender.value].DisplayName == defenderName && attacker.value == session.SelectedAttacker + 1);
            Check(checks, "refresh and workshop roundtrip spend no coins", session.Match.Coins == balance);
            attacker.value = 0;
            session.RefreshBlueprints();
            RefreshHud(hud);
            Check(
                checks,
                "random enemy choice remains random",
                session.SelectedAttacker == -1 && attacker.value == 0);
            // Replace only the runtime catalog copy to model a deleted file; no user file is touched.
            attacker.value = 1;
            SiegeSettings originalSettings = session.Settings;
            var settingsCopy = Object.Instantiate(session.Settings);
            try
            {
                var config = new SerializedObject(settingsCopy);
                config.FindProperty("usePlayerBlueprintFolders").boolValue = false;
                config.FindProperty("ships").arraySize = 0;
                config.ApplyModifiedPropertiesWithoutUndo();
                typeof(SiegeSession).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, settingsCopy);
                session.RefreshBlueprints();
                RefreshHud(hud);
                Check(
                    checks,
                    "removed choices safely fall back to random and empty defender list",
                    session.SelectedAttacker == -1 && attacker.value == 0 && defender.options.Count == 0);
                Check(checks, "empty attacker catalog prevents wave start", !session.BeginNextWave());
            }
            finally
            {
                typeof(SiegeSession).GetField("settings", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(session, originalSettings);
                session.RefreshBlueprints();
                Object.DestroyImmediate(settingsCopy);
            }

            return Write("usability-selection", checks);
        }
        catch (Exception error)
        {
            return Write("usability-selection", checks, error);
        }
    }

    private static SiegeSession FreshSession()
    {
        SiegeSession session = Object.FindFirstObjectByType<SiegeSession>();
        if (!EditorApplication.isPlaying || session?.Match == null || session.Match.Wave != 0 || session.Fleet.Ships.Any(ship => ship != null))
        {
            throw new InvalidOperationException("Requires fresh normal Siege Play with real blueprint catalogs.");
        }

        return session;
    }

    private static void RefreshHud(SiegeHud hud)
    {
        typeof(SiegeHud).GetField("nextRefresh", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(hud, 0f);
        typeof(SiegeHud).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(hud, null);
    }

    private static void Check(List<string> checks, string label, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(label);
        }

        checks.Add(label);
    }

    private static string Write(string name, List<string> checks, Exception error = null)
    {
        string path = "Temp/Siege/" + name + ".json";
        Directory.CreateDirectory("Temp/Siege");
        File.WriteAllText(
            path,
            JsonUtility.ToJson(
                new Report { status = error == null ? "passed" : "failed", checks = checks, error = error?.ToString() },
                true));
        return path;
    }

    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks;
        public string error;
    }
}

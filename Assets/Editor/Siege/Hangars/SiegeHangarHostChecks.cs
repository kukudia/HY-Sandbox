// 职责：验证真实菜单新版基地战与独立预研会话复用机库UI、舰队与返航结算。
// 入口：RunStage() / RunPrototype()；只注入运行时蓝图目录，报告写Temp/Hangars/host-*.json。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class SiegeHangarHostChecks
{
    private const string Pending = "HY.HangarHostChecks";
    private static IEnumerator scenario;
    private static Report report;
    private static double deadline;
    private static string mode;
    private static readonly Dictionary<string, string> preferences = new Dictionary<string, string>();
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    static SiegeHangarHostChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
    }

    [MenuItem("Tools/HY-Sandbox/Siege/Validate menu hangar host")]
    public static void RunStage()
    {
        Start("stage", "Assets/Scenes/MainMenu.unity");
    }

    [MenuItem("Tools/HY-Sandbox/Siege/Validate prototype hangar host")]
    public static void RunPrototype()
    {
        Start("prototype", "Assets/Scenes/Siege.unity");
    }

    private static void Start(string target, string path)
    {
        if (EditorApplication.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != path || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Open only saved " + path + " in Edit Mode first.");
        }

        SessionState.SetString(Pending, target);
        EditorApplication.isPlaying = true;
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && !string.IsNullOrEmpty(SessionState.GetString(Pending, "")))
        {
            mode = SessionState.GetString(Pending, "");
            report = new Report
            {
                status = "running"
            };
            scenario = Scenario();
            deadline = EditorApplication.timeSinceStartup + 240;
            Application.logMessageReceived += LogError;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.ExitingPlayMode && scenario != null)
        {
            report.errors.Add("Play exited before completion.");
            Finish();
        }
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Hangar host validation exceeded 240 seconds.");
            }

            if (!scenario.MoveNext())
            {
                Finish();
            }
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
            Finish();
        }
    }

    private static IEnumerator Scenario()
    {
        yield return null;
        MainMenuController menu = null;
        ISiegeDeploymentHost host;
        SiegeBase fortress;
        SiegeGameplayStage stage = null;
        SiegeSession prototype = null;
        if (mode == "stage")
        {
            BackupPreferences();
            menu = Object.FindAnyObjectByType<MainMenuController>();
            Check(menu != null && menu.MenuVisible && !menu.Busy, "Cold main menu is interactive");
            menu.Choose(MenuDestination.Siege);
            menu.SelectBlueprint(0);
            Check(menu.Selected.IsExample, "Menu selects the authored starter without writing a blueprint");
            menu.LaunchSelected();
            while (menu.Busy)
            {
                yield return null;
            }

            stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
            Check(stage != null && stage.Base != null && stage.Base.IsAlive, "Menu launches the real new Siege host");
            Check(Object.FindObjectsByType<SiegeSession>().Length == 0, "New Siege host leaves prototype unloaded");
            host = stage;
            fortress = stage.Base;
            Check(
                host.Fleet.UsesHangars && host.Fleet.DeploymentCount == 1,
                "Starter base exposes its single authored hangar");
        }
        else
        {
            while (PlayManager.instance == null || PlayManager.instance.SiegeSession == null || PlayManager.instance.SiegeSession.Fleet == null)
            {
                yield return null;
            }

            prototype = PlayManager.instance.SiegeSession;
            host = prototype;
            fortress = prototype.Fleet.Base;
            Check(
                host.Fleet.UsesHangars && host.Fleet.DeploymentCount == 3,
                "Prototype binds its three actual base hangars");
            Check(fortress != null && fortress.IsAlive, "Prototype owns a surviving authored base");
        }

        BlockDataList finch = JsonUtility.FromJson<BlockDataList>(Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch").text);
        Check(host.Fleet.FitsHangar(0, finch), "Real host hangar has sufficient internal clearance for Finch");
        var oversized = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(finch));
        oversized.blocks[0].x = 64;
        var options = new[]
        {
            new SiegeShipOption("Host Finch", JsonUtility.ToJson(finch), 1, finch.blocks.Count),
            new SiegeShipOption("Host Oversized", JsonUtility.ToJson(oversized), 1, oversized.blocks.Count)
        };
        if (stage != null)
        {
            typeof(SiegeGameplayStage).GetField("defenders", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stage, options);
            typeof(SiegeGameplayStage).GetField("attackers", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stage, new[] { options[0] });
        }
        else
        {
            typeof(SiegeSession).GetProperty(nameof(SiegeSession.DefenderOptions)).GetSetMethod(true).Invoke(prototype, new object[] { options });
            typeof(SiegeSession).GetProperty(nameof(SiegeSession.AttackerOptions)).GetSetMethod(true).Invoke(prototype, new object[] { new[] { options[0] } });
        }

        host.SelectDefender(0);
        yield return null;
        yield return null;
        SiegeDeploymentView view = Object.FindObjectsByType<SiegeDeploymentView>(FindObjectsInactive.Include).Single(candidate => candidate.gameObject.scene == fortress.Building.gameObject.scene);
        var ui = new SerializedObject(view);
        Button marker = (Button)ui.FindProperty("markers").GetArrayElementAtIndex(0).objectReferenceValue;
        marker.onClick.Invoke();
        Check(
            view.SelectedDock == 0 && ((GameObject)ui.FindProperty("card").objectReferenceValue).activeInHierarchy,
            "Saved deployment marker opens the real host card");
        Dropdown picker = (Dropdown)ui.FindProperty("defenderBlueprint").objectReferenceValue;
        Check(
            picker.options.Count == 1 && picker.options[0].text.Contains("Host Finch"),
            "Shared dropdown excludes oversized blueprint for the selected hangar");
        // Dropdown initializes its tween runner in Start after an inactive card is first enabled.
        yield return null;
        yield return null;
        picker.Show();
        for (int i = 0; i < 8; i++)
        {
            yield return null;
        }

        Text row = picker.GetComponentsInChildren<Text>().First(text => text.name == "Item Label");
        Check(
            row.rectTransform.rect.height >= row.fontSize + 4 && row.cachedTextGenerator.vertexCount > 4,
            "Real expanded hangar dropdown renders readable option text");
        Directory.CreateDirectory("Temp/Hangars");
        ScreenCapture.CaptureScreenshot("Temp/Hangars/host-" + mode + "-dropdown.png");
        double capturedUntil = EditorApplication.timeSinceStartup + .4d;
        while (EditorApplication.timeSinceStartup < capturedUntil)
        {
            yield return null;
        }

        picker.Hide();
        Button confirm = (Button)ui.FindProperty("confirm").objectReferenceValue;
        Check(confirm.interactable, "Real confirmation button accepts valid free-bay preview");
        long coins = host.Match.Coins;
        confirm.onClick.Invoke();
        SiegeFleetEntry ship = host.Fleet.Ships[0];
        Check(
            ship != null && ship.IsAlive && host.Match.Coins == coins - 1,
            "Saved confirmation purchases exactly one assigned ship");
        Check(
            view.SelectedDock < 0 && view.Preview == null,
            "Confirmed deployment clears temporary preview and card");
        Check(
            !host.TryDeployAt(0, 0, out _) && host.Fleet.Ships[0] == ship,
            "Both host adapters reject another purchase in the assigned hangar");
        string shipId = ship.Id;
        Durability coreHealth = fortress.Core.GetComponent<Durability>();
        Durability hangarHealth = host.Fleet.GetHangar(0).GetComponent<Durability>();
        coreHealth.currentDurability = coreHealth.maxDurability * .5f;
        hangarHealth.currentDurability = hangarHealth.maxDurability * .5f;
        long repairQuote = fortress.Quote();
        Check(
            repairQuote > 0 && host.Settings.AutoRepairBase,
            "Host auto-refit has a real paid core and hangar repair quote");
        bool began = stage != null ? stage.BeginWave() : prototype.BeginNextWave();
        Check(
            began && host.Match.Phase == SiegePhase.Battle,
            "Actual host begins its configured battle lifecycle");
        long beforeReward = host.Match.Coins;
        int kills = ClearWave(host.Match);
        long pending = host.Match.PendingCoins;
        Check(
            host.Match.Phase == SiegePhase.Returning && pending > 0 && host.Match.Coins == beforeReward,
            "Cleared real host defers all wave income during return");
        while (host.Match.Phase == SiegePhase.Returning)
        {
            yield return null;
        }

        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        Check(
            host.Match.Phase == SiegePhase.Intermission && host.Match.PendingCoins == 0,
            "Host fixed-step return completes and settles pending income");
        Check(
            host.FlightGuidance.CompletedCount == 1 && host.FlightGuidance.FailedCount == 0 && ship.IsAlive,
            "Real host recovers its assigned docked ship");
        Check(
            fortress.Quote() == 0 && Mathf.Approximately(hangarHealth.currentDurability, hangarHealth.maxDurability),
            "Host automatically spends coins to repair the damaged hangar after return");
        long comboBonus = prototype != null ? SiegeSession.ComboBonusFor(kills, host.Settings.ComboBonusCoins, host.Settings.ComboBonusCap) : 0;
        Check(
            host.Match.Coins == beforeReward + pending + comboBonus - repairQuote,
            "Host credits wave rewards once and subtracts its actual repair quote");
        long settledCoins = host.Match.Coins;
        Check(!host.Match.CompleteReturn(), "Already settled host rejects a second return settlement");
        yield return null;
        yield return null;
        Check(host.Match.Coins == settledCoins, "Settled host balance stays unchanged on subsequent frames");
        if (menu != null)
        {
            menu.ReturnToMenu();
            while (menu.Busy)
            {
                yield return null;
            }

            Check(
                menu.MenuVisible && Object.FindObjectsByType<SiegeGameplayStage>().Length == 0,
                "Returning to menu unloads the new host");
            Check(
                !ControlUnit.RuntimeUnits.Any(unit => unit != null && unit.runtimeUnitId == shipId),
                "Returning to menu leaves no deployed ship behind in Main");
        }
        else
        {
            prototype.End();
            prototype.Fleet.Scrap(0);
            Check(
                prototype.Match.Phase == SiegePhase.Defeat && !ship.IsAlive,
                "Prototype exit releases guidance and its assigned ship");
        }
    }

    private static int ClearWave(SiegeMatch match)
    {
        int kills = 0;
        var registered = (HashSet<string>)typeof(SiegeMatch).GetField("attackers", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(match);
        foreach (string id in registered.ToArray())
        {
            if (match.ResolveAttacker(id, true))
            {
                kills++;
            }
        }

        while (match.PendingAttackers > 0)
        {
            string id = "Host check attacker " + Guid.NewGuid().ToString("N");
            if (!match.RegisterAttacker(id, int.MaxValue) || !match.ResolveAttacker(id, true))
            {
                throw new InvalidOperationException("Could not finish isolated host wave.");
            }

            kills++;
        }

        return kills;
    }

    private static void BackupPreferences()
    {
        preferences.Clear();
        foreach (string key in new[]
        {
            "HY.Menu.RecentName",
            "HY.Menu.RecentBuilding"
        }

        )
        {
            preferences[key] = PlayerPrefs.HasKey(key) ? key.EndsWith("RecentBuilding", StringComparison.Ordinal) ? PlayerPrefs.GetInt(key).ToString() : PlayerPrefs.GetString(key) : null;
        }
    }

    private static void RestorePreferences()
    {
        foreach (var item in preferences)
        {
            if (item.Value == null)
            {
                PlayerPrefs.DeleteKey(item.Key);
            }
            else if (item.Key.EndsWith("RecentBuilding", StringComparison.Ordinal))
            {
                PlayerPrefs.SetInt(item.Key, int.Parse(item.Value));
            }
            else
            {
                PlayerPrefs.SetString(item.Key, item.Value);
            }
        }

        PlayerPrefs.Save();
        preferences.Clear();
    }

    private static void Check(bool success, string description)
    {
        if (!success)
        {
            throw new InvalidOperationException(description);
        }

        report.checks.Add(description);
    }

    private static void LogError(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            report.errors.Add(message + "\n" + stack);
        }
    }

    private static void Finish()
    {
        Application.logMessageReceived -= LogError;
        EditorApplication.update -= Tick;
        (scenario as IDisposable)?.Dispose();
        scenario = null;
        SessionState.SetString(Pending, "");
        RestorePreferences();
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/Hangars");
        File.WriteAllText("Temp/Hangars/host-" + mode + ".json", JsonUtility.ToJson(report, true));
        Debug.Log("Hangar " + mode + " host checks: " + report.status + " / " + report.checks.Count);
        EditorApplication.isPlaying = false;
    }
}

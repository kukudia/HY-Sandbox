// 职责：通过真实Main模式入口和真实伤害链验证共享HUD的投影、损伤动画、击杀去重与退出恢复。
// 入口：Run() / RunArena()；只在Play实例上刺激，不修改蓝图、场景或HUD资产。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class CombatHudPlayProbe
{
    private const string Pending = "HY.SharedCombatHudProbe";
    private static readonly List<string> checks = new List<string>();
    private static IEnumerator scenario;
    private static double nextStep;
    private static double deadline;
    private static string error;
    private static string mode;
    private static SceneSetup[] previousSetup;
    public static string Status { get; private set; } = "idle";

    static CombatHudPlayProbe()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/HY-Sandbox/Validate Combat HUD")]
    public static void Run()
    {
        Begin("siege", "Assets/Scenes/Main.unity");
    }

    [MenuItem("Tools/HY-Sandbox/Combat/Validate shared HUD in 5v5")]
    public static void RunArena()
    {
        Begin("arena", "Assets/Scenes/Random5v5Arena.unity");
    }

    [MenuItem("Tools/HY-Sandbox/Combat/Validate shared HUD in exploration")]
    public static void RunExploration()
    {
        Begin("exploration", "Assets/Scenes/Main.unity");
    }

    [MenuItem("Tools/HY-Sandbox/Combat/Validate shared HUD in legacy Siege")]
    public static void RunLegacy()
    {
        Begin("legacy", "Assets/Scenes/Siege.unity");
    }

    private static void Begin(string kind, string path)
    {
        if (EditorApplication.isPlaying || Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
        {
            throw new InvalidOperationException("Exit Play Mode and save scenes before validation.");
        }

        previousSetup = EditorSceneManager.GetSceneManagerSetup();
        EditorSceneManager.OpenScene(path);
        SessionState.SetString(Pending, kind);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        string pending = SessionState.GetString(Pending, "");
        if (string.IsNullOrEmpty(pending))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            mode = pending;
            checks.Clear();
            error = null;
            Status = "running " + mode;
            scenario = mode == "arena" ? Arena() : mode == "exploration" ? Exploration() : mode == "legacy" ? Legacy() : Siege();
            deadline = EditorApplication.timeSinceStartup + 180;
            nextStep = 0;
            EditorApplication.update += Tick;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.EraseString(Pending);
            if (previousSetup != null)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }
    }

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < nextStep)
        {
            return;
        }

        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("HUD scenario stopped or exceeded 180s: " + Status);
            }

            if (!scenario.MoveNext())
            {
                Finish();
                return;
            }

            nextStep = EditorApplication.timeSinceStartup + (scenario.Current is float delay ? delay : .03f);
        }
        catch (Exception exception)
        {
            error = exception.ToString();
            Finish();
        }
    }

    private static IEnumerator Siege()
    {
        while (BuildManager.instance == null || BuildManager.instance.IsLoadingBlocks)
        {
            yield return .1f;
        }

        GameModeController modes = PlayManager.instance.Modes;
        bool friendlyDebug = DebugManager.instance.ShowsAI(UnitFaction.Player);
        bool enemyDebug = DebugManager.instance.ShowsAI(UnitFaction.Enemy);
        modes.LaunchSiege(null);
        while (modes.State == GameModeState.Loading)
        {
            yield return .1f;
        }

        Check("Main loads SiegeGameplay through production mode entry", modes.State == GameModeState.Siege);
        SiegeGameplayStage stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
        SiegeCombatHud entry = Object.FindAnyObjectByType<SiegeCombatHud>();
        CombatHud hud = entry.Hud;
        Check("HUD is bound to the additive battlefield", hud.Battlefield == stage.gameObject.scene);
        Check(
            "Only one combat HUD is active",
            Object.FindObjectsByType<CombatHud>(FindObjectsSortMode.None).Length == 1);
        hud.Panels.ClearDebug();
        Check(
            "Debug drawer controls existing debug service",
            !DebugManager.instance.showPowerRange && !DebugManager.instance.ShowsAI(UnitFaction.Enemy));
        Check("Real wave starts", stage.BeginWave());
        while (!hud.Candidates.Any(unit => unit.faction == UnitFaction.Enemy))
        {
            yield return .1f;
        }

        Time.timeScale = 0f;
        ControlUnit enemy = hud.Candidates.First(unit => unit.faction == UnitFaction.Enemy);
        Camera camera = hud.WorldCamera;
        Pose pose = new Pose(camera.transform.position, camera.transform.rotation);
        camera.transform.position = enemy.cockpit.transform.position + new Vector3(0, 12, -35);
        camera.transform.LookAt(enemy.cockpit.transform.position + Vector3.up * 3f);
        yield return .45f;
        CombatNameplate plate = hud.GetComponentsInChildren<CombatNameplate>().First(view => view.IsVisible);
        Vector3 screen = camera.WorldToScreenPoint(enemy.cockpit.transform.position + Vector3.up * 3f);
        Vector3 actual = RectTransformUtility.WorldToScreenPoint(null, plate.transform.position);
        Check("Nameplate anchor matches projected cockpit", Vector2.Distance(screen, actual) < 2f);
        RectTransform fill = (RectTransform)plate.transform.Find("Bar/Fill");
        RectTransform lag = (RectTransform)plate.transform.Find("Bar/Damage");
        Durability health = enemy.cockpit.GetComponent<Durability>();
        health.ApplyDamage(Mathf.Min(10f, health.currentDurability * .1f), stage.Base.Unit);
        yield return .13f;
        Check("Damage trail remains behind immediate health", lag.anchorMax.x > fill.anchorMax.x);
        yield return .7f;
        Check(
            "Damage trail settles while simulation is paused",
            Mathf.Abs(lag.anchorMax.x - fill.anchorMax.x) < .005f);
        camera.transform.rotation *= Quaternion.Euler(0, 150, 0);
        yield return .15f;
        CombatThreatMarkers threats = hud.GetComponentInChildren<CombatThreatMarkers>();
        Check("Offscreen enemy produces a direction marker", threats.VisibleCount > 0);
        CombatThreatView marker = threats.GetComponentsInChildren<CombatThreatView>().First(view => view.Used);
        Check("Direction marker completes visible fade-in", marker.GetComponent<CanvasGroup>().alpha > .1f);
        RectTransform bounds = (RectTransform)threats.transform;
        Check("Direction marker remains inside canvas", bounds.rect.Contains(marker.Position));
        Vector2 before = marker.Position;
        camera.transform.rotation *= Quaternion.Euler(0, 12, 0);
        yield return .04f;
        Check("Marker responds within 40ms of camera motion", Vector2.Distance(before, marker.Position) > .1f);
        camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        int kills = stage.Match.DefeatedAttackers;
        int messages = hud.Notifications.TotalMessages;
        health.ApplyDamage(999999, stage.Base.Unit);
        health.ApplyDamage(999999, stage.Base.Unit);
        yield return .3f;
        Check("Repeated lethal damage resolves one kill", stage.Match.DefeatedAttackers == kills + 1);
        Check("One confirmed kill produces one notification", hud.Notifications.TotalMessages == messages + 1);
        for (int i = 0; i < 8; i++)
        {
            hud.ShowDestruction("BURST " + i, "BASE", "PRESENTATION STRESS", true);
        }

        Check("Celebration queue stays bounded during bursts", hud.Notifications.PendingCount <= 4);
        hud.Panels.ToggleDebug();
        yield return .25f;
        Check("Debug drawer opens", hud.Panels.IsOpen);
        hud.Panels.ToggleThrusters();
        yield return .25f;
        Check(
            "Thrust drawer opens and replaces debug drawer",
            entry.GetComponentInChildren<ThrusterInfoPanel>().IsOpen);
        Screenshot("Siege-tools");
        Time.timeScale = 1f;
        modes.ReturnToBuild();
        while (modes.State == GameModeState.Returning)
        {
            yield return .1f;
        }

        Check(
            "Return unloads Siege HUD",
            modes.State == GameModeState.Building && Object.FindAnyObjectByType<SiegeCombatHud>() == null);
        Check(
            "Return restores debug preferences",
            DebugManager.instance.ShowsAI(UnitFaction.Player) == friendlyDebug && DebugManager.instance.ShowsAI(UnitFaction.Enemy) == enemyDebug);
        modes.LaunchSiege(null);
        while (modes.State == GameModeState.Loading)
        {
            yield return .1f;
        }

        Check(
            "Reentry starts with empty notifications",
            Object.FindAnyObjectByType<SiegeCombatHud>().Hud.Notifications.TotalMessages == 0);
        modes.ReturnToBuild();
        while (modes.State == GameModeState.Returning)
        {
            yield return .1f;
        }
    }

    private static IEnumerator Legacy()
    {
        SiegeSession session = Object.FindAnyObjectByType<SiegeSession>();
        while (session.Match == null)
        {
            yield return .1f;
        }

        CombatHud hud = MainUIPanels.instance.CombatHud;
        Check("Legacy Siege starts real wave", session.BeginNextWave());
        while (!hud.Candidates.Any(unit => unit.faction == UnitFaction.Enemy))
        {
            yield return .1f;
        }

        Check(
            "Legacy Siege has one shared HUD",
            Object.FindObjectsByType<CombatHud>(FindObjectsSortMode.None).Length == 1);
        Check("Legacy HUD binds its battlefield", hud.Battlefield == session.gameObject.scene);
        Time.timeScale = 0f;
        Durability victim = hud.Candidates.First(unit => unit.faction == UnitFaction.Enemy).cockpit.GetComponent<Durability>();
        int kills = session.Match.DefeatedAttackers;
        int messages = hud.Notifications.TotalMessages;
        victim.ApplyDamage(999999, null);
        victim.ApplyDamage(999999, null);
        yield return .4f;
        Check("Legacy repeated lethal damage settles once", session.Match.DefeatedAttackers == kills + 1);
        Check(
            "Legacy rule settlement produces one shared notice",
            hud.Notifications.TotalMessages == messages + 1);
        hud.Panels.ToggleDebug();
        yield return .2f;
        Check("Legacy debug drawer opens", hud.Panels.IsOpen);
        Screenshot("Legacy-tools");
        Time.timeScale = 1f;
    }

    private static IEnumerator Arena()
    {
        SkirmishMatch match = Object.FindAnyObjectByType<SkirmishMatch>();
        while (match == null || match.Round == 0)
        {
            yield return .1f;
            match = Object.FindAnyObjectByType<SkirmishMatch>();
        }

        yield return .4f;
        CombatHud hud = Object.FindAnyObjectByType<CombatHud>();
        Check(
            "5v5 uses one shared HUD",
            hud != null && Object.FindObjectsByType<CombatHud>(FindObjectsSortMode.None).Length == 1);
        Check(
            "Both teams participate in nameplates",
            hud.Candidates.Any(unit => unit.faction == UnitFaction.Player) && hud.Candidates.Any(unit => unit.faction == UnitFaction.Enemy));
        Time.timeScale = 0f;
        int count = match.Hud.KillMessages;
        Durability victim = match.CockpitAt(5).GetComponent<Durability>();
        ControlUnit attacker = match.CockpitAt(0).GetComponentInParent<ControlUnit>();
        victim.ApplyDamage(999999, attacker);
        victim.ApplyDamage(999999, attacker);
        yield return .3f;
        Check(
            "5v5 shared feed receives one attributed kill",
            match.Hud.KillMessages == count + 1 && hud.Notifications.TotalMessages == count + 1);
        Check("5v5 continues after enemy death", PlayManager.instance.playMode && match.RedAlive == 4);
        ThrusterInfoPanel thrusters = hud.GetComponentInChildren<ThrusterInfoPanel>();
        thrusters.BindOwner(attacker, "AI");
        hud.Panels.ToggleThrusters();
        Thruster source = attacker.Thrusters.First(item => item != null);
        int category = source is MainThruster ? 0 : source is UniversalThruster ? 1 : 2;
        thrusters.SelectCategory(category);
        yield return .5f;
        TMPro.TMP_Text value = thrusters.GetComponentsInChildren<TMPro.TMP_Text>().FirstOrDefault(label => label.name == "Value" && label.text.Contains("PWR"));
        Check("AI thrusters remain visible without player input ownership", value != null);
        Check(
            "Thrust text reads real cached actuator output",
            value.text.Contains($"{source.thrust:0.0} / {source.maxThrust:0.0}"));
        Screenshot("Arena-kill");
        Time.timeScale = 1f;
    }

    private static IEnumerator Exploration()
    {
        while (BuildManager.instance == null || BuildManager.instance.IsLoadingBlocks)
        {
            yield return .1f;
        }

        GameModeController modes = PlayManager.instance.Modes;
        modes.ShowSelection();
        modes.SelectExploration();
        while (modes.State == GameModeState.Loading)
        {
            yield return .1f;
        }

        Check("Exploration starts through normal mode selection", modes.State == GameModeState.Exploring);
        CombatHud hud = MainUIPanels.instance.CombatHud;
        Check(
            "Exploration keeps Main-owned shared HUD active",
            hud.isActiveAndEnabled && hud.Battlefield == PlayManager.instance.WorldSession.gameObject.scene);
        hud.ShowPlayerKill("PRESENTATION PROBE");
        yield return .3f;
        Check("Exploration celebration uses shared notifications", hud.Notifications.TotalMessages == 1);
        hud.Panels.ToggleDebug();
        yield return .2f;
        Check("Exploration debug drawer opens", hud.Panels.IsOpen);
        hud.Panels.ToggleThrusters();
        yield return .3f;
        Check("Exploration thrust drawer opens", MainUIPanels.instance.ThrusterInfoPanel.IsOpen);
        Screenshot("Exploration-tools");
        modes.ReturnToBuild();
        while (modes.State == GameModeState.Returning)
        {
            yield return .1f;
        }

        Check(
            "Exploration returns to build with drawers closed",
            modes.State == GameModeState.Building && !hud.Panels.IsOpen);
    }

    private static void Screenshot(string name)
    {
        Directory.CreateDirectory("Temp/SharedCombatHud");
        ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/SharedCombatHud/" + name + ".png"));
    }

    private static void Check(string name, bool passed)
    {
        Status = name;
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Time.timeScale = 1f;
        Directory.CreateDirectory("Temp/SharedCombatHud");
        Status = error == null ? "passed " + mode : "failed " + mode;
        File.WriteAllText(
            "Temp/SharedCombatHud/" + mode + ".json",
            JsonUtility.ToJson(new Report { status = Status, checks = checks, error = error }, true));
        EditorApplication.isPlaying = false;
    }

    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks;
        public string error;
    }
}

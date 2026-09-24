using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class CombatHudPlayProbe
{
    private const string Pending = "HY.CombatHudProbe";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static IEnumerator _scenario;
    private static double _nextStep;
    private static double _deadline;

    static CombatHudPlayProbe() { EditorApplication.playModeStateChanged += OnPlayModeChanged; }

    [MenuItem("Tools/HY-Sandbox/Validate Combat HUD")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity")
            throw new InvalidOperationException("Open Main before validating combat HUD.");
        Directory.CreateDirectory("Temp/CombatHud");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear();
            Errors.Clear();
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!(behaviour is UnityEngine.EventSystems.UIBehaviour) && !(behaviour is CombatHud))
                    behaviour.enabled = false;
            _scenario = Scenario();
            _nextStep = 0;
            _deadline = EditorApplication.timeSinceStartup + 30;
            EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Pending, false);
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("Combat HUD probe timed out.");
            if (EditorApplication.timeSinceStartup < _nextStep) return;
            if (!_scenario.MoveNext()) { Finish(); return; }
            _nextStep = EditorApplication.timeSinceStartup + (_scenario.Current is float delay ? delay : 0.1f);
        }
        catch (Exception error) { Errors.Add(error.ToString()); Finish(); }
    }

    private static IEnumerator Scenario()
    {
        MainUIPanels panels = MainUIPanels.instance;
        CombatHud hud = panels.CombatHud;
        Check("Combat HUD is bound under PlayPanel", hud != null && hud.transform.IsChildOf(panels.playPanel.transform));
        panels.buildPanel.SetActive(false);
        panels.playPanel.SetActive(true);
        PlayManager.instance.playMode = true;
        Camera camera = PlayManager.instance.mainCamera;
        Check("Play camera is available", camera != null);

        ControlUnit attacker = new GameObject("Probe Player").AddComponent<ControlUnit>();
        attacker.enabled = false;
        attacker.faction = UnitFaction.Player;

        ControlUnit enemy = CreateEnemy("Probe Raider", camera.transform.position + camera.transform.forward * 20f);
        yield return 0.4f;
        RectTransform plate = hud.transform.Find("Enemy Nameplate") as RectTransform;
        Check("Enemy nameplate is created", plate != null && plate.gameObject.activeInHierarchy);
        Check("Enemy identity survives runtime grouping", plate.Find("Name").GetComponent<Text>().text == "Probe Raider");
        Check("Aggregate health is displayed", plate.Find("Value").GetComponent<Text>().text == "80 / 100");
        ScreenCapture.CaptureScreenshot("Temp/CombatHud/EnemyNameplate.png");
        yield return 0.6f;

        SerializedObject hudSettings = new SerializedObject(hud);
        hudSettings.FindProperty("_killDuration").floatValue = 8f;
        hudSettings.ApplyModifiedPropertiesWithoutUndo();
        enemy.cockpit.GetComponent<Durability>().ApplyDamage(100f, attacker);
        yield return 0.1f;
        RectTransform notice = hud.transform.Find("KillNotice") as RectTransform;
        Check("Player cockpit kill shows notice", notice.gameObject.activeInHierarchy);
        Check("Kill notice uses enemy identity", notice.Find("EnemyName").GetComponent<Text>().text == "Probe Raider");
        Check("Kill count increments once", notice.Find("Count").GetComponent<Text>().text == "KILLS  01");
        ScreenCapture.CaptureScreenshot("Temp/CombatHud/KillNotice.png");
        yield return 6f;

        ControlUnit environmental = CreateEnemy("Collision Victim", camera.transform.position + camera.transform.forward * 22f);
        environmental.cockpit.GetComponent<Durability>().UpdateDurablility(-100f);
        Check("Environment damage does not count as player kill", notice.Find("Count").GetComponent<Text>().text == "KILLS  01");
        yield return 0.6f;
    }

    private static ControlUnit CreateEnemy(string name, Vector3 position)
    {
        GameObject root = new GameObject(name);
        root.transform.position = position;
        ControlUnit unit = root.AddComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = UnitFaction.Enemy;
        GameObject cockpitObject = new GameObject("Cockpit");
        cockpitObject.transform.SetParent(root.transform, false);
        Cockpit cockpit = cockpitObject.AddComponent<Cockpit>();
        cockpit.faction = UnitFaction.Enemy;
        cockpitObject.AddComponent<EnemyIdentity>().SetDisplayName(name);
        Durability durability = cockpitObject.AddComponent<Durability>();
        durability.enabled = false;
        durability.currentDurability = 80f;
        durability.enabled = true;
        durability.currentDurability = 80f;
        unit.cockpit = cockpit;
        unit.cockpits = new[] { cockpit };
        unit.hasValidCockpit = true;
        PlayManager.instance.RegisterControlUnit(unit);
        return unit;
    }

    private static void Check(string name, bool passed)
    {
        if (!passed) throw new InvalidOperationException(name);
        Checks.Add(name);
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        File.WriteAllText("Temp/CombatHud/Validation.json", JsonUtility.ToJson(new Report
        {
            passed = Errors.Count == 0,
            checks = Checks.ToArray(),
            errors = Errors.ToArray(),
            unity = Application.unityVersion
        }, true));
        EditorApplication.isPlaying = false;
    }

    [Serializable]
    private sealed class Report
    {
        public bool passed;
        public string unity;
        public string[] checks;
        public string[] errors;
    }
}

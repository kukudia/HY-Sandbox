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
                if (!(behaviour is UnityEngine.EventSystems.UIBehaviour) && !(behaviour is CombatHud)
                    && !(behaviour is MainUIPanels) && !(behaviour is ThrusterInfoPanel))
                    behaviour.enabled = false;
            foreach (Collider collider in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                collider.enabled = false;
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
        hud.SendMessage("RefreshEnemies");
        hud.SendMessage("LateUpdate");
        RectTransform plate = hud.transform.Find("Enemy Nameplate") as RectTransform;
        Check("Enemy nameplate is created", plate != null);
        Vector3 labelPoint = enemy.cockpit.transform.position + Vector3.up * 2.5f;
        Vector3 projected = camera.WorldToScreenPoint(labelPoint);
        Check($"Enemy nameplate is visible ({projected.x:0},{projected.y:0},{projected.z:0}; {Screen.width}x{Screen.height})",
            plate.gameObject.activeInHierarchy);
        Check("Enemy identity survives runtime grouping", plate.Find("Name").GetComponent<Text>().text == "Probe Raider");
        Check("Aggregate health is displayed", plate.Find("Value").GetComponent<Text>().text == "120 / 150");
        Check("Enemy dual bars show unit and cockpit health",
            plate.Find("CockpitValue").GetComponent<Text>().text == "80 / 100"
            && Mathf.Abs(((RectTransform)plate.Find("Bar/Fill")).anchorMax.x - 0.8f) < 0.01f
            && Mathf.Abs(((RectTransform)plate.Find("CockpitBar/Fill")).anchorMax.x - 0.8f) < 0.01f);
        Transform detachedBlock = enemy.transform.Find("Probe Armor");
        detachedBlock.SetParent(null);
        enemy.TryGetTotalDurability(out float detachedCurrent, out float detachedMaximum);
        Check("Detached block leaves current unit health", detachedCurrent == 80f && detachedMaximum == 100f);
        hud.SendMessage("RefreshEnemies");
        string detachedHealth = plate.Find("Value").GetComponent<Text>().text;
        Check($"Spawn maximum survives block detachment ({detachedHealth})", detachedHealth == "80 / 150");
        StatusIcon statusIcon = enemy.cockpit.gameObject.AddComponent<StatusIcon>();
        DebugManager.instance.showDurabilityStatus = true;
        statusIcon.durabilityStatus = DurabilityStatus.Damaged;
        statusIcon.RefreshStatusIcons();
        yield return 0.1f;
        statusIcon.SendMessage("LateUpdate");
        CanvasGroup iconGroup = DebugManager.instance.StatusIconRoot.Find("Cockpit Status Icons")?.GetComponent<CanvasGroup>();
        Check("Status icon appears with visibility fade", iconGroup != null && iconGroup.alpha > 0f);
        ScreenCapture.CaptureScreenshot("Temp/CombatHud/EnemyNameplate.png");
        yield return 0.6f;

        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = "Nameplate Occluder";
        Vector3 target = enemy.cockpit.transform.position + Vector3.up * 2.5f;
        blocker.transform.position = Vector3.Lerp(camera.transform.position, target, 0.5f);
        blocker.transform.localScale = Vector3.one * 3f;
        Physics.SyncTransforms();
        Check("Occluder intersects camera ray", blocker.GetComponent<Collider>().Raycast(
            new Ray(camera.transform.position, (target - camera.transform.position).normalized),
            out RaycastHit blockerHit, Vector3.Distance(camera.transform.position, target)));
        RaycastHit[] hits = new RaycastHit[512];
        int hitCount = Physics.RaycastNonAlloc(camera.transform.position,
            (target - camera.transform.position).normalized, hits,
            Vector3.Distance(camera.transform.position, target) - 0.01f,
            ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
        Check($"Physics query includes occluder ({hitCount} hits)",
            System.Array.Exists(hits, hit => hit.collider == blocker.GetComponent<Collider>()));
        RaycastHit[] shortHits = new RaycastHit[64];
        int shortCount = Physics.RaycastNonAlloc(camera.transform.position,
            (target - camera.transform.position).normalized, shortHits,
            Vector3.Distance(camera.transform.position, target) - 0.01f,
            ~((1 << 2) | (1 << 5)), QueryTriggerInteraction.Ignore);
        Check($"HUD ray buffer includes occluder ({shortCount} hits)",
            System.Array.Exists(shortHits, hit => hit.collider == blocker.GetComponent<Collider>()));
        bool blocked = (bool)typeof(CombatHud).GetMethod("IsOccluded", System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic).Invoke(hud, new object[] { target, enemy });
        Check("HUD occlusion query sees geometry", blocked);
        float iconAlpha = iconGroup.alpha;
        statusIcon.SendMessage("LateUpdate");
        Check("3D geometry fades status icon", iconGroup.alpha < iconAlpha);
        float visibleAlpha = plate.GetComponent<CanvasGroup>().alpha;
        hud.SendMessage("LateUpdate");
        Check("3D geometry fades nameplate", plate.GetComponent<CanvasGroup>().alpha < visibleAlpha);
        yield return 0.3f;
        hud.SendMessage("LateUpdate");
        Check("Occluded nameplate disappears", plate.GetComponent<CanvasGroup>().alpha <= 0.01f);
        blocker.SetActive(false);
        Physics.SyncTransforms();
        yield return 0.1f;
        hud.SendMessage("LateUpdate");
        Check("Nameplate fades back when geometry clears", plate.gameObject.activeSelf
            && plate.GetComponent<CanvasGroup>().alpha > 0f);
        statusIcon.SendMessage("LateUpdate");
        Check("Status icon returns when geometry clears", iconGroup.gameObject.activeSelf);

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

        ControlUnit player = CreatePlayer(camera.transform.position + camera.transform.forward * 12f);
        PlayManager.instance.blocksParent = player.transform;
        typeof(PlayManager).GetField("maxHealth")?.SetValue(PlayManager.instance, 150f);
        yield return 0.4f;
        typeof(MainUIPanels).GetField("_nextHealthRefresh", System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic).SetValue(panels, 0f);
        panels.SendMessage("Update");
        Check($"Player HUD totals all unit durability ({panels.healthValue.text})",
            panels.healthValue.text == "120 / 150");
        Check("Player cockpit bar shows its own durability",
            panels.healthValue.transform.parent.Find("CockpitBarValue").GetComponent<Text>().text == "80 / 100");
        Text telemetry = panels.healthValue.transform.parent.Find("FlightTelemetry")?.GetComponent<Text>();
        Check("Flight telemetry uses Canvas", player.hoverFlightController.IsUsedByControlUnit
            && telemetry != null && telemetry.text.Contains("TARGET HEIGHT"));
        MainThruster main = player.GetComponentInChildren<MainThruster>();
        main.GetComponent<Power>().currentPower = 100f;
        main.thrust = 50f;
        ThrusterInfoPanel thrusterPanel = panels.ThrusterInfoPanel;
        Check("Thruster panel is bound", thrusterPanel != null);
        thrusterPanel.Toggle();
        yield return 0.4f;
        main.thrust = 50f;
        thrusterPanel.SelectCategory(0);
        thrusterPanel.SendMessage("Update");
        Check("F1 panel shows owned main thruster", thrusterPanel.IsOpen
            && thrusterPanel.transform.Find("Window/Viewport/Content/Thruster Row") != null);
        RectTransform fill = thrusterPanel.transform.Find("Window/Viewport/Content/Thruster Row/Track/Fill") as RectTransform;
        Check("Thrust bar matches force ratio", fill != null && Mathf.Abs(fill.anchorMax.x - 0.5f) < 0.01f);
        ScreenCapture.CaptureScreenshot("Temp/CombatHud/ThrusterInfoPanel.png");
        yield return 0.6f;
        thrusterPanel.SelectCategory(1);
        yield return 0.3f;
        thrusterPanel.SendMessage("Update");
        Check("Navigation filters thruster types", thrusterPanel.transform.Find("Window/Viewport/EmptyState").gameObject.activeSelf);
        thrusterPanel.Close();
        yield return 0.6f;
    }

    private static ControlUnit CreatePlayer(Vector3 position)
    {
        GameObject root = new GameObject("Probe Player Unit");
        root.transform.position = position;
        ControlUnit unit = root.AddComponent<ControlUnit>();
        unit.enabled = false;
        unit.faction = UnitFaction.Player;
        GameObject cockpitObject = new GameObject("Cockpit");
        cockpitObject.transform.SetParent(root.transform, false);
        Cockpit cockpit = cockpitObject.AddComponent<Cockpit>();
        cockpit.faction = UnitFaction.Player;
        Durability cockpitHealth = cockpitObject.AddComponent<Durability>();
        cockpitHealth.currentDurability = 80f;
        GameObject block = new GameObject("Probe Thruster");
        block.transform.SetParent(root.transform, false);
        Durability blockHealth = block.AddComponent<Durability>();
        blockHealth.maxDurability = 50f;
        blockHealth.currentDurability = 40f;
        MainThruster thruster = block.AddComponent<MainThruster>();
        thruster.controlUnit = unit;
        thruster.maxThrust = 100f;
        unit.cockpit = cockpit;
        unit.cockpits = new[] { cockpit };
        unit.hasValidCockpit = true;
        GameObject hoverObject = new GameObject("Hover Controller");
        hoverObject.transform.SetParent(root.transform, false);
        HoverFlightController hover = hoverObject.AddComponent<HoverFlightController>();
        HoverThruster hoverThruster = hoverObject.AddComponent<HoverThruster>();
        unit.hoverFlightController = hover;
        hover.thrusters = new[] { hoverThruster };
        hover.Init();
        return unit;
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
        EnemyIdentity identity = cockpitObject.AddComponent<EnemyIdentity>();
        identity.SetDisplayName(name);
        Durability durability = cockpitObject.AddComponent<Durability>();
        durability.enabled = false;
        durability.currentDurability = 80f;
        durability.enabled = true;
        durability.currentDurability = 80f;
        if (name == "Probe Raider")
        {
            GameObject armor = new GameObject("Probe Armor");
            armor.transform.SetParent(root.transform, false);
            Durability armorHealth = armor.AddComponent<Durability>();
            armorHealth.maxDurability = 50f;
            armorHealth.currentDurability = 40f;
            identity.SetSpawnMaxHealth(150f);
        }
        else identity.SetSpawnMaxHealth(100f);
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

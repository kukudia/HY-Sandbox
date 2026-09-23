using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Exercises the saved Main UI in a disposable Play Mode copy, without scenario writes to saves.</summary>
[InitializeOnLoad]
public static class BuildPalettePlayProbe
{
    private const string Pending = "HY.BuildPaletteProbe";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static IEnumerator _scenario;
    private static double _resume;
    private static double _deadline;
    private static bool _background;
    private static Mouse _probeMouse;

    static BuildPalettePlayProbe() { EditorApplication.playModeStateChanged += StateChanged; }

    [MenuItem("Tools/Build Palette/Run Play Mode validation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity") throw new InvalidOperationException("Open Main before testing.");
        Directory.CreateDirectory("Temp/BuildPalette");
        if (File.Exists("Temp/BuildPalette/Validation.json")) File.Delete("Temp/BuildPalette/Validation.json");
        SessionState.SetBool(Pending, true); EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            Checks.Clear(); Errors.Clear(); _background = Application.runInBackground; Application.runInBackground = true;
            // Awake establishes manager references. Stop gameplay updates and pending load/camera coroutines during assertions.
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!(behaviour is UIBehaviour) && !(behaviour is MainUIButtons) && !(behaviour is BuildPalette) && !(behaviour is BuildPaletteItem) && !(behaviour is ConnectorPlacementHints)) { behaviour.enabled = false; behaviour.StopAllCoroutines(); }
            Application.logMessageReceived += Capture;
            _scenario = Scenario(); _resume = 0; _deadline = EditorApplication.timeSinceStartup + 95; EditorApplication.update += Tick;
        }
        if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Pending, false);
    }
    private static void Capture(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Errors.Add(message + "\n" + stack); }
    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("Build palette probe timeout.");
            if (EditorApplication.timeSinceStartup < _resume) return;
            if (!_scenario.MoveNext()) { Finish(); return; }
            _resume = EditorApplication.timeSinceStartup + (_scenario.Current is float delay ? delay : 0.15f);
        }
        catch (Exception error) { Errors.Add(error.ToString()); Finish(); }
    }
    private static void Finish()
    {
        if (_probeMouse != null) { InputSystem.RemoveDevice(_probeMouse); _probeMouse = null; }
        EditorApplication.update -= Tick; Application.logMessageReceived -= Capture; Application.runInBackground = _background;
        File.WriteAllText("Temp/BuildPalette/Validation.json", JsonUtility.ToJson(new Report { passed = Errors.Count == 0, checks = Checks.ToArray(), errors = Errors.ToArray(), unity = Application.unityVersion }, true));
        EditorApplication.isPlaying = false;
    }
    [Serializable] private class Report { public bool passed; public string unity; public string[] checks; public string[] errors; }
    private static void Check(string name, bool passed) { if (!passed) throw new InvalidOperationException(name); Checks.Add(name); }
    private static void Invoke(object target, string name) { target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null); }

    private static void MovePointer(Vector2 position)
    {
        // Direct test-device state also works when the Editor Game view is not focused.
        InputSystem.EnableDevice(_probeMouse);
        InputSystem.QueueStateEvent(_probeMouse, new MouseState { position = position });
        InputSystem.Update();
        InputState.Change(_probeMouse, new MouseState { position = position }, InputUpdateType.Dynamic);
        _probeMouse.MakeCurrent();
        EventSystem.current.currentInputModule.Process();
    }

    private static Block RangeBlock(Vector3 position)
    {
        Block block = Object.Instantiate(Resources.Load<GameObject>("Blocks/1x1x1")).GetComponent<Block>();
        block.transform.position = position;
        foreach (Connector connector in block.connectors) { connector.canConnect = true; connector.isConnected = false; }
        return block;
    }

    private static float HintAlpha(ConnectorPlacementHints hints)
    {
        var entries = (IDictionary)typeof(ConnectorPlacementHints).GetField("_hints", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(hints);
        float highest = 0f;
        foreach (DictionaryEntry entry in entries)
            highest = Mathf.Max(highest, (float)entry.Value.GetType().GetField("alpha").GetValue(entry.Value));
        return highest;
    }

    private static IEnumerator Scenario()
    {
        var ui = Object.FindFirstObjectByType<MainUIButtons>(FindObjectsInactive.Include);
        ui.gameObject.SetActive(true);
        foreach (Transform panel in ui.transform) panel.gameObject.SetActive(panel.name == "BuildPanel");
        yield return 0.5f;
        ui.enabled = false;
        var palette = ui.GetComponentInChildren<BuildPalette>();
        var items = palette.GetComponentsInChildren<BuildPaletteItem>(true);
        Check("Saved catalog contains all 23 buildable Prefabs, no Bot/Connector", items.Length == Resources.LoadAll<GameObject>("Blocks").Count(p => p.GetComponent<Block>() != null && p.name != "Connector") && items.All(i => i.BlockName != "Bot" && i.BlockName != "Connector"));
        Check("New salvage blocks have saved icon buttons", new[] { "CargoHold", "CoinHold", "TechnologyHold", "CollectionBotContainer" }.All(n => items.Any(i => i.BlockName == n)));
        Check("Every preview has a sprite and no persistent text label", items.All(i => i.transform.Find("Preview").GetComponent<Image>().sprite != null && i.GetComponentInChildren<Text>(true) == null));
        var tooltip = palette.transform.Find("HoveredBlockName").GetComponent<Text>();
        Check("Tooltip hidden by default", !tooltip.gameObject.activeSelf);
        for (int category = 0; category < 6; category++)
        {
            palette.transform.Find("CategoryNavigation").GetChild(category).GetComponent<Button>().onClick.Invoke();
            Check("Category " + category + " filters exact set", items.All(i => i.gameObject.activeSelf == (category == 0 || i.Category == category)));
        }
        var cargo = items.Single(i => i.BlockName == "CargoHold");
        var pointer = new PointerEventData(EventSystem.current);
        ExecuteEvents.Execute(cargo.gameObject, pointer, ExecuteEvents.pointerEnterHandler);
        Check("Pointer hover displays name", tooltip.gameObject.activeSelf && tooltip.text == "CargoHold");
        ScreenCapture.CaptureScreenshot("Temp/BuildPalette/Salvage-hover.png"); yield return 0.5f;
        ExecuteEvents.Execute(cargo.gameObject, pointer, ExecuteEvents.pointerExitHandler);
        Check("Pointer exit clears name", !tooltip.gameObject.activeSelf);
        cargo.GetComponent<Button>().onClick.Invoke();
        Check("Icon click selects correct resource", BuildManager.instance.currentBlockResourcePath == "Blocks/CargoHold");
        palette.SelectCategory(0); yield return 0.2f;
        ScreenCapture.CaptureScreenshot("Temp/BuildPalette/All-blocks.png"); yield return 0.5f;
        var scroll = palette.GetComponent<ScrollRect>();
        Check("Overflow content scrolls within viewport", scroll.content.rect.height > scroll.viewport.rect.height);
        scroll.verticalNormalizedPosition = 0;
        palette.SelectCategory(2);
        Check("Category resets scroll to top", Mathf.Abs(scroll.content.anchoredPosition.y) < 0.01f);

        if (GameManager.instance.blocksParent != null) GameManager.instance.blocksParent.gameObject.SetActive(false);
        var target = Object.Instantiate(Resources.Load<GameObject>("Blocks/2x2x2")); target.name = "Connector probe";
        target.transform.position = new Vector3(0, 3, 0); target.transform.rotation = Quaternion.Euler(20, 35, 10);
        Block block = target.GetComponent<Block>();
        foreach (Connector connector in block.connectors) { connector.canConnect = true; connector.isConnected = false; }
        block.connectors[0].canConnect = false; block.connectors[1].isConnected = true;
        var hints = BuildManager.instance.GetComponent<ConnectorPlacementHints>();
        var camera = BuildManager.instance.mainCamera;
        camera.transform.position = target.transform.position + new Vector3(4, 3, -5); camera.transform.LookAt(target.transform.position);
        Physics.SyncTransforms();
        hints.Show(camera, BuildManager.instance.BuildRange, BuildManager.instance.blockLayer); yield return 0.35f;
        Check("Hints exclude disabled and occupied connectors", hints.VisibleCount == block.connectors.Count - 2);
        var serialized = new SerializedObject(hints); Mesh mesh = (Mesh)serialized.FindProperty("_outlineMesh").objectReferenceValue;
        Check("Outline measures 0.9 by 0.9 world units", Mathf.Abs(mesh.bounds.size.x * serialized.FindProperty("_size").floatValue - 0.9f) < 0.001f && Mathf.Abs(mesh.bounds.size.y * serialized.FindProperty("_size").floatValue - 0.9f) < 0.001f);
        palette.SelectCategory(1); yield return 0.3f;
        ScreenCapture.CaptureScreenshot("Temp/BuildPalette/Connector-hints.png"); yield return 0.5f;
        BuildManager.instance.SetBuildMode(false); yield return 0.35f;
        Check("Leaving build mode clears hints even without ghost", hints.VisibleCount == 0);
        _probeMouse = InputSystem.AddDevice<Mouse>("BuildPaletteProbeMouse");
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        BuildManager.instance.currentBlockResourcePath = "Blocks/1x1x1";
        Physics.SyncTransforms();
        string pointerDiagnostic = "mouse=" + Mouse.current.position.ReadValue() + ", screen=" + Screen.width + "x" + Screen.height + ", UI=" + EventSystem.current.IsPointerOverGameObject();
        Ray diagnosticRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        pointerDiagnostic += ", hit=" + (Physics.Raycast(diagnosticRay, out RaycastHit diagnosticHit, 100f, BuildManager.instance.blockLayer) ? diagnosticHit.collider.name : "none");
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.2f;
        Check("World hover creates ghost and connector hints (ghost=" + (BuildManager.instance.currentGhost != null) + ", hints=" + hints.VisibleCount + ", " + pointerDiagnostic + ")", BuildManager.instance.currentGhost != null && hints.VisibleCount == block.connectors.Count - 2);
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        foreach (Connector connector in block.connectors) connector.canConnect = false;
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.35f;
        Check("No eligible connector clears stale ghost and hints", BuildManager.instance.currentGhost == null && hints.VisibleCount == 0);
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        foreach (Connector connector in block.connectors) connector.canConnect = true;
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.2f;
        Check("Preview recovers when connector becomes available", BuildManager.instance.currentGhost != null);
        var buttonRect = items.First(i => i.gameObject.activeInHierarchy).GetComponent<RectTransform>();
        Vector2 buttonPosition = RectTransformUtility.WorldToScreenPoint(null, buttonRect.position);
        MovePointer(buttonPosition);
        yield return 0.05f;
        Check("EventSystem detects palette hover", EventSystem.current.IsPointerOverGameObject());
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.2f;
        Check("UI hover blocks placement while retaining nearby hints", BuildManager.instance.currentGhost == null && hints.AvailableCount > 0);
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        hints.Show(camera, BuildManager.instance.BuildRange, BuildManager.instance.blockLayer); Object.Destroy(target); yield return 0.35f;
        Check("Destroyed target clears hints", hints.VisibleCount == 0);
        // Invalid paths used to leave a stale preview or throw. This also exercises the production entrypoint.
        BuildManager.instance.currentBlockResourcePath = "Blocks/MissingProbeResource";
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Invoke(BuildManager.instance, "HandleBuildingPreview");
        Check("Invalid resource safely clears preview", BuildManager.instance.currentGhost == null && BuildManager.instance.hoveredConnector == null);

        target = Object.Instantiate(Resources.Load<GameObject>("Blocks/2x2x2"));
        target.transform.SetPositionAndRotation(new Vector3(0, 3, 0), Quaternion.identity);
        block = target.GetComponent<Block>();
        camera.transform.position = new Vector3(0, 9, 0);
        camera.transform.LookAt(target.transform.position, Vector3.forward);
        BuildManager.instance.SetCurrentBlockResource("Blocks/2x2x2");
        yield return 0.1f;
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Physics.SyncTransforms();
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.2f;
        Block largeGhost = BuildManager.instance.currentGhost != null
            ? BuildManager.instance.currentGhost.GetComponent<Block>() : null;
        Connector largeTarget = BuildManager.instance.hoveredConnector;
        Vector3 largeTargetNormal = largeTarget != null ? block.GetConnectorWorldNormal(largeTarget) : Vector3.zero;
        float expectedHalfExtent = largeGhost != null && largeTarget != null
            ? Mathf.Abs(Vector3.Dot(largeTargetNormal, largeGhost.transform.rotation * Vector3.right)) * largeGhost.x * 0.5f
                + Mathf.Abs(Vector3.Dot(largeTargetNormal, largeGhost.transform.rotation * Vector3.up)) * largeGhost.y * 0.5f
                + Mathf.Abs(Vector3.Dot(largeTargetNormal, largeGhost.transform.rotation * Vector3.forward)) * largeGhost.z * 0.5f
            : -1f;
        float actualOffset = largeGhost != null && largeTarget != null
            ? Vector3.Dot(largeGhost.transform.position - block.GetConnectorWorldPosition(largeTarget), largeTargetNormal)
            : -1f;
        Color largePreviewColor = largeGhost != null && largeGhost.GetComponentInChildren<MeshRenderer>() != null
            ? largeGhost.GetComponentInChildren<MeshRenderer>().material.color : Color.red;
        int availableTargetCount = block.connectors.Count(block.IsConnectorAvailableForPlacement);
        Ray largeRay = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
        bool largeRayHit = Physics.Raycast(largeRay, out RaycastHit largeHit, BuildManager.instance.BuildRange + Vector3.Distance(largeRay.origin, camera.transform.position), BuildManager.instance.blockLayer);
        Check("2x2x2 preview uses rotated half extent and remains placeable (ghost=" + (largeGhost != null)
            + ", target=" + (largeTarget != null) + ", expected=" + expectedHalfExtent.ToString("F3")
            + ", actual=" + actualOffset.ToString("F3") + ", color=" + largePreviewColor
            + ", available=" + availableTargetCount + ", ray=" + largeRayHit + ", hit=" + (largeRayHit ? largeHit.collider.name : "none")
            + ", resource=" + BuildManager.instance.currentBlockResourcePath + ")", largeGhost != null && largeTarget != null
            && largeGhost.x == 2 && largeGhost.y == 2 && largeGhost.z == 2
            && Mathf.Abs(actualOffset - expectedHalfExtent) < 0.001f
            && largePreviewColor.g > largePreviewColor.r);
        Vector3 targetPoint = block.GetConnectorWorldPosition(largeTarget);
        Check("2x2x2 connectors coincide on all axes without half-cell drift", largeGhost.connectors.Any(c => c.canConnect
            && Vector3.Distance(largeGhost.GetConnectorWorldPosition(c), targetPoint) < 0.001f
            && Vector3.Dot(largeGhost.GetConnectorWorldNormal(c), -largeTargetNormal) > 0.999f));
        Check("2x2x2 top preview X/Z stay on integer offsets", Mathf.Abs(largeGhost.transform.position.x - Mathf.Round(largeGhost.transform.position.x)) < 0.001f
            && Mathf.Abs(largeGhost.transform.position.z - Mathf.Round(largeGhost.transform.position.z)) < 0.001f);
        BuildManager.instance.SetBuildMode(false);
        Object.Destroy(target);
        yield return 0.1f;

        Block connectionA = RangeBlock(new Vector3(30, 0, 0));
        Block connectionB = RangeBlock(new Vector3(31, 0, 0));
        Physics.SyncTransforms();
        connectionA.CheckConnection();
        bool initiallyConnected = connectionA.connectors.Any(c => c.isConnected) && connectionB.connectors.Any(c => c.isConnected);
        foreach (Connector connector in connectionB.connectors) connector.canConnect = false;
        connectionA.CheckConnection();
        bool disabledOppositeRejected = !connectionA.connectors.Any(c => c.isConnected) && !connectionB.connectors.Any(c => c.isConnected);
        foreach (Connector connector in connectionB.connectors) connector.canConnect = true;
        connectionB.transform.position = new Vector3(33, 0, 0);
        connectionA.CheckConnection();
        bool disconnectedOnMove = !connectionA.connectors.Any(c => c.isConnected) && !connectionB.connectors.Any(c => c.isConnected);
        connectionB.transform.position = new Vector3(31, 0, 0);
        Physics.SyncTransforms();
        connectionB.CheckConnection();
        bool reconnected = connectionA.connectors.Any(c => c.isConnected) && connectionB.connectors.Any(c => c.isConnected);
        Check("CheckConnection rejects disabled Connector on either side", initiallyConnected && disabledOppositeRejected);
        Check("CheckConnection refreshes both sides after move and reconnection", disconnectedOnMove && reconnected);

        List<List<Block>> connectedGroups = BlockGroupManager.GroupBlocks(new List<Block> { connectionA, connectionB });
        bool groupedWhenConnected = connectedGroups.Count == 1;
        foreach (Connector connector in connectionB.connectors) connector.canConnect = false;
        Physics.SyncTransforms();
        connectionA.CheckConnection();
        List<List<Block>> disabledOppositeGroups = BlockGroupManager.GroupBlocks(new List<Block> { connectionA, connectionB });
        bool splitWhenOppositeDisabled = disabledOppositeGroups.Count == 2;
        foreach (Connector connector in connectionB.connectors) connector.canConnect = true;
        foreach (Connector connector in connectionA.connectors) connector.canConnect = false;
        Physics.SyncTransforms();
        connectionB.CheckConnection();
        List<List<Block>> disabledCurrentGroups = BlockGroupManager.GroupBlocks(new List<Block> { connectionA, connectionB });
        bool splitWhenCurrentDisabled = disabledCurrentGroups.Count == 2;
        Check("GroupBlocks follows valid two-sided Connector connections", groupedWhenConnected
            && splitWhenOppositeDisabled && splitWhenCurrentDisabled);
        Object.Destroy(connectionA.gameObject); Object.Destroy(connectionB.gameObject);

        // Three separate blocks prove discovery is independent of the hovered block and camera ray.
        camera.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.identity);
        var near = RangeBlock(new Vector3(0, 10, 6));
        var adjacent = RangeBlock(new Vector3(4, 10, 8));
        var far = RangeBlock(new Vector3(0, 10, 17));
        adjacent.gameObject.AddComponent<BoxCollider>(); // Multiple colliders must not duplicate the six connectors.
        Physics.SyncTransforms();
        serialized.FindProperty("_fadeInSeconds").floatValue = 1f;
        serialized.FindProperty("_fadeOutSeconds").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        BuildManager.instance.currentBlockResourcePath = "Blocks/1x1x1";
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.9f));
        Invoke(BuildManager.instance, "HandleBuildingPreview");
        yield return 0.12f;
        float earlyAlpha = HintAlpha(hints);
        Check("Hint appearance interpolates through partial opacity", earlyAlpha > 0f && earlyAlpha < 1f);
        Check("Entire camera neighborhood shows both nearby blocks exactly once", hints.AvailableCount == 12);
        yield return 1.1f;
        Check("Hint fade-in reaches full opacity", HintAlpha(hints) > 0.99f);
        ScreenCapture.CaptureScreenshot("Temp/BuildPalette/Range-hints.png"); yield return 0.3f;
        Check("15-unit sphere includes boundary and rejects outside", BuildManager.instance.IsWithinBuildRange(camera.transform.position + Vector3.forward * 15f) && !BuildManager.instance.IsWithinBuildRange(camera.transform.position + Vector3.forward * 15.01f));
        near.gameObject.SetActive(false);
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Invoke(BuildManager.instance, "HandleBuildingPreview");
        Check("Distant block cannot create a preview", BuildManager.instance.currentGhost == null);
        near.gameObject.SetActive(true);
        camera.transform.position += Vector3.forward * 5f;
        yield return 0.3f;
        Check("Moving camera discovers newly reachable block", hints.AvailableCount == 18);
        camera.transform.position = new Vector3(0, 10, 0);
        near.gameObject.SetActive(false); adjacent.gameObject.SetActive(false);
        far.transform.position = new Vector3(0, 10, 14.5f);
        foreach (Connector connector in far.connectors) connector.canConnect = connector.normal == Vector3.forward;
        Physics.SyncTransforms();
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Invoke(BuildManager.instance, "HandleBuildingPreview");
        Check("Boundary connector cannot place its snapped center beyond 15 units", BuildManager.instance.currentGhost != null
            && !BuildManager.instance.IsWithinBuildRange(BuildManager.instance.currentGhost.transform.position)
            && BuildManager.instance.currentGhost.GetComponentInChildren<MeshRenderer>().material.color.r > 0.9f
            && BuildManager.instance.currentGhost.GetComponentInChildren<MeshRenderer>().material.color.g < 0.1f);
        BuildManager.instance.SetBuildMode(false);

        hints.Hide(); yield return 0.12f;
        Check("Disappearance retains partially transparent outlines", hints.AvailableCount == 0 && hints.VisibleCount > 0 && HintAlpha(hints) > 0f && HintAlpha(hints) < 1f);
        yield return 1.1f;
        Check("Fade-out releases all expired connector entries", hints.VisibleCount == 0 && HintAlpha(hints) == 0f);
        Object.Destroy(near.gameObject); Object.Destroy(adjacent.gameObject); Object.Destroy(far.gameObject);
        yield return 0.2f;

        var panels = ui.GetComponent<MainUIPanels>();
        panels.playPanel.SetActive(true); panels.buildPanel.SetActive(false);
        var health = (RectTransform)panels.playPanel.transform.Find("CockpitHealthBar");
        var healthFill = health.Find("HealthBar/Fill").GetComponent<Image>();
        var cockpit = new GameObject("Health value probe").AddComponent<Cockpit>();
        foreach (float value in new[] { 0f, 50f, 100f })
        {
            panels.UpdateHealthBar(cockpit.gameObject, value, 100f);
            Check("Health fill " + value + "% uses normalized amount", Mathf.Abs(healthFill.fillAmount - value / 100f) < 0.001f);
            if (value == 100f) Check("Full health is green", healthFill.color.g > healthFill.color.r);
            if (value == 0f) Check("Empty health is red", healthFill.color.r > healthFill.color.g);
        }
        panels.UpdateHealthBar(cockpit.gameObject, 1f, 0f);
        Check("Zero maximum health never yields NaN", healthFill.fillAmount == 0f);
        panels.UpdateHealthBar(cockpit.gameObject, 50f, 100f);
        cockpit.faction = UnitFaction.Enemy;
        panels.UpdateHealthBar(cockpit.gameObject, 10f, 100f);
        Check("Enemy damage cannot overwrite player health", Mathf.Abs(healthFill.fillAmount - 0.5f) < 0.001f);
        Check("Health HUD is anchored at bottom left", health.anchorMin == Vector2.zero && health.anchorMax == Vector2.zero && health.pivot == Vector2.zero);
        Check("Health gauge no longer uses moving scrollbar handles", health.GetComponentInChildren<Scrollbar>(true) == null && healthFill.type == Image.Type.Filled);
        ScreenCapture.CaptureScreenshot("Temp/BuildPalette/Health-HUD.png"); yield return 0.3f;
        Object.Destroy(cockpit.gameObject);

    }
}

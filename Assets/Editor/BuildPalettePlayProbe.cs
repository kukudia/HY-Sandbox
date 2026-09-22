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
            _scenario = Scenario(); _resume = 0; _deadline = EditorApplication.timeSinceStartup + 65; EditorApplication.update += Tick;
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

        var target = Object.Instantiate(Resources.Load<GameObject>("Blocks/2x2x2")); target.name = "Connector probe";
        target.transform.position = new Vector3(0, 3, 0); target.transform.rotation = Quaternion.Euler(20, 35, 10);
        Block block = target.GetComponent<Block>();
        foreach (Connector connector in block.connectors) { connector.canConnect = true; connector.isConnected = false; }
        block.connectors[0].canConnect = false; block.connectors[1].isConnected = true;
        var hints = BuildManager.instance.GetComponent<ConnectorPlacementHints>();
        hints.Show(block); yield return 0.2f;
        Check("Hints exclude disabled and occupied connectors", hints.VisibleCount == block.connectors.Count - 2);
        var serialized = new SerializedObject(hints); Mesh mesh = (Mesh)serialized.FindProperty("_outlineMesh").objectReferenceValue;
        Check("Outline measures 0.9 by 0.9 world units", Mathf.Abs(mesh.bounds.size.x * serialized.FindProperty("_size").floatValue - 0.9f) < 0.001f && Mathf.Abs(mesh.bounds.size.y * serialized.FindProperty("_size").floatValue - 0.9f) < 0.001f);
        var camera = BuildManager.instance.mainCamera;
        camera.transform.position = target.transform.position + new Vector3(4, 3, -5); camera.transform.LookAt(target.transform.position);
        palette.SelectCategory(1); yield return 0.3f;
        ScreenCapture.CaptureScreenshot("Temp/BuildPalette/Connector-hints.png"); yield return 0.5f;
        BuildManager.instance.SetBuildMode(false); yield return 0.2f;
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
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.2f;
        Check("No eligible connector clears stale ghost and hints", BuildManager.instance.currentGhost == null && hints.VisibleCount == 0);
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        foreach (Connector connector in block.connectors) connector.canConnect = true;
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.2f;
        Check("Preview recovers when connector becomes available", BuildManager.instance.currentGhost != null);
        var buttonRect = items.First(i => i.gameObject.activeInHierarchy).GetComponent<RectTransform>();
        Vector2 buttonPosition = RectTransformUtility.WorldToScreenPoint(null, buttonRect.position);
        MovePointer(buttonPosition);
        Check("EventSystem detects palette hover", EventSystem.current.IsPointerOverGameObject());
        Invoke(BuildManager.instance, "HandleBuildingPreview"); yield return 0.2f;
        Check("UI hover blocks world preview and placement path", BuildManager.instance.currentGhost == null && hints.VisibleCount == 0);
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        hints.Show(block); Object.Destroy(target); yield return 0.3f;
        Check("Destroyed target clears hints", hints.VisibleCount == 0);
        // Invalid paths used to leave a stale preview or throw. This also exercises the production entrypoint.
        BuildManager.instance.currentBlockResourcePath = "Blocks/MissingProbeResource";
        MovePointer(new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
        Invoke(BuildManager.instance, "HandleBuildingPreview");
        Check("Invalid resource safely clears preview", BuildManager.instance.currentGhost == null && BuildManager.instance.hoveredConnector == null);
    }
}

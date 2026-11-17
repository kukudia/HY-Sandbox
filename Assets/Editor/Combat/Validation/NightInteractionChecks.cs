// 职责：通过真实Siege生命周期与跨帧InputSystem事件回归夜班交互、筛选和重开。
// 入口：RunLive()；只操作Play实例，结束恢复硬件与背景输入设置，不写玩家蓝图。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class NightInteractionChecks
{
    private static IEnumerator routine;
    private static double next;
    private static double deadline;
    private static readonly List<string> checks = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static Mouse mouse;
    private static Keyboard keyboard;
    private static Mouse physicalMouse;
    private static Keyboard physicalKeyboard;
    private static bool mouseEnabled;
    private static bool keyboardEnabled;
    private static InputSettings.BackgroundBehavior previousBackground;
    private static InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
    public static string Status { get; private set; } = "idle";

    public static void RunLive()
    {
        if (!EditorApplication.isPlaying || routine != null || PlayManager.instance.Modes.State != GameModeState.Building)
        {
            throw new InvalidOperationException("Start Build in Play Mode and wait for loading first.");
        }

        checks.Clear();
        errors.Clear();
        Application.logMessageReceived += Capture;
        physicalMouse = Mouse.current;
        physicalKeyboard = Keyboard.current;
        mouseEnabled = physicalMouse != null && physicalMouse.enabled;
        keyboardEnabled = physicalKeyboard != null && physicalKeyboard.enabled;
        if (mouseEnabled)
        {
            InputSystem.DisableDevice(physicalMouse);
        }

        if (keyboardEnabled)
        {
            InputSystem.DisableDevice(physicalKeyboard);
        }

        previousBackground = InputSystem.settings.backgroundBehavior;
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        mouse = InputSystem.AddDevice<Mouse>("Night interaction mouse");
        keyboard = InputSystem.AddDevice<Keyboard>("Night interaction keys");
        mouse.MakeCurrent();
        keyboard.MakeCurrent();
        Application.runInBackground = true;
        routine = Scenario();
        deadline = EditorApplication.timeSinceStartup + 150;
        next = 0;
        Status = "running";
        EditorApplication.update += Tick;
    }

    private static IEnumerator Scenario()
    {
        while (BuildManager.instance.IsLoadingBlocks)
        {
            yield return .1f;
        }

        GameModeController modes = PlayManager.instance.Modes;
        modes.LaunchSiege(null);
        while (modes.State == GameModeState.Loading)
        {
            yield return .1f;
        }

        SiegeGameplayStage stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
        Check("Production Siege deployed", stage != null && stage.Base.IsAlive);
        TextAsset finch = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
        BlockDataList finchData = JsonUtility.FromJson<BlockDataList>(finch.text);
        stage.DefenderOptions[0] = new SiegeShipOption("Interaction Finch", finch.text, 5, finchData.blocks.Count);
        bool deployed = false;
        for (int option = 0; option < stage.DefenderOptions.Length && !deployed; option++)
        {
            deployed = stage.TryDeployAt(option, 0, out _);
        }

        Check("Real friendly deployment", deployed);
        SiegeFleetEntry ship = stage.Fleet.Ships.First(item => item != null && item.IsAlive);
        Check(
            "Friendly name uses actual design",
            ship.Cockpit.GetComponent<EnemyIdentity>().DisplayName == ship.DisplayName);
        Check("Wave begins", stage.BeginWave());
        while (stage.FlightGuidance.IsGuiding(ship))
        {
            yield return .1f;
        }

        Check(
            "Launch completed before player handover: alive=" + ship.IsAlive + " phase=" + stage.Match.Phase + " guidance=" + stage.FlightGuidance.LastFailureReason,
            ship.IsAlive && stage.Match.Phase == SiegePhase.Battle);
        Camera camera = PlayManager.instance.mainCamera;
        camera.transform.position = ship.Cockpit.transform.position + new Vector3(0, 4, -18);
        camera.transform.LookAt(ship.Cockpit.transform);
        yield return .1f;
        Vector2 point = camera.WorldToScreenPoint(ship.Cockpit.transform.position);
        Check(
            "Click ray reaches friendly hull",
            Physics.Raycast(
                camera.ScreenPointToRay(point),
                out RaycastHit hit,
                100f,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<ControlUnit>() == ship.Unit);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return .05f;
        float firstClickAt = Time.unscaledTime;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        yield return .03f;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return .03f;
        GameObject clickShield = new GameObject(
            "Double click UI boundary fixture",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(GraphicRaycaster));
        try
        {
            Canvas canvas = clickShield.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32000;
            GameObject image = new GameObject("Click target", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(clickShield.transform, false);
            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            yield return .03f;
            Check("UI boundary fixture intercepts pointer", EventSystem.current.IsPointerOverGameObject());
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
            yield return .03f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
            yield return .03f;
        }
        finally
        {
            Object.DestroyImmediate(clickShield);
        }

        yield return .03f;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        yield return .03f;
        Check("UI interruption tested within double-click time window", Time.unscaledTime - firstClickAt <= .35f);
        Check("UI click breaks pending hull double click", stage.Fleet.Driven == null);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return .05f;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        yield return .05f;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        yield return .05f;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
        yield return .05f;
        Check(
            "Double click grants unique player control",
            stage.Fleet.Driven == ship && ship.Cockpit.ControlMode == UnitControlMode.Player && InputManager.instance.keyInput);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
        stage.Observe();
        yield return .1f;
        Check(
            "Observe restores spectator and AI",
            stage.Fleet.Driven == null && ship.Cockpit.ControlMode == UnitControlMode.Autonomous && !InputManager.instance.keyInput);
        DebugManager debug = DebugManager.instance;
        CombatHudPanels panels = stage.GetComponentInChildren<SiegeCombatHud>().Hud.Panels;
        panels.ToggleDebug();
        yield return .3f;
        var panelData = new SerializedObject(panels);
        Dropdown dropdown = (Dropdown)panelData.FindProperty("factionFilter").objectReferenceValue;
        Check("Faction selector has all three teams", dropdown != null && dropdown.options.Count == 3);
        dropdown.value = 2;
        yield return .2f;
        Check(
            "Enemy filter excludes friendly ship",
            debug.Filter == DebugManager.FactionFilter.Enemy && !debug.Includes(ship.Cockpit));
        dropdown.value = 1;
        yield return .2f;
        Check(
            "Friendly filter includes friendly ship",
            debug.Filter == DebugManager.FactionFilter.Friendly && debug.Includes(ship.Cockpit));
        Toggle trace = (Toggle)panelData.FindProperty("debugToggles").GetArrayElementAtIndex(5).objectReferenceValue;
        ExecuteEvents.Execute(
            trace.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
        yield return .4f;
        NavigationTrack[] tracks = debug.GetComponentsInChildren<NavigationTrack>();
        Check(
            "Trace toggle samples actual friendly flight",
            debug.showFlightTrajectories && tracks.Any(item => item.Owner == ship.Unit && item.TracePoints > 0));
        dropdown.value = 2;
        yield return .3f;
        Check("Changing faction releases excluded tracks", !tracks.Any(item => item.Owner == ship.Unit));
        dropdown.value = 0;
        yield return .3f;
        Check("All teams restores friendly track", tracks.Any(item => item.Owner == ship.Unit));
        panels.ClearDebug();
        panels.CloseAll();
        yield return .2f;
        Check("Disabled traces release all slots", tracks.All(item => item.Owner == null));
        Check("Can take control again", stage.TryTakeControl(ship.Id));
        ship.Cockpit.GetComponent<Durability>().ApplyDamage(999999, null);
        yield return .3f;
        Check(
            "Pilot death returns to observer",
            stage.Fleet.Driven == null && !InputManager.instance.keyInput && Object.FindAnyObjectByType<SiegeObserverCamera>().IsActive);
        stage.Base.Core.GetComponent<Durability>().ApplyDamage(999999, null);
        yield return 1.8f;
        Check(
            "Failure exposes Restart",
            stage.ShowFailure && stage.GetComponentInChildren<SiegeCombatHud>().transform.Find("Siege Failure/Restart").gameObject.activeInHierarchy);
        SiegeGameplayStage previous = stage;
        Button restart = stage.GetComponentInChildren<SiegeCombatHud>().transform.Find("Siege Failure/Restart").GetComponent<Button>();
        ExecuteEvents.Execute(
            restart.gameObject,
            new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left },
            ExecuteEvents.pointerClickHandler);
        yield return .2f;
        while (modes.State == GameModeState.Returning || modes.State == GameModeState.Loading)
        {
            yield return .1f;
        }

        stage = Object.FindAnyObjectByType<SiegeGameplayStage>();
        Check(
            "Failure Restart creates a fresh same-mode base",
            stage != null && stage != previous && stage.Base.IsAlive && stage.Match.Wave == 0 && modes.State == GameModeState.Siege);
        modes.ReturnToBuild();
        while (modes.State == GameModeState.Returning)
        {
            yield return .1f;
        }

        Check(
            "Exit restores building mode",
            modes.State == GameModeState.Building && BuildManager.instance.enabled && !PauseMenu.IsPaused);
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

    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < next)
        {
            return;
        }

        try
        {
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException(Status);
            }

            if (!routine.MoveNext())
            {
                Finish(null);
                return;
            }

            next = EditorApplication.timeSinceStartup + (routine.Current is float delay ? delay : .03f);
        }
        catch (Exception error)
        {
            Finish(error.ToString());
        }
    }

    private static void Finish(string error)
    {
        EditorApplication.update -= Tick;
        (routine as IDisposable)?.Dispose();
        routine = null;
        Application.logMessageReceived -= Capture;
        if (errors.Count > 0)
        {
            error = (error ?? string.Empty) + string.Join("\n", errors);
        }

        if (mouse != null && mouse.added)
        {
            InputSystem.RemoveDevice(mouse);
        }

        if (keyboard != null && keyboard.added)
        {
            InputSystem.RemoveDevice(keyboard);
        }

        if (mouseEnabled && physicalMouse != null && physicalMouse.added)
        {
            InputSystem.EnableDevice(physicalMouse);
            physicalMouse.MakeCurrent();
        }

        if (keyboardEnabled && physicalKeyboard != null && physicalKeyboard.added)
        {
            InputSystem.EnableDevice(physicalKeyboard);
            physicalKeyboard.MakeCurrent();
        }

        InputSystem.settings.backgroundBehavior = previousBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
        Status = error == null ? "passed" : "failed";
        Directory.CreateDirectory("Temp/NightDevelopment");
        File.WriteAllText(
            "Temp/NightDevelopment/interactions.json",
            JsonUtility.ToJson(new Report { checks = checks, error = error }, true));
        EditorApplication.isPlaying = false;
    }

    private static void Capture(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            errors.Add(message + "\n" + trace);
        }
    }

    [Serializable]
    private sealed class Report
    {
        public List<string> checks;
        public string error;
    }
}

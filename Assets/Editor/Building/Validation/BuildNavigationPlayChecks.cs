// 职责：跨真实帧验证工坊右键长按、UI捕获、方向键移动和失焦释放，不写个人存档。
// 入口：RunLive()；完成恢复输入设备/设置，在Temp记录观察结果。
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

public static class BuildNavigationPlayChecks
{
    private static IEnumerator routine;
    private static double next;
    private static double deadline;
    private static readonly List<string> checks = new List<string>();
    private static Mouse mouse;
    private static Keyboard keyboard;
    public static string Status { get; private set; } = "idle";

    public static void RunLive()
    {
        if (!EditorApplication.isPlaying || routine != null || PlayManager.instance?.Modes?.State != GameModeState.Building || BuildManager.instance.IsLoadingBlocks)
        {
            throw new InvalidOperationException("Requires a loaded Build Play session.");
        }

        checks.Clear();
        routine = Scenario();
        deadline = EditorApplication.timeSinceStartup + 60;
        next = 0;
        Status = "running";
        EditorApplication.update += Tick;
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
                throw new TimeoutException("Input boundary probe stopped or exceeded 60s.");
            }

            if (routine.MoveNext())
            {
                next = EditorApplication.timeSinceStartup + (routine.Current is float delay ? delay : .01f);
                return;
            }

            Status = "passed";
        }
        catch (Exception error)
        {
            Status = "failed";
            checks.Add(error.ToString());
        }

        EditorApplication.update -= Tick;
        (routine as IDisposable)?.Dispose();
        routine = null;
        Directory.CreateDirectory("Temp/NightDevelopment");
        File.WriteAllLines("Temp/NightDevelopment/build-navigation.txt", new[] { Status }.Concat(checks));
    }

    private static IEnumerator Scenario()
    {
        Mouse physicalMouse = Mouse.current;
        Keyboard physicalKeys = Keyboard.current;
        bool mouseEnabled = physicalMouse != null && physicalMouse.enabled;
        bool keysEnabled = physicalKeys != null && physicalKeys.enabled;
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputManager input = InputManager.instance;
        CameraController camera = CameraController.instance;
        BuildManager build = BuildManager.instance;
        Pose initial = new Pose(camera.transform.position, camera.transform.rotation);
        BuildType tool = build.currentBuildType;
        var inputData = new SerializedObject(input);
        float hold = inputData.FindProperty("buildNavigationHoldSeconds").floatValue;
        try
        {
            if (mouseEnabled)
            {
                InputSystem.DisableDevice(physicalMouse);
            }

            if (keysEnabled)
            {
                InputSystem.DisableDevice(physicalKeys);
            }

            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            mouse = InputSystem.AddDevice<Mouse>("Build navigation mouse");
            keyboard = InputSystem.AddDevice<Keyboard>("Build navigation keys");
            mouse.MakeCurrent();
            keyboard.MakeCurrent();
            inputData.FindProperty("buildNavigationHoldSeconds").floatValue = .2f;
            inputData.ApplyModifiedPropertiesWithoutUndo();
            input.EnterBuildMode();
            camera.StopCameraMotion();
            Vector2 free = new Vector2(Screen.width * .5f, Screen.height * .5f);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free });
            yield return .1f;
            Check(!EventSystem.current.IsPointerOverGameObject(), "Navigation fixture begins on clear world surface");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free }.WithButton(MouseButton.Right));
            yield return .08f;
            Check(input.lockView && camera.currentMode == CameraMode.Lock, "Short right click keeps editing");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free });
            yield return .1f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free }.WithButton(MouseButton.Right));
            yield return .4f;
            Check(!input.lockView && camera.currentMode == CameraMode.FreeFly, "Long right hold captures navigation");
            Vector3 before = camera.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow));
            yield return .35f;
            Check(
                Vector3.Distance(before, camera.transform.position) > 1f,
                "Held RMB plus UpArrow moves the real camera");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free });
            yield return .2f;
            Check(
                input.lockView && camera.currentMode == CameraMode.Lock && build.currentBuildType == tool,
                "Release restores editing and the same tool");
            Vector3 released = camera.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.UpArrow));
            yield return .2f;
            Check(
                (camera.transform.position - released).sqrMagnitude < .000001f,
                "Arrow without RMB does not move workshop camera");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            RectTransform button = MainUIButtons.instance.moveButton.GetComponent<RectTransform>();
            Vector2 ui = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
            InputSystem.QueueStateEvent(mouse, new MouseState { position = ui });
            yield return .15f;
            Check(EventSystem.current.IsPointerOverGameObject(), "Right hold fixture hits actual toolbar UI");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = ui }.WithButton(MouseButton.Right));
            yield return .4f;
            Check(input.lockView, "Right hold starting on toolbar remains editing");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free }.WithButton(MouseButton.Right));
            yield return .3f;
            Check(input.lockView, "Dragging held UI right button into world does not capture");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free });
            yield return .15f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free }.WithButton(MouseButton.Right));
            yield return .4f;
            Check(!input.lockView, "Fresh world hold captures after UI release");
            input.SendMessage("OnApplicationFocus", false);
            yield return .15f;
            Check(
                input.lockView && camera.currentMode == CameraMode.Lock,
                "Focus loss releases an active capture even while right is held");
            input.SendMessage("OnApplicationFocus", true);
            yield return .3f;
            Check(input.lockView, "Regaining focus requires a physical release before recapture");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free });
            yield return .15f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free }.WithButton(MouseButton.Right));
            yield return .4f;
            Check(!input.lockView, "Fresh right hold after focus recovery captures normally");
            input.SendMessage("OnApplicationPause", true);
            yield return .1f;
            Check(input.lockView, "Application pause also releases workshop navigation");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = free });
            yield return .1f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.B));
            yield return .15f;
            Check(input.lockView && build.currentBuildType == tool, "B no longer toggles workshop building mode");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return .1f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Tab));
            yield return .15f;
            Check(camera.currentMode == CameraMode.Lock, "Tab does not put workshop camera into gameplay view");
        }
        finally
        {
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

            if (keysEnabled && physicalKeys != null && physicalKeys.added)
            {
                InputSystem.EnableDevice(physicalKeys);
                physicalKeys.MakeCurrent();
            }

            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            inputData.FindProperty("buildNavigationHoldSeconds").floatValue = hold;
            inputData.ApplyModifiedPropertiesWithoutUndo();
            input.EnterBuildMode();
            camera.transform.SetPositionAndRotation(initial.position, initial.rotation);
        }
    }

    private static void Check(bool passed, string name)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}

// 职责：在Play验证综合场地、预设起终点、自由相机与暂停期间的真实输入链。
// 入口：Begin()；虚拟输入设备在finally清理，输出保存Temp/EnemyAvoidanceLab。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class EnemyLabViewChecks
{
    public static Task<string> Pending
    {
        get; private set;
    }

    public static void Begin()
    {
        Pending = Run();
    }

    private static async Task<string> Run()
    {
        EnemyAvoidanceLab lab = UnityEngine.Object.FindAnyObjectByType<EnemyAvoidanceLab>();
        if (!Application.isPlaying || lab == null)
        {
            throw new InvalidOperationException("Requires navigation lab Play Mode.");
        }

        var checks = new List<string>();
        Keyboard originalKeyboard = Keyboard.current;
        Mouse originalMouse = Mouse.current;
        Keyboard keyboard = null;
        Mouse mouse = null;
        CursorLockMode previousLock = Cursor.lockState;
        bool previousVisible = Cursor.visible;
        try
        {
            lab.SelectLayout(lab.LayoutCount - 1);
            Check(
                checks,
                "Integrated obstacle district is available",
                lab.LayoutCount >= 12 && lab.LocationCount == 6);
            lab.SetPaused(true);
            for (int i = 0; i < lab.LocationCount; i++)
            {
                Check(checks, "Selectable start " + i, lab.SelectLocation(i, true));
                Vector3 start = lab.Ship.transform.position;
                Check(checks, "Selectable goal " + i, lab.SelectLocation(i, false));
                Vector3 goal = (Vector3)typeof(EnemyAvoidanceLab).GetField("targetPosition", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(lab);
                Check(
                    checks,
                    "Start and target resolve to same intended root location " + i,
                    Vector3.Distance(start, goal + Vector3.up * lab.Ship.GetComponent<EnemyController>().targetHoverHeightOffset) < 0.01f);
            }

            Check(
                checks,
                "Invalid preset selection is rejected",
                !lab.SelectLocation(-1, true) && !lab.SelectLocation(999, false));
            lab.SelectLocation(0, true);
            lab.SelectLocation(1, false);
            EnemyLabCamera view = lab.ViewCamera;
            Check(checks, "Free camera is authored on scene camera", view != null);
            view.SetFreeView(true);
            Check(checks, "Free view uses perspective", view.FreeView && !Camera.main.orthographic);
            keyboard = InputSystem.AddDevice<Keyboard>("LabViewTestKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("LabViewTestMouse");
            Vector3 before = view.transform.position;
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { position = new Vector2(Screen.width * 0.7f, Screen.height * 0.5f), buttons = 2 });
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.E));
            await Task.Delay(450);
            Check(
                checks,
                "Right mouse captures free look",
                view.Looking && Cursor.lockState == CursorLockMode.Locked);
            Check(
                checks,
                "Free camera moves with WASD and elevation while paused",
                Vector3.Distance(before, view.transform.position) > 1f && Time.timeScale == 0f);
            Quaternion rotation = view.transform.rotation;
            InputSystem.QueueDeltaStateEvent(mouse.delta, new Vector2(45f, -15f));
            await Task.Delay(200);
            Check(
                checks,
                "Mouse delta rotates perspective view",
                Quaternion.Angle(rotation, view.transform.rotation) > 1f);
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { position = new Vector2(Screen.width * 0.7f, Screen.height * 0.5f), buttons = 0 });
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(150);
            Check(
                checks,
                "Release returns cursor control to panel",
                !view.Looking && Cursor.lockState == previousLock && Cursor.visible == previousVisible);
            view.ResetOverview();
            Check(checks, "Overview is orthographic and resets pose", !view.FreeView && Camera.main.orthographic);
            lab.StopTrial();
            string timeline = File.ReadAllText(Path.Combine(lab.RecordingDirectory, "timeline.jsonl"));
            Check(
                checks,
                "Camera and endpoint changes are recorded",
                timeline.Contains("camera_free") && timeline.Contains("camera_pose") && timeline.Contains("select_goal"));
            string result = string.Join("\n", checks);
            File.WriteAllText("Temp/EnemyAvoidanceLab/view-checks.txt", result);
            return result;
        }
        finally
        {
            if (lab.ViewCamera != null)
            {
                lab.ViewCamera.ResetOverview();
            }

            if (mouse != null)
            {
                InputSystem.RemoveDevice(mouse);
            }

            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            if (originalKeyboard != null && originalKeyboard.added)
            {
                originalKeyboard.MakeCurrent();
            }

            if (originalMouse != null && originalMouse.added)
            {
                originalMouse.MakeCurrent();
            }

            Cursor.lockState = previousLock;
            Cursor.visible = previousVisible;
            lab.SetPaused(false);
        }
    }

    private static void Check(List<string> checks, string name, bool value)
    {
        checks.Add((value ? "PASS " : "FAIL ") + name);
        if (!value)
        {
            throw new InvalidOperationException(name);
        }
    }
}

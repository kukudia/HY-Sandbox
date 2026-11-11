// 职责：验证试验场蓝图替换、操作记录、暂停恢复、退出落盘和布局切换的完整主链。
// 入口：Begin()；仅在独立EnemyAvoidanceLab的Play Mode运行，结果写入Temp。
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class EnemyLabPlayChecks
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
            throw new InvalidOperationException("Requires lab Play Mode.");
        }

        var checks = new List<string>();
        Keyboard original = Keyboard.current;
        Keyboard keyboard = null;
        Mouse originalMouse = Mouse.current;
        Mouse mouse = null;
        try
        {
            Check(checks, "Eleven editable courses available", lab.LayoutCount >= 11);
            Check(checks, "Read-only catalog includes built-in ships", lab.BlueprintCount >= 3);
            lab.SelectLayout(5);
            lab.SelectBlueprint(0);
            ControlUnit previous = lab.Ship;
            Check(checks, "Selected Finch blueprint deploys", lab.DeploySelected());
            await Task.Delay(350);
            Check(
                checks,
                "Previous test ship is retired",
                previous == null || !previous.gameObject.activeInHierarchy);
            Check(
                checks,
                "Deployment retains exactly test ship and target",
                PlayManager.instance.GetControlUnits().Count == 2);
            string first = lab.RecordingDirectory;
            Check(
                checks,
                "Run writes blueprint and scenario metadata",
                File.Exists(Path.Combine(first, "blueprint.json")) && File.Exists(Path.Combine(first, "metadata.json")));
            lab.SetPaused(true);
            Check(checks, "Pause freezes simulation", Time.timeScale == 0f);
            lab.SetGoal(new Vector3(65f, 5f, 15f));
            keyboard = InputSystem.AddDevice<Keyboard>("NavigationLabTestKeyboard");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            await Task.Delay(200);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            await Task.Delay(200);
            mouse = InputSystem.AddDevice<Mouse>("NavigationLabTestMouse");
            Vector3 screen = Camera.main.WorldToScreenPoint(new Vector3(50f, 0f, 25f));
            lab.BeginPointPick(false);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 1 });
            await Task.Delay(200);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = screen, buttons = 0 });
            await Task.Delay(200);
            Check(
                checks,
                "Mouse point-pick sets world goal",
                Mathf.Abs(lab.Ship.GetComponent<EnemyController>().CaptureNavigation().position.x) < 200f && Mathf.Abs(GetGoal(lab).x - 50f) < 0.2f && Mathf.Abs(GetGoal(lab).z - 25f) < 0.2f);
            lab.SetPaused(false);
            Check(checks, "Resume restores time scale", Time.timeScale > 0f);
            await Task.Delay(350);
            lab.StopTrial();
            string timeline = File.ReadAllText(Path.Combine(first, "timeline.jsonl"));
            Check(
                checks,
                "Keyboard press and release are recorded",
                timeline.Contains("key_down") && timeline.Contains("key_up"));
            Check(
                checks,
                "Mouse press and release are recorded",
                timeline.Contains("mouse_down") && timeline.Contains("mouse_up"));
            Check(
                checks,
                "Pause and goal changes are recorded",
                timeline.Contains("set_goal") && timeline.Contains("pause"));
            Check(
                checks,
                "Navigation log contains applied thrust and route state",
                timeline.Contains("appliedInput") && timeline.Contains("routeRevision"));
            Check(
                checks,
                "Stop saves final result and closes writer",
                File.ReadAllText(Path.Combine(first, "result.json")).Contains("stopped"));
            lab.SelectBlueprint(1);
            await Task.Delay(1100);
            lab.RecordOperation("validation_checkpoint", "flush session operations");
            string operations;
            using (var reader = new StreamReader(new FileStream(
                Path.Combine(Path.GetDirectoryName(first), "operations.jsonl"),
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite)))
            {
                operations = reader.ReadToEnd();
            }

            Check(checks, "Between-trial operations remain in session log", operations.Contains("select_blueprint"));
            Check(checks, "Second blueprint deploys independently", lab.DeploySelected());
            await Task.Delay(200);
            Check(checks, "Next run uses a unique recording folder", lab.RecordingDirectory != first);
            for (int i = 0; i < lab.LayoutCount; i++)
            {
                lab.SelectLayout(i);
                Check(
                    checks,
                    "Layout " + i + " starts a recorded trial",
                    lab.Running && File.Exists(Path.Combine(lab.RecordingDirectory, "metadata.json")));
            }

            lab.StopTrial();
            string result = string.Join("\n", checks);
            Directory.CreateDirectory("Temp/EnemyAvoidanceLab");
            File.WriteAllText("Temp/EnemyAvoidanceLab/field-checks.txt", result);
            return result;
        }
        finally
        {
            if (keyboard != null)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            if (original != null && original.added)
            {
                original.MakeCurrent();
            }

            lab.SetPaused(false);
        }
    }

    private static Vector3 GetGoal(EnemyAvoidanceLab lab)
    {
        return (Vector3)typeof(EnemyAvoidanceLab).GetField(
            "targetPosition",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(lab);
    }

    private static void Check(List<string> checks, string name, bool passed)
    {
        checks.Add((passed ? "PASS " : "FAIL ") + name);
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }
    }
}

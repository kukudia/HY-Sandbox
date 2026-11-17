// 职责：用真实InputSystem和制作Prefab验证所选方块移动、复制、网格及距离边界。
// 入口：RunLive()；仅玩家工坊，临时文件存储，结束恢复缓存、历史、相机和设备。
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
using Object = UnityEngine.Object;

public static class BuildManipulationPlayChecks
{
    private static IEnumerator routine;
    private static double next;
    private static double deadline;
    private static readonly List<string> checks = new List<string>();
    private static readonly List<string> errors = new List<string>();
    private static Keyboard keyboard;
    public static string Status { get; private set; } = "idle";

    public static void RunLive()
    {
        if (!EditorApplication.isPlaying || routine != null || BuildManager.instance == null || BuildManager.instance.IsLoadingBlocks || PlayManager.instance?.Modes?.State != GameModeState.Building || BuildManager.instance.CurrentBuildTarget != BuildTargetKind.PlayerSave)
        {
            throw new InvalidOperationException("Requires an idle, loaded player workshop Play session.");
        }

        checks.Clear();
        errors.Clear();
        Status = "running";
        next = 0;
        deadline = EditorApplication.timeSinceStartup + 120;
        routine = Scenario();
        Application.logMessageReceived += Capture;
        EditorApplication.update += Tick;
    }

    private static void Capture(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            errors.Add(message + "\n" + trace);
        }
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
                throw new TimeoutException("Manipulation probe stopped or exceeded 120s.");
            }

            if (routine.MoveNext())
            {
                next = EditorApplication.timeSinceStartup + (routine.Current is float delay ? delay : .01f);
                return;
            }
        }
        catch (Exception error)
        {
            errors.Add(error.ToString());
        }

        EditorApplication.update -= Tick;
        (routine as IDisposable)?.Dispose();
        routine = null;
        Application.logMessageReceived -= Capture;
        Status = errors.Count == 0 ? "passed" : "failed";
        Directory.CreateDirectory("Temp/NightDevelopment");
        File.WriteAllLines("Temp/NightDevelopment/build-manipulation.txt", new[] { Status }.Concat(checks).Concat(errors));
    }

    private static IEnumerator Scenario()
    {
        BuildManager build = BuildManager.instance;
        SaveManager saves = SaveManager.instance;
        CameraController camera = CameraController.instance;
        Keyboard hardware = Keyboard.current;
        bool enabled = hardware != null && hardware.enabled;
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        Pose pose = new Pose(camera.transform.position, camera.transform.rotation);
        BuildType tool = build.currentBuildType;
        string resource = build.currentBlockResourcePath;
        Block selected = build.selectedBlock;
        Block last = build.lastSaveBlock;
        BlockDataList cache = saves.cachedData;
        FieldInfo storeField = typeof(SaveManager).GetField("_playerFiles", BindingFlags.Instance | BindingFlags.NonPublic);
        object store = storeField.GetValue(saves);
        object history = typeof(ActionManager).GetMethod("CaptureHistory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ActionManager.instance, null);
        object selection = typeof(BuildManager).GetField("_selection", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(build);
        MethodInfo select = selection.GetType().GetMethod("SelectBlock");
        GameObject uiSelection = EventSystem.current.currentSelectedGameObject;
        var made = new List<Block>();
        string directory = Path.GetFullPath(Path.Combine("Temp/NightDevelopment/Manipulation", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        try
        {
            Type storeType = typeof(SaveManager).Assembly.GetType("BlueprintFileStore", true);
            storeField.SetValue(saves, Activator.CreateInstance(storeType, new object[] { directory }));
            saves.cachedData = JsonUtility.FromJson<BlockDataList>(JsonUtility.ToJson(cache));
            string actual = saves.GetSavePath(build.CurrentBuildName);
            Check(
                Path.GetFullPath(actual).StartsWith(directory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "Production writes target only the unique temporary store");
            if (enabled)
            {
                InputSystem.DisableDevice(hardware);
            }

            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            keyboard = InputSystem.AddDevice<Keyboard>("Build manipulation keys");
            keyboard.MakeCurrent();
            EventSystem.current.SetSelectedGameObject(null);
            InputManager.instance.EnterBuildMode();
            camera.StopCameraMotion();
            build.SetBuildType(BuildType.Move);
            foreach (string name in new[]
            {
                "1x1x1",
                "2x1x1",
                "2x2x1",
                "2x2x2"
            }

            )
            {
                foreach (int yaw in new[]
                {
                    0,
                    90
                }

                )
                {
                    float sign = yaw == 0 ? 1f : -1f;
                    Vector3 origin = Vector3.one * 1000f * sign;
                    camera.transform.SetPositionAndRotation(origin, Quaternion.identity);
                    Quaternion rotation = Quaternion.Euler(0, yaw, 0);
                    GameObject prefab = Resources.Load<GameObject>("Blocks/" + name);
                    Block template = prefab.GetComponent<Block>();
                    Vector3 half = Half(template, rotation);
                    Vector3 initial = origin + new Vector3(1, 1, 6) + half;
                    build.CreateBlock(prefab, "Blocks/" + name, initial, rotation);
                    Block block = build.lastSaveBlock;
                    made.Add(block);
                    select.Invoke(selection, new object[] { block });
                    yield return .1f;
                    string fixture = name + " yaw=" + yaw;
                    Check(build.selectedBlock == block, fixture + " selects a real in-range prefab");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                    yield return .15f;
                    Check(
                        Near(block.transform.position, initial + Vector3.forward) && Grid(block),
                        fixture + " W moves one aligned world cell");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return .1f;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Z));
                    yield return .15f;
                    Check(
                        Near(block.transform.position, initial) && Grid(block),
                        fixture + " real Ctrl-Z restores original aligned cell");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return .1f;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Y));
                    yield return .15f;
                    Check(
                        Near(block.transform.position, initial + Vector3.forward) && Grid(block),
                        fixture + " real Ctrl-Y restores aligned move");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return .1f;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.D));
                    yield return .15f;
                    Block duplicate = build.selectedBlock;
                    if (duplicate != null && duplicate != block)
                    {
                        made.Add(duplicate);
                    }

                    Vector3 expected = initial + Vector3.forward + Vector3.right * half.x * 2f;
                    Check(
                        duplicate != null && duplicate != block && Near(duplicate.transform.position, expected) && Grid(duplicate),
                        fixture + " Shift-D duplicates flush without overlap");
                    Check(
                        duplicate.x == block.x && duplicate.y == block.y && duplicate.z == block.z && Quaternion.Angle(duplicate.transform.rotation, rotation) < .01f,
                        fixture + " duplicate preserves dimensions and rotation");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return .1f;
                    camera.transform.position = duplicate.transform.position - Vector3.forward * (build.BuildRange - .2f);
                    Vector3 atLimit = duplicate.transform.position;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W));
                    yield return .15f;
                    Check(Near(duplicate.transform.position, atLimit), fixture + " movement cannot cross build range");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    build.DeselectBlock();
                    foreach (Block item in made.ToArray())
                    {
                        if (item != null)
                        {
                            build.RemoveBlock(item);
                            Object.Destroy(item.gameObject);
                        }
                    }

                    made.Clear();
                    yield return .1f;
                }
            }

            Check(
                File.Exists(saves.GetSavePath(build.CurrentBuildName)),
                "Real production edits persisted inside the temporary store");
        }
        finally
        {
            build.DeselectBlock();
            foreach (Block item in made)
            {
                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }

            saves.cachedData = cache;
            storeField.SetValue(saves, store);
            typeof(ActionManager).GetMethod("RestoreHistory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ActionManager.instance, new[] { history });
            if (keyboard != null && keyboard.added)
            {
                InputSystem.RemoveDevice(keyboard);
            }

            if (enabled && hardware != null && hardware.added)
            {
                InputSystem.EnableDevice(hardware);
                hardware.MakeCurrent();
            }

            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
            InputManager.instance.EnterBuildMode();
            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            build.SetBuildType(tool);
            build.currentBlockResourcePath = resource;
            select.Invoke(selection, new object[] { selected });
            build.lastSaveBlock = last;
            EventSystem.current?.SetSelectedGameObject(uiSelection);
            typeof(BuildManager).GetMethod("RefreshBlueprintUI", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(build, null);
        }
    }

    private static Vector3 Half(Block block, Quaternion rotation)
    {
        Vector3 x = rotation * Vector3.right * block.x;
        Vector3 y = rotation * Vector3.up * block.y;
        Vector3 z = rotation * Vector3.forward * block.z;
        return new Vector3(
            Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
            Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y),
            Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z)) * .5f;
    }

    private static bool Grid(Block block)
    {
        Vector3 min = block.transform.position - Half(block, block.transform.rotation);
        return Near(min, new Vector3(Mathf.Round(min.x), Mathf.Round(min.y), Mathf.Round(min.z)));
    }

    private static bool Near(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < .000001f;
    private static void Check(bool valid, string name)
    {
        if (!valid)
        {
            throw new InvalidOperationException(name);
        }

        checks.Add(name);
    }
}

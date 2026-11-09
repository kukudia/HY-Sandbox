// 职责：通过真实基地放置、已制作按钮和输入管线验证机库尺寸、移动、旋转与六库限制。
// 入口：Run()；从保存的 Main 启动隔离 Play，所有蓝图只写 Temp/Hangars，结束自动退出。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class HangarWorkshopChecks
{
    private const string Pending = "HY.HangarWorkshopChecks";
    private const string Name = "Isolated Hangar Workshop";
    private static IEnumerator scenario;
    private static readonly Stack<IEnumerator> routines = new Stack<IEnumerator>();
    private static Report report;
    private static double deadline;
    private static double nextTick;
    private static Mouse physicalMouse;
    private static Keyboard physicalKeyboard;
    private static Mouse mouse;
    private static Keyboard keyboard;
    private static InputSettings.BackgroundBehavior backgroundBehavior;
    private static bool runInBackground;
    private static string path;
    [Serializable]
    private sealed class Report
    {
        public string status;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }

    static HangarWorkshopChecks()
    {
        EditorApplication.playModeStateChanged += Changed;
    }

    [MenuItem("HY-Sandbox/Siege/Hangars/Validate Base Workshop Interaction")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || SceneManager.sceneCount != 1 || SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity" || SceneManager.GetActiveScene().isDirty)
        {
            throw new InvalidOperationException("Open only saved Main in Edit Mode before validation.");
        }

        Directory.CreateDirectory("Temp/Hangars");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
        }

        if (state == PlayModeStateChange.ExitingPlayMode && scenario != null)
        {
            report.errors.Add("Play Mode ended before the interaction probe completed.");
            Finish();
            return;
        }

        if (state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        report = new Report
        {
            status = "running"
        };
        runInBackground = Application.runInBackground;
        Application.runInBackground = true;
        backgroundBehavior = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        // Stop Start from opening the player's selected vehicle; Awake has already made explicit dependencies.
        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (!(behaviour is UnityEngine.EventSystems.UIBehaviour))
            {
                behaviour.enabled = false;
                behaviour.StopAllCoroutines();
            }
        }

        physicalMouse = Mouse.current;
        physicalKeyboard = Keyboard.current;
        if (physicalMouse != null)
        {
            InputSystem.DisableDevice(physicalMouse);
        }

        if (physicalKeyboard != null)
        {
            InputSystem.DisableDevice(physicalKeyboard);
        }

        mouse = InputSystem.AddDevice<Mouse>("HangarWorkshopMouse");
        keyboard = InputSystem.AddDevice<Keyboard>("HangarWorkshopKeyboard");
        Application.logMessageReceived += Capture;
        scenario = Scenario();
        routines.Clear();
        routines.Push(scenario);
        deadline = EditorApplication.timeSinceStartup + 120d;
        nextTick = 0d;
        EditorApplication.update += Tick;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isPaused)
        {
            return;
        }

        EditorApplication.QueuePlayerLoopUpdate();
        if (EditorApplication.timeSinceStartup < nextTick)
        {
            return;
        }

        nextTick = EditorApplication.timeSinceStartup + 0.05d;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                throw new TimeoutException("Hangar workshop interaction probe timed out.");
            }

            while (routines.Count > 0)
            {
                IEnumerator current = routines.Peek();
                if (!current.MoveNext())
                {
                    (current as IDisposable)?.Dispose();
                    routines.Pop();
                    continue;
                }

                if (current.Current is IEnumerator nested)
                {
                    routines.Push(nested);
                }

                return;
            }

            Finish();
        }
        catch (Exception error)
        {
            report.errors.Add(error.ToString());
            Finish();
        }
    }

    private static IEnumerator Scenario()
    {
        SaveManager saves = SaveManager.instance;
        BuildManager build = BuildManager.instance;
        string folder = Path.GetFullPath("Temp/Hangars/Workshop-" + Guid.NewGuid().ToString("N"));
        Inject(saves, "_playerFiles", Path.Combine(folder, "Saves"));
        Inject(saves, "_enemyFiles", Path.Combine(folder, "Enemies"));
        Inject(saves, "_buildingFiles", Path.Combine(folder, "Buildings"));
        Inject(saves, "_baseFiles", Path.Combine(folder, "Bases"));
        saves.saves.Clear();
        saves.enemyBlueprints.Clear();
        saves.buildingBlueprints.Clear();
        saves.baseBlueprints.Clear();
        saves.blocks.Clear();
        saves.cachedData = new BlockDataList();
        path = saves.GetBaseBlueprintPath(Name);
        var design = new BlockDataList
        {
            foundation = new BuildingFoundationData
            {
                width = 64,
                length = 64
            }
        };
        File.WriteAllText(path, JsonUtility.ToJson(design));
        build.moveCameraDuringBlockLoad = false;
        build.BlockLoadIntervalSeconds = 0f;
        build.EnterBaseBlueprintBuildMode(Name);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        yield return Frames(3);
        Check(
            build.IsEditingBaseBlueprint && build.Foundation != null && saves.cachedData.blocks.Count == 0,
            "isolated base opens with an empty 64m foundation");
        Check(
            Path.GetDirectoryName(path) == Path.Combine(folder, "Bases"),
            "base edits use an isolated base-only file store");
        Camera camera = build.mainCamera;
        CameraController.instance.StopCameraMotion();
        Vector3 aim = build.Foundation.transform.TransformPoint(new Vector3(2.5f, 0f, 2.5f));
        camera.transform.position = aim + new Vector3(0f, 36f, -12f);
        camera.transform.LookAt(aim);
        build.SetCurrentBlockResource("Hangars/Hangar");
        Vector2 pointer = camera.WorldToScreenPoint(aim);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
        yield return Frames(8);
        Check(
            build.currentGhost != null && build.hoveredConnector != null,
            "hangar palette resource creates a real foundation placement preview");
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
        yield return Frames(3);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
        yield return Frames(2);
        HangarModule hangar = GameManager.instance.blocksParent.GetComponentsInChildren<HangarModule>().Single();
        Block block = hangar.GetComponent<Block>();
        Check(
            saves.cachedData.blocks.Count == 1 && Saved(block).resourcePath.Contains("Hangar"),
            "real foundation click places and saves one hangar");
        Check(
            block.Neighbors().Contains(build.Foundation.GetComponent<Block>()),
            "placed floor actually connects to foundation through PhysX");
        build.SetCurrentBlockResource(string.Empty);
        build.SetBuildType(BuildType.Select);
        camera.transform.position = block.transform.position + new Vector3(13f, 10f, -25f);
        camera.transform.LookAt(block.transform.position);
        pointer = camera.WorldToScreenPoint(block.transform.position);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
        yield return Frames(3);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
        yield return Frames(2);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
        yield return Frames(2);
        Check(build.selectedBlock == block, "real pointer click selects the placed hangar");
        HangarFacePanel[] faces = hangar.GetComponentsInChildren<HangarFacePanel>(true);
        HangarFacePanel visible = faces.First(face => face.GetComponent<CanvasGroup>().interactable);
        Check(
            faces.Count(face => face.GetComponent<CanvasGroup>().interactable) >= 1 && faces.Any(face => !face.GetComponent<CanvasGroup>().blocksRaycasts),
            "camera-facing controls accept input while back faces do not intercept it");
        Vector3 bottomBefore = block.transform.position - Vector3.up * block.y * 0.5f;
        for (int axis = 0; axis < 3; axis++)
        {
            Vector3Int before = new Vector3Int(block.x, block.y, block.z);
            ResizeButton(visible, axis, 1).onClick.Invoke();
            yield return Frames(2);
            Vector3Int after = new Vector3Int(block.x, block.y, block.z);
            Check(after[axis] == before[axis] + 1, "authored plus button grows axis " + axis);
            Check(Dimension(Saved(block), axis) == after[axis], "axis " + axis + " change is persisted to base JSON");
            ActionManager.instance.Undo();
            Check(Dimension(Saved(block), axis) == before[axis], "resize Undo restores JSON for axis " + axis);
            ActionManager.instance.Redo();
            Check(Dimension(Saved(block), axis) == after[axis], "resize Redo reapplies JSON for axis " + axis);
            ResizeButton(visible, axis, -1).onClick.Invoke();
            Check(Dimension(Saved(block), axis) == before[axis], "authored minus button shrinks axis " + axis);
        }

        Check(
            Mathf.Approximately(block.transform.position.y - block.y * 0.5f, bottomBefore.y),
            "height adjustments preserve the floor contact plane");
        Check(
            block.Neighbors().Contains(build.Foundation.GetComponent<Block>()),
            "resized floor retains foundation support");
        ResizeButton(visible, 0, 1).onClick.Invoke();
        ResizeButton(visible, 1, 1).onClick.Invoke();
        ResizeButton(visible, 2, 1).onClick.Invoke();
        yield return Frames(2);
        Check(
            block.Neighbors().Contains(build.Foundation.GetComponent<Block>()),
            "odd-size changes retain grid-aligned bottom connections");
        Vector3 oldPosition = block.transform.position;
        build.SetBuildType(BuildType.Move);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return Frames(2);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames(2);
        Check(
            Vector3.Distance(block.transform.position, oldPosition) >= 0.9f,
            "real movement input moves selected hangar");
        Check(
            Vector3.Distance(new Vector3(Saved(block).posX, Saved(block).posY, Saved(block).posZ), block.transform.position) < 0.01f,
            "hangar movement is persisted to JSON");
        Quaternion oldRotation = block.transform.rotation;
        build.SetBuildType(BuildType.Rotate);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
        yield return Frames(2);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames(2);
        Check(
            Mathf.Abs(Quaternion.Angle(oldRotation, block.transform.rotation) - 90f) < 0.01f && Vector3.Dot(block.transform.up, Vector3.up) > .999f,
            "real rotation input turns hangar ninety degrees while retaining upright floor");
        ActionManager.instance.Undo();
        Check(
            Quaternion.Angle(oldRotation, block.transform.rotation) < 0.01f,
            "rotation Undo restores original door heading");
        build.SetBuildType(BuildType.Move);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.D));
        yield return Frames(2);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames(2);
        Check(
            GameManager.instance.blocksParent.GetComponentsInChildren<HangarModule>().Length == 2,
            "shift movement duplicates a hangar through existing construction input");
        Block duplicate = build.selectedBlock;
        Check(
            duplicate != null && duplicate != block && duplicate.x == block.x && duplicate.y == block.y && duplicate.z == block.z,
            "hangar copy preserves resized capacity and has a separate selection");
        for (int index = 2; index < HangarRules.MaximumCount; index++)
        {
            build.CreateBlock(
                Resources.Load<GameObject>("Hangars/Hangar"),
                "Hangars/Hangar",
                new Vector3(10f + (index % 3) * 18f, 4.5f, 32f + (index / 3) * 16f),
                Quaternion.identity);
        }

        Check(
            GameManager.instance.blocksParent.GetComponentsInChildren<HangarModule>().Length == 6 && saves.cachedData.blocks.Count == 6,
            "base stores exactly the configured six independent hangars");
        build.CreateBlock(
            Resources.Load<GameObject>("Hangars/Hangar"),
            "Hangars/Hangar",
            new Vector3(50f, 4.5f, 55f),
            Quaternion.identity);
        Check(
            GameManager.instance.blocksParent.GetComponentsInChildren<HangarModule>().Length == 6 && saves.cachedData.blocks.Count == 6,
            "direct placement refuses a seventh hangar without changing JSON");
        build.SetBuildType(BuildType.Move);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftShift, Key.D));
        yield return Frames(2);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
        yield return Frames(2);
        Check(
            GameManager.instance.blocksParent.GetComponentsInChildren<HangarModule>().Length == 6 && saves.cachedData.blocks.Count == 6,
            "copy input also refuses a seventh hangar");
        // Restore a useful selected view for the captured evidence, then verify the saved dimensions after reload.
        build.SetBuildType(BuildType.Select);
        build.DeselectBlock();
        camera.transform.position = block.transform.position + new Vector3(18f, 13f, -28f);
        camera.transform.LookAt(block.transform.position);
        pointer = camera.WorldToScreenPoint(block.transform.position);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
        yield return Frames(3);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
        yield return Frames(2);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
        yield return Frames(3);
        ScreenCapture.CaptureScreenshot("Temp/Hangars/workshop.png");
        yield return Frames(3);
        string id = block.uniqueId;
        Vector3Int dimensions = new Vector3Int(block.x, block.y, block.z);
        build.EnterBaseBlueprintBuildMode(Name);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        yield return Frames(3);
        Block restored = GameManager.instance.blocksParent.GetComponentsInChildren<Block>().Single(item => item.uniqueId == id);
        Check(
            new Vector3Int(restored.x, restored.y, restored.z) == dimensions && restored.GetComponent<HangarModule>().InnerSize == (Vector3)dimensions - new Vector3(2f, 1f, 2f),
            "reopening the base restores the edited capacity and actual geometry");
        Check(
            GameManager.instance.blocksParent.GetComponentsInChildren<HangarModule>().Length == 6,
            "six-hangar layout survives closing and reopening the base");
    }

    private static IEnumerator Frames(int count)
    {
        // Editor-driven scenario supports nested enumerators through Tick's small explicit stack below.
        for (int i = 0; i < count; i++)
        {
            yield return null;
        }
    }

    private static Button ResizeButton(HangarFacePanel panel, int axis, int delta)
    {
        return panel.transform.Find((delta > 0 ? "Increase " : "Decrease ") + axis).GetComponent<Button>();
    }

    private static BlockData Saved(Block block)
    {
        return JsonUtility.FromJson<BlockDataList>(File.ReadAllText(path)).blocks.Single(data => data.id == block.uniqueId);
    }

    private static int Dimension(BlockData data, int axis)
    {
        return axis == 0 ? data.x : axis == 1 ? data.y : data.z;
    }

    private static void Inject(SaveManager saves, string field, string directory)
    {
        Type type = typeof(SaveManager).Assembly.GetType("BlueprintFileStore", true);
        typeof(SaveManager).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(saves, Activator.CreateInstance(type, new object[] { directory }));
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }

        report.checks.Add(message);
    }

    private static void Capture(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            report.errors.Add(message + "\n" + stack);
        }
    }

    private static void Finish()
    {
        Application.logMessageReceived -= Capture;
        EditorApplication.update -= Tick;
        foreach (IEnumerator routine in routines)
        {
            (routine as IDisposable)?.Dispose();
        }

        routines.Clear();
        scenario = null;
        if (mouse != null)
        {
            InputSystem.RemoveDevice(mouse);
        }

        if (keyboard != null)
        {
            InputSystem.RemoveDevice(keyboard);
        }

        if (physicalMouse != null)
        {
            InputSystem.EnableDevice(physicalMouse);
        }

        if (physicalKeyboard != null)
        {
            InputSystem.EnableDevice(physicalKeyboard);
        }

        InputSystem.settings.backgroundBehavior = backgroundBehavior;
        Application.runInBackground = runInBackground;
        report.status = report.errors.Count == 0 ? "passed" : "failed";
        File.WriteAllText("Temp/Hangars/workshop-checks.json", JsonUtility.ToJson(report, true));
        SessionState.SetBool(Pending, false);
        EditorApplication.isPlaying = false;
    }
}

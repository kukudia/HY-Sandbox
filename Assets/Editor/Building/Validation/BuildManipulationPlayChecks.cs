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
    private static Mouse mouse;
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
        Mouse hardwareMouse = Mouse.current;
        bool mouseEnabled = hardwareMouse != null && hardwareMouse.enabled;
        bool enabled = hardware != null && hardware.enabled;
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        Pose pose = new Pose(camera.transform.position, camera.transform.rotation);
        BuildType tool = build.currentBuildType;
        string resource = build.currentBlockResourcePath;
        Block selected = build.selectedBlock;
        Block last = build.lastSaveBlock;
        BlockDataList cache = saves.cachedData;
        List<Block> registeredBlocks = new List<Block>(saves.blocks);
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
            if (mouseEnabled)
            {
                InputSystem.DisableDevice(hardwareMouse);
            }

            mouse = InputSystem.AddDevice<Mouse>("Build manipulation mouse");
            mouse.MakeCurrent();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .5f, Screen.height * .5f) });
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
                    build.SetBuildType(BuildType.Rotate);
                    Vector3 beforeRotation = block.transform.position;
                    Quaternion expectedRotation = Quaternion.AngleAxis(90f, Vector3.forward) * rotation;
                    Vector3 halfAfterRotation = Half(block, expectedRotation);
                    Vector3 expectedRotationPosition = new Vector3(
                        Mathf.Floor(beforeRotation.x) + Mathf.RoundToInt(halfAfterRotation.x * 2f) % 2 * .5f,
                        Mathf.Floor(beforeRotation.y) + Mathf.RoundToInt(halfAfterRotation.y * 2f) % 2 * .5f,
                        Mathf.Floor(beforeRotation.z) + Mathf.RoundToInt(halfAfterRotation.z * 2f) % 2 * .5f);
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E));
                    yield return .15f;
                    Check(
                        Quaternion.Angle(block.transform.rotation, expectedRotation) < .01f,
                        fixture + " real E rotates around the camera forward axis");
                    Check(
                        Near(block.transform.position, expectedRotationPosition) && Grid(block),
                        fixture + " rotation swaps parity offsets around the integer anchor");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return .1f;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Z));
                    yield return .15f;
                    Check(
                        Near(block.transform.position, beforeRotation) && Quaternion.Angle(block.transform.rotation, rotation) < .01f,
                        fixture + " undo restores both rotation and center");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return .1f;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Y));
                    yield return .15f;
                    Check(
                        Near(block.transform.position, expectedRotationPosition) && Quaternion.Angle(block.transform.rotation, expectedRotation) < .01f,
                        fixture + " redo restores both rotation and swapped center");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    yield return .1f;
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
                    yield return .15f;
                    Check(
                        Near(block.transform.position, beforeRotation) && Quaternion.Angle(block.transform.rotation, rotation) < .01f && Grid(block),
                        fixture + " reverse rotation has no cell drift");
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    build.SetBuildType(BuildType.Move);
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

            Vector3 dragOrigin = Vector3.one * 1500f;
            camera.transform.SetPositionAndRotation(dragOrigin, Quaternion.identity);
            build.CreateBlock(
                Resources.Load<GameObject>("Blocks/1x1x1"),
                "Blocks/1x1x1",
                dragOrigin + new Vector3(.5f, .5f, 6.5f),
                Quaternion.identity);
            Block dragged = build.lastSaveBlock;
            made.Add(dragged);
            select.Invoke(selection, new object[] { dragged });
            yield return .15f;
            MoveAxisHandle handle = build.moveAxis.GetComponentsInChildren<MoveAxisHandle>().Single(item => Vector3.Dot(item.WorldAxis, Vector3.right) > .99f);
            Collider handleCollider = handle.GetComponentInChildren<Collider>();
            Vector3 hitPoint = handleCollider.bounds.center;
            Vector2 pointer = build.mainCamera.WorldToScreenPoint(hitPoint);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            yield return .15f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
            yield return .15f;
            object manipulation = typeof(BuildManager).GetField("_manipulation", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(build);
            FieldInfo active = manipulation.GetType().GetField("activeHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(active.GetValue(manipulation) == handle, "Real axis collider captures left mouse drag");
            Vector3 startPosition = dragged.transform.position;
            string savedBeforeDrag = File.ReadAllText(saves.GetSavePath(build.CurrentBuildName));
            foreach (int cells in new[]
            {
                1,
                2,
                3
            }

            )
            {
                Vector2 movedPointer = build.mainCamera.WorldToScreenPoint(hitPoint + Vector3.right * cells);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = movedPointer }.WithButton(MouseButton.Left));
                yield return .15f;
            }

            Check(
                Vector3.Distance(startPosition, dragged.transform.position) >= 2f && Grid(dragged),
                "Held real mouse drag traverses multiple aligned cells");
            Check(
                File.ReadAllText(saves.GetSavePath(build.CurrentBuildName)) == savedBeforeDrag,
                "Held drag does not write the blueprint per traversed cell");
            Vector3 dragEnd = dragged.transform.position;
            Vector2 releasedPointer = mouse.position.ReadValue();
            InputSystem.QueueStateEvent(mouse, new MouseState { position = releasedPointer });
            yield return .15f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Z));
            yield return .15f;
            Check(
                Near(dragged.transform.position, startPosition),
                "One undo restores the start of the whole axis drag");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return .1f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Y));
            yield return .15f;
            Check(
                Near(dragged.transform.position, dragEnd),
                "One redo restores the final position of the whole drag");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return .1f;
            camera.transform.position = dragged.transform.position - new Vector3(.5f, .5f, 6.5f);
            yield return .1f;
            hitPoint = handleCollider.bounds.center;
            pointer = build.mainCamera.WorldToScreenPoint(hitPoint);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            yield return .15f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
            yield return .15f;
            Check(active.GetValue(manipulation) == handle, "Second real axis gesture begins after redo");
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { position = build.mainCamera.WorldToScreenPoint(hitPoint + Vector3.right * 2) }.WithButton(MouseButton.Left));
            yield return .15f;
            Check(!Near(dragged.transform.position, dragEnd), "Second gesture has an uncommitted live preview");
            string savedAtEnd = File.ReadAllText(saves.GetSavePath(build.CurrentBuildName));
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { position = mouse.position.ReadValue() }.WithButton(MouseButton.Left).WithButton(MouseButton.Right));
            yield return .35f;
            Check(
                !InputManager.instance.lockView && Near(dragged.transform.position, dragEnd) && active.GetValue(manipulation) == null,
                "Navigation cancellation rolls uncommitted drag back to its start");
            Check(
                File.ReadAllText(saves.GetSavePath(build.CurrentBuildName)) == savedAtEnd,
                "Cancelled drag leaves saved blueprint unchanged");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            InputManager.instance.EnterBuildMode();
            yield return .1f;
            foreach (string interruption in new[]
            {
                "focus",
                "pause",
                "disable"
            }

            )
            {
                hitPoint = handleCollider.bounds.center;
                pointer = build.mainCamera.WorldToScreenPoint(hitPoint);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
                yield return .1f;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
                yield return .15f;
                Check(active.GetValue(manipulation) == handle, interruption + " captures a fresh axis drag");
                InputSystem.QueueStateEvent(
                    mouse,
                    new MouseState { position = build.mainCamera.WorldToScreenPoint(hitPoint + Vector3.right * 2) }.WithButton(MouseButton.Left));
                yield return .15f;
                Check(!Near(dragged.transform.position, dragEnd), interruption + " has a live uncommitted position");
                if (interruption == "disable")
                {
                    build.enabled = false;
                }
                else
                {
                    InputManager.instance.SendMessage(interruption == "focus" ? "OnApplicationFocus" : "OnApplicationPause", interruption == "pause");
                }

                yield return .1f;
                Check(
                    Near(dragged.transform.position, dragEnd) && active.GetValue(manipulation) == null,
                    interruption + " rolls the gesture back");
                Check(
                    File.ReadAllText(saves.GetSavePath(build.CurrentBuildName)) == savedAtEnd,
                    interruption + " preserves the committed blueprint");
                build.enabled = true;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
                InputManager.instance.EnterBuildMode();
                yield return .1f;
            }

            Check(
                File.Exists(saves.GetSavePath(build.CurrentBuildName)),
                "Real production edits persisted inside the temporary store");
            build.SetBuildType(BuildType.Rotate);
            camera.transform.position = dragged.transform.position - new Vector3(.5f, .5f, 6.5f);
            yield return .15f;
            RotateAxisHandle rotateHandle = build.rotateAxis.GetComponentsInChildren<RotateAxisHandle>().Single(item => Vector3.Dot(item.WorldAxis, Vector3.forward) > .99f);
            Collider rotateCollider = rotateHandle.GetComponentInChildren<Collider>();
            Vector3 rotationStart = dragged.transform.position;
            Quaternion orientationStart = dragged.transform.rotation;
            Vector3 rotationHit = rotateCollider.bounds.center + Vector3.right * rotateCollider.bounds.extents.x * .5f;
            pointer = build.mainCamera.WorldToScreenPoint(rotationHit);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            yield return .15f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
            yield return .15f;
            FieldInfo activeRotate = manipulation.GetType().GetField("activeRotateHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            Check(activeRotate.GetValue(manipulation) == rotateHandle, "Real rotation collider captures left mouse");
            Check(
                Quaternion.Angle(dragged.transform.rotation, orientationStart) < .01f && Near(dragged.transform.position, rotationStart),
                "Rotation press without pointer movement preserves pose");
            Vector3 projected = (Vector3)manipulation.GetType().GetField("rotateDragStart", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manipulation);
            Vector3 turned = rotationStart + Quaternion.AngleAxis(90f, Vector3.forward) * (projected - rotationStart);
            string savedBeforeRotation = File.ReadAllText(saves.GetSavePath(build.CurrentBuildName));
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { position = build.mainCamera.WorldToScreenPoint(turned) }.WithButton(MouseButton.Left));
            yield return .15f;
            Check(
                Quaternion.Angle(dragged.transform.rotation, Quaternion.AngleAxis(90f, Vector3.forward) * orientationStart) < .01f && Grid(dragged),
                "Real rotation pointer turns one aligned quarter turn");
            Check(
                File.ReadAllText(saves.GetSavePath(build.CurrentBuildName)) == savedBeforeRotation,
                "Held rotation does not persist its preview");
            Vector3 rotatedPosition = dragged.transform.position;
            Quaternion rotatedOrientation = dragged.transform.rotation;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = mouse.position.ReadValue() });
            yield return .15f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Z));
            yield return .15f;
            Check(
                Near(dragged.transform.position, rotationStart) && Quaternion.Angle(dragged.transform.rotation, orientationStart) < .01f,
                "One undo restores rotation and center");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return .1f;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl, Key.Y));
            yield return .15f;
            Check(
                Near(dragged.transform.position, rotatedPosition) && Quaternion.Angle(dragged.transform.rotation, rotatedOrientation) < .01f,
                "One redo restores rotation and center");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return .1f;
            rotationHit = rotateCollider.bounds.center + Vector3.right * rotateCollider.bounds.extents.x * .5f;
            pointer = build.mainCamera.WorldToScreenPoint(rotationHit);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            yield return .1f;
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer }.WithButton(MouseButton.Left));
            yield return .15f;
            Check(activeRotate.GetValue(manipulation) == rotateHandle, "Rotation recaptures after undo/redo");
            projected = (Vector3)manipulation.GetType().GetField("rotateDragStart", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(manipulation);
            turned = rotatedPosition + Quaternion.AngleAxis(90f, Vector3.forward) * (projected - rotatedPosition);
            InputSystem.QueueStateEvent(
                mouse,
                new MouseState { position = build.mainCamera.WorldToScreenPoint(turned) }.WithButton(MouseButton.Left));
            yield return .15f;
            Check(
                Quaternion.Angle(dragged.transform.rotation, rotatedOrientation) > 45f,
                "Rotation has a cancellable preview");
            build.SetBuildType(BuildType.Move);
            Check(
                Near(dragged.transform.position, rotatedPosition) && Quaternion.Angle(dragged.transform.rotation, rotatedOrientation) < .01f,
                "Tool change cancels rotation and restores center");
            InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer });
            // Exercise the real loader separately from the selected-block interaction fixture.
            build.DeselectBlock();
            foreach (Block item in made)
            {
                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }

            made.Clear();
            yield return .1f;
            Transform originalParent = GameManager.instance.blocksParent;
            Transform originalPlayParent = PlayManager.instance.blocksParent;
            List<Block> originalBlocks = new List<Block>(saves.blocks);
            GameManager.instance.blocksParent = null;
            PlayManager.instance.blocksParent = null;
            GameObject loadedParent = null;
            object loader = null;
            try
            {
                BlockDataList invalidDesign = JsonUtility.FromJson<BlockDataList>("{\"blocks\":[" + "{\"id\":\"valid-before\",\"resourcePath\":\"Blocks/1x1x1\",\"x\":1,\"y\":1,\"z\":1,\"posX\":2000.5,\"posY\":2000.5,\"posZ\":2000.5,\"rotW\":1}," + "{\"id\":\"bad-material\",\"resourcePath\":\"Blocks/1x1x1\",\"x\":1,\"y\":1,\"z\":1,\"materialId\":\"unavailable-test-material\",\"rotW\":1}," + "{\"id\":\"bad-hangar\",\"resourcePath\":\"Hangars/Hangar\",\"x\":4,\"y\":4,\"z\":4,\"hangarEntrance\":99,\"rotW\":1}," + "{\"id\":\"valid-after\",\"resourcePath\":\"Blocks/1x1x1\",\"x\":1,\"y\":1,\"z\":1,\"posX\":2003.5,\"posY\":2000.5,\"posZ\":2000.5,\"rotW\":1}]}");
                string path = saves.GetSavePath("Repairable probe");
                string originalJson = JsonUtility.ToJson(invalidDesign);
                File.WriteAllText(path, originalJson);
                Assembly runtime = typeof(BuildManager).Assembly;
                Type loaderType = runtime.GetType("BlueprintLoader");
                loader = Activator.CreateInstance(loaderType, build, saves, GameManager.instance, PlayManager.instance);
                Type contextType = runtime.GetType("BuildTargetContext");
                object context = contextType.GetMethod("PlayerSave").Invoke(null, new object[] { "Repairable probe", "unused" });
                Type settingsType = runtime.GetType("BlueprintLoadSettings");
                object settings = Activator.CreateInstance(settingsType);
                settingsType.GetField("FastLoading").SetValue(settings, true);
                settingsType.GetField("GridSize").SetValue(settings, 1f);
                loaderType.GetMethod("Load").Invoke(loader, new object[] { context, build.blocksParentPrefab, settings, build.mainCamera, null, null });
                loadedParent = GameManager.instance.blocksParent.gameObject;
                while ((bool)loaderType.GetProperty("IsLoading").GetValue(loader))
                {
                    yield return .05f;
                }

                Check(
                    loadedParent.GetComponentsInChildren<Block>().Length == 2,
                    "Repairable loader continues after invalid material and hangar entrance");
                Check(
                    saves.blocks.Any(item => item.uniqueId == "valid-before") && saves.blocks.Any(item => item.uniqueId == "valid-after"),
                    "Repairable loader restores valid modules on both sides of invalid data");
                Check(
                    saves.cachedData.blocks.Count == 4 && saves.cachedData.blocks.Any(item => item.id == "bad-material") && saves.cachedData.blocks.Any(item => item.id == "bad-hangar"),
                    "Repairable loader retains unavailable module snapshots");
                Check(File.ReadAllText(path) == originalJson, "Repairable loader leaves source file unchanged");
                TextAsset finch = Resources.Load<TextAsset>("WorldBlueprints/Enemies/CODEX_E01_Finch");
                BlockDataList brokenUnit = JsonUtility.FromJson<BlockDataList>(finch.text);
                brokenUnit.blocks[1].materialId = "unavailable-test-material";
                ControlUnit refused = ModularUnitFactory.Spawn(
                    brokenUnit,
                    "Invalid material spawn fixture",
                    Vector3.one * 3000f,
                    Quaternion.identity,
                    UnitFaction.Enemy);
                Check(refused == null, "Combat factory rejects a design with an unrestorable material");
                yield return .1f;
                Check(
                    !Object.FindObjectsByType<ControlUnit>(FindObjectsInactive.Include).Any(unit => unit.name == "Invalid material spawn fixture"),
                    "Combat factory removes its partial root after rejection");
            }
            finally
            {
                loader?.GetType().GetMethod("Cancel").Invoke(loader, null);
                if (loadedParent != null)
                {
                    Object.Destroy(loadedParent);
                }

                GameManager.instance.blocksParent = originalParent;
                PlayManager.instance.blocksParent = originalPlayParent;
                saves.blocks.Clear();
                saves.blocks.AddRange(originalBlocks);
            }
        }
        finally
        {
            build.enabled = true;
            build.DeselectBlock();
            foreach (Block item in made)
            {
                if (item != null)
                {
                    Object.Destroy(item.gameObject);
                }
            }

            saves.cachedData = cache;
            saves.blocks.Clear();
            saves.blocks.AddRange(registeredBlocks);
            storeField.SetValue(saves, store);
            typeof(ActionManager).GetMethod("RestoreHistory", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(ActionManager.instance, new[] { history });
            if (mouse != null && mouse.added)
            {
                InputSystem.RemoveDevice(mouse);
            }

            if (mouseEnabled && hardwareMouse != null && hardwareMouse.added)
            {
                InputSystem.EnableDevice(hardwareMouse);
                hardwareMouse.MakeCurrent();
            }

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

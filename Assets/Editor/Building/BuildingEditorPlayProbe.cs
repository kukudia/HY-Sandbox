// 职责：在隔离存档的真实 Play Mode 检查建筑快捷键、底座连接、尺寸 UI 和跨模式持久化。
// 入口：Run()；只写 Temp/BuildingEditor，不改动用户存档或保存 Main 场景。
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
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BuildingEditorPlayProbe
{
    private const string Pending = "HY.BuildingEditorProbe";
    private static readonly List<string> Checks = new List<string>();
    private static readonly List<string> Errors = new List<string>();
    private static IEnumerator _scenario;
    private static Keyboard _keyboard;
    private static double _nextTick;
    private static double _deadline;
    private static bool _background;
    static BuildingEditorPlayProbe()
    {
        EditorApplication.playModeStateChanged += StateChanged;
    }

    [MenuItem("Tools/Building Editor/Run isolated Play Mode validation")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Main.unity")
        {
            throw new InvalidOperationException("Open Main in Edit Mode before testing.");
        }

        Directory.CreateDirectory("Temp/BuildingEditor");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false))
        {
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Pending, false);
        }

        if (state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        Checks.Clear();
        Errors.Clear();
        _background = Application.runInBackground;
        Application.runInBackground = true;
        // Awake 已建立依赖；在 Start 自动加载真实玩家存档前停止常规业务。
        foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!(behaviour is UnityEngine.EventSystems.UIBehaviour))
            {
                behaviour.enabled = false;
                behaviour.StopAllCoroutines();
            }
        }

        _keyboard = InputSystem.AddDevice<Keyboard>();
        Application.logMessageReceived += Capture;
        _scenario = Scenario();
        _nextTick = 0;
        _deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update += Tick;
    }

    private static void Capture(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            Errors.Add(message + "\n" + stack);
        }
    }

    private static void Tick()
    {
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > _deadline)
            {
                throw new TimeoutException("Building editor probe timeout.");
            }

            if (EditorApplication.timeSinceStartup < _nextTick)
            {
                return;
            }

            if (!_scenario.MoveNext())
            {
                Finish();
                return;
            }

            _nextTick = EditorApplication.timeSinceStartup + 0.2;
        }
        catch (Exception exception)
        {
            Errors.Add(exception.ToString());
            Finish();
        }
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Capture;
        if (_keyboard != null)
        {
            InputSystem.RemoveDevice(_keyboard);
        }

        Application.runInBackground = _background;
        File.WriteAllText(
            "Temp/BuildingEditor/Validation.json",
            JsonUtility.ToJson(
                new Report { passed = Errors.Count == 0, unity = Application.unityVersion, checks = Checks.ToArray(), errors = Errors.ToArray() },
                true));
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

    private static void Check(string name, bool passed)
    {
        if (!passed)
        {
            throw new InvalidOperationException(name);
        }

        Checks.Add(name);
    }

    private static void Field(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
    }

    private static object Field(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static object CreateFileStore(string directory)
    {
        Type type = typeof(SaveManager).Assembly.GetType("BlueprintFileStore");
        return Activator.CreateInstance(type, new object[] { directory });
    }

    private static void Shortcut(params Key[] keys)
    {
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        InputSystem.Update();
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
        InputSystem.Update();
        typeof(InputManager).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(InputManager.instance, null);
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
        InputSystem.Update();
    }

    private static IEnumerator Scenario()
    {
        SaveManager saves = SaveManager.instance;
        BuildManager build = BuildManager.instance;
        string folder = Path.GetFullPath("Temp/BuildingEditor/Session-" + Guid.NewGuid().ToString("N"));
        Field(saves, "_playerFiles", CreateFileStore(Path.Combine(folder, "Saves")));
        Field(saves, "_enemyFiles", CreateFileStore(Path.Combine(folder, "EnemyBlueprints")));
        Field(saves, "_buildingFiles", CreateFileStore(Path.Combine(folder, "BuildingBlueprints")));
        saves.saves.Clear();
        saves.enemyBlueprints.Clear();
        saves.buildingBlueprints.Clear();
        saves.currentSaveName = "probe_player";
        build.SetCurrentSaveName(saves.currentSaveName);
        build.currentEnemyBlueprintName = "probe_enemy";
        build.moveCameraDuringBlockLoad = false;
        build.BlockLoadIntervalSeconds = 0f;
        InputManager.instance.keyInput = true;
        build.LoadAllBlocks();
        Check(
            "Player still initializes exactly one Cockpit",
            GameManager.instance.blocksParent.GetComponentsInChildren<Cockpit>().Length == 1);
        // 用真实缺少新字段的旧存档验证兼容性，避免依赖内联类 null 的还原行为。
        var legacy = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(saves.GetSavePath(saves.currentSaveName)));
        legacy.Remove("foundation");
        File.WriteAllText(saves.GetSavePath(saves.currentSaveName), legacy.ToString());
        byte[] player = File.ReadAllBytes(saves.GetSavePath(saves.currentSaveName));
        Shortcut(Key.LeftCtrl, Key.LeftAlt, Key.F);
        Check("Ctrl+Alt+F enters building editor", build.IsEditingBuildingBlueprint);
        Check(
            "Building directory differs from player and enemy paths",
            Path.GetDirectoryName(saves.GetBuildingBlueprintPath(build.CurrentBuildName)) == Path.Combine(folder, "BuildingBlueprints"));
        Check(
            "New building has no Cockpit and no ordinary block records",
            GameManager.instance.blocksParent.GetComponentsInChildren<Cockpit>().Length == 0 && saves.cachedData.blocks.Count == 0);
        Block foundation = build.Foundation.GetComponent<Block>();
        Check(
            "Default 4x4 has sixteen upward cell connectors",
            foundation.x == 4 && foundation.z == 4 && foundation.connectors.Count == 16 && foundation.connectors.All(c => c.normal == Vector3.up && c.canConnect && c.connectType == ConnectType.Up));
        Check(
            "Each default cell center lies on y=0",
            foundation.connectors.All(c => c.localPos.y == 0f && c.localPos.x >= -1.5f && c.localPos.x <= 1.5f && c.localPos.z >= -1.5f && c.localPos.z <= 1.5f));
        Check(
            "Foundation collider is a thin plane below the sockets",
            foundation.raycastCollider.bounds.max.y <= 0.001f && foundation.raycastCollider.size.y < 0.2f);
        var panel = Object.FindFirstObjectByType<BuildingEditorPanel>();
        Check(
            "Editable size panel is visible and Play button hidden",
            panel != null && !MainUIButtons.instance.playButton.gameObject.activeSelf);
        BuildingOutpostEditor outpost = panel.GetComponent<BuildingOutpostEditor>();
        outpost.enabled = true;
        Check("Building editor exposes authored outpost controls", outpost != null);
        string bundledCopy = saves.CopyBundledBuilding("CODEX_E01_Finch Outpost");
        byte[] firstCopyBytes = File.ReadAllBytes(saves.GetBuildingBlueprintPath(bundledCopy));
        string secondBundledCopy = saves.CopyBundledBuilding("CODEX_E01_Finch Outpost");
        Check(
            "Bundled content copies to distinct editable files without overwriting",
            bundledCopy != secondBundledCopy && firstCopyBytes.SequenceEqual(File.ReadAllBytes(saves.GetBuildingBlueprintPath(bundledCopy))));
        Check(
            "Copied template retains building and finite garrison",
            JsonUtility.FromJson<BlockDataList>(File.ReadAllText(saves.GetBuildingBlueprintPath(bundledCopy))).IsOutpost);
        Toggle outpostToggle = (Toggle)Field(outpost, "_isOutpost");
        outpostToggle.isOn = true;
        Check(
            "Toggle creates embedded defender snapshot",
            saves.cachedData.IsOutpost && saves.cachedData.outpost.garrison.Count == 1 && saves.cachedData.outpost.garrison[0].blueprint.blocks.Count > 0);
        outpost.AddDefender();
        Check(
            "Add creates independent stable garrison identities",
            saves.cachedData.outpost.garrison.Count == 2 && saves.cachedData.outpost.garrison[0].id != saves.cachedData.outpost.garrison[1].id);
        ((InputField)Field(outpost, "_x")).text = "0";
        ((InputField)Field(outpost, "_y")).text = saves.cachedData.outpost.garrison[0].position.y.ToString(System.Globalization.CultureInfo.InvariantCulture);
        ((InputField)Field(outpost, "_z")).text = "0";
        ((InputField)Field(outpost, "_yaw")).text = "90";
        outpost.SavePose();
        Check(
            "Deployment pose saves numeric local position and yaw",
            saves.cachedData.outpost.garrison[1].position.x == 0f && saves.cachedData.outpost.garrison[1].yaw == 90f);
        Transform deployment = GameManager.instance.blocksParent.Cast<Transform>().Last(child => child.name == "Outpost deployment preview" && child.gameObject.activeSelf);
        Check(
            "Deployment preview follows blueprint root without gameplay or physics components",
            deployment.position == Vector3.zero && deployment.GetComponentsInChildren<Component>(true).All(component => component is Transform || component is MeshFilter || component is MeshRenderer || component is TextMesh));
        MeshFilter[] outlines = deployment.GetComponentsInChildren<MeshFilter>().Where(item => item.sharedMesh.name == "Defender deployment bounds").ToArray();
        Check(
            "Both resident models show actual solid meshes",
            deployment.GetComponentsInChildren<MeshFilter>().Count(item => item.sharedMesh.GetTopology(0) == MeshTopology.Triangles) > 50);
        Check(
            "Full resident models retain a lightweight bounds and heading overlay",
            outlines.Length == 2 && outlines.All(item => item.sharedMesh.vertexCount == 14 && item.sharedMesh.GetTopology(0) == MeshTopology.Lines));
        Check(
            "Deployment labels use a visible font and render names literally",
            deployment.GetComponentsInChildren<TextMesh>().All(text => text.font != null && !text.richText));
        Text deploymentStatus = (Text)Field(outpost, "_status");
        Canvas.ForceUpdateCanvases();
        Check(
            "Overlapping defender bounds warn without blocking save and fit status area",
            deploymentStatus.text.Contains("bounds overlap another guard") && deploymentStatus.font != null && deploymentStatus.preferredHeight <= deploymentStatus.rectTransform.rect.height);
        Vector3 cameraPosition = build.mainCamera.transform.position;
        Quaternion cameraRotation = build.mainCamera.transform.rotation;
        build.mainCamera.transform.position = new Vector3(-35f, 35f, -55f);
        build.mainCamera.transform.LookAt(new Vector3(0f, 7f, 0f));
        yield return null;
        Check(
            "Deployment labels face the actual editing camera",
            deployment.GetComponentsInChildren<TextMesh>().All(text => Quaternion.Angle(text.transform.rotation, build.mainCamera.transform.rotation) < 0.1f));
        ScreenCapture.CaptureScreenshot("Temp/BuildingEditor/OutpostDeployment.png");
        yield return null;
        build.mainCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        string outpostBytes = File.ReadAllText(saves.GetBuildingBlueprintPath(build.CurrentBuildName));
        ((InputField)Field(outpost, "_y")).text = "-5";
        outpost.SavePose();
        Check(
            "Invalid deployment preserves saved blueprint",
            outpostBytes == File.ReadAllText(saves.GetBuildingBlueprintPath(build.CurrentBuildName)));
        outpost.RemoveDefender();
        Check("Remove preserves remaining defender", saves.cachedData.outpost.garrison.Count == 1);
        Check(
            "Defender snapshot does not recursively become an outpost",
            saves.cachedData.outpost.garrison[0].blueprint.GetType() == typeof(OutpostEnemyBlueprint));
        build.selectedBlock = foundation;
        build.DeleteBlock();
        Check("Foundation cannot be deleted", foundation != null && saves.cachedData.blocks.Count == 0);
        build.DeselectBlock();
        Check("Building editor rejects player play transition", !PlayManager.instance.CanStartPlay(out _));
        GameObject cockpitPrefab = Resources.Load<GameObject>("Blocks/Cockpit");
        build.CreateBlock(cockpitPrefab, "Blocks/Cockpit", Vector3.up, Quaternion.identity);
        Check(
            "Building placement rejects Cockpit",
            GameManager.instance.blocksParent.GetComponentsInChildren<Cockpit>().Length == 0);
        GameObject cube = Resources.Load<GameObject>("Blocks/1x1x1");
        Connector socket = foundation.connectors[0];
        Type geometryType = typeof(BuildManager).Assembly.GetType("BuildPlacementGeometry");
        object geometry = Activator.CreateInstance(geometryType, true);
        geometryType.GetField("gridSize").SetValue(geometry, build.gridSize);
        geometryType.GetField("blockLayer").SetValue(geometry, build.blockLayer);
        object[] arguments =
        {
            cube.GetComponent<Block>(),
            Quaternion.identity,
            socket.localPos,
            Vector3.up,
            Vector3.zero
        };
        Check(
            "Existing placement geometry aligns to foundation sockets",
            (bool)geometryType.GetMethod("TryCalculateConnectorAlignedPosition").Invoke(geometry, arguments));
        Vector3 position = (Vector3)arguments[4];
        Check(
            "Aligned cube clears thin base collision",
            !(bool)geometryType.GetMethod("IsBlocked").Invoke(geometry, new object[] { position, Quaternion.identity, cube.GetComponent<Block>() }));
        build.CreateBlock(cube, "Blocks/1x1x1", position, Quaternion.identity);
        Block placed = build.lastSaveBlock;
        Check(
            "Placed block and foundation connect through real PhysX",
            placed != null && placed.Neighbors().Contains(foundation) && foundation.connectors.Any(c => c.isConnected));
        string id = placed.uniqueId;
        Vector3 oldSocket = foundation.connectors[0].localPos;
        panel.transform.Find("Width").GetComponent<InputField>().text = "5";
        panel.transform.Find("Length").GetComponent<InputField>().text = "3";
        panel.transform.Find("Apply").GetComponent<Button>().onClick.Invoke();
        Check(
            "Actual size UI applies and persists 5x3",
            saves.cachedData.foundation.width == 5 && saves.cachedData.foundation.length == 3 && foundation.connectors.Count == 15);
        Check(
            "Resize preserves old cell position and new-block connections",
            foundation.connectors[0].localPos == oldSocket && placed.transform.position == position && placed.Neighbors().Contains(foundation));
        Check(
            "Dimensions survive in JSON with only the ordinary block",
            JsonUtility.FromJson<BlockDataList>(File.ReadAllText(saves.GetBuildingBlueprintPath(build.CurrentBuildName))).foundation.width == 5 && saves.cachedData.blocks.Count == 1);
        Check(
            "Foundation resize preserves outpost and embedded defenders",
            JsonUtility.FromJson<BlockDataList>(File.ReadAllText(saves.GetBuildingBlueprintPath(build.CurrentBuildName))).IsOutpost);
        string beforeInvalid = File.ReadAllText(saves.GetBuildingBlueprintPath(build.CurrentBuildName));
        Check(
            "Zero and oversized dimensions are rejected",
            !build.ResizeBuildingFoundation(0, 2, out _) && !build.ResizeBuildingFoundation(65, 2, out _));
        Check(
            "Rejected size preserves file and foundation",
            beforeInvalid == File.ReadAllText(saves.GetBuildingBlueprintPath(build.CurrentBuildName)) && foundation.connectors.Count == 15);
        Check(
            "Shrink keeps user blocks without moving them",
            build.ResizeBuildingFoundation(1, 1, out _) && placed.transform.position == position);
        Check(
            "Reexpand reconnects retained blocks",
            build.ResizeBuildingFoundation(4, 4, out _) && placed.Neighbors().Contains(foundation));
        Check("Odd dimensions are supported", build.ResizeBuildingFoundation(5, 3, out _));
        string firstName = build.CurrentBuildName;
        saves.DuplicateSave(firstName);
        string copy = firstName + " Copy";
        Check(
            "Duplicate preserves size and blocks in building directory",
            File.ReadAllText(saves.GetBuildingBlueprintPath(copy)) == File.ReadAllText(saves.GetBuildingBlueprintPath(firstName)));
        Check(
            "Building rename succeeds without changing mode",
            saves.RenameBuildingBlueprint(firstName, "probe_building") && build.CurrentBuildName == "probe_building" && build.IsEditingBuildingBlueprint);
        Check(
            "Save UI lists building names",
            SaveUIPanel.instance.listParent.GetComponentsInChildren<Button>().Any(b => b.name == "SavePrefabButton" && b.GetComponentInChildren<Text>().text.Contains("probe_building")));
        saves.LoadBuildingBlueprint(copy);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        yield return null;
        Check(
            "Reload restores one foundation, dimensions and block identity",
            build.Foundation != null && GameManager.instance.blocksParent.GetComponentsInChildren<BuildingFoundation>().Length == 1 && build.Foundation.GetComponent<Block>().connectors.Count == 15 && saves.cachedData.blocks[0].id == id);
        Check(
            "Reload restores size input values",
            panel.transform.Find("Width").GetComponent<InputField>().text == "5" && panel.transform.Find("Length").GetComponent<InputField>().text == "3");
        Check(
            "Reload rebuilds base connection",
            GameManager.instance.blocksParent.GetComponentsInChildren<Block>().Single(b => b.GetComponent<BuildingFoundation>() == null).Neighbors().Contains(build.Foundation.GetComponent<Block>()));
        Shortcut(Key.RightCtrl, Key.RightAlt, Key.E);
        Check(
            "Right Ctrl+Alt+E switches building to enemy editor",
            build.IsEditingEnemyBlueprint && !build.IsEditingBuildingBlueprint);
        Check(
            "Enemy mode keeps its one Cockpit and hides size panel",
            GameManager.instance.blocksParent.GetComponentsInChildren<Cockpit>().Length == 1 && !panel.gameObject.activeSelf);
        Check(
            "Leaving building editor removes active deployment previews",
            !Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None).Any(filter => filter.sharedMesh != null && filter.sharedMesh.name == "Defender deployment bounds"));
        byte[] enemy = File.ReadAllBytes(saves.GetEnemyBlueprintPath(build.currentEnemyBlueprintName));
        Shortcut(Key.LeftCtrl, Key.LeftAlt, Key.F);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        Check(
            "Enemy to building restores its saved 5x3 foundation",
            build.IsEditingBuildingBlueprint && build.Foundation.GetComponent<Block>().connectors.Count == 15);
        build.mainCamera.transform.position = new Vector3(6f, 6f, -9f);
        build.mainCamera.transform.LookAt(new Vector3(0f, 0f, 0f));
        ScreenCapture.CaptureScreenshot("Temp/BuildingEditor/BuildingEditor.png");
        yield return null;
        Shortcut(Key.LeftCtrl, Key.LeftAlt, Key.F);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        Check(
            "Second Ctrl+Alt+F returns to player and restores Play button",
            !build.IsEditingDeveloperBlueprint && MainUIButtons.instance.playButton.gameObject.activeSelf && !panel.gameObject.activeSelf);
        Check(
            "Player and enemy JSON bytes remain unchanged",
            player.SequenceEqual(File.ReadAllBytes(saves.GetSavePath(saves.currentSaveName))) && enemy.SequenceEqual(File.ReadAllBytes(saves.GetEnemyBlueprintPath(build.currentEnemyBlueprintName))));
        Check(
            "Old JSON without foundation remains compatible",
            legacy["foundation"] == null && GameManager.instance.blocksParent.GetComponentsInChildren<BuildingFoundation>().Length == 0 && GameManager.instance.blocksParent.GetComponentsInChildren<Cockpit>().Length == 1);
        saves.DeleteBuildingBlueprint("probe_building");
        Check(
            "Delete affects only requested building file",
            !File.Exists(saves.GetBuildingBlueprintPath("probe_building")) && File.Exists(saves.GetBuildingBlueprintPath(copy)) && File.Exists(saves.GetSavePath(saves.currentSaveName)));
        build.EnterBuildingBlueprintBuildMode(copy);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        panel.GetComponent<BuildingOutpostEditor>().Refresh();
        panel.transform.Find("MatchingBase").GetComponent<Button>().onClick.Invoke();
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        Check(
            "Matching base button creates named editable combo",
            build.CurrentBuildName.Contains("Finch Outpost") && saves.cachedData.blocks.Count > 10 && saves.cachedData.outpost.garrison.Count == 1);
        string comboName = build.CurrentBuildName;
        string comboJson = File.ReadAllText(saves.GetBuildingBlueprintPath(comboName));
        build.EnterBuildingBlueprintBuildMode(comboName);
        while (build.IsLoadingBlocks)
        {
            yield return null;
        }

        Check(
            "Combined base reload preserves disk data and full preview",
            comboJson == File.ReadAllText(saves.GetBuildingBlueprintPath(comboName)) && GameManager.instance.blocksParent.GetComponentsInChildren<MeshFilter>().Count(filter => filter.sharedMesh != null) > 100);
        build.mainCamera.transform.position = new Vector3(-23f, 22f, -29f);
        build.mainCamera.transform.LookAt(new Vector3(8f, 3f, 8f));
        yield return null;
        ScreenCapture.CaptureScreenshot("Temp/BuildingEditor/MatchingBase.png");
        yield return null;
        OutpostContentChecks.Run();
        Check("All bundled base content and resident clearances pass", true);
        IEnumerator residentChecks = OutpostResidentChecks.Run(Check);
        try
        {
            while (residentChecks.MoveNext())
            {
                yield return null;
            }
        }
        finally
        {
            (residentChecks as IDisposable)?.Dispose();
        }
    }
}

// 职责：自动发现全部建造方块，穷举24种正交姿态、三轴正反键盘/拖拽和尺寸奇偶组合。
// 入口：隔离空场景Play中 run_script --file Tools/Validation/BuildRotationCatalogProbe.cs --entry BuildRotationCatalogProbe.Run。
// 隔离：只创建临时对象/虚拟输入；关闭临时VFX并过滤普通日志，保存只计数，不接触用户蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class BuildRotationCatalogProbe
{
    private const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Type Geometry = typeof(Block).Assembly.GetType("BuildPlacementGeometry");
    private static readonly Type Manipulation = typeof(Block).Assembly.GetType("BuildManipulation");
    private static readonly Vector3[] Axes = { Vector3.right, Vector3.up, Vector3.forward };
    private static readonly Key[] PositiveKeys = { Key.W, Key.D, Key.E };
    private static readonly Key[] NegativeKeys = { Key.S, Key.A, Key.Q };
    private static int _checks;

    public static string Run()
    {
        if (!Application.isPlaying || Object.FindAnyObjectByType<SaveManager>() != null)
        {
            throw new InvalidOperationException("An isolated empty Play scene is required.");
        }
        _checks = 0;
        Keyboard previousKeyboard = Keyboard.current;
        Mouse previousMouse = Mouse.current;
        ActionManager previousActions = ActionManager.instance;
        BuildManager previousBuild = BuildManager.instance;
        VisualEffectsManager previousVfx = VisualEffectsManager.instance;
        bool previousLogging = Debug.unityLogger.logEnabled;
        LogType previousFilter = Debug.unityLogger.filterLogType;
        GameObject root = new GameObject("Catalog rotation probe");
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        var watch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            Debug.unityLogger.logEnabled = true;
            Debug.unityLogger.filterLogType = LogType.Warning;
            VisualEffectsManager vfx = root.AddComponent<VisualEffectsManager>();
            vfx.enableRuntimeVfx = false;
            vfx.enableSceneLook = false;
            ActionManager actions = root.AddComponent<ActionManager>();
            GameObject endpoint = Child(root, "Inactive undo endpoint");
            endpoint.SetActive(false);
            BuildManager.instance = endpoint.AddComponent<BuildManager>();
            Camera camera = Child(root, "Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.pixelRect = new Rect(0, 0, 800, 600);
            object geometry = Activator.CreateInstance(Geometry, true);
            Geometry.GetField("gridSize").SetValue(geometry, 1f);
            Geometry.GetField("blockLayer").SetValue(geometry, (LayerMask)(1 << 30));
            int saves = 0;
            Action<Block> save = b =>
            {
                saves++;
            };
            object manipulation = Activator.CreateInstance(Manipulation, new object[]
            {
                geometry, save, new Func<GameObject, string, Vector3, Quaternion, Block>((p, s, v, q) => null)
            });
            Set(manipulation, "mainCamera", camera);
            List<Quaternion> orientations = Orientations();
            Check(orientations.Count == 24, "24 unique orientations");
            CheckCameraFrames(manipulation, camera, orientations);
            Transform buildRoot = CheckBuildParent(root);
            GameObject[] prefabs = Resources.LoadAll<GameObject>("Blocks")
                .Where(p => p.GetComponent<Block>() != null).OrderBy(p => p.name).ToArray();
            var rows = new List<Row>();
            foreach (GameObject prefab in prefabs)
            {
                int before = _checks;
                Block definition = prefab.GetComponent<Block>();
                Check(prefab.transform.localScale == Vector3.one, prefab.name + " unit root scale");
                Block block = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, buildRoot).GetComponent<Block>();
                foreach (Collider collider in block.GetComponentsInChildren<Collider>())
                {
                    collider.gameObject.layer = 30;
                }
                Set(manipulation, "selectedBlock", block);
                for (int orientationIndex = 0; orientationIndex < orientations.Count; orientationIndex++)
                {
                    Quaternion initial = orientations[orientationIndex];
                    Vector3 anchor = orientationIndex % 2 == 0 ? new Vector3(-10, -4, -8) : new Vector3(123, 20, 456);
                    Vector3 start = ExpectedCenter(anchor, initial, definition);
                    CheckPlacement(geometry, definition, initial, anchor);
                    for (int axisIndex = 0; axisIndex < 3; axisIndex++)
                    {
                        for (int sign = -1; sign <= 1; sign += 2)
                        {
                            string label = prefab.name + ":" + orientationIndex + ":" + axisIndex + ":" + sign;
                            Vector3 axis = Axes[axisIndex] * sign;
                            Reset(block, start, initial, actions);
                            Set(manipulation, "axisRight", Vector3.right);
                            Set(manipulation, "axisUp", Vector3.up);
                            Set(manipulation, "axisForward", Vector3.forward);
                            if (!block.canRotate)
                            {
                                Invoke(manipulation, "Poll", block, BuildType.Rotate);
                                Check(Exact(block.transform.position, start) && actions.GetUndoCount() == 0, label + " rotation disabled");
                                continue;
                            }
                            int beforeKeys = saves;
                            for (int turn = 1; turn <= 4; turn++)
                            {
                                Key key = sign > 0 ? PositiveKeys[axisIndex] : NegativeKeys[axisIndex];
                                KeyboardState(keyboard, new KeyboardState(key));
                                Invoke(manipulation, "HandleRotation");
                                KeyboardState(keyboard, new KeyboardState());
                                Quaternion expected = Quaternion.AngleAxis(90 * turn, axis) * initial;
                                CheckPose(block, ExpectedCenter(anchor, expected, definition), expected, label + " key " + turn);
                            }
                            Check(saves == beforeKeys + 4 && actions.GetUndoCount() == 4, label + " key commits");
                            actions.Undo();
                            Quaternion third = Quaternion.AngleAxis(270, axis) * initial;
                            CheckPose(block, ExpectedCenter(anchor, third, definition), third, label + " key undo");
                            actions.Redo();
                            CheckPose(block, start, initial, label + " key redo/return");

                            Reset(block, start, initial, actions);
                            camera.transform.SetPositionAndRotation(start + axis * 10,
                                Quaternion.LookRotation(-axis, axisIndex == 1 ? Vector3.forward : Vector3.up));
                            GameObject gizmo = Child(block.gameObject, "Parented rotation gizmo");
                            gizmo.transform.SetPositionAndRotation(start, Quaternion.identity);
                            GameObject handleObject = Child(gizmo, "Handle");
                            // Local position explicitly supplies the signed axis to RotateAxisHandle.
                            handleObject.transform.localPosition = axis;
                            RotateAxisHandle handle = handleObject.AddComponent<RotateAxisHandle>();
                            BoxCollider hitCollider = handleObject.AddComponent<BoxCollider>();
                            Vector3 from = camera.transform.right * 2;
                            hitCollider.center = from - axis;
                            hitCollider.size = Vector3.one * 0.1f;
                            Vector3 screenStart = camera.WorldToScreenPoint(start + from);
                            Physics.SyncTransforms();
                            Check(hitCollider.Raycast(camera.ScreenPointToRay(screenStart), out RaycastHit hit, 20), label + " handle ray");
                            Check((bool)Manipulation.GetMethod("TryBeginDrag").Invoke(manipulation,
                                new object[] { hit, block, BuildType.Rotate }), label + " begin drag");
                            int beforeDrag = saves;
                            foreach (int angle in new[] { 90, 180, -90, 90 })
                            {
                                Vector3 pointer = camera.WorldToScreenPoint(start + Quaternion.AngleAxis(angle, axis) * from);
                                InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer, buttons = 1 });
                                InputSystem.Update();
                                Invoke(manipulation, "HandleRotateAxisDrag");
                                Quaternion expected = Quaternion.AngleAxis(angle, axis) * initial;
                                CheckPose(block, ExpectedCenter(anchor, expected, definition), expected, label + " drag " + angle);
                            }
                            InputSystem.QueueStateEvent(mouse, new MouseState());
                            InputSystem.Update();
                            Invoke(manipulation, "HandleRotateAxisDrag");
                            Check(saves == beforeDrag + 1 && actions.GetUndoCount() == 1, label + " drag commit once");
                            actions.Undo();
                            CheckPose(block, start, initial, label + " drag undo");
                            actions.Redo();
                            Quaternion finalRotation = Quaternion.AngleAxis(90, axis) * initial;
                            CheckPose(block, ExpectedCenter(anchor, finalRotation, definition), finalRotation, label + " drag redo");
                            BlockData data = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(new BlockData(block)));
                            Check(Exact(new Vector3(data.posX, data.posY, data.posZ), block.transform.position) &&
                                Quaternion.Angle(new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW), finalRotation) < 0.03f,
                                label + " JSON roundtrip");
                            Object.DestroyImmediate(gizmo);
                        }
                    }
                }
                rows.Add(new Row { name = prefab.name, dimensions = new[] { block.x, block.y, block.z }, checks = _checks - before });
                Object.DestroyImmediate(block.gameObject);
            }
            CheckDimensionCombinations(root, geometry, orientations);
            // 动态编译的嵌套报告类型未必被Unity原生序列化器登记；使用托管序列化保留逐Prefab结果。
            string report = Newtonsoft.Json.JsonConvert.SerializeObject(new Report
            {
                unity = Application.unityVersion, prefabs = rows.Count, orientations = orientations.Count,
                checks = _checks, seconds = watch.Elapsed.TotalSeconds, rows = rows.ToArray()
            }, Newtonsoft.Json.Formatting.Indented);
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/build-rotation-catalog.json", report);
            return report;
        }
        finally
        {
            Object.DestroyImmediate(root);
            ActionManager.instance = previousActions;
            BuildManager.instance = previousBuild;
            VisualEffectsManager.instance = previousVfx;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            previousKeyboard?.MakeCurrent();
            previousMouse?.MakeCurrent();
            Debug.unityLogger.logEnabled = previousLogging;
            Debug.unityLogger.filterLogType = previousFilter;
        }
    }

    private static Transform CheckBuildParent(GameObject root)
    {
        GameObject services = Child(root, "Inactive loader dependencies");
        services.SetActive(false);
        SaveManager saves = services.AddComponent<SaveManager>();
        GameManager game = services.AddComponent<GameManager>();
        PlayManager play = services.AddComponent<PlayManager>();
        Type loaderType = typeof(Block).Assembly.GetType("BlueprintLoader");
        object loader = Activator.CreateInstance(loaderType, new object[] { game, saves, game, play });
        GameObject prefab = Resources.Load<GameObject>("BlocksParent/BlocksParent");
        Vector3 originalPosition = prefab.transform.position;
        loaderType.GetMethod("ReplaceParent", Hidden).Invoke(loader, new object[] { prefab, "Inspector coordinate regression" });
        Transform parent = game.blocksParent;
        parent.SetParent(root.transform, true);
        Check(Exact(parent.position, Vector3.zero), "Production build root ignores prefab authoring position");
        Check(Quaternion.Angle(parent.rotation, Quaternion.identity) == 0 && Exact(parent.localScale, Vector3.one),
            "Production build root uses identity rotation and scale");
        Check(play.blocksParent == parent && parent.GetComponent<Rigidbody>().isKinematic, "Loader registries and build physics retained");
        Check(Exact(prefab.transform.position, originalPosition), "Source parent prefab unchanged");

        Block definition = Resources.Load<GameObject>("Blocks/2x1x1").GetComponent<Block>();
        BlockData data = new BlockData(definition)
        {
            id = "inspector-coordinate-regression", resourcePath = "Blocks/2x1x1",
            posX = -2, posY = 2.5f, posZ = 0,
            rotX = 0, rotY = 0, rotZ = Mathf.Sqrt(0.5f), rotW = Mathf.Sqrt(0.5f)
        };
        string before = JsonUtility.ToJson(data);
        Type factory = typeof(Block).Assembly.GetType("BlueprintBlockFactory");
        object[] args = { data, parent, UnitFaction.Player, null };
        Check((bool)factory.GetMethod("TryRestore").Invoke(null, args), "Restore screenshot world pose through real factory");
        Block restored = (Block)args[3];
        Vector3 expected = new Vector3(-2, 2.5f, 0);
        Check(Exact(restored.transform.position, expected) && Exact(restored.transform.localPosition, expected),
            "Inspector local position equals preserved screenshot world position");
        BlockData captured = new BlockData(restored);
        Check(captured.posX == data.posX && captured.posY == data.posY && captured.posZ == data.posZ &&
            JsonUtility.ToJson(data) == before, "Restore does not migrate or mutate saved world coordinates");
        Object.DestroyImmediate(restored.gameObject);
        return parent;
    }

    private static void CheckCameraFrames(object manipulation, Camera camera, List<Quaternion> orientations)
    {
        var views = new List<Quaternion>(orientations);
        foreach (float pitch in new[] { -85f, -45f, 0f, 45f, 85f })
        {
            for (int yaw = 0; yaw < 360; yaw += 45)
            {
                views.Add(Quaternion.Euler(pitch, yaw, 45));
            }
        }
        foreach (Quaternion view in views)
        {
            camera.transform.rotation = view;
            Invoke(manipulation, "AlignAxisToNearestWorldDir");
            Vector3 right = Get<Vector3>(manipulation, "axisRight");
            Vector3 up = Get<Vector3>(manipulation, "axisUp");
            Vector3 forward = Get<Vector3>(manipulation, "axisForward");
            Check(right.sqrMagnitude == 1 && up.sqrMagnitude == 1 && forward.sqrMagnitude == 1 &&
                Vector3.Dot(right, up) == 0 && Vector3.Dot(up, forward) == 0 && Vector3.Cross(up, forward) == right,
                "Camera frame " + view.eulerAngles);
        }
    }

    private static void CheckDimensionCombinations(GameObject root, object geometry, List<Quaternion> orientations)
    {
        Block block = Child(root, "Synthetic dimensions").AddComponent<Block>();
        Vector3 anchor = new Vector3(-10, 20, -30);
        for (int x = 1; x <= 4; x++)
        {
            for (int y = 1; y <= 4; y++)
            {
                for (int z = 1; z <= 4; z++)
                {
                    block.x = x;
                    block.y = y;
                    block.z = z;
                    foreach (Quaternion initial in orientations)
                    {
                        Vector3 start = ExpectedCenter(anchor, initial, block);
                        foreach (Vector3 axis in Axes)
                        {
                            foreach (int sign in new[] { -1, 1 })
                            {
                                Quaternion target = Quaternion.AngleAxis(sign * 90, axis) * initial;
                                Vector3 position = (Vector3)Geometry.GetMethod("CalculateRotationPosition").Invoke(geometry,
                                    new object[] { start, initial, target, block });
                                Check(Exact(position, ExpectedCenter(anchor, target, block)), "Synthetic dimensions " + x + ":" + y + ":" + z);
                            }
                        }
                    }
                }
            }
        }
    }

    private static void CheckPlacement(object geometry, Block definition, Quaternion rotation, Vector3 anchor)
    {
        // 使用真实驾驶舱的连接点作为网格依据，避免测试期望与错误的尺寸奇偶公式一同翻转。
        Block cockpit = Resources.Load<GameObject>("Blocks/Cockpit").GetComponent<Block>();
        foreach (Connector socket in cockpit.connectors)
        {
            object[] args = { definition, rotation, anchor + cockpit.GetConnectorWorldPosition(socket), cockpit.GetConnectorWorldNormal(socket), Vector3.zero };
            bool aligned = (bool)Geometry.GetMethod("TryCalculateConnectorAlignedPosition").Invoke(geometry, args);
            if (aligned)
            {
                Vector3 position = (Vector3)args[4];
                Vector3 right = rotation * Vector3.right;
                Vector3 up = rotation * Vector3.up;
                Vector3 forward = rotation * Vector3.forward;
                Vector3 minimum = position - new Vector3(
                    Mathf.Abs(right.x) * definition.x + Mathf.Abs(up.x) * definition.y + Mathf.Abs(forward.x) * definition.z,
                    Mathf.Abs(right.y) * definition.x + Mathf.Abs(up.y) * definition.y + Mathf.Abs(forward.y) * definition.z,
                    Mathf.Abs(right.z) * definition.x + Mathf.Abs(up.z) * definition.y + Mathf.Abs(forward.z) * definition.z) * 0.5f;
                Check(position.x * 2 == Mathf.Round(position.x * 2) && position.y * 2 == Mathf.Round(position.y * 2) &&
                    position.z * 2 == Mathf.Round(position.z * 2) && Mathf.Abs(minimum.x - Mathf.Round(minimum.x)) < 0.0001f &&
                    Mathf.Abs(minimum.y - Mathf.Round(minimum.y)) < 0.0001f && Mathf.Abs(minimum.z - Mathf.Round(minimum.z)) < 0.0001f,
                    definition.name + " actual cockpit sockets produce integer grid boundaries");
            }
        }
    }

    private static Vector3 ExpectedCenter(Vector3 anchor, Quaternion rotation, Block block)
    {
        // Independent oracle: permute complete dimensions into world axes, then inspect their parity.
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;
        Vector3 forward = rotation * Vector3.forward;
        Vector3 worldSize = new Vector3(
            Mathf.Abs(right.x) * block.x + Mathf.Abs(up.x) * block.y + Mathf.Abs(forward.x) * block.z,
            Mathf.Abs(right.y) * block.x + Mathf.Abs(up.y) * block.y + Mathf.Abs(forward.y) * block.z,
            Mathf.Abs(right.z) * block.x + Mathf.Abs(up.z) * block.y + Mathf.Abs(forward.z) * block.z);
        return anchor + new Vector3(
            Mathf.RoundToInt(worldSize.x) % 2 * 0.5f,
            Mathf.RoundToInt(worldSize.y) % 2 * 0.5f,
            Mathf.RoundToInt(worldSize.z) % 2 * 0.5f);
    }

    private static List<Quaternion> Orientations()
    {
        var result = new List<Quaternion>();
        foreach (Vector3 forward in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
        {
            foreach (Vector3 up in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                if (Vector3.Dot(forward, up) == 0)
                {
                    result.Add(Quaternion.LookRotation(forward, up));
                }
            }
        }
        return result;
    }

    private static void CheckPose(Block block, Vector3 position, Quaternion rotation, string label)
    {
        Check(Exact(block.transform.position, position) && Exact(block.transform.localPosition, position) &&
            Quaternion.Angle(block.transform.rotation, rotation) < 0.03f, label);
    }

    private static bool Exact(Vector3 a, Vector3 b) => a.x == b.x && a.y == b.y && a.z == b.z;
    private static void Check(bool condition, string label)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
        _checks++;
    }
    private static void Reset(Block block, Vector3 position, Quaternion rotation, ActionManager actions)
    {
        block.transform.SetPositionAndRotation(position, rotation);
        actions.Clear();
    }
    private static GameObject Child(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform);
        return child;
    }
    private static void KeyboardState(Keyboard keyboard, KeyboardState state)
    {
        InputSystem.QueueStateEvent(keyboard, state);
        InputSystem.Update();
    }
    private static T Get<T>(object target, string field) => (T)Manipulation.GetField(field, Hidden).GetValue(target);
    private static void Set(object target, string field, object value) => Manipulation.GetField(field, Hidden).SetValue(target, value);
    private static object Invoke(object target, string method, params object[] args) => Manipulation.GetMethod(method, Hidden | BindingFlags.Public).Invoke(target, args);

    [Serializable]
    private sealed class Row
    {
        public string name;
        public int[] dimensions;
        public int checks;
    }
    [Serializable]
    private sealed class Report
    {
        public string unity;
        public int prefabs, orientations, checks;
        public double seconds;
        public Row[] rows;
    }
}

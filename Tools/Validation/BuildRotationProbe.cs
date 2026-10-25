// 职责：在隔离空场景 Play Mode 验证旋转前后尺寸奇偶对齐、无浮点尾差、阻挡和 Undo。
// 入口：unity command run_script --file Tools/Validation/BuildRotationProbe.cs --entry BuildRotationProbe.Run。
// 隔离：只使用临时对象/虚拟输入设备，保存回调仅采样内存，不写用户蓝图。
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

public static class BuildRotationProbe
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly Type GeometryType = typeof(Block).Assembly.GetType("BuildPlacementGeometry");
    private static readonly Type ManipulationType = typeof(Block).Assembly.GetType("BuildManipulation");

    public static string Run()
    {
        if (!Application.isPlaying || Object.FindAnyObjectByType<SaveManager>() != null)
        {
            throw new InvalidOperationException("Run in an empty Play scene without user save services.");
        }

        var checks = new List<string>();
        GameObject root = new GameObject("Build rotation probe");
        Keyboard previousKeyboard = Keyboard.current;
        Mouse previousMouse = Mouse.current;
        Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
        Mouse mouse = InputSystem.AddDevice<Mouse>();
        ActionManager previousActions = ActionManager.instance;
        BuildManager previousBuild = BuildManager.instance;
        VisualEffectsManager previousVfx = VisualEffectsManager.instance;
        try
        {
            ActionManager actions = root.AddComponent<ActionManager>();
            GameObject managerObject = Child(root, "Inactive undo save endpoint");
            managerObject.SetActive(false);
            BuildManager.instance = managerObject.AddComponent<BuildManager>();
            Camera camera = Child(root, "Drag camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5;
            camera.pixelRect = new Rect(0, 0, 800, 600);
            object geometry = Activator.CreateInstance(GeometryType, true);
            GeometryType.GetField("gridSize").SetValue(geometry, 1f);
            GeometryType.GetField("blockLayer").SetValue(geometry, (LayerMask)(1 << 30));
            int saveCount = 0;
            Action<Block> save = block => { saveCount++; };
            var manipulation = Activator.CreateInstance(ManipulationType, new object[]
            {
                geometry, save, new Func<GameObject, string, Vector3, Quaternion, Block>((p, s, v, q) => null)
            });
            Set(manipulation, "mainCamera", camera);

            foreach (string resource in new[] { "Blocks/1x1x1", "Blocks/2x1x1", "Blocks/2x2x1", "Blocks/2x2x2" })
            {
                GameObject prefab = Resources.Load<GameObject>(resource);
                Require(prefab != null, resource + " exists", checks);
                Block definition = prefab.GetComponent<Block>();
                // A unit cube's upper socket. Even-length faces require a half-cell tangent offset.
                object[] placement = { definition, Quaternion.identity, new Vector3(0, 0.5f, 0), Vector3.up, Vector3.zero };
                bool aligned = (bool)GeometryType.GetMethod("TryCalculateConnectorAlignedPosition").Invoke(geometry, placement);
                Require(aligned, resource + " connector placement", checks);
                Vector3 center = (Vector3)placement[4];
                Block block = Object.Instantiate(prefab, center, Quaternion.identity, root.transform).GetComponent<Block>();
                foreach (Collider collider in block.GetComponentsInChildren<Collider>())
                {
                    collider.gameObject.layer = 30;
                }
                Set(manipulation, "selectedBlock", block);
                Require(block.transform.position == center, resource + " creation preserves connector center", checks);
                foreach (float xAngle in new[] { 0f, 90f, 180f, 270f })
                {
                    foreach (float yAngle in new[] { 0f, 90f, 180f, 270f })
                    {
                        Quaternion facing = Quaternion.Euler(xAngle, yAngle, 0);
                        object[] rotatedPlacement = { definition, facing, new Vector3(0, 0.5f, 0), facing * Vector3.up, Vector3.zero };
                        bool success = (bool)GeometryType.GetMethod("TryCalculateConnectorAlignedPosition").Invoke(geometry, rotatedPlacement);
                        Vector3 result = (Vector3)rotatedPlacement[4];
                        Require(success && result.x * 2 == Mathf.Round(result.x * 2) &&
                            result.y * 2 == Mathf.Round(result.y * 2) && result.z * 2 == Mathf.Round(result.z * 2),
                            resource + " rotated connector center is exact half-grid " + xAngle + ":" + yAngle, checks);
                    }
                }

                Quaternion initial = Quaternion.Euler(0, 90, 0);
                float a = block.x % 2 == 0 ? 0.5f : 0f;
                float b = block.y % 2 == 0 ? 0.5f : 0f;
                float c = block.z % 2 == 0 ? 0.5f : 0f;
                Vector3 anchor = new Vector3(0, 1, 0);
                // 已知 Y=90° 起始姿态、绕世界 X 四次的解析解，不调用被测位置算法求期望。
                Vector3[] centers =
                {
                    anchor + new Vector3(c, b, a),
                    anchor + new Vector3(c, a, b),
                    anchor + new Vector3(c, b, a),
                    anchor + new Vector3(c, a, b)
                };
                block.transform.position = centers[0];
                block.transform.rotation = initial;
                Set(manipulation, "axisRight", Vector3.right);
                Set(manipulation, "axisUp", Vector3.up);
                Set(manipulation, "axisForward", Vector3.forward);
                actions.Clear();
                for (int turn = 1; turn <= 4; turn++)
                {
                    KeyboardState(keyboard, new KeyboardState());
                    KeyboardState(keyboard, new KeyboardState(Key.W));
                    Invoke(manipulation, "HandleRotation");
                    Quaternion expected = Quaternion.AngleAxis(90 * turn, Vector3.right) * initial;
                    Require(Exact(block.transform.position, centers[turn % 4]) && Quaternion.Angle(block.transform.rotation, expected) < 0.02f,
                        resource + " keyboard world-axis turn " + turn, checks);
                }
                Require(actions.GetUndoCount() == 4, resource + " keyboard undo entries", checks);
                actions.Undo();
                Require(Exact(block.transform.position, centers[3]) && Quaternion.Angle(block.transform.rotation,
                    Quaternion.AngleAxis(270, Vector3.right) * initial) < 0.02f, resource + " undo center and orientation", checks);
                actions.Redo();
                Require(Exact(block.transform.position, centers[0]) && Quaternion.Angle(block.transform.rotation, initial) < 0.02f,
                    resource + " redo center and orientation", checks);
                KeyboardState(keyboard, new KeyboardState());

                RotateAxisHandle handle = Child(root, "right drag handle").AddComponent<RotateAxisHandle>();
                Set(manipulation, "activeRotateHandle", handle);
                Set(manipulation, "blockStartRot", initial);
                Set(manipulation, "blockStartRotatePos", centers[0]);
                Set(manipulation, "rotateLastRot", initial);
                Set(manipulation, "rotateLastPos", centers[0]);
                camera.transform.SetPositionAndRotation(centers[0] + Vector3.right * 10,
                    Quaternion.LookRotation(Vector3.left, Vector3.up));
                Vector3 from = camera.transform.right * 2;
                Set(manipulation, "rotateDragStart", centers[0] + from);
                actions.Clear();
                int beforeDrag = saveCount;
                foreach (float angle in new[] { 15f, 30f, 60f, 90f, 150f, 0f, -90f, 90f })
                {
                    Vector3 pointer = camera.WorldToScreenPoint(centers[0] + Quaternion.AngleAxis(angle, Vector3.right) * from);
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = pointer, buttons = 1 });
                    InputSystem.Update();
                    Invoke(manipulation, "HandleRotateAxisDrag");
                    int turns = Mathf.RoundToInt(angle / 90);
                    Require(Exact(block.transform.position, centers[(turns % 4 + 4) % 4]) && Quaternion.Angle(block.transform.rotation,
                        Quaternion.AngleAxis(turns * 90, Vector3.right) * initial) < 0.02f,
                        resource + " grid-anchor drag " + angle, checks);
                }
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                Invoke(manipulation, "HandleRotateAxisDrag");
                Require(saveCount == beforeDrag + 1 && actions.GetUndoCount() == 1,
                    resource + " drag commits once on release", checks);
                actions.Undo();
                Require(Exact(block.transform.position, centers[0]) && Quaternion.Angle(block.transform.rotation, initial) < 0.02f,
                    resource + " drag undo", checks);
                actions.Redo();
                Require(Exact(block.transform.position, centers[1]) && Quaternion.Angle(block.transform.rotation,
                    Quaternion.AngleAxis(90, Vector3.right) * initial) < 0.02f, resource + " drag redo", checks);

                GameObject obstacle = Child(root, "Rotation blocker");
                obstacle.layer = 30;
                obstacle.transform.position = centers[2];
                obstacle.AddComponent<Block>();
                obstacle.AddComponent<BoxCollider>();
                Quaternion beforeBlocked = block.transform.rotation;
                int undoCount = actions.GetUndoCount();
                int savesBeforeBlocked = saveCount;
                KeyboardState(keyboard, new KeyboardState(Key.W));
                Invoke(manipulation, "HandleRotation");
                Require(Exact(block.transform.position, centers[1]) && block.transform.rotation == beforeBlocked &&
                    actions.GetUndoCount() == undoCount && saveCount == savesBeforeBlocked,
                    resource + " blocked rotation changes neither pose nor history", checks);
                KeyboardState(keyboard, new KeyboardState());
                Object.DestroyImmediate(obstacle);
                Object.DestroyImmediate(block.gameObject);
                Object.DestroyImmediate(handle.gameObject);
            }

            // 用户提供的明确回归：X 半格偏移必须换到 Z，逐分量严格相等，不使用 Vector3 的近似比较。
            Block even = Resources.Load<GameObject>("Blocks/2x1x1").GetComponent<Block>();
            Vector3 start = new Vector3(0.5f, 1, 0);
            Block userBlock = Object.Instantiate(even.gameObject, start, Quaternion.identity, root.transform).GetComponent<Block>();
            Set(manipulation, "selectedBlock", userBlock);
            Vector3[] userPositions =
            {
                new Vector3(0, 1, 0.5f), start,
                new Vector3(0, 1, 0.5f), start
            };
            for (int turn = 0; turn < 4; turn++)
            {
                KeyboardState(keyboard, new KeyboardState(Key.D));
                Invoke(manipulation, "HandleRotation");
                KeyboardState(keyboard, new KeyboardState());
                Require(Exact(userBlock.transform.position, userPositions[turn]), "User 2x1x1 real D key quarter-turn " + turn, checks);
                BlockData data = JsonUtility.FromJson<BlockData>(JsonUtility.ToJson(new BlockData(userBlock)));
                Block restored = Object.Instantiate(even.gameObject, new Vector3(data.posX, data.posY, data.posZ),
                    new Quaternion(data.rotX, data.rotY, data.rotZ, data.rotW), root.transform).GetComponent<Block>();
                Require(Exact(restored.transform.position, userPositions[turn]) &&
                    Quaternion.Angle(restored.transform.rotation, userBlock.transform.rotation) < 0.02f,
                    "User 2x1x1 JSON and Awake roundtrip " + turn, checks);
                Object.DestroyImmediate(restored.gameObject);
            }
            Object.DestroyImmediate(userBlock.gameObject);
            foreach (Vector3 origin in new[] { Vector3.zero, new Vector3(-10, -4, -8), new Vector3(123, 20, 456) })
            {
                Vector3 position = start + origin;
                Quaternion rotation = Quaternion.identity;
                Vector3[] expected =
                {
                    new Vector3(0, 1, 0.5f), start,
                    new Vector3(0, 1, 0.5f), start
                };
                for (int turn = 0; turn < 100; turn++)
                {
                    Quaternion next = Quaternion.Euler(0, ((turn + 1) % 4) * 90, 0);
                    position = (Vector3)GeometryType.GetMethod("CalculateRotationPosition").Invoke(geometry,
                        new object[] { position, rotation, next, even });
                    rotation = next;
                    Require(Exact(position, expected[turn % 4] + origin), "2x1x1 exact parity cycle " + origin + ":" + turn, checks);
                }
            }
            string report = JsonUtility.ToJson(new Report
            {
                unity = Application.unityVersion,
                passed = checks.Count,
                checks = checks.ToArray()
            }, true);
            Directory.CreateDirectory("Temp");
            File.WriteAllText("Temp/build-rotation-probe.json", report);
            return report;
        }
        finally
        {
            Object.DestroyImmediate(root);
            if (previousVfx == null && VisualEffectsManager.instance != null)
            {
                Object.DestroyImmediate(VisualEffectsManager.instance.gameObject);
            }
            ActionManager.instance = previousActions;
            BuildManager.instance = previousBuild;
            InputSystem.RemoveDevice(keyboard);
            InputSystem.RemoveDevice(mouse);
            previousKeyboard?.MakeCurrent();
            previousMouse?.MakeCurrent();
        }
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

    private static void Set(object instance, string field, object value)
    {
        ManipulationType.GetField(field, Private).SetValue(instance, value);
    }

    private static void Invoke(object instance, string method)
    {
        ManipulationType.GetMethod(method, Private).Invoke(instance, null);
    }

    private static void Require(bool condition, string label, List<string> checks)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
        checks.Add(label);
    }

    private static bool Exact(Vector3 actual, Vector3 expected)
    {
        return actual.x == expected.x && actual.y == expected.y && actual.z == expected.z;
    }

    [Serializable]
    private sealed class Report
    {
        public string unity;
        public int passed;
        public string[] checks;
    }
}
